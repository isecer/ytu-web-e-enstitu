using LisansUstuBasvuruSistemi.Models; using LisansUstuBasvuruSistemi.Models.FilterModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Utilities.Enums;

namespace LisansUstuBasvuruSistemi.Controllers
{
    [System.Web.Mvc.OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    [Authorize(Roles = RoleNames.Uyruklar)]
    public class UyruklarController : Controller
    {
        private LisansustuBasvuruSistemiEntities db = new LisansustuBasvuruSistemiEntities();
        public ActionResult Index()
        {
            return Index(new fmUyruklar { });
        }
        [HttpPost]
        public ActionResult Index(fmUyruklar model)
        {

            var q = from s in db.Uyruklars
                    select s;

            if (model.UyrukKod.HasValue) q = q.Where(p => p.UyrukKod == model.UyrukKod);
            if (!model.KisaAd.IsNullOrWhiteSpace()) q = q.Where(p => p.KisaAd.Contains(model.KisaAd));
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
            var model = new Uyruklar();
            if (id.HasValue && id > 0)
            {
                var data = db.Uyruklars.Where(p => p.UyrukKod == id).FirstOrDefault();
                if (data != null) model = data;
            }
            ViewBag.OldID = model.UyrukKod;
            return View(model);
        }
        [HttpPost]
        public ActionResult Kayit(Uyruklar kModel, int OldID, string dlgid = "")
        {
            var MmMessage = new MmMessage();
            MmMessage.IsDialog = !dlgid.IsNullOrWhiteSpace();
            MmMessage.DialogID = dlgid;
            #region Kontrol
            if (OldID <= 0)
            {
                if (kModel.UyrukKod <= 0)
                {
                    string msg = "Uyruk kodu giriniz.";
                    MmMessage.Messages.Add(msg);
                    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "UyrukKod" });
                }
                else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "UyrukKod" });
            }
            if (kModel.KisaAd.IsNullOrWhiteSpace())
            {
                string msg = "Uyruk kısa adı Boş bırakılamaz.";
                MmMessage.Messages.Add(msg);
                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "KisaAd" });
            }
            else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "KisaAd" });
            if (kModel.Ad.IsNullOrWhiteSpace())
            {
                string msg = "Uyruk adı Boş bırakılamaz.";
                MmMessage.Messages.Add(msg);
                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "Ad" });
            }
            else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "Ad" });
            #endregion
            if (MmMessage.Messages.Count == 0)
            {
                if (OldID <= 0)
                {
                    kModel.IsAktif = true;
                    db.Uyruklars.Add(kModel);
                }
                else
                {
                    var data = db.Uyruklars.Where(p => p.UyrukKod == OldID).First();
                    data.Ad = kModel.Ad;
                    data.KisaAd = kModel.KisaAd;
                    data.IsAktif = kModel.IsAktif;
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
            var kayit = db.Uyruklars.Where(p => p.UyrukKod == id).FirstOrDefault();
            string message = "";
            bool success = true;
            if (kayit != null)
            {

                try
                {
                    message = "'" + kayit.Ad + "' İsimli Uyruk Silindi!";
                    db.Uyruklars.Remove(kayit);
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    success = false;
                    message = "'" + kayit.Ad + "' İsimli Uyruk Silinemedi! <br/> Bilgi:" + ex.ToExceptionMessage();
                    Management.SistemBilgisiKaydet(message, "Uyruklar/Sil<br/><br/>" + ex.ToExceptionStackTrace(), BilgiTipi.OnemsizHata);
                }
            }
            else
            {
                success = false;
                message = "Silmek istediğiniz Uyruk sistemde bulunamadı!";
            }
            return Json(new { success = success, message = message }, "application/json", JsonRequestBehavior.AllowGet);
        }

    }
}
