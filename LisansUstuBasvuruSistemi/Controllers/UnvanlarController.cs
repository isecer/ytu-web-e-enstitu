using BiskaUtil;
using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.MenuAndRoles;
using System;
using System.Linq;
using System.Web.Mvc;

namespace LisansUstuBasvuruSistemi.Controllers
{
    [System.Web.Mvc.OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    [Authorize(Roles = RoleNames.Unvanlar)]
    public class UnvanlarController : Controller
    {
        private LisansustuBasvuruSistemiEntities db = new LisansustuBasvuruSistemiEntities();
        [Authorize(Roles = RoleNames.Unvanlar)]
        public ActionResult Index()
        {
            return Index(new fmUnvanlar { });
        }
        [HttpPost]
        public ActionResult Index(fmUnvanlar model)
        {

            var q = from s in db.Unvanlars
                    select s;

            if (model.UnvanSiraNo.HasValue) q = q.Where(p => p.UnvanSiraNo == model.UnvanSiraNo);
            if (!model.UnvanAdi.IsNullOrWhiteSpace()) q = q.Where(p => p.UnvanAdi.Contains(model.UnvanAdi));
            if (model.IsAktif.HasValue) q = q.Where(p => p.IsAktif == model.IsAktif.Value);
            model.RowCount = q.Count();
            if (!model.Sort.IsNullOrWhiteSpace()) q = q.OrderBy(model.Sort);
            else q = q.OrderBy(o => o.UnvanAdi);
            var PS = Management.setStartRowInx(model.StartRowIndex, model.PageIndex, model.PageCount, model.RowCount, model.PageSize);
            model.PageIndex = PS.PageIndex;
            model.data = q.Skip(PS.StartRowIndex).Take(model.PageSize).ToArray();
            var IndexModel = new MIndexBilgi();
            IndexModel.Toplam = model.RowCount;
            IndexModel.Aktif = q.Where(p => p.IsAktif).Count();
            IndexModel.Pasif = q.Where(p => !p.IsAktif).Count();
            ViewBag.IndexModel = IndexModel;
            ViewBag.IsAktif = new SelectList(Management.cmbAktifPasifData(true), "Value", "Caption", model.IsAktif);
            return View(model);
        }
        public ActionResult Kayit(int? id, string dlgid)
        {
            var MmMessage = new MmMessage();
            MmMessage.IsDialog = !dlgid.IsNullOrWhiteSpace();
            MmMessage.DialogID = dlgid;
            ViewBag.MmMessage = MmMessage;
            var model = new Unvanlar();
            if (id.HasValue && id > 0)
            {
                var data = db.Unvanlars.Where(p => p.UnvanID == id).FirstOrDefault();
                if (data != null) model = data;
            }

            return View(model);
        }
        [HttpPost]
        public ActionResult Kayit(Unvanlar kModel, string dlgid = "")
        {
            var MmMessage = new MmMessage();
            MmMessage.IsDialog = !dlgid.IsNullOrWhiteSpace();
            MmMessage.DialogID = dlgid;
            #region Kontrol

            //if (kModel.UnvanSiraNo <= 0)
            //{
            //    string msg = "Sıra Numarası 0 dan büyük olmalıdır.";
            //    MmMessage.Messages.Add(msg);
            //    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "UnvanSiraNo" });
            //}
            //else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "UnvanSiraNo" });
            if (kModel.UnvanAdi.IsNullOrWhiteSpace())
            {
                string msg = "Ünvan Adı Boş bırakılamaz.";
                MmMessage.Messages.Add(msg);
                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "UnvanAdi" });
            }
            else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "UnvanAdi" });
            #endregion

            if (MmMessage.Messages.Count == 0)
            {

                if (kModel.UnvanID <= 0)
                {
                    kModel.IsAktif = true;
                    kModel.IslemYapanID = UserIdentity.Current.Id;
                    kModel.IslemYapanIP = UserIdentity.Ip;
                    kModel.IslemTarihi = DateTime.Now;

                    db.Unvanlars.Add(kModel);
                }
                else
                {
                    var data = db.Unvanlars.Where(p => p.UnvanID == kModel.UnvanID).First();
                    //data.UnvanSiraNo = kModel.UnvanSiraNo;
                    data.UnvanAdi = kModel.UnvanAdi;
                    data.IsAktif = kModel.IsAktif;
                    data.IslemYapanID = UserIdentity.Current.Id;
                    data.IslemYapanIP = UserIdentity.Ip;
                    data.IslemTarihi = DateTime.Now;
                }
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            else
            {
                MessageBox.Show("Uyarı", MessageBox.MessageType.Warning, MmMessage.Messages.ToArray());
            }

            ViewBag.MmMessage = MmMessage;
            return View(kModel);
        }
        public ActionResult Sil(int id)
        {
            var kayit = db.Unvanlars.Where(p => p.UnvanID == id).FirstOrDefault();
            string message = "";
            bool success = true;
            if (kayit != null)
            {

                try
                {
                    message = "'" + kayit.UnvanAdi + "' İsimli Ünvan Silindi!";
                    db.Unvanlars.Remove(kayit);
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    success = false;
                    message = "'" + kayit.UnvanAdi + "' İsimli Ünvan Silinemedi! <br/> Bilgi:" + ex.ToExceptionMessage();
                    Management.SistemBilgisiKaydet(message, "Ünvanlar/Sil<br/><br/>" + ex.ToExceptionStackTrace(), BilgiTipi.OnemsizHata);
                }
            }
            else
            {
                success = false;
                message = "Silmek istediğiniz Ünvan sistemde bulunamadı!";
            }
            return Json(new { success = success, message = message }, "application/json", JsonRequestBehavior.AllowGet);
        }
        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }
    }
}
