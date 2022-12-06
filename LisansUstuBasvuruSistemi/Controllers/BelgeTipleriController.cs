using LisansUstuBasvuruSistemi.Models; using LisansUstuBasvuruSistemi.Models.FilterModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using BiskaUtil;

namespace LisansUstuBasvuruSistemi.Controllers
{
    [System.Web.Mvc.OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    [Authorize(Roles = RoleNames.BelgeTipleri)]
    public class BelgeTipleriController : Controller
    {
        private LisansustuBasvuruSistemiEntities db = new LisansustuBasvuruSistemiEntities();
        public ActionResult Index()
        {
            return Index(new fmBelgeTipleri { });
        }
        [HttpPost]
        public ActionResult Index(fmBelgeTipleri model)
        {

            var q = from s in db.BelgeTipleris
                    select new frBelgeTipleri
                    {
                        BelgeTipID = s.BelgeTipID,
                        BelgeTipAdi = s.BelgeTipAdi,
                        IsAktif = s.IsAktif,
                        IslemTarihi = s.IslemTarihi,
                        IslemYapanID = s.IslemYapanID,
                        IslemYapan = s.Kullanicilar.Ad + " " + s.Kullanicilar.Soyad,
                        IslemYapanIP = s.IslemYapanIP

                    };

            if (model.IsAktif.HasValue) q = q.Where(p => p.IsAktif == model.IsAktif);
            if (!model.BelgeTipAdi.IsNullOrWhiteSpace()) q = q.Where(p => p.BelgeTipAdi.Contains(model.BelgeTipAdi));
            model.RowCount = q.Count();
            if (!model.Sort.IsNullOrWhiteSpace()) q = q.OrderBy(model.Sort);
            else q = q.OrderBy(o => o.BelgeTipAdi);
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
        public ActionResult Kayit(int? id )
        {
            var MmMessage = new MmMessage(); 
            ViewBag.MmMessage = MmMessage;
            var model = new BelgeTipleri();
            if (id.HasValue)
            {
                var data = db.BelgeTipleris.Where(p => p.BelgeTipID == id).FirstOrDefault();
                if (data != null)
                {
                    model = data; 
                }
            } 
            return View(model);
        }
        [HttpPost]
        [Authorize(Roles = RoleNames.BelgeTipleriKayıt)]
        public ActionResult Kayit(BelgeTipleri kModel)
        {
            var MmMessage = new MmMessage(); 
            #region Kontrol
            if (kModel.BelgeTipAdi.IsNullOrWhiteSpace())
            {
                MmMessage.Messages.Add("Belge Tip Adını Giriniz.");
                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Error, PropertyName = "BelgeTipAdi" });
            }
            #endregion
            if (MmMessage.Messages.Count == 0)
            {
                if (kModel.BelgeTipID <= 0)
                {
                   kModel.IsAktif = true;
                    kModel.IslemTarihi = DateTime.Now;
                    kModel.IslemYapanID = UserIdentity.Current.Id;
                    kModel.IslemYapanIP = UserIdentity.Ip;
                   db.BelgeTipleris.Add(kModel);
                }
                else
                {
                    var data = db.BelgeTipleris.Where(p => p.BelgeTipID == kModel.BelgeTipID).First();
                    data.BelgeTipAdi = kModel.BelgeTipAdi;
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
        [Authorize(Roles = RoleNames.BelgeTipleriSil)]
        public ActionResult Sil(int? id)
        {
            var data = db.BelgeTipleris.Where(p => p.BelgeTipID == id).FirstOrDefault(); 
            string message = "";
            bool success = true;
            if (data != null)
            {

                try
                {
                    message = "'" + data.BelgeTipAdi + "' İsimli belge tipi Silindi!";
                    db.BelgeTipleris.Remove(data);
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    success = false;
                    message = "'" + data.BelgeTipAdi + "' İsimli belge tipi Silinemedi! <br/> Bilgi:" + ex.ToExceptionMessage();
                    Management.SistemBilgisiKaydet(message, "BelgeTipleri/Sil<br/><br/>" + ex.ToExceptionStackTrace(), BilgiTipi.OnemsizHata);
                }
            }
            else
            {
                success = false;
                message = "Silmek istediğiniz Belge tipi sistemde bulunamadı!";
            }
            return Json(new { success = success, message = message }, "application/json", JsonRequestBehavior.AllowGet);
        }
    }
}