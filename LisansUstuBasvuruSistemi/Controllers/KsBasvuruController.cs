using System;
using System.Linq;
using System.Web.Mvc;
using LisansUstuBasvuruSistemi.Business;
using Entities.Entities;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.Helpers;
using LisansUstuBasvuruSistemi.Utilities.MenuAndRoles;
using LisansUstuBasvuruSistemi.Utilities.SystemSetting;
using LisansUstuBasvuruSistemi.Utilities.Logs;

namespace LisansUstuBasvuruSistemi.Controllers
{
    [Authorize]
    [OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    public class KsBasvuruController : Controller
    {
        // GET: KayitSilmeBasvuru
        private readonly LubsDbEntities _entities = new LubsDbEntities();
        [AllowAnonymous]
        public ActionResult Index(string ekd, Guid? showBasvuruUniqueId = null)
        {

            return Index(new FmKayitSilmeBasvuruDto() { PageSize = 50, ShowBasvuruUniqueId = showBasvuruUniqueId }, ekd);
        }
        [AllowAnonymous]
        [HttpPost]
        public ActionResult Index(FmKayitSilmeBasvuruDto model, string ekd)
        {
            var enstituKod = EnstituBus.GetSelectedEnstitu(ekd);
            #region bilgiModel
            var bbModel = new IndexPageInfoDto
            {
                SistemBasvuruyaAcik = KayitSilmeAyar.KayitSilmeBasvuruAlimiAcik.GetAyar(enstituKod).ToBoolean(false)
            };
            var kullanici = _entities.Kullanicilars.First(p => p.KullaniciID == UserIdentity.Current.Id);
            bbModel.Kullanici = kullanici;
            if (kullanici.YtuOgrencisi)
            {
                var otb = _entities.OgrenimTipleris.First(p => p.EnstituKod == enstituKod && p.OgrenimTipKod == kullanici.OgrenimTipKod);

                bbModel.OgrenimDurumAdi = kullanici.OgrenimDurumlari.OgrenimDurumAdi;
                bbModel.OgrenimTipAdi = otb.OgrenimTipAdi;
                bbModel.AnabilimdaliAdi = kullanici.Programlar.AnabilimDallari.AnabilimDaliAdi;
                bbModel.ProgramAdi = kullanici.Programlar.ProgramAdi;
                bbModel.OgrenciNo = kullanici.OgrenciNo;
                bbModel.KullaniciTipYetki = kullanici.OgrenimDurumID == OgrenimDurumEnum.HalenOğrenci;
                bbModel.EnstituYetki = kullanici.Programlar.AnabilimDallari.EnstituKod == enstituKod;

                var kullKayitB = KullanicilarBus.OgrenciBilgisiGuncelleObs(kullanici.KullaniciID);
                if (kullanici.KayitTarihi != kullKayitB.KayitTarihi || kullanici.KayitYilBaslangic != kullKayitB.BaslangicYil || kullanici.KayitDonemID != kullKayitB.DonemID)
                {
                    kullanici.KayitYilBaslangic = kullKayitB.BaslangicYil;
                    kullanici.KayitDonemID = kullKayitB.DonemID;
                    kullanici.KayitTarihi = kullKayitB.KayitTarihi;
                    _entities.SaveChanges();
                }
                if (kullKayitB.KayitVar == false)
                {
                    bbModel.KullaniciTipYetki = false;
                    bbModel.KullaniciTipYetkiYokMsj = "Öğrenim Bilginiz Doğrulanamdı. Hesap bilgilerinizde bulunan YTÜ Lüsansüstü Öğrenci bilgilerinizin doğruluğunu kontrol ediniz lütfen";
                }
                else bbModel.KayitDonemi = kullanici.KayitYilBaslangic + "/" + (kullanici.KayitYilBaslangic + 1) + " " + _entities.Donemlers.First(p => p.DonemID == kullanici.KayitDonemID.Value).DonemAdi + " , " + kullanici.KayitTarihi.ToFormatDate();



            }
            else
            {
                bbModel.KullaniciTipYetki = false;
                bbModel.KullaniciTipYetkiYokMsj = "Mezuniyet başvurusu yapabilmek için hesap bilgilerinizde YTÜ Lisansüstü öğrencisi olduğunuza dair bilgilerin eksiksiz olarak doldurulması gerekmektedir. Profilinizi güncellemek ve başvurunuzu yeniden denemek için sağ üst köşedeki 'Profil bilgilerini düzenle' butonuna tıklayarak 'YTÜ Lisansüstü Öğrencisi Misiniz?' sorusunu cevaplayınız.";
            }
            bbModel.Enstitü = _entities.Enstitulers.First(p => p.EnstituKod == enstituKod);
            bbModel.Kullanici = kullanici;
            ViewBag.BModel = bbModel;
            #endregion


            var q =
                    from kayitSilme in _entities.KayitSilmeBasvurus
                    join kullanicilar in _entities.Kullanicilars on kayitSilme.KullaniciID equals kullanicilar.KullaniciID
                    join programlar in _entities.Programlars on kayitSilme.ProgramKod equals programlar.ProgramKod
                    join ogrenimTipleri in _entities.OgrenimTipleris on new { kayitSilme.EnstituKod, kayitSilme.OgrenimTipKod } equals new { ogrenimTipleri.EnstituKod, ogrenimTipleri.OgrenimTipKod }
                    where kayitSilme.EnstituKod == enstituKod && kayitSilme.KullaniciID == UserIdentity.Current.Id
                    select new FrKayitSilmeBasvuruDto
                    {

                        KayitSilmeBasvuruID = kayitSilme.KayitSilmeBasvuruID,
                        UniqueID = kayitSilme.UniqueID,
                        KayitSilmeDurumID = kayitSilme.KayitSilmeDurumID,
                        DonemID = kayitSilme.DonemID,
                        DonemAdi = kayitSilme.Donemler.DonemAdi,
                        BasvuruTarihi = kayitSilme.BasvuruTarihi,
                        ResimAdi = kullanicilar.ResimAdi,
                        UserKey = kullanicilar.UserKey,
                        AdSoyad = kullanicilar.Ad + " " + kullanicilar.Soyad,
                        OgrenciNo = kayitSilme.OgrenciNo,
                        OgrenimTipAdi = ogrenimTipleri.OgrenimTipAdi,
                        ProgramAdi = programlar.ProgramAdi,
                        AnabilimDaliAdi = programlar.AnabilimDallari.AnabilimDaliAdi,

                        OgretimYiliBaslangic = kayitSilme.OgretimYiliBaslangic,

                        EYKYaGonderildi = kayitSilme.EYKYaGonderildi,
                        EYKYaGonderimDurumAciklamasi = kayitSilme.EYKYaGonderimDurumAciklamasi,
                        EYKYaHazirlandi = kayitSilme.EYKYaHazirlandi,
                        EYKDaOnaylandi = kayitSilme.EYKDaOnaylandi,
                        EYKDaOnaylanmadiDurumAciklamasi = kayitSilme.EYKDaOnaylanmadiDurumAciklamasi,


                    };

            model.RowCount = q.Count();
            if (model.ShowBasvuruUniqueId.HasValue)
                q = q.OrderBy(o => o.UniqueID == model.ShowBasvuruUniqueId ? 1 : 2).ThenBy(t => t.BasvuruTarihi);
            else q = !model.Sort.IsNullOrWhiteSpace() ? q.OrderBy(model.Sort) : q.OrderByDescending(o => o.BasvuruTarihi);
            model.Data = q.Skip(model.StartRowIndex).Take(model.PageSize).ToList();
            return View(model);
        }


        public ActionResult BasvuruYap(Guid? id = null, string ekd = "")
        {
            var mMessage = new MmMessage();
            KmKayitSilmeBasvuruDto model;
            var enstituKod = EnstituBus.GetSelectedEnstitu(ekd);

            var errorMessage = KayitSilmeBus.BasvuruKontrol(enstituKod, id);

            if (!errorMessage.Any())
            {
                model = KayitSilmeBus.GetKayitSilmeBasvuru(id, enstituKod);
            }
            else
            {
                MessageBox.Show("Uyarı", MessageBox.MessageType.Warning, errorMessage.ToArray());
                return RedirectToAction("Index");

            }
            if (mMessage.MessageType != MsgTypeEnum.Information) mMessage.MessageType = mMessage.IsSuccess ? MsgTypeEnum.Success : MsgTypeEnum.Warning;

            return View(model);
        }
        [ValidateInput(false)]
        [HttpPost]
        public ActionResult BasvuruYap(KmKayitSilmeBasvuruDto kModel, string ekd = "")
        {
            var mMessage = new MmMessage
            {
                MessageType = MsgTypeEnum.Success,
                Title = "Kayıt Silme Başvuru İşlemi"
            };
            kModel.EnstituKod = EnstituBus.GetSelectedEnstitu(ekd);


            var kayitSilmeKontrolMessages = KayitSilmeBus.BasvuruKontrol(kModel.EnstituKod, kModel.UniqueID);

            if (kayitSilmeKontrolMessages.Any())
            {
                mMessage.Messages.AddRange(kayitSilmeKontrolMessages);
            }

            if (!mMessage.Messages.Any())
            { 
                if ((!kModel.UniqueID.HasValue || kModel.UniqueID.Value == Guid.Empty) && kModel.DosyaNufusKayitOrnegi == null)
                    mMessage.MessagesDialog.Add(new MrMessage
                    {
                        PropertyName = "NufusKayitOrnekDosyaAdi",
                        MessageType = kModel.NufusKayitOrnekDosyaAdi.IsNullOrWhiteSpace() ? MsgTypeEnum.Error : MsgTypeEnum.Success
                    });

                if (!kModel.IsTaahhutOnay)
                {
                    mMessage.Messages.Add("Taahhüt onayı veriniz.");

                }
                mMessage.MessagesDialog.Add(new MrMessage
                {
                    PropertyName = "IsTaahhutOnay",
                    MessageType = !kModel.IsTaahhutOnay ? MsgTypeEnum.Error : MsgTypeEnum.Success
                });
                if (!mMessage.Messages.Any())
                {
                    KayitSilmeBus.AddOrUpdateKayitSilmeBasvuru(kModel);

                    mMessage.IsSuccess = true;

                }
            }

            mMessage.MessageType = mMessage.IsSuccess ? MsgTypeEnum.Success : MsgTypeEnum.Error;
            return mMessage.ToJsonResult();
        }

        public ActionResult Sil(Guid? uniqueId)
        {

            var mmMessage = KayitSilmeBus.KayitSilmeBasvuruSilKontrol(uniqueId);

            if (mmMessage.IsSuccess)
            {
                try
                {
                    var kayitSilme = _entities.KayitSilmeBasvurus.FirstOrDefault(f => f.UniqueID == uniqueId);

                    if (kayitSilme != null)
                    {
                        _entities.KayitSilmeBasvurus.Remove(kayitSilme);
                        _entities.SaveChanges();
                        mmMessage.Messages.Add(kayitSilme.BasvuruTarihi.ToFormatDateAndTime() + " tarihli Kayıt Silme başvurusu silindi!");
                        LogIslemleri.LogEkle("KayitSilme", LogCrudType.Delete, kayitSilme.ToJson());
                    }
                }
                catch (Exception ex)
                {
                    mmMessage.MessageType = MsgTypeEnum.Error;
                    mmMessage.IsSuccess = false;
                    mmMessage.Messages.Add("Silmek istediğiniz Kayıt Silme başvurusu silinemedi!");
                    mmMessage.Title = "Hata";
                    SistemBilgilendirmeBus.SistemBilgisiKaydet(ex.ToExceptionMessage(), ex.ToExceptionStackTrace(), BilgiTipiEnum.OnemsizHata);
                }
            }
            return Json(new
            {
                mmMessage.IsSuccess,
                messageView = ViewRenderHelper.RenderPartialView("Ajax", "GetMessage", mmMessage)
            }, "application/json", JsonRequestBehavior.AllowGet);
        }




        public ActionResult GetDurumView(Guid? uniqueId)
        {
            var donemProjesiBasvuru = _entities.KayitSilmeBasvurus.Where(p => p.UniqueID == uniqueId).Select(s => new FrKayitSilmeBasvuruDto
            {
                KayitSilmeBasvuruID = s.KayitSilmeBasvuruID,
                KayitSilmeDurumID = s.KayitSilmeDurumID,
                OgretimYiliBaslangic = s.OgretimYiliBaslangic,
                DonemAdi = s.Donemler.DonemAdi,
                BasvuruTarihi = s.BasvuruTarihi,
                IsHarcBirimiOnayladi = s.IsHarcBirimiOnayladi,
                HarcBirimiOnayAciklamasi = s.HarcBirimiOnayAciklamasi,
                IsKutuphaneBirimiOnayladi = s.IsKutuphaneBirimiOnayladi,
                KutuphaneBirimiOnayAciklamasi = s.KutuphaneBirimiOnayAciklamasi,
                EYKYaGonderildi = s.EYKYaGonderildi,
                EYKYaGonderimDurumAciklamasi = s.EYKYaGonderimDurumAciklamasi,
                EYKYaHazirlandi = s.EYKYaHazirlandi,
                EYKDaOnaylandi = s.EYKDaOnaylandi,
                EYKDaOnaylanmadiDurumAciklamasi = s.EYKDaOnaylanmadiDurumAciklamasi
            }).FirstOrDefault() ?? new FrKayitSilmeBasvuruDto();
            var ksBasvuruDurumView = donemProjesiBasvuru.ToKsBasvuruDurumView().ToString();
            var ksBasvuruDonemView = donemProjesiBasvuru.ToKsBasvuruDonemView().ToString();
            return new { ksBasvuruDurumView, ksBasvuruDonemView }.ToJsonResult();
        }


        public ActionResult EykDurumKayit(Guid uniqueId, int onayTipId, bool? onaylandi, string aciklama, DateTime? onayTarihi, string eykSayisi)
        {
            var mmMessage = new MmMessage
            {
                IsSuccess = false,
                Title = "Kayıt Silme " + (onayTipId == EykTipEnum.EykDaOnaylandi ? "EYK'da onay" : (onayTipId == EykTipEnum.EykYaHazirlandi ? "EYK'ya Hazırlık" : "EYK'ya gönderim")) + " işlemi",
                MessageType = MsgTypeEnum.Warning
            };

            var kayitSilmeBasvuru = _entities.KayitSilmeBasvurus.FirstOrDefault(p => p.UniqueID == uniqueId);

            if (onayTipId == EykTipEnum.EykYaGonderildi && !RoleNames.KayitSilmeEykYaGonder.InRoleCurrent())
            {
                mmMessage.Messages.Add("Kayıt Silme başvurularını EYK'ya gönderme yetkisine sahip değilsiniz!");
            }
            else if (onayTipId == EykTipEnum.EykYaHazirlandi && !RoleNames.KayitSilmeEykYaHazirlandi.InRoleCurrent())
            {
                mmMessage.Messages.Add("Kayıt Silme başvurularında EYK'ya hazırlık yetkisine sahip değilsiniz!");
            }
            else if (onayTipId == EykTipEnum.EykDaOnaylandi && !RoleNames.KayitSilmeEykDaOnay.InRoleCurrent())
            {
                mmMessage.Messages.Add("Kayıt Silme başvurularında EYK'da onay yetkisine sahip değilsiniz!");
            }
            else if (kayitSilmeBasvuru == null)
            {
                mmMessage.Messages.Add("İşlem yapılmak istenen Kayıt Silme sistemde bulunamadı!");
            }
            else if (kayitSilmeBasvuru.IsKutuphaneBirimiOnayladi != true)
            {
                mmMessage.Messages.Add("İşlem yapılmak istenen Kayıtta Kütüphane Birimi onayı bulunmadığından EYK işlemleri yapılamaz!");
            }
            else
            {
                if (onayTipId == EykTipEnum.EykDaOnaylandi)
                {
                    if (kayitSilmeBasvuru.EYKYaHazirlandi != true)
                    {
                        mmMessage.Messages.Add("EYK Ya hazırlanmayan Kayıt Silme üzerinde EYK Onayı işlemi yapılamaz!");
                    }
                    else if (onaylandi == true)
                    {
                        if (!onayTarihi.HasValue) mmMessage.Messages.Add("EYK'da onay tarihini giriniz!");
                        if (eykSayisi.IsNullOrWhiteSpace()) mmMessage.Messages.Add("EYK Sayısı giriniz!");
                    }
                    else if (onaylandi == false && aciklama.IsNullOrWhiteSpace())
                    {
                        mmMessage.Messages.Add("EYK'da onaylanmama sebebini giriniz!");
                    }

                }
                else if (onayTipId == EykTipEnum.EykYaGonderildi)
                {
                    if (kayitSilmeBasvuru.KayitSilmeDurumID != KayitSilmeDurumEnums.EnstituYonetimKuruluSureci)
                    {
                        mmMessage.Messages.Add("EYK ya gönderim işlemi yapılabilmesi için öğrencinin sınavdan başarılı olması gerekmetekdir.");
                    }
                    else if (kayitSilmeBasvuru.EYKYaHazirlandi.HasValue)
                    {
                        mmMessage.Messages.Add("EYK ya hazırlama işlemi yapılan bir Kayıt Silme Eyk'ya gönderim işlemi gerçekleştirilemez!");
                    }
                    else if (onaylandi == false && aciklama.IsNullOrWhiteSpace())
                    {
                        mmMessage.Messages.Add("EYK'ya gönderiminin onaylanmama sebebini giriniz!");
                    }
                }

                if (!mmMessage.Messages.Any())
                {
                    var isDegisiklikVar = false;
                    if (onayTipId == EykTipEnum.EykYaGonderildi)
                    {

                        isDegisiklikVar = kayitSilmeBasvuru.EYKYaGonderildi != onaylandi || aciklama != kayitSilmeBasvuru.EYKYaGonderimDurumAciklamasi;
                        kayitSilmeBasvuru.EYKYaGonderimDurumAciklamasi = onaylandi == false ? aciklama : "";
                        kayitSilmeBasvuru.EYKYaGonderildi = onaylandi;
                        kayitSilmeBasvuru.EYKYaGonderildiIslemTarihi = DateTime.Now;
                        kayitSilmeBasvuru.EYKYaGonderildiIslemYapanID = UserIdentity.Current.Id;
                        mmMessage.Messages.Add("Form EYK ya " + (onaylandi.HasValue ? (onaylandi.Value ? "'Gönderildi'" : "'Gönderilmedi'") : "Gönderilmesi bekleniyor") + " şeklinde güncellendi...");
                    }
                    else if (onayTipId == EykTipEnum.EykYaHazirlandi)
                    {

                        kayitSilmeBasvuru.EYKYaHazirlandi = onaylandi;
                        kayitSilmeBasvuru.EYKYaHazirlandiIslemTarihi = DateTime.Now;
                        kayitSilmeBasvuru.EYKYaHazirlandiIslemYapanID = UserIdentity.Current.Id;
                        mmMessage.Messages.Add("Form EYK ya " + (onaylandi.HasValue ? (onaylandi.Value ? "'Hazırlandı'" : "'Hazırlanmadı'") : " Hazırlanması bekleniyor") + " şeklinde güncellendi...");
                    }
                    else if (onayTipId == EykTipEnum.EykDaOnaylandi)
                    {
                        isDegisiklikVar = kayitSilmeBasvuru.EYKDaOnaylandi != onaylandi || aciklama != kayitSilmeBasvuru.EYKDaOnaylanmadiDurumAciklamasi || kayitSilmeBasvuru.EYKTarihi != onayTarihi;
                        kayitSilmeBasvuru.EYKDaOnaylandi = onaylandi;
                        if (onaylandi.HasValue)
                        {
                            kayitSilmeBasvuru.EYKTarihi = onayTarihi;
                            kayitSilmeBasvuru.EYKSayisi = eykSayisi;
                        }
                        kayitSilmeBasvuru.EYKDaOnaylandiIslemYapanID = UserIdentity.Current.Id;
                        kayitSilmeBasvuru.EYKDaOnaylandiOnayTarihi = DateTime.Now;
                        kayitSilmeBasvuru.EYKDaOnaylanmadiDurumAciklamasi = onaylandi == false ? aciklama : "";

                        mmMessage.Messages.Add("Form EYK da " + (onaylandi.HasValue ? (onaylandi.Value ? "'Onaylandı'" : "'Onaylanmadı'") : "İşlem bekliyor") + " şeklinde güncellendi...");
                        var ogrenci = kayitSilmeBasvuru.Kullanicilar;
                        if (onaylandi == true)
                        {
                            if (ogrenci.YtuOgrencisi && ogrenci.ProgramKod == kayitSilmeBasvuru.ProgramKod && ogrenci.OgrenimTipKod == kayitSilmeBasvuru.OgrenimTipKod)
                            {
                                ogrenci.YtuOgrencisi = false;
                                ogrenci.IslemTarihi = DateTime.Now;
                                ogrenci.IslemYapanID = UserIdentity.Current.Id;
                                ogrenci.IslemYapanIP = UserIdentity.Ip;
                            }
                        }
                    }
                    _entities.SaveChanges();
                    mmMessage.MessageType = MsgTypeEnum.Success;
                    mmMessage.IsSuccess = true;

                    LogIslemleri.LogEkle("KayitSilmeBasvuru", LogCrudType.Update, kayitSilmeBasvuru.ToJson());
                    if (isDegisiklikVar && onaylandi.HasValue)
                    {
                        var eykDaOnayOrGonderim = onayTipId == EykTipEnum.EykDaOnaylandi;
                        if (onaylandi == false) KayitSilmeBus.SendMailEykOnaylanmadi(kayitSilmeBasvuru.KayitSilmeBasvuruID, eykDaOnayOrGonderim);
                        else KayitSilmeBus.SendMailEykOnaylandi(kayitSilmeBasvuru.KayitSilmeBasvuruID);
                    }

                }
            }
            var strView = ViewRenderHelper.RenderPartialView("Ajax", "GetMessage", mmMessage);
            return new
            {
                mmMessage.IsSuccess,
                Messages = strView,
                mmMessage
            }.ToJsonResult();
        }


        public ActionResult BasvuruOnay(Guid uniqueId, bool? isOnaylandi, string aciklama, bool isOnayTipHarcOrKutuphane)
        {
            var mmMessage = new MmMessage
            {
                IsSuccess = false,
                Title = "Kayıt Silme " + (isOnayTipHarcOrKutuphane ? "Harç Birimi" : "Kütüphane Birimi") + " onay işlemi",
                MessageType = MsgTypeEnum.Warning
            };
            var kayitSilmeBasvuru = _entities.KayitSilmeBasvurus.FirstOrDefault(f => f.UniqueID == uniqueId);
            if (kayitSilmeBasvuru == null)
            {
                mmMessage.Messages.Add("Onay işlemi yapılmak istenen kayıt sistemde bulunamadı!");
            }
            if (isOnayTipHarcOrKutuphane && !RoleNames.KayitSilmeHarcBirimiBasvuruOnayYetkisi.InRoleCurrent())
            {
                mmMessage.Messages.Add("Kayıt silme Harç Birimi onayı yetkisine sahip değilsiniz!");

            }
            if (!isOnayTipHarcOrKutuphane && !RoleNames.KayitSilmeKutuphaneBirimiBasvuruOnayYetkisi.InRoleCurrent())
            {
                mmMessage.Messages.Add("Kayıt silme Kütüphane Birimi onayı yetkisine sahip değilsiniz!");

            }

            if (isOnaylandi == false && aciklama.IsNullOrWhiteSpace())
            {
                mmMessage.Messages.Add("Reddedildi durumu seçildiğinde açıklama girişi zorunludur!");
            }

            if (!mmMessage.Messages.Any())
            {
                if (isOnayTipHarcOrKutuphane && kayitSilmeBasvuru.IsKutuphaneBirimiOnayladi.HasValue)
                {
                    mmMessage.Messages.Add("Başvuru için Kütüphane Birimi onayı işlemi gerçekleştirildiğinden Harç Birimi onayı işlemi yapılamaz! İşlem için önce Kütüphane Birim onay durumu kaldırılmalıdır.");
                }
                if (!isOnayTipHarcOrKutuphane && kayitSilmeBasvuru.EYKYaGonderildi.HasValue)
                {
                    mmMessage.Messages.Add("Başvuru için EYK'ya gönderim onay işlemi gerçekleştirildiğinden Kütüphane Birimi onayı işlemi yapılamaz! İşlem için önce Eyk'ya gönderim onay durumu kaldırılmalıdır.");
                }
            }


            if (!mmMessage.Messages.Any())
            {
                var isDegisiklikVar = false;
                if (isOnayTipHarcOrKutuphane)
                {
                    isDegisiklikVar = isOnaylandi.HasValue && (kayitSilmeBasvuru.IsHarcBirimiOnayladi != isOnaylandi || kayitSilmeBasvuru.HarcBirimiOnayAciklamasi != aciklama);
                    kayitSilmeBasvuru.HarcBirimiOnayYapanID = UserIdentity.Current.Id;
                    kayitSilmeBasvuru.IsHarcBirimiOnayladi = isOnaylandi;
                    kayitSilmeBasvuru.HarcBirimiOnayAciklamasi = aciklama;
                    kayitSilmeBasvuru.HarcBirimiOnayIslemTarihi = DateTime.Now;

                    kayitSilmeBasvuru.KayitSilmeDurumID = isOnaylandi == true ? KayitSilmeDurumEnums.KutuphaneBirimiOnaySureci : KayitSilmeDurumEnums.HarcBirimiOnaySureci;
                }
                else
                {
                    isDegisiklikVar = isOnaylandi==false && (kayitSilmeBasvuru.IsKutuphaneBirimiOnayladi != false || kayitSilmeBasvuru.KutuphaneBirimiOnayAciklamasi != aciklama);
                    kayitSilmeBasvuru.KutuphaneBirimiOnayYapanID = UserIdentity.Current.Id;
                    kayitSilmeBasvuru.IsKutuphaneBirimiOnayladi = isOnaylandi;
                    kayitSilmeBasvuru.KutuphaneBirimiOnayAciklamasi = aciklama;
                    kayitSilmeBasvuru.KutuphaneBirimiOnayIslemTarihi = DateTime.Now;
                    kayitSilmeBasvuru.KayitSilmeDurumID = isOnaylandi == true ? KayitSilmeDurumEnums.EnstituYonetimKuruluSureci : KayitSilmeDurumEnums.KutuphaneBirimiOnaySureci;
                }
                _entities.SaveChanges();
                LogIslemleri.LogEkle("KayitSilmeBasvuru", LogCrudType.Update, kayitSilmeBasvuru.ToJson());
                if (isDegisiklikVar)
                {
                    if (isOnayTipHarcOrKutuphane)
                    { 
                        KayitSilmeBus.SendMailHarcBirimiOnay(kayitSilmeBasvuru.KayitSilmeBasvuruID, isOnaylandi.Value);
                    }
                    else 
                    { 
                        KayitSilmeBus.SendMailKutuphaneBirimiRet(kayitSilmeBasvuru.KayitSilmeBasvuruID);
                    }
                }

                mmMessage.IsSuccess = true;
                mmMessage.Messages.Add((isOnayTipHarcOrKutuphane ? "Harç Birimi" : "Kütüphane Birimi") + " için başvuru " + (isOnaylandi.HasValue ? (isOnaylandi == true ? "Onaylandı." : "Reddedildi.") : "onayı kaldırıldı."));
            }

            var strView = ViewRenderHelper.RenderPartialView("Ajax", "GetMessage", mmMessage);
            return new
            {
                mmMessage.IsSuccess,
                Messages = strView,
                mmMessage
            }.ToJsonResult();
        }
    }
}