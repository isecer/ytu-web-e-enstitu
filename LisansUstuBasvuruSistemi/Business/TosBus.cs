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
using System.IO;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.Logs;
 

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
        public static bool BasvuruOlustur(int kullaniciId)
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
                        var ogrenciYeterlikBilgi =
                            obsOgrenci.OgrenciYeters.FirstOrDefault(p => p.DR_YET_GNL_SNV_DURUM == "Başarılı" && p.DR_YET_SOZ_SNV_DURUM == "Başarılı");


                        if (ogrenciYeterlikBilgi != null && ogrenciYeterlikBilgi.DR_YET_SOZ_SNV_TARIH.ToDate().HasValue)
                        {
                            var yeterlikBasariTarihi = ogrenciYeterlikBilgi.DR_YET_SOZ_SNV_TARIH.ToDate().Value;

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
                                basvuru.YeterlikSozluSinavTarihi = yeterlikBasariTarihi;
                                basvuru.IslemTarihi = DateTime.Now;
                                basvuru.IslemYapanID = UserIdentity.Current.Id;
                                basvuru.IslemYapanIP = UserIdentity.Ip;
                            }
                            else
                            {
                                basvuru = entities.ToBasvurus.Add(new ToBasvuru
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
                                    YeterlikSozluSinavTarihi = yeterlikBasariTarihi,
                                    IslemTarihi = DateTime.Now,
                                    IslemYapanID = UserIdentity.Current.Id,
                                    IslemYapanIP = UserIdentity.Ip
                                });
                            }
                            entities.SaveChanges();


                        }
                        else
                        {
                            MessageBox.Show("Uyarı", MessageBox.MessageType.Information, "Başvuru işleminin yapılabilmesi için OBS sisteminde yeterlik sözlü sınavından başarılı olunması gerekmetkedir.");
                        }

                    }
                    else
                    {
                        SistemBilgilendirmeBus.SistemBilgisiKaydet("OBS sisteminden öğrenci bilgisi kontrolü yapılamadı. Hata:" + obsOgrenci.HataMsj, "TezOneriSavunmaBus/TezIzlemeJuriOneriSenkronizasyonMsg", LogTipiEnum.Kritik);
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
                    //entities.SaveChanges();
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
                    else if (!isAdmin && !TiAyar.TezOneriSavunmaBasvuruAlimiAcik.GetAyarTi(tezOneriSavunma.ToBasvuru.EnstituKod, "false").ToBoolean().Value && UserIdentity.Current.IsAdmin == false)
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
                var basvuru2 = db.ToBasvurus.First(p => p.UniqueID == toUniqueId);
                model.IlkOneriBitisTarihi = (basvuruKiterKontrol.Table as ToBasvuru)?.IlkOneriBitisTarihi;
                model.IkinciOneriBitisTarihi = (basvuruKiterKontrol.Table as ToBasvuru)?.IkinciOneriBitisTarihi;
                model.RetDuzeltmeBitisTarihi = (basvuruKiterKontrol.Table as ToBasvuru)?.RetDuzeltmeBitisTarihi;
            }
            return model;

        }


        public static MmMessage SendMailTosBilgisi(int? toBasvuruSavunmaId, int? srTalepId)
        {
            var mmMessage = new MmMessage();
            try
            {
                using (var db = new LisansustuBasvuruSistemiEntities())
                {

                    var toBasvuruSavunma = new ToBasvuruSavunma();
                    var srTalebi = new SRTalepleri();

                    if (toBasvuruSavunmaId.HasValue)
                    {
                        toBasvuruSavunma = db.ToBasvuruSavunmas.First(p => p.ToBasvuruSavunmaID == toBasvuruSavunmaId);
                        if (srTalepId.HasValue) srTalebi = toBasvuruSavunma.SRTalepleris.FirstOrDefault();
                    }
                    else if (srTalepId.HasValue)
                    {
                        srTalebi = db.SRTalepleris.First(p => p.SRTalepID == srTalepId);
                        toBasvuruSavunma = srTalebi.ToBasvuruSavunma;
                    }

                    var juriler = toBasvuruSavunma.ToBasvuruSavunmaKomites.ToList();
                    var sablonTipIDs = new List<int>();
                    var mModel = new List<SablonMailModel>();
                    var danisman = juriler.First(p => p.IsTezDanismani);
                    var isSavunmaOrToplanti = false;
                    var gonderilenMEkleris = new List<GonderilenMailEkleri>
                    {
                        new GonderilenMailEkleri
                        {
                            EkAdi = toBasvuruSavunma.CalismaRaporDosyaAdi,
                            EkDosyaYolu = toBasvuruSavunma.CalismaRaporDosyaYolu,
                        }
                    };
                    var kul = toBasvuruSavunma.ToBasvuru.Kullanicilar;
                    if (toBasvuruSavunmaId.HasValue)
                    {
                        isSavunmaOrToplanti = true;
                        sablonTipIDs.AddRange(new List<int> { MailSablonTipiEnum.TosBaslatildiOgrenci, MailSablonTipiEnum.TosBaslatildiDanisman });
                        mModel.Add(new SablonMailModel
                        {

                            JuriTipAdi = "TezDanismani",
                            UnvanAdi = danisman.UnvanAdi,
                            AdSoyad = danisman.AdSoyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = danisman.EMail, ToOrBcc = true } },
                            MailSablonTipID = MailSablonTipiEnum.TosBaslatildiDanisman
                        });
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Öğrenci " + kul.Ad + " " + kul.Soyad,
                            AdSoyad = kul.Ad + " " + kul.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = toBasvuruSavunma.ToBasvuru.Kullanicilar.EMail, ToOrBcc = true } },
                            MailSablonTipID = MailSablonTipiEnum.TosBaslatildiOgrenci
                        });
                    }
                    if (srTalepId.HasValue)
                    {
                        sablonTipIDs.AddRange(new List<int> { MailSablonTipiEnum.TosToplantiBilgiKomite, MailSablonTipiEnum.TosToplantiBilgiOgrenci });
                        mModel.Add(new SablonMailModel
                        {

                            JuriTipAdi = "Öğrenci " + kul.Ad + " " + kul.Soyad,
                            AdSoyad = kul.Ad + " " + kul.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = toBasvuruSavunma.ToBasvuru.Kullanicilar.EMail, ToOrBcc = true } },
                            MailSablonTipID = MailSablonTipiEnum.TosToplantiBilgiOgrenci
                        });
                        mModel.AddRange(juriler.Select(item => new SablonMailModel
                        {
                            UnvanAdi = item.UnvanAdi,
                            AdSoyad = item.AdSoyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = item.EMail, ToOrBcc = true } },
                            MailSablonTipID = MailSablonTipiEnum.TosToplantiBilgiKomite,
                            JuriTipAdi = item.IsTezDanismani ? "Tez Danışmanı" : "Komite Üyesi"
                        }));
                    }

                    var enstitu = toBasvuruSavunma.ToBasvuru.Enstituler;

                    var sablonlar = db.MailSablonlaris.Where(p => p.IsAktif && sablonTipIDs.Contains(p.MailSablonTipID) && p.EnstituKod == enstitu.EnstituKod).ToList();


                    var abdL = toBasvuruSavunma.ToBasvuru.Programlar.AnabilimDallari;
                    var prgL = toBasvuruSavunma.ToBasvuru.Programlar;
                    var oncekiMailTarihi = isSavunmaOrToplanti ? toBasvuruSavunma.ToSavunmaBaslatildiMailGonderimTarihi : toBasvuruSavunma.ToplantiBilgiGonderimTarihi;

                    var isSended = false;
                    foreach (var item in mModel)
                    {
                        item.Sablon = sablonlar.FirstOrDefault(p => p.MailSablonTipID == item.MailSablonTipID);
                        if (item.Sablon == null) continue;
                        item.SablonParametreleri = item.Sablon.MailSablonTipleri.Parametreler.Split(',').ToList().Select(s => s.Trim()).ToList();

                        gonderilenMEkleris.AddRange(item.Sablon.MailSablonlariEkleris.Select(itemSe => new GonderilenMailEkleri { EkAdi = itemSe.EkAdi, EkDosyaYolu = itemSe.EkDosyaYolu, }));
                        foreach (var itemEk in gonderilenMEkleris)
                        {
                            var ekTamYol = System.Web.HttpContext.Current.Server.MapPath("~" + itemEk.EkDosyaYolu);
                            if (System.IO.File.Exists(ekTamYol))
                            {
                                var fExtension = Path.GetExtension(ekTamYol);
                                item.Attachments.Add(new System.Net.Mail.Attachment(new MemoryStream(System.IO.File.ReadAllBytes(ekTamYol)),
                                    itemEk.EkAdi.ToSetNameFileExtension(fExtension), System.Net.Mime.MediaTypeNames.Application.Octet));

                            }
                            else SistemBilgilendirmeBus.SistemBilgisiKaydet("Mail gönderilirken eklenen dosya eki sistemde bulunamadı!<br/>Dosya Adı:" + itemEk.EkAdi + " <br/>Dosya Yolu:" + ekTamYol, "Management/sendMailTIToplantiBilgisi", LogTipiEnum.Uyarı);
                        }
                        if (item.Sablon.GonderilecekEkEpostalar != null) item.EMails.AddRange(item.Sablon.GonderilecekEkEpostalar.Split(',').Select(s => new MailSendList { EMail = s.Trim(), ToOrBcc = false }));
                        var mailParameterDtos = new List<MailParameterDto>();


                        if (item.SablonParametreleri.Any(a => a == "@EnstituAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "EnstituAdi", Value = enstitu.EnstituAd });
                        if (item.SablonParametreleri.Any(a => a == "@WebAdresi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "WebAdresi", Value = enstitu.WebAdresi, IsLink = true });
                        if (item.SablonParametreleri.Any(a => a == "@AdSoyad"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "AdSoyad", Value = item.AdSoyad });
                        if (item.SablonParametreleri.Any(a => a == "@UnvanAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "UnvanAdi", Value = item.UnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciAdSoyad"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "OgrenciAdSoyad", Value = kul.Ad + " " + kul.Soyad });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciNo"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "OgrenciNo", Value = toBasvuruSavunma.ToBasvuru.OgrenciNo });
                        if (item.SablonParametreleri.Any(a => a == "@AnabilimdaliAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "AnabilimdaliAdi", Value = abdL.AnabilimDaliAdi });
                        if (item.SablonParametreleri.Any(a => a == "@ProgramAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "ProgramAdi", Value = prgL.ProgramAdi });
                        if (item.SablonParametreleri.Any(a => a == "@TezBaslikTr"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "TezBaslikTr", Value = toBasvuruSavunma.YeniTezBaslikTr });
                        if (item.SablonParametreleri.Any(a => a == "@TezBaslikEn"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "TezBaslikEn", Value = toBasvuruSavunma.YeniTezBaslikEn });
                        if (item.SablonParametreleri.Any(a => a == "@YokDrBursiyeriBilgi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "YokDrBursiyeriBilgi", Value = toBasvuruSavunma.IsYokDrBursiyeriVar ? "Var (Öncelikli Alan: " + toBasvuruSavunma.YokDrOncelikliAlan + ")" : "Yok" });
                        if (item.SablonParametreleri.Any(a => a == "@DonemAdi"))
                        {
                            var donemBilgi = toBasvuruSavunma.SavunmaBasvuruTarihi.ToAraRaporDonemBilgi();
                            mailParameterDtos.Add(new MailParameterDto { Key = "DonemAdi", Value = donemBilgi.DonemAdiLong });
                        }
                        if (item.SablonParametreleri.Any(a => a == "@OncekiMailTarihi"))
                        {
                            mailParameterDtos.Add(new MailParameterDto { Key = "OncekiMailTarihi", Value = oncekiMailTarihi?.ToFormatDateAndTime() });
                        }
                        #region SR Talebi
                        if (item.MailSablonTipID == MailSablonTipiEnum.TosToplantiBilgiKomite || item.MailSablonTipID == MailSablonTipiEnum.TosToplantiBilgiOgrenci)
                        {
                            if (item.SablonParametreleri.Any(a => a == "@ToplantiTarihi"))
                                mailParameterDtos.Add(new MailParameterDto { Key = "ToplantiTarihi", Value = srTalebi.Tarih.ToLongDateString() });
                            if (item.SablonParametreleri.Any(a => a == "@ToplantiSaati"))
                                mailParameterDtos.Add(new MailParameterDto
                                {
                                    Key = "ToplantiSaati",
                                    Value = $"{srTalebi.BasSaat:hh\\:mm}"
                                });

                            if (!srTalebi.IsOnline)
                            {
                                if (item.SablonParametreleri.Any(a => a == "@ToplantiSekli"))
                                {
                                    mailParameterDtos.Add(new MailParameterDto { Key = "ToplantiSekli", Value = "Yüz Yüze" });
                                }
                                if (item.SablonParametreleri.Any(a => a == "@ToplantiYeriBaslik"))
                                {
                                    mailParameterDtos.Add(new MailParameterDto { Key = "ToplantiYeriBaslik", Value = "Toplantı Salonu" });
                                }
                                if (item.SablonParametreleri.Any(a => a == "@ToplantiYeri"))
                                {
                                    mailParameterDtos.Add(new MailParameterDto { Key = "ToplantiYeri", Value = srTalebi.SalonAdi });
                                }
                            }
                            else
                            {
                                if (item.SablonParametreleri.Any(a => a == "@ToplantiSekli"))
                                {
                                    mailParameterDtos.Add(new MailParameterDto { Key = "ToplantiSekli", Value = "Çevrim İçi" });
                                }
                                if (item.SablonParametreleri.Any(a => a == "@ToplantiYeriBaslik"))
                                {
                                    mailParameterDtos.Add(new MailParameterDto { Key = "ToplantiYeriBaslik", Value = "Toplantı Katılım Linki" });
                                }
                                if (item.SablonParametreleri.Any(a => a == "@ToplantiYeri"))
                                {
                                    mailParameterDtos.Add(new MailParameterDto { Key = "ToplantiYeri", Value = srTalebi.SalonAdi, IsLink = true });
                                }
                            }
                        }
                        #endregion
                        #region DanismanKomite
                        if (item.SablonParametreleri.Any(a => a == "@DanismanBilgi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "DanismanBilgi", Value = danisman.UnvanAdi + " " + danisman.AdSoyad });
                        if (item.SablonParametreleri.Any(a => a == "@DanismanUni"))
                        {
                            mailParameterDtos.Add(new MailParameterDto { Key = "DanismanUni", Value = danisman.UniversiteAdi });
                        }
                        foreach (var itemTik in juriler.Where(p => !p.IsTezDanismani).Select((s, inx) => new { s, inx = inx + 1 }))
                        {

                            if (item.SablonParametreleri.Any(a => a == "@TikBilgi" + itemTik.inx))
                                mailParameterDtos.Add(new MailParameterDto { Key = "TikBilgi" + itemTik.inx, Value = itemTik.s.UnvanAdi + " " + itemTik.s.AdSoyad });
                            if (item.SablonParametreleri.Any(a => a == "@TikBilgiUni" + itemTik.inx))
                            {
                                mailParameterDtos.Add(new MailParameterDto { Key = "TikBilgiUni" + itemTik.inx, Value = itemTik.s.UniversiteAdi });
                            }
                        }
                        #endregion

                        var mCOntent = SystemMails.GetSystemMailContent(enstitu.EnstituAd, item.Sablon.SablonHtml, item.Sablon.SablonAdi, mailParameterDtos);
                        var snded = MailManager.SendMail(enstitu.EnstituKod, mCOntent.Title, mCOntent.HtmlContent, item.EMails, item.Attachments);
                        if (!snded) continue;
                        isSended = true;
                        var kModel = new GonderilenMailler
                        {
                            Tarih = DateTime.Now,
                            EnstituKod = enstitu.EnstituKod,
                            MesajID = null,
                            IslemTarihi = DateTime.Now,
                            Konu = mCOntent.Title
                        };
                        if (!item.AdSoyad.IsNullOrWhiteSpace()) kModel.Konu += " (" + item.AdSoyad + ")";
                        if (!item.JuriTipAdi.IsNullOrWhiteSpace()) kModel.Konu += " (" + item.JuriTipAdi + ")";
                        kModel.IslemYapanID = UserIdentity.Current == null || !UserIdentity.Current.IsAuthenticated ? 1 : UserIdentity.Current.Id;
                        kModel.IslemYapanIP = UserIdentity.Ip;
                        kModel.Aciklama = item.Sablon.Sablon ?? "";
                        kModel.AciklamaHtml = mCOntent.HtmlContent ?? "";
                        kModel.Gonderildi = true;
                        foreach (var itemGk in item.EMails)
                        {
                            kModel.GonderilenMailKullanicilars.Add(new GonderilenMailKullanicilar { Email = itemGk.EMail });
                        }
                        foreach (var itemMe in gonderilenMEkleris)
                        {
                            kModel.GonderilenMailEkleris.Add(itemMe);
                        }
                        db.GonderilenMaillers.Add(kModel);
                        if (isSavunmaOrToplanti) toBasvuruSavunma.ToSavunmaBaslatildiMailGonderimTarihi = DateTime.Now;
                        else toBasvuruSavunma.ToplantiBilgiGonderimTarihi = DateTime.Now;

                        LogIslemleri.LogEkle("ToBasvuruSavunma", LogCrudType.Update, toBasvuruSavunma.ToJson());
                    }
                    if (isSended) db.SaveChanges();
                    mmMessage.IsSuccess = true;
                    mmMessage.MessageType = MsgTypeEnum.Success;

                }
            }
            catch (Exception ex)
            {
                var message = "Tez Öneri Savunma toplantısı için Komite üyelerine mail gönderilirken bir hata oluştu! \r\nSRTalepID:" + srTalepId;
                SistemBilgilendirmeBus.SistemBilgisiKaydet(message + "\r\n Hata:" + ex.ToExceptionMessage(), "TezOneriSavunmaBus/SendMailTosBilgisi \r\n" + ex.ToExceptionStackTrace(), LogTipiEnum.Hata);
                mmMessage.Messages.Add(message + "</br> Hata:" + ex.ToExceptionMessage());
                mmMessage.MessageType = MsgTypeEnum.Error;
                mmMessage.IsSuccess = false;
            }
            return mmMessage;
        }
        public static MmMessage SendMailTosDegerlendirmeLink(Guid toBasvuruSavunmaId, Guid? tosKomiteUniqueId, bool isLinkOrSonuc)
        {
            var mmMessage = new MmMessage();
            try
            {
                using (var db = new LisansustuBasvuruSistemiEntities())
                {
                    var toBasvuruSavunma = db.ToBasvuruSavunmas.First(p => p.UniqueID == toBasvuruSavunmaId);
                    var qJurilers = toBasvuruSavunma.ToBasvuruSavunmaKomites.AsQueryable();
                    if (isLinkOrSonuc)
                    {
                        if (tosKomiteUniqueId.HasValue) qJurilers = qJurilers.Where(p => p.UniqueID == (tosKomiteUniqueId ?? p.UniqueID));
                        else qJurilers = qJurilers.Where(p => !p.IsTezDanismani);
                    }
                    else
                    {
                        if (tosKomiteUniqueId.HasValue) qJurilers = qJurilers.Where(p => p.UniqueID == tosKomiteUniqueId);
                        else qJurilers = qJurilers.Where(p => p.IsTezDanismani);
                    }
                    var juriler = qJurilers.ToList();

                    var mModel = new List<SablonMailModel>();

                    var enstitu = toBasvuruSavunma.ToBasvuru.Enstituler;

                    var abdL = toBasvuruSavunma.ToBasvuru.Programlar.AnabilimDallari;
                    var prgL = toBasvuruSavunma.ToBasvuru.Programlar;
                    var kul = toBasvuruSavunma.ToBasvuru.Kullanicilar;
                    if (isLinkOrSonuc)
                    {
                        foreach (var item in juriler)
                        {
                            item.UniqueID = Guid.NewGuid();
                        }
                    }
                    else
                    {
                        mModel.Add(new SablonMailModel
                        {

                            JuriTipAdi = "Öğrenci " + kul.Ad + " " + kul.Soyad,
                            AdSoyad = kul.Ad + " " + kul.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = toBasvuruSavunma.ToBasvuru.Kullanicilar.EMail, ToOrBcc = true } },
                            MailSablonTipID = MailSablonTipiEnum.TosDegerlendirmeSonucGonderimOgrenci,
                        });
                    }

                    mModel.AddRange(juriler.Select(item => new SablonMailModel
                    {
                        UniqueID = item.UniqueID,
                        UnvanAdi = item.UnvanAdi,
                        AdSoyad = item.AdSoyad,
                        EMails = new List<MailSendList> { new MailSendList { EMail = item.EMail, ToOrBcc = true } },
                        MailSablonTipID = isLinkOrSonuc ? MailSablonTipiEnum.TosDegerlendirmeLinkGonderimKomite : MailSablonTipiEnum.TosDegerlendirmeSonucGonderimDanisman,
                        JuriTipAdi = item.IsTezDanismani ? "Tez Danışmanı" : "Komite Üyesi",
                    }));
                    var mailSablonTipIDs = mModel.Select(s => s.MailSablonTipID).Distinct().ToList();
                    var sablonlar = db.MailSablonlaris.Where(p => p.IsAktif && mailSablonTipIDs.Contains(p.MailSablonTipID) && p.EnstituKod == enstitu.EnstituKod).ToList();
                    foreach (var item in mModel)
                    {

                        var juri = juriler.FirstOrDefault(p => p.UniqueID == item.UniqueID);
                        item.Sablon = sablonlar.FirstOrDefault(p => p.MailSablonTipID == item.MailSablonTipID);
                        if (item.Sablon == null) continue;
                        item.SablonParametreleri = item.Sablon.MailSablonTipleri.Parametreler.Split(',').ToList().Select(s => s.Trim()).ToList();
                        var gonderilenMailEkleri = new List<GonderilenMailEkleri>();

                        foreach (var itemSe in item.Sablon.MailSablonlariEkleris)
                        {
                            var ekTamYol = System.Web.HttpContext.Current.Server.MapPath("~" + itemSe.EkDosyaYolu);
                            if (System.IO.File.Exists(ekTamYol))
                            {
                                var fExtension = Path.GetExtension(ekTamYol);
                                item.Attachments.Add(new System.Net.Mail.Attachment(new MemoryStream(System.IO.File.ReadAllBytes(ekTamYol)),
                                    itemSe.EkAdi.ToSetNameFileExtension(fExtension), System.Net.Mime.MediaTypeNames.Application.Octet));
                                gonderilenMailEkleri.Add(new GonderilenMailEkleri
                                {
                                    EkAdi = itemSe.EkAdi,
                                    EkDosyaYolu = itemSe.EkDosyaYolu,
                                });
                            }
                            else SistemBilgilendirmeBus.SistemBilgisiKaydet("Mail gönderilirken eklenen dosya eki sistemde bulunamadı!<br/>Dosya Adı:" + itemSe.EkAdi + " <br/>Dosya Yolu:" + ekTamYol, "Management/sendMailTIToplantiBilgisi", LogTipiEnum.Uyarı);
                        }
                        if (!isLinkOrSonuc)
                        {
                            var ds = new List<int?>() { toBasvuruSavunma.ToBasvuruSavunmaID };
                            if (item.MailSablonTipID == MailSablonTipiEnum.TosDegerlendirmeSonucGonderimDanisman) ds.Add(1);
                            var ekler = Management.ExportRaporPdf(RaporTipiEnum.TezOneriSavunmaFormu, ds);
                            gonderilenMailEkleri.AddRange(ekler.Select(s => new GonderilenMailEkleri { EkAdi = s.Name, EkDosyaYolu = "" }));
                            item.Attachments.AddRange(ekler);
                        }

                        if (item.Sablon.GonderilecekEkEpostalar != null) item.EMails.AddRange(item.Sablon.GonderilecekEkEpostalar.Split(',').Select(s => new MailSendList { EMail = s.Trim(), ToOrBcc = false }));
                        var mailParameterDtos = new List<MailParameterDto>();


                        if (item.SablonParametreleri.Any(a => a == "@EnstituAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "EnstituAdi", Value = enstitu.EnstituAd });
                        if (item.SablonParametreleri.Any(a => a == "@WebAdresi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "WebAdresi", Value = enstitu.WebAdresi, IsLink = true });
                        if (item.SablonParametreleri.Any(a => a == "@AdSoyad"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "AdSoyad", Value = item.AdSoyad });
                        if (item.SablonParametreleri.Any(a => a == "@UnvanAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "UnvanAdi", Value = item.UnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciAdSoyad"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "OgrenciAdSoyad", Value = kul.Ad + " " + kul.Soyad });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciNo"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "OgrenciNo", Value = toBasvuruSavunma.ToBasvuru.OgrenciNo });
                        if (item.SablonParametreleri.Any(a => a == "@AnabilimdaliAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "AnabilimdaliAdi", Value = abdL.AnabilimDaliAdi });
                        if (item.SablonParametreleri.Any(a => a == "@ProgramAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "ProgramAdi", Value = prgL.ProgramAdi });
                        if (item.SablonParametreleri.Any(a => a == "@TezBaslikTr"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "TezBaslikTr", Value = toBasvuruSavunma.YeniTezBaslikTr });
                        if (item.SablonParametreleri.Any(a => a == "@TezBaslikEn"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "TezBaslikEn", Value = toBasvuruSavunma.YeniTezBaslikEn });
                        if (item.SablonParametreleri.Any(a => a == "@YokDrBursiyeriBilgi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "YokDrBursiyeriBilgi", Value = toBasvuruSavunma.IsYokDrBursiyeriVar ? "Var (Öncelikli Alan: " + toBasvuruSavunma.YokDrOncelikliAlan + ")" : "Yok" });
                        if (item.SablonParametreleri.Any(a => a == "@DonemAdi"))
                        {
                            var donemBilgi = toBasvuruSavunma.SavunmaBasvuruTarihi.ToAraRaporDonemBilgi();
                            mailParameterDtos.Add(new MailParameterDto { Key = "DonemAdi", Value = donemBilgi.DonemAdiLong });
                        }
                        if (item.SablonParametreleri.Any(a => a == "@Link"))
                        {
                            mailParameterDtos.Add(new MailParameterDto { Key = "Link", Value = enstitu.SistemErisimAdresi + "/TosBasvuru/Index?IsDegerlendirme=" + item.UniqueID, IsLink = true });
                        }
                        if (item.SablonParametreleri.Any(a => a == "@OncekiMailTarihi"))
                        {
                            mailParameterDtos.Add(isLinkOrSonuc
                                ? new MailParameterDto
                                {
                                    Key = "OncekiMailTarihi",
                                    Value = juri.LinkGonderimTarihi?.ToFormatDateAndTime()
                                }
                                : new MailParameterDto
                                {
                                    Key = "OncekiMailTarihi",
                                    Value = toBasvuruSavunma.DegerlendirmeSonucMailTarihi?.ToFormatDateAndTime()
                                });
                        }
                        var mCOntent = SystemMails.GetSystemMailContent(enstitu.EnstituAd, item.Sablon.SablonHtml, item.Sablon.SablonAdi, mailParameterDtos);
                        var snded = MailManager.SendMail(enstitu.EnstituKod, mCOntent.Title, mCOntent.HtmlContent, item.EMails, item.Attachments);
                        if (!snded) continue;
                        var kModel = new GonderilenMailler
                        {
                            Tarih = DateTime.Now,
                            EnstituKod = enstitu.EnstituKod,
                            MesajID = null,
                            IslemTarihi = DateTime.Now,
                            Konu = mCOntent.Title
                        };
                        if (!item.AdSoyad.IsNullOrWhiteSpace()) kModel.Konu += " (" + item.AdSoyad + ")";
                        if (!item.JuriTipAdi.IsNullOrWhiteSpace()) kModel.Konu += " (" + item.JuriTipAdi + ")";
                        kModel.IslemYapanID = UserIdentity.Current == null || !UserIdentity.Current.IsAuthenticated ? 1 : UserIdentity.Current.Id;
                        kModel.IslemYapanIP = UserIdentity.Ip;
                        kModel.Aciklama = item.Sablon.Sablon ?? "";
                        kModel.AciklamaHtml = mCOntent.HtmlContent ?? "";
                        kModel.Gonderildi = true;
                        foreach (var itemGk in item.EMails)
                        {
                            kModel.GonderilenMailKullanicilars.Add(new GonderilenMailKullanicilar { Email = itemGk.EMail });
                        }
                        foreach (var itemMe in gonderilenMailEkleri)
                        {
                            kModel.GonderilenMailEkleris.Add(itemMe);
                        }
                        if (isLinkOrSonuc)
                        {
                            juri.DegerlendirmeIslemTarihi = null;
                            juri.DegerlendirmeIslemYapanIP = null;
                            juri.DegerlendirmeYapanID = null;
                            juri.ToBasvuruSavunmaDurumID = null;
                            juri.IsCalismaRaporuAltAlanUygun = null;
                            juri.Aciklama = null;
                            juri.IsLinkGonderildi = true;
                            juri.LinkGonderimTarihi = DateTime.Now;
                            juri.LinkGonderenID = UserIdentity.Current.Id;

                        }

                        db.GonderilenMaillers.Add(kModel);
                        db.SaveChanges();
                        if (isLinkOrSonuc) LogIslemleri.LogEkle("ToBasvuruSavunmaKomite", LogCrudType.Update, juri.ToJson());
                    }

                    mmMessage.IsSuccess = true;
                    mmMessage.MessageType = MsgTypeEnum.Success;

                }
            }
            catch (Exception ex)
            {
                var message = "";
                message = isLinkOrSonuc ? "Tez Öneri Savunması değerlendirmesi için Komite üyelerine değerlendirme davetiye linki mail olarak gönderilirken bir hata oluştu!" : "Tez Öneri Savunması değerlendirme sonucu Komite üyelerine mail olarak gönderilirken bir hata oluştu!";
                SistemBilgilendirmeBus.SistemBilgisiKaydet(message + "\r\n Hata:" + ex.ToExceptionMessage(), "TezOneriSavunmaBus/SendMailTosDegerlendirmeLink \r\n" + ex.ToExceptionStackTrace(), LogTipiEnum.Hata);
                //mmMessage.Title = "Hata";
                mmMessage.Messages.Add(message + "</br> Hata:" + ex.ToExceptionMessage());
                mmMessage.MessageType = MsgTypeEnum.Error;
                mmMessage.IsSuccess = false;
            }
            return mmMessage;
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