using BiskaUtil;
using LisansUstuBasvuruSistemi.Business;
using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.MenuAndRoles;
using LisansUstuBasvuruSistemi.Utilities.SystemData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace LisansUstuBasvuruSistemi.Controllers
{
    [System.Web.Mvc.OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    [Authorize(Roles = RoleNames.OgrenimTipleri)]
    public class OgrenimTipleriController : Controller
    {
        private readonly LisansustuBasvuruSistemiEntities _entities = new LisansustuBasvuruSistemiEntities();
        public ActionResult Index()
        {
            return Index(new FmOgrenimTipleri { });
        }
        [HttpPost]
        public ActionResult Index(FmOgrenimTipleri model)
        {
            var enstKods = UserIdentity.Current.EnstituKods ?? new List<string>();
            var q = from s in _entities.OgrenimTipleris
                    join ea in _entities.Enstitulers on new { s.EnstituKod } equals new { ea.EnstituKod }
                    where enstKods.Contains(s.EnstituKod)
                    select new FrOgrenimTipleri
                    {
                        OgrenimTipID = s.OgrenimTipID,
                        EnstituKod = s.EnstituKod,
                        EnstituAd = ea.EnstituAd,
                        OgrenimTipKod = s.OgrenimTipKod,
                        IsAktif = s.IsAktif,
                        IslemTarihi = s.IslemTarihi,
                        IslemYapanID = s.IslemYapanID,
                        IslemYapanIP = s.IslemYapanIP,
                        IslemYapan = s.Kullanicilar.Ad + " " + s.Kullanicilar.Soyad,
                        OgrenimTipAdi = s.OgrenimTipAdi,

                        IsMezuniyetBasvurusuYapabilir = s.IsMezuniyetBasvurusuYapabilir,
                        MBasvuruSonDonemKaydiKontrolEdilecekDersKodlari = s.MBasvuruSonDonemKaydiKontrolEdilecekDersKodlari,
                        MBasvuruToplamKrediKriteri = s.MBasvuruToplamKrediKriteri,
                        MBasvuruAGNOKriteri = s.MBasvuruAGNOKriteri,
                        MBasvuruAKTSKriteri = s.MBasvuruAKTSKriteri,
                        MBSinavUzatmaSuresiGun = s.MBSinavUzatmaSuresiGun,
                        MBTezTeslimSuresiGun = s.MBTezTeslimSuresiGun,
                        MBSRTalebiKacGunSonraAlabilir = s.MBSRTalebiKacGunSonraAlabilir
                    };
            if (model.IsAktif.HasValue) q = q.Where(p => p.IsAktif == model.IsAktif);
            if (model.EnstituKod.IsNullOrWhiteSpace() == false) q = q.Where(p => p.EnstituKod == model.EnstituKod);
            if (model.OgrenimTipKod.HasValue) q = q.Where(p => p.OgrenimTipKod == model.OgrenimTipKod);
            if (!model.OgrenimTipAd.IsNullOrWhiteSpace()) q = q.Where(p => p.OgrenimTipAdi.Contains(model.OgrenimTipAd));
            model.RowCount = q.Count();
            q = !model.Sort.IsNullOrWhiteSpace() ? q.OrderBy(model.Sort) : q.OrderBy(o => o.OgrenimTipAdi);
            model.data = q.Skip(model.StartRowIndex).Take(model.PageSize).ToArray(); ;
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
        public ActionResult Kayit(int? id, string dlgid)
        {
            var mmMessage = new MmMessage
            {
                IsDialog = !dlgid.IsNullOrWhiteSpace(),
                DialogID = dlgid
            };
            ViewBag.MmMessage = mmMessage;
            var model = new OgrenimTipleri();
            if (id.HasValue)
            {
                model = _entities.OgrenimTipleris.First(p => p.OgrenimTipID == id);
                if (!UserIdentity.Current.EnstituKods.Contains(model.EnstituKod)) model = new OgrenimTipleri();


            }

            ViewBag.EnstituKod = new SelectList(EnstituBus.GetCmbYetkiliEnstituler(true), "Value", "Caption", model.EnstituKod);
            return View(model);
        }
        [HttpPost]
        public ActionResult Kayit(OgrenimTipleri kModel, List<int> secilenId)
        {
            var mmMessage = new MmMessage();
            #region Kontrol


            if (secilenId == null) secilenId = new List<int>();
            if (kModel.EnstituKod.IsNullOrWhiteSpace())
            {
                mmMessage.Messages.Add("Enstitü seçiniz");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "EnstituKod" });
            }
            else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "EnstituKod" });

            if (kModel.OgrenimTipKod <= 0)
            {
                mmMessage.Messages.Add("Kayıt işlemini yapabilmeni için Öğrenim Tipi Kod kısmını doldurmanız gerekmektedir!");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "OgrenimTipKod" });
            }
            else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "OgrenimTipKod" });

            if (kModel.OgrenimTipAdi.IsNullOrWhiteSpace())
            {
                mmMessage.Messages.Add("Öğrenim Tip Adı Giriniz.");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "OgrenimTipAdi" });
            }
            else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "OgrenimTipAdi" });

            if (kModel.IsMezuniyetBasvurusuYapabilir)
            {
                if (kModel.MBasvuruSonDonemKaydiKontrolEdilecekDersKodlari.IsNullOrWhiteSpace())
                {
                    mmMessage.Messages.Add("Son döneminde kayıt yaptırması gereken ders kodlarını giriniz!");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "MBasvuruSonDonemKaydiKontrolEdilecekDersKodlari" });
                }
                else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "MBasvuruSonDonemKaydiKontrolEdilecekDersKodlari" });
                if (kModel.MBasvuruToplamKrediKriteri.HasValue == false)
                {
                    mmMessage.Messages.Add("Toplam Kredi kriteri bilgisini giriniz!");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "MBasvuruToplamKrediKriteri" });
                }
                else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "MBasvuruToplamKrediKriteri" });
                if (kModel.MBasvuruAGNOKriteri.HasValue == false || !(kModel.MBasvuruAGNOKriteri > 0 && kModel.MBasvuruAGNOKriteri <= 4))
                {
                    mmMessage.Messages.Add("Genel AGNO kriteri 1 ile 4 arasında bir değer olmalıdır!");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "MBasvuruAGNOKriteri" });
                }
                else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "MBasvuruAGNOKriteri" });
                if (kModel.MBasvuruAKTSKriteri.HasValue == false)
                {
                    mmMessage.Messages.Add("Toplam AKTS kriteri bilgisini giriniz!");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "MBasvuruAKTSKriteri" });
                }
                else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "MBasvuruAKTSKriteri" });

                if (kModel.MBSinavUzatmaSuresiGun.HasValue == false)
                {
                    mmMessage.Messages.Add("Sınav Uzatma Süresi Gün bilgisi boş bırakılamaz.");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "MBSinavUzatmaSuresiGun" });
                }
                else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "MBSinavUzatmaSuresiGun" });
                if (kModel.MBTezTeslimSuresiGun.HasValue == false)
                {
                    mmMessage.Messages.Add("Tez Teslim Süresi Gün bilgisi boş bırakılamaz.");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "MBTezTeslimSuresiGun" });
                }
                else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "MBTezTeslimSuresiGun" });
                if (kModel.MBSRTalebiKacGunSonraAlabilir.HasValue == false)
                {
                    mmMessage.Messages.Add("SR Talebi Kaç Gün Sonra Alabilir bilgisi boş bırakılamaz.");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "MBSRTalebiKacGunSonraAlabilir" });
                }
                else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "MBSRTalebiKacGunSonraAlabilir" });

            }

            if (mmMessage.Messages.Count == 0)
            {

                var cnt = _entities.OgrenimTipleris.Any(p => p.OgrenimTipID != kModel.OgrenimTipID && p.EnstituKod == kModel.EnstituKod && p.OgrenimTipKod == kModel.OgrenimTipKod);
                if (cnt)
                {
                    mmMessage.Messages.Add("Tanımlamak istediğiniz Öğrenim Tipi Kodu seçilen Enstitü için daha önceden sisteme tanımlanmıştır, tekrar tanımlanamaz!");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "OgrenimTipKod" });
                }
            }
            #endregion
            if (mmMessage.Messages.Count == 0)
            {
                if (kModel.IsMezuniyetBasvurusuYapabilir == false)
                {
                    kModel.MBasvuruSonDonemKaydiKontrolEdilecekDersKodlari = "";
                    kModel.MBasvuruToplamKrediKriteri = null;
                    kModel.MBasvuruAGNOKriteri = null;
                    kModel.MBasvuruAKTSKriteri = null;
                    kModel.MBTezTeslimSuresiGun = null;
                    kModel.MBSinavUzatmaSuresiGun = null;
                    kModel.MBSRTalebiKacGunSonraAlabilir = null;
                }

                if (kModel.OgrenimTipID <= 0)
                {
                    kModel.IsAktif = true;
                    var ogrnt = _entities.OgrenimTipleris.Add(new OgrenimTipleri
                    {
                        EnstituKod = kModel.EnstituKod,
                        OgrenimTipKod = kModel.OgrenimTipKod,
                        OgrenimTipAdi = kModel.OgrenimTipAdi,

                        IsMezuniyetBasvurusuYapabilir = kModel.IsMezuniyetBasvurusuYapabilir,
                        MBasvuruSonDonemKaydiKontrolEdilecekDersKodlari = kModel.MBasvuruSonDonemKaydiKontrolEdilecekDersKodlari,
                        MBasvuruToplamKrediKriteri = kModel.MBasvuruToplamKrediKriteri,
                        MBasvuruAGNOKriteri = kModel.MBasvuruAGNOKriteri,
                        MBasvuruAKTSKriteri = kModel.MBasvuruAKTSKriteri,
                        MBSinavUzatmaSuresiGun = kModel.MBSinavUzatmaSuresiGun,
                        MBTezTeslimSuresiGun = kModel.MBTezTeslimSuresiGun,

                        MBSRTalebiKacGunSonraAlabilir = kModel.MBSRTalebiKacGunSonraAlabilir,
                        IsAktif = kModel.IsAktif,
                        IslemYapanID = UserIdentity.Current.Id,
                        IslemYapanIP = UserIdentity.Ip,
                        IslemTarihi = DateTime.Now
                    });
                    _entities.SaveChanges();
                }
                else
                {
                    var kayit = _entities.OgrenimTipleris.First(p => p.OgrenimTipID == kModel.OgrenimTipID);

                    kayit.EnstituKod = kModel.EnstituKod;
                    kayit.OgrenimTipKod = kModel.OgrenimTipKod;
                    kayit.OgrenimTipAdi = kModel.OgrenimTipAdi;
                    kayit.IsMezuniyetBasvurusuYapabilir = kModel.IsMezuniyetBasvurusuYapabilir;
                    kayit.MBasvuruSonDonemKaydiKontrolEdilecekDersKodlari = kModel.MBasvuruSonDonemKaydiKontrolEdilecekDersKodlari;
                    kayit.MBasvuruToplamKrediKriteri = kModel.MBasvuruToplamKrediKriteri;
                    kayit.MBasvuruAGNOKriteri = kModel.MBasvuruAGNOKriteri;
                    kayit.MBasvuruAKTSKriteri = kModel.MBasvuruAKTSKriteri;
                    kayit.MBSinavUzatmaSuresiGun = kModel.MBSinavUzatmaSuresiGun;
                    kayit.MBTezTeslimSuresiGun = kModel.MBTezTeslimSuresiGun;
                    kayit.MBSRTalebiKacGunSonraAlabilir = kModel.MBSRTalebiKacGunSonraAlabilir;
                    kayit.IsAktif = kModel.IsAktif;
                    kayit.IslemYapanID = UserIdentity.Current.Id;
                    kayit.IslemYapanIP = UserIdentity.Ip;
                    kayit.IslemTarihi = DateTime.Now;
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
            return View(kModel);
        }
        public ActionResult Sil(int? id)
        {
            var kayit = _entities.OgrenimTipleris.FirstOrDefault(p => p.OgrenimTipID == id);
            var ogrenimTipleri = _entities.OgrenimTipleris.FirstOrDefault(p => p.OgrenimTipID == kayit.OgrenimTipID);
            string message = "";
            bool success = true;
            if (kayit != null)
            {

                try
                {
                    message = "'" + ogrenimTipleri.OgrenimTipAdi + "' İsimli Öğrenim tipi Silindi!";
                    _entities.OgrenimTipleris.Remove(kayit);
                    _entities.SaveChanges();
                }
                catch (Exception ex)
                {
                    success = false;
                    message = "'" + ogrenimTipleri.OgrenimTipAdi + "' İsimli Öğrenim tipi Silinemedi! <br/> Bilgi:" + ex.ToExceptionMessage();
                    SistemBilgilendirmeBus.SistemBilgisiKaydet(message, "OgrenimTipleri/Sil<br/><br/>" + ex.ToExceptionStackTrace(), LogType.OnemsizHata);
                }
            }
            else
            {
                success = false;
                message = "Silmek istediğiniz Öğrenim tipi sistemde bulunamadı!";
            }
            return Json(new { success = success, message = message }, "application/json", JsonRequestBehavior.AllowGet);
        }
    }
}
