using BiskaUtil;
using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.MenuAndRoles;
using System;
using System.Linq;
using System.Web.Mvc;
using LisansUstuBasvuruSistemi.Utilities.Extensions;

namespace LisansUstuBasvuruSistemi.Controllers
{
    [System.Web.Mvc.OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    [Authorize(Roles = RoleNames.Sehirler)]
    public class SehirlerController : Controller
    {
        private LisansustuBasvuruSistemiEntities db = new LisansustuBasvuruSistemiEntities();
        public ActionResult Index()
        {
            return Index(new fmSehirler { });
        }
        [HttpPost]
        public ActionResult Index(fmSehirler model)
        {

            var q = from s in db.Sehirlers
                    select s;

            if (model.SehirKod.HasValue) q = q.Where(p => p.SehirKod == model.SehirKod);
            if (!model.Ad.IsNullOrWhiteSpace()) q = q.Where(p => p.Ad.Contains(model.Ad));
            if (model.IsAktif.HasValue) q = q.Where(p => p.IsAktif == model.IsAktif);

            model.RowCount = q.Count();
            if (!model.Sort.IsNullOrWhiteSpace()) q = q.OrderBy(model.Sort);
            else q = q.OrderBy(o => o.Ad);
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
            var model = new Sehirler();
            if (id.HasValue && id > 0)
            {
                var data = db.Sehirlers.Where(p => p.SehirKod == id).FirstOrDefault();
                if (data != null) model = data;
            }
            ViewBag.OldID = model.SehirKod;
            return View(model);
        }
        [HttpPost]
        public ActionResult Kayit(Sehirler kModel, int OldID, string dlgid = "")
        {
            var MmMessage = new MmMessage();
            MmMessage.IsDialog = !dlgid.IsNullOrWhiteSpace();
            MmMessage.DialogID = dlgid;
            #region Kontrol
            if (OldID <= 0)
            {
                if (kModel.SehirKod <= 0)
                {
                    string msg = "Şehir kodu giriniz.";
                    MmMessage.Messages.Add(msg);
                    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "SehirKod" });
                }
                else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "SehirKod" });
            }
            if (kModel.Ad.IsNullOrWhiteSpace())
            {
                string msg = "Şehir adı Boş bırakılamaz.";
                MmMessage.Messages.Add(msg);
                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "Ad" });
            }
            else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "Ad" });
            #endregion
            if (MmMessage.Messages.Count == 0)
            {
                kModel.IslemTarihi = DateTime.Now;
                kModel.IslemYapanID = UserIdentity.Current.Id;
                kModel.IslemYapanIP = UserIdentity.Ip;
                if (OldID <= 0)
                {
                    kModel.IsAktif = true;
                    db.Sehirlers.Add(kModel);
                }
                else
                {
                    var data = db.Sehirlers.Where(p => p.SehirKod == OldID).First();
                    data.Ad = kModel.Ad;
                    data.IsAktif = kModel.IsAktif;
                    data.IslemYapanIP = kModel.IslemYapanIP;
                    data.IslemYapanID = kModel.IslemYapanID;
                    data.IslemTarihi = kModel.IslemTarihi;
                }
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            else
            {
                MessageBox.Show("Uyarı", MessageBox.MessageType.Warning, MmMessage.Messages.ToArray());
            }

            ViewBag.MmMessage = MmMessage;
            ViewBag.OldID = OldID;
            return View(kModel);
        }
        public ActionResult Sil(int id)
        {
            var kayit = db.Sehirlers.Where(p => p.SehirKod == id).FirstOrDefault();
            string message = "";
            bool success = true;
            if (kayit != null)
            {

                try
                {
                    message = "'" + kayit.Ad + "' İsimli Şehir Silindi!";
                    db.Sehirlers.Remove(kayit);
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    success = false;
                    message = "'" + kayit.Ad + "' İsimli Şehir Silinemedi! <br/> Bilgi:" + ex.ToExceptionMessage();
                    Management.SistemBilgisiKaydet(message, "Sehirler/Sil<br/><br/>" + ex.ToExceptionStackTrace(), LogType.OnemsizHata);
                }
            }
            else
            {
                success = false;
                message = "Silmek istediğiniz Şehir sistemde bulunamadı!";
            }
            return Json(new { success = success, message = message }, "application/json", JsonRequestBehavior.AllowGet);
        }

    }
}
