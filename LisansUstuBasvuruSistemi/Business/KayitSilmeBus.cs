using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BiskaUtil;
using Entities.Entities;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.Helpers;
using LisansUstuBasvuruSistemi.Utilities.Logs;
using LisansUstuBasvuruSistemi.Utilities.MailManager;
using LisansUstuBasvuruSistemi.Utilities.MenuAndRoles;
using LisansUstuBasvuruSistemi.Utilities.SystemSetting;

namespace LisansUstuBasvuruSistemi.Business
{
    public static class KayitSilmeBus
    {

        public static IHtmlString ToKsBasvuruDurumView(this FrKayitSilmeBasvuruDto model)
        {
            model = model ?? new FrKayitSilmeBasvuruDto();
            var pagerString = model.ToRenderPartialViewHtml("KsBasvuru", "BasvuruDurumView");
            return pagerString;
        }
        public static IHtmlString ToKsBasvuruDonemView(this FrKayitSilmeBasvuruDto model)
        {
            model = model ?? new FrKayitSilmeBasvuruDto();
            var pagerString = model.ToRenderPartialViewHtml("KsBasvuru", "BasvuruDonemView");
            return pagerString;
        }
        public static List<string> BasvuruKontrol(string enstituKod, Guid? kayitSilmeBasvuruUniqueId)
        {
            var errorMessage = new List<string>();
            using (var entities = new LubsDbEntities())
            {
                var isGuncelleme = kayitSilmeBasvuruUniqueId.HasValue && kayitSilmeBasvuruUniqueId.Value != Guid.Empty;
                var kayitSilmeBasvuru = isGuncelleme
                    ? entities.KayitSilmeBasvurus.FirstOrDefault(p => p.UniqueID == kayitSilmeBasvuruUniqueId)
                    : null;

                var basvuranKullaniciId = kayitSilmeBasvuru?.KullaniciID ?? UserIdentity.Current.Id;
                var basvuranKullanici = entities.Kullanicilars.First(f => f.KullaniciID == basvuranKullaniciId);

                var basvuruDuzeltmeyetkisi = RoleNames.KayitSilmeBasvuruDuzeltmeYetkisi.InRoleCurrent();
                var enstituBasvuruOnayYetkisi = RoleNames.KayitSilmeGelenBasvurular.InRoleCurrent();
                var isBasvuruAlimiAcik = KayitSilmeAyar.KayitSilmeBasvuruAlimiAcik.GetAyar(enstituKod).ToBoolean(false);

                if (!enstituBasvuruOnayYetkisi && !isBasvuruAlimiAcik)
                {
                    errorMessage.Add("Kayıt silme başvuru işlemleri şu an kapalıdır.");
                    return errorMessage;
                }

                if (isGuncelleme && kayitSilmeBasvuru != null)
                {
                    if (!basvuruDuzeltmeyetkisi && kayitSilmeBasvuru.KullaniciID != UserIdentity.Current.Id)
                    {
                        errorMessage.Add("Kayıt silme başvurusu üzerinde düzenleme yapma yetkiniz bulunmamaktadır.");
                        return errorMessage;
                    }
                }

                var kontrolEdilecekEnstituKod = kayitSilmeBasvuru?.EnstituKod ?? basvuranKullanici.EnstituKod;
                if (!UserIdentity.Current.EnstituKods.Contains(kontrolEdilecekEnstituKod))
                {
                    errorMessage.Add("Bu enstitü için işlem yetkiniz bulunmamaktadır.");
                    return errorMessage;
                }

                if (enstituKod != basvuranKullanici.EnstituKod)
                {
                    var enstitu = entities.Enstitulers.First(p => p.EnstituKod == basvuranKullanici.EnstituKod);
                    errorMessage.Add("Başvuru yapılan enstitü ile kayıtlı olunan enstitü uyuşmamaktadır. Enstitünüz: " + enstitu.EnstituAd);
                    return errorMessage;
                }
                if (!isGuncelleme)
                {

                    if (!basvuranKullanici.YtuOgrencisi)
                    {
                        errorMessage.Add("Başvuru yapabilmeniz için YTU öğrencisi olduğunuzu belirtmeniz gerekmetedir. Profil bilgilerinizden YTU öğrencisi bilgilerinizi doldurunuz.");
                    }

                    var zatenBasvuruVar = entities.KayitSilmeBasvurus.Any(a =>
                        a.KullaniciID == basvuranKullaniciId &&
                        a.OgrenciNo == basvuranKullanici.OgrenciNo &&
                        a.ProgramKod == basvuranKullanici.ProgramKod && a.IsHarcBirimiOnayladi != false && a.IsKutuphaneBirimiOnayladi != false && a.EYKYaGonderildi != false && a.EYKYaHazirlandi != false && a.EYKDaOnaylandi != false);

                    if (zatenBasvuruVar)
                    {
                        errorMessage.Add("Okuduğunuz programa ait daha önce yapılmış bir kayıt silme başvurusu bulunmaktadır.");
                    }
                }
            }

            return errorMessage;
        }


        public static MmMessage KayitSilmeBasvuruSilKontrol(Guid? uniqueId)
        {
            var msg = new MmMessage
            {
                IsSuccess = true
            };

            using (var entities = new LubsDbEntities())
            {
                var kayitYetki = RoleNames.KayitSilmeBasvuruDuzeltmeYetkisi.InRoleCurrent();
                var basvuru =
                    entities.KayitSilmeBasvurus.FirstOrDefault(p => p.UniqueID == uniqueId);
                if (basvuru == null)
                {
                    msg.IsSuccess = false;
                    msg.Messages.Add("Aranan başvuru sistemde bulunamadı.");
                }
                else
                {
                    if (!UserIdentity.Current.EnstituKods.Contains(basvuru.EnstituKod) && kayitYetki &&
                        basvuru.KullaniciID != UserIdentity.Current.Id)
                    {
                        msg.IsSuccess = false;
                        msg.Messages.Add("Bu enstitüye ait başvuruyu silmeye yetkili değilsiniz!");
                    }
                    else if (!KayitSilmeAyar.KayitSilmeBasvuruAlimiAcik.GetAyar(basvuru.EnstituKod).ToBoolean(false) && UserIdentity.Current.IsAdmin == false)
                    {
                        msg.IsSuccess = false;
                        msg.Messages.Add("Kayıt Silme başvuru süreci kapalı olduğundan başvuru üzerinden herhangi bir işlem yapılamaz!");

                    }
                    else if (kayitYetki == false && basvuru.KullaniciID != UserIdentity.Current.Id)
                    {
                        msg.IsSuccess = false;
                        msg.Messages.Add("Başka bir kullanıcıya ait başvuruyu silmeye hakkınız yoktur!");
                    }
                    else if (basvuru.IsHarcBirimiOnayladi == true)
                    {
                        msg.IsSuccess = false;
                        msg.Messages.Add("Harç birim tarafından onaylanan başvurular silinemez!");
                    }
                }
            }

            return msg;
        }


        public static KmKayitSilmeBasvuruDto GetKayitSilmeBasvuru(Guid? id, string enstituKod)
        {
            var model = new KmKayitSilmeBasvuruDto();
            var kayitYetki = RoleNames.KayitSilmeBasvuruDuzeltmeYetkisi.InRoleCurrent();
            using (var entities = new LubsDbEntities())
            {
                if (id.HasValue)
                {
                    var kayitSilmeBasvuru = entities.KayitSilmeBasvurus.First(p => p.UniqueID == id && p.KullaniciID == (kayitYetki ? p.KullaniciID : UserIdentity.Current.Id));
                    var ogrenimTip = entities.OgrenimTipleris.First(p => p.EnstituKod == kayitSilmeBasvuru.EnstituKod && p.OgrenimTipKod == kayitSilmeBasvuru.OgrenimTipKod);
                    model.UniqueID = kayitSilmeBasvuru.UniqueID;
                    model.BasvuruTarihi = kayitSilmeBasvuru.BasvuruTarihi;
                    model.KullaniciID = kayitSilmeBasvuru.KullaniciID;
                    model.AdSoyad = kayitSilmeBasvuru.Kullanicilar.Ad + " " + kayitSilmeBasvuru.Kullanicilar.Soyad;
                    model.OgrenciNo = kayitSilmeBasvuru.OgrenciNo;

                    model.OgrenimTipAdi = ogrenimTip.OgrenimTipAdi;
                    model.AnabilimdaliAdi = kayitSilmeBasvuru.Programlar.AnabilimDallari.AnabilimDaliAdi;
                    model.ProgramAdi = kayitSilmeBasvuru.Programlar.ProgramAdi;
                    model.NufusKayitOrnekDosyaAdi = kayitSilmeBasvuru.NufusKayitOrnekDosyaAdi;
                    model.NufusKayitOrnekDosyaYolu = kayitSilmeBasvuru.NufusKayitOrnekDosyaYolu;
                    model.IsTaahhutOnay = kayitSilmeBasvuru.IsTaahhutOnay;
                }
                else
                {

                    var kul = entities.Kullanicilars.First(p => p.KullaniciID == UserIdentity.Current.Id);
                    var ogrenimTip = entities.OgrenimTipleris.First(p => p.EnstituKod == enstituKod && p.OgrenimTipKod == kul.OgrenimTipKod);


                    model.BasvuruTarihi = DateTime.Now;
                    model.KullaniciID = kul.KullaniciID;
                    model.AdSoyad = kul.Ad + " " + kul.Soyad;
                    model.OgrenciNo = kul.OgrenciNo;

                    model.OgrenimTipAdi = ogrenimTip.OgrenimTipAdi;
                    model.AnabilimdaliAdi = kul.Programlar.AnabilimDallari.AnabilimDaliAdi;
                    model.ProgramAdi = kul.Programlar.ProgramAdi;
                }

                return model;
            }
        }
        public static KsBasvuruDetayDto GetSecilenBasvuruDetay(Guid? uniqueId)
        {
            var model = new KsBasvuruDetayDto();
            using (var entities = new LubsDbEntities())
            {
                var basvuru = entities.KayitSilmeBasvurus.First(p => p.UniqueID == uniqueId);
                var enstitu = entities.Enstitulers.First(p => p.EnstituKod == basvuru.EnstituKod);

                model.UniqueID = basvuru.UniqueID;
                model.KayitSilmeBasvuruID = basvuru.KayitSilmeBasvuruID;
                model.KayitSilmeDurumID = basvuru.KayitSilmeDurumID;
                model.EnstituKod = basvuru.EnstituKod;
                model.EnstituAdi = enstitu.EnstituAd;
                model.KullaniciID = basvuru.KullaniciID;
                model.OgretimYiliBaslangic = basvuru.OgretimYiliBaslangic;
                model.DonemID = basvuru.DonemID;
                model.DonemAdi = basvuru.OgretimYiliBaslangic + "/" + (basvuru.OgretimYiliBaslangic + 1) + " " + basvuru.Donemler.DonemAdi;
                model.BasvuruTarihi = basvuru.BasvuruTarihi;
                model.ResimAdi = basvuru.Kullanicilar.ResimAdi;
                model.Ad = basvuru.Kullanicilar.Ad;
                model.Soyad = basvuru.Kullanicilar.Soyad;
                model.TcKimlikNo = basvuru.Kullanicilar.TcKimlikNo;
                model.OgrenciNo = basvuru.OgrenciNo;
                model.OgrenimTipKod = basvuru.OgrenimTipKod;
                var ogrenimTipi = entities.OgrenimTipleris.First(f =>
                    f.EnstituKod == enstitu.EnstituKod && f.OgrenimTipKod == basvuru.OgrenimTipKod);
                model.OgrenimTipAdi = ogrenimTipi.OgrenimTipAdi;
                model.AnabilimdaliAdi = basvuru.Programlar.AnabilimDallari.AnabilimDaliAdi;
                model.ProgramAdi = basvuru.Programlar.ProgramAdi;
                model.ProgramKod = basvuru.ProgramKod;
                model.KayitOgretimYiliBaslangic = basvuru.KayitOgretimYiliBaslangic;
                model.KayitOgretimYiliDonemID = basvuru.KayitOgretimYiliDonemID;
                model.KayitTarihi = basvuru.KayitTarihi;
                model.KayitDonemi = basvuru.KayitOgretimYiliBaslangic + "/" + (basvuru.KayitOgretimYiliBaslangic + 1) + " " + entities.Donemlers.First(p => p.DonemID == basvuru.KayitOgretimYiliDonemID.Value).DonemAdi;





                model.NufusKayitOrnekDosyaYolu = basvuru.NufusKayitOrnekDosyaYolu;
                model.NufusKayitOrnekDosyaAdi = basvuru.NufusKayitOrnekDosyaAdi;
                model.IsTaahhutOnay = basvuru.IsTaahhutOnay;

                model.IsHarcBirimiOnayladi = basvuru.IsHarcBirimiOnayladi;
                model.HarcBirimiOnayYapanID = basvuru.HarcBirimiOnayYapanID;
                if (basvuru.HarcBirimiOnayYapanID.HasValue)
                {
                    var harcBirimiOnayYapan = entities.Kullanicilars.FirstOrDefault(f => f.KullaniciID == basvuru.HarcBirimiOnayYapanID);
                    model.HarcBirimiOnayYapanKullanici = $"{harcBirimiOnayYapan.Ad} {harcBirimiOnayYapan.Soyad}";
                }

                model.HarcBirimiOnayIslemTarihi = basvuru.HarcBirimiOnayIslemTarihi;
                model.HarcBirimiOnayAciklamasi = basvuru.HarcBirimiOnayAciklamasi;

                model.IsKutuphaneBirimiOnayladi = basvuru.IsKutuphaneBirimiOnayladi;
                model.KutuphaneBirimiOnayYapanID = basvuru.KutuphaneBirimiOnayYapanID;
                if (basvuru.KutuphaneBirimiOnayYapanID.HasValue)
                {
                    var kutuphaneBirimiOnayYapan = entities.Kullanicilars.FirstOrDefault(f => f.KullaniciID == basvuru.KutuphaneBirimiOnayYapanID);
                    model.KutuphaneBirimiOnayYapanKullanici = $"{kutuphaneBirimiOnayYapan.Ad} {kutuphaneBirimiOnayYapan.Soyad}";
                }
                model.KutuphaneBirimiOnayIslemTarihi = basvuru.KutuphaneBirimiOnayIslemTarihi;
                model.KutuphaneBirimiOnayAciklamasi = basvuru.KutuphaneBirimiOnayAciklamasi;

                model.EYKYaGonderildi = basvuru.EYKYaGonderildi;
                model.EYKYaGonderildiIslemYapanID = basvuru.EYKYaGonderildiIslemYapanID;
                model.EYKYaGonderildiIslemTarihi = basvuru.EYKYaGonderildiIslemTarihi;
                model.EYKYaGonderimDurumAciklamasi = basvuru.EYKYaGonderimDurumAciklamasi;
                model.EYKYaHazirlandi = basvuru.EYKYaHazirlandi;
                model.EYKYaHazirlandiIslemTarihi = basvuru.EYKYaHazirlandiIslemTarihi;
                model.EYKYaHazirlandiIslemYapanID = basvuru.EYKYaHazirlandiIslemYapanID;
                model.EYKDaOnaylandi = basvuru.EYKDaOnaylandi;
                model.EYKDaOnaylandiIslemYapanID = basvuru.EYKDaOnaylandiIslemYapanID;
                model.EYKTarihi = basvuru.EYKTarihi;
                model.EYKSayisi = basvuru.EYKSayisi;
                model.EYKDaOnaylandiOnayTarihi = basvuru.EYKDaOnaylandiOnayTarihi;
                model.EYKDaOnaylanmadiDurumAciklamasi = basvuru.EYKDaOnaylanmadiDurumAciklamasi;

                model.EYKYaGonderildi = basvuru.EYKYaGonderildi;
                model.EYKYaGonderimDurumAciklamasi = basvuru.EYKYaGonderimDurumAciklamasi;
                model.EYKYaHazirlandi = basvuru.EYKYaHazirlandi;
                model.EYKDaOnaylandi = basvuru.EYKDaOnaylandi;
                model.EYKDaOnaylanmadiDurumAciklamasi = basvuru.EYKDaOnaylanmadiDurumAciklamasi;

                model.IslemTarihi = basvuru.IslemTarihi;
                model.IslemYapanID = basvuru.IslemYapanID;
                model.IslemYapanIP = basvuru.IslemYapanIP;






            }
            return model;
        }




        public static Guid AddOrUpdateKayitSilmeBasvuru(KmKayitSilmeBasvuruDto model)
        {
            using (var entities = new LubsDbEntities())
            {
                var kullanici = entities.Kullanicilars.First(f => f.KullaniciID == model.KullaniciID);

                var kayitSilmeBasvuru = entities.KayitSilmeBasvurus.FirstOrDefault(p => p.EnstituKod == model.EnstituKod
                                                                                         && p.KullaniciID == model.KullaniciID
                                                                                         && p.UniqueID == model.UniqueID);
                if (model.DosyaNufusKayitOrnegi != null)
                {
                    model.NufusKayitOrnekDosyaYolu = FileHelper.SaveKayitSilmeBasvuruNufusKayitOrnegiDosya(model.DosyaNufusKayitOrnegi);
                    model.NufusKayitOrnekDosyaAdi = model.DosyaNufusKayitOrnegi.FileName;
                }
                // model.YeterlikSozluSinavTarihi= eklenecek mi ?
                var donemInfo = (kayitSilmeBasvuru?.BasvuruTarihi ?? DateTime.Now.Date).ToKsAkademikDonemBilgi();
                if (kayitSilmeBasvuru != null)
                {


                    kayitSilmeBasvuru.BasvuruTarihi = DateTime.Now;
                    kayitSilmeBasvuru.OgretimYiliBaslangic = donemInfo.BaslangicYil;
                    kayitSilmeBasvuru.DonemID = donemInfo.DonemId;
                    kayitSilmeBasvuru.OgrenimTipKod = kullanici.OgrenimTipKod.Value;
                    kayitSilmeBasvuru.OgrenciNo = kullanici.OgrenciNo;
                    kayitSilmeBasvuru.ProgramKod = kullanici.ProgramKod;
                    kayitSilmeBasvuru.KayitOgretimYiliBaslangic = kullanici.KayitYilBaslangic.Value;
                    kayitSilmeBasvuru.KayitOgretimYiliDonemID = kullanici.KayitDonemID.Value;
                    kayitSilmeBasvuru.KayitTarihi = kullanici.KayitTarihi.Value;
                    kayitSilmeBasvuru.IslemTarihi = DateTime.Now;
                    kayitSilmeBasvuru.IslemYapanIP = UserIdentity.Ip;
                    kayitSilmeBasvuru.IslemYapanID = UserIdentity.Current.Id;
                    kayitSilmeBasvuru.NufusKayitOrnekDosyaYolu = model.NufusKayitOrnekDosyaYolu;
                    kayitSilmeBasvuru.NufusKayitOrnekDosyaAdi = model.NufusKayitOrnekDosyaAdi;

                    entities.SaveChanges();
                    LogIslemleri.LogEkle("KayitSilmeBasvuru", LogCrudType.Update, kayitSilmeBasvuru.ToJson());
                    SendMailBasvuruYapildi(kayitSilmeBasvuru.KayitSilmeBasvuruID);
                    return kayitSilmeBasvuru.UniqueID;

                }

                kayitSilmeBasvuru = entities.KayitSilmeBasvurus.Add(new KayitSilmeBasvuru
                {
                    UniqueID = Guid.NewGuid(),
                    EnstituKod = model.EnstituKod,
                    KayitSilmeDurumID = KayitSilmeDurumEnums.HarcBirimiOnaySureci,
                    OgretimYiliBaslangic = donemInfo.BaslangicYil,
                    DonemID = donemInfo.DonemId,
                    BasvuruTarihi = DateTime.Now,
                    KullaniciID = kullanici.KullaniciID,
                    OgrenciNo = kullanici.OgrenciNo,
                    OgrenimTipKod = kullanici.OgrenimTipKod.Value,
                    ProgramKod = kullanici.ProgramKod,
                    KayitOgretimYiliBaslangic = kullanici.KayitYilBaslangic.Value,
                    KayitOgretimYiliDonemID = kullanici.KayitDonemID.Value,
                    KayitTarihi = kullanici.KayitTarihi.Value,
                    NufusKayitOrnekDosyaAdi = model.NufusKayitOrnekDosyaAdi,
                    NufusKayitOrnekDosyaYolu = model.NufusKayitOrnekDosyaYolu,
                    IsTaahhutOnay = model.IsTaahhutOnay,
                    IslemTarihi = DateTime.Now,
                    IslemYapanIP = UserIdentity.Ip,
                    IslemYapanID = UserIdentity.Current.Id

                });
                entities.SaveChanges();
                return kayitSilmeBasvuru.UniqueID;

            }
        }

        public static List<CmbStringDto> CmbKsDonemListe(string enstituKod, bool bosSecimVar = false)
        {

            using (var entities = new LubsDbEntities())
            {
                var donems = entities.KayitSilmeBasvurus.Where(p => p.EnstituKod == enstituKod).Select(s => new { s.OgretimYiliBaslangic, s.DonemID, s.Donemler.DonemAdi })
                  .Distinct().OrderByDescending(o => o.OgretimYiliBaslangic).ThenByDescending(t => t.DonemID)
                  .Select(s => new CmbStringDto
                  {
                      Value = s.OgretimYiliBaslangic + "" + s.DonemID,
                      Caption = s.OgretimYiliBaslangic + "/" + (s.OgretimYiliBaslangic + 1) + " " + s.DonemAdi

                  }).ToList();
                if (bosSecimVar) donems.Insert(0, new CmbStringDto { Value = null, Caption = "" });
                return donems;
            }
        }
        public static List<CmbIntDto> CmbKsOgrenimTipleri(string enstituKod, bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var entities = new LubsDbEntities())
            {
                var ogrenimTipKods = entities.KayitSilmeBasvurus
                    .Where(p => p.EnstituKod == enstituKod)
                    .Select(s => s.OgrenimTipKod)
                    .Distinct()
                    .ToList();
                var data = entities.OgrenimTipleris.Where(p => p.EnstituKod == enstituKod && ogrenimTipKods.Contains(p.OgrenimTipKod)).Select(s => new { s.OgrenimTipKod, s.OgrenimTipAdi }).OrderBy(o => o.OgrenimTipKod).Distinct().ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.OgrenimTipKod, Caption = item.OgrenimTipAdi });
                }
            }
            return dct;

        }

        public static List<CmbIntDto> CmbKsDurumListe(bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();

            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            dct.Add(new CmbIntDto { Value = KsFilterDurumEnums.HarcBirimiOnayBekleniyor, Caption = "Harç Birimi Onayı Bekleniyor" });
            dct.Add(new CmbIntDto { Value = KsFilterDurumEnums.HarcBirimiTarafindanOnaylandi, Caption = "Harç Birimi Tarafından Onaylandı" });
            dct.Add(new CmbIntDto { Value = KsFilterDurumEnums.HarcBirimiTarafindanReddedildi, Caption = "Harç Birimi Reddedildi" });
            dct.Add(new CmbIntDto { Value = KsFilterDurumEnums.KutuphaneBirimiOnayBekleniyor, Caption = "Kütüphane Birimi Onayı Bekleniyor" });
            dct.Add(new CmbIntDto { Value = KsFilterDurumEnums.KutuphaneBirimiTarafindanOnaylandi, Caption = "Kütüphane Birimi Tarafından Onaylandı" });
            dct.Add(new CmbIntDto { Value = KsFilterDurumEnums.KutuphaneBirimiTarafindanReddedildi, Caption = "Kütüphane Birimi Reddedildi" });
            dct.Add(new CmbIntDto { Value = KsFilterDurumEnums.EykYaGonderimOnayiBekleniyor, Caption = "EYK'ya Gönderimi Bekleniyor" });
            dct.Add(new CmbIntDto { Value = KsFilterDurumEnums.EykYaGonderimiOnaylandi, Caption = "EYK'ya Gönderimi Onaylandı" });
            dct.Add(new CmbIntDto { Value = KsFilterDurumEnums.EykYaGonderimiOnaylanmadi, Caption = "EYK'ya Gönderimi Onaylanmadı" });
            dct.Add(new CmbIntDto { Value = KsFilterDurumEnums.EykYaHazirlandi, Caption = "EYK'ya Hazırlandı" });
            dct.Add(new CmbIntDto { Value = KsFilterDurumEnums.EykDaOnaylandi, Caption = "EYK'da Onaylandı" });
            dct.Add(new CmbIntDto { Value = KsFilterDurumEnums.EykDaOnaylanmadi, Caption = "EYK'da Onaylanmadı" });
            return dct;
        }
        public static List<CmbStringDto> GetCmbFilterKsProgramlar(string enstituKod, int? ogrenimTipKod, bool bosSecimVar = false)
        {
            using (var entities = new LubsDbEntities())
            {
                var programKod = entities.KayitSilmeBasvurus
                  .Where(p => p.EnstituKod == enstituKod && p.OgrenimTipKod == (ogrenimTipKod ?? p.OgrenimTipKod)).Select(s => s.ProgramKod).Distinct().ToList();

                var programlar = entities.Programlars.Where(p => programKod.Contains(p.ProgramKod))
                    .Select(s => new { s.ProgramKod, s.ProgramAdi }).OrderBy(o => o.ProgramAdi).Select(
                        s =>
                            new CmbStringDto { Value = s.ProgramKod, Caption = s.ProgramAdi }
                    ).ToList();
                if (bosSecimVar) programlar.Insert(0, new CmbStringDto { Value = null, Caption = "" });

                return programlar;
            }
        }

        public static MmMessage SendMailBasvuruYapildi(int kayitSilmeBasvuruId)
        {
            return MailSenderKs.SendMailBasvuruYapildi(kayitSilmeBasvuruId);
        }
        public static MmMessage SendMailHarcBirimiOnay(int kayitSilmeBasvuruId, bool isOnayOrRet)
        {
            return MailSenderKs.SendMailHarcBirimiOnay(kayitSilmeBasvuruId, isOnayOrRet);
        }
        public static MmMessage SendMailKutuphaneBirimiRet(int kayitSilmeBasvuruId)
        {
            return MailSenderKs.SendMailKutuphaneBirimiRet(kayitSilmeBasvuruId);
        }
        public static MmMessage SendMailEykOnaylandi(int kayitSilmeBasvuruId)
        {
            return MailSenderKs.SendMailEykOnaylandi(kayitSilmeBasvuruId);
        }
        public static MmMessage SendMailEykOnaylanmadi(int kayitSilmeBasvuruId, bool isEykYaOrEykDa)
        {
            return MailSenderKs.SendMailEykOnaylanmadi(kayitSilmeBasvuruId, isEykYaOrEykDa);
        }

    }
}