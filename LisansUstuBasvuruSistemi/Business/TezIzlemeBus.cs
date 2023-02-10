using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.Helpers;
using LisansUstuBasvuruSistemi.Utilities.Logs;
using LisansUstuBasvuruSistemi.Utilities.MenuAndRoles;
using LisansUstuBasvuruSistemi.Utilities.SystemSetting;

namespace LisansUstuBasvuruSistemi.Business
{
    public static class TezIzlemeBus
    {
        public static TiBasvuruDetayDto GetSecilenBasvuruTiDetay(int tiBasvuruId, Guid? uniqueId)
        {
            var model = new TiBasvuruDetayDto();
            using (var db = new LisansustuBasvuruSistemiEntities())
            {

                var basvuru = db.TIBasvurus.First(p => p.TIBasvuruID == tiBasvuruId);
                var enstitu = db.Enstitulers.First(p => p.EnstituKod == basvuru.EnstituKod);

                var eslesenDanisman = db.Kullanicilars.FirstOrDefault(p => p.KullaniciID == (basvuru.TezDanismanID ?? 0));
                if (eslesenDanisman != null)
                {
                    var unvanAdi = eslesenDanisman.Unvanlar != null ? eslesenDanisman.Unvanlar.UnvanAdi : "";
                    model.TezDanismanBilgiEslesen = unvanAdi + " " + eslesenDanisman.Ad + " " + eslesenDanisman.Soyad;
                }
                else
                {
                    model.TezDanismanBilgiEslesen = "Sistemde eşleşen tez danışmanı bulunamadı.";
                }
                model.TIBasvuruAraRaporList = basvuru.TIBasvuruAraRapors.Where(p => !uniqueId.HasValue || p.TIBasvuruAraRaporKomites.Any(a => a.UniqueID == uniqueId)).Select(s => new TiBasvuruAraRaporDto
                {
                    UniqueID = s.UniqueID,
                    FormKodu = s.FormKodu,
                    TIBasvuruAraRaporID = s.TIBasvuruAraRaporID,
                    TIBasvuruID = s.TIBasvuruID,
                    AraRaporSayisi = s.AraRaporSayisi,
                    RaporTarihi = s.RaporTarihi,
                    IsTezDiliTr = s.IsTezDiliTr,
                    TezBaslikTr = s.TezBaslikTr,
                    TezBaslikEn = s.TezBaslikEn,
                    IsTezDiliDegisecek = s.IsTezDiliDegisecek,
                    YeniTezDiliTr = s.YeniTezDiliTr,
                    SinavAdi = s.SinavAdi,
                    SinavPuani = s.SinavPuani,
                    SinavYili = s.SinavYili,
                    IsTezBasligiDegisti = s.IsTezBasligiDegisti,
                    TezBasligiDegisimGerekcesi = s.TezBasligiDegisimGerekcesi,
                    YeniTezBaslikTr = s.YeniTezBaslikTr,
                    YeniTezBaslikEn = s.YeniTezBaslikEn,
                    TICalismaRaporDosyaAdi = s.TICalismaRaporDosyaAdi,
                    TICalismaRaporDosyaYolu = s.TICalismaRaporDosyaYolu,
                    DonemAdi = s.DonemBaslangicYil + "/" + (s.DonemBaslangicYil + 1) + " " + (s.DonemID == 1 ? "Güz" : "Bahar"),
                    IsYokDrBursiyeriVar = s.IsYokDrBursiyeriVar,
                    YokDrOncelikliAlan = s.YokDrOncelikliAlan,
                    RSBaslatildiMailGonderimTarihi = s.RSBaslatildiMailGonderimTarihi,
                    ToplantiBilgiGonderimTarihi = s.ToplantiBilgiGonderimTarihi,
                    IslemTarihi = s.IslemTarihi,
                    IslemYapanID = s.IslemYapanID,
                    IslemYapanIP = s.IslemYapanIP,
                    TIBasvuruAraRaporDurumID = s.TIBasvuruAraRaporDurumID,
                    TIBasvuruAraaRaporDurumAdi = s.TIBasvuruAraRaporDurumlari.TIBasvuruAraRaporDurumAdi,
                    TIBasvuruAraRaporKomites = s.TIBasvuruAraRaporKomites.ToList(),
                    SRModel = (from sR in s.SRTalepleris
                               join tt in db.SRTalepTipleris on sR.SRTalepTipID equals tt.SRTalepTipID
                               join sal in db.SRSalonlars on sR.SRSalonID equals sal.SRSalonID into def1
                               from defSl in def1.DefaultIfEmpty()
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
                                   SalonAdi = sR.SRSalonID.HasValue ? defSl.SalonAdi : sR.SalonAdi,
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
                                   IslemYapanIP = s.IslemYapanIP,
                                   SRTaleplerJuris = sR.SRTaleplerJuris.ToList(),
                               }).FirstOrDefault()
                }).OrderByDescending(o => o.RaporTarihi).ToList();
                model.TezDanismanID = basvuru.TezDanismanID;
                model.TIBasvuruID = basvuru.TIBasvuruID;
                model.BasvuruTarihi = basvuru.BasvuruTarihi;
                model.KullaniciID = basvuru.KullaniciID;
                model.KayitDonemi = basvuru.KayitOgretimYiliBaslangic + "/" + (basvuru.KayitOgretimYiliBaslangic + 1) + " " + db.Donemlers.First(p => p.DonemID == basvuru.KayitOgretimYiliDonemID.Value).DonemAdi;
                model.KullaniciTipID = basvuru.KullaniciTipID;
                model.ResimAdi = basvuru.ResimAdi;
                model.Ad = basvuru.Kullanicilar.Ad;
                model.Soyad = basvuru.Kullanicilar.Soyad;
                model.TcKimlikNo = basvuru.TcKimlikNo;
                model.PasaportNo = basvuru.PasaportNo;
                model.UyrukKod = basvuru.UyrukKod;
                model.OgrenciNo = basvuru.OgrenciNo;
                model.OgrenimTipAdi = db.OgrenimTipleris.First(p => p.EnstituKod == basvuru.EnstituKod && p.OgrenimTipKod == basvuru.OgrenimTipKod).OgrenimTipAdi;

                model.AnabilimdaliAdi = basvuru.Programlar.AnabilimDallari.AnabilimDaliAdi;
                model.ProgramAdi = basvuru.Programlar.ProgramAdi;
                model.OgrenimDurumID = basvuru.OgrenimDurumID;
                model.OgrenimTipKod = basvuru.OgrenimTipKod;
                model.ProgramKod = basvuru.ProgramKod;
                model.KayitOgretimYiliBaslangic = basvuru.KayitOgretimYiliBaslangic;
                model.KayitOgretimYiliDonemID = basvuru.KayitOgretimYiliDonemID;
                model.KayitTarihi = basvuru.KayitTarihi;
                model.EnstituAdi = enstitu.EnstituAd;
                model.KullaniciID = basvuru.KullaniciID;
                model.BasvuruTarihi = basvuru.BasvuruTarihi;

                model.IslemTarihi = basvuru.IslemTarihi;
                model.IslemYapanID = basvuru.IslemYapanID;
                model.IslemYapanIP = basvuru.IslemYapanIP;
                model.DegerlendirenUniqueID = uniqueId;



            }
            return model;

        }
        public static MmMessage GetAktifTezIzlemeSurecKontrol(string enstituKod, int? kullaniciId, int? tiBasvuruId = null)
        {
            var msg = new MmMessage
            {
                IsSuccess = true
            };
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var kayitYetki = RoleNames.TiGelenBasvuruKayit.InRoleCurrent();
                if (tiBasvuruId.HasValue)
                {
                    var tiBasvuru = db.TIBasvurus.FirstOrDefault(p => p.TIBasvuruID == tiBasvuruId.Value);
                    if (tiBasvuru == null)
                    {
                        msg.IsSuccess = false;
                        msg.Messages.Add("Aranan başvuru sistemde bulunamadı.");
                        if (kayitYetki == false) SistemBilgilendirmeBus.SistemBilgisiKaydet("Aranan başvuru sistemde bulunamadı! \r\n Çağrılan Tez İzleme Başvuru ID:" + tiBasvuruId, "TI Başvuru Düzelt", LogType.Uyarı);
                    }
                    else
                    {
                        var basvuruAcikmi =
                            TiAyar.BasvurusuAcikmi.GetAyarTi(tiBasvuru.EnstituKod, "false").ToBoolean() ?? false;
                        if (tiBasvuru.EnstituKod != enstituKod)
                        {
                            SistemBilgilendirmeBus.SistemBilgisiKaydet("Seçilen Tez İzleme başvurusu Enstitü kodu ile aktif Enstitü kodu uyuşmuyor! \r\n Çağrılan Tez İzleme Başvuru Enstitü Kod:" + tiBasvuru.EnstituKod + " \r\n Aktif Enstitü Kod:" + enstituKod + " \r\n Çağrılan Tez İzleme Başvuru ID:" + tiBasvuru.TIBasvuruID + " \r\n Başvuru Sahibi:" + tiBasvuru.Kullanicilar.KullaniciAdi, "TIK Başvuru Düzelt", LogType.Uyarı);
                            enstituKod = tiBasvuru.EnstituKod;
                        }
                        if (!UserIdentity.Current.EnstituKods.Contains(tiBasvuru.EnstituKod) && kayitYetki && tiBasvuru.KullaniciID != UserIdentity.Current.Id)
                        {
                            msg.IsSuccess = false;
                            msg.Messages.Add("Bu Enstitü için Yetkili Değilsiniz.");
                            var message = $"Bu enstitüye ait Tez İzleme başvurusu güncellemeye yetkili değilsiniz!\r\n Tez İzleme Başvuru ID: { tiBasvuru.TIBasvuruID } \r\n Başvuru sahibi: { tiBasvuru.Kullanicilar.Ad + " " + tiBasvuru.Kullanicilar.Soyad } \r\n Başvuru Tarihi: " + tiBasvuru.BasvuruTarihi;
                            SistemBilgilendirmeBus.SistemBilgisiKaydet(message, "Başvuru Düzelt", LogType.Saldırı);
                        }
                        else if (!basvuruAcikmi && UserIdentity.Current.IsAdmin == false)
                        {
                            msg.IsSuccess = false;
                            msg.Messages.Add("Başvuru süreci dolduğundan başvuru üzerinden herhangi bir işlem yapılamaz!");

                        }
                        if (kayitYetki == false && tiBasvuru.KullaniciID != UserIdentity.Current.Id)
                        {
                            msg.IsSuccess = false;
                            msg.Messages.Add("Bu İşlem için Yetkili Değilsiniz.");
                            SistemBilgilendirmeBus.SistemBilgisiKaydet("Başka bir kullanıcıya ait Tez İzleme başvurusu düzenlemeye hakkınız yoktur! \r\n Çağrılan Tez İzleme Başvuru ID:" + tiBasvuru.TIBasvuruID + " \r\n Başvuru Sahibi:" + tiBasvuru.Kullanicilar.KullaniciAdi, "TIK Başvuru Düzelt", LogType.Saldırı);
                        }
                    }
                }
                else
                {
                    msg.IsSuccess = TiAyar.BasvurusuAcikmi.GetAyarTi(enstituKod, "false").ToBoolean() ?? false;
                    if (kullaniciId.HasValue == false) kullaniciId = UserIdentity.Current.Id;
                    else if (kullaniciId != UserIdentity.Current.Id && RoleNames.KullaniciAdinaTezIzlemeBasvurusuYap.InRoleCurrent() == false && UserIdentity.Current.IsAdmin == false)
                    {
                        SistemBilgilendirmeBus.SistemBilgisiKaydet("Başka bir kullanıcıya adına başvuru yapılmak isteniyor! \r\n Başvuru yapılmak istenen Kullanıcı ID:" + kullaniciId + " \r\n İşlem Yapan Kullanıcı ID:" + UserIdentity.Current.Id, "Tez İzleme Başvuru Yap", LogType.Saldırı);
                        kullaniciId = UserIdentity.Current.Id;
                    }
                    var kullanici = db.Kullanicilars.First(p => p.KullaniciID == kullaniciId.Value);
                    if (msg.IsSuccess == false)
                    {
                        msg.Messages.Add("Başvuru Süreci Kapalı");
                    }
                    else
                    {
                        if (kullanici.YtuOgrencisi && kullanici.OgrenimDurumID == OgrenimDurum.HalenOğrenci && kullanici.OgrenimTipKod.IsDoktora())
                        {
                            var aktifDevamEdenBasvuruVar = db.TIBasvurus.Any(p => p.KullaniciID == kullaniciId && p.OgrenciNo == kullanici.OgrenciNo && p.TIBasvuruID != tiBasvuruId.Value);//aynı başvuru sürecindeki başvurular baz alınsın
                            if (aktifDevamEdenBasvuruVar)// toplam başvuru kontrol
                            {
                                msg.IsSuccess = false;
                                msg.Messages.Add("Aktif olarak devam eden bir Tez izleme süreciniz bulunuyor. Yeni başvuru yapamazsınız. Ara rapor oluşturmak için aşağıda bulunan başvuru detayınızdan 'Yeni Rapor Oluştur' butonuna tıklayınız.");


                            }
                            else
                            {
                                var sondonemKayitOlmasiGerekenDersKodlari = TiAyar.SonDonemKayitOlunmasiGerekenDersKodlari.GetAyarTi(enstituKod, "");

                                var sondonemKayitOlmasiGerekenDersKodlariList = sondonemKayitOlmasiGerekenDersKodlari.Split(',').ToList();
                                var ogrenciBilgi = Management.StudentControl(kullanici.TcKimlikNo);

                                var bkMsg = new List<string>();
                                if (sondonemKayitOlmasiGerekenDersKodlariList.Any() && ogrenciBilgi.AktifDonemDers.DersKodNums.Count(p => sondonemKayitOlmasiGerekenDersKodlariList.Any(a => a == p)) != sondonemKayitOlmasiGerekenDersKodlariList.Count)
                                {
                                    bkMsg.Add(string.Join(", ", sondonemKayitOlmasiGerekenDersKodlari) + " kodlu derslere son dönemde kayıt yaptırmanız gerekmektedi.");
                                }
                                if (bkMsg.Count > 0)
                                {
                                    msg.Messages.Add("Tez izleme başvurunuz aşağıdaki sebeplerden dolayı başlatılamadı.");
                                    msg.Messages.AddRange(bkMsg);
                                    msg.IsSuccess = false;
                                }
                            }
                        }
                        else
                        {
                            msg.IsSuccess = false;
                            msg.Messages.Add("Tez İzleme başvurusunu Aktif olarak okuyan Doktora ve Bütünleşik Doktora öğrencileri tarafından yapılabilir.");
                        }




                    }
                }

            }
            return msg;

        } 
        public static MmMessage SendMailTiBilgisi(int? tiBasvuruAraRaporId, int? srTalepId)
        {
            var mmMessage = new MmMessage();
            try
            {
                using (var db = new LisansustuBasvuruSistemiEntities())
                {

                    var tiAraRapor = new TIBasvuruAraRapor();
                    var srTalebi = new SRTalepleri();

                    if (tiBasvuruAraRaporId.HasValue)
                    {
                        tiAraRapor = db.TIBasvuruAraRapors.First(p => p.TIBasvuruAraRaporID == tiBasvuruAraRaporId);
                        if (srTalepId.HasValue) srTalebi = tiAraRapor.SRTalepleris.FirstOrDefault();
                    }
                    else if (srTalepId.HasValue)
                    {
                        srTalebi = db.SRTalepleris.First(p => p.SRTalepID == srTalepId);
                        tiAraRapor = srTalebi.TIBasvuruAraRapor;
                    }

                    var juriler = tiAraRapor.TIBasvuruAraRaporKomites.ToList();
                    var sablonTipIDs = new List<int>();
                    var mModel = new List<SablonMailModel>();
                    var danisman = juriler.First(p => p.JuriTipAdi == "TezDanismani");
                    var isAraRaporOrToplanti = false;
                    var gonderilenMEkleris = new List<GonderilenMailEkleri>
                    {
                        new GonderilenMailEkleri
                        {
                            EkAdi = tiAraRapor.TICalismaRaporDosyaAdi,
                            EkDosyaYolu = tiAraRapor.TICalismaRaporDosyaYolu,
                        }
                    };

                    if (tiBasvuruAraRaporId.HasValue)
                    {
                        isAraRaporOrToplanti = true;
                        sablonTipIDs.AddRange(new List<int> { MailSablonTipi.TI_AraRaporBaslatildiOgrenci, MailSablonTipi.TI_AraRaporBaslatildiDanisman });
                        mModel.Add(new SablonMailModel
                        {

                            JuriTipAdi = "TezDanismani",
                            UnvanAdi = danisman.UnvanAdi,
                            AdSoyad = danisman.AdSoyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = danisman.EMail, ToOrBcc = true } },
                            MailSablonTipID = MailSablonTipi.TI_AraRaporBaslatildiDanisman
                        });
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Öğrenci " + tiAraRapor.TIBasvuru.Ad + " " + tiAraRapor.TIBasvuru.Soyad,
                            AdSoyad = tiAraRapor.TIBasvuru.Ad + " " + tiAraRapor.TIBasvuru.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = tiAraRapor.TIBasvuru.Kullanicilar.EMail, ToOrBcc = true } },
                            MailSablonTipID = MailSablonTipi.TI_AraRaporBaslatildiOgrenci
                        });
                    }
                    if (srTalepId.HasValue)
                    {
                        sablonTipIDs.AddRange(new List<int> { MailSablonTipi.TI_ToplantiBilgiKomite, MailSablonTipi.TI_ToplantiBilgiOgrenci });
                        mModel.Add(new SablonMailModel
                        {

                            JuriTipAdi = "Öğrenci " + tiAraRapor.TIBasvuru.Ad + " " + tiAraRapor.TIBasvuru.Soyad,
                            AdSoyad = tiAraRapor.TIBasvuru.Ad + " " + tiAraRapor.TIBasvuru.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = tiAraRapor.TIBasvuru.Kullanicilar.EMail, ToOrBcc = true } },
                            MailSablonTipID = MailSablonTipi.TI_ToplantiBilgiOgrenci
                        });
                        mModel.AddRange(juriler.Select(item => new SablonMailModel
                        {
                            UnvanAdi = item.UnvanAdi,
                            AdSoyad = item.AdSoyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = item.EMail, ToOrBcc = true } },
                            MailSablonTipID = MailSablonTipi.TI_ToplantiBilgiKomite,
                            JuriTipAdi = item.JuriTipAdi,
                            TIBasvuruAraRaporKomiteID = danisman.TIBasvuruAraRaporKomiteID
                        }));
                    }

                    var enstitu = tiAraRapor.TIBasvuru.Enstituler;

                    var sablonlar = db.MailSablonlaris.Where(p => sablonTipIDs.Contains(p.MailSablonTipID) && p.EnstituKod == enstitu.EnstituKod).ToList();


                    var abdL = tiAraRapor.TIBasvuru.Programlar.AnabilimDallari;
                    var prgL = tiAraRapor.TIBasvuru.Programlar;
                    var oncekiMailTarihi = isAraRaporOrToplanti ? tiAraRapor.RSBaslatildiMailGonderimTarihi : tiAraRapor.ToplantiBilgiGonderimTarihi;

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
                            else SistemBilgilendirmeBus.SistemBilgisiKaydet("Mail gönderilirken eklenen dosya eki sistemde bulunamadı!<br/>Dosya Adı:" + itemEk.EkAdi + " <br/>Dosya Yolu:" + ekTamYol, "Management/sendMailTIToplantiBilgisi", LogType.Uyarı);
                        }
                        if (item.Sablon.GonderilecekEkEpostalar != null) item.EMails.AddRange(item.Sablon.GonderilecekEkEpostalar.Split(',').Select(s => new MailSendList { EMail = s.Trim(), ToOrBcc = false }));
                        var paramereDegerleri = new List<MailReplaceParameterDto>();


                        if (item.SablonParametreleri.Any(a => a == "@EnstituAdi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "EnstituAdi", Value = enstitu.EnstituAd });
                        if (item.SablonParametreleri.Any(a => a == "@WebAdresi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "WebAdresi", Value = enstitu.WebAdresi, IsLink = true });
                        if (item.SablonParametreleri.Any(a => a == "@AdSoyad"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "AdSoyad", Value = item.AdSoyad });
                        if (item.SablonParametreleri.Any(a => a == "@UnvanAdi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "UnvanAdi", Value = item.UnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciAdSoyad"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "OgrenciAdSoyad", Value = tiAraRapor.TIBasvuru.Ad + " " + tiAraRapor.TIBasvuru.Soyad });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciNo"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "OgrenciNo", Value = tiAraRapor.TIBasvuru.OgrenciNo });
                        if (item.SablonParametreleri.Any(a => a == "@AnabilimdaliAdi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "AnabilimdaliAdi", Value = abdL.AnabilimDaliAdi });
                        if (item.SablonParametreleri.Any(a => a == "@ProgramAdi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "ProgramAdi", Value = prgL.ProgramAdi });
                        if (item.SablonParametreleri.Any(a => a == "@TezBaslikTr"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "TezBaslikTr", Value = tiAraRapor.TezBaslikTr });
                        if (item.SablonParametreleri.Any(a => a == "@TezBaslikEn"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "TezBaslikEn", Value = tiAraRapor.TezBaslikEn });
                        if (item.SablonParametreleri.Any(a => a == "@YokDrBursiyeriBilgi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "YokDrBursiyeriBilgi", Value = tiAraRapor.IsYokDrBursiyeriVar ? "Var (Öncelikli Alan: " + tiAraRapor.YokDrOncelikliAlan + ")" : "Yok" });
                        if (item.SablonParametreleri.Any(a => a == "@DonemAdi"))
                        {
                            var donemBilgi = tiAraRapor.RaporTarihi.ToAraRaporDonemBilgi();
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "DonemAdi", Value = donemBilgi.DonemAdiLong });
                        }
                        if (item.SablonParametreleri.Any(a => a == "@OncekiMailTarihi"))
                        { 
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "OncekiMailTarihi", Value = oncekiMailTarihi?.ToString("dd-MM-yyyy HH:mm") });
                        }
                        #region SR Talebi
                        if (item.MailSablonTipID == MailSablonTipi.TI_ToplantiBilgiKomite || item.MailSablonTipID == MailSablonTipi.TI_ToplantiBilgiOgrenci)
                        {
                            if (item.SablonParametreleri.Any(a => a == "@ToplantiTarihi"))
                                paramereDegerleri.Add(new MailReplaceParameterDto { Key = "ToplantiTarihi", Value = srTalebi.Tarih.ToLongDateString() });
                            if (item.SablonParametreleri.Any(a => a == "@ToplantiSaati"))
                                paramereDegerleri.Add(new MailReplaceParameterDto { Key = "ToplantiSaati", Value =
                                    $"{srTalebi.BasSaat:hh\\:mm}"
                                });

                            if (!srTalebi.IsOnline)
                            {
                                if (item.SablonParametreleri.Any(a => a == "@ToplantiSekli"))
                                {
                                    paramereDegerleri.Add(new MailReplaceParameterDto { Key = "ToplantiSekli", Value = "Yüz Yüze" });
                                }
                                if (item.SablonParametreleri.Any(a => a == "@ToplantiYeriBaslik"))
                                {
                                    paramereDegerleri.Add(new MailReplaceParameterDto { Key = "ToplantiYeriBaslik", Value = "Toplantı Salonu" });
                                }
                                if (item.SablonParametreleri.Any(a => a == "@ToplantiYeri"))
                                {
                                    paramereDegerleri.Add(new MailReplaceParameterDto { Key = "ToplantiYeri", Value = srTalebi.SalonAdi });
                                }
                            }
                            else
                            {
                                if (item.SablonParametreleri.Any(a => a == "@ToplantiSekli"))
                                {
                                    paramereDegerleri.Add(new MailReplaceParameterDto { Key = "ToplantiSekli", Value = "Çevrim İçi" });
                                }
                                if (item.SablonParametreleri.Any(a => a == "@ToplantiYeriBaslik"))
                                {
                                    paramereDegerleri.Add(new MailReplaceParameterDto { Key = "ToplantiYeriBaslik", Value = "Toplantı Katılım Linki" });
                                }
                                if (item.SablonParametreleri.Any(a => a == "@ToplantiYeri"))
                                {
                                    paramereDegerleri.Add(new MailReplaceParameterDto { Key = "ToplantiYeri", Value = srTalebi.SalonAdi, IsLink = true });
                                }
                            }
                        }
                        #endregion
                        #region DanismanKomite
                        if (item.SablonParametreleri.Any(a => a == "@DanismanBilgi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "DanismanBilgi", Value = danisman.UnvanAdi + " " + danisman.AdSoyad });
                        if (item.SablonParametreleri.Any(a => a == "@DanismanUni"))
                        {
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "DanismanUni", Value = danisman.UniversiteAdi });
                        }
                        foreach (var itemTik in juriler.Where(p => p.JuriTipAdi != "TezDanismani").Select((s, inx) => new { s, inx = inx + 1 }))
                        {

                            if (item.SablonParametreleri.Any(a => a == "@TikBilgi" + itemTik.inx))
                                paramereDegerleri.Add(new MailReplaceParameterDto { Key = "TikBilgi" + itemTik.inx, Value = itemTik.s.UnvanAdi + " " + itemTik.s.AdSoyad });
                            if (item.SablonParametreleri.Any(a => a == "@TikBilgiUni" + itemTik.inx))
                            {
                                paramereDegerleri.Add(new MailReplaceParameterDto { Key = "TikBilgiUni" + itemTik.inx, Value =itemTik.s.UniversiteAdi });
                            }
                        }
                        #endregion

                        var mCOntent = SystemMails.GetSystemMailContent(enstitu.EnstituAd, item.Sablon.SablonHtml, item.Sablon.SablonAdi, paramereDegerleri);
                        var snded = MailManager.SendMail(enstitu.EnstituKod, mCOntent.Title, mCOntent.HtmlContent, item.EMails, item.Attachments);
                        if (snded)
                        {
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
                            if (isAraRaporOrToplanti) tiAraRapor.RSBaslatildiMailGonderimTarihi = DateTime.Now;
                            else tiAraRapor.ToplantiBilgiGonderimTarihi = DateTime.Now;

                            LogIslemleri.LogEkle("TIBasvuruAraRapor", IslemTipi.Update, tiAraRapor.ToJson());


                        }
                    }
                    if (isSended) db.SaveChanges();
                    mmMessage.IsSuccess = true;
                    mmMessage.MessageType = Msgtype.Success;

                }
            }
            catch (Exception ex)
            {
                var message = "Tez İzleme toplantısı için Komite üyelerine mail gönderilirken bir hata oluştu! \r\nSRTalepID:" + srTalepId;
                SistemBilgilendirmeBus.SistemBilgisiKaydet(message + "\r\n Hata:" + ex.ToExceptionMessage(), "Management/sendMailTIBilgisi \r\n" + ex.ToExceptionStackTrace(), LogType.Hata);
                mmMessage.Messages.Add(message + "</br> Hata:" + ex.ToExceptionMessage());
                mmMessage.MessageType = Msgtype.Error;
                mmMessage.IsSuccess = false;
            }
            return mmMessage;
        }
        public static MmMessage SendMailTiDegerlendirmeLink(int tiBasvuruAraRaporId, Guid? uniqueId, bool isLinkOrSonuc)
        {
            var mmMessage = new MmMessage();
            try
            {
                using (var db = new LisansustuBasvuruSistemiEntities())
                {
                    var tiAraRapor = db.TIBasvuruAraRapors.First(p => p.TIBasvuruAraRaporID == tiBasvuruAraRaporId);
                    var juriler = tiAraRapor.TIBasvuruAraRaporKomites.Where(p => (isLinkOrSonuc ? p.JuriTipAdi != "TezDanismani" : p.JuriTipAdi == "TezDanismani") && p.UniqueID == (uniqueId ?? p.UniqueID)).ToList();

                    var mModel = new List<SablonMailModel>();

                    var enstitu = tiAraRapor.TIBasvuru.Enstituler;

                    var abdL = tiAraRapor.TIBasvuru.Programlar.AnabilimDallari;
                    var prgL = tiAraRapor.TIBasvuru.Programlar;

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

                            JuriTipAdi = "Öğrenci " + tiAraRapor.TIBasvuru.Ad + " " + tiAraRapor.TIBasvuru.Soyad,
                            AdSoyad = tiAraRapor.TIBasvuru.Ad + " " + tiAraRapor.TIBasvuru.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = tiAraRapor.TIBasvuru.Kullanicilar.EMail, ToOrBcc = true } },
                            MailSablonTipID = MailSablonTipi.TI_DegerlendirmeSonucGonderimOgrenci,
                        });
                    }

                    foreach (var item in juriler)
                    {
                        mModel.Add(new SablonMailModel
                        {
                            UniqueID = item.UniqueID,

                            UnvanAdi = item.UnvanAdi,
                            AdSoyad = item.AdSoyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = item.EMail, ToOrBcc = true } },
                            MailSablonTipID = isLinkOrSonuc ? MailSablonTipi.TI_DegerlendirmeLinkGonderimKomite : MailSablonTipi.TI_DegerlendirmeSonucGonderimDanisman,
                            JuriTipAdi = item.JuriTipAdi,
                        });
                    }
                    var mailSablonTipIDs = mModel.Select(s => s.MailSablonTipID).Distinct().ToList();
                    var sablonlar = db.MailSablonlaris.Where(p => mailSablonTipIDs.Contains(p.MailSablonTipID) && p.EnstituKod == enstitu.EnstituKod).ToList();
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
                            else SistemBilgilendirmeBus.SistemBilgisiKaydet("Mail gönderilirken eklenen dosya eki sistemde bulunamadı!<br/>Dosya Adı:" + itemSe.EkAdi + " <br/>Dosya Yolu:" + ekTamYol, "Management/sendMailTIToplantiBilgisi", LogType.Uyarı);
                        }
                        if (!isLinkOrSonuc)
                        {
                            var ds = new List<int?>() { tiBasvuruAraRaporId };
                            if (item.MailSablonTipID == MailSablonTipi.TI_DegerlendirmeSonucGonderimDanisman) ds.Add(1);
                            var ekler = Management.exportRaporPdf(RaporTipleri.TezIzlemeDegerlendirmeFormu, ds);
                            gonderilenMailEkleri.AddRange(ekler.Select(s => new GonderilenMailEkleri { EkAdi = s.Name, EkDosyaYolu = "" }));
                            item.Attachments.AddRange(ekler);
                        }

                        if (item.Sablon.GonderilecekEkEpostalar != null) item.EMails.AddRange(item.Sablon.GonderilecekEkEpostalar.Split(',').Select(s => new MailSendList { EMail = s.Trim(), ToOrBcc = false }));
                        var paramereDegerleri = new List<MailReplaceParameterDto>();


                        if (item.SablonParametreleri.Any(a => a == "@EnstituAdi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "EnstituAdi", Value = enstitu.EnstituAd });
                        if (item.SablonParametreleri.Any(a => a == "@WebAdresi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "WebAdresi", Value = enstitu.WebAdresi, IsLink = true });
                        if (item.SablonParametreleri.Any(a => a == "@AdSoyad"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "AdSoyad", Value = item.AdSoyad });
                        if (item.SablonParametreleri.Any(a => a == "@UnvanAdi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "UnvanAdi", Value = item.UnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciAdSoyad"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "OgrenciAdSoyad", Value = tiAraRapor.TIBasvuru.Ad + " " + tiAraRapor.TIBasvuru.Soyad });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciNo"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "OgrenciNo", Value = tiAraRapor.TIBasvuru.OgrenciNo });
                        if (item.SablonParametreleri.Any(a => a == "@AnabilimdaliAdi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "AnabilimdaliAdi", Value = abdL.AnabilimDaliAdi });
                        if (item.SablonParametreleri.Any(a => a == "@ProgramAdi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "ProgramAdi", Value = prgL.ProgramAdi });
                        if (item.SablonParametreleri.Any(a => a == "@TezBaslikTr"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "TezBaslikTr", Value = tiAraRapor.TezBaslikTr });
                        if (item.SablonParametreleri.Any(a => a == "@TezBaslikEn"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "TezBaslikEn", Value = tiAraRapor.TezBaslikEn });
                        if (item.SablonParametreleri.Any(a => a == "@YokDrBursiyeriBilgi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "YokDrBursiyeriBilgi", Value = tiAraRapor.IsYokDrBursiyeriVar ? "Var (Öncelikli Alan: " + tiAraRapor.YokDrOncelikliAlan + ")" : "Yok" });
                        if (item.SablonParametreleri.Any(a => a == "@DonemAdi"))
                        {
                            var donemBilgi = tiAraRapor.RaporTarihi.ToAraRaporDonemBilgi();
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "DonemAdi", Value = donemBilgi.DonemAdiLong });
                        }
                        if (item.SablonParametreleri.Any(a => a == "@Link"))
                        {
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "Link", Value = enstitu.SistemErisimAdresi + "/TIBasvuru/Index?IsDegerlendirme=" + item.UniqueID, IsLink = true });
                        }
                        if (item.SablonParametreleri.Any(a => a == "@OncekiMailTarihi"))
                        {
                            paramereDegerleri.Add(isLinkOrSonuc
                                ? new MailReplaceParameterDto
                                {
                                    Key = "OncekiMailTarihi",
                                    Value = juri.LinkGonderimTarihi?.ToString("dd-MM-yyyy HH:mm")
                                }
                                : new MailReplaceParameterDto
                                {
                                    Key = "OncekiMailTarihi",
                                    Value = tiAraRapor.DegerlendirmeSonucMailTarihi?.ToString("dd-MM-yyyy HH:mm")
                                });
                        }
                        var mCOntent = SystemMails.GetSystemMailContent(enstitu.EnstituAd, item.Sablon.SablonHtml, item.Sablon.SablonAdi, paramereDegerleri);
                        var snded = MailManager.SendMail(enstitu.EnstituKod, mCOntent.Title, mCOntent.HtmlContent, item.EMails, item.Attachments);
                        if (snded)
                        {
                            var kModel = new GonderilenMailler();
                            kModel.Tarih = DateTime.Now;
                            kModel.EnstituKod = enstitu.EnstituKod;
                            kModel.MesajID = null;
                            kModel.IslemTarihi = DateTime.Now;
                            kModel.Konu = mCOntent.Title;
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
                                juri.IsBasarili = null;
                                juri.IsTezIzlemeRaporuAltAlanUygun = null;
                                juri.IsTezIzlemeRaporuTezOnerisiUygun = null;
                                juri.Aciklama = null;
                                juri.IsLinkGonderildi = true;
                                juri.LinkGonderimTarihi = DateTime.Now;
                                juri.LinkGonderenID = UserIdentity.Current.Id;

                            }

                            db.GonderilenMaillers.Add(kModel);
                            db.SaveChanges();
                            if (isLinkOrSonuc) LogIslemleri.LogEkle("TIBasvuruAraRaporKomite", IslemTipi.Update, juri.ToJson());
                        }
                    }

                    mmMessage.IsSuccess = true;
                    mmMessage.MessageType = Msgtype.Success;

                }
            }
            catch (Exception ex)
            {
                var message = "";
                message = isLinkOrSonuc ? "Tez İzleme değerlendirmesi için Komite üyelerine değerlendirme davetiye linki mail olarak gönderilirken bir hata oluştu!" : "Tez İzleme değerlendirmesi sonucu Komite üyelerine mail olarak gönderilirken bir hata oluştu!";
                SistemBilgilendirmeBus.SistemBilgisiKaydet(message + "\r\n Hata:" + ex.ToExceptionMessage(), "Management/sendMailTIDegerlendirmeLink \r\n" + ex.ToExceptionStackTrace(), LogType.Hata);
                //mmMessage.Title = "Hata";
                mmMessage.Messages.Add(message + "</br> Hata:" + ex.ToExceptionMessage());
                mmMessage.MessageType = Msgtype.Error;
                mmMessage.IsSuccess = false;
            }
            return mmMessage;
        }
        public static MmMessage GetTiBasvuruSilKontrol(int tiBasvuruId)
        {
            var msg = new MmMessage
            {
                IsSuccess = true
            };

            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var kayitYetki = RoleNames.TiGelenBasvuruKayit.InRoleCurrent();
                var basvuru = db.TIBasvurus.FirstOrDefault(p => p.TIBasvuruID == tiBasvuruId);
                if (basvuru == null)
                {
                    msg.IsSuccess = false;
                    msg.Messages.Add("Başvuru Bulunamadı.");
                }
                else
                {
                    if (!UserIdentity.Current.EnstituKods.Contains(basvuru.EnstituKod) && kayitYetki && basvuru.KullaniciID != UserIdentity.Current.Id)
                    {
                        msg.IsSuccess = false;
                        msg.Messages.Add("Bu enstitüye ait başvuruyu silmeye yetkili değilsiniz!");
                        string message = "Bu enstitüye ait tez izleme başvurusu silmeye yetkili değilsiniz!\r\n Tez İzleme Başvuru ID: " + basvuru.TIBasvuruID + " \r\n Tez İzleme Başvuru sahibi: " + basvuru.Kullanicilar.Ad + " " + basvuru.Kullanicilar.Soyad + " \r\n Başvuru Tarihi: " + basvuru.BasvuruTarihi.ToString();
                        SistemBilgilendirmeBus.SistemBilgisiKaydet(message, "TIK Başvuru Sil", LogType.Kritik);
                    }
                    else if (!TiAyar.BasvurusuAcikmi.GetAyarTi(basvuru.EnstituKod, "false").ToBoolean().Value && UserIdentity.Current.IsAdmin == false)
                    {
                        msg.IsSuccess = false;
                        msg.Messages.Add("Başvuru süreci dolduğundan başvuru üzerinden herhangi bir işlem yapılamaz!");

                    }
                    else if (kayitYetki == false && basvuru.KullaniciID != UserIdentity.Current.Id)
                    {
                        msg.IsSuccess = false;
                        msg.Messages.Add("Başka bir kullanıcıya ait başvuruyu silmeye hakkınız yoktur!");
                        SistemBilgilendirmeBus.SistemBilgisiKaydet("Başka bir kullanıcıya ait Tez İzleme başvurusunu silmeye hakkınız yoktur! \r\n Silinmeye çalışılan Tez İzleme Başvuru ID:" + basvuru.TIBasvuruID + " \r\n Tez İzleme Başvuru Sahibi:" + basvuru.Kullanicilar.KullaniciAdi + " \r\n Başvuru Tarihi:" + basvuru.BasvuruTarihi.ToString(), "Başvuru Sil", LogType.Saldırı);
                    }
                    //else if (KayitYetki == false && basvuru.MezuniyetYayinKontrolDurumID == MezuniyetYayinKontrolDurumu.Onaylandi)
                    //{
                    //    msg.IsSuccess = false;
                    //    msg.Messages.Add("Taslak Harici Başvurular Silinemez.");
                    //}
                }
            }
            return msg;
        }
        public static string IsSuccessSinavPuanUye(this string sinavPuani, bool sinavPuanKontroluYap, int puanKriteri)
        {
            string msg = "";
            if (sinavPuani.IsNullOrWhiteSpace())
            {
                msg = "Dil Sınavı puanı bilgisi boş bırakılamaz";
            }
            else
            {
                if (sinavPuanKontroluYap)
                {
                    sinavPuani = sinavPuani.Replace(" ", "").Replace(".", ",");
                    var isSinavPuaniSayi = sinavPuani.IsNumberX();
                    if (!isSinavPuaniSayi)
                    {
                        msg = "Dil Sınavı puanı girişi sayıdan oluşmalıdır.";
                    }
                    else
                    {
                        var puan = Convert.ToDouble(sinavPuani);
                        if (puanKriteri > puan || puan > 100)
                        {
                            msg = "Dil Sınavı puanı girişi " + puanKriteri + " ile 100 notları arasında olmalıdır.";
                        }
                    }
                }
            }
            return msg;
        }
        public static bool ToTiUyeFormSuccessRow(this string juriTipAdi, bool tezDiliTr, bool adSoyadSuccess, bool unvanAdiSuccess, bool eMailSuccess, bool universiteIdSuccess, bool isAnabilimdaliProgramAdiSuccess, bool isDilSinaviOrUniversiteSuccess, bool dilSinavAdiSuccess, bool dilPuaniSuccess, bool sinavTarihiSuccess)
        {
            var retVal = adSoyadSuccess && unvanAdiSuccess && eMailSuccess && universiteIdSuccess && isAnabilimdaliProgramAdiSuccess && isDilSinaviOrUniversiteSuccess && dilSinavAdiSuccess && dilPuaniSuccess && sinavTarihiSuccess;

            return retVal;
        }
 
        public static List<CmbStringDto> CmbTiAktifDonemListe(bool bosSecimVar = false)
        {
            var dct = new List<CmbStringDto>();

            if (bosSecimVar) dct.Add(new CmbStringDto { Value = "", Caption = "" });
            for (int i = DateTime.Now.Year; i >= 2020; i--)
            {
                dct.Add(new CmbStringDto { Value = i + "2", Caption = i + "/" + (i + 1) + " Bahar" });
                dct.Add(new CmbStringDto { Value = i + "1", Caption = i + "/" + (i + 1) + " Güz" });
            }
            return dct;
        } 
        public static List<CmbIntDto> CmbTiAraRaporDurumListe(bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();

            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var arDurums = db.TIBasvuruAraRaporDurumlaris.Select(s => new CmbIntDto { Value = s.TIBasvuruAraRaporDurumID, Caption = s.TIBasvuruAraRaporDurumAdi }).ToList();
                dct.AddRange(arDurums);
            }
            return dct;
        }
        public static List<CmbIntDto> CmbAraRaporSayisi(bool bosSecimVar = false, int max = 50)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            for (int i = 1; i <= max; i++)
            {
                dct.Add(new CmbIntDto { Value = i, Caption = i + ". Rapor" });
            }

            return dct;

        }
    }
}