
using BiskaUtil;
using LisansUstuBasvuruSistemi.Business;
using LisansUstuBasvuruSistemi.Raporlar.Mezuniyet;
using LisansUstuBasvuruSistemi.Raporlar.TezDanismanOneri;
using LisansUstuBasvuruSistemi.Raporlar.TezIzleme;
using LisansUstuBasvuruSistemi.Raporlar.TezIzlemeJuriOneri;
using LisansUstuBasvuruSistemi.Raporlar.TezOneriSavunma;
using LisansUstuBasvuruSistemi.Raporlar.Yeterlik;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.MenuAndRoles;
using LisansUstuBasvuruSistemi.Utilities.SystemSetting;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Web;
using System.Xml;

namespace LisansUstuBasvuruSistemi.Models
{
    public static class Management
    {

        public static string Tuz = "@BİSKAmcumu";
        public static int UniversiteYtuKod { get; } = 67;

        #region YokData 

        public static List<int> GetUniversiteTurKods()
        {
            var uTurList = new List<int>();

            uTurList.Add(1);// - DEVLET ÜNİVERSİTELERİ
                            //uTurList.Add(2);// - VAKIF ÜNİVERSİTELERİ
                            //uTurList.Add(3);// - 4702 SAYILI KANUN İLE VAKFA BAĞLI KURULAN MYO'LAR 
            uTurList.Add(4);// - ASKERİ EĞİTİM VEREN OKULLAR
            uTurList.Add(5);// - POLİS AKADEMİSİ
                            //uTurList.Add(6);// - KKTC'DE EĞİTİM VEREN ÜNİVERSİTELER 
                            //uTurList.Add(7);// - TÜRKİ CUMHURİYETLERİNDE BULUNAN ÜNİVERSİTELER
            uTurList.Add(8);// - TODAİE
            uTurList.Add(9);// - DİĞER(SAĞLIK BAKANLIĞI, ADALET BAKANLIĞI, VAKIF GUREBA VB.)

            return uTurList;
        }
        public static List<int> GetBirimTurKods()
        {
            var bTurList = new List<int>();
            bTurList.Add(0);//YÖK
            bTurList.Add(1);//-Üniversite
            bTurList.Add(2);//-Fakülte
            bTurList.Add(4);//-Enstitü
            bTurList.Add(5);//-Yüksekokul
            bTurList.Add(6);//-Meslek Yüksekokulu
            bTurList.Add(7);//-Eğitim Araştırma Hastanesi
            bTurList.Add(8);//-Uygulama ve Araştırma Merkezi
            bTurList.Add(9);//-Rektörlük
            bTurList.Add(10);//-Bölüm
            bTurList.Add(11);//-Anabilim Dalı
            bTurList.Add(12);//-Bilim Dalı
            bTurList.Add(13);//-Önlisans/Lisans Programı
            bTurList.Add(14);//-Sanat Dalı
            bTurList.Add(15);//-Anasanat Dalı
            bTurList.Add(16);//-Yüksek Lisans Programı
            bTurList.Add(17);//-Doktora Programı
            bTurList.Add(18);//-Sanatta Yeterlilik Programı
            bTurList.Add(19);//-Tıpta Uzmanlık Programı
            bTurList.Add(20);//-Önlisans Programı
            bTurList.Add(21);//-Disiplinlerarası Anabilim Dalı
            bTurList.Add(22);//-Disiplinlerarası Yüksek Lisans Programı
            bTurList.Add(23);//-Bütünleşik Doktora Programı
            bTurList.Add(24);//-Disiplinlerarası Doktora Programı


            return bTurList;
        }

        #endregion

        #region Yetki/Kimlik  
        public static void AddMessage(SystemInformation sis)
        {
            int? currid = UserIdentity.Current == null ? null : (int?)UserIdentity.Current.Id;
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                db.SistemBilgilendirmes.Add(new SistemBilgilendirme
                {
                    BilgiTipi = (byte)sis.InfoType,
                    Kategori = sis.Category,
                    Message = sis.Message,
                    StackTrace = sis.StackTrace,
                    IslemYapanIP = UserIdentity.Ip,
                    IslemTarihi = DateTime.Now,
                    IslemYapanID = currid
                });
                db.SaveChanges();
            }
        }
        public static string getRoot()
        {
            var root = HttpRuntime.AppDomainAppVirtualPath;
            root = root.EndsWith("/") ? root : root + "/";
            return root;
        }
        #endregion

        #region ComboData 
        public static List<KmMzOtoMail> getZmMailZamanData(bool? chkD = null)
        {

            var bsMList = new List<KmMzOtoMail>();
            bsMList.Add(new KmMzOtoMail { gID = 1, Checked = chkD ?? false, MailSablonTipID = null, ZamanTipID = ZamanTipi.Gun, Zaman = 1, Gonderildi = false, Aciklama = "Başvuru süreci bitimine 1 Gün kala Taslak durumundaki başvuruları bildir (Öğrenci)" });
            bsMList.Add(new KmMzOtoMail { gID = 2, Checked = chkD ?? false, MailSablonTipID = null, ZamanTipID = ZamanTipi.Gun, Zaman = 2, Gonderildi = false, Aciklama = "Başvuru süreci bitimine 2 Gün kala Taslak durumundaki başvuruları bildir (Öğrenci)" });
            bsMList.Add(new KmMzOtoMail { gID = 3, Checked = chkD ?? false, MailSablonTipID = MailSablonTipi.Mez_EykTarihineGoreSrAlinmali, ZamanTipID = ZamanTipi.Gun, Zaman = 10, Gonderildi = false, Aciklama = "SR talebi yapma süreci bitimine 10 Gün kala SR talebi yapmayanları bildir (Danışman,Öğrenci)" });
            bsMList.Add(new KmMzOtoMail { gID = 4, Checked = chkD ?? false, MailSablonTipID = MailSablonTipi.Mez_EykTarihineGoreSrAlinmali, ZamanTipID = ZamanTipi.Gun, Zaman = 5, Gonderildi = false, Aciklama = "SR talebi yapma süreci bitimine 5 Gün kala SR talebi yapmayanları bildir (Danışman,Öğrenci)" });
            bsMList.Add(new KmMzOtoMail { gID = 5, Checked = chkD ?? false, MailSablonTipID = MailSablonTipi.Mez_EykTarihineGoreSrAlinmadi, ZamanTipID = ZamanTipi.Gun, Zaman = -5, Gonderildi = false, Aciklama = "SR talebi yapma sürecini 5 Gün aşanları bildir (Enstitü)" });

            bsMList.Add(new KmMzOtoMail { gID = 10, Checked = chkD ?? false, MailSablonTipID = MailSablonTipi.Mez_SinavDegerlendirmeHatirlantmaDanismanDR, ZamanTipID = ZamanTipi.Gun, Zaman = -1, Gonderildi = false, Aciklama = "DR Sınav sonucu değerlendirmesi için hatırlatma (Danışman)" });
            bsMList.Add(new KmMzOtoMail { gID = 11, Checked = chkD ?? false, MailSablonTipID = MailSablonTipi.Mez_SinavDegerlendirmeHatirlantmaDanismanYL, ZamanTipID = ZamanTipi.Gun, Zaman = -1, Gonderildi = false, Aciklama = "YL Sınav sonucu değerlendirmesi için hatırlatma (Danışman)" });

            bsMList.Add(new KmMzOtoMail { gID = 6, Checked = chkD ?? false, MailSablonTipID = MailSablonTipi.Mez_TezSinavSonucuSistemeGirilmedi, ZamanTipID = ZamanTipi.Gun, Zaman = -5, Gonderildi = false, Aciklama = "Sınav olup sonucunu 5 gün içinde getirmeyenleri bildir (Estitü,Danışman,Öğrenci)" });
            bsMList.Add(new KmMzOtoMail { gID = 7, Checked = chkD ?? false, MailSablonTipID = MailSablonTipi.Mez_TezKontrolTezDosyasiYuklenmeli, ZamanTipID = ZamanTipi.Gun, Zaman = -7, Gonderildi = false, Aciklama = "Sınav olup Tez Dosyasını 7 gün içinde yüklemeyenleri bildir (Öğrenci)" });
            bsMList.Add(new KmMzOtoMail { gID = 8, Checked = chkD ?? false, MailSablonTipID = MailSablonTipi.Mez_CiltliTezTeslimYapilmali, ZamanTipID = ZamanTipi.Gun, Zaman = 5, Gonderildi = false, Aciklama = "Tez teslim tutanağını teslim tarihine 5 gün kala teslim etmeyenleri bildir (Danışman,Öğrenci)" });
            bsMList.Add(new KmMzOtoMail { gID = 9, Checked = chkD ?? false, MailSablonTipID = MailSablonTipi.Mez_CiltliTezTeslimYapilmadi, ZamanTipID = ZamanTipi.Gun, Zaman = -5, Gonderildi = false, Aciklama = "Tez teslim tutanağını teslim tarihini 5 gün geçirenleri bildir (Enstitü)" });

            return bsMList;
        }
        public static List<CmbIntDto> getbasvuruSurecleri(string EnstituKod, int BasvuruSurecTipID, bool bosSecimVar = false)
        {
            var lst = new List<CmbIntDto>();
            if (bosSecimVar) lst.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = (from s in db.BasvuruSurecs.Where(p => p.EnstituKod == EnstituKod && p.BasvuruSurecTipID == BasvuruSurecTipID)
                            join d in db.Donemlers on s.DonemID equals d.DonemID
                            orderby s.BaslangicTarihi descending
                            select new
                            {
                                s.BasvuruSurecID,
                                s.BaslangicYil,
                                s.BitisYil,
                                d.DonemAdi,
                                s.BaslangicTarihi,
                                s.BitisTarihi
                            }).ToList();
                foreach (var item in data)
                {
                    lst.Add(new CmbIntDto { Value = item.BasvuruSurecID, Caption = (item.BaslangicYil + "/" + item.BitisYil + " " + item.DonemAdi + " (" + item.BaslangicTarihi.ToDateString() + " - " + item.BitisTarihi.ToDateString() + ")") });
                }
            }
            return lst;
        }
        public static List<CmbIntDto> getbasvuruSurecleri(int KullaniciID, string EnstituKod, int BasvuruSurecTipID, bool bosSecimVar = false)
        {
            var lst = new List<CmbIntDto>();
            if (bosSecimVar) lst.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var qBasvuruSurecIDs = db.Basvurulars.Where(p => p.KullaniciID == KullaniciID && p.BasvuruSurec.EnstituKod == EnstituKod && p.BasvuruSurec.BasvuruSurecTipID == BasvuruSurecTipID && p.BasvurularTercihleris.Any(a => a.MulakatSonuclaris.Any(a2 => a2.MulakatSonucTipID == MulakatSonucTipi.Asil || a2.MulakatSonucTipID == MulakatSonucTipi.Yedek))).Select(s => s.BasvuruSurecID).Distinct().ToList();

                var data = (from s in db.BasvuruSurecs.Where(p => qBasvuruSurecIDs.Contains(p.BasvuruSurecID))
                            join d in db.Donemlers on s.DonemID equals d.DonemID
                            orderby s.BaslangicTarihi descending
                            select new
                            {
                                s.BasvuruSurecID,
                                s.BaslangicYil,
                                s.BitisYil,
                                d.DonemAdi,
                                s.BaslangicTarihi,
                                s.BitisTarihi
                            }).ToList();
                foreach (var item in data)
                {
                    lst.Add(new CmbIntDto { Value = item.BasvuruSurecID, Caption = (item.BaslangicYil + "/" + item.BitisYil + " " + item.DonemAdi + " (" + item.BaslangicTarihi.ToDateString() + " - " + item.BitisTarihi.ToDateString() + ")") });
                }
            }
            return lst;
        }





        public static List<CmbIntDto> cmbYetkiGruplari(bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });

            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.YetkiGruplaris.OrderBy(o => o.YetkiGrupAdi).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.YetkiGrupID, Caption = item.YetkiGrupAdi });
                }
            }
            return dct;

        }


        public static List<CmbIntDto> cmbUyruk(bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.Uyruklars.OrderBy(o => o.UyrukKod == 3009 ? 0 : 1).ThenBy(t => t.Ad).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.UyrukKod, Caption = (item.KisaAd != null && item.KisaAd != "" ? item.Ad + " (" + item.KisaAd + ")" : item.Ad) });
                }
            }
            return dct;

        }



        public static List<CmbIntDto> cmbUnvanlar(bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.Unvanlars.OrderBy(o => o.UnvanAdi).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.UnvanID, Caption = item.UnvanAdi });
                }
            }
            return dct;

        }
        public static List<CmbIntDto> cmbBirimler(bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.Birimlers.OrderBy(o => o.BirimAdi).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.BirimID, Caption = item.BirimAdi });
                }
            }
            return dct;

        }
        public static List<Birimler> getBirimler()
        {

            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                return db.Birimlers.OrderBy(o => o.BirimAdi).ToList();

            }
        }

        public static List<CmbIntDto> cmbCinsiyetler(bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.Cinsiyetlers.Where(p => p.IsAktif).OrderBy(o => o.CinsiyetAdi).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.CinsiyetID, Caption = item.CinsiyetAdi });
                }
            }
            return dct;

        }
        public static List<CmbIntDto> cmbBasvuruDurumListe(bool bosSecimVar = false, bool Tumu = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.BasvuruDurumlaris.Where(p => (Tumu ? true : p.BasvuranGorsun)).OrderBy(o => o.BasvuruDurumID).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.BasvuruDurumID, Caption = item.BasvuruDurumAdi });
                }
            }

            return dct;

        }





        public static List<CmbIntDto> cmbMulakatSonucTip(bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.MulakatSonucTipleris.Where(p => p.MulakatSonucTipID > 0).OrderBy(o => o.MulakatSonucTipID).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.MulakatSonucTipID, Caption = item.MulakatSonucTipAdi });
                }
            }

            return dct;

        }

        public static List<CmbBoolDto> cmbSinavBelgeTaahhut(bool bosSecimVar = false)
        {
            var dct = new List<CmbBoolDto>();
            if (bosSecimVar) dct.Add(new CmbBoolDto { Value = null, Caption = "" });
            dct.Add(new CmbBoolDto { Value = true, Caption = "Taahhüt Olan" });
            dct.Add(new CmbBoolDto { Value = false, Caption = "Taahhüt Olmayan" });
            return dct;
        }
        public static List<CmbBoolDto> cmbBolumOrOgrenci(bool bosSecimVar = false)
        {
            var dct = new List<CmbBoolDto>();
            if (bosSecimVar) dct.Add(new CmbBoolDto { Value = null, Caption = "" });
            dct.Add(new CmbBoolDto { Value = true, Caption = "Anabilim Dalları" });
            dct.Add(new CmbBoolDto { Value = false, Caption = "Öğrenciler" });
            return dct;

        }
        public static List<CmbIntDto> cmbKayitDurum()
        {
            var dct = new List<CmbIntDto>();
            dct.Add(new CmbIntDto { Value = null, Caption = "İşlem Görmeyenler" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.KayitDurumlaris.ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.KayitDurumID, Caption = item.KayitDurumAdi });
                }
            }
            return dct;

        }


        public static List<CmbIntDto> cmbOTYedekCarpanData(bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            dct.Add(new CmbIntDto { Value = 1, Caption = "Asil Öğrenci Kadar Yedek Öğrenci" });
            dct.Add(new CmbIntDto { Value = 2, Caption = "Asil Öğrencinin 2 Katı Kadar Yedek Öğrenci" });
            dct.Add(new CmbIntDto { Value = 3, Caption = "Asil Öğrencinin 3 Katı Kadar Yedek Öğrenci" });
            dct.Add(new CmbIntDto { Value = 4, Caption = "Asil Öğrencinin 4 Katı Kadar Yedek Öğrenci" });
            return dct;

        }



        public static List<CmbStringDto> GetDiller(bool bosSecimVar = false)
        {
            var dct = new List<CmbStringDto>();
            if (bosSecimVar) dct.Add(new CmbStringDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var diller = db.SistemDilleris.ToList();
                foreach (var item in diller)
                {
                    dct.Add(new CmbStringDto { Value = item.DilKodu, Caption = item.DilAdi });
                }
            }
            return dct;

        }


          

        public static List<CmbIntDto> cmbGetAktifUniversiteler(bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.Universitelers.OrderBy(o => o.Ad).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.UniversiteID, Caption = item.Ad + (item.KisaAd.IsNullOrWhiteSpace() ? "" : " (" + item.KisaAd + ")") });
                }
            }
            return dct;

        }
        public static List<CmbIntDto> cmbGetOgrenciBolumleri(string EnstituKod, bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.OgrenciBolumleris.Where(p => p.EnstituKod == EnstituKod && p.IsAktif).OrderBy(o => o.BolumAdi).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.OgrenciBolumID, Caption = item.BolumAdi });
                }
            }
            return dct;

        }
        public static List<CmbIntDto> cmbGetNotSistemleri(bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.NotSistemleris.Where(p => p.IsAktif).OrderBy(o => o.NotSistemID).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.NotSistemID, Caption = item.NotSistemAdi });
                }
            }
            return dct;

        }

        public static NotSistemleri getNotSistemi(int NotSistemID)
        {

            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                return db.NotSistemleris.Where(p => p.NotSistemID == NotSistemID).OrderBy(o => o.NotSistemID).FirstOrDefault();

            }

        }
        public static List<CmbStringDto> cmbGetAktifAnabilimDallariStr(string EnstituKod, bool bosSecimVar = false)
        {
            var dct = new List<CmbStringDto>();
            if (bosSecimVar) dct.Add(new CmbStringDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.AnabilimDallaris.Where(p => p.IsAktif && p.EnstituKod == EnstituKod).OrderBy(o => o.AnabilimDaliAdi).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbStringDto { Value = item.AnabilimDaliAdi, Caption = item.AnabilimDaliAdi });
                }
            }
            return dct;
        }
        public static List<CmbIntDto> cmbGetAktifAnabilimDallari(string EnstituKod, bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.AnabilimDallaris.Where(p => p.IsAktif && p.EnstituKod == EnstituKod).OrderBy(o => o.AnabilimDaliAdi).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.AnabilimDaliID, Caption = item.AnabilimDaliAdi });
                }
            }
            return dct;
        }
        public static List<CmbIntDto> cmbGetAktifBolumlerX(int OgrenimTipKod, int BasvuruSurecID, int? KullaniciTipID = null, bool SadeceKotasiOlanlar = true)
        {

            var dct = new List<CmbIntDto>();
            if (OgrenimTipKod == 0 && BasvuruSurecID == 0) return dct;
            if (!KullaniciTipID.HasValue) KullaniciTipID = UserIdentity.Current.KullaniciTipID;

            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var kulTip = db.KullaniciTipleris.Where(p => p.KullaniciTipID == KullaniciTipID).First();
                var basvurusrc = db.BasvuruSurecs.Where(p => p.BasvuruSurecID == BasvuruSurecID).First();
                var q = from p in db.Programlars
                        join k in db.BasvuruSurecKotalars.Where(p => p.BasvuruSurecID == BasvuruSurecID) on p.ProgramKod equals k.ProgramKod
                        join bl in db.AnabilimDallaris on p.AnabilimDaliKod equals bl.AnabilimDaliKod
                        join ot in db.OgrenimTipleris on new { k.BasvuruSurec.EnstituKod, k.OgrenimTipKod } equals new { ot.EnstituKod, ot.OgrenimTipKod }
                        where bl.EnstituKod == basvurusrc.EnstituKod && ot.OgrenimTipKod == OgrenimTipKod
                        group new { bl.AnabilimDaliID, bl.AnabilimDaliAdi, k.AlanIciKota, k.AlanDisiKota, k.OrtakKota, k.OrtakKotaSayisi } by new { bl.AnabilimDaliID, bl.AnabilimDaliAdi } into g1
                        orderby g1.Key.AnabilimDaliAdi
                        select new
                        {
                            g1.Key.AnabilimDaliID,
                            g1.Key.AnabilimDaliAdi,
                            cnt = g1.Where(p => p.AlanIciKota > 0 || p.AlanDisiKota > 0 || (p.OrtakKota && p.OrtakKotaSayisi > 0)).Count()

                        };
                if (SadeceKotasiOlanlar) q = q.Where(p => p.cnt > 0);
                var qdata = q.ToList();
                if (qdata.Count > 0) dct.Add(new CmbIntDto { Value = null, Caption = "" });

                foreach (var item in qdata)
                {
                    dct.Add(new CmbIntDto { Value = item.AnabilimDaliID, Caption = item.AnabilimDaliAdi + " " });
                }


            }
            return dct;
        }
        public static List<CmbStringDto> cmbGetAktifProgramlarX(int AnabilimDaliID, int _OgrenimTipKod, int BasvuruSurecID, int KullaniciTipID, bool SadeceKotasiOlanlar = true)
        {
            var dct = new List<CmbStringDto>();
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                bool Yerli = true;
                if (KullaniciTipID > 0) Yerli = db.KullaniciTipleris.Where(p => p.KullaniciTipID == KullaniciTipID).First().Yerli;
                var q = from p in db.Programlars
                        join k in db.BasvuruSurecKotalars.Where(p => p.BasvuruSurecID == BasvuruSurecID) on new { p.ProgramKod, OgrenimTipKod = _OgrenimTipKod } equals new { k.ProgramKod, k.OgrenimTipKod }
                        where p.AnabilimDaliID == AnabilimDaliID
                        group new { p.ProgramKod, p.ProgramAdi, k.AlanIciKota, k.AlanDisiKota, k.OrtakKota, k.OrtakKotaSayisi } by new { p.ProgramKod, p.ProgramAdi } into g1
                        orderby g1.Key.ProgramAdi
                        select new
                        {
                            g1.Key.ProgramKod,
                            g1.Key.ProgramAdi,
                            cnt = g1.Where(p => p.AlanIciKota > 0 || p.AlanDisiKota > 0 || (p.OrtakKota && p.OrtakKotaSayisi > 0)).Count()

                        };
                if (SadeceKotasiOlanlar) q = q.Where(p => p.cnt > 0);
                var qdata = q.ToList();
                if (qdata.Count > 0) dct.Add(new CmbStringDto { Value = null, Caption = "" });

                foreach (var item in qdata)
                {
                    dct.Add(new CmbStringDto { Value = item.ProgramKod, Caption = item.ProgramAdi + " " });
                }


            }
            return dct;
        }

        public static List<CmbIntDto> cmbGetSinavTipGruplari(bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.SinavTipGruplaris.OrderBy(o => o.SinavTipGrupAdi).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.SinavTipGrupID, Caption = item.SinavTipGrupAdi });
                }
            }
            return dct;

        } 
        public static List<CmbIntDto> cmbGetOzelNotTipleri(bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.OzelNotTipleris.OrderBy(o => o.OzelNotTipID).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.OzelNotTipID, Caption = item.OzelNotTipAdi });
                }
            }
            return dct;

        }

        public static List<CmbIntDto> cmbGetAktifSinavlar(string EnstituKodu, int? SinavTipGrupID = null, bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = (from s in db.SinavTipleris.Where(s2 => s2.EnstituKod == EnstituKodu && s2.IsAktif)
                            join stl in db.SinavTipleris on new { s.SinavTipID } equals new { stl.SinavTipID }
                            orderby stl.SinavAdi
                            select new
                            {
                                s.SinavTipID,
                                s.SinavTipKod,
                                s.SinavTipGrupID,
                                stl.SinavAdi
                            }).AsQueryable();
                if (SinavTipGrupID.HasValue) data = data.Where(p => p.SinavTipGrupID == SinavTipGrupID.Value);
                var qdata = data.ToList();
                foreach (var item in qdata)
                {
                    dct.Add(new CmbIntDto { Value = item.SinavTipID, Caption = item.SinavAdi });
                }
            }
            return dct;

        }
        public static List<CmbIntDto> cmbGetBSAktifSinavlar(string EnstituKodu, List<int> SinavTipGrupIDs, bool bosSecimVar = false)
        {
            SinavTipGrupIDs = SinavTipGrupIDs ?? new List<int>();
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var bssT = db.BasvuruSurecSinavTipleris.Where(p => p.EnstituKod == EnstituKodu).Select(s => s.SinavTipID).Distinct();
                var data = (from s in db.SinavTipleris.Where(s2 => s2.EnstituKod == EnstituKodu && bssT.Contains(s2.SinavTipID))

                            join stl in db.SinavTipleris on new { s.SinavTipID } equals new { stl.SinavTipID }
                            orderby stl.SinavAdi
                            select new
                            {
                                s.SinavTipKod,
                                s.SinavTipGrupID,
                                stl.SinavAdi
                            }).AsQueryable();
                if (SinavTipGrupIDs.Count > 0) data = data.Where(p => SinavTipGrupIDs.Contains(p.SinavTipGrupID));
                var qdata = data.ToList();
                foreach (var item in qdata)
                {
                    dct.Add(new CmbIntDto { Value = item.SinavTipKod, Caption = item.SinavAdi });
                }
            }
            return dct;

        }
        public static List<CmbIntDto> cmbGetdAktifSinavlar(List<CmbMultyTypeDto> filterM, int BasvuruSurecID, int SinavTipGrupID, bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = (from s in db.BasvuruSurecSinavTipleris.Where(s2 => s2.IsAktif && s2.BasvuruSurecID == BasvuruSurecID)
                            join stl in db.SinavTipleris on new { s.SinavTipID } equals new { stl.SinavTipID }
                            orderby stl.SinavAdi
                            where s.SinavTipGrupID == SinavTipGrupID
                            select new
                            {
                                s.SinavTipID,
                                s.SinavTipGrupID,
                                stl.SinavAdi
                            }).ToList();

                var qSinavOt = db.BasvuruSurecSinavTipleriOTNotAraliklaris.Where(p => p.BasvuruSurecID == BasvuruSurecID).ToList();
                var qJoin = (from s in qSinavOt
                             join fl in filterM on new { s.OgrenimTipKod, s.Ingilizce } equals new { OgrenimTipKod = fl.Value, Ingilizce = fl.ValueB }
                             group new { s.SinavTipID, s.OgrenimTipKod, s.IsGecerli, s.IsIstensin, s.Ingilizce, ProgramKod = fl.ValueS2 } by new { s.SinavTipID, s.OgrenimTipKod, s.IsGecerli, s.IsIstensin, s.Ingilizce, ProgramKod = fl.ValueS2 } into g1
                             select new
                             {
                                 g1.Key.SinavTipID,
                                 g1.Key.OgrenimTipKod,
                                 g1.Key.IsGecerli,
                                 g1.Key.IsIstensin,
                                 IsIstensin2 = db.BasvuruSurecSinavTipleriOTNotAraliklariGecersizProgramlars.Where(p => p.BasvuruSurecSinavTipleriOTNotAraliklari.BasvuruSurecID == BasvuruSurecID && p.BasvuruSurecSinavTipleriOTNotAraliklari.SinavTipID == g1.Key.SinavTipID && p.BasvuruSurecSinavTipleriOTNotAraliklari.OgrenimTipKod == g1.Key.OgrenimTipKod && p.BasvuruSurecSinavTipleriOTNotAraliklari.Ingilizce == g1.Key.Ingilizce && p.ProgramKod == g1.Key.ProgramKod).Any() == false,
                                 g1.Key.Ingilizce,
                                 g1.Key.ProgramKod,

                             }).ToList();

                var ProgramKods = filterM.Select(s => s.ValueS2).ToList();
                int inxBosR = 0;
                var otIDs = filterM.Select(s => s.Value).ToList();

                foreach (var item in data)
                {

                    var qnyGecersiz = qJoin.Where(p => p.SinavTipID == item.SinavTipID && (p.IsGecerli == false)).Any();
                    var qGecerliAmaIstenmesin = qJoin.Where(p => p.SinavTipID == item.SinavTipID && p.IsGecerli && (p.IsIstensin == false || p.IsIstensin2 == false)).Select(s => new { s.SinavTipID, s.OgrenimTipKod, s.ProgramKod }).Distinct().ToList();
                    var Istensin = qGecerliAmaIstenmesin.Count != ProgramKods.Count;
                    var sinavBVarmi = qJoin.Where(p => p.SinavTipID == item.SinavTipID && filterM.Any(a => a.Value == p.OgrenimTipKod && a.ValueB == p.Ingilizce)).Any();

                    if (!qnyGecersiz && Istensin && sinavBVarmi)
                    {
                        if (inxBosR == 0)
                        {
                            inxBosR++;
                            if (bosSecimVar) dct.Add(new CmbIntDto { Caption = "" });
                        }
                        dct.Add(new CmbIntDto { Value = item.SinavTipID, Caption = item.SinavAdi });
                    }

                }
            }
            return dct;

        }
        public static List<CmbDoubleDto> cmbGetSinavTipOzelNot(int SinavTipID, bool bosSecimVar = false)
        {
            var dct = new List<CmbDoubleDto>();
            if (bosSecimVar) dct.Add(new CmbDoubleDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.SinavTipleris.Where(p => p.SinavTipID == SinavTipID).SelectMany(s => s.SinavNotlaris).Select(s => new CmbDoubleDto
                {
                    Value = s.SinavNotDeger,
                    Caption = s.SinavNotAdi + " (Yüzlük karşılığı: " + s.SinavNotDeger + ")"
                }).OrderBy(o => o.Value).ToList();
                dct.AddRange(data);
            }
            return dct;

        }

        public static List<CmbIntDto> cmbGetYetkiliAnabilimDallari(bool bosSecimVar = false, string EnstituKod = "")
        {
            var EnstKods = UserIdentity.Current.EnstituKods ?? new List<string>();
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.AnabilimDallaris.Where(p => EnstKods.Contains(p.EnstituKod));
                if (EnstituKod.IsNullOrWhiteSpace() == false) data = data.Where(p => p.EnstituKod == EnstituKod);
                var data2 = data.OrderBy(o => o.AnabilimDaliAdi).ToList();
                foreach (var item in data2)
                {
                    dct.Add(new CmbIntDto { Value = item.AnabilimDaliID, Caption = item.AnabilimDaliAdi });
                }
            }
            return dct;

        } 
        public static List<CmbStringDto> cmbGetAktifProgramlar(bool bosSecimVar = false, int? AnabilimDaliID = 0)
        {

            var dct = new List<CmbStringDto>();
            if (bosSecimVar) dct.Add(new CmbStringDto { Value = "", Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.Programlars.Where(p => p.AnabilimDaliID == AnabilimDaliID && p.IsAktif).OrderBy(o => o.ProgramAdi).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbStringDto { Value = item.ProgramKod, Caption = item.ProgramAdi });
                }
            }
            return dct;
        }
        public static List<CmbStringDto> cmbGetAktifProgramlar(bool bosSecimVar = false)
        {
            var dct = new List<CmbStringDto>();
            if (bosSecimVar) dct.Add(new CmbStringDto { Value = "", Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.Programlars.Where(p =>  p.IsAktif).OrderBy(o => o.ProgramAdi).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbStringDto { Value = item.ProgramKod, Caption = item.ProgramAdi });
                }
            }
            return dct;
        }
        public static List<CmbStringDto> cmbGetAktifProgramlar(string EnstituKod, bool bosSecimVar = false, bool IsAbdShow = false)
        {
            var dct = new List<CmbStringDto>();
            if (bosSecimVar) dct.Add(new CmbStringDto { Value = "", Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.Programlars.Where(p =>p.AnabilimDallari.IsAktif && p.IsAktif && p.AnabilimDallari.EnstituKod == EnstituKod).OrderBy(o => o.ProgramAdi).ToList();
                foreach (var item in data)
                {
                    if (IsAbdShow)
                    {
                        var AbdL = item.AnabilimDallari;
                        dct.Add(new CmbStringDto { Value = item.ProgramKod, Caption = AbdL.AnabilimDaliAdi + " / " + item.ProgramAdi });
                    }
                    else
                    {
                        dct.Add(new CmbStringDto { Value = item.ProgramKod, Caption = item.ProgramAdi });
                    }
                }
            }
            return dct.OrderBy(o => o.Caption).ToList();
        }
        public static List<CmbStringDto> CmbGetBSTumProgramlar(int BasvuruSurecID, bool IsBolumOrOgrenci, List<int> OgrenimTipKods, bool IsBSonucOrMulakat)
        {
            var dct = new List<CmbStringDto>();

            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var BasvuruSureci = db.BasvuruSurecs.Where(p => p.BasvuruSurecID == BasvuruSurecID).First();
                var KullaniciProgramKods = UserBus.GetUserProgramKods(UserIdentity.Current.Id, BasvuruSureci.EnstituKod);

                if (IsBSonucOrMulakat)
                {
                    dct = (from Vw in db.vW_ProgramBasvuruSonucSayisal.Where(p => p.BasvuruSurecID == BasvuruSurecID && OgrenimTipKods.Contains(p.OgrenimTipKod))
                           where
                                 (IsBolumOrOgrenci ? true : (Vw.AIAsilCount > 0 || Vw.ADAsilCount > 0))
                                 && KullaniciProgramKods.Contains(Vw.ProgramKod)
                           select new
                           {
                               Vw.OgrenimTipKod,
                               //IsOnayliBasvuruVar = (IsBolumOrOgrenci ? true : (IsMulakatOrSonuc ? (Vw.ToplamBasvuru > 0) : (Vw.AIAsilCount > 0 || Vw.ADAsilCount > 0))),
                               Value = Vw.ProgramKod,
                               Caption = Vw.OgrenimTipAdi + " > " + Vw.ProgramAdi,

                           }).Select(s => new CmbStringDto
                           {
                               Value = s.Value,
                               Caption = s.Caption,

                           }).ToList();

                }
                else
                {
                    if (IsBolumOrOgrenci)
                    {
                        dct = (from s in db.BasvuruSurecKotalars.Where(p => p.BasvuruSurecID == BasvuruSurecID && OgrenimTipKods.Contains(p.OgrenimTipKod))
                               join pl in db.Programlars on s.ProgramKod equals pl.ProgramKod
                               join ot in db.OgrenimTipleris.Where(p => p.EnstituKod == BasvuruSureci.EnstituKod) on s.OgrenimTipKod equals ot.OgrenimTipKod
                               where KullaniciProgramKods.Contains(s.ProgramKod)
                               select new CmbStringDto
                               {
                                   Value = s.ProgramKod,
                                   Caption = ot.OgrenimTipAdi + " > " + pl.ProgramAdi,

                               }).OrderBy(o => o.Caption).ToList();
                    }
                    else
                    {
                        var BDurums = new List<int> { BasvuruDurumu.Onaylandı, BasvuruDurumu.Gonderildi };
                        dct = (from s in db.BasvurularTercihleris.Where(p => p.Basvurular.BasvuruSurecID == BasvuruSurecID && BDurums.Contains(p.Basvurular.BasvuruDurumID) && OgrenimTipKods.Contains(p.OgrenimTipKod))
                               join pl in db.Programlars on s.ProgramKod equals pl.ProgramKod
                               join ot in db.OgrenimTipleris.Where(p => p.EnstituKod == BasvuruSureci.EnstituKod) on s.OgrenimTipKod equals ot.OgrenimTipKod
                               where KullaniciProgramKods.Contains(s.ProgramKod)
                               select new CmbStringDto
                               {
                                   Value = s.ProgramKod,
                                   Caption = ot.OgrenimTipAdi + " > " + pl.ProgramAdi,

                               }).Distinct().OrderBy(o => o.Caption).ToList();
                    }


                }
            }
            return dct;

        }

         
        public static List<CmbIntDto> cmbGetAktifOgrenimTipleri(int BasvuruSurecID, bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {

                var otS = from bs in db.BasvuruSurecs.Where(p => p.BasvuruSurecID == BasvuruSurecID)
                          join s in db.BasvuruSurecOgrenimTipleris.Where(p => p.IsAktif) on bs.BasvuruSurecID equals s.BasvuruSurecID
                          join ot in db.OgrenimTipleris on new { bs.EnstituKod, s.OgrenimTipKod } equals new { ot.EnstituKod, ot.OgrenimTipKod }
                          select new
                          {
                              Kod = s.OgrenimTipKod,
                              Ad = ot.OgrenimTipAdi

                          };

                var qdata = otS.ToList();
                foreach (var item in qdata)
                {
                    dct.Add(new CmbIntDto { Value = item.Kod, Caption = item.Ad });
                }
            }
            return dct;

        } 



        public static List<CmbIntDto> cmbGetAktifAnketler(string EnstituKod, bool bosSecimVar = false, int? DahilAnketID = null)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.Ankets.Where(p => p.EnstituKod == EnstituKod && (p.IsAktif || p.AnketID == DahilAnketID)).OrderBy(o => o.AnketAdi).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.AnketID, Caption = item.AnketAdi });
                }
            }
            return dct;

        }
        public static List<CmbIntDto> cmbAktifOgrenimDurumu(bool bosSecimVar = false, bool? Aktif = true, int? HaricOgreniDurumID = null, bool? IsBasvurudaGozuksun = null, bool? IsHesapKayittaGozuksun = null)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var qData = db.OgrenimDurumlaris.AsQueryable();
                if (Aktif.HasValue) qData = qData.Where(p => p.IsAktif == Aktif.Value);
                if (HaricOgreniDurumID.HasValue) qData = qData.Where(p => p.OgrenimDurumID == HaricOgreniDurumID.Value);
                if (IsBasvurudaGozuksun.HasValue) qData = qData.Where(p => p.IsBasvurudaGozuksun == IsBasvurudaGozuksun.Value);
                if (IsHesapKayittaGozuksun.HasValue) qData = qData.Where(p => p.IsHesapKayittaGozuksun == IsHesapKayittaGozuksun.Value);
                var data = qData.OrderBy(o => o.OgrenimDurumAdi).ToList();
                foreach (var item in qData)
                {
                    dct.Add(new CmbIntDto { Value = item.OgrenimDurumID, Caption = item.OgrenimDurumAdi });
                }
            }
            return dct;

        }
        public static List<CmbIntDto> cmbAktifOgrenimDurumu2(bool bosSecimVar = false, bool? Aktif = true, int? HaricOgreniDurumID = null, bool? IsBasvurudaGozuksun = null, bool? IsHesapKayittaGozuksun = null)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var qData = db.OgrenimDurumlaris.AsQueryable();
                if (Aktif.HasValue) qData = qData.Where(p => p.IsAktif == Aktif.Value);
                if (HaricOgreniDurumID.HasValue) qData = qData.Where(p => p.OgrenimDurumID == HaricOgreniDurumID.Value);
                if (IsBasvurudaGozuksun.HasValue) qData = qData.Where(p => p.IsBasvurudaGozuksun == IsBasvurudaGozuksun.Value);
                if (IsHesapKayittaGozuksun.HasValue) qData = qData.Where(p => p.IsHesapKayittaGozuksun == IsHesapKayittaGozuksun.Value);
                var data = qData.OrderBy(o => o.OgrenimDurumAdi).ToList();
                foreach (var item in qData)
                {
                    dct.Add(new CmbIntDto { Value = item.OgrenimDurumID, Caption = item.OgrenimDurumAdi });
                }
            }
            return dct;

        }


        #endregion


        #region Data

         

        public static PersisWsDataModel getWsPersisOE(string term)
        {
            Ws_Persis.Service1SoapClient cl = new Ws_Persis.Service1SoapClient("Service1Soap");

            var data = cl.irfan_veri("irfan", "irfan123", term);
            var dataPers = (PersisWsDataModel)JsonConvert.DeserializeObject(data, typeof(PersisWsDataModel));

            return dataPers;
        }



        public static bool ResimBilgisiLazimOlanKayitVarMi(int KullaniciID)
        {
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                return db.Basvurulars.Where(p => p.KullaniciID == KullaniciID).Any() || db.MezuniyetBasvurularis.Any(a => a.KullaniciID == KullaniciID);
            }

        }







        public static int? getAktifBasvuruSurecID(string EnstituKod, int BasvuruSurecTipID, int? BasvuruSurecID = null, bool? IsMulakatDurum = null)
        {
            using (var db = new LisansustuBasvuruSistemiEntities())
            {

                var nowDate = DateTime.Now;
                var bf = db.BasvuruSurecs.Where(p => p.BasvuruSurecTipID == BasvuruSurecTipID && (p.BaslangicTarihi <= nowDate && p.BitisTarihi >= nowDate) && p.IsAktif && (p.EnstituKod == EnstituKod) && p.BasvuruSurecID == (BasvuruSurecID.HasValue ? BasvuruSurecID.Value : p.BasvuruSurecID));
                if (IsMulakatDurum.HasValue) bf = bf.Where(p => p.SonucGirisBaslangicTarihi.HasValue == IsMulakatDurum.Value);
                var qBf = bf.FirstOrDefault();
                int? ID = null;
                if (qBf != null) ID = qBf.BasvuruSurecID;
                return ID;
            }
        }

        public static int? getAktifTalepSurecID(string EnstituKod, int? TalepSurecID = null)
        {
            using (var db = new LisansustuBasvuruSistemiEntities())
            {

                var nowDate = DateTime.Now;
                var bf = db.TalepSurecleris.Where(p => (p.BaslangicTarihi <= nowDate && p.BitisTarihi >= nowDate) && p.IsAktif && (p.EnstituKod == EnstituKod) && p.TalepSurecID == (TalepSurecID.HasValue ? TalepSurecID.Value : p.TalepSurecID));
                var rTalepSurecID = bf.Select(s => s.TalepSurecID).FirstOrDefault();

                return rTalepSurecID > 0 ? rTalepSurecID : (int?)null;
            }
        }


         
        #endregion

        #region SendMails
        public static List<System.Net.Mail.Attachment> exportRaporPdf(int raporTipID, List<int?> DataID)
        {

            var mdl = new List<System.Net.Mail.Attachment>();

            var ms = new MemoryStream();

            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                if (raporTipID == RaporTipleri.MezuniyetBasvuruRaporu)
                {

                    var gd = Guid.NewGuid().ToString().Substr(0, 5);

                    var MezuniyetBasvurulariID = DataID[0].Value;
                    var rpr = new RprMezuniyetYayinSartiOnayiFormu(MezuniyetBasvurulariID);
                    rpr.CreateDocument();
                    rpr.DisplayName = "MezuniyetBasvuruFormu_" + gd + ".pdf";
                    rpr.ExportOptions.Pdf.Compressed = true;
                    ms = new MemoryStream();
                    rpr.ExportToPdf(ms);
                    ms.Seek(0, System.IO.SeekOrigin.Begin);
                    var attc = new System.Net.Mail.Attachment(ms, rpr.DisplayName, "application/pdf");
                    attc.ContentDisposition.ModificationDate = DateTime.Now;
                    mdl.Add(attc);
                }
                else if (raporTipID == RaporTipleri.MezuniyetTezTeslimFormu)
                {

                    var MBID = DataID[0].Value;
                    var IlkOrIkinci = DataID[1].Value;
                    var MB = db.MezuniyetBasvurularis.First(p => p.MezuniyetBasvurulariID == MBID);
                    var rpr = new RprMezuniyetTezTeslimFormu_FR0338(MB.RowID, IlkOrIkinci == 1);

                    rpr.CreateDocument();
                    rpr.DisplayName += ".pdf";
                    rpr.ExportOptions.Pdf.Compressed = true;
                    ms = new MemoryStream();
                    rpr.ExportToPdf(ms);
                    ms.Seek(0, System.IO.SeekOrigin.Begin);
                    var attc = new System.Net.Mail.Attachment(ms, rpr.DisplayName, "application/pdf");
                    attc.ContentDisposition.ModificationDate = DateTime.Now;
                    mdl.Add(attc);
                }
                else if (raporTipID == RaporTipleri.MezuniyetTezDuzeltmeVeJuriUyelerineTezTeslimTutanagi)
                {

                    var SRTalepID = DataID[0].Value;
                    var rpr = new RprMezuniyetTezDuzeltmeJuriUyelerineCiltliTezTeslimTutanagi_FR0329_FR0325(DataID[0].Value);

                    rpr.CreateDocument();
                    rpr.DisplayName += ".pdf";
                    rpr.ExportOptions.Pdf.Compressed = true;
                    ms = new MemoryStream();
                    rpr.ExportToPdf(ms);
                    ms.Seek(0, System.IO.SeekOrigin.Begin);
                    var attc = new System.Net.Mail.Attachment(ms, rpr.DisplayName, "application/pdf");
                    attc.ContentDisposition.ModificationDate = DateTime.Now;
                    mdl.Add(attc);
                }
                else if (raporTipID == RaporTipleri.MezuniyetTezSinavSonucFormu)
                {
                    var SRTalepID = DataID[0].Value;
                    var SrTalep = db.SRTalepleris.Where(p => p.SRTalepID == SRTalepID).First();

                    var rpr = new RprTezSinavSonucTutanagi_FR0342_FR0377(SrTalep.UniqueID.Value);

                    rpr.CreateDocument();
                    rpr.DisplayName += ".pdf";
                    rpr.ExportOptions.Pdf.Compressed = true;
                    ms = new MemoryStream();
                    rpr.ExportToPdf(ms);
                    ms.Seek(0, System.IO.SeekOrigin.Begin);
                    var attc = new System.Net.Mail.Attachment(ms, rpr.DisplayName, "application/pdf");
                    attc.ContentDisposition.ModificationDate = DateTime.Now;
                    mdl.Add(attc);
                }
                else if (raporTipID == RaporTipleri.MezuniyetJuriUyelerineTezTeslimFormu)
                {
                    var MezuniyetJuriOneriFormID = DataID[0].Value;
                    var rpr = new RprJuriUyelerineTezTeslimFormu_FR0341_FR0302(MezuniyetJuriOneriFormID);

                    rpr.CreateDocument();
                    rpr.DisplayName += ".pdf";
                    rpr.ExportOptions.Pdf.Compressed = true;
                    ms = new MemoryStream();
                    rpr.ExportToPdf(ms);
                    ms.Seek(0, System.IO.SeekOrigin.Begin);
                    var attc = new System.Net.Mail.Attachment(ms, rpr.DisplayName, "application/pdf");
                    attc.ContentDisposition.ModificationDate = DateTime.Now;
                    mdl.Add(attc);
                }
                else if (raporTipID == RaporTipleri.MezuniyetTezdenUretilenYayinlariDegerlendirmeFormu)
                {
                    var MezuniyetJuriOneriFormID = DataID[0].Value;
                    var MezuniyetJuriOneriFormuJuriID = DataID[1];
                    var rpr = new RprMezuniyetTezdenUretilenYayinlariDegerlendirmeFormu_FR0304(MezuniyetJuriOneriFormID, MezuniyetJuriOneriFormuJuriID);

                    rpr.CreateDocument();
                    rpr.DisplayName += ".pdf";
                    rpr.ExportOptions.Pdf.Compressed = true;
                    ms = new MemoryStream();
                    rpr.ExportToPdf(ms);
                    ms.Seek(0, System.IO.SeekOrigin.Begin);
                    var attc = new System.Net.Mail.Attachment(ms, rpr.DisplayName, "application/pdf");
                    attc.ContentDisposition.ModificationDate = DateTime.Now;
                    mdl.Add(attc);
                }
                else if (raporTipID == RaporTipleri.MezuniyetDoktoraTezDegerlendirmeFormu)
                {
                    var MezuniyetJuriOneriFormID = DataID[0].Value;
                    var MezuniyetJuriOneriFormuJuriID = DataID[1];
                    var rpr = new RprMezuniyetTezDegerlendirmeFormu_FR0303(MezuniyetJuriOneriFormID, MezuniyetJuriOneriFormuJuriID);

                    rpr.CreateDocument();
                    rpr.DisplayName += ".pdf";
                    rpr.ExportOptions.Pdf.Compressed = true;
                    ms = new MemoryStream();
                    rpr.ExportToPdf(ms);
                    ms.Seek(0, System.IO.SeekOrigin.Begin);
                    var attc = new System.Net.Mail.Attachment(ms, rpr.DisplayName, "application/pdf");
                    attc.ContentDisposition.ModificationDate = DateTime.Now;
                    mdl.Add(attc);
                }
                else if (raporTipID == RaporTipleri.MezuniyetTezKontrolFormu)
                {
                    var ID = DataID[0].Value;
                    var rpr = new RprMezuniyetTezKontrolFormu(null, ID);

                    rpr.CreateDocument();
                    rpr.DisplayName += ".pdf";
                    rpr.ExportOptions.Pdf.Compressed = true;
                    ms = new MemoryStream();
                    rpr.ExportToPdf(ms);
                    ms.Seek(0, System.IO.SeekOrigin.Begin);
                    var attc = new System.Net.Mail.Attachment(ms, rpr.DisplayName, "application/pdf");
                    attc.ContentDisposition.ModificationDate = DateTime.Now;
                    mdl.Add(attc);
                }
                else if (raporTipID == RaporTipleri.TezIzlemeDegerlendirmeFormu)
                {
                    var ID = DataID[0].Value;
                    var rpr = new RprTiDegerlendirmeFormu_FR0307(ID);

                    rpr.CreateDocument();
                    rpr.DisplayName += ".pdf";
                    var IsSwhoRaporDetay = false;
                    if (DataID.Count > 1) IsSwhoRaporDetay = DataID[1].ToIntToBooleanObj() ?? false;
                    if (IsSwhoRaporDetay)
                    {
                        var rpr2 = new RprTiDegerlendirmeFormuDetay_FR0307(ID);
                        rpr2.CreateDocument();
                        rpr2.DisplayName += ".pdf";
                        rpr.Pages.AddRange(rpr2.Pages);
                    }
                    rpr.ExportOptions.Pdf.Compressed = true;
                    ms = new MemoryStream();
                    rpr.ExportToPdf(ms);
                    ms.Seek(0, System.IO.SeekOrigin.Begin);
                    var attc = new System.Net.Mail.Attachment(ms, rpr.DisplayName, "application/pdf");
                    attc.ContentDisposition.ModificationDate = DateTime.Now;
                    mdl.Add(attc);
                }
                else if (raporTipID == RaporTipleri.TezOneriSavunmaFormu)
                {
                    var ID = DataID[0].Value;  
                    var rpr = new RprToSavunmaFormu_FR0348(ID);
                    rpr.CreateDocument(); 
                    rpr.DisplayName += ".pdf";
                    var isSwhoRaporDetay = false;
                    if (DataID.Count > 1) isSwhoRaporDetay = DataID[1].ToIntToBooleanObj() ?? false;
                    if (isSwhoRaporDetay)
                    {
                        var rpr2 = new RprToSavunmaFormuDetay_FR0348(ID);
                        rpr2.CreateDocument();
                        rpr2.DisplayName += ".pdf";
                        rpr.Pages.AddRange(rpr2.Pages);
                    }
                    rpr.ExportOptions.Pdf.Compressed = true;
                    ms = new MemoryStream();
                    rpr.ExportToPdf(ms);
                    ms.Seek(0, System.IO.SeekOrigin.Begin);
                    var attc = new System.Net.Mail.Attachment(ms, rpr.DisplayName, "application/pdf");
                    attc.ContentDisposition.ModificationDate = DateTime.Now;
                    mdl.Add(attc);
                }
                else if (raporTipID == RaporTipleri.TezDanismanOneriFormu)
                {
                    var ID = DataID[0].Value;
                    var rpr = new RprTezDanismaniOneriFormu_FR0347(ID);

                    rpr.CreateDocument();
                    rpr.DisplayName += ".pdf";
                    rpr.ExportOptions.Pdf.Compressed = true;
                    ms = new MemoryStream();
                    rpr.ExportToPdf(ms);
                    ms.Seek(0, System.IO.SeekOrigin.Begin);
                    var attc = new System.Net.Mail.Attachment(ms, rpr.DisplayName, "application/pdf");
                    attc.ContentDisposition.ModificationDate = DateTime.Now;
                    mdl.Add(attc);
                }
                else if (raporTipID == RaporTipleri.TezDanismanDegisiklikFormu)
                {
                    var ID = DataID[0].Value;
                    var rpr = new RprTezDanismaniDegisiklikFormu_FR0308(ID);

                    rpr.CreateDocument();
                    rpr.DisplayName += ".pdf";
                    rpr.ExportOptions.Pdf.Compressed = true;
                    ms = new MemoryStream();
                    rpr.ExportToPdf(ms);
                    ms.Seek(0, System.IO.SeekOrigin.Begin);
                    var attc = new System.Net.Mail.Attachment(ms, rpr.DisplayName, "application/pdf");
                    attc.ContentDisposition.ModificationDate = DateTime.Now;
                    mdl.Add(attc);
                }
                else if (raporTipID == RaporTipleri.TezEsDanismanOneriFormu)
                {
                    var ID = DataID[0].Value; // tdo es danisman id
                    var rpr = new RprTezEsDanismaniOneriFormu_FR0320(ID);

                    rpr.CreateDocument();
                    rpr.DisplayName += ".pdf";
                    rpr.ExportOptions.Pdf.Compressed = true;
                    ms = new MemoryStream();
                    rpr.ExportToPdf(ms);
                    ms.Seek(0, System.IO.SeekOrigin.Begin);
                    var attc = new System.Net.Mail.Attachment(ms, rpr.DisplayName, "application/pdf");
                    attc.ContentDisposition.ModificationDate = DateTime.Now;
                    mdl.Add(attc);
                }
                else if (raporTipID == RaporTipleri.YeterlikDoktoraSinavSonucFormu)
                {
                    var id = DataID[0].Value;
                    var rpr = new RprDrYeterlikSinavDegerlendirmeFormu_FR1227(id);
                    rpr.CreateDocument();
                    rpr.DisplayName += ".pdf";
                    rpr.ExportOptions.Pdf.Compressed = true;
                    ms = new MemoryStream();
                    rpr.ExportToPdf(ms);
                    ms.Seek(0, System.IO.SeekOrigin.Begin);
                    var attc = new System.Net.Mail.Attachment(ms, rpr.DisplayName, "application/pdf");
                    attc.ContentDisposition.ModificationDate = DateTime.Now;
                    mdl.Add(attc);
                }
                else if (raporTipID == RaporTipleri.TezIzlemeJuriOneriFormu)
                {
                    var id = DataID[0].Value; 

                    var rpr = new RprTijOneriFormu_FR0306(id);
                    rpr.CreateDocument();
                    rpr.DisplayName += ".pdf";
                    rpr.ExportOptions.Pdf.Compressed = true;
                    ms = new MemoryStream();
                    rpr.ExportToPdf(ms);
                    ms.Seek(0, System.IO.SeekOrigin.Begin);
                    var attc = new System.Net.Mail.Attachment(ms, rpr.DisplayName, "application/pdf");
                    attc.ContentDisposition.ModificationDate = DateTime.Now;
                    mdl.Add(attc);
                }
                else if (raporTipID == RaporTipleri.TezIzlemeJuriDegisiklikFormu)
                {
                    var id = DataID[0].Value;

                    var rpr = new RprTijDegisiklikFormu_FR1460(id);
                    rpr.CreateDocument();
                    rpr.DisplayName += ".pdf";
                    rpr.ExportOptions.Pdf.Compressed = true;
                    ms = new MemoryStream();
                    rpr.ExportToPdf(ms);
                    ms.Seek(0, System.IO.SeekOrigin.Begin);
                    var attc = new System.Net.Mail.Attachment(ms, rpr.DisplayName, "application/pdf");
                    attc.ContentDisposition.ModificationDate = DateTime.Now;
                    mdl.Add(attc);
                }
            }
            return mdl;
        }

        #endregion

        #region Extension 
        public static int PageSize = 15;
        #endregion
    }

}