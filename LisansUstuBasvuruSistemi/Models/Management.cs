
using BiskaUtil;
using LisansUstuBasvuruSistemi.Raporlar;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OsymWebServiceClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Validation;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Security;
using System.ServiceModel;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using System.Xml;
using DevExpress.XtraCharts.Native;
using LisansUstuBasvuruSistemi.Models.ObsService;
using LisansUstuBasvuruSistemi.Utilities.Dtos.CmbDtos;
using LisansUstuBasvuruSistemi.Utilities.Dtos.DmDtos;
using LisansUstuBasvuruSistemi.Models.FilterModel;

namespace LisansUstuBasvuruSistemi.Models
{
    public static class Management
    {

        public static string Tuz = "@BİSKAmcumu";
        public static int UniversiteYtuKod { get; } = 67;
        public static List<string> FExtensions()
        {
            return new List<string>() { ".jpg", ".jpeg", ".tif", ".bmp", ".png", ".txt", ".doc", ".docx", ".xls", ".xlsx", ".pdf", ".rtf", ".pptx" };
        }


        public static List<Enstituler> Enstitulers = new List<Enstituler>();

        public static List<Enstituler> GetEnstituler()
        {
            if (!Enstitulers.Any())
            {
                using (LisansustuBasvuruSistemiEntities db = new LisansustuBasvuruSistemiEntities())
                {
                    Enstitulers = db.Enstitulers.Where(p => p.IsAktif).OrderBy(o => o.EnstituAd).ToList();
                }
            }

            return Enstitulers;

        }

        public static Enstituler[] GetEnstituler(bool sadeceYetkiliOlduguEnstituler = false)
        {
            var enst = Enstitulers.AsQueryable();
            if (sadeceYetkiliOlduguEnstituler && UserIdentity.Current.IsAdmin == false) enst = enst.Where(p => UserIdentity.Current.EnstituKods.Contains(p.EnstituKod));
            return enst.Where(p => p.IsAktif).OrderBy(o => o.EnstituAd).ToArray();
        }
        public static Enstituler[] GetKullaniciEnstituler(int KullaniciID)
        {
            using (LisansustuBasvuruSistemiEntities db = new LisansustuBasvuruSistemiEntities())
            {
                return db.Enstitulers.Where(p => p.IsAktif && db.KullaniciEnstituYetkileris.Any(a => p.EnstituKod == a.EnstituKod && a.KullaniciID == KullaniciID)).OrderBy(o => o.EnstituAd).ToArray();

            }
        }

        #region gsisData


        public static StudentControl StudentControl(string TcKimlikNo = null)
        {
            ObsGetData obsData = new ObsGetData();
            return obsData.GetObsStudentControl(TcKimlikNo);
        }

        public static string gsisKayitAktar(string kayit)
        {
            System.Net.ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };//SSL hatasını gidermek için 
            using (Ws_GsisAktarim.ServiceSoapClient aktarim = new Ws_GsisAktarim.ServiceSoapClient("ServiceSoap"))
            {
                return aktarim.studentRegistration(kayit);
            }

        }
        public static OnlineOdemeProgramDetayModel GetGsisProgramUcretKontrol(OnlineOdemeProgramDetayModel mdl)
        {
            System.Net.ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };//SSL hatasını gidermek için 
            using (Ws_GsisOdeme.ServiceSoapClient wS = new Ws_GsisOdeme.ServiceSoapClient("ServiceSoap1"))
            {
                var data = wS.getProgramFee(mdl.ProgramKod, mdl.OgrenimTipKod.ToString());
                mdl.OdemeDonemNo = mdl.IsDekontOrSanalPos ? 1 : (mdl.OdemeListesi.Count > 0 ? (mdl.OdemeListesi.Select(s => s.DonemNo).Max() + 1) : 1);
                if (data.returnInfo == "1")
                {
                    mdl.IsOdemeVar = true;
                    mdl.OdenecekUcret = data.amount.ToDouble().Value;
                    mdl.OdemeBaslangicTarihi = data.firstpaymentdate != "" ? Convert.ToDateTime(data.firstpaymentdate) : (DateTime?)null;
                    mdl.OdemeBitisTarihi = data.lastpaymentdate != "" ? Convert.ToDateTime(data.lastpaymentdate) : (DateTime?)null;
                    if (mdl.OdemeBaslangicTarihi.HasValue && mdl.OdemeBitisTarihi.HasValue)
                    {
                        if (mdl.OdemeBaslangicTarihi <= DateTime.Now.Date && mdl.OdemeBitisTarihi >= DateTime.Now.Date)
                            mdl.IsOdemeIslemiAcik = true;
                        else
                        {

                            mdl.AciklamaSelectedLng = "Sadece Ödeme Tarih Aralığında Ödeme İşlemi Yapılabilir.";
                        }
                    }
                    else
                    {
                        mdl.AciklamaSelectedLng = "Sadece Ödeme Tarih Aralığında Ödeme İşlemi Yapılabilir.";
                    }
                }
                else
                {
                    mdl.AciklamaSelectedLng = "Hata oluştu";
                    Management.SistemBilgisiKaydet("Gsis Program Ücreti Sorgulanırken Servis Hata Döndürdü! (" + mdl.AdSoyad + ")", "Management/GetGsisProgramUcretKontrol/getProgramFee ProgramKod:" + mdl.ProgramKod + " OgrenimTipKod:" + mdl.OgrenimTipKod + " Dönen Hata Kodu:" + data.returnInfo, BilgiTipi.Kritik);

                }


            }
            return mdl;
        }
        public static bool GetGsisUcretOdemeTarihKontrol(out string Msj, out DateTime? BaslangicTarihi, out DateTime? BitisTarihi)
        {
            bool returnValue = true;
            string _Msj = "";
            DateTime? _BaslangicTarihi = null;
            DateTime? _BitisTarihi = null;
            System.Net.ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };//SSL hatasını gidermek için 
            using (Ws_GsisOdeme.ServiceSoapClient wS = new Ws_GsisOdeme.ServiceSoapClient("ServiceSoap1"))
            {
                var OdemeBasTarih = wS.getFirstPaymentDate();
                var OdemeBitTarih = wS.getLastPaymentDate();
                if (OdemeBasTarih == "-1" || OdemeBasTarih == "-2")
                {
                    _Msj = "Hata";
                    Management.SistemBilgisiKaydet("Gsis Öğrenci Borcuna Ait Ödeme Başlangıç Tarihi Sorgulanırken Servis Hata Döndürdü!", "Management/GetGsisUcretOdemeTarihKontrol/getFirstPaymentDate Dönen Hata Kodu:" + OdemeBasTarih, BilgiTipi.Kritik);
                    returnValue = false;
                }
                else
                {
                    _BaslangicTarihi = OdemeBasTarih != "" ? Convert.ToDateTime(OdemeBasTarih) : (DateTime?)null;
                }
                if (OdemeBitTarih == "-1" || OdemeBitTarih == "-2")
                {
                    returnValue = false;
                    _Msj = "Hata";
                    Management.SistemBilgisiKaydet("Gsis Öğrenci Borcuna Ait Ödeme Bitiş Tarihi Sorgulanırken Servis Hata Döndürdü!", "Management/GetGsisUcretOdemeTarihKontrol/getLastPaymentDate Dönen Hata Kodu:" + OdemeBitTarih, BilgiTipi.Kritik);
                }
                else
                {
                    _BitisTarihi = OdemeBasTarih != "" ? Convert.ToDateTime(OdemeBitTarih) : (DateTime?)null;
                }
            }
            Msj = _Msj;
            BaslangicTarihi = _BaslangicTarihi;
            BitisTarihi = _BitisTarihi;
            return returnValue;
        }
        public static OnlineOdemeProgramDetayModel GetGsisOgrenciOgrenimBorcSorgula(OnlineOdemeProgramDetayModel mdl)
        {
            System.Net.ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };//SSL hatasını gidermek için 
            using (Ws_GsisOdeme.ServiceSoapClient wS = new Ws_GsisOdeme.ServiceSoapClient("ServiceSoap1"))
            {


                var data = wS.paymentControl(mdl.OgrenciNo.Trim());
                var RetVal = Convert.ToDouble(data);
                mdl.OdemeDonemNo = mdl.IsDekontOrSanalPos ? 1 : (mdl.OdemeListesi.Count > 0 ? (mdl.OdemeListesi.Select(s => s.DonemNo).Max() + 1) : 1);
                if (RetVal > 0)
                {
                    //borçlandırma var dönen değer borç tutarı
                    mdl.OdenecekUcret = RetVal;
                    mdl.IsOdemeVar = true;
                    var OdemeBasTarih = wS.getFirstPaymentDate();
                    var OdemeBitTarih = wS.getLastPaymentDate();
                    if (OdemeBasTarih == "-1" || OdemeBasTarih == "-2")
                    {

                        Management.SistemBilgisiKaydet("Gsis Öğrenci Borcuna Ait Ödeme Başlangıç Tarihi Sorgulanırken Servis Hata Döndürdü! (" + mdl.AdSoyad + " [" + mdl.OgrenciNo + "])", "Management/getFirstPaymentDate/paymentControl Dönen Hata Kodu:" + OdemeBasTarih, BilgiTipi.Kritik);
                    }
                    else
                    {
                        mdl.OdemeBaslangicTarihi = OdemeBasTarih != "" ? Convert.ToDateTime(OdemeBasTarih) : (DateTime?)null;
                    }
                    if (OdemeBitTarih == "-1" || OdemeBitTarih == "-2")
                    {
                        Management.SistemBilgisiKaydet("Gsis Öğrenci Borcuna Ait Ödeme Bitiş Tarihi Sorgulanırken Servis Hata Döndürdü! (" + mdl.AdSoyad + " [" + mdl.OgrenciNo + "])", "Management/GetOnlineOdemeProgramDetay/getLastPaymentDate Dönen Hata Kodu:" + OdemeBitTarih, BilgiTipi.Kritik);
                    }
                    else
                    {
                        mdl.OdemeBitisTarihi = OdemeBasTarih != "" ? Convert.ToDateTime(OdemeBitTarih) : (DateTime?)null;
                    }
                    if (mdl.OdemeBaslangicTarihi.HasValue && mdl.OdemeBitisTarihi.HasValue)
                    {
                        if (mdl.OdemeBaslangicTarihi <= DateTime.Now.Date && mdl.OdemeBitisTarihi >= DateTime.Now.Date)
                            mdl.IsOdemeIslemiAcik = true;
                        else
                        {

                            mdl.AciklamaSelectedLng = "Sadece Ödeme Tarih Aralığında Ödeme İşlemi Yapılabilir.";
                        }
                    }
                    else
                    {
                        mdl.AciklamaSelectedLng = "Sadece Ödeme Tarih Aralığında Ödeme İşlemi Yapılabilir.";
                    }
                }
                else if (RetVal == 0)
                {
                    //borcu yok
                    mdl.AciklamaSelectedLng = "Borç Yok";

                }
                else if (RetVal == -1)
                {
                    //borçlandırma yok
                    mdl.AciklamaSelectedLng = "Boçlandırma Bulunamadı";
                }
                else if (RetVal == -2 || RetVal == -3)
                {

                    mdl.AciklamaSelectedLng = "Hata Oluştu!";
                    //sistem hatası
                    Management.SistemBilgisiKaydet("Gsis Öğrenci Borcu Sorgulanırken Servis Hata Döndürdü! (" + mdl.AdSoyad + " [" + mdl.OgrenciNo + "])", "Management/GetOnlineOdemeProgramDetay/paymentControl Dönen Hata Kodu:" + RetVal, BilgiTipi.Kritik);
                }

            }
            return mdl;
        }
        public static bool SetPaymentInfoGsis(string OgrenciNo, string OdenenTutar, string DekontNo, DateTime DekontTarihi, out string Msj)
        {
            var returnValue = false;
            var _msj = "";
            try
            {
                System.Net.ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };//SSL hatasını gidermek için 
                using (Ws_GsisOdeme.ServiceSoapClient wS = new Ws_GsisOdeme.ServiceSoapClient("ServiceSoap1"))
                {
                    var vsRetVal = wS.setPaymentInfo(OgrenciNo, OdenenTutar, DekontNo, DekontTarihi.ToString("yyyy-MM-dd"));
                    if (vsRetVal == "1")
                    {
                        returnValue = true;
                    }
                    else
                    {
                        _msj = OgrenciNo + " numaralı öğrencinin  " + DekontNo + " dekont numarası " + DekontTarihi.ToString("dd.MM.yyyy") + " ödeme tarihi ve " + OdenenTutar + " TL ödenen tutar bilgisi! Gsis veb servisi ile işlenirken bir hata oluştu! Sevisten dönen değer:" + vsRetVal;
                    }

                }
            }
            catch (Exception ex)
            {
                _msj = OgrenciNo + " numaralı öğrencinin  " + DekontNo + " dekont numarası " + DekontTarihi.ToString("dd.MM.yyyy") + " ödeme tarihi ve " + OdenenTutar + " TL ödenen tutar bilgisi! Gsis veb servisine işlenirken bir hata oluştu! Hata:" + ex.ToExceptionMessage();

            }
            Msj = _msj;
            return returnValue;
        }




        //public static Ws_GsisMezuniyetBilgi.TezInfo GetGsisMezuniyetTezBilgi(string OgrenciNo)
        //{
        //    //Aşağıdaki kullanıcı adı ve şifre servisi eklerken de gerekli
        //    string UserName = "hazirlik";
        //    string Password = "J6Gnx3Ah";
        //    using (Ws_GsisMezuniyetBilgi.DanismanServiceSoapClient client = new Ws_GsisMezuniyetBilgi.DanismanServiceSoapClient())
        //    {
        //        client.ClientCredentials.Windows.ClientCredential.UserName = UserName;
        //        client.ClientCredentials.Windows.ClientCredential.Password = Password;
        //        var data = client.TezInfoGSIS(OgrenciNo); 
        //        if (!data.ad.IsNullOrWhiteSpace()) data.ad = System.Web.HttpUtility.HtmlDecode(data.ad).Trim();
        //        if (!data.tezDanismani.IsNullOrWhiteSpace()) data.tezDanismani = System.Web.HttpUtility.HtmlDecode(data.tezDanismani).Trim();
        //        if (!data.tezDanismaniUnvan.IsNullOrWhiteSpace()) data.tezDanismaniUnvan = System.Web.HttpUtility.HtmlDecode(data.tezDanismaniUnvan).Trim();
        //        if (!data.tezDanismaniUnvan.IsNullOrWhiteSpace() && data.tezDanismaniUnvan.Contains("Üye")) data.tezDanismaniUnvan = "DR.ÖĞR.ÜYE.";
        //        if (!data.esDanismani.IsNullOrWhiteSpace()) data.esDanismani = System.Web.HttpUtility.HtmlDecode(data.esDanismani).Trim();
        //        if (!data.esDanismaniUnvan.IsNullOrWhiteSpace()) data.esDanismaniUnvan = System.Web.HttpUtility.HtmlDecode(data.esDanismaniUnvan).Trim();
        //        if (!data.esDanismaniUnvan.IsNullOrWhiteSpace() && data.esDanismaniUnvan.Contains("Üye")) data.esDanismaniUnvan = "DR.ÖĞR.ÜYE.";
        //        if (!data.tikUyesi1.IsNullOrWhiteSpace()) data.tikUyesi1 = System.Web.HttpUtility.HtmlDecode(data.tikUyesi1).Trim();
        //        if (!data.tikUyesi1Unvan.IsNullOrWhiteSpace()) data.tikUyesi1Unvan = System.Web.HttpUtility.HtmlDecode(data.tikUyesi1Unvan).Trim();
        //        if (!data.tikUyesi1Unvan.IsNullOrWhiteSpace() && data.tikUyesi1Unvan.Contains("Üye")) data.tikUyesi1Unvan = "DR.ÖĞR.ÜYE.";
        //        if (!data.tikUyesi2.IsNullOrWhiteSpace()) data.tikUyesi2 = System.Web.HttpUtility.HtmlDecode(data.tikUyesi2).Trim();
        //        if (!data.tikUyesi2Unvan.IsNullOrWhiteSpace()) data.tikUyesi2Unvan = System.Web.HttpUtility.HtmlDecode(data.tikUyesi2Unvan).Trim();
        //        if (!data.tikUyesi2Unvan.IsNullOrWhiteSpace() && data.tikUyesi2Unvan.Contains("Üye")) data.tikUyesi2Unvan = "DR.ÖĞR.ÜYE.";
        //        //if (!data.ingilizceAd.IsNullOrWhiteSpace()) data.ingilizceAd = System.Web.HttpUtility.HtmlDecode(data.ingilizceAd).Trim();
        //        return data;

        //        //  var derslers = model.Select(s => new { s.DersKodu, s.DersAdi }).Distinct().ToList();
        //        //  var DersWarMi = derslers.Where(p => p.DersKodu == "IKT4000").FirstOrDefault();

        //    }

        //}



        #endregion

        #region YokData
        public static YokStudentControl yokStudentControl(long TcKimlikNo)
        {
            var model = new YokStudentControl();
            try
            {
                var KullaniciAdi = SistemAyar.getAyar(SistemAyar.AyarYOKWSKullaniciAdi);
                var Sifre = SistemAyar.getAyar(SistemAyar.AyarYOKWSKullaniciSifre);
                System.Net.ServicePointManager.Expect100Continue = false;

                BasicHttpBinding basicAuthBinding = new BasicHttpBinding(BasicHttpSecurityMode.TransportCredentialOnly);

                basicAuthBinding.Security.Transport.ClientCredentialType = HttpClientCredentialType.Basic;
                EndpointAddress basicAuthEndpoint = new EndpointAddress("http://servisler.yok.gov.tr/ws/yuksekogretim/egitim?wsdl");
                Ws_YokOgrenciSorgula.YuksekOgretimEgitimBilgisiPortClient clnt = new Ws_YokOgrenciSorgula.YuksekOgretimEgitimBilgisiPortClient(basicAuthBinding, basicAuthEndpoint);


                clnt.ClientCredentials.UserName.UserName = KullaniciAdi;
                clnt.ClientCredentials.UserName.Password = Sifre;
                var ebORtype = new Ws_YokOgrenciSorgula.EgitimBilgisiRequestType { TcKimlikNo = TcKimlikNo };
                var deger = clnt.EgitimBilgisi(ebORtype);
                if (deger.Sonuc.SonucKod == 1)
                {
                    var BirimKods = GetBirimTurKods();
                    var UniKods = GetUniversiteTurKods();
                    var OtKods = GetOgrenimTurKods();
                    var OStatuKods = new List<int> { 1, 10 }; // aktif ve pasif

                    var EgitimBilg = deger.EgitimBilgisiKayit.Where(p => p != null
                                                                         && p.OgrencilikBilgi != null
                                                                         && OStatuKods.Contains(p.OgrencilikBilgi.OgrencilikStatusu.Kod)
                                                                         && BirimKods.Contains(p.BirimBilgi.BirimTuru.Kod)
                                                                         && UniKods.Contains(p.BirimBilgi.UniversiteTuru.Kod)
                                                                         && OtKods.Contains(p.BirimBilgi.OgrenimTuru.Kod)
                    ).ToList();

                    foreach (var item in EgitimBilg)
                    {
                        model.AktifOgrenimListesi.Add(item.BirimBilgi.BirimAdi);
                    }
                    model.KayitVar = EgitimBilg.Any();

                }
                else
                {
                    model.Hata = true;
                    model.KayitVar = false;
                    model.Mesaj = "Yök öğrenci sorgulama servisinden sorgulanan öğrenci için sonuç bilgisi başarılı dönmemektedir." + " \r\n\r\nSonuç Kod:" + deger.Sonuc.SonucKod + "\r\nSonucMesaj:" + deger.Sonuc.SonucMesaj;
                    model.Mesaj += "\r\n\r\n" + JsonConvert.SerializeObject(deger);
                    SistemBilgisiKaydet(model.Mesaj, "Management/yokStudentControl", BilgiTipi.Kritik);

                }
            }
            catch (Exception ex)
            {
                model.KayitVar = false;
                model.Hata = true;
                model.Mesaj = "YÖK Servisinden Öğrenci Bilgisi kontrol edilirken bir hata oluştu.Hata:" + ex.ToExceptionMessage();
                SistemBilgisiKaydet(model.Mesaj, "Management/yokStudentControl \r\n" + ex.ToExceptionStackTrace(), BilgiTipi.Kritik);
            }

            return model;
        }

        public static List<int> GetOgrenimTurKods()
        {
            var oTurList = new List<int>();
            oTurList.Add(1);// - NORMAL ÖĞRETİM
                            //oTurList.Add(2);// - İKİNCİ ÖĞRETİM
            oTurList.Add(3);// - UZAKTAN ÖĞRETİM
            oTurList.Add(4);// - AÇIK ÖĞRETİM

            return oTurList;
        }

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
        public static OnlineOdemeProgramDetayModel GetOnlineOdemeProgramDetay(string UniqueID, bool IsDekontOrSanalPos, bool IsBasvuruOrYatayGecis, bool YokODKontrolYap)
        {
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var _UniqueID = new Guid(UniqueID);
                var BTercih = db.BasvurularTercihleris.Where(p => p.UniqueID == _UniqueID).First();
                var Basvuru = BTercih.Basvurular;
                var BSurec = Basvuru.BasvuruSurec;
                var mdl = (from s in db.BasvurularTercihleris.Where(p => p.UniqueID == _UniqueID)
                           join b in db.Basvurulars on s.BasvuruID equals b.BasvuruID
                           join kt in db.KullaniciTipleris on b.KullaniciTipID equals kt.KullaniciTipID
                           join k in db.Kullanicilars on b.KullaniciID equals k.KullaniciID
                           join bs in db.BasvuruSurecs on b.BasvuruSurecID equals bs.BasvuruSurecID
                           join dnmL in db.Donemlers on new { bs.DonemID } equals new { dnmL.DonemID }
                           join bsot in db.BasvuruSurecOgrenimTipleris on new { bs.BasvuruSurecID, s.OgrenimTipKod } equals new { bsot.BasvuruSurecID, bsot.OgrenimTipKod }
                           join bsKot in db.BasvuruSurecKotalars on new { bs.BasvuruSurecID, s.ProgramKod, s.OgrenimTipKod } equals new { bsKot.BasvuruSurecID, bsKot.ProgramKod, bsKot.OgrenimTipKod }
                           join p in db.Programlars on s.ProgramKod equals p.ProgramKod
                           join ot in db.OgrenimTipleris on new { s.OgrenimTipKod, s.Basvurular.BasvuruSurec.EnstituKod } equals new { ot.OgrenimTipKod, ot.EnstituKod }
                           select new OnlineOdemeProgramDetayModel
                           {
                               BasvuruTercihID = s.BasvuruTercihID,
                               EnstituKod = bs.EnstituKod,
                               BasvuruSurecID = b.BasvuruSurecID,
                               SurecAdi = bs.BaslangicYil + "/" + bs.BitisYil + " " + dnmL.DonemAdi,
                               DonemBaslangicYil = bs.BaslangicYil,
                               DonemID = bs.DonemID,
                               SurecBaslangicTarihi = bs.BaslangicTarihi,
                               SurecBitisTarihi = bs.BitisTarihi,
                               BasvuruID = s.BasvuruID,
                               AlanTipID = s.AlanTipID,
                               AlanKota = bsKot.OrtakKota ? (bsKot.OrtakKotaSayisi.Value + (bsKot.AlanDisiEkKota ?? 0)) : (s.AlanTipID == AlanTipi.AlanIci ? (bsKot.AlanIciKota + (bsKot.AlanIciEkKota ?? 0)) : (bsKot.AlanDisiKota + (bsKot.AlanDisiEkKota ?? 0))),
                               KullaniciID = b.KullaniciID,
                               IsYerliOgrenci = kt.Yerli,
                               TcKimlikNo = kt.Yerli ? b.TcKimlikNo : b.PasaportNo,
                               OgrenciNo = k.OgrenciNo,
                               AdSoyad = k.Ad + " " + k.Soyad,
                               YtuOgrencisi = k.YtuOgrencisi,
                               EMail = k.EMail,
                               CepTel = k.CepTel,
                               UniqueID = s.UniqueID,
                               ProgramKod = p.ProgramKod == "704" ? "7B2" : p.ProgramKod,
                               ProgramAdi = p.ProgramAdi,
                               AlesTipID = p.AlesTipID,
                               OgrenimTipKod = s.OgrenimTipKod,
                               OgrenimTipAdi = ot.OgrenimTipAdi,
                               Ucretli = IsBasvuruOrYatayGecis ? p.Ucretli : false,
                               Ucret = IsBasvuruOrYatayGecis ? p.Ucret : null,
                               YokOgrenciKontroluYap = (bsot.YokOgrenciKontroluYap || bsKot.YokOgrenciKontroluYap),
                               IstenecekKatkiPayiTutari = bsot.IstenecekKatkiPayiTutari,
                               KayitOldu = s.MulakatSonuclaris.Any(s2 => s2.KayitDurumID == KayitDurumu.KayitOldu),
                               MulakatSonuclari = s.MulakatSonuclaris.FirstOrDefault(),
                               OdemeListesi = s.BasvurularTercihleriKayitOdemeleris.Where(p => p.IsOdendi).ToList(),
                               IsBelgeYuklemesiVar = bs.IsBelgeYuklemeVar,
                               IsKayittaBelgeOnayiZorunlu = bs.IsKayittaBelgeOnayiZorunlu,
                               BasvurularYuklenenBelgeler = b.BasvurularYuklenenBelgelers.ToList(),
                               BasvuruBilgi = b,

                           }).FirstOrDefault();

                if (mdl != null)
                {
                    #region BelgeKontrol
                    if (mdl.IsBelgeYuklemesiVar && mdl.IsKayittaBelgeOnayiZorunlu)
                    {
                        var qtercihler = new List<CmbMultyTypeDto>();
                        var tercihler = (from s in Basvuru.BasvurularTercihleris
                                         join at in db.AlanTipleris on s.AlanTipID equals at.AlanTipID
                                         join kt in db.BasvuruSurecKotalars.Where(p => p.BasvuruSurecID == Basvuru.BasvuruSurecID) on new { s.OgrenimTipKod, s.ProgramKod } equals new { kt.OgrenimTipKod, kt.ProgramKod }
                                         select new basvuruTercihModel
                                         {
                                             BasvuruTercihID = s.BasvuruTercihID,
                                             BasvuruID = s.BasvuruID,
                                             SiraNo = s.SiraNo,
                                             Ingilizce = s.Programlar.Ingilizce,
                                             YlBilgiIste = Basvuru.YLUniversiteID.HasValue,
                                             AlanTipID = at.AlanTipID,
                                             AlanTipAdi = at.AlanTipAdi,
                                             OgrenimTipKod = s.OgrenimTipKod,
                                             ProgramKod = s.ProgramKod,
                                         }).ToList();
                        foreach (var item in tercihler)
                        {

                            qtercihler.Add(new CmbMultyTypeDto { Value = item.OgrenimTipKod, ValueB = item.Ingilizce, ValueS2 = item.ProgramKod });
                            item.ProgramBilgileri = Management.getKontenjanProgramBilgi(item.ProgramKod, item.OgrenimTipKod, Basvuru.BasvuruSurecID, Basvuru.KullaniciTipID.Value, Basvuru.LOgrenciBolumID, Basvuru.LUniversiteID);
                            item.ProgramBilgileri.AlanTipID = item.AlanTipID;
                            item.Ingilizce = item.ProgramBilgileri.Ingilizce;

                        }
                        var AlesIstensinmi = Management.cmbGetdAktifSinavlar(qtercihler, Basvuru.BasvuruSurecID, SinavTipGrup.Ales_Gree, false).Count > 0;
                        var DilIstensinmi = Management.cmbGetdAktifSinavlar(qtercihler, Basvuru.BasvuruSurecID, SinavTipGrup.DilSinavlari, false).Count > 0;
                        var TomerIstensinmi = !Basvuru.KullaniciTipleri.Yerli && Management.cmbGetdAktifSinavlar(qtercihler, Basvuru.BasvuruSurecID, SinavTipGrup.Tomer, false).Count > 0;



                        var SurecBelgeleri = db.BasvuruSurecBelgeTipleris.Where(p => p.BasvuruSurecID == Basvuru.BasvuruSurecID && p.IsYerliOrYabanci == (p.IsYerliOrYabanci.HasValue ? mdl.IsYerliOgrenci : (bool?)null)).ToList();

                        var ZorunluOlmayanlar = new List<int>();
                        if (AlesIstensinmi)
                        {
                            var AlesBilgi = Basvuru.BasvurularSinavBilgis.Where(p => p.SinavTipGrupID == SinavTipGrup.Ales_Gree).First();
                            var AGSinavBelgesi = SurecBelgeleri.Where(a => a.IsZorunlu && a.BasvuruBelgeTipID == BasvuruBelgeTipi.AlesGreSinaviBelgesi &&
                                                                          !a.BasvuruSurecBelgeTipleriYuklemeSeklis.Any(a2 => a2.SinavTipKod == AlesBilgi.SinavTipKod && a2.IsKayitSonrasiGetirilecek)).FirstOrDefault();
                            if (AGSinavBelgesi == null) ZorunluOlmayanlar.Add(BasvuruBelgeTipi.AlesGreSinaviBelgesi);
                        }
                        else ZorunluOlmayanlar.Add(BasvuruBelgeTipi.AlesGreSinaviBelgesi);
                        if (DilIstensinmi)
                        {
                            var DilBilgi = Basvuru.BasvurularSinavBilgis.Where(p => p.SinavTipGrupID == SinavTipGrup.DilSinavlari).First();
                            var DilSinavBelgesi = SurecBelgeleri.Where(a => a.IsZorunlu && a.BasvuruBelgeTipID == BasvuruBelgeTipi.DilSinaviBelgesi &&
                                                                          !a.BasvuruSurecBelgeTipleriYuklemeSeklis.Any(a2 => a2.SinavTipKod == DilBilgi.SinavTipKod && a2.IsKayitSonrasiGetirilecek)).FirstOrDefault();
                            if (DilSinavBelgesi == null) ZorunluOlmayanlar.Add(BasvuruBelgeTipi.DilSinaviBelgesi);
                        }
                        else ZorunluOlmayanlar.Add(BasvuruBelgeTipi.DilSinaviBelgesi);

                        if (TomerIstensinmi)
                        {
                            var TomerBilgi = Basvuru.BasvurularSinavBilgis.Where(p => p.SinavTipGrupID == SinavTipGrup.Tomer).First();
                            var TomerSinavBelgesi = SurecBelgeleri.Where(a => a.IsZorunlu && a.BasvuruBelgeTipID == BasvuruBelgeTipi.TomerSinaviBelgesi &&
                                                                          !a.BasvuruSurecBelgeTipleriYuklemeSeklis.Any(a2 => a2.SinavTipKod == TomerBilgi.SinavTipKod && a2.IsKayitSonrasiGetirilecek)).FirstOrDefault();
                            if (TomerSinavBelgesi == null) ZorunluOlmayanlar.Add(BasvuruBelgeTipi.TomerSinaviBelgesi);
                        }
                        else ZorunluOlmayanlar.Add(BasvuruBelgeTipi.TomerSinaviBelgesi);

                        if (!Basvuru.YLUniversiteID.HasValue) ZorunluOlmayanlar.Add(BasvuruBelgeTipi.YLEgitimBelgesi);

                        var IstenecekBelgeler = SurecBelgeleri.Where(p => p.IsZorunlu && !ZorunluOlmayanlar.Contains(p.BasvuruBelgeTipID)).ToList();

                        var EksikBelgeler = IstenecekBelgeler.Where(p => !mdl.BasvurularYuklenenBelgeler.Any(a => a.BasvuruBelgeTipID == p.BasvuruBelgeTipID)).OrderBy(o => o.BasvuruBelgeTipID).ToList();
                        if (EksikBelgeler.Any())
                        {
                            foreach (var item in EksikBelgeler)
                            {
                                var Belge = item.BasvuruBelgeTipleri;
                                mdl.BelgeKontrolMessages.Add("'" + Belge.BasvuruBelgeTipAdi + "' isimli belge öğrenci tarafından yüklenmemiştir.");
                            }
                        }
                        else
                        {
                            var OnaysizBelgeler = mdl.BasvurularYuklenenBelgeler.Where(p => IstenecekBelgeler.Any(a => a.BasvuruBelgeTipID == p.BasvuruBelgeTipID && !p.IsOnaylandi)).OrderBy(o => o.BasvuruBelgeTipID).ToList();
                            if (OnaysizBelgeler.Any())
                            {
                                foreach (var item in OnaysizBelgeler)
                                {
                                    var Belge = item.BasvuruBelgeTipleri;
                                    mdl.BelgeKontrolMessages.Add("'" + Belge.BasvuruBelgeTipAdi + "' isimli belgeyi kontrol edip onaylayınız.");
                                }
                            }
                        }
                    }
                    #endregion

                    mdl.OdenecekUcret = mdl.Ucret;
                    mdl.IsDekontOrSanalPos = IsDekontOrSanalPos;
                    mdl.ProgramAdi = mdl.ProgramAdi.Trim();
                    mdl.AdSoyad = mdl.AdSoyad.Trim();
                    string ProgramAdiTr = mdl.ProgramAdi;
                    string SurecAdiTr = mdl.SurecAdi;

                    if (mdl.Ucretli)
                    {
                        mdl.IsOgrenimUcretiOrKatkiPayi = true;

                        if (mdl.KayitOldu)
                        {
                            if (mdl.OgrenciNo.IsNullOrWhiteSpace() || !mdl.YtuOgrencisi)
                            {

                                mdl.IsOdemeIslemiAcik = false;
                                mdl.AciklamaSelectedLng = "Ödeme işlemini yapabilmeniz için Profil bilginizi düzeltip YTU öğrencisi olduğunuzu belirtiniz!";
                            }
                            else
                            {
                                mdl = GetGsisOgrenciOgrenimBorcSorgula(mdl);
                            }
                        }
                        else
                        {
                            if (mdl.MulakatSonuclari != null && ((mdl.MulakatSonuclari.MulakatSonucTipID == MulakatSonucTipi.Asil && !mdl.MulakatSonuclari.KayitDurumID.HasValue)
                                                                    ||
                                                                  (mdl.MulakatSonuclari.MulakatSonucTipID == MulakatSonucTipi.Yedek && mdl.MulakatSonuclari.KayitDurumID == KayitDurumu.OnKayit)))
                            {
                                mdl = GetGsisProgramUcretKontrol(mdl);
                                if (!(mdl.SurecBaslangicTarihi <= DateTime.Now.Date && mdl.SurecBitisTarihi.Value.AddDays(40) >= DateTime.Now.Date))
                                {
                                    mdl.OdemeBaslangicTarihi = mdl.SurecBaslangicTarihi;
                                    mdl.OdemeBitisTarihi = mdl.SurecBitisTarihi.Value.AddDays(25);
                                    mdl.IsOdemeIslemiAcik = false;
                                    mdl.AciklamaSelectedLng = "Sadece Ödeme Tarih Aralığında Ödeme İşlemi Yapılabilir.";

                                }
                            }
                            else
                            {
                                mdl.IsOdemeVar = false;
                                if ((mdl.MulakatSonuclari.MulakatSonucTipID == MulakatSonucTipi.Asil && !mdl.MulakatSonuclari.KayitDurumID.HasValue))
                                {
                                    mdl.AciklamaSelectedLng = "Kayıt süreci tamamlanmış Asil adaylar dekont giriş işlemi yapamazlar.";
                                }
                                else if (mdl.MulakatSonuclari.MulakatSonucTipID == MulakatSonucTipi.Yedek && mdl.MulakatSonuclari.KayitDurumID != KayitDurumu.OnKayit)
                                {
                                    if (mdl.MulakatSonuclari.KayitDurumID == KayitDurumu.KayitOldu)
                                    {
                                        mdl.AciklamaSelectedLng = "Kayıt işlemleriniz tamamlanmıştır. Herhangi bir dekont girişi yapılamaz.";
                                    }
                                    else if (mdl.MulakatSonuclari.KayitDurumID == KayitDurumu.KayitOlmadi)
                                    {
                                        mdl.AciklamaSelectedLng = "Bu tercihiniz için kayıt iptali yapılmıştır. Herhangi bir dekont girişi yapılamaz.";
                                    }
                                    else if (!mdl.MulakatSonuclari.KayitDurumID.HasValue)
                                        mdl.AciklamaSelectedLng = "Yetkililer tarafından Ön Kayıt işlemleriniz yapılana dek dekont girişi yapamazsınız. Lütfen ön kayıt işleminizi bekleyiniz.";
                                }
                            }
                        }

                    }
                    else if (mdl.YokOgrenciKontroluYap && mdl.IsYerliOgrenci && YokODKontrolYap)
                    {

                        var YokKontrol = Management.yokStudentControl(mdl.TcKimlikNo.ToLong().Value);
                        mdl.IsYokOgrenciKaydiVar = YokKontrol.KayitVar;
                        if (YokKontrol.Hata)
                        {
                            mdl.IsOdemeVar = false;
                            mdl.YokOgrenciKontrolHataVar = true;
                            mdl.IsOdemeIslemiAcik = false;
                            mdl.AciklamaSelectedLng = YokKontrol.Mesaj;
                        }
                        else if (YokKontrol.KayitVar)
                        {
                            mdl.IsYokOgrenciKaydiVar = true;
                            mdl.IsOdemeVar = true;
                            string msj = "";
                            DateTime? BaslangicTarihi = null;
                            DateTime? BitisTarihi = null;
                            var IsSuccess = GetGsisUcretOdemeTarihKontrol(out msj, out BaslangicTarihi, out BitisTarihi);
                            if (IsSuccess)
                            {
                                mdl.OdemeBaslangicTarihi = BaslangicTarihi;
                                mdl.OdemeBitisTarihi = BitisTarihi;
                                if (mdl.KayitOldu == false)
                                {
                                    if (mdl.OdemeBaslangicTarihi.HasValue && mdl.OdemeBitisTarihi.HasValue)
                                    {
                                        if (mdl.OdemeBaslangicTarihi <= DateTime.Now.Date && mdl.OdemeBitisTarihi >= DateTime.Now.Date)
                                            mdl.IsOdemeIslemiAcik = true;
                                        else
                                            mdl.AciklamaSelectedLng = "Sadece Ödeme Tarih Aralığında Ödeme İşlemi Yapılabilir.";
                                    }
                                    else
                                        mdl.AciklamaSelectedLng = "Ödeme Tarih Aralığı Bulunamadı! Lütfen Enstitü İle Görüşünüz.";
                                }
                                mdl.AktifOgrenimListesi = YokKontrol.AktifOgrenimListesi;
                                mdl.IsOgrenimUcretiOrKatkiPayi = false;
                                mdl.OdenecekUcret = mdl.IstenecekKatkiPayiTutari;
                                mdl.OdemeDonemNo = 1;
                                if (mdl.IsDekontOrSanalPos || (mdl.IsOdemeVar))
                                {

                                    mdl.Aciklama = SurecAdiTr + " Kayıt Dönemi " + (ProgramAdiTr + " (" + mdl.ProgramKod + ")") + " Programına Ait " + mdl.OdemeDonemNo + ".Dönem Katkı Payı Ödemesi";
                                    mdl.AciklamaSelectedLng = SurecAdiTr + " Kayıt Dönemi " + (ProgramAdiTr + " (" + mdl.ProgramKod + ")") + " Programına Ait " + mdl.OdemeDonemNo + ".Dönem Katkı Payı Ödemesi";
                                }
                                var Tercih = db.BasvurularTercihleris.Where(p => p.UniqueID == _UniqueID).First();
                                Tercih.IsOgrenimUcretiOrKatkiPayi = false;
                                Tercih.ProgramUcret = mdl.IstenecekKatkiPayiTutari;
                                db.SaveChanges();
                            }
                            else
                            {
                                mdl.AciklamaSelectedLng = msj;
                            }
                        }
                        else
                        {
                            var Tercih = db.BasvurularTercihleris.Where(p => p.UniqueID == _UniqueID).First();
                            Tercih.IsOgrenimUcretiOrKatkiPayi = null;
                            Tercih.ProgramUcret = null;
                            db.SaveChanges();
                        }



                    }
                    if (mdl.IsOdemeVar)
                    {
                        if (mdl.OdemeListesi.Any(a => mdl.OdemeBaslangicTarihi <= a.DekontTarih.Value && mdl.OdemeBitisTarihi >= a.DekontTarih.Value))
                        {
                            mdl.IsOdemeIslemiAcik = false;
                            mdl.AciklamaSelectedLng = "Aktif ödeme tarih aralığında daha önceden yapılmış bir ödeme işleminiz bulunmaktadır.";

                        }
                    }
                    if (mdl.IsOgrenimUcretiOrKatkiPayi.HasValue && (mdl.IsDekontOrSanalPos || (mdl.IsOdemeVar && mdl.IsOdemeIslemiAcik)))
                    {
                        mdl.OdemeDonemNo = mdl.OdemeDonemNo == 0 ? 1 : mdl.OdemeDonemNo;

                        mdl.Aciklama = mdl.IsOgrenimUcretiOrKatkiPayi.Value ? (SurecAdiTr + " Kayıt Dönemi " + (ProgramAdiTr + " (" + mdl.ProgramKod + ")") + " Programına Ait " + mdl.OdemeDonemNo + ".Dönem Öğrenim Ücreti Ödemesi") : (SurecAdiTr + " Kayıt Dönemi " + (ProgramAdiTr + " (" + mdl.ProgramKod + ")") + " Programına Ait " + mdl.OdemeDonemNo + ".Dönem Katkı Payı Ödemesi");
                        mdl.AciklamaSelectedLng = mdl.Aciklama;
                    }
                    foreach (var item in mdl.OdemeListesi)
                    {

                        if (mdl.IsOgrenimUcretiOrKatkiPayi == true) item.Aciklama = mdl.SurecAdi + " Kayıt Dönemi " + (mdl.ProgramAdi + " (" + mdl.ProgramKod + ")") + " Programına Ait " + item.DonemNo + ".Dönem Öğrenim Ücreti Ödemesi";
                        else item.Aciklama = mdl.SurecAdi + " Kayıt Dönemi " + (mdl.ProgramAdi + " (" + mdl.ProgramKod + ")") + " Programına Ait " + item.DonemNo + ".Dönem Katkı Payı Ödemesi";

                    }
                }
                return mdl;
            }

        }

        #endregion

        #region Yetki/Kimlik
        public static void Update()
        {
            UpdateRoles2();
            UpdateMenus2();
        }
        static void UpdateRoles2()
        {
            var roleAttrs = Membership.Roles();
            using (LisansustuBasvuruSistemiEntities db = new LisansustuBasvuruSistemiEntities())
            {
                var dbRoller = db.Rollers.ToArray();
                foreach (var attr in roleAttrs)
                {
                    var dbrole = dbRoller.FirstOrDefault(p => p.RolID == attr.RolID);

                    if (dbrole == null)
                    {
                        db.Rollers.Add(new Roller
                        {
                            RolID = attr.RolID,
                            GorunurAdi = attr.GorunurAdi,
                            Aciklama = attr.Aciklama,
                            Kategori = attr.Kategori,
                            RolAdi = attr.RolAdi
                        });
                    }
                    else
                    {
                        dbrole.RolID = attr.RolID;
                        dbrole.GorunurAdi = attr.GorunurAdi;
                        dbrole.Aciklama = attr.Aciklama;
                        dbrole.Kategori = attr.Kategori;
                        dbrole.RolAdi = attr.RolAdi;
                    }
                    db.SaveChanges();
                }
            }
        }
        static void UpdateMenus2()
        {
            var menuAttrs = Membership.Menus();
            using (LisansustuBasvuruSistemiEntities db = new LisansustuBasvuruSistemiEntities())
            {
                var err = new List<string>();
                var dbMenus = db.Menulers.ToArray();
                foreach (var attr in menuAttrs)
                {
                    var dbmenu = dbMenus.FirstOrDefault(p => p.MenuID == attr.MenuID);
                    if (dbmenu == null)
                    {
                        var yeniMenu = new Menuler
                        {
                            MenuID = attr.MenuID,
                            MenuUrl = attr.MenuUrl,
                            BagliMenuID = attr.BagliMenuID,
                            MenuAdi = attr.MenuAdi,
                            MenuCssClass = attr.MenuCssClass,
                            MenuIconUrl = attr.MenuIconUrl,
                            DilCeviriYap = attr.DilCeviriYap,
                            YetkisizErisim = attr.YetkisizErisim,
                            YetkiliEnstitu = attr.YetkiliEnstituler,
                            AuthenticationControl = attr.AuthenticationControl,
                            SiraNo = attr.SiraNo
                        };
                        db.Menulers.Add(yeniMenu);
                        if (attr.BagliRoller != null && attr.BagliRoller.Length > 0)
                        {
                            var dbRoller = db.Rollers.Where(p => attr.BagliRoller.Contains(p.RolAdi)).ToArray();
                            foreach (var dbRole in dbRoller)
                            {
                                yeniMenu.Rollers.Add(dbRole);
                            }

                        }
                        db.SaveChanges();
                    }
                    else
                    {
                        dbmenu.MenuUrl = attr.MenuUrl;
                        dbmenu.BagliMenuID = attr.BagliMenuID;
                        dbmenu.MenuAdi = attr.MenuAdi;
                        dbmenu.MenuCssClass = attr.MenuCssClass;
                        dbmenu.MenuIconUrl = attr.MenuIconUrl;
                        dbmenu.DilCeviriYap = attr.DilCeviriYap;
                        dbmenu.YetkisizErisim = attr.YetkisizErisim;
                        dbmenu.YetkiliEnstitu = attr.YetkiliEnstituler;
                        dbmenu.AuthenticationControl = attr.AuthenticationControl;
                        dbmenu.SiraNo = attr.SiraNo;
                        if (attr.BagliRoller != null && attr.BagliRoller.Length > 0)
                        {
                            var dbRoller = db.Rollers.Where(p => attr.BagliRoller.Contains(p.RolAdi)).ToArray();
                            var nRols = dbRoller.Select(s => s.RolID).ToList();
                            var Yeni = dbmenu.Rollers.Where(a => !nRols.Contains(a.RolID)).ToList();
                            var varolan = dbmenu.Rollers.Where(a => nRols.Contains(a.RolID)).ToList();
                            //foreach (var item in varolan)
                            //{
                            //    try
                            //    {
                            //        db.Rollers.Remove(item);
                            //        db.SaveChanges();
                            //    }
                            //    catch (Exception ex)
                            //    {
                            //        err.Add("Menü Adı:" + dbmenu.MenuAdi + " \r\n Rol Adı:" + item.GorunurAdi + "\r\n Hata:" + ex.ToExceptionMessage());
                            //        stkTrc = ex.ToExceptionStackTrace();
                            //    }
                            //}
                            //foreach (var item in varolan)
                            //{
                            //    dbmenu.Rollers.Add(item);
                            //}
                            foreach (var dbRole in Yeni)
                            {
                                dbmenu.Rollers.Add(dbRole);

                            }
                        }
                        db.SaveChanges();
                    }
                }
                //if (err.Count > 0)
                //{
                //    Management.SistemBilgisiKaydet("Rol optimizasyonu yapılırken "+err.Count+" Rolün silme işlemi sırasında hata oluştu! \r\n" + string.Join("\r\n", err), stkTrc, BilgiTipi.Uyarı);
                //}
            }
        }



        public static Roller[] Roles { get; set; }
        public static Roller[] GetAllRoles()
        {
            if (Roles == null)
            {
                using (LisansustuBasvuruSistemiEntities db = new LisansustuBasvuruSistemiEntities())
                {
                    Roles = db.Rollers.Include("Menulers").ToArray();
                }
            }
            return Roles;
        }
        public static Menuler[] Menulers { get; set; }
        public static Menuler[] GetAllMenu()
        {
            if (Menulers == null)
            {
                using (LisansustuBasvuruSistemiEntities db = new LisansustuBasvuruSistemiEntities())
                {
                    Menulers = db.Menulers.OrderBy(o => o.SiraNo).ToArray();
                }
            }
            return Menulers;
        }
        public static Menuler[] GetUserMenus()
        {
            string UserName = HttpContext.Current.User.Identity.Name;

            if (UserName.IsNullOrWhiteSpace()) return new Menuler[] { };
            using (LisansustuBasvuruSistemiEntities db = new LisansustuBasvuruSistemiEntities())
            {
                var menus = new List<Menuler>();
                var kull = db.Kullanicilars.Where(p => p.KullaniciAdi == UserName).FirstOrDefault();
                if (kull == null) FormsAuthenticationUtil.SignOut();
                var kullRoll = kull.Rollers.SelectMany(s => s.Menulers).Distinct().OrderBy(o => o.SiraNo).ToList();
                var ygRoll = kull.YetkiGruplari.YetkiGrupRolleris.SelectMany(s => s.Roller.Menulers).Distinct().OrderBy(o => o.SiraNo).ToList();
                menus.AddRange(kullRoll);
                menus.AddRange(ygRoll.Where(p => !kullRoll.Any(a => a.MenuID == p.MenuID)));
                return menus.ToArray();
            }

        }

        public static URoles GetUserRoles(string userName = null)
        {
            string UserName = userName ?? HttpContext.Current.User.Identity.Name;
            var rolls = new URoles();
            using (LisansustuBasvuruSistemiEntities db = new LisansustuBasvuruSistemiEntities())
            {
                //return db.Rollers.ToArray();//-----------------------------------------------------------<<<Silinecek

                var kull = db.Kullanicilars.Where(p => p.KullaniciAdi == UserName).FirstOrDefault();
                if (kull != null)
                {
                    var kullRoll = kull.Rollers.ToList();

                    var ygRols = kull.YetkiGruplari.YetkiGrupRolleris.Select(s => s.Roller).ToList();
                    rolls.YetkiGrupID = kull.YetkiGrupID;
                    rolls.YetkiGrupAdi = kull.YetkiGruplari.YetkiGrupAdi;
                    rolls.YetkiGrupRolleri = ygRols;
                    rolls.TumRoller.AddRange(ygRols);
                    rolls.TumRoller.AddRange(kullRoll.Where(p => !ygRols.Any(a => a.RolID == p.RolID)));
                    rolls.EklenenRoller.AddRange(rolls.TumRoller.Where(p => rolls.YetkiGrupRolleri.Any(a => a.RolID == p.RolID) == false));
                    return rolls;


                }
                else
                {
                    FormsAuthenticationUtil.SignOut();
                    throw new SecurityException("Kullanıcı Tanımlı Değil");
                }

            }
        }
        public static URoles GetUserRoles(int KullaniciID)
        {
            var rolls = new URoles();
            using (LisansustuBasvuruSistemiEntities db = new LisansustuBasvuruSistemiEntities())
            {
                var kull = db.Kullanicilars.Where(p => p.KullaniciID == KullaniciID).FirstOrDefault();
                if (kull != null)
                {
                    var dRoll = kull.Rollers.ToList();

                    var ygRols = kull.YetkiGruplari.YetkiGrupRolleris.Select(s => s.Roller).ToList();
                    rolls.YetkiGrupID = kull.YetkiGrupID;
                    rolls.YetkiGrupAdi = kull.YetkiGruplari.YetkiGrupAdi;
                    rolls.YetkiGrupRolleri = ygRols;
                    rolls.TumRoller.AddRange(ygRols);
                    rolls.TumRoller.AddRange(dRoll.Where(p => !ygRols.Any(a => a.RolID == p.RolID)).Distinct());
                    rolls.EklenenRoller.AddRange(rolls.TumRoller.Where(p => rolls.YetkiGrupRolleri.Any(a => a.RolID == p.RolID) == false));
                    return rolls;
                }
                else
                    throw new SecurityException("Kullanıcı Tanımlı Değil");
            }
        }
        public static List<Roller> GetYetkiGrupRoles(int YetkiGrupID)
        {
            using (LisansustuBasvuruSistemiEntities db = new LisansustuBasvuruSistemiEntities())
            {
                var kull = db.YetkiGrupRolleris.Where(p => p.YetkiGrupID == YetkiGrupID).ToList();

                var rolIDs = kull.Select(s => s.RolID).ToList();
                return db.Rollers.Where(p => rolIDs.Contains(p.RolID)).ToList();


            }
        }
        public static bool InRoleCurrent(this string RoleName)
        {
            if (UserIdentity.Current != null && UserIdentity.Current.Roles != null)
            {
                return UserIdentity.Current.Roles.Any(a => a == RoleName);
            }
            else return false;
        }

        public static List<kulaniciProgramYetkiModel> GetKullaniciProgramlari(int KullaniciID, string EnstituKod)
        {
            using (LisansustuBasvuruSistemiEntities db = new LisansustuBasvuruSistemiEntities())
            {
                var kull = (from s in db.Programlars
                            join b in db.AnabilimDallaris on s.AnabilimDaliID equals b.AnabilimDaliID
                            join e in db.Enstitulers on b.EnstituKod equals e.EnstituKod
                            select new kulaniciProgramYetkiModel
                            {
                                EnstituKod = e.EnstituKod,
                                EnstituAdi = e.EnstituAd,
                                EnstituKisaAd = e.EnstituKisaAd,
                                AnabilimDaliAdi = b.AnabilimDaliAdi,
                                AnabilimDaliKod = b.AnabilimDaliKod,
                                ProgramKod = s.ProgramKod,
                                ProgramAdi = s.ProgramAdi,
                                YetkiVar = db.KullaniciProgramlaris.Any(a => a.KullaniciID == KullaniciID && a.ProgramKod == s.ProgramKod)
                            });

                if (EnstituKod.IsNullOrWhiteSpace() == false) kull = kull.Where(p => p.EnstituKod == EnstituKod);
                var data = kull.OrderByDescending(o => o.YetkiVar).ThenBy(t => t.AnabilimDaliAdi).ThenBy(t => t.ProgramAdi).ToList();
                return data;
            }
        }
        public static List<Kullanicilar> GetRoluOlanKullanicilar(List<string> RolAdi, string EnstituKod = null)
        {
            using (LisansustuBasvuruSistemiEntities db = new LisansustuBasvuruSistemiEntities())
            {
                var qRolKuls = db.Kullanicilars.Include("KullaniciProgramlaris").Include("Birimler").Include("KullaniciTipleri").Where(p => p.YetkiGruplari.YetkiGrupRolleris.Any(a => RolAdi.Contains(a.Roller.RolAdi)) || p.Rollers.Any(a => RolAdi.Contains(a.RolAdi))).AsQueryable();

                if (EnstituKod.IsNullOrWhiteSpace() == false) qRolKuls = qRolKuls.Where(p => p.EnstituKod == EnstituKod);
                var data = qRolKuls.OrderByDescending(o => o.Ad).ThenBy(t => t.Soyad).ToList();
                return data;
            }
        }
        public static List<CmbIntDto> GetCmbRoluOlanKullanicilar(List<string> RolAdi, string EnstituKod = null, bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var qRolKuls = db.Kullanicilars.Include("KullaniciProgramlaris").Include("Birimler").Include("KullaniciTipleri").Where(p => p.Rollers.Any(a => RolAdi.Contains(a.RolAdi))).AsQueryable();

                if (EnstituKod.IsNullOrWhiteSpace() == false) qRolKuls = qRolKuls.Where(p => p.EnstituKod == EnstituKod);
                var data = qRolKuls.OrderByDescending(o => o.Ad).ThenBy(t => t.Soyad).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.KullaniciID, Caption = item.Ad + " " + item.Soyad + " [Tc: " + item.TcKimlikNo + "]" });
                }
            }
            return dct;

        }
        public static List<CheckObject<Kullanicilar>> GetProgramYetkisiOlanKullanicilar(List<Kullanicilar> Kullanicilar, string ProgramKod, string EnstituKod = null)
        {
            var data = new List<CheckObject<Kullanicilar>>();

            var qData = Kullanicilar.Where(p => p.EnstituKod == (EnstituKod == null ? p.EnstituKod : EnstituKod))
                .Select(s => new CheckObject<Kullanicilar>
                {
                    Checked = s.KullaniciProgramlaris.Any(a => a.ProgramKod == ProgramKod),
                    Value = s
                }).OrderByDescending(o => o.Checked).ThenBy(t => t.Value.Ad).ThenBy(t => t.Value.Soyad).ToList();
            return qData;

        }
        public static List<CmbStringDto> GetCmbKullaniciProgramlari(int KullaniciID, string EnstituKod, bool BosSecimVar)
        {
            var dct = new List<CmbStringDto>();
            if (BosSecimVar) dct.Add(new CmbStringDto { Value = null, Caption = "" });
            using (LisansustuBasvuruSistemiEntities db = new LisansustuBasvuruSistemiEntities())
            {
                var kulPorgId = GetUserProgramKods(KullaniciID, EnstituKod);
                var kull = (from s in db.Programlars
                            join b in db.AnabilimDallaris on s.AnabilimDaliKod equals b.AnabilimDaliKod
                            where b.EnstituKod == EnstituKod && kulPorgId.Contains(s.ProgramKod)
                            select new CmbStringDto
                            {
                                Value = s.ProgramKod,
                                Caption = s.ProgramAdi
                            }).OrderBy(t => t.Caption);
                var data = kull.ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbStringDto { Value = item.Value, Caption = item.Caption });
                }
                return dct;
            }
        }

        public static void SetUserRoles(int KullaniciID, List<int> RolIDs, int YetkiGrupID)
        {
            using (LisansustuBasvuruSistemiEntities db = new LisansustuBasvuruSistemiEntities())
            {
                var k = db.Kullanicilars.Where(p => p.KullaniciID == KullaniciID).FirstOrDefault();
                if (k != null)
                {

                    var droles = k.Rollers.ToArray();
                    foreach (var drole in droles)
                        k.Rollers.Remove(drole);
                    k.YetkiGrupID = YetkiGrupID;
                    db.SaveChanges();
                    var uRoles = Management.GetUserRoles(k.KullaniciID);
                    RolIDs = RolIDs.Where(p => !uRoles.YetkiGrupRolleri.Any(a => a.RolID == p)).ToList();

                    if (RolIDs != null && RolIDs.Count > 0)
                    {
                        var newRoles = db.Rollers.Where(p => RolIDs.Contains(p.RolID));
                        foreach (var nr in newRoles)
                            k.Rollers.Add(nr);
                        db.SaveChanges();
                    }
                }
                else
                    throw new SecurityException("Kullanıcı Tanımlı Değil");
            }
        }


        public static frKullanicilar GetUser(string userName = null)
        {
            string UserName = userName ?? HttpContext.Current.User.Identity.Name;
            using (LisansustuBasvuruSistemiEntities db = new LisansustuBasvuruSistemiEntities())
            {

                var q = from s in db.Kullanicilars
                        where s.KullaniciAdi == UserName
                        select new frKullanicilar
                        {
                            KullaniciID = s.KullaniciID,
                            EnstituKod = s.EnstituKod,
                            ResimAdi = s.ResimAdi,
                            YetkiGrupID = s.YetkiGrupID,
                            KullaniciTipID = s.KullaniciTipID,
                            KullaniciTipAdi = s.KullaniciTipleri.KullaniciTipAdi,
                            Sifre = s.Sifre,
                            SicilNo = s.SicilNo,
                            Ad = s.Ad,
                            Soyad = s.Soyad,
                            UnvanID = s.UnvanID,
                            BirimID = s.BirimID,
                            CinsiyetID = s.CinsiyetID,
                            TcKimlikNo = s.TcKimlikNo,
                            PasaportNo = s.PasaportNo,
                            CepTel = s.CepTel,
                            EMail = s.EMail,
                            Adres = s.Adres,
                            YtuOgrencisi = s.YtuOgrencisi,
                            OgrenimTipKod = s.OgrenimTipKod,
                            ProgramKod = s.ProgramKod,
                            OgrenimDurumID = s.OgrenimDurumID,
                            FixedHeader = s.FixedHeader,
                            FixedSidebar = s.FixedSidebar,
                            ScrollSidebar = s.ScrollSidebar,
                            RightSidebar = s.RightSidebar,
                            CustomNavigation = s.CustomNavigation,
                            ToggledNavigation = s.ToggledNavigation,
                            BoxedOrFullWidth = s.BoxedOrFullWidth,
                            ThemeName = s.ThemeName,
                            BackgroundImage = s.BackgroundImage,
                            SifresiniDegistirsin = s.SifresiniDegistirsin,
                            IsAktif = s.IsAktif,
                            IsActiveDirectoryUser = s.IsActiveDirectoryUser,
                            IsAdmin = s.IsAdmin,
                            IsOnline = s.IsOnline,
                            Aciklama = s.Aciklama,
                            ParolaSifirlamaKodu = s.ParolaSifirlamaKodu,
                            ParolaSifirlamGecerlilikTarihi = s.ParolaSifirlamGecerlilikTarihi,
                            OlusturmaTarihi = s.OlusturmaTarihi,
                            LastLogonDate = s.LastLogonDate,
                            LastLogonIP = s.LastLogonIP,
                            IslemTarihi = s.IslemTarihi,
                            IslemYapanIP = s.IslemYapanIP
                        };
                var kull = q.FirstOrDefault();
                return kull;
            }
        }



        public static frKullanicilar GetUser(int KullaniciID)
        {

            using (LisansustuBasvuruSistemiEntities db = new LisansustuBasvuruSistemiEntities())
            {
                var q = from s in db.Kullanicilars
                        where s.KullaniciID == KullaniciID
                        select new frKullanicilar
                        {
                            EnstituKod = s.EnstituKod,
                            KullaniciID = s.KullaniciID,
                            ResimAdi = s.ResimAdi,
                            YetkiGrupID = s.YetkiGrupID,
                            KullaniciAdi = s.KullaniciAdi,
                            KullaniciTipID = s.KullaniciTipID,
                            KullaniciTipAdi = s.KullaniciTipleri.KullaniciTipAdi,
                            Sifre = s.Sifre,
                            SicilNo = s.SicilNo,
                            Ad = s.Ad,
                            Soyad = s.Soyad,
                            UnvanID = s.UnvanID,
                            BirimID = s.BirimID,
                            CinsiyetID = s.CinsiyetID,
                            TcKimlikNo = s.TcKimlikNo,
                            PasaportNo = s.PasaportNo,
                            CepTel = s.CepTel,
                            EMail = s.EMail,
                            Adres = s.Adres,
                            YtuOgrencisi = s.YtuOgrencisi,
                            OgrenimTipKod = s.OgrenimTipKod,
                            ProgramKod = s.ProgramKod,
                            OgrenimDurumID = s.OgrenimDurumID,
                            FixedHeader = s.FixedHeader,
                            FixedSidebar = s.FixedSidebar,
                            ScrollSidebar = s.ScrollSidebar,
                            RightSidebar = s.RightSidebar,
                            CustomNavigation = s.CustomNavigation,
                            ToggledNavigation = s.ToggledNavigation,
                            BoxedOrFullWidth = s.BoxedOrFullWidth,
                            ThemeName = s.ThemeName,
                            BackgroundImage = s.BackgroundImage,
                            SifresiniDegistirsin = s.SifresiniDegistirsin,
                            IsAktif = s.IsAktif,
                            IsActiveDirectoryUser = s.IsActiveDirectoryUser,
                            IsAdmin = s.IsAdmin,
                            IsOnline = s.IsOnline,
                            Aciklama = s.Aciklama,
                            ParolaSifirlamaKodu = s.ParolaSifirlamaKodu,
                            ParolaSifirlamGecerlilikTarihi = s.ParolaSifirlamGecerlilikTarihi,
                            OlusturmaTarihi = s.OlusturmaTarihi,
                            LastLogonDate = s.LastLogonDate,
                            LastLogonIP = s.LastLogonIP,
                            IslemTarihi = s.IslemTarihi,
                            IslemYapanIP = s.IslemYapanIP
                        };
                var kull = q.FirstOrDefault();
                return kull;
            }
        }
        public static frKullanicilar GetLoginUser(string KullaniciAdi)
        {
            using (LisansustuBasvuruSistemiEntities db = new LisansustuBasvuruSistemiEntities())
            {
                var q = from s in db.Kullanicilars.Where(p => p.IsAktif)
                        join ktl in db.KullaniciTipleris on new { s.KullaniciTipID } equals new { ktl.KullaniciTipID }
                        select new frKullanicilar
                        {
                            KullaniciID = s.KullaniciID,
                            ResimAdi = s.ResimAdi,
                            YetkiGrupID = s.YetkiGrupID,
                            KullaniciTipID = s.KullaniciTipID,
                            KullaniciTipAdi = ktl.KullaniciTipAdi,
                            KullaniciAdi = s.KullaniciAdi,
                            Sifre = s.Sifre,
                            SicilNo = s.SicilNo,
                            Ad = s.Ad,
                            Soyad = s.Soyad,
                            UnvanID = s.UnvanID,
                            BirimID = s.BirimID,
                            CinsiyetID = s.CinsiyetID,
                            TcKimlikNo = s.TcKimlikNo,
                            PasaportNo = s.PasaportNo,
                            CepTel = s.CepTel,
                            EMail = s.EMail,
                            Adres = s.Adres,
                            YtuOgrencisi = s.YtuOgrencisi,
                            OgrenciNo = s.OgrenciNo,
                            OgrenimTipKod = s.OgrenimTipKod,
                            ProgramKod = s.ProgramKod,
                            OgrenimDurumID = s.OgrenimDurumID,
                            FixedHeader = s.FixedHeader,
                            FixedSidebar = s.FixedSidebar,
                            ScrollSidebar = s.ScrollSidebar,
                            RightSidebar = s.RightSidebar,
                            CustomNavigation = s.CustomNavigation,
                            ToggledNavigation = s.ToggledNavigation,
                            BoxedOrFullWidth = s.BoxedOrFullWidth,
                            ThemeName = s.ThemeName,
                            BackgroundImage = s.BackgroundImage,
                            SifresiniDegistirsin = s.SifresiniDegistirsin,
                            IsAktif = s.IsAktif,
                            IsActiveDirectoryUser = s.IsActiveDirectoryUser,
                            IsAdmin = s.IsAdmin,
                            IsOnline = s.IsOnline,
                            Aciklama = s.Aciklama,
                            ParolaSifirlamaKodu = s.ParolaSifirlamaKodu,
                            ParolaSifirlamGecerlilikTarihi = s.ParolaSifirlamGecerlilikTarihi,
                            OlusturmaTarihi = s.OlusturmaTarihi,
                            LastLogonDate = s.LastLogonDate,
                            LastLogonIP = s.LastLogonIP,
                            IslemTarihi = s.IslemTarihi,
                            IslemYapanIP = s.IslemYapanIP
                        };
                var kull = q.Where(p => p.KullaniciAdi == KullaniciAdi || p.TcKimlikNo == KullaniciAdi || p.EMail == KullaniciAdi).FirstOrDefault();
                return kull;
            }
        }
        public static Kullanicilar Login(string KullaniciAdi, string Pwd)
        {
            using (LisansustuBasvuruSistemiEntities db = new LisansustuBasvuruSistemiEntities())
            {
                var sifre = Pwd.ComputeHash(Tuz);
                //var u = db.Kullanicilars.Where(p => p.KullaniciAdi == Uid && p.Sifre == sifre).FirstOrDefault();
                var u = db.Kullanicilars.Where(p => p.KullaniciAdi == KullaniciAdi || p.TcKimlikNo == KullaniciAdi || p.EMail == KullaniciAdi).FirstOrDefault();
                if (u != null)
                {
                    if (u.Sifre == sifre) return u;
                    return null;
                }
                return u;
            }
        }
        public static List<string> GetUserEnstituKods(int KullID)
        {
            using (LisansustuBasvuruSistemiEntities db = new LisansustuBasvuruSistemiEntities())
            {
                return db.KullaniciEnstituYetkileris.Where(a => a.KullaniciID == KullID).Select(s => s.EnstituKod).ToList();

            }
        }
        public static List<string> GetUserProgramKods(int KullID, string EnstituKod)
        {
            using (LisansustuBasvuruSistemiEntities db = new LisansustuBasvuruSistemiEntities())
            {
                var kullProg = (from kp in db.KullaniciProgramlaris.Where(a => a.KullaniciID == KullID)
                                join s in db.Programlars on kp.ProgramKod equals s.ProgramKod
                                join b in db.AnabilimDallaris on s.AnabilimDaliKod equals b.AnabilimDaliKod
                                where b.EnstituKod == EnstituKod
                                select s.ProgramKod
                               ).ToList();
                return kullProg;

            }
        }

        public static void CreateAdmin()
        {
            using (LisansustuBasvuruSistemiEntities db = new LisansustuBasvuruSistemiEntities())
            {
                if (db.Kullanicilars.Where(p => p.IsAktif == true).Count() == 0)
                {
                    var adm = db.Kullanicilars.Where(p => p.KullaniciAdi == "admin").FirstOrDefault();
                    if (adm == null)
                    {
                        #region Default Admin
                        db.Kullanicilars.Add(new Kullanicilar
                        {
                            KullaniciAdi = "admin",
                            IsAdmin = true,
                            Sifre = "123".ComputeHash(Management.Tuz),
                            IsAktif = true,
                            ResimAdi = "Images/avatars/DefaultUserImage.png",
                            Ad = "Administrator",
                            Aciklama = "Yönetici",
                            IsActiveDirectoryUser = false,
                            IsOnline = false,
                            SifresiniDegistirsin = false,
                            LastLogonIP = ""
                        });
                    }
                    else
                    {
                        adm.IsAktif = true;
                        adm.Sifre = "123".ComputeHash(Management.Tuz);
                        db.SaveChanges();
                    }
                    db.SaveChanges();
                    #endregion
                }
            }
        }
        public static void SetLastLogon()
        {
            string UserName = HttpContext.Current.User.Identity.Name;
            using (LisansustuBasvuruSistemiEntities db = new LisansustuBasvuruSistemiEntities())
            {
                var kull = db.Kullanicilars.Where(p => p.KullaniciAdi == UserName).FirstOrDefault();
                if (kull != null)
                {
                    kull.LastLogonDate = DateTime.Now;
                    kull.LastLogonIP = UserIdentity.Ip;
                    db.SaveChanges();
                }
            }
        }
        public static UserIdentity GetUserIdentity(string UserName)
        {

            var kull = Management.GetUser(UserName);
            if (kull == null)
            {
                FormsAuthenticationUtil.SignOut();
                return null;
            }


            var roller = Management.GetUserRoles(UserName);

            UserIdentity ui = new UserIdentity(UserName);

            ui.NameSurname = kull.Ad + " " + kull.Soyad;
            ui.Id = kull.KullaniciID;
            ui.Description = kull.EMail;
            ui.IsAdmin = kull.IsAdmin;
            //ui.Password = kull.Sifre;
            //ui.Domain = "";
            ui.HasToChahgePassword = kull.SifresiniDegistirsin;
            ui.IsActiveDirectoryImpersonateWorking = false;
            ui.IsActiveDirectoryUser = kull.IsActiveDirectoryUser;
            ui.Roles.AddRange(roller.TumRoller.Select(s => s.RolAdi).ToArray());
            ui.ImagePath = kull.ResimAdi.toKullaniciResim();
            ui.Informations.Add("FixedHeader", kull.FixedHeader);
            ui.Informations.Add("FixedSidebar", kull.FixedSidebar);
            ui.Informations.Add("ScrollSidebar", kull.ScrollSidebar);
            ui.Informations.Add("RightSidebar", kull.RightSidebar);
            ui.Informations.Add("CustomNavigation", kull.CustomNavigation);
            ui.Informations.Add("ToggledNavigation", kull.ToggledNavigation);
            ui.Informations.Add("BoxedOrFullWidth", kull.BoxedOrFullWidth);
            ui.Informations.Add("ThemeName", kull.ThemeName);
            ui.Informations.Add("BackgroundImage", kull.BackgroundImage);
            ui.KullaniciTipID = kull.KullaniciTipID;

            ui.EnstituKods = Management.GetUserEnstituKods(kull.KullaniciID);
            ui.SeciliEnstituKodu = kull.EnstituKod;
            #region Last Logon Information
            Management.SetLastLogon();
            #endregion
            return ui;
            //return RedirectToAction("HomePage", "Home");             
            throw new Exception("Not Impletemented Method for Account Logon");
        }
        public static void AddMessage(SystemInformation sis)
        {
            int? currid = UserIdentity.Current == null ? null : (int?)UserIdentity.Current.Id;
            using (LisansustuBasvuruSistemiEntities db = new LisansustuBasvuruSistemiEntities())
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
        public static bool IsContainsEnstitu(this string ekod)
        {
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                ekod = ekod.ToLower();
                var sdils = db.Enstitulers.Where(p => p.IsAktif).ToList();
                return sdils.Select(s => s.EnstituKisaAd.ToLower()).Any(a => a == ekod);
            }
        }


        public static string getSelectedEnstitu(string EKD)
        {
            return Enstitulers.Where(p => p.EnstituKisaAd.ToLower() == EKD.ToLower()).First().EnstituKod;
        }

        #endregion

        #region ComboData
        public static List<CmbStringDto> cmbGetMulakatSaatleri()
        {
            var dct = new List<CmbStringDto>();
            for (int i = 6; i <= 18; i++)
            {
                string saat = "";

                if (i.ToString().Length == 1) saat += "0" + i.ToString() + ":00";
                else saat += i.ToString() + ":00";
                dct.Add(new CmbStringDto { Value = saat, Caption = saat });
            }
            return dct;

        }
        public static List<CmbIntDto> GetKrediKartAktifYilList(bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var nowY = DateTime.Now.Year;
                var endY = nowY + 10;
                for (var i = nowY; i <= endY; i++)
                {
                    dct.Add(new CmbIntDto { Value = i, Caption = i.ToString() });
                }
            }
            return dct;

        }
        public static List<CmbIntDto> GetAYList(bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                for (var i = 1; i <= 12; i++)
                {
                    dct.Add(new CmbIntDto { Value = i, Caption = i.ToString() });
                }
            }
            return dct;

        }
        public static List<kmBsOtoMail> getBsMailZamanData()
        {

            var bsMList = new List<kmBsOtoMail>();
            bsMList.Add(new kmBsOtoMail { gID = 1, ZamanTipID = ZamanTipi.Gun, Zaman = 2, Gonderildi = false });
            bsMList.Add(new kmBsOtoMail { gID = 2, ZamanTipID = ZamanTipi.Gun, Zaman = 1, Gonderildi = false });
            //bsMList.Add(new kmBsOtoMail {gID=3, ZamanTipID = ZamanTipi.Saat, Zaman = 8, Gonderildi = false });
            //bsMList.Add(new kmBsOtoMail {gID=4, ZamanTipID = ZamanTipi.Saat, Zaman = 1, Gonderildi = false });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                foreach (var item in bsMList)
                {
                    var zamanAdi = db.ZamanTipleris.Where(p => p.ZamanTipID == item.ZamanTipID).First();
                    item.ZamanTipAdi = zamanAdi.ZamanTipAdi;
                }
            }
            return bsMList;
        }
        public static List<kmMzOtoMail> getZmMailZamanData(bool? chkD = null)
        {

            var bsMList = new List<kmMzOtoMail>();
            bsMList.Add(new kmMzOtoMail { gID = 1, Checked = chkD ?? false, MailSablonTipID = null, ZamanTipID = ZamanTipi.Gun, Zaman = 1, Gonderildi = false, Aciklama = "Başvuru süreci bitimine 1 Gün kala Taslak durumundaki başvuruları bildir (Öğrenci)" });
            bsMList.Add(new kmMzOtoMail { gID = 2, Checked = chkD ?? false, MailSablonTipID = null, ZamanTipID = ZamanTipi.Gun, Zaman = 2, Gonderildi = false, Aciklama = "Başvuru süreci bitimine 2 Gün kala Taslak durumundaki başvuruları bildir (Öğrenci)" });
            bsMList.Add(new kmMzOtoMail { gID = 3, Checked = chkD ?? false, MailSablonTipID = MailSablonTipi.Mez_EykTarihineGoreSrAlinmali, ZamanTipID = ZamanTipi.Gun, Zaman = 10, Gonderildi = false, Aciklama = "SR talebi yapma süreci bitimine 10 Gün kala SR talebi yapmayanları bildir (Danışman,Öğrenci)" });
            bsMList.Add(new kmMzOtoMail { gID = 4, Checked = chkD ?? false, MailSablonTipID = MailSablonTipi.Mez_EykTarihineGoreSrAlinmali, ZamanTipID = ZamanTipi.Gun, Zaman = 5, Gonderildi = false, Aciklama = "SR talebi yapma süreci bitimine 5 Gün kala SR talebi yapmayanları bildir (Danışman,Öğrenci)" });
            bsMList.Add(new kmMzOtoMail { gID = 5, Checked = chkD ?? false, MailSablonTipID = MailSablonTipi.Mez_EykTarihineGoreSrAlinmadi, ZamanTipID = ZamanTipi.Gun, Zaman = -5, Gonderildi = false, Aciklama = "SR talebi yapma sürecini 5 Gün aşanları bildir (Enstitü)" });

            bsMList.Add(new kmMzOtoMail { gID = 10, Checked = chkD ?? false, MailSablonTipID = MailSablonTipi.Mez_SinavDegerlendirmeHatirlantmaDanismanDR, ZamanTipID = ZamanTipi.Gun, Zaman = -1, Gonderildi = false, Aciklama = "DR Sınav sonucu değerlendirmesi için hatırlatma (Danışman)" });
            bsMList.Add(new kmMzOtoMail { gID = 11, Checked = chkD ?? false, MailSablonTipID = MailSablonTipi.Mez_SinavDegerlendirmeHatirlantmaDanismanYL, ZamanTipID = ZamanTipi.Gun, Zaman = -1, Gonderildi = false, Aciklama = "YL Sınav sonucu değerlendirmesi için hatırlatma (Danışman)" });

            bsMList.Add(new kmMzOtoMail { gID = 6, Checked = chkD ?? false, MailSablonTipID = MailSablonTipi.Mez_TezSinavSonucuSistemeGirilmedi, ZamanTipID = ZamanTipi.Gun, Zaman = -5, Gonderildi = false, Aciklama = "Sınav olup sonucunu 5 gün içinde getirmeyenleri bildir (Estitü,Danışman,Öğrenci)" });
            bsMList.Add(new kmMzOtoMail { gID = 7, Checked = chkD ?? false, MailSablonTipID = MailSablonTipi.Mez_TezKontrolTezDosyasiYuklenmeli, ZamanTipID = ZamanTipi.Gun, Zaman = -7, Gonderildi = false, Aciklama = "Sınav olup Tez Dosyasını 7 gün içinde yüklemeyenleri bildir (Öğrenci)" });
            bsMList.Add(new kmMzOtoMail { gID = 8, Checked = chkD ?? false, MailSablonTipID = MailSablonTipi.Mez_CiltliTezTeslimYapilmali, ZamanTipID = ZamanTipi.Gun, Zaman = 5, Gonderildi = false, Aciklama = "Tez teslim tutanağını teslim tarihine 5 gün kala teslim etmeyenleri bildir (Danışman,Öğrenci)" });
            bsMList.Add(new kmMzOtoMail { gID = 9, Checked = chkD ?? false, MailSablonTipID = MailSablonTipi.Mez_CiltliTezTeslimYapilmadi, ZamanTipID = ZamanTipi.Gun, Zaman = -5, Gonderildi = false, Aciklama = "Tez teslim tutanağını teslim tarihini 5 gün geçirenleri bildir (Enstitü)" });

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
        public static List<CmbIntDto> getTalepSurecleri(string EnstituKod, bool bosSecimVar = false)
        {
            var lst = new List<CmbIntDto>();
            if (bosSecimVar) lst.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = (from s in db.TalepSurecleris.Where(p => p.EnstituKod == EnstituKod)
                            orderby s.BaslangicTarihi descending
                            select new
                            {
                                s.TalepSurecID,
                                s.BaslangicTarihi,
                                s.BitisTarihi
                            }).ToList();
                foreach (var item in data)
                {
                    lst.Add(new CmbIntDto { Value = item.TalepSurecID, Caption = item.BaslangicTarihi.ToDateString() + " - " + item.BitisTarihi.ToDateString() });
                }
            }
            return lst;
        }

        public static List<CmbIntDto> getmezuniyetSurecleri(string EnstituKod, bool bosSecimVar = false)
        {
            var lst = new List<CmbIntDto>();
            if (bosSecimVar) lst.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = (from s in db.MezuniyetSurecis.Where(p => p.EnstituKod == EnstituKod)
                            join d in db.Donemlers on s.DonemID equals d.DonemID
                            orderby s.BaslangicTarihi descending
                            select new
                            {
                                s.MezuniyetSurecID,
                                s.BaslangicYil,
                                s.BitisYil,
                                s.SiraNo,
                                d.DonemAdi,
                                s.BaslangicTarihi,
                                s.BitisTarihi
                            }).ToList();
                foreach (var item in data)
                {
                    lst.Add(new CmbIntDto { Value = item.MezuniyetSurecID, Caption = (item.BaslangicYil + "/" + item.BitisYil + " " + item.DonemAdi + " " + item.SiraNo + " (" + item.BaslangicTarihi.ToDateString() + " - " + item.BitisTarihi.ToDateString() + ")") });
                }
            }
            return lst;
        }
        public static List<CmbIntDto> getMezuniyetSurecGroup(string EnstituKod, bool bosSecimVar = false)
        {
            var lst = new List<CmbIntDto>();
            if (bosSecimVar) lst.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var Data = (from s in db.MezuniyetSurecis.Where(p => p.EnstituKod == EnstituKod)
                            join d in db.Donemlers on s.DonemID equals d.DonemID
                            select new
                            {
                                s.DonemID,
                                s.BaslangicYil,
                                s.BitisYil,
                                d.DonemAdi
                            }).Distinct().OrderByDescending(o => o.BaslangicYil).ToList();
                foreach (var item in Data)
                {
                    lst.Add(new CmbIntDto { Value = (item.BaslangicYil + "" + item.DonemID).ToInt().Value, Caption = (item.BaslangicYil + "/" + (item.BitisYil) + " " + item.DonemAdi) });
                }
            }
            return lst;
        }
        public static List<CmbStringDto> getmezuniyetKayitDonemleri(string EnstituKod, int? MezuniyetSurecID = null, bool bosSecimVar = false)
        {
            var lst = new List<CmbStringDto>();
            if (bosSecimVar) lst.Add(new CmbStringDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {

                var qData = (from s in db.MezuniyetSurecis.Where(p => p.EnstituKod == EnstituKod)
                             join bsv in db.MezuniyetBasvurularis on s.MezuniyetSurecID equals bsv.MezuniyetSurecID
                             join d in db.Donemlers on bsv.KayitOgretimYiliDonemID equals d.DonemID
                             orderby s.BaslangicTarihi descending
                             select new
                             {
                                 s.MezuniyetSurecID,
                                 bsv.KayitOgretimYiliBaslangic,
                                 bsv.KayitOgretimYiliDonemID,
                                 d.DonemAdi
                             }).AsQueryable();
                if (MezuniyetSurecID.HasValue) qData = qData.Where(p => p.MezuniyetSurecID == MezuniyetSurecID.Value);
                var dataDst = qData.Select(s => new { s.KayitOgretimYiliBaslangic, s.KayitOgretimYiliDonemID, s.DonemAdi }).Distinct().OrderByDescending(o => o.KayitOgretimYiliBaslangic).ThenBy(t => t.KayitOgretimYiliDonemID).ToList();
                foreach (var item in dataDst)
                {
                    lst.Add(new CmbStringDto { Value = item.KayitOgretimYiliBaslangic + "_" + item.KayitOgretimYiliDonemID, Caption = (item.KayitOgretimYiliBaslangic + "/" + (item.KayitOgretimYiliBaslangic + 1) + " " + item.DonemAdi) });
                }
            }
            return lst;
        }
        public static List<CmbMultyTypeDto> getOkunanDonemList(this int MezuniyetSurecID, int BasYil, int DonemID)
        {
            var kdonems = new List<CmbMultyTypeDto>();
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var surec = db.MezuniyetSurecis.Where(p => p.MezuniyetSurecID == MezuniyetSurecID).First();

                for (int i = BasYil; i <= surec.BaslangicYil; i++)
                {
                    for (int r = 1; r <= 2; r++)
                    {
                        bool add = true;
                        if (i == BasYil)
                        {
                            if (DonemID == 2 && r == 1) add = false;
                        }
                        else if (i == surec.BaslangicYil)
                        {
                            if (surec.DonemID == 1 && r == 2) add = false;
                        }
                        if (add) kdonems.Add(new CmbMultyTypeDto { Inx = i, Value = r });
                    }
                }
            }
            return kdonems;
        }
        public static List<CmbIntDto> getbasvuruSurecleriDekont(string EKD, bool bosSecimVar = false)
        {
            var lst = new List<CmbIntDto>();
            string EnstituKod = getSelectedEnstitu(EKD);
            if (bosSecimVar) lst.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = (from s in db.BasvuruSurecs.Where(p => p.EnstituKod == EnstituKod)
                            join d in db.Donemlers on s.DonemID equals d.DonemID
                            where s.Basvurulars.Any(a => a.BasvurularTercihleris.Any(a2 => a2.Programlar.Ucretli))
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
        public static List<CmbStringDto> getbasvuruSurecleriTercihlerOdeme(int? BasvuruSurecID, int KullaniciID, bool bosSecimVar = false, bool IsSanalPosOrDekont = false)
        {
            var lst = new List<CmbStringDto>();
            BasvuruSurecID = BasvuruSurecID == null ? 0 : BasvuruSurecID;
            if (bosSecimVar) lst.Add(new CmbStringDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {

                var data = (from s in db.BasvurularTercihleris.Where(p => p.Basvurular.KullaniciID == KullaniciID && p.Basvurular.BasvuruSurecID == BasvuruSurecID)
                            join b in db.Basvurulars on s.BasvuruID equals b.BasvuruID
                            join p in db.Programlars on s.ProgramKod equals p.ProgramKod
                            join ot in db.OgrenimTipleris.Where(p => p.OgrenimTipKod == (IsSanalPosOrDekont ? OgrenimTipi.TezsizYuksekLisans : p.OgrenimTipKod)) on new { s.Basvurular.BasvuruSurec.EnstituKod, s.OgrenimTipKod } equals new { ot.EnstituKod, ot.OgrenimTipKod }
                            join ms in db.MulakatSonuclaris on s.BasvuruTercihID equals ms.BasvuruTercihID into defMs
                            from MS in defMs.DefaultIfEmpty()
                            orderby s.BasvuruTercihID
                            select new
                            {
                                s.UniqueID,
                                s.ProgramKod,
                                p.ProgramAdi,
                                ot.OgrenimTipAdi,
                                OdemeYapabilir = b.BasvurularTercihleris.Any(a => a.MulakatSonuclaris.Any(a2 => a2.KayitDurumID.HasValue && a2.KayitDurumlari.IsKayitOldu == true))
                                                    ? b.BasvurularTercihleris.Any(a => a.BasvuruTercihID == s.BasvuruTercihID && a.MulakatSonuclaris.Any(a2 => a2.KayitDurumID.HasValue && a2.KayitDurumlari.IsKayitOldu == true))
                                                    : (MS != null && (MS.MulakatSonucTipID == MulakatSonucTipi.Asil || MS.MulakatSonucTipID == MulakatSonucTipi.Yedek))

                            }).ToList();
                foreach (var item in data)
                {

                    if (item.OdemeYapabilir)
                    {
                        lst.Add(new CmbStringDto { Value = item.UniqueID.ToString(), Caption = (item.ProgramAdi + "[" + item.ProgramKod + "], " + item.OgrenimTipAdi) });
                    }

                }
                return lst;
            }
        }
        public static List<CmbStringDto> getAkademikTarih(bool bosSecimVar = false, int? EklenecekYil = null)
        {
            var lst = new List<CmbStringDto>();
            var donems = new List<CmbIntDto>();
            if (bosSecimVar) lst.Add(new CmbStringDto { Value = "", Caption = "" });
            int addY = EklenecekYil.HasValue ? EklenecekYil.Value : 1;
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                donems = db.Donemlers.OrderBy(o => o.DonemID).Select(s => new CmbIntDto { Value = s.DonemID, Caption = s.DonemAdi }).ToList();
            }
            for (int i = (DateTime.Now.Year + addY); i >= 2012; i--)
            {
                lst.Add(new CmbStringDto { Value = i.ToString() + "/" + (i + 1).ToString() + "/2", Caption = i.ToString() + "/" + (i + 1).ToString() + " " + donems.Where(p => p.Value == 2).First().Caption });
                lst.Add(new CmbStringDto { Value = i.ToString() + "/" + (i + 1).ToString() + "/1", Caption = i.ToString() + "/" + (i + 1).ToString() + " " + donems.Where(p => p.Value == 1).First().Caption });
            }
            return lst;
        }
        public static List<CmbStringDto> getGrupKod(int GrupSayisi, string GrupAdi = "Grup", bool bosSecimVar = false)
        {
            var lst = new List<CmbStringDto>();
            if (bosSecimVar) lst.Add(new CmbStringDto());
            for (int i = 1; i <= GrupSayisi; i++)
            {
                lst.Add(new CmbStringDto { Value = GrupAdi + i.ToString(), Caption = GrupAdi + i.ToString() });
            }
            return lst;
        }
        public static List<CmbBoolDto> getVeVeya(bool bosSecimVar = false)
        {
            var lst = new List<CmbBoolDto>();
            if (bosSecimVar) lst.Add(new CmbBoolDto());
            lst.Add(new CmbBoolDto { Value = true, Caption = "Ve" });
            lst.Add(new CmbBoolDto { Value = false, Caption = "Veya" });

            return lst;
        }
        public static List<CmbIntDto> getTarihKriterSecim(bool bosSecimVar = false)
        {
            var lst = new List<CmbIntDto>();
            if (bosSecimVar) lst.Add(new CmbIntDto());
            lst.Add(new CmbIntDto { Value = TarihKriterSecim.SecilenTarihVeOncesi, Caption = "Seçilen Tarih ve Öncesi" });
            lst.Add(new CmbIntDto { Value = TarihKriterSecim.SecilenTarihAraligi, Caption = "Seçilen Tarih Aralığı" });
            lst.Add(new CmbIntDto { Value = TarihKriterSecim.SecilenTarihVeSonrasi, Caption = "Seçilen Tarih ve Sonrası" });

            return lst;
        }
        public static CmbStringDto getAkademikBulundugumuzTarih(DateTime? tarih = null)
        {
            var mdl = new CmbStringDto();
            var trh = tarih.HasValue ? tarih.Value.TodateToShortDate() : DateTime.Now.TodateToShortDate();
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var eoy = trh.toEoYilBilgi();
                var sDonem = db.Donemlers.Where(p => p.DonemID == eoy.Donem).First();
                eoy.DonemAdi = sDonem.DonemAdi;
                mdl.Value = eoy.BaslangicYili + "/" + eoy.BitisYili + "/" + eoy.Donem;
                mdl.Caption = eoy.BaslangicYili + " / " + eoy.BitisYili + " " + eoy.DonemAdi;

            }
            return mdl;
        }
        public static List<CmbStringDto> getBelgeTeslimSaatler()
        {
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var saatler = db.BelgeTipDetaySaatlers.OrderBy(o => o.TalepBaslangicSaat).Select(s => new { s.TeslimBaslangicSaat, s.TeslimBitisSaat }).Distinct().ToList();
                var lst = new List<CmbStringDto>();
                lst.Add(new CmbStringDto { Caption = "" });
                lst.Add(new CmbStringDto { Value = "00:00-23:59", Caption = "Bugün verilecekler (Tümü)" });
                foreach (var item in saatler)
                {
                    var bsSt = string.Format("{0:hh\\:mm}", item.TeslimBaslangicSaat);
                    var btSt = string.Format("{0:hh\\:mm}", item.TeslimBitisSaat);
                    lst.Add(new CmbStringDto { Value = (bsSt + "-" + btSt), Caption = "Bugün verilecekler (" + (bsSt + "-" + btSt) + ")" });
                }

                return lst;
            }
        }
        public static BelgeTipDetaySaatler getSelectedSaat(DateTime IslemTarihi, int BelgeTipID, int OgrenimDurumID, string _EnstituKod)
        {
            var rtatilDurum = BelgeTalepAyar.BelgeTalebiResmiTatilDurum.getAyarBT(_EnstituKod, "0").ToBoolean().Value;
            TimeSpan talepZamani = new TimeSpan(IslemTarihi.Hour, IslemTarihi.Minute, IslemTarihi.Second);
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var dofW = IslemTarihi.DayOfWeek.ToString("d").ToInt().Value;
                var Bastarih = IslemTarihi.TodateToShortDate();
                var tarih = IslemTarihi.TodateToShortDate();
            gehBiliBili:
                var btSaat = db.BelgeTipDetaySaatlers.Include("BelgeTipDetay").Where(p => p.BelgeTipDetay.OgrenimDurumID == OgrenimDurumID && p.HaftaGunID == dofW && p.BelgeTipDetay.BelgeTipDetayBelgelers.Any(a => a.BelgeTipID == BelgeTipID) && p.TalepBaslangicSaat <= talepZamani && p.TalepBitisSaat >= talepZamani).FirstOrDefault();
                tarih = tarih.AddDays(btSaat.EklenecekGun);

                if (rtatilDurum)
                {
                    var uygunlukKontrol = getUygunKontrol(tarih);
                    if (uygunlukKontrol.Value.Value == false)
                    {
                        tarih = uygunlukKontrol.Caption.Value;
                        dofW = tarih.DayOfWeek.ToString("d").ToInt().Value;

                        talepZamani = new TimeSpan(1, 1, 1);
                        goto gehBiliBili;
                    }
                    btSaat.EklenecekGun = (tarih - Bastarih).TotalDays.toIntObj().Value;
                }
                return btSaat;
            }
        }
        public static CmbBoolDatetimeDto getUygunKontrol(DateTime nTarih)
        {
            var mdl = new CmbBoolDatetimeDto();
            mdl.Value = true;
            mdl.Caption = nTarih;
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                bool success = false;
                while (success == false)
                {
                    success = true;
                    var ResmiTatilDegisen = db.SROzelTanimlars.Where(p => p.IsAktif && p.SROzelTanimTipID == SROzelTanimTip.ResmiTatilDegisen && p.BasTarih.Value <= mdl.Caption && p.BitTarih >= mdl.Caption).FirstOrDefault();
                    if (ResmiTatilDegisen != null)
                    {
                        success = false;
                        mdl.Value = false;
                        mdl.Caption = nTarih = ResmiTatilDegisen.BitTarih.Value.AddDays(1);
                    }
                    else
                    {
                        var ResmiTatilSabit = db.SROzelTanimlars.Where(p => p.IsAktif && p.SROzelTanimTipID == SROzelTanimTip.ResmiTatilSabit && p.Ay.Value == mdl.Caption.Value.Month && p.Gun == mdl.Caption.Value.Day).FirstOrDefault();
                        if (ResmiTatilSabit != null)
                        {
                            success = false;
                            mdl.Value = false;
                            mdl.Caption = nTarih = nTarih.AddDays(1);
                        }
                    }
                }
            }
            return mdl;
        }
        public static BelgeTalepleri getBelge(int BelgeTalepID)
        {
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.BelgeTalepleris.Where(p => p.BelgeTalepID == BelgeTalepID).FirstOrDefault();
                return data;
            }
        }
        public static CmbIntDto toVerilmeBilgisi(this int BelgeTalepID, string IslemTipListeAdi)
        {
            var html = "";
            var mdl = new CmbIntDto();
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var belge = db.BelgeTalepleris.Where(p => p.BelgeTalepID == BelgeTalepID).FirstOrDefault();
                if (belge.BelgeDurumID == BelgeTalepDurum.TalepEdildi || belge.BelgeDurumID == BelgeTalepDurum.Hazirlaniyor || belge.BelgeDurumID == BelgeTalepDurum.Hazirlandi)
                {
                    var verilecekTarih = belge.TalepTarihi.AddDays(belge.EklenecekGun).TodateToShortDate();
                    var days = (verilecekTarih - DateTime.Now.TodateToShortDate());
                    var day = Convert.ToInt32(days.Days);
                    bool gelecek = (days.Days > 0);
                    mdl.Value = days.Days;
                    var saatAralik = belge.TeslimBaslangicSaat.Value.ToString(@"hh\:mm") + "-" + belge.TeslimBitisSaat.Value.ToString(@"hh\:mm");
                    var _durum = "(" + belge.BelgeDurumlari.DurumAdi + ")";
                    if (verilecekTarih == DateTime.Now.TodateToShortDate())
                    {
                        html += "<span style='font-size:9pt;font-weight:bold;'>" + verilecekTarih.ToString("dd.MM.yyyy") + " " + saatAralik + "</span> <br /><span style='font-size:8.5pt;'>Bu Gün Verilecek " + _durum + "</span>";
                    }
                    else if (day == 1)
                    {
                        html += "<span style='font-size:9pt;font-weight:bold;'>" + verilecekTarih.ToString("dd.MM.yyyy") + " " + saatAralik + "</span> <br /><span style='font-size:8.5pt;'>Yarın Verilecek " + _durum + "</span>";
                    }
                    else
                    {
                        if (gelecek)
                        {
                            html += "<span style='font-size:9pt;font-weight:bold;'>" + verilecekTarih.ToString("dd.MM.yyyy") + " " + saatAralik + "</span> <br /><span style='font-size:8.5pt;'>" + day + " Gün Sonra Verilecek " + _durum + "</span>";
                        }
                        else
                        {
                            html += "<span style='font-size:9pt;font-weight:bold;'>" + verilecekTarih.ToString("dd.MM.yyyy") + " " + saatAralik + "</span> <br /><span style='font-size:8.5pt;'>" + Math.Abs(day) + " Gün Önce Verilmeliydi" + _durum + "</span>";

                        }
                    }
                }
                else if (belge.BelgeDurumID == BelgeTalepDurum.Verildi)
                {
                    html += "<span style='font-size:9pt;font-weight:bold;'>" + belge.IslemTarihi.ToString("dd-MM-yyyy HH:mm:ss") + "</span> <br /><span style='font-size:8.5pt;'>Tarihinde Verildi</span>";

                }
                else
                {
                    html += "<span style='font-size:9pt;font-weight:bold;'>" + belge.IslemTarihi.ToString("dd-MM-yyyy HH:mm:ss") + "</span> <br /><span style='font-size:8.5pt;'>Tarihinde " + IslemTipListeAdi + "</span>";
                }
                mdl.Caption = html;
                return mdl;
            }
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
        public static List<CmbBoolDto> cmbAktifPasifData(bool bosSecimVar = false)
        {
            var dct = new List<CmbBoolDto>();
            if (bosSecimVar) dct.Add(new CmbBoolDto { Value = null, Caption = "" });
            dct.Add(new CmbBoolDto { Value = true, Caption = "Aktif" });
            dct.Add(new CmbBoolDto { Value = false, Caption = "Pasif" });
            return dct;

        }
        public static List<CmbBoolDto> cmbAcikKapaliData(bool bosSecimVar = false)
        {
            var dct = new List<CmbBoolDto>();
            if (bosSecimVar) dct.Add(new CmbBoolDto { Value = null, Caption = "" });
            dct.Add(new CmbBoolDto { Value = true, Caption = "Kapalı" });
            dct.Add(new CmbBoolDto { Value = false, Caption = "Açık" });
            return dct;

        }
        public static List<CmbBoolDto> cmbDosyaEkiDurumData(bool bosSecimVar = false)
        {
            var dct = new List<CmbBoolDto>();
            if (bosSecimVar) dct.Add(new CmbBoolDto { Value = null, Caption = "" });
            dct.Add(new CmbBoolDto { Value = true, Caption = "Dosya Eki Olanlar" });
            dct.Add(new CmbBoolDto { Value = false, Caption = "Dosya Eki Olmayanlar" });
            return dct;

        }
        public static List<CmbBoolDto> cmbOnayDurumData(bool bosSecimVar = false)
        {
            var dct = new List<CmbBoolDto>();
            if (bosSecimVar) dct.Add(new CmbBoolDto { Value = null, Caption = "" });
            dct.Add(new CmbBoolDto { Value = true, Caption = "Onaylandı" });
            dct.Add(new CmbBoolDto { Value = false, Caption = "Onaylanmadı" });
            return dct;

        }
        public static List<CmbBoolDto> cmbJOAsilYedekDurumData(bool bosSecimVar = false)
        {
            var dct = new List<CmbBoolDto>();
            if (bosSecimVar) dct.Add(new CmbBoolDto { Value = null, Caption = "" });
            dct.Add(new CmbBoolDto { Value = true, Caption = "Asil" });
            dct.Add(new CmbBoolDto { Value = false, Caption = "Yedek" });
            return dct;
        }
        public static List<CmbBoolDto> cmbJOEykGonderimDurumData(bool bosSecimVar = false, DateTime? OnayTarihi = null)
        {
            var dct = new List<CmbBoolDto>();
            if (bosSecimVar) dct.Add(new CmbBoolDto { Value = null, Caption = "" });
            dct.Add(new CmbBoolDto { Value = true, Caption = "EYK'ya gönderimi onaylandı" + (OnayTarihi.HasValue ? " (" + OnayTarihi.ToFormatDateAndTime() + ")" : "") });
            dct.Add(new CmbBoolDto { Value = false, Caption = "EYK'ya gönderimi onaylanmadı" });
            return dct;
        }
        public static List<CmbBoolDto> cmbJOEykOnayDurumData(bool bosSecimVar = false)
        {
            var dct = new List<CmbBoolDto>();
            if (bosSecimVar) dct.Add(new CmbBoolDto { Value = null, Caption = "" });
            dct.Add(new CmbBoolDto { Value = true, Caption = "Eyk'da Onaylandı" });
            dct.Add(new CmbBoolDto { Value = false, Caption = "Eyk'da Onaylanmadı" });
            return dct;

        }
        public static List<CmbBoolDto> cmbDoluBosData(bool bosSecimVar = false)
        {
            var dct = new List<CmbBoolDto>();
            if (bosSecimVar) dct.Add(new CmbBoolDto { Value = null, Caption = "" });
            dct.Add(new CmbBoolDto { Value = true, Caption = "Dolu" });
            dct.Add(new CmbBoolDto { Value = false, Caption = "Boş" });
            return dct;

        }
        public static List<CmbBoolDto> cmbGrupOlanData(bool bosSecimVar = false)
        {
            var dct = new List<CmbBoolDto>();
            if (bosSecimVar) dct.Add(new CmbBoolDto { Value = null, Caption = "Tümü" });
            dct.Add(new CmbBoolDto { Value = true, Caption = "Grup Olanlar" });
            dct.Add(new CmbBoolDto { Value = false, Caption = "Grup Olmayanlar" });
            return dct;

        }
        public static List<CmbBoolDto> cmbKazanmaDurumData(bool bosSecimVar = false)
        {
            var dct = new List<CmbBoolDto>();
            if (bosSecimVar) dct.Add(new CmbBoolDto { Value = null, Caption = "Tümü" });
            dct.Add(new CmbBoolDto { Value = true, Caption = "Kazananlar" });
            dct.Add(new CmbBoolDto { Value = false, Caption = "Kazanamayanlar" });
            return dct;

        }

        public static List<CmbIntDto> cmbEsitlikKosulu()
        {
            var dct = new List<CmbIntDto>();

            dct.Add(new CmbIntDto { Value = -1, Caption = "<" });
            dct.Add(new CmbIntDto { Value = 0, Caption = "==" });
            dct.Add(new CmbIntDto { Value = 1, Caption = ">" });
            return dct;

        }
        public static List<CmbBoolDto> cmbItirazAktifPasifData(bool bosSecimVar = false)
        {
            var dct = new List<CmbBoolDto>();
            if (bosSecimVar) dct.Add(new CmbBoolDto { Value = null, Caption = "" });
            dct.Add(new CmbBoolDto { Value = false, Caption = "Açık Olan İtirazlar" });
            dct.Add(new CmbBoolDto { Value = true, Caption = "Kapalı Olan İtirazlar" });
            return dct;

        }

        public static List<CmbIntDto> cmbBasvuruEvlilikDurumu(bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            dct.Add(new CmbIntDto { Value = 1, Caption = "Evli Olan Kullanıcıları Listele" });
            dct.Add(new CmbIntDto { Value = 2, Caption = "Bekar Olan Kullanıcıları Listele" });
            return dct;

        }


        public static List<CmbIntDto> cmbSehirler(bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.Sehirlers.OrderBy(o => o.Ad).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.SehirKod, Caption = item.Ad });
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
        public static List<CmbIntDto> cmbKullaniciTipleri(bool bosSecimVar = false, bool IsHesapOlusturFiltre = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.KullaniciTipleris.Where(p => p.YeniHesapOlusturabilir == (IsHesapOlusturFiltre ? true : p.YeniHesapOlusturabilir)).OrderBy(o => o.KullaniciTipAdi);
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.KullaniciTipID, Caption = item.KullaniciTipAdi });
                }
            }
            return dct;

        }
        public static List<CmbIntDto> cmbKullaniciTipleri(bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.KullaniciTipleris.Where(p => p.KurumIci == false).OrderBy(o => o.KullaniciTipAdi);
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.KullaniciTipID, Caption = item.KullaniciTipAdi });
                }
            }
            return dct;

        }
        public static List<CmbIntDto> cmbKullaniciTipleriOgrenciler(bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.KullaniciTipleris.Where(p => p.BasvuruYapabilir).OrderBy(o => o.KullaniciTipAdi);
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.KullaniciTipID, Caption = item.KullaniciTipAdi });
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
        public static List<CmbStringDto> cmbMezuniyetJofUnvanlar(bool bosSecimVar = false)
        {
            var dct = new List<CmbStringDto>();
            if (bosSecimVar) dct.Add(new CmbStringDto { Value = "", Caption = "" });
            var LstUnvan = new List<string>();
            LstUnvan = new List<string> { "PROF.DR.", "DOÇ.DR.", "DR.ÖĞR.ÜYE." };

            foreach (var item in LstUnvan)
            {
                dct.Add(new CmbStringDto { Value = item, Caption = item });
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
        public static List<CmbIntDto> cmbBasvuruDurum(bool bosSecimVar = false, bool IsGelenBasvuruYetki = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.BasvuruDurumlaris.Where(p => p.IsAktif && (!IsGelenBasvuruYetki ? p.BasvuranGorsun : p.YoneticiGorsun)).OrderBy(o => o.BasvuruDurumID).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.BasvuruDurumID, Caption = item.BasvuruDurumAdi });
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
        public static List<int> makaleYayinDurumIstenenler()
        {
            return new List<int> { 5, 4 };//ulusal ulusrarası  makale    
        }
        public static bool makaleYayinDurumIsteniyormu(this int YayinTurID)
        {
            return makaleYayinDurumIstenenler().Contains(YayinTurID);
        }
        public static CmbIntDto GetCevaplanmamisMesajCount(string EnstituKod)
        {

            var model = new CmbIntDto();
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var EnstituKods = GetUserEnstituKods(UserIdentity.Current.Id);
                var qListe = db.Mesajlars.Where(p => EnstituKods.Contains(p.EnstituKod) && p.EnstituKod == EnstituKod && p.UstMesajID.HasValue == false && !p.IsAktif && p.Silindi == false).OrderByDescending(o => (o.Mesajlar1.Any() ? o.Mesajlar1.Select(s => s.Tarih).Max() : o.Tarih)).AsQueryable();
                var Liste = qListe.Take(20).ToList();
                var htmlContent = "";
                foreach (var item in Liste)
                {

                    var kul = item.Kullanicilar;
                    htmlContent += "<a href='javascript:void(0);' class='list-group-item' style='padding-top:0px;padding-bottom:0px;padding-left:2px;padding-right:-1px;'>" +
                                      "<table style='table-layout:fixed;width:100%;'>" +
                                              "<tr>" +
                                                  "<td width='40'><img style='width:40px;height:40px;' src ='" + ((item.KullaniciID > 0 ? item.Kullanicilar.ResimAdi : "").toKullaniciResim()) + "' class='pull-left' ></td>" +
                                                  "<td><span class='contacts-title'>" + item.AdSoyad + "</span><span style='float:right;font-size:8pt;'><b>" + (item.Mesajlar1.Any() ? item.Mesajlar1.Select(s => s.Tarih).Max().ToFormatDateAndTime() : item.Tarih.ToFormatDateAndTime()) + "</b></span><p><b>Konu:</b> " + item.Konu + "</p></td>" +
                                              "</tr>" +
                                          "</table>" +
                                  "</a>";
                }
                model.Value = qListe.Count();
                model.Caption = htmlContent;
                return model;

            }
        }

        public static List<CmbIntDto> cmbMezuniyetYayinDurum(bool bosSecimVar = false, bool Tumu = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.MezuniyetYayinKontrolDurumlaris.Where(p => p.IsAktif && (Tumu ? true : p.BasvuranGorsun)).OrderBy(o => o.MezuniyetYayinKontrolDurumID).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.MezuniyetYayinKontrolDurumID, Caption = item.MezuniyetYayinKontrolDurumAdi });
                }
            }
            return dct;

        }
        public static List<CmbIntDto> cmbMezuniyetYayinDurumListe(bool bosSecimVar = false, bool Tumu = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.MezuniyetYayinKontrolDurumlaris.Where(p => (Tumu ? true : p.BasvuranGorsun)).OrderBy(o => o.MezuniyetYayinKontrolDurumID).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.MezuniyetYayinKontrolDurumID, Caption = item.MezuniyetYayinKontrolDurumAdi });
                }
            }

            return dct;

        }
        public static List<CmbIntDto> cmbJuriOneriFormuDurumu(bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            dct.Add(new CmbIntDto { Value = 0, Caption = "Form oluşturulmadı" });
            dct.Add(new CmbIntDto { Value = 1, Caption = "Form oluşturuldu" });
            dct.Add(new CmbIntDto { Value = 2, Caption = "Eyk'Ya Gonderimi Onaylandi" });
            dct.Add(new CmbIntDto { Value = 3, Caption = "Eyk'Ya Gonderimi Onaylanmadi" });
            dct.Add(new CmbIntDto { Value = 4, Caption = "Eyk'Da Onaylandı" });
            dct.Add(new CmbIntDto { Value = 5, Caption = "Eyk'Da Onaylanmadı" });

            return dct;

        }
        public static List<int> getMBasvuruDurumIDs()
        {
            return new List<int>() { BasvuruDurumu.Taslak, BasvuruDurumu.Onaylandı };
        }
        public static List<BasvuruDurumlari> cmbBasvuruDurumListeDBilgi(List<int> SelectedBDurumID = null)
        {
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var qdata = db.BasvuruDurumlaris.Where(p => p.IsAktif);
                if (SelectedBDurumID != null) qdata = qdata.Where(p => SelectedBDurumID.Contains(p.BasvuruDurumID)).OrderBy(o => o.BasvuruDurumID);
                var data = qdata.ToList();
                return data;

            }

        }

        public static kmMezuniyetSureciOgrenimTipModel getMezuniyetOgrenimTipKriterleri(string EnstituKod, int MezuniyetSurecID)
        {
            var model = new kmMezuniyetSureciOgrenimTipModel();
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                model.OgrenimTipKriterList = (from o in db.OgrenimTipleris.Where(p => p.EnstituKod == EnstituKod && p.IsMezuniyetBasvurusuYapabilir && p.IsAktif)
                                              join s in db.MezuniyetSureciOgrenimTipKriterleris on new
                                              {
                                                  o.OgrenimTipKod,
                                                  MezuniyetSurecID
                                              }
                                                 equals new
                                                 {
                                                     s.OgrenimTipKod,
                                                     s.MezuniyetSurecID
                                                 }
                                                 into def1
                                              from defS in def1.DefaultIfEmpty()
                                              join ot in db.OgrenimTipleris on o.OgrenimTipID equals ot.OgrenimTipID
                                              where o.IsAktif
                                              select new kmMezuniyetSureciOgrenimTipKriterleri
                                              {

                                                  MezuniyetSureciOgrenimTipKriterID = defS != null ? defS.MezuniyetSureciOgrenimTipKriterID : 0,
                                                  OrjinalVeri = defS == null,
                                                  OgrenimTipKod = o.OgrenimTipKod,
                                                  OgrenimTipID = o.OgrenimTipID,
                                                  MBasvuruSonDonemKaydiKontrolEdilecekDersKodlari = defS != null ? defS.MBasvuruSonDonemKaydiKontrolEdilecekDersKodlari : o.MBasvuruSonDonemKaydiKontrolEdilecekDersKodlari,
                                                  MBasvuruToplamKrediKriteri = defS != null ? defS.MBasvuruToplamKrediKriteri : o.MBasvuruToplamKrediKriteri.Value,
                                                  MBasvuruAGNOKriteri = defS != null ? defS.MBasvuruAGNOKriteri : o.MBasvuruAGNOKriteri.Value,
                                                  MBasvuruAKTSKriteri = defS != null ? defS.MBasvuruAKTSKriteri : o.MBasvuruAKTSKriteri.Value,
                                                  MBSinavUzatmaSuresiGun = defS != null ? defS.MBSinavUzatmaSuresiGun : o.MBSinavUzatmaSuresiGun.Value,
                                                  MBTezTeslimSuresiGun = defS != null ? defS.MBTezTeslimSuresiGun : o.MBTezTeslimSuresiGun.Value,
                                                  MBSRTalebiKacGunSonraAlabilir = defS != null ? defS.MBSRTalebiKacGunSonraAlabilir : o.MBSRTalebiKacGunSonraAlabilir.Value,
                                                  OgrenimTipAdi = ot.OgrenimTipAdi
                                              }).ToList();
            }
            return model;
        }



        public static List<MezuniyetYayinKontrolDurumlari> cmbMezuniyetYayinDurumListeDBilgi(List<int> SelectedBDurumID = null)
        {
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var qdata = db.MezuniyetYayinKontrolDurumlaris.Where(p => p.IsAktif);
                if (SelectedBDurumID != null) qdata = qdata.Where(p => SelectedBDurumID.Contains(p.MezuniyetYayinKontrolDurumID)).OrderBy(o => o.MezuniyetYayinKontrolDurumID);
                var data = qdata.ToList();
                return data;

            }

        }
        public static List<MezuniyetSinavDurumlari> cmbMezuniyetSinavDurumListeDBilgi()
        {
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var qdata = db.MezuniyetSinavDurumlaris.Where(p => p.IsAktif).OrderBy(o => o.MezuniyetSinavDurumID);

                var data = qdata.ToList();
                return data;

            }

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
        public static List<CmbIntDto> cmbBelgeTipleri(bool bosSecimVar = false, int? OgrenimDurumID = null, string _EnstituKod = null)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.BelgeTipleris.Where(p => (OgrenimDurumID.HasValue ? p.BelgeTipDetayBelgelers.Any(a => a.BelgeTipID == p.BelgeTipID && a.BelgeTipDetay.OgrenimDurumID == OgrenimDurumID.Value && a.BelgeTipDetay.EnstituKod == _EnstituKod && a.BelgeTipDetay.IsAktif) : true) && p.IsAktif).OrderBy(o => o.BelgeTipID).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.BelgeTipID, Caption = item.BelgeTipAdi });
                }
            }
            return dct;

        }
        public static List<CmbIntDto> cmbMailSablonlari(string _EnstituKodu, bool bosSecimVar = false, bool? SistemMailFiltre = null)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.MailSablonlaris.Where(p => p.EnstituKod == _EnstituKodu && p.IsAktif && p.MailSablonTipleri.SistemMaili == (SistemMailFiltre.HasValue ? SistemMailFiltre.Value : p.MailSablonTipleri.SistemMaili)).OrderBy(o => o.SablonAdi).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.MailSablonlariID, Caption = item.SablonAdi });
                }
            }

            return dct;

        }
        public static List<CmbIntDto> cmbMailSablonTipleri(bool? SistemMaili = null, bool bosSecimVar = false, bool? IsOlusturulmayanlar = null)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.MailSablonTipleris.Where(p => IsOlusturulmayanlar == true ? !p.MailSablonlaris.Any() : true && p.SistemMaili == (SistemMaili.HasValue ? SistemMaili.Value : p.SistemMaili)).OrderBy(o => o.SablonTipAdi).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.MailSablonTipID, Caption = item.SablonTipAdi });
                }
            }

            return dct;

        }

        public static BelgeTipDetay getBtipDetay(int BelgeTipID, int OgrenimDurumID, string _EnstituKod)
        {
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var btip = db.BelgeTipDetays.Where(p => p.BelgeTipDetayBelgelers.Any(a => a.BelgeTipID == BelgeTipID) && p.OgrenimDurumID == OgrenimDurumID && p.EnstituKod == _EnstituKod).First();
                return btip;
            }
        }

        public static List<CmbIntDto> cmbBelgeTalepDurum(bool bosSecimVar = false, bool Yonetici = false, bool yeniKayit = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.BelgeDurumlaris.Where(p => p.BelgeDurumID == (yeniKayit ? BelgeTalepDurum.TalepEdildi : p.BelgeDurumID) && p.IsAktif && (Yonetici ? true : p.TalepEdenGorsun == true)).OrderBy(o => o.BelgeDurumID).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.BelgeDurumID, Caption = item.DurumAdi });
                }
            }
            return dct;

        }
        public static List<BelgeDurumlari> BelgeTalepDurumList()
        {
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.BelgeDurumlaris.Where(p => p.IsAktif).OrderBy(o => o.BelgeDurumID).ToList();
                return data;

            }

        }
        public static List<TalepDurumlari> TalepDurumList()
        {
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.TalepDurumlaris.OrderBy(o => o.TalepDurumID).ToList();
                return data;

            }

        }
        public static List<CmbIntDto> cmbBelgeTalepDurumListe(bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.BelgeDurumlaris.Where(p => p.IsAktif).OrderBy(o => o.BelgeDurumID).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.BelgeDurumID, Caption = item.DurumAdi });
                }
            }
            return dct;

        }
        public static List<CmbBoolDto> cmbEvetHayirData(bool bosSecimVar = false)
        {
            var dct = new List<CmbBoolDto>();
            if (bosSecimVar) dct.Add(new CmbBoolDto { Value = null, Caption = "" });
            dct.Add(new CmbBoolDto { Value = true, Caption = "Evet" });
            dct.Add(new CmbBoolDto { Value = false, Caption = "Hayir" });
            return dct;

        }
        public static List<CmbBoolDto> cmbAlanEslesmeData(bool bosSecimVar = false)
        {
            var dct = new List<CmbBoolDto>();
            if (bosSecimVar) dct.Add(new CmbBoolDto { Value = null, Caption = "" });
            dct.Add(new CmbBoolDto { Value = true, Caption = "Lisans veya Yüksek Lisansdan Biri Eşleşiyor ise Alan İçi" });
            dct.Add(new CmbBoolDto { Value = false, Caption = "Lisans ve Yüksek Lisans Eşleşiyor ise Alan İçi" });
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
        public static List<CmbIntDto> cmbSRDurum(bool bosSecimVar = false, bool Yonetici = false, bool yeniKayit = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.SRDurumlaris.Where(p => p.SRDurumID == (yeniKayit ? BelgeTalepDurum.TalepEdildi : p.SRDurumID) && p.IsAktif && (Yonetici ? true : p.TalepEdenGorsun == true)).OrderBy(o => o.SRDurumID).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.SRDurumID, Caption = item.DurumAdi });
                }
            }
            return dct;

        }
        public static List<SRDurumlari> SRDurumList()
        {
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.SRDurumlaris.Where(p => p.IsAktif).OrderBy(o => o.SRDurumID).ToList();
                return data;

            }

        }
        public static List<CmbIntDto> cmbSRDurumListe(bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.SRDurumlaris.Where(p => p.IsAktif).OrderBy(o => o.SRDurumID).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.SRDurumID, Caption = item.DurumAdi });
                }
            }
            return dct;

        }
        public static List<CmbIntDto> cmbTDDurumListe(bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });

            dct.Add(new CmbIntDto { Value = 2, Caption = "İşlem Bekleyenler" });
            dct.Add(new CmbIntDto { Value = 0, Caption = "Düzeltme Talep Edildi" });
            dct.Add(new CmbIntDto { Value = 1, Caption = "Onaylananlar" });

            return dct;

        }
        public static List<CmbIntDto> cmbSalonlar(string _EnstituKod, bool bosSecimVar = false, bool? IsAktif = true)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var qdata = db.SRSalonlars.Where(p => p.IsAktif && p.EnstituKod == _EnstituKod);

                if (IsAktif.HasValue) qdata = qdata.Where(p => p.IsAktif == IsAktif.Value);
                var data = qdata.OrderBy(o => o.SalonAdi).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.SRSalonID, Caption = item.SalonAdi });
                }
            }
            return dct;

        }
        public static List<CmbIntDto> cmbSalonlar(string _EnstituKod, int SRTalepTipID, bool bosSecimVar = false, bool? IsAktif = true)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var qdata = db.SRSalonlars.Where(p => p.IsAktif && p.EnstituKod == _EnstituKod && p.SRSalonTalepTipleris.Any(a => a.SRTalepTipID == SRTalepTipID));

                if (IsAktif.HasValue) qdata = qdata.Where(p => p.IsAktif == IsAktif.Value);
                var data = qdata.OrderBy(o => o.SalonAdi).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.SRSalonID, Caption = item.SalonAdi });
                }
            }
            return dct;

        }
        public static List<CmbIntDto> cmbAylar(bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.Aylars.OrderBy(o => o.AyID).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.AyID, Caption = item.AyAdi });
                }
            }
            return dct;

        }

        public static CmbMultyTypeDto SRkotaKontrol(int TalepYapanID, int SRTalepTipID, int? id = null)
        {
            var cmbMD = new CmbMultyTypeDto();
            cmbMD.ValueB = true;
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var ttip = db.SRTalepTipleris.Where(p => p.SRTalepTipID == SRTalepTipID).First();
                if (ttip.MaxCevaplanmamisTalep.HasValue)
                {
                    var q = db.SRTalepleris.Where(p => p.TalepYapanID == TalepYapanID && p.SRTalepTipID == SRTalepTipID && p.SRDurumID == SRTalepDurum.TalepEdildi);
                    if (id.HasValue) q = q.Where(p => p.SRTalepID != id.Value);
                    var kayitlar = q.ToList();
                    int cnt = kayitlar.Count;
                    cmbMD.Value = cnt;
                    if (ttip.MaxCevaplanmamisTalep.Value <= cnt && !id.HasValue)
                    {
                        cmbMD.ValueS = ttip.TalepTipAdi + " talep tipi için yapabileceğiniz rezervasyon talebi sayısı " + ttip.MaxCevaplanmamisTalep.Value + " adettir, daha önceden yapmış olduğunuz " + cnt + "  adet işlem bekleyen rezervasyon talebiniz bulunmaktadır. İşlem bekleyen rezervasyon talepleriniz işlem görene kadar yeni rezervasyon talebi yapamazsınız!";
                        cmbMD.ValueB = false;
                    }
                    else
                    {
                        cmbMD.ValueS = ttip.TalepTipAdi + " talep tipi için yapabileceğiniz rezervasyon talebi sayısı " + ttip.MaxCevaplanmamisTalep.Value + " adettir, daha önceden yapmış olduğunuz " + kayitlar + "  adet işlem bekleyen rezervasyon talebiniz bulunmaktadır. " + ttip.MaxCevaplanmamisTalep.Value + " adet yeni rezervasyon talebi yapabilirsiniz!";
                        cmbMD.ValueB = true;
                    }

                }
            }
            return cmbMD;
        }
        public static SRSalonSaatlerModel getSalonBosSaatler(int SRSalonID, int SRTalepTipID, DateTime Tarih, int? SRTalepID = null, int? SROzelTanimID = null, DateTime? MinTarih = null)
        {
            var model = new SRSalonSaatlerModel();
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var nTarih = Tarih.ToShortDateString().ToDate().Value;
                var dofW = nTarih.DayOfWeek.ToString("d").ToInt().Value;
                var haftaGunu = db.HaftaGunleris.Where(p => p.HaftaGunID == dofW).First();
                var salon = db.SRSalonlars.Where(p => p.SRSalonID == SRSalonID).First();
                var SecilenTarihRezervasyonlar = db.SRTalepleris.Where(p => p.SRSalonID == SRSalonID && p.Tarih == nTarih && (p.SRDurumID == SRTalepDurum.Onaylandı || p.SRDurumID == SRTalepDurum.TalepEdildi)).ToList();
                var ResmiTatilDegisen = db.SROzelTanimlars.Where(p => p.IsAktif && p.SROzelTanimTipID == SROzelTanimTip.ResmiTatilDegisen && p.BasTarih.Value <= nTarih && p.BitTarih >= nTarih).FirstOrDefault();
                var ResmiTatilSabit = db.SROzelTanimlars.Where(p => p.IsAktif && p.SROzelTanimTipID == SROzelTanimTip.ResmiTatilSabit && p.Ay.Value == nTarih.Month && p.Gun == nTarih.Day).FirstOrDefault();
                var Rezervasyonlar = db.SROzelTanimlars.Where(p => p.IsAktif && p.SROzelTanimTipID == SROzelTanimTip.Rezervasyon && p.SRSalonID == SRSalonID && p.Tarih == nTarih).FirstOrDefault();
                var Rezerve = db.SROzelTanimlars.Where(p => p.SROzelTanimGunlers.Any(a => a.HaftaGunID == dofW) && p.SROzelTanimID != (SROzelTanimID.HasValue ? SROzelTanimID.Value : 0) && p.IsAktif && p.SROzelTanimTipID == SROzelTanimTip.Rezerve && p.SRSalonID == SRSalonID && p.BasTarih.Value <= nTarih && p.BitTarih >= nTarih).FirstOrDefault();
                var TalepTip = db.SRTalepTipleris.Where(p => p.SRTalepTipID == SRTalepTipID).First();
                model.Tarih = nTarih;
                var salonSaatleri = db.SRSaatlers.Where(p => p.SRSalonID == SRSalonID && p.HaftaGunID == haftaGunu.HaftaGunID).Select(s => new SRSalonSaatler
                {
                    SRSaatID = s.SRSaatID,
                    SRSalonID = s.SRSalonID,
                    HaftaGunID = s.HaftaGunID,
                    HaftaGunAdi = haftaGunu.HaftaGunAdi,
                    BasSaat = s.BasSaat,
                    BitSaat = s.BitSaat,
                    SalonDurumID = SRSalonDurum.Boş,
                    Aciklama = "Rezervasyon için uygun"
                }).ToList();

                foreach (var item in SecilenTarihRezervasyonlar)
                {
                    var TalepTipiLng = item.SRTalepTipleri;
                    var Aciklama = TalepTipiLng.TalepTipAdi + ", " + item.Kullanicilar.Ad + " " + item.Kullanicilar.Soyad;
                    var SalonSaat = salonSaatleri.Where(p => p.SRSalonID == item.SRSalonID && p.BasSaat == item.BasSaat && p.BitSaat == item.BitSaat).FirstOrDefault();
                    if (SalonSaat != null)
                    {
                        SalonSaat.Checked = SRTalepID == item.SRTalepID;
                        SalonSaat.SalonDurumID = SRSalonDurum.Alındı;
                        SalonSaat.Aciklama = Aciklama;

                    }
                    else
                    {
                        salonSaatleri.Add(new SRSalonSaatler
                        {
                            SRSalonID = item.SRSalonID.Value,
                            HaftaGunID = item.HaftaGunID,
                            HaftaGunAdi = haftaGunu.HaftaGunAdi,
                            BasSaat = item.BasSaat,
                            BitSaat = item.BitSaat,
                            SalonDurumID = SRSalonDurum.Alındı,
                            Aciklama = Aciklama,
                            Checked = true,
                        });
                    }
                }


                if (SROzelTanimID.HasValue) //gelentalepGuncellemeIse
                {
                    var talep = db.SROzelTanimlars.Where(p => p.SROzelTanimID == SROzelTanimID.Value).First();
                    if (Tarih == talep.Tarih && talep.SRSalonID == SRSalonID)
                    {
                        var rezTip = db.SRTalepTipleris.Where(p => p.SRTalepTipID == talep.SRTalepTipID).First();
                        foreach (var item in talep.SROzelTanimSaatlers)
                        {

                            if (!salonSaatleri.Any(a => a.BasSaat == item.BasSaat && a.BitSaat == item.BitSaat))
                            {
                                var _rw = new SRSalonSaatler();
                                _rw.SRSalonID = talep.SRSalonID.Value;
                                _rw.HaftaGunID = dofW;
                                _rw.HaftaGunAdi = haftaGunu.HaftaGunAdi;
                                _rw.BasSaat = item.BasSaat;
                                _rw.BitSaat = item.BitSaat;
                                _rw.SalonDurumID = SRSalonDurum.Alındı;
                                _rw.Aciklama = rezTip.TalepTipAdi + ", " + talep.Aciklama;
                                salonSaatleri.Add(_rw);
                            }
                            else
                            {
                                var qdata = salonSaatleri.Where(p => p.BasSaat == item.BasSaat && p.BitSaat == item.BitSaat).FirstOrDefault();
                                if (Tarih == talep.Tarih && qdata != null)
                                {
                                    qdata.SalonDurumID = SRSalonDurum.Alındı;
                                    qdata.Aciklama = rezTip.TalepTipAdi + ", " + talep.Aciklama;
                                }

                            }
                        }
                    }

                }
                if (TalepTip.SRTalepTipleriAktifAylars.Any(a => a.AyID == nTarih.Month) == false && UserIdentity.Current.IsAdmin == false)
                {
                    salonSaatleri.Clear();
                    var syLst = TalepTip.SRTalepTipleriAktifAylars.SelectMany(s => s.Aylar.AyAdi).ToList();
                    var SRTalepTipi = TalepTip;

                    model.GenelAciklama = SRTalepTipi.TalepTipAdi + " talep tipi için talep yapılabilecek aylar: '" + string.Join(", ", syLst) + "' Bu ayların dışında sistem rezervasyon işlemine kapalıdır.";
                }
                else if (salonSaatleri.Count == 0)
                {

                    model.GenelAciklama = model.SRSalonAdi + " Salonu için " + model.Tarih.ToString("dd.MM.yyyy") + " tarihi için rezervasyon alınamaz.";
                }

                model.HaftaGunID = haftaGunu.HaftaGunID;
                model.SRSalonID = salon.SRSalonID;
                model.SRSalonAdi = salon.SalonAdi;
                model.HaftaGunundeSaatlerVar = salonSaatleri.Count > 0;
                model.HaftaGunAdi = haftaGunu.HaftaGunAdi;
                //model.BosSaatSayisi = salonSaatleri.Where(a => a.Dolu == false).Count();
                //model.DoluSaatSayisi = salonSaatleri.Where(a => a.Dolu).Count();



                foreach (var item in salonSaatleri)
                {
                    var qGTalepEslesen = SecilenTarihRezervasyonlar.Where(a => a.SRTalepID != (SRTalepID ?? 0) &&
                                                                (
                                                                  (a.BasSaat == item.BasSaat || a.BitSaat == item.BitSaat) ||
                                                                (
                                                                    (a.BasSaat < item.BasSaat && a.BitSaat > item.BasSaat) || a.BasSaat < item.BitSaat && a.BitSaat > item.BitSaat) ||
                                                                    (a.BasSaat > item.BasSaat && a.BasSaat < item.BitSaat) || a.BitSaat > item.BasSaat && a.BitSaat < item.BitSaat)
                                                                ).FirstOrDefault();
                    var nowDate = DateTime.Now;
                    if (MinTarih.HasValue) nowDate = MinTarih.Value;
                    var kTarih = Convert.ToDateTime(Tarih.ToShortDateString() + " " + item.BasSaat.Hours + ":" + item.BasSaat.Minutes + ":" + item.BasSaat.Seconds);
                    if (qGTalepEslesen != null)
                    {

                        var rezTip = db.SRTalepTipleris.Where(p => p.SRTalepTipID == qGTalepEslesen.SRTalepTipID).First();
                        item.SalonDurumID = qGTalepEslesen.SRDurumID == SRTalepDurum.Onaylandı ? SRSalonDurum.Dolu : SRSalonDurum.OnTalep;
                        item.Disabled = true;
                        item.Aciklama = qGTalepEslesen.SRDurumID == SRTalepDurum.Onaylandı ? rezTip.TalepTipAdi + ", " + qGTalepEslesen.Kullanicilar.Ad + " " + qGTalepEslesen.Kullanicilar.Soyad : "Onay bekliyor";

                    }
                    else if (ResmiTatilDegisen != null)
                    {
                        item.SalonDurumID = SRSalonDurum.ResmiTatil;
                        item.Disabled = true;
                        item.Aciklama = ResmiTatilDegisen.Aciklama;
                    }
                    else if (ResmiTatilSabit != null)
                    {
                        item.SalonDurumID = SRSalonDurum.ResmiTatil;
                        item.Disabled = true;
                        item.Aciklama = ResmiTatilSabit.Aciklama;
                    }
                    else if (Rezerve != null)
                    {
                        var rezTip = db.SRTalepTipleris.Where(p => p.SRTalepTipID == Rezerve.SRTalepTipID).First();
                        item.SalonDurumID = SRSalonDurum.Dolu;
                        item.Disabled = true;
                        item.Aciklama = rezTip.TalepTipAdi + ", " + Rezerve.Aciklama;
                    }
                    else if (Rezervasyonlar != null)
                    {
                        var qRez = Rezervasyonlar.SROzelTanimSaatlers.Where(a => a.SROzelTanimID != (SROzelTanimID ?? 0) &&
                                                                    (
                                                                      (a.BasSaat == item.BasSaat || a.BitSaat == item.BitSaat) ||
                                                                    (
                                                                        (a.BasSaat < item.BasSaat && a.BitSaat > item.BasSaat) || a.BasSaat < item.BitSaat && a.BitSaat > item.BitSaat) ||
                                                                        (a.BasSaat > item.BasSaat && a.BasSaat < item.BitSaat) || a.BitSaat > item.BasSaat && a.BitSaat < item.BitSaat)
                                                                  ).FirstOrDefault();
                        if (qRez != null)
                        {
                            var rezTip = db.SRTalepTipleris.Where(p => p.SRTalepTipID == qRez.SROzelTanimlar.SRTalepTipID).First();
                            item.SalonDurumID = SRSalonDurum.Dolu;
                            item.Disabled = true;
                            item.Aciklama = rezTip.TalepTipAdi + ", " + Rezervasyonlar.Aciklama;
                        }
                        else if (kTarih < nowDate && item.SalonDurumID == SRSalonDurum.Boş)
                        {
                            item.SalonDurumID = SRSalonDurum.GecmisTarih;
                            item.Disabled = true;
                            item.Aciklama = "Geçmişe dönük rezervasyon alınamaz.";
                        }
                    }
                    else if (kTarih < nowDate && item.SalonDurumID == SRSalonDurum.Boş)
                    {
                        item.SalonDurumID = SRSalonDurum.GecmisTarih;
                        item.Disabled = true;
                        item.Aciklama = "Geçmişe dönük rezervasyon alınamaz.";
                    }

                }

                var qData = (from s in salonSaatleri
                             join d in db.SRSalonDurumlaris on s.SalonDurumID equals d.SRSalonDurumID
                             select new SRSalonSaatler
                             {
                                 SRSaatID = s.SRSaatID,
                                 SRSalonID = s.SRSalonID,
                                 HaftaGunID = s.HaftaGunID,
                                 HaftaGunAdi = haftaGunu.HaftaGunAdi,
                                 BasSaat = s.BasSaat,
                                 BitSaat = s.BitSaat,
                                 SalonDurumID = s.SalonDurumID,
                                 SalonDurumAdi = d.SalonDurumAdi,
                                 Aciklama = s.Aciklama,
                                 Disabled = s.Disabled,
                                 Checked = s.Checked,
                                 Color = d.Color
                             }).OrderBy(o => o.BasSaat).ToList();

                model.Data = qData;
            }
            return model;
        }
        public static MmMessage SRKayitKontrol(int SRSalonID, int SRTalepTipID, DateTime Tarih, List<SROzelTanimSaatler> Saatler, int? SRTalepID = null, int? SROzelTanimID = null, DateTime? Tarih2 = null, List<int> haftaGunID = null, DateTime? MinTarih = null)
        {
            var mmMessage = new MmMessage();
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var bitTar = Tarih2 ?? Tarih;
                haftaGunID = haftaGunID ?? new List<int>();
                for (DateTime date = Tarih; date <= bitTar; date = date.AddDays(1.0))
                {
                    var nTarih = date.ToShortDateString().ToDate().Value;
                    var dofW = nTarih.DayOfWeek.ToString("d").ToInt().Value;

                    if (!haftaGunID.Contains(dofW) && haftaGunID.Count > 0)
                    {
                        continue;
                    }
                    var salon = db.SRSalonlars.Where(p => p.SRSalonID == SRSalonID).First();

                    var haftaGunu = db.HaftaGunleris.Where(p => p.HaftaGunID == dofW).First();
                    var ResmiTatilDegisen = db.SROzelTanimlars.Where(p => p.IsAktif && p.SROzelTanimTipID == SROzelTanimTip.ResmiTatilDegisen && p.BasTarih.Value <= nTarih && p.BitTarih >= nTarih).FirstOrDefault();
                    var ResmiTatilSabit = db.SROzelTanimlars.Where(p => p.IsAktif && p.SROzelTanimTipID == SROzelTanimTip.ResmiTatilSabit && p.Ay.Value == nTarih.Month && p.Gun == nTarih.Day).FirstOrDefault();
                    var Rezervasyonlar = db.SROzelTanimlars.Where(p => p.SROzelTanimID != (SROzelTanimID.HasValue ? SROzelTanimID.Value : 0) && p.IsAktif && p.SROzelTanimTipID == SROzelTanimTip.Rezervasyon && p.SRSalonID == SRSalonID && p.Tarih == nTarih).ToList();
                    var Rezerve = db.SROzelTanimlars.Where(p => p.SROzelTanimGunlers.Any(a => a.HaftaGunID == dofW) && p.SROzelTanimID != (SROzelTanimID.HasValue ? SROzelTanimID.Value : 0) && p.IsAktif && p.SROzelTanimTipID == SROzelTanimTip.Rezerve && p.SRSalonID == SRSalonID && p.BasTarih.Value <= nTarih && p.BitTarih >= nTarih).FirstOrDefault();
                    var tTip = db.SRTalepTipleris.Where(p => p.SRTalepTipID == SRTalepTipID).First();


                    if (tTip.SRTalepTipleriAktifAylars.Any(a => a.AyID == nTarih.Month) == false && RoleNames.SRGelenTalepler.InRoleCurrent() == false)
                    {
                        var syLst = tTip.SRTalepTipleriAktifAylars.SelectMany(s => s.Aylar.AyAdi).ToList();
                        string msg = tTip.TalepTipAdi + " talep tipi için talep yapılabilecek aylar: '" + string.Join(", ", syLst) + "' Bu ayların dışında sistem rezervasyon işlemine kapalıdır.";
                        mmMessage.Messages.Add(msg);
                        mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "Tarih" });

                    }

                    else
                    {

                        if (Tarih2.HasValue)
                        {

                            var qTalepEslesen = db.SRTalepleris.Where(a => a.SRSalonID == SRSalonID && a.Tarih == nTarih).Any(p => p.SRDurumID == SRTalepDurum.Onaylandı || p.SRDurumID == SRTalepDurum.TalepEdildi);
                            if (qTalepEslesen)
                            {
                                mmMessage.Messages.Add(nTarih.ToShortDateString() + "Tarihi için " + salon.SalonAdi + " Salonu için dolu saatler var!");
                                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "Tarih" });
                            }
                            if (ResmiTatilDegisen != null || ResmiTatilSabit != null)
                            {

                                mmMessage.Messages.Add("Resmi tatillerde rezervasyon alınamaz.");
                                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "Tarih" });
                            }
                            else if (Rezerve != null)
                            {

                                mmMessage.Messages.Add(nTarih.ToShortDateString() + " Tarihinde " + salon.SalonAdi + " Salonu doludur!");
                                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "Tarih" });

                            }
                            else if (Rezervasyonlar.Count > 0)
                            {

                                mmMessage.Messages.Add(nTarih.ToShortDateString() + " Tarihinde " + salon.SalonAdi + " Salonu doludur!");
                                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "Tarih" });

                            }
                        }
                        else
                        {
                            foreach (var item in Saatler)
                            {

                                var nowDate = DateTime.Now;
                                if (MinTarih.HasValue) nowDate = MinTarih.Value;
                                var kTarih = Convert.ToDateTime(Tarih.ToShortDateString() + " " + item.BasSaat.Hours + ":" + item.BasSaat.Minutes + ":" + item.BasSaat.Seconds);

                                var qTalepEslesen = db.SRTalepleris.Where(a => a.SRTalepID != (SRTalepID ?? 0) && a.SRSalonID == SRSalonID && a.Tarih == nTarih &&
                                                 (
                                                   (a.BasSaat == item.BasSaat || a.BitSaat == item.BitSaat) ||
                                                 (
                                                     (a.BasSaat < item.BasSaat && a.BitSaat > item.BasSaat) || a.BasSaat < item.BitSaat && a.BitSaat > item.BitSaat) ||
                                                     (a.BasSaat > item.BasSaat && a.BasSaat < item.BitSaat) || a.BitSaat > item.BasSaat && a.BitSaat < item.BitSaat)
                                                 );

                                if (qTalepEslesen.Any(p => p.SRDurumID == SRTalepDurum.Onaylandı || p.SRDurumID == SRTalepDurum.TalepEdildi))
                                {
                                    mmMessage.Messages.Add((nTarih.ToShortDateString() + " " + item.BasSaat.ToString() + " - " + item.BitSaat.ToString()) + " Tarihi için " + salon.SalonAdi + " Salonu Doludur! Lütfen boş bir saat seçiniz.");
                                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "Tarih" });
                                }
                                if (ResmiTatilDegisen != null || ResmiTatilSabit != null)
                                {
                                    ;
                                    mmMessage.Messages.Add("Resmi tatillerde rezervasyon alınamaz.");
                                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "Tarih" });
                                }
                                else if (Rezerve != null)
                                {
                                    mmMessage.Messages.Add(item.BasSaat.ToString() + " - " + item.BitSaat.ToString() + " Tarihinde " + salon.SalonAdi + " Salonu doludur!");
                                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "Tarih" });

                                }
                                else if (Rezervasyonlar.Count > 0)
                                {
                                    foreach (var itemRO in Rezervasyonlar)
                                    {


                                        var qRez = itemRO.SROzelTanimSaatlers.Where(a =>
                                                                                    (
                                                                                      (a.BasSaat == item.BasSaat || a.BitSaat == item.BitSaat) ||
                                                                                    (
                                                                                        (a.BasSaat < item.BasSaat && a.BitSaat > item.BasSaat) || a.BasSaat < item.BitSaat && a.BitSaat > item.BitSaat) ||
                                                                                        (a.BasSaat > item.BasSaat && a.BasSaat < item.BitSaat) || a.BitSaat > item.BasSaat && a.BitSaat < item.BitSaat)
                                                                                  ).FirstOrDefault();
                                        if (qRez != null)
                                        {
                                            mmMessage.Messages.Add(item.BasSaat.ToString() + " - " + item.BitSaat.ToString() + " Tarihinde " + salon.SalonAdi + " Salonu doludur!");
                                            mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "Tarih" });
                                        }
                                    }
                                }
                                else if (kTarih < nowDate)
                                {
                                    mmMessage.Messages.Add("Geçmişe dönük rezervasyon alınamaz.");
                                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "Tarih" });
                                }
                                else if (salon.SRSaatlers.Any(a => a.BasSaat == item.BasSaat && a.BitSaat == item.BitSaat) == false)
                                {
                                    mmMessage.Messages.Add("Rezervasyon için seçilen sat uygun değildir.");
                                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "Tarih" });
                                }
                            }
                        }
                    }
                }

            }
            return mmMessage;
        }


        public static List<CmbIntDto> cmbSRTalepTipleri(bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.SRTalepTipleris.OrderBy(o => o.SRTalepTipID).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.SRTalepTipID, Caption = item.TalepTipAdi });
                }
            }
            return dct;
        }
        public static List<CmbIntDto> cmbTalepTipleri(bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.TalepTipleris.OrderBy(o => o.TalepTipID).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.TalepTipID, Caption = item.TalepTipAdi });
                }
            }
            return dct;
        }
        public static List<CmbIntDto> cmbTalepTipleriSurec(int TalepSurecID, int TalepTipID, bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var Surec = db.TalepSurecleris.Where(p => p.TalepSurecID == TalepSurecID).First();
                var TalepTipIDs = Surec.TalepSureciTalepTipleris.Select(s => s.TalepTipID).ToList();
                var data = db.TalepTipleris.Where(p => (p.TalepTipID == TalepTipID || TalepTipIDs.Contains(p.TalepTipID))).OrderBy(o => o.TalepTipID).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.TalepTipID, Caption = item.TalepTipAdi });
                }
            }
            return dct;
        }
        public static List<CmbIntDto> cmbTalepDurumlari(bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.TalepDurumlaris.OrderBy(o => o.TalepDurumID).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.TalepDurumID, Caption = item.TalepDurumAdi });
                }
            }
            return dct;
        }
        public static List<CmbIntDto> cmbTalepTipleri(int? KullaniciTipID, bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var qdata = db.SRTalepTipleris.Where(p => p.IsTezSinavi == false).OrderBy(o => o.SRTalepTipID).AsQueryable();
                if (KullaniciTipID.HasValue) qdata = qdata.Where(p => p.SRTalepTipKullanicilars.Any(a => a.KullaniciTipID == KullaniciTipID));
                var data = qdata.ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.SRTalepTipID, Caption = item.TalepTipAdi });
                }
            }
            return dct;
        }
        public static List<CmbIntDto> cmbOzelTanimTipleri(bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.SROzelTanimTipleris.OrderBy(o => o.SROzelTanimTipID).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.SROzelTanimTipID, Caption = item.SROzelTanimTipAdi });
                }
            }
            return dct;
        }

        public static List<CmbIntDto> cmbArGorStatuleri(bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.TalepArGorStatuleris.OrderBy(o => o.TalepArGorStatuID).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.TalepArGorStatuID, Caption = item.StatuAdi });
                }
            }
            return dct;
        }

        public static List<CmbIntDto> cmbMezuniyetSurecYayinTurleri(int MezuniyetSurecID, int KullaniciID, bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var kriter = Management.MezuniyetAktifOTYayinB(MezuniyetSurecID, KullaniciID);
                var IDs = kriter.Where(p => p.IsGecerli).Select(s => s.MezuniyetYayinTurID).Distinct().ToList();

                var qdata = db.MezuniyetYayinTurleris.AsQueryable();
                if (IDs.Count > 0) qdata = qdata.Where(p => IDs.Contains(p.MezuniyetYayinTurID));
                var data = qdata.OrderBy(o => o.MezuniyetYayinTurAdi).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.MezuniyetYayinTurID, Caption = item.MezuniyetYayinTurAdi });
                }
            }
            return dct;
        }
        public static List<CmbIntDto> cmbMezuniyetYayinTurleri(bool bosSecimVar = false)
        {

            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {

                var data = db.MezuniyetYayinTurleris.OrderBy(o => o.MezuniyetYayinTurAdi).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.MezuniyetYayinTurID, Caption = item.MezuniyetYayinTurAdi });
                }
            }
            return dct;
        }
        public static List<CmbIntDto> cmbMezuniyetYayinBelgeTurleri(bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.MezuniyetYayinBelgeTurleris.OrderBy(o => o.BelgeTurAdi).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.MezuniyetYayinBelgeTurID, Caption = item.BelgeTurAdi });
                }
            }
            return dct;
        }
        public static List<CmbIntDto> cmbMezuniyetYayinLinkTurleri(bool IsKaynakOrYayin, bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.MezuniyetYayinLinkTurleris.Where(p => p.IsKaynakOrYayin == IsKaynakOrYayin).OrderBy(o => o.LinkTurAdi).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.MezuniyetYayinLinkTurID, Caption = item.LinkTurAdi });
                }
            }
            return dct;
        }
        public static List<CmbIntDto> cmbMezuniyetYayinMetinTurleri(bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.MezuniyetYayinMetinTurleris.OrderBy(o => o.MetinTurAdi).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.MezuniyetYayinMetinTurID, Caption = item.MetinTurAdi });
                }
            }
            return dct;
        }
        public static List<CmbIntDto> cmbMezuniyetYayinIndexTurleri(bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.MezuniyetYayinIndexTurleris.OrderBy(o => o.IndexTurAdi).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.MezuniyetYayinIndexTurID, Caption = item.IndexTurAdi });
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
        public static List<CmbBoolDto> cmbVarYokData(bool bosSecimVar = false)
        {
            var dct = new List<CmbBoolDto>();
            if (bosSecimVar) dct.Add(new CmbBoolDto { Value = null, Caption = "" });
            dct.Add(new CmbBoolDto { Value = true, Caption = "Var" });
            dct.Add(new CmbBoolDto { Value = false, Caption = "Yok" });
            return dct;

        }
        public static List<CmbBoolDto> cmbIdariAkademikdata(bool bosSecimVar = false)
        {
            var dct = new List<CmbBoolDto>();
            if (bosSecimVar) dct.Add(new CmbBoolDto { Value = null, Caption = "" });
            dct.Add(new CmbBoolDto { Value = true, Caption = "Idari" });
            dct.Add(new CmbBoolDto { Value = false, Caption = "Akademik" });
            return dct;

        }


        public static List<CmbStringDto> cmbGetAktifEnstituler(bool bosSecimVar = false)
        {
            var dct = new List<CmbStringDto>();
            if (bosSecimVar) dct.Add(new CmbStringDto { Value = null, Caption = "" });
            var data = Enstitulers.Where(p => p.IsAktif).OrderBy(o => o.EnstituAd).ToList();
            foreach (var item in data)
            {
                dct.Add(new CmbStringDto { Value = item.EnstituKod, Caption = item.EnstituAd });
            }
            return dct;

        }
        public static List<CmbStringDto> cmbGetYetkiliEnstituler(bool bosSecimVar = false)
        {
            var dct = new List<CmbStringDto>();
            var EnstKods = UserIdentity.Current.EnstituKods ?? new List<string>();

            if (bosSecimVar) dct.Add(new CmbStringDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = Enstitulers.Where(p => EnstKods.Contains(p.EnstituKod)).OrderBy(o => o.EnstituAd).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbStringDto { Value = item.EnstituKod, Caption = item.EnstituAd });
                }
            }
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
        public static List<CmbIntDto> getSinavDiller(bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.SinavDilleris.ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.SinavDilID, Caption = item.DilAdi });
                }
            }
            return dct;

        }
        public static List<CmbBoolDto> getGrupGoster()
        {
            var dct = new List<CmbBoolDto>();
            dct.Add(new CmbBoolDto { Value = true, Caption = "Grup Olarak Göster" });
            dct.Add(new CmbBoolDto { Value = false, Caption = "Tek Olarak Göster" });
            return dct;

        }

        public static List<CmbIntDto> cmbGetKontrolTipleri(bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.BasvuruSurecKontrolTipleris.OrderBy(o => o.BasvuruSurecKontrolTipAdi).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.BasvuruSurecKontrolTipID, Caption = item.BasvuruSurecKontrolTipAdi });
                }
            }
            return dct;

        }

        public static List<CmbStringDto> cmbGetWsDonemler(bool bosSecimVar = false)
        {
            var dct = new List<CmbStringDto>();
            if (bosSecimVar) dct.Add(new CmbStringDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.Donemlers.Where(p => p.IsWsDonem).OrderBy(o => o.DonemID).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbStringDto { Value = item.WsDonemKod, Caption = item.DonemAdi });
                }
            }
            return dct;

        }
        public static List<CmbIntDto> cmbGetWsSinavCekimTipleri(bool bosSecimVar = false, int? FilterCekimTip = null)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var qdata = db.WsSinavCekimTipleris.AsQueryable();
                if (FilterCekimTip.HasValue) qdata = qdata.Where(p => p.WsSinavCekimTipID == FilterCekimTip.Value);
                var data = qdata.OrderBy(o => o.WsSinavCekimTipAdi).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.WsSinavCekimTipID, Caption = item.WsSinavCekimTipAdi });
                }
            }
            return dct;

        }
        public static List<CmbStringDto> cmbGetWsSinavCekimTipDetay(int WsSinavCekimTipID, bool bosSecimVar = false)
        {
            var dct = new List<CmbStringDto>();
            if (bosSecimVar) dct.Add(new CmbStringDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.WsSinavCekipTipDetays.Where(p => p.WsSinavCekimTipID == WsSinavCekimTipID).OrderBy(o => o.WsSinavCekimKod).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbStringDto { Value = item.WsSinavCekimKod, Caption = item.WsSinavCekimAd });
                }
            }
            return dct;

        }
        public static List<CmbStringDto> cmbGetWsSinavCekimTipDetayGetLocalData(int WsSinavCekimTipID, int SinavTipKod, bool bosSecimVar = false)
        {
            var dct = new List<CmbStringDto>();
            if (bosSecimVar) dct.Add(new CmbStringDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = (from s in db.SinavSonuclaris.Where(p => p.SinavTipKod == SinavTipKod)
                            join d in db.SinavDilleris on s.SinavDilID equals d.SinavDilID
                            orderby s.SinavTarihi, d.DilAdi
                            select new { s.SinavDilID, d.DilAdi, s.SinavTarihi }).ToList();

                foreach (var item in data)
                {
                    dct.Add(new CmbStringDto { Value = item.SinavDilID + "~" + item.DilAdi + "~" + item.SinavTarihi.ToString("dd-MM-yyyy"), Caption = item.SinavTarihi.ToString("dd-MM-yyyy") + "~" + item.DilAdi });
                }
            }
            return dct.Distinct().ToList();

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
        public static List<CmbIntDto> cmbAraRaporSayisi(bool bosSecimVar = false, int Max = 50)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            for (int i = 1; i <= Max; i++)
            {
                dct.Add(new CmbIntDto { Value = i, Caption = i + ". Rapor" });
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
        public static List<CmbStringDto> cmbGetAktifProgramlarStr(string EnstituKod, bool bosSecimVar = false)
        {
            var dct = new List<CmbStringDto>();
            if (bosSecimVar) dct.Add(new CmbStringDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.Programlars.Where(p => p.IsAktif && p.AnabilimDallari.EnstituKod == EnstituKod).OrderBy(o => o.ProgramAdi).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbStringDto { Value = item.ProgramAdi, Caption = item.ProgramAdi });
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
            if (!KullaniciTipID.HasValue || RoleNames.KullaniciAdinaBasvuruYap.InRoleCurrent() == false) KullaniciTipID = UserIdentity.Current.KullaniciTipID;

            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var kulTip = db.KullaniciTipleris.Where(p => p.KullaniciTipID == KullaniciTipID).First();
                var basvurusrc = db.BasvuruSurecs.Where(p => p.BasvuruSurecID == BasvuruSurecID).First();
                var q = from p in db.Programlars
                        join k in db.BasvuruSurecKotalars.Where(p => p.BasvuruSurecID == BasvuruSurecID) on p.ProgramKod equals k.ProgramKod
                        join bl in db.AnabilimDallaris on p.AnabilimDaliKod equals bl.AnabilimDaliKod
                        join ot in db.OgrenimTipleris on new { k.BasvuruSurec.EnstituKod, k.OgrenimTipKod } equals new { ot.EnstituKod, ot.OgrenimTipKod }
                        where bl.EnstituKod == basvurusrc.EnstituKod && ot.OgrenimTipKod == OgrenimTipKod && p.KullaniciTipleri.Yerli == kulTip.Yerli
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
                        where p.AnabilimDaliID == AnabilimDaliID && p.KullaniciTipleri.Yerli == Yerli
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
        public static List<CmbStringDto> cmbGetAktifAnabilimDallari(bool bosSecimVar = false)
        {
            var dct = new List<CmbStringDto>();
            if (bosSecimVar) dct.Add(new CmbStringDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.AnabilimDallaris.Where(p => p.IsAktif).OrderBy(o => o.AnabilimDaliAdi).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbStringDto { Value = item.AnabilimDaliKod, Caption = item.AnabilimDaliAdi });
                }
            }
            return dct;
        }
        public static List<CmbIntDto> cmbGetTumAnabilimDallari(bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.AnabilimDallaris.OrderBy(o => o.AnabilimDaliAdi).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.AnabilimDaliID, Caption = item.AnabilimDaliAdi });
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
        public static List<CmbIntDto> cmbGetOzelTarihTipleri(bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.OzelTarihTipleris.OrderBy(o => o.OzelTarihTipID).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.OzelTarihTipID, Caption = item.OzelTarihTipAdi });
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
        public static List<CmbIntDto> cmbGetBasvuruSurecAktifSinavTipTipleri(int BasvuruSurecID, int? SinavTipGrupID = null, bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = (from s in db.BasvuruSurecSinavTipleris.Where(s2 => s2.IsAktif && s2.BasvuruSurecID == BasvuruSurecID)
                            join stl in db.SinavTipleris on new { s.SinavTipID } equals new { stl.SinavTipID }
                            orderby stl.SinavAdi
                            select new
                            {
                                s.SinavTipID,
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
        public static List<CmbIntDto> cmbGetAktifSinavlar(string EnstituKodu, List<int> SinavTipGrupIDs, bool bosSecimVar = false)
        {
            SinavTipGrupIDs = SinavTipGrupIDs ?? new List<int>();
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = (from s in db.SinavTipleris.Where(s2 => s2.EnstituKod == EnstituKodu && s2.IsAktif)
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
        public static List<CmbIntDto> cmbGetSinavSonucSinavTips(bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var kods = db.SinavSonuclaris.Select(s => s.SinavTipKod).Distinct();
                var data = (

                            from s in db.SinavTipleris.Where(s2 => kods.Contains(s2.SinavTipKod))
                            join stl in db.SinavTipleris on new { s.SinavTipID } equals new { stl.SinavTipID }
                            orderby stl.SinavAdi
                            select new
                            {
                                s.SinavTipKod,
                                s.SinavTipGrupID,
                                stl.SinavAdi
                            }).Distinct().ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.SinavTipKod, Caption = item.SinavAdi });
                }
            }
            return dct;
        }
        public static List<CmbIntDto> cmbGetAktifSinavTips(bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = (

                            from s in db.SinavTipleris
                            join stl in db.SinavTipleris on new { s.SinavTipID } equals new { stl.SinavTipID }
                            orderby stl.SinavAdi
                            where s.IsAktif
                            select new
                            {
                                s.SinavTipKod,
                                s.SinavTipGrupID,
                                stl.SinavAdi
                            }).Distinct().ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.SinavTipKod, Caption = item.SinavAdi });
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
        public static List<CmbStringDto> cmbGetYetkiliProgramAnabilimDallari(bool bosSecimVar = false, string EnstituKod = "")
        {
            var EnstKods = UserIdentity.Current.EnstituKods ?? new List<string>();
            var dct = new List<CmbStringDto>();
            if (bosSecimVar) dct.Add(new CmbStringDto { Value = "", Caption = "" });
            var userPkod = Management.GetUserProgramKods(UserIdentity.Current.Id, EnstituKod);
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.AnabilimDallaris.Where(p => EnstKods.Contains(p.EnstituKod) && p.EnstituKod == EnstituKod && p.Programlars.Any(a => userPkod.Contains(a.ProgramKod)));
                if (EnstituKod.IsNullOrWhiteSpace() == false) data = data.Where(p => p.EnstituKod == EnstituKod);
                var data2 = data.OrderBy(o => o.AnabilimDaliAdi).ToList();
                foreach (var item in data2)
                {
                    dct.Add(new CmbStringDto { Value = item.AnabilimDaliKod, Caption = item.AnabilimDaliAdi });
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
                var data = db.Programlars.Where(p => p.IsAktif).OrderBy(o => o.ProgramAdi).ToList();
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
                var data = db.Programlars.Where(p => p.IsAktif && p.AnabilimDallari.EnstituKod == EnstituKod).OrderBy(o => o.ProgramAdi).ToList();
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
                data = data.OrderBy(o => o.ProgramAdi).ToList();
            }
            return dct;
        }
        public static List<CmbStringDto> cmbGetAktifProgramlarEnstituYetki(bool bosSecimVar = false)
        {
            var dct = new List<CmbStringDto>();
            if (bosSecimVar) dct.Add(new CmbStringDto { Value = "", Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.Programlars.Where(p => p.IsAktif && UserIdentity.Current.EnstituKods.Contains(p.AnabilimDallari.EnstituKod)).OrderBy(o => o.ProgramAdi).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbStringDto { Value = item.ProgramKod, Caption = item.ProgramAdi });
                }
            }
            return dct;
        }
        public static List<CmbStringDto> cmbGetTumProgramlar(bool bosSecimVar = false)
        {
            var dct = new List<CmbStringDto>();
            if (bosSecimVar) dct.Add(new CmbStringDto { Value = "", Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.Programlars.OrderBy(o => o.ProgramAdi).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbStringDto { Value = item.ProgramKod, Caption = item.ProgramAdi });
                }
            }
            return dct;

        }
        public static List<CmbStringDto> CmbGetBSTumProgramlar(int BasvuruSurecID, bool IsBolumOrOgrenci, List<int> OgrenimTipKods, bool IsBSonucOrMulakat)
        {
            var dct = new List<CmbStringDto>();

            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var BasvuruSureci = db.BasvuruSurecs.Where(p => p.BasvuruSurecID == BasvuruSurecID).First();
                var KullaniciProgramKods = Management.GetUserProgramKods(UserIdentity.Current.Id, BasvuruSureci.EnstituKod);

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
        public static List<CmbStringDto> cmbGetYetkiliProgramlar(bool bosSecimVar = false)
        {
            var EnstKods = UserIdentity.Current.EnstituKods ?? new List<string>();
            var dct = new List<CmbStringDto>();
            if (bosSecimVar) dct.Add(new CmbStringDto { Value = "", Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.Programlars.Where(p => EnstKods.Contains(p.AnabilimDallari.EnstituKod)).OrderBy(o => o.ProgramAdi).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbStringDto { Value = item.ProgramKod, Caption = item.ProgramAdi });
                }
            }
            return dct;

        }
        public static List<CmbStringDto> cmbGetKullaniciProgramlari(int KullaniciID, string EnstituKod, string BolumKod, bool bosSecimVar = false)
        {

            var dct = new List<CmbStringDto>();
            if (bosSecimVar) dct.Add(new CmbStringDto { Value = "", Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var kulProgId = Management.GetUserProgramKods(KullaniciID, EnstituKod);
                var data = db.Programlars.Where(p => p.AnabilimDaliKod == BolumKod && kulProgId.Contains(p.ProgramKod)).OrderBy(o => o.ProgramAdi).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbStringDto { Value = item.ProgramKod, Caption = item.ProgramAdi });
                }
            }
            return dct;
        }
        public static List<CmbStringDto> cmbGetKullaniciMulakatProgramlari(int BasvuruSurecID, int KullaniciID, bool bosSecimVar = false, int? HaricMulakatID = null)
        {

            var dct = new List<CmbStringDto>();
            if (bosSecimVar) dct.Add(new CmbStringDto { Value = "", Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var bsurec = db.BasvuruSurecs.Where(p => p.BasvuruSurecID == BasvuruSurecID).First();
                var ots = db.BasvuruSurecOgrenimTipleris.Where(p => p.BasvuruSurecID == bsurec.BasvuruSurecID && p.MulakatSurecineGirecek).Select(s => s.OgrenimTipKod).ToList();
                var kulProgId = Management.GetUserProgramKods(KullaniciID, bsurec.EnstituKod);
                var bsurecProg = bsurec.BasvuruSurecKotalars.Where(a => (!a.MulakatSurecineGirecek.HasValue || a.MulakatSurecineGirecek == true) && ots.Contains(a.OgrenimTipKod)).Select(s => new { s.ProgramKod, s.OgrenimTipKod }).Distinct();
                var tanimlanan = db.Mulakats.Where(p => p.BasvuruSurecID == BasvuruSurecID).Select(s => new { s.ProgramKod, s.OgrenimTipKod }).ToList();
                var data = (from s in bsurecProg
                            join p in db.Programlars.Where(p => kulProgId.Contains(p.ProgramKod)) on s.ProgramKod equals p.ProgramKod
                            select new { s.ProgramKod, s.OgrenimTipKod, p.ProgramAdi }).ToList();

                string HaricProgramKod = "";
                if (HaricMulakatID.HasValue)
                {
                    var hMul = db.Mulakats.Where(p => p.MulakatID == HaricMulakatID.Value).First();
                    HaricProgramKod = hMul.ProgramKod;
                }
                foreach (var item in data)
                {
                    if (dct.Any(a => a.Value == item.ProgramKod) == false && ((tanimlanan.Any(a => a.OgrenimTipKod == item.OgrenimTipKod && a.ProgramKod == item.ProgramKod) == false) || (!HaricProgramKod.IsNullOrWhiteSpace() ? item.ProgramKod == HaricProgramKod : 1 == 2)))
                        dct.Add(new CmbStringDto { Value = item.ProgramKod, Caption = item.ProgramAdi });
                }
            }
            return dct;
        }
        public static List<CmbIntDto> cmbGetKullaniciMulakatOgrenimTipleri(int BasvuruSurecID, int KullaniciID, string ProgramKod, bool bosSecimVar = false, int? HaricMulakatID = null)
        {

            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var bsurec = db.BasvuruSurecs.Where(p => p.BasvuruSurecID == BasvuruSurecID && p.BasvuruSurecOgrenimTipleris.Any(an => an.MulakatSurecineGirecek)).First();
                var kulProgId = Management.GetUserProgramKods(KullaniciID, bsurec.EnstituKod);
                var bsurecOt = bsurec.BasvuruSurecKotalars.Where(p => kulProgId.Contains(p.ProgramKod) && p.ProgramKod == ProgramKod && (!p.MulakatSurecineGirecek.HasValue || p.MulakatSurecineGirecek == true)).Select(s => s.OgrenimTipKod).Distinct().ToList();

                int? HaricOgrenimTipKod = null;
                if (HaricMulakatID.HasValue)
                {
                    var hMul = db.Mulakats.Where(p => p.MulakatID == HaricMulakatID.Value).First();
                    HaricOgrenimTipKod = hMul.OgrenimTipKod;
                }
                var mulOtIds = db.Mulakats.Where(p => p.BasvuruSurecID == BasvuruSurecID && p.ProgramKod == ProgramKod).Select(s => s.OgrenimTipKod).Distinct().ToList();
                foreach (var item in mulOtIds)
                {
                    bsurecOt.Remove(item);
                }
                var data = db.OgrenimTipleris.Where(p => p.EnstituKod == bsurec.EnstituKod && (bsurecOt.Contains(p.OgrenimTipKod) || (HaricOgrenimTipKod.HasValue ? p.OgrenimTipKod == HaricOgrenimTipKod.Value : false))).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.OgrenimTipKod, Caption = item.OgrenimTipAdi });
                }
            }
            return dct;
        }
        public static List<CmbIntDto> cmbGetMulakatSinavTurleri(bool BosSecimVar)
        {
            var dct = new List<CmbIntDto>();
            if (BosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var mlkST = db.MulakatSinavTurleris.Where(p => p.IsAktif).OrderBy(o => o.MulakatSinavTurID).ToList();

                foreach (var item in mlkST)
                {
                    dct.Add(new CmbIntDto { Value = item.MulakatSinavTurID, Caption = item.MulakatSinavTurAdi });
                }
            }
            return dct;
        }
        public static List<CmbIntDto> cmbGetKampusler(bool BosSecimVar)
        {
            var dct = new List<CmbIntDto>();
            if (BosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var mlkST = db.Kampuslers.Where(p => p.IsAktif).OrderBy(o => o.KampusAdi).ToList();

                foreach (var item in mlkST)
                {
                    dct.Add(new CmbIntDto { Value = item.KampusID, Caption = item.KampusAdi });
                }
            }
            return dct;
        }
        public static List<CmbIntDto> cmbGetSeciliBolumler(string EKD, int OgrenimTipKod)
        {
            var _EntituKod = getSelectedEnstitu(EKD);
            var dct = new List<CmbIntDto>();
            dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {

                var listBolID = db.Programlars.Where(p => p.AnabilimDallari.IsAktif && p.Kotalars.Any(a => a.OgrenimTipKod == OgrenimTipKod) && p.AnabilimDallari.EnstituKod == _EntituKod).Select(s => s.AnabilimDallari.AnabilimDaliID).Distinct().ToList();


                var bols = db.AnabilimDallaris.Where(p => listBolID.Contains(p.AnabilimDaliID)).OrderBy(o => o.AnabilimDaliAdi).ToList();

                foreach (var item in bols)
                {
                    dct.Add(new CmbIntDto { Value = item.AnabilimDaliID, Caption = item.AnabilimDaliAdi });
                }
            }
            return dct;
        }
        public static List<krMulakatSonuc> getMulakatSonucListMulakatsiz(int BasvuruSurecID, string ProgramKod, int OgrenimTipKod, List<int> BasvuruTercihleriIDs)
        {
            var mlktSonucModel = new List<krMulakatSonuc>();
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var BasvuruSurec = db.BasvuruSurecs.Where(p => p.BasvuruSurecID == BasvuruSurecID).First();

                var YaziliSinaviIstensin = false;
                var SozluSinaviIstensin = false;
                var tercihEdenler = BasvuruSurec.Basvurulars.Where(p => p.BasvurularTercihleris.Any(a => a.ProgramKod == ProgramKod && a.OgrenimTipKod == OgrenimTipKod) && p.BasvuruDurumID == BasvuruDurumu.Onaylandı).ToList();

                var kota = BasvuruSurec.BasvuruSurecKotalars.Where(p => p.OgrenimTipKod == OgrenimTipKod && p.ProgramKod == ProgramKod).First();
                var data = db.BasvurularTercihleris.Where(a => a.Basvurular.BasvuruSurecID == BasvuruSurecID && a.ProgramKod == ProgramKod && a.OgrenimTipKod == OgrenimTipKod && a.Basvurular.BasvuruDurumID == BasvuruDurumu.Onaylandı).OrderBy(o => o.Basvurular.Ad).ThenBy(t => t.Basvurular.Soyad).ToList();
                var bsSinavBilgi = BasvuruSurec.BasvuruSurecSinavTipleris.Where(p => p.BasvuruSurecID == BasvuruSurecID).ToList();
                var BasvuruSurecOgrenimTipi = BasvuruSurec.BasvuruSurecOgrenimTipleris.Where(p => p.OgrenimTipKod == OgrenimTipKod).First();

                var MulakatSonuclari = BasvuruSurec.MulakatSonuclaris.Where(p => BasvuruTercihleriIDs.Contains(p.BasvuruTercihID)).ToList();

                var qSinavOt = BasvuruSurec.BasvuruSurecSinavTipleriOTNotAraliklaris.Where(p => p.BasvuruSurecID == BasvuruSurecID).ToList();

                foreach (var btercih in data)
                {

                    var dilSinavi = btercih.Basvurular.BasvurularSinavBilgis.Where(p => p.SinavTipGrupID == SinavTipGrup.DilSinavlari).FirstOrDefault();
                    var kbtercih = MulakatSonuclari.Where(p => p.BasvuruTercihID == btercih.BasvuruTercihID).FirstOrDefault();
                    var mlktSonucItem = new krMulakatSonuc();
                    mlktSonucItem.IsAlesYerineDosyaNotuIstensin = kota.IsAlesYerineDosyaNotuIstensin == true;

                    mlktSonucItem.AdSoyad = btercih.Basvurular.Ad + " " + btercih.Basvurular.Soyad;


                    if (!mlktSonucItem.IsAlesYerineDosyaNotuIstensin)
                    {
                        bool sinavYok = qSinavOt.Where(p => p.OgrenimTipKod == btercih.OgrenimTipKod && p.SinavTipleri.SinavTipGrupID == SinavTipGrup.Ales_Gree
                                        && (p.BasvuruSurecSinavTipleriOTNotAraliklariGecersizProgramlars.Any(a => a.ProgramKod == btercih.ProgramKod) == true || !p.IsGecerli || !p.IsIstensin)).Any();


                        var sinavBilgi = btercih.Basvurular.BasvurularSinavBilgis.Where(p => p.SinavTipGrupID == SinavTipGrup.Ales_Gree).FirstOrDefault();
                        if (sinavBilgi != null && sinavYok == false) // ales notu al
                        {
                            var _snvBilgi = bsSinavBilgi.Where(p => p.SinavTipID == sinavBilgi.SinavTipID).First();
                            if (_snvBilgi.WebService) // ales notu al
                            {
                                var wsxmlNot = sinavBilgi.WsXmlData.toSinavSonucAlesXmlModel();
                                if (btercih.Programlar.AlesNotuYuksekOlanAlinsin)
                                {
                                    var maxNot = new Dictionary<int, double>();
                                    if (btercih.Programlar.ProgramlarAlesEslesmeleris.Any(a => a.AlesTipID == AlesTipBilgi.Sayısal)) maxNot.Add(AlesTipBilgi.Sayısal, wsxmlNot.SAY_PUAN.ToDouble().Value.ToString("n2").ToDouble().Value);
                                    if (btercih.Programlar.ProgramlarAlesEslesmeleris.Any(a => a.AlesTipID == AlesTipBilgi.Sözel)) maxNot.Add(AlesTipBilgi.Sözel, wsxmlNot.SOZ_PUAN.ToDouble().Value.ToString("n2").ToDouble().Value);
                                    if (btercih.Programlar.ProgramlarAlesEslesmeleris.Any(a => a.AlesTipID == AlesTipBilgi.EşitAğırlık)) maxNot.Add(AlesTipBilgi.EşitAğırlık, wsxmlNot.EA_PUAN.ToDouble().Value.ToString("n2").ToDouble().Value);
                                    mlktSonucItem.AlesNotuOrDosyaNotu = maxNot.Select(s => s.Value).Max();
                                }
                                else
                                {
                                    if (btercih.Programlar.AlesTipID == AlesTipBilgi.Sayısal)
                                        mlktSonucItem.AlesNotuOrDosyaNotu = wsxmlNot.SAY_PUAN.ToDouble().ToString("n2").ToDouble().Value;
                                    else if (btercih.Programlar.AlesTipID == AlesTipBilgi.Sözel)
                                        mlktSonucItem.AlesNotuOrDosyaNotu = wsxmlNot.SOZ_PUAN.ToDouble().ToString("n2").ToDouble().Value;
                                    else if (btercih.Programlar.AlesTipID == AlesTipBilgi.EşitAğırlık)
                                        mlktSonucItem.AlesNotuOrDosyaNotu = wsxmlNot.EA_PUAN.ToDouble().ToString("n2").ToDouble().Value;
                                }
                            }
                            else
                                mlktSonucItem.AlesNotuOrDosyaNotu = sinavBilgi.SinavNotu;
                        }
                        else mlktSonucItem.AlesNotuOrDosyaNotu = null;
                    }
                    else if (kbtercih != null)
                    {
                        mlktSonucItem.AlesNotuOrDosyaNotu = kbtercih.AlesNotuOrDosyaNotu;
                    }

                    var _BasvuruAgnoAlimTipID = btercih.Programlar.BasvuruAgnoAlimTipID;
                    if (_BasvuruAgnoAlimTipID.HasValue)
                    {
                        if (BasvuruSurecOgrenimTipi.LEgitimBilgisiIste && BasvuruSurecOgrenimTipi.YLEgitimBilgisiIste)
                        {
                            if (_BasvuruAgnoAlimTipID.Value == BasvuruAgnoAlimTipi.LisansAlinsin)
                            {
                                mlktSonucItem.Agno = btercih.Basvurular.LMezuniyetNotu100LukSistem;
                            }
                            else if (_BasvuruAgnoAlimTipID.Value == BasvuruAgnoAlimTipi.YLisansAlinsin)
                            {
                                mlktSonucItem.Agno = btercih.Basvurular.YLMezuniyetNotu100LukSistem;
                            }
                            else if (_BasvuruAgnoAlimTipID.Value == BasvuruAgnoAlimTipi.L_YLYuzdeBelirlensin)
                            {
                                var oran = ((btercih.Programlar.LYuzdeOran * btercih.Basvurular.LMezuniyetNotu100LukSistem) / 100.00) + ((btercih.Programlar.YLYuzdeOran * btercih.Basvurular.YLMezuniyetNotu100LukSistem) / 100.00);
                                mlktSonucItem.Agno = oran;
                            }
                        }
                        else if (BasvuruSurecOgrenimTipi.LEgitimBilgisiIste && !BasvuruSurecOgrenimTipi.YLEgitimBilgisiIste)
                        {
                            mlktSonucItem.Agno = btercih.Basvurular.LMezuniyetNotu100LukSistem;
                        }
                        else if (!BasvuruSurecOgrenimTipi.LEgitimBilgisiIste && BasvuruSurecOgrenimTipi.YLEgitimBilgisiIste)
                        {
                            mlktSonucItem.Agno = btercih.Basvurular.YLMezuniyetNotu100LukSistem;
                        }
                    }
                    else
                    {
                        if (btercih.OgrenimTipKod == OgrenimTipi.Doktra)
                            mlktSonucItem.Agno = btercih.Basvurular.YLMezuniyetNotu100LukSistem;
                        else
                            mlktSonucItem.Agno = btercih.Basvurular.LMezuniyetNotu100LukSistem;
                    }


                    mlktSonucItem.AlanKota = kota.OrtakKota ? kota.OrtakKotaSayisi.Value : (btercih.AlanTipID == AlanTipi.AlanIci ? kota.AlanIciKota : kota.AlanDisiKota);

                    if (kbtercih != null)
                    {

                        mlktSonucItem.MulakatSonucID = kbtercih.MulakatSonucID;
                        mlktSonucItem.MulakatSonucTipID = kbtercih.MulakatSonucTipID;
                        mlktSonucItem.KayitDurumID = kbtercih.KayitDurumID;
                        mlktSonucItem.BasvuruSurecID = BasvuruSurecID;
                        mlktSonucItem.MulakatID = kbtercih.MulakatID;
                        mlktSonucItem.BasvuruID = kbtercih.BasvuruID;
                        mlktSonucItem.BasvuruTercihID = kbtercih.BasvuruTercihID;
                        mlktSonucItem.AlanTipID = kbtercih.AlanTipID;
                        mlktSonucItem.SiraNo = kbtercih.SiraNo;
                        mlktSonucItem.SinavaGirmediY = kbtercih.SinavaGirmediY;
                        mlktSonucItem.YaziliNotu = kbtercih.YaziliNotu;
                        mlktSonucItem.SozluNotu = kbtercih.SozluNotu;
                        mlktSonucItem.SinavaGirmediS = kbtercih.SinavaGirmediS;
                        mlktSonucItem.GirisSinavNotu = kbtercih.GirisSinavNotu;
                        mlktSonucItem.GenelBasariNotu = kbtercih.GenelBasariNotu;

                    }
                    else
                    {
                        mlktSonucItem.MulakatSonucID = 0;
                        mlktSonucItem.MulakatSonucTipID = MulakatSonucTipi.Hesaplanmadı;
                        mlktSonucItem.BasvuruSurecID = BasvuruSurecID;
                        mlktSonucItem.MulakatID = null;
                        mlktSonucItem.BasvuruID = btercih.BasvuruID;
                        mlktSonucItem.BasvuruTercihID = btercih.BasvuruTercihID;
                        mlktSonucItem.AlanTipID = btercih.AlanTipID;
                        mlktSonucItem.SiraNo = null;
                        mlktSonucItem.SinavaGirmediY = null;
                        mlktSonucItem.SinavaGirmediS = null;
                        mlktSonucItem.YaziliNotu = null;
                        mlktSonucItem.SozluNotu = null;
                        mlktSonucItem.GirisSinavNotu = null;
                        mlktSonucItem.GenelBasariNotu = null;
                    }
                    mlktSonucItem.KullaniID = btercih.Basvurular.KullaniciID;
                    bool successRow = true;
                    if (YaziliSinaviIstensin)
                    {
                        if (mlktSonucItem.SinavaGirmediY.HasValue == false && mlktSonucItem.YaziliNotu.HasValue == false) successRow = false;
                    }
                    if (SozluSinaviIstensin)
                    {
                        if (mlktSonucItem.SinavaGirmediS.HasValue == false && mlktSonucItem.SozluNotu.HasValue == false) successRow = false;
                    }
                    mlktSonucItem.SuccessRow = successRow;

                    mlktSonucModel.Add(mlktSonucItem);

                }
            }
            return mlktSonucModel;
        }
        public static List<krMulakatSonuc> getMulakatSonucList(int MulakatID)
        {
            var mlktSonucModel = new List<krMulakatSonuc>();
            using (var db = new LisansustuBasvuruSistemiEntities())
            {

                var mulakat = db.Mulakats.Where(p => p.MulakatID == MulakatID).First();
                var mulSinavTurs = mulakat.MulakatDetays.ToList();
                var YaziliSinaviIstensin = mulSinavTurs.Any(a => a.MulakatSinavTurID == MulakatSinavTur.Yazili);
                var SozluSinaviIstensin = mulSinavTurs.Any(a => a.MulakatSinavTurID == MulakatSinavTur.Sozlu);
                var tercihEdenler = mulakat.BasvuruSurec.Basvurulars.Where(p => p.BasvurularTercihleris.Any(a => a.ProgramKod == mulakat.ProgramKod && a.OgrenimTipKod == mulakat.OgrenimTipKod) && p.BasvuruDurumID == BasvuruDurumu.Onaylandı).ToList();

                var kota = db.BasvuruSurecKotalars.Where(p => p.BasvuruSurecID == mulakat.BasvuruSurecID && p.OgrenimTipKod == mulakat.OgrenimTipKod && p.ProgramKod == mulakat.ProgramKod).First();
                var data = db.BasvurularTercihleris.Where(a => a.Basvurular.BasvuruSurecID == mulakat.BasvuruSurecID && a.ProgramKod == mulakat.ProgramKod && a.OgrenimTipKod == mulakat.OgrenimTipKod && a.Basvurular.BasvuruDurumID == BasvuruDurumu.Onaylandı).OrderBy(o => o.Basvurular.Ad).ThenBy(t => t.Basvurular.Soyad).ToList();
                var bsSinavBilgi = db.BasvuruSurecSinavTipleris.Where(p => p.BasvuruSurecID == mulakat.BasvuruSurecID).ToList();
                var BasvuruSurecOgrenimTipi = db.BasvuruSurecOgrenimTipleris.Where(p => p.BasvuruSurecID == mulakat.BasvuruSurecID && p.OgrenimTipKod == mulakat.OgrenimTipKod).First();



                var qSinavOt = db.BasvuruSurecSinavTipleriOTNotAraliklaris.Where(p => p.BasvuruSurecID == mulakat.BasvuruSurecID).ToList();

                foreach (var btercih in data)
                {

                    var dilSinavi = btercih.Basvurular.BasvurularSinavBilgis.Where(p => p.SinavTipGrupID == SinavTipGrup.DilSinavlari).FirstOrDefault();
                    var kbtercih = mulakat.MulakatSonuclaris.Where(p => p.BasvuruTercihID == btercih.BasvuruTercihID).FirstOrDefault();
                    var mlktSonucItem = new krMulakatSonuc();
                    mlktSonucItem.IsAlesYerineDosyaNotuIstensin = kota.IsAlesYerineDosyaNotuIstensin == true;

                    mlktSonucItem.AdSoyad = btercih.Basvurular.Ad + " " + btercih.Basvurular.Soyad;


                    if (!mlktSonucItem.IsAlesYerineDosyaNotuIstensin)
                    {
                        bool sinavYok = qSinavOt.Where(p => p.OgrenimTipKod == btercih.OgrenimTipKod && p.SinavTipleri.SinavTipGrupID == SinavTipGrup.Ales_Gree
                                        && (p.BasvuruSurecSinavTipleriOTNotAraliklariGecersizProgramlars.Any(a => a.ProgramKod == btercih.ProgramKod) == true || !p.IsGecerli || !p.IsIstensin)).Any();


                        var sinavBilgi = btercih.Basvurular.BasvurularSinavBilgis.Where(p => p.SinavTipGrupID == SinavTipGrup.Ales_Gree).FirstOrDefault();
                        if (sinavBilgi != null && sinavYok == false) // ales notu al
                        {
                            var _snvBilgi = bsSinavBilgi.Where(p => p.SinavTipID == sinavBilgi.SinavTipID).First();
                            if (_snvBilgi.WebService) // ales notu al
                            {
                                var wsxmlNot = sinavBilgi.WsXmlData.toSinavSonucAlesXmlModel();
                                if (btercih.Programlar.AlesNotuYuksekOlanAlinsin)
                                {
                                    var maxNot = new Dictionary<int, double>();
                                    if (btercih.Programlar.ProgramlarAlesEslesmeleris.Any(a => a.AlesTipID == AlesTipBilgi.Sayısal)) maxNot.Add(AlesTipBilgi.Sayısal, wsxmlNot.SAY_PUAN.ToDouble().Value.ToString("n2").ToDouble().Value);
                                    if (btercih.Programlar.ProgramlarAlesEslesmeleris.Any(a => a.AlesTipID == AlesTipBilgi.Sözel)) maxNot.Add(AlesTipBilgi.Sözel, wsxmlNot.SOZ_PUAN.ToDouble().Value.ToString("n2").ToDouble().Value);
                                    if (btercih.Programlar.ProgramlarAlesEslesmeleris.Any(a => a.AlesTipID == AlesTipBilgi.EşitAğırlık)) maxNot.Add(AlesTipBilgi.EşitAğırlık, wsxmlNot.EA_PUAN.ToDouble().Value.ToString("n2").ToDouble().Value);
                                    mlktSonucItem.AlesNotuOrDosyaNotu = maxNot.Select(s => s.Value).Max();
                                }
                                else
                                {
                                    if (btercih.Programlar.AlesTipID == AlesTipBilgi.Sayısal)
                                        mlktSonucItem.AlesNotuOrDosyaNotu = wsxmlNot.SAY_PUAN.ToDouble().ToString("n2").ToDouble().Value;
                                    else if (btercih.Programlar.AlesTipID == AlesTipBilgi.Sözel)
                                        mlktSonucItem.AlesNotuOrDosyaNotu = wsxmlNot.SOZ_PUAN.ToDouble().ToString("n2").ToDouble().Value;
                                    else if (btercih.Programlar.AlesTipID == AlesTipBilgi.EşitAğırlık)
                                        mlktSonucItem.AlesNotuOrDosyaNotu = wsxmlNot.EA_PUAN.ToDouble().ToString("n2").ToDouble().Value;
                                }
                            }
                            else
                                mlktSonucItem.AlesNotuOrDosyaNotu = sinavBilgi.SinavNotu;
                        }
                        else mlktSonucItem.AlesNotuOrDosyaNotu = null;
                    }
                    else if (kbtercih != null)
                    {
                        mlktSonucItem.AlesNotuOrDosyaNotu = kbtercih.AlesNotuOrDosyaNotu;
                    }

                    var _BasvuruAgnoAlimTipID = btercih.Programlar.BasvuruAgnoAlimTipID;
                    if (_BasvuruAgnoAlimTipID.HasValue)
                    {
                        if (BasvuruSurecOgrenimTipi.LEgitimBilgisiIste && BasvuruSurecOgrenimTipi.YLEgitimBilgisiIste)
                        {
                            if (_BasvuruAgnoAlimTipID.Value == BasvuruAgnoAlimTipi.LisansAlinsin)
                            {
                                mlktSonucItem.Agno = btercih.Basvurular.LMezuniyetNotu100LukSistem;
                            }
                            else if (_BasvuruAgnoAlimTipID.Value == BasvuruAgnoAlimTipi.YLisansAlinsin)
                            {
                                mlktSonucItem.Agno = btercih.Basvurular.YLMezuniyetNotu100LukSistem;
                            }
                            else if (_BasvuruAgnoAlimTipID.Value == BasvuruAgnoAlimTipi.L_YLYuzdeBelirlensin)
                            {
                                var oran = ((btercih.Programlar.LYuzdeOran * btercih.Basvurular.LMezuniyetNotu100LukSistem) / 100.00) + ((btercih.Programlar.YLYuzdeOran * btercih.Basvurular.YLMezuniyetNotu100LukSistem) / 100.00);
                                mlktSonucItem.Agno = oran;
                            }
                        }
                        else if (BasvuruSurecOgrenimTipi.LEgitimBilgisiIste && !BasvuruSurecOgrenimTipi.YLEgitimBilgisiIste)
                        {
                            mlktSonucItem.Agno = btercih.Basvurular.LMezuniyetNotu100LukSistem;
                        }
                        else if (!BasvuruSurecOgrenimTipi.LEgitimBilgisiIste && BasvuruSurecOgrenimTipi.YLEgitimBilgisiIste)
                        {
                            mlktSonucItem.Agno = btercih.Basvurular.YLMezuniyetNotu100LukSistem;
                        }
                    }
                    else
                    {
                        if (btercih.OgrenimTipKod == OgrenimTipi.Doktra)
                            mlktSonucItem.Agno = btercih.Basvurular.YLMezuniyetNotu100LukSistem;
                        else
                            mlktSonucItem.Agno = btercih.Basvurular.LMezuniyetNotu100LukSistem;
                    }


                    mlktSonucItem.AlanKota = kota.OrtakKota ? kota.OrtakKotaSayisi.Value : (btercih.AlanTipID == AlanTipi.AlanIci ? kota.AlanIciKota : kota.AlanDisiKota);

                    if (kbtercih != null)
                    {

                        mlktSonucItem.MulakatSonucID = kbtercih.MulakatSonucID;
                        mlktSonucItem.MulakatSonucTipID = kbtercih.MulakatSonucTipID;
                        mlktSonucItem.KayitDurumID = kbtercih.KayitDurumID;
                        mlktSonucItem.BasvuruSurecID = mulakat.BasvuruSurecID;
                        mlktSonucItem.MulakatID = kbtercih.MulakatID;
                        mlktSonucItem.BasvuruID = kbtercih.BasvuruID;
                        mlktSonucItem.BasvuruTercihID = kbtercih.BasvuruTercihID;
                        mlktSonucItem.AlanTipID = kbtercih.AlanTipID;
                        mlktSonucItem.SiraNo = kbtercih.SiraNo;

                        mlktSonucItem.SinavaGirmediY = kbtercih.SinavaGirmediY;
                        mlktSonucItem.YaziliNotu = kbtercih.YaziliNotu;
                        mlktSonucItem.SozluNotu = kbtercih.SozluNotu;
                        mlktSonucItem.SinavaGirmediS = kbtercih.SinavaGirmediS;
                        mlktSonucItem.GirisSinavNotu = kbtercih.GirisSinavNotu;
                        mlktSonucItem.GenelBasariNotu = kbtercih.GenelBasariNotu;

                    }
                    else
                    {
                        mlktSonucItem.MulakatSonucID = 0;
                        mlktSonucItem.MulakatSonucTipID = MulakatSonucTipi.Hesaplanmadı;
                        mlktSonucItem.BasvuruSurecID = mulakat.BasvuruSurecID;
                        mlktSonucItem.MulakatID = MulakatID;
                        mlktSonucItem.BasvuruID = btercih.BasvuruID;
                        mlktSonucItem.BasvuruTercihID = btercih.BasvuruTercihID;
                        mlktSonucItem.AlanTipID = btercih.AlanTipID;
                        mlktSonucItem.SiraNo = null;
                        mlktSonucItem.SinavaGirmediY = null;
                        mlktSonucItem.SinavaGirmediS = null;
                        mlktSonucItem.YaziliNotu = null;
                        mlktSonucItem.SozluNotu = null;
                        mlktSonucItem.GirisSinavNotu = null;
                        mlktSonucItem.GenelBasariNotu = null;
                    }
                    mlktSonucItem.KullaniID = btercih.Basvurular.KullaniciID;
                    bool successRow = true;
                    if (YaziliSinaviIstensin)
                    {
                        if (mlktSonucItem.SinavaGirmediY.HasValue == false && mlktSonucItem.YaziliNotu.HasValue == false) successRow = false;
                    }
                    if (SozluSinaviIstensin)
                    {
                        if (mlktSonucItem.SinavaGirmediS.HasValue == false && mlktSonucItem.SozluNotu.HasValue == false) successRow = false;
                    }
                    mlktSonucItem.SuccessRow = successRow;

                    mlktSonucModel.Add(mlktSonucItem);

                }
            }
            return mlktSonucModel;
        }
        public static List<krMulakatSonuc> getMulakatSonucHesapList(int BasvuruSurecID, string ProgramKod = null, int? OgrenimTipKod = null)
        {
            using (var db = new LisansustuBasvuruSistemiEntities())
            {


                var qTercihlers = (from bt in db.BasvurularTercihleris
                                   join snc in db.MulakatSonuclaris on bt.BasvuruTercihID equals snc.BasvuruTercihID into defsnc
                                   from Msnc in defsnc.DefaultIfEmpty()
                                   join sncT in db.MulakatSonucTipleris on Msnc.MulakatSonucTipID equals sncT.MulakatSonucTipID into defsncT
                                   from MsncT in defsncT.DefaultIfEmpty()
                                   join pr in db.Programlars on bt.ProgramKod equals pr.ProgramKod
                                   join b in db.Basvurulars on bt.BasvuruID equals b.BasvuruID
                                   join bs in db.BasvuruSurecs on b.BasvuruSurecID equals bs.BasvuruSurecID
                                   join BsOt in db.BasvuruSurecOgrenimTipleris on new { b.BasvuruSurecID, bt.OgrenimTipKod } equals new { BsOt.BasvuruSurecID, BsOt.OgrenimTipKod }
                                   join BsKt in db.BasvuruSurecKotalars on new { b.BasvuruSurecID, bt.ProgramKod, bt.OgrenimTipKod } equals new { BsKt.BasvuruSurecID, BsKt.ProgramKod, BsKt.OgrenimTipKod }
                                   join BtAles in db.BasvurularSinavBilgis.Where(p => p.SinavTipGrupID == SinavTipGrup.Ales_Gree) on b.BasvuruID equals BtAles.BasvuruID into defBtAles
                                   from AlesSinavi in defBtAles.DefaultIfEmpty()
                                   join BtAlesD in db.BasvuruSurecSinavTipleris on new { b.BasvuruSurecID, AlesSinavi.SinavTipID } equals new { BtAlesD.BasvuruSurecID, BtAlesD.SinavTipID } into defBtAlesD
                                   from AlesDetay in defBtAlesD.DefaultIfEmpty()
                                   join BtDil in db.BasvurularSinavBilgis.Where(p => p.SinavTipGrupID == SinavTipGrup.DilSinavlari) on b.BasvuruID equals BtDil.BasvuruID into defBtDil
                                   from DilSinavi in defBtDil.DefaultIfEmpty()
                                   join BtDilD in db.BasvuruSurecSinavTipleris on new { b.BasvuruSurecID, DilSinavi.SinavTipID } equals new { BtDilD.BasvuruSurecID, BtDilD.SinavTipID } into defBtDilD
                                   from DilDetay in defBtDilD.DefaultIfEmpty()
                                   join BtTomer in db.BasvurularSinavBilgis.Where(p => p.SinavTipGrupID == SinavTipGrup.Tomer) on b.BasvuruID equals BtTomer.BasvuruID into defBtTomer
                                   from TomerSinavi in defBtTomer.DefaultIfEmpty()
                                   join dekont in db.BasvurularTercihleriKayitOdemeleris on new { bt.BasvuruTercihID, DonemNo = 1, IsOdendi = true } equals new { dekont.BasvuruTercihID, dekont.DonemNo, dekont.IsOdendi } into defDekont
                                   from Dk in defDekont.DefaultIfEmpty()
                                   join Mul in db.Mulakats on new { b.BasvuruSurecID, bt.OgrenimTipKod, bt.ProgramKod } equals new { Mul.BasvuruSurecID, Mul.OgrenimTipKod, Mul.ProgramKod } into defMul
                                   from Ml in defMul.DefaultIfEmpty()
                                   where b.BasvuruSurecID == BasvuruSurecID &&
                                         bt.ProgramKod == (ProgramKod ?? bt.ProgramKod) &&
                                         bt.OgrenimTipKod == (OgrenimTipKod ?? bt.OgrenimTipKod) &&
                                         (b.BasvuruDurumID == BasvuruDurumu.Onaylandı || Msnc != null)

                                   select new
                                   {
                                       b.BasvuruSurecID,
                                       bs.BasvuruSurecTipID,
                                       b.BasvuruID,
                                       b.KullaniciID,
                                       b.KullaniciTipleri.Yerli,
                                       b.LOgrenimDurumID,
                                       b.LNotSistemID,
                                       b.LMezuniyetNotu,
                                       b.LMezuniyetNotu100LukSistem,
                                       b.YLNotSistemID,
                                       b.YLMezuniyetNotu,
                                       b.YLMezuniyetNotu100LukSistem,
                                       AdSoyad = b.Ad + " " + b.Soyad,
                                       bt.UniqueID,
                                       bt.BasvuruTercihID,
                                       bt.OgrenimTipKod,
                                       bt.ProgramKod,
                                       bt.AlanTipID,
                                       bt.KayitSiraNo,
                                       pr.AnabilimDaliKod,
                                       pr.Ingilizce,
                                       pr.AlesNotuYuksekOlanAlinsin,
                                       pr.AlesTipID,
                                       ProgramAlesTipIDs = pr.ProgramlarAlesEslesmeleris.Select(s => s.AlesTipID).ToList(),
                                       pr.BasvuruAgnoAlimTipID,
                                       pr.LYuzdeOran,
                                       pr.YLYuzdeOran,
                                       pr.Ucretli,
                                       MulakatID = Msnc != null ? Msnc.MulakatID : (int?)null,
                                       MulakatSonucID = Msnc != null ? Msnc.MulakatSonucID : (int?)null,
                                       MulakatSonucTipID = Msnc != null ? Msnc.MulakatSonucTipID : MulakatSonucTipi.Hesaplanmadı,
                                       MsncT.MulakatSonucTipAdi,
                                       Msnc.Aciklama,
                                       Msnc.KayitDurumID,
                                       Msnc.KayitDurumlari,
                                       Msnc.SiraNo,
                                       Msnc.SinavaGirmediY,
                                       Msnc.YaziliNotu,
                                       Msnc.SinavaGirmediS,
                                       Msnc.SozluNotu,
                                       Msnc.GirisSinavNotu,
                                       Msnc.GenelBasariNotu,
                                       Msnc.AlesNotuOrDosyaNotu,
                                       AlesIsteniyor = bs.BasvuruSurecSinavTipleriOTNotAraliklaris.Where(a => a.OgrenimTipKod == bt.OgrenimTipKod && a.Ingilizce == pr.Ingilizce && a.SinavTipID == (AlesSinavi != null ? AlesSinavi.SinavTipID : a.SinavTipID) && a.SinavTipleri.SinavTipGrupID == SinavTipGrup.Ales_Gree && (!a.BasvuruSurecSinavTipleriOTNotAraliklariGecersizProgramlars.Any(a2 => a2.ProgramKod == bt.ProgramKod) && a.IsGecerli && a.IsIstensin)).Any(),
                                       WsXmlData = AlesSinavi == null ? null : AlesSinavi.WsXmlData,
                                       SinavNotu = AlesSinavi == null ? (double?)null : AlesSinavi.SinavNotu,
                                       WebService = AlesDetay == null ? (bool?)null : AlesDetay.WebService,
                                       DilIsteniyor = bs.BasvuruSurecSinavTipleriOTNotAraliklaris.Where(a => a.OgrenimTipKod == bt.OgrenimTipKod && a.Ingilizce == pr.Ingilizce && a.SinavTipID == (DilSinavi != null ? DilSinavi.SinavTipID : a.SinavTipID) && a.SinavTipleri.SinavTipGrupID == SinavTipGrup.DilSinavlari && (!a.BasvuruSurecSinavTipleriOTNotAraliklariGecersizProgramlars.Any(a2 => a2.ProgramKod == bt.ProgramKod) && a.IsGecerli && a.IsIstensin)).Any(),
                                       IsTaahhutVar = DilSinavi == null ? (bool?)null : DilSinavi.IsTaahhutVar,
                                       DSinavNotu = DilSinavi == null ? (double?)null : DilSinavi.SinavNotu,
                                       DWsXmlData = DilSinavi == null ? null : DilSinavi.WsXmlData,
                                       DSinavTipKod = DilSinavi == null ? null : DilSinavi.SinavTipKod,
                                       DWebService = DilDetay != null ? DilDetay.WebService : (bool?)null,
                                       SinavAdi = DilDetay != null ? DilDetay.SinavTipleri.SinavAdi : "",

                                       TSinavTipID = TomerSinavi != null ? TomerSinavi.SinavTipID : 0,
                                       MulakatSurecineGirecek = BsKt.MulakatSurecineGirecek ?? BsOt.MulakatSurecineGirecek,
                                       IsMulakatTanimlandi = Ml != null,
                                       IsAlesYerineDosyaNotuIstensin = BsKt.IsAlesYerineDosyaNotuIstensin == true,
                                       BsKt.OrtakKota,
                                       BsKt.OrtakKotaSayisi,
                                       BsKt.AlanIciKota,
                                       BsKt.AlanDisiKota,
                                       BsKt.AlanIciEkKota,
                                       BsKt.AlanDisiEkKota,
                                       BsKt.MinAGNO,
                                       BsOt.YedekOgrenciSayisiKotaCarpani,
                                       BilimselHazirlikVar = Msnc != null ? Msnc.BilimselHazirlikVar : null,
                                       YaziliSinaviIstensin = (Msnc != null ? Msnc.Mulakat.MulakatDetays.Any(a => a.MulakatSinavTurID == MulakatSinavTur.Yazili) : false),
                                       SozluSinaviIstensin = (Msnc != null ? Msnc.Mulakat.MulakatDetays.Any(a => a.MulakatSinavTurID == MulakatSinavTur.Sozlu) : false),
                                       DekontNo = Dk != null ? Dk.DekontNo : null,
                                       DekontTarihi = Dk != null ? Dk.DekontTarih : null,
                                       bs.IsBelgeYuklemeVar,
                                       BasvuruSurecBelgeTipleris = bs.BasvuruSurecBelgeTipleris,
                                       BasvurularYuklenenBelgelers = b.BasvurularYuklenenBelgelers
                                   }).ToList();

                var Tercihlers = qTercihlers.Where(p => p.MulakatSurecineGirecek ? (p.IsMulakatTanimlandi == true) : true).Select((s, inx) => new krMulakatSonuc

                {

                    BasvuruSurecID = s.BasvuruSurecID,
                    BasvuruSurecTipID = s.BasvuruSurecTipID,
                    BasvuruID = s.BasvuruID,
                    BasvuruTercihID = s.BasvuruTercihID,
                    KullaniID = s.KullaniciID,
                    AdSoyad = s.AdSoyad,
                    MulakatSurecineGirecek = s.MulakatSurecineGirecek,
                    MulakatID = s.MulakatID ?? 0,
                    MulakatSonucID = s.MulakatSonucID ?? 0,
                    MulakatSonucTipID = s.MulakatSonucTipID,
                    MulakatSonucTipAdi = s.MulakatSonucTipAdi,
                    Aciklama = s.Aciklama,
                    KayitDurumID = s.KayitDurumID,
                    KayitDurumlari = s.KayitDurumlari,
                    SiraNo = s.SiraNo,
                    KayitOncelikSiraNo = s.KayitSiraNo,
                    YaziliSinaviIstensin = s.YaziliSinaviIstensin,
                    SinavaGirmediY = s.SinavaGirmediY,
                    YaziliNotu = s.YaziliNotu,
                    SozluSinaviIstensin = s.SozluSinaviIstensin,
                    SinavaGirmediS = s.SinavaGirmediS,
                    SozluNotu = s.SozluNotu,
                    GirisSinavNotu = s.GirisSinavNotu,
                    GenelBasariNotu = s.GenelBasariNotu,
                    AlanTipID = s.AlanTipID,
                    AnabilimDaliKod = s.AnabilimDaliKod,
                    ProgramKod = s.ProgramKod,
                    OgrenimTipKod = s.OgrenimTipKod,
                    IsUcretliKayit = s.DekontNo != null || s.DekontTarihi.HasValue || s.Ucretli,
                    AlanKota = s.OrtakKota ? s.OrtakKotaSayisi.Value : (s.AlanTipID == AlanTipi.AlanIci ? s.AlanIciKota : s.AlanDisiKota),
                    AlanKotaYedek = (s.OrtakKota ? s.OrtakKotaSayisi.Value : (s.AlanTipID == AlanTipi.AlanIci ? s.AlanIciKota : s.AlanDisiKota) * s.YedekOgrenciSayisiKotaCarpani),
                    LOgrenimDurumID = s.LOgrenimDurumID,
                    UniqueID = s.UniqueID.ToString(),
                    Ingilizce = s.Ingilizce,
                    DekontNo = s.DekontNo,
                    DekontTarihi = s.DekontTarihi,
                    ShowBilimselHazirlik = s.OgrenimTipKod == OgrenimTipi.TezsizYuksekLisans ? false : true,
                    BilimselHazirlikVar = s.OgrenimTipKod == OgrenimTipi.TezsizYuksekLisans ? false : (s.AlanTipID == AlanTipi.AlanDisi ? true : (s.BilimselHazirlikVar ?? false)),
                    IsDilTaahhutVar = s.IsTaahhutVar,
                    EnabledBilimselHazirlik = s.AlanTipID == AlanTipi.AlanIci,
                    IsAlesYerineDosyaNotuIstensin = s.IsAlesYerineDosyaNotuIstensin,
                    AlesNotuOrDosyaNotu = (!s.IsAlesYerineDosyaNotuIstensin ?
                                                              (s.AlesIsteniyor ?
                                                                  (s.WebService == true ?
                                                                          (s.AlesNotuYuksekOlanAlinsin ?
                                                                                  s.ProgramAlesTipIDs.toSinavSonucAlesMaxNot(s.WsXmlData)
                                                                                  :
                                                                                  new List<int> { s.AlesTipID }.toSinavSonucAlesMaxNot(s.WsXmlData)
                                                                           )
                                                                           :
                                                                           s.SinavNotu
                                                                   )
                                                                   :
                                                                   null
                                                               )
                                                               : s.AlesNotuOrDosyaNotu
                                                             ),

                    SinavAdi = s.DilIsteniyor ? s.SinavAdi : "",
                    SinavTipKod = s.DilIsteniyor ? s.DSinavTipKod : (int?)null,
                    SinavNotu = (
                                                              s.DilIsteniyor ?
                                                                  (s.DWebService == true ?
                                                                           (s.DSinavNotu > 0 ? (s.DSinavNotu ?? s.DWsXmlData.toStrObjEmptString().toSinavSonucDil(s.BasvuruID)) : null)
                                                                           :
                                                                           s.DSinavNotu
                                                                   )
                                                                   :
                                                                   null
                                                             ),
                    Agno = (

                                                           s.OgrenimTipKod == OgrenimTipi.Doktra ?
                                                             (s.BasvuruSurecTipID != BasvuruSurecTipi.YatayGecisBasvuru ?
                                                                  (
                                                                      s.BasvuruAgnoAlimTipID.HasValue ?
                                                                              (
                                                                                s.BasvuruAgnoAlimTipID == BasvuruAgnoAlimTipi.LisansAlinsin ?
                                                                                  s.LMezuniyetNotu100LukSistem
                                                                                  :
                                                                                  (
                                                                                      s.BasvuruAgnoAlimTipID == BasvuruAgnoAlimTipi.YLisansAlinsin ?
                                                                                      s.YLMezuniyetNotu100LukSistem
                                                                                      :
                                                                                      ((s.LYuzdeOran * s.LMezuniyetNotu100LukSistem) / 100.00) + ((s.YLYuzdeOran * s.YLMezuniyetNotu100LukSistem) / 100.00)

                                                                                  )
                                                                               )
                                                                      :
                                                                      s.YLMezuniyetNotu100LukSistem
                                                                  )
                                                                  :
                                                                  s.YLMezuniyetNotu100LukSistem
                                                              )
                                                              :
                                                              s.LMezuniyetNotu100LukSistem
                                                      ),
                    MezuniyetNotSistemi = (
                                                         OgrenimTipi.Doktra == s.OgrenimTipKod ?
                                                         (
                                                          s.BasvuruSurecTipID != BasvuruSurecTipi.YatayGecisBasvuru ? (
                                                               s.BasvuruAgnoAlimTipID.HasValue ?
                                                                      s.BasvuruAgnoAlimTipID == BasvuruAgnoAlimTipi.LisansAlinsin ?
                                                                      s.LNotSistemID.Value
                                                                      :
                                                                      (
                                                                          s.BasvuruAgnoAlimTipID == BasvuruAgnoAlimTipi.YLisansAlinsin ?
                                                                          s.YLNotSistemID.Value
                                                                          :
                                                                          -1

                                                                      )
                                                               : s.YLNotSistemID.Value
                                                             )
                                                             : s.YLNotSistemID.Value
                                                          )
                                                          :
                                                          s.LNotSistemID.Value
                                                      ),
                    MezuniyetNotu = (
                                                         OgrenimTipi.Doktra == s.OgrenimTipKod ?
                                                             (
                                                              s.BasvuruSurecTipID != BasvuruSurecTipi.YatayGecisBasvuru ? (s.BasvuruAgnoAlimTipID.HasValue ?
                                                                      s.BasvuruAgnoAlimTipID == BasvuruAgnoAlimTipi.LisansAlinsin ?
                                                                      s.LMezuniyetNotu.Value
                                                                      :
                                                                      (
                                                                          s.BasvuruAgnoAlimTipID == BasvuruAgnoAlimTipi.YLisansAlinsin ?
                                                                          s.YLMezuniyetNotu.Value
                                                                          :
                                                                          -1

                                                                      )
                                                               : s.YLMezuniyetNotu.Value
                                                               )
                                                               :
                                                               s.YLMezuniyetNotu.Value
                                                            )
                                                          :
                                                          s.LMezuniyetNotu.Value
                                                      ),
                    KayittaIstenecekBelgeCount = s.BasvuruSurecBelgeTipleris.Where(p => !GetHariciBTID(s.YLNotSistemID, s.AlesIsteniyor, s.DilIsteniyor, s.TSinavTipID > 0, s.Yerli).Contains(p.BasvuruBelgeTipID) && !p.BasvuruSurecBelgeTipleriYuklemeSeklis.Any(a => new List<int> { (s.DilIsteniyor && s.IsTaahhutVar == true ? s.DSinavTipKod.Value : -1) }.Contains(a.SinavTipKod))).Count(),
                    BasvurudaYuklenenBelgeCount = s.BasvurularYuklenenBelgelers.Where(p => !GetHariciBTID(s.YLNotSistemID, s.AlesIsteniyor, s.DilIsteniyor, s.TSinavTipID > 0, s.Yerli).Contains(p.BasvuruBelgeTipID) && !p.BasvuruBelgeTipleri.BasvuruBelgeTipleriYuklemeSeklis.Any(a => new List<int> { (s.DilIsteniyor && s.IsTaahhutVar == true ? s.DSinavTipKod.Value : -1) }.Contains(a.SinavTipKod))).Count(),
                    SuccessRow = (s.MulakatSurecineGirecek ?
                                                                          (
                                                                              (s.MulakatID.HasValue) ?
                                                                                    (s.IsAlesYerineDosyaNotuIstensin ? s.AlesNotuOrDosyaNotu.HasValue : true) && (s.YaziliSinaviIstensin ? s.SinavaGirmediY == true || s.YaziliNotu.HasValue : true) && (s.SozluSinaviIstensin ? s.SinavaGirmediS == true || s.SozluNotu.HasValue : true)
                                                                                    :
                                                                                    false
                                                                           )
                                                               : (s.IsAlesYerineDosyaNotuIstensin ? (s.AlesNotuOrDosyaNotu.HasValue) : true)
                                                              )
                }).OrderBy(o => o.AdSoyad).ToList();



                return Tercihlers;
            }

        }
        public static List<int> GetHariciBTID(int? YLNotSistemID, bool AlesIsteniyor, bool DilIsteniyor, bool TomerIsteniyor, bool IsYerli)
        {
            var retL = new List<int>();
            if (!YLNotSistemID.HasValue) retL.Add(BasvuruBelgeTipi.YLEgitimBelgesi);
            if (!AlesIsteniyor) retL.Add(BasvuruBelgeTipi.AlesGreSinaviBelgesi);
            if (!DilIsteniyor) retL.Add(BasvuruBelgeTipi.DilSinaviBelgesi);
            if (!TomerIsteniyor) retL.Add(BasvuruBelgeTipi.TomerSinaviBelgesi);
            retL.Add(IsYerli ? BasvuruBelgeTipi.TaninirlikBelgesi : BasvuruBelgeTipi.DenklikBelgesi);
            return retL;
        }
        public static ChkListModel GetProgramaBasvurulamayacakOgrenciBolumleri(string EnstituKod, string ProgramKod, List<int> Secilenler = null)
        {
            var Model = new ChkListModel();
            using (var db = new LisansustuBasvuruSistemiEntities())
            {

                if (Secilenler == null && !ProgramKod.IsNullOrWhiteSpace())
                {
                    var data = db.Programlars.Where(p => p.ProgramKod == ProgramKod).FirstOrDefault();
                    Secilenler = data.ProgramlarAlandisiBolumKisitlamalaris.Select(s => s.OgrenciBolumID).ToList();

                }
                if (Secilenler == null) Secilenler = new List<int>();
                var Bolumlers = db.OgrenciBolumleris.Where(p => p.EnstituKod == EnstituKod).Select(s => new { s.OgrenciBolumID, s.BolumAdi }).OrderBy(o => o.BolumAdi).ToList();
                var dataR = Bolumlers.Select(s => new CheckObject<ChkListDataModel>
                {
                    Value = new ChkListDataModel { ID = s.OgrenciBolumID, Caption = s.BolumAdi },
                    Checked = Secilenler.Contains(s.OgrenciBolumID)
                }).OrderByDescending(o => o.Checked);
                Model.Data = dataR;
                return Model;
            }
        }
        public static ChkListModel GetProgramaEslesenOgrenciBolumleri(string EnstituKod, string ProgramKod, List<int> Secilenler = null)
        {
            var Model = new ChkListModel();
            using (var db = new LisansustuBasvuruSistemiEntities())
            {

                if (Secilenler == null && !ProgramKod.IsNullOrWhiteSpace())
                {
                    var data = db.Programlars.Where(p => p.ProgramKod == ProgramKod).FirstOrDefault();
                    Secilenler = data.BolumEslestirs.Select(s => s.OgrenciBolumID).ToList();

                }
                if (Secilenler == null) Secilenler = new List<int>();
                var Bolumlers = db.OgrenciBolumleris.Where(p => p.EnstituKod == EnstituKod).Select(s => new { s.OgrenciBolumID, s.BolumAdi }).OrderBy(o => o.BolumAdi).ToList();
                var dataR = Bolumlers.Select(s => new CheckObject<ChkListDataModel>
                {
                    Value = new ChkListDataModel { ID = s.OgrenciBolumID, Caption = s.BolumAdi },
                    Checked = Secilenler.Contains(s.OgrenciBolumID)
                }).OrderByDescending(o => o.Checked);
                Model.Data = dataR;
                return Model;
            }
        }
        public static kontenjanBilgiModel getOgrenimTipiKotaBilgi(int BasvuruSurecID, int OgrenimTipKod, int KullaniciID, int BasvuruSurecTipID, int? BasvuruID = null, List<int> EklenenOgrenimTipIDs = null)
        {

            //var BasvuruSurecID = Management.getAktifBasvuruSurecID(EnstituKod);
            var mdl = new kontenjanBilgiModel();

            EklenenOgrenimTipIDs = EklenenOgrenimTipIDs ?? new List<int>();
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var Kullanici = db.Kullanicilars.Where(p => p.KullaniciID == KullaniciID).First();
                var BasvuruSureci = db.BasvuruSurecs.Where(p => p.BasvuruSurecID == BasvuruSurecID).First();
                var Enstitu = BasvuruSureci.Enstituler;

                var OgrenimTipKods = new List<int>() { OgrenimTipKod };

                var DigerOgrenimTips = BasvuruSureci.BasvuruSurecOTOrtBasvrs.Where(p => p.OgrenimTipKod == OgrenimTipKod || p.OgrenimTipKod2 == OgrenimTipKod).ToList();
                foreach (var item in DigerOgrenimTips)
                {
                    OgrenimTipKods.Add(item.OgrenimTipKod);
                    OgrenimTipKods.Add(item.OgrenimTipKod2);
                }

                var OgrenimTipKotaList = (from bsk in BasvuruSureci.BasvuruSurecKotalars
                                          join pr in db.Programlars on bsk.ProgramKod equals pr.ProgramKod
                                          join ab in db.AnabilimDallaris.Where(p => p.EnstituKod == BasvuruSureci.EnstituKod) on pr.AnabilimDaliID equals ab.AnabilimDaliID
                                          join bso in BasvuruSureci.BasvuruSurecOgrenimTipleris on bsk.OgrenimTipKod equals bso.OgrenimTipKod
                                          join ot in db.OgrenimTipleris.Where(p => p.EnstituKod == BasvuruSureci.EnstituKod) on bsk.OgrenimTipKod equals ot.OgrenimTipKod
                                          where OgrenimTipKods.Contains(bsk.OgrenimTipKod)
                                          group new { pr.ProgramKod, ab.AnabilimDaliID } by new
                                          {
                                              ab.EnstituKod,
                                              Enstitu.EnstituAd,
                                              ot.OgrenimTipKod,
                                              ot.OgrenimTipAdi,
                                              bso.GrupGoster,
                                              bso.GrupKodu,
                                              ot.GrupAdi,
                                              bso.LEgitimBilgisiIste,
                                              bso.YLEgitimBilgisiIste,
                                              bso.Kota,
                                              BasvuruSureci.ToplamKota,
                                              ot.IsAktif,
                                          } into g1
                                          select new kontenjanBilgiModel
                                          {
                                              BasvuruSurecKontrolTipID = BasvuruSureci.Kota_BasvuruSurecKontrolTipID,
                                              EnstituKod = g1.Key.EnstituKod,
                                              EnstituAdi = g1.Key.EnstituAd,
                                              OgrenimTipKod = g1.Key.OgrenimTipKod,
                                              OgrenimTipAdi = g1.Key.OgrenimTipAdi,
                                              GrupGoster = g1.Key.GrupGoster,
                                              GrupKodu = g1.Key.GrupKodu,
                                              GrupAdi = g1.Key.GrupAdi,
                                              LEgitimBilgisiIste = g1.Key.LEgitimBilgisiIste,
                                              YLEgitimBilgisiIste = g1.Key.YLEgitimBilgisiIste,
                                              Kota = g1.Key.Kota,
                                              ToplamKota = g1.Key.ToplamKota,
                                              IsAktif = g1.Key.IsAktif,
                                              KontenjanBulunanBolumSayisi = g1.Select(s => s.AnabilimDaliID).Distinct().Count(),
                                              KontenjanBulunanProgramSayisi = g1.Count()
                                          }).ToList();




                #region TercihKotaKontrol


                var qBTercihs = (from bt in db.BasvurularTercihleris
                                 join b in db.Basvurulars on bt.BasvuruID equals b.BasvuruID
                                 join bs in db.BasvuruSurecs.Where(p => p.BasvuruSurecTipID == BasvuruSurecTipID) on b.BasvuruSurecID equals bs.BasvuruSurecID
                                 where new List<int> { BasvuruDurumu.Onaylandı, BasvuruDurumu.Gonderildi }.Contains(b.BasvuruDurumID)
                                        && (BasvuruID.HasValue ? b.BasvuruID != BasvuruID.Value : true)
                                        && b.KullaniciID == KullaniciID
                                 select new
                                 {
                                     bs.BasvuruSurecID,
                                     bs.EnstituKod,
                                     bs.BaslangicYil,
                                     bs.BitisYil,
                                     bs.DonemID,
                                     b.KullaniciID,
                                     b.BasvuruID,
                                     bt.OgrenimTipKod
                                 }).AsQueryable();
                if (BasvuruSureci.Kota_BasvuruSurecKontrolTipID == KotaHesapTipleri.YilveDonemToplam)
                {
                    qBTercihs = qBTercihs.Where(p => p.EnstituKod == BasvuruSureci.EnstituKod && p.BaslangicYil == BasvuruSureci.BaslangicYil && p.DonemID == BasvuruSureci.DonemID);
                }
                else
                {
                    qBTercihs = qBTercihs.Where(p => p.BasvuruSurecID == BasvuruSureci.BasvuruSurecID);
                }

                var BasvuruTercihleri = qBTercihs.ToList();

                var ToplamBasvuruAdet = BasvuruTercihleri.Count;

                foreach (var item in OgrenimTipKotaList)
                {
                    var OgrenimTipiToplamBasvuru = BasvuruTercihleri.Where(p => p.OgrenimTipKod == item.OgrenimTipKod).Count();//öğrenim tipine göre kota aşımı
                    var ToplamOgrenimTipiCount = OgrenimTipiToplamBasvuru + EklenenOgrenimTipIDs.Where(p2 => p2 == item.OgrenimTipKod).Count();
                    item.KalanKota = item.Kota - ToplamOgrenimTipiCount;
                    item.ToplamKalanKota = item.ToplamKota - BasvuruTercihleri.Count - EklenenOgrenimTipIDs.Count;
                }


                mdl = OgrenimTipKotaList.Where(p => p.OgrenimTipKod == OgrenimTipKod).FirstOrDefault();
                if (mdl == null)
                {
                    var Ot = db.OgrenimTipleris.Where(p => p.OgrenimTipKod == OgrenimTipKod).First();
                    mdl = new kontenjanBilgiModel { OgrenimTipAdi = Ot.OgrenimTipAdi, GrupAdi = Ot.GrupAdi, OgrenimTipKod = OgrenimTipKod };
                }
                mdl.OBOgrenimTipleri = OgrenimTipKotaList.Where(p => p.OgrenimTipKod != OgrenimTipKod).ToList();

                var fOtEklenemeyecekler = BasvuruTercihleri.Where(p => OgrenimTipKods.Contains(p.OgrenimTipKod) == false).Select(s => s.OgrenimTipKod).ToList(); // farklı öğrenim tipi eklenemeyeceklerin listesi
                var fOtEklenemeyeceklerAds = db.OgrenimTipleris.Where(p => p.EnstituKod == BasvuruSureci.EnstituKod && fOtEklenemeyecekler.Contains(p.OgrenimTipKod)).Select(s => s.OgrenimTipAdi).ToList();// farklı öğrenim tipi eklenemeyeceklerin adı

                if (fOtEklenemeyecekler.Count > 0)
                {
                    mdl.FarkliOgrenimTipiEklenemez = true;
                    mdl.FarkliOgrenimTipEklenemezAds = string.Join(", ", fOtEklenemeyeceklerAds);
                }
                else if (BasvuruSureci.FarkliOgrenimTipleriAyniBasvurudaAlinabilsin == false && (EklenenOgrenimTipIDs.Any(a => a != OgrenimTipKod)))
                {
                    mdl.FarkliOgrenimTipiEklenemezAyniBasvuruda = true;
                }


                #endregion




            }
            return mdl;
        }
        public static kontenjanProgramBilgiModel getKontenjanProgramBilgi(string ProgramKod, int OgrenimTipKod, int BasvuruSurecID, int KullaniciTipID, int? LOgrenimDurumID = null, int? LUniversiteID = null)
        {
            var mdl = new kontenjanProgramBilgiModel();
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                bool Yerli = true;
                if (KullaniciTipID > 0) Yerli = db.KullaniciTipleris.Where(p => p.KullaniciTipID == KullaniciTipID).First().Yerli;
                var BasvuruSureci = db.BasvuruSurecs.Where(p => p.BasvuruSurecID == BasvuruSurecID).First();
                var q = from p in db.Programlars
                        join prl in db.Programlars on p.ProgramKod equals prl.ProgramKod
                        join at in db.AlesTipleris on new { p.AlesTipID } equals new { at.AlesTipID }
                        join atL in db.AlesTipleris on new { at.AlesTipID } equals new { atL.AlesTipID }
                        join b in db.AnabilimDallaris on p.AnabilimDaliKod equals b.AnabilimDaliKod
                        join k in db.BasvuruSurecKotalars.Where(p => p.BasvuruSurecID == BasvuruSurecID) on p.ProgramKod equals k.ProgramKod
                        join ot in db.OgrenimTipleris on new { k.BasvuruSurec.EnstituKod, k.OgrenimTipKod } equals new { ot.EnstituKod, ot.OgrenimTipKod }
                        join ota in db.OgrenimTipleris on ot.OgrenimTipID equals ota.OgrenimTipID
                        where p.ProgramKod == ProgramKod && k.OgrenimTipKod == OgrenimTipKod && p.KullaniciTipleri.Yerli == Yerli
                        select new kontenjanProgramBilgiModel
                        {
                            ProgramlarAlesEslesmeleris = p.ProgramlarAlesEslesmeleris,
                            OgrenimTipKod = k.OgrenimTipKod,
                            OgrenimTipAdi = ota.OgrenimTipAdi,
                            ProgramKod = prl.ProgramKod,
                            ProgramAdi = prl.ProgramAdi,

                            AnabilimDaliKod = b.AnabilimDaliKod,
                            AnabilimDaliAdi = b.AnabilimDaliAdi,
                            AlesTipID = atL.AlesTipID,
                            AlesTipAdi = atL.AlesTipAdi,
                            Ingilizce = p.Ingilizce,
                            OrtakKota = k.OrtakKota,
                            OrtakKotaSayisi = k.OrtakKotaSayisi,
                            AlanIciKota = k.AlanIciKota,
                            AlanDisiKota = k.AlanDisiKota,
                            MinAles = k.MinAles,
                            MinAgno = k.MinAGNO,
                            YLAgnoKriteri = p.YLAgnoKriteri,
                            TYLAgnoKriteri = p.TYLAgnoKriteri,
                            DAgnoKriteri = p.DAgnoKriteri,
                            ProgramSecimiEkBilgi = p.ProgramSecimiEkBilgi,
                            Aciklama = p.Aciklama,
                            Ucret = p.Ucret,
                            Ucretli = p.Ucretli,
                            AlesNotuYuksekOlanAlinsin = p.AlesNotuYuksekOlanAlinsin,
                            BasvuruAgnoAlimTipID = p.BasvuruAgnoAlimTipID,
                            LYuzdeOran = p.LYuzdeOran,
                            YLYuzdeOran = p.YLYuzdeOran,
                            LYLHerhangiBirindeGecenAlanIci = p.LYLHerhangiBirindeGecenAlanIci,
                            AnabilimDallari = p.AnabilimDallari,

                        };
                mdl = q.FirstOrDefault();


                if (mdl.MinAgno.HasValue)
                {
                    if (new List<int> { OgrenimTipi.Doktra, OgrenimTipi.ButunlesikDoktora }.Contains(OgrenimTipKod))
                    {
                        if (mdl.BasvuruAgnoAlimTipID.HasValue)
                        {

                            mdl.MinAgno = mdl.DAgnoKriteri;
                            if (mdl.BasvuruAgnoAlimTipID.Value == BasvuruAgnoAlimTipi.LisansAlinsin)
                            {
                                var Msg = mdl.ProgramAdi + " programına başvuru için Lisans Eğitimi mezuniyet notunuz en az " + mdl.MinAgno.Value.ToString("n2") + " olmalıdır.";
                                if (LUniversiteID == Management.UniversiteYtuKod && OgrenimTipKod == OgrenimTipi.TezliYuksekLisans && LOgrenimDurumID == OgrenimDurum.HalenOğrenci)
                                    Msg += "AGNO güncelleme sonrası alt sınır sağlanmadığı takdirde başvurunuz geçersiz sayılacaktır.";
                                mdl.MinAgnoAciklama = Msg;
                            }
                            else if (mdl.BasvuruAgnoAlimTipID.Value == BasvuruAgnoAlimTipi.YLisansAlinsin)
                            {
                                var Msg = mdl.ProgramAdi + " programına başvuru için Yüksek Lisans Eğitimi mezuniyet notunuz en az " + mdl.MinAgno.Value.ToString("n2") + " olmalıdır.";
                                if (LUniversiteID == Management.UniversiteYtuKod && OgrenimTipKod == OgrenimTipi.TezliYuksekLisans && LOgrenimDurumID == OgrenimDurum.HalenOğrenci)
                                    Msg += "AGNO güncelleme sonrası alt sınır sağlanmadığı takdirde başvurunuz geçersiz sayılacaktır.";
                                mdl.MinAgnoAciklama = Msg;
                            }
                            else if (mdl.BasvuruAgnoAlimTipID.Value == BasvuruAgnoAlimTipi.L_YLYuzdeBelirlensin)
                            {
                                mdl.MinAgnoAciklama = mdl.ProgramAdi + " programına başvuru için % " + mdl.LYuzdeOran.Value.ToString("n2") + " Lisans mezuniyet notu ile % " + mdl.YLYuzdeOran.Value.ToString("n2") + " Yüksek Lisans mezuniyet notunun toplamı en az " + mdl.MinAgno.Value.ToString("n2") + " olmalıdır.";
                            }
                        }
                        else
                        {
                            var Msg = mdl.ProgramAdi + " programına başvuru için Yüksek Lisans Eğitimi mezuniyet notunuz en az " + mdl.MinAgno.Value.ToString("n2") + " olmalıdır.";
                            mdl.MinAgnoAciklama = Msg;
                        }
                    }
                    else
                    {
                        var Msg = mdl.ProgramAdi + " programına başvuru için Lisans Eğitimi mezuniyet notunuz en az " + mdl.MinAgno.Value.ToString("n2") + " olmalıdır.";
                        mdl.MinAgnoAciklama = Msg;
                    }
                }


            }
            return mdl;

        }

        public static DateTime NowDate { get { return DateTime.Now; } }

        public static DateTime EkSureBasTar { get { return new DateTime(2021, 2, 1, 13, 00, 0); } }
        public static DateTime EkSureBitTar { get { return new DateTime(2021, 2, 5, 16, 0, 0); } }
        public static List<CmbStringDto> cmbGetAktifOgrenimTipleriGrup(int BasvuruSurecID, bool bosSecimVar = false)
        {
            var dct = new List<CmbStringDto>();
            if (bosSecimVar) dct.Add(new CmbStringDto { Value = "", Caption = "" });

            using (var db = new LisansustuBasvuruSistemiEntities())
            {

                int? OgrenimnTipKod = null;
                if (new List<int> { 1066, 1065 }.Contains(BasvuruSurecID) && NowDate > EkSureBasTar && NowDate <= EkSureBitTar)
                {
                    OgrenimnTipKod = OgrenimTipi.TezsizYuksekLisans;
                }


                var otS = from bs in db.BasvuruSurecs.Where(p => p.BasvuruSurecID == BasvuruSurecID)
                          join s in db.BasvuruSurecOgrenimTipleris.Where(p => p.IsAktif) on bs.BasvuruSurecID equals s.BasvuruSurecID
                          join ot in db.OgrenimTipleris on new { bs.EnstituKod, s.OgrenimTipKod } equals new { ot.EnstituKod, ot.OgrenimTipKod }
                          where ot.OgrenimTipKod == (OgrenimnTipKod ?? ot.OgrenimTipKod)
                          select new
                          {
                              Kod = ot.GrupGoster ? ot.GrupKodu : (ot.OgrenimTipKod + ""),
                              Ad = ot.GrupGoster ? ot.GrupAdi : ot.OgrenimTipAdi
                          };
                var q2 = from o in otS
                         group new { o.Kod, o.Ad } by new { o.Kod, o.Ad } into g1
                         select new
                         {

                             g1.Key.Kod,
                             g1.Key.Ad,

                         };
                var qdata = q2.ToList();
                foreach (var item in qdata)
                {
                    dct.Add(new CmbStringDto { Value = item.Kod, Caption = item.Ad });
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
        public static List<CmbIntDto> cmbGetAktifSubOgrenimTipleri(int BasvuruSurecID, string Kod, bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                int? OgrenimnTipKod = null;
                if (new List<int> { 1066, 1065 }.Contains(BasvuruSurecID) && NowDate > EkSureBasTar && NowDate <= EkSureBitTar)
                {
                    OgrenimnTipKod = OgrenimTipi.TezsizYuksekLisans;
                }

                var ots = (from bs in db.BasvuruSurecs.Where(p => p.BasvuruSurecID == BasvuruSurecID)
                           join s in db.BasvuruSurecOgrenimTipleris.Where(p => p.IsAktif) on bs.BasvuruSurecID equals s.BasvuruSurecID
                           where s.IsAktif && s.GrupGoster && s.GrupKodu == Kod && s.OgrenimTipKod == (OgrenimnTipKod ?? s.OgrenimTipKod)
                           select s.OgrenimTipKod).ToList();
                bool IsSubOT = ots.Count > 0;
                if (IsSubOT)
                {
                    if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
                    var bsurec = db.BasvuruSurecs.Where(p => p.BasvuruSurecID == BasvuruSurecID).First();
                    var data = db.OgrenimTipleris.Where(p => p.EnstituKod == bsurec.EnstituKod && ots.Contains(p.OgrenimTipKod)).ToList();
                    foreach (var item in data)
                    {
                        dct.Add(new CmbIntDto
                        {
                            Value = item.OgrenimTipKod,
                            Caption = item.OgrenimTipAdi
                        });
                    }
                }
            }
            return dct;

        }
        public static List<CmbIntDto> cmbGetTumOgrenimTipleri(string EnstituKod, bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.OgrenimTipleris.Where(p => p.EnstituKod == EnstituKod).OrderBy(o => o.OgrenimTipAdi).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.OgrenimTipKod, Caption = item.OgrenimTipAdi });
                }
            }
            return dct;

        }
        public static List<CmbIntDto> cmbAktifOgrenimTipleri(bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.OgrenimTipleris.Where(p => p.IsAktif).OrderBy(o => o.OgrenimTipAdi).Select(s => new { s.OgrenimTipKod, s.OgrenimTipAdi }).Distinct().ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.OgrenimTipKod, Caption = item.OgrenimTipAdi });
                }
            }
            return dct;

        }
        public static List<CmbIntDto> cmbAktifOgrenimTipleri(string EnstituKod, bool bosSecimVar = false, bool? Aktif = true, int? HaricOgreniTipKod = null)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.OgrenimTipleris.Where(p => p.EnstituKod == EnstituKod && (Aktif.HasValue ? p.IsAktif == Aktif.Value : true) && (HaricOgreniTipKod.HasValue ? p.OgrenimTipKod != HaricOgreniTipKod.Value : true)).OrderBy(o => o.OgrenimTipAdi).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.OgrenimTipKod, Caption = item.OgrenimTipAdi });
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
        public static List<CmbIntDto> cmbGetAktifAlesTipleri(bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.AlesTipleris.OrderBy(o => o.AlesTipAdi).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.AlesTipID, Caption = item.AlesTipAdi });
                }
            }
            return dct;

        }
        public static List<CmbIntDto> cmbGetMesajKategorileri(string EnstituKod = "", bool bosSecimVar = false, bool? IsAktif = null)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var qdata = db.MesajKategorileris.AsQueryable();
                if (IsAktif.HasValue) qdata = qdata.Where(p => p.IsAktif == IsAktif.Value);
                if (EnstituKod.IsNullOrWhiteSpace() == false) qdata = qdata.Where(p => p.EnstituKod == EnstituKod);
                var data = qdata.OrderBy(o => o.KategoriAdi).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.MesajKategoriID, Caption = item.Enstituler.EnstituKisaAd + " / " + item.KategoriAdi });
                }
            }
            return dct;

        }
        public static List<CmbIntDto> cmbGetMesajYillari(string EnstituKod = "", bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var qdata = db.Mesajlars.AsQueryable();
                if (EnstituKod.IsNullOrWhiteSpace() == false) qdata = qdata.Where(p => p.MesajKategorileri.EnstituKod == EnstituKod);
                var data = qdata.Select(s => s.Tarih.Year).Distinct().OrderByDescending(o => o).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item, Caption = item.ToString() + " Yılı" });
                }
            }
            return dct;

        }
        public static List<CmbIntDto> cmbGetBasvuruAgnoAlimTipleri(bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var qdata = db.BasvuruAgnoAlimTipleris;
                var data = qdata.OrderBy(o => o.BasvuruAgnoAlimTipID).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.BasvuruAgnoAlimTipID, Caption = item.AgnoAlimTipAdi });
                }
            }
            return dct;

        }


        public static List<CmbIntDto> cmbMzSinavDurumListe(bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.MezuniyetSinavDurumlaris.ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.MezuniyetSinavDurumID, Caption = item.MezuniyetSinavDurumAdi });
                }
            }
            return dct;
        }
        public static List<CmbStringDto> cmbTIAktifDonemListe(bool bosSecimVar = false)
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

        public static List<CmbIntDto> cmbTIAraRaporDurumListe(bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();

            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var ArDurums = db.TIBasvuruAraRaporDurumlaris.Select(s => new CmbIntDto { Value = s.TIBasvuruAraRaporDurumID, Caption = s.TIBasvuruAraRaporDurumAdi }).ToList();
                dct.AddRange(ArDurums);
            }
            return dct;
        }
        public static List<CmbIntDto> CmbTDODanismanTalepTip(bool isDegisiklikTalebi, bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = (from s in db.TDODanismanTalepTipleris.Where(p => p.TDODanismanTalepTipID == (isDegisiklikTalebi ? p.TDODanismanTalepTipID : 1))
                            select new
                            {
                                s.TDODanismanTalepTipID,
                                s.TalepTipAdi
                            }).AsQueryable();
                var qdata = data.ToList();
                foreach (var item in qdata)
                {
                    dct.Add(new CmbIntDto { Value = item.TDODanismanTalepTipID, Caption = item.TalepTipAdi });
                }
            }
            return dct;

        }
        public static List<CmbIntDto> cmbTDOOneriDurumListe(bool bosSecimVar = false, bool IsEsDanisman = false)
        {
            var dct = new List<CmbIntDto>();

            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });

            dct.Add(new CmbIntDto { Value = 1, Caption = "Danışman Onayı Bekleniyor" });
            dct.Add(new CmbIntDto { Value = 2, Caption = "Danışman Tarafından Onaylandı" });
            dct.Add(new CmbIntDto { Value = 3, Caption = "Danışman Tarafından Onaylanmadı" });
            dct.Add(new CmbIntDto { Value = 4, Caption = "EYK'ya Gönderimi Bekleniyor" });
            dct.Add(new CmbIntDto { Value = 5, Caption = "EYK'ya Gönderimi Onaylandı" });
            dct.Add(new CmbIntDto { Value = 6, Caption = "EYK'ya Gönderimi Onaylanmadı" });
            dct.Add(new CmbIntDto { Value = 7, Caption = "EYK'da Onay Bekliyor" });
            dct.Add(new CmbIntDto { Value = 8, Caption = "EYK'Da Onaylandı" });
            dct.Add(new CmbIntDto { Value = 9, Caption = "EYK'Da Onaylanmadı" });
            return dct;
        }
        public static List<CmbIntDto> cmbTDOEsOneriDurumListe(bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();

            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });

            dct.Add(new CmbIntDto { Value = 4, Caption = "EYK'ya Gönderimi Bekleniyor" });
            dct.Add(new CmbIntDto { Value = 5, Caption = "EYK'ya Gönderimi Onaylandı" });
            dct.Add(new CmbIntDto { Value = 6, Caption = "EYK'ya Gönderimi Onaylanmadı" });
            dct.Add(new CmbIntDto { Value = 7, Caption = "EYK'da Onay Bekliyor" });
            dct.Add(new CmbIntDto { Value = 8, Caption = "EYK'Da Onaylandı" });
            dct.Add(new CmbIntDto { Value = 9, Caption = "EYK'Da Onaylanmadı" });
            return dct;
        }
        public static List<CmbBoolDto> cmbTeslimFormDurumu(bool bosSecimVar = false)
        {
            var dct = new List<CmbBoolDto>();
            if (bosSecimVar) dct.Add(new CmbBoolDto { Value = null, Caption = "" });
            dct.Add(new CmbBoolDto { Value = true, Caption = "Teslim Formu Oluşturuldu" });
            dct.Add(new CmbBoolDto { Value = false, Caption = "Teslim Formu Oluşturulmadı" });

            return dct;
        }

        public static List<CmbBoolDto> cmbMezuniyetDurumListe(bool bosSecimVar = false)
        {
            var dct = new List<CmbBoolDto>();
            if (bosSecimVar) dct.Add(new CmbBoolDto { Value = null, Caption = "" });
            dct.Add(new CmbBoolDto { Value = null, Caption = "Sonuç Girilmedi" });
            dct.Add(new CmbBoolDto { Value = true, Caption = "Mezun Oldu" });
            dct.Add(new CmbBoolDto { Value = false, Caption = "Mezun Olamadı" });

            return dct;
        }
        public static List<CmbIntDto> cmbMezuniyetDurumIDListe(bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = -1, Caption = "" });
            dct.Add(new CmbIntDto { Value = null, Caption = "Sonuç Girilmedi" });
            dct.Add(new CmbIntDto { Value = 1, Caption = "Mezun Oldu" });
            dct.Add(new CmbIntDto { Value = 0, Caption = "Mezun Olamadı" });
            return dct;
        }
        public static PagerIndexDto setStartRowInx(int srIndex, int pgIndex, int pgCount, int rwCount, int pgSize)
        {
            int setStartRowInx = 0;
            if (rwCount <= srIndex) setStartRowInx = rwCount / pgSize;
            else setStartRowInx = srIndex;
            int setPageInx = pgIndex;
            if ((decimal)rwCount / (decimal)pgSize == 0 || pgCount < pgIndex) setPageInx = 1;

            return new PagerIndexDto { StartRowIndex = setStartRowInx, PageIndex = setPageInx };
        }
        public static List<CmbIntDto> cmbGetHaftaGunleri(bool bosSecimVar = false, bool? IsHaftaSonu = null)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var qdata = db.HaftaGunleris.AsQueryable();
                if (IsHaftaSonu.HasValue) qdata = qdata.Where(p => p.IsHaftaSonu == IsHaftaSonu);
                var data = qdata.OrderByDescending(o => o.HaftaGunID > 0).ThenBy(o => o.HaftaGunID).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.HaftaGunID, Caption = item.HaftaGunAdi });
                }
            }
            return dct;
        }
        #endregion


        #region Data


        public static string GetTercihRowHtml(TercihRowModel model)
        {
            return RenderPartialView("Basvuru", "CreateTercihRowHtml", model);
        }
        public static kotaKontrolModel AlanKontrol(int BasvuruSurecID, int LOgrenciBolumID, int? YLOgrenciBolumID, int OgrenimTipKod, string tprog, int KullaniciID, int BasvuruID = 0)
        {
            var Model = new kotaKontrolModel();
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var BasvuruSurec = db.BasvuruSurecs.Where(p => p.BasvuruSurecID == BasvuruSurecID).First();
                var Program = db.Programlars.Where(p => p.ProgramKod == tprog).First();
                var OhrenciBolumIDs = Program.BolumEslestirs.Select(s => s.OgrenciBolumID).ToList();
                var AlanTips = db.AlanTipleris.ToList();
                Model.AlanTipID = AlanTipi.AlanDisi;
                Model.AlanIci = false;
                var BasvuruSurecKota = BasvuruSurec.BasvuruSurecKotalars.Where(p => p.ProgramKod == tprog && p.OgrenimTipKod == OgrenimTipKod).FirstOrDefault();

                if (YLOgrenciBolumID.HasValue)
                {
                    if (Program.LYLHerhangiBirindeGecenAlanIci)
                    {
                        if (OhrenciBolumIDs.Contains(LOgrenciBolumID) || OhrenciBolumIDs.Contains(YLOgrenciBolumID.Value))
                        {
                            Model.AlanTipID = AlanTipi.AlanIci;
                            Model.AlanIci = true;
                        }
                    }
                    else
                    {
                        if (OhrenciBolumIDs.Contains(LOgrenciBolumID) && OhrenciBolumIDs.Contains(YLOgrenciBolumID.Value))
                        {
                            Model.AlanTipID = AlanTipi.AlanIci;
                            Model.AlanIci = true;
                        }

                    }
                }
                else
                {
                    if (OhrenciBolumIDs.Contains(LOgrenciBolumID))
                    {
                        Model.AlanTipID = AlanTipi.AlanIci;
                        Model.AlanIci = true;
                    }
                }


                if (BasvuruSurecKota != null)
                {
                    if (BasvuruSurecKota.OrtakKota)
                    {
                        Model.AlanTipID = AlanTipi.AlanDisi;
                        Model.AlanIci = false;
                    }
                    Model.Kota = BasvuruSurecKota.OrtakKota ? (BasvuruSurecKota.OrtakKotaSayisi.Value) : (Model.AlanTipID == AlanTipi.AlanIci ? BasvuruSurecKota.AlanIciKota : BasvuruSurecKota.AlanDisiKota);

                }
                else Model.Kota = 0;
                if (BasvuruSurec.Kota_BasvuruSurecKontrolTipID == KotaHesapTipleri.SeciliBasvuruSureci)
                {
                    Model.AyniProgramBasvurusu = BasvuruSurec.Basvurulars.Any(p => p.KullaniciID == KullaniciID && p.BasvurularTercihleris.Any(a => a.OgrenimTipKod == OgrenimTipKod && a.ProgramKod == tprog && a.BasvuruID != BasvuruID));
                }
                else
                {
                    Model.AyniProgramBasvurusu = db.Basvurulars.Any(p => p.KullaniciID == KullaniciID && (p.BasvuruSurec.BaslangicYil == BasvuruSurec.BaslangicYil && p.BasvuruSurec.BitisYil == BasvuruSurec.BitisYil && p.BasvuruSurec.DonemID == BasvuruSurec.DonemID) && p.BasvurularTercihleris.Any(a => a.OgrenimTipKod == OgrenimTipKod && a.ProgramKod == tprog && a.BasvuruID != BasvuruID));
                }

                var AlesTipleri = db.AlesTipleris.Where(p => p.AlesTipID == Program.AlesTipID).First();
                Model.AlesTipID = AlesTipleri.AlesTipID;
                Model.AlesTipAdi = AlesTipleri.AlesTipAdi;
                Model.AlesTipKisaAdi = string.Join("", AlesTipleri.AlesTipAdi.Split(' ').ToList().Select(s => s.Substring(0, 1).ToUpper()));

                Model.AlanTipAdi = AlanTips.Where(p => p.AlanTipID == Model.AlanTipID).First().AlanTipAdi;
                Model.AlanTipKisaAdi = string.Join("", Model.AlanTipAdi.Split(' ').ToList().Select(s => s.Substring(0, 1).ToUpper()));



                if (!Model.AlanIci && BasvuruSurecKota.IsAlandisiBolumKisitlamasi && Model.Kota > 0 && !BasvuruSurecKota.OrtakKota)
                {

                    var BasvurabilecekOgrenciBolumleri = BasvuruSurec.BasvuruSurecProgramlarAlandisiBolumKisitlamalaris.Where(p => p.ProgramKod == Program.ProgramKod).ToList();

                    if (YLOgrenciBolumID.HasValue)
                    {
                        if (Program.LYLHerhangiBirindeGecenAlanIci)
                        {

                            if ((BasvurabilecekOgrenciBolumleri.Any(a => a.OgrenciBolumID == LOgrenciBolumID) || BasvurabilecekOgrenciBolumleri.Any(a => a.OgrenciBolumID == YLOgrenciBolumID.Value)) == false)
                            {

                                Model.AlanDisiProgramKisitlamasiMsg = "Başvurmak istediğiniz '" + Program.ProgramAdi + "' programı eğitim bilgilerinize göre alan dışı tercih olarak değerlendirilimiştir. Bu programa alan dışı başvuru kabulunde belirlenen bölümler arasında eğitim bilgilerinizdeki lisans veya yüksek lisantan mezun olunan bölüm bilgileriniz uyuşmamaktadır.";
                                Model.AlertInputNames = new List<string> { "LOgrenciBolumID", "YLOgrenciBolumID" };
                            }
                        }
                        else
                        {
                            if (!BasvurabilecekOgrenciBolumleri.Any(a => a.OgrenciBolumID == LOgrenciBolumID) && !BasvurabilecekOgrenciBolumleri.Any(a => a.OgrenciBolumID == YLOgrenciBolumID.Value))
                            {

                                Model.AlanDisiProgramKisitlamasiMsg = "Başvurmak istediğiniz '" + Program.ProgramAdi + "' programı eğitim bilgilerinize göre  alan dışı tercih olarak değerlendirilimiştir. Bu programa alan dışı başvuru kabulunde belirlenen bölümler arasında eğitim bilgilerinizdeki lisans ve yüksek lisanstan mezun olunan bölüm bilgileriniz uyuşmamaktadır.";
                                Model.AlertInputNames = new List<string> { "LOgrenciBolumID", "YLOgrenciBolumID" };
                            }
                            else if (!BasvurabilecekOgrenciBolumleri.Any(a => a.OgrenciBolumID == LOgrenciBolumID))
                            {

                                Model.AlanDisiProgramKisitlamasiMsg = "Başvurmak istediğiniz '" + Program.ProgramAdi + "' programı eğitim bilgilerinize göre alan dışı tercih olarak değerlendirilimiştir. Bu programa alan dışı başvuru kabulunde belirlenen bölümler arasında eğitim bilgilerinizdeki lisanstan mezun olunan bölüm bilginiz uyuşmamaktadır."; ;
                                Model.AlertInputNames = new List<string> { "LOgrenciBolumID" };
                            }
                            else if (!BasvurabilecekOgrenciBolumleri.Any(a => a.OgrenciBolumID == YLOgrenciBolumID))
                            {

                                Model.AlanDisiProgramKisitlamasiMsg = "Başvurmak istediğiniz '" + Program.ProgramAdi + "' programı eğitim bilgilerinize göre alan dışı tercih olarak değerlendirilimiştir. Bu programa alan dışı başvuru kabulunde belirlenen bölümler arasında eğitim bilgilerinizdeki yüksek lisanstan mezun olunan bölüm bilginiz uyuşmamaktadır.";
                                Model.AlertInputNames = new List<string> { "YLOgrenciBolumID" };
                            }
                        }
                    }
                    else if (!BasvurabilecekOgrenciBolumleri.Any(a => a.OgrenciBolumID == LOgrenciBolumID))
                    {

                        Model.AlanDisiProgramKisitlamasiMsg = "Başvurmak istediğiniz '" + Program.ProgramAdi + "' programı eğitim bilgilerinize göre alan dışı tercih olarak değerlendirilimiştir. Bu programa alan dışı başvuru kabulunde belirlenen bölümler arasında eğitim bilgilerinizdeki lisanstan mezun olunan bölüm bilginiz uyuşmamaktadır.";
                        Model.AlertInputNames = new List<string> { "LOgrenciBolumID" };
                    }
                    Model.AlanDisiProgramKisitlamasiVar = !Model.AlanDisiProgramKisitlamasiMsg.IsNullOrWhiteSpace();

                }
            }

            return Model;

        }
        public static kotaKontrolModel AlanKontrolYG(int BasvuruSurecID, List<int> OgrenciBolumID, int OgrenimTipKod, string tprog, int KullaniciID, int BasvuruID = 0)
        {
            var Model = new kotaKontrolModel();
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var BasvuruSurec = db.BasvuruSurecs.Where(p => p.BasvuruSurecID == BasvuruSurecID).First();
                var Program = db.Programlars.Where(p => p.ProgramKod == tprog).First();
                var OhrenciBolumIDs = Program.BolumEslestirs.Select(s => s.OgrenciBolumID).ToList();
                var AlanTips = db.AlanTipleris.ToList();
                Model.AlanTipID = AlanTipi.AlanDisi;
                Model.AlanIci = false;
                var BasvuruSurecKota = BasvuruSurec.BasvuruSurecKotalars.Where(p => p.ProgramKod == tprog && p.OgrenimTipKod == OgrenimTipKod).FirstOrDefault();


                if (OhrenciBolumIDs.Any(a => OgrenciBolumID.Contains(a)))
                {
                    Model.AlanTipID = AlanTipi.AlanIci;
                    Model.AlanIci = true;
                }
                if (BasvuruSurecKota != null)
                {
                    if (BasvuruSurecKota.OrtakKota)
                    {
                        Model.AlanTipID = AlanTipi.AlanDisi;
                        Model.AlanIci = false;
                    }
                    Model.Kota = BasvuruSurecKota.OrtakKota ? (BasvuruSurecKota.OrtakKotaSayisi.Value) : (Model.AlanTipID == AlanTipi.AlanIci ? BasvuruSurecKota.AlanIciKota : BasvuruSurecKota.AlanDisiKota);

                }
                else Model.Kota = 0;
                if (BasvuruSurec.Kota_BasvuruSurecKontrolTipID == KotaHesapTipleri.SeciliBasvuruSureci)
                {
                    Model.AyniProgramBasvurusu = BasvuruSurec.Basvurulars.Any(p => p.KullaniciID == KullaniciID && p.BasvurularTercihleris.Any(a => a.OgrenimTipKod == OgrenimTipKod && a.ProgramKod == tprog && a.BasvuruID != BasvuruID));
                }
                else
                {
                    Model.AyniProgramBasvurusu = db.Basvurulars.Any(p => p.KullaniciID == KullaniciID && (p.BasvuruSurec.BaslangicYil == BasvuruSurec.BaslangicYil && p.BasvuruSurec.BitisYil == BasvuruSurec.BitisYil && p.BasvuruSurec.DonemID == BasvuruSurec.DonemID) && p.BasvurularTercihleris.Any(a => a.OgrenimTipKod == OgrenimTipKod && a.ProgramKod == tprog && a.BasvuruID != BasvuruID));
                }

                var AlesTipleri = db.AlesTipleris.Where(p => p.AlesTipID == Program.AlesTipID).First();
                Model.AlesTipID = AlesTipleri.AlesTipID;
                Model.AlesTipAdi = AlesTipleri.AlesTipAdi;
                Model.AlesTipKisaAdi = string.Join("", AlesTipleri.AlesTipAdi.Split(' ').ToList().Select(s => s.Substring(0, 1).ToUpper()));

                Model.AlanTipAdi = AlanTips.Where(p => p.AlanTipID == Model.AlanTipID).First().AlanTipAdi;
                Model.AlanTipKisaAdi = string.Join("", Model.AlanTipAdi.Split(' ').ToList().Select(s => s.Substring(0, 1).ToUpper()));



                if (!Model.AlanIci && BasvuruSurecKota.IsAlandisiBolumKisitlamasi && Model.Kota > 0 && !BasvuruSurecKota.OrtakKota)
                {

                    var BasvurabilecekOgrenciBolumleri = BasvuruSurec.BasvuruSurecProgramlarAlandisiBolumKisitlamalaris.Where(p => p.ProgramKod == Program.ProgramKod).ToList();


                    if (OgrenciBolumID.Count > 1)
                    {
                        if (BasvurabilecekOgrenciBolumleri.Any(a => OgrenciBolumID.Contains(a.OgrenciBolumID)) == false)
                        {

                            Model.AlanDisiProgramKisitlamasiMsg = "Başvurmak istediğiniz '" + Program.ProgramAdi + "' programı eğitim bilgilerinize göre alan dışı tercih olarak değerlendirilimiştir. Bu programa alan dışı başvuru kabulunde belirlenen bölümler arasında eğitim bilgilerinizdeki lisans, yüksek lisantan, doktora bölüm bilgileriniz uyuşmamaktadır.";
                            Model.AlertInputNames = new List<string> { "LOgrenciBolumID", "YLOgrenciBolumID", "DROgrenciBolumID" };
                        }
                    }
                    else
                    {

                        Model.AlanDisiProgramKisitlamasiMsg = "Başvurmak istediğiniz '" + Program.ProgramAdi + "' programı eğitim bilgilerinize göre alan dışı tercih olarak değerlendirilimiştir. Bu programa alan dışı başvuru kabulunde belirlenen bölümler arasında eğitim bilgilerinizdeki Lisanstan veya Yüksek Lisanstan mezun olunan bölüm bilginiz uyuşmamaktadır.";
                        Model.AlertInputNames = new List<string> { "LOgrenciBolumID" };
                    }
                    Model.AlanDisiProgramKisitlamasiVar = !Model.AlanDisiProgramKisitlamasiMsg.IsNullOrWhiteSpace();

                }

                return Model;
            }
        }
        public static StudentControl KullaniciKayitBilgisiGuncelle(int KullaniciID)
        {
            var kayitBilgi = new StudentControl();
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var kulls = db.Kullanicilars.Where(p => p.KullaniciID == KullaniciID).First();
                if (kulls.YtuOgrencisi)
                {
                    kayitBilgi = Management.StudentControl(kulls.TcKimlikNo);
                    if (kayitBilgi.KayitVar && kayitBilgi.OgrenciInfo.OGRENIMSEVIYE_ID.toIntObj() == kulls.OgrenimTipKod)
                    {
                        kulls.KayitDonemID = kayitBilgi.DonemID;
                        kulls.KayitYilBaslangic = kayitBilgi.BaslangicYil;
                        kulls.KayitTarihi = kayitBilgi.KayitTarihi;
                        if (kayitBilgi.OgrenciInfo != null)
                        {
                            int? danismanID = null;
                            if (!kayitBilgi.OgrenciInfo.DANISMAN_TC1.IsNullOrWhiteSpace())
                            {
                                var danisman = db.Kullanicilars.Where(p => p.TcKimlikNo == kayitBilgi.OgrenciInfo.DANISMAN_TC1).FirstOrDefault();
                                if (danisman != null)
                                    danismanID = danisman.KullaniciID;
                            }

                            kulls.DanismanID = danismanID;
                        }

                    }
                    else
                    {
                        kulls.YtuOgrencisi = false;
                        kulls.OgrenimTipKod = null;
                        kulls.ProgramKod = null;
                        kulls.OgrenciNo = null;
                        kulls.KayitDonemID = null;
                        kulls.KayitYilBaslangic = null;
                        kulls.KayitTarihi = null;
                    }
                    db.SaveChanges();
                }
                return kayitBilgi;
            }
        }


        public static SinavBilgiModel getSinavBilgisi(int BasvuruSurecID, int SinavTipID, List<int> OgrenimTipKods, List<string> ProgramKods, List<bool> Ingilizces)
        {
            var mdl = new SinavBilgiModel();
            using (var db = new LisansustuBasvuruSistemiEntities())
            {




                mdl = (from s in db.BasvuruSurecSinavTipleris.Where(p => p.BasvuruSurecID == BasvuruSurecID && p.SinavTipID == SinavTipID)
                       join sd in db.SinavTipleris on new { s.SinavTipID } equals new { sd.SinavTipID }
                       select new SinavBilgiModel
                       {
                           SinavAdi = sd.SinavAdi,
                           BasvuruSurecSinavTipID = s.BasvuruSurecSinavTipID,
                           BasvuruSurecID = s.BasvuruSurecID,

                           SinavTipID = s.SinavTipID,
                           EnstituKod = s.EnstituKod,
                           //LocalService = s.LocalService,
                           SinavTipGrupID = s.SinavTipGrupID,
                           SinavTipKod = s.SinavTipKod,
                           TarihGirisMaxGecmisYil = s.TarihGirisMaxGecmisYil,
                           WebService = s.WebService,
                           WebServiceKod = s.WebServiceKod,
                           WsSinavCekimTipID = s.WsSinavCekimTipID,
                           OzelTarih = s.OzelTarih,
                           OzelNot = s.OzelNot,
                           OzelNotTipID = s.OzelNotTipID,
                           OzelTarihTipID = s.OzelTarihTipID,
                           NotDonusum = s.NotDonusum,
                           NotDonusumFormulu = s.NotDonusumFormulu,
                           Tarih1 = s.Tarih1,
                           Tarih2 = s.Tarih2,
                           KusuratVar = s.KusuratVar,
                           Min = s.Min,
                           Max = s.Max,
                           GIsTaahhutVar = s.GIsTaahhutVar,
                           IsAktif = s.IsAktif,

                           BasvuruSurecSinavTarihleris = s.BasvuruSurecSinavTarihleris.ToList(),
                           BasvuruSurecSinavNotlaris = s.BasvuruSurecSinavNotlaris.ToList()
                       }).First();
                if (mdl.WebService && mdl.WsSinavCekimTipID.HasValue && mdl.WsSinavCekimTipID.Value == WsCekimTipi.Tarih)
                {
                    mdl.SinavTipleriDonems = (from sq in db.BasvuruSurecSinavTipleriDonems.Where(p => p.BasvuruSurecSinavTipID == mdl.BasvuruSurecSinavTipID)
                                              select new krSinavTipleriDonems
                                              {
                                                  SinavTipID = sq.BasvuruSurecSinavTipID,
                                                  SinavTipDonemID = sq.BasvuruSurecSinavTipDonemID,
                                                  Yil = sq.Yil,
                                                  SinavDilID = sq.SinavDilID,
                                                  WsDonemKod = sq.WsDonemKod,
                                                  WsDonemAd = sq.WsDonemAd
                                              }).ToList();
                    //var sinavDiller = db.SinavDilleris.ToList();
                    //foreach (var item in mdl.SinavTipleriDonems)
                    //{
                    //    item.WsDonemAd = item.WsDonemKod + " " + sinavDiller.Where(p => p.SinavDilID == item.SinavDilID).First().DilAdi;
                    //}
                }
                else
                {

                    mdl.SinavTipleriDonems = (from sq in db.BasvuruSurecSinavTipleriDonems.Where(p => p.BasvuruSurecSinavTipID == mdl.BasvuruSurecSinavTipID)
                                              select new krSinavTipleriDonems
                                              {
                                                  SinavTipID = sq.BasvuruSurecSinavTipID,
                                                  SinavTipDonemID = sq.BasvuruSurecSinavTipDonemID,
                                                  Yil = sq.Yil,
                                                  SinavDilID = sq.SinavDilID,
                                                  WsDonemKod = sq.WsDonemKod,
                                                  WsDonemAd = sq.WsDonemAd,
                                                  IsTaahhutVar = sq.IsTaahhutVar
                                              }).ToList();

                }
                mdl.BasvuruSurecSinavTiplerSubSinavAraliks = new List<BasvuruSurecSinavTiplerSubSinavAralik>();
                var SubSdata = db.BasvuruSurecSinavTiplerSubSinavAraliks.Where(p => p.BasvuruSurecSinavTipleri.BasvuruSurecID == BasvuruSurecID && p.BasvuruSurecSinavTipleri.SinavTipID == SinavTipID).ToList();
                foreach (var item in SubSdata)
                {
                    item.SubSinavAralikAdi = item.SubSinavAralikAdi;
                    mdl.BasvuruSurecSinavTiplerSubSinavAraliks.Add(item);
                }
                var bsDilIds = db.BasvuruSurecSinavTipleriDils.Where(p => p.BasvuruSurecSinavTipID == mdl.BasvuruSurecSinavTipID).Select(s => s.SinavDilID).ToList();

                var qOgrenimTipKods = OgrenimTipKods.Select((s, inx) => new { s = s, Index = inx }).ToList();
                var qProgramKods = ProgramKods.Select((s, inx) => new { s = s, Index = inx }).ToList();
                var qIngilizces = Ingilizces.Select((s, inx) => new { s = s, Index = inx }).ToList();
                var qtercihler = (from s in qOgrenimTipKods
                                  join p in qProgramKods on s.Index equals p.Index
                                  join pr in db.Programlars on p.s equals pr.ProgramKod
                                  join qi in qIngilizces on s.Index equals qi.Index
                                  select new { ProgramKod = p.s, OgrenimTipKod = s.s, Ingilizce = qi.s }).ToList();



                var prOzelTanim = new List<BasvuruSurecSinavTipleriOT_SNA_OT>();
                foreach (var item in qtercihler)
                {

                    var pKriter = (from s in db.BasvuruSurecSinavTipleriOT_SNA
                                   join ot in db.BasvuruSurecSinavTipleriOT_SNA_OT on s.BasvuruSurecSinavTipleriOT_SNAID equals ot.BasvuruSurecSinavTipleriOT_SNAID
                                   join pr in db.BasvuruSurecSinavTipleriOT_SNA_PR on s.BasvuruSurecSinavTipleriOT_SNAID equals pr.BasvuruSurecSinavTipleriOT_SNAID
                                   where s.BasvuruSurecID == BasvuruSurecID && s.SinavTipID == SinavTipID && ot.OgrenimTipKod == item.OgrenimTipKod && ot.Ingilizce == item.Ingilizce && pr.ProgramKod == item.ProgramKod

                                   select new
                                   {
                                       ot.BasvuruSurecSinavTipleriOT_SNA_OTID,
                                       ot.BasvuruSurecSinavTipleriOT_SNAID,
                                       ot.OgrenimTipKod,
                                       ot.Ingilizce,
                                       ot.IsGecerli,
                                       ot.IsIstensin,
                                       ot.IsOzelNotAralik,
                                       ot.Min,
                                       ot.Max,
                                       prID = pr.BasvuruSurecSinavTipleriOT_SNA_PRID

                                   }).FirstOrDefault();
                    if (pKriter != null)
                        prOzelTanim.Add(new BasvuruSurecSinavTipleriOT_SNA_OT
                        {
                            BasvuruSurecSinavTipleriOT_SNA_OTID = pKriter.BasvuruSurecSinavTipleriOT_SNA_OTID,
                            BasvuruSurecSinavTipleriOT_SNAID = pKriter.BasvuruSurecSinavTipleriOT_SNAID,
                            OgrenimTipKod = pKriter.OgrenimTipKod,
                            Ingilizce = pKriter.Ingilizce,
                            IsGecerli = pKriter.IsGecerli,
                            IsIstensin = pKriter.IsIstensin,
                            IsOzelNotAralik = pKriter.IsOzelNotAralik,
                            Min = pKriter.Min,
                            Max = pKriter.Max
                        });
                    else
                    {
                        var StandartOtNotAralik = db.BasvuruSurecSinavTipleriOTNotAraliklaris.Where(p => p.IsOzelNotAralik && p.SinavTipID == SinavTipID && p.BasvuruSurecID == BasvuruSurecID).ToList();
                        var qqQT = StandartOtNotAralik.Where(p => qtercihler.Any(a => a.OgrenimTipKod == p.OgrenimTipKod && a.Ingilizce == p.Ingilizce)).OrderByDescending(o => o.Min).FirstOrDefault();
                        if (qqQT != null)
                        {

                            prOzelTanim.Add(new BasvuruSurecSinavTipleriOT_SNA_OT
                            {
                                Min = qqQT.Min,
                                Max = qqQT.Max
                            });
                        }
                    }

                }
                mdl.Min = prOzelTanim.Select(s => s.Min).Max();
                mdl.Max = prOzelTanim.Select(s => s.Max).Max();
                if (mdl.OzelNotTipID == OzelNotTip.SeciliNotlar)
                {
                    mdl.MinNotAdi = mdl.BasvuruSurecSinavNotlaris.OrderBy(o => o.SinavNotDeger).Select(s => s.SinavNotAdi).First();
                    mdl.MaxNotAdi = mdl.BasvuruSurecSinavNotlaris.OrderByDescending(o => o.SinavNotDeger).Select(s => s.SinavNotAdi).First();
                }
                else if (mdl.Min.HasValue)
                {

                    mdl.MinNotAdi = mdl.Min.Value.ToString();
                    mdl.MaxNotAdi = mdl.Max.Value.ToString();
                }



                mdl.BsSinavDilleri = db.SinavDilleris.Where(p => bsDilIds.Contains(p.SinavDilID)).ToList();
                mdl.SinavDilleriStr = string.Join(", ", mdl.BsSinavDilleri.Select(s => s.DilAdi).ToList());
            }
            return mdl;
        }


        public static YayinBilgiModel getYayinBilgisi(int MezuniyetSurecID, int MezuniyetYayinTurID)
        {
            var mdl = new YayinBilgiModel();
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                mdl = (from s in db.MezuniyetSureciYayinTurleris.Where(p => p.MezuniyetSurecID == MezuniyetSurecID && p.MezuniyetYayinTurID == MezuniyetYayinTurID)
                       join sd in db.MezuniyetYayinTurleris on new { s.MezuniyetYayinTurID } equals new { sd.MezuniyetYayinTurID }
                       join yb in db.MezuniyetYayinBelgeTurleris on new { s.MezuniyetYayinBelgeTurID } equals new { MezuniyetYayinBelgeTurID = (int?)yb.MezuniyetYayinBelgeTurID } into defyb
                       from ybD in defyb.DefaultIfEmpty()
                       join klk in db.MezuniyetYayinLinkTurleris on new { s.KaynakMezuniyetYayinLinkTurID } equals new { KaynakMezuniyetYayinLinkTurID = (int?)klk.MezuniyetYayinLinkTurID } into defklk
                       from klkD in defklk.DefaultIfEmpty()
                       join ym in db.MezuniyetYayinMetinTurleris on new { s.MezuniyetYayinMetinTurID } equals new { MezuniyetYayinMetinTurID = (int?)ym.MezuniyetYayinMetinTurID } into defym
                       from ymD in defym.DefaultIfEmpty()
                       join kl in db.MezuniyetYayinLinkTurleris on new { s.YayinMezuniyetYayinLinkTurID } equals new { YayinMezuniyetYayinLinkTurID = (int?)kl.MezuniyetYayinLinkTurID } into defkl
                       from klD in defkl.DefaultIfEmpty()
                       join inx in db.MezuniyetYayinIndexTurleris on new { s.MezuniyetYayinIndexTurID } equals new { MezuniyetYayinIndexTurID = (int?)inx.MezuniyetYayinIndexTurID } into definx
                       from inxD in definx.DefaultIfEmpty()
                       select new YayinBilgiModel
                       {
                           MezuniyetYayinTurID = s.MezuniyetYayinTurID,
                           MezuniyetYayinTurAdi = sd.MezuniyetYayinTurAdi,
                           MezuniyetYayinTarihZorunlu = s.TarihIstensin,
                           MezuniyetYayinBelgeTurID = s.MezuniyetYayinBelgeTurID,
                           MezuniyetYayinBelgeTurAdi = ybD != null ? ybD.BelgeTurAdi : "",
                           MezuniyetYayinBelgeTurZorunlu = s.BelgeZorunlu,
                           MezuniyetYayinKaynakLinkTurID = s.KaynakMezuniyetYayinLinkTurID,
                           MezuniyetYayinKaynakLinkTurAdi = klkD != null ? klkD.LinkTurAdi : "",
                           MezuniyetYayinKaynakLinkIsUrl = klD != null ? klD.IsUrl : false,
                           MezuniyetYayinKaynakLinkTurZorunlu = s.KaynakLinkiZorunlu,
                           MezuniyetYayinMetinTurID = s.MezuniyetYayinMetinTurID,
                           MezuniyetYayinMetinTurAdi = ymD != null ? ymD.MetinTurAdi : "",
                           MezuniyetYayinMetinZorunlu = s.MetinZorunlu,
                           MezuniyetYayinLinkTurID = s.YayinMezuniyetYayinLinkTurID,
                           MezuniyetYayinLinkTurAdi = klD != null ? klD.LinkTurAdi : "",
                           MezuniyetYayinLinkIsUrl = klD != null ? klD.IsUrl : false,
                           MezuniyetYayinLinkiZorunlu = s.YayinLinkiZorunlu,
                           MezuniyetYayinIndexTurZorunlu = s.YayinIndexTurIstensin,
                           MezuniyetKabulEdilmisMakaleZorunlu = s.YayinKabulEdilmisMakaleIstensin,
                           YayinDeatKurulusIstensin = s.YayinDeatKurulusIstensin,
                           YayinDergiAdiIstensin = s.YayinDergiAdiIstensin,
                           YayinMevcutDurumIstensin = s.YayinMevcutDurumIstensin,
                           YayinProjeEkibiIstensin = s.YayinProjeEkibiIstensin,
                           YayinProjeTurIstensin = s.YayinProjeTurIstensin,
                           YayinYazarlarIstensin = s.YayinYazarlarIstensin,
                           YayinYilCiltSayiIstensin = s.YayinYilCiltSayiIstensin,
                           IsTarihAraligiIstensin = s.IsTarihAraligiIstensin,
                           YayinEtkinlikAdiIstensin = s.YayinEtkinlikAdiIstensin,
                           YayinYerBilgisiIstensin = s.YayinYerBilgisiIstensin,
                       }).First();
                mdl.YayinIndexTurleri = db.MezuniyetYayinIndexTurleris.ToList();
                mdl.MezuniyetYayinProjeTurleris = db.MezuniyetYayinProjeTurleris.ToList();
            }
            return mdl;
        }
        public static int? toStrToSinavDilID(this string strDil)
        {
            strDil = strDil ?? "ingilizce";
            int? dilID = null;
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var dilB = db.SinavDilleris.Where(p => strDil.Contains(p.DilAdi)).FirstOrDefault();
                if (dilB != null)
                {
                    dilID = dilB.SinavDilID;
                }
            }

            return dilID;

        }

        public static SinavSonucBilgiModel getSinavTipSonucModel(int SinavTipID, int BasvuruSurecID, string Yil, int? WsSonucID, string Tck, int? BasvuruID = null, string MinNotAdi = "")
        {
            SinavSonucBilgiModel mdl = new SinavSonucBilgiModel();
            mdl.MinNotAdi = MinNotAdi;

            try
            {

                using (var db = new LisansustuBasvuruSistemiEntities())
                {
                    var Surec = db.BasvuruSurecs.Where(p => p.BasvuruSurecID == BasvuruSurecID).First();

                    if (BasvuruID.HasValue == false)
                    {
                        var sinav = Management.getSinavBilgisi(BasvuruSurecID, SinavTipID, new List<int>(), new List<string>(), new List<bool>());


                        mdl.SinavTipGrupID = sinav.SinavTipGrupID;
                        var WebServiceKod = sinav.WebServiceKod;
                        //WebServiceKod = "15";
                        var strDonem = Yil + "-" + sinav.SinavAdi;
                        mdl.SinavAdi = strDonem;
                        mdl.TCKimlikNo = Tck;
                        mdl.SinavYili = Yil;
                        mdl.EnstituKod = sinav.EnstituKod;

                        var SinavGecerlilikBaslangTarihi = Surec.BaslangicTarihi.AddYears(-5);

                        OSYMBinding binding = new OSYMBinding();
                        EndpointAddress endpointAddress = new
                        EndpointAddress("https://vps.osym.gov.tr/Ext/Provider/BilgiServisi/Sonuc");
                        var KullaniciAdi = SistemAyar.getAyar(SistemAyar.AyarOsymWSKullaniciAdi);
                        var Sifre = SistemAyar.getAyar(SistemAyar.AyarOsymWSKullaniciSifre);
                        using (var client = new Ws_Osym_1.SonucClient(binding, endpointAddress))
                        {

                            client.ChannelFactory.Endpoint.Behaviors.Remove<System.ServiceModel.Description.ClientCredentials>();
                            client.ChannelFactory.Endpoint.Behaviors.Add(new OSYMCredentials());
                            client.ClientCredentials.UserName.UserName = KullaniciAdi;
                            client.ClientCredentials.UserName.Password = Sifre;
                            var qGrop = client.SinavGrupBilgileriniGetir();

                            var sonuc = client.SinavSonuclariGetir(Tck, Yil.ToInt().Value, WebServiceKod.ToInt().Value);

                            //mdl.SinavKodu = sonuc.SinavId;




                            if (sonuc.SonucKodu.ToString() == "Basarili")
                            {
                                #region sonucGoster

                                Ws_Osym_1.SinavSonucTemelBilgi qSonuc = null;
                                if (WsSonucID.HasValue)
                                {
                                    qSonuc = sonuc.Sonuc.Where(p => p.AciklamaTarihi >= SinavGecerlilikBaslangTarihi && p.Id == WsSonucID.Value).FirstOrDefault();
                                }
                                else
                                {
                                    // var sonuclar = sonuc.Sonuc.Where(p => donemsB.Any(a => p.Ad.ToLower().Contains(a.ToLower()))).ToList();
                                    var sonuclar = sonuc.Sonuc.Where(p => p.AciklamaTarihi >= SinavGecerlilikBaslangTarihi && (sinav.SinavTipKod == 23 ? (p.Ad.Contains("e-YDS")) : !p.Ad.Contains("e-YDS"))).ToList();
                                    foreach (var item in sonuclar)
                                    {
                                        mdl.WsSinavSonucList.Add(new CmbIntDto { Value = item.Id, Caption = Yil + " (" + item.AciklamaTarihi.ToDateString() + ")" });
                                    }

                                }
                                if (qSonuc != null)
                                {
                                    mdl.IsSinavSonucuVar = sonuc.Sonuc.Any();
                                    mdl.Durum = OsymSonucTip.BuAdayaAitSinavSonucuMevcut;
                                    var sinavSonucu = client.SinavSonucXml(Tck, qSonuc.Id);
                                    mdl.AciklanmaTarihi = sinavSonucu.Sonuc.AciklamaTarihi == DateTime.MinValue ? (DateTime?)null : sinavSonucu.Sonuc.AciklamaTarihi;


                                    var xml = new XmlDocument();
                                    xml.LoadXml(sinavSonucu.Sonuc.Xml);
                                    mdl.WsXmlData = sinavSonucu.Sonuc.Xml;

                                    if (sinav.SinavTipGrupID == SinavTipGrup.DilSinavlari)
                                    {
                                        var output = mdl.WsXmlData.toSinavSonucDilXmlModel();
                                        if (!output.DILADI.IsNullOrWhiteSpace()) output.DIL = output.DILADI;
                                        else if (!output.DIL_ADI.IsNullOrWhiteSpace()) output.DIL = output.DIL_ADI;

                                        bool isContainsLang = false;
                                        foreach (var item in sinav.BsSinavDilleri) // dil sınavnda istenen dillerin kontrolü yapılır
                                        {
                                            if (output.DIL.ToLower().Contains(item.DilAdi.ToLower()) || item.DilAdi.ToLower().Contains(output.DIL.ToLower())) { isContainsLang = true; break; }
                                        }
                                        if (isContainsLang)
                                        {
                                            mdl.SinavDilID = output.DIL.toStrToSinavDilID();
                                            mdl.jSonValDilSinavi = output;
                                            if (output.PUAN.ToDouble().HasValue)
                                            {
                                                mdl.Puan = output.PUAN.Replace('.', ',').ToDouble().Value;
                                            }
                                            else
                                            {
                                                mdl.Durum = OsymSonucTip.BuAdayaAitSinavSonucuYok;
                                                mdl.Puan = 0;
                                                string bilgi = "SinavID=" + SinavTipID + "," + sinav.SinavAdi + " \r\n BasvuruSurecID=" + BasvuruSurecID + " \r\n Yıl=" + Yil + "\r\n TcKimlikNo=" + Tck + "\r\n Puan=" + output.PUAN;
                                                Management.SistemBilgisiKaydet("Web servisinden çekilen sınav bilgisine ait sayısal bir sonuca rastlanmadı!\r\n" + bilgi + " \r\n XmlData: " + mdl.WsXmlData, "Management/getSinavTipSonucModel", BilgiTipi.Bilgi, UserIdentity.Current.Id, UserIdentity.Ip);
                                            }
                                        }
                                        else
                                        {
                                            mdl.WsXmlData = null;
                                            mdl.Durum = OsymSonucTip.BuAdayaAitSinavSonucuYok;
                                            mdl.Aciklama = sonuc.Aciklama ?? sonuc.SonucKodu.toStrObjEmptString();
                                        }

                                    }
                                    else
                                    {
                                        var output = mdl.WsXmlData.toSinavSonucAlesXmlModel();
                                        mdl.jSonValAlesSinavi = output;
                                        if (sinav.EnstituKod == EnstituKodlari.FenBilimleri)
                                        {

                                            if (mdl.jSonValAlesSinavi.SAY_PUAN.ToDouble().HasValue)
                                            {
                                                mdl.Puan = output.SAY_PUAN.Replace('.', ',').ToDouble().Value.ToString("n2").ToDouble().Value;
                                            }
                                            else
                                            {
                                                mdl.Durum = OsymSonucTip.BuAdayaAitSinavSonucuYok;
                                                mdl.Puan = 0;
                                                mdl.Aciklama = sonuc.Aciklama ?? sonuc.SonucKodu.toStrObjEmptString();
                                                string bilgi = "SinavID=" + SinavTipID + "," + sinav.SinavAdi + " \r\n BasvuruSurecID=" + BasvuruSurecID + " \r\n Yıl=" + Yil + "\r\n TcKimlikNo=" + Tck + "\r\n Puan=" + output.SAY_PUAN;
                                                Management.SistemBilgisiKaydet("Web servisinden çekilen sınav bilgisine ait sayısal bir sonuca rastlanmadı!\r\n" + bilgi + " \r\n XmlData: " + mdl.WsXmlData, "Management/getSinavTipSonucModel", BilgiTipi.Bilgi, UserIdentity.Current.Id, UserIdentity.Ip);
                                            }
                                            mdl.AlesTipID = AlesTipBilgi.Sayısal;
                                        }
                                        else
                                        {
                                            if (mdl.jSonValAlesSinavi.SOZ_PUAN.ToDouble().HasValue)
                                            {
                                                mdl.Puan = output.SOZ_PUAN.Replace('.', ',').ToDouble().Value.ToString("n2").ToDouble().Value;

                                            }
                                            else
                                            {
                                                mdl.Durum = OsymSonucTip.BuAdayaAitSinavSonucuYok;
                                                mdl.Puan = 0;
                                                mdl.Aciklama = sonuc.Aciklama ?? sonuc.SonucKodu.toStrObjEmptString();
                                                string bilgi = "SinavID=" + SinavTipID + "," + sinav.SinavAdi + " \r\n BasvuruSurecID=" + BasvuruSurecID + " \r\n Yıl=" + Yil + "\r\n TcKimlikNo=" + Tck + "\r\n Puan=" + output.SOZ_PUAN;
                                                Management.SistemBilgisiKaydet("Web servisinden çekilen sınav bilgisine ait sayısal bir sonuca rastlanmadı!\r\n" + bilgi + " \r\n XmlData: " + mdl.WsXmlData, "Management/getSinavTipSonucModel", BilgiTipi.Bilgi, UserIdentity.Current.Id, UserIdentity.Ip);
                                            }

                                        }
                                    }

                                }
                                else
                                {

                                    mdl.Durum = OsymSonucTip.BuAdayaAitSinavSonucuYok;
                                    mdl.ShowIsTaahhutVar = sinav.SinavTipleriDonems.Any(a => a.Yil == Yil.ToInt().Value && a.IsTaahhutVar);
                                    mdl.Aciklama = sonuc.Aciklama ?? sonuc.SonucKodu.toStrObjEmptString();
                                    //string bilgi = "SinavID=" + SinavTipID + "," + sinav.SinavAdi + " \r\n BasvuruSurecID=" + BasvuruSurecID + " \r\n Yıl=" + Yil + "\r\n Donem=" + DonemB + "\r\n TcKimlikNo=" + Tck + " \r\nSonuç kodu:" + sonuc.SonucKodu + " \r\n Acıklama: " + sonuc.Aciklama;
                                    //Management.SistemBilgisiKaydet("Web servisinden  sınav bilgisi çekilemedi! \r\nDetay: " + bilgi, "Management/getSinavTipSonucModel", BilgiTipi.Bilgi, UserIdentity.Current.Id, UserIdentity.Ip);

                                }
                                #endregion
                            }
                            else
                            {
                                mdl.Durum = OsymSonucTip.BuAdayaAitSinavSonucuYok;
                                mdl.ShowIsTaahhutVar = sinav.SinavTipleriDonems.Any(a => a.Yil == Yil.ToInt().Value && a.IsTaahhutVar);

                                mdl.Aciklama = sonuc.Aciklama ?? sonuc.SonucKodu.toStrObjEmptString();
                                if (sonuc.SonucKodu.ToString() != "KayitBulunamadi")
                                {
                                    string bilgi = "SinavID=" + SinavTipID + "," + sinav.SinavAdi + " \r\n BasvuruSurecID=" + BasvuruSurecID + " \r\n Yıl=" + Yil + "\r\n TcKimlikNo=" + Tck + " \r\nSonuç kodu:" + sonuc.SonucKodu + " \r\n Acıklama: " + sonuc.Aciklama;
                                    Management.SistemBilgisiKaydet("Web servisinden  sınav bilgisi çekilemedi! \r\nDetay: " + bilgi, "Management/getSinavTipSonucModel", BilgiTipi.Kritik, UserIdentity.Current.Id, UserIdentity.Ip);

                                }

                            }

                        }
                        if (sinav.WebServiceKod == "18" && mdl.Durum == OsymSonucTip.BuAdayaAitSinavSonucuYok)
                        {
                            var Sinavlarx = GetYdsSonuc(Tck, SinavGecerlilikBaslangTarihi);
                            foreach (var item in Sinavlarx.Where(p => p.SinavTarihi.Value.Year == Yil.ToInt().Value))
                            {
                                mdl.WsSinavSonucList.Add(new CmbIntDto { Value = item.ID, Caption = Yil + " (" + item.SinavTarihi.ToDateString() + ")" });
                            }

                            #region YdsSet

                            if (Sinavlarx.Any())
                            {
                                #region sonucGoster

                                YDSinavlar qSonuc = null;
                                if (WsSonucID.HasValue)
                                {
                                    qSonuc = Sinavlarx.Where(p => p.ID == WsSonucID.Value).FirstOrDefault();
                                }

                                if (qSonuc != null)
                                {
                                    mdl.IsSinavSonucuVar = true;
                                    mdl.Durum = OsymSonucTip.BuAdayaAitSinavSonucuMevcut;
                                    mdl.AciklanmaTarihi = qSonuc.SinavTarihi == DateTime.MinValue ? (DateTime?)null : qSonuc.SinavTarihi;

                                    mdl.jSonValDilSinavi = new SinavSonucDilXmlModel { DIL = "İNGİLİZCE / ENGLISH", PUAN = mdl.Puan.ToString() };
                                    mdl.WsXmlData = mdl.jSonValDilSinavi.toJsonText();
                                    mdl.SinavDilID = "ingilizce".toStrToSinavDilID();
                                    if (qSonuc.Sonuc.HasValue)
                                    {
                                        mdl.Puan = qSonuc.Sonuc.Value;
                                    }
                                    else
                                    {
                                        mdl.Durum = OsymSonucTip.BuAdayaAitSinavSonucuYok;
                                        mdl.Puan = 0;
                                        string bilgi = "SinavID=" + SinavTipID + "," + sinav.SinavAdi + " \r\n BasvuruSurecID=" + BasvuruSurecID + " \r\n Yıl=" + Yil + "\r\n TcKimlikNo=" + Tck + "\r\n Puan=" + qSonuc.Sonuc;
                                        Management.SistemBilgisiKaydet("Web servisinden çekilen sınav bilgisine ait sayısal bir sonuca rastlanmadı!\r\n" + bilgi + " \r\n XmlData: " + mdl.WsXmlData, "Management/getSinavTipSonucModel", BilgiTipi.Bilgi, UserIdentity.Current.Id, UserIdentity.Ip);
                                    }
                                }
                                else
                                {

                                    mdl.Durum = OsymSonucTip.BuAdayaAitSinavSonucuYok;
                                    mdl.ShowIsTaahhutVar = sinav.SinavTipleriDonems.Any(a => a.Yil == Yil.ToInt().Value && a.IsTaahhutVar);

                                }
                                #endregion
                            }
                            else
                            {
                                mdl.Durum = OsymSonucTip.BuAdayaAitSinavSonucuYok;
                                mdl.ShowIsTaahhutVar = sinav.SinavTipleriDonems.Any(a => a.Yil == Yil.ToInt().Value && a.IsTaahhutVar);


                            }

                            #endregion
                        }


                    }
                    else
                    {
                        var snvB = db.BasvurularSinavBilgis.Where(p => p.BasvuruID == BasvuruID.Value && p.SinavTipID == SinavTipID).First();
                        var sinav = Management.getSinavBilgisi(snvB.Basvurular.BasvuruSurecID, snvB.SinavTipID, new List<int>(), new List<string>(), new List<bool>());
                        if (sinav.WsSinavCekimTipID.HasValue && sinav.WsSinavCekimTipID.Value == WsCekimTipi.Tarih)
                        {
                            mdl.SinavTipGrupID = sinav.SinavTipGrupID;
                            var dBilgi = db.SinavDilleris.Where(p => p.SinavDilID == snvB.SinavDilID).First();
                            var WebServiceKod = sinav.WebServiceKod;
                            var qSonuc = snvB;
                            mdl.Durum = qSonuc != null ? OsymSonucTip.BuAdayaAitSinavSonucuMevcut : OsymSonucTip.BuAdayaAitSinavSonucuYok;
                            mdl.SinavKodu = sinav.SinavTipKod.ToString();
                            mdl.SinavYili = snvB.SinavTarihi.Value.Year.ToString();
                            mdl.SinavAdi = (mdl.SinavYili + " - " + snvB.SinavTarihi.ToString("dd-MM-yyyy") + " - " + sinav.SinavAdi + " (" + dBilgi.DilAdi + ")");
                            mdl.TCKimlikNo = Tck;
                            mdl.SinavDonemi = (mdl.SinavYili + " " + snvB.SinavTarihi.ToString("dd-MM-yyyy") + "-" + sinav.SinavAdi + " (" + dBilgi.DilAdi + ")");
                            mdl.AciklanmaTarihi = qSonuc != null ? qSonuc.SinavTarihi.TodateToShortDate() : (DateTime?)null;
                            mdl.EnstituKod = sinav.EnstituKod;
                            if (mdl.Durum == OsymSonucTip.BuAdayaAitSinavSonucuMevcut)
                            {
                                var basv = db.Basvurulars.Where(p => p.BasvuruID == BasvuruID).First();
                                mdl.SinavDilID = Yil.ToInt();
                                mdl.Puan = qSonuc.SinavNotu;
                                mdl.jSonValDilSinavi = new SinavSonucDilXmlModel
                                {

                                    AD = basv.Ad,
                                    SOYAD = basv.Soyad,
                                    PUAN = mdl.Puan.ToString(),
                                    DIL = dBilgi.DilAdi,

                                };
                            }
                        }
                        else
                        {
                            mdl.EnstituKod = sinav.EnstituKod;
                            mdl.Durum = OsymSonucTip.BuAdayaAitSinavSonucuMevcut;
                            mdl.SinavTipGrupID = sinav.SinavTipGrupID;
                            mdl.SinavKodu = sinav.WebServiceKod;
                            mdl.SinavAdi = snvB.WsSinavYil + " (" + snvB.WsAciklanmaTarihi.ToString("dd.MM.yyyy") + ") " + sinav.SinavAdi;
                            mdl.TCKimlikNo = snvB.Basvurular.Kullanicilar.TcKimlikNo;
                            mdl.SinavYili = snvB.WsSinavYil.ToString();
                            // mdl.SinavDonemi = snvB.WsSinavDonem + " , " + dBilgi.DonemAdi;
                            mdl.AciklanmaTarihi = snvB.WsAciklanmaTarihi;



                            if (sinav.SinavTipGrupID == SinavTipGrup.DilSinavlari)
                            {
                                var ShowIsTaahhutVar = sinav.SinavTipleriDonems.Any(a => a.WsDonemKod == snvB.WsSinavDonem && a.Yil == snvB.WsSinavYil && a.IsTaahhutVar);
                                mdl.ShowIsTaahhutVar = ShowIsTaahhutVar;
                                mdl.IsTaahhutVar = snvB.IsTaahhutVar ?? false;
                                if ((snvB.IsTaahhutVar == true && ShowIsTaahhutVar) == false || !snvB.WsXmlData.IsNullOrWhiteSpace())
                                {
                                    mdl.WsXmlData = snvB.WsXmlData;
                                    var output = new SinavSonucDilXmlModel();
                                    if (sinav.WebServiceKod != "18")
                                    {
                                        output = snvB.WsXmlData.toSinavSonucDilXmlModel();
                                    }
                                    else
                                    {
                                        try
                                        {
                                            output = Newtonsoft.Json.JsonConvert.DeserializeObject<SinavSonucDilXmlModel>(mdl.WsXmlData);
                                        }
                                        catch
                                        {
                                            output = mdl.WsXmlData.toSinavSonucDilXmlModel();
                                        }

                                    }
                                    output.PUAN = snvB.SinavNotu.ToString();
                                    mdl.jSonValDilSinavi = output;
                                    output.DIL = output.DIL.IsNullOrWhiteSpace() ? (output.DILADI.IsNullOrWhiteSpace() ? output.DIL_ADI : output.DILADI) : output.DIL;
                                    mdl.SinavDilID = output.DIL.toStrToSinavDilID();
                                    mdl.Puan = output.PUAN.Replace('.', ',').ToDouble().Value;
                                }


                            }
                            else
                            {
                                var xml = new XmlDocument();
                                xml.LoadXml(snvB.WsXmlData);
                                mdl.WsXmlData = snvB.WsXmlData;
                                string jsonString = Newtonsoft.Json.JsonConvert.SerializeXmlNode(xml);
                                var jobject = JObject.Parse(jsonString);

                                var output = jobject.Children<JProperty>().Select(prop => prop.Value.ToObject<SinavSonucAlesXmlModel>()).FirstOrDefault();
                                mdl.jSonValAlesSinavi = output;
                                if (sinav.EnstituKod == EnstituKodlari.FenBilimleri)
                                {
                                    mdl.Puan = output.SAY_PUAN.Replace('.', ',').ToDouble().Value.ToString("n2").ToDouble().Value;
                                    mdl.AlesTipID = AlesTipBilgi.Sayısal;
                                }
                                else
                                {
                                    mdl.Puan = output.SOZ_PUAN.Replace('.', ',').ToDouble().Value.ToString("n2").ToDouble().Value;
                                    mdl.AlesTipID = AlesTipBilgi.EşitAğırlık;
                                }

                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                string bilgi = "SinavID=" + SinavTipID + " \r\n BasvuruSurecID=" + BasvuruSurecID + " \r\n Yıl=" + Yil + "\r\n TcKimlikNo=" + Tck;
                Management.SistemBilgisiKaydet("Web servisinden sınav bilgisi çekilirken bir hata oluştu!\r\n" + bilgi + " \r\n Hata: " + ex.ToExceptionMessage(), ex.ToExceptionStackTrace(), BilgiTipi.Kritik, UserIdentity.Current.Id, UserIdentity.Ip);
            }
            return mdl;
        }

        public static List<YDSinavlar> GetYdsSonuc(string TC, DateTime IlkGecerlilikTarihi)
        {
            var Model = new List<YDSinavlar>();

            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            string authInfo = "126982" + ":" + "kzB)s2U796";
            authInfo = Convert.ToBase64String(Encoding.Default.GetBytes(authInfo));


            var sbURL = "https://servisler.yok.gov.tr";
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(sbURL + "/yokdil/OgrencininSinavlariniGetir");
            httpWebRequest.Timeout = 180000;

            httpWebRequest.ContentType = "application/json;charset=UTF-8";
            httpWebRequest.Method = "POST";

            var headerItems = new Dictionary<string, string>();
            foreach (var item in headerItems)
            {
                httpWebRequest.Headers.Add(item.Key, item.Value);
            }
            httpWebRequest.Headers.Add("Basic", authInfo);


            using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
            {
                streamWriter.Write(TC);
            }

            var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
            using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
            {
                var result = streamReader.ReadToEnd();
                Model = Newtonsoft.Json.JsonConvert.DeserializeObject<List<YDSinavlar>>(result);
            }
            foreach (var item in Model)
            {

                httpWebRequest = (HttpWebRequest)WebRequest.Create(sbURL + "/yokdil/OgrencininNotlariniGetir?sinavID=" + item.ID);
                httpWebRequest.Timeout = 180000;

                httpWebRequest.ContentType = "application/json;charset=UTF-8";
                httpWebRequest.Method = "POST";

                headerItems = new Dictionary<string, string>();
                foreach (var Hitem in headerItems)
                {
                    httpWebRequest.Headers.Add(Hitem.Key, Hitem.Value);
                }
                httpWebRequest.Headers.Add("Basic", authInfo);


                using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                {
                    streamWriter.Write(TC);
                }

                httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    var result = streamReader.ReadToEnd();
                    result = result.Replace("\"1\"", "_1");
                    var data = Newtonsoft.Json.JsonConvert.DeserializeObject<YDSonuclar>(result);
                    if (data != null && data._1 != null)
                    {
                        var Sonuc = data._1.FirstOrDefault();
                        if (Sonuc != null && Sonuc.DersAd.Replace("İ", "i").ToLower().Contains("ingilizce") && Sonuc.Sonuc > 0)
                        {
                            item.SinavTarihi = Convert.ToDateTime(item.Tarih);
                            item.DersAd = Sonuc.DersAd;
                            item.Sonuc = Sonuc.Sonuc;
                            item.Aciklama = Sonuc.Aciklama;
                        }
                    }
                }
            }
            return Model.Where(p => p.SinavTarihi.HasValue && p.SinavTarihi.Value >= IlkGecerlilikTarihi).ToList();
        }


        public static List<Kotalar> getWsKotalar(string EnstituKod, int BasvuruSurecTipID)
        {
            var kotaModel = new List<Kotalar>();
            System.Net.ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
            Ws_Kotalar.WebServiceSoapClient soap = new Ws_Kotalar.WebServiceSoapClient("WebServiceSoap");

            var data = soap.Kontenjanlar();
            if (data != null)
            {
                foreach (var item in data.Where(p => p.Instid == EnstituKod && (p.Fieldquota > 0 || p.Fieldoutquota > 0)))
                {
                    kotaModel.Add(new Kotalar
                    {
                        BasvuruSurecTipID = BasvuruSurecTipID,
                        EnstituKod = item.Instid,
                        ProgramKod = item.Branchid,
                        OgrenimTipKod = item.Edutypeid,
                        OrtakKota = false,
                        AlanIciKota = item.Fieldquota,
                        AlanDisiKota = item.Fieldoutquota,
                        MinAles = item.Minles,
                        MinAGNO = item.Minagno,
                        IslemTarihi = DateTime.Now,
                        IslemYapanID = UserIdentity.Current.Id,
                        IslemYapanIP = UserIdentity.Ip
                    });
                }
            }
            if (BasvuruSurecTipID == BasvuruSurecTipi.YTUYeniMezunDRBasvuru)
            {
                kotaModel = kotaModel.Where(p => p.OgrenimTipKod == OgrenimTipi.Doktra || p.OgrenimTipKod == OgrenimTipi.ButunlesikDoktora || p.OgrenimTipKod == OgrenimTipi.SanattaYeterlilik).ToList();
            }
            return kotaModel;
        }

        public static PersisWsDataModel getWsPersisOE(string term)
        {
            Ws_Persis.Service1SoapClient cl = new Ws_Persis.Service1SoapClient("Service1Soap");

            var data = cl.irfan_veri("irfan", "irfan123", term);
            var dataPers = (PersisWsDataModel)JsonConvert.DeserializeObject(data, typeof(PersisWsDataModel));

            return dataPers;
        }

        public static MmMessage kuKontrol(kmBasvuru kModel)
        {
            var _MmMessage = new MmMessage();
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var kullanici = db.Kullanicilars.Where(p => p.KullaniciID == kModel.KullaniciID).First();
                bool IsKurumIci = kullanici.KullaniciTipleri.KurumIci;
                bool IsYerli = kullanici.KullaniciTipleri.Yerli;

                #region kullaniciKontrol

                if (IsYerli)
                    if (kModel.TcKimlikNo.IsNullOrWhiteSpace())
                    {
                        _MmMessage.Messages.Add("Tc Kimlik No Giriniz");

                        _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "TcKimlikNo" });
                    }
                    else if (kModel.TcKimlikNo.IsNumber() == false)
                    {
                        _MmMessage.Messages.Add("Tc Kimlik No Sadece Sayıdan Oluşmalıdır");
                        _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "TcKimlikNo" });

                    }
                    else if (kModel.TcKimlikNo.Length != 11)
                    {
                        _MmMessage.Messages.Add("Tc Kimlik No 11 haneli olmalıdır!");
                        _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "TcKimlikNo" });

                    }
                    else _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "TcKimlikNo" });
                if (!IsYerli)
                    if (kModel.PasaportNo.IsNullOrWhiteSpace())
                    {
                        string msg = "Pasaport No Giriniz";
                        _MmMessage.Messages.Add(msg);

                        _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "PasaportNo" });
                    }
                    else _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "PasaportNo" });
                if (!kModel.CinsiyetID.HasValue)
                {
                    _MmMessage.Messages.Add("Cinsiyet Bilgisini Seçiniz.");
                    _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "CinsiyetID" });
                }
                else _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "CinsiyetID" });



                if (kModel.AnaAdi.IsNullOrWhiteSpace())
                {
                    _MmMessage.Messages.Add("Ana Adı Giriniz.");
                    _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "AnaAdi" });
                }
                else _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "AnaAdi" });

                if (kModel.BabaAdi.IsNullOrWhiteSpace())
                {
                    _MmMessage.Messages.Add("Baba Adı Giriniz.");
                    _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "BabaAdi" });
                }
                else _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "BabaAdi" });

                if (!kModel.DogumYeriKod.HasValue)
                {
                    _MmMessage.Messages.Add("Doğum Yeri Giriniz.");
                    _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "DogumYeriKod" });
                }
                else _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "DogumYeriKod" });

                if (!kModel.DogumTarihi.HasValue)
                {
                    _MmMessage.Messages.Add("Doğum Tarihi Giriniz.");
                    _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "DogumTarihi" });
                }
                else _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "DogumTarihi" });
                if (IsYerli)
                    if (!kModel.NufusilIlceKod.HasValue)
                    {
                        _MmMessage.Messages.Add("Nüfus İl/İlçe Giriniz.");
                        _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "NufusilIlceKod" });
                    }
                    else _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "NufusilIlceKod" });

                if (IsYerli)
                    if (!kModel.CiltNo.HasValue)
                    {
                        _MmMessage.Messages.Add("Cilt No Bilgisi Giriniz.");
                        _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "CiltNo" });
                    }
                    else _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "CiltNo" });
                if (IsYerli)
                    if (!kModel.AileNo.HasValue)
                    {
                        _MmMessage.Messages.Add("Aile No Bilgisi Giriniz.");
                        _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "AileNo" });
                    }
                    else _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "AileNo" });
                if (IsYerli)
                    if (!kModel.SiraNo.HasValue)
                    {
                        _MmMessage.Messages.Add("Sıra No Bilgisi Giriniz.");
                        _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "SiraNo" });
                    }
                    else _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "SiraNo" });

                if (!kModel.UyrukKod.HasValue)
                {
                    _MmMessage.Messages.Add("Uyruk Giriniz.");
                    _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "UyrukKod" });
                }
                else _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "UyrukKod" });



                if (IsYerli)
                    if (!kModel.SehirKod.HasValue)
                    {
                        _MmMessage.Messages.Add("Yaşadığı Şehir Bilgisini Giriniz.");
                        _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "SehirKod" });
                    }
                    else _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "SehirKod" });

                if (kModel.CepTel.IsNullOrWhiteSpace() && kModel.EvTel.IsNullOrWhiteSpace() && kModel.IsTel.IsNullOrWhiteSpace())
                {
                    _MmMessage.Messages.Add("Cep, iş ve ev telefonu bilgilerinden en az birinin girilmesi zorunludur!");
                    _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "CepTel" });
                    _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "EvTel" });
                    _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "IsTel" });
                }
                else
                {
                    if (kModel.CepTel.IsNullOrWhiteSpace() == false) _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "CepTel" });
                    if (kModel.EvTel.IsNullOrWhiteSpace() == false) _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "EvTel" });
                    if (kModel.IsTel.IsNullOrWhiteSpace() == false) _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "IsTel" });
                }

                if (kModel.EMail.IsNullOrWhiteSpace())
                {
                    _MmMessage.Messages.Add("EMail Giriniz.");
                    _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "EMail" });
                }
                else if (kModel.EMail.ToIsValidEmail())
                {
                    _MmMessage.Messages.Add("Lütfen EMail Formatını Doğru Giriniz");
                    _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "EMail" });
                }
                else
                {
                    _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "EMail" });
                }

                if (kModel.Adres.IsNullOrWhiteSpace() && kModel.Adres2.IsNullOrWhiteSpace())
                {
                    _MmMessage.Messages.Add("Adres Bilgisi Giriniz.");
                    _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "Adres" });
                    _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "Adres2" });
                }
                else
                {
                    if (!kModel.Adres.IsNullOrWhiteSpace()) _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "Adres" });
                    if (!kModel.Adres2.IsNullOrWhiteSpace()) _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "Adres2" });
                }


                #endregion
            }
            return _MmMessage;
        }
        public static MmMessage obKontrol(kmBasvuru kModel)
        {
            var _MmMessage = new MmMessage();
            using (var db = new LisansustuBasvuruSistemiEntities())
            {

                if (!kModel.LUniversiteID.HasValue)
                {
                    _MmMessage.Messages.Add("Lisans Eğitimi Üniversite Bilgisini Seçiniz!");
                    _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "LUniversiteID" });
                }
                else _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "LUniversiteID" });

                if (kModel.LFakulteAdi.IsNullOrWhiteSpace())
                {
                    _MmMessage.Messages.Add("Lisans Eğitimi Fakülte Adı Bilgisini Giriniz!");
                    _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "LFakulteAdi" });
                }
                else _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "LFakulteAdi" });
                if (!kModel.LOgrenciBolumID.HasValue)
                {
                    _MmMessage.Messages.Add("Lisans Eğitimi Bölüm Bilgisini Seçiniz!");
                    _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "LOgrenciBolumID" });
                }
                else _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "LOgrenciBolumID" });
                if (kModel.LUniversiteID == UniversiteYtuKod)
                {
                    var bsurec = db.BasvuruSurecs.Where(p => p.BasvuruSurecID == kModel.BasvuruSurecID).First();
                    if (bsurec.AGNOGirisBaslangicTarihi.HasValue && kModel._OgrenimTipKod.Contains(OgrenimTipi.TezliYuksekLisans) && kModel.LOgrenimDurumID.HasValue == false)
                    {
                        string msg = "Öğrenim durumunuzu seçiniz";
                        _MmMessage.Messages.Add(msg);
                        _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "LOgrenimDurumID" });
                    }
                    else _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "LOgrenimDurumID" });
                }
                else _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "LOgrenimDurumID" });
                if (!kModel.LNotSistemID.HasValue)
                {

                    _MmMessage.Messages.Add("Lisans Eğitimi Not Sistemi Bilgisini Seçiniz!");
                    _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "LNotSistemID" });
                }
                else
                {
                    _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "LNotSistemID" });

                    var notSistemi = Management.getNotSistemi(kModel.LNotSistemID.Value);
                    var nots = db.NotSistemleris.Where(p => p.NotSistemID == notSistemi.NotSistemID).First();
                    if (!kModel.LMezuniyetNotu.HasValue)
                    {
                        string msg = "Lisans Eğitimi Not Bilgisini Giriniz!";
                        _MmMessage.Messages.Add(msg);
                        _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "LMezuniyetNotu" });

                    }
                    else if (!((double)nots.MinNot <= kModel.LMezuniyetNotu.Value && (double)nots.MaxNot >= kModel.LMezuniyetNotu))
                    {
                        _MmMessage.Messages.Add("Lisans Eğitimi Notu " + notSistemi.NotSistemAdi + " not sistemine göre " + nots.MinNot.ToString() + " ile " + nots.MaxNot.ToString() + " arasında bir değer olmalıdır!");

                        _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "LMezuniyetNotu" });
                    }
                    else _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "LMezuniyetNotu" });

                }
                var BasvuruSurec = db.BasvuruSurecs.Where(p => p.BasvuruSurecID == kModel.BasvuruSurecID).First();

                if (kModel.YLDurum || BasvuruSurec.BasvuruSurecTipID == BasvuruSurecTipi.YatayGecisBasvuru)
                {


                    if (!kModel.YLUniversiteID.HasValue)
                    {

                        _MmMessage.Messages.Add("Yüksek Lisans Eğitimi Üniversite Bilgisini Seçiniz!");
                        _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "YLUniversiteID" });
                    }
                    else _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "YLUniversiteID" });

                    if (kModel.YLFakulteAdi.IsNullOrWhiteSpace())
                    {
                        _MmMessage.Messages.Add("Yüksek Lisans Eğitimi Anabilim Dalı Bilgisini Giriniz!");
                        _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "YLFakulteAdi" });
                    }
                    else _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "YLFakulteAdi" });
                    if (!kModel.YLOgrenciBolumID.HasValue)
                    {
                        _MmMessage.Messages.Add("Yüksek Lisans Eğitimi Program Bilgisini Seçiniz!");
                        _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "YLOgrenciBolumID" });
                    }
                    else _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "YLOgrenciBolumID" });
                    if (!kModel.YLNotSistemID.HasValue)
                    {
                        _MmMessage.Messages.Add("Yüksek Lisans Eğitimi Not Sistemi Bilgisini Seçiniz!");
                        _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "YLNotSistemID" });
                    }
                    else
                    {
                        _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "YLNotSistemID" });

                        var notSistemi = Management.getNotSistemi(kModel.YLNotSistemID.Value);
                        var nots = db.NotSistemleris.Where(p => p.NotSistemID == notSistemi.NotSistemID).First();
                        if (!kModel.YLMezuniyetNotu.HasValue)
                        {
                            _MmMessage.Messages.Add("Yüksek Lisans Eğitimi Not Bilgisini Giriniz!");
                            _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "YLMezuniyetNotu" });

                        }
                        else if (!((double)nots.MinNot <= kModel.YLMezuniyetNotu.Value && (double)nots.MaxNot >= kModel.YLMezuniyetNotu))
                        {

                            _MmMessage.Messages.Add("Yüksek Lisans Eğitimi Notu " + notSistemi.NotSistemAdi + " not sistemine göre " + nots.MinNot.ToString() + " ile " + nots.MaxNot.ToString() + " arasında bir değer olmalıdır!");
                            _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "YLMezuniyetNotu" });
                        }
                        else _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "YLMezuniyetNotu" });

                    }
                    if (kModel.YLDurum && BasvuruSurec.BasvuruSurecTipID == BasvuruSurecTipi.YatayGecisBasvuru)
                    {
                        if (!kModel.DRUniversiteID.HasValue)
                        {
                            _MmMessage.Messages.Add("Doktora Lisans Eğitimi Üniversite Bilgisini Seçiniz!");
                            _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "DRUniversiteID" });
                        }
                        else _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "DRUniversiteID" });

                        if (kModel.DRFakulteAdi.IsNullOrWhiteSpace())
                        {
                            _MmMessage.Messages.Add("Doktora Lisans Eğitimi Anabilim Dalı Bilgisini Giriniz!<");
                            _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "DRFakulteAdi" });
                        }
                        else _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "DRFakulteAdi" });
                        if (!kModel.DROgrenciBolumID.HasValue)
                        {
                            _MmMessage.Messages.Add("Doktora Lisans Eğitimi Program Bilgisini Seçiniz!");
                            _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "DROgrenciBolumID" });
                        }
                        else _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "DROgrenciBolumID" });
                        if (!kModel.DRNotSistemID.HasValue)
                        {
                            _MmMessage.Messages.Add("Doktora Lisans Eğitimi Not Sistemi Bilgisini Seçiniz!");
                            _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "DRNotSistemID" });
                        }
                        else
                        {
                            _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "DRNotSistemID" });

                            var notSistemi = Management.getNotSistemi(kModel.DRNotSistemID.Value);
                            var nots = db.NotSistemleris.Where(p => p.NotSistemID == notSistemi.NotSistemID).First();
                            if (!kModel.DRMezuniyetNotu.HasValue)
                            {
                                _MmMessage.Messages.Add("Doktora Lisans Eğitimi Not Bilgisini Giriniz!");
                                _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "DRMezuniyetNotu" });

                            }
                            else if (!((double)nots.MinNot <= kModel.DRMezuniyetNotu.Value && (double)nots.MaxNot >= kModel.DRMezuniyetNotu))
                            {

                                _MmMessage.Messages.Add("Doktora Lisans Eğitimi Notu " + notSistemi.NotSistemAdi + " not sistemine göre " + nots.MinNot.ToString() + " ile " + nots.MaxNot.ToString() + " arasında bir değer olmalıdır!");
                                _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "DRMezuniyetNotu" });
                            }
                            else _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "DRMezuniyetNotu" });

                        }
                    }
                }

                if (kModel.TercihSayisi > 0 || _MmMessage.Messages.Count == 0)
                {

                    _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Nothing, PropertyName = "OgrenimTipKod" });
                    _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Nothing, PropertyName = "SubOgrenimTipKod" });
                    _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Nothing, PropertyName = "BolumID" });
                    _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Nothing, PropertyName = "ProgramKod" });
                    _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "trch_SiraNo" });
                }
            }
            return _MmMessage;
        }
        public static MmMessage programAgnoMinControl(kmBasvuru kModel, List<CmbIntDto> ProgramBilgi)
        {
            var _MmMessage = new MmMessage();
            using (var db = new LisansustuBasvuruSistemiEntities())
            {

                var bsurec = db.BasvuruSurecs.Where(p => p.BasvuruSurecID == kModel.BasvuruSurecID).First();
                if (bsurec.AGNOGirisBaslangicTarihi.HasValue == false || kModel.LOgrenimDurumID != OgrenimDurum.HalenOğrenci)
                {

                    var prBilgi = new List<kontenjanProgramBilgiModel>();
                    foreach (var item in ProgramBilgi)
                    {
                        var kotPmodel = Management.getKontenjanProgramBilgi(item.Caption, item.Value.Value, kModel.BasvuruSurecID, kModel.KullaniciTipID.Value, kModel.LOgrenimDurumID, kModel.LUniversiteID);
                        prBilgi.Add(kotPmodel);
                    }
                    var maxAgnoFirstPr = prBilgi.Where(p => p.MinAgno.HasValue).OrderByDescending(o => o.MinAgno).FirstOrDefault();
                    if (maxAgnoFirstPr != null)
                    {
                        var agnoYuzluk = 0.0;
                        bool lProp = false;
                        bool ylProp = false;
                        if (maxAgnoFirstPr.BasvuruAgnoAlimTipID.HasValue && new List<int> { OgrenimTipi.Doktra, OgrenimTipi.ButunlesikDoktora }.Contains(maxAgnoFirstPr.OgrenimTipKod))
                        {
                            if (maxAgnoFirstPr.BasvuruAgnoAlimTipID.Value == BasvuruAgnoAlimTipi.LisansAlinsin)
                            {
                                agnoYuzluk = kModel.LMezuniyetNotu.Value.ToNotCevir(kModel.LNotSistemID.Value).Not100Luk;
                                lProp = true;
                            }
                            else if (maxAgnoFirstPr.BasvuruAgnoAlimTipID.Value == BasvuruAgnoAlimTipi.YLisansAlinsin)
                            {
                                agnoYuzluk = kModel.YLMezuniyetNotu.Value.ToNotCevir(kModel.YLNotSistemID.Value).Not100Luk;
                                ylProp = true;
                            }
                            else if (maxAgnoFirstPr.BasvuruAgnoAlimTipID.Value == BasvuruAgnoAlimTipi.L_YLYuzdeBelirlensin)
                            {
                                var LagnoYuzluk = kModel.LMezuniyetNotu.Value.ToNotCevir(kModel.LNotSistemID.Value).Not100Luk;
                                var YagnoYuzluk = kModel.YLMezuniyetNotu.Value.ToNotCevir(kModel.YLNotSistemID.Value).Not100Luk;
                                agnoYuzluk = ((maxAgnoFirstPr.LYuzdeOran.Value * LagnoYuzluk) / 100.00) + ((maxAgnoFirstPr.YLYuzdeOran.Value * YagnoYuzluk) / 100.00);
                                lProp = true;
                                ylProp = true;
                            }
                        }
                        else
                        {
                            if (ProgramBilgi != null && ProgramBilgi.Count == 1 && kModel.OgrenimTipKod == 0) kModel.OgrenimTipKod = ProgramBilgi.First().Value ?? 0;

                            if (new List<int> { OgrenimTipi.Doktra }.Contains(kModel.OgrenimTipKod))
                            {

                                agnoYuzluk = kModel.YLMezuniyetNotu.Value.ToNotCevir(kModel.YLNotSistemID.Value).Not100Luk;
                                ylProp = true;
                            }
                            else
                            {

                                agnoYuzluk = kModel.LMezuniyetNotu.Value.ToNotCevir(kModel.LNotSistemID.Value).Not100Luk;
                                lProp = true;
                            }
                        }
                        if (maxAgnoFirstPr.MinAgno.Value > agnoYuzluk)
                        {
                            _MmMessage.Messages.Add(maxAgnoFirstPr.MinAgnoAciklama);
                            if (lProp) _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "LMezuniyetNotu" });
                            if (ylProp) _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "YLMezuniyetNotu" });

                        }
                    }
                }
            }
            return _MmMessage;
        }
        public static MmMessage stKontrol(int BasvuruSurecID, List<int> OgrenimTipKods, List<bool> Ingilizces, int SinavTipID, int? SinavYil, int? SinavDilID, string SinavDonem, string WsXmlData, List<string> ProgramKods, DateTime? SinavTarihi, DateTime BasvuruTarihi, int? SubSinavAralikID, double? BasvuruSurecSubNot, double SinavNotu, bool? IsTaahhutVar = false, bool IsEgitimDiliTurkce = false)
        {
            var _MmMessage = new MmMessage();

            var mdlSinavTip = Management.getSinavBilgisi(BasvuruSurecID, SinavTipID, OgrenimTipKods, ProgramKods, Ingilizces);
            var IdN = (mdlSinavTip.SinavTipGrupID == SinavTipGrup.Ales_Gree ? "A" : (mdlSinavTip.SinavTipGrupID == SinavTipGrup.DilSinavlari ? "D" : "T"));

            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                if (mdlSinavTip.WebService)
                {

                    krSinavTipleriDonems bsSdonemBilgi = null;
                    if (SinavDonem.IsNullOrWhiteSpace())
                    {
                        string msg = mdlSinavTip.SinavAdi + " Sınavı için Dönem seçiniz!";
                        _MmMessage.Messages.Add(msg);
                        _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "BasvurularSinavBilgi_" + IdN + ".WsSinavDonem" });
                    }
                    else
                    {
                        var _yil = SinavDonem.Split('~')[0].ToInt().Value;


                        bsSdonemBilgi = mdlSinavTip.SinavTipleriDonems.Where(a => a.Yil == _yil).FirstOrDefault();
                        if (bsSdonemBilgi == null)
                        {

                            Management.SistemBilgisiKaydet("BasvuruSurecID:" + BasvuruSurecID + "\n SinavTipID:" + SinavTipID + "\n Yil:" + _yil + "\n Bilgisi sistemde bulunamadı! Konsoldan müdahale olabilir!", "Basvuru/getSinavTipSonuc", BilgiTipi.Saldırı);
                            _yil = 0001;

                            _MmMessage.Messages.Add(mdlSinavTip.SinavAdi + " Sınavı için sınav sonucu bilgisi alınamadı! Lütfen bilgileri doğru girdiğinizden emin olunuz.");
                            _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "BasvurularSinavBilgi_" + IdN + ".WsSinavDonem" });
                        }
                        else
                            _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "BasvurularSinavBilgi_" + IdN + ".WsSinavDonem" });
                        var SinavNotuIstenmesin = bsSdonemBilgi == null || !(bsSdonemBilgi.IsTaahhutVar && IsTaahhutVar == true);
                        if (SinavNotuIstenmesin && WsXmlData.IsNullOrWhiteSpace())
                        {
                            _MmMessage.Messages.Add(mdlSinavTip.SinavAdi + " Sınavı için sınav sonucu bilgisi alınamadı! Lütfen bilgileri doğru girdiğinizden emin olunuz.");
                            _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "BasvurularSinavBilgi_" + IdN + ".WsSonucID" });
                        }
                        else { _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "BasvurularSinavBilgi_" + IdN + ".WsSonucID" }); }
                    }

                    if (_MmMessage.Messages.Count == 0)
                    {


                        if (mdlSinavTip.SinavTipGrupID == SinavTipGrup.Ales_Gree)
                        {
                            var output = WsXmlData.toSinavSonucAlesXmlModel();
                            var Programlars = db.Programlars.Where(p => ProgramKods.Contains(p.ProgramKod)).ToList();
                            double tepmNot = 0;
                            var _min = mdlSinavTip.Min.Value;
                            var _max = mdlSinavTip.Max.Value;
                            foreach (var item in Programlars)
                            {
                                if (item.AlesNotuYuksekOlanAlinsin && item.AnabilimDallari.EnstituKod == EnstituKodlari.SosyalBilimleri)
                                {
                                    var maxNot = new Dictionary<int, double>();
                                    var bsurec = db.BasvuruSurecs.Where(p => p.BasvuruSurecID == BasvuruSurecID).First();
                                    if (bsurec.EnstituKod == EnstituKodlari.SosyalBilimleri)
                                    {
                                        if (item.ProgramlarAlesEslesmeleris.Any(a => a.AlesTipID == AlesTipBilgi.Sayısal)) maxNot.Add(AlesTipBilgi.Sayısal, output.SAY_PUAN.ToDouble().Value.ToString("n2").ToDouble().Value);
                                        if (item.ProgramlarAlesEslesmeleris.Any(a => a.AlesTipID == AlesTipBilgi.Sözel)) maxNot.Add(AlesTipBilgi.Sözel, output.SOZ_PUAN.ToDouble().Value.ToString("n2").ToDouble().Value);
                                        if (item.ProgramlarAlesEslesmeleris.Any(a => a.AlesTipID == AlesTipBilgi.EşitAğırlık)) maxNot.Add(AlesTipBilgi.EşitAğırlık, output.EA_PUAN.ToDouble().Value.ToString("n2").ToDouble().Value);

                                    }
                                    else
                                    {
                                        maxNot.Add(AlesTipBilgi.Sözel, output.SOZ_PUAN.ToDouble().Value.ToString("n2").ToDouble().Value);
                                        maxNot.Add(AlesTipBilgi.EşitAğırlık, output.EA_PUAN.ToDouble().Value.ToString("n2").ToDouble().Value);

                                    }
                                    tepmNot = maxNot.Select(s => s.Value).Max();
                                    if (tepmNot < _min || tepmNot > _max)
                                    {
                                        _MmMessage.Messages.Add(item.ProgramAdi + " Programı tercihi için " + item.AlesTipleri.AlesTipAdi + " sınav notu " + _min.ToString() + " ile " + _max.ToString() + " notları arasında olması gerekmektedir!");
                                        if (mdlSinavTip.NotDonusum) _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "BasvurularSinavBilgi_" + IdN + ".BasvuruSurecSubNot" });
                                        else _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "BasvurularSinavBilgi_" + IdN + ".SinavNotu" });
                                    }
                                    else
                                    {
                                        if (mdlSinavTip.NotDonusum) _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "BasvurularSinavBilgi_" + IdN + ".BasvuruSurecSubNot" });
                                        else _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "BasvurularSinavBilgi_" + IdN + ".SinavNotu" });
                                    }
                                }
                                else
                                {
                                    if (item.AlesTipID == AlesTipBilgi.Sayısal)
                                    {
                                        tepmNot = output.SAY_PUAN.ToDouble().Value.ToString("n2").ToDouble().Value;
                                    }
                                    else if (item.AlesTipID == AlesTipBilgi.EşitAğırlık)
                                    {
                                        tepmNot = output.EA_PUAN.ToDouble().Value.ToString("n2").ToDouble().Value;
                                    }
                                    else if (item.AlesTipID == AlesTipBilgi.Sözel)
                                    {
                                        tepmNot = output.SOZ_PUAN.ToDouble().Value.ToString("n2").ToDouble().Value;

                                    }
                                    if (tepmNot < _min || tepmNot > _max)
                                    {
                                        _MmMessage.Messages.Add(item.ProgramAdi + " Programı tercihi için " + item.AlesTipleri.AlesTipAdi + " sınav notu " + _min.ToString() + " ile " + _max.ToString() + " notları arasında olması gerekmektedir!");
                                        if (mdlSinavTip.NotDonusum) _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "BasvurularSinavBilgi_" + IdN + ".BasvuruSurecSubNot" });
                                        else _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "BasvurularSinavBilgi_" + IdN + ".SinavNotu" });
                                    }
                                    else
                                    {
                                        if (mdlSinavTip.NotDonusum) _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "BasvurularSinavBilgi_" + IdN + ".BasvuruSurecSubNot" });
                                        else _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "BasvurularSinavBilgi_" + IdN + ".SinavNotu" });
                                    }
                                }


                            }

                        }
                        else
                        {
                            var SinavNotuIstensin = !(bsSdonemBilgi.IsTaahhutVar && IsTaahhutVar == true);

                            if (SinavNotuIstensin && SinavNotu <= 0)
                            {
                                _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "BasvurularSinavBilgi_" + IdN + ".WsSinavYil" });
                                _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "BasvurularSinavBilgi_" + IdN + ".WsSinavDonem" });
                                _MmMessage.Messages.Add(mdlSinavTip.SinavAdi + " Sınavı için sınav sonucu bilgisi alınamadı! Lütfen bilgileri doğru girdiğinizden emin olunuz.");
                            }
                            else if (SinavNotuIstensin)
                            {
                                var _min = mdlSinavTip.Min.Value;
                                var _max = mdlSinavTip.Max.Value;
                                var _not = mdlSinavTip.NotDonusum ? BasvuruSurecSubNot.Value : SinavNotu;
                                if (_not < _min || _not > _max)
                                {
                                    _MmMessage.Messages.Add(mdlSinavTip.SinavAdi + " Sınavı için Not girişi " + _min.ToString() + " ile " + _max.ToString() + " notları arasında olması gerekmektedir");
                                    if (mdlSinavTip.NotDonusum) _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "BasvurularSinavBilgi_" + IdN + ".BasvuruSurecSubNot" });
                                    else _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "BasvurularSinavBilgi_" + IdN + ".SinavNotu" });
                                }
                                else
                                {
                                    if (mdlSinavTip.NotDonusum) _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "BasvurularSinavBilgi_" + IdN + ".BasvuruSurecSubNot" });
                                    else _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "BasvurularSinavBilgi_" + IdN + ".SinavNotu" });
                                }
                            }

                        }

                    }

                }
                else
                {
                    var IsTarihNotIstensin = true;
                    if (mdlSinavTip.SinavTipGrupID == SinavTipGrup.Tomer && IsTaahhutVar == true) // Tomer sınavı ve taahhüt var ise
                    {
                        // mezuniyet türkçe eğitimli ise  ve türkçe programa başvurmuşsa veya türkçe programa başvuru yoksa sınav tarihi istenmeyecek.
                        if ((Ingilizces.Any(a => !a) && IsEgitimDiliTurkce) || !Ingilizces.Any(a => !a))
                        {
                            IsTarihNotIstensin = false;
                        }
                    }
                    if (IsTarihNotIstensin)
                        if (!SinavTarihi.HasValue)
                        {
                            _MmMessage.Messages.Add(mdlSinavTip.SinavAdi + " Sınavı için sınav tarihi seçiniz!");
                            _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "BasvurularSinavBilgi_" + IdN + ".SinavTarihi" });
                        }
                        else
                        {
                            if (mdlSinavTip.OzelTarih)
                            {
                                if (mdlSinavTip.OzelTarihTipID == OzelTarihTip.BelirliTarhidenSonrasi)
                                {
                                    if (SinavTarihi.Value < mdlSinavTip.Tarih1.Value)
                                    {
                                        _MmMessage.Messages.Add(mdlSinavTip.SinavAdi + " Sınavı için sınav tarihi " + mdlSinavTip.Tarih1.Value.ToDateString() + " tarihi ve sonraki sınavlar kabul edilebilir!");
                                        _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "BasvurularSinavBilgi_" + IdN + ".SinavTarihi" });

                                    }
                                    else _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "BasvurularSinavBilgi_" + IdN + ".SinavTarihi" });
                                }
                                else if (mdlSinavTip.OzelTarihTipID == OzelTarihTip.BelirliTarihdenOncesi)
                                {
                                    if (SinavTarihi.Value > mdlSinavTip.Tarih1.Value)
                                    {
                                        _MmMessage.Messages.Add(mdlSinavTip.SinavAdi + " Sınavı için sınav tarihi " + mdlSinavTip.Tarih1.Value.ToDateString() + " tarihi ve önceki sınavlar kabul edilebilir!");
                                        _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "BasvurularSinavBilgi_" + IdN + ".SinavTarihi" });

                                    }
                                    else _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "BasvurularSinavBilgi_" + IdN + ".SinavTarihi" });

                                }
                                else if (mdlSinavTip.OzelTarihTipID == OzelTarihTip.IkiTarihArasi)
                                {
                                    if (!(SinavTarihi.Value >= mdlSinavTip.Tarih1.Value && SinavTarihi.Value <= mdlSinavTip.Tarih2.Value))
                                    {
                                        _MmMessage.Messages.Add(mdlSinavTip.SinavAdi + " Sınavı için sınav tarihi " + mdlSinavTip.Tarih1.Value.ToDateString() + " ile " + mdlSinavTip.Tarih2.Value.ToDateString() + " tarih aralığındaki sınavlar kabul edilebilir!");
                                        _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "BasvurularSinavBilgi_" + IdN + ".SinavTarihi" });

                                    }
                                    else _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "BasvurularSinavBilgi_" + IdN + ".SinavTarihi" });
                                }
                                else if (mdlSinavTip.OzelTarihTipID == OzelTarihTip.BelirliTarihler)
                                {
                                    if (!mdlSinavTip.BasvuruSurecSinavTarihleris.Any(a => a.SinavTarihi == SinavTarihi))
                                    {
                                        _MmMessage.Messages.Add(mdlSinavTip.SinavAdi + " Sınavı için sınav tarihi " + mdlSinavTip.Tarih1.Value.ToDateString() + " tarihi ve önceki sınavlar kabul edilebilir!");
                                        _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "BasvurularSinavBilgi_" + IdN + ".SinavTarihi" });

                                    }
                                    else _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "BasvurularSinavBilgi_" + IdN + ".SinavTarihi" });
                                }
                            }
                            else
                            {
                                var nSinavT = BasvuruTarihi.TodateToShortDate().AddYears(-mdlSinavTip.TarihGirisMaxGecmisYil.Value);
                                if (SinavTarihi.Value < nSinavT)
                                {
                                    _MmMessage.Messages.Add(mdlSinavTip.SinavAdi + " Sınavı için sınav tarihi Başvuru tarihinden itibaren maksimum " + mdlSinavTip.TarihGirisMaxGecmisYil + " Yıl öncesine kadar geçerlidir!");
                                    _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "BasvurularSinavBilgi_" + IdN + ".SinavTarihi" });

                                }
                                else if ((IsTaahhutVar == null || IsTaahhutVar.Value == false) && SinavTarihi.Value > DateTime.Now.TodateToShortDate())
                                {
                                    _MmMessage.Messages.Add(mdlSinavTip.SinavAdi + " Sınavı için sınav tarihi " + DateTime.Now.ToDateString() + " tarihi ve önceki sınavlar kabul edilebilir!");

                                    _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "BasvurularSinavBilgi_" + IdN + ".SinavTarihi" });

                                }
                                else _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "BasvurularSinavBilgi_" + IdN + ".SinavTarihi" });
                            }

                        }

                    if (mdlSinavTip.SinavTipGrupID != SinavTipGrup.Ales_Gree && mdlSinavTip.SinavTipGrupID != SinavTipGrup.Tomer)
                    {
                        if (SinavDilID.HasValue == false)
                        {
                            _MmMessage.Messages.Add(mdlSinavTip.SinavAdi + " Sınavı için sinav dili seçiniz!");
                            _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "BasvurularSinavBilgi_" + IdN + ".SinavDilID" });
                        }
                        else _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "BasvurularSinavBilgi_" + IdN + ".SinavDilID" });


                    }

                    var SinavNotuIstensin = !(mdlSinavTip.GIsTaahhutVar && IsTaahhutVar == true);
                    if (mdlSinavTip.SinavTipGrupID == SinavTipGrup.Tomer) SinavNotuIstensin = IsTarihNotIstensin;
                    if (SinavNotuIstensin)
                    {
                        if (mdlSinavTip.OzelNot)
                        {
                            if (mdlSinavTip.OzelNotTipID == OzelNotTip.SeciliNotAraliklari && SubSinavAralikID.HasValue == false)
                            {
                                _MmMessage.Messages.Add(mdlSinavTip.SinavAdi + " Sınavı için puan sistemi seçiniz!");
                                _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "BasvurularSinavBilgi_" + IdN + ".SubSinavAralikID" });
                            }
                            else if (mdlSinavTip.OzelNotTipID == OzelNotTip.SeciliNotlar)
                            {
                                if (mdlSinavTip.BasvuruSurecSinavNotlaris.Any(a => a.SinavNotDeger == SinavNotu) == false)
                                {
                                    _MmMessage.Messages.Add(mdlSinavTip.SinavAdi + " Sınavı için Sınavı için girdiğiniz sınav notu  listedeki sınav notlarından hiçbiri ile eşleşmiyor! Lütfen listedeki notlardan birini seçiniz.");
                                    _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "BasvurularSinavBilgi_" + IdN + ".SinavNotu" });
                                }
                                else _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "BasvurularSinavBilgi_" + IdN + ".SinavNotu" });
                            }
                            else
                            {
                                _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "BasvurularSinavBilgi_" + IdN + ".SubSinavAralikID" });
                                if (!BasvuruSurecSubNot.HasValue || BasvuruSurecSubNot <= 0)
                                {
                                    _MmMessage.Messages.Add(mdlSinavTip.SinavAdi + " Sınavı için sınav notu giriniz!");
                                    _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "BasvurularSinavBilgi_" + IdN + ".BasvuruSurecSubNot" });
                                }
                                else if (BasvuruSurecSubNot.HasValue && BasvuruSurecSubNot > 0)
                                {
                                    var notK = mdlSinavTip.BasvuruSurecSinavTiplerSubSinavAraliks.Where(p => p.SubSinavAralikID == SubSinavAralikID).First();
                                    var _min = notK.SubSinavMin;
                                    var _max = notK.SubSinavMax;
                                    if (BasvuruSurecSubNot < _min || BasvuruSurecSubNot > _max)
                                    {
                                        _MmMessage.Messages.Add(mdlSinavTip.SinavAdi + " Sınavı için Not girişi " + _min.ToString() + " ile " + _max.ToString() + " notları arasında olması gerekmektedir");
                                        _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "BasvurularSinavBilgi_" + IdN + ".BasvuruSurecSubNot" });
                                    }
                                    else _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "BasvurularSinavBilgi_" + IdN + ".BasvuruSurecSubNot" });
                                }
                                else _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "BasvurularSinavBilgi_" + IdN + ".BasvuruSurecSubNot" });
                                if (SinavNotu <= 0)
                                {
                                    _MmMessage.Messages.Add(mdlSinavTip.SinavAdi + " Sınavı için sınav notu giriniz!");
                                    _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "BasvurularSinavBilgi_" + IdN + ".BasvuruSurecSubNot" });
                                }
                                else _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "BasvurularSinavBilgi_" + IdN + ".BasvuruSurecSubNot" });
                            }
                        }

                        if (SinavNotu <= 0)
                        {
                            _MmMessage.Messages.Add(mdlSinavTip.SinavAdi + " Sınavı için sınav notu giriniz!");
                            _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "BasvurularSinavBilgi_" + IdN + ".SinavNotu" });
                        }
                        else
                        {

                            var _min = mdlSinavTip.Min.Value;
                            var _max = mdlSinavTip.Max.Value;
                            var _not = mdlSinavTip.NotDonusum ? BasvuruSurecSubNot.Value : SinavNotu;
                            if (_not < _min || _not > _max)
                            {
                                _MmMessage.Messages.Add(mdlSinavTip.SinavAdi + " Sınavı için Not girişi " + _min.ToString() + " ile " + _max.ToString() + " notları arasında olması gerekmektedir");
                                if (mdlSinavTip.NotDonusum) _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "BasvurularSinavBilgi_" + IdN + ".BasvuruSurecSubNot" });
                                else _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "BasvurularSinavBilgi_" + IdN + ".SinavNotu" });
                            }
                            else
                            {
                                if (mdlSinavTip.NotDonusum) _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "BasvurularSinavBilgi_" + IdN + ".BasvuruSurecSubNot" });
                                else _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "BasvurularSinavBilgi_" + IdN + ".SinavNotu" });
                            }

                            if (SinavNotu <= 0)
                            {
                                if (mdlSinavTip.NotDonusum) _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "BasvurularSinavBilgi_" + IdN + ".BasvuruSurecSubNot" });
                                else _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "BasvurularSinavBilgi_" + IdN + ".SinavNotu" });

                            }
                        }

                    }
                }

            }


            return _MmMessage;
        }

        public static MmMessage TezKontrol(kmMezuniyetBasvuru kModel)
        {
            var _MmMessage = new MmMessage();
            if (!kModel.IsTezDiliTr.HasValue)
            {
                string msg = "Tez dilini seçiniz.";
                _MmMessage.Messages.Add(msg);

                _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "IsTezDiliTr" });
            }
            else _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "IsTezDiliTr" });
            if (kModel.TezBaslikTr.IsNullOrWhiteSpace())
            {
                _MmMessage.Messages.Add("Tez Başlığını Türkçe Olarak Giriniz.");

                _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "TezBaslikTr" });
            }
            else _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "TezBaslikTr" });
            if (kModel.TezBaslikEn.IsNullOrWhiteSpace())
            {
                _MmMessage.Messages.Add("Tez Başlığını İngilizce Olarak Giriniz.");

                _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "TezBaslikEn" });
            }
            else _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "TezBaslikEn" });

            if (kModel.TezDanismanUnvani.IsNullOrWhiteSpace())
            {
                _MmMessage.Messages.Add("Tez Danışman Unvanı Seçiniz.");
                _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "TezDanismanUnvani" });
            }
            if (kModel.TezDanismanAdi.IsNullOrWhiteSpace())
            {
                _MmMessage.Messages.Add("Tez Danışman Adı Giriniz.");
                _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "TezDanismanAdi" });
            }
            if (!kModel.TezDanismanAdi.IsNullOrWhiteSpace() && !kModel.TezDanismanUnvani.IsNullOrWhiteSpace())
            {
                _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "TezDanismanAdi" });
                _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "TezDanismanUnvani" });
            }


            if (!kModel.TezEsDanismanAdi.IsNullOrWhiteSpace() || !kModel.TezEsDanismanUnvani.IsNullOrWhiteSpace() || !kModel.TezEsDanismanEMail.IsNullOrWhiteSpace())
            {
                if (kModel.TezEsDanismanUnvani.IsNullOrWhiteSpace())
                {
                    _MmMessage.Messages.Add("Tez Eş Danışman Unvanı Seçiniz.");
                    _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "TezEsDanismanUnvani" });
                }
                else _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "TezEsDanismanUnvani" });

                if (kModel.TezEsDanismanAdi.IsNullOrWhiteSpace())
                {
                    _MmMessage.Messages.Add("Tez Eş Danışman Adı Giriniz.");
                    _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "TezEsDanismanAdi" });
                }
                else _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "TezEsDanismanAdi" });
                if (kModel.TezEsDanismanEMail.IsNullOrWhiteSpace())
                {
                    _MmMessage.Messages.Add("Eş Danışman E-Posta Bilgisini Giriniz.");
                    _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "TezEsDanismanEMail" });

                }
                else if (kModel.TezEsDanismanEMail.ToIsValidEmail())
                {
                    _MmMessage.Messages.Add("Lütfen E-Posta Adres Tekrarını Uygun Formatta Giriniz.");
                    _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "TezEsDanismanEMail" });

                }
                else _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "TezEsDanismanEMail" });
            }
            else
            {
                _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Nothing, PropertyName = "TezEsDanismanAdi" });
                _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Nothing, PropertyName = "TezEsDanismanUnvani" });
                _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Nothing, PropertyName = "TezEsDanismanEMail" });
            }
            if (kModel.TezOzet.IsNullOrWhiteSpace())
            {
                _MmMessage.Messages.Add("Tez Özetini Türkçe Olarak Giriniz.");
                _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "TezOzet" });
            }
            else _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "TezOzet" });
            if (kModel.OzetAnahtarKelimeler.IsNullOrWhiteSpace())
            {
                _MmMessage.Messages.Add("Tez Özeti Anahtar Kelimelerini Türkçe Olarak Giriniz.");
                _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "OzetAnahtarKelimeler" });
            }
            else _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "OzetAnahtarKelimeler" });
            if (kModel.TezAbstract.IsNullOrWhiteSpace())
            {
                _MmMessage.Messages.Add("Tez Özetini İngilizce Olarak Giriniz.");
                _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "TezAbstract" });
            }
            else _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "TezAbstract" });
            if (kModel.AbstractAnahtarKelimeler.IsNullOrWhiteSpace())
            {
                _MmMessage.Messages.Add("Tez Özeti Anahtar Kelimelerini İngilizce Olarak Giriniz.");
                _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "AbstractAnahtarKelimeler" });
            }
            else _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "AbstractAnahtarKelimeler" });
            var dTipCultureEk = kModel.OgrenimTipKod == OgrenimTipi.TezliYuksekLisans ? "YL" : "DR";

            return _MmMessage;
        }

        public static bool MezuniyetSureciOgrenimTipUygunMu(int MezuniyetSurecID, int KullaniciID)
        {
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var kul = db.Kullanicilars.Where(p => p.KullaniciID == KullaniciID).First();
                return db.MezuniyetSureciYonetmelikleris.Any(p => p.MezuniyetSurecID == MezuniyetSurecID && p.MezuniyetSureciYonetmelikleriOTs.Any(a => a.OgrenimTipKod == kul.OgrenimTipKod && a.IsGecerli));
            }
        }
        public static MezuniyetSureciYonetmelikleri MezuniyetAktifYonetmelik(int MezuniyetSurecID, int KullaniciID, int? MezuniyetBasvurulariID)
        {
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                decimal baslangic = 0;
                if (MezuniyetBasvurulariID > 0)
                {
                    var MBasvuru = db.MezuniyetBasvurularis.Where(p => p.MezuniyetBasvurulariID == MezuniyetBasvurulariID).First();
                    baslangic = Convert.ToDecimal(MBasvuru.KayitOgretimYiliBaslangic + "," + MBasvuru.KayitOgretimYiliDonemID.Value);
                }
                else
                {
                    var kul = db.Kullanicilars.Where(p => p.KullaniciID == KullaniciID).First();
                    baslangic = Convert.ToDecimal(kul.KayitYilBaslangic + "," + kul.KayitDonemID.Value);
                }
                var kriter = db.MezuniyetSureciYonetmelikleris.Include("MezuniyetSureciYonetmelikleriOTs").Where(p => p.MezuniyetSurecID == MezuniyetSurecID).ToList().First(f =>
                                                                             f.TarihKriterID == TarihKriterSecim.SecilenTarihVeOncesi ?
                                                                                 (Convert.ToDecimal(f.BaslangicYil + "," + f.DonemID) >= baslangic)
                                                                                   :
                                                                                 (f.TarihKriterID == TarihKriterSecim.SecilenTarihVeSonrasi ?
                                                                                       (Convert.ToDecimal(f.BaslangicYil + "," + f.DonemID) <= baslangic)
                                                                                      :
                                                                                       (
                                                                                           (Convert.ToDecimal(f.BaslangicYil + "," + f.DonemID) <= baslangic && Convert.ToDecimal(f.BaslangicYilB + "," + f.DonemIDB) >= baslangic)
                                                                                       )
                                                                                 )

                                                                       );
                return kriter;
            }
        }
        public static List<MezuniyetSureciYonetmelikleriOT> MezuniyetAktifOTYayinB(int MezuniyetSurecID, int KullaniciID)
        {
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var kul = db.Kullanicilars.Where(p => p.KullaniciID == KullaniciID).First();
                var baslangic = Convert.ToDecimal(kul.KayitYilBaslangic + "," + kul.KayitDonemID.Value);
                var kriter = db.MezuniyetSureciYonetmelikleris.Where(p => p.MezuniyetSurecID == MezuniyetSurecID).ToList().First(f =>
                                                                             f.TarihKriterID == TarihKriterSecim.SecilenTarihVeOncesi ?
                                                                                 (Convert.ToDecimal(f.BaslangicYil + "," + f.DonemID) >= baslangic)
                                                                                   :
                                                                                 (f.TarihKriterID == TarihKriterSecim.SecilenTarihVeSonrasi ?
                                                                                       (Convert.ToDecimal(f.BaslangicYil + "," + f.DonemID) <= baslangic)
                                                                                      :
                                                                                       (
                                                                                           (Convert.ToDecimal(f.BaslangicYil + "," + f.DonemID) <= baslangic && Convert.ToDecimal(f.BaslangicYilB + "," + f.DonemIDB) >= baslangic)
                                                                                       )
                                                                                 )

                                                                       );
                var ots = kriter.MezuniyetSureciYonetmelikleriOTs.Where(p => p.OgrenimTipKod == kul.OgrenimTipKod).ToList();
                return ots;
            }
        }
        public static MmMessage YayinKontrol(kmMezuniyetBasvuru kModel)
        {
            var _MmMessage = new MmMessage();
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                //var kriterSecim=db.MezuniyetSureciYonetmelikleris.Where(p=>p.BaslangicYil)
                var kriter = MezuniyetAktifYonetmelik(kModel.MezuniyetSurecID, kModel.KullaniciID, kModel.MezuniyetBasvurulariID);
                var yturAds = db.MezuniyetYayinTurleris.ToList();
                var kul = db.Kullanicilars.Where(p => p.KullaniciID == kModel.KullaniciID).First();
                var kriterDetay = (from s in kriter.MezuniyetSureciYonetmelikleriOTs.Where(p => p.OgrenimTipKod == kul.OgrenimTipKod).ToList()
                                   join yta in yturAds on s.MezuniyetYayinTurID equals yta.MezuniyetYayinTurID
                                   group new { s.MezuniyetYayinTurID, yta.MezuniyetYayinTurAdi, s.OgrenimTipKod, s.IsGecerli, s.IsZorunlu, s.GrupKodu, s.IsVeOrVeya } by new { IsZorunlu = s.IsZorunlu, IsGrup = s.GrupKodu.IsNullOrWhiteSpace() == false, s.GrupKodu } into g1
                                   select new
                                   {
                                       g1.Key.IsGrup,
                                       g1.Key.GrupKodu,
                                       g1.Key.IsZorunlu,
                                       data = g1.ToList()
                                   }).ToList();


                var qYbaslik = kModel._YayinBasligi.Select((s, inx) => new { YayinBasligi = s, Index = inx }).ToList();
                var qYtarih = kModel._MezuniyetYayinTarih.Select((s, inx) => new { MezuniyetYayinTarih = s, Index = inx }).ToList();
                var qMytID = kModel._MezuniyetYayinTurID.Select((s, inx) => new { MezuniyetYayinTurID = s, Index = inx }).ToList();
                var qMybelge = kModel._MezuniyetYayinBelgesiAdi.Select((s, inx) => new { MezuniyetYayinBelgesiAdi = s, Index = inx }).ToList();
                var qMkLink = kModel._MezuniyetYayinKaynakLinki.Select((s, inx) => new { MezuniyetYayinKaynakLinki = s, Index = inx }).ToList();
                var qMbelge = kModel._YayinMetniBelgesiAdi.Select((s, inx) => new { YayinMetniBelgesiAdi = s, Index = inx }).ToList();
                var qMyLink = kModel._MezuniyetYayinLinki.Select((s, inx) => new { MezuniyetYayinLinki = s, Index = inx }).ToList();
                var qIndex = kModel._MezuniyetYayinIndexTurID.Select((s, inx) => new { MezuniyetYayinIndexTurID = s, Index = inx }).ToList();

                var qYayins = (from b in qYbaslik
                               join myt in qMytID on b.Index equals myt.Index
                               join yt in qYtarih on b.Index equals yt.Index
                               join yta in yturAds on myt.MezuniyetYayinTurID equals yta.MezuniyetYayinTurID
                               join myb in qMybelge on b.Index equals myb.Index
                               join mkl in qMkLink on b.Index equals mkl.Index
                               join mb in qMbelge on b.Index equals mb.Index
                               join myl in qMyLink on b.Index equals myl.Index
                               join mI in qIndex on b.Index equals mI.Index
                               select new
                               {
                                   b.Index,
                                   myt.MezuniyetYayinTurID,
                                   yta.MezuniyetYayinTurAdi,
                                   yt.MezuniyetYayinTarih,
                                   myb.MezuniyetYayinBelgesiAdi,
                                   mkl.MezuniyetYayinKaynakLinki,
                                   mb.YayinMetniBelgesiAdi,
                                   myl.MezuniyetYayinLinki,
                                   mI.MezuniyetYayinIndexTurID
                               }).ToList();
                if (kriterDetay.Any(p => p.IsZorunlu) == false && qYbaslik.Count > 0)
                {
                    _MmMessage.Messages.Add("Mezuniyet başvurunuz için herhangi bir yayın bilgisi istenmemektedir. Yayın bilgisi ekleyemezsiniz.");
                    _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "MezuniyetYayinTurID" });
                }
                else
                {
                    foreach (var item in kriterDetay)
                    {
                        if (item.IsZorunlu)
                        {
                            if (item.IsGrup)
                            {
                                var contains = item.data.Select(s => new { s.MezuniyetYayinTurID, s.MezuniyetYayinTurAdi }).ToList();

                                if (!qYayins.Any(a => contains.Any(a2 => a2.MezuniyetYayinTurID == a.MezuniyetYayinTurID)) && kModel.YayinBilgisi == null)
                                {
                                    _MmMessage.Messages.Add(string.Join(", ", contains.Select(s => s.MezuniyetYayinTurAdi)) + " Yayın Türlerinden birinin eklenmesi gerekmektedir.");
                                    _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "MezuniyetYayinTurID" });
                                }
                                else if (qYayins.Count(a => contains.Any(a2 => a2.MezuniyetYayinTurID == a.MezuniyetYayinTurID)) > 1)
                                {
                                    _MmMessage.Messages.Add(string.Join(", ", contains.Select(s => s.MezuniyetYayinTurAdi)) + " Yayın Türlerinden sadece biri eklenebilir.");
                                    _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "MezuniyetYayinTurID" });
                                }
                            }
                            else
                            {
                                foreach (var item2 in item.data)
                                {
                                    if (!qYayins.Any(a => item2.MezuniyetYayinTurID == a.MezuniyetYayinTurID) && kModel.YayinBilgisi == null)
                                    {

                                        _MmMessage.Messages.Add(item2.MezuniyetYayinTurAdi + " Yayın türünün eklenmesi gerekmektedir.");
                                        _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "MezuniyetYayinTurID" });
                                    }

                                    else if (qYayins.Count(a => item2.MezuniyetYayinTurID == a.MezuniyetYayinTurID) > 1)
                                    {
                                        _MmMessage.Messages.Add(item2.MezuniyetYayinTurAdi + " Yayın Türünden sadece 1 adet eklenebilir.");
                                        _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "MezuniyetYayinTurID" });
                                    }
                                }

                            }
                        }
                        else
                        {
                            foreach (var itemY in qYayins)
                            {
                                if (item.data.Any(a => a.MezuniyetYayinTurID == itemY.MezuniyetYayinTurID && a.IsZorunlu == false))
                                {
                                    _MmMessage.Messages.Add(itemY.MezuniyetYayinTurAdi + " Yayın kabul edilmemektedir. Bu Yayın bilgisini eklenemez.");
                                    _MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "MezuniyetYayinTurID" });
                                }
                            }
                        }
                    }
                }

            }
            return _MmMessage;
        }


        public static void SistemBilgisiKaydet(Exception ex, byte BilgiTipi)
        {
            using (var db = new LisansustuBasvuruSistemiEntities())
            {


                db.SistemBilgilendirmes.Add(new SistemBilgilendirme
                {
                    BilgiTipi = BilgiTipi,
                    Message = ex.ToExceptionMessage(),
                    IslemYapanID = UserIdentity.Current != null && UserIdentity.Current.IsAuthenticated && UserIdentity.Current.Id > 0 ? UserIdentity.Current.Id : (int?)null,
                    IslemYapanIP = UserIdentity.Ip,
                    IslemTarihi = DateTime.Now,
                    StackTrace = ex.ToExceptionStackTrace()
                });
                db.SaveChanges();
            }
        }

        public static void SistemBilgisiKaydet(Exception ex, string Message, byte BilgiTipi)
        {
            using (var db = new LisansustuBasvuruSistemiEntities())
            {


                db.SistemBilgilendirmes.Add(new SistemBilgilendirme
                {
                    BilgiTipi = BilgiTipi,
                    Message = Message,
                    IslemYapanID = UserIdentity.Current != null && UserIdentity.Current.IsAuthenticated && UserIdentity.Current.Id > 0 ? UserIdentity.Current.Id : (int?)null,
                    IslemYapanIP = UserIdentity.Ip,
                    IslemTarihi = DateTime.Now,
                    StackTrace = ex.ToExceptionStackTrace()
                });
                db.SaveChanges();
            }
        }
        public static void SistemBilgisiKaydet(string Mesaj, string StakTrace, byte BilgiTipi)
        {
            using (var db = new LisansustuBasvuruSistemiEntities())
            {


                db.SistemBilgilendirmes.Add(new SistemBilgilendirme
                {
                    Message = Mesaj,
                    BilgiTipi = BilgiTipi,
                    IslemYapanID = UserIdentity.Current != null && UserIdentity.Current.IsAuthenticated && UserIdentity.Current.Id > 0 ? UserIdentity.Current.Id : (int?)null,
                    IslemYapanIP = UserIdentity.Ip,
                    IslemTarihi = DateTime.Now,
                    StackTrace = StakTrace
                });
                db.SaveChanges();
            }
        }

        public static void SistemBilgisiKaydet(string Mesaj, string StakTrace, byte BilgiTipi, int? KullaniciID, string KullaniciIP)
        {
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                db.SistemBilgilendirmes.Add(new SistemBilgilendirme
                {
                    Message = Mesaj,
                    BilgiTipi = BilgiTipi,
                    IslemYapanID = KullaniciID,
                    IslemYapanIP = !KullaniciIP.IsNullOrWhiteSpace() ? KullaniciIP : UserIdentity.Ip,
                    IslemTarihi = DateTime.Now,
                    StackTrace = StakTrace
                });
                db.SaveChanges();
            }
        }
        public static bool ResimBilgisiLazimOlanKayitVarMi(int KullaniciID)
        {
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                return db.Basvurulars.Where(p => p.KullaniciID == KullaniciID).Any() || db.MezuniyetBasvurularis.Any(a => a.KullaniciID == KullaniciID);
            }

        }
        public static string ResimKaydet(HttpPostedFileBase Resim)
        {
            try
            {
                string mimeType = Resim.ContentType;
                Stream fileStream = Resim.InputStream;
                Bitmap bmp = new Bitmap(fileStream);

                string folderName = SistemAyar.KullaniciResimYolu;
                bool RotasYonDegisimLog = SistemAyar.getAyar(SistemAyar.RotasyonuDegisenResimleriLogla).ToBoolean().Value;
                bool Boyutlandirma = SistemAyar.getAyar(SistemAyar.KullaniciResimKaydiBoyutlandirma).ToBoolean().Value;
                bool KaliteOpt = SistemAyar.getAyar(SistemAyar.KullaniciResimKaydiKaliteOpt).ToBoolean().Value;
                string ResimAdi = Resim.FileName.ToFileNameAddGuid();
                var ResimYolu = Path.Combine(System.Web.HttpContext.Current.Server.MapPath("~/" + folderName), ResimAdi);


                if (Boyutlandirma)
                {
                    try
                    {
                        var uzn = SistemAyar.getAyar(SistemAyar.KullaniciResimKaydiHeightPx);
                        var gens = SistemAyar.getAyar(SistemAyar.KullaniciResimKaydiWidthPx);

                        int Uzunluk = uzn.IsNullOrWhiteSpace() ? 560 : uzn.ToInt().Value;
                        int Genislik = gens.IsNullOrWhiteSpace() ? 560 : gens.ToInt().Value;
                        var img = bmp.resizeImage(new Size(Genislik, Uzunluk));
                        img.Save(ResimYolu, ImageFormat.Jpeg);
                    }
                    catch (Exception ex)
                    {
                        Management.SistemBilgisiKaydet(ex, "Resmin boyutlandırma işlemi yapılıp kayıt edilirken bir hata oluştu.\r\n Hata:" + ex.ToExceptionMessage(), BilgiTipi.OnemsizHata);
                    }
                }
                else
                {
                    bmp.Save(ResimYolu, ImageFormat.Jpeg);
                }

                if (KaliteOpt)
                {
                    #region Quality check
                    try
                    {

                        Bitmap bmp_Q = new Bitmap(ResimYolu);

                        ImageCodecInfo jpgEncoder = GetEncoder(ImageFormat.Jpeg);


                        Int64 quality = 100L;
                        if (Resim.ContentLength >= 80000 && Resim.ContentLength < 200000) quality = 80;
                        else if (Resim.ContentLength >= 200000 && Resim.ContentLength < 400000) quality = 70;
                        else if (Resim.ContentLength >= 400000 && Resim.ContentLength < 600000) quality = 60;
                        else if (Resim.ContentLength >= 600000 && Resim.ContentLength < 800000) quality = 50;
                        else if (Resim.ContentLength >= 800000 && Resim.ContentLength < 1000000) quality = 40;
                        else if (Resim.ContentLength >= 1000000) quality = 30;
                        System.Drawing.Imaging.Encoder myEncoder = System.Drawing.Imaging.Encoder.Quality;
                        var path2 = ResimYolu + Guid.NewGuid().ToString().Substr(0, 4).ToString() + ".jpg";
                        EncoderParameters myEncoderParameters = new EncoderParameters(1);
                        EncoderParameter myEncoderParameter = new EncoderParameter(myEncoder, quality);
                        myEncoderParameters.Param[0] = myEncoderParameter;
                        bmp_Q.Save(path2, jpgEncoder, myEncoderParameters);
                        bmp_Q.Dispose();
                        if (File.Exists(ResimYolu))
                            File.Delete(ResimYolu);
                        var imgTmp = Image.FromFile(path2);
                        imgTmp.Save(ResimYolu, ImageFormat.Jpeg);
                        imgTmp.Dispose();
                        File.Delete(path2);
                    }
                    catch (Exception errQuality)
                    {
                        Management.SistemBilgisiKaydet(errQuality, "Resmin kalitesi değiştirilirken hata oluştu.\r\n Hata:" + errQuality.ToExceptionMessage(), BilgiTipi.OnemsizHata);
                    }
                    #endregion
                }

                #region Rotation
                try
                {

                    Image img1 = Image.FromFile(ResimYolu);
                    var prop = img1.PropertyItems.Where(p => p.Id == 0x0112).FirstOrDefault();
                    if (prop != null)
                    {
                        int orientationValue = img1.GetPropertyItem(prop.Id).Value[0];
                        RotateFlipType rotateFlipType = GetOrientationToFlipType(orientationValue);
                        img1.RotateFlip(rotateFlipType);
                        var path2 = ResimYolu + Guid.NewGuid().ToString().Substr(0, 4).ToString() + ".jpg";
                        img1.Save(path2);
                        img1.Dispose();
                        if (File.Exists(ResimYolu))
                            File.Delete(ResimYolu);
                        var imgTmp = Image.FromFile(path2);
                        imgTmp.Save(ResimYolu, ImageFormat.Jpeg);
                        imgTmp.Dispose();
                        File.Delete(path2);
                        if (RotasYonDegisimLog)
                        {
                            Management.SistemBilgisiKaydet("Rotasyon farklılığı görünen resim düzeltildi! Resim:" + ResimYolu, "Management/resimKaydet", BilgiTipi.Bilgi);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Management.SistemBilgisiKaydet(ex, "Hesap kayıt sırasında resim rotasyonu yapılırken bir hata oluştu.\r\n Hata:" + ex.ToExceptionMessage(), BilgiTipi.OnemsizHata);
                }
                #endregion


                return ResimAdi;
            }
            catch (Exception ex)
            {
                Management.SistemBilgisiKaydet("Resim kaydedilirken bir hata oluştu! Hata: " + ex.ToExceptionMessage(), ex.ToExceptionStackTrace(), BilgiTipi.Hata, null, UserIdentity.Ip);
                return null;
            }
        }

        private static RotateFlipType GetOrientationToFlipType(int orientationValue)
        {
            RotateFlipType rotateFlipType = RotateFlipType.RotateNoneFlipNone;

            switch (orientationValue)
            {
                case 1:
                    rotateFlipType = RotateFlipType.RotateNoneFlipNone;
                    break;
                case 2:
                    rotateFlipType = RotateFlipType.RotateNoneFlipX;
                    break;
                case 3:
                    rotateFlipType = RotateFlipType.Rotate180FlipNone;
                    break;
                case 4:
                    rotateFlipType = RotateFlipType.Rotate180FlipX;
                    break;
                case 5:
                    rotateFlipType = RotateFlipType.Rotate90FlipX;
                    break;
                case 6:
                    rotateFlipType = RotateFlipType.Rotate90FlipNone;
                    break;
                case 7:
                    rotateFlipType = RotateFlipType.Rotate270FlipX;
                    break;
                case 8:
                    rotateFlipType = RotateFlipType.Rotate270FlipNone;
                    break;
                default:
                    rotateFlipType = RotateFlipType.RotateNoneFlipNone;
                    break;
            }

            return rotateFlipType;
        }


        public static void VaryQualityLevel(string path)
        {
            // Get a bitmap.
            Bitmap bmp1 = new Bitmap(path);
            ImageCodecInfo jpgEncoder = GetEncoder(ImageFormat.Jpeg);

            // Create an Encoder object based on the GUID
            // for the Quality parameter category.
            System.Drawing.Imaging.Encoder myEncoder =
                System.Drawing.Imaging.Encoder.Quality;

            // Create an EncoderParameters object.
            // An EncoderParameters object has an array of EncoderParameter
            // objects. In this case, there is only one
            // EncoderParameter object in the array.
            EncoderParameters myEncoderParameters = new EncoderParameters(1);

            EncoderParameter myEncoderParameter = new EncoderParameter(myEncoder, 50L);
            myEncoderParameters.Param[0] = myEncoderParameter;
            bmp1.Save(@"d:\TestPhotoQualityFifty.jpg", jpgEncoder, myEncoderParameters);

            myEncoderParameter = new EncoderParameter(myEncoder, 100L);
            myEncoderParameters.Param[0] = myEncoderParameter;
            bmp1.Save(@"d:\TestPhotoQualityHundred.jpg", jpgEncoder, myEncoderParameters);

            // Save the bitmap as a JPG file with zero quality level compression.
            myEncoderParameter = new EncoderParameter(myEncoder, 0L);
            myEncoderParameters.Param[0] = myEncoderParameter;
            bmp1.Save(@"d:\TestPhotoQualityZero.jpg", jpgEncoder, myEncoderParameters);

        }
        public static ImageCodecInfo GetEncoder(ImageFormat format)
        {

            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();

            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }
            return null;
        }

        public static int? getAktifBasvuruSurecID(string EnstituKod, int BasvuruSurecTipID, int? BasvuruSurecID = null, bool? IsMulakatDurum = null)
        {
            using (LisansustuBasvuruSistemiEntities db = new LisansustuBasvuruSistemiEntities())
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
        public static int? getAktifMulakatSurecID(string EnstituKod, int BasvuruSurecTipID, int? BasvuruSurecID = null, bool? IsMulakatDurum = null)
        {
            using (LisansustuBasvuruSistemiEntities db = new LisansustuBasvuruSistemiEntities())
            {

                var nowDate = DateTime.Now;
                var bf = db.BasvuruSurecs.Where(p => p.BasvuruSurecTipID == BasvuruSurecTipID && (p.BaslangicTarihi <= nowDate && p.BitisTarihi >= nowDate) && (p.EnstituKod == EnstituKod) && p.BasvuruSurecID == (BasvuruSurecID.HasValue ? BasvuruSurecID.Value : p.BasvuruSurecID));
                if (IsMulakatDurum.HasValue) bf = bf.Where(p => p.SonucGirisBaslangicTarihi.HasValue == IsMulakatDurum.Value);
                var qBf = bf.FirstOrDefault();
                int? ID = null;
                if (qBf != null) ID = qBf.BasvuruSurecID;
                return ID;
            }
        }
        public static int? getAktifTalepSurecID(string EnstituKod, int? TalepSurecID = null)
        {
            using (LisansustuBasvuruSistemiEntities db = new LisansustuBasvuruSistemiEntities())
            {

                var nowDate = DateTime.Now;
                var bf = db.TalepSurecleris.Where(p => (p.BaslangicTarihi <= nowDate && p.BitisTarihi >= nowDate) && p.IsAktif && (p.EnstituKod == EnstituKod) && p.TalepSurecID == (TalepSurecID.HasValue ? TalepSurecID.Value : p.TalepSurecID));
                var rTalepSurecID = bf.Select(s => s.TalepSurecID).FirstOrDefault();

                return rTalepSurecID > 0 ? rTalepSurecID : (int?)null;
            }
        }
        public static frTalepSurec GetTalepSurec(int TalepSurecID)
        {
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var nowDate = DateTime.Now;
                var xD = (from s in db.TalepSurecleris.Where(p => p.TalepSurecID == TalepSurecID)
                          join k in db.Kullanicilars on s.IslemYapanID equals k.KullaniciID
                          select new frTalepSurec
                          {
                              TalepSurecID = s.TalepSurecID,
                              EnstituKod = s.EnstituKod,
                              BaslangicTarihi = s.BaslangicTarihi,
                              BitisTarihi = s.BitisTarihi,
                              IsAktif = s.IsAktif,
                              IslemYapanID = s.IslemYapanID,
                              IslemYapan = k.KullaniciAdi,
                              IslemTarihi = s.IslemTarihi,
                              IslemYapanIP = s.IslemYapanIP,
                              AktifSurec = (s.BaslangicTarihi <= nowDate && s.BitisTarihi >= nowDate)
                          }).FirstOrDefault();
                return xD;
            }
        }
        public static int? getAktifBSMulakatDahilSurecID(string EnstituKod, int? BasvuruSurecID = null)
        {
            using (LisansustuBasvuruSistemiEntities db = new LisansustuBasvuruSistemiEntities())
            {

                var nowDate = DateTime.Now;
                var surecler = db.BasvuruSurecs.Where(p => p.EnstituKod == EnstituKod && p.SonucGirisBaslangicTarihi.HasValue && p.IsAktif && p.BasvuruSurecID == (BasvuruSurecID.HasValue ? BasvuruSurecID : p.BasvuruSurecID)).ToList();
                var bf = surecler.Where(p => p.BaslangicTarihi <= nowDate && p.SonucGirisBaslangicTarihi > nowDate);
                var qBf = bf.FirstOrDefault();
                int? ID = null;
                if (qBf != null) ID = qBf.BasvuruSurecID;
                return ID;
            }
        }
        public static int? getAktifMulakatSonucGiris(string EnstituKod, int? BasvuruSurecID = null)
        {
            using (LisansustuBasvuruSistemiEntities db = new LisansustuBasvuruSistemiEntities())
            {
                var nowDate = DateTime.Now;
                int? ID = null;
                var bs = db.BasvuruSurecs.Where(p => p.EnstituKod == EnstituKod && p.BasvuruSurecMulakatSinavTurleris.Any() && (p.SonucGirisBaslangicTarihi <= nowDate && p.SonucGirisBitisTarihi >= nowDate) && p.BasvuruSurecID == (BasvuruSurecID.HasValue ? BasvuruSurecID.Value : p.BasvuruSurecID)).FirstOrDefault();
                if (bs != null) ID = bs.BasvuruSurecID;
                return ID;
            }
        }

        public static int? getAktifMezuniyetSurecID(string EnstituKod, int? MezuniyetSurecID = null)
        {
            using (LisansustuBasvuruSistemiEntities db = new LisansustuBasvuruSistemiEntities())
            {

                var nowDate = DateTime.Now;
                var bf = db.MezuniyetSurecis.Where(p => (p.BaslangicTarihi <= nowDate && p.BitisTarihi >= nowDate) && p.IsAktif && (p.EnstituKod == EnstituKod) && p.MezuniyetSurecID == (MezuniyetSurecID.HasValue ? MezuniyetSurecID.Value : p.MezuniyetSurecID));
                var qBf = bf.FirstOrDefault();
                int? ID = null;
                if (qBf != null) ID = qBf.MezuniyetSurecID;
                return ID;
            }
        }

        public static MmMessage getBasvuruSilKontrol(int BasvuruID, int BasvuruSurecTipID)
        {
            var msg = new MmMessage();
            msg.IsSuccess = true;

            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var basvuru = db.Basvurulars.Where(p => p.BasvuruID == BasvuruID).FirstOrDefault();
                if (basvuru == null)
                {
                    msg.IsSuccess = false;
                    msg.Messages.Add("Aranan başvuru sistemde bulunamadı.");
                }
                else
                {
                    if (!UserIdentity.Current.EnstituKods.Contains(basvuru.BasvuruSurec.EnstituKod) && RoleNames.GelenBasvurularKayit.InRoleCurrent() && basvuru.KullaniciID != UserIdentity.Current.Id)
                    {
                        msg.IsSuccess = false;
                        msg.Messages.Add("Bu enstitüye ait başvuruyu silmeye yetkili değilsiniz!");
                        string message = "Bu enstitüye ait başvuruyu silmeye yetkili değilsiniz!\r\n Başvuru ID: " + basvuru.BasvuruID + " \r\n Başvuru sahibi: " + basvuru.Kullanicilar.Ad + " " + basvuru.Kullanicilar.Soyad + " \r\n Başvuru Tarihi: " + basvuru.BasvuruTarihi.ToString();
                        Management.SistemBilgisiKaydet(message, "Başvuru Sil", BilgiTipi.Kritik);
                    }
                    else if (!getAktifBasvuruSurecID(basvuru.BasvuruSurec.EnstituKod, BasvuruSurecTipID, basvuru.BasvuruSurecID).HasValue && UserIdentity.Current.IsAdmin == false)
                    {
                        msg.IsSuccess = false;
                        msg.Messages.Add("Başvuru süreci dolduğundan başvuru üzerinden herhangi bir işlem yapılamaz!");

                    }
                    else if (RoleNames.GelenBasvurularKayit.InRoleCurrent() == false && basvuru.KullaniciID != UserIdentity.Current.Id)
                    {
                        msg.IsSuccess = false;
                        msg.Messages.Add("Başka bir kullanıcıya ait başvuruyu silmeye hakkınız yoktur!");
                        SistemBilgisiKaydet("Başka bir kullanıcıya ait başvuruyu silmeye hakkınız yoktur! \r\n Silinmeye çalışılan Başvuru ID:" + basvuru.BasvuruID + " \r\n Başvuru Sahibi:" + basvuru.Kullanicilar.KullaniciAdi + " \r\n Başvuru Tarihi:" + basvuru.BasvuruTarihi.ToString(), "Başvuru Sil", BilgiTipi.Saldırı);
                    }
                    else if (RoleNames.GelenBasvurularKayit.InRoleCurrent() == false && (basvuru.BasvuruDurumID != BasvuruDurumu.Gonderildi && basvuru.BasvuruDurumID == BasvuruDurumu.Onaylandı))
                    {
                        msg.IsSuccess = false;
                        msg.Messages.Add("Başvuru durumu '" + basvuru.BasvuruDurumlari.BasvuruDurumAdi + "' olan başvurularda silme işlemi yapamazsınız!");
                    }
                    else if (RoleNames.GelenBasvurularKayit.InRoleCurrent() && basvuru.BasvuruDurumID == BasvuruDurumu.Gonderildi && UserIdentity.Current.IsAdmin == false)
                    {
                        msg.IsSuccess = false;
                        msg.Messages.Add("Başvuru durumu '" + basvuru.BasvuruDurumlari.BasvuruDurumAdi + "' olan başvurularda silme işlemi yapamazsınız!");
                        SistemBilgisiKaydet("Başvuru durumu '" + basvuru.BasvuruDurumlari.BasvuruDurumAdi + "' olan başvurularda silme işlemi yapamazsınız! \r\n Çağrılan Başvuru ID:" + basvuru.BasvuruID + " \r\n Başvuru Sahibi:" + basvuru.Kullanicilar.KullaniciAdi, "Başvuru Sil", BilgiTipi.Kritik);
                    }
                }
            }
            return msg;
        }
        public static MmMessage getAktifBasvurSurecKontrol(string EnstituKod, int BasvuruSurecTipID, int? KullaniciID, int? BasvuruID = null)
        {
            var msg = new MmMessage();
            msg.IsSuccess = true;
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                if (BasvuruID.HasValue)
                {
                    var basvuru = db.Basvurulars.Where(p => p.BasvuruSurec.BasvuruSurecTipID == BasvuruSurecTipID && p.BasvuruID == BasvuruID.Value).FirstOrDefault();
                    if (basvuru == null)
                    {
                        msg.IsSuccess = false;
                        msg.Messages.Add("Aranan başvuru sistemde bulunamadı.");
                        if (RoleNames.GelenBasvurularKayit.InRoleCurrent() == false) SistemBilgisiKaydet("Aranan başvuru sistemde bulunamadı! \r\n Çağrılan Başvuru ID:" + BasvuruID, "Başvuru Düzelt", BilgiTipi.Uyarı);
                    }
                    else
                    {
                        if (basvuru.BasvuruSurec.EnstituKod != EnstituKod)
                        {
                            SistemBilgisiKaydet("Seçilen başvuru Enstitü kodu ile aktif Enstitü kodu uyuşmuyor! \r\n Çağrılan Başvuru Enstitü Kod:" + basvuru.BasvuruSurec.EnstituKod + " \r\n Aktif Enstitü Kod:" + EnstituKod + " \r\n Çağrılan Başvuru ID:" + basvuru.BasvuruID + " \r\n Başvuru Sahibi:" + basvuru.Kullanicilar.KullaniciAdi, "Başvuru Düzelt", BilgiTipi.Uyarı);
                            EnstituKod = basvuru.BasvuruSurec.EnstituKod;
                        }
                        if (!UserIdentity.Current.EnstituKods.Contains(basvuru.BasvuruSurec.EnstituKod) && RoleNames.GelenBasvurularKayit.InRoleCurrent() && basvuru.KullaniciID != UserIdentity.Current.Id)
                        {
                            msg.IsSuccess = false;
                            msg.Messages.Add("Bu enstitüye ait başvuruyu güncellemeye yetkili değilsiniz!");
                            string message = "Bu enstitüye ait başvuruyu güncellemeye yetkili değilsiniz!\r\n Başvuru ID: " + basvuru.BasvuruID + " \r\n Başvuru sahibi: " + basvuru.Kullanicilar.Ad + " " + basvuru.Kullanicilar.Soyad + " \r\n Başvuru Tarihi: " + basvuru.BasvuruTarihi.ToString();
                            Management.SistemBilgisiKaydet(message, "Başvuru Düzelt", BilgiTipi.Saldırı);
                        }
                        else if (!getAktifBasvuruSurecID(EnstituKod, BasvuruSurecTipID, basvuru.BasvuruSurecID).HasValue && UserIdentity.Current.IsAdmin == false)
                        {
                            msg.IsSuccess = false;
                            msg.Messages.Add("Başvuru süreci dolduğundan başvuru üzerinden herhangi bir işlem yapılamaz!");

                        }
                        if (RoleNames.GelenBasvurularKayit.InRoleCurrent() == false && basvuru.KullaniciID != UserIdentity.Current.Id)
                        {
                            msg.IsSuccess = false;
                            msg.Messages.Add("Başka bir kullanıcıya ait başvuruyu düzenlemeye hakkınız yoktur!");
                            SistemBilgisiKaydet("Başka bir kullanıcıya ait başvuruyu düzenlemeye hakkınız yoktur! \r\n Çağrılan Başvuru ID:" + basvuru.BasvuruID + " \r\n Başvuru Sahibi:" + basvuru.Kullanicilar.KullaniciAdi, "Başvuru Düzelt", BilgiTipi.Saldırı);
                        }
                        else if (RoleNames.GelenBasvurularKayit.InRoleCurrent() && basvuru.MulakatSonuclaris.Any(a => a.KayitDurumID.HasValue && a.KayitDurumlari.IsKayitOldu == true) && UserIdentity.Current.IsAdmin == false)
                        {
                            msg.IsSuccess = false;
                            msg.Messages.Add("Başvuru durumu '" + basvuru.BasvuruDurumlari.BasvuruDurumAdi + "' olan başvurularda düzenleme işlemi yapamazsınız!");
                            SistemBilgisiKaydet("Başvuru durumu '" + basvuru.BasvuruDurumlari.BasvuruDurumAdi + "' olan başvurularda düzenleme işlemi yapamazsınız! \r\n Çağrılan Başvuru ID:" + basvuru.BasvuruID + " \r\n Başvuru Sahibi:" + basvuru.Kullanicilar.KullaniciAdi, "Başvuru Düzelt", BilgiTipi.Kritik);
                        }

                    }
                }
                else
                {
                    int? BasvuruSurecID = getAktifBasvuruSurecID(EnstituKod, BasvuruSurecTipID);
                    msg.IsSuccess = BasvuruSurecID.HasValue;
                    if (KullaniciID.HasValue == false) KullaniciID = UserIdentity.Current.Id;
                    else if (KullaniciID != UserIdentity.Current.Id && RoleNames.KullaniciAdinaBasvuruYap.InRoleCurrent() == false && UserIdentity.Current.IsAdmin == false)
                    {
                        SistemBilgisiKaydet("Başka bir kullanıcıya adına başvuru yapılmak isteniyor! \r\n Başvuru yapılmak istenen Kullanıcı ID:" + KullaniciID + " \r\n İşlem Yapan Kullanıcı ID:" + UserIdentity.Current.Id, "Başvury Yap", BilgiTipi.Saldırı);
                        KullaniciID = UserIdentity.Current.Id;
                    }
                    var kul = db.Kullanicilars.Where(p => p.KullaniciID == KullaniciID.Value).First();
                    if (msg.IsSuccess == false)
                    {
                        msg.Messages.Add("Sistem başvuru işlemlerine kapalıdır. Başvuru İşlemi ile ilgili detaylı bilgi almak için duyuruları takip edebilirsiniz.");
                    }
                    else if (!(kul.KullaniciTipleri.BasvuruYapabilir))
                    {
                        msg.IsSuccess = false;
                        msg.Messages.Add(kul.KullaniciTipleri + " kullanıcı tipi için başvuru işlemi kapalıdır.");
                    }
                    else
                    {
                        var BasvurularTercihleri = new List<BasvurularTercihleri>();
                        var bsSureci = db.BasvuruSurecs.Where(p => p.BasvuruSurecID == BasvuruSurecID).First();
                        if (bsSureci.Kota_BasvuruSurecKontrolTipID == KotaHesapTipleri.YilveDonemToplam) //aynı yıl aynı dönemdeki tüm başvurular baz alınsın
                            BasvurularTercihleri = db.BasvurularTercihleris.Where(p => p.Basvurular.BasvuruSurec.EnstituKod == bsSureci.EnstituKod && p.Basvurular.BasvuruSurec.BaslangicYil == bsSureci.BaslangicYil &&
                                                                        p.Basvurular.BasvuruSurec.BitisYil == bsSureci.BitisYil &&
                                                                        p.Basvurular.BasvuruSurec.DonemID == bsSureci.DonemID &&
                                                                        p.Basvurular.KullaniciID == KullaniciID &&
                                                                        (BasvuruID.HasValue ? p.Basvurular.BasvuruID != BasvuruID.Value : true) &&
                                                                        (p.Basvurular.BasvuruDurumID == BasvuruDurumu.Onaylandı || p.Basvurular.BasvuruDurumID == BasvuruDurumu.Gonderildi)
                                                                        ).ToList();

                        else
                            BasvurularTercihleri = db.BasvurularTercihleris.Where(p => p.Basvurular.BasvuruSurecID == BasvuruSurecID &&
                                                                        p.Basvurular.KullaniciID == KullaniciID && (BasvuruID.HasValue ? p.Basvurular.BasvuruID != BasvuruID.Value : true) &&
                                                                       (p.Basvurular.BasvuruDurumID == BasvuruDurumu.Onaylandı || p.Basvurular.BasvuruDurumID == BasvuruDurumu.Gonderildi)).ToList();//aynı başvuru sürecindeki başvurular baz alınsın

                        if (bsSureci.ToplamKota <= BasvurularTercihleri.Count)// toplam başvuru kontrol
                        {
                            msg.IsSuccess = false;
                            string mesaj = "";
                            var DonemAdi = bsSureci.BaslangicYil + "/" + bsSureci.BitisYil + " " + bsSureci.Donemler.DonemAdi;
                            if (bsSureci.Kota_BasvuruSurecKontrolTipID == KotaHesapTipleri.YilveDonemToplam)
                            {
                                mesaj = DonemAdi + " döneminde yapabileceğiniz başvuru limitini doldurdunuz. Yeni başvuru yapamazsınız!";
                            }
                            else
                            {
                                mesaj = "Bu başvuru sürecinde yapabileceğiniz başvuru limitini doldurdunuz. Yeni başvuru yapamazsınız!";

                            }
                            msg.Messages.Add(mesaj);


                        }

                    }
                }

            }
            return msg;

        }
        public static kmBasvuru getSecilenBasvuru(int BasvuruID)
        {
            var model = new kmBasvuru();
            using (var db = new LisansustuBasvuruSistemiEntities())
            {

                var basvuru = db.Basvurulars.Include("BasvuruDurumlari").Where(p => p.BasvuruID == BasvuruID).FirstOrDefault();
                var kul = db.Kullanicilars.Where(p => p.KullaniciID == basvuru.KullaniciID).First();

                string EKD = basvuru.BasvuruSurec.Enstituler.EnstituKisaAd;
                #region BasvuruBilgi
                model.EnstituKod = basvuru.BasvuruSurec.EnstituKod;
                model.KullaniciID = basvuru.KullaniciID;
                model.BasvuruID = basvuru.BasvuruID;
                model.BasvuruSurecID = basvuru.BasvuruSurecID;
                model.BasvuruTarihi = basvuru.BasvuruTarihi;
                model.BasvuruDurumID = basvuru.BasvuruDurumID;
                model.BasvuruDurumAciklamasi = basvuru.BasvuruDurumAciklamasi;
                model.KullaniciID = basvuru.KullaniciID;
                model.LEgitimDiliTurkce = basvuru.LEgitimDiliTurkce;
                model.YLEgitimDiliTurkce = basvuru.YLEgitimDiliTurkce;
                if (kul.KullaniciTipID != basvuru.KullaniciTipID)
                {
                    model.KullaniciTipID = kul.KullaniciTipID;
                    model.ResimAdi = kul.ResimAdi;
                    model.Ad = kul.Ad;
                    model.Soyad = kul.Soyad;
                    model.CinsiyetID = kul.CinsiyetID;
                    model.TcKimlikNo = kul.TcKimlikNo;
                    model.PasaportNo = kul.PasaportNo;
                    model.CepTel = kul.CepTel;
                    model.EMail = kul.EMail;
                    model.Adres = kul.Adres;

                }
                else
                {
                    model.KullaniciTipID = basvuru.KullaniciTipID;
                    model.ResimAdi = basvuru.ResimAdi;
                    model.Ad = basvuru.Ad;
                    model.Soyad = basvuru.Soyad;
                    model.CinsiyetID = basvuru.CinsiyetID;

                    model.IsTel = basvuru.IsTel;
                    model.EvTel = basvuru.EvTel;
                    model.CepTel = basvuru.CepTel;
                    model.EMail = basvuru.EMail;
                    model.Adres = basvuru.Adres;
                    model.Adres2 = basvuru.Adres2;
                }

                model.LUniversiteID = basvuru.LUniversiteID;
                model.LFakulteAdi = basvuru.LFakulteAdi;
                model.LOgrenciBolumID = basvuru.LOgrenciBolumID;
                model.LOgrenimDurumID = basvuru.LOgrenimDurumID;
                model.LBaslamaTarihi = basvuru.LBaslamaTarihi;
                model.LMezuniyetTarihi = basvuru.LMezuniyetTarihi;
                model.LNotSistemID = basvuru.LNotSistemID;
                model.LMezuniyetNotu = basvuru.LMezuniyetNotu;
                model.LMezuniyetNotu100LukSistem = basvuru.LMezuniyetNotu100LukSistem;
                model.YLUniversiteID = basvuru.YLUniversiteID;
                model.YLFakulteAdi = basvuru.YLFakulteAdi;
                model.YLOgrenciBolumID = basvuru.YLOgrenciBolumID;
                model.YLBaslamaTarihi = basvuru.YLBaslamaTarihi;
                model.YLMezuniyetTarihi = basvuru.YLMezuniyetTarihi;
                model.YLNotSistemID = basvuru.YLNotSistemID;
                model.YLMezuniyetNotu = basvuru.YLMezuniyetNotu;
                model.YLMezuniyetNotu100LukSistem = basvuru.YLMezuniyetNotu100LukSistem;
                model.DRUniversiteID = basvuru.DRUniversiteID;
                model.DRFakulteAdi = basvuru.DRFakulteAdi;
                model.DROgrenciBolumID = basvuru.DROgrenciBolumID;
                model.DRBaslamaTarihi = basvuru.DRBaslamaTarihi;
                model.DRMezuniyetTarihi = basvuru.DRMezuniyetTarihi;
                model.DRNotSistemID = basvuru.DRNotSistemID;
                model.DRMezuniyetNotu = basvuru.DRMezuniyetNotu;
                model.DRMezuniyetNotu100LukSistem = basvuru.DRMezuniyetNotu100LukSistem;
                model.IslemTarihi = DateTime.Now;
                model.IslemYapanID = UserIdentity.Current.Id;
                model.IslemYapanIP = UserIdentity.Ip;
                #endregion
                var ylb = db.BasvuruSurecOgrenimTipleris.Where(p => p.BasvuruSurecID == model.BasvuruSurecID && p.YLEgitimBilgisiIste).Select(s => s.OgrenimTipKod).ToList();
                model.YLDurum = basvuru.BasvurularTercihleris.Any(a => ylb.Contains(a.OgrenimTipKod));
                var qtercihler = new List<CmbMultyTypeDto>();
                var tercihler = (from s in basvuru.BasvurularTercihleris
                                 join at in db.AlanTipleris on s.AlanTipID equals at.AlanTipID
                                 join kt in db.BasvuruSurecKotalars.Where(p => p.BasvuruSurecID == basvuru.BasvuruSurecID) on new { s.OgrenimTipKod, s.ProgramKod } equals new { kt.OgrenimTipKod, kt.ProgramKod }
                                 select new basvuruTercihModel
                                 {
                                     BasvuruTercihID = s.BasvuruTercihID,
                                     BasvuruID = s.BasvuruID,
                                     SiraNo = s.SiraNo,
                                     Ingilizce = s.Programlar.Ingilizce,
                                     YlBilgiIste = basvuru.YLUniversiteID.HasValue,
                                     AlanTipID = at.AlanTipID,
                                     AlanTipAdi = at.AlanTipAdi,
                                     OgrenimTipKod = s.OgrenimTipKod,
                                     ProgramKod = s.ProgramKod,
                                     IsSecilenTercih = s.IsSecilenTercih,
                                 }).ToList();
                foreach (var item in tercihler)
                {
                    qtercihler.Add(new CmbMultyTypeDto { Value = item.OgrenimTipKod, ValueB = item.Ingilizce, ValueS2 = item.ProgramKod });
                    item.ProgramBilgileri = Management.getKontenjanProgramBilgi(item.ProgramKod, item.OgrenimTipKod, basvuru.BasvuruSurecID, basvuru.KullaniciTipID.Value, basvuru.LOgrenciBolumID, basvuru.LUniversiteID);
                    item.ProgramBilgileri.AlanTipID = item.AlanTipID;
                    item.Ingilizce = item.ProgramBilgileri.Ingilizce;

                }
                model.ODurumIstensin = basvuru.BasvuruSurec.AGNOGirisBaslangicTarihi.HasValue;
                model.BasvuruTercihleri = tercihler;
                model.AlesIstensinmi = Management.cmbGetdAktifSinavlar(qtercihler, model.BasvuruSurecID, SinavTipGrup.Ales_Gree, true).Count > 0;
                model.DilIstensinmi = Management.cmbGetdAktifSinavlar(qtercihler, model.BasvuruSurecID, SinavTipGrup.DilSinavlari, true).Count > 0;
                var TomerVar = Management.cmbGetdAktifSinavlar(qtercihler, model.BasvuruSurecID, SinavTipGrup.Tomer, true).Count > 0 && model.KullaniciTipID == KullaniciTipBilgi.YabanciOgrenci;
                model.TomerIstensinmi = TomerVar;
                model.LEgitimDiliIstensinMi = TomerVar;
                model.YLEgitimDiliIstensinMi = model.YLDurum && model.LEgitimDiliIstensinMi;
                if (model.AlesIstensinmi) model.BasvurularSinavBilgi_A = basvuru.BasvurularSinavBilgis.Where(p => p.SinavTipGrupID == SinavTipGrup.Ales_Gree).FirstOrDefault();
                if (model.DilIstensinmi) model.BasvurularSinavBilgi_D = basvuru.BasvurularSinavBilgis.Where(p => p.SinavTipGrupID == SinavTipGrup.DilSinavlari).FirstOrDefault();
                if (model.TomerIstensinmi) model.BasvurularSinavBilgi_T = basvuru.BasvurularSinavBilgis.Where(p => p.SinavTipGrupID == SinavTipGrup.Tomer).FirstOrDefault();
                if (basvuru.BasvuruDurumID == BasvuruDurumu.Onaylandı) model.Onaylandi = true;

            }
            return model;

        }



        public class RequestModel
        {
            public long Tckn { get; set; }
            public int ID { get; set; }
        }

        public static basvuruDetayModel getSecilenBasvuruDetay(int BasvuruID)
        {
            var model = new basvuruDetayModel();
            using (var db = new LisansustuBasvuruSistemiEntities())
            {

                var KulID = RoleNames.GelenBasvurular.InRoleCurrent() || RoleNames.MulakatSureci.InRoleCurrent() ? (int?)null : UserIdentity.Current.Id;



                var DilTr = "Türkçe";
                var DilEn = "İngilizce";
                #region BasvuruBilgi
                model = (from s in db.Basvurulars
                         join bs in db.BasvuruSurecs on s.BasvuruSurecID equals bs.BasvuruSurecID
                         join enst in db.Enstitulers on bs.EnstituKod equals enst.EnstituKod
                         join kt in db.KullaniciTipleris on s.KullaniciTipID equals kt.KullaniciTipID
                         join cns in db.Cinsiyetlers on s.CinsiyetID equals cns.CinsiyetID
                         join uy in db.Uyruklars on s.UyrukKod equals uy.UyrukKod
                         join dy in db.Sehirlers on s.DogumYeriKod equals dy.SehirKod
                         join ys in db.Sehirlers on s.SehirKod equals ys.SehirKod
                         join nf in db.Sehirlers on s.NufusilIlceKod equals nf.SehirKod into defNf
                         from NF in defNf.DefaultIfEmpty()
                         join k in db.Kullanicilars on s.KullaniciID equals k.KullaniciID
                         join lu in db.Universitelers on s.LUniversiteID equals lu.UniversiteID
                         join lob in db.OgrenciBolumleris on s.LOgrenciBolumID equals lob.OgrenciBolumID
                         join lns in db.NotSistemleris on s.LNotSistemID equals lns.NotSistemID
                         join od in db.OgrenimDurumlaris on s.LOgrenimDurumID equals od.OgrenimDurumID into defod
                         from OD in defod.DefaultIfEmpty()

                         join ylu in db.Universitelers on s.YLUniversiteID equals ylu.UniversiteID into defylu
                         from YLU in defylu.DefaultIfEmpty()
                         join ylob in db.OgrenciBolumleris on s.YLOgrenciBolumID equals ylob.OgrenciBolumID into defylob
                         from YLOB in defylob.DefaultIfEmpty()
                         join ylns in db.NotSistemleris on s.YLNotSistemID equals ylns.NotSistemID into defylsn
                         from YLSN in defylsn.DefaultIfEmpty()

                         join dru in db.Universitelers on s.DRUniversiteID equals dru.UniversiteID into defdru
                         from DRU in defdru.DefaultIfEmpty()
                         join drob in db.OgrenciBolumleris on s.DROgrenciBolumID equals drob.OgrenciBolumID into defyDro
                         from DROB in defyDro.DefaultIfEmpty()
                         join drns in db.NotSistemleris on s.DRNotSistemID equals drns.NotSistemID into defdrsn
                         from DRSN in defdrsn.DefaultIfEmpty()
                         where s.BasvuruID == BasvuruID && s.KullaniciID == (KulID ?? s.KullaniciID)
                         select new basvuruDetayModel
                         {
                             BasvuruSurecTipID = bs.BasvuruSurecTipID,
                             IsYerli = kt.Yerli,
                             EnstituAdi = enst.EnstituAd,
                             BasvuruID = s.BasvuruID,
                             BasvuruSurec = bs,
                             BasvuruSurecID = s.BasvuruSurecID,
                             KullaniciID = s.KullaniciID,
                             BasvuruTarihi = s.BasvuruTarihi,
                             BasvuruDurumID = s.BasvuruDurumID,
                             BasvuruDurumAciklamasi = s.BasvuruDurumAciklamasi,
                             KullaniciTipID = s.KullaniciTipID,
                             ResimAdi = k.ResimAdi,
                             Ad = s.Ad,
                             Soyad = s.Soyad,
                             CinsiyetID = s.CinsiyetID,
                             AnaAdi = s.AnaAdi,
                             BabaAdi = s.BabaAdi,
                             DogumYeriKod = s.DogumYeriKod,
                             DogumTarihi = s.DogumTarihi,
                             NufusilIlceKod = s.NufusilIlceKod,
                             CiltNo = s.CiltNo,
                             AileNo = s.AileNo,
                             SiraNo = s.SiraNo,
                             TcKimlikNo = s.TcKimlikNo,
                             PasaportNo = s.PasaportNo,
                             UyrukKod = s.UyrukKod,
                             SehirKod = s.SehirKod,
                             IsTel = s.IsTel,
                             CepTel = s.CepTel,
                             EvTel = s.EvTel,
                             EMail = s.EMail,
                             Adres = s.Adres,
                             Adres2 = s.Adres2,
                             LUniversiteID = s.LUniversiteID,
                             LUniversiteAdi = lu.Ad,
                             LFakulteAdi = s.LFakulteAdi,
                             LOgrenimDurumID = s.LOgrenimDurumID,
                             OgrenimDurumAdi = OD.OgrenimDurumAdi,
                             LEgitimDiliTurkce = s.LEgitimDiliTurkce,
                             LegitimDilAdi = s.LEgitimDiliTurkce.HasValue ? (s.LEgitimDiliTurkce.Value ? DilTr : DilEn) : "",
                             LOgrenciBolumID = s.LOgrenciBolumID,
                             LBolumAdi = lob.BolumAdi,
                             LBaslamaTarihi = s.LBaslamaTarihi,
                             LMezuniyetTarihi = s.LMezuniyetTarihi,
                             LNotSistemID = s.LNotSistemID,
                             LNotSistemi = lns.NotSistemAdi,
                             LMezuniyetNotu = s.LMezuniyetNotu,
                             LMezuniyetNotu100LukSistem = s.LMezuniyetNotu100LukSistem,
                             YLUniversiteID = s.YLUniversiteID,
                             YLUniversiteAdi = YLU.Ad,
                             YLFakulteAdi = s.YLFakulteAdi,
                             YLOgrenciBolumID = s.YLOgrenciBolumID,
                             YLBolumAdi = YLOB.BolumAdi,
                             YLBaslamaTarihi = s.YLBaslamaTarihi,
                             YLMezuniyetTarihi = s.YLMezuniyetTarihi,
                             YLNotSistemID = s.YLNotSistemID,
                             YLNotSistemi = YLSN.NotSistemAdi,
                             YLMezuniyetNotu = s.YLMezuniyetNotu,
                             YLMezuniyetNotu100LukSistem = s.YLMezuniyetNotu100LukSistem,
                             YLEgitimDiliTurkce = s.YLEgitimDiliTurkce,
                             YLegitimDilAdi = s.YLEgitimDiliTurkce.HasValue ? (s.YLEgitimDiliTurkce.Value ? DilTr : DilEn) : "",

                             DRUniversiteID = s.DRUniversiteID,
                             DRUniversiteAdi = DRU.Ad,
                             DRFakulteAdi = s.DRFakulteAdi,
                             DROgrenciBolumID = s.DROgrenciBolumID,
                             DRBolumAdi = DROB.BolumAdi,
                             DRBaslamaTarihi = s.DRBaslamaTarihi,
                             DRMezuniyetTarihi = s.DRMezuniyetTarihi,
                             DRNotSistemID = s.DRNotSistemID,
                             DRNotSistemi = DRSN.NotSistemAdi,
                             DRMezuniyetNotu = s.DRMezuniyetNotu,
                             DRMezuniyetNotu100LukSistem = s.DRMezuniyetNotu100LukSistem,
                             DREgitimDiliTurkce = s.DREgitimDiliTurkce,
                             DRegitimDilAdi = s.DREgitimDiliTurkce.HasValue ? (s.DREgitimDiliTurkce.Value ? DilTr : DilEn) : "",


                             Kapatildi = s.Kapatildi,
                             KapatmaAciklamasi = s.KapatmaAciklamasi,
                             IslemTarihi = s.IslemTarihi,
                             KullaniciTipAdi = kt.KullaniciTipAdi,
                             Cinsiyet = cns.CinsiyetAdi,

                             UyrukAdi = uy.Ad,
                             DogumYeriAdi = dy.Ad,
                             YasadigiSehirAdi = ys.Ad,
                             NufusIlIcleAdi = NF.Ad,
                             RowID = s.RowID,
                         }

                    ).First();
                #endregion
                var basvuru = db.Basvurulars.Where(p => p.BasvuruID == BasvuruID && p.KullaniciID == (KulID ?? p.KullaniciID)).First();
                var bsurec = model.BasvuruSurec;



                var AlesIstensinmi = basvuru.BasvurularSinavBilgis.Any(a => a.SinavTipGrupID == SinavTipGrup.Ales_Gree);
                var DilIstensinmi = basvuru.BasvurularSinavBilgis.Any(a => a.SinavTipGrupID == SinavTipGrup.DilSinavlari);
                var TomerVar = basvuru.BasvurularSinavBilgis.Any(a => a.SinavTipGrupID == SinavTipGrup.Tomer);
                var TomerIstensinmi = TomerVar;

                var Atipleris = db.AlanTipleris.ToList();
                var tercihler = (from s in basvuru.BasvurularTercihleris
                                 join at in Atipleris on s.AlanTipID equals at.AlanTipID
                                 select new basvuruTercihModel
                                 {
                                     BasvuruTercihID = s.BasvuruTercihID,
                                     BasvuruID = s.BasvuruID,
                                     UniqueID = s.UniqueID,
                                     SiraNo = s.SiraNo,
                                     Ingilizce = s.Programlar.Ingilizce,
                                     YlBilgiIste = basvuru.YLUniversiteID.HasValue,
                                     AlanTipID = at.AlanTipID,
                                     AlanTipAdi = at.AlanTipAdi,
                                     OgrenimTipKod = s.OgrenimTipKod,
                                     ProgramKod = s.ProgramKod,
                                     IsSecilenTercih = s.IsSecilenTercih,
                                     IsAsilOrYedek = s.MulakatSonuclaris.Any(a => new List<int> { MulakatSonucTipi.Asil, MulakatSonucTipi.Yedek }.Contains(a.MulakatSonucTipID)),
                                     ProgramBilgileri = new kontenjanProgramBilgiModel(),
                                     IsLagnoOrYlAgnoAlinsin = s.OgrenimTipKod == OgrenimTipi.Doktra ? (s.Programlar.BasvuruAgnoAlimTipID.HasValue ? s.Programlar.BasvuruAgnoAlimTipID.Value == BasvuruAgnoAlimTipi.LisansAlinsin : false) : true,
                                 }).ToList();
                foreach (var item in tercihler)
                {

                    item.ProgramBilgileri = Management.getKontenjanProgramBilgi(item.ProgramKod, item.OgrenimTipKod, basvuru.BasvuruSurecID, basvuru.KullaniciTipID.Value, basvuru.LOgrenciBolumID, basvuru.LUniversiteID);
                    item.ProgramBilgileri.AlanTipID = item.AlanTipID;

                }
                model.IsLAgnoOrYLAgnoAlinsin = !tercihler.Any(a => a.IsLagnoOrYlAgnoAlinsin == false);
                model.BasvuruTercihleri = tercihler;

                #region Sinavlar

                var otKods = tercihler.Select(s => s.OgrenimTipKod).ToList();
                var Ings = tercihler.Select(s => s.Ingilizce).ToList();
                var prks = tercihler.Select(s => s.ProgramKod).ToList();
                if (AlesIstensinmi)
                {
                    model.BasvurularSinavBilgi_A.Sinav = basvuru.BasvurularSinavBilgis.Where(p => p.SinavTipGrupID == SinavTipGrup.Ales_Gree).FirstOrDefault();
                    model.BasvurularSinavBilgi_A.SinavDetay = Management.getSinavBilgisi(model.BasvuruSurecID, model.BasvurularSinavBilgi_A.Sinav.SinavTipID, otKods, prks, Ings);
                    model.BasvurularSinavBilgi_A.BasvuruTarihi = model.BasvuruTarihi;
                }
                if (DilIstensinmi)
                {
                    model.BasvurularSinavBilgi_D.BasvuruTarihi = model.BasvuruTarihi;
                    model.BasvurularSinavBilgi_D.Sinav = basvuru.BasvurularSinavBilgis.Where(p => p.SinavTipGrupID == SinavTipGrup.DilSinavlari).FirstOrDefault();
                    model.BasvurularSinavBilgi_D.SinavDetay = Management.getSinavBilgisi(model.BasvuruSurecID, model.BasvurularSinavBilgi_D.Sinav.SinavTipID, otKods, prks, Ings);

                    if (model.BasvurularSinavBilgi_D.SinavDetay.WsSinavCekimTipID.HasValue && model.BasvurularSinavBilgi_D.SinavDetay.WsSinavCekimTipID == WsCekimTipi.Tarih)
                    {
                        var sinavDils = db.SinavDilleris.Where(p => p.SinavDilID == model.BasvurularSinavBilgi_D.Sinav.SinavDilID).First();
                        model.BasvurularSinavBilgi_D.Sinav.WsSinavDili = sinavDils.DilAdi;
                    }
                }
                if (TomerIstensinmi)
                {
                    model.BasvurularSinavBilgi_T.BasvuruTarihi = model.BasvuruTarihi;
                    model.BasvurularSinavBilgi_T.Sinav = basvuru.BasvurularSinavBilgis.Where(p => p.SinavTipGrupID == SinavTipGrup.Tomer).FirstOrDefault();
                    if (model.BasvurularSinavBilgi_T.Sinav != null) model.BasvurularSinavBilgi_T.SinavDetay = Management.getSinavBilgisi(model.BasvuruSurecID, model.BasvurularSinavBilgi_T.Sinav.SinavTipID, otKods, prks, Ings);
                    model.BasvurularSinavBilgi_T.IsTurkceProgramVar = tercihler.Any(a => a.Ingilizce == false);
                }

                #endregion

                var bdurum = basvuru.BasvuruDurumlari;
                model.BasvuruDurumAdi = bdurum.BasvuruDurumAdi;
                model.DurumClassName = bdurum.ClassName;
                model.DurumColor = bdurum.Color;
                model.BasvuruDurumAciklamasi = basvuru.BasvuruDurumAciklamasi;
            }
            return model;

        }
        public static MmMessage getTDOBasvuruSilKontrol(int TDOBasvuruID)
        {
            var msg = new MmMessage();
            msg.IsSuccess = true;

            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var KayitYetki = RoleNames.TDOGelenBasvuruKayit.InRoleCurrent();
                var basvuru = db.TDOBasvurus.Where(p => p.TDOBasvuruID == TDOBasvuruID).FirstOrDefault();
                if (basvuru == null)
                {
                    msg.IsSuccess = false;
                    msg.Messages.Add("Aranan başvuru sistemde bulunamadı.");
                }
                else
                {
                    if (!UserIdentity.Current.EnstituKods.Contains(basvuru.EnstituKod) && KayitYetki && basvuru.KullaniciID != UserIdentity.Current.Id)
                    {
                        msg.IsSuccess = false;
                        msg.Messages.Add("Bu enstitüye ait başvuruyu silmeye yetkili değilsiniz!");
                        string message = "Bu enstitüye ait tez danışman başvurusu silmeye yetkili değilsiniz!\r\n Tez İzleme Başvuru ID: " + basvuru.TDOBasvuruID + " \r\n Tez İzleme Başvuru sahibi: " + basvuru.Kullanicilar.Ad + " " + basvuru.Kullanicilar.Soyad + " \r\n Başvuru Tarihi: " + basvuru.BasvuruTarihi.ToString();
                        Management.SistemBilgisiKaydet(message, "Tez Danışman Başvuru Sil", BilgiTipi.Kritik);
                    }
                    else if (!TDOAyar.BasvurusuAcikmi.getAyarTDO(basvuru.EnstituKod, "false").ToBoolean().Value && UserIdentity.Current.IsAdmin == false)
                    {
                        msg.IsSuccess = false;
                        msg.Messages.Add("Başvuru süreci dolduğundan başvuru üzerinden herhangi bir işlem yapılamaz!");

                    }
                    else if (KayitYetki == false && basvuru.KullaniciID != UserIdentity.Current.Id)
                    {
                        msg.IsSuccess = false;
                        msg.Messages.Add("Başka bir kullanıcıya ait başvuruyu silmeye hakkınız yoktur!");
                        SistemBilgisiKaydet("Başka bir kullanıcıya ait Tez danışmanı öneri başvurusunu silmeye hakkınız yoktur! \r\n Silinmeye Tez Danışman Başvuru Başvuru ID:" + basvuru.TDOBasvuruID + " \r\n Tez danışmanı öneri Başvuru Sahibi:" + basvuru.Kullanicilar.KullaniciAdi + " \r\n Başvuru Tarihi:" + basvuru.BasvuruTarihi.ToString(), "Başvuru Sil", BilgiTipi.Saldırı);
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
        public static MmMessage getTIBasvuruSilKontrol(int TIBasvuruID)
        {
            var msg = new MmMessage();
            msg.IsSuccess = true;

            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var KayitYetki = RoleNames.TIGelenBasvuruKayit.InRoleCurrent();
                var basvuru = db.TIBasvurus.Where(p => p.TIBasvuruID == TIBasvuruID).FirstOrDefault();
                if (basvuru == null)
                {
                    msg.IsSuccess = false;
                    msg.Messages.Add("Başvuru Bulunamadı.");
                }
                else
                {
                    if (!UserIdentity.Current.EnstituKods.Contains(basvuru.EnstituKod) && KayitYetki && basvuru.KullaniciID != UserIdentity.Current.Id)
                    {
                        msg.IsSuccess = false;
                        msg.Messages.Add("Bu enstitüye ait başvuruyu silmeye yetkili değilsiniz!");
                        string message = "Bu enstitüye ait tez izleme başvurusu silmeye yetkili değilsiniz!\r\n Tez İzleme Başvuru ID: " + basvuru.TIBasvuruID + " \r\n Tez İzleme Başvuru sahibi: " + basvuru.Kullanicilar.Ad + " " + basvuru.Kullanicilar.Soyad + " \r\n Başvuru Tarihi: " + basvuru.BasvuruTarihi.ToString();
                        Management.SistemBilgisiKaydet(message, "TIK Başvuru Sil", BilgiTipi.Kritik);
                    }
                    else if (!TIAyar.BasvurusuAcikmi.getAyarTI(basvuru.EnstituKod, "false").ToBoolean().Value && UserIdentity.Current.IsAdmin == false)
                    {
                        msg.IsSuccess = false;
                        msg.Messages.Add("Başvuru süreci dolduğundan başvuru üzerinden herhangi bir işlem yapılamaz!");

                    }
                    else if (KayitYetki == false && basvuru.KullaniciID != UserIdentity.Current.Id)
                    {
                        msg.IsSuccess = false;
                        msg.Messages.Add("Başka bir kullanıcıya ait başvuruyu silmeye hakkınız yoktur!");
                        SistemBilgisiKaydet("Başka bir kullanıcıya ait Tez İzleme başvurusunu silmeye hakkınız yoktur! \r\n Silinmeye çalışılan Tez İzleme Başvuru ID:" + basvuru.TIBasvuruID + " \r\n Tez İzleme Başvuru Sahibi:" + basvuru.Kullanicilar.KullaniciAdi + " \r\n Başvuru Tarihi:" + basvuru.BasvuruTarihi.ToString(), "Başvuru Sil", BilgiTipi.Saldırı);
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
        public static MmMessage getAktifTezDanismanOneriSurecKontrol(string EnstituKod, int? KullaniciID, int? TDOBasvuruID = null)
        {
            var msg = new MmMessage();
            msg.IsSuccess = true;
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var KayitYetki = RoleNames.TDOGelenBasvuruKayit.InRoleCurrent();
                if (TDOBasvuruID.HasValue)
                {
                    var basvuru = db.TDOBasvurus.Where(p => p.TDOBasvuruID == TDOBasvuruID.Value).FirstOrDefault();
                    if (basvuru == null)
                    {
                        msg.IsSuccess = false;
                        msg.Messages.Add("Aranan başvuru sistemde bulunamadı.");
                        if (KayitYetki == false) SistemBilgisiKaydet("Aranan başvuru sistemde bulunamadı! \r\n Çağrılan Tez danışmanı öneri Başvuru ID:" + TDOBasvuruID, "TDO Başvuru Düzelt", BilgiTipi.Uyarı);
                    }
                    else
                    {
                        if (basvuru.EnstituKod != EnstituKod)
                        {
                            SistemBilgisiKaydet("Seçilen Tez danışmanı öneri başvurusu Enstitü kodu ile aktif Enstitü kodu uyuşmuyor! \r\n Çağrılan Tez danışmanı öneri Başvuru Enstitü Kod:" + basvuru.EnstituKod + " \r\n Aktif Enstitü Kod:" + EnstituKod + " \r\n Çağrılan Tez İzleme Başvuru ID:" + basvuru.TDOBasvuruID + " \r\n Başvuru Sahibi:" + basvuru.Kullanicilar.KullaniciAdi, "Tez danışmanı öneri Düzelt", BilgiTipi.Uyarı);
                            EnstituKod = basvuru.EnstituKod;
                        }
                        if (!UserIdentity.Current.EnstituKods.Contains(basvuru.EnstituKod) && KayitYetki && basvuru.KullaniciID != UserIdentity.Current.Id)
                        {
                            msg.IsSuccess = false;
                            msg.Messages.Add("Bu Enstitüde yetkili değilsiniz.");
                            string message = "Bu enstitüye ait Tez danışmanı öneri başvurusu güncellemeye yetkili değilsiniz!\r\n Tez İzleme Başvuru ID: " + basvuru.TDOBasvuruID + " \r\n Başvuru sahibi: " + basvuru.Kullanicilar.Ad + " " + basvuru.Kullanicilar.Soyad + " \r\n Başvuru Tarihi: " + basvuru.BasvuruTarihi.ToString();
                            Management.SistemBilgisiKaydet(message, "Başvuru Düzelt", BilgiTipi.Saldırı);
                        }
                        else if (!TDOAyar.BasvurusuAcikmi.getAyarTDO(basvuru.EnstituKod, "false").ToBoolean().Value && UserIdentity.Current.IsAdmin == false)
                        {
                            msg.IsSuccess = false;
                            msg.Messages.Add("Başvuru süreci dolduğundan başvuru üzerinden herhangi bir işlem yapılamaz!");

                        }
                        if (KayitYetki == false && basvuru.KullaniciID != UserIdentity.Current.Id)
                        {
                            msg.IsSuccess = false;
                            msg.Messages.Add("Bu işlem için yetkili değilsiniz.");
                            SistemBilgisiKaydet("Başka bir kullanıcıya ait Tez danışmanı öneri başvurusu düzenlemeye hakkınız yoktur! \r\n Çağrılan Tez İzleme Başvuru ID:" + basvuru.TDOBasvuruID + " \r\n Başvuru Sahibi:" + basvuru.Kullanicilar.KullaniciAdi, "Tez danışmanı öneri Düzelt", BilgiTipi.Saldırı);
                        }

                    }
                }
                else
                {
                    msg.IsSuccess = TDOAyar.BasvurusuAcikmi.getAyarTDO(EnstituKod, "false").ToBoolean().Value;
                    if (KullaniciID.HasValue == false) KullaniciID = UserIdentity.Current.Id;
                    else if (KullaniciID != UserIdentity.Current.Id && RoleNames.KullaniciAdinaTezDanismanOnerisiYap.InRoleCurrent() == false && UserIdentity.Current.IsAdmin == false)
                    {
                        SistemBilgisiKaydet("Başka bir kullanıcıya adına başvuru yapılmak isteniyor! \r\n Başvuru yapılmak istenen Kullanıcı ID:" + KullaniciID + " \r\n İşlem Yapan Kullanıcı ID:" + UserIdentity.Current.Id, "Tez danışmanı önerisi Yap", BilgiTipi.Saldırı);
                        KullaniciID = UserIdentity.Current.Id;
                    }
                    var kul = db.Kullanicilars.Where(p => p.KullaniciID == KullaniciID.Value).First();
                    if (msg.IsSuccess == false)
                    {
                        msg.Messages.Add("Başvuru süreci kapalı.");
                    }
                    else
                    {
                        if (kul.YtuOgrencisi && kul.OgrenimDurumID == OgrenimDurum.HalenOğrenci)
                        {
                            var AktifDevamEdenBasvuruVar = db.TDOBasvurus.Any(p => p.KullaniciID == KullaniciID && p.OgrenciNo == kul.OgrenciNo && p.TDOBasvuruID != TDOBasvuruID.Value);//aynı başvuru sürecindeki başvurular baz alınsın
                            if (AktifDevamEdenBasvuruVar)// toplam başvuru kontrol
                            {
                                msg.IsSuccess = false;
                                msg.Messages.Add("Aktif olarak devam eden bir Tez danışmanı öneri süreciniz bulunuyor. Yeni başvuru yapamazsınız.Tez danışmanı önerisi oluşturmak için aşağıda bulunan başvuru detayınızdan 'Yeni tez danışmanı önerisi' butonuna tıklayınız.");


                            }
                        }
                        else
                        {
                            msg.IsSuccess = false;
                            msg.Messages.Add("Tez danışman atama başvurusunu Aktif olarak okuyan öğrencileri tarafından yapılabilir.");
                        }
                    }
                }

            }
            return msg;

        }
        public static MmMessage getAktifTezIzlemeSurecKontrol(string EnstituKod, int? KullaniciID, int? TIBasvuruID = null)
        {
            var msg = new MmMessage();
            msg.IsSuccess = true;
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var KayitYetki = RoleNames.TIGelenBasvuruKayit.InRoleCurrent();
                if (TIBasvuruID.HasValue)
                {
                    var basvuru = db.TIBasvurus.Where(p => p.TIBasvuruID == TIBasvuruID.Value).FirstOrDefault();
                    if (basvuru == null)
                    {
                        msg.IsSuccess = false;
                        msg.Messages.Add("Aranan başvuru sistemde bulunamadı.");
                        if (KayitYetki == false) SistemBilgisiKaydet("Aranan başvuru sistemde bulunamadı! \r\n Çağrılan Tez İzleme Başvuru ID:" + TIBasvuruID, "TI Başvuru Düzelt", BilgiTipi.Uyarı);
                    }
                    else
                    {
                        if (basvuru.EnstituKod != EnstituKod)
                        {
                            SistemBilgisiKaydet("Seçilen Tez İzleme başvurusu Enstitü kodu ile aktif Enstitü kodu uyuşmuyor! \r\n Çağrılan Tez İzleme Başvuru Enstitü Kod:" + basvuru.EnstituKod + " \r\n Aktif Enstitü Kod:" + EnstituKod + " \r\n Çağrılan Tez İzleme Başvuru ID:" + basvuru.TIBasvuruID + " \r\n Başvuru Sahibi:" + basvuru.Kullanicilar.KullaniciAdi, "TIK Başvuru Düzelt", BilgiTipi.Uyarı);
                            EnstituKod = basvuru.EnstituKod;
                        }
                        if (!UserIdentity.Current.EnstituKods.Contains(basvuru.EnstituKod) && KayitYetki && basvuru.KullaniciID != UserIdentity.Current.Id)
                        {
                            msg.IsSuccess = false;
                            msg.Messages.Add("Bu Enstitü için Yetkili Değilsiniz.");
                            string message = "Bu enstitüye ait Tez İzleme başvurusu güncellemeye yetkili değilsiniz!\r\n Tez İzleme Başvuru ID: " + basvuru.TIBasvuruID + " \r\n Başvuru sahibi: " + basvuru.Kullanicilar.Ad + " " + basvuru.Kullanicilar.Soyad + " \r\n Başvuru Tarihi: " + basvuru.BasvuruTarihi.ToString();
                            Management.SistemBilgisiKaydet(message, "Başvuru Düzelt", BilgiTipi.Saldırı);
                        }
                        else if (!TIAyar.BasvurusuAcikmi.getAyarTI(basvuru.EnstituKod, "false").ToBoolean().Value && UserIdentity.Current.IsAdmin == false)
                        {
                            msg.IsSuccess = false;
                            msg.Messages.Add("Başvuru süreci dolduğundan başvuru üzerinden herhangi bir işlem yapılamaz!");

                        }
                        if (KayitYetki == false && basvuru.KullaniciID != UserIdentity.Current.Id)
                        {
                            msg.IsSuccess = false;
                            msg.Messages.Add("Bu İşlem için Yetkili Değilsiniz.");
                            SistemBilgisiKaydet("Başka bir kullanıcıya ait Tez İzleme başvurusu düzenlemeye hakkınız yoktur! \r\n Çağrılan Tez İzleme Başvuru ID:" + basvuru.TIBasvuruID + " \r\n Başvuru Sahibi:" + basvuru.Kullanicilar.KullaniciAdi, "TIK Başvuru Düzelt", BilgiTipi.Saldırı);
                        }
                    }
                }
                else
                {
                    msg.IsSuccess = TIAyar.BasvurusuAcikmi.getAyarTI(EnstituKod, "false").ToBoolean().Value;
                    if (KullaniciID.HasValue == false) KullaniciID = UserIdentity.Current.Id;
                    else if (KullaniciID != UserIdentity.Current.Id && RoleNames.KullaniciAdinaTezIzlemeBasvurusuYap.InRoleCurrent() == false && UserIdentity.Current.IsAdmin == false)
                    {
                        SistemBilgisiKaydet("Başka bir kullanıcıya adına başvuru yapılmak isteniyor! \r\n Başvuru yapılmak istenen Kullanıcı ID:" + KullaniciID + " \r\n İşlem Yapan Kullanıcı ID:" + UserIdentity.Current.Id, "Tez İzleme Başvuru Yap", BilgiTipi.Saldırı);
                        KullaniciID = UserIdentity.Current.Id;
                    }
                    var kul = db.Kullanicilars.Where(p => p.KullaniciID == KullaniciID.Value).First();
                    if (msg.IsSuccess == false)
                    {
                        msg.Messages.Add("Başvuru Süreci Kapalı");
                    }
                    else
                    {
                        if (kul.YtuOgrencisi && kul.OgrenimDurumID == OgrenimDurum.HalenOğrenci && (kul.OgrenimTipKod == OgrenimTipi.Doktra || kul.OgrenimTipKod == OgrenimTipi.ButunlesikDoktora))
                        {
                            var AktifDevamEdenBasvuruVar = db.TIBasvurus.Any(p => p.KullaniciID == KullaniciID && p.OgrenciNo == kul.OgrenciNo && p.TIBasvuruID != TIBasvuruID.Value);//aynı başvuru sürecindeki başvurular baz alınsın
                            if (AktifDevamEdenBasvuruVar)// toplam başvuru kontrol
                            {
                                msg.IsSuccess = false;
                                msg.Messages.Add("Aktif olarak devam eden bir Tez izleme süreciniz bulunuyor. Yeni başvuru yapamazsınız. Ara rapor oluşturmak için aşağıda bulunan başvuru detayınızdan 'Yeni Rapor Oluştur' butonuna tıklayınız.");


                            }
                            else
                            {
                                var sondonemKayitOlmasiGerekenDersKodlari = TIAyar.SonDonemKayitOlunmasiGerekenDersKodlari.getAyarTI(EnstituKod, "");

                                var sondonemKayitOlmasiGerekenDersKodlariList = sondonemKayitOlmasiGerekenDersKodlari.Split(',').ToList();
                                var ogrenciBilgi = Management.StudentControl(kul.TcKimlikNo);

                                var BkMsg = new List<string>();
                                if (sondonemKayitOlmasiGerekenDersKodlariList.Any() && ogrenciBilgi.AktifDonemDers.DersKodNums.Where(p => sondonemKayitOlmasiGerekenDersKodlariList.Any(a => a == p)).Count() != sondonemKayitOlmasiGerekenDersKodlariList.Count)
                                {
                                    BkMsg.Add(string.Join(", ", sondonemKayitOlmasiGerekenDersKodlari) + " kodlu derslere son dönemde kayıt yaptırmanız gerekmektedi.");
                                }
                                if (BkMsg.Count > 0)
                                {
                                    msg.Messages.Add("Tez izleme başvurunuz aşağıdaki sebeplerden dolayı başlatılamadı.");
                                    msg.Messages.AddRange(BkMsg);
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
        public static BasvuruDetayModelTDO getSecilenBasvuruTDODetay(int TDOBasvuruID, Guid? UniqueID)
        {
            var model = new BasvuruDetayModelTDO() { TDOBasvuruID = TDOBasvuruID };
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var IsYoneticiYetki = RoleNames.TDOEYKdaOnayYetkisi.InRoleCurrent();
                var IsDanismanOnayYetki = RoleNames.TDODanismanOnayYetkisi.InRoleCurrent();
                var basvuru = db.TDOBasvurus.Where(p => p.TDOBasvuruID == TDOBasvuruID).First();
                KullaniciKayitBilgisiGuncelle(basvuru.KullaniciID);
                var enstitu = db.Enstitulers.Where(p => p.EnstituKod == basvuru.EnstituKod).First();
                var ShowAllRow = basvuru.KullaniciID == UserIdentity.Current.Id || RoleNames.TDOEYKyaGonderimYetkisi.InRoleCurrent() || RoleNames.TDOEYKdaOnayYetkisi.InRoleCurrent();
            tekrarYukle:
                model.TDOBasvuruDanisman = basvuru.TDOBasvuruDanisman;
                model.TDOBasvuruDanismanList = (from s in basvuru.TDOBasvuruDanismen
                                                select new TDOBasvuruDanismanModel
                                                {
                                                    UniqueID = s.UniqueID,
                                                    IsObsData = s.IsObsData,
                                                    TDODanismanTalepTipID = s.TDODanismanTalepTipID,
                                                    TalepTipAdi = s.TDODanismanTalepTipleri.TalepTipAdi,
                                                    DonemBaslangicYil = s.DonemBaslangicYil,
                                                    DonemID = s.DonemID,
                                                    DonemAdi = s.DonemBaslangicYil + "/" + (s.DonemBaslangicYil + 1) + " " + (s.DonemID == 1 ? "Güz" : "Bahar"),
                                                    FormKodu = s.FormKodu,
                                                    TDOBasvuruDanismanID = s.TDOBasvuruDanismanID,
                                                    TDOBasvuruID = s.TDOBasvuruID,
                                                    BasvuruTarihi = s.BasvuruTarihi,
                                                    IsTezDiliTr = s.IsTezDiliTr,
                                                    TezBaslikTr = s.TezBaslikTr,
                                                    TezBaslikEn = s.TezBaslikEn,
                                                    YeniTezBaslikTr = s.YeniTezBaslikTr,
                                                    YeniTezBaslikEn = s.YeniTezBaslikEn,
                                                    SinavAdi = s.SinavAdi,
                                                    SinavPuani = s.SinavPuani,
                                                    SinavYili = s.SinavYili,
                                                    VarolanTezDanismanID = s.VarolanTezDanismanID,
                                                    VarolanDanismanOnayladi = s.VarolanDanismanOnayladi,
                                                    VarolanDanismanOnayTarihi = s.VarolanDanismanOnayTarihi,
                                                    VarolanDanismanOnaylanmadiAciklama = s.VarolanDanismanOnaylanmadiAciklama,

                                                    TDUniversiteAdi = s.TDUniversiteAdi,
                                                    TezDanismanID = s.TezDanismanID,
                                                    TDAdSoyad = s.TDAdSoyad,
                                                    TDUnvanAdi = s.TDUnvanAdi,
                                                    TDAnabilimDaliAdi = s.TDAnabilimDaliAdi,
                                                    TDProgramAdi = s.TDProgramAdi,
                                                    TDSinavTipID = s.TDSinavTipID,
                                                    TDSinavAdi = s.TDSinavAdi,
                                                    TDSinavYili = s.TDSinavYili,
                                                    TDSinavPuani = s.TDSinavPuani,
                                                    TDOgrenciSayisiDR = s.TDOgrenciSayisiDR,
                                                    TDOgrenciSayisiYL = s.TDOgrenciSayisiYL,
                                                    TDTezSayisiDR = s.TDTezSayisiDR,
                                                    TDTezSayisiYL = s.TDTezSayisiYL,
                                                    DanismanOnayladi = s.DanismanOnayladi,
                                                    DanismanOnayTarihi = s.DanismanOnayTarihi,
                                                    DanismanOnaylanmadiAciklama = s.DanismanOnaylanmadiAciklama,

                                                    EYKYaGonderildi = s.EYKYaGonderildi,
                                                    EYKYaGonderildiIslemTarihi = s.EYKYaGonderildiIslemTarihi,
                                                    EYKYaGonderildiIslemYapanID = s.EYKYaGonderildiIslemYapanID,

                                                    EYKDaOnaylandi = s.EYKDaOnaylandi,
                                                    EYKDaOnaylandiIslemYapanID = s.EYKDaOnaylandiIslemYapanID,
                                                    EYKDaOnaylandiOnayTarihi = s.EYKDaOnaylandiOnayTarihi,
                                                    EYKDaOnaylanmadiDurumAciklamasi = s.EYKDaOnaylanmadiDurumAciklamasi,
                                                    IslemTarihi = s.IslemTarihi,
                                                    IslemYapanID = s.IslemYapanID,
                                                    IslemYapanIP = s.IslemYapanIP,
                                                    TDOBasvuruEsDanismen = basvuru.TDOBasvuruDanismen.SelectMany(sm => sm.TDOBasvuruEsDanismen).OrderByDescending(oe => oe.TDOBasvuruEsDanismanID).ToList(),
                                                    EsDanismanBilgi = basvuru.TDOBasvuruDanismen.SelectMany(sm => sm.TDOBasvuruEsDanismen).OrderByDescending(o => o.TDOBasvuruEsDanismanID).FirstOrDefault()

                                                }).Where(p => p.TezDanismanID == (ShowAllRow ? p.TezDanismanID : UserIdentity.Current.Id) || p.VarolanTezDanismanID == (ShowAllRow ? p.VarolanTezDanismanID : UserIdentity.Current.Id)).OrderByDescending(o => o.BasvuruTarihi).ToList();
                if (model.TDOBasvuruDanismanList.Any() && !basvuru.AktifTDOBasvuruDanismanID.HasValue)
                {
                    basvuru.AktifTDOBasvuruDanismanID = model.TDOBasvuruDanismanList.Last().TDOBasvuruDanismanID;
                    db.SaveChanges();
                }

                var KulIds = model.TDOBasvuruDanismanList.Select(s => s.VarolanTezDanismanID).ToList();
                var Kulls = db.Kullanicilars.Where(p => KulIds.Contains(p.KullaniciID)).ToList();

                var inx = 0;
                foreach (var item in model.TDOBasvuruDanismanList.OrderByDescending(o => o.TDOBasvuruDanismanID))
                {
                    inx++;
                    if (item.VarolanTezDanismanID.HasValue)
                    {
                        var kul = Kulls.First(p => p.KullaniciID == item.VarolanTezDanismanID);
                        item.VarolanDanismanAd = kul.Unvanlar.UnvanAdi + " " + kul.Ad + " " + kul.Soyad;
                    }

                    if (inx == 1)
                    {
                        item.IsYeniEsDanismanOneriOrDegisiklik = !item.TDOBasvuruEsDanismen.Any(ae => ae.EYKDaOnaylandi == true);
                        if (item.IsYeniEsDanismanOneriOrDegisiklik)
                        {
                            item.TdoEsBasvurusuYapabilir = (item.EsDanismanBilgi == null ||
                                                             item.EsDanismanBilgi.EYKYaGonderildi == false ||
                                                             item.EsDanismanBilgi.EYKDaOnaylandi == false);
                        }
                        else
                        {
                            item.TdoEsBasvurusuYapabilir = item.EsDanismanBilgi == null || (item.EsDanismanBilgi.EYKYaGonderildi == false ||
                                                             item.EsDanismanBilgi.EYKDaOnaylandi.HasValue);
                        }
                        if (item.TdoEsBasvurusuYapabilir)
                        {
                            if (model.TDOBasvuruDanisman != null)
                            {
                                item.TdoEsBasvurusuYapabilir = IsYoneticiYetki || model.TDOBasvuruDanisman.TezDanismanID == UserIdentity.Current.Id;
                            }
                        }

                    }
                }
                model.AktifTDOBasvuruDanismanID = basvuru.AktifTDOBasvuruDanismanID;
                model.BasvuruTarihi = basvuru.BasvuruTarihi;
                model.KullaniciID = basvuru.KullaniciID;
                model.KayitDonemi = basvuru.KayitOgretimYiliDonemID.HasValue ? (basvuru.KayitOgretimYiliBaslangic + "/" + (basvuru.KayitOgretimYiliBaslangic + 1) + " " + db.Donemlers.Where(p => p.DonemID == basvuru.KayitOgretimYiliDonemID.Value).First().DonemAdi) : "";
                model.KullaniciTipID = basvuru.KullaniciTipID;
                model.ResimAdi = basvuru.ResimAdi;
                model.Ad = basvuru.Ad;
                model.Soyad = basvuru.Soyad;
                model.TcKimlikNo = basvuru.TcKimlikNo;
                model.PasaportNo = basvuru.PasaportNo;
                model.UyrukKod = basvuru.UyrukKod;
                model.OgrenciNo = basvuru.OgrenciNo;
                model.OgrenimTipAdi = db.OgrenimTipleris.Where(p => p.EnstituKod == basvuru.EnstituKod && p.OgrenimTipKod == basvuru.OgrenimTipKod).First().OgrenimTipAdi;
                var progLng = basvuru.Programlar;
                model.AnabilimdaliAdi = progLng.AnabilimDallari.AnabilimDaliAdi;
                model.ProgramAdi = progLng.ProgramAdi;
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
                model.DegerlendirenUniqueID = UniqueID;

                if (!model.TDOBasvuruDanismanList.Any(a => a.TezDanismanID == basvuru.Kullanicilar.DanismanID))
                {
                    var Eslestirildi = ObsDanismanBasvurBilgiEslestir(model.KullaniciID, model.TDOBasvuruID);
                    if (Eslestirildi)
                    {
                        basvuru = db.TDOBasvurus.Where(p => p.TDOBasvuruID == TDOBasvuruID).First();
                        goto tekrarYukle;
                    }

                }
                if (model.TDOBasvuruDanismanList.Any())
                {
                    var firstRow = model.TDOBasvuruDanismanList.First();
                    firstRow.VarolanDanismanGozuksun = firstRow.TDODanismanTalepTipID == TDODanismanTalepTip.TezDanismaniDegisikligi || firstRow.TDODanismanTalepTipID == TDODanismanTalepTip.TezDanismaniVeBaslikDegisikligi;
                    firstRow.VarolanDanismanOnayIslemiAcik = (IsDanismanOnayYetki && firstRow.VarolanTezDanismanID == UserIdentity.Current.Id || IsYoneticiYetki) && !firstRow.IsObsData && !firstRow.DanismanOnayladi.HasValue;
                    if (firstRow.VarolanDanismanGozuksun)
                    {
                        firstRow.YeniDanismanOnayIslemiAcik = firstRow.VarolanDanismanOnayladi == true && (IsDanismanOnayYetki && firstRow.TezDanismanID == UserIdentity.Current.Id || IsYoneticiYetki) && !firstRow.IsObsData && !firstRow.EYKYaGonderildi.HasValue;
                    }
                    else
                    {
                        firstRow.YeniDanismanOnayIslemiAcik = (IsDanismanOnayYetki && firstRow.TezDanismanID == UserIdentity.Current.Id || IsYoneticiYetki) && !firstRow.IsObsData && !firstRow.EYKYaGonderildi.HasValue;

                    }
                    firstRow.IsYeniTezBasligiGozuksun = firstRow.TDODanismanTalepTipID == TDODanismanTalepTip.TezDanismaniVeBaslikDegisikligi || firstRow.TDODanismanTalepTipID == TDODanismanTalepTip.TezBasligiDegisikligi;

                    firstRow.IsDuzeltSilYapabilir = firstRow.DanismanOnayladi != true && firstRow.VarolanDanismanOnayladi != true;
                }


                TDOBasvuruEsDanisman lastEsBasvuru = null;
                if (basvuru.TDOBasvuruDanisman != null)
                    lastEsBasvuru = basvuru.TDOBasvuruDanisman.TDOBasvuruEsDanismen
                                        .OrderByDescending(o => o.TDOBasvuruEsDanismanID).FirstOrDefault();
                model.IsYeniDanismanOneriOrDegisiklik = model.TDOBasvuruDanisman == null;
                if (model.IsYeniDanismanOneriOrDegisiklik)
                {
                    model.TdoBasvurusuYapabilir = (model.TDOBasvuruDanisman == null || model.TDOBasvuruDanisman.DanismanOnayladi == false || model.TDOBasvuruDanisman.EYKYaGonderildi == false || model.TDOBasvuruDanisman.EYKDaOnaylandi == false);


                }
                else
                {
                    model.TdoBasvurusuYapabilir = (model.TDOBasvuruDanisman.VarolanDanismanOnayladi == false || model.TDOBasvuruDanisman.DanismanOnayladi == false || model.TDOBasvuruDanisman.EYKYaGonderildi == false || model.TDOBasvuruDanisman.EYKDaOnaylandi.HasValue);

                    if (model.TdoBasvurusuYapabilir) model.TdoBasvurusuYapabilir = (lastEsBasvuru == null || lastEsBasvuru.EYKYaGonderildi == false || lastEsBasvuru.EYKDaOnaylandi.HasValue);
                    if (model.TdoBasvurusuYapabilir)
                    {
                        if (model.TDOBasvuruDanisman != null)
                        {
                            model.TdoBasvurusuYapabilir = IsYoneticiYetki || model.TDOBasvuruDanisman.TezDanismanID == UserIdentity.Current.Id || model.KullaniciID == UserIdentity.Current.Id;
                        }
                    }
                }


            }
            return model;

        }
        public static bool ObsDanismanBasvurBilgiEslestir(int kullaniciID, int? tDOBasvuruID)
        {
            using (var db = new LisansustuBasvuruSistemiEntities())
            {

                var ogr = db.Kullanicilars.First(p => p.KullaniciID == kullaniciID);
                var ogrenciInfo = StudentControl(ogr.TcKimlikNo);
                if (ogr.DanismanID.HasValue)
                {

                    var sonBasvuru = db.TDOBasvuruDanismen.Where(p => p.TDOBasvuru.KullaniciID == kullaniciID).OrderByDescending(o => o.TDOBasvuruDanismanID).FirstOrDefault();

                    if (sonBasvuru == null || sonBasvuru.TezDanismanID != ogr.DanismanID)
                    {
                        var Danisman = db.Kullanicilars.Where(p => p.KullaniciID == ogr.DanismanID).First();
                        var kModel = new TDOBasvuruDanisman();
                        kModel.IsObsData = true;
                        kModel.BasvuruTarihi = DateTime.Now;
                        var donemBilgi = kModel.BasvuruTarihi.ToAraRaporDonemBilgi();
                        kModel.DonemBaslangicYil = donemBilgi.BaslangicYil;
                        kModel.DonemID = donemBilgi.DonemID;
                        var formKodu = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 8).ToUpper();
                        while (db.TDOBasvuruDanismen.Any(a => a.FormKodu == formKodu))
                        {
                            formKodu = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 8).ToUpper();
                        }
                        kModel.UniqueID = Guid.NewGuid();
                        kModel.FormKodu = formKodu;


                        kModel.IsTezDiliTr = ogrenciInfo.IsTezDiliTr;

                        kModel.TezBaslikTr = ogrenciInfo.OgrenciTez.TEZ_BASLIK;
                        kModel.TezBaslikEn = ogrenciInfo.OgrenciTez.TEZ_BASLIK_ENG.IsNullOrWhiteSpace() ? ogrenciInfo.OgrenciTez.TEZ_BASLIK : ogrenciInfo.OgrenciTez.TEZ_BASLIK_ENG;
                        kModel.TezDanismanID = Danisman.KullaniciID;
                        kModel.TDAdSoyad = Danisman.Ad + " " + Danisman.Soyad;
                        kModel.TDUnvanAdi = Danisman.Unvanlar.UnvanAdi;
                        kModel.TDProgramAdi = ogrenciInfo.DanismanInfo.PROGRAM_AD;
                        kModel.TDAnabilimDaliAdi = ogrenciInfo.DanismanInfo.ANABILIMDALI_AD;

                        kModel.TDOgrenciSayisiYL = ogrenciInfo.DanismanInfo.DANISMAN_OLUNAN_YL_SAYI1.toIntObj();
                        kModel.TDOgrenciSayisiDR = ogrenciInfo.DanismanInfo.DANISMAN_OLUNAN_DR_SAYI1.toIntObj();
                        kModel.TDTezSayisiYL = ogrenciInfo.DanismanInfo.DANISMAN_MEZUN_YL_SAYI1.toIntObj();
                        kModel.TDTezSayisiDR = ogrenciInfo.DanismanInfo.DANISMAN_MEZUN_DR_SAYI1.toIntObj();

                        kModel.DanismanOnayladi = true;
                        kModel.EYKYaGonderildi = true;
                        kModel.EYKDaOnaylandi = true;
                        kModel.TDODanismanTalepTipID = TDODanismanTalepTip.TezDanismaniOnerisi;

                        kModel.IslemTarihi = DateTime.Now;
                        kModel.IslemYapanID = UserIdentity.Current.Id;
                        kModel.IslemYapanIP = UserIdentity.Ip;

                        if (tDOBasvuruID.HasValue)
                        {
                            var tDoBasvuru = db.TDOBasvurus.First(p => p.TDOBasvuruID == tDOBasvuruID);
                            kModel.TDOBasvuruID = tDOBasvuruID.Value;
                            var added = db.TDOBasvuruDanismen.Add(kModel);
                            tDoBasvuru.AktifTDOBasvuruDanismanID = added.TDOBasvuruDanismanID;
                        }

                        db.SaveChanges();

                        return true;
                    }

                }

                return false;

            }
        }
        public static BasvuruDetayModelTI getSecilenBasvuruTIDetay(int TIBasvuruID, Guid? UniqueID)
        {
            var model = new BasvuruDetayModelTI();
            using (var db = new LisansustuBasvuruSistemiEntities())
            {

                var basvuru = db.TIBasvurus.Where(p => p.TIBasvuruID == TIBasvuruID).First();
                var enstitu = db.Enstitulers.Where(p => p.EnstituKod == basvuru.EnstituKod).First();

                var EslesenDanisman = db.Kullanicilars.Where(p => p.KullaniciID == (basvuru.TezDanismanID ?? 0)).FirstOrDefault();
                if (EslesenDanisman != null)
                {
                    var UnvanAdi = EslesenDanisman.Unvanlar != null ? EslesenDanisman.Unvanlar.UnvanAdi : "";
                    model.TezDanismanBilgiEslesen = UnvanAdi + " " + EslesenDanisman.Ad + " " + EslesenDanisman.Soyad;
                }
                else
                {
                    model.TezDanismanBilgiEslesen = "Sistemde eşleşen tez danışmanı bulunamadı.";
                }
                model.TIBasvuruAraRaporList = basvuru.TIBasvuruAraRapors.Where(p => UniqueID.HasValue ? p.TIBasvuruAraRaporKomites.Any(a => a.UniqueID == UniqueID) : true).Select(s => new TIBasvuruAraRaporModel
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
                               select new frTalepler
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
                model.KayitDonemi = basvuru.KayitOgretimYiliBaslangic + "/" + (basvuru.KayitOgretimYiliBaslangic + 1) + " " + db.Donemlers.Where(p => p.DonemID == basvuru.KayitOgretimYiliDonemID.Value).First().DonemAdi;
                model.KullaniciTipID = basvuru.KullaniciTipID;
                model.ResimAdi = basvuru.ResimAdi;
                model.Ad = basvuru.Kullanicilar.Ad;
                model.Soyad = basvuru.Kullanicilar.Soyad;
                model.TcKimlikNo = basvuru.TcKimlikNo;
                model.PasaportNo = basvuru.PasaportNo;
                model.UyrukKod = basvuru.UyrukKod;
                model.OgrenciNo = basvuru.OgrenciNo;
                model.OgrenimTipAdi = db.OgrenimTipleris.Where(p => p.EnstituKod == basvuru.EnstituKod && p.OgrenimTipKod == basvuru.OgrenimTipKod).First().OgrenimTipAdi;

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
                model.DegerlendirenUniqueID = UniqueID;



            }
            return model;

        }



        public static MmMessage getMezuniyetBasvurusuSilKontrol(int MezuniyetBasvurulariID)
        {
            var msg = new MmMessage();
            msg.IsSuccess = true;

            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var KayitYetki = RoleNames.MezuniyetGelenBasvurularKayit.InRoleCurrent();
                var basvuru = db.MezuniyetBasvurularis.Where(p => p.MezuniyetBasvurulariID == MezuniyetBasvurulariID).FirstOrDefault();
                if (basvuru == null)
                {
                    msg.IsSuccess = false;
                    msg.Messages.Add("Aranan başvuru sistemde bulunamadı.");
                }
                else
                {
                    if (!UserIdentity.Current.EnstituKods.Contains(basvuru.MezuniyetSureci.EnstituKod) && KayitYetki && basvuru.KullaniciID != UserIdentity.Current.Id)
                    {
                        msg.IsSuccess = false;
                        msg.Messages.Add("Bu enstitüye ait başvuruyu silmeye yetkili değilsiniz!");
                        string message = "Bu enstitüye ait mezuniyet başvurusu silmeye yetkili değilsiniz!\r\n Mezuniyet Başvuru ID: " + basvuru.MezuniyetBasvurulariID + " \r\n Mezuniyet Başvuru sahibi: " + basvuru.Kullanicilar.Ad + " " + basvuru.Kullanicilar.Soyad + " \r\n Başvuru Tarihi: " + basvuru.BasvuruTarihi.ToString();
                        Management.SistemBilgisiKaydet(message, "Mezuniyet Başvuru Sil", BilgiTipi.Kritik);
                    }
                    else if (!getAktifMezuniyetSurecID(basvuru.MezuniyetSureci.EnstituKod, basvuru.MezuniyetSurecID).HasValue && UserIdentity.Current.IsAdmin == false)
                    {
                        msg.IsSuccess = false;
                        msg.Messages.Add("Başvuru süreci dolduğundan başvuru üzerinden herhangi bir işlem yapılamaz!");

                    }
                    else if (KayitYetki == false && basvuru.KullaniciID != UserIdentity.Current.Id)
                    {
                        msg.IsSuccess = false;
                        msg.Messages.Add("Başka bir kullanıcıya ait başvuruyu silmeye hakkınız yoktur!");
                        SistemBilgisiKaydet("Başka bir kullanıcıya ait mezuniyet başvurusunu silmeye hakkınız yoktur! \r\n Silinmeye çalışılan Mezuniyet Başvuru ID:" + basvuru.MezuniyetBasvurulariID + " \r\n Mezuniyet Başvuru Sahibi:" + basvuru.Kullanicilar.KullaniciAdi + " \r\n Başvuru Tarihi:" + basvuru.BasvuruTarihi.ToString(), "Başvuru Sil", BilgiTipi.Saldırı);
                    }
                    else if (KayitYetki == false && basvuru.MezuniyetYayinKontrolDurumID != MezuniyetYayinKontrolDurumu.Taslak)
                    {
                        msg.IsSuccess = false;
                        msg.Messages.Add("Taslak Harici Başvurular Silinemez.");
                    }
                }
            }
            return msg;
        }



        public static MmMessage getAktifMezuniyetSurecKontrol(string EnstituKod, int? KullaniciID, int? MezuniyetBasvurulariID = null)
        {
            var msg = new MmMessage();
            msg.IsSuccess = true;
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var KayitYetki = RoleNames.MezuniyetGelenBasvurularKayit.InRoleCurrent();
                if (MezuniyetBasvurulariID.HasValue)
                {
                    var basvuru = db.MezuniyetBasvurularis.Where(p => p.MezuniyetBasvurulariID == MezuniyetBasvurulariID.Value).FirstOrDefault();
                    if (basvuru == null)
                    {
                        msg.IsSuccess = false;
                        msg.Messages.Add("Aranan başvuru sistemde bulunamadı.");
                        if (KayitYetki == false) SistemBilgisiKaydet("Aranan başvuru sistemde bulunamadı! \r\n Çağrılan Mezuniyet Başvuru ID:" + MezuniyetBasvurulariID, "Mezuniyet Başvuru Düzelt", BilgiTipi.Uyarı);
                    }
                    else
                    {
                        if (basvuru.MezuniyetSureci.EnstituKod != EnstituKod)
                        {
                            SistemBilgisiKaydet("Seçilen Mezuniyet başvurusu Enstitü kodu ile aktif Enstitü kodu uyuşmuyor! \r\n Çağrılan Mezuniyet Başvuru Enstitü Kod:" + basvuru.MezuniyetSureci.EnstituKod + " \r\n Aktif Enstitü Kod:" + EnstituKod + " \r\n Çağrılan Mezuniyet Başvuru ID:" + basvuru.MezuniyetBasvurulariID + " \r\n Başvuru Sahibi:" + basvuru.Kullanicilar.KullaniciAdi, "Mezuniyet Başvuru Düzelt", BilgiTipi.Uyarı);
                            EnstituKod = basvuru.MezuniyetSureci.EnstituKod;
                        }
                        if (!UserIdentity.Current.EnstituKods.Contains(basvuru.MezuniyetSureci.EnstituKod) && KayitYetki && basvuru.KullaniciID != UserIdentity.Current.Id)
                        {
                            msg.IsSuccess = false;
                            msg.Messages.Add("Bu Enstitü İçin Yetkili Değilsiniz.");
                            string message = "Bu enstitüye ait mezuniyet başvurusu güncellemeye yetkili değilsiniz!\r\n Mezuniyet Başvuru ID: " + basvuru.MezuniyetBasvurulariID + " \r\n Başvuru sahibi: " + basvuru.Kullanicilar.Ad + " " + basvuru.Kullanicilar.Soyad + " \r\n Başvuru Tarihi: " + basvuru.BasvuruTarihi.ToString();
                            Management.SistemBilgisiKaydet(message, "Başvuru Düzelt", BilgiTipi.Saldırı);
                        }
                        else if (!getAktifMezuniyetSurecID(EnstituKod, basvuru.MezuniyetSurecID).HasValue && UserIdentity.Current.IsAdmin == false)
                        {
                            msg.IsSuccess = false;
                            msg.Messages.Add("Başvuru süreci dolduğundan başvuru üzerinden herhangi bir işlem yapılamaz!");

                        }
                        if (KayitYetki == false && basvuru.KullaniciID != UserIdentity.Current.Id)
                        {
                            msg.IsSuccess = false;
                            msg.Messages.Add("Bu İşlem için Yetkili Değilsiniz.");
                            SistemBilgisiKaydet("Başka bir kullanıcıya ait Mezuniyet başvurusu düzenlemeye hakkınız yoktur! \r\n Çağrılan Mezuniyet Başvuru ID:" + basvuru.MezuniyetBasvurulariID + " \r\n Başvuru Sahibi:" + basvuru.Kullanicilar.KullaniciAdi, "Mezuniyet Başvuru Düzelt", BilgiTipi.Saldırı);
                        }
                        else if (KayitYetki == false && (basvuru.IsDanismanOnay == true))
                        {
                            msg.IsSuccess = false;
                            msg.Messages.Add("Danışman tarafından onaylanan başvurunuzda düzenleme işlemi yapamazsınız!");
                        }

                    }
                }
                else
                {
                    int? MezuniyetSurecID = getAktifMezuniyetSurecID(EnstituKod);
                    msg.IsSuccess = MezuniyetSurecID.HasValue;
                    if (KullaniciID.HasValue == false) KullaniciID = UserIdentity.Current.Id;
                    else if (KullaniciID != UserIdentity.Current.Id && RoleNames.KullaniciAdinaBasvuruYap.InRoleCurrent() == false && UserIdentity.Current.IsAdmin == false)
                    {
                        SistemBilgisiKaydet("Başka bir kullanıcıya adına başvuru yapılmak isteniyor! \r\n Başvuru yapılmak istenen Kullanıcı ID:" + KullaniciID + " \r\n İşlem Yapan Kullanıcı ID:" + UserIdentity.Current.Id, "Mezuniyet Başvury Yap", BilgiTipi.Saldırı);
                        KullaniciID = UserIdentity.Current.Id;
                    }
                    var kul = db.Kullanicilars.Where(p => p.KullaniciID == KullaniciID.Value).First();
                    if (msg.IsSuccess == false)
                    {
                        msg.Messages.Add("Başvuru Süreci Kapalı");
                    }
                    else if (!(kul.KullaniciTipleri.BasvuruYapabilir))
                    {
                        msg.IsSuccess = false;
                        msg.Messages.Add("Kullanıcı Hesap Türünüz için Başvuru İşlemleri Kapalıdır.");
                    }
                    else
                    {
                        if (kul.YtuOgrencisi)
                        {
                            if (kul.OgrenimDurumID != OgrenimDurum.HalenOğrenci)
                            {
                                msg.IsSuccess = false;
                                msg.Messages.Add("Mezuniyet Başvuru işlemini yapabilmeniz için profil kısmındaki öğrenim bilgilerinizde bulunan Öğrenim durumunuzun Halen öğrenci olarak seçilmesi gerekmektedir. (Not: özel öğrenciler bu sistem üzerinden başvuru yapamazlar.)");
                            }
                            else if (kul.KayitDonemID.HasValue == false)
                            {
                                msg.IsSuccess = false;
                                msg.Messages.Add("Kayıt Tarihi Bilginiz Eksik Başvuru Yapamazsınız");
                            }

                            var basvuruVar = db.MezuniyetBasvurularis.Any(p => p.MezuniyetSurecID == MezuniyetSurecID &&
                                                                        p.KullaniciID == KullaniciID);
                            if (basvuruVar)
                            {
                                msg.IsSuccess = false;
                                msg.Messages.Add("Bu mezuniyet süreci için başvurunuz bulunmaktadır tekrar başvuru yapamazsınız!");

                            }
                            else if (MezuniyetSureciOgrenimTipUygunMu(MezuniyetSurecID.Value, KullaniciID.Value) == false)
                            {
                                var otsAdi = db.OgrenimTipleris.Where(p => p.OgrenimTipKod == kul.OgrenimTipKod).First().OgrenimTipAdi;
                                msg.IsSuccess = false;
                                msg.Messages.Add(otsAdi + " Öğrenim seviyesinde okuyan öğrenciler mezuniyet başvurusu yapamazlar");
                            }
                            else if ((KullaniciID != UserIdentity.Current.Id && RoleNames.KullaniciAdinaBasvuruYap.InRoleCurrent() == false) && kul.OgrenimTipKod == OgrenimTipi.TezliYuksekLisans && (kul.KayitTarihi > Convert.ToDateTime("31-03-2016") && Management.getOkunanDonemList(MezuniyetSurecID.Value, kul.KayitYilBaslangic.Value, kul.KayitDonemID.Value).Count < 4))
                            {
                                var otsAdi = db.OgrenimTipleris.Where(p => p.OgrenimTipKod == kul.OgrenimTipKod).First().OgrenimTipAdi;
                                msg.IsSuccess = false;
                                msg.Messages.Add(otsAdi + " öğrenim seviyesi okuyan öğrencilerin mezuniyet başvurusu için en az 4 dönem okumaları gerekmektedir.");
                            }
                            else
                            {
                                var BasvuruKriterleri = db.MezuniyetSureciOgrenimTipKriterleris.Where(p => p.MezuniyetSurecID == MezuniyetSurecID.Value && p.OgrenimTipKod == kul.OgrenimTipKod).First();
                                var BasvuruSonDonemSecilecekDersKodlari = BasvuruKriterleri.MBasvuruSonDonemKaydiKontrolEdilecekDersKodlari.Split(',').ToList();
                                var ogrenciBilgi = Management.StudentControl(kul.TcKimlikNo);
                                var BkMsg = new List<string>();
                                if (BasvuruSonDonemSecilecekDersKodlari.Any() && ogrenciBilgi.AktifDonemDers.DersKodNums.Where(p => BasvuruSonDonemSecilecekDersKodlari.Any(a => a == p)).Count() != BasvuruSonDonemSecilecekDersKodlari.Count)
                                {
                                    BkMsg.Add(string.Join(", ", BasvuruSonDonemSecilecekDersKodlari) + " kodlu derslere son dönemde kayıt yaptırmanız gerekmektedi.");
                                }
                                if (BasvuruKriterleri.MBasvuruToplamKrediKriteri > ogrenciBilgi.AktifDonemDers.ToplamKredi)
                                {
                                    BkMsg.Add("Toplam Kredi sayınız " + BasvuruKriterleri.MBasvuruToplamKrediKriteri + " krediden büyük ya da eşit olmalıdır. Mevcut Kredi: " + ogrenciBilgi.AktifDonemDers.ToplamKredi);

                                }
                                if (BasvuruKriterleri.MBasvuruAGNOKriteri > ogrenciBilgi.AktifDonemDers.Agno)
                                {
                                    BkMsg.Add("Ortalamanız " + BasvuruKriterleri.MBasvuruAGNOKriteri + " ortalamasından büyük ya da eşit olmalıdır. Mevcut Ortalama: " + ogrenciBilgi.AktifDonemDers.Agno.ToString("n2"));

                                }
                                if (BasvuruKriterleri.MBasvuruAKTSKriteri > ogrenciBilgi.AktifDonemDers.ToplamAkts)
                                {
                                    BkMsg.Add("Akts toplamınız " + BasvuruKriterleri.MBasvuruAKTSKriteri + " akts'den büyük ya da eşit olmalıdır. Mevcut Akts: " + ogrenciBilgi.AktifDonemDers.ToplamAkts);

                                }
                                if (BkMsg.Count > 0)
                                {
                                    var otsAdi = db.OgrenimTipleris.Where(p => p.OgrenimTipKod == kul.OgrenimTipKod).First().OgrenimTipAdi;
                                    msg.Messages.Add(otsAdi + " mezuniyet başvurunuz aşağıdaki sebeplerden dolayı başlatılamadı.");
                                    msg.Messages.AddRange(BkMsg);
                                    msg.IsSuccess = false;
                                }
                            }
                        }
                        else
                        {
                            msg.IsSuccess = false;
                            msg.Messages.Add("Mezuniyet başvurusu yapabilmeniz için Profil bilginizi düzelterek YTU öğrencisi olduğunuzu belirtiniz.");
                        }




                    }
                }

            }
            return msg;

        }
        public static kmMezuniyetBasvuru getSecilenBasvuruMezuniyet(int MezuniyetBasvurulariID)
        {
            var model = new kmMezuniyetBasvuru();
            using (var db = new LisansustuBasvuruSistemiEntities())
            {

                var basvuru = db.MezuniyetBasvurularis.Include("MezuniyetYayinKontrolDurumlari").Where(p => p.MezuniyetBasvurulariID == MezuniyetBasvurulariID).FirstOrDefault();
                var kul = db.Kullanicilars.Where(p => p.KullaniciID == basvuru.KullaniciID).First();

                string EKD = basvuru.MezuniyetSureci.Enstituler.EnstituKisaAd;
                #region BasvuruBilgi
                model.EnstituKod = basvuru.MezuniyetSureci.EnstituKod;
                model.MezuniyetBasvurulariID = basvuru.MezuniyetBasvurulariID;
                model.MezuniyetSurecID = basvuru.MezuniyetSurecID;
                model.BasvuruTarihi = basvuru.BasvuruTarihi;
                model.MezuniyetYayinKontrolDurumID = basvuru.MezuniyetYayinKontrolDurumID;
                model.MezuniyetYayinKontrolDurumAciklamasi = basvuru.MezuniyetYayinKontrolDurumAciklamasi;
                model.KullaniciID = basvuru.KullaniciID;
                model.KayitDonemi = basvuru.KayitOgretimYiliBaslangic + "/" + (basvuru.KayitOgretimYiliBaslangic + 1) + " " + db.Donemlers.Where(p => p.DonemID == basvuru.KayitOgretimYiliDonemID.Value).First().DonemAdi;
                if (kul.KullaniciTipID != basvuru.KullaniciTipID)
                {
                    model.KullaniciTipID = kul.KullaniciTipID;
                    model.ResimAdi = kul.ResimAdi;
                    model.Ad = kul.Ad;
                    model.Soyad = kul.Soyad;
                    model.TcKimlikNo = kul.TcKimlikNo;
                    model.PasaportNo = kul.PasaportNo;


                }
                else
                {
                    model.KullaniciTipID = basvuru.KullaniciTipID;
                    model.ResimAdi = basvuru.ResimAdi;
                    model.Ad = basvuru.Ad;
                    model.Soyad = basvuru.Soyad;
                    model.TcKimlikNo = basvuru.TcKimlikNo;
                    model.PasaportNo = basvuru.PasaportNo;
                    model.UyrukKod = basvuru.UyrukKod;
                }
                model.OgrenciNo = basvuru.OgrenciNo;
                model.OgrenimTipAdi = db.OgrenimTipleris.Where(p => p.EnstituKod == model.EnstituKod && p.OgrenimTipKod == basvuru.OgrenimTipKod).First().OgrenimTipAdi;
                var progLng = basvuru.Programlar;
                model.AnabilimdaliAdi = progLng.AnabilimDallari.AnabilimDaliAdi;
                model.ProgramAdi = progLng.ProgramAdi;
                model.OgrenimDurumID = basvuru.OgrenimDurumID;
                model.OgrenimTipKod = basvuru.OgrenimTipKod;
                model.ProgramKod = basvuru.ProgramKod;
                model.KayitOgretimYiliBaslangic = basvuru.KayitOgretimYiliBaslangic;
                model.KayitOgretimYiliDonemID = basvuru.KayitOgretimYiliDonemID;
                model.KayitTarihi = basvuru.KayitTarihi;
                model.IsTezDiliTr = basvuru.IsTezDiliTr;
                model.TezBaslikTr = basvuru.TezBaslikTr;
                model.TezBaslikEn = basvuru.TezBaslikEn;
                model.TezDanismanAdi = basvuru.TezDanismanAdi;
                model.TezDanismanUnvani = basvuru.TezDanismanUnvani;
                model.TezEsDanismanAdi = basvuru.TezEsDanismanAdi;
                model.TezEsDanismanUnvani = basvuru.TezEsDanismanUnvani;
                model.TezEsDanismanEMail = basvuru.TezEsDanismanEMail;
                model.TezOzet = basvuru.TezOzet;
                model.OzetAnahtarKelimeler = basvuru.OzetAnahtarKelimeler;
                model.TezOzetHtml = basvuru.TezOzetHtml;
                model.TezAbstract = basvuru.TezAbstract;
                model.TezAbstractHtml = basvuru.TezAbstractHtml;
                model.AbstractAnahtarKelimeler = basvuru.AbstractAnahtarKelimeler;
                model.DanismanImzaliFormDosyaAdi = basvuru.DanismanImzaliFormDosyaAdi;
                model.DanismanImzaliFormDosyaYolu = basvuru.DanismanImzaliFormDosyaYolu;
                model.IslemTarihi = DateTime.Now;
                model.IslemYapanID = UserIdentity.Current.Id;
                model.IslemYapanIP = UserIdentity.Ip;
                #endregion

                var yayins = (from qs in db.MezuniyetBasvurulariYayins.Where(p => p.MezuniyetBasvurulariID == MezuniyetBasvurulariID)
                              join s in db.MezuniyetSureciYayinTurleris on new { qs.MezuniyetBasvurulari.MezuniyetSurecID, qs.MezuniyetYayinTurID } equals new { s.MezuniyetSurecID, s.MezuniyetYayinTurID }
                              join sd in db.MezuniyetYayinTurleris on new { s.MezuniyetYayinTurID } equals new { sd.MezuniyetYayinTurID }
                              join yb in db.MezuniyetYayinBelgeTurleris on new { s.MezuniyetYayinBelgeTurID } equals new { MezuniyetYayinBelgeTurID = (int?)yb.MezuniyetYayinBelgeTurID } into defyb
                              from ybD in defyb.DefaultIfEmpty()
                              join klk in db.MezuniyetYayinLinkTurleris on new { s.KaynakMezuniyetYayinLinkTurID } equals new { KaynakMezuniyetYayinLinkTurID = (int?)klk.MezuniyetYayinLinkTurID } into defklk
                              from klkD in defklk.DefaultIfEmpty()
                              join ym in db.MezuniyetYayinMetinTurleris on new { s.MezuniyetYayinMetinTurID } equals new { MezuniyetYayinMetinTurID = (int?)ym.MezuniyetYayinMetinTurID } into defym
                              from ymD in defym.DefaultIfEmpty()
                              join kl in db.MezuniyetYayinLinkTurleris on new { s.YayinMezuniyetYayinLinkTurID } equals new { YayinMezuniyetYayinLinkTurID = (int?)kl.MezuniyetYayinLinkTurID } into defkl
                              from klD in defkl.DefaultIfEmpty()
                              join inx in db.MezuniyetYayinIndexTurleris on new { qs.MezuniyetYayinIndexTurID } equals new { MezuniyetYayinIndexTurID = (int?)inx.MezuniyetYayinIndexTurID } into definx
                              from inxD in definx.DefaultIfEmpty()
                              select new YayinBilgiModel
                              {
                                  Yayinlanmis = qs.Yayinlanmis,
                                  MezuniyetBasvurulariYayinID = qs.MezuniyetBasvurulariYayinID,
                                  YayinBasligi = qs.YayinBasligi,
                                  MezuniyetYayinTarih = qs.MezuniyetYayinTarih,
                                  MezuniyetYayinTarihZorunlu = s.TarihIstensin,
                                  MezuniyetYayinTurID = qs.MezuniyetYayinTurID,
                                  MezuniyetYayinTurAdi = sd.MezuniyetYayinTurAdi,
                                  MezuniyetYayinBelgeTurID = s.MezuniyetYayinBelgeTurID,
                                  MezuniyetYayinBelgeTurAdi = ybD != null ? ybD.BelgeTurAdi : "",
                                  MezuniyetYayinBelgeAdi = ybD != null ? qs.MezuniyetYayinBelgeAdi : "",
                                  MezuniyetYayinBelgeDosyaYolu = ybD != null ? qs.MezuniyetYayinBelgeDosyaYolu : "",
                                  MezuniyetYayinBelgeTurZorunlu = s.BelgeZorunlu,
                                  MezuniyetYayinKaynakLinkTurID = s.KaynakMezuniyetYayinLinkTurID,
                                  MezuniyetYayinKaynakLinkTurAdi = klkD != null ? klkD.LinkTurAdi : "",
                                  MezuniyetYayinKaynakLinkIsUrl = klkD != null ? klkD.IsUrl : false,
                                  MezuniyetYayinKaynakLinkTurZorunlu = s.KaynakLinkiZorunlu,
                                  MezuniyetYayinMetinTurID = s.MezuniyetYayinMetinTurID,
                                  MezuniyetYayinMetinTurAdi = ymD != null ? ymD.MetinTurAdi : "",
                                  MezuniyetYayinMetniBelgeAdi = ymD != null ? qs.MezuniyetYayinMetniBelgeAdi : "",
                                  MezuniyetYayinMetniBelgeYolu = qs.MezuniyetYayinMetniBelgeYolu,
                                  MezuniyetYayinMetinZorunlu = s.MetinZorunlu,
                                  MezuniyetYayinLinkTurID = s.YayinMezuniyetYayinLinkTurID,
                                  MezuniyetYayinLinkTurAdi = klD != null ? klD.LinkTurAdi : "",
                                  MezuniyetYayinLinkIsUrl = klD != null ? klD.IsUrl : false,
                                  MezuniyetYayinLinkiZorunlu = s.YayinLinkiZorunlu,
                                  MezuniyetYayinKaynakLinki = qs.MezuniyetYayinKaynakLinki,
                                  MezuniyetYayinLinki = qs.MezuniyetYayinLinki,
                                  MezuniyetYayinIndexTurZorunlu = s.YayinIndexTurIstensin,
                                  MezuniyetYayinIndexTurAdi = inxD != null ? inxD.IndexTurAdi : "",
                                  MezuniyetKabulEdilmisMakaleZorunlu = s.YayinKabulEdilmisMakaleIstensin,
                                  MezuniyetYayinKabulEdilmisMakaleAdi = qs.MezuniyetYayinKabulEdilmisMakaleAdi,
                                  MezuniyetYayinKabulEdilmisMakaleDosyaYolu = qs.MezuniyetYayinKabulEdilmisMakaleDosyaYolu,
                                  YayinDeatKurulusIstensin = s.YayinDeatKurulusIstensin,
                                  ProjeDeatKurulus = qs.ProjeDeatKurulus,
                                  YayinDergiAdiIstensin = s.YayinDergiAdiIstensin,
                                  DergiAdi = qs.DergiAdi,
                                  YayinMevcutDurumIstensin = s.YayinMevcutDurumIstensin,
                                  IsProjeTamamlandiOrDevamEdiyor = qs.IsProjeTamamlandiOrDevamEdiyor,
                                  YayinProjeEkibiIstensin = s.YayinProjeEkibiIstensin,
                                  ProjeEkibi = qs.ProjeEkibi,
                                  YayinProjeTurIstensin = s.YayinProjeTurIstensin,
                                  ProjeTurAdi = qs.MezuniyetYayinProjeTurID.HasValue ? qs.MezuniyetYayinProjeTurleri.ProjeTurAdi : "",
                                  YayinYazarlarIstensin = s.YayinYazarlarIstensin,
                                  YazarAdi = qs.YazarAdi,
                                  YayinYilCiltSayiIstensin = s.YayinYilCiltSayiIstensin,
                                  YilCiltSayiSS = qs.YilCiltSayiSS,
                                  IsTarihAraligiIstensin = s.IsTarihAraligiIstensin,
                                  TarihAraligi = qs.TarihAraligi


                              }).ToList();

                model.MezuniyetBasvuruYayinlari = yayins;
                if (basvuru.MezuniyetYayinKontrolDurumID == MezuniyetYayinKontrolDurumu.Onaylandi) model.Onaylandi = true;

            }
            return model;

        }
        public static string IsSuccessSinavPuanUye(this string SinavPuani, bool SinavPuanKontroluYap, int PuanKriteri)
        {
            string Msg = "";
            if (SinavPuani.IsNullOrWhiteSpace())
            {
                Msg = "Dil Sınavı puanı bilgisi boş bırakılamaz";
            }
            else
            {
                if (SinavPuanKontroluYap)
                {
                    SinavPuani = SinavPuani.Replace(" ", "").Replace(".", ",");
                    var IsSinavPuaniSayi = SinavPuani.IsNumberX();
                    if (!IsSinavPuaniSayi)
                    {
                        Msg = "Dil Sınavı puanı girişi sayıdan oluşmalıdır.";
                    }
                    else
                    {
                        var Puan = Convert.ToDouble(SinavPuani);
                        if (PuanKriteri > Puan || Puan > 100)
                        {
                            Msg = "Dil Sınavı puanı girişi " + PuanKriteri + " ile 100 notları arasında olmalıdır.";
                        }
                    }
                }
            }
            return Msg;
        }
        public static bool ToTIUyeFormSuccessRow(this string JuriTipAdi, bool TezDiliTr, bool AdSoyadSuccess, bool UnvanAdiSuccess, bool EMailSuccess, bool UniversiteIDSuccess, bool IsAnabilimdaliProgramAdiSuccess, bool IsDilSinaviOrUniversiteSuccess, bool DilSinavAdiSuccess, bool DilPuaniSuccess, bool SinavTarihiSuccess)
        {
            bool retVal = AdSoyadSuccess && UnvanAdiSuccess && EMailSuccess && UniversiteIDSuccess && IsAnabilimdaliProgramAdiSuccess && IsDilSinaviOrUniversiteSuccess && DilSinavAdiSuccess && DilPuaniSuccess && SinavTarihiSuccess;

            return retVal;
        }

        public static bool ToJOFormSuccessRow(this string JuriTipAdi, bool TezDiliTr, bool AdSoyadSuccess, bool UnvanAdiSuccess, bool EMailSuccess, bool UniversiteIDSuccess, bool UzmanlikAlaniSuccess, bool BilimselCalismalarAnahtarSozcuklerSuccess, bool DilSinavAdiSuccess, bool DilPuaniSuccess)
        {
            bool retVal = false;
            if (new List<string> { "YtuIciJuri4", "YtuDisiJuri4" }.Contains(JuriTipAdi))
            {
                if (TezDiliTr)
                {
                    retVal = (
                                (
                                    AdSoyadSuccess &&
                                    UnvanAdiSuccess &&
                                    EMailSuccess &&
                                    UniversiteIDSuccess &&
                                    UzmanlikAlaniSuccess &&
                                    BilimselCalismalarAnahtarSozcuklerSuccess
                                 )
                                 ||
                                 (
                                     !AdSoyadSuccess &&
                                     !UnvanAdiSuccess &&
                                     !EMailSuccess &&
                                     !UniversiteIDSuccess &&
                                     !UzmanlikAlaniSuccess &&
                                     !BilimselCalismalarAnahtarSozcuklerSuccess

                                 )
                            );
                }
                else
                {
                    retVal = (
                               (
                                   AdSoyadSuccess &&
                                   UnvanAdiSuccess &&
                                   EMailSuccess &&
                                   UniversiteIDSuccess &&
                                   UzmanlikAlaniSuccess &&
                                   BilimselCalismalarAnahtarSozcuklerSuccess &&
                                   DilSinavAdiSuccess &&
                                   DilPuaniSuccess
                                )
                                ||
                                (
                                    !AdSoyadSuccess &&
                                    !UnvanAdiSuccess &&
                                    !EMailSuccess &&
                                    !UniversiteIDSuccess &&
                                    !UzmanlikAlaniSuccess &&
                                    !BilimselCalismalarAnahtarSozcuklerSuccess &&
                                    !DilSinavAdiSuccess &&
                                    !DilPuaniSuccess
                                )
                           );
                }

            }
            else
            {
                if (TezDiliTr)
                {
                    retVal = (
                                  AdSoyadSuccess &&
                                  UnvanAdiSuccess &&
                                  EMailSuccess &&
                                  UniversiteIDSuccess &&
                                  UzmanlikAlaniSuccess &&
                                  BilimselCalismalarAnahtarSozcuklerSuccess
                              );
                }
                else
                {
                    retVal = (
                                 AdSoyadSuccess &&
                                 UnvanAdiSuccess &&
                                 EMailSuccess &&
                                 UniversiteIDSuccess &&
                                 UzmanlikAlaniSuccess &&
                                 BilimselCalismalarAnahtarSozcuklerSuccess &&
                                 DilSinavAdiSuccess &&
                                 DilPuaniSuccess
                             );
                }
            }
            return retVal;
        }


        public static basvuruDetayModelMezuniyet getSecilenBasvuruMezuniyetDetay(int MezuniyetBasvurulariID, int? MezuniyetBasvurulariYayinID = null, int? ShowDetayYayinID = null)
        {
            var model = new basvuruDetayModelMezuniyet();
            using (var db = new LisansustuBasvuruSistemiEntities())
            {

                var basvuru = db.MezuniyetBasvurularis.Where(p => p.MezuniyetBasvurulariID == MezuniyetBasvurulariID).First();

                var bsurec = basvuru.MezuniyetSureci;
                var bSurecOtKriter = bsurec.MezuniyetSureciOgrenimTipKriterleris.Where(p => p.OgrenimTipKod == basvuru.OgrenimTipKod).First();
                var enstitu = db.Enstitulers.Where(p => p.EnstituKod == bsurec.EnstituKod).First();

                var EslesenDanisman = db.Kullanicilars.Where(p => p.KullaniciID == (basvuru.TezDanismanID ?? 0)).FirstOrDefault();
                if (EslesenDanisman != null)
                {
                    model.TezDanismanBilgiEslesen = EslesenDanisman.Unvanlar.UnvanAdi + " " + EslesenDanisman.Ad + " " + EslesenDanisman.Soyad;
                }
                else
                {
                    model.TezDanismanBilgiEslesen = "Sistemde eşleşen tez danışmanı bulunamadı.";
                }
                model.MezuniyetSinavDurumID = basvuru.MezuniyetSinavDurumID;
                model.RowID = basvuru.RowID;
                model.MezuniyetBasvurulariTezDosyalaris = basvuru.MezuniyetBasvurulariTezDosyalaris;
                var OnayYapanIDs = model.MezuniyetBasvurulariTezDosyalaris.Where(p => p.OnayYapanID.HasValue).Select(s => s.OnayYapanID).ToList();
                var Kuls = db.Kullanicilars.Where(p => OnayYapanIDs.Contains(p.KullaniciID)).ToList();
                foreach (var item in model.MezuniyetBasvurulariTezDosyalaris.Where(p => p.OnayYapanID.HasValue))
                {
                    var Kul = Kuls.Where(p => p.KullaniciID == item.IslemYapanID).First();
                    item.IslemYapanIP = Kul.Ad + " " + Kul.Soyad;

                }
                model.TezDanismanID = basvuru.TezDanismanID;
                model.IsAnketVar = bsurec.AnketID.HasValue;
                model.IsAnketDolduruldu = basvuru.AnketCevaplaris.Any();
                model.MezuniyetBasvurulariID = basvuru.MezuniyetBasvurulariID;
                model.MezuniyetSurecID = basvuru.MezuniyetSurecID;
                model.BasvuruTarihi = basvuru.BasvuruTarihi;
                model.MezuniyetYayinKontrolDurumID = basvuru.MezuniyetYayinKontrolDurumID;
                model.MezuniyetYayinKontrolDurumAciklamasi = basvuru.MezuniyetYayinKontrolDurumAciklamasi;
                model.KullaniciID = basvuru.KullaniciID;
                model.KayitDonemi = basvuru.KayitOgretimYiliBaslangic + "/" + (basvuru.KayitOgretimYiliBaslangic + 1) + " " + db.Donemlers.Where(p => p.DonemID == basvuru.KayitOgretimYiliDonemID.Value).First().DonemAdi;
                model.KullaniciTipID = basvuru.KullaniciTipID;
                model.ResimAdi = basvuru.ResimAdi;
                model.Ad = basvuru.Ad;
                model.Soyad = basvuru.Soyad;
                model.TcKimlikNo = basvuru.TcKimlikNo;
                model.PasaportNo = basvuru.PasaportNo;
                model.UyrukKod = basvuru.UyrukKod;
                model.OgrenciNo = basvuru.OgrenciNo;
                model.OgrenimTipAdi = db.OgrenimTipleris.Where(p => p.EnstituKod == bsurec.EnstituKod && p.OgrenimTipKod == basvuru.OgrenimTipKod).First().OgrenimTipAdi;
                var progLng = basvuru.Programlar;
                model.AnabilimdaliAdi = progLng.AnabilimDallari.AnabilimDaliAdi;
                model.ProgramAdi = progLng.ProgramAdi;
                model.OgrenimDurumID = basvuru.OgrenimDurumID;
                model.OgrenimTipKod = basvuru.OgrenimTipKod;
                model.ProgramKod = basvuru.ProgramKod;
                model.KayitOgretimYiliBaslangic = basvuru.KayitOgretimYiliBaslangic;
                model.KayitOgretimYiliDonemID = basvuru.KayitOgretimYiliDonemID;
                model.KayitTarihi = basvuru.KayitTarihi;
                model.IsTezDiliTr = basvuru.IsTezDiliTr;
                model.TezBaslikTr = basvuru.TezBaslikTr;
                model.TezBaslikEn = basvuru.TezBaslikEn;
                model.IsDanismanOnay = basvuru.IsDanismanOnay;
                model.DanismanOnayTarihi = basvuru.DanismanOnayTarihi;
                model.DanismanOnayAciklama = basvuru.DanismanOnayAciklama;
                model.TezDanismanUnvani = basvuru.TezDanismanUnvani;
                model.TezDanismanAdi = basvuru.TezDanismanUnvani + " " + basvuru.TezDanismanAdi;
                model.TezEsDanismanUnvani = basvuru.TezEsDanismanUnvani;
                if (!basvuru.TezEsDanismanUnvani.IsNullOrWhiteSpace())
                    model.TezEsDanismanAdi = basvuru.TezEsDanismanUnvani + " " + basvuru.TezEsDanismanAdi;
                model.TezEsDanismanEMail = basvuru.TezEsDanismanEMail;
                model.TezOzet = basvuru.TezOzet;
                model.OzetAnahtarKelimeler = basvuru.OzetAnahtarKelimeler;
                model.TezOzetHtml = basvuru.TezOzetHtml;
                model.TezAbstract = basvuru.TezAbstract;
                model.TezAbstractHtml = basvuru.TezAbstractHtml;
                model.AbstractAnahtarKelimeler = basvuru.AbstractAnahtarKelimeler;
                model.DanismanImzaliFormDosyaYolu = basvuru.DanismanImzaliFormDosyaYolu;
                model.DanismanImzaliFormDosyaAdi = basvuru.DanismanImzaliFormDosyaAdi;
                model.IsYerli = basvuru.KullaniciTipleri.Yerli;
                model.EnstituAdi = enstitu.EnstituAd;
                model.MezuniyetBasvurulariID = basvuru.MezuniyetBasvurulariID;
                model.MezuniyetSureci = basvuru.MezuniyetSureci;
                model.MezuniyetSurecID = basvuru.MezuniyetSurecID;
                model.KullaniciID = basvuru.KullaniciID;
                model.BasvuruTarihi = basvuru.BasvuruTarihi;
                model.MezuniyetYayinKontrolDurumID = basvuru.MezuniyetYayinKontrolDurumID;
                model.MezuniyetYayinKontrolDurumAciklamasi = basvuru.MezuniyetYayinKontrolDurumAciklamasi;

                model.IslemTarihi = basvuru.IslemTarihi;
                model.IslemYapanID = basvuru.IslemYapanID;
                model.IslemYapanIP = basvuru.IslemYapanIP;
                var nowDate = DateTime.Now;
                model.BasvuruSureciTarihi = bsurec.BaslangicYil + "/" + bsurec.BitisYil + " " + db.Donemlers.Where(p => p.DonemID == bsurec.DonemID).First().DonemAdi + " (" + bsurec.BaslangicTarihi.ToDateString() + "-" + bsurec.BitisTarihi.ToDateString() + ")";
                model.sonucGirisSureciAktif = bsurec.BaslangicTarihi <= nowDate && bsurec.BitisTarihi >= nowDate;
                model.IsMezunOldu = basvuru.IsMezunOldu;
                model.MezuniyetTarihi = basvuru.MezuniyetTarihi;
                model.EYKTarihi = basvuru.EYKTarihi;
                model.TezTeslimSonTarih = basvuru.TezTeslimSonTarih;
                model.MezuniyetJuriOneriFormlaris = db.MezuniyetJuriOneriFormlaris.Include("MezuniyetJuriOneriFormuJurileris").Where(p => p.MezuniyetBasvurulariID == basvuru.MezuniyetBasvurulariID).ToList();


                foreach (var item in model.MezuniyetJuriOneriFormlaris.SelectMany(s => s.MezuniyetJuriOneriFormuJurileris).Where(p => p.UniversiteID.HasValue))
                {

                    item.UniversiteAdi = item.Universiteler.Ad;
                }


                model.EYKYaGonderildi = model.MezuniyetJuriOneriFormlaris.Select(s => s.EYKYaGonderildi).FirstOrDefault();
                model.EYKDaOnaylandi = model.MezuniyetJuriOneriFormlaris.Select(s => s.EYKDaOnaylandi).FirstOrDefault();
                var yayins = (from qs in db.MezuniyetBasvurulariYayins.Where(p => p.MezuniyetBasvurulariID == MezuniyetBasvurulariID)
                              join s in db.MezuniyetSureciYayinTurleris on new { qs.MezuniyetBasvurulari.MezuniyetSurecID, qs.MezuniyetYayinTurID } equals new { s.MezuniyetSurecID, s.MezuniyetYayinTurID }
                              join sd in db.MezuniyetYayinTurleris on new { s.MezuniyetYayinTurID } equals new { sd.MezuniyetYayinTurID }
                              join yb in db.MezuniyetYayinBelgeTurleris on new { s.MezuniyetYayinBelgeTurID } equals new { MezuniyetYayinBelgeTurID = (int?)yb.MezuniyetYayinBelgeTurID } into defyb
                              from ybD in defyb.DefaultIfEmpty()
                              join klk in db.MezuniyetYayinLinkTurleris on new { s.KaynakMezuniyetYayinLinkTurID } equals new { KaynakMezuniyetYayinLinkTurID = (int?)klk.MezuniyetYayinLinkTurID } into defklk
                              from klkD in defklk.DefaultIfEmpty()
                              join ym in db.MezuniyetYayinMetinTurleris on new { s.MezuniyetYayinMetinTurID } equals new { MezuniyetYayinMetinTurID = (int?)ym.MezuniyetYayinMetinTurID } into defym
                              from ymD in defym.DefaultIfEmpty()
                              join kl in db.MezuniyetYayinLinkTurleris on new { s.YayinMezuniyetYayinLinkTurID } equals new { YayinMezuniyetYayinLinkTurID = (int?)kl.MezuniyetYayinLinkTurID } into defkl
                              from klD in defkl.DefaultIfEmpty()
                              join inx in db.MezuniyetYayinIndexTurleris on new { qs.MezuniyetYayinIndexTurID } equals new { MezuniyetYayinIndexTurID = (int?)inx.MezuniyetYayinIndexTurID } into definx
                              from inxD in definx.DefaultIfEmpty()
                              select new YayinBilgiModel
                              {
                                  MezuniyetYayinTurID = qs.MezuniyetYayinTurID,
                                  ShowDetayYayinID = ShowDetayYayinID,
                                  MezuniyetBasvurulariYayinID = qs.MezuniyetBasvurulariYayinID,
                                  MezuniyetBasvurulariID = qs.MezuniyetBasvurulariID,
                                  DanismanIsmiVar = qs.DanismanIsmiVar,
                                  TezIcerikUyumuVar = qs.TezIcerikUyumuVar,
                                  Onaylandi = qs.Onaylandi,
                                  RetAciklamasi = qs.RetAciklamasi,
                                  YayinBasligi = qs.YayinBasligi,
                                  Yayinlanmis = qs.Yayinlanmis,
                                  MezuniyetYayinTarih = qs.MezuniyetYayinTarih,
                                  MezuniyetYayinTarihZorunlu = s.TarihIstensin,
                                  MezuniyetYayinTurAdi = sd.MezuniyetYayinTurAdi,
                                  MezuniyetYayinBelgeTurID = s.MezuniyetYayinBelgeTurID,
                                  MezuniyetYayinBelgeTurAdi = ybD != null ? ybD.BelgeTurAdi : "",
                                  MezuniyetYayinBelgeAdi = ybD != null ? qs.MezuniyetYayinBelgeAdi : "",
                                  MezuniyetYayinBelgeDosyaYolu = ybD != null ? qs.MezuniyetYayinBelgeDosyaYolu : "",
                                  MezuniyetYayinBelgeTurZorunlu = s.BelgeZorunlu,
                                  MezuniyetYayinKaynakLinkTurID = s.KaynakMezuniyetYayinLinkTurID,
                                  MezuniyetYayinKaynakLinkTurAdi = klkD != null ? klkD.LinkTurAdi : "",
                                  MezuniyetYayinKaynakLinkIsUrl = klkD != null ? klkD.IsUrl : false,
                                  MezuniyetYayinKaynakLinkTurZorunlu = s.KaynakLinkiZorunlu,
                                  MezuniyetYayinMetinTurID = s.MezuniyetYayinMetinTurID,
                                  MezuniyetYayinMetinTurAdi = ymD != null ? ymD.MetinTurAdi : "",
                                  MezuniyetYayinMetniBelgeAdi = ymD != null ? qs.MezuniyetYayinMetniBelgeAdi : "",
                                  MezuniyetYayinMetniBelgeYolu = qs.MezuniyetYayinMetniBelgeYolu,
                                  MezuniyetYayinMetinZorunlu = s.MetinZorunlu,
                                  MezuniyetYayinLinkTurID = s.YayinMezuniyetYayinLinkTurID,
                                  MezuniyetYayinLinkTurAdi = klD != null ? klD.LinkTurAdi : "",
                                  MezuniyetYayinLinkIsUrl = klD != null ? klD.IsUrl : false,
                                  MezuniyetYayinLinkiZorunlu = s.YayinLinkiZorunlu,
                                  MezuniyetYayinKaynakLinki = qs.MezuniyetYayinKaynakLinki,
                                  MezuniyetYayinLinki = qs.MezuniyetYayinLinki,
                                  MezuniyetYayinIndexTurZorunlu = s.YayinIndexTurIstensin,
                                  MezuniyetYayinIndexTurAdi = inxD != null ? inxD.IndexTurAdi : "",
                                  MezuniyetYayinIndexTurID = qs.MezuniyetYayinIndexTurID,
                                  YayinIndexTurleri = db.MezuniyetYayinIndexTurleris.ToList(),
                                  MezuniyetKabulEdilmisMakaleZorunlu = s.YayinKabulEdilmisMakaleIstensin,
                                  MezuniyetYayinKabulEdilmisMakaleAdi = qs.MezuniyetYayinKabulEdilmisMakaleAdi,
                                  MezuniyetYayinKabulEdilmisMakaleDosyaYolu = qs.MezuniyetYayinKabulEdilmisMakaleDosyaYolu,
                                  YayinDeatKurulusIstensin = s.YayinDeatKurulusIstensin,
                                  ProjeDeatKurulus = qs.ProjeDeatKurulus,
                                  YayinDergiAdiIstensin = s.YayinDergiAdiIstensin,
                                  DergiAdi = qs.DergiAdi,
                                  YayinMevcutDurumIstensin = s.YayinMevcutDurumIstensin,
                                  IsProjeTamamlandiOrDevamEdiyor = qs.IsProjeTamamlandiOrDevamEdiyor,
                                  YayinProjeEkibiIstensin = s.YayinProjeEkibiIstensin,
                                  ProjeEkibi = qs.ProjeEkibi,
                                  YayinProjeTurIstensin = s.YayinProjeTurIstensin,
                                  MezuniyetYayinProjeTurID = qs.MezuniyetYayinProjeTurID,
                                  ProjeTurAdi = qs.MezuniyetYayinProjeTurID.HasValue ? qs.MezuniyetYayinProjeTurleri.ProjeTurAdi : "",
                                  YayinYazarlarIstensin = s.YayinYazarlarIstensin,
                                  YazarAdi = qs.YazarAdi,
                                  YayinYilCiltSayiIstensin = s.YayinYilCiltSayiIstensin,
                                  YilCiltSayiSS = qs.YilCiltSayiSS,
                                  IsTarihAraligiIstensin = s.IsTarihAraligiIstensin,
                                  TarihAraligi = qs.TarihAraligi,
                                  YayinYerBilgisiIstensin = s.YayinYerBilgisiIstensin,
                                  YerBilgisi = qs.YerBilgisi,
                                  YayinEtkinlikAdiIstensin = s.YayinEtkinlikAdiIstensin,
                                  EtkinlikAdi = qs.EtkinlikAdi


                              });

                if (MezuniyetBasvurulariYayinID.HasValue)
                {
                    model.SelectedYayin = yayins.Where(p => p.MezuniyetBasvurulariYayinID == MezuniyetBasvurulariYayinID).First();
                }
                else
                {
                    model.YayinBilgileri = yayins.ToList();
                }

                #region SalonRezervasyonlari 
                model.MezuniyetSRModel.SalonRezervasyonlari = (from s in db.SRTalepleris
                                                               join tt in db.SRTalepTipleris on s.SRTalepTipID equals tt.SRTalepTipID
                                                               join mb in db.MezuniyetBasvurularis on s.MezuniyetBasvurulariID equals mb.MezuniyetBasvurulariID
                                                               join sal in db.SRSalonlars on s.SRSalonID equals sal.SRSalonID into def1
                                                               from defSl in def1.DefaultIfEmpty()
                                                               join hg in db.HaftaGunleris on s.HaftaGunID equals hg.HaftaGunID
                                                               join d in db.SRDurumlaris on s.SRDurumID equals d.SRDurumID
                                                               join sd in db.MezuniyetSinavDurumlaris on (s.MezuniyetSinavDurumID ?? MezuniyetSinavDurum.SonucGirilmedi) equals sd.MezuniyetSinavDurumID into def2
                                                               from defSD in def2.DefaultIfEmpty()
                                                               join sdj in db.MezuniyetSinavDurumlaris on (s.JuriSonucMezuniyetSinavDurumID ?? MezuniyetSinavDurum.SonucGirilmedi) equals sdj.MezuniyetSinavDurumID into def3
                                                               from defsdj in def3.DefaultIfEmpty()
                                                               let jof = mb.MezuniyetJuriOneriFormlaris.FirstOrDefault()
                                                               where s.MezuniyetBasvurulariID == basvuru.MezuniyetBasvurulariID
                                                               select new frTalepler
                                                               {
                                                                   SRTalepID = s.SRTalepID,
                                                                   UniqueID = s.UniqueID,
                                                                   TalepYapanID = s.TalepYapanID,
                                                                   TalepTipAdi = tt.TalepTipAdi,
                                                                   SRTalepTipID = s.SRTalepTipID,
                                                                   SRSalonID = s.SRSalonID,
                                                                   SalonAdi = s.SRSalonID.HasValue ? defSl.SalonAdi : s.SalonAdi,
                                                                   DegerlendirmeSonucMailTarihi = s.DegerlendirmeSonucMailTarihi,

                                                                   Tarih = s.Tarih,
                                                                   HaftaGunID = s.HaftaGunID,
                                                                   HaftaGunAdi = hg.HaftaGunAdi,
                                                                   BasSaat = s.BasSaat,
                                                                   BitSaat = s.BitSaat,
                                                                   MezuniyetSinavDurumID = s.MezuniyetSinavDurumID,
                                                                   MezuniyetSinavDurumIslemTarihi = s.MezuniyetSinavDurumIslemTarihi,
                                                                   MezuniyetSinavDurumIslemYapanID = s.MezuniyetSinavDurumIslemYapanID,
                                                                   SDurumAdi = defSD != null ? defSD.MezuniyetSinavDurumAdi : "",
                                                                   SDurumListeAdi = defSD != null ? defSD.MezuniyetSinavDurumAdi : "",
                                                                   SClassName = defSD != null ? defSD.ClassName : "",
                                                                   SColor = defSD != null ? defSD.Color : "",
                                                                   SRDurumID = s.SRDurumID,
                                                                   DurumAdi = d.DurumAdi,
                                                                   DurumListeAdi = d.DurumAdi,
                                                                   ClassName = d.ClassName,
                                                                   Color = d.Color,
                                                                   SRDurumAciklamasi = s.SRDurumAciklamasi,
                                                                   JuriSonucMezuniyetSinavDurumID = s.JuriSonucMezuniyetSinavDurumID,
                                                                   IsOyBirligiOrCouklugu = s.IsOyBirligiOrCouklugu,
                                                                   RSBaslatildiMailGonderimTarihi = s.RSBaslatildiMailGonderimTarihi,
                                                                   JuriSonucMezuniyetSinavDurumAdi = defsdj.MezuniyetSinavDurumAdi,
                                                                   IslemTarihi = s.IslemTarihi,
                                                                   IslemYapanID = s.IslemYapanID,
                                                                   IslemYapanIP = s.IslemYapanIP,
                                                                   IsTezBasligiDegisti = s.IsTezBasligiDegisti,
                                                                   IsTezDiliTr = mb.IsTezDiliTr == true,
                                                                   TezBaslikTr = jof.IsTezBasligiDegisti == true ? jof.YeniTezBaslikTr : mb.TezBaslikTr,
                                                                   TezBaslikEn = jof.IsTezBasligiDegisti == true ? jof.YeniTezBaslikEn : mb.TezBaslikEn,
                                                                   YeniTezBaslikTr = s.YeniTezBaslikTr,
                                                                   YeniTezBaslikEn = s.YeniTezBaslikEn,
                                                                   SRTaleplerJuris = s.SRTaleplerJuris.ToList(),
                                                                   IsSonSRTalebi = !mb.SRTalepleris.Any(a => a.SRTalepID > s.SRTalepID),
                                                                   // IslemYetkisiVar = !mb.SRTalepleris.Any(a => a.SRTalepID > s.SRTalepID) && (!mb.MezuniyetBasvurulariTezDosyalaris.Any(a => a.IsOnaylandiOrDuzeltme == true)),
                                                                   SRTalepleriBezCiltFormus = s.SRTalepleriBezCiltFormus,
                                                                   UzatmaSonSRTarih = s.MezuniyetSinavDurumID == MezuniyetSinavDurum.Uzatma ? (EntityFunctions.AddDays(s.Tarih, bSurecOtKriter.MBSinavUzatmaSuresiGun).Value) : DateTime.Now,
                                                                   TeslimSonTarih = model.TezTeslimSonTarih ?? EntityFunctions.AddDays(s.Tarih, bSurecOtKriter.MBTezTeslimSuresiGun).Value,
                                                                   IsOgrenciUzatmaSonrasiOnay = s.IsOgrenciUzatmaSonrasiOnay,
                                                                   OgrenciOnayTarihi = s.OgrenciOnayTarihi,
                                                                   IsDanismanUzatmaSonrasiOnay = s.IsDanismanUzatmaSonrasiOnay,
                                                                   DanismanOnayTarihi = s.DanismanOnayTarihi,
                                                                   DanismanUzatmaSonrasiOnayAciklama = s.DanismanUzatmaSonrasiOnayAciklama,
                                                                   IsYokDrBursiyeriVar = s.IsYokDrBursiyeriVar,
                                                                   YokDrOncelikliAlan = s.YokDrOncelikliAlan

                                                               }).OrderByDescending(o => o.SRTalepID).ToList();
                foreach (var item in model.MezuniyetSRModel.SalonRezervasyonlari)
                {

                    item.SrDurumSelectList.SRDurumID = new SelectList(Management.cmbSRDurumListe(false), "Value", "Caption", item.SRDurumID);
                    item.SrDurumSelectList.MezuniyetSinavDurumID = new SelectList(Management.cmbMzSinavDurumListe(), "Value", "Caption", item.MezuniyetSinavDurumID);

                }
                model.MezuniyetDurumSelectList.IsMezunOldu = new SelectList(Management.cmbMezuniyetDurumListe(), "Value", "Caption", model.IsMezunOldu);
                var SonRezervasyon = model.MezuniyetSRModel.SalonRezervasyonlari.OrderByDescending(o => o.SRTalepID).FirstOrDefault();
                if (SonRezervasyon != null)
                {
                    if (SonRezervasyon.MezuniyetSinavDurumID == MezuniyetSinavDurum.Uzatma && SonRezervasyon.IsDanismanUzatmaSonrasiOnay == true)
                    {
                        var SonTarih = SonRezervasyon.Tarih.AddDays(bSurecOtKriter.MBSinavUzatmaSuresiGun);
                    }
                }


                model.MezuniyetSRModel.EykIlkSrMaxTarih = model.EYKTarihi.HasValue ? (model.TezTeslimSonTarih ?? model.EYKTarihi.Value.AddDays(bSurecOtKriter.MBTezTeslimSuresiGun)) : (DateTime?)null;
                model.MezuniyetSRModel.IsSrEykSureAsimi = model.EYKTarihi.HasValue && model.MezuniyetSRModel.EykIlkSrMaxTarih < DateTime.Now;


                #endregion

                if (model.IsAnketDolduruldu == false)
                {
                    if (bsurec.AnketID.HasValue)
                    {

                        if (!db.AnketCevaplaris.Any(a => a.MezuniyetBasvurulariID == MezuniyetBasvurulariID))
                        {

                            var anketSorulari = (from bsa in db.Ankets.Where(p => p.AnketID == bsurec.AnketID)
                                                 join aso in db.AnketSorus on bsa.AnketID equals aso.AnketID
                                                 join sb in db.AnketCevaplaris.Where(p => p.MezuniyetBasvurulariID == MezuniyetBasvurulariID && p.Basvurular.KullaniciID == basvuru.KullaniciID) on aso.AnketSoruID equals sb.AnketSoruID into def1
                                                 from sbc in def1.DefaultIfEmpty()
                                                 select new
                                                 {
                                                     aso.AnketSoruID,
                                                     AnketSoruSecenekID = sbc != null ? sbc.AnketSoruSecenekID : (int?)null,
                                                     aso.IsTabloVeriGirisi,
                                                     aso.IsTabloVeriMaxSatir,
                                                     Aciklama = sbc != null ? sbc.EkAciklama : "",
                                                     aso.SiraNo,
                                                     aso.SoruAdi,
                                                     Secenekler = aso.AnketSoruSeceneks.Select(ss => new
                                                     {
                                                         ss.AnketSoruSecenekID,
                                                         ss.AnketSoruID,
                                                         ss.SiraNo,
                                                         ss.IsYaziOrSayi,
                                                         ss.IsEkAciklamaGir,
                                                         ss.SecenekAdi
                                                     }).OrderBy(o => o.SiraNo).ToList()


                                                 }).OrderBy(o => o.SiraNo).ToList();
                            var modelAnk = new kmAnketlerCevap();
                            modelAnk.RowID = basvuru.RowID.ToString();
                            modelAnk.AnketTipID = 4;
                            modelAnk.BasvuruSurecID = bsurec.MezuniyetSurecID;
                            modelAnk.AnketID = bsurec.AnketID.Value;
                            modelAnk.JsonStringData = anketSorulari.toJsonText();
                            foreach (var item in anketSorulari)
                            {
                                modelAnk.AnketCevapModel.Add(new AnketCevapModel
                                {
                                    SecilenAnketSoruSecenekID = item.AnketSoruSecenekID,
                                    SoruBilgi = new frAnketDetay { AnketSoruID = item.AnketSoruID, SoruAdi = item.SoruAdi, SiraNo = item.SiraNo, Aciklama = item.Aciklama, IsTabloVeriGirisi = item.IsTabloVeriGirisi, IsTabloVeriMaxSatir = item.IsTabloVeriMaxSatir },
                                    SoruSecenek = item.Secenekler.Select(s => new frAnketSecenekDetay { AnketSoruSecenekID = s.AnketSoruSecenekID, SiraNo = s.SiraNo, IsEkAciklamaGir = s.IsEkAciklamaGir, SecenekAdi = s.SecenekAdi }).ToList(),
                                    SelectListSoruSecenek = new SelectList(item.Secenekler.ToList(), "AnketSoruSecenekID", "SecenekAdi", item.AnketSoruSecenekID)
                                });
                            }

                            model.AnketView = Management.RenderPartialView("Ajax", "getAnket", modelAnk);
                        }
                    }
                }

                var bdurum = basvuru.MezuniyetYayinKontrolDurumlari;
                model.MezuniyetYayinKontrolDurumAdi = bdurum.MezuniyetYayinKontrolDurumAdi;
                model.DurumClassName = bdurum.ClassName;
                model.DurumColor = bdurum.Color;
                model.MezuniyetYayinKontrolDurumAciklamasi = basvuru.MezuniyetYayinKontrolDurumAciklamasi;
            }
            return model;

        }



        #endregion

        #region SendMails
        public static List<System.Net.Mail.Attachment> exportRaporPdf(int raporTipID, List<int?> DataID)
        {

            var mdl = new List<System.Net.Mail.Attachment>();

            var ms = new MemoryStream();

            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                if (raporTipID == RaporTipleri.Basvuru)
                {

                    var BasvuruID = DataID[0].Value;
                    var terch = db.BasvurularTercihleris.Where(p => p.BasvuruID == BasvuruID).ToList();
                    rprBasvuruDoktora rpr = null;
                    rprBasvuruYL rpr2 = null;
                    var gd = Guid.NewGuid().ToString().Substr(0, 5);

                    var DoktoraTercihleri = terch.Where(p => p.OgrenimTipKod == OgrenimTipi.Doktra).ToList();
                    if (DoktoraTercihleri.Count > 0)
                    {
                        foreach (var item in DoktoraTercihleri)
                        {
                            var nrpr = new rprBasvuruDoktora(item.BasvuruTercihID);
                            nrpr.CreateDocument();

                            if (rpr == null) rpr = nrpr;
                            else rpr.Pages.AddRange(nrpr.Pages);

                            rpr.DisplayName = "DoktoraBF_" + gd + ".pdf";
                            rpr.ExportOptions.Pdf.Compressed = true;

                        }
                        ms = new MemoryStream();
                        rpr.ExportToPdf(ms);
                        rpr.ExportOptions.Pdf.Compressed = true;
                        ms.Seek(0, System.IO.SeekOrigin.Begin);
                        var attc = new System.Net.Mail.Attachment(ms, rpr.DisplayName, "application/pdf");

                        attc.ContentDisposition.ModificationDate = DateTime.Now;
                        mdl.Add(attc);
                    }
                    var YLTercihleri = terch.Where(p => p.OgrenimTipKod != OgrenimTipi.Doktra).OrderBy(o => o.SiraNo).ToList();
                    if (YLTercihleri.Count > 0)
                    {
                        if (YLTercihleri.Count == 1)
                        {
                            var nrpr2 = new rprBasvuruYL(YLTercihleri[0].BasvuruTercihID, null);
                            nrpr2.CreateDocument();
                            if (rpr2 == null) rpr2 = nrpr2;
                            else rpr2.Pages.AddRange(nrpr2.Pages);

                        }
                        else
                        {
                            var nrpr2 = new rprBasvuruYL(YLTercihleri[0].BasvuruTercihID, YLTercihleri[1].BasvuruTercihID);
                            nrpr2.CreateDocument();
                            if (rpr2 == null) rpr2 = nrpr2;
                            else rpr2.Pages.AddRange(nrpr2.Pages);
                        }
                        rpr2.DisplayName = "YüksekLisansBF_" + gd + ".pdf";
                        ms = new MemoryStream();
                        rpr2.ExportToPdf(ms);
                        rpr2.ExportOptions.Pdf.Compressed = true;
                        ms.Seek(0, System.IO.SeekOrigin.Begin);
                        var attc = new System.Net.Mail.Attachment(ms, rpr2.DisplayName, "application/pdf");
                        attc.ContentDisposition.ModificationDate = DateTime.Now;
                        mdl.Add(attc);
                    }




                }
                else if (raporTipID == RaporTipleri.MezuniyetBasvuruRaporu)
                {

                    var gd = Guid.NewGuid().ToString().Substr(0, 5);

                    var MezuniyetBasvurulariID = DataID[0].Value;
                    var rpr = new rprMezuniyetYayinSartiOnayiFormu(MezuniyetBasvurulariID);
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
                    var MB = db.MezuniyetBasvurularis.Where(p => p.MezuniyetBasvurulariID == MBID).First();
                    var rpr = new rprMezuniyetTezTeslimFormu_FR0338(MB.RowID, IlkOrIkinci == 1);

                    rpr.CreateDocument();
                    rpr.DisplayName = rpr.DisplayName + ".pdf";
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
                    var rpr = new rprMezuniyetTezDuzeltmeJuriUyelerineCiltliTezTeslimTutanagi_FR0329_FR0325(DataID[0].Value);

                    rpr.CreateDocument();
                    rpr.DisplayName = rpr.DisplayName + ".pdf";
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

                    var rpr = new rprTezSinavSonucTutanagi_FR0342_FR0377(SrTalep.UniqueID.Value);

                    rpr.CreateDocument();
                    rpr.DisplayName = rpr.DisplayName + ".pdf";
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
                    var rpr = new rprJuriUyelerineTezTeslimFormu_FR0341_FR0302(MezuniyetJuriOneriFormID);

                    rpr.CreateDocument();
                    rpr.DisplayName = rpr.DisplayName + ".pdf";
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
                    var rpr = new rprMezuniyetTezdenUretilenYayinlariDegerlendirmeFormu_FR0304(MezuniyetJuriOneriFormID, MezuniyetJuriOneriFormuJuriID);

                    rpr.CreateDocument();
                    rpr.DisplayName = rpr.DisplayName + ".pdf";
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
                    var rpr = new rprMezuniyetTezDegerlendirmeFormu_FR0303(MezuniyetJuriOneriFormID, MezuniyetJuriOneriFormuJuriID);

                    rpr.CreateDocument();
                    rpr.DisplayName = rpr.DisplayName + ".pdf";
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
                    var rpr = new rprMezuniyetTezKontrolFormu(null, ID);

                    rpr.CreateDocument();
                    rpr.DisplayName = rpr.DisplayName + ".pdf";
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
                    var rpr = new rprTIDegerlendirmeFormu_FR0307(ID);

                    rpr.CreateDocument();
                    rpr.DisplayName = rpr.DisplayName + ".pdf";
                    var IsSwhoRaporDetay = false;
                    if (DataID.Count > 1) IsSwhoRaporDetay = DataID[1].toIntToBooleanObj() ?? false;
                    if (IsSwhoRaporDetay)
                    {
                        var rpr2 = new rprTIDegerlendirmeFormuDetay_FR0307(ID);
                        rpr2.CreateDocument();
                        rpr2.DisplayName = rpr2.DisplayName + ".pdf";
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
                    var rpr = new rprTezDanismaniOneriFormu_FR0347(ID);

                    rpr.CreateDocument();
                    rpr.DisplayName = rpr.DisplayName + ".pdf";
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
                    var rpr = new rprTezEsDanismaniOneriFormu_FR0320(ID);

                    rpr.CreateDocument();
                    rpr.DisplayName = rpr.DisplayName + ".pdf";
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

        public static Exception YeniHesapMailGonder(Kullanicilar kModel, string sfr)
        {

            using (var db = new LisansustuBasvuruSistemiEntities())
            {

                var mailBilgi = EnstituMailInfo.GetEnstituMailBilgisi(kModel.EnstituKod);
                var mRowModel = new List<mailTableRow>();
                var enstitu = db.Enstitulers.Where(p => p.EnstituKod == kModel.EnstituKod).First();


                var _ea = mailBilgi.SistemErisimAdresi;
                var WurlAddr = _ea.Split('/').ToList();
                if (_ea.Contains("//"))
                    _ea = WurlAddr[0] + "//" + WurlAddr.Skip(2).Take(1).First();
                else
                    _ea = "http://" + WurlAddr.First();
                mRowModel.Add(new mailTableRow { Baslik = "Ad Soyad", Aciklama = kModel.Ad + " " + kModel.Soyad });

                if (kModel.BirimID.HasValue)
                {
                    var birim = db.Birimlers.Where(p => p.BirimID == kModel.BirimID).First();
                    mRowModel.Add(new mailTableRow { Baslik = "Birim", Aciklama = birim.BirimAdi });
                }
                if (kModel.UnvanID.HasValue)
                {
                    var unvan = db.Unvanlars.Where(p => p.UnvanID == kModel.UnvanID).First();
                    mRowModel.Add(new mailTableRow { Baslik = "Unvan", Aciklama = unvan.UnvanAdi });
                }
                if (kModel.SicilNo.IsNullOrWhiteSpace() == false) mRowModel.Add(new mailTableRow { Baslik = "Sicil No", Aciklama = kModel.SicilNo });
                if (kModel.TcKimlikNo.IsNullOrWhiteSpace() == false) mRowModel.Add(new mailTableRow { Baslik = "Tc kimlik No", Aciklama = kModel.TcKimlikNo });
                if (kModel.PasaportNo.IsNullOrWhiteSpace() == false) mRowModel.Add(new mailTableRow { Baslik = "Pasaport No", Aciklama = kModel.PasaportNo });
                if (kModel.CepTel.IsNullOrWhiteSpace() == false) mRowModel.Add(new mailTableRow { Baslik = "Cep Tel", Aciklama = kModel.CepTel });

                mRowModel.Add(new mailTableRow { Baslik = "Kullanıcı Adı", Aciklama = kModel.KullaniciAdi });
                mRowModel.Add(new mailTableRow { Baslik = "Şifre", Aciklama = kModel.IsActiveDirectoryUser ? "Email şifreniz ile aynı" : sfr });
                mRowModel.Add(new mailTableRow { Baslik = "Sistem Erişim Adresi", Aciklama = "<a href='" + mailBilgi.SistemErisimAdresi + "' target='_blank'>" + mailBilgi.SistemErisimAdresi + "</a>" });
                var mmmC = new mdlMailMainContent();

                mmmC.EnstituAdi = enstitu.EnstituAd;
                var mtc = new mailTableContent();
                mtc.AciklamaBasligi = "Kullanıcı hesabınız oluşturuldu. Sisteme Giriş Bilgisi Aşağıdaki Gibidir.";
                mtc.Detaylar = mRowModel;
                var tavleContent = Management.RenderPartialView("Ajax", "getMailTableContent", mtc);
                mmmC.Content = tavleContent;
                mmmC.LogoPath = _ea + "/Content/assets/images/ytu_logo_tr.png";
                mmmC.UniversiteAdi = "Yıldız Tekni Üniversitesi";
                string htmlMail = Management.RenderPartialView("Ajax", "getMailContent", mmmC);
                var User = mailBilgi.SmtpKullaniciAdi;
                var snded = MailManager.sendMailRetVal(kModel.EnstituKod, User, htmlMail, kModel.EMail, null);
                return snded;

            }
        }
        public static MmMessage sendMailTDOBilgisi(int TDOBasvuruDanismanID)
        {
            var mmMessage = new MmMessage();
            try
            {
                using (var db = new LisansustuBasvuruSistemiEntities())
                {
                    var TDOBasvuruDanisman = db.TDOBasvuruDanismen.Where(p => p.TDOBasvuruDanismanID == TDOBasvuruDanismanID).First();
                    var TDODanismanTalepTipID = TDOBasvuruDanisman.TDODanismanTalepTipID;
                    var TDOBasvuru = TDOBasvuruDanisman.TDOBasvuru;
                    var Ogrenci = TDOBasvuruDanisman.TDOBasvuru.Kullanicilar; 
                    var mModel = new List<SablonMailModel>();

                    if (TDODanismanTalepTipID == TDODanismanTalepTip.TezDanismaniOnerisi)
                    {
                         var Danisman = db.Kullanicilars.Where(p => p.KullaniciID == TDOBasvuruDanisman.TezDanismanID).First();
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Danışman",
                            EMails = new List<MailSendList> { new MailSendList { EMail = Danisman.EMail, ToOrBcc = true } },
                            MailSablonTipID = MailSablonTipi.TDO_DanismanOnerisiYapildiDanisman
                        });
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Öğrenci",
                            EMails = new List<MailSendList> { new MailSendList { EMail = Ogrenci.EMail, ToOrBcc = true } },
                            MailSablonTipID = MailSablonTipi.TDO_DanismanOnerisiYapildiOgrenci
                        });
                    }
                    else if (TDODanismanTalepTipID == TDODanismanTalepTip.TezDanismaniDegisikligi)
                    {
                         var VarolanDanisman = db.Kullanicilars.Where(p => p.KullaniciID == TDOBasvuruDanisman.VarolanTezDanismanID).First();
                        var YeniDanisman = db.Kullanicilars.Where(p => p.KullaniciID == TDOBasvuruDanisman.TezDanismanID).First();
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Varolan Danışman",
                            EMails = new List<MailSendList> { new MailSendList { EMail = VarolanDanisman.EMail, ToOrBcc = true } },
                            MailSablonTipID = MailSablonTipi.TDO_TezDanismanDegisikligiVarolanDanisman
                        });
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Yeni Danışman",
                            EMails = new List<MailSendList> { new MailSendList { EMail = YeniDanisman.EMail, ToOrBcc = true } },
                            MailSablonTipID = MailSablonTipi.TDO_TezDanismanDegisikligiYeniDanisman
                        });
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Öğrenci",
                            EMails = new List<MailSendList> { new MailSendList { EMail = Ogrenci.EMail, ToOrBcc = true } },
                            MailSablonTipID = MailSablonTipi.TDO_TezDanismanDegisikligiOgrenci
                        });
                    }
                    if (TDODanismanTalepTipID == TDODanismanTalepTip.TezDanismaniVeBaslikDegisikligi)
                    {
                          var VarolanDanisman = db.Kullanicilars.Where(p => p.KullaniciID == TDOBasvuruDanisman.VarolanTezDanismanID).First();
                        var YeniDanisman = db.Kullanicilars.Where(p => p.KullaniciID == TDOBasvuruDanisman.TezDanismanID).First();
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Varolan Danışman",
                            EMails = new List<MailSendList> { new MailSendList { EMail = VarolanDanisman.EMail, ToOrBcc = true } },
                            MailSablonTipID = MailSablonTipi.TDO_TezDanismanVeBaslikDegisikligiVarolanDanisman
                        });
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Yeni Danışman",
                            EMails = new List<MailSendList> { new MailSendList { EMail = YeniDanisman.EMail, ToOrBcc = true } },
                            MailSablonTipID = MailSablonTipi.TDO_TezDanismanVeBaslikDegisikligiYeniDanisman
                        });
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Öğrenci",
                            EMails = new List<MailSendList> { new MailSendList { EMail = Ogrenci.EMail, ToOrBcc = true } },
                            MailSablonTipID = MailSablonTipi.TDO_TezDanismanVeBaslikDegisikligiOgrenci
                        });
                    }
                    if (TDODanismanTalepTipID == TDODanismanTalepTip.TezBasligiDegisikligi)
                    {
                          var Danisman = db.Kullanicilars.Where(p => p.KullaniciID == TDOBasvuruDanisman.TezDanismanID).First();
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Danışman",
                            EMails = new List<MailSendList> { new MailSendList { EMail = Danisman.EMail, ToOrBcc = true } },
                            MailSablonTipID = MailSablonTipi.TDO_TezBasligiDegisikligiDanisman
                        });
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Öğrenci",
                            EMails = new List<MailSendList> { new MailSendList { EMail = Ogrenci.EMail, ToOrBcc = true } },
                            MailSablonTipID = MailSablonTipi.TDO_TezBasligiDegisikligiOgrenci
                        });
                    }




                    var Enstitu = TDOBasvuruDanisman.TDOBasvuru.Enstituler;
                    var SablonTipIDs = mModel.Select(s => s.MailSablonTipID).ToList();
                    var Sablonlar = db.MailSablonlaris.Where(p => SablonTipIDs.Contains(p.MailSablonTipID) && p.EnstituKod == Enstitu.EnstituKod).ToList();


                    var PrgL = TDOBasvuru.Programlar;
                    var OgrS = db.OgrenimTipleris.Where(p => p.OgrenimTipKod == TDOBasvuru.OgrenimTipKod && p.EnstituKod == Enstitu.EnstituKod).First();

                    bool IsSended = false;
                    foreach (var item in mModel)
                    {

                        item.Sablon = Sablonlar.Where(p => p.MailSablonTipID == item.MailSablonTipID).First();
                        item.SablonParametreleri = item.Sablon.MailSablonTipleri.Parametreler.Split(',').ToList().Select(s => s.Trim()).ToList();



                        if (item.Sablon.GonderilecekEkEpostalar != null) item.EMails.AddRange(item.Sablon.GonderilecekEkEpostalar.Split(',').Select(s => new MailSendList { EMail = s.Trim(), ToOrBcc = false }));
                        var ParamereDegerleri = new List<MailReplaceParameterModel>();


                        if (item.SablonParametreleri.Any(a => a == "@EnstituAdi"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "EnstituAdi", Value = Enstitu.EnstituAd });
                        if (item.SablonParametreleri.Any(a => a == "@WebAdresi"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "WebAdresi", Value = Enstitu.WebAdresi, IsLink = true });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenimSeviyesiAdi"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "OgrenimSeviyesiAdi", Value = OgrS.OgrenimTipAdi });
                        if (item.SablonParametreleri.Any(a => a == "@ProgramAdi"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "ProgramAdi", Value = PrgL.ProgramAdi });
                        if (item.SablonParametreleri.Any(a => a == "@AdSoyad"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "AdSoyad", Value = TDOBasvuru.Ad + " " + TDOBasvuru.Soyad });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciNo"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "OgrenciNo", Value = TDOBasvuru.OgrenciNo });
                        if (item.SablonParametreleri.Any(a => a == "@DanismanUnvanAdi"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "DanismanUnvanAdi", Value = TDOBasvuruDanisman.TDUnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@DanismanAdSoyad"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "DanismanAdSoyad", Value = TDOBasvuruDanisman.TDAdSoyad });
                        if (item.SablonParametreleri.Any(a => a == "@VarolanDanismanUnvanAdi"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "VarolanDanismanUnvanAdi", Value = TDOBasvuruDanisman.VarolanTDUnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@VarolanDanismanAdSoyad"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "VarolanDanismanUnvanAdi", Value = TDOBasvuruDanisman.VarolanTDAdSoyad });
                        if (item.SablonParametreleri.Any(a => a == "@YeniDanismanUnvanAdi"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "YeniDanismanUnvanAdi", Value = TDOBasvuruDanisman.TDUnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@YeniDanismanAdSoyad"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "YeniDanismanAdSoyad", Value = TDOBasvuruDanisman.TDAdSoyad });

                        if (item.SablonParametreleri.Any(a => a == "@TezDili"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "TezDili", Value = TDOBasvuruDanisman.IsTezDiliTr ? "Türkçe" : "İngilizce" });

                        if (item.SablonParametreleri.Any(a => a == "@TezBasligiTr"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "TezBasligiTr", Value = TDOBasvuruDanisman.TezBaslikTr });

                        if (item.SablonParametreleri.Any(a => a == "@TezBasligiEn"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "TezBasligiEn", Value = TDOBasvuruDanisman.TezBaslikEn });

                        if (item.SablonParametreleri.Any(a => a == "@YeniTezBasligiTr"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "YeniTezBasligiTr", Value = TDOBasvuruDanisman.YeniTezBaslikTr });

                        if (item.SablonParametreleri.Any(a => a == "@YeniTezBasligiEn"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "YeniTezBasligiEn", Value = TDOBasvuruDanisman.YeniTezBaslikEn });

                        if (TDOBasvuruDanisman.EYKDaOnaylandi == true && item.SablonParametreleri.Any(a => a == "@EYKTarihi"))
                        {
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "EYKTarihi", Value = TDOBasvuruDanisman.EYKDaOnaylandiOnayTarihi.ToString("dd-MM-yyyy") });
                        }
                        if (item.SablonParametreleri.Any(a => a == "@Link"))
                        {
                            if (item.JuriTipAdi == "Öğrenci")
                                ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "Link", Value = Enstitu.SistemErisimAdresi + "/TDOBasvurular/Index?TDOBasvuruID=" + TDOBasvuruDanisman.TDOBasvuruID, IsLink = true });
                            else ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "Link", Value = Enstitu.SistemErisimAdresi + "/TDOGelenBasvurular/Index?TDOBasvuruID=" + TDOBasvuruDanisman.TDOBasvuruID, IsLink = true });
                        }
                        var mCOntent = SystemMails.GetSystemMailContent(Enstitu.EnstituAd, item.Sablon.SablonHtml, item.Sablon.SablonAdi, ParamereDegerleri);
                        var snded = MailManager.sendMail(Enstitu.EnstituKod, mCOntent.Title, mCOntent.HtmlContent, item.EMails, item.Attachments);
                        if (snded)
                        {
                            IsSended = true;
                            var kModel = new GonderilenMailler();
                            kModel.Tarih = DateTime.Now;
                            kModel.EnstituKod = Enstitu.EnstituKod;
                            kModel.MesajID = null;
                            kModel.IslemTarihi = DateTime.Now;
                            kModel.Konu = mCOntent.Title;
                            if (!item.JuriTipAdi.IsNullOrWhiteSpace()) kModel.Konu += " (" + item.JuriTipAdi + ")";
                            kModel.IslemYapanID = UserIdentity.Current == null || !UserIdentity.Current.IsAuthenticated ? 1 : UserIdentity.Current.Id;
                            kModel.IslemYapanIP = UserIdentity.Ip;
                            kModel.Aciklama = item.Sablon.Sablon ?? "";
                            kModel.AciklamaHtml = mCOntent.HtmlContent ?? "";
                            kModel.Gonderildi = true;
                            foreach (var itemGK in item.EMails)
                            {
                                kModel.GonderilenMailKullanicilars.Add(new GonderilenMailKullanicilar { Email = itemGK.EMail });
                            }

                            db.GonderilenMaillers.Add(kModel);
                        }
                    }
                    if (IsSended) db.SaveChanges();
                    mmMessage.IsSuccess = true;
                    mmMessage.MessageType = Msgtype.Success;

                }
            }
            catch (Exception ex)
            {
                var message = "Tez danışmanı öneri başvurusu için danışman ve öğrenciye mail gönderilirken bir hata oluştu! \r\nTDOBasvuruDanismanID:" + TDOBasvuruDanismanID;
                Management.SistemBilgisiKaydet(message + "\r\n Hata:" + ex.ToExceptionMessage(), "Management/sendMailTDOBilgisi \r\n" + ex.ToExceptionStackTrace(), BilgiTipi.Hata);
                mmMessage.Messages.Add(message + "</br> Hata:" + ex.ToExceptionMessage());
                mmMessage.MessageType = Msgtype.Error;
                mmMessage.IsSuccess = false;
            }
            return mmMessage;
        }
        public static MmMessage sendMailTDODanismanOnay(int TDOBasvuruDanismanID, bool IsOnayOrRed)
        {
            var mmMessage = new MmMessage();
            try
            {
                using (var db = new LisansustuBasvuruSistemiEntities())
                {
                    var TDOBasvuruDanisman = db.TDOBasvuruDanismen.Where(p => p.TDOBasvuruDanismanID == TDOBasvuruDanismanID).First();
                    var TDODanismanTalepTipID = TDOBasvuruDanisman.TDODanismanTalepTipID;
                    var TDOBasvuru = TDOBasvuruDanisman.TDOBasvuru;
                    var Ogrenci = TDOBasvuruDanisman.TDOBasvuru.Kullanicilar;
                    var mModel = new List<SablonMailModel>();

                    if (TDODanismanTalepTipID == TDODanismanTalepTip.TezDanismaniOnerisi)
                    {
                        if (IsOnayOrRed)
                        {
                            var Danisman = db.Kullanicilars.Where(p => p.KullaniciID == TDOBasvuruDanisman.TezDanismanID).First();
                            mModel.Add(new SablonMailModel
                            {
                                JuriTipAdi = "Danışman",
                                EMails = new List<MailSendList> { new MailSendList { EMail = Danisman.EMail, ToOrBcc = true } },
                                MailSablonTipID = MailSablonTipi.TDO_DanismanOnerisiOnaylandiDanisman
                            });
                        }
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Öğrenci",
                            EMails = new List<MailSendList> { new MailSendList { EMail = Ogrenci.EMail, ToOrBcc = true } },
                            MailSablonTipID = IsOnayOrRed ? MailSablonTipi.TDO_DanismanOnerisiOnaylandiOgrenci : MailSablonTipi.TDO_DanismanOnerisiReddedildiOgrenci
                        });
                    }
                    else if (TDODanismanTalepTipID == TDODanismanTalepTip.TezDanismaniDegisikligi)
                    {
                        var YeniDanisman = db.Kullanicilars.Where(p => p.KullaniciID == TDOBasvuruDanisman.TezDanismanID).First();

                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Yeni Danışman",
                            EMails = new List<MailSendList> { new MailSendList { EMail = YeniDanisman.EMail, ToOrBcc = true } },
                            MailSablonTipID = IsOnayOrRed ? MailSablonTipi.TDO_TezDanismanDegisikligiOnaylandiYeniDanisman : MailSablonTipi.TDO_TezDanismanDegisikligiRetEdildiYeniDanisman
                        });
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Öğrenci",
                            EMails = new List<MailSendList> { new MailSendList { EMail = Ogrenci.EMail, ToOrBcc = true } },
                            MailSablonTipID = IsOnayOrRed ? MailSablonTipi.TDO_TezDanismanDegisikligiOnaylandiOgrenci : MailSablonTipi.TDO_TezDanismanDegisikligiRetEdildiOgrenci
                        });
                    }
                    if (TDODanismanTalepTipID == TDODanismanTalepTip.TezDanismaniVeBaslikDegisikligi)
                    {
                        var YeniDanisman = db.Kullanicilars.Where(p => p.KullaniciID == TDOBasvuruDanisman.TezDanismanID).First();

                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Yeni Danışman",
                            EMails = new List<MailSendList> { new MailSendList { EMail = YeniDanisman.EMail, ToOrBcc = true } },
                            MailSablonTipID = IsOnayOrRed ? MailSablonTipi.TDO_TezDanismanVeBaslikDegisikligiOnaylandiYeniDanisman : MailSablonTipi.TDO_TezDanismanVeBaslikDegisikligiRetEdildiYeniDanisman
                        });
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Öğrenci",
                            EMails = new List<MailSendList> { new MailSendList { EMail = Ogrenci.EMail, ToOrBcc = true } },
                            MailSablonTipID = IsOnayOrRed ? MailSablonTipi.TDO_TezDanismanVeBaslikDegisikligiOnaylandiOgrenci : MailSablonTipi.TDO_TezDanismanVeBaslikDegisikligiRetEdildiOgrenci
                        });
                    }
                    if (TDODanismanTalepTipID == TDODanismanTalepTip.TezBasligiDegisikligi)
                    {
                        var Danisman = db.Kullanicilars.Where(p => p.KullaniciID == TDOBasvuruDanisman.TezDanismanID).First();
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Danışman",
                            EMails = new List<MailSendList> { new MailSendList { EMail = Danisman.EMail, ToOrBcc = true } },
                            MailSablonTipID = IsOnayOrRed ? MailSablonTipi.TDO_TezBasligiDegisikligiOnaylandiDanisman : MailSablonTipi.TDO_TezBasligiDegisikligiRetEdildiDanisman
                        });
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Öğrenci",
                            EMails = new List<MailSendList> { new MailSendList { EMail = Ogrenci.EMail, ToOrBcc = true } },
                            MailSablonTipID = IsOnayOrRed ? MailSablonTipi.TDO_TezBasligiDegisikligiOnaylandiOgrenci : MailSablonTipi.TDO_TezBasligiDegisikligiRetEdildiOgrenci
                        });
                    }



                    var Enstitu = TDOBasvuruDanisman.TDOBasvuru.Enstituler;
                    var SablonTipIDs = mModel.Select(s => s.MailSablonTipID).ToList();
                    var Sablonlar = db.MailSablonlaris.Where(p => SablonTipIDs.Contains(p.MailSablonTipID) && p.EnstituKod == Enstitu.EnstituKod).ToList();


                    var PrgL = TDOBasvuru.Programlar;
                    var OgrS = db.OgrenimTipleris.Where(p => p.OgrenimTipKod == TDOBasvuru.OgrenimTipKod && p.EnstituKod == Enstitu.EnstituKod).First();

                    bool IsSended = false;

                    foreach (var item in mModel)
                    {

                        item.Sablon = Sablonlar.Where(p => p.MailSablonTipID == item.MailSablonTipID).First();
                        item.SablonParametreleri = item.Sablon.MailSablonTipleri.Parametreler.Split(',').ToList().Select(s => s.Trim()).ToList();



                        if (item.Sablon.GonderilecekEkEpostalar != null) item.EMails.AddRange(item.Sablon.GonderilecekEkEpostalar.Split(',').Select(s => new MailSendList { EMail = s.Trim(), ToOrBcc = false }));
                        var ParamereDegerleri = new List<MailReplaceParameterModel>();
                        var GonderilenMailEkleri = new List<GonderilenMailEkleri>();
                        if (IsOnayOrRed)
                        {
                            var IDs = new List<int?>() { TDOBasvuruDanismanID };
                            var Ekler = Management.exportRaporPdf(RaporTipleri.TezDanismanOneriFormu, IDs);
                            item.Attachments.AddRange(Ekler);
                            GonderilenMailEkleri.AddRange(Ekler.Select(s => new GonderilenMailEkleri { EkAdi = s.Name, EkDosyaYolu = "" }));

                        }
                        if (item.SablonParametreleri.Any(a => a == "@EnstituAdi"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "EnstituAdi", Value = Enstitu.EnstituAd });
                        if (item.SablonParametreleri.Any(a => a == "@WebAdresi"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "WebAdresi", Value = Enstitu.WebAdresi, IsLink = true });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenimSeviyesiAdi"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "OgrenimSeviyesiAdi", Value = OgrS.OgrenimTipAdi });
                        if (item.SablonParametreleri.Any(a => a == "@ProgramAdi"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "ProgramAdi", Value = PrgL.ProgramAdi });
                        if (item.SablonParametreleri.Any(a => a == "@AdSoyad"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "AdSoyad", Value = TDOBasvuru.Ad + " " + TDOBasvuru.Soyad });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciNo"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "OgrenciNo", Value = TDOBasvuru.OgrenciNo });
                        if (item.SablonParametreleri.Any(a => a == "@DanismanUnvanAdi"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "DanismanUnvanAdi", Value = TDOBasvuruDanisman.TDUnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@DanismanAdSoyad"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "DanismanAdSoyad", Value = TDOBasvuruDanisman.TDAdSoyad });
                        if (item.SablonParametreleri.Any(a => a == "@VarolanDanismanUnvanAdi"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "VarolanDanismanUnvanAdi", Value = TDOBasvuruDanisman.VarolanTDUnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@VarolanDanismanAdSoyad"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "VarolanDanismanUnvanAdi", Value = TDOBasvuruDanisman.VarolanTDAdSoyad });
                        if (item.SablonParametreleri.Any(a => a == "@YeniDanismanUnvanAdi"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "YeniDanismanUnvanAdi", Value = TDOBasvuruDanisman.TDUnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@YeniDanismanAdSoyad"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "YeniDanismanAdSoyad", Value = TDOBasvuruDanisman.TDAdSoyad });

                        if (item.SablonParametreleri.Any(a => a == "@TezBaslikTr"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "TezBaslikTr", Value = TDOBasvuruDanisman.TezBaslikTr });
                        if (item.SablonParametreleri.Any(a => a == "@TezBaslikEn"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "TezBaslikEn", Value = TDOBasvuruDanisman.TezBaslikEn });
                        if (item.SablonParametreleri.Any(a => a == "@YeniTezBaslikTr"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "YeniTezBaslikTr", Value = TDOBasvuruDanisman.YeniTezBaslikTr });
                        if (item.SablonParametreleri.Any(a => a == "@YeniTezBaslikEn"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "YeniTezBaslikEn", Value = TDOBasvuruDanisman.YeniTezBaslikEn });
                        if (item.SablonParametreleri.Any(a => a == "@TezDili"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "TezDili", Value = (TDOBasvuruDanisman.IsTezDiliTr ? "Türkçe" : "İngilizce") });

                        if (TDOBasvuruDanisman.EYKDaOnaylandi == true && item.SablonParametreleri.Any(a => a == "@EYKTarihi"))
                        {
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "EYKTarihi", Value = TDOBasvuruDanisman.EYKDaOnaylandiOnayTarihi.ToString("dd-MM-yyyy") });
                        }
                        if (!IsOnayOrRed)
                        {
                            var RetAciklama = "";

                            if (TDODanismanTalepTipID == TDODanismanTalepTip.TezBasligiDegisikligi || TDODanismanTalepTipID == TDODanismanTalepTip.TezDanismaniOnerisi) RetAciklama = TDOBasvuruDanisman.DanismanOnaylanmadiAciklama;
                            else RetAciklama = TDOBasvuruDanisman.VarolanDanismanOnaylanmadiAciklama;


                            if (item.SablonParametreleri.Any(a => a == "@RedAciklama"))
                            {
                                ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "RedAciklama", Value = RetAciklama });
                            }
                            if (item.SablonParametreleri.Any(a => a == "@RetAciklama"))
                            {
                                ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "RetAciklama", Value = RetAciklama });
                            }
                        }
                        if (TDOBasvuruDanisman.EYKDaOnaylandi == true && item.SablonParametreleri.Any(a => a == "@EYKTarihi"))
                        {
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "EYKTarihi", Value = TDOBasvuruDanisman.EYKDaOnaylandiOnayTarihi.ToString("dd-MM-yyyy") });
                        }
                        if (item.SablonParametreleri.Any(a => a == "@Link"))
                        {
                            if (item.JuriTipAdi == "Öğrenci")
                                ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "Link", Value = Enstitu.SistemErisimAdresi + "/TDOBasvurular/Index?TDOBasvuruID=" + TDOBasvuruDanisman.TDOBasvuruID, IsLink = true });
                            else ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "Link", Value = Enstitu.SistemErisimAdresi + "/TDOGelenBasvurular/Index?TDOBasvuruID=" + TDOBasvuruDanisman.TDOBasvuruID, IsLink = true });
                        }

                        var mCOntent = SystemMails.GetSystemMailContent(Enstitu.EnstituAd, item.Sablon.SablonHtml, item.Sablon.SablonAdi, ParamereDegerleri);
                        var snded = MailManager.sendMail(Enstitu.EnstituKod, mCOntent.Title, mCOntent.HtmlContent, item.EMails, item.Attachments);
                        if (snded)
                        {
                            IsSended = true;
                            var kModel = new GonderilenMailler();
                            kModel.Tarih = DateTime.Now;
                            kModel.EnstituKod = Enstitu.EnstituKod;
                            kModel.MesajID = null;
                            kModel.IslemTarihi = DateTime.Now;
                            kModel.Konu = mCOntent.Title;
                            if (!item.JuriTipAdi.IsNullOrWhiteSpace()) kModel.Konu += " (" + item.JuriTipAdi + ")";
                            kModel.IslemYapanID = UserIdentity.Current == null || !UserIdentity.Current.IsAuthenticated ? 1 : UserIdentity.Current.Id;
                            kModel.IslemYapanIP = UserIdentity.Ip;
                            kModel.Aciklama = item.Sablon.Sablon ?? "";
                            kModel.AciklamaHtml = mCOntent.HtmlContent ?? "";
                            kModel.Gonderildi = true;
                            foreach (var itemGK in item.EMails)
                            {
                                kModel.GonderilenMailKullanicilars.Add(new GonderilenMailKullanicilar { Email = itemGK.EMail });
                            }
                            foreach (var itemME in GonderilenMailEkleri)
                            {
                                kModel.GonderilenMailEkleris.Add(itemME);
                            }
                            db.GonderilenMaillers.Add(kModel);
                        }
                    }

                    if (IsSended) db.SaveChanges();
                    mmMessage.IsSuccess = true;
                    mmMessage.MessageType = Msgtype.Success;

                }
            }
            catch (Exception ex)
            {
                var message = "Tez danışmanı öneri başvurusu için danışman ve öğrenciye mail gönderilirken bir hata oluştu! \r\nTDOBasvuruDanismanID:" + TDOBasvuruDanismanID;
                Management.SistemBilgisiKaydet(message + "\r\n Hata:" + ex.ToExceptionMessage(), "Management/sendMailTDOBilgisi \r\n" + ex.ToExceptionStackTrace(), BilgiTipi.Hata);
                mmMessage.Messages.Add(message + "</br> Hata:" + ex.ToExceptionMessage());
                mmMessage.MessageType = Msgtype.Error;
                mmMessage.IsSuccess = false;
            }
            return mmMessage;
        }
        public static MmMessage sendMailTDOEYKOnay(int TDOBasvuruDanismanID, bool IsOnayOrRed)
        {
            var mmMessage = new MmMessage();
            try
            {
                using (var db = new LisansustuBasvuruSistemiEntities())
                {
                    var TDOBasvuruDanisman = db.TDOBasvuruDanismen.Where(p => p.TDOBasvuruDanismanID == TDOBasvuruDanismanID).First();
                    var TDODanismanTalepTipID = TDOBasvuruDanisman.TDODanismanTalepTipID;
                    var TDOBasvuru = TDOBasvuruDanisman.TDOBasvuru;
                    var Ogrenci = TDOBasvuruDanisman.TDOBasvuru.Kullanicilar;
                    var mModel = new List<SablonMailModel>();
                    if (TDODanismanTalepTipID == TDODanismanTalepTip.TezDanismaniOnerisi)
                    {

                        var Danisman = db.Kullanicilars.Where(p => p.KullaniciID == TDOBasvuruDanisman.TezDanismanID).First();
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Danışman",
                            EMails = new List<MailSendList> { new MailSendList { EMail = Danisman.EMail, ToOrBcc = true } },
                            MailSablonTipID = IsOnayOrRed ? MailSablonTipi.TDO_DanismanOnerisiEYKDaOnaylandiDanisman : MailSablonTipi.TDO_DanismanOnerisiEYKDaReddedildiOgrenciDanisman
                        });

                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Öğrenci",
                            EMails = new List<MailSendList> { new MailSendList { EMail = Ogrenci.EMail, ToOrBcc = true } },
                            MailSablonTipID = IsOnayOrRed ? MailSablonTipi.TDO_DanismanOnerisiEYKDaOnaylandiOgrenci : MailSablonTipi.TDO_DanismanOnerisiEYKDaReddedildiOgrenciDanisman
                        });
                    }
                    else if (TDODanismanTalepTipID == TDODanismanTalepTip.TezDanismaniDegisikligi)
                    {
                        var YeniDanisman = db.Kullanicilars.Where(p => p.KullaniciID == TDOBasvuruDanisman.TezDanismanID).First();

                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Yeni Danışman",
                            EMails = new List<MailSendList> { new MailSendList { EMail = YeniDanisman.EMail, ToOrBcc = true } },
                            MailSablonTipID = IsOnayOrRed ? MailSablonTipi.TDO_TezDanismanDegisikligiEYKDaOnaylandiYeniDanisman : MailSablonTipi.TDO_TezDanismanDegisikligiEYKDaRetEdildiYeniDanisman
                        });
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Öğrenci",
                            EMails = new List<MailSendList> { new MailSendList { EMail = Ogrenci.EMail, ToOrBcc = true } },
                            MailSablonTipID = IsOnayOrRed ? MailSablonTipi.TDO_TezDanismanDegisikligiEYKDaOnaylandiOgrenci : MailSablonTipi.TDO_TezDanismanDegisikligiEYKDaRetEdildiOgrenci
                        });
                    }
                    if (TDODanismanTalepTipID == TDODanismanTalepTip.TezDanismaniVeBaslikDegisikligi)
                    {
                        var YeniDanisman = db.Kullanicilars.Where(p => p.KullaniciID == TDOBasvuruDanisman.TezDanismanID).First();

                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Yeni Danışman",
                            EMails = new List<MailSendList> { new MailSendList { EMail = YeniDanisman.EMail, ToOrBcc = true } },
                            MailSablonTipID = IsOnayOrRed ? MailSablonTipi.TDO_TezDanismanVeBaslikDegisikligiEYKDaOnaylandiYeniDanisman : MailSablonTipi.TDO_TezDanismanVeBaslikDegisikligiEYKDaRetEdildiYeniDanisman
                        });
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Öğrenci",
                            EMails = new List<MailSendList> { new MailSendList { EMail = Ogrenci.EMail, ToOrBcc = true } },
                            MailSablonTipID = IsOnayOrRed ? MailSablonTipi.TDO_TezDanismanVeBaslikDegisikligiEYKDaOnaylandiOgrenci : MailSablonTipi.TDO_TezDanismanVeBaslikDegisikligiEYKDaRetEdildiOgrenci
                        });
                    }
                    if (TDODanismanTalepTipID == TDODanismanTalepTip.TezBasligiDegisikligi)
                    {
                        var Danisman = db.Kullanicilars.Where(p => p.KullaniciID == TDOBasvuruDanisman.TezDanismanID).First();
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Danışman",
                            EMails = new List<MailSendList> { new MailSendList { EMail = Danisman.EMail, ToOrBcc = true } },
                            MailSablonTipID = IsOnayOrRed ? MailSablonTipi.TDO_TezBasligiDegisikligiEYKDaOnaylandiDanisman : MailSablonTipi.TDO_TezBasligiDegisikligiEYKDaRetEdildiDanisman
                        });
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Öğrenci",
                            EMails = new List<MailSendList> { new MailSendList { EMail = Ogrenci.EMail, ToOrBcc = true } },
                            MailSablonTipID = IsOnayOrRed ? MailSablonTipi.TDO_TezBasligiDegisikligiEYKDaOnaylandiOgrenci : MailSablonTipi.TDO_TezBasligiDegisikligiEYKDaRetEdildiOgrenci
                        });
                    }
                    var SablonTipIDs = mModel.Select(s => s.MailSablonTipID).ToList();

                    var Enstitu = TDOBasvuruDanisman.TDOBasvuru.Enstituler;
                    var Sablonlar = db.MailSablonlaris.Where(p => SablonTipIDs.Contains(p.MailSablonTipID) && p.EnstituKod == Enstitu.EnstituKod).ToList();
                    var PrgL = TDOBasvuru.Programlar;
                    var OgrS = db.OgrenimTipleris.Where(p => p.OgrenimTipKod == TDOBasvuru.OgrenimTipKod && p.EnstituKod == Enstitu.EnstituKod).First();

                    bool IsSended = false;

                    foreach (var item in mModel)
                    {

                        item.Sablon = Sablonlar.Where(p => p.MailSablonTipID == item.MailSablonTipID).First();
                        item.SablonParametreleri = item.Sablon.MailSablonTipleri.Parametreler.Split(',').ToList().Select(s => s.Trim()).ToList();



                        if (item.Sablon.GonderilecekEkEpostalar != null) item.EMails.AddRange(item.Sablon.GonderilecekEkEpostalar.Split(',').Select(s => new MailSendList { EMail = s.Trim(), ToOrBcc = false }));
                        var ParamereDegerleri = new List<MailReplaceParameterModel>();
                        var GonderilenMailEkleri = new List<GonderilenMailEkleri>();



                        if (item.SablonParametreleri.Any(a => a == "@EnstituAdi"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "EnstituAdi", Value = Enstitu.EnstituAd });
                        if (item.SablonParametreleri.Any(a => a == "@WebAdresi"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "WebAdresi", Value = Enstitu.WebAdresi, IsLink = true });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenimSeviyesiAdi"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "OgrenimSeviyesiAdi", Value = OgrS.OgrenimTipAdi });
                        if (item.SablonParametreleri.Any(a => a == "@ProgramAdi"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "ProgramAdi", Value = PrgL.ProgramAdi });
                        if (item.SablonParametreleri.Any(a => a == "@AdSoyad"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "AdSoyad", Value = TDOBasvuru.Ad + " " + TDOBasvuru.Soyad });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciNo"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "OgrenciNo", Value = TDOBasvuru.OgrenciNo });
                        if (item.SablonParametreleri.Any(a => a == "@DanismanUnvanAdi"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "DanismanUnvanAdi", Value = TDOBasvuruDanisman.TDUnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@DanismanAdSoyad"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "DanismanAdSoyad", Value = TDOBasvuruDanisman.TDAdSoyad });
                        if (item.SablonParametreleri.Any(a => a == "@VarolanDanismanUnvanAdi"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "VarolanDanismanUnvanAdi", Value = TDOBasvuruDanisman.VarolanTDUnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@VarolanDanismanAdSoyad"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "VarolanDanismanUnvanAdi", Value = TDOBasvuruDanisman.VarolanTDAdSoyad });
                        if (item.SablonParametreleri.Any(a => a == "@YeniDanismanUnvanAdi"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "YeniDanismanUnvanAdi", Value = TDOBasvuruDanisman.TDUnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@YeniDanismanAdSoyad"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "YeniDanismanAdSoyad", Value = TDOBasvuruDanisman.TDAdSoyad });

                        if (item.SablonParametreleri.Any(a => a == "@TezBaslikTr"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "TezBaslikTr", Value = TDOBasvuruDanisman.TezBaslikTr });
                        if (item.SablonParametreleri.Any(a => a == "@TezBaslikEn"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "TezBaslikEn", Value = TDOBasvuruDanisman.TezBaslikEn });
                        if (item.SablonParametreleri.Any(a => a == "@YeniTezBaslikTr"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "YeniTezBaslikTr", Value = TDOBasvuruDanisman.YeniTezBaslikTr });
                        if (item.SablonParametreleri.Any(a => a == "@YeniTezBaslikEn"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "YeniTezBaslikEn", Value = TDOBasvuruDanisman.YeniTezBaslikEn });
                        if (item.SablonParametreleri.Any(a => a == "@TezDili"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "TezDili", Value = (TDOBasvuruDanisman.IsTezDiliTr ? "Türkçe" : "İngilizce") });

                        if (TDOBasvuruDanisman.EYKDaOnaylandi == true && item.SablonParametreleri.Any(a => a == "@EYKTarihi"))
                        {
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "EYKTarihi", Value = TDOBasvuruDanisman.EYKDaOnaylandiOnayTarihi.ToString("dd-MM-yyyy") });
                        }
                        if (!IsOnayOrRed)
                        {
                            var RetAciklama = TDOBasvuruDanisman.EYKDaOnaylanmadiDurumAciklamasi;



                            if (item.SablonParametreleri.Any(a => a == "@RedAciklama"))
                            {
                                ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "RedAciklama", Value = RetAciklama });
                            }
                            if (item.SablonParametreleri.Any(a => a == "@RetAciklama"))
                            {
                                ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "RetAciklama", Value = RetAciklama });
                            }
                        }
                        if (TDOBasvuruDanisman.EYKDaOnaylandi == true && item.SablonParametreleri.Any(a => a == "@EYKTarihi"))
                        {
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "EYKTarihi", Value = TDOBasvuruDanisman.EYKDaOnaylandiOnayTarihi.ToString("dd-MM-yyyy") });
                        }
                        if (item.SablonParametreleri.Any(a => a == "@Link"))
                        {
                            if (item.JuriTipAdi == "Öğrenci")
                                ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "Link", Value = Enstitu.SistemErisimAdresi + "/TDOBasvurular/Index?TDOBasvuruID=" + TDOBasvuruDanisman.TDOBasvuruID, IsLink = true });
                            else ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "Link", Value = Enstitu.SistemErisimAdresi + "/TDOGelenBasvurular/Index?TDOBasvuruID=" + TDOBasvuruDanisman.TDOBasvuruID, IsLink = true });
                        }
                        var mCOntent = SystemMails.GetSystemMailContent(Enstitu.EnstituAd, item.Sablon.SablonHtml, item.Sablon.SablonAdi, ParamereDegerleri);
                        var snded = MailManager.sendMail(Enstitu.EnstituKod, mCOntent.Title, mCOntent.HtmlContent, item.EMails, item.Attachments);
                        if (snded)
                        {
                            IsSended = true;
                            var kModel = new GonderilenMailler();
                            kModel.Tarih = DateTime.Now;
                            kModel.EnstituKod = Enstitu.EnstituKod;
                            kModel.MesajID = null;
                            kModel.IslemTarihi = DateTime.Now;
                            kModel.Konu = mCOntent.Title;
                            if (!item.JuriTipAdi.IsNullOrWhiteSpace()) kModel.Konu += " (" + item.JuriTipAdi + ")";
                            kModel.IslemYapanID = UserIdentity.Current == null || !UserIdentity.Current.IsAuthenticated ? 1 : UserIdentity.Current.Id;
                            kModel.IslemYapanIP = UserIdentity.Ip;
                            kModel.Aciklama = item.Sablon.Sablon ?? "";
                            kModel.AciklamaHtml = mCOntent.HtmlContent ?? "";
                            kModel.Gonderildi = true;
                            foreach (var itemGK in item.EMails)
                            {
                                kModel.GonderilenMailKullanicilars.Add(new GonderilenMailKullanicilar { Email = itemGK.EMail });
                            }
                            foreach (var itemME in GonderilenMailEkleri)
                            {
                                kModel.GonderilenMailEkleris.Add(itemME);
                            }
                            db.GonderilenMaillers.Add(kModel);
                        }
                    }
                    if (IsSended) db.SaveChanges();
                    mmMessage.IsSuccess = true;
                    mmMessage.MessageType = Msgtype.Success;

                }
            }
            catch (Exception ex)
            {
                var message = "Tez danışmanı öneri başvurusu için danışman ve öğrenciye mail gönderilirken bir hata oluştu! \r\nTDOBasvuruDanismanID:" + TDOBasvuruDanismanID;
                Management.SistemBilgisiKaydet(message + "\r\n Hata:" + ex.ToExceptionMessage(), "Management/sendMailTDOBilgisi \r\n" + ex.ToExceptionStackTrace(), BilgiTipi.Hata);
                mmMessage.Messages.Add(message + "</br> Hata:" + ex.ToExceptionMessage());
                mmMessage.MessageType = Msgtype.Error;
                mmMessage.IsSuccess = false;
            }
            return mmMessage;
        }

        public static MmMessage sendMailTDOESBilgisi(int TDOBasvuruEsDanismanID)
        {
            var mmMessage = new MmMessage();
            try
            {
                using (var db = new LisansustuBasvuruSistemiEntities())
                {
                    var EsDanisman = db.TDOBasvuruEsDanismen.Where(p => p.TDOBasvuruEsDanismanID == TDOBasvuruEsDanismanID).First();
                    var TDOBasvuruDanisman = EsDanisman.TDOBasvuruDanisman;
                    var TDOBasvuru = TDOBasvuruDanisman.TDOBasvuru;
                    var Danisman = db.Kullanicilars.Where(p => p.KullaniciID == TDOBasvuruDanisman.TezDanismanID).First();
                    var Ogrenci = TDOBasvuruDanisman.TDOBasvuru.Kullanicilar;
                    var mModel = new List<SablonMailModel>();


                    if (EsDanisman.IsDegisiklikTalebi)
                    {
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Danışman",
                            EMails = new List<MailSendList> { new MailSendList { EMail = Danisman.EMail, ToOrBcc = true } },
                            MailSablonTipID = MailSablonTipi.TDO_EsDanismanDegisikligiYapildiDanisman
                        });
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Öğrenci",
                            EMails = new List<MailSendList> { new MailSendList { EMail = Ogrenci.EMail, ToOrBcc = true } },
                            MailSablonTipID = MailSablonTipi.TDO_EsDanismanDegisikligiYapildiOgrenci
                        });
                    }
                    else
                    {
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Danışman",
                            EMails = new List<MailSendList> { new MailSendList { EMail = Danisman.EMail, ToOrBcc = true } },
                            MailSablonTipID = MailSablonTipi.TDO_EsDanismanOnerisiYapildiDanisman
                        });
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Öğrenci",
                            EMails = new List<MailSendList> { new MailSendList { EMail = Ogrenci.EMail, ToOrBcc = true } },
                            MailSablonTipID = MailSablonTipi.TDO_EsDanismanOnerisiYapildiOgrenci
                        });
                    }



                    var Enstitu = TDOBasvuruDanisman.TDOBasvuru.Enstituler;
                    var SablonTipIDs = mModel.Select(s => s.MailSablonTipID).ToList();
                    var Sablonlar = db.MailSablonlaris.Where(p => SablonTipIDs.Contains(p.MailSablonTipID) && p.EnstituKod == Enstitu.EnstituKod).ToList();


                    var PrgL = TDOBasvuru.Programlar;
                    var OgrS = db.OgrenimTipleris.Where(p => p.OgrenimTipKod == TDOBasvuru.OgrenimTipKod && p.EnstituKod == Enstitu.EnstituKod).First();

                    bool IsSended = false;
                    foreach (var item in mModel)
                    {

                        item.Sablon = Sablonlar.Where(p => p.MailSablonTipID == item.MailSablonTipID).First();
                        item.SablonParametreleri = item.Sablon.MailSablonTipleri.Parametreler.Split(',').ToList().Select(s => s.Trim()).ToList();

                        var GonderilenMailEkleri = new List<GonderilenMailEkleri>();

                        var IDs = new List<int?>() { TDOBasvuruEsDanismanID };
                        var Ekler = Management.exportRaporPdf(RaporTipleri.TezEsDanismanOneriFormu, IDs);
                        item.Attachments.AddRange(Ekler);
                        GonderilenMailEkleri.AddRange(Ekler.Select(s => new GonderilenMailEkleri { EkAdi = s.Name, EkDosyaYolu = "" }));



                        if (item.Sablon.GonderilecekEkEpostalar != null) item.EMails.AddRange(item.Sablon.GonderilecekEkEpostalar.Split(',').Select(s => new MailSendList { EMail = s.Trim(), ToOrBcc = false }));
                        var ParamereDegerleri = new List<MailReplaceParameterModel>();


                        if (item.SablonParametreleri.Any(a => a == "@EnstituAdi"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "EnstituAdi", Value = Enstitu.EnstituAd });
                        if (item.SablonParametreleri.Any(a => a == "@WebAdresi"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "WebAdresi", Value = Enstitu.WebAdresi, IsLink = true });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenimSeviyesiAdi"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "OgrenimSeviyesiAdi", Value = OgrS.OgrenimTipAdi });
                        if (item.SablonParametreleri.Any(a => a == "@ProgramAdi"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "ProgramAdi", Value = PrgL.ProgramAdi });
                        if (item.SablonParametreleri.Any(a => a == "@AdSoyad"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "AdSoyad", Value = TDOBasvuru.Ad + " " + TDOBasvuru.Soyad });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciNo"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "OgrenciNo", Value = TDOBasvuru.OgrenciNo });
                        if (item.SablonParametreleri.Any(a => a == "@DanismanUnvanAdi"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "DanismanUnvanAdi", Value = TDOBasvuruDanisman.TDUnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@DanismanAdSoyad"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "DanismanAdSoyad", Value = TDOBasvuruDanisman.TDAdSoyad });

                        if (item.SablonParametreleri.Any(a => a == "@EsDanismanUnvanAdi"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "EsDanismanUnvanAdi", Value = EsDanisman.UnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@EsDanismanAdSoyad"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "EsDanismanAdSoyad", Value = EsDanisman.AdSoyad });
                        if (item.SablonParametreleri.Any(a => a == "@EsDanismanUniversite"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "EsDanismanUniversite", Value = EsDanisman.UniversiteAdi });
                        if (item.SablonParametreleri.Any(a => a == "@VarolanEsDanismanAdSoyad"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "VarolanEsDanismanAdSoyad", Value = EsDanisman.OncekiEsDanismanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@YeniEsDanismanUnvanAdi"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "YeniEsDanismanUnvanAdi", Value = EsDanisman.UnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@YeniEsDanismanAdSoyad"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "YeniEsDanismanUnvanAdi", Value = EsDanisman.AdSoyad });


                        if (item.SablonParametreleri.Any(a => a == "@TezBaslikTr"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "TezBaslikTr", Value = TDOBasvuruDanisman.TezBaslikTr });
                        if (item.SablonParametreleri.Any(a => a == "@TezBaslikEn"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "TezBaslikEn", Value = TDOBasvuruDanisman.TezBaslikEn });
                        if (item.SablonParametreleri.Any(a => a == "@TezDili"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "TezDili", Value = (TDOBasvuruDanisman.IsTezDiliTr ? "Türkçe" : "İngilizce") });
                        if (item.SablonParametreleri.Any(a => a == "@Link"))
                        {
                            if (item.JuriTipAdi == "Öğrenci")
                                ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "Link", Value = Enstitu.SistemErisimAdresi + "/TDOBasvurular/Index?TDOBasvuruID=" + TDOBasvuruDanisman.TDOBasvuruID, IsLink = true });
                            else ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "Link", Value = Enstitu.SistemErisimAdresi + "/TDOGelenBasvurular/Index?TDOBasvuruID=" + TDOBasvuruDanisman.TDOBasvuruID, IsLink = true });
                        }


                        var mCOntent = SystemMails.GetSystemMailContent(Enstitu.EnstituAd, item.Sablon.SablonHtml, item.Sablon.SablonAdi, ParamereDegerleri);
                        var snded = MailManager.sendMail(Enstitu.EnstituKod, mCOntent.Title, mCOntent.HtmlContent, item.EMails, item.Attachments);
                        if (snded)
                        {
                            IsSended = true;
                            var kModel = new GonderilenMailler();
                            kModel.Tarih = DateTime.Now;
                            kModel.EnstituKod = Enstitu.EnstituKod;
                            kModel.MesajID = null;
                            kModel.IslemTarihi = DateTime.Now;
                            kModel.Konu = mCOntent.Title;
                            if (!item.JuriTipAdi.IsNullOrWhiteSpace()) kModel.Konu += " (" + item.JuriTipAdi + ")";
                            kModel.IslemYapanID = UserIdentity.Current == null || !UserIdentity.Current.IsAuthenticated ? 1 : UserIdentity.Current.Id;
                            kModel.IslemYapanIP = UserIdentity.Ip;
                            kModel.Aciklama = item.Sablon.Sablon ?? "";
                            kModel.AciklamaHtml = mCOntent.HtmlContent ?? "";
                            kModel.Gonderildi = true;
                            foreach (var itemGK in item.EMails)
                            {
                                kModel.GonderilenMailKullanicilars.Add(new GonderilenMailKullanicilar { Email = itemGK.EMail });
                            }
                            foreach (var itemME in GonderilenMailEkleri)
                            {
                                kModel.GonderilenMailEkleris.Add(itemME);
                            }
                            db.GonderilenMaillers.Add(kModel);
                        }
                    }
                    if (IsSended) db.SaveChanges();
                    mmMessage.IsSuccess = true;
                    mmMessage.MessageType = Msgtype.Success;

                }
            }
            catch (Exception ex)
            {
                var message = "Tez Eş danışmanı öneri başvurusu için danışman ve öğrenciye mail gönderilirken bir hata oluştu! \r\nTDOEsBasvuruDanismanID:" + TDOBasvuruEsDanismanID;
                Management.SistemBilgisiKaydet(message + "\r\n Hata:" + ex.ToExceptionMessage(), "Management/sendMailTDOBilgisi \r\n" + ex.ToExceptionStackTrace(), BilgiTipi.Hata);
                mmMessage.Messages.Add(message + "</br> Hata:" + ex.ToExceptionMessage());
                mmMessage.MessageType = Msgtype.Error;
                mmMessage.IsSuccess = false;
            }
            return mmMessage;
        }
        public static MmMessage sendMailTDOEsEYKOnay(int TDOBasvuruDanismanID, bool IsOnayOrRed)
        {
            var mmMessage = new MmMessage();
            try
            {
                using (var db = new LisansustuBasvuruSistemiEntities())
                {
                    var TDOBasvuruDanisman = db.TDOBasvuruDanismen.Where(p => p.TDOBasvuruDanismanID == TDOBasvuruDanismanID).First();
                    var EsDanisman = TDOBasvuruDanisman.TDOBasvuruEsDanismen.First();
                    var TDOBasvuru = TDOBasvuruDanisman.TDOBasvuru;
                    var Danisman = db.Kullanicilars.Where(p => p.KullaniciID == TDOBasvuruDanisman.TezDanismanID).First();
                    var Ogrenci = TDOBasvuruDanisman.TDOBasvuru.Kullanicilar;
                    var mModel = new List<SablonMailModel>();

                    if (EsDanisman.IsDegisiklikTalebi)
                    {
                        if (!IsOnayOrRed)
                        {
                            mModel.Add(new SablonMailModel
                            {
                                JuriTipAdi = "Öğrenci, Danışman",
                                EMails = new List<MailSendList> { new MailSendList { EMail = Ogrenci.EMail, ToOrBcc = true }, new MailSendList { EMail = Danisman.EMail, ToOrBcc = true } },
                                MailSablonTipID = MailSablonTipi.TDO_EsDanismanDegisikligiEYKDaRetEdildiOgrenciDanisman
                            });
                        }
                        else
                        {
                            mModel.Add(new SablonMailModel
                            {
                                JuriTipAdi = "Danışman",
                                EMails = new List<MailSendList> { new MailSendList { EMail = Danisman.EMail, ToOrBcc = true } },
                                MailSablonTipID = MailSablonTipi.TDO_EsDanismanDegisikligiEYKDaOnaylandiDanisman
                            });
                            mModel.Add(new SablonMailModel
                            {
                                JuriTipAdi = "Eş Danışman",
                                EMails = new List<MailSendList> { new MailSendList { EMail = EsDanisman.EMail, ToOrBcc = true } },
                                MailSablonTipID = MailSablonTipi.TDO_EsDanismanDegisikligiEYKDaOnaylandiEsDanisman
                            });
                            mModel.Add(new SablonMailModel
                            {
                                JuriTipAdi = "Öğrenci",
                                EMails = new List<MailSendList> { new MailSendList { EMail = Ogrenci.EMail, ToOrBcc = true } },
                                MailSablonTipID = MailSablonTipi.TDO_EsDanismanDegisikligiEYKDaOnaylandiOgrenci
                            });
                        }
                    }
                    else
                    {
                        if (!IsOnayOrRed)
                        {
                            mModel.Add(new SablonMailModel
                            {
                                JuriTipAdi = "Öğrenci, Danışman",
                                EMails = new List<MailSendList> { new MailSendList { EMail = Ogrenci.EMail, ToOrBcc = true }, new MailSendList { EMail = Danisman.EMail, ToOrBcc = true } },
                                MailSablonTipID = MailSablonTipi.TDO_EsDanismanOnerisiEYKDaReddedildiOgrenciDanisman
                            });
                        }
                        else
                        {
                            mModel.Add(new SablonMailModel
                            {
                                JuriTipAdi = "Danışman",
                                EMails = new List<MailSendList> { new MailSendList { EMail = Danisman.EMail, ToOrBcc = true } },
                                MailSablonTipID = MailSablonTipi.TDO_EsDanismanOnerisiEYKDaOnaylandiDanisman
                            });
                            mModel.Add(new SablonMailModel
                            {
                                JuriTipAdi = "Eş Danışman",
                                EMails = new List<MailSendList> { new MailSendList { EMail = EsDanisman.EMail, ToOrBcc = true } },
                                MailSablonTipID = MailSablonTipi.TDO_EsDanismanOnerisiEYKDaOnaylandiEsDanisman
                            });
                            mModel.Add(new SablonMailModel
                            {
                                JuriTipAdi = "Öğrenci",
                                EMails = new List<MailSendList> { new MailSendList { EMail = Ogrenci.EMail, ToOrBcc = true } },
                                MailSablonTipID = MailSablonTipi.TDO_EsDanismanOnerisiEYKDaOnaylandiOgrenci
                            });
                        }
                    }


                    var Enstitu = TDOBasvuruDanisman.TDOBasvuru.Enstituler;
                    var SablonTipIDs = mModel.Select(s => s.MailSablonTipID).ToList();
                    var Sablonlar = db.MailSablonlaris.Where(p => SablonTipIDs.Contains(p.MailSablonTipID) && p.EnstituKod == Enstitu.EnstituKod).ToList();


                    var PrgL = TDOBasvuru.Programlar;
                    var OgrS = db.OgrenimTipleris.Where(p => p.OgrenimTipKod == TDOBasvuru.OgrenimTipKod && p.EnstituKod == Enstitu.EnstituKod).First();

                    bool IsSended = false;
                    foreach (var item in mModel)
                    {

                        item.Sablon = Sablonlar.Where(p => p.MailSablonTipID == item.MailSablonTipID).First();
                        item.SablonParametreleri = item.Sablon.MailSablonTipleri.Parametreler.Split(',').ToList().Select(s => s.Trim()).ToList();



                        if (item.Sablon.GonderilecekEkEpostalar != null) item.EMails.AddRange(item.Sablon.GonderilecekEkEpostalar.Split(',').Select(s => new MailSendList { EMail = s.Trim(), ToOrBcc = false }));
                        var ParamereDegerleri = new List<MailReplaceParameterModel>();


                        if (item.SablonParametreleri.Any(a => a == "@EnstituAdi"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "EnstituAdi", Value = Enstitu.EnstituAd });
                        if (item.SablonParametreleri.Any(a => a == "@WebAdresi"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "WebAdresi", Value = Enstitu.WebAdresi, IsLink = true });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenimSeviyesiAdi"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "OgrenimSeviyesiAdi", Value = OgrS.OgrenimTipAdi });
                        if (item.SablonParametreleri.Any(a => a == "@ProgramAdi"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "ProgramAdi", Value = PrgL.ProgramAdi });
                        if (item.SablonParametreleri.Any(a => a == "@AdSoyad"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "AdSoyad", Value = TDOBasvuru.Ad + " " + TDOBasvuru.Soyad });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciNo"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "OgrenciNo", Value = TDOBasvuru.OgrenciNo });
                        if (item.SablonParametreleri.Any(a => a == "@DanismanUnvanAdi"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "DanismanUnvanAdi", Value = TDOBasvuruDanisman.TDUnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@DanismanAdSoyad"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "DanismanAdSoyad", Value = TDOBasvuruDanisman.TDAdSoyad });

                      
                        if (item.SablonParametreleri.Any(a => a == "@EsDanismanUnvanAdi"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "EsDanismanUnvanAdi", Value = EsDanisman.UnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@EsDanismanAdSoyad"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "EsDanismanAdSoyad", Value = EsDanisman.AdSoyad });
                        if (item.SablonParametreleri.Any(a => a == "@EsDanismanUniversite"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "EsDanismanUniversite", Value = EsDanisman.UniversiteAdi });
                        if (item.SablonParametreleri.Any(a => a == "@VarolanEsDanismanAdSoyad"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "VarolanEsDanismanAdSoyad", Value = EsDanisman.OncekiEsDanismanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@YeniEsDanismanUnvanAdi"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "YeniEsDanismanUnvanAdi", Value = EsDanisman.UnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@YeniEsDanismanAdSoyad"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "YeniEsDanismanUnvanAdi", Value = EsDanisman.AdSoyad });


                        if (item.SablonParametreleri.Any(a => a == "@TezBaslikTr"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "TezBaslikTr", Value = TDOBasvuruDanisman.TezBaslikTr });
                        if (item.SablonParametreleri.Any(a => a == "@TezBaslikEn"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "TezBaslikEn", Value = TDOBasvuruDanisman.TezBaslikEn });
                        if (item.SablonParametreleri.Any(a => a == "@TezDili"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "TezDili", Value = (TDOBasvuruDanisman.IsTezDiliTr ? "Türkçe" : "İngilizce") });

                        if (EsDanisman.EYKDaOnaylandi == true && item.SablonParametreleri.Any(a => a == "@EYKTarihi"))
                        {
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "EYKTarihi", Value = EsDanisman.EYKDaOnaylandiOnayTarihi.ToString("dd-MM-yyyy") });
                        }
                        if (IsOnayOrRed == false && item.SablonParametreleri.Any(a => a == "@RedAciklama"))
                        {
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "RedAciklama", Value = EsDanisman.EYKDaOnaylanmadiDurumAciklamasi });
                        }
                        if (item.SablonParametreleri.Any(a => a == "@Link"))
                        {
                            if (item.JuriTipAdi == "Öğrenci")
                                ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "Link", Value = Enstitu.SistemErisimAdresi + "/TDOBasvurular/Index?TDOBasvuruID=" + TDOBasvuruDanisman.TDOBasvuruID, IsLink = true });
                            else ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "Link", Value = Enstitu.SistemErisimAdresi + "/TDOGelenBasvurular/Index?TDOBasvuruID=" + TDOBasvuruDanisman.TDOBasvuruID, IsLink = true });
                        }
                        var mCOntent = SystemMails.GetSystemMailContent(Enstitu.EnstituAd, item.Sablon.SablonHtml, item.Sablon.SablonAdi, ParamereDegerleri);
                        var snded = MailManager.sendMail(Enstitu.EnstituKod, mCOntent.Title, mCOntent.HtmlContent, item.EMails, item.Attachments);
                        if (snded)
                        {
                            IsSended = true;
                            var kModel = new GonderilenMailler();
                            kModel.Tarih = DateTime.Now;
                            kModel.EnstituKod = Enstitu.EnstituKod;
                            kModel.MesajID = null;
                            kModel.IslemTarihi = DateTime.Now;
                            kModel.Konu = mCOntent.Title;
                            if (!item.JuriTipAdi.IsNullOrWhiteSpace()) kModel.Konu += " (" + item.JuriTipAdi + ")";
                            kModel.IslemYapanID = UserIdentity.Current == null || !UserIdentity.Current.IsAuthenticated ? 1 : UserIdentity.Current.Id;
                            kModel.IslemYapanIP = UserIdentity.Ip;
                            kModel.Aciklama = item.Sablon.Sablon ?? "";
                            kModel.AciklamaHtml = mCOntent.HtmlContent ?? "";
                            kModel.Gonderildi = true;
                            foreach (var itemGK in item.EMails)
                            {
                                kModel.GonderilenMailKullanicilars.Add(new GonderilenMailKullanicilar { Email = itemGK.EMail });
                            }
                            db.GonderilenMaillers.Add(kModel);
                        }
                    }
                    if (IsSended) db.SaveChanges();
                    mmMessage.IsSuccess = true;
                    mmMessage.MessageType = Msgtype.Success;

                }
            }
            catch (Exception ex)
            {
                var message = "Tez danışmanı öneri başvurusu için danışman ve öğrenciye mail gönderilirken bir hata oluştu! \r\nTDOBasvuruDanismanID:" + TDOBasvuruDanismanID;
                Management.SistemBilgisiKaydet(message + "\r\n Hata:" + ex.ToExceptionMessage(), "Management/sendMailTDOBilgisi \r\n" + ex.ToExceptionStackTrace(), BilgiTipi.Hata);
                mmMessage.Messages.Add(message + "</br> Hata:" + ex.ToExceptionMessage());
                mmMessage.MessageType = Msgtype.Error;
                mmMessage.IsSuccess = false;
            }
            return mmMessage;
        }
        public static MmMessage sendMailTIBilgisi(int? TIBasvuruAraRaporID, int? SRTalepID)
        {
            var mmMessage = new MmMessage();
            try
            {
                using (var db = new LisansustuBasvuruSistemiEntities())
                {

                    var TIAraRapor = new TIBasvuruAraRapor();
                    var SRTalebi = new SRTalepleri();

                    if (TIBasvuruAraRaporID.HasValue)
                    {
                        TIAraRapor = db.TIBasvuruAraRapors.Where(p => p.TIBasvuruAraRaporID == TIBasvuruAraRaporID).First();
                        if (SRTalepID.HasValue) SRTalebi = TIAraRapor.SRTalepleris.FirstOrDefault();
                    }
                    else if (SRTalepID.HasValue)
                    {
                        SRTalebi = db.SRTalepleris.Where(p => p.SRTalepID == SRTalepID).First();
                        TIAraRapor = SRTalebi.TIBasvuruAraRapor;
                    }

                    var Juriler = TIAraRapor.TIBasvuruAraRaporKomites.ToList();
                    var SablonTipIDs = new List<int>();
                    var mModel = new List<SablonMailModel>();
                    var Danisman = Juriler.Where(p => p.JuriTipAdi == "TezDanismani").First();
                    bool IsAraRaporOrToplanti = false;
                    var gonderilenMEkleris = new List<GonderilenMailEkleri>();
                    gonderilenMEkleris.Add(new GonderilenMailEkleri
                    {
                        EkAdi = TIAraRapor.TICalismaRaporDosyaAdi,
                        EkDosyaYolu = TIAraRapor.TICalismaRaporDosyaYolu,
                    });

                    if (TIBasvuruAraRaporID.HasValue)
                    {
                        IsAraRaporOrToplanti = true;
                        SablonTipIDs.AddRange(new List<int> { MailSablonTipi.TI_AraRaporBaslatildiOgrenci, MailSablonTipi.TI_AraRaporBaslatildiDanisman });
                        mModel.Add(new SablonMailModel
                        {

                            JuriTipAdi = "TezDanismani",
                            UnvanAdi = Danisman.UnvanAdi,
                            AdSoyad = Danisman.AdSoyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = Danisman.EMail, ToOrBcc = true } },
                            MailSablonTipID = MailSablonTipi.TI_AraRaporBaslatildiDanisman
                        });
                        mModel.Add(new SablonMailModel
                        {
                            JuriTipAdi = "Öğrenci " + TIAraRapor.TIBasvuru.Ad + " " + TIAraRapor.TIBasvuru.Soyad,

                            AdSoyad = TIAraRapor.TIBasvuru.Ad + " " + TIAraRapor.TIBasvuru.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = TIAraRapor.TIBasvuru.Kullanicilar.EMail, ToOrBcc = true } },
                            MailSablonTipID = MailSablonTipi.TI_AraRaporBaslatildiOgrenci
                        });
                    }
                    if (SRTalepID.HasValue)
                    {
                        SablonTipIDs.AddRange(new List<int> { MailSablonTipi.TI_ToplantiBilgiKomite, MailSablonTipi.TI_ToplantiBilgiOgrenci });
                        mModel.Add(new SablonMailModel
                        {

                            JuriTipAdi = "Öğrenci " + TIAraRapor.TIBasvuru.Ad + " " + TIAraRapor.TIBasvuru.Soyad,
                            AdSoyad = TIAraRapor.TIBasvuru.Ad + " " + TIAraRapor.TIBasvuru.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = TIAraRapor.TIBasvuru.Kullanicilar.EMail, ToOrBcc = true } },
                            MailSablonTipID = MailSablonTipi.TI_ToplantiBilgiOgrenci
                        });
                        foreach (var item in Juriler)
                        {
                            mModel.Add(new SablonMailModel
                            {

                                UnvanAdi = item.UnvanAdi,
                                AdSoyad = item.AdSoyad,
                                EMails = new List<MailSendList> { new MailSendList { EMail = item.EMail, ToOrBcc = true } },
                                MailSablonTipID = MailSablonTipi.TI_ToplantiBilgiKomite,
                                JuriTipAdi = item.JuriTipAdi,
                                TIBasvuruAraRaporKomiteID = Danisman.TIBasvuruAraRaporKomiteID
                            });
                        }
                    }

                    var Enstitu = TIAraRapor.TIBasvuru.Enstituler;

                    var Sablonlar = db.MailSablonlaris.Where(p => SablonTipIDs.Contains(p.MailSablonTipID) && p.EnstituKod == Enstitu.EnstituKod).ToList();


                    var AbdL = TIAraRapor.TIBasvuru.Programlar.AnabilimDallari;
                    var PrgL = TIAraRapor.TIBasvuru.Programlar;
                    var OncekiMailTarihi = IsAraRaporOrToplanti ? TIAraRapor.RSBaslatildiMailGonderimTarihi : TIAraRapor.ToplantiBilgiGonderimTarihi;

                    bool IsSended = false;
                    foreach (var item in mModel)
                    {

                        item.Sablon = Sablonlar.Where(p => p.MailSablonTipID == item.MailSablonTipID).First();
                        item.SablonParametreleri = item.Sablon.MailSablonTipleri.Parametreler.Split(',').ToList().Select(s => s.Trim()).ToList();

                        foreach (var itemSe in item.Sablon.MailSablonlariEkleris)
                        {
                            gonderilenMEkleris.Add(new GonderilenMailEkleri
                            {
                                EkAdi = itemSe.EkAdi,
                                EkDosyaYolu = itemSe.EkDosyaYolu,
                            });
                        }
                        foreach (var itemEk in gonderilenMEkleris)
                        {
                            var ekTamYol = System.Web.HttpContext.Current.Server.MapPath("~" + itemEk.EkDosyaYolu);
                            if (System.IO.File.Exists(ekTamYol))
                            {
                                var FExtension = Path.GetExtension(ekTamYol);
                                item.Attachments.Add(new System.Net.Mail.Attachment(new MemoryStream(System.IO.File.ReadAllBytes(ekTamYol)),
                                    itemEk.EkAdi.ToSetNameFileExtension(FExtension), System.Net.Mime.MediaTypeNames.Application.Octet));

                            }
                            else Management.SistemBilgisiKaydet("Mail gönderilirken eklenen dosya eki sistemde bulunamadı!<br/>Dosya Adı:" + itemEk.EkAdi + " <br/>Dosya Yolu:" + ekTamYol, "Management/sendMailTIToplantiBilgisi", BilgiTipi.Uyarı);
                        }
                        if (item.Sablon.GonderilecekEkEpostalar != null) item.EMails.AddRange(item.Sablon.GonderilecekEkEpostalar.Split(',').Select(s => new MailSendList { EMail = s.Trim(), ToOrBcc = false }));
                        var ParamereDegerleri = new List<MailReplaceParameterModel>();


                        if (item.SablonParametreleri.Any(a => a == "@EnstituAdi"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "EnstituAdi", Value = Enstitu.EnstituAd });
                        if (item.SablonParametreleri.Any(a => a == "@WebAdresi"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "WebAdresi", Value = Enstitu.WebAdresi, IsLink = true });
                        if (item.SablonParametreleri.Any(a => a == "@AdSoyad"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "AdSoyad", Value = item.AdSoyad });
                        if (item.SablonParametreleri.Any(a => a == "@UnvanAdi"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "UnvanAdi", Value = item.UnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciAdSoyad"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "OgrenciAdSoyad", Value = TIAraRapor.TIBasvuru.Ad + " " + TIAraRapor.TIBasvuru.Soyad });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciNo"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "OgrenciNo", Value = TIAraRapor.TIBasvuru.OgrenciNo });
                        if (item.SablonParametreleri.Any(a => a == "@AnabilimdaliAdi"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "AnabilimdaliAdi", Value = AbdL.AnabilimDaliAdi });
                        if (item.SablonParametreleri.Any(a => a == "@ProgramAdi"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "ProgramAdi", Value = PrgL.ProgramAdi });
                        if (item.SablonParametreleri.Any(a => a == "@TezBaslikTr"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "TezBaslikTr", Value = TIAraRapor.TezBaslikTr });
                        if (item.SablonParametreleri.Any(a => a == "@TezBaslikEn"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "TezBaslikEn", Value = TIAraRapor.TezBaslikEn });
                        if (item.SablonParametreleri.Any(a => a == "@YokDrBursiyeriBilgi"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "YokDrBursiyeriBilgi", Value = TIAraRapor.IsYokDrBursiyeriVar ? "Var (Öncelikli Alan: " + TIAraRapor.YokDrOncelikliAlan + ")" : "Yok" });
                        if (item.SablonParametreleri.Any(a => a == "@DonemAdi"))
                        {
                            var DonemBilgi = TIAraRapor.RaporTarihi.ToAraRaporDonemBilgi();
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "DonemAdi", Value = DonemBilgi.DonemAdiLong });
                        }
                        if (item.SablonParametreleri.Any(a => a == "@OncekiMailTarihi"))
                        {

                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "OncekiMailTarihi", Value = OncekiMailTarihi.HasValue ? OncekiMailTarihi.Value.ToString("dd-MM-yyyy HH:mm") : null });
                        }
                        #region SR Talebi
                        if (item.MailSablonTipID == MailSablonTipi.TI_ToplantiBilgiKomite || item.MailSablonTipID == MailSablonTipi.TI_ToplantiBilgiOgrenci)
                        {
                            if (item.SablonParametreleri.Any(a => a == "@ToplantiTarihi"))
                                ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "ToplantiTarihi", Value = SRTalebi.Tarih.ToLongDateString() });
                            if (item.SablonParametreleri.Any(a => a == "@ToplantiSaati"))
                                ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "ToplantiSaati", Value = string.Format("{0:hh\\:mm}", SRTalebi.BasSaat) });

                            if (!SRTalebi.IsOnline)
                            {
                                if (item.SablonParametreleri.Any(a => a == "@ToplantiSekli"))
                                {
                                    ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "ToplantiSekli", Value = "Yüz Yüze" });
                                }
                                if (item.SablonParametreleri.Any(a => a == "@ToplantiYeriBaslik"))
                                {
                                    ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "ToplantiYeriBaslik", Value = "Toplantı Salonu" });
                                }
                                if (item.SablonParametreleri.Any(a => a == "@ToplantiYeri"))
                                {
                                    ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "ToplantiYeri", Value = SRTalebi.SalonAdi });
                                }
                            }
                            else
                            {
                                if (item.SablonParametreleri.Any(a => a == "@ToplantiSekli"))
                                {
                                    ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "ToplantiSekli", Value = "Çevrim İçi" });
                                }
                                if (item.SablonParametreleri.Any(a => a == "@ToplantiYeriBaslik"))
                                {
                                    ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "ToplantiYeriBaslik", Value = "Toplantı Katılım Linki" });
                                }
                                if (item.SablonParametreleri.Any(a => a == "@ToplantiYeri"))
                                {
                                    ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "ToplantiYeri", Value = SRTalebi.SalonAdi, IsLink = true });
                                }
                            }
                        }
                        #endregion
                        #region DanismanKomite
                        if (item.SablonParametreleri.Any(a => a == "@DanismanBilgi"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "DanismanBilgi", Value = Danisman.UnvanAdi + " " + Danisman.AdSoyad });
                        if (item.SablonParametreleri.Any(a => a == "@DanismanUni"))
                        {
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "DanismanUni", Value = Danisman.UniversiteAdi });
                        }
                        foreach (var itemTik in Juriler.Where(p => p.JuriTipAdi != "TezDanismani").Select((s, inx) => new { s, inx = inx + 1 }))
                        {

                            if (item.SablonParametreleri.Any(a => a == "@TikBilgi" + itemTik.inx))
                                ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "TikBilgi" + itemTik.inx, Value = itemTik.s.UnvanAdi + " " + itemTik.s.AdSoyad });
                            if (item.SablonParametreleri.Any(a => a == "@TikBilgiUni" + itemTik.inx))
                            {
                                var UniversiteAdi = itemTik.s.UniversiteAdi;
                                ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "TikBilgiUni" + itemTik.inx, Value = UniversiteAdi });
                            }
                        }
                        #endregion

                        var mCOntent = SystemMails.GetSystemMailContent(Enstitu.EnstituAd, item.Sablon.SablonHtml, item.Sablon.SablonAdi, ParamereDegerleri);
                        var snded = MailManager.sendMail(Enstitu.EnstituKod, mCOntent.Title, mCOntent.HtmlContent, item.EMails, item.Attachments);
                        if (snded)
                        {
                            IsSended = true;
                            var kModel = new GonderilenMailler();
                            kModel.Tarih = DateTime.Now;
                            kModel.EnstituKod = Enstitu.EnstituKod;
                            kModel.MesajID = null;
                            kModel.IslemTarihi = DateTime.Now;
                            kModel.Konu = mCOntent.Title;
                            if (!item.JuriTipAdi.IsNullOrWhiteSpace()) kModel.Konu += " (" + item.JuriTipAdi + ")";
                            kModel.IslemYapanID = UserIdentity.Current == null || !UserIdentity.Current.IsAuthenticated ? 1 : UserIdentity.Current.Id;
                            kModel.IslemYapanIP = UserIdentity.Ip;
                            kModel.Aciklama = item.Sablon.Sablon ?? "";
                            kModel.AciklamaHtml = mCOntent.HtmlContent ?? "";
                            kModel.Gonderildi = true;
                            foreach (var itemGK in item.EMails)
                            {
                                kModel.GonderilenMailKullanicilars.Add(new GonderilenMailKullanicilar { Email = itemGK.EMail });
                            }
                            foreach (var itemME in gonderilenMEkleris)
                            {
                                kModel.GonderilenMailEkleris.Add(itemME);
                            }
                            db.GonderilenMaillers.Add(kModel);
                            if (IsAraRaporOrToplanti) TIAraRapor.RSBaslatildiMailGonderimTarihi = DateTime.Now;
                            else TIAraRapor.ToplantiBilgiGonderimTarihi = DateTime.Now;

                            LogIslemleri.LogEkle("TIBasvuruAraRapor", IslemTipi.Update, TIAraRapor.ToJson());


                        }
                    }
                    if (IsSended) db.SaveChanges();
                    mmMessage.IsSuccess = true;
                    mmMessage.MessageType = Msgtype.Success;

                }
            }
            catch (Exception ex)
            {
                var message = "Tez İzleme toplantısı için Komite üyelerine mail gönderilirken bir hata oluştu! \r\nSRTalepID:" + SRTalepID;
                Management.SistemBilgisiKaydet(message + "\r\n Hata:" + ex.ToExceptionMessage(), "Management/sendMailTIBilgisi \r\n" + ex.ToExceptionStackTrace(), BilgiTipi.Hata);
                mmMessage.Messages.Add(message + "</br> Hata:" + ex.ToExceptionMessage());
                mmMessage.MessageType = Msgtype.Error;
                mmMessage.IsSuccess = false;
            }
            return mmMessage;
        }
        public static MmMessage sendMailTIDegerlendirmeLink(int TIBasvuruAraRaporID, Guid? UniqueID, bool IsLinkOrSonuc)
        {
            var mmMessage = new MmMessage();
            try
            {
                using (var db = new LisansustuBasvuruSistemiEntities())
                {
                    var TIAraRapor = db.TIBasvuruAraRapors.Where(p => p.TIBasvuruAraRaporID == TIBasvuruAraRaporID).First();
                    var Juriler = TIAraRapor.TIBasvuruAraRaporKomites.Where(p => (IsLinkOrSonuc ? p.JuriTipAdi != "TezDanismani" : p.JuriTipAdi == "TezDanismani") && p.UniqueID == (UniqueID ?? p.UniqueID)).ToList();

                    var mModel = new List<SablonMailModel>();

                    var Enstitu = TIAraRapor.TIBasvuru.Enstituler;

                    var AbdL = TIAraRapor.TIBasvuru.Programlar.AnabilimDallari;
                    var PrgL = TIAraRapor.TIBasvuru.Programlar;

                    if (IsLinkOrSonuc)
                    {
                        foreach (var item in Juriler)
                        {
                            item.UniqueID = Guid.NewGuid();
                        }
                    }
                    else
                    {
                        mModel.Add(new SablonMailModel
                        {

                            JuriTipAdi = "Öğrenci " + TIAraRapor.TIBasvuru.Ad + " " + TIAraRapor.TIBasvuru.Soyad,
                            AdSoyad = TIAraRapor.TIBasvuru.Ad + " " + TIAraRapor.TIBasvuru.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = TIAraRapor.TIBasvuru.Kullanicilar.EMail, ToOrBcc = true } },
                            MailSablonTipID = MailSablonTipi.TI_DegerlendirmeSonucGonderimOgrenci,
                        });
                    }

                    foreach (var item in Juriler)
                    {
                        mModel.Add(new SablonMailModel
                        {
                            UniqueID = item.UniqueID,

                            UnvanAdi = item.UnvanAdi,
                            AdSoyad = item.AdSoyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = item.EMail, ToOrBcc = true } },
                            MailSablonTipID = IsLinkOrSonuc ? MailSablonTipi.TI_DegerlendirmeLinkGonderimKomite : MailSablonTipi.TI_DegerlendirmeSonucGonderimDanisman,
                            JuriTipAdi = item.JuriTipAdi,
                        });
                    }
                    var MailSablonTipIDs = mModel.Select(s => s.MailSablonTipID).Distinct().ToList();
                    var Sablonlar = db.MailSablonlaris.Where(p => MailSablonTipIDs.Contains(p.MailSablonTipID) && p.EnstituKod == Enstitu.EnstituKod).ToList();
                    foreach (var item in mModel)
                    {

                        var Juri = Juriler.Where(p => p.UniqueID == item.UniqueID).FirstOrDefault();
                        item.Sablon = Sablonlar.Where(p => p.MailSablonTipID == item.MailSablonTipID).First();
                        item.SablonParametreleri = item.Sablon.MailSablonTipleri.Parametreler.Split(',').ToList().Select(s => s.Trim()).ToList();
                        var GonderilenMailEkleri = new List<GonderilenMailEkleri>();

                        foreach (var itemSe in item.Sablon.MailSablonlariEkleris)
                        {
                            var ekTamYol = System.Web.HttpContext.Current.Server.MapPath("~" + itemSe.EkDosyaYolu);
                            if (System.IO.File.Exists(ekTamYol))
                            {
                                var FExtension = Path.GetExtension(ekTamYol);
                                item.Attachments.Add(new System.Net.Mail.Attachment(new MemoryStream(System.IO.File.ReadAllBytes(ekTamYol)),
                                    itemSe.EkAdi.ToSetNameFileExtension(FExtension), System.Net.Mime.MediaTypeNames.Application.Octet));
                                GonderilenMailEkleri.Add(new GonderilenMailEkleri
                                {
                                    EkAdi = itemSe.EkAdi,
                                    EkDosyaYolu = itemSe.EkDosyaYolu,
                                });
                            }
                            else Management.SistemBilgisiKaydet("Mail gönderilirken eklenen dosya eki sistemde bulunamadı!<br/>Dosya Adı:" + itemSe.EkAdi + " <br/>Dosya Yolu:" + ekTamYol, "Management/sendMailTIToplantiBilgisi", BilgiTipi.Uyarı);
                        }
                        if (!IsLinkOrSonuc)
                        {
                            var IDs = new List<int?>() { TIBasvuruAraRaporID };
                            if (item.MailSablonTipID == MailSablonTipi.TI_DegerlendirmeSonucGonderimDanisman) IDs.Add(1);
                            var Ekler = Management.exportRaporPdf(RaporTipleri.TezIzlemeDegerlendirmeFormu, IDs);
                            GonderilenMailEkleri.AddRange(Ekler.Select(s => new GonderilenMailEkleri { EkAdi = s.Name, EkDosyaYolu = "" }));
                            item.Attachments.AddRange(Ekler);
                        }

                        if (item.Sablon.GonderilecekEkEpostalar != null) item.EMails.AddRange(item.Sablon.GonderilecekEkEpostalar.Split(',').Select(s => new MailSendList { EMail = s.Trim(), ToOrBcc = false }));
                        var ParamereDegerleri = new List<MailReplaceParameterModel>();


                        if (item.SablonParametreleri.Any(a => a == "@EnstituAdi"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "EnstituAdi", Value = Enstitu.EnstituAd });
                        if (item.SablonParametreleri.Any(a => a == "@WebAdresi"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "WebAdresi", Value = Enstitu.WebAdresi, IsLink = true });
                        if (item.SablonParametreleri.Any(a => a == "@AdSoyad"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "AdSoyad", Value = item.AdSoyad });
                        if (item.SablonParametreleri.Any(a => a == "@UnvanAdi"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "UnvanAdi", Value = item.UnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciAdSoyad"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "OgrenciAdSoyad", Value = TIAraRapor.TIBasvuru.Ad + " " + TIAraRapor.TIBasvuru.Soyad });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciNo"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "OgrenciNo", Value = TIAraRapor.TIBasvuru.OgrenciNo });
                        if (item.SablonParametreleri.Any(a => a == "@AnabilimdaliAdi"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "AnabilimdaliAdi", Value = AbdL.AnabilimDaliAdi });
                        if (item.SablonParametreleri.Any(a => a == "@ProgramAdi"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "ProgramAdi", Value = PrgL.ProgramAdi });
                        if (item.SablonParametreleri.Any(a => a == "@TezBaslikTr"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "TezBaslikTr", Value = TIAraRapor.TezBaslikTr });
                        if (item.SablonParametreleri.Any(a => a == "@TezBaslikEn"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "TezBaslikEn", Value = TIAraRapor.TezBaslikEn });
                        if (item.SablonParametreleri.Any(a => a == "@YokDrBursiyeriBilgi"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "YokDrBursiyeriBilgi", Value = TIAraRapor.IsYokDrBursiyeriVar ? "Var (Öncelikli Alan: " + TIAraRapor.YokDrOncelikliAlan + ")" : "Yok" });
                        if (item.SablonParametreleri.Any(a => a == "@DonemAdi"))
                        {
                            var DonemBilgi = TIAraRapor.RaporTarihi.ToAraRaporDonemBilgi();
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "DonemAdi", Value = DonemBilgi.DonemAdiLong });
                        }
                        if (item.SablonParametreleri.Any(a => a == "@Link"))
                        {
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "Link", Value = Enstitu.SistemErisimAdresi + "/TIBasvuru/Index?IsDegerlendirme=" + item.UniqueID, IsLink = true });
                        }
                        if (item.SablonParametreleri.Any(a => a == "@OncekiMailTarihi"))
                        {
                            if (IsLinkOrSonuc)
                            {
                                ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "OncekiMailTarihi", Value = Juri.LinkGonderimTarihi.HasValue ? Juri.LinkGonderimTarihi.Value.ToString("dd-MM-yyyy HH:mm") : null });
                            }
                            else
                            {
                                ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "OncekiMailTarihi", Value = TIAraRapor.DegerlendirmeSonucMailTarihi.HasValue ? TIAraRapor.DegerlendirmeSonucMailTarihi.Value.ToString("dd-MM-yyyy HH:mm") : null });

                            }
                        }
                        var mCOntent = SystemMails.GetSystemMailContent(Enstitu.EnstituAd, item.Sablon.SablonHtml, item.Sablon.SablonAdi, ParamereDegerleri);
                        var snded = MailManager.sendMail(Enstitu.EnstituKod, mCOntent.Title, mCOntent.HtmlContent, item.EMails, item.Attachments);
                        if (snded)
                        {
                            var kModel = new GonderilenMailler();
                            kModel.Tarih = DateTime.Now;
                            kModel.EnstituKod = Enstitu.EnstituKod;
                            kModel.MesajID = null;
                            kModel.IslemTarihi = DateTime.Now;
                            kModel.Konu = mCOntent.Title;
                            if (!item.JuriTipAdi.IsNullOrWhiteSpace()) kModel.Konu += " (" + item.JuriTipAdi + ")";
                            kModel.IslemYapanID = UserIdentity.Current == null || !UserIdentity.Current.IsAuthenticated ? 1 : UserIdentity.Current.Id;
                            kModel.IslemYapanIP = UserIdentity.Ip;
                            kModel.Aciklama = item.Sablon.Sablon ?? "";
                            kModel.AciklamaHtml = mCOntent.HtmlContent ?? "";
                            kModel.Gonderildi = true;
                            foreach (var itemGK in item.EMails)
                            {
                                kModel.GonderilenMailKullanicilars.Add(new GonderilenMailKullanicilar { Email = itemGK.EMail });
                            }
                            foreach (var itemME in GonderilenMailEkleri)
                            {
                                kModel.GonderilenMailEkleris.Add(itemME);
                            }
                            if (IsLinkOrSonuc)
                            {
                                Juri.DegerlendirmeIslemTarihi = null;
                                Juri.DegerlendirmeIslemYapanIP = null;
                                Juri.DegerlendirmeYapanID = null;
                                Juri.IsBasarili = null;
                                Juri.IsTezIzlemeRaporuAltAlanUygun = null;
                                Juri.IsTezIzlemeRaporuTezOnerisiUygun = null;
                                Juri.Aciklama = null;
                                Juri.IsLinkGonderildi = true;
                                Juri.LinkGonderimTarihi = DateTime.Now;
                                Juri.LinkGonderenID = UserIdentity.Current.Id;

                            }

                            db.GonderilenMaillers.Add(kModel);
                            db.SaveChanges();
                            if (IsLinkOrSonuc) LogIslemleri.LogEkle("TIBasvuruAraRaporKomite", IslemTipi.Update, Juri.ToJson());
                        }
                    }

                    mmMessage.IsSuccess = true;
                    mmMessage.MessageType = Msgtype.Success;

                }
            }
            catch (Exception ex)
            {
                var message = "";
                if (IsLinkOrSonuc) message = "Tez İzleme değerlendirmesi için Komite üyelerine değerlendirme davetiye linki mail olarak gönderilirken bir hata oluştu!";
                else message = "Tez İzleme değerlendirmesi sonucu Komite üyelerine mail olarak gönderilirken bir hata oluştu!";
                Management.SistemBilgisiKaydet(message + "\r\n Hata:" + ex.ToExceptionMessage(), "Management/sendMailTIDegerlendirmeLink \r\n" + ex.ToExceptionStackTrace(), BilgiTipi.Hata);
                //mmMessage.Title = "Hata";
                mmMessage.Messages.Add(message + "</br> Hata:" + ex.ToExceptionMessage());
                mmMessage.MessageType = Msgtype.Error;
                mmMessage.IsSuccess = false;
            }
            return mmMessage;
        }
        public static MmMessage sendMailMezuniyetDegerlendirmeLink(int SRTalepID, Guid? UniqueID = null, bool IsLinkOrSonuc = false, bool IsYeniLink = true, string EMail = "")
        {
            var mmMessage = new MmMessage();
            try
            {
                using (var db = new LisansustuBasvuruSistemiEntities())
                {

                    var SRTalep = db.SRTalepleris.Where(p => p.SRTalepID == SRTalepID).First();
                    var qJuriler = SRTalep.SRTaleplerJuris.AsQueryable();
                    if (UniqueID.HasValue) qJuriler = qJuriler.Where(p => p.UniqueID == UniqueID.Value);
                    else qJuriler = qJuriler.Where(p => p.JuriTipAdi != "TezDanismani");
                    var Juriler = qJuriler.ToList();
                    var MB = SRTalep.MezuniyetBasvurulari;
                    var mModel = new List<SablonMailModel>();

                    var Enstitu = MB.MezuniyetSureci.Enstituler;

                    var AbdL = MB.Programlar.AnabilimDallari;
                    var PrgL = MB.Programlar;
                    var Jof = MB.MezuniyetJuriOneriFormlaris.First();



                    if (IsLinkOrSonuc)
                    {

                        foreach (var item in Juriler)
                        {
                            if (IsYeniLink) item.UniqueID = Guid.NewGuid();
                            mModel.Add(new SablonMailModel
                            {
                                UniqueID = item.UniqueID,

                                UnvanAdi = item.UnvanAdi,
                                AdSoyad = item.JuriAdi,
                                EMails = new List<MailSendList> { new MailSendList { EMail = (EMail.IsNullOrWhiteSpace() ? item.Email : EMail), ToOrBcc = true } },
                                MailSablonTipID = MB.OgrenimTipKod == OgrenimTipi.TezliYuksekLisans ? MailSablonTipi.Mez_SinavDegerlendirmeDavetGonderimJuriYL : MailSablonTipi.Mez_SinavDegerlendirmeDavetGonderimJuriDR,
                                JuriTipAdi = item.JuriTipAdi,
                            });
                        }
                    }
                    else
                    {
                        var Danisman = db.Kullanicilars.Where(p => p.KullaniciID == MB.TezDanismanID).First();
                        var SRDanisman = SRTalep.SRTaleplerJuris.Where(p => p.JuriTipAdi == "TezDanismani").FirstOrDefault();
                        mModel.Add(new SablonMailModel
                        {
                            UniqueID = null,
                            UnvanAdi = Danisman.Unvanlar.UnvanAdi,
                            AdSoyad = Danisman.Ad + " " + Danisman.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = SRDanisman != null && !SRDanisman.Email.IsNullOrWhiteSpace() ? SRDanisman.Email : Danisman.EMail, ToOrBcc = true } },
                            MailSablonTipID = MB.OgrenimTipKod == OgrenimTipi.TezliYuksekLisans ? MailSablonTipi.Mez_SinavSonucBilgiGonderimDanismanYL : MailSablonTipi.Mez_SinavSonucBilgiGonderimDanismanDR,
                            JuriTipAdi = "",
                        });
                        var Ogrenci = db.Kullanicilars.Where(p => p.KullaniciID == MB.KullaniciID).First();
                        mModel.Add(new SablonMailModel
                        {
                            UniqueID = null,

                            UnvanAdi = "",
                            AdSoyad = Ogrenci.Ad + " " + Ogrenci.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = Ogrenci.EMail, ToOrBcc = true } },
                            MailSablonTipID = MB.OgrenimTipKod == OgrenimTipi.TezliYuksekLisans ? MailSablonTipi.Mez_SinavSonucBilgiGonderimOgrenciYL : MailSablonTipi.Mez_SinavSonucBilgiGonderimOgrenciDR,
                            JuriTipAdi = "",
                        });
                    }

                    var MailSablonTipIDs = mModel.Select(s => s.MailSablonTipID).Distinct().ToList();
                    var Sablonlar = db.MailSablonlaris.Where(p => MailSablonTipIDs.Contains(p.MailSablonTipID) && p.EnstituKod == Enstitu.EnstituKod).ToList();
                    foreach (var item in mModel)
                    {

                        item.Sablon = Sablonlar.Where(p => p.MailSablonTipID == item.MailSablonTipID).First();
                        item.SablonParametreleri = item.Sablon.MailSablonTipleri.Parametreler.Split(',').ToList().Select(s => s.Trim()).ToList();
                        var GonderilenMailEkleri = new List<GonderilenMailEkleri>();
                        if (!IsLinkOrSonuc)
                        {
                            var IDs = new List<int?>() { SRTalepID };
                            var Ekler = Management.exportRaporPdf(RaporTipleri.MezuniyetTezSinavSonucFormu, IDs);
                            GonderilenMailEkleri.AddRange(Ekler.Select(s => new GonderilenMailEkleri { EkAdi = s.Name, EkDosyaYolu = "" }));
                            item.Attachments.AddRange(Ekler);
                        }
                        foreach (var itemSe in item.Sablon.MailSablonlariEkleris)
                        {
                            var ekTamYol = System.Web.HttpContext.Current.Server.MapPath("~" + itemSe.EkDosyaYolu);
                            if (System.IO.File.Exists(ekTamYol))
                            {
                                var FExtension = Path.GetExtension(ekTamYol);
                                item.Attachments.Add(new System.Net.Mail.Attachment(new MemoryStream(System.IO.File.ReadAllBytes(ekTamYol)),
                                    itemSe.EkAdi.ToSetNameFileExtension(FExtension), System.Net.Mime.MediaTypeNames.Application.Octet));
                                GonderilenMailEkleri.Add(new GonderilenMailEkleri
                                {
                                    EkAdi = itemSe.EkAdi,
                                    EkDosyaYolu = itemSe.EkDosyaYolu,
                                });
                            }
                            else Management.SistemBilgisiKaydet("Mail gönderilirken eklenen dosya eki sistemde bulunamadı!<br/>Dosya Adı:" + itemSe.EkAdi + " <br/>Dosya Yolu:" + ekTamYol, "Management/sendMailMezuniyetDegerlendirmeLink", BilgiTipi.Uyarı);
                        }
                        if (item.Sablon.GonderilecekEkEpostalar != null) item.EMails.AddRange(item.Sablon.GonderilecekEkEpostalar.Split(',').Select(s => new MailSendList { EMail = s.Trim(), ToOrBcc = false }));
                        var ParamereDegerleri = new List<MailReplaceParameterModel>();

                        if (item.SablonParametreleri.Any(a => a == "@EnstituAdi"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "EnstituAdi", Value = Enstitu.EnstituAd });
                        if (item.SablonParametreleri.Any(a => a == "@WebAdresi"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "WebAdresi", Value = Enstitu.WebAdresi, IsLink = true });
                        if (IsLinkOrSonuc)
                        {
                            if (item.SablonParametreleri.Any(a => a == "@JuriAdSoyad"))
                                ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "JuriAdSoyad", Value = item.AdSoyad });
                            if (item.SablonParametreleri.Any(a => a == "@JuriUnvanAdi"))
                                ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "JuriUnvanAdi", Value = item.UnvanAdi });
                        }
                        else
                        {
                            if (item.SablonParametreleri.Any(a => a == "@DanismanUnvanAdi"))
                                ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "DanismanUnvanAdi", Value = MB.TezDanismanUnvani });
                            if (item.SablonParametreleri.Any(a => a == "@DanismanAdSoyad"))
                                ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "DanismanAdSoyad", Value = MB.TezDanismanAdi });
                        }
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciAdSoyad"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "OgrenciAdSoyad", Value = MB.Ad + " " + MB.Soyad });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciNo"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "OgrenciNo", Value = MB.OgrenciNo });
                        if (item.SablonParametreleri.Any(a => a == "@AnabilimdaliAdi"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "AnabilimdaliAdi", Value = AbdL.AnabilimDaliAdi });
                        if (item.SablonParametreleri.Any(a => a == "@ProgramAdi"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "ProgramAdi", Value = PrgL.ProgramAdi });
                        if (item.SablonParametreleri.Any(a => a == "@SinavTarihi"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "SinavTarihi", Value = SRTalep.Tarih.ToLongDateString() });
                        if (item.SablonParametreleri.Any(a => a == "@SinavSaati"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "SinavSaati", Value = string.Format("{0:hh\\:mm}", SRTalep.BasSaat) });
                        if (item.SablonParametreleri.Any(a => a == "@Link"))
                        {
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "Link", Value = Enstitu.SistemErisimAdresi + "/Mezuniyet/GSinavDegerlendir?UniqueID=" + item.UniqueID, IsLink = true });
                        }

                        var mCOntent = SystemMails.GetSystemMailContent(Enstitu.EnstituAd, item.Sablon.SablonHtml, item.Sablon.SablonAdi, ParamereDegerleri);
                        var snded = MailManager.sendMail(Enstitu.EnstituKod, mCOntent.Title, mCOntent.HtmlContent, item.EMails, item.Attachments);
                        if (snded)
                        {
                            var kModel = new GonderilenMailler();
                            kModel.Tarih = DateTime.Now;
                            kModel.EnstituKod = Enstitu.EnstituKod;
                            kModel.MesajID = null;
                            kModel.IslemTarihi = DateTime.Now;
                            kModel.Konu = mCOntent.Title;
                            if (!item.JuriTipAdi.IsNullOrWhiteSpace()) kModel.Konu += " (" + item.JuriTipAdi + ")";
                            kModel.IslemYapanID = UserIdentity.Current == null || !UserIdentity.Current.IsAuthenticated ? 1 : UserIdentity.Current.Id;
                            kModel.IslemYapanIP = UserIdentity.Ip;
                            kModel.Aciklama = item.Sablon.Sablon ?? "";
                            kModel.AciklamaHtml = mCOntent.HtmlContent ?? "";
                            kModel.Gonderildi = true;
                            foreach (var itemGK in item.EMails)
                            {
                                kModel.GonderilenMailKullanicilars.Add(new GonderilenMailKullanicilar { Email = itemGK.EMail });
                            }
                            foreach (var itemME in GonderilenMailEkleri)
                            {
                                kModel.GonderilenMailEkleris.Add(itemME);
                            }
                            if (IsLinkOrSonuc)
                            {
                                var Juri = Juriler.Where(p => p.UniqueID == item.UniqueID).FirstOrDefault();

                                Juri.DegerlendirmeIslemTarihi = null;
                                Juri.DegerlendirmeIslemYapanIP = null;
                                Juri.DegerlendirmeYapanID = null;
                                Juri.MezuniyetSinavDurumID = null;
                                Juri.Aciklama = null;
                                Juri.IsLinkGonderildi = true;
                                Juri.LinkGonderimTarihi = DateTime.Now;
                                Juri.LinkGonderenID = UserIdentity.Current.Id;
                                db.SaveChanges();
                                LogIslemleri.LogEkle("SRTaleplerJuri", IslemTipi.Update, Juri.ToJson());
                            }

                            db.GonderilenMaillers.Add(kModel);
                            db.SaveChanges();
                        }
                    }

                    mmMessage.IsSuccess = true;
                    mmMessage.MessageType = Msgtype.Success;

                }
            }
            catch (Exception ex)
            {
                var message = "";
                message = "Tez Sınavı değerlendirmesi için Jüri üyelerine değerlendirme davetiye linki mail olarak gönderilirken bir hata oluştu!";
                Management.SistemBilgisiKaydet(message + "\r\n Hata:" + ex.ToExceptionMessage(), "Management/sendMailMezuniyetDegerlendirmeLink \r\n" + ex.ToExceptionStackTrace(), BilgiTipi.Hata);
                //mmMessage.Title = "Hata";
                mmMessage.Messages.Add(message + "</br> Hata:" + ex.ToExceptionMessage());
                mmMessage.MessageType = Msgtype.Error;
                mmMessage.IsSuccess = false;
            }
            return mmMessage;
        }

        public static MmMessage sendMailMezuniyetSinavYerBilgisi(int SRTalepID, bool IsOnaylandi)
        {
            var mmMessage = new MmMessage();
            try
            {
                using (var db = new LisansustuBasvuruSistemiEntities())
                {



                    var talep = db.SRTalepleris.Where(p => p.SRTalepID == SRTalepID).First();

                    var MB = talep.MezuniyetBasvurulari;
                    var JuriOneriFormu = MB.MezuniyetJuriOneriFormlaris.First();


                    var Enstitu = MB.MezuniyetSureci.Enstituler;

                    var mModel = new List<SablonMailModel>();
                    var JuriSablonTipID = 0;
                    var OgrenciSablonTipID = 0;
                    if (IsOnaylandi)
                    {
                        JuriSablonTipID = MB.OgrenimTipKod == OgrenimTipi.TezliYuksekLisans ? MailSablonTipi.Mez_SinavYerBilgisiGonderimJuriYL : MailSablonTipi.Mez_SinavYerBilgisiGonderimJuriDoktora;
                        OgrenciSablonTipID = MB.OgrenimTipKod == OgrenimTipi.TezliYuksekLisans ? MailSablonTipi.Mez_SinavYerBilgisiGonderimOgrenciYL : MailSablonTipi.Mez_SinavYerBilgisiGonderimOgrenciDoktora;
                    }
                    else
                    {
                        JuriSablonTipID = MailSablonTipi.Mez_SinavYerBilgisiOnaylanmadi;
                        OgrenciSablonTipID = MailSablonTipi.Mez_SinavYerBilgisiOnaylanmadi;
                    }


                    var Juriler = JuriOneriFormu.MezuniyetJuriOneriFormuJurileris.Where(p => p.IsAsilOrYedek.HasValue).ToList();

                    mModel.Add(new SablonMailModel
                    {
                        AdSoyad = MB.Ad + " " + MB.Soyad,
                        EMails = new List<MailSendList> { new MailSendList { EMail = MB.Kullanicilar.EMail, ToOrBcc = true } },
                        MailSablonTipID = OgrenciSablonTipID,
                    });
                    var Danisman = Juriler.Where(p => p.JuriTipAdi == "TezDanismani").First();
                    if (!IsOnaylandi)
                    {
                        mModel.Add(new SablonMailModel
                        {

                            AdSoyad = Danisman.UnvanAdi + " " + Danisman.AdSoyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = Danisman.EMail, ToOrBcc = true } },
                            MailSablonTipID = JuriSablonTipID,

                        });
                        if (!MB.TezEsDanismanEMail.IsNullOrWhiteSpace())
                            mModel.Add(new SablonMailModel
                            {

                                AdSoyad = MB.TezEsDanismanUnvani + " " + MB.TezEsDanismanAdi,
                                EMails = new List<MailSendList> { new MailSendList { EMail = MB.TezEsDanismanEMail, ToOrBcc = true } },
                                MailSablonTipID = JuriSablonTipID,

                            });
                    }
                    else
                    {
                        foreach (var item in Juriler.Where(p => p.IsAsilOrYedek == true))
                        {


                            mModel.Add(new SablonMailModel
                            {

                                AdSoyad = item.AdSoyad,
                                EMails = new List<MailSendList> { new MailSendList { EMail = item.EMail, ToOrBcc = true } },
                                MailSablonTipID = JuriSablonTipID,
                                JuriTipAdi = item.JuriTipAdi,
                                UnvanAdi = item.UnvanAdi
                            });

                            if (item.JuriTipAdi == "TezDanismani" && !MB.TezEsDanismanEMail.IsNullOrWhiteSpace())
                            {
                                mModel.Add(new SablonMailModel
                                {

                                    AdSoyad = MB.TezEsDanismanAdi,
                                    EMails = new List<MailSendList> { new MailSendList { EMail = MB.TezEsDanismanEMail, ToOrBcc = true } },
                                    MailSablonTipID = JuriSablonTipID,
                                    JuriTipAdi = item.JuriTipAdi,
                                    UnvanAdi = MB.TezEsDanismanUnvani
                                });
                            }
                        }


                    }


                    foreach (var item in mModel)
                    {

                        var AbdL = MB.Programlar.AnabilimDallari;
                        var PrgL = MB.Programlar;
                        item.Sablon = db.MailSablonlaris.Where(p => p.MailSablonTipID == item.MailSablonTipID && p.EnstituKod == Enstitu.EnstituKod).First();
                        item.SablonParametreleri = item.Sablon.MailSablonTipleri.Parametreler.Split(',').ToList().Select(s => s.Trim()).ToList();
                        var gonderilenMEkleris = new List<GonderilenMailEkleri>();
                        foreach (var itemSe in item.Sablon.MailSablonlariEkleris)
                        {
                            var ekTamYol = System.Web.HttpContext.Current.Server.MapPath("~" + itemSe.EkDosyaYolu);
                            if (System.IO.File.Exists(ekTamYol))
                            {
                                var FExtension = Path.GetExtension(ekTamYol);
                                item.Attachments.Add(new System.Net.Mail.Attachment(new MemoryStream(System.IO.File.ReadAllBytes(ekTamYol)),
                                    itemSe.EkAdi.ToSetNameFileExtension(FExtension), System.Net.Mime.MediaTypeNames.Application.Octet));
                                gonderilenMEkleris.Add(new GonderilenMailEkleri
                                {
                                    EkAdi = itemSe.EkAdi,
                                    EkDosyaYolu = itemSe.EkDosyaYolu,
                                });
                            }
                            else Management.SistemBilgisiKaydet("Mail gönderilirken eklenen dosya eki sistemde bulunamadı!<br/>Dosya Adı:" + itemSe.EkAdi + " <br/>Dosya Yolu:" + ekTamYol, "Management/sendMailMezuniyetSinavYerBilgisi", BilgiTipi.Uyarı);
                        }


                        if (item.Sablon.GonderilecekEkEpostalar != null) item.EMails.AddRange(item.Sablon.GonderilecekEkEpostalar.Split(',').Select(s => new MailSendList { EMail = s.Trim(), ToOrBcc = false }));
                        var ParamereDegerleri = new List<MailReplaceParameterModel>();

                        if (item.SablonParametreleri.Any(a => a == "@EnstituAdi"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "EnstituAdi", Value = Enstitu.EnstituAd });
                        if (item.SablonParametreleri.Any(a => a == "@WebAdresi"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "WebAdresi", Value = Enstitu.WebAdresi, IsLink = true });
                        if (item.SablonParametreleri.Any(a => a == "@EYKTarihi"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "EYKTarihi", Value = MB.EYKTarihi.Value.ToDateString() });
                        if (item.SablonParametreleri.Any(a => a == "@AdSoyad"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "AdSoyad", Value = item.AdSoyad });
                        if (item.SablonParametreleri.Any(a => a == "@UnvanAdi"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "UnvanAdi", Value = item.UnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciAdSoyad"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "OgrenciAdSoyad", Value = MB.Ad + " " + MB.Soyad });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciNo"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "OgrenciNo", Value = MB.OgrenciNo });
                        if (item.SablonParametreleri.Any(a => a == "@AnabilimdaliAdi"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "AnabilimdaliAdi", Value = AbdL.AnabilimDaliAdi });
                        if (item.SablonParametreleri.Any(a => a == "@ProgramAdi"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "ProgramAdi", Value = PrgL.ProgramAdi });
                        if (item.SablonParametreleri.Any(a => a == "@SinavTarihi"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "SinavTarihi", Value = talep.Tarih.ToLongDateString() });
                        if (item.SablonParametreleri.Any(a => a == "@SinavSaati"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "SinavSaati", Value = string.Format("{0:hh\\:mm}", talep.BasSaat) + "-" + string.Format("{0:hh\\:mm}", talep.BitSaat) });
                        if (item.SablonParametreleri.Any(a => a == "@SinavYeri"))
                        {
                            var SinavYerAdi = talep.SRSalonID.HasValue ? talep.SRSalonlar.SalonAdi : talep.SalonAdi;
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "SinavYeri", Value = SinavYerAdi });
                        }
                        if (item.SablonParametreleri.Any(a => a == "@IptalAciklamasi"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "IptalAciklamasi", Value = talep.SRDurumAciklamasi });
                        if (item.SablonParametreleri.Any(a => a == "@DanismanBilgi"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "DanismanBilgi", Value = Danisman.UnvanAdi + " " + Danisman.AdSoyad });
                        if (item.SablonParametreleri.Any(a => a == "@DanismanUni"))
                        {
                            var UniversiteAdi = Danisman.UniversiteID.HasValue ? Danisman.Universiteler.Ad : Danisman.UniversiteAdi;
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "DanismanUni", Value = UniversiteAdi });
                        }
                        foreach (var itemAsil in JuriOneriFormu.MezuniyetJuriOneriFormuJurileris.Where(p => p.JuriTipAdi != "TezDanismani" && p.IsAsilOrYedek == true).Select((s, inx) => new { s, inx = inx + 1 }))
                        {

                            if (item.SablonParametreleri.Any(a => a == "@AsilBilgi" + itemAsil.inx))
                                ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "AsilBilgi" + itemAsil.inx, Value = itemAsil.s.UnvanAdi + " " + itemAsil.s.AdSoyad });
                            if (item.SablonParametreleri.Any(a => a == "@AsilBilgiUni" + itemAsil.inx))
                            {
                                var UniversiteAdi = itemAsil.s.UniversiteID.HasValue ? itemAsil.s.Universiteler.Ad : itemAsil.s.UniversiteAdi;
                                ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "AsilBilgiUni" + itemAsil.inx, Value = UniversiteAdi });
                            }
                        }
                        foreach (var itemYedek in JuriOneriFormu.MezuniyetJuriOneriFormuJurileris.Where(p => p.IsAsilOrYedek == false).Select((s, inx) => new { s, inx = inx + 1 }))
                        {
                            if (item.SablonParametreleri.Any(a => a == "@YedekBilgi" + itemYedek.inx))
                                ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "YedekBilgi" + itemYedek.inx, Value = itemYedek.s.UnvanAdi + " " + itemYedek.s.AdSoyad });
                            if (item.SablonParametreleri.Any(a => a == "@YedekBilgiUni" + itemYedek.inx))
                            {
                                var UniversiteAdi = itemYedek.s.UniversiteID.HasValue ? itemYedek.s.Universiteler.Ad : itemYedek.s.UniversiteAdi;
                                ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "YedekBilgiUni" + itemYedek.inx, Value = UniversiteAdi });
                            }
                        }


                        var mCOntent = SystemMails.GetSystemMailContent(Enstitu.EnstituAd, item.Sablon.SablonHtml, item.Sablon.SablonAdi, ParamereDegerleri);
                        var snded = MailManager.sendMail(Enstitu.EnstituKod, mCOntent.Title, mCOntent.HtmlContent, item.EMails, item.Attachments);
                        if (snded)
                        {
                            var kModel = new GonderilenMailler();
                            kModel.Tarih = DateTime.Now;
                            kModel.EnstituKod = Enstitu.EnstituKod;
                            kModel.MesajID = null;
                            kModel.IslemTarihi = DateTime.Now;
                            kModel.Konu = item.Sablon.SablonAdi + " (" + item.AdSoyad + ")";
                            if (!item.JuriTipAdi.IsNullOrWhiteSpace()) kModel.Konu = kModel.Konu + " (" + item.JuriTipAdi + ")";
                            kModel.IslemYapanID = UserIdentity.Current == null || !UserIdentity.Current.IsAuthenticated ? 1 : UserIdentity.Current.Id;
                            kModel.IslemYapanIP = UserIdentity.Ip;
                            kModel.Aciklama = item.Sablon.Sablon ?? "";
                            kModel.AciklamaHtml = mCOntent.HtmlContent ?? "";
                            kModel.Gonderildi = true;
                            kModel.GonderilenMailKullanicilars = item.EMails.Select(s => new GonderilenMailKullanicilar { Email = s.EMail }).ToList();
                            kModel.GonderilenMailEkleris = gonderilenMEkleris;
                            db.GonderilenMaillers.Add(kModel);
                            db.SaveChanges();
                        }
                    }
                    var message = "'" + talep.Kullanicilar.Ad + " " + talep.Kullanicilar.Soyad + "'  kullanıcısının yapmış olduğu salon rezervasyonuna ait " + Juriler.Count + " adet jüriye mail olarak gönderildi!";
                    mmMessage.IsSuccess = true;
                    mmMessage.Messages.Add(message);
                    mmMessage.MessageType = Msgtype.Success;

                }
            }
            catch (Exception ex)
            {
                var message = "Salon rezervasyonuna ait jürilere mail gönderilirken bir hata oluştu! \r\nSRTalepID:" + SRTalepID;
                Management.SistemBilgisiKaydet(message + "\r\n Hata:" + ex.ToExceptionMessage(), "Management/sendMailMezuniyetSinavYerBilgisi \r\n" + ex.ToExceptionStackTrace(), BilgiTipi.Hata);
                //mmMessage.Title = "Hata";
                mmMessage.Messages.Add(message + "</br> Hata:" + ex.ToExceptionMessage());
                mmMessage.MessageType = Msgtype.Error;
                mmMessage.IsSuccess = false;
            }
            return mmMessage;
        }

        public static MmMessage sendMailMezuniyetSinavSonucu(int SRTalepID, int MezuniyetSinavDurumID)
        {
            var mmMessage = new MmMessage();
            try
            {
                using (var db = new LisansustuBasvuruSistemiEntities())
                {
                    var talep = db.SRTalepleris.Where(p => p.SRTalepID == SRTalepID).First();

                    var MB = talep.MezuniyetBasvurulari;
                    var MBOtipKriter = talep.MezuniyetBasvurulari.MezuniyetSureci.MezuniyetSureciOgrenimTipKriterleris.Where(p => p.OgrenimTipKod == MB.OgrenimTipKod).First();
                    var JuriOneriFormu = MB.MezuniyetJuriOneriFormlaris.First();


                    var Enstitu = MB.MezuniyetSureci.Enstituler;

                    var mModel = new List<SablonMailModel>();
                    var SablonTipID = 0;
                    if (MezuniyetSinavDurumID == MezuniyetSinavDurum.Basarili)
                    {
                        SablonTipID = MB.OgrenimTipKod == OgrenimTipi.TezliYuksekLisans ? MailSablonTipi.Mez_SinavSonucuBasariliBilgisiGonderimYL : MailSablonTipi.Mez_SinavSonucuBasariliBilgisiGonderimDoktora;
                    }
                    else if (MezuniyetSinavDurumID == MezuniyetSinavDurum.Uzatma)
                    {
                        SablonTipID = MailSablonTipi.Mez_SinavSonucuUzatmaBilgisiGonderim;
                    }


                    var TezDanismani = JuriOneriFormu.MezuniyetJuriOneriFormuJurileris.Where(p => p.JuriTipAdi == "TezDanismani").First();

                    var MezuniyetMailModel = new SablonMailModel
                    {

                        AdSoyad = MB.Ad + " " + MB.Soyad,
                        EMails = new List<MailSendList> {
                                        new MailSendList {EMail= MB.Kullanicilar.EMail,
                                                           ToOrBcc = true
                                                         }
                                                    },
                        MailSablonTipID = SablonTipID,
                        Attachments = MezuniyetSinavDurumID == MezuniyetSinavDurum.Basarili ? Management.exportRaporPdf(RaporTipleri.MezuniyetTezDuzeltmeVeJuriUyelerineTezTeslimTutanagi, new List<int?> { SRTalepID }) : new List<System.Net.Mail.Attachment>()
                    };


                    MezuniyetMailModel.EMails.Add(new MailSendList
                    {
                        EMail = TezDanismani.EMail,
                        ToOrBcc = false
                    });
                    if (!MB.TezEsDanismanEMail.IsNullOrWhiteSpace()) MezuniyetMailModel.EMails.Add(new MailSendList { EMail = MB.TezEsDanismanEMail, ToOrBcc = false });



                    mModel.Add(MezuniyetMailModel);


                    foreach (var item in mModel)
                    {


                        var AbdL = MB.Programlar.AnabilimDallari;
                        var PrgL = MB.Programlar;
                        item.Sablon = db.MailSablonlaris.Where(p => p.MailSablonTipID == item.MailSablonTipID && p.EnstituKod == Enstitu.EnstituKod).First();
                        item.SablonParametreleri = item.Sablon.MailSablonTipleri.Parametreler.Split(',').ToList().Select(s => s.Trim()).ToList();

                        if (item.Sablon.GonderilecekEkEpostalar != null) item.EMails.AddRange(item.Sablon.GonderilecekEkEpostalar.Split(',').Select(s => new MailSendList { EMail = s.Trim(), ToOrBcc = false }));


                        var gonderilenMEkleris = new List<GonderilenMailEkleri>();
                        foreach (var itemSe in item.Sablon.MailSablonlariEkleris)
                        {
                            var ekTamYol = System.Web.HttpContext.Current.Server.MapPath("~" + itemSe.EkDosyaYolu);
                            if (System.IO.File.Exists(ekTamYol))
                            {
                                var FExtension = Path.GetExtension(ekTamYol);
                                item.Attachments.Add(new System.Net.Mail.Attachment(new MemoryStream(System.IO.File.ReadAllBytes(ekTamYol)),
                                    itemSe.EkAdi.ToSetNameFileExtension(FExtension), System.Net.Mime.MediaTypeNames.Application.Octet));
                                gonderilenMEkleris.Add(new GonderilenMailEkleri
                                {
                                    EkAdi = itemSe.EkAdi,
                                    EkDosyaYolu = itemSe.EkDosyaYolu,
                                });
                            }
                            else Management.SistemBilgisiKaydet("Mail gönderilirken eklenen dosya eki sistemde bulunamadı!<br/>Dosya Adı:" + itemSe.EkAdi + " <br/>Dosya Yolu:" + ekTamYol, "Management/sendMailMezuniyetSinavSonucu", BilgiTipi.Uyarı);
                        }

                        var ParamereDegerleri = new List<MailReplaceParameterModel>();

                        if (item.SablonParametreleri.Any(a => a == "@EnstituAdi"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "EnstituAdi", Value = Enstitu.EnstituAd });
                        if (item.SablonParametreleri.Any(a => a == "@WebAdresi"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "WebAdresi", Value = Enstitu.WebAdresi, IsLink = true });

                        if (item.SablonParametreleri.Any(a => a == "@AdSoyad"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "AdSoyad", Value = item.AdSoyad });

                        if (item.SablonParametreleri.Any(a => a == "@SinavTarihi"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "SinavTarihi", Value = talep.Tarih.ToString("dd-MM-yyyy") });
                        if (item.SablonParametreleri.Any(a => a == "@SinavYeri"))
                        {
                            var SinavYerAdi = talep.SRSalonID.HasValue ? talep.SRSalonlar.SalonAdi : talep.SalonAdi;
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "SinavYeri", Value = SinavYerAdi });
                        }
                        if (item.SablonParametreleri.Any(a => a == "@UzatmaTarihi"))
                        {
                            if (talep.MezuniyetSinavDurumID == MezuniyetSinavDurum.Uzatma)
                            {
                                var UzatmaTarihi = talep.Tarih.AddDays(MBOtipKriter.MBSinavUzatmaSuresiGun).ToString("dd-MM-yyyy");
                                ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "UzatmaTarihi", Value = UzatmaTarihi });
                            }
                        }
                        var mCOntent = SystemMails.GetSystemMailContent(Enstitu.EnstituAd, item.Sablon.SablonHtml, item.Sablon.SablonAdi, ParamereDegerleri);

                        var snded = MailManager.sendMail(Enstitu.EnstituKod, mCOntent.Title, mCOntent.HtmlContent, item.EMails, item.Attachments);
                        if (snded)
                        {
                            var kModel = new GonderilenMailler();
                            kModel.Tarih = DateTime.Now;
                            kModel.EnstituKod = Enstitu.EnstituKod;
                            kModel.MesajID = null;
                            kModel.IslemTarihi = DateTime.Now;
                            kModel.Konu = item.Sablon.SablonAdi + " (" + item.AdSoyad + ")";
                            if (!item.JuriTipAdi.IsNullOrWhiteSpace()) kModel.Konu = kModel.Konu + " (" + item.JuriTipAdi + ")";
                            kModel.IslemYapanID = UserIdentity.Current == null || !UserIdentity.Current.IsAuthenticated ? 1 : UserIdentity.Current.Id;
                            kModel.IslemYapanIP = UserIdentity.Ip;
                            kModel.Aciklama = item.Sablon.Sablon ?? "";
                            kModel.AciklamaHtml = mCOntent.HtmlContent ?? "";
                            kModel.Gonderildi = true;
                            kModel.GonderilenMailKullanicilars = item.EMails.Select(s => new GonderilenMailKullanicilar { Email = s.EMail }).ToList();
                            kModel.GonderilenMailEkleris = gonderilenMEkleris;
                            db.GonderilenMaillers.Add(kModel);
                            db.SaveChanges();
                        }
                    }
                    var message = "'" + talep.Kullanicilar.Ad + " " + talep.Kullanicilar.Soyad + "'  öğrencisinin tez sınav sonucu bilgisi mail olarak gönderildi!";
                    mmMessage.IsSuccess = true;
                    mmMessage.Messages.Add(message);
                    mmMessage.MessageType = Msgtype.Success;

                }
            }
            catch (Exception ex)
            {
                var message = "Tez sınav sonucu bilgisi mail olarak gönderilirken bir hata oluştu! \r\nSRTalepID:" + SRTalepID;
                Management.SistemBilgisiKaydet(message + "\r\n Hata:" + ex.ToExceptionMessage(), "Management/sendMailMezuniyetSinavSonucu \r\n" + ex.ToExceptionStackTrace(), BilgiTipi.Hata);
                mmMessage.Messages.Add(message + "</br> Hata:" + ex.ToExceptionMessage());
                mmMessage.MessageType = Msgtype.Error;
                mmMessage.IsSuccess = false;
            }
            return mmMessage;
        }

        public static MmMessage sendMailMezuniyetTezSablonKontrol(int MezuniyetBasvurulariTezDosyaID, int SablonTipID, string Aciklama = null)
        {
            var mmMessage = new MmMessage();
            try
            {
                using (var db = new LisansustuBasvuruSistemiEntities())
                {
                    var talep = db.MezuniyetBasvurulariTezDosyalaris.Where(p => p.MezuniyetBasvurulariTezDosyaID == MezuniyetBasvurulariTezDosyaID).First();
                    var MB = talep.MezuniyetBasvurulari;
                    var SR = MB.SRTalepleris.Where(p => p.MezuniyetSinavDurumID == MezuniyetSinavDurum.Basarili).First();


                    var Enstitu = MB.MezuniyetSureci.Enstituler;

                    var mModel = new List<SablonMailModel>();

                    var MezuniyetMailModel = new SablonMailModel
                    {
                        AdSoyad = MB.Ad + " " + MB.Soyad,
                        EMails = new List<MailSendList> {
                                        new MailSendList {EMail= MB.Kullanicilar.EMail,
                                                           ToOrBcc = true
                                                         }
                                                    },
                        MailSablonTipID = SablonTipID,
                        Attachments = SablonTipID == MailSablonTipi.Mez_TezKontrolTezDosyasiBasarili ? Management.exportRaporPdf(RaporTipleri.MezuniyetTezKontrolFormu, new List<int?> { MezuniyetBasvurulariTezDosyaID }) : new List<System.Net.Mail.Attachment>()
                    };

                    mModel.Add(MezuniyetMailModel);


                    foreach (var item in mModel)
                    {


                        item.Sablon = db.MailSablonlaris.Where(p => p.MailSablonTipID == item.MailSablonTipID && p.EnstituKod == Enstitu.EnstituKod).First();
                        item.SablonParametreleri = item.Sablon.MailSablonTipleri.Parametreler.Split(',').ToList().Select(s => s.Trim()).ToList();

                        if (item.Sablon.GonderilecekEkEpostalar != null) item.EMails.AddRange(item.Sablon.GonderilecekEkEpostalar.Split(',').Select(s => new MailSendList { EMail = s.Trim(), ToOrBcc = false }));


                        var gonderilenMEkleris = new List<GonderilenMailEkleri>();
                        foreach (var itemSe in item.Sablon.MailSablonlariEkleris)
                        {
                            var ekTamYol = System.Web.HttpContext.Current.Server.MapPath("~" + itemSe.EkDosyaYolu);
                            if (System.IO.File.Exists(ekTamYol))
                            {
                                var FExtension = Path.GetExtension(ekTamYol);
                                item.Attachments.Add(new System.Net.Mail.Attachment(new MemoryStream(System.IO.File.ReadAllBytes(ekTamYol)),
                                    itemSe.EkAdi.ToSetNameFileExtension(FExtension), System.Net.Mime.MediaTypeNames.Application.Octet));
                                gonderilenMEkleris.Add(new GonderilenMailEkleri
                                {
                                    EkAdi = itemSe.EkAdi,
                                    EkDosyaYolu = itemSe.EkDosyaYolu,
                                });
                            }
                            else Management.SistemBilgisiKaydet("Mail gönderilirken eklenen dosya eki sistemde bulunamadı!<br/>Dosya Adı:" + itemSe.EkAdi + " <br/>Dosya Yolu:" + ekTamYol, "Management/sendMailMezuniyetSinavSonucu", BilgiTipi.Uyarı);
                        }

                        var ParamereDegerleri = new List<MailReplaceParameterModel>();

                        if (item.SablonParametreleri.Any(a => a == "@EnstituAdi"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "EnstituAdi", Value = Enstitu.EnstituAd });
                        if (item.SablonParametreleri.Any(a => a == "@WebAdresi"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "WebAdresi", Value = Enstitu.WebAdresi, IsLink = true });

                        if (item.SablonParametreleri.Any(a => a == "@AdSoyad"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "AdSoyad", Value = item.AdSoyad });
                        if (item.SablonParametreleri.Any(a => a == "@SRTarihi"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "SRTarihi", Value = SR.Tarih.ToShortDateString() });
                        if (item.SablonParametreleri.Any(a => a == "@Aciklama"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "Aciklama", Value = Aciklama });
                        var mCOntent = SystemMails.GetSystemMailContent(Enstitu.EnstituAd, item.Sablon.SablonHtml, item.Sablon.SablonAdi, ParamereDegerleri);
                        var snded = MailManager.sendMail(Enstitu.EnstituKod, mCOntent.Title, mCOntent.HtmlContent, item.EMails, item.Attachments);
                        if (snded)
                        {
                            var kModel = new GonderilenMailler();
                            kModel.Tarih = DateTime.Now;
                            kModel.EnstituKod = Enstitu.EnstituKod;
                            kModel.MesajID = null;
                            kModel.IslemTarihi = DateTime.Now;
                            kModel.Konu = item.Sablon.SablonAdi + " (" + item.AdSoyad + ")";
                            if (SablonTipID == MailSablonTipi.Mez_TezKontrolTezDosyasiYuklendi || UserIdentity.Current == null || !UserIdentity.Current.IsAuthenticated) kModel.IslemYapanID = 1;
                            else kModel.IslemYapanID = UserIdentity.Current.Id;
                            kModel.IslemYapanIP = UserIdentity.Ip;
                            kModel.Aciklama = item.Sablon.Sablon ?? "";
                            kModel.AciklamaHtml = mCOntent.HtmlContent ?? "";
                            kModel.Gonderildi = true;
                            kModel.GonderilenMailKullanicilars = item.EMails.Select(s => new GonderilenMailKullanicilar { Email = s.EMail }).ToList();
                            kModel.GonderilenMailEkleris = gonderilenMEkleris;
                            db.GonderilenMaillers.Add(kModel);
                            db.SaveChanges();
                        }
                    }
                    mmMessage.IsSuccess = true;
                    if (SablonTipID == MailSablonTipi.Mez_TezKontrolTezDosyasiBasarili)
                        mmMessage.Messages.Add("'" + MB.Kullanicilar.Ad + " " + MB.Kullanicilar.Soyad + "'  öğrencisine ait tez şablon dosyası kontrolü başarılı olduğu bilgisi mail olarak gönderildi!");
                    else if (SablonTipID == MailSablonTipi.Mez_TezKontrolTezDosyasiOnaylanmadi)
                        mmMessage.Messages.Add("'" + MB.Kullanicilar.Ad + " " + MB.Kullanicilar.Soyad + "'  öğrencisine ait tez şablon dosyası kontrolü onaylanmadığı bilgisi mail olarak gönderildi!");
                    else if (SablonTipID == MailSablonTipi.Mez_TezKontrolTezDosyasiYuklendi)
                        mmMessage.Messages.Add("'" + MB.Kullanicilar.Ad + " " + MB.Kullanicilar.Soyad + "'  öğrencisine ait tez şablon dosyası yüklendi bilgisi mail olarak gönderildi!");
                    mmMessage.MessageType = Msgtype.Success;

                }
            }
            catch (Exception ex)
            {
                var Msg = "";
                if (SablonTipID == MailSablonTipi.Mez_TezKontrolTezDosyasiBasarili)
                    Msg = "Tez şablon dosyası kontrolü başarılı olduğu bilgisi mail olarak gönderilirken bir hata oluştu! \r\nMezuniyetBasvurulariTezDosyaID:" + MezuniyetBasvurulariTezDosyaID;
                else if (SablonTipID == MailSablonTipi.Mez_TezKontrolTezDosyasiOnaylanmadi)
                    Msg = "Tez şablon dosyası kontrolü onaylanmadığı bilgisii mail olarak gönderilirken bir hata oluştu! \r\nMezuniyetBasvurulariTezDosyaID:" + MezuniyetBasvurulariTezDosyaID;
                else if (SablonTipID == MailSablonTipi.Mez_TezKontrolTezDosyasiYuklendi)
                    Msg = "Tez şablon dosyası yüklendi bilgisi mail olarak gönderilirken bir hata oluştu! \r\nMezuniyetBasvurulariTezDosyaID:" + MezuniyetBasvurulariTezDosyaID;

                Management.SistemBilgisiKaydet(Msg + "\r\n Hata:" + ex.ToExceptionMessage(), "Management/sendMailMezuniyetTezSablonKontrol \r\n" + ex.ToExceptionStackTrace(), BilgiTipi.Hata);
                mmMessage.Messages.Add(Msg + "</br> Hata:" + ex.ToExceptionMessage());
                mmMessage.MessageType = Msgtype.Error;
                mmMessage.IsSuccess = false;
            }
            return mmMessage;
        }
        #endregion

        #region Extension



        public static string GetFileName(this string Path)
        {
            return System.IO.Path.GetFileName(Path);
        }
        public static string GetFileExtension(this string Path)
        {
            return System.IO.Path.GetExtension(Path);
        }
        public static string ToSetNameFileExtension(this string FName, string Extension)
        {
            if (FName.ToLower().Contains(Extension.ToLower()) == false) FName += Extension;
            return FName;
        }
        public static string ToFileNameAddGuid(this string FileName, string Extension = null, string addGuid = null)
        {
            FileName = FileName.GetFileName();
            Extension = Extension ?? FileName.GetFileExtension();
            var nGuid = Guid.NewGuid().ToString().Substr(0, 4);
            if (addGuid != null) nGuid = addGuid + "_" + nGuid;
            FileName = FileName.Replace(Extension, "_" + nGuid).ReplaceSpecialCharacter() + Extension;
            FileName = FileName.Replace("+", "_");
            return FileName;
        }

        public static decimal? ToMoney(this string moneyString)
        {
            var groupSeparator = System.Threading.Thread.CurrentThread.CurrentCulture.NumberFormat.CurrencyGroupSeparator;
            var decimalSeparator = System.Threading.Thread.CurrentThread.CurrentCulture.NumberFormat.CurrencyDecimalSeparator;
            return ToMoney(moneyString, decimalSeparator, groupSeparator);
        }
        public static decimal ToMoney(this string moneyString, decimal defaultValue)
        {
            var ms = ToMoney(moneyString);
            return (ms.HasValue ? ms.Value : defaultValue);
        }
        public static decimal? ToMoney(this string moneyString, string decimalSeparator, string groupSeparator)
        {
            char[] numbers = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };
            var moneyStr = string.Join("",
                                          moneyString
                                          .ToCharArray()
                                          .Where(p => (p.ToString() == groupSeparator || p.ToString() == decimalSeparator || numbers.Contains(p))).ToArray()
                                      );
            decimal def = 0;
            if (decimal.TryParse(moneyStr, out def)) return def;
            return null;
        }

        public static bool ToIsValidEmail(this string Email)
        {
            bool IsSuccess = !Regex.IsMatch(Email,
                @"^(?("")(""[^""]+?""@)|(([0-9a-z]((\.(?!\.))|[-!#\$%&'\*\+/=\?\^`\{\}\|~\w])*)(?<=[0-9a-z])@))" +
                @"(?(\[)(\[(\d{1,3}\.){3}\d{1,3}\])|(([0-9a-z][-\w]*[0-9a-z]*\.)+[a-z0-9]{2,24}))$",
                RegexOptions.IgnoreCase);
            if (!IsSuccess) IsSuccess = !Email.IsASCII();
            return IsSuccess;
        }
        public static bool IsASCII(this string value)
        {
            return Encoding.UTF8.GetByteCount(value) == value.Length;
        }
        public static string ToExceptionMessage(this Exception ex)
        {
            int ix = 1;
            Dictionary<int, string> msgs = new Dictionary<int, string>() { { ix, ex.Message } };
            var innException = ex;
            while ((innException = innException.InnerException) != null)
            {
                ix++;
                msgs.Add(ix, innException.Message);
            }
            var returnMsg = string.Join("\r\n", msgs.Select(s => s.Key + "- " + s.Value).ToArray());

            if (ex is DbEntityValidationException)
            {
                var msgsVex = new List<string>();
                var exV = (DbEntityValidationException)ex;
                foreach (var eve in exV.EntityValidationErrors)
                {
                    foreach (var ve in eve.ValidationErrors)
                    {
                        msgsVex.Add(string.Format("State: {0} Property: {1}, Error: {2}", eve.Entry.State, ve.PropertyName, ve.ErrorMessage));
                    }
                }
                if (msgsVex.Any())
                {
                    msgsVex.Insert(0, "Veri Giriş Hataları:");
                    returnMsg += "\r\n" + string.Join("\r\n", msgsVex);
                }
            }

            return returnMsg;
        }
        public static string ToExceptionStackTrace(this Exception ex)
        {
            Dictionary<int, string> stck = new Dictionary<int, string>();

            int ix = 1;
            var innException = ex;
            stck.Add(ix, ex.StackTrace);
            while ((innException = innException.InnerException) != null)
            {
                ix++;
                stck.Add(ix, innException.StackTrace);
            }
            return string.Join("\r\n", stck.Select(s => s.Key + "- " + s.Value).ToArray());
        }


        public static string RenderPartialView(string controllerName, string partialView, object model)
        {
            //try
            //{


            if (HttpContext.Current == null)
                HttpContext.Current = new HttpContext(
                                        new HttpRequest(null, "http://www.lisansustu.yildiz.edu.tr", null),
                                        new HttpResponse(null));
            var context = new HttpContextWrapper(System.Web.HttpContext.Current) as HttpContextBase;
            var routes = new System.Web.Routing.RouteData();
            routes.Values.Add("controller", controllerName);
            var requestContext = new System.Web.Routing.RequestContext(context, routes);
            string requiredString = requestContext.RouteData.GetRequiredString("controller");
            var controllerFactory = ControllerBuilder.Current.GetControllerFactory();
            var controller = controllerFactory.CreateController(requestContext, requiredString) as ControllerBase;
            controller.ControllerContext = new ControllerContext(context, routes, controller);
            var ViewData = new ViewDataDictionary();
            var TempData = new TempDataDictionary();
            ViewData.Model = model;
            using (var sw = new StringWriter())
            {
                var viewResult = ViewEngines.Engines.FindPartialView(controller.ControllerContext, partialView);
                var viewContext = new ViewContext(controller.ControllerContext, viewResult.View, ViewData, TempData, sw);
                viewResult.View.Render(viewContext, sw);
                return sw.GetStringBuilder().ToString();
            }
            //}
            //catch (Exception ex)
            //{
            //    SistemBilgisiKaydet("View Render Edilirken Bir Hata Oluştu!\r\nViewPath:" + controllerName + "/" + partialView + " \r\nhata: " + ex.ToExceptionMessage(), ex.ToExceptionStackTrace(), BilgiTipi.Hata);
            //    return "";
            //}
        }
        public static IHtmlString ToRenderPartialViewHtml(this object model, string controllerName, string partialView)
        {
            var strView = RenderPartialView(controllerName, partialView, model);
            return new HtmlString(strView);
        }
        public static int PageSize = 15;

        public static string RemoveIllegalFileNameChars(this string input, string replacement = "")
        {
            var regexSearch = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
            var r = new Regex(string.Format("[{0}]", Regex.Escape(regexSearch)));
            return r.Replace(input, replacement);
        }
        public static string ReplaceSpecialCharacter(this string gelenStr)
        {
            string regexSearch = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
            Regex r = new Regex(string.Format("[{0}]", Regex.Escape(regexSearch)));
            var fname = r.Replace(gelenStr, "");
            return fname;

        }
        public static bool IsSpecialCharacterCheck(this string gelenStr)
        {
            var regexItem = new Regex("^[a-zA-Z0-9 ]*$");
            return regexItem.IsMatch(gelenStr);
        }

        public static Image CreateQrCode(this string Kod, int Width = 360, int Height = 360)
        {
            var url = string.Format("http://chart.apis.google.com/chart?cht=qr&chs={1}x{2}&chl={0}", Kod, Width, Height);
            WebResponse response = default(WebResponse);
            Stream remoteStream = default(Stream);
            StreamReader readStream = default(StreamReader);
            WebRequest request = WebRequest.Create(url);
            response = request.GetResponse();
            remoteStream = response.GetResponseStream();
            readStream = new StreamReader(remoteStream);
            System.Drawing.Image img = System.Drawing.Image.FromStream(remoteStream);

            response.Close();
            remoteStream.Close();
            readStream.Close();
            return img;
        }

        public static bool IsImage(this string Uzanti)
        {
            var imagesTypes = new List<string>();
            imagesTypes.Add("Png");
            imagesTypes.Add("Jpg");
            imagesTypes.Add("Bmp");
            imagesTypes.Add("Tif");
            imagesTypes.Add("Gif");
            return imagesTypes.Contains(Uzanti);
        }
        public static DateTime TodateToShortDate(this DateTime Tarih)
        {
            var data1 = Tarih.ToDateString().ToDate().Value;
            return data1;
        }
        public static DateTime? TodateToShortDate(this DateTime? Tarih)
        {
            if (Tarih != null) return Tarih.ToDateString().ToDate().Value;
            else return null;
        }

        public static string ToBelirtilmemis(this int? Sayi)
        {
            if (!Sayi.HasValue) return "Belirtilmemiş";
            else return Sayi.Value.ToString();

        }

        public static string ToCinsiyet(this int? Sayi)
        {
            var cins = "";
            if (!Sayi.HasValue) cins = "Belirtilmemiş";
            else if (Sayi == 1) cins = "Erkek";
            else if (Sayi == 2) cins = "Kadın";
            else cins = Sayi.Value.ToString();
            return cins;

        }
        public static string ToEvliBekar(this bool? durum)
        {
            var cins = "";
            if (!durum.HasValue) cins = "Belirtilmemiş";
            else if (durum.Value) cins = "Evli";
            else if (!durum.Value) cins = "Bekar";
            else cins = durum.Value.ToString();
            return cins;
        }
        public static string ToAsilYedek(this bool? durum)
        {
            var cins = "";
            if (!durum.HasValue) cins = "-";
            else if (durum.Value) cins = "Asil";
            else if (!durum.Value) cins = "Yedek";
            else cins = durum.Value.ToString();
            return cins;
        }

        public static string ToFormatDate(this DateTime? datetime)
        {
            if (!datetime.HasValue) return "";
            if (datetime == DateTime.MinValue) return "";
            else return datetime.ToString("dd.MM.yyyy");

        }
        public static string ToFormatDate(this DateTime datetime)
        {
            if (datetime == DateTime.MinValue) return "";
            else return datetime.ToString("dd.MM.yyyy");

        }
        public static string ToFormatDateAndTime(this DateTime? datetime)
        {
            if (!datetime.HasValue) return "";
            if (datetime == DateTime.MinValue) return "";
            else return datetime.ToString("dd.MM.yyyy HH:mm");

        }
        public static string ToFormatDateAndTime(this DateTime datetime)
        {
            if (datetime == DateTime.MinValue) return "";
            else return datetime.ToString("dd.MM.yyyy HH:mm");

        }
        public static string ToFormatTime(this DateTime? datetime)
        {
            if (!datetime.HasValue) return "";
            if (datetime == DateTime.MinValue) return "";
            else return datetime.ToString("HH.mm");

        }
        public static string ToFormatTime(this DateTime datetime)
        {
            if (datetime == DateTime.MinValue) return "";
            else return datetime.ToString("HH.mm");

        }


        public static Image resizeImage(this Image imgToResize, Size size)
        {
            int sourceWidth = imgToResize.Width;
            int sourceHeight = imgToResize.Height;

            float nPercent = 0;
            float nPercentW = 0;
            float nPercentH = 0;

            nPercentW = ((float)sourceWidth / (float)size.Width);
            nPercentH = ((float)sourceHeight / (float)size.Height);

            if (nPercentH > nPercentW)
                nPercent = nPercentH;
            else
                nPercent = nPercentW;

            int destWidth = (int)(sourceWidth / nPercent);
            int destHeight = (int)(sourceHeight / nPercent);

            Bitmap b = new Bitmap(destWidth, destHeight);
            Graphics g = Graphics.FromImage((Image)b);
            g.InterpolationMode = InterpolationMode.Bicubic;
            b.SetResolution(200, 200);
            g.DrawImage(imgToResize, 0, 0, destWidth, destHeight);
            g.Dispose();

            return (Image)b;
        }
        public static string toKullaniciResim(this string ResimAdi)
        {

            var rsm = ResimAdi.IsNullOrWhiteSpace() ? (getRoot() + SistemAyar.KullaniciDefaultResim) : (getRoot() + SistemAyar.KullaniciResimYolu + "/" + ResimAdi);
            return rsm;
        }
        public static SinavSonucAlesXmlModel toSinavSonucAlesXmlModel(this string obj)
        {
            var xml = new XmlDocument();
            xml.LoadXml(obj);
            string jsonString = Newtonsoft.Json.JsonConvert.SerializeXmlNode(xml);
            var jobject = JObject.Parse(jsonString);
            var output = jobject.Children<JProperty>().Select(prop => prop.Value.ToObject<SinavSonucAlesXmlModel>()).FirstOrDefault();

            return output;
        }
        public static double? toSinavSonucAlesMaxNot(this List<int> AlesTips, string xmlstring)
        {
            var sonuclar = xmlstring.toSinavSonucAlesXmlModel();
            var maxNot = new Dictionary<int, double>();
            if (AlesTips.Any(a => a == AlesTipBilgi.Sayısal)) maxNot.Add(AlesTipBilgi.Sayısal, sonuclar.SAY_PUAN.ToDouble().Value.ToString("n2").ToDouble().Value);
            if (AlesTips.Any(a => a == AlesTipBilgi.Sözel)) maxNot.Add(AlesTipBilgi.Sözel, sonuclar.SOZ_PUAN.ToDouble().Value.ToString("n2").ToDouble().Value);
            if (AlesTips.Any(a => a == AlesTipBilgi.EşitAğırlık)) maxNot.Add(AlesTipBilgi.EşitAğırlık, sonuclar.EA_PUAN.ToDouble().Value.ToString("n2").ToDouble().Value);
            return maxNot.Select(s => s.Value).Max();
        }
        public static SinavSonucDilXmlModel toSinavSonucDilXmlModel(this string obj)
        {
            try
            {
                var xml = new XmlDocument();
                xml.LoadXml(obj);
                string jsonString = Newtonsoft.Json.JsonConvert.SerializeXmlNode(xml);
                var jobject = JObject.Parse(jsonString);
                var output = jobject.Children<JProperty>().Select(prop => prop.Value.ToObject<SinavSonucDilXmlModel>()).FirstOrDefault();
                return output;
            }
            catch
            {

                return Newtonsoft.Json.JsonConvert.DeserializeObject<SinavSonucDilXmlModel>(obj);


            }


        }
        public static double? toSinavSonucDil(this string obj, int BasvuruID = 0)
        {
            return obj.toSinavSonucDilXmlModel().PUAN.ToDouble();

        }

        public static JsonResult toJsonResult(this object obj)
        {
            var jsr = new JsonResult();
            jsr.ContentEncoding = System.Text.Encoding.UTF8;
            jsr.ContentType = "application/json";
            jsr.JsonRequestBehavior = JsonRequestBehavior.AllowGet;
            jsr.Data = obj;
            return jsr;
        }

        public static string toJsonText(this object obj)
        {
            return JsonConvert.SerializeObject(obj); ;
        }

        public static string toEmptyStringZero(this object obj)
        {
            string retval = "";
            if (obj != null && obj.ToString() != "0") retval = obj.ToString();
            return retval;
        }
        public static int? toNullIntZero(this object obj)
        {
            int? retval = null;
            if (obj != null && obj.ToString() != "0") retval = obj.ToString().ToInt();
            return retval;
        }
        public static int ToEmptyStringToZero(this object obj)
        {
            int retval = 0;
            if (obj != null && obj.ToString().Trim() != "") retval = obj.ToString().ToInt().Value;
            return retval;
        }
        public static int? toIntObj(this object obj)
        {
            if (obj != null && (obj.IsNumber())) return Convert.ToInt32(obj);
            else return (int?)null;
        }
        public static double? toDoubleObj(this object obj)
        {
            if (obj != null && obj.IsNumber()) return Convert.ToDouble(obj);
            else return (double?)null;
        }
        public static bool? toBooleanObj(this object obj)
        {
            bool dgr;
            if (obj != null && bool.TryParse(obj.ToString(), out dgr)) return Convert.ToBoolean(obj);
            else return (bool?)null;
        }
        public static bool? toIntToBooleanObj(this object obj)
        {
            var IntValue = obj.toIntObj();
            if (obj != null && IntValue.HasValue)
            {

                if (IntValue == 1) return true;
                else if (IntValue == 0) return false;
                else return (bool?)null;
            }
            else return (bool?)null;
        }
        public static decimal? toDecimalObj(this object obj)
        {
            if (obj != null && obj.IsNumber()) return Convert.ToDecimal(obj);
            else return (decimal?)null;
        }
        public static string toStrObj(this object obj)
        {
            if (obj != null) return Convert.ToString(obj);
            else return (string)null;
        }
        public static string toStrObjEmptString(this object obj)
        {
            if (obj != null)
            {
                var Str = Convert.ToString(obj);
                return Str.Trim();
            }
            else return "";
        }
        public static bool IsNumber(this object value)
        {
            double sayi;
            return double.TryParse(value.ToString(), out sayi);
        }
        public static bool IsNumber2(this object value)
        {
            return value is sbyte
                    || value is byte
                    || value is short
                    || value is ushort
                    || value is int
                    || value is uint
                    || value is long
                    || value is ulong
                    || value is float
                    || value is double
                    || value is decimal;
        }
        public static bool IsNumberX(this object value)
        {
            double Deger;
            var durum = double.TryParse(value.toStrObj(), out Deger);
            return durum;
        }
        public static bool IsURL(this string source)
        {
            return Uri.IsWellFormedUriString(source, UriKind.RelativeOrAbsolute);
        }

        public static bool IsValidUrl(this string urlString)
        {
            if (urlString.IsNullOrWhiteSpace()) return false;
            Uri uri;
            return Uri.TryCreate(urlString, UriKind.RelativeOrAbsolute, out uri)
                && (uri.Scheme == Uri.UriSchemeHttp
                 || uri.Scheme == Uri.UriSchemeHttps
                 || uri.Scheme == Uri.UriSchemeFtp
                 || uri.Scheme == Uri.UriSchemeMailto
                 );
        }
        public static EOyilBilgi toEoYilBilgi(this DateTime datetime)
        {

            var mdl = new EOyilBilgi();
            var nowYear = datetime.Year;
            if (datetime.Month >= 2 && datetime.Month <= 8)
            {
                mdl.Donem = 2;
            }
            else
            {
                mdl.BaslangicYili = datetime.Year;
                mdl.BitisYili = datetime.Year + 1;
                mdl.Donem = 1;
            }
            if (datetime.Month <= 8)
            {
                mdl.BaslangicYili = nowYear - 1;
                mdl.BitisYili = nowYear;
            }
            else
            {
                mdl.BaslangicYili = nowYear;
                mdl.BitisYili = nowYear + 1;
            }
            return mdl;
        }
        public static CevrilenNotModel ToNotCevir(this double deger, int Sistem)
        {
            var mdl = new CevrilenNotModel();
            if (Sistem == NotSistemi.Not1LikSistem)
            {  // && CSistem == 100
                mdl.Not1Lik = 1;
                mdl.Not4Luk = 4;
                mdl.Not5Lik = 5;
                mdl.Not100Luk = (30d + (-35d / 2d + (0.5825d + (0.1925d + 0.195833d * (-2d + deger)) * (-3d + deger)) * (-1d + deger)) * (-5d + deger)).ToString("n2").ToDouble().Value;
            }
            else if (Sistem == NotSistemi.Not4LükSistem)
            {
                //&& CSistem == 100
                mdl.Not1Lik = 1;
                mdl.Not4Luk = 4;
                mdl.Not5Lik = 5;
                mdl.Not100Luk = (100d + (70d / 3d + (0.00166667d + 0.00166667d * (-2d + deger)) * (-1d + deger)) * (-4d + deger)).ToString("n2").ToDouble().Value;
            }
            else if (Sistem == NotSistemi.Not5LikSistem)
            {
                mdl.Not1Lik = 1;
                mdl.Not4Luk = 4;
                mdl.Not5Lik = 5;
                mdl.Not100Luk = (100d + (18.6667d + (-0.000952381d + (0.0021645d + 0.00155844d * (-4d + deger)) * (-3d + deger)) * (-1.25d + deger)) * (-5d + deger)).ToString("n2").ToDouble().Value;
            }
            else if (Sistem == NotSistemi.Not100LükSistem)
            {
                mdl.Not1Lik = 1;
                mdl.Not4Luk = 4;
                mdl.Not5Lik = 5;
                mdl.Not100Luk = deger.ToString("n2").ToDouble().Value;
            }
            else if (Sistem == NotSistemi.Not20LikSistem)
            {
                mdl.Not1Lik = 1;
                mdl.Not4Luk = 4;
                mdl.Not5Lik = 5;
                mdl.Not100Luk = ToNotCevir((deger * (0.2)), NotSistemi.Not4LükSistem).Not100Luk.ToString("n2").ToDouble().Value;
            }
            return mdl;

        }
        public static double toGenelBasariNotu(this double MezuniyetNotu100LukSistem, bool MulakatSurecineGirecek, BasvuruSurecOgrenimTipleri BasurecOT, bool IsAlesYerineDosyaNotuIstensin, double? AlesNotu, double? GirisSinavNotu = null)
        {

            var formul = "";

            string retVal = "";
            string reGexF = "";
            string AlesKey = IsAlesYerineDosyaNotuIstensin ? "Dosya" : "Ales";
            if (BasurecOT.OgrenimTipKod == OgrenimTipi.TezsizYuksekLisans)
            {
                if (AlesNotu.HasValue)
                {
                    // MezuniyetNotu100LukSistem + AlesNotu
                    formul = IsAlesYerineDosyaNotuIstensin ? BasurecOT.GBNFormuluD : BasurecOT.GBNFormulu;
                    reGexF = formul.Replace("Agno", MezuniyetNotu100LukSistem.ToString()).Replace(AlesKey, AlesNotu.ToString());
                    retVal = reGexF.Replace(".", ",").EvaluateExpression().ToString("n2");
                }
                else
                {
                    //sadece MezuniyetNotu100LukSistem  
                    reGexF = MezuniyetNotu100LukSistem.ToString();
                    retVal = reGexF.Replace(".", ",").EvaluateExpression().ToString("n2");
                }
            }
            else
            {
                if (AlesNotu.HasValue && GirisSinavNotu.HasValue)
                {
                    // MezuniyetNotu100LukSistem + GirisSinavNotu + AGNO 
                    formul = IsAlesYerineDosyaNotuIstensin ? BasurecOT.GBNFormuluD : BasurecOT.GBNFormulu;
                    reGexF = formul.Replace("Agno", MezuniyetNotu100LukSistem.ToString()).Replace(AlesKey, AlesNotu.ToString()).Replace("Mülakat", GirisSinavNotu.Value.ToString());
                    retVal = reGexF.Replace(".", ",").EvaluateExpression().ToString("n2");
                }
                else if (GirisSinavNotu.HasValue)
                {

                    // MezuniyetNotu100LukSistem + GirisSinavNotu 
                    formul = IsAlesYerineDosyaNotuIstensin ? BasurecOT.GBNFormuluDDosyasiz : BasurecOT.GBNFormuluAlessiz;
                    reGexF = formul.Replace("Agno", MezuniyetNotu100LukSistem.ToString()).Replace("Mülakat", GirisSinavNotu.Value.ToString());
                    retVal = reGexF.Replace(".", ",").EvaluateExpression().ToString("n2");
                }
                else if (AlesNotu.HasValue)
                {
                    // MezuniyetNotu100LukSistem + GirisSinavNotu 
                    formul = IsAlesYerineDosyaNotuIstensin ? BasurecOT.GBNFormuluDMulakatsiz : BasurecOT.GBNFormuluMulakatsiz;
                    reGexF = formul.Replace("Agno", MezuniyetNotu100LukSistem.ToString()).Replace(AlesKey, AlesNotu.Value.ToString());
                    retVal = reGexF.Replace(".", ",").EvaluateExpression().ToString("n2");
                }
                else
                {
                    reGexF = MezuniyetNotu100LukSistem.ToString();
                    retVal = reGexF.Replace(".", ",").EvaluateExpression().ToString("n2");
                }
            }
            return retVal.ToDouble().Value;

        }
        public static string toDeviceType(this string ua)
        {
            string ret = "";
            // Check if user agent is a smart TV - http://goo.gl/FocDk
            if (Regex.IsMatch(ua, @"GoogleTV|SmartTV|Internet.TV|NetCast|NETTV|AppleTV|boxee|Kylo|Roku|DLNADOC|CE\-HTML", RegexOptions.IgnoreCase))
            {
                ret = "Tv";
            }
            // Check if user agent is a TV Based Gaming Console
            else if (Regex.IsMatch(ua, "Xbox|PLAYSTATION.3|Wii", RegexOptions.IgnoreCase))
            {
                ret = "Tv";
            }
            // Check if user agent is a Tablet
            else if ((Regex.IsMatch(ua, "iP(a|ro)d", RegexOptions.IgnoreCase) || (Regex.IsMatch(ua, "tablet", RegexOptions.IgnoreCase)) && (!Regex.IsMatch(ua, "RX-34", RegexOptions.IgnoreCase)) || (Regex.IsMatch(ua, "FOLIO", RegexOptions.IgnoreCase))))
            {
                ret = "Tablet";
            }
            // Check if user agent is an Android Tablet
            else if ((Regex.IsMatch(ua, "Linux", RegexOptions.IgnoreCase)) && (Regex.IsMatch(ua, "Android", RegexOptions.IgnoreCase)) && (!Regex.IsMatch(ua, "Fennec|mobi|HTC.Magic|HTCX06HT|Nexus.One|SC-02B|fone.945", RegexOptions.IgnoreCase)))
            {
                ret = "Tablet";
            }
            // Check if user agent is a Kindle or Kindle Fire
            else if ((Regex.IsMatch(ua, "Kindle", RegexOptions.IgnoreCase)) || (Regex.IsMatch(ua, "Mac.OS", RegexOptions.IgnoreCase)) && (Regex.IsMatch(ua, "Silk", RegexOptions.IgnoreCase)))
            {
                ret = "Tablet";
            }
            // Check if user agent is a pre Android 3.0 Tablet
            else if ((Regex.IsMatch(ua, @"GT-P10|SC-01C|SHW-M180S|SGH-T849|SCH-I800|SHW-M180L|SPH-P100|SGH-I987|zt180|HTC(.Flyer|\\_Flyer)|Sprint.ATP51|ViewPad7|pandigital(sprnova|nova)|Ideos.S7|Dell.Streak.7|Advent.Vega|A101IT|A70BHT|MID7015|Next2|nook", RegexOptions.IgnoreCase)) || (Regex.IsMatch(ua, "MB511", RegexOptions.IgnoreCase)) && (Regex.IsMatch(ua, "RUTEM", RegexOptions.IgnoreCase)))
            {
                ret = "Tablet";
            }
            // Check if user agent is unique Mobile User Agent
            else if ((Regex.IsMatch(ua, "BOLT|Fennec|Iris|Maemo|Minimo|Mobi|mowser|NetFront|Novarra|Prism|RX-34|Skyfire|Tear|XV6875|XV6975|Google.Wireless.Transcoder", RegexOptions.IgnoreCase)))
            {
                ret = "Mobile";
            }
            // Check if user agent is an odd Opera User Agent - http://goo.gl/nK90K
            else if ((Regex.IsMatch(ua, "Opera", RegexOptions.IgnoreCase)) && (Regex.IsMatch(ua, "Windows.NT.5", RegexOptions.IgnoreCase)) && (Regex.IsMatch(ua, @"HTC|Xda|Mini|Vario|SAMSUNG\-GT\-i8000|SAMSUNG\-SGH\-i9", RegexOptions.IgnoreCase)))
            {
                ret = "Mobile";
            }
            // Check if user agent is Windows Desktop
            else if ((Regex.IsMatch(ua, "Windows.(NT|XP|ME|9)")) && (!Regex.IsMatch(ua, "Phone", RegexOptions.IgnoreCase)) || (Regex.IsMatch(ua, "Win(9|.9|NT)", RegexOptions.IgnoreCase)))
            {
                ret = "Desktop";
            }
            // Check if agent is Mac Desktop
            else if ((Regex.IsMatch(ua, "Macintosh|PowerPC", RegexOptions.IgnoreCase)) && (!Regex.IsMatch(ua, "Silk", RegexOptions.IgnoreCase)))
            {
                ret = "Desktop";
            }
            // Check if user agent is a Linux Desktop
            else if ((Regex.IsMatch(ua, "Linux", RegexOptions.IgnoreCase)) && (Regex.IsMatch(ua, "X11", RegexOptions.IgnoreCase)))
            {
                ret = "Desktop";
            }
            // Check if user agent is a Solaris, SunOS, BSD Desktop
            else if ((Regex.IsMatch(ua, "Solaris|SunOS|BSD", RegexOptions.IgnoreCase)))
            {
                ret = "Desktop";
            }
            // Check if user agent is a Desktop BOT/Crawler/Spider
            else if ((Regex.IsMatch(ua, "Bot|Crawler|Spider|Yahoo|ia_archiver|Covario-IDS|findlinks|DataparkSearch|larbin|Mediapartners-Google|NG-Search|Snappy|Teoma|Jeeves|TinEye", RegexOptions.IgnoreCase)) && (!Regex.IsMatch(ua, "Mobile", RegexOptions.IgnoreCase)))
            {
                ret = "Desktop";
            }
            // Otherwise assume it is a Mobile Device
            else
            {
                ret = "Mobile";
            }
            return ret;
        }

        public static string toTIDegerlendirmeSonucu(bool? IsOyBirligiOrCouklugu, bool? IsBasariliOrBasarisiz)
        {
            string ReturnSonuc = "";

            if (IsOyBirligiOrCouklugu.HasValue && IsBasariliOrBasarisiz.HasValue)
            {
                ReturnSonuc += IsOyBirligiOrCouklugu.Value ? "Oy Birliği ile" : "Oy Çokluğu ile";
                ReturnSonuc += IsBasariliOrBasarisiz.Value ? " Başarılı" : " Başarısız";

            }
            return ReturnSonuc;
        }
        public static UrlInfoModel toUrlInfo(this Uri uri)
        {
            var model = new UrlInfoModel();
            model.Root = LisansUstuBasvuruSistemi.Models.Management.getRoot();
            var webSite = uri.AbsoluteUri.Replace(uri.AbsolutePath, "");

            webSite = webSite.IndexOf("?") > -1 ? webSite.Substring(0, webSite.IndexOf("?")) : webSite;
            webSite = webSite.EndsWith("/") ? webSite : webSite + "/";
            var apath = uri.AbsolutePath.IndexOf("?") > -1 ? uri.AbsolutePath.Substring(0, uri.AbsolutePath.IndexOf("?")) : uri.AbsolutePath;
            var spl = apath.Split('/').Where(p => p != "").Select((item, inx) => new { item, inx }).ToList();
            string selectedEnstKisAd = (spl.Count == 0 ? "FBE" : (spl.First().item.IsContainsEnstitu() ? spl.First().item : "FBE")).ToLower();

            model.Query = uri.Query;
            model.EnstituKisaAd = selectedEnstKisAd;
            var enst = (selectedEnstKisAd + "/").ToLower();
            var tspl = new List<string>();
            model.FakeRoot = model.Root + enst;
            model.DefaultUri = webSite + enst;
            var lstNoEqLnq = new List<string>() { selectedEnstKisAd };
            var laspath = string.Join("/", spl.Where(p => !p.item.IsContainsEnstitu()).Select(s => s.item));
            foreach (var item in spl.Where(p => !lstNoEqLnq.Contains(p.item)).Select(s => s.item))
            {
                tspl.Add(item);
            }
            if (tspl.Count > 0)
            {

                apath = model.Root + enst + tspl[0] + "/Index";

            }
            else
            {
                apath = model.Root + enst + "home/index";
            }
            model.LastPath = laspath;
            apath = apath.IndexOf("I") > -1 ? apath.Replace("I", "i").ToLower() : apath.ToLower();
            model.AbsolutePath = apath;

            return model;
        }

        public static List<CmbIntDto> CmbCardBonusType()
        {
            var mdl = new List<CmbIntDto>();
            mdl.Add(new CmbIntDto { Value = null, Caption = "" });
            mdl.Add(new CmbIntDto { Value = 1, Caption = "Bonus Kart Özelliği Var" });
            mdl.Add(new CmbIntDto { Value = 0, Caption = "Bonus Kart Özelliği Yok" });
            return mdl;
        }
        public static List<CmbIntDto> CmbCardMaximumType()
        {
            var mdl = new List<CmbIntDto>();
            mdl.Add(new CmbIntDto { Value = null, Caption = "" });
            mdl.Add(new CmbIntDto { Value = 1, Caption = "Var" });
            mdl.Add(new CmbIntDto { Value = 0, Caption = "Yok" });
            return mdl;
        }
        public static List<CmbIntDto> CmbTaksitList()
        {
            var mdl = new List<CmbIntDto>();
            mdl.Add(new CmbIntDto { Value = null, Caption = "Taksit İstemiyorum" });
            mdl.Add(new CmbIntDto { Value = 5, Caption = "5 Taksit" });
            return mdl;
        }


        public static string DekontOdemeIsle(string SiparisNo, DateTime DekontTarih, string Ucret, string taksit, string CardNo)
        {
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var msg = "";
                var DekontBilgi = db.BasvurularTercihleriKayitOdemeleris.Where(p => p.DekontNo == SiparisNo).FirstOrDefault();
                if (DekontBilgi != null)
                {

                    if (!DekontBilgi.IsOdendi)
                    {
                        var kul = DekontBilgi.BasvurularTercihleri.Basvurular.Kullanicilar;
                        var tercihPrg = DekontBilgi.BasvurularTercihleri.Programlar;
                        DekontBilgi.Ucret = Ucret.ToDouble().Value;
                        DekontBilgi.DekontTarih = DekontTarih;
                        DekontBilgi.IsOdendi = true;
                        DekontBilgi.IslemYapanIP = UserIdentity.Ip;
                        DekontBilgi.IslemTarihi = DateTime.Now;
                        if (UserIdentity.Current != null && UserIdentity.Current.IsAuthenticated)
                        {
                            DekontBilgi.IslemYapanID = UserIdentity.Current.Id;


                        }
                        db.SaveChanges();

                        if (DekontBilgi.DonemNo > 1 && DekontBilgi.BasvurularTercihleri.MulakatSonuclaris.Any(a => a.KayitDurumID.HasValue && a.KayitDurumlari.IsKayitOldu == true))
                        {
                            string msj = "";
                            var IsSuccess = SetPaymentInfoGsis(kul.OgrenciNo, Ucret, SiparisNo, DekontTarih, out msj);

                            if (!IsSuccess)
                            {
                                Management.SistemBilgisiKaydet("Sanal pos ödeme sonrası dekont bilgisi işlenemedi! (" + kul.Ad + " " + kul.Soyad + ") " + msj, "Management/DekontOdemeIsle", BilgiTipi.Kritik);
                            }

                        }


                        #region SendMail
                        var Enstituler = DekontBilgi.BasvurularTercihleri.Basvurular.BasvuruSurec.Enstituler;
                        string EnstituKod = Enstituler.EnstituKod;
                        var Sablon = db.MailSablonlaris.Where(p => p.MailSablonTipleri.SistemMaili && p.MailSablonTipID == MailSablonTipi.LisansustuSanalPosOdemeBilgisi).First();
                        var mailBilgi = EnstituMailInfo.GetEnstituMailBilgisi(EnstituKod);
                        var mmmC = new mdlMailMainContent();

                        mmmC.EnstituAdi = db.Enstitulers.Where(p => p.EnstituKod == EnstituKod).First().EnstituAd;
                        var _ea = mailBilgi.SistemErisimAdresi;
                        var WurlAddr = _ea.Split('/').ToList();
                        if (_ea.Contains("//"))
                            _ea = WurlAddr[0] + "//" + WurlAddr.Skip(2).Take(1).First();
                        else
                            _ea = "http://" + WurlAddr.First();
                        mmmC.LogoPath = _ea + "/Content/assets/images/ytu_logo_tr.png";
                        mmmC.UniversiteAdi = "Yıldız Tekni Üniversitesi";

                        var contentHtml = Sablon.SablonHtml;
                        #region replaces 
                        var OdemeIslemAdi = "";

                        if (DekontBilgi.BasvurularTercihleri.IsOgrenimUcretiOrKatkiPayi.Value)
                        {
                            OdemeIslemAdi = DekontBilgi.DonemNo + ". Dönem Öğrenim Ücreti Ödemesi";
                        }
                        else
                        {

                            OdemeIslemAdi = "Katkı Payı Ücreti Ödemesi";
                        }
                        var Bsurec = DekontBilgi.BasvurularTercihleri.Basvurular.BasvuruSurec;
                        var KayitDonemi = Bsurec.BaslangicYil + "/" + Bsurec.BitisYil + " " + Bsurec.Donemler.DonemAdi;
                        var TaksitBilgisi = "";
                        if (taksit.IsNullOrWhiteSpace())
                        {
                            TaksitBilgisi = "Taksit Yok";
                        }
                        else
                        {
                            TaksitBilgisi = taksit + " Taksit";
                        }
                        contentHtml = contentHtml.Replace("@AdSoyad", kul.Ad + " " + kul.Soyad);
                        contentHtml = contentHtml.Replace("@OdemeIslemAdi", OdemeIslemAdi);
                        contentHtml = contentHtml.Replace("@KayitDonemi", KayitDonemi);
                        contentHtml = contentHtml.Replace("@ProgramAdi", tercihPrg.ProgramAdi);
                        contentHtml = contentHtml.Replace("@KartNo", CardNo);
                        contentHtml = contentHtml.Replace("@DekontNo", SiparisNo);
                        contentHtml = contentHtml.Replace("@DekontTarihi", DekontBilgi.DekontTarih.ToString("dd.MM.yyyy"));
                        contentHtml = contentHtml.Replace("@KartNo", CardNo);
                        contentHtml = contentHtml.Replace("@Ucret", Ucret);
                        contentHtml = contentHtml.Replace("@Taksit", TaksitBilgisi);
                        contentHtml = contentHtml.Replace("@EnstituAdi", mmmC.EnstituAdi);
                        var webadresLink = "<a href='" + mailBilgi.WebAdresi + "' target='_blank'>" + mailBilgi.WebAdresi + "</a>";
                        contentHtml = contentHtml.Replace("@WebAdresi", webadresLink);
                        mmmC.Content = contentHtml;
                        #endregion
                        msg = contentHtml;
                        // msg = "Sayın " + kul.Ad + " " + kul.Soyad + " <br/>Lisansüstü başvuru sistemi üzerinden <b>" + CardNo + "</b> numaralı kartınızla <b>" + DekontBilgi.DekontTarih.ToString("dd.MM.yyyy") + "</b> tarihinde yapmış olduğunuz <b>" + tercihPrg.ProgramAdi + "</b> Program kaydı için gereken <b>" + Ucret + " TL</b> Ücretli Sanal Pos ödeme işleminiz <b>" + (taksit.IsNullOrWhiteSpace() ? "Taksitsiz" : taksit + " Taksit") + "</b> olarak <b>" + SiparisNo + "</b> Sipariş Numarası ile başarılı bir şekilde gerçekleşmiştir.";

                        var EMailList = new List<MailSendList> { new MailSendList { EMail = kul.EMail, ToOrBcc = true } };
                        mmmC.Content = contentHtml;
                        string htmlMail = Management.RenderPartialView("Ajax", "getMailContent", mmmC);
                        var Attachments = new List<System.Net.Mail.Attachment>();
                        if (Sablon.GonderilecekEkEpostalar.IsNullOrWhiteSpace() == false)
                        {
                            EMailList.AddRange(Sablon.GonderilecekEkEpostalar.Split(',').Select(s => new MailSendList { EMail = s.Trim(), ToOrBcc = false }));
                        }

                        var gonderilenMEkleris = new List<GonderilenMailEkleri>();
                        foreach (var itemSe in Sablon.MailSablonlariEkleris)
                        {
                            var ekTamYol = System.Web.HttpContext.Current.Server.MapPath("~" + itemSe.EkDosyaYolu);
                            if (System.IO.File.Exists(ekTamYol))
                            {
                                var FExtension = Path.GetExtension(ekTamYol);
                                Attachments.Add(new System.Net.Mail.Attachment(new MemoryStream(System.IO.File.ReadAllBytes(ekTamYol)),
                                    itemSe.EkAdi.ToSetNameFileExtension(FExtension), System.Net.Mime.MediaTypeNames.Application.Octet));
                                gonderilenMEkleris.Add(new GonderilenMailEkleri
                                {
                                    EkAdi = itemSe.EkAdi,
                                    EkDosyaYolu = itemSe.EkDosyaYolu,
                                });
                            }
                            else Management.SistemBilgisiKaydet("Mail gönderilirken eklenen dosya eki sistemde bulunamadı!<br/>Dosya Adı:" + itemSe.EkAdi + " <br/>Dosya Yolu:" + ekTamYol, "Management/sendMailMezuniyetSinavYerBilgisi", BilgiTipi.Uyarı);
                        }
                        var sndMail = MailManager.sendMail(EnstituKod, Sablon.SablonAdi, htmlMail, EMailList, Attachments);

                        if (sndMail)
                        {

                            var mailList = new List<GonderilenMailKullanicilar>();
                            foreach (var item in EMailList)
                            {
                                mailList.Add(new GonderilenMailKullanicilar
                                {
                                    Email = item.EMail,
                                });
                            }

                            var kModel = new GonderilenMailler();
                            kModel.EnstituKod = EnstituKod;
                            kModel.MesajID = null;
                            kModel.Tarih = DateTime.Now;
                            kModel.IslemTarihi = DateTime.Now;
                            kModel.Gonderildi = true;
                            kModel.Konu = Enstituler.EnstituKisaAd + " SANAL POS ÖDEME İŞLEMİ (" + kul.Ad + " " + kul.Soyad + ")";
                            kModel.IslemYapanID = UserIdentity.Current == null || !UserIdentity.Current.IsAuthenticated ? 1 : UserIdentity.Current.Id;
                            kModel.IslemYapanIP = UserIdentity.Ip;
                            kModel.Aciklama = "";
                            kModel.AciklamaHtml = htmlMail ?? "";
                            kModel.GonderilenMailKullanicilars = mailList;
                            kModel.GonderilenMailEkleris = gonderilenMEkleris;
                            db.GonderilenMaillers.Add(kModel);
                            db.SaveChanges();
                        }
                    }
                    #endregion
                }
                else
                {
                    msg = "Ödeme İşleminden Sonra Sipariş Numarası Hiçbir Program Tercihi İle Eşleşmedi!";
                    Management.SistemBilgisiKaydet("Ödeme İşleminden Sonra Sipariş Numarası Hiçbir Program Tercihi İle Eşleşmedi! <br/>Sipariş No: " + SiparisNo, "Management/DekontOdemeIsle", BilgiTipi.Kritik);
                }
                return msg;
            }

        }
        public static string DekontNoUret(int KayitYilBas, int DonemNo, int OdemeDonemNo, string ProgramKod, string EnstituKod)
        {
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var EnstituKodB = EnstituKod == EnstituKodlari.FenBilimleri ? "F" : "S";
                string DekontNo = DekontNoBirlestir(EnstituKodB, KayitYilBas, DonemNo, OdemeDonemNo, ProgramKod);

                while (db.BasvurularTercihleriKayitOdemeleris.Where(p => p.DekontNo != null && p.DekontNo != "" && p.DekontNo == DekontNo).Any())
                {
                    DekontNo = DekontNoBirlestir(EnstituKodB, KayitYilBas, DonemNo, OdemeDonemNo, ProgramKod);
                }
                return DekontNo;
            }
        }
        public static string DekontNoBirlestir(string EnstituKodB, int KayitYilBas, int DonemNo, int OdemeDonemNo, string ProgramKod)
        {
            var rndID = Guid.NewGuid().ToString().ToUpper().Substring(0, 6);
            return EnstituKodB + "-" + KayitYilBas + "/" + DonemNo + "-" + OdemeDonemNo + '-' + ProgramKod + "-" + rndID;

        }

        public static string ToMezuniyetJuriUnvanAdi(this string UnvanAdi)
        {
            UnvanAdi = UnvanAdi.Trim().ToLower().Replace("  ", ".").Replace(". ", ".").Replace(" .", ".").Replace(" ", ".");
            var ProfUnvan = new List<string> { "PROFESÖR".ToLower(), "PROFESÖR.DR".ToLower(), "PROF.DR.".ToLower(), "Prof.".ToLower() };
            var DocUnvan = new List<string> { "DOÇENT".ToLower(), "DOÇENT.DR".ToLower(), "Doç.".ToLower() };
            var OgUyeUnvan = new List<string> { "DR.ÖĞR.ÜYE".ToLower(), "DR.ÖĞR.ÜYESİ".ToLower(), "DR.ÖĞRETİM.ÜYE".ToLower(), "DR.ÖĞRETİM.ÜYESİ".ToLower() };
            if (ProfUnvan.Any(a => a.Contains(UnvanAdi))) return "PROF.DR.";
            else if (DocUnvan.Any(a => a.Contains(UnvanAdi))) return "DOÇ.DR.";
            else if (OgUyeUnvan.Any(a => a.Contains(UnvanAdi))) return "DR.ÖĞR.ÜYE.";
            else return UnvanAdi.ToUpper();
        }

        public static TarihAralikModel ToAraRaporDonemBilgi(this DateTime date)
        {
            var model = new TarihAralikModel();
            if (date.Month < 6)
            {
                model.BaslangicYil = date.Year - 1;
                model.DonemID = 2;
                model.DonemAdi = "Bahar";
                model.DonemAdiEn = "Spring";
                model.BaslangicTarihi = new DateTime(date.Year, 1, 1);
                model.BitisTarihi = new DateTime(date.Year, 6, 1).AddDays(-1);
            }
            else
            {
                model.BaslangicYil = date.Year;
                model.BaslangicTarihi = new DateTime(date.Year, 6, 1);
                model.BitisTarihi = new DateTime(date.Year + 1, 1, 1).AddDays(-1);
                model.DonemID = 1;
                model.DonemAdi = "Güz";
                model.DonemAdiEn = "Fall";

            }

            return model;
        }


        public static IHtmlString ToMezuniyetDurum(this frMezuniyetBasvurulari model)
        {
            var PagerString = model.ToRenderPartialViewHtml("Mezuniyet", "BasvuruDurumView");/// MHelper.RenderPartialView("Ajax", "Renrerpagination", model);
            return PagerString;
        }
        public static IHtmlString ToMezuniyetDetayBasvuru(this basvuruDetayModelMezuniyet model)
        {
            var PagerString = model.ToRenderPartialViewHtml("Ajax", "getDetailMezuniyet_t1_Basvuru");/// MHelper.RenderPartialView("Ajax", "Renrerpagination", model);
            return PagerString;
        }
        public static IHtmlString ToMezuniyetDetayEYKSureci(this basvuruDetayModelMezuniyet model)
        {
            var PagerString = model.ToRenderPartialViewHtml("Ajax", "getDetailMezuniyet_t2_EYKSureci");/// MHelper.RenderPartialView("Ajax", "Renrerpagination", model);
            return PagerString;
        }
        public static IHtmlString ToMezuniyetDetaySinavSureci(this basvuruDetayModelMezuniyet model)
        {
            var PagerString = model.ToRenderPartialViewHtml("Ajax", "getDetailMezuniyet_t3_SinavSureci");/// MHelper.RenderPartialView("Ajax", "Renrerpagination", model);
            return PagerString;
        }
        public static IHtmlString ToMezuniyetDetayTezKontrolSureci(this basvuruDetayModelMezuniyet model)
        {
            var PagerString = model.ToRenderPartialViewHtml("Ajax", "getDetailMezuniyet_t4_TezKontrolSureci");/// MHelper.RenderPartialView("Ajax", "Renrerpagination", model);
            return PagerString;
        }
        public static IHtmlString ToMezuniyetDetayMezuniyetSureci(this basvuruDetayModelMezuniyet model)
        {
            var PagerString = model.ToRenderPartialViewHtml("Ajax", "getDetailMezuniyet_t5_MezuniyetSureci");/// MHelper.RenderPartialView("Ajax", "Renrerpagination", model);
            return PagerString;
        }
        #endregion

    }

}