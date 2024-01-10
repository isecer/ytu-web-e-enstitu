using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Business;
using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.Helpers;
using LisansUstuBasvuruSistemi.Utilities.MenuAndRoles;
using LisansUstuBasvuruSistemi.Utilities.SystemData;

namespace LisansUstuBasvuruSistemi.Controllers
{
    [System.Web.Mvc.OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    [Authorize(Roles = RoleNames.OgrenciBolumleri)]
    public class OgrenciBolumleriController : Controller
    {
        private readonly LisansustuBasvuruSistemiEntities _entities = new LisansustuBasvuruSistemiEntities();
        public ActionResult Index(string ekd)
        {
            return Index(new FmOgrenciBolumleri(), ekd);
        }
        [HttpPost]
        public ActionResult Index(FmOgrenciBolumleri model, string ekd)
        {
            model.EnstituKod = EnstituBus.GetSelectedEnstitu(ekd);
            var enstKods = UserIdentity.Current.EnstituKods ?? new List<string>();
            var q = from s in _entities.OgrenciBolumleris
                    join se in _entities.OgrenciBolumleris on new { s.OgrenciBolumID } equals new { se.OgrenciBolumID }
                    join e in _entities.Enstitulers on new { s.EnstituKod } equals new { e.EnstituKod }
                    where enstKods.Contains(s.EnstituKod)
                    select new FrOgrenciBolumleri
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
            q = !model.Sort.IsNullOrWhiteSpace() ? q.OrderBy(model.Sort) : q.OrderBy(o => o.EnstituAd).ThenBy(o => o.BolumAdi);
            model.data = q.Skip(model.StartRowIndex).Take(model.PageSize).ToArray();
            var indexModel = new MIndexBilgi
            {
                Toplam = model.RowCount,
                Aktif = q.Count(p => p.IsAktif),
                Pasif = q.Count(p => !p.IsAktif)
            };
            ViewBag.IndexModel = indexModel;
            ViewBag.EnstituKod = new SelectList(EnstituBus.GetCmbYetkiliEnstituler(true), "Value", "Caption", model.EnstituKod);
            ViewBag.IsAktif = new SelectList(ComboData.GetCmbAktifPasifData(true), "Value", "Caption", model.IsAktif);
            return View(model);
        }
        public ActionResult Kayit(int? id)
        {
            var mmMessage = new MmMessage();
            ViewBag.MmMessage = mmMessage;
            var model = new OgrenciBolumleri();

            var data = _entities.OgrenciBolumleris.FirstOrDefault(p => p.OgrenciBolumID == id);
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
            var mmMessage = new MmMessage();
            #region Kontrol


            if (kModel.EnstituKod.IsNullOrWhiteSpace())
            {
                mmMessage.Messages.Add("Enstitü seçiniz");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "EnstituKod" });
            }
            else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "EnstituKod" });
            if (kModel.BolumAdi.IsNullOrWhiteSpace())
            {
                mmMessage.Messages.Add("Bölüm Adı Giriniz.");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "BolumAdi" });
            }
            else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "BolumAdi" });


            #endregion
            if (mmMessage.Messages.Count == 0)
            {
                kModel.IslemYapanID = UserIdentity.Current.Id;
                kModel.IslemYapanIP = UserIdentity.Ip;
                kModel.IslemTarihi = DateTime.Now;

                var ogrenciBolumu = _entities.OgrenciBolumleris.FirstOrDefault(p => p.OgrenciBolumID == kModel.OgrenciBolumID);
                if (ogrenciBolumu == null)
                {
                    kModel.IsAktif = true;
                    _entities.OgrenciBolumleris.Add(kModel);
                }
                else
                {
                    ogrenciBolumu.EnstituKod = kModel.EnstituKod;
                    ogrenciBolumu.BolumAdi = kModel.BolumAdi;
                    ogrenciBolumu.IsAktif = kModel.IsAktif;
                    ogrenciBolumu.IslemYapanID = kModel.IslemYapanID;
                    ogrenciBolumu.IslemYapanIP = kModel.IslemYapanIP;
                    ogrenciBolumu.IslemTarihi = kModel.IslemTarihi;
                }
                _entities.SaveChanges();
                return RedirectToAction("Index");
            }
            else
            {
                MessageBox.Show("Uyarı", MessageBox.MessageType.Warning, mmMessage.Messages.ToArray());
            }

            ViewBag.MmMessage = mmMessage;
            ViewBag.EnstituKod = new SelectList(EnstituBus.GetCmbYetkiliEnstituler(true), "Value", "Caption", kModel.EnstituKod);
            ViewBag.IsAktif = new SelectList(ComboData.GetCmbAktifPasifData(true), "Value", "Caption", kModel.IsAktif);
            return View(kModel);
        }
        public ActionResult Sil(int? id)
        {
            var kayit = _entities.OgrenciBolumleris.FirstOrDefault(p => p.OgrenciBolumID == id);
            string message = "";
            bool success = true;
            if (kayit != null)
            {

                try
                {
                    message = "'" + kayit.BolumAdi + "' İsimli Öğrenci Bölüm Silindi!";
                    _entities.OgrenciBolumleris.Remove(kayit);
                    _entities.SaveChanges();
                }
                catch (Exception ex)
                {
                    success = false;
                    message = "'" + kayit.BolumAdi + "' İsimli Öğrenci Bölüm Silinemedi! <br/> Bilgi:" + ex.ToExceptionMessage();
                    SistemBilgilendirmeBus.SistemBilgisiKaydet(message,  ex.ToExceptionStackTrace(), BilgiTipiEnum.OnemsizHata);
                }
            }
            else
            {
                success = false;
                message = "Silmek istediğiniz Öğrenci Bölüm sistemde bulunamadı!";
            }
            return Json(new { success, message }, "application/json", JsonRequestBehavior.AllowGet);
        }

    }
}
