
using BiskaUtil;
using LisansUstuBasvuruSistemi.Business;
using LisansUstuBasvuruSistemi.Models.ObsService;
using LisansUstuBasvuruSistemi.Raporlar;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.Helpers;
using LisansUstuBasvuruSistemi.Utilities.Logs;
using LisansUstuBasvuruSistemi.Utilities.MenuAndRoles;
using LisansUstuBasvuruSistemi.Utilities.SystemSetting;
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
                    Management.SistemBilgisiKaydet("Gsis Program Ücreti Sorgulanırken Servis Hata Döndürdü! (" + mdl.AdSoyad + ")", "Management/GetGsisProgramUcretKontrol/getProgramFee ProgramKod:" + mdl.ProgramKod + " OgrenimTipKod:" + mdl.OgrenimTipKod + " Dönen Hata Kodu:" + data.returnInfo, LogType.Kritik);

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
                    Management.SistemBilgisiKaydet("Gsis Öğrenci Borcuna Ait Ödeme Başlangıç Tarihi Sorgulanırken Servis Hata Döndürdü!", "Management/GetGsisUcretOdemeTarihKontrol/getFirstPaymentDate Dönen Hata Kodu:" + OdemeBasTarih, LogType.Kritik);
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
                    Management.SistemBilgisiKaydet("Gsis Öğrenci Borcuna Ait Ödeme Bitiş Tarihi Sorgulanırken Servis Hata Döndürdü!", "Management/GetGsisUcretOdemeTarihKontrol/getLastPaymentDate Dönen Hata Kodu:" + OdemeBitTarih, LogType.Kritik);
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

                        Management.SistemBilgisiKaydet("Gsis Öğrenci Borcuna Ait Ödeme Başlangıç Tarihi Sorgulanırken Servis Hata Döndürdü! (" + mdl.AdSoyad + " [" + mdl.OgrenciNo + "])", "Management/getFirstPaymentDate/paymentControl Dönen Hata Kodu:" + OdemeBasTarih, LogType.Kritik);
                    }
                    else
                    {
                        mdl.OdemeBaslangicTarihi = OdemeBasTarih != "" ? Convert.ToDateTime(OdemeBasTarih) : (DateTime?)null;
                    }
                    if (OdemeBitTarih == "-1" || OdemeBitTarih == "-2")
                    {
                        Management.SistemBilgisiKaydet("Gsis Öğrenci Borcuna Ait Ödeme Bitiş Tarihi Sorgulanırken Servis Hata Döndürdü! (" + mdl.AdSoyad + " [" + mdl.OgrenciNo + "])", "Management/GetOnlineOdemeProgramDetay/getLastPaymentDate Dönen Hata Kodu:" + OdemeBitTarih, LogType.Kritik);
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
                    Management.SistemBilgisiKaydet("Gsis Öğrenci Borcu Sorgulanırken Servis Hata Döndürdü! (" + mdl.AdSoyad + " [" + mdl.OgrenciNo + "])", "Management/GetOnlineOdemeProgramDetay/paymentControl Dönen Hata Kodu:" + RetVal, LogType.Kritik);
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
 


        #endregion

        #region YokData
        public static YokStudentControl yokStudentControl(long TcKimlikNo)
        {
            var model = new YokStudentControl();
            try
            {
                var KullaniciAdi = SistemAyar.GetAyar(SistemAyar.AyarYokwsKullaniciAdi);
                var Sifre = SistemAyar.GetAyar(SistemAyar.AyarYokwsKullaniciSifre);
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
                    SistemBilgisiKaydet(model.Mesaj, "Management/yokStudentControl", LogType.Kritik);

                }
            }
            catch (Exception ex)
            {
                model.KayitVar = false;
                model.Hata = true;
                model.Mesaj = "YÖK Servisinden Öğrenci Bilgisi kontrol edilirken bir hata oluştu.Hata:" + ex.ToExceptionMessage();
                SistemBilgisiKaydet(model.Mesaj, "Management/yokStudentControl \r\n" + ex.ToExceptionStackTrace(), LogType.Kritik);
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

         public static List<CmbIntDto> getbasvuruSurecleriDekont(string EKD, bool bosSecimVar = false)
        {
            var lst = new List<CmbIntDto>();
            string EnstituKod = EnstituBus.GetSelectedEnstitu(EKD);
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
            var rtatilDurum = BelgeTalepAyar.BelgeTalebiResmiTatilDurum.GetAyarBt(_EnstituKod, "0").ToBoolean().Value;
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


                    if (tTip.SRTalepTipleriAktifAylars.Any(a => a.AyID == nTarih.Month) == false && RoleNames.SrGelenTalepler.InRoleCurrent() == false)
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
            var userPkod = UserBus.GetUserProgramKods(UserIdentity.Current.Id, EnstituKod);
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
                var kulProgId = UserBus.GetUserProgramKods(KullaniciID, EnstituKod);
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
                var kulProgId = UserBus.GetUserProgramKods(KullaniciID, bsurec.EnstituKod);
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
                var kulProgId = UserBus.GetUserProgramKods(KullaniciID, bsurec.EnstituKod);
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
                        var KullaniciAdi = SistemAyar.GetAyar(SistemAyar.AyarOsymWsKullaniciAdi);
                        var Sifre = SistemAyar.GetAyar(SistemAyar.AyarOsymWsKullaniciSifre);
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
                                                Management.SistemBilgisiKaydet("Web servisinden çekilen sınav bilgisine ait sayısal bir sonuca rastlanmadı!\r\n" + bilgi + " \r\n XmlData: " + mdl.WsXmlData, "Management/getSinavTipSonucModel", LogType.Bilgi, UserIdentity.Current.Id, UserIdentity.Ip);
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
                                                Management.SistemBilgisiKaydet("Web servisinden çekilen sınav bilgisine ait sayısal bir sonuca rastlanmadı!\r\n" + bilgi + " \r\n XmlData: " + mdl.WsXmlData, "Management/getSinavTipSonucModel", LogType.Bilgi, UserIdentity.Current.Id, UserIdentity.Ip);
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
                                                Management.SistemBilgisiKaydet("Web servisinden çekilen sınav bilgisine ait sayısal bir sonuca rastlanmadı!\r\n" + bilgi + " \r\n XmlData: " + mdl.WsXmlData, "Management/getSinavTipSonucModel", LogType.Bilgi, UserIdentity.Current.Id, UserIdentity.Ip);
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
                                    Management.SistemBilgisiKaydet("Web servisinden  sınav bilgisi çekilemedi! \r\nDetay: " + bilgi, "Management/getSinavTipSonucModel", LogType.Kritik, UserIdentity.Current.Id, UserIdentity.Ip);

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
                                        Management.SistemBilgisiKaydet("Web servisinden çekilen sınav bilgisine ait sayısal bir sonuca rastlanmadı!\r\n" + bilgi + " \r\n XmlData: " + mdl.WsXmlData, "Management/getSinavTipSonucModel", LogType.Bilgi, UserIdentity.Current.Id, UserIdentity.Ip);
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
                Management.SistemBilgisiKaydet("Web servisinden sınav bilgisi çekilirken bir hata oluştu!\r\n" + bilgi + " \r\n Hata: " + ex.ToExceptionMessage(), ex.ToExceptionStackTrace(), LogType.Kritik, UserIdentity.Current.Id, UserIdentity.Ip);
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

                            Management.SistemBilgisiKaydet("BasvuruSurecID:" + BasvuruSurecID + "\n SinavTipID:" + SinavTipID + "\n Yil:" + _yil + "\n Bilgisi sistemde bulunamadı! Konsoldan müdahale olabilir!", "Basvuru/getSinavTipSonuc", LogType.Saldırı);
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
                bool RotasYonDegisimLog = SistemAyar.GetAyar(SistemAyar.RotasyonuDegisenResimleriLogla).ToBoolean().Value;
                bool Boyutlandirma = SistemAyar.GetAyar(SistemAyar.KullaniciResimKaydiBoyutlandirma).ToBoolean().Value;
                bool KaliteOpt = SistemAyar.GetAyar(SistemAyar.KullaniciResimKaydiKaliteOpt).ToBoolean().Value;
                string ResimAdi = Resim.FileName.ToFileNameAddGuid();
                var ResimYolu = Path.Combine(System.Web.HttpContext.Current.Server.MapPath("~/" + folderName), ResimAdi);


                if (Boyutlandirma)
                {
                    try
                    {
                        var uzn = SistemAyar.GetAyar(SistemAyar.KullaniciResimKaydiHeightPx);
                        var gens = SistemAyar.GetAyar(SistemAyar.KullaniciResimKaydiWidthPx);

                        int Uzunluk = uzn.IsNullOrWhiteSpace() ? 560 : uzn.ToInt().Value;
                        int Genislik = gens.IsNullOrWhiteSpace() ? 560 : gens.ToInt().Value;
                        var img = bmp.resizeImage(new Size(Genislik, Uzunluk));
                        img.Save(ResimYolu, ImageFormat.Jpeg);
                    }
                    catch (Exception ex)
                    {
                        Management.SistemBilgisiKaydet(ex, "Resmin boyutlandırma işlemi yapılıp kayıt edilirken bir hata oluştu.\r\n Hata:" + ex.ToExceptionMessage(), LogType.OnemsizHata);
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

                        ImageCodecInfo jpgEncoder = FileExtension.GetImageCodecInfo(ImageFormat.Jpeg);


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
                        Management.SistemBilgisiKaydet(errQuality, "Resmin kalitesi değiştirilirken hata oluştu.\r\n Hata:" + errQuality.ToExceptionMessage(), LogType.OnemsizHata);
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
                            Management.SistemBilgisiKaydet("Rotasyon farklılığı görünen resim düzeltildi! Resim:" + ResimYolu, "Management/resimKaydet", LogType.Bilgi);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Management.SistemBilgisiKaydet(ex, "Hesap kayıt sırasında resim rotasyonu yapılırken bir hata oluştu.\r\n Hata:" + ex.ToExceptionMessage(), LogType.OnemsizHata);
                }
                #endregion


                return ResimAdi;
            }
            catch (Exception ex)
            {
                Management.SistemBilgisiKaydet("Resim kaydedilirken bir hata oluştu! Hata: " + ex.ToExceptionMessage(), ex.ToExceptionStackTrace(), LogType.Hata, null, UserIdentity.Ip);
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
        public static int? getAktifMulakatSurecID(string EnstituKod, int BasvuruSurecTipID, int? BasvuruSurecID = null, bool? IsMulakatDurum = null)
        {
            using (var db = new LisansustuBasvuruSistemiEntities())
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
            using (var db = new LisansustuBasvuruSistemiEntities())
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
            using (var db = new LisansustuBasvuruSistemiEntities())
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
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var nowDate = DateTime.Now;
                int? ID = null;
                var bs = db.BasvuruSurecs.Where(p => p.EnstituKod == EnstituKod && p.BasvuruSurecMulakatSinavTurleris.Any() && (p.SonucGirisBaslangicTarihi <= nowDate && p.SonucGirisBitisTarihi >= nowDate) && p.BasvuruSurecID == (BasvuruSurecID.HasValue ? BasvuruSurecID.Value : p.BasvuruSurecID)).FirstOrDefault();
                if (bs != null) ID = bs.BasvuruSurecID;
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
                        Management.SistemBilgisiKaydet(message, "Başvuru Sil", LogType.Kritik);
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
                        SistemBilgisiKaydet("Başka bir kullanıcıya ait başvuruyu silmeye hakkınız yoktur! \r\n Silinmeye çalışılan Başvuru ID:" + basvuru.BasvuruID + " \r\n Başvuru Sahibi:" + basvuru.Kullanicilar.KullaniciAdi + " \r\n Başvuru Tarihi:" + basvuru.BasvuruTarihi.ToString(), "Başvuru Sil", LogType.Saldırı);
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
                        SistemBilgisiKaydet("Başvuru durumu '" + basvuru.BasvuruDurumlari.BasvuruDurumAdi + "' olan başvurularda silme işlemi yapamazsınız! \r\n Çağrılan Başvuru ID:" + basvuru.BasvuruID + " \r\n Başvuru Sahibi:" + basvuru.Kullanicilar.KullaniciAdi, "Başvuru Sil", LogType.Kritik);
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
                        if (RoleNames.GelenBasvurularKayit.InRoleCurrent() == false) SistemBilgisiKaydet("Aranan başvuru sistemde bulunamadı! \r\n Çağrılan Başvuru ID:" + BasvuruID, "Başvuru Düzelt", LogType.Uyarı);
                    }
                    else
                    {
                        if (basvuru.BasvuruSurec.EnstituKod != EnstituKod)
                        {
                            SistemBilgisiKaydet("Seçilen başvuru Enstitü kodu ile aktif Enstitü kodu uyuşmuyor! \r\n Çağrılan Başvuru Enstitü Kod:" + basvuru.BasvuruSurec.EnstituKod + " \r\n Aktif Enstitü Kod:" + EnstituKod + " \r\n Çağrılan Başvuru ID:" + basvuru.BasvuruID + " \r\n Başvuru Sahibi:" + basvuru.Kullanicilar.KullaniciAdi, "Başvuru Düzelt", LogType.Uyarı);
                            EnstituKod = basvuru.BasvuruSurec.EnstituKod;
                        }
                        if (!UserIdentity.Current.EnstituKods.Contains(basvuru.BasvuruSurec.EnstituKod) && RoleNames.GelenBasvurularKayit.InRoleCurrent() && basvuru.KullaniciID != UserIdentity.Current.Id)
                        {
                            msg.IsSuccess = false;
                            msg.Messages.Add("Bu enstitüye ait başvuruyu güncellemeye yetkili değilsiniz!");
                            string message = "Bu enstitüye ait başvuruyu güncellemeye yetkili değilsiniz!\r\n Başvuru ID: " + basvuru.BasvuruID + " \r\n Başvuru sahibi: " + basvuru.Kullanicilar.Ad + " " + basvuru.Kullanicilar.Soyad + " \r\n Başvuru Tarihi: " + basvuru.BasvuruTarihi.ToString();
                            Management.SistemBilgisiKaydet(message, "Başvuru Düzelt", LogType.Saldırı);
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
                            SistemBilgisiKaydet("Başka bir kullanıcıya ait başvuruyu düzenlemeye hakkınız yoktur! \r\n Çağrılan Başvuru ID:" + basvuru.BasvuruID + " \r\n Başvuru Sahibi:" + basvuru.Kullanicilar.KullaniciAdi, "Başvuru Düzelt", LogType.Saldırı);
                        }
                        else if (RoleNames.GelenBasvurularKayit.InRoleCurrent() && basvuru.MulakatSonuclaris.Any(a => a.KayitDurumID.HasValue && a.KayitDurumlari.IsKayitOldu == true) && UserIdentity.Current.IsAdmin == false)
                        {
                            msg.IsSuccess = false;
                            msg.Messages.Add("Başvuru durumu '" + basvuru.BasvuruDurumlari.BasvuruDurumAdi + "' olan başvurularda düzenleme işlemi yapamazsınız!");
                            SistemBilgisiKaydet("Başvuru durumu '" + basvuru.BasvuruDurumlari.BasvuruDurumAdi + "' olan başvurularda düzenleme işlemi yapamazsınız! \r\n Çağrılan Başvuru ID:" + basvuru.BasvuruID + " \r\n Başvuru Sahibi:" + basvuru.Kullanicilar.KullaniciAdi, "Başvuru Düzelt", LogType.Kritik);
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
                        SistemBilgisiKaydet("Başka bir kullanıcıya adına başvuru yapılmak isteniyor! \r\n Başvuru yapılmak istenen Kullanıcı ID:" + KullaniciID + " \r\n İşlem Yapan Kullanıcı ID:" + UserIdentity.Current.Id, "Başvury Yap", LogType.Saldırı);
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

        public static BasvuruDetaySecilenDto getSecilenBasvuruDetay(int BasvuruID)
        {
            var model = new BasvuruDetaySecilenDto();
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
                         select new BasvuruDetaySecilenDto
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

     
      
     
        #endregion

        #region Extension



      
       
       
      
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
        //public static string ToExceptionMessage(this Exception ex)
        //{
        //    int ix = 1;
        //    Dictionary<int, string> msgs = new Dictionary<int, string>() { { ix, ex.Message } };
        //    var innException = ex;
        //    while ((innException = innException.InnerException) != null)
        //    {
        //        ix++;
        //        msgs.Add(ix, innException.Message);
        //    }
        //    var returnMsg = string.Join("\r\n", msgs.Select(s => s.Key + "- " + s.Value).ToArray());

        //    if (ex is DbEntityValidationException)
        //    {
        //        var msgsVex = new List<string>();
        //        var exV = (DbEntityValidationException)ex;
        //        foreach (var eve in exV.EntityValidationErrors)
        //        {
        //            foreach (var ve in eve.ValidationErrors)
        //            {
        //                msgsVex.Add(string.Format("State: {0} Property: {1}, Error: {2}", eve.Entry.State, ve.PropertyName, ve.ErrorMessage));
        //            }
        //        }
        //        if (msgsVex.Any())
        //        {
        //            msgsVex.Insert(0, "Veri Giriş Hataları:");
        //            returnMsg += "\r\n" + string.Join("\r\n", msgsVex);
        //        }
        //    }

        //    return returnMsg;
        //}
        //public static string ToExceptionStackTrace(this Exception ex)
        //{
        //    Dictionary<int, string> stck = new Dictionary<int, string>();

        //    int ix = 1;
        //    var innException = ex;
        //    stck.Add(ix, ex.StackTrace);
        //    while ((innException = innException.InnerException) != null)
        //    {
        //        ix++;
        //        stck.Add(ix, innException.StackTrace);
        //    }
        //    return string.Join("\r\n", stck.Select(s => s.Key + "- " + s.Value).ToArray());
        //}


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

        public static int PageSize = 15;

        //public static string RemoveIllegalFileNameChars(this string input, string replacement = "")
        //{
        //    var regexSearch = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
        //    var r = new Regex(string.Format("[{0}]", Regex.Escape(regexSearch)));
        //    return r.Replace(input, replacement);
        //}
        //public static string ReplaceSpecialCharacter(this string gelenStr)
        //{
        //    string regexSearch = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
        //    Regex r = new Regex(string.Format("[{0}]", Regex.Escape(regexSearch)));
        //    var fname = r.Replace(gelenStr, "");
        //    return fname;

        //}
        //public static bool IsSpecialCharacterCheck(this string gelenStr)
        //{
        //    var regexItem = new Regex("^[a-zA-Z0-9 ]*$");
        //    return regexItem.IsMatch(gelenStr);
        //}

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
        //public static string ToKullaniciResim(this string ResimAdi)
        //{

        //    var rsm = ResimAdi.IsNullOrWhiteSpace() ? (Management.getRoot() + SistemAyar.KullaniciDefaultResim) : (Management.getRoot() + SistemAyar.KullaniciResimYolu + "/" + ResimAdi);
        //    return rsm;
        //}

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

        public static string ToTIDegerlendirmeSonucu(bool? IsOyBirligiOrCouklugu, bool? IsBasariliOrBasarisiz)
        {
            string ReturnSonuc = "";

            if (IsOyBirligiOrCouklugu.HasValue && IsBasariliOrBasarisiz.HasValue)
            {
                ReturnSonuc += IsOyBirligiOrCouklugu.Value ? "Oy Birliği ile" : "Oy Çokluğu ile";
                ReturnSonuc += IsBasariliOrBasarisiz.Value ? " Başarılı" : " Başarısız";

            }
            return ReturnSonuc;
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
                                Management.SistemBilgisiKaydet("Sanal pos ödeme sonrası dekont bilgisi işlenemedi! (" + kul.Ad + " " + kul.Soyad + ") " + msj, "Management/DekontOdemeIsle", LogType.Kritik);
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
                            else Management.SistemBilgisiKaydet("Mail gönderilirken eklenen dosya eki sistemde bulunamadı!<br/>Dosya Adı:" + itemSe.EkAdi + " <br/>Dosya Yolu:" + ekTamYol, "Management/sendMailMezuniyetSinavYerBilgisi", LogType.Uyarı);
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
                    Management.SistemBilgisiKaydet("Ödeme İşleminden Sonra Sipariş Numarası Hiçbir Program Tercihi İle Eşleşmedi! <br/>Sipariş No: " + SiparisNo, "Management/DekontOdemeIsle", LogType.Kritik);
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

       


        
        #endregion
    }

}