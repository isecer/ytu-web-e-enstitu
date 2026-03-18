using BiskaUtil;
using DevExpress.XtraReports.UI;
using Entities.Entities;
using LisansUstuBasvuruSistemi.Raporlar.Genel;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.Helpers;
using LisansUstuBasvuruSistemi.Utilities.MailManager;
using LisansUstuBasvuruSistemi.Utilities.MenuAndRoles;
using LisansUstuBasvuruSistemi.Utilities.SystemSetting;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;


namespace LisansUstuBasvuruSistemi.Business
{
    public static class TosBus
    {
        public static IHtmlString TosBasvuruDurumView(this TosDurumDto model)
        {
            var pagerString = model.ToRenderPartialViewHtml("TosBasvuru", "BasvuruDurumView");
            return pagerString;
        }
        public static IHtmlString TosBasvuruDonemView(this ToBasvuruSavunmaDto model)
        {
            var pagerString = model.ToRenderPartialViewHtml("TosBasvuru", "BasvuruDonemView");
            return pagerString;
        }
        public static bool BasvuruOlustur(int kullaniciId, DateTime? yeterlikSozluSinavTarihi = null)
        {

            using (var entities = new LubsDbEntities())
            {
                var kul = entities.Kullanicilars.First(f => f.KullaniciID == kullaniciId);
                if (kul.YtuOgrencisi)
                {
                    var obsOgrenci = KullanicilarBus.OgrenciKontrol(kul.OgrenciNo);


                    if (!obsOgrenci.Hata)
                    {
                        kul = entities.Kullanicilars.First(f => f.KullaniciID == kullaniciId);
                        if (!kul.OgrenimTipKod.HasValue)
                        {
                            SistemBilgilendirmeBus.SistemBilgisiKaydet("Tez öneri savunma sınavı oluşturulabilmesi için Öğrenci Öğrenim tipi bilgisi null olmamalı.<br/>Öğrenci Kullanıcı id: " + kul.KullaniciID, ObjectExtensions.GetCurrentMethodPath(), BilgiTipiEnum.Hata);
                            return false;
                        }
                        var ogrenciYeterlikBilgi =
                            obsOgrenci.OgrenciYeters.FirstOrDefault(p => p.DR_YET_GNL_SNV_DURUM == "Başarılı" && p.DR_YET_SOZ_SNV_DURUM == "Başarılı");

                        var ogrenciYetSozSnvTarih = ogrenciYeterlikBilgi?.DR_YET_SOZ_SNV_TARIH.ToDate();
                        if (!yeterlikSozluSinavTarihi.HasValue && ogrenciYetSozSnvTarih.HasValue)
                        {
                            yeterlikSozluSinavTarihi = ogrenciYetSozSnvTarih.Value;

                        }

                        if (!yeterlikSozluSinavTarihi.HasValue)
                        {
                            SistemBilgilendirmeBus.SistemBilgisiKaydet(kul.Ad + " " + kul.Soyad + " Öğrencisi için Tez öneri savunma sınavı başvurusu oluşturulabilmesi için yeterlik sözlü sınav tarihinin OBS sisteminde tanımlı olması gerekmetkedir.", ObjectExtensions.GetCurrentMethodPath(), BilgiTipiEnum.Hata);
                            return true;
                        }
                        //Yeterlik sözlü sınavı tarihi bulunuyor ise  tez öneri savunmas başvurusu oluşturulabilir 
                        var basvuru = entities.ToBasvurus.FirstOrDefault(f =>
                            f.KullaniciID == kul.KullaniciID && f.EnstituKod == kul.EnstituKod &&
                            (f.ProgramKod == kul.ProgramKod || f.OgrenciNo == kul.OgrenciNo));
                        if (basvuru != null)
                        {
                            basvuru.ProgramKod = kul.ProgramKod;
                            basvuru.OgrenciNo = kul.OgrenciNo;
                            basvuru.OgrenimTipKod = kul.OgrenimTipKod.Value;
                            basvuru.KayitOgretimYiliBaslangic = kul.KayitYilBaslangic;
                            basvuru.KayitOgretimYiliDonemID = kul.KayitDonemID;
                            basvuru.YeterlikSozluSinavTarihi = yeterlikSozluSinavTarihi.Value;
                            basvuru.IslemTarihi = DateTime.Now;
                            basvuru.IslemYapanID = GlobalSistemSetting.SystemDefaultAdminKullaniciId;
                            basvuru.IslemYapanIP = "::1";
                        }
                        else
                        {
                            entities.ToBasvurus.Add(new ToBasvuru
                            {
                                UniqueID = Guid.NewGuid(),
                                EnstituKod = kul.EnstituKod,
                                BasvuruTarihi = DateTime.Now,
                                KullaniciID = kul.KullaniciID,
                                OgrenciNo = kul.OgrenciNo,
                                OgrenimTipKod = kul.OgrenimTipKod.Value,
                                ProgramKod = kul.ProgramKod,
                                KayitOgretimYiliBaslangic = kul.KayitYilBaslangic,
                                KayitOgretimYiliDonemID = kul.KayitDonemID,
                                KayitTarihi = kul.KayitTarihi,
                                TezDanismanID = kul.DanismanID,
                                YeterlikSozluSinavTarihi = yeterlikSozluSinavTarihi.Value,
                                IslemTarihi = DateTime.Now,
                                IslemYapanID = GlobalSistemSetting.SystemDefaultAdminKullaniciId,
                                IslemYapanIP = "::1"
                            });
                        }
                        entities.SaveChanges();


                    }
                    else
                    {
                        SistemBilgilendirmeBus.SistemBilgisiKaydet("OBS sisteminden öğrenci bilgisi kontrolü yapılamadı. Hata:" + obsOgrenci.HataMsj, ObjectExtensions.GetCurrentMethodPath(), BilgiTipiEnum.Kritik);
                    }
                }

            }

            return true;
        }


        public class SavunmaDurumInfo
        {
            public int SavunmaNo { get; set; }
            public MmMessage MmMessage { get; set; }
        }
        public static SavunmaDurumInfo TosDurumInfo(Guid toUniqueId, DateTime? sinavTarihi = null, DateTime? yeterlikSozluSinavTarihi = null)
        {
            var savunmaDurumInfo = new SavunmaDurumInfo();
            var msg = new MmMessage();

            using (var entities = new LubsDbEntities())
            {
                var toBasvuru = entities.ToBasvurus.First(p => p.UniqueID == toUniqueId);
                var tezOneriIlkSavunmaHakkiAyKriter = TiAyar.TezOneriIlkSavunmaHakkiAyKriter.GetAyar(toBasvuru.EnstituKod).ToInt(0);
                var tezOneriIkinciSavunmaHakkiAyKriter = TiAyar.TezOneriIkinciSavunmaHakkiAyKriter.GetAyar(toBasvuru.EnstituKod).ToInt(0);
                var tezOneriToplamSavunmaHakkiAyKriter = tezOneriIlkSavunmaHakkiAyKriter + tezOneriIkinciSavunmaHakkiAyKriter;
                var tezOneriDuzeltmeSonrasiSavunmaHakkiAyKriter = TiAyar.TezOneriDuzeltmeSonrasiSavunmaHakkiAyKriter.GetAyar(toBasvuru.EnstituKod).ToInt(0);

                var toBasvuruSavunmas = toBasvuru.ToBasvuruSavunmas.OrderByDescending(o => o.ToBasvuruSavunmaID).ToList();
                var sonBasvuruDegerlendirmeYapilan = toBasvuruSavunmas.FirstOrDefault(p => p.ToBasvuruSavunmaDurumID.HasValue);

                sinavTarihi = sinavTarihi ?? DateTime.Now;
                sinavTarihi = sinavTarihi.Value.Date;
                string muafiyetBilgisi = toBasvuru.IsBasvuruKriterMuaf
                    ? "Öğrenci başvuru kriterlerinden muaf tutulmaktadır. "
                    : "";
                yeterlikSozluSinavTarihi = yeterlikSozluSinavTarihi ?? toBasvuru.YeterlikSozluSinavTarihi;
                if (sonBasvuruDegerlendirmeYapilan == null)
                {

                    var ilkBitisTarihi = toBasvuru.IlkOneriBitisTarihi ?? yeterlikSozluSinavTarihi.Value.ToGetBitisTarihi(tezOneriIlkSavunmaHakkiAyKriter).AddDays(1).AddSeconds(-1);
                    var ikinciBitisTarihi = toBasvuru.IkinciOneriBitisTarihi ?? yeterlikSozluSinavTarihi.Value.ToGetBitisTarihi(tezOneriToplamSavunmaHakkiAyKriter).AddDays(1).AddSeconds(-1);


                    if (toBasvuru.IsBasvuruKriterMuaf || sinavTarihi <= ilkBitisTarihi)
                    {
                        savunmaDurumInfo.SavunmaNo = 1;
                        msg.MessagesDialog.Add(new MrMessage
                        {
                            IsSucces = true,
                            Message = $"{muafiyetBilgisi}1. Savunma hakkı kullanımı için {yeterlikSozluSinavTarihi.Value.ToFormatDate()} / {ilkBitisTarihi.ToFormatDate()} tarihleri arasında savunma sınavı yapılabilir. Bu, öğrencinin ilk tez öneri savunmasıdır."
                        });
                    }
                    else if (sinavTarihi <= ikinciBitisTarihi)
                    {
                        savunmaDurumInfo.SavunmaNo = 2;
                        msg.MessagesDialog.Add(new MrMessage
                        {
                            IsSucces = true,
                            Message = $"2. Savunma hakkı kullanımı için {ilkBitisTarihi.ToFormatDate()} / {ikinciBitisTarihi.ToFormatDate()} tarihleri arasında savunma sınavı yapılabilir. İlk savunma hakkı süresi geçmiştir."
                        });
                    }
                    else
                    {
                        savunmaDurumInfo.SavunmaNo = 0;
                        msg.MessagesDialog.Add(new MrMessage
                        {
                            IsSucces = false,
                            Message = $"Süreniz dolduğundan başvuru yapamazsınız. Yeterlik sözlü sınavınızı {yeterlikSozluSinavTarihi.Value.ToFormatDate()} tarihinde tamamladınız. " +
                                      $"1. savunma hakkınız için son başvuru tarihi {ilkBitisTarihi.ToFormatDate()}, " +
                                      $"2. savunma hakkınız için son başvuru tarihi {ikinciBitisTarihi.ToFormatDate()} idi. " +
                                      $"Toplam savunma süresi {tezOneriToplamSavunmaHakkiAyKriter} ay olup, bu süre içinde savunma yapılması gerekmekteydi."
                        });
                    }

                    toBasvuru.IlkOneriBitisTarihi = ilkBitisTarihi;
                    toBasvuru.IkinciOneriBitisTarihi = ikinciBitisTarihi;
                    toBasvuru.RetDuzeltmeBitisTarihi = null;
                }
                else if (sonBasvuruDegerlendirmeYapilan.ToBasvuruSavunmaDurumID == ToBasvuruSavunmaDurumuEnum.KabulEdildi)
                {
                    savunmaDurumInfo.SavunmaNo = 1;
                    msg.MessagesDialog.Add(new MrMessage
                    {
                        IsSucces = true,
                        Message = $"{muafiyetBilgisi}{sonBasvuruDegerlendirmeYapilan.SRTalepleris.First().Tarih.ToFormatDate()} tarihinde yapılan {sonBasvuruDegerlendirmeYapilan.SavunmaNo}. savunma başarılı bir şekilde sonuçlandı. Öğrenci isterse yeni bir tez öneri savunma başvurusu yapabilir. Bu durumda, yeni başvuru 1. savunma olarak değerlendirilecektir."
                    });

                    toBasvuru.RetDuzeltmeBitisTarihi = null;
                    toBasvuru.IlkOneriBitisTarihi = null;
                    toBasvuru.IkinciOneriBitisTarihi = null;
                }
                else if (sonBasvuruDegerlendirmeYapilan.ToBasvuruSavunmaDurumID == ToBasvuruSavunmaDurumuEnum.Reddedildi)
                {
                    var sonBasvuruSinav = sonBasvuruDegerlendirmeYapilan.SRTalepleris.First();
                    var retBitisTarihi = (toBasvuru.RetDuzeltmeBitisTarihi ?? sonBasvuruSinav.Tarih.ToGetBitisTarihi(tezOneriIkinciSavunmaHakkiAyKriter)).Date;

                    if (sonBasvuruDegerlendirmeYapilan.SavunmaNo == 1 && (toBasvuru.IsBasvuruKriterMuaf || sinavTarihi <= retBitisTarihi))
                    {
                        savunmaDurumInfo.SavunmaNo = 2;
                        msg.MessagesDialog.Add(new MrMessage
                        {
                            IsSucces = true,
                            Message = $"{muafiyetBilgisi}{sonBasvuruSinav.Tarih.Date.ToFormatDate()} tarihinde yapılan 1. savunma sınavı başarısızlıkla sonuçlanmıştır. 2. savunma hakkınız bulunmaktadır ve bu sınavın {retBitisTarihi.ToFormatDate()} tarihine kadar yapılması gerekmektedir."
                        });
                    }
                    else
                    {
                        savunmaDurumInfo.SavunmaNo = toBasvuru.IsBasvuruKriterMuaf ? sonBasvuruDegerlendirmeYapilan.SavunmaNo : 0;
                        msg.MessagesDialog.Add(new MrMessage
                        {
                            IsSucces = toBasvuru.IsBasvuruKriterMuaf,
                            Message = toBasvuru.IsBasvuruKriterMuaf
                                ? $"{muafiyetBilgisi}Son savunma başarısızlıkla sonuçlanmıştır, ancak öğrenci kriterlerden muaf olduğu için yeni bir başvuru yapabilir. Savunma numarası {savunmaDurumInfo.SavunmaNo} olarak devam edecektir."
                                : $"{sonBasvuruSinav.Tarih.Date.ToFormatDate()} tarihinde yapılan 2. savunma başarısızlıkla sonuçlanmıştır. Maalesef başka savunma hakkınız kalmamıştır."
                        });
                    }

                    toBasvuru.RetDuzeltmeBitisTarihi = retBitisTarihi;
                    toBasvuru.IlkOneriBitisTarihi = null;
                    toBasvuru.IkinciOneriBitisTarihi = null;
                }

                else if (sonBasvuruDegerlendirmeYapilan.ToBasvuruSavunmaDurumID == ToBasvuruSavunmaDurumuEnum.Duzeltme)
                {
                    var duzeltmeAlinanSinav = sonBasvuruDegerlendirmeYapilan.SRTalepleris.First();
                    var duzeltmeBitisTarihi = (toBasvuru.RetDuzeltmeBitisTarihi ?? duzeltmeAlinanSinav.Tarih.ToGetBitisTarihi(tezOneriDuzeltmeSonrasiSavunmaHakkiAyKriter)).Date;

                    var isDuzeltmeAktif = sinavTarihi <= duzeltmeBitisTarihi;

                    if (isDuzeltmeAktif || toBasvuru.IsBasvuruKriterMuaf)
                    {
                        savunmaDurumInfo.SavunmaNo = sonBasvuruDegerlendirmeYapilan.SavunmaNo;
                        msg.MessagesDialog.Add(new MrMessage
                        {
                            IsSucces = true,
                            Message = $"{muafiyetBilgisi}{duzeltmeAlinanSinav.Tarih.Date.ToFormatDate()} tarihli {sonBasvuruDegerlendirmeYapilan.SavunmaNo}. savunmadan düzeltme hakkı alındı. " +
                                      $"Düzeltme son tarihi: {duzeltmeBitisTarihi.ToFormatDate()}. "
                        });
                    }
                    else if (sonBasvuruDegerlendirmeYapilan.SavunmaNo == 1)
                    {
                        savunmaDurumInfo.SavunmaNo = 2;
                        var ikinciBitisTarihi = duzeltmeBitisTarihi.ToGetBitisTarihi(tezOneriIkinciSavunmaHakkiAyKriter);
                        var isIkinciSavunmaAktif = sinavTarihi <= ikinciBitisTarihi;

                        var message =
                            $"1. savunma düzeltme süresi {duzeltmeBitisTarihi.ToFormatDate()} tarihinde doldu. ";


                        // 2. savunma hakkı doldu mu kontrolü
                        if (!isIkinciSavunmaAktif)
                        {
                            message += $"2. savunma hakkı {ikinciBitisTarihi.ToFormatDate()} tarihinde doldu. Savunma hakkınız kalmadı. ";
                        }
                        else
                        {
                            message += $"2. savunma son tarihi: {ikinciBitisTarihi.ToFormatDate()}. ";
                        }

                        msg.MessagesDialog.Add(new MrMessage
                        {
                            IsSucces = isIkinciSavunmaAktif,
                            Message = message
                        });

                        toBasvuru.IkinciOneriBitisTarihi = ikinciBitisTarihi;
                    }
                    else
                    {
                        savunmaDurumInfo.SavunmaNo = 0;
                        msg.MessagesDialog.Add(new MrMessage
                        {
                            IsSucces = false,
                            Message = $"Tüm savunma hakları kullanıldı. Aktif savunma hakkı bulunmamaktadır."
                        });
                    }

                    toBasvuru.RetDuzeltmeBitisTarihi = duzeltmeBitisTarihi;
                    toBasvuru.IlkOneriBitisTarihi = null;
                }


                savunmaDurumInfo.MmMessage = msg;
                toBasvuru.YeterlikSozluSinavTarihi = yeterlikSozluSinavTarihi.Value;
                entities.SaveChanges();
                return savunmaDurumInfo;
            }
        }

        public static MmMessage GetTosSilKontrol(Guid toBasvuruSavunmaUniqueId)
        {
            var msg = new MmMessage();

            using (var entities = new LubsDbEntities())
            {
                var tezOneriSavunma = entities.ToBasvuruSavunmas.FirstOrDefault(f => f.UniqueID == toBasvuruSavunmaUniqueId);
                if (tezOneriSavunma == null)
                {
                    msg.Messages.Add("Tez Öneri Savunması Bulunamadı.");
                }
                else
                {
                    var silYetki = RoleNames.TosGelenBasvuruSil.InRoleCurrent();
                    var isAdmin = UserIdentity.Current.IsAdmin;
                    if (!UserIdentity.Current.EnstituKods.Contains(tezOneriSavunma.ToBasvuru.EnstituKod) && silYetki && tezOneriSavunma.ToBasvuru.KullaniciID != UserIdentity.Current.Id)
                    {
                        msg.Messages.Add("Bu enstitüye ait başvuruyu silmeye yetkili değilsiniz!");
                    }
                    else if (!isAdmin && !TiAyar.TezOneriSavunmaBasvuruAlimiAcik.GetAyar(tezOneriSavunma.ToBasvuru.EnstituKod, "false").ToBoolean(false) && UserIdentity.Current.IsAdmin == false)
                    {
                        msg.Messages.Add("Başvuru süreci kapalı olduğundan başvuru üzerinden herhangi bir işlem yapılamaz!");

                    }
                    else if (!isAdmin && silYetki == false && tezOneriSavunma.ToBasvuru.KullaniciID != UserIdentity.Current.Id)
                    {
                        msg.Messages.Add("Başka bir kullanıcıya ait başvuruyu silmeye hakkınız yoktur!");
                    }
                    else if (tezOneriSavunma.ToBasvuruSavunmaKomites.Any(a => a.ToBasvuruSavunmaDurumID.HasValue))
                    {
                        msg.Messages.Add("Komite üyeleri tarafından değerlendirme yapıldıktan sonra Tez Öneri Savunması silinemez!");
                    }

                }

                msg.IsSuccess = !msg.Messages.Any();
            }
            return msg;
        }
        public static TosBasvuruDetayDto GetSecilenBasvuruDetay(Guid toUniqueId, Guid? tosKomiteUniqueId)
        {
            var model = new TosBasvuruDetayDto();
            using (var entities = new LubsDbEntities())
            {

                var basvuru = entities.ToBasvurus.First(p => p.UniqueID == toUniqueId);
                var enstitu = entities.Enstitulers.First(p => p.EnstituKod == basvuru.EnstituKod);

                var eslesenDanisman = entities.Kullanicilars.FirstOrDefault(p => p.KullaniciID == (basvuru.TezDanismanID ?? 0));
                if (eslesenDanisman != null)
                {
                    model.TezDanismaniUserKey = eslesenDanisman.UserKey;
                    var unvanAdi = eslesenDanisman.Unvanlar != null ? eslesenDanisman.Unvanlar.UnvanAdi : "";
                    model.TezDanismanBilgiEslesen = unvanAdi + " " + eslesenDanisman.Ad + " " + eslesenDanisman.Soyad;
                }
                else
                {
                    model.TezDanismanBilgiEslesen = "Sistemde eşleşen tez danışmanı bulunamadı.";
                }



                model.ToBasvuruID = basvuru.ToBasvuruID;
                model.UniqueID = basvuru.UniqueID;
                model.IsBasvuruKriterMuaf = basvuru.IsBasvuruKriterMuaf;
                model.YeterlikSozluSinavTarihi = basvuru.YeterlikSozluSinavTarihi;
                model.TezDanismanID = basvuru.TezDanismanID;
                model.BasvuruTarihi = basvuru.BasvuruTarihi;
                model.YeterlikSozluSinavTarihi = basvuru.YeterlikSozluSinavTarihi;
                model.KullaniciID = basvuru.KullaniciID;
                model.KayitDonemi = basvuru.KayitOgretimYiliBaslangic + "/" + (basvuru.KayitOgretimYiliBaslangic + 1) + " " + entities.Donemlers.First(p => p.DonemID == basvuru.KayitOgretimYiliDonemID.Value).DonemAdi;
                model.ResimAdi = basvuru.Kullanicilar.ResimAdi;
                model.Ad = basvuru.Kullanicilar.Ad;
                model.Soyad = basvuru.Kullanicilar.Soyad;
                model.TcKimlikNo = basvuru.Kullanicilar.TcKimlikNo;
                model.OgrenciNo = basvuru.OgrenciNo;
                model.OgrenimTipAdi = entities.OgrenimTipleris.First(p => p.EnstituKod == basvuru.EnstituKod && p.OgrenimTipKod == basvuru.OgrenimTipKod).OgrenimTipAdi;

                model.AnabilimdaliAdi = basvuru.Programlar.AnabilimDallari.AnabilimDaliAdi;
                model.ProgramAdi = basvuru.Programlar.ProgramAdi;
                model.OgrenimTipKod = basvuru.OgrenimTipKod;
                model.ProgramKod = basvuru.ProgramKod;
                model.KayitOgretimYiliBaslangic = basvuru.KayitOgretimYiliBaslangic;
                model.KayitOgretimYiliDonemID = basvuru.KayitOgretimYiliDonemID;
                model.KayitTarihi = basvuru.KayitTarihi;
                model.EnstituAdi = enstitu.EnstituAd;
                model.KullaniciID = basvuru.KullaniciID;
                model.BasvuruTarihi = basvuru.BasvuruTarihi;
                model.IlkOneriBitisTarihi = basvuru.IlkOneriBitisTarihi;
                model.IkinciOneriBitisTarihi = basvuru.IkinciOneriBitisTarihi;
                model.RetDuzeltmeBitisTarihi = basvuru.RetDuzeltmeBitisTarihi;
                model.IslemTarihi = basvuru.IslemTarihi;
                model.IslemYapanID = basvuru.IslemYapanID;
                model.IslemYapanIP = basvuru.IslemYapanIP;
                model.DegerlendirenUniqueID = tosKomiteUniqueId;

                model.ToBasvuruSavunmaList = basvuru.ToBasvuruSavunmas
                    .Select(s => new ToBasvuruSavunmaDto
                    {
                        UniqueID = s.UniqueID,
                        FormKodu = s.FormKodu,
                        ToBasvuruSavunmaID = s.ToBasvuruSavunmaID,
                        ToBasvuruID = s.ToBasvuruID,
                        SavunmaBasvuruTarihi = s.SavunmaBasvuruTarihi,
                        SavunmaNo = s.SavunmaNo,
                        IsTezDiliTr = s.IsTezDiliTr,
                        YeniTezBaslikTr = s.YeniTezBaslikTr,
                        YeniTezBaslikEn = s.YeniTezBaslikEn,
                        CalismaRaporDosyaAdi = s.CalismaRaporDosyaAdi,
                        CalismaRaporDosyaYolu = s.CalismaRaporDosyaYolu,
                        DonemAdi = s.DonemBaslangicYil + " / " + (s.DonemBaslangicYil + 1) + " " + (s.DonemID == 1 ? "Güz" : "Bahar"),
                        IsYokDrBursiyeriVar = s.IsYokDrBursiyeriVar,
                        YokDrOncelikliAlan = s.YokDrOncelikliAlan,
                        IslemTarihi = s.IslemTarihi,
                        IslemYapanID = s.IslemYapanID,
                        IslemYapanIP = s.IslemYapanIP,
                        ToBasvuruSavunmaDurumID = s.ToBasvuruSavunmaDurumID,
                        DurumAdi = s.ToBasvuruSavunmaDurumID.HasValue ? s.ToBasvuruSavunmaDurumlari.DurumAdi : "Henüz Değerlendirme Tamamlanmadı",
                        IsOyBirligiOrCoklugu = s.IsOyBirligiOrCoklugu,
                        DegerlendirmeSonucMailTarihi = s.DegerlendirmeSonucMailTarihi,
                        ToplantiBilgiGonderimTarihi = s.ToplantiBilgiGonderimTarihi,
                        ToSavunmaBaslatildiMailGonderimTarihi = s.ToSavunmaBaslatildiMailGonderimTarihi,
                        DurumModel = new TosDurumDto
                        {

                            IsTezOnerisiVar = true,
                            ToBasvuruSavunmaDurumID = s.ToBasvuruSavunmaDurumID,
                            IsSrTalebiYapildi = s.SRTalepleris.Any(),
                            DegerlendirmeBasladi = s.ToBasvuruSavunmaKomites.Any(a => a.ToBasvuruSavunmaDurumID.HasValue),
                            IsOyBirligiOrCoklugu = s.IsOyBirligiOrCoklugu
                        },

                        ToBasvuruSavunmaKomites = entities.ToBasvuruSavunmaKomites.Where(p => p.ToBasvuruSavunmaID == s.ToBasvuruSavunmaID).Include("ToBasvuruSavunmaDurumlari").ToList(),
                        //ToBasvuruSavunmaKomites = s.ToBasvuruSavunmaKomites.AsQueryable().Include("ToBasvuruSavunmaDurumlari").ToList(),
                        SRModel = (from sR in s.SRTalepleris
                                   join tt in entities.SRTalepTipleris on sR.SRTalepTipID equals tt.SRTalepTipID
                                   join hg in entities.HaftaGunleris on sR.HaftaGunID equals hg.HaftaGunID
                                   join d in entities.SRDurumlaris on sR.SRDurumID equals d.SRDurumID
                                   select new FrTalepler
                                   {
                                       SRTalepID = sR.SRTalepID,
                                       TalepYapanID = sR.TalepYapanID,
                                       TalepTipAdi = tt.TalepTipAdi,
                                       SRTalepTipID = sR.SRTalepTipID,
                                       SRSalonID = sR.SRSalonID,
                                       IsOnline = sR.IsOnline,
                                       SalonAdi = sR.SalonAdi,
                                       Tarih = sR.Tarih,
                                       HaftaGunID = sR.HaftaGunID,
                                       HaftaGunAdi = hg.HaftaGunAdi,
                                       BasSaat = sR.BasSaat,
                                       BitSaat = sR.BitSaat,
                                       SRDurumID = sR.SRDurumID,
                                       DurumAdi = d.DurumAdi,
                                       DurumListeAdi = d.DurumAdi,
                                       ClassName = d.ClassName,
                                       Color = d.Color,
                                       SRDurumAciklamasi = sR.SRDurumAciklamasi,
                                       IslemTarihi = s.IslemTarihi,
                                       IslemYapanID = s.IslemYapanID,
                                       IslemYapanIP = s.IslemYapanIP
                                   }).FirstOrDefault()
                    }).OrderByDescending(o => o.SavunmaBasvuruTarihi).ToList();

                model.ToplamBasarisizTezOneriSavunmaHak = TiAyar.TezOneriToplamBasarisizTezOneriSavunmaHak.GetAyar(basvuru.EnstituKod).ToInt();
                model.IlkSavunmaHakkiAyKriter = TiAyar.TezOneriIlkSavunmaHakkiAyKriter.GetAyar(basvuru.EnstituKod).ToInt();
                model.IkinciSavunmaHakkiAyKriter = TiAyar.TezOneriIkinciSavunmaHakkiAyKriter.GetAyar(basvuru.EnstituKod).ToInt();

                var sonTos = model.ToBasvuruSavunmaList.FirstOrDefault();

                model.DurumHtmlString = (
                                            sonTos != null ? sonTos.DurumModel : new TosDurumDto()
                                        ).TosBasvuruDurumView().ToString();
                model.DonemHtmlString = (sonTos ?? new ToBasvuruSavunmaDto()).TosBasvuruDonemView().ToString();

                var sinavTarihi = model.ToBasvuruSavunmaList.FirstOrDefault()?.SRModel?.Tarih;
                var basvuruKiterKontrol = TosDurumInfo(basvuru.UniqueID, sinavTarihi);

                if (sonTos == null)
                {  

                    if (basvuruKiterKontrol.MmMessage.IsSuccess)
                    {
                        model.IsAnketDolduruldu = basvuru.AnketCevaplaris.Any();
                        if (model.IsAnketDolduruldu == false)
                        {
                            var anketId = TiAyar.TezOneriIlkBasvuruAnketi.GetAyar(basvuru.EnstituKod, "").ToInt();
                            model.IsAnketVar = anketId > 0;
                            if (anketId > 0)
                            {
                                model.AnketView = AnketlerBus.GetAnketView(
                                    anketId: anketId.Value,
                                    anketTipId: AnketTipiEnum.DoktoraTezOneriSinaviBasvuruAnketi,
                                    toBasvuruId: basvuru.ToBasvuruID,
                                    rowId: basvuru.UniqueID.ToString()
                                );
                            }
                        }
                    }

                }

                if (basvuruKiterKontrol.MmMessage.MessagesDialog.Any())
                {
                    model.SavunmaBasvurusuYapmaSureBilgisiInfo = string.Join("<br/>", basvuruKiterKontrol.MmMessage.MessagesDialog.Select(s => s.Message));
                }

            }
            return model;

        }


        public static MmMessage SendMailTosBilgisi(int? toBasvuruSavunmaId, int? srTalepId)
        {
            return MailSenderTos.SendMailTosBilgisi(toBasvuruSavunmaId, srTalepId);
        }
        public static MmMessage SendMailTosDegerlendirmeLink(Guid toBasvuruSavunmaId, Guid? tosKomiteUniqueId, bool isLinkOrSonuc)
        {
            return MailSenderTos.SendMailTosDegerlendirmeLink(toBasvuruSavunmaId, tosKomiteUniqueId, isLinkOrSonuc);
        }
        public static List<CmbStringDto> CmbTiDonemListe(string enstituKod, bool bosSecimVar = false)
        {

            using (var entities = new LubsDbEntities())
            {
                var donems = entities.ToBasvuruSavunmas.Select(s => new { s.DonemBaslangicYil, s.DonemID, s.Donemler.DonemAdi })
                    .Distinct().OrderByDescending(o => o.DonemBaslangicYil).ThenByDescending(t => t.DonemID).Select(s => new CmbStringDto
                    {
                        Value = s.DonemBaslangicYil + "" + s.DonemID,
                        Caption = s.DonemBaslangicYil + "/" + (s.DonemBaslangicYil + 1) + " " + s.DonemAdi

                    }).ToList();
                if (bosSecimVar) donems.Insert(0, new CmbStringDto { Value = null, Caption = "" });
                return donems;
            }
        }
        public static List<CmbStringDto> CmbTosDonemListeBasvuru(string enstituKod, bool bosSecimVar = false)
        {
            var cmbDonems = CmbTiDonemListe(enstituKod);
            if (!cmbDonems.Any())
            {
                var donem = DateTime.Now.ToAkademikDonemBilgi();
                cmbDonems.Add(new CmbStringDto()
                {
                    Value = donem.BaslangicYil + "" + donem.DonemId,
                    Caption = donem.BaslangicYil + "/" + (donem.BaslangicYil + 1) + " " + donem.DonemAdi
                });
                if (bosSecimVar) cmbDonems.Insert(0, new CmbStringDto { Value = null, Caption = "" });
            }
            using (var entities = new LubsDbEntities())
            {
                var donems = entities.ToBasvuruSavunmas.Select(s => new { s.DonemBaslangicYil, s.DonemID, s.Donemler.DonemAdi })
                    .Distinct().OrderByDescending(o => o.DonemBaslangicYil).ThenByDescending(t => t.DonemID).Select(s => new CmbStringDto
                    {
                        Value = s.DonemBaslangicYil + "" + s.DonemID,
                        Caption = s.DonemBaslangicYil + "/" + (s.DonemBaslangicYil + 1) + " " + s.DonemAdi

                    }).ToList();
                if (bosSecimVar) donems.Insert(0, new CmbStringDto { Value = null, Caption = "" });
                return donems;
            }
        }
        public static List<CmbIntDto> CmbTosNumarasi(bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            for (int i = 1; i <= 2; i++)
            {
                dct.Add(new CmbIntDto { Value = i, Caption = i + ". Savunma" });
            }

            return dct;

        }
        public static List<CmbIntDto> CmbTosDurumListe(bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();

            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });


            dct.Add(new CmbIntDto { Value = 999, Caption = "Tez Önerisi Yapmayanlar" });
            dct.Add(new CmbIntDto { Value = 1000, Caption = "Sınav Bilgisi Girilmeyenler" });
            dct.Add(new CmbIntDto { Value = 1001, Caption = "Sınav Bilgisi Girilenler" });
            dct.Add(new CmbIntDto { Value = 1002, Caption = "Değerlendirme Sürecinde Olanlar" });
            dct.Add(new CmbIntDto { Value = 1003, Caption = "Değerlendirme Sürecinde Tamamlananlar" });
            dct.Add(new CmbIntDto { Value = 1, Caption = "Başarılı Olanlar" });
            dct.Add(new CmbIntDto { Value = 2, Caption = "Başarısız Olanlar" });
            dct.Add(new CmbIntDto { Value = 3, Caption = "Düzeltme Alanlar" });
            return dct;
        }

        public static List<CmbStringDto> CmbDonemListe(bool bosSecimVar = false)
        {

            using (var entities = new LubsDbEntities())
            {
                var donems = entities.ToBasvuruSavunmas.Select(s => new { s.DonemBaslangicYil, s.DonemID, s.Donemler.DonemAdi })
                    .Distinct().OrderByDescending(o => o.DonemBaslangicYil).ThenByDescending(t => t.DonemID).Select(s => new CmbStringDto
                    {
                        Value = s.DonemBaslangicYil + "" + s.DonemID,
                        Caption = s.DonemBaslangicYil + "/" + (s.DonemBaslangicYil + 1) + " " + s.DonemAdi

                    }).ToList();
                if (bosSecimVar) donems.Insert(0, new CmbStringDto { Value = null, Caption = "" });
                return donems;
            }
        }
        public static List<CmbIntDto> GetCmbFilterAnabilimDallari(string enstituKod, bool bosSecimVar = false)
        {
            using (var entities = new LubsDbEntities())
            {
                var anabilimDaliIds = entities.ToBasvurus
                    .Where(p => p.EnstituKod == enstituKod).Select(s => s.Programlar.AnabilimDaliID).Distinct().ToList();

                var anabilimDallaris = entities.AnabilimDallaris.Where(p => anabilimDaliIds.Contains(p.AnabilimDaliID))
                    .Select(s => new { s.AnabilimDaliID, s.AnabilimDaliAdi }).OrderBy(o => o.AnabilimDaliAdi).Select(
                        s =>
                            new CmbIntDto { Value = s.AnabilimDaliID, Caption = s.AnabilimDaliAdi }
                        ).ToList();
                if (bosSecimVar) anabilimDallaris.Insert(0, new CmbIntDto { Value = null, Caption = "" });

                return anabilimDallaris;
            }
        }

        public static XtraReport TezOneriAraRaporIstemiYazilari(int toBasvuruSavunmaId)
        {
            using (var entities = new LubsDbEntities())
            {
                var sablonTipIds = new List<int>
                    {
                        YaziSablonTipiEnum.TosDegerlendirmeSonucuAraRaporIstemiYazisiDanisman,
                        YaziSablonTipiEnum.TosDegerlendirmeSonucuAraRaporIstemiYazisiTikUyeleri
                };

                var tezOneriBasvuruSavunma = entities.ToBasvuruSavunmas.First(p => p.ToBasvuruSavunmaID == toBasvuruSavunmaId);
                var tezOneribasvuru = tezOneriBasvuruSavunma.ToBasvuru;
                var enstitu = tezOneribasvuru.Enstituler;
                var anabilimDaliAdi = tezOneribasvuru.Programlar.AnabilimDallari.AnabilimDaliAdi.IlkHarfiBuyut();
                var programAdi = tezOneribasvuru.Programlar.ProgramAdi.IlkHarfiBuyut();
                var ogrenciNo = tezOneribasvuru.OgrenciNo;
                var ogrenciAdSoyad = (tezOneribasvuru.Kullanicilar.Ad).IlkHarfiBuyut() + " " + tezOneribasvuru.Kullanicilar.Soyad.ToUpper();

                var tiks = tezOneriBasvuruSavunma.ToBasvuruSavunmaKomites.Where(p => !p.IsTezDanismani).ToList();
                var tezDanisman = tezOneriBasvuruSavunma.ToBasvuruSavunmaKomites.First(p => p.IsTezDanismani);

                var tezBaslik = tezOneriBasvuruSavunma.IsTezDiliTr
                    ? tezOneriBasvuruSavunma.YeniTezBaslikTr
                    : tezOneriBasvuruSavunma.YeniTezBaslikEn;


                DateTime? sinavTarihi = null;
                if (tezOneriBasvuruSavunma.ToBasvuruSavunmaDurumID == ToBasvuruSavunmaDurumuEnum.Reddedildi)
                {
                    var sinav = tezOneriBasvuruSavunma.SRTalepleris.First();
                    sinavTarihi = sinav.Tarih;
                    sablonTipIds = new List<int> { YaziSablonTipiEnum.TezOneriSavunmaTezOnerisiBasarisizYazisiAbd, YaziSablonTipiEnum.TezOneriSavunmaTezOnerisiBasarisizYazisiDanisman };
                }
                else if (tezOneriBasvuruSavunma.ToBasvuruSavunmaDurumID == ToBasvuruSavunmaDurumuEnum.Duzeltme)
                {
                    var sinav = tezOneriBasvuruSavunma.SRTalepleris.First();
                    sinavTarihi = sinav.Tarih;
                    sablonTipIds = new List<int> { YaziSablonTipiEnum.TezOneriSavunmaTezOnerisiDuzeltmeYazisiAbd, YaziSablonTipiEnum.TezOneriSavunmaTezOnerisiDuzeltmeYazisiDanisman };
                }
                var sablonlar = entities.YaziSablonlaris.Where(p => sablonTipIds.Contains(p.YaziSablonTipID) && p.EnstituKod == enstitu.EnstituKod && p.IsAktif).ToList();

                var sablonInx = 0;
                XtraReport rprX = null;
                var sablonModel = new List<KeyValuePair<YaziSablonlari, ToBasvuruSavunmaKomite>>();

                foreach (var sablonTipId in sablonTipIds)
                {
                    var sablon = sablonlar.FirstOrDefault(f => f.YaziSablonTipID == sablonTipId);
                    if (sablon == null) continue;
                    if (sablon.YaziSablonTipID == YaziSablonTipiEnum.TezOneriSavunmaTezOnerisiBasarisizYazisiAbd || sablon.YaziSablonTipID == YaziSablonTipiEnum.TezOneriSavunmaTezOnerisiDuzeltmeYazisiAbd)
                    {
                        sablonModel.Add(new KeyValuePair<YaziSablonlari, ToBasvuruSavunmaKomite>(sablon, new ToBasvuruSavunmaKomite()));
                    }
                    else if (sablon.YaziSablonTipID == YaziSablonTipiEnum.TosDegerlendirmeSonucuAraRaporIstemiYazisiDanisman ||
                             sablon.YaziSablonTipID == YaziSablonTipiEnum.TezOneriSavunmaTezOnerisiBasarisizYazisiDanisman ||
                             sablon.YaziSablonTipID == YaziSablonTipiEnum.TezOneriSavunmaTezOnerisiDuzeltmeYazisiDanisman)
                    {
                        sablonModel.Add(new KeyValuePair<YaziSablonlari, ToBasvuruSavunmaKomite>(sablon, tezDanisman));
                    }
                    else
                    {
                        // Diğer sablonlar için asilJuris elemanlarını ekle
                        tiks.ForEach(item =>
                            sablonModel.Add(new KeyValuePair<YaziSablonlari, ToBasvuruSavunmaKomite>(sablon, item)));
                    }

                }

                foreach (var sablon in sablonModel)
                {
                    var tik1 = tezOneriBasvuruSavunma.ToBasvuruSavunmaKomites.First(p => p.TikNum == 1);
                    var tik2 = tezOneriBasvuruSavunma.ToBasvuruSavunmaKomites.First(p => p.TikNum == 2);
                    var parameters = new List<MailParameterDto>
                    {
                        new MailParameterDto { Key = "AnabilimDaliAdi", Value = anabilimDaliAdi.IlkHarfiBuyut() },
                        new MailParameterDto { Key = "ProgramAdi", Value = programAdi.IlkHarfiBuyut() },
                        new MailParameterDto { Key = "OgrenciNo", Value = ogrenciNo },
                        new MailParameterDto { Key = "OgrenciAdSoyad", Value = ogrenciAdSoyad.IlkHarfiBuyut() },
                        new MailParameterDto { Key = "DanismanUnvan", Value = tezDanisman.UnvanAdi.IlkHarfiBuyut() },
                        new MailParameterDto { Key = "DanismanAdSoyad", Value = tezDanisman.AdSoyad.IlkHarfiBuyut() },
                        new MailParameterDto { Key = "TezBaslik", Value = tezBaslik },
                        new MailParameterDto { Key = "TezOneriTarihi", Value = sinavTarihi.ToFormatDate() },
                        new MailParameterDto { Key = "SeciliTikUyesiUnvan", Value = sablon.Value.UnvanAdi.IlkHarfiBuyut()},
                        new MailParameterDto { Key = "SeciliTikUyesiAdSoyad", Value =  sablon.Value.AdSoyad.IlkHarfiBuyut()},
                        new MailParameterDto { Key = "SeciliTikUyesiUniversite", Value =  sablon.Value.UniversiteAdi.IlkHarfiBuyut()},
                        new MailParameterDto { Key = "TikUyesi1Unvan", Value = tik1.UnvanAdi.IlkHarfiBuyut()},
                        new MailParameterDto { Key = "TikUyesi1AdSoyad", Value = tik1.AdSoyad.IlkHarfiBuyut()},
                        new MailParameterDto { Key = "TikUyesi1Universite", Value = tik1.UniversiteAdi.IlkHarfiBuyut()},
                        new MailParameterDto { Key = "TikUyesi2Unvan", Value =tik2.UnvanAdi.IlkHarfiBuyut()},
                        new MailParameterDto { Key = "TikUyesi2AdSoyad", Value = tik2.AdSoyad.IlkHarfiBuyut()},
                        new MailParameterDto { Key = "TikUyesi2Universite", Value = tik2.UniversiteAdi.IlkHarfiBuyut()},
                    };

                    var html = ValueReplaceExtension.ProcessHtmlContent(sablon.Key.SablonHtml, parameters);
                    var htmlFooter = ValueReplaceExtension.ProcessHtmlContent(sablon.Key.SablonFooterHtml, parameters);
                    if (sablonInx == 0)
                    {
                        rprX = new RprYaziSablonOlusturucu(enstitu, html, htmlFooter, sablon.Key.Konu);
                        rprX.CreateDocument();
                    }
                    else
                    {
                        var rapor = new RprYaziSablonOlusturucu(enstitu, html, htmlFooter, sablon.Key.Konu);
                        rapor.CreateDocument();
                        rprX.Pages.AddRange(rapor.Pages);
                    }


                    sablonInx++;
                }
                return rprX;

            }
        }


    }
}