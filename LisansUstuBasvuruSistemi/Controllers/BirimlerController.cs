using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using LisansUstuBasvuruSistemi.Models; using LisansUstuBasvuruSistemi.Models.FilterModel;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Utilities.Enums;

namespace LisansUstuBasvuruSistemi.Controllers
{
    [System.Web.Mvc.OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    [Authorize(Roles = RoleNames.Birimler)]
    public class BirimlerController : Controller
    {
        private LisansustuBasvuruSistemiEntities db = new LisansustuBasvuruSistemiEntities();
        [Authorize(Roles = RoleNames.Unvanlar)]
        public ActionResult Index()
        {
            return Index(new fmBirimler { });
        }
        [HttpPost]
        public ActionResult Index(fmBirimler model)
        {

            var q = from s in db.Birimlers
                    select s;
             
            if (model.BirimKod.IsNullOrWhiteSpace() == false) q = q.Where(p => p.BirimKod == model.BirimKod);
            if (!model.BirimAdi.IsNullOrWhiteSpace()) q = q.Where(p => p.BirimAdi.Contains(model.BirimAdi));
            if (model.IsAktif.HasValue) q = q.Where(p => p.IsAktif == model.IsAktif.Value);
            model.RowCount = q.Count();
            if (!model.Sort.IsNullOrWhiteSpace()) q = q.OrderBy(model.Sort);
            else q = q.OrderBy(o => o.BirimAdi);
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
            var model = new Birimler();
            if (id.HasValue && id > 0)
            {
                var data = db.Birimlers.Where(p => p.BirimID == id).FirstOrDefault();
                if (data != null) model = data;
            }

            ViewBag.UstBirimID = new SelectList(Management.cmbBirimler(true), "Value", "Caption", model.UstBirimID);
            return View(model);
        }
        [HttpPost]
        public ActionResult Kayit(Birimler kModel, string dlgid = "")
        {
            var MmMessage = new MmMessage();
            MmMessage.IsDialog = !dlgid.IsNullOrWhiteSpace();
            MmMessage.DialogID = dlgid;
            #region Kontrol
             
            if (kModel.BirimAdi.IsNullOrWhiteSpace())
            {
                string msg = "Birim Adı Boş bırakılamaz.";
                MmMessage.Messages.Add(msg);
                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "BirimAdi" });
            }
            else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "BirimAdi" });
            #endregion

            if (MmMessage.Messages.Count == 0)
            {
                kModel.IslemTarihi = DateTime.Now;
                kModel.IslemYapanID = UserIdentity.Current.Id;
                kModel.IslemYapanIP = UserIdentity.Ip;
                if (kModel.BirimID <= 0)
                {
                    kModel.IsAktif = true;
                    db.Birimlers.Add(kModel);
                }
                else
                {
                    var data = db.Birimlers.Where(p => p.BirimID == kModel.BirimID).First();
                    data.BirimKod = kModel.BirimKod;
                    data.BirimAdi = kModel.BirimAdi;
                    data.UstBirimID = kModel.UstBirimID;
                    data.IsAktif = kModel.IsAktif;
                    data.IslemTarihi = kModel.IslemTarihi;
                    data.IslemYapanID = kModel.IslemYapanID;
                    data.IslemYapanIP = kModel.IslemYapanIP;
                }
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            else
            {
                MessageBox.Show("Uyarı", MessageBox.MessageType.Warning, MmMessage.Messages.ToArray());
            }
            ViewBag.UstBirimID = new SelectList(Management.cmbBirimler(true), "Value", "Caption", kModel.UstBirimID);
            ViewBag.MmMessage = MmMessage;
            return View(kModel);
        }
        public ActionResult Sil(int id)
        {
            var kayit = db.Birimlers.Where(p => p.BirimID == id).FirstOrDefault();
            string message = "";
            bool success = true;
            if (kayit != null)
            {

                try
                {
                    message = "'" + kayit.BirimAdi + "' İsimli Birim Silindi!";
                    db.Birimlers.Remove(kayit);
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    success = false;
                    message = "'" + kayit.BirimAdi + "' İsimli Birim Silinemedi! <br/> Bilgi:" + ex.ToExceptionMessage(); 
                    Management.SistemBilgisiKaydet(message, "Birimler/Sil<br/><br/>" + ex.ToExceptionStackTrace(), BilgiTipi.OnemsizHata);
                }
            }
            else
            {
                success = false;
                message = "Silmek istediğiniz Birim sistemde bulunamadı!";
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
