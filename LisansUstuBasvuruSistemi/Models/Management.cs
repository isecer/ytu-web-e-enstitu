
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
        public static string GetRoot()
        {
            var root = HttpRuntime.AppDomainAppVirtualPath;
            root = root.EndsWith("/") ? root : root + "/";
            return root;
        }
        #endregion

        #region ComboData 
        public static List<KmMzOtoMail> GetZmMailZamanData(bool? isChecked = null)
        {

            var bsMList = new List<KmMzOtoMail>
            {
                new KmMzOtoMail { gID = 1, Checked = isChecked ?? false, MailSablonTipID = null, ZamanTipID = ZamanTipiEnum.Gun, Zaman = 1, Gonderildi = false, Aciklama = "Başvuru süreci bitimine 1 Gün kala Taslak durumundaki başvuruları bildir (Öğrenci)" },
                new KmMzOtoMail { gID = 2, Checked = isChecked ?? false, MailSablonTipID = null, ZamanTipID = ZamanTipiEnum.Gun, Zaman = 2, Gonderildi = false, Aciklama = "Başvuru süreci bitimine 2 Gün kala Taslak durumundaki başvuruları bildir (Öğrenci)" },
                new KmMzOtoMail { gID = 3, Checked = isChecked ?? false, MailSablonTipID = MailSablonTipiEnum.MezEykTarihineGoreSrAlinmali, ZamanTipID = ZamanTipiEnum.Gun, Zaman = 10, Gonderildi = false, Aciklama = "SR talebi yapma süreci bitimine 10 Gün kala SR talebi yapmayanları bildir (Danışman,Öğrenci)" },
                new KmMzOtoMail { gID = 4, Checked = isChecked ?? false, MailSablonTipID = MailSablonTipiEnum.MezEykTarihineGoreSrAlinmali, ZamanTipID = ZamanTipiEnum.Gun, Zaman = 5, Gonderildi = false, Aciklama = "SR talebi yapma süreci bitimine 5 Gün kala SR talebi yapmayanları bildir (Danışman,Öğrenci)" },
                new KmMzOtoMail { gID = 5, Checked = isChecked ?? false, MailSablonTipID = MailSablonTipiEnum.MezEykTarihineGoreSrAlinmadi, ZamanTipID = ZamanTipiEnum.Gun, Zaman = -5, Gonderildi = false, Aciklama = "SR talebi yapma sürecini 5 Gün aşanları bildir (Enstitü)" },
                new KmMzOtoMail { gID = 10, Checked = isChecked ?? false, MailSablonTipID = MailSablonTipiEnum.MezSinavDegerlendirmeHatirlantmaDanismanDr, ZamanTipID = ZamanTipiEnum.Gun, Zaman = -1, Gonderildi = false, Aciklama = "DR Sınav sonucu değerlendirmesi için hatırlatma (Danışman)" },
                new KmMzOtoMail { gID = 11, Checked = isChecked ?? false, MailSablonTipID = MailSablonTipiEnum.MezSinavDegerlendirmeHatirlantmaDanismanYl, ZamanTipID = ZamanTipiEnum.Gun, Zaman = -1, Gonderildi = false, Aciklama = "YL Sınav sonucu değerlendirmesi için hatırlatma (Danışman)" },
                new KmMzOtoMail { gID = 6, Checked = isChecked ?? false, MailSablonTipID = MailSablonTipiEnum.MezTezSinavSonucuSistemeGirilmedi, ZamanTipID = ZamanTipiEnum.Gun, Zaman = -5, Gonderildi = false, Aciklama = "Sınav olup sonucunu 5 gün içinde getirmeyenleri bildir (Estitü,Danışman,Öğrenci)" },
                new KmMzOtoMail { gID = 7, Checked = isChecked ?? false, MailSablonTipID = MailSablonTipiEnum.MezTezKontrolTezDosyasiYuklenmeli, ZamanTipID = ZamanTipiEnum.Gun, Zaman = -7, Gonderildi = false, Aciklama = "Sınav olup Tez Dosyasını 7 gün içinde yüklemeyenleri bildir (Öğrenci)" },
                new KmMzOtoMail { gID = 8, Checked = isChecked ?? false, MailSablonTipID = MailSablonTipiEnum.MezCiltliTezTeslimYapilmali, ZamanTipID = ZamanTipiEnum.Gun, Zaman = 5, Gonderildi = false, Aciklama = "Tez teslim tutanağını teslim tarihine 5 gün kala teslim etmeyenleri bildir (Danışman,Öğrenci)" },
                new KmMzOtoMail { gID = 9, Checked = isChecked ?? false, MailSablonTipID = MailSablonTipiEnum.MezCiltliTezTeslimYapilmadi, ZamanTipID = ZamanTipiEnum.Gun, Zaman = -5, Gonderildi = false, Aciklama = "Tez teslim tutanağını teslim tarihini 5 gün geçirenleri bildir (Enstitü)" }
            };

            return bsMList;
        }
        public static List<CmbIntDto> GetbasvuruSurecleri(string enstituKod, int basvuruSurecTipId, bool bosSecimVar = false)
        {
            var lst = new List<CmbIntDto>();
            if (bosSecimVar) lst.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = (from s in db.BasvuruSurecs.Where(p => p.EnstituKod == enstituKod && p.BasvuruSurecTipID == basvuruSurecTipId)
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
                    lst.Add(new CmbIntDto { Value = item.BasvuruSurecID, Caption = (item.BaslangicYil + "/" + item.BitisYil + " " + item.DonemAdi + " (" + item.BaslangicTarihi.ToFormatDate() + " - " + item.BitisTarihi.ToFormatDate() + ")") });
                }
            }
            return lst;
        }





        public static List<CmbIntDto> CmbYetkiGruplari(bool bosSecimVar = false)
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


        public static List<CmbIntDto> CmbUyruk(bool bosSecimVar = false)
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



        public static List<CmbIntDto> CmbUnvanlar(bool bosSecimVar = false)
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
        public static List<CmbIntDto> CmbBirimler(bool bosSecimVar = false)
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
        public static List<Birimler> GetBirimler()
        {

            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                return db.Birimlers.OrderBy(o => o.BirimAdi).ToList();

            }
        }

        public static List<CmbIntDto> CmbCinsiyetler(bool bosSecimVar = false)
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
        public static List<CmbIntDto> CmbBasvuruDurumListe(bool bosSecimVar = false, bool isTumu = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.BasvuruDurumlaris.Where(p => (isTumu || p.BasvuranGorsun)).OrderBy(o => o.BasvuruDurumID).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.BasvuruDurumID, Caption = item.BasvuruDurumAdi });
                }
            }

            return dct;

        }





        public static List<CmbIntDto> CmbMulakatSonucTip(bool bosSecimVar = false)
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

        public static List<CmbBoolDto> CmbSinavBelgeTaahhut(bool bosSecimVar = false)
        {
            var dct = new List<CmbBoolDto>();
            if (bosSecimVar) dct.Add(new CmbBoolDto { Value = null, Caption = "" });
            dct.Add(new CmbBoolDto { Value = true, Caption = "Taahhüt Olan" });
            dct.Add(new CmbBoolDto { Value = false, Caption = "Taahhüt Olmayan" });
            return dct;
        }
        public static List<CmbBoolDto> CmbBolumOrOgrenci(bool bosSecimVar = false)
        {
            var dct = new List<CmbBoolDto>();
            if (bosSecimVar) dct.Add(new CmbBoolDto { Value = null, Caption = "" });
            dct.Add(new CmbBoolDto { Value = true, Caption = "Anabilim Dalları" });
            dct.Add(new CmbBoolDto { Value = false, Caption = "Öğrenciler" });
            return dct;

        }
        public static List<CmbIntDto> CmbKayitDurum()
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


        public static List<CmbIntDto> CmbOtYedekCarpanData(bool bosSecimVar = false)
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



        public static List<CmbIntDto> CmbGetAktifUniversiteler(bool bosSecimVar = false, bool isYtuHaric = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.Universitelers.Where(p=>!isYtuHaric || p.UniversiteID!=UniversiteYtuKod).OrderBy(o => o.Ad).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.UniversiteID, Caption = item.Ad + (item.KisaAd.IsNullOrWhiteSpace() ? "" : " (" + item.KisaAd + ")") });
                }
            }
            return dct;

        }
        public static List<CmbIntDto> CmbGetAktifUniversiteler(bool bosSecimVar = false)
        { 
            return CmbGetAktifUniversiteler(bosSecimVar, false); 
        }
        public static List<CmbIntDto> CmbGetOgrenciBolumleri(string enstituKod, bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.OgrenciBolumleris.Where(p => p.EnstituKod == enstituKod && p.IsAktif).OrderBy(o => o.BolumAdi).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.OgrenciBolumID, Caption = item.BolumAdi });
                }
            }
            return dct;

        }
        public static List<CmbIntDto> CmbGetNotSistemleri(bool bosSecimVar = false)
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

        public static NotSistemleri GetNotSistemi(int notSistemId)
        {

            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                return db.NotSistemleris.Where(p => p.NotSistemID == notSistemId).OrderBy(o => o.NotSistemID).FirstOrDefault();

            }

        }
        public static List<CmbStringDto> CmbGetAktifAnabilimDallariStr(string enstituKod, bool bosSecimVar = false)
        {
            var dct = new List<CmbStringDto>();
            if (bosSecimVar) dct.Add(new CmbStringDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.AnabilimDallaris.Where(p => p.IsAktif && p.EnstituKod == enstituKod).OrderBy(o => o.AnabilimDaliAdi).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbStringDto { Value = item.AnabilimDaliAdi, Caption = item.AnabilimDaliAdi });
                }
            }
            return dct;
        }
        public static List<CmbIntDto> CmbGetAktifAnabilimDallari(string enstituKod, bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.AnabilimDallaris.Where(p => p.IsAktif && p.EnstituKod == enstituKod).OrderBy(o => o.AnabilimDaliAdi).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.AnabilimDaliID, Caption = item.AnabilimDaliAdi });
                }
            }
            return dct;
        }
        public static List<CmbStringDto> CmbGetAktifProgramlarX(int anabilimDaliId, int ogrenimTipKod, int basvuruSurecId, int kullaniciTipId, bool sadeceKotasiOlanlar = true)
        {
            var dct = new List<CmbStringDto>();
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                bool isYerli = true;
                if (kullaniciTipId > 0) isYerli = db.KullaniciTipleris.First(p => p.KullaniciTipID == kullaniciTipId).Yerli;
                var q = from p in db.Programlars
                        join k in db.BasvuruSurecKotalars.Where(p => p.BasvuruSurecID == basvuruSurecId) on new { p.ProgramKod, OgrenimTipKod = ogrenimTipKod } equals new { k.ProgramKod, k.OgrenimTipKod }
                        where p.AnabilimDaliID == anabilimDaliId
                        group new { p.ProgramKod, p.ProgramAdi, k.AlanIciKota, k.AlanDisiKota, k.OrtakKota, k.OrtakKotaSayisi } by new { p.ProgramKod, p.ProgramAdi } into g1
                        orderby g1.Key.ProgramAdi
                        select new
                        {
                            g1.Key.ProgramKod,
                            g1.Key.ProgramAdi,
                            cnt = g1.Count(p => p.AlanIciKota > 0 || p.AlanDisiKota > 0 || (p.OrtakKota && p.OrtakKotaSayisi > 0))

                        };
                if (sadeceKotasiOlanlar) q = q.Where(p => p.cnt > 0);
                var qdata = q.ToList();
                if (qdata.Count > 0) dct.Add(new CmbStringDto { Value = null, Caption = "" });

                foreach (var item in qdata)
                {
                    dct.Add(new CmbStringDto { Value = item.ProgramKod, Caption = item.ProgramAdi + " " });
                }


            }
            return dct;
        }

        public static List<CmbIntDto> CmbGetSinavTipGruplari(bool bosSecimVar = false)
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
        public static List<CmbIntDto> CmbGetOzelNotTipleri(bool bosSecimVar = false)
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

        public static List<CmbIntDto> CmbGetAktifSinavlar(string enstituKodu, int? sinavTipGrupId = null, bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = (from s in db.SinavTipleris.Where(s2 => s2.EnstituKod == enstituKodu && s2.IsAktif)
                            join stl in db.SinavTipleris on new { s.SinavTipID } equals new { stl.SinavTipID }
                            orderby stl.SinavAdi
                            select new
                            {
                                s.SinavTipID,
                                s.SinavTipKod,
                                s.SinavTipGrupID,
                                stl.SinavAdi
                            }).AsQueryable();
                if (sinavTipGrupId.HasValue) data = data.Where(p => p.SinavTipGrupID == sinavTipGrupId.Value);
                var qdata = data.ToList();
                foreach (var item in qdata)
                {
                    dct.Add(new CmbIntDto { Value = item.SinavTipID, Caption = item.SinavAdi });
                }
            }
            return dct;

        }
        public static List<CmbIntDto> CmbGetBsAktifSinavlar(string enstituKodu, List<int> sinavTipGrupIDs, bool bosSecimVar = false)
        {
            sinavTipGrupIDs = sinavTipGrupIDs ?? new List<int>();
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var bssT = db.BasvuruSurecSinavTipleris.Where(p => p.EnstituKod == enstituKodu).Select(s => s.SinavTipID).Distinct();
                var data = (from s in db.SinavTipleris.Where(s2 => s2.EnstituKod == enstituKodu && bssT.Contains(s2.SinavTipID))

                            join stl in db.SinavTipleris on new { s.SinavTipID } equals new { stl.SinavTipID }
                            orderby stl.SinavAdi
                            select new
                            {
                                s.SinavTipKod,
                                s.SinavTipGrupID,
                                stl.SinavAdi
                            }).AsQueryable();
                if (sinavTipGrupIDs.Count > 0) data = data.Where(p => sinavTipGrupIDs.Contains(p.SinavTipGrupID));
                var qdata = data.ToList();
                foreach (var item in qdata)
                {
                    dct.Add(new CmbIntDto { Value = item.SinavTipKod, Caption = item.SinavAdi });
                }
            }
            return dct;

        }
        public static List<CmbIntDto> CmbGetdAktifSinavlar(List<CmbMultyTypeDto> filterM, int basvuruSurecId, int sinavTipGrupId, bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = (from s in db.BasvuruSurecSinavTipleris.Where(s2 => s2.IsAktif && s2.BasvuruSurecID == basvuruSurecId)
                            join stl in db.SinavTipleris on new { s.SinavTipID } equals new { stl.SinavTipID }
                            orderby stl.SinavAdi
                            where s.SinavTipGrupID == sinavTipGrupId
                            select new
                            {
                                s.SinavTipID,
                                s.SinavTipGrupID,
                                stl.SinavAdi
                            }).ToList();

                var qSinavOt = db.BasvuruSurecSinavTipleriOTNotAraliklaris.Where(p => p.BasvuruSurecID == basvuruSurecId).ToList();
                var qJoin = (from s in qSinavOt
                             join fl in filterM on new { s.OgrenimTipKod, s.Ingilizce } equals new { OgrenimTipKod = fl.Value, Ingilizce = fl.ValueB }
                             group new { s.SinavTipID, s.OgrenimTipKod, s.IsGecerli, s.IsIstensin, s.Ingilizce, ProgramKod = fl.ValueS2 } by new { s.SinavTipID, s.OgrenimTipKod, s.IsGecerli, s.IsIstensin, s.Ingilizce, ProgramKod = fl.ValueS2 } into g1
                             select new
                             {
                                 g1.Key.SinavTipID,
                                 g1.Key.OgrenimTipKod,
                                 g1.Key.IsGecerli,
                                 g1.Key.IsIstensin,
                                 IsIstensin2 = db.BasvuruSurecSinavTipleriOTNotAraliklariGecersizProgramlars.Any(p => p.BasvuruSurecSinavTipleriOTNotAraliklari.BasvuruSurecID == basvuruSurecId && p.BasvuruSurecSinavTipleriOTNotAraliklari.SinavTipID == g1.Key.SinavTipID && p.BasvuruSurecSinavTipleriOTNotAraliklari.OgrenimTipKod == g1.Key.OgrenimTipKod && p.BasvuruSurecSinavTipleriOTNotAraliklari.Ingilizce == g1.Key.Ingilizce && p.ProgramKod == g1.Key.ProgramKod) == false,
                                 g1.Key.Ingilizce,
                                 g1.Key.ProgramKod,

                             }).ToList();

                var programKods = filterM.Select(s => s.ValueS2).ToList();
                int inxBosR = 0;
                var otIDs = filterM.Select(s => s.Value).ToList();

                foreach (var item in data)
                {

                    var qnyGecersiz = qJoin.Any(p => p.SinavTipID == item.SinavTipID && (p.IsGecerli == false));
                    var qGecerliAmaIstenmesin = qJoin.Where(p => p.SinavTipID == item.SinavTipID && p.IsGecerli && (p.IsIstensin == false || p.IsIstensin2 == false)).Select(s => new { s.SinavTipID, s.OgrenimTipKod, s.ProgramKod }).Distinct().ToList();
                    var isIstensin = qGecerliAmaIstenmesin.Count != programKods.Count;
                    var sinavBVarmi = qJoin.Any(p => p.SinavTipID == item.SinavTipID && filterM.Any(a => a.Value == p.OgrenimTipKod && a.ValueB == p.Ingilizce));

                    if (!qnyGecersiz && isIstensin && sinavBVarmi)
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
        public static List<CmbDoubleDto> CmbGetSinavTipOzelNot(int sinavTipId, bool bosSecimVar = false)
        {
            var dct = new List<CmbDoubleDto>();
            if (bosSecimVar) dct.Add(new CmbDoubleDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.SinavTipleris.Where(p => p.SinavTipID == sinavTipId).SelectMany(s => s.SinavNotlaris).Select(s => new CmbDoubleDto
                {
                    Value = s.SinavNotDeger,
                    Caption = s.SinavNotAdi + " (Yüzlük karşılığı: " + s.SinavNotDeger + ")"
                }).OrderBy(o => o.Value).ToList();
                dct.AddRange(data);
            }
            return dct;

        }

        public static List<CmbIntDto> CmbGetYetkiliAnabilimDallari(bool bosSecimVar = false, string enstituKod = "")
        {
            var enstKods = UserIdentity.Current.EnstituKods ?? new List<string>();
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.AnabilimDallaris.Where(p => enstKods.Contains(p.EnstituKod));
                if (enstituKod.IsNullOrWhiteSpace() == false) data = data.Where(p => p.EnstituKod == enstituKod);
                var data2 = data.OrderBy(o => o.AnabilimDaliAdi).ToList();
                foreach (var item in data2)
                {
                    dct.Add(new CmbIntDto { Value = item.AnabilimDaliID, Caption = item.AnabilimDaliAdi });
                }
            }
            return dct;

        }
        public static List<CmbStringDto> CmbGetAktifProgramlar(bool bosSecimVar = false, int? anabilimDaliId = 0)
        {

            var dct = new List<CmbStringDto>();
            if (bosSecimVar) dct.Add(new CmbStringDto { Value = "", Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.Programlars.Where(p => p.AnabilimDaliID == anabilimDaliId && p.IsAktif).OrderBy(o => o.ProgramAdi).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbStringDto { Value = item.ProgramKod, Caption = item.ProgramAdi });
                }
            }
            return dct;
        }
        public static List<CmbStringDto> CmbGetAktifProgramlar(bool bosSecimVar = false)
        {
            var dct = new List<CmbStringDto>();
            if (bosSecimVar) dct.Add(new CmbStringDto { Value = "", Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.Programlars.Where(p => p.IsAktif).OrderBy(o => o.ProgramAdi).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbStringDto { Value = item.ProgramKod, Caption = item.ProgramAdi });
                }
            }
            return dct;
        }
        public static List<CmbStringDto> CmbGetAktifProgramlar(string enstituKod, bool bosSecimVar = false, bool isAbdShow = false)
        {
            var dct = new List<CmbStringDto>();
            if (bosSecimVar) dct.Add(new CmbStringDto { Value = "", Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.Programlars.Where(p => p.AnabilimDallari.IsAktif && p.IsAktif && p.AnabilimDallari.EnstituKod == enstituKod).OrderBy(o => o.ProgramAdi).ToList();
                foreach (var item in data)
                {
                    if (isAbdShow)
                    {
                        var abdL = item.AnabilimDallari;
                        dct.Add(new CmbStringDto { Value = item.ProgramKod, Caption = abdL.AnabilimDaliAdi + " / " + item.ProgramAdi });
                    }
                    else
                    {
                        dct.Add(new CmbStringDto { Value = item.ProgramKod, Caption = item.ProgramAdi });
                    }
                }
            }
            return dct.OrderBy(o => o.Caption).ToList();
        }
        public static List<CmbStringDto> CmbGetBsTumProgramlar(int basvuruSurecId, bool isBolumOrOgrenci, List<int> ogrenimTipKods, bool isBSonucOrMulakat)
        {
            List<CmbStringDto> dct;

            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var basvuruSureci = db.BasvuruSurecs.First(p => p.BasvuruSurecID == basvuruSurecId);
                var kullaniciProgramKods = UserBus.GetUserProgramKods(UserIdentity.Current.Id, basvuruSureci.EnstituKod);

                if (isBSonucOrMulakat)
                {
                    dct = (from vw in db.vW_ProgramBasvuruSonucSayisal.Where(p => p.BasvuruSurecID == basvuruSurecId && ogrenimTipKods.Contains(p.OgrenimTipKod))
                           where
                                 (isBolumOrOgrenci || (vw.AIAsilCount > 0 || vw.ADAsilCount > 0))
                                 && kullaniciProgramKods.Contains(vw.ProgramKod)
                           select new
                           {
                               vw.OgrenimTipKod,
                               //IsOnayliBasvuruVar = (IsBolumOrOgrenci ? true : (IsMulakatOrSonuc ? (Vw.ToplamBasvuru > 0) : (Vw.AIAsilCount > 0 || Vw.ADAsilCount > 0))),
                               Value = vw.ProgramKod,
                               Caption = vw.OgrenimTipAdi + " > " + vw.ProgramAdi,

                           }).Select(s => new CmbStringDto
                           {
                               Value = s.Value,
                               Caption = s.Caption,

                           }).ToList();

                }
                else
                {
                    if (isBolumOrOgrenci)
                    {
                        dct = (from s in db.BasvuruSurecKotalars.Where(p => p.BasvuruSurecID == basvuruSurecId && ogrenimTipKods.Contains(p.OgrenimTipKod))
                               join pl in db.Programlars on s.ProgramKod equals pl.ProgramKod
                               join ot in db.OgrenimTipleris.Where(p => p.EnstituKod == basvuruSureci.EnstituKod) on s.OgrenimTipKod equals ot.OgrenimTipKod
                               where kullaniciProgramKods.Contains(s.ProgramKod)
                               select new CmbStringDto
                               {
                                   Value = s.ProgramKod,
                                   Caption = ot.OgrenimTipAdi + " > " + pl.ProgramAdi,

                               }).OrderBy(o => o.Caption).ToList();
                    }
                    else
                    {
                        var bDurums = new List<int> { BasvuruDurumuEnum.Onaylandı, BasvuruDurumuEnum.Gonderildi };
                        dct = (from s in db.BasvurularTercihleris.Where(p => p.Basvurular.BasvuruSurecID == basvuruSurecId && bDurums.Contains(p.Basvurular.BasvuruDurumID) && ogrenimTipKods.Contains(p.OgrenimTipKod))
                               join pl in db.Programlars on s.ProgramKod equals pl.ProgramKod
                               join ot in db.OgrenimTipleris.Where(p => p.EnstituKod == basvuruSureci.EnstituKod) on s.OgrenimTipKod equals ot.OgrenimTipKod
                               where kullaniciProgramKods.Contains(s.ProgramKod)
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


        public static List<CmbIntDto> CmbGetAktifOgrenimTipleri(int basvuruSurecId, bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {

                var otS = from bs in db.BasvuruSurecs.Where(p => p.BasvuruSurecID == basvuruSurecId)
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



        public static List<CmbIntDto> CmbGetAktifAnketler(string enstituKod, bool bosSecimVar = false, int? dahilAnketId = null)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.Ankets.Where(p => p.EnstituKod == enstituKod && (p.IsAktif || p.AnketID == dahilAnketId)).OrderBy(o => o.AnketAdi).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.AnketID, Caption = item.AnketAdi });
                }
            }
            return dct;

        }
        public static List<CmbIntDto> CmbAktifOgrenimDurumu(bool bosSecimVar = false, bool? isAktif = true, int? haricOgreniDurumId = null, bool? isBasvurudaGozuksun = null, bool? IsHesapKayittaGozuksun = null)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var qData = db.OgrenimDurumlaris.AsQueryable();
                if (isAktif.HasValue) qData = qData.Where(p => p.IsAktif == isAktif.Value);
                if (haricOgreniDurumId.HasValue) qData = qData.Where(p => p.OgrenimDurumID == haricOgreniDurumId.Value);
                if (isBasvurudaGozuksun.HasValue) qData = qData.Where(p => p.IsBasvurudaGozuksun == isBasvurudaGozuksun.Value);
                if (IsHesapKayittaGozuksun.HasValue) qData = qData.Where(p => p.IsHesapKayittaGozuksun == IsHesapKayittaGozuksun.Value);
                var data = qData.OrderBy(o => o.OgrenimDurumAdi).ToList();
                foreach (var item in qData)
                {
                    dct.Add(new CmbIntDto { Value = item.OgrenimDurumID, Caption = item.OgrenimDurumAdi });
                }
            }
            return dct;

        }
        public static List<CmbIntDto> CmbAktifOgrenimDurumu2(bool bosSecimVar = false, bool? isAktif = true, int? haricOgreniDurumId = null, bool? isBasvurudaGozuksun = null, bool? IsHesapKayittaGozuksun = null)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var qData = db.OgrenimDurumlaris.AsQueryable();
                if (isAktif.HasValue) qData = qData.Where(p => p.IsAktif == isAktif.Value);
                if (haricOgreniDurumId.HasValue) qData = qData.Where(p => p.OgrenimDurumID == haricOgreniDurumId.Value);
                if (isBasvurudaGozuksun.HasValue) qData = qData.Where(p => p.IsBasvurudaGozuksun == isBasvurudaGozuksun.Value);
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



        public static PersisWsDataModel GetWsPersisOe(string term)
        {
            Ws_Persis.Service1SoapClient cl = new Ws_Persis.Service1SoapClient("Service1Soap");

            var data = cl.irfan_veri("irfan", "irfan123", term);
            var dataPers = (PersisWsDataModel)JsonConvert.DeserializeObject(data, typeof(PersisWsDataModel));

            return dataPers;
        }



        public static bool ResimBilgisiLazimOlanKayitVarMi(int kullaniciId)
        {
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                return db.Basvurulars.Any(p => p.KullaniciID == kullaniciId) || db.MezuniyetBasvurularis.Any(a => a.KullaniciID == kullaniciId);
            }

        }







        public static int? GetAktifBasvuruSurecId(string enstituKod, int basvuruSurecTipId, int? basvuruSurecId = null, bool? isMulakatDurum = null)
        {
            using (var db = new LisansustuBasvuruSistemiEntities())
            {

                var nowDate = DateTime.Now;
                var bf = db.BasvuruSurecs.Where(p => p.BasvuruSurecTipID == basvuruSurecTipId && (p.BaslangicTarihi <= nowDate && p.BitisTarihi >= nowDate) && p.IsAktif && (p.EnstituKod == enstituKod) && p.BasvuruSurecID == (basvuruSurecId ?? p.BasvuruSurecID));
                if (isMulakatDurum.HasValue) bf = bf.Where(p => p.SonucGirisBaslangicTarihi.HasValue == isMulakatDurum.Value);
                var qBf = bf.FirstOrDefault();
                return qBf?.BasvuruSurecID;
            }
        }

        public static int? GetAktifTalepSurecId(string enstituKod, int? talepSurecId = null)
        {
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var nowDate = DateTime.Now;
                var bf = db.TalepSurecleris.FirstOrDefault(p => (p.BaslangicTarihi <= nowDate && p.BitisTarihi >= nowDate) && p.IsAktif && (p.EnstituKod == enstituKod) && p.TalepSurecID == (talepSurecId ?? p.TalepSurecID));

                return bf?.TalepSurecID;
            }
        }



        #endregion

        #region SendMails
        public static List<System.Net.Mail.Attachment> ExportRaporPdf(int raporTipId, List<int?> dataId)
        {

            var mdl = new List<System.Net.Mail.Attachment>();

            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                MemoryStream ms;
                switch (raporTipId)
                {
                    case RaporTipiEnum.MezuniyetBasvuruRaporu:
                        {
                            var gd = Guid.NewGuid().ToString().Substring(0, 5);

                            var mezuniyetBasvurulariId = dataId[0].Value;
                            var rpr = new RprMezuniyetYayinSartiOnayiFormu(mezuniyetBasvurulariId);
                            rpr.CreateDocument();
                            rpr.DisplayName = "MezuniyetBasvuruFormu_" + gd + ".pdf";
                            rpr.ExportOptions.Pdf.Compressed = true;
                            ms = new MemoryStream();
                            rpr.ExportToPdf(ms);
                            ms.Seek(0, System.IO.SeekOrigin.Begin);
                            var attc = new System.Net.Mail.Attachment(ms, rpr.DisplayName, "application/pdf");
                            attc.ContentDisposition.ModificationDate = DateTime.Now;
                            mdl.Add(attc);
                            break;
                        }
                    case RaporTipiEnum.MezuniyetTezTeslimFormu:
                        {
                            var mbid = dataId[0].Value;
                            var ilkOrIkinci = dataId[1].Value;
                            var mb = db.MezuniyetBasvurularis.First(p => p.MezuniyetBasvurulariID == mbid);
                            var rpr = new RprMezuniyetTezTeslimFormu_FR0338(mb.RowID, ilkOrIkinci == 1);

                            rpr.CreateDocument();
                            rpr.DisplayName += ".pdf";
                            rpr.ExportOptions.Pdf.Compressed = true;
                            ms = new MemoryStream();
                            rpr.ExportToPdf(ms);
                            ms.Seek(0, System.IO.SeekOrigin.Begin);
                            var attc = new System.Net.Mail.Attachment(ms, rpr.DisplayName, "application/pdf");
                            attc.ContentDisposition.ModificationDate = DateTime.Now;
                            mdl.Add(attc);
                            break;
                        }
                    case RaporTipiEnum.MezuniyetTezDuzeltmeVeJuriUyelerineTezTeslimTutanagi:
                        {
                            var rpr = new RprMezuniyetTezDuzeltmeJuriUyelerineCiltliTezTeslimTutanagi_FR0329_FR0325(dataId[0].Value);

                            rpr.CreateDocument();
                            rpr.DisplayName += ".pdf";
                            rpr.ExportOptions.Pdf.Compressed = true;
                            ms = new MemoryStream();
                            rpr.ExportToPdf(ms);
                            ms.Seek(0, System.IO.SeekOrigin.Begin);
                            var attc = new System.Net.Mail.Attachment(ms, rpr.DisplayName, "application/pdf");
                            attc.ContentDisposition.ModificationDate = DateTime.Now;
                            mdl.Add(attc);
                            break;
                        }
                    case RaporTipiEnum.MezuniyetTezSinavSonucFormu:
                        {
                            var srTalepId = dataId[0].Value;
                            var srTalep = db.SRTalepleris.First(p => p.SRTalepID == srTalepId);

                            var rpr = new RprTezSinavSonucTutanagi_FR0342_FR0377(srTalep.UniqueID.Value);

                            rpr.CreateDocument();
                            rpr.DisplayName += ".pdf";
                            rpr.ExportOptions.Pdf.Compressed = true;
                            ms = new MemoryStream();
                            rpr.ExportToPdf(ms);
                            ms.Seek(0, System.IO.SeekOrigin.Begin);
                            var attc = new System.Net.Mail.Attachment(ms, rpr.DisplayName, "application/pdf");
                            attc.ContentDisposition.ModificationDate = DateTime.Now;
                            mdl.Add(attc);
                            break;
                        }
                    case RaporTipiEnum.MezuniyetJuriUyelerineTezTeslimFormu:
                        {
                            if (!dataId[0].HasValue)
                                throw new Exception("mezuniyetJuriOneriFormId boş gelmekte.");
                            var mezuniyetJuriOneriFormId = dataId[0].Value;
                            var rpr = new RprJuriUyelerineTezTeslimFormu_FR0341_FR0302(mezuniyetJuriOneriFormId);

                            rpr.CreateDocument();
                            rpr.DisplayName += ".pdf";
                            rpr.ExportOptions.Pdf.Compressed = true;
                            ms = new MemoryStream();
                            rpr.ExportToPdf(ms);
                            ms.Seek(0, System.IO.SeekOrigin.Begin);
                            var attc = new System.Net.Mail.Attachment(ms, rpr.DisplayName, "application/pdf");
                            attc.ContentDisposition.ModificationDate = DateTime.Now;
                            mdl.Add(attc);
                            break;
                        }
                    case RaporTipiEnum.MezuniyetTezdenUretilenYayinlariDegerlendirmeFormu:
                        {
                            if (!dataId[0].HasValue)
                                throw new Exception("mezuniyetJuriOneriFormId boş gelmekte.");
                            var mezuniyetJuriOneriFormId = dataId[0].Value;
                            var mezuniyetJuriOneriFormuJuriId = dataId[1];
                            var rpr = new RprMezuniyetTezdenUretilenYayinlariDegerlendirmeFormu_FR0304(mezuniyetJuriOneriFormId, mezuniyetJuriOneriFormuJuriId);

                            rpr.CreateDocument();
                            rpr.DisplayName += ".pdf";
                            rpr.ExportOptions.Pdf.Compressed = true;
                            ms = new MemoryStream();
                            rpr.ExportToPdf(ms);
                            ms.Seek(0, System.IO.SeekOrigin.Begin);
                            var attc = new System.Net.Mail.Attachment(ms, rpr.DisplayName, "application/pdf");
                            attc.ContentDisposition.ModificationDate = DateTime.Now;
                            mdl.Add(attc);
                            break;
                        }
                    case RaporTipiEnum.MezuniyetDoktoraTezDegerlendirmeFormu:
                        {

                            var mezuniyetJuriOneriFormId = dataId[0].Value;
                            var mezuniyetJuriOneriFormuJuriId = dataId[1];
                            var rpr = new RprMezuniyetTezDegerlendirmeFormu_FR0303(mezuniyetJuriOneriFormId, mezuniyetJuriOneriFormuJuriId);

                            rpr.CreateDocument();
                            rpr.DisplayName += ".pdf";
                            rpr.ExportOptions.Pdf.Compressed = true;
                            ms = new MemoryStream();
                            rpr.ExportToPdf(ms);
                            ms.Seek(0, System.IO.SeekOrigin.Begin);
                            var attc = new System.Net.Mail.Attachment(ms, rpr.DisplayName, "application/pdf");
                            attc.ContentDisposition.ModificationDate = DateTime.Now;
                            mdl.Add(attc);
                            break;
                        }
                    case RaporTipiEnum.MezuniyetTezKontrolFormu:
                        {
                            var id = dataId[0].Value;
                            var rpr = new RprMezuniyetTezKontrolFormu(null, id);

                            rpr.CreateDocument();
                            rpr.DisplayName += ".pdf";
                            rpr.ExportOptions.Pdf.Compressed = true;
                            ms = new MemoryStream();
                            rpr.ExportToPdf(ms);
                            ms.Seek(0, System.IO.SeekOrigin.Begin);
                            var attc = new System.Net.Mail.Attachment(ms, rpr.DisplayName, "application/pdf");
                            attc.ContentDisposition.ModificationDate = DateTime.Now;
                            mdl.Add(attc);
                            break;
                        }
                    case RaporTipiEnum.TezIzlemeDegerlendirmeFormu:
                        {
                            var id = dataId[0].Value;
                            var rpr = new RprTiDegerlendirmeFormu_FR0307(id);

                            rpr.CreateDocument();
                            rpr.DisplayName += ".pdf";
                            var isSwhoRaporDetay = false;
                            if (dataId.Count > 1) isSwhoRaporDetay = dataId[1].ToIntToBooleanObj() ?? false;
                            if (isSwhoRaporDetay)
                            {
                                var rpr2 = new RprTiDegerlendirmeFormuDetay_FR0307(id);
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
                            break;
                        }
                    case RaporTipiEnum.TezOneriSavunmaFormu:
                        {
                            var id = dataId[0].Value;
                            var rpr = new RprToSavunmaFormu_FR0348(id);
                            rpr.CreateDocument();
                            rpr.DisplayName += ".pdf";
                            var isSwhoRaporDetay = false;
                            if (dataId.Count > 1) isSwhoRaporDetay = dataId[1].ToIntToBooleanObj() ?? false;
                            if (isSwhoRaporDetay)
                            {
                                var rpr2 = new RprToSavunmaFormuDetay_FR0348(id);
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
                            break;
                        }
                    case RaporTipiEnum.TezDanismanOneriFormu:
                        {
                            var id = dataId[0].Value;
                            var rpr = new RprTezDanismaniOneriFormu_FR0347(id);

                            rpr.CreateDocument();
                            rpr.DisplayName += ".pdf";
                            rpr.ExportOptions.Pdf.Compressed = true;
                            ms = new MemoryStream();
                            rpr.ExportToPdf(ms);
                            ms.Seek(0, System.IO.SeekOrigin.Begin);
                            var attc = new System.Net.Mail.Attachment(ms, rpr.DisplayName, "application/pdf");
                            attc.ContentDisposition.ModificationDate = DateTime.Now;
                            mdl.Add(attc);
                            break;
                        }
                    case RaporTipiEnum.TezDanismanDegisiklikFormu:
                        {
                            var id = dataId[0].Value;
                            var rpr = new RprTezDanismaniDegisiklikFormu_FR0308(id);

                            rpr.CreateDocument();
                            rpr.DisplayName += ".pdf";
                            rpr.ExportOptions.Pdf.Compressed = true;
                            ms = new MemoryStream();
                            rpr.ExportToPdf(ms);
                            ms.Seek(0, System.IO.SeekOrigin.Begin);
                            var attc = new System.Net.Mail.Attachment(ms, rpr.DisplayName, "application/pdf");
                            attc.ContentDisposition.ModificationDate = DateTime.Now;
                            mdl.Add(attc);
                            break;
                        }
                    case RaporTipiEnum.TezEsDanismanOneriFormu:
                        {
                            var id = dataId[0].Value; // tdo es danisman id
                            var rpr = new RprTezEsDanismaniOneriFormu_FR0320(id);

                            rpr.CreateDocument();
                            rpr.DisplayName += ".pdf";
                            rpr.ExportOptions.Pdf.Compressed = true;
                            ms = new MemoryStream();
                            rpr.ExportToPdf(ms);
                            ms.Seek(0, System.IO.SeekOrigin.Begin);
                            var attc = new System.Net.Mail.Attachment(ms, rpr.DisplayName, "application/pdf");
                            attc.ContentDisposition.ModificationDate = DateTime.Now;
                            mdl.Add(attc);
                            break;
                        }
                    case RaporTipiEnum.YeterlikDoktoraSinavSonucFormu:
                        {
                            var id = dataId[0].Value;
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
                            break;
                        }
                    case RaporTipiEnum.TezIzlemeJuriOneriFormu:
                        {
                            var id = dataId[0].Value;

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
                            break;
                        }
                    case RaporTipiEnum.TezIzlemeJuriDegisiklikFormu:
                        {
                            var id = dataId[0].Value;

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
                            break;
                        }
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