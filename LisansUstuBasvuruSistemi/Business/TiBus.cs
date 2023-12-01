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
    public static class TiBus
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
                    model.TezDanismaniUserKey = eslesenDanisman.UserKey;
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
                        if (kayitYetki == false) SistemBilgilendirmeBus.SistemBilgisiKaydet("Aranan başvuru sistemde bulunamadı! \r\n Çağrılan Tez İzleme Başvuru ID:" + tiBasvuruId, "TI Başvuru Düzelt", LogTipiEnum.Uyarı);
                    }
                    else
                    {
                        var basvuruAcikmi =
                            TiAyar.BasvurusuAcikmi.GetAyarTi(tiBasvuru.EnstituKod, "false").ToBoolean() ?? false;
                        if (tiBasvuru.EnstituKod != enstituKod)
                        {
                            SistemBilgilendirmeBus.SistemBilgisiKaydet("Seçilen Tez İzleme başvurusu Enstitü kodu ile aktif Enstitü kodu uyuşmuyor! \r\n Çağrılan Tez İzleme Başvuru Enstitü Kod:" + tiBasvuru.EnstituKod + " \r\n Aktif Enstitü Kod:" + enstituKod + " \r\n Çağrılan Tez İzleme Başvuru ID:" + tiBasvuru.TIBasvuruID + " \r\n Başvuru Sahibi:" + tiBasvuru.Kullanicilar.KullaniciAdi, "TIK Başvuru Düzelt", LogTipiEnum.Uyarı);
                            enstituKod = tiBasvuru.EnstituKod;
                        }
                        if (!UserIdentity.Current.EnstituKods.Contains(tiBasvuru.EnstituKod) && kayitYetki && tiBasvuru.KullaniciID != UserIdentity.Current.Id)
                        {
                            msg.IsSuccess = false;
                            msg.Messages.Add("Bu Enstitü için Yetkili Değilsiniz.");
                            var message = $"Bu enstitüye ait Tez İzleme başvurusu güncellemeye yetkili değilsiniz!\r\n Tez İzleme Başvuru ID: { tiBasvuru.TIBasvuruID } \r\n Başvuru sahibi: { tiBasvuru.Kullanicilar.Ad + " " + tiBasvuru.Kullanicilar.Soyad } \r\n Başvuru Tarihi: " + tiBasvuru.BasvuruTarihi;
                            SistemBilgilendirmeBus.SistemBilgisiKaydet(message, "Başvuru Düzelt", LogTipiEnum.Saldırı);
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
                            SistemBilgilendirmeBus.SistemBilgisiKaydet("Başka bir kullanıcıya ait Tez İzleme başvurusu düzenlemeye hakkınız yoktur! \r\n Çağrılan Tez İzleme Başvuru ID:" + tiBasvuru.TIBasvuruID + " \r\n Başvuru Sahibi:" + tiBasvuru.Kullanicilar.KullaniciAdi, "TIK Başvuru Düzelt", LogTipiEnum.Saldırı);
                        }
                    }
                }
                else
                {
                    msg.IsSuccess = TiAyar.BasvurusuAcikmi.GetAyarTi(enstituKod, "false").ToBoolean() ?? false;
                    if (kullaniciId.HasValue == false) kullaniciId = UserIdentity.Current.Id;
                    else if (kullaniciId != UserIdentity.Current.Id   && UserIdentity.Current.IsAdmin == false)
                    {
                        kullaniciId = UserIdentity.Current.Id;
                    }
                    var kul = db.Kullanicilars.First(p => p.KullaniciID == kullaniciId.Value);
                    if (msg.IsSuccess == false)
                    {
                        msg.Messages.Add("Başvuru Süreci Kapalı");
                    }
                    else
                    {
                        if (kul.YtuOgrencisi && kul.OgrenimDurumID == OgrenimDurumEnum.HalenOğrenci && kul.OgrenimTipKod.IsDoktora())
                        {
                            var aktifDevamEdenBasvuruVar = db.TIBasvurus.Any(p => p.KullaniciID == kullaniciId && p.OgrenciNo == kul.OgrenciNo && p.TIBasvuruID != tiBasvuruId.Value);//aynı başvuru sürecindeki başvurular baz alınsın
                            if (aktifDevamEdenBasvuruVar)// toplam başvuru kontrol
                            {
                                msg.IsSuccess = false;
                                msg.Messages.Add("Aktif olarak devam eden bir Tez izleme süreciniz bulunuyor. Yeni başvuru yapamazsınız. Ara rapor oluşturmak için aşağıda bulunan başvuru detayınızdan 'Yeni Rapor Oluştur' butonuna tıklayınız.");


                            }
                            else
                            {
                                var sondonemKayitOlmasiGerekenDersKodlari = TiAyar.SonDonemKayitOlunmasiGerekenDersKodlari.GetAyarTi(enstituKod, "");

                                var sondonemKayitOlmasiGerekenDersKodlariList = sondonemKayitOlmasiGerekenDersKodlari.Split(',').Where(p => !p.IsNullOrWhiteSpace()).ToList();
                            
                                var ogrenciBilgi = KullanicilarBus.OgrenciKontrol(kul.TcKimlikNo);

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
                    var kul = tiAraRapor.TIBasvuru.Kullanicilar;
                    if (tiBasvuruAraRaporId.HasValue)
                    {
                        isAraRaporOrToplanti = true;
                        sablonTipIDs.AddRange(new List<int> { MailSablonTipiEnum.TiAraRaporBaslatildiOgrenci, MailSablonTipiEnum.TiAraRaporBaslatildiDanisman });
                        mModel.Add(new SablonMailModel
                        {

                            JuriTipAdi = "TezDanismani",
                            UnvanAdi = danisman.UnvanAdi,
                            AdSoyad = danisman.AdSoyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = danisman.EMail, ToOrBcc = true } },
                            MailSablonTipID = MailSablonTipiEnum.TiAraRaporBaslatildiDanisman
                        });
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Öğrenci " + kul.Ad + " " + kul.Soyad,
                            AdSoyad = kul.Ad + " " + kul.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = tiAraRapor.TIBasvuru.Kullanicilar.EMail, ToOrBcc = true } },
                            MailSablonTipID = MailSablonTipiEnum.TiAraRaporBaslatildiOgrenci
                        });
                    }
                    if (srTalepId.HasValue)
                    {
                        sablonTipIDs.AddRange(new List<int> { MailSablonTipiEnum.TiToplantiBilgiKomite, MailSablonTipiEnum.TiToplantiBilgiOgrenci });
                        mModel.Add(new SablonMailModel
                        {

                            JuriTipAdi = "Öğrenci " + kul.Ad + " " + kul.Soyad,
                            AdSoyad = kul.Ad + " " + kul.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = tiAraRapor.TIBasvuru.Kullanicilar.EMail, ToOrBcc = true } },
                            MailSablonTipID = MailSablonTipiEnum.TiToplantiBilgiOgrenci
                        });
                        mModel.AddRange(juriler.Select(item => new SablonMailModel
                        {
                            UnvanAdi = item.UnvanAdi,
                            AdSoyad = item.AdSoyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = item.EMail, ToOrBcc = true } },
                            MailSablonTipID = MailSablonTipiEnum.TiToplantiBilgiKomite,
                            JuriTipAdi = item.JuriTipAdi,
                            TIBasvuruAraRaporKomiteID = danisman.TIBasvuruAraRaporKomiteID
                        }));
                    }

                    var enstitu = tiAraRapor.TIBasvuru.Enstituler;

                    var sablonlar = db.MailSablonlaris.Where(p => p.IsAktif && sablonTipIDs.Contains(p.MailSablonTipID) && p.EnstituKod == enstitu.EnstituKod).ToList();


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
                            mailParameterDtos.Add(new MailParameterDto { Key = "OgrenciNo", Value = tiAraRapor.TIBasvuru.OgrenciNo });
                        if (item.SablonParametreleri.Any(a => a == "@AnabilimdaliAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "AnabilimdaliAdi", Value = abdL.AnabilimDaliAdi });
                        if (item.SablonParametreleri.Any(a => a == "@ProgramAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "ProgramAdi", Value = prgL.ProgramAdi });
                        if (item.SablonParametreleri.Any(a => a == "@TezBaslikTr"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "TezBaslikTr", Value = tiAraRapor.TezBaslikTr });
                        if (item.SablonParametreleri.Any(a => a == "@TezBaslikEn"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "TezBaslikEn", Value = tiAraRapor.TezBaslikEn });
                        if (item.SablonParametreleri.Any(a => a == "@YokDrBursiyeriBilgi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "YokDrBursiyeriBilgi", Value = tiAraRapor.IsYokDrBursiyeriVar ? "Var (Öncelikli Alan: " + tiAraRapor.YokDrOncelikliAlan + ")" : "Yok" });
                        if (item.SablonParametreleri.Any(a => a == "@DonemAdi"))
                        {
                            var donemBilgi = tiAraRapor.RaporTarihi.ToAraRaporDonemBilgi();
                            mailParameterDtos.Add(new MailParameterDto { Key = "DonemAdi", Value = donemBilgi.DonemAdiLong });
                        }
                        if (item.SablonParametreleri.Any(a => a == "@OncekiMailTarihi"))
                        {
                            mailParameterDtos.Add(new MailParameterDto { Key = "OncekiMailTarihi", Value = oncekiMailTarihi?.ToFormatDateAndTime() });
                        }
                        #region SR Talebi
                        if (item.MailSablonTipID == MailSablonTipiEnum.TiToplantiBilgiKomite || item.MailSablonTipID == MailSablonTipiEnum.TiToplantiBilgiOgrenci)
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
                        foreach (var itemTik in juriler.Where(p => p.JuriTipAdi != "TezDanismani").Select((s, inx) => new { s, inx = inx + 1 }))
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
                        if (isAraRaporOrToplanti) tiAraRapor.RSBaslatildiMailGonderimTarihi = DateTime.Now;
                        else tiAraRapor.ToplantiBilgiGonderimTarihi = DateTime.Now;

                        LogIslemleri.LogEkle("TIBasvuruAraRapor", LogCrudType.Update, tiAraRapor.ToJson());
                    }
                    if (isSended) db.SaveChanges();
                    mmMessage.IsSuccess = true;
                    mmMessage.MessageType = MsgTypeEnum.Success;

                }
            }
            catch (Exception ex)
            {
                var message = "Tez İzleme toplantısı için Komite üyelerine mail gönderilirken bir hata oluştu! \r\nSRTalepID:" + srTalepId;
                SistemBilgilendirmeBus.SistemBilgisiKaydet(message + "\r\n Hata:" + ex.ToExceptionMessage(), "Management/sendMailTIBilgisi \r\n" + ex.ToExceptionStackTrace(), LogTipiEnum.Hata);
                mmMessage.Messages.Add(message + "</br> Hata:" + ex.ToExceptionMessage());
                mmMessage.MessageType = MsgTypeEnum.Error;
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
                    var kul = tiAraRapor.TIBasvuru.Kullanicilar;
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
                            EMails = new List<MailSendList> { new MailSendList { EMail = tiAraRapor.TIBasvuru.Kullanicilar.EMail, ToOrBcc = true } },
                            MailSablonTipID = MailSablonTipiEnum.TiDegerlendirmeSonucGonderimOgrenci,
                        });
                    }

                    mModel.AddRange(juriler.Select(item => new SablonMailModel
                    {
                        UniqueID = item.UniqueID,
                        UnvanAdi = item.UnvanAdi,
                        AdSoyad = item.AdSoyad,
                        EMails = new List<MailSendList> { new MailSendList { EMail = item.EMail, ToOrBcc = true } },
                        MailSablonTipID = isLinkOrSonuc ? MailSablonTipiEnum.TiDegerlendirmeLinkGonderimKomite : MailSablonTipiEnum.TiDegerlendirmeSonucGonderimDanisman,
                        JuriTipAdi = item.JuriTipAdi,
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
                            var ds = new List<int?>() { tiBasvuruAraRaporId };
                            if (item.MailSablonTipID == MailSablonTipiEnum.TiDegerlendirmeSonucGonderimDanisman) ds.Add(1);
                            var ekler = Management.ExportRaporPdf(RaporTipiEnum.TezIzlemeDegerlendirmeFormu, ds);
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
                            mailParameterDtos.Add(new MailParameterDto { Key = "OgrenciNo", Value = tiAraRapor.TIBasvuru.OgrenciNo });
                        if (item.SablonParametreleri.Any(a => a == "@AnabilimdaliAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "AnabilimdaliAdi", Value = abdL.AnabilimDaliAdi });
                        if (item.SablonParametreleri.Any(a => a == "@ProgramAdi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "ProgramAdi", Value = prgL.ProgramAdi });
                        if (item.SablonParametreleri.Any(a => a == "@TezBaslikTr"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "TezBaslikTr", Value = tiAraRapor.TezBaslikTr });
                        if (item.SablonParametreleri.Any(a => a == "@TezBaslikEn"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "TezBaslikEn", Value = tiAraRapor.TezBaslikEn });
                        if (item.SablonParametreleri.Any(a => a == "@YokDrBursiyeriBilgi"))
                            mailParameterDtos.Add(new MailParameterDto { Key = "YokDrBursiyeriBilgi", Value = tiAraRapor.IsYokDrBursiyeriVar ? "Var (Öncelikli Alan: " + tiAraRapor.YokDrOncelikliAlan + ")" : "Yok" });
                        if (item.SablonParametreleri.Any(a => a == "@DonemAdi"))
                        {
                            var donemBilgi = tiAraRapor.RaporTarihi.ToAraRaporDonemBilgi();
                            mailParameterDtos.Add(new MailParameterDto { Key = "DonemAdi", Value = donemBilgi.DonemAdiLong });
                        }
                        if (item.SablonParametreleri.Any(a => a == "@Link"))
                        {
                            mailParameterDtos.Add(new MailParameterDto { Key = "Link", Value = enstitu.SistemErisimAdresi + "/TIBasvuru/Index?IsDegerlendirme=" + item.UniqueID, IsLink = true });
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
                                    Value = tiAraRapor.DegerlendirmeSonucMailTarihi?.ToFormatDateAndTime()
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
                        if (isLinkOrSonuc) LogIslemleri.LogEkle("TIBasvuruAraRaporKomite", LogCrudType.Update, juri.ToJson());
                    }

                    mmMessage.IsSuccess = true;
                    mmMessage.MessageType = MsgTypeEnum.Success;

                }
            }
            catch (Exception ex)
            {
                var message = "";
                message = isLinkOrSonuc ? "Tez İzleme değerlendirmesi için Komite üyelerine değerlendirme davetiye linki mail olarak gönderilirken bir hata oluştu!" : "Tez İzleme değerlendirmesi sonucu Komite üyelerine mail olarak gönderilirken bir hata oluştu!";
                SistemBilgilendirmeBus.SistemBilgisiKaydet(message + "\r\n Hata:" + ex.ToExceptionMessage(), "Management/sendMailTIDegerlendirmeLink \r\n" + ex.ToExceptionStackTrace(), LogTipiEnum.Hata);
                //mmMessage.Title = "Hata";
                mmMessage.Messages.Add(message + "</br> Hata:" + ex.ToExceptionMessage());
                mmMessage.MessageType = MsgTypeEnum.Error;
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
                        SistemBilgilendirmeBus.SistemBilgisiKaydet(message, "TIK Başvuru Sil", LogTipiEnum.Kritik);
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
                        SistemBilgilendirmeBus.SistemBilgisiKaydet("Başka bir kullanıcıya ait Tez İzleme başvurusunu silmeye hakkınız yoktur! \r\n Silinmeye çalışılan Tez İzleme Başvuru ID:" + basvuru.TIBasvuruID + " \r\n Tez İzleme Başvuru Sahibi:" + basvuru.Kullanicilar.KullaniciAdi + " \r\n Başvuru Tarihi:" + basvuru.BasvuruTarihi.ToString(), "Başvuru Sil", LogTipiEnum.Saldırı);
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

         
        public static List<CmbIntDto> CmbTiAraRaporDurumListe(bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();

            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var arDurums = db.TIBasvuruAraRaporDurumlaris.Select(s => new CmbIntDto { Value = s.TIBasvuruAraRaporDurumID, Caption = s.TIBasvuruAraRaporDurumAdi }).ToList();
                dct.AddRange(arDurums);
            }
            dct.Add(new CmbIntDto { Value = 1000, Caption = "Başarılı Olanlar" });
            dct.Add(new CmbIntDto { Value = 1001, Caption = "Başarısız Olanlar" });
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
        public static List<CmbStringDto> CmbTiDonemListe(string enstituKod, bool bosSecimVar = false)
        {

            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var donems = db.TIBasvuruAraRapors.Select(s => new { s.DonemBaslangicYil, s.DonemID, s.Donemler.DonemAdi })
                    .Distinct().OrderByDescending(o => o.DonemBaslangicYil).ThenByDescending(t => t.DonemID).Select(s => new CmbStringDto
                    {
                        Value = s.DonemBaslangicYil + "" + s.DonemID,
                        Caption = s.DonemBaslangicYil + "/" + (s.DonemBaslangicYil + 1) + " " + s.DonemAdi

                    }).ToList();
                if (bosSecimVar) donems.Insert(0, new CmbStringDto { Value = null, Caption = "" });
                return donems;
            }
        }

        public static List<CmbStringDto> CmbTiDonemListeBasvuru(string enstituKod, bool bosSecimVar = false)
        {
            var cmbDonems = CmbTiDonemListe(enstituKod, false);
            if (!cmbDonems.Any())
            {
                var donem = DateTime.Now.ToAraRaporDonemBilgi();
                cmbDonems.Add(new CmbStringDto()
                {
                    Value = donem.BaslangicYil + "" + donem.DonemID,
                    Caption = donem.BaslangicYil + "/" + (donem.BaslangicYil + 1) + " " + donem.DonemAdi
                });
                if (bosSecimVar) cmbDonems.Insert(0, new CmbStringDto { Value = null, Caption = "" });
            }
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var donems = db.TIBasvuruAraRapors.Select(s => new { s.DonemBaslangicYil, s.DonemID, s.Donemler.DonemAdi })
                    .Distinct().OrderByDescending(o => o.DonemBaslangicYil).ThenByDescending(t => t.DonemID).Select(s => new CmbStringDto
                    {
                        Value = s.DonemBaslangicYil + "" + s.DonemID,
                        Caption = s.DonemBaslangicYil + "/" + (s.DonemBaslangicYil + 1) + " " + s.DonemAdi

                    }).ToList();
                if (bosSecimVar) donems.Insert(0, new CmbStringDto { Value = null, Caption = "" });
                return donems;
            }
        }

        public static List<CmbIntDto> GetCmbFilterTiAnabilimDallari(string enstituKod, bool bosSecimVar = false)
        {
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var yeterliAnabilimDaliIds = db.TIBasvurus
                    .Where(p => p.EnstituKod == enstituKod).Select(s => s.Programlar.AnabilimDaliID).Distinct().ToList();

                var anabilimDallaris = db.AnabilimDallaris.Where(p => yeterliAnabilimDaliIds.Contains(p.AnabilimDaliID))
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