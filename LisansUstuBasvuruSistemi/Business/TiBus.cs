using System;
using System.Collections.Generic;
using System.Linq;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.Helpers;
using LisansUstuBasvuruSistemi.Utilities.MailManager;
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

                if (!basvuru.TezDanismanID.HasValue || basvuru.TezDanismanID.Value <= 0)
                {
                    var ogrenci = basvuru.Kullanicilar;
                    if (ogrenci.YtuOgrencisi && basvuru.OgrenciNo == ogrenci.OgrenciNo &&
                        basvuru.ProgramKod == ogrenci.ProgramKod)
                    {
                        basvuru.TezDanismanID = ogrenci.DanismanID;
                        db.SaveChanges();
                    }
                }

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
                    model.TezDanismanID = null;
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
                    }
                    else
                    {
                        var basvuruAcikmi =
                            TiAyar.TiBasvurusuAcikmi.GetAyarTi(tiBasvuru.EnstituKod, "false").ToBoolean() ?? false;

                        if (!UserIdentity.Current.EnstituKods.Contains(tiBasvuru.EnstituKod) && kayitYetki && tiBasvuru.KullaniciID != UserIdentity.Current.Id)
                        {
                            msg.IsSuccess = false;
                            msg.Messages.Add("Bu Enstitü için Yetkili Değilsiniz.");
                            var message = $"Bu enstitüye ait Tez İzleme başvurusu güncellemeye yetkili değilsiniz!\r\n Tez İzleme Başvuru ID: {tiBasvuru.TIBasvuruID} \r\n Başvuru sahibi: {tiBasvuru.Kullanicilar.Ad + " " + tiBasvuru.Kullanicilar.Soyad} \r\n Başvuru Tarihi: " + tiBasvuru.BasvuruTarihi;
                            SistemBilgilendirmeBus.SistemBilgisiKaydet(message, ObjectExtensions.GetCurrentMethodPath(), BilgiTipiEnum.Saldırı);
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
                            SistemBilgilendirmeBus.SistemBilgisiKaydet("Başka bir kullanıcıya ait Tez İzleme başvurusu düzenlemeye hakkınız yoktur! \r\n Çağrılan Tez İzleme Başvuru ID:" + tiBasvuru.TIBasvuruID + " \r\n Başvuru Sahibi:" + tiBasvuru.Kullanicilar.KullaniciAdi, ObjectExtensions.GetCurrentMethodPath(), BilgiTipiEnum.Saldırı);
                        }
                    }
                }
                else
                {
                    msg.IsSuccess = TiAyar.TiBasvurusuAcikmi.GetAyarTi(enstituKod, "false").ToBoolean() ?? false;
                    if (kullaniciId.HasValue == false) kullaniciId = UserIdentity.Current.Id;
                    else if (kullaniciId != UserIdentity.Current.Id && UserIdentity.Current.IsAdmin == false)
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
                                var sondonemKayitOlmasiGerekenDersKodlari = TiAyar.TiSonDonemKayitOlunmasiGerekenDersKodlari.GetAyarTi(enstituKod);

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
                    }
                    else if (!TiAyar.TiBasvurusuAcikmi.GetAyarTi(basvuru.EnstituKod, "false").ToBoolean().Value && UserIdentity.Current.IsAdmin == false)
                    {
                        msg.IsSuccess = false;
                        msg.Messages.Add("Başvuru süreci dolduğundan başvuru üzerinden herhangi bir işlem yapılamaz!");

                    }
                    else if (kayitYetki == false && basvuru.KullaniciID != UserIdentity.Current.Id)
                    {
                        msg.IsSuccess = false;
                        msg.Messages.Add("Başka bir kullanıcıya ait başvuruyu silmeye hakkınız yoktur!");
                        SistemBilgilendirmeBus.SistemBilgisiKaydet("Başka bir kullanıcıya ait Tez İzleme başvurusunu silmeye hakkınız yoktur! \r\n Silinmeye çalışılan Tez İzleme Başvuru ID:" + basvuru.TIBasvuruID + " \r\n Tez İzleme Başvuru Sahibi:" + basvuru.Kullanicilar.KullaniciAdi + " \r\n Başvuru Tarihi:" + basvuru.BasvuruTarihi, ObjectExtensions.GetCurrentMethodPath(), BilgiTipiEnum.Saldırı);
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


        public static MmMessage SendMailTiBilgisi(int? tiBasvuruAraRaporId, int? srTalepId)
        {
            return MailSenderTi.SendMailTiBilgisi(tiBasvuruAraRaporId, srTalepId);
        }
        public static MmMessage SendMailTiDegerlendirmeLink(int tiBasvuruAraRaporId, Guid? uniqueId, bool isLinkOrSonuc)
        {
            return MailSenderTi.SendMailTiDegerlendirmeLink(tiBasvuruAraRaporId, uniqueId, isLinkOrSonuc);
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
            var cmbDonems = CmbTiDonemListe(enstituKod);
            if (!cmbDonems.Any())
            {
                var donem = DateTime.Now.ToTiAraRaporDonemBilgi();
                cmbDonems.Add(new CmbStringDto()
                {
                    Value = donem.BaslangicYil + "" + donem.DonemId,
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