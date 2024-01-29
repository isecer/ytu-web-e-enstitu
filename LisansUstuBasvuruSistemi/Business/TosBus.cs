using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Helpers;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.MenuAndRoles;
using LisansUstuBasvuruSistemi.Utilities.SystemSetting;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.MailManager;


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

            using (var entities = new LisansustuBasvuruSistemiEntities())
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
                        if (!yeterlikSozluSinavTarihi.HasValue) return true;
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

        public static int TosSavunmaNo(Guid toUniqueId, Guid? tosUniqueId, DateTime kontrolTarih)
        {
            using (var entities = new LisansustuBasvuruSistemiEntities())
            {
                var toBasvuru = entities.ToBasvurus.First(p => p.UniqueID == toUniqueId);

                var tosbasvurus = toBasvuru.ToBasvuruSavunmas.Where(p => p.UniqueID != tosUniqueId)
                    .Select(s => new { s.ToBasvuruSavunmaID, s.SavunmaNo, s.ToBasvuruSavunmaDurumID, savunmaSinaviVar = s.SRTalepleris.Any() }).ToList();
                var sonBasvuru = tosbasvurus.OrderByDescending(o => o.ToBasvuruSavunmaID).FirstOrDefault();
                if (!tosbasvurus.Any(a => a.savunmaSinaviVar))
                {
                    var tezOneriIlkSavunmaHakkiAyKriter =
                        TiAyar.TezOneriIlkSavunmaHakkiAyKriter.GetAyarTi(toBasvuru.EnstituKod).ToInt(0);
                    var tezOneriIkinciSavunmaHakkiAyKriter =
                        TiAyar.TezOneriIkinciSavunmaHakkiAyKriter.GetAyarTi(toBasvuru.EnstituKod).ToInt(0);
                    var tezOneriToplamSavunmaHakkiAyKriter =
                        tezOneriIlkSavunmaHakkiAyKriter + tezOneriIkinciSavunmaHakkiAyKriter;
                    var ilkOneriBitisTarihi = toBasvuru.IlkOneriBitisTarihi ?? toBasvuru.YeterlikSozluSinavTarihi.ToGetBitisTarihi(tezOneriIlkSavunmaHakkiAyKriter);
                    var ikinciOneriBitisTarihi = toBasvuru.IkinciOneriBitisTarihi ?? toBasvuru.YeterlikSozluSinavTarihi.ToGetBitisTarihi(tezOneriToplamSavunmaHakkiAyKriter);

                    if (toBasvuru.IsBasvuruKriterMuaf || kontrolTarih.Date <= ilkOneriBitisTarihi.Date) return 1;
                    if (kontrolTarih.Date <= ikinciOneriBitisTarihi) return 2;
                    return 3;
                }
                if (sonBasvuru?.ToBasvuruSavunmaDurumID == ToBasvuruSavunmaDurumuEnum.KabulEdildi) return 1;
                if (sonBasvuru?.ToBasvuruSavunmaDurumID == ToBasvuruSavunmaDurumuEnum.Duzeltme) return sonBasvuru.SavunmaNo;
                return (sonBasvuru?.SavunmaNo ?? 0) + 1;

            }
        }
        public static MmMessage TosKalanHakSavunmaBaslangicTarihKriter(Guid toUniqueId, DateTime? kontrolTarih = null)
        {
            var msg = new MmMessage();

            using (var entities = new LisansustuBasvuruSistemiEntities())
            {
                var toBasvuru = entities.ToBasvurus.First(p => p.UniqueID == toUniqueId);

                var toBasvuruSavunmas = toBasvuru.ToBasvuruSavunmas.Where(p => p.ToBasvuruSavunmaDurumID.HasValue).OrderByDescending(o => o.ToBasvuruSavunmaID).ToList();

                var tezOneriIlkSavunmaHakkiAyKriter =
                    TiAyar.TezOneriIlkSavunmaHakkiAyKriter.GetAyarTi(toBasvuru.EnstituKod).ToInt(0);
                var tezOneriIkinciSavunmaHakkiAyKriter =
                    TiAyar.TezOneriIkinciSavunmaHakkiAyKriter.GetAyarTi(toBasvuru.EnstituKod).ToInt(0);
                var tezOneriToplamSavunmaHakkiAyKriter =
                    tezOneriIlkSavunmaHakkiAyKriter + tezOneriIkinciSavunmaHakkiAyKriter;
                if (!toBasvuruSavunmas.Any())
                {
                    var ilkBitisTarihi = toBasvuru.IlkOneriBitisTarihi ?? toBasvuru.YeterlikSozluSinavTarihi.ToGetBitisTarihi(tezOneriIlkSavunmaHakkiAyKriter);
                    var ikincibitisTarihi = toBasvuru.IkinciOneriBitisTarihi ?? toBasvuru.YeterlikSozluSinavTarihi.ToGetBitisTarihi(tezOneriToplamSavunmaHakkiAyKriter);


                    var strDateYeterlikSinav = toBasvuru.YeterlikSozluSinavTarihi.ToFormatDate();
                    var strDateIlk = ilkBitisTarihi.ToFormatDate();
                    var strDateIkinci = ikincibitisTarihi.ToFormatDate();

                    var ekMsg = "";
                    kontrolTarih = DateTime.Now.Date;
                    if (toBasvuru.IsBasvuruKriterMuaf)
                    {
                        ekMsg = "<br/> Not: Öğrenci bu kriterlerden muaf tutulmaktadır.";
                    }
                    else if (kontrolTarih > ikincibitisTarihi)
                    {
                        ekMsg = "<br/>Süreniz dolduğundan başvuru yapamazsınız.";
                    }

                    msg.MessagesDialog.Add(new MrMessage
                    {
                        IsSucces = toBasvuru.IsBasvuruKriterMuaf || kontrolTarih <= ikincibitisTarihi,
                        Message = $"1. Savunma hakkı kullanımı için {strDateYeterlikSinav} / {strDateIlk} tarihleri arasında savunma sınavı yapılması gerekir.<br/>" +
                                  $"2. Savunma hakkı kullanımı için {strDateIlk} / {strDateIkinci} tarihleri arasında savunma sınavı yapılması gerekir." + ekMsg
                    });
                    toBasvuru.IlkOneriBitisTarihi = ilkBitisTarihi;
                    toBasvuru.IkinciOneriBitisTarihi = ikincibitisTarihi;
                    toBasvuru.RetDuzeltmeBitisTarihi = null;
                    msg.Table = toBasvuru;
                    return msg;
                }

                var sonBasvuru = toBasvuruSavunmas.First();
                var isYeniSinavAlindi = toBasvuru.ToBasvuruSavunmas.Any(a =>
                    a.ToBasvuruSavunmaID > sonBasvuru.ToBasvuruSavunmaID && a.SRTalepleris.Any());
                if (sonBasvuru.ToBasvuruSavunmaDurumID == ToBasvuruSavunmaDurumuEnum.Duzeltme)
                {
                    kontrolTarih = (kontrolTarih ?? sonBasvuru.SRTalepleris.First().Tarih).Date;
                    var tezOneriDuzeltmeSonrasiSavunmaHakkiAyKriter =
                        TiAyar.TezOneriDuzeltmeSonrasiSavunmaHakkiAyKriter.GetAyarTi(toBasvuru.EnstituKod).ToInt(0);
                    var duzeltmeAlinanSinav = sonBasvuru.SRTalepleris.First();

                    var duzeltmeBitisTarihi = (toBasvuru.RetDuzeltmeBitisTarihi ?? duzeltmeAlinanSinav.Tarih.ToGetBitisTarihi(tezOneriDuzeltmeSonrasiSavunmaHakkiAyKriter)).Date;
                    var strDate = duzeltmeBitisTarihi.ToFormatDate();
                    var ekMsg = "";
                    if (toBasvuru.IsBasvuruKriterMuaf)
                    {
                        ekMsg = "<br/> Not: Öğrenci bu kriterlerden muaf tutulmaktadır.";
                    }
                    msg.MessagesDialog.Add(new MrMessage
                    {
                        IsSucces = toBasvuru.IsBasvuruKriterMuaf || kontrolTarih <= duzeltmeBitisTarihi,
                        Message = isYeniSinavAlindi ?
                                $"{duzeltmeAlinanSinav.Tarih.Date.ToFormatDate()} tarihindeki savunmasından {tezOneriDuzeltmeSonrasiSavunmaHakkiAyKriter} aylık bir düzeltme hakkı alındı ve bu süre içinde yeni tez öneri savunma oluşturuldu." :
                                $"{duzeltmeAlinanSinav.Tarih.Date.ToFormatDate()} tarihindeki savunmasından {tezOneriDuzeltmeSonrasiSavunmaHakkiAyKriter} aylık bir düzeltme hakkı alınmıştır. Düzeltme işlemlerinin yapılıp {strDate} tarihine kadar yeni tez öneri savunma yapılması gerekmektedir." + ekMsg
                    });
                    toBasvuru.RetDuzeltmeBitisTarihi = duzeltmeBitisTarihi;
                    toBasvuru.IlkOneriBitisTarihi = null;
                    toBasvuru.IkinciOneriBitisTarihi = null;
                    msg.Table = toBasvuru;
                    //entities.SaveChanges();
                    return msg;
                }

                if (sonBasvuru.ToBasvuruSavunmaDurumID == ToBasvuruSavunmaDurumuEnum.RetEdildi)
                {
                    var sonBasvuruSinav = sonBasvuru.SRTalepleris.First();
                    kontrolTarih = (kontrolTarih ?? sonBasvuruSinav.Tarih).Date;

                    var maxBasarisizSavunmaNo = TiAyar.TezOneriToplamBasarisizTezOneriSavunmaHak.GetAyarTi(toBasvuru.EnstituKod).ToInt(0);
                    if (sonBasvuru.SavunmaNo >= maxBasarisizSavunmaNo && !toBasvuru.IsBasvuruKriterMuaf)
                    {
                        msg.MessagesDialog.Add(new MrMessage
                        {
                            Message = $"{sonBasvuruSinav.Tarih.Date.ToFormatDate()} tarihinde yapılan {sonBasvuru.SavunmaNo}. savunması başarısızlıkla sonuçlandığından yeni bir tez öneri savunma hakkı kalmamıştır."
                        });
                    }
                    else
                    {
                        var retBitisTarihi = (toBasvuru.RetDuzeltmeBitisTarihi ?? sonBasvuruSinav.Tarih.ToGetBitisTarihi(tezOneriIkinciSavunmaHakkiAyKriter)).Date;
                        var strDate = retBitisTarihi.ToFormatDate();
                        var ekMsg = "";
                        if (toBasvuru.IsBasvuruKriterMuaf)
                        {
                            ekMsg = "<br/> Not: Öğrenci bu kriterlerden muaf tutulmaktadır.";
                        }
                        msg.MessagesDialog.Add(new MrMessage
                        {
                            IsSucces = toBasvuru.IsBasvuruKriterMuaf || kontrolTarih <= retBitisTarihi,
                            Message = $"Son yapılan {sonBasvuruSinav.Tarih.Date.ToFormatDate()} tarihli tez öneri savunması başarısızlıkla sonuçlandığından {strDate} tarihine kadar {sonBasvuru.SavunmaNo + 1}. savunma hakkı tanınmıştır. Bu süre içerisinde yeni bir sınav talebi " + (isYeniSinavAlindi ? "oluşturuldu." : "oluşturulmadı.") + ekMsg
                        });
                        toBasvuru.RetDuzeltmeBitisTarihi = retBitisTarihi;
                        toBasvuru.IlkOneriBitisTarihi = null;
                        toBasvuru.IkinciOneriBitisTarihi = null;
                        msg.Table = toBasvuru;
                        //entities.SaveChanges();
                    }
                    return msg;

                }
                if (sonBasvuru.ToBasvuruSavunmaDurumID == ToBasvuruSavunmaDurumuEnum.KabulEdildi)
                {
                    var sonBasvuruSinav = sonBasvuru.SRTalepleris.First();

                    if (!toBasvuru.ToBasvuruSavunmas.Any(a => a.ToBasvuruSavunmaID > sonBasvuru.ToBasvuruSavunmaID))
                        msg.MessagesDialog.Add(new MrMessage
                        {
                            IsSucces = true,
                            Message = $"{sonBasvuruSinav.Tarih.ToFormatDate()} tarihinde yapılan {sonBasvuru.SavunmaNo}. savunma  başarılı bir şekilde sonuçlandığı için istendiği zaman tekrar tez öneri savunma talebi yapılabilir."
                        });
                    toBasvuru.RetDuzeltmeBitisTarihi = null;
                    toBasvuru.IlkOneriBitisTarihi = null;
                    toBasvuru.IkinciOneriBitisTarihi = null;
                    entities.SaveChanges();
                    return msg;

                }
                return msg;
            }

        }

        public static MmMessage GetTosSilKontrol(Guid toBasvuruSavunmaUniqueId)
        {
            var msg = new MmMessage();

            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var tezOneriSavunma = db.ToBasvuruSavunmas.FirstOrDefault(f => f.UniqueID == toBasvuruSavunmaUniqueId);
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
                    else if (!isAdmin && !TiAyar.TezOneriSavunmaBasvuruAlimiAcik.GetAyarTi(tezOneriSavunma.ToBasvuru.EnstituKod, "false").ToBoolean(false) && UserIdentity.Current.IsAdmin == false)
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
            using (var db = new LisansustuBasvuruSistemiEntities())
            {

                var basvuru = db.ToBasvurus.First(p => p.UniqueID == toUniqueId);
                var enstitu = db.Enstitulers.First(p => p.EnstituKod == basvuru.EnstituKod);

                var eslesenDanisman = db.Kullanicilars.FirstOrDefault(p => p.KullaniciID == (basvuru.TezDanismanID ?? 0));
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
                model.KayitDonemi = basvuru.KayitOgretimYiliBaslangic + "/" + (basvuru.KayitOgretimYiliBaslangic + 1) + " " + db.Donemlers.First(p => p.DonemID == basvuru.KayitOgretimYiliDonemID.Value).DonemAdi;
                model.ResimAdi = basvuru.Kullanicilar.ResimAdi;
                model.Ad = basvuru.Kullanicilar.Ad;
                model.Soyad = basvuru.Kullanicilar.Soyad;
                model.TcKimlikNo = basvuru.Kullanicilar.TcKimlikNo;
                model.OgrenciNo = basvuru.OgrenciNo;
                model.OgrenimTipAdi = db.OgrenimTipleris.First(p => p.EnstituKod == basvuru.EnstituKod && p.OgrenimTipKod == basvuru.OgrenimTipKod).OgrenimTipAdi;

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

                model.ToBasvuruSavunmaList = basvuru.ToBasvuruSavunmas.Where(p => !tosKomiteUniqueId.HasValue || p.ToBasvuruSavunmaKomites.Any(a => a.UniqueID == tosKomiteUniqueId))
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

                        ToBasvuruSavunmaKomites = db.ToBasvuruSavunmaKomites.Where(p => p.ToBasvuruSavunmaID == s.ToBasvuruSavunmaID).Include("ToBasvuruSavunmaDurumlari").ToList(),
                        //ToBasvuruSavunmaKomites = s.ToBasvuruSavunmaKomites.AsQueryable().Include("ToBasvuruSavunmaDurumlari").ToList(),
                        SRModel = (from sR in s.SRTalepleris
                                   join tt in db.SRTalepTipleris on sR.SRTalepTipID equals tt.SRTalepTipID
                                   join hg in db.HaftaGunleris on sR.HaftaGunID equals hg.HaftaGunID
                                   join d in db.SRDurumlaris on sR.SRDurumID equals d.SRDurumID
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

                model.ToplamBasarisizTezOneriSavunmaHak = TiAyar.TezOneriToplamBasarisizTezOneriSavunmaHak.GetAyarTi(basvuru.EnstituKod).ToInt();
                model.IlkSavunmaHakkiAyKriter = TiAyar.TezOneriIlkSavunmaHakkiAyKriter.GetAyarTi(basvuru.EnstituKod).ToInt();
                model.IkinciSavunmaHakkiAyKriter = TiAyar.TezOneriIkinciSavunmaHakkiAyKriter.GetAyarTi(basvuru.EnstituKod).ToInt();





                var sonTos = model.ToBasvuruSavunmaList.FirstOrDefault();




                model.DurumHtmlString = (
                                            sonTos != null ? sonTos.DurumModel : new TosDurumDto()
                                        ).TosBasvuruDurumView().ToString();
                model.DonemHtmlString = (sonTos ?? new ToBasvuruSavunmaDto()).TosBasvuruDonemView().ToString();


                var basvuruKiterKontrol = TosKalanHakSavunmaBaslangicTarihKriter(basvuru.UniqueID);


                if (basvuruKiterKontrol.MessagesDialog.Any())
                {
                    model.SavunmaBasvurusuYapmaSureBilgisiInfo = string.Join("<br/>", basvuruKiterKontrol.MessagesDialog.Select(s => s.Message));
                }
                model.IlkOneriBitisTarihi = (basvuruKiterKontrol.Table as ToBasvuru)?.IlkOneriBitisTarihi;
                model.IkinciOneriBitisTarihi = (basvuruKiterKontrol.Table as ToBasvuru)?.IkinciOneriBitisTarihi;
                model.RetDuzeltmeBitisTarihi = (basvuruKiterKontrol.Table as ToBasvuru)?.RetDuzeltmeBitisTarihi;
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

            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var donems = db.ToBasvuruSavunmas.Select(s => new { s.DonemBaslangicYil, s.DonemID, s.Donemler.DonemAdi })
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
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var donems = db.ToBasvuruSavunmas.Select(s => new { s.DonemBaslangicYil, s.DonemID, s.Donemler.DonemAdi })
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
            dct.Add(new CmbIntDto { Value = 3, Caption = "Uzatma Alanlar" });
            return dct;
        }

        public static List<CmbStringDto> CmbDonemListe(bool bosSecimVar = false)
        {

            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var donems = db.ToBasvuruSavunmas.Select(s => new { s.DonemBaslangicYil, s.DonemID, s.Donemler.DonemAdi })
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
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var anabilimDaliIds = db.ToBasvurus
                    .Where(p => p.EnstituKod == enstituKod).Select(s => s.Programlar.AnabilimDaliID).Distinct().ToList();

                var anabilimDallaris = db.AnabilimDallaris.Where(p => anabilimDaliIds.Contains(p.AnabilimDaliID))
                    .Select(s => new { s.AnabilimDaliID, s.AnabilimDaliAdi }).OrderBy(o => o.AnabilimDaliAdi).Select(
                        s =>
                            new CmbIntDto { Value = s.AnabilimDaliID, Caption = s.AnabilimDaliAdi }
                        ).ToList();
                if (bosSecimVar) anabilimDallaris.Insert(0, new CmbIntDto { Value = null, Caption = "" });

                return anabilimDallaris;
            }
        }
    }
}