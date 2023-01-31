using BiskaUtil;
using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.MenuAndRoles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using LisansUstuBasvuruSistemi.Business;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.SystemData;

namespace LisansUstuBasvuruSistemi.Controllers
{
    [System.Web.Mvc.OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    [Authorize(Roles = RoleNames.OgrenciBolumleri)]
    public class OgrenciBolumleriController : Controller
    {
        private LisansustuBasvuruSistemiEntities db = new LisansustuBasvuruSistemiEntities();
        public ActionResult Index()
        {
            return Index(new fmOgrenciBolumleri { });
        }
        [HttpPost]
        public ActionResult Index(fmOgrenciBolumleri model)
        {
            var EnstKods = UserIdentity.Current.EnstituKods ?? new List<string>();
            var q = from s in db.OgrenciBolumleris
                    join se in db.OgrenciBolumleris on new { s.OgrenciBolumID } equals new { se.OgrenciBolumID }
                    join e in db.Enstitulers on new { s.EnstituKod } equals new { e.EnstituKod }
                    where EnstKods.Contains(s.EnstituKod)
                    select new frOgrenciBolumleri
                    {

                        OgrenciBolumID = s.OgrenciBolumID,
                        EnstituKod = s.EnstituKod,
                        EnstituAd = e.EnstituAd,
                        BolumAdi = se.BolumAdi,
                        IsAktif = s.IsAktif,
                        IslemTarihi = s.IslemTarihi,
                        IslemYapanID = s.IslemYapanID,
                        IslemYapanIP = s.IslemYapanIP,
                        IslemYapan = s.Kullanicilar.Ad + " " + s.Kullanicilar.Soyad,

                    };

            if (!model.EnstituKod.IsNullOrWhiteSpace()) q = q.Where(p => p.EnstituKod == model.EnstituKod);
            if (model.OgrenciBolumID.HasValue) q = q.Where(p => p.OgrenciBolumID == model.OgrenciBolumID.Value);
            if (model.IsAktif.HasValue) q = q.Where(p => p.IsAktif == model.IsAktif.Value);
            if (!model.BolumAdi.IsNullOrWhiteSpace()) q = q.Where(p => p.BolumAdi.Contains(model.BolumAdi));
            model.RowCount = q.Count();
            if (!model.Sort.IsNullOrWhiteSpace()) q = q.OrderBy(model.Sort);
            else q = q.OrderBy(o => o.EnstituAd).ThenBy(o => o.BolumAdi);
            var PS = Management.setStartRowInx(model.StartRowIndex, model.PageIndex, model.PageCount, model.RowCount, model.PageSize);
            model.PageIndex = PS.PageIndex;
            model.data = q.Skip(PS.StartRowIndex).Take(model.PageSize).ToArray();
            var IndexModel = new MIndexBilgi();
            IndexModel.Toplam = model.RowCount;
            IndexModel.Aktif = q.Where(p => p.IsAktif).Count();
            IndexModel.Pasif = q.Where(p => !p.IsAktif).Count();
            ViewBag.IndexModel = IndexModel;
            ViewBag.EnstituKod = new SelectList(EnstituBus.GetCmbYetkiliEnstituler(true), "Value", "Caption", model.EnstituKod);
            ViewBag.IsAktif = new SelectList(ComboData.GetCmbAktifPasifData(true), "Value", "Caption", model.IsAktif);
            return View(model);
        }
        public ActionResult Kayit(int? id)
        {
            var MmMessage = new MmMessage();
            ViewBag.MmMessage = MmMessage;
            var model = new OgrenciBolumleri();

            var data = db.OgrenciBolumleris.Where(p => p.OgrenciBolumID == id).FirstOrDefault();
            if (data != null)
            {
                model = data;
            }

            ViewBag.EnstituKod = new SelectList(EnstituBus.GetCmbYetkiliEnstituler(true), "Value", "Caption", model.EnstituKod);
            ViewBag.IsAktif = new SelectList(ComboData.GetCmbAktifPasifData(true), "Value", "Caption", model.IsAktif);

            return View(model);
        }
        [HttpPost]
        public ActionResult Kayit(OgrenciBolumleri kModel)
        {
            var MmMessage = new MmMessage();
            #region Kontrol


            if (kModel.EnstituKod.IsNullOrWhiteSpace())
            {
                MmMessage.Messages.Add("Enstitü seçiniz");
                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "EnstituKod" });
            }
            else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "EnstituKod" });
            if (kModel.BolumAdi.IsNullOrWhiteSpace())
            {
                MmMessage.Messages.Add("Bölüm Adı Giriniz.");
                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "BolumAdi" });
            }
            else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "BolumAdi" });


            #endregion
            if (MmMessage.Messages.Count == 0)
            {
                kModel.IslemYapanID = UserIdentity.Current.Id;
                kModel.IslemYapanIP = UserIdentity.Ip;
                kModel.IslemTarihi = DateTime.Now;

                var OgrenciBolumu = db.OgrenciBolumleris.Where(p => p.OgrenciBolumID == kModel.OgrenciBolumID).FirstOrDefault();
                if (OgrenciBolumu == null)
                {
                    kModel.IsAktif = true;
                    db.OgrenciBolumleris.Add(kModel);
                }
                else
                {
                    OgrenciBolumu.EnstituKod = kModel.EnstituKod;
                    OgrenciBolumu.BolumAdi = kModel.BolumAdi;
                    OgrenciBolumu.IsAktif = kModel.IsAktif;
                    OgrenciBolumu.IslemYapanID = kModel.IslemYapanID;
                    OgrenciBolumu.IslemYapanIP = kModel.IslemYapanIP;
                    OgrenciBolumu.IslemTarihi = kModel.IslemTarihi;
                }
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            else
            {
                MessageBox.Show("Uyarı", MessageBox.MessageType.Warning, MmMessage.Messages.ToArray());
            }

            ViewBag.MmMessage = MmMessage;
            ViewBag.EnstituKod = new SelectList(EnstituBus.GetCmbYetkiliEnstituler(true), "Value", "Caption", kModel.EnstituKod);
            ViewBag.IsAktif = new SelectList(ComboData.GetCmbAktifPasifData(true), "Value", "Caption", kModel.IsAktif);
            return View(kModel);
        }
        public ActionResult Sil(int? id)
        {
            var kayit = db.OgrenciBolumleris.Where(p => p.OgrenciBolumID == id).FirstOrDefault(); 
            string message = "";
            bool success = true;
            if (kayit != null)
            {

                try
                {
                    message = "'" + kayit.BolumAdi + "' İsimli Öğrenci Bölüm Silindi!";
                    db.OgrenciBolumleris.Remove(kayit);
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    success = false;
                    message = "'" + kayit.BolumAdi + "' İsimli Öğrenci Bölüm Silinemedi! <br/> Bilgi:" + ex.ToExceptionMessage();
                    Management.SistemBilgisiKaydet(message, "Bolumler/Sil<br/><br/>" + ex.ToExceptionStackTrace(), LogType.OnemsizHata);
                }
            }
            else
            {
                success = false;
                message = "Silmek istediğiniz Öğrenci Bölüm sistemde bulunamadı!";
            }
            return Json(new { success = success, message = message }, "application/json", JsonRequestBehavior.AllowGet);
        }

    }
}
