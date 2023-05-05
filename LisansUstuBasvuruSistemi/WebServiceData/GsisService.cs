using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Utilities.Extensions;

namespace LisansUstuBasvuruSistemi.WebServiceData
{
    public static class GsisService
    {
        public static Ws_GsisMezuniyetBilgi.TezInfo GetGsisMezuniyetTezBilgi(string OgrenciNo)
        {
            //Aşağıdaki kullanıcı adı ve şifre servisi eklerken de gerekli
            string UserName = "hazirlik";
            string Password = "J6Gnx3Ah";
            using (Ws_GsisMezuniyetBilgi.DanismanServiceSoapClient client = new Ws_GsisMezuniyetBilgi.DanismanServiceSoapClient())
            {
                client.ClientCredentials.Windows.ClientCredential.UserName = UserName;
                client.ClientCredentials.Windows.ClientCredential.Password = Password;
                var data = client.TezInfoGSIS(OgrenciNo);

                if (!data.ad.IsNullOrWhiteSpace()) data.ad = System.Web.HttpUtility.HtmlDecode(data.ad).Trim();
                if (!data.tezDanismani.IsNullOrWhiteSpace()) data.tezDanismani = System.Web.HttpUtility.HtmlDecode(data.tezDanismani).Trim();
                if (!data.tezDanismaniUnvan.IsNullOrWhiteSpace()) data.tezDanismaniUnvan = System.Web.HttpUtility.HtmlDecode(data.tezDanismaniUnvan).Trim();
                if (!data.tezDanismaniUnvan.IsNullOrWhiteSpace() && data.tezDanismaniUnvan.Contains("Üye")) data.tezDanismaniUnvan = "DR.ÖĞR.ÜYE.";
                if (!data.esDanismani.IsNullOrWhiteSpace()) data.esDanismani = System.Web.HttpUtility.HtmlDecode(data.esDanismani).Trim();
                if (!data.esDanismaniUnvan.IsNullOrWhiteSpace()) data.esDanismaniUnvan = System.Web.HttpUtility.HtmlDecode(data.esDanismaniUnvan).Trim();
                if (!data.esDanismaniUnvan.IsNullOrWhiteSpace() && data.esDanismaniUnvan.Contains("Üye")) data.esDanismaniUnvan = "DR.ÖĞR.ÜYE.";
                if (!data.tikUyesi1.IsNullOrWhiteSpace()) data.tikUyesi1 = System.Web.HttpUtility.HtmlDecode(data.tikUyesi1).Trim();
                if (!data.tikUyesi1Unvan.IsNullOrWhiteSpace()) data.tikUyesi1Unvan = System.Web.HttpUtility.HtmlDecode(data.tikUyesi1Unvan).Trim();
                if (!data.tikUyesi1Unvan.IsNullOrWhiteSpace() && data.tikUyesi1Unvan.Contains("Üye")) data.tikUyesi1Unvan = "DR.ÖĞR.ÜYE.";
                if (!data.tikUyesi2.IsNullOrWhiteSpace()) data.tikUyesi2 = System.Web.HttpUtility.HtmlDecode(data.tikUyesi2).Trim();
                if (!data.tikUyesi2Unvan.IsNullOrWhiteSpace()) data.tikUyesi2Unvan = System.Web.HttpUtility.HtmlDecode(data.tikUyesi2Unvan).Trim();
                if (!data.tikUyesi2Unvan.IsNullOrWhiteSpace() && data.tikUyesi2Unvan.Contains("Üye")) data.tikUyesi2Unvan = "DR.ÖĞR.ÜYE.";
                //if (!data.ingilizceAd.IsNullOrWhiteSpace()) data.ingilizceAd = System.Web.HttpUtility.HtmlDecode(data.ingilizceAd).Trim();
                return data;

                //  var derslers = model.Select(s => new { s.DersKodu, s.DersAdi }).Distinct().ToList();
                //  var DersWarMi = derslers.Where(p => p.DersKodu == "IKT4000").FirstOrDefault();

            }

        }
        public static string gsisKayitAktar(string kayit)
        {
            System.Net.ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };//SSL hatasını gidermek için 
            using (Ws_GsisAktarim.ServiceSoapClient aktarim = new Ws_GsisAktarim.ServiceSoapClient("ServiceSoap"))
            {
                return aktarim.studentRegistration(kayit);
            }

        }
        //public static OnlineOdemeProgramDetayModel GetGsisProgramUcretKontrol(OnlineOdemeProgramDetayModel mdl)
        //{
        //    System.Net.ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };//SSL hatasını gidermek için 
        //    using (Ws_GsisOdeme.ServiceSoapClient wS = new Ws_GsisOdeme.ServiceSoapClient("ServiceSoap1"))
        //    {
        //        var data = wS.getProgramFee(mdl.ProgramKod, mdl.OgrenimTipKod.ToString());
        //        mdl.OdemeDonemNo = mdl.IsDekontOrSanalPos ? 1 : (mdl.OdemeListesi.Count > 0 ? (mdl.OdemeListesi.Select(s => s.DonemNo).Max() + 1) : 1);
        //        if (data.returnInfo == "1")
        //        {
        //            mdl.IsOdemeVar = true;
        //            mdl.OdenecekUcret = data.amount.toDoubleObj();
        //            mdl.OdemeBaslangicTarihi = data.firstpaymentdate != "" ? Convert.ToDateTime(data.firstpaymentdate) : (DateTime?)null;
        //            mdl.OdemeBitisTarihi = data.lastpaymentdate != "" ? Convert.ToDateTime(data.lastpaymentdate) : (DateTime?)null;
        //            if (mdl.OdemeBaslangicTarihi.HasValue && mdl.OdemeBitisTarihi.HasValue)
        //            {
        //                if (mdl.OdemeBaslangicTarihi <= DateTime.Now.Date && mdl.OdemeBitisTarihi >= DateTime.Now.Date)
        //                    mdl.IsOdemeIslemiAcik = true;
        //                else
        //                {

        //                    mdl.AciklamaSelectedLng = "Sadece Ödeme Tarih Aralığında Ödeme İşlemi Yapılabilir.";
        //                }
        //            }
        //            else
        //            {
        //                mdl.AciklamaSelectedLng = "Sadece Ödeme Tarih Aralığında Ödeme İşlemi Yapılabilir.";
        //            }
        //        }
        //        else
        //        {
        //            mdl.AciklamaSelectedLng = "Hata oluştu";
        //            Management.SistemBilgisiKaydet("Gsis Program Ücreti Sorgulanırken Servis Hata Döndürdü! (" + mdl.AdSoyad + ")", "Management/GetGsisProgramUcretKontrol/getProgramFee ProgramKod:" + mdl.ProgramKod + " OgrenimTipKod:" + mdl.OgrenimTipKod + " Dönen Hata Kodu:" + data.returnInfo, BilgiTipi.Kritik);

        //        }


        //    }
        //    return mdl;
        //}
        //public static bool GetGsisUcretOdemeTarihKontrol(out string Msj, out DateTime? BaslangicTarihi, out DateTime? BitisTarihi)
        //{
        //    bool returnValue = true;
        //    string _Msj = "";
        //    DateTime? _BaslangicTarihi = null;
        //    DateTime? _BitisTarihi = null;
        //    System.Net.ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };//SSL hatasını gidermek için 
        //    using (Ws_GsisOdeme.ServiceSoapClient wS = new Ws_GsisOdeme.ServiceSoapClient("ServiceSoap1"))
        //    {
        //        var OdemeBasTarih = wS.getFirstPaymentDate();
        //        var OdemeBitTarih = wS.getLastPaymentDate();
        //        if (OdemeBasTarih == "-1" || OdemeBasTarih == "-2")
        //        {
        //            _Msj = "Hata";
        //            Management.SistemBilgisiKaydet("Gsis Öğrenci Borcuna Ait Ödeme Başlangıç Tarihi Sorgulanırken Servis Hata Döndürdü!", "Management/GetGsisUcretOdemeTarihKontrol/getFirstPaymentDate Dönen Hata Kodu:" + OdemeBasTarih, BilgiTipi.Kritik);
        //            returnValue = false;
        //        }
        //        else
        //        {
        //            _BaslangicTarihi = OdemeBasTarih != "" ? Convert.ToDateTime(OdemeBasTarih) : (DateTime?)null;
        //        }
        //        if (OdemeBitTarih == "-1" || OdemeBitTarih == "-2")
        //        {
        //            returnValue = false;
        //            _Msj = "Hata";
        //            Management.SistemBilgisiKaydet("Gsis Öğrenci Borcuna Ait Ödeme Bitiş Tarihi Sorgulanırken Servis Hata Döndürdü!", "Management/GetGsisUcretOdemeTarihKontrol/getLastPaymentDate Dönen Hata Kodu:" + OdemeBitTarih, BilgiTipi.Kritik);
        //        }
        //        else
        //        {
        //            _BitisTarihi = OdemeBasTarih != "" ? Convert.ToDateTime(OdemeBitTarih) : (DateTime?)null;
        //        }
        //    }
        //    Msj = _Msj;
        //    BaslangicTarihi = _BaslangicTarihi;
        //    BitisTarihi = _BitisTarihi;
        //    return returnValue;
        //}
        //public static OnlineOdemeProgramDetayModel GetGsisOgrenciOgrenimBorcSorgula(OnlineOdemeProgramDetayModel mdl)
        //{
        //    System.Net.ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };//SSL hatasını gidermek için 
        //    using (Ws_GsisOdeme.ServiceSoapClient wS = new Ws_GsisOdeme.ServiceSoapClient("ServiceSoap1"))
        //    {


        //        var data = wS.paymentControl(mdl.OgrenciNo.Trim());
        //        var RetVal = Convert.ToDouble(data);
        //        mdl.OdemeDonemNo = mdl.IsDekontOrSanalPos ? 1 : (mdl.OdemeListesi.Count > 0 ? (mdl.OdemeListesi.Select(s => s.DonemNo).Max() + 1) : 1);
        //        if (RetVal > 0)
        //        {
        //            //borçlandırma var dönen değer borç tutarı
        //            mdl.OdenecekUcret = RetVal;
        //            mdl.IsOdemeVar = true;
        //            var OdemeBasTarih = wS.getFirstPaymentDate();
        //            var OdemeBitTarih = wS.getLastPaymentDate();
        //            if (OdemeBasTarih == "-1" || OdemeBasTarih == "-2")
        //            {

        //                Management.SistemBilgisiKaydet("Gsis Öğrenci Borcuna Ait Ödeme Başlangıç Tarihi Sorgulanırken Servis Hata Döndürdü! (" + mdl.AdSoyad + " [" + mdl.OgrenciNo + "])", "Management/getFirstPaymentDate/paymentControl Dönen Hata Kodu:" + OdemeBasTarih, BilgiTipi.Kritik);
        //            }
        //            else
        //            {
        //                mdl.OdemeBaslangicTarihi = OdemeBasTarih != "" ? Convert.ToDateTime(OdemeBasTarih) : (DateTime?)null;
        //            }
        //            if (OdemeBitTarih == "-1" || OdemeBitTarih == "-2")
        //            {
        //                Management.SistemBilgisiKaydet("Gsis Öğrenci Borcuna Ait Ödeme Bitiş Tarihi Sorgulanırken Servis Hata Döndürdü! (" + mdl.AdSoyad + " [" + mdl.OgrenciNo + "])", "Management/GetOnlineOdemeProgramDetay/getLastPaymentDate Dönen Hata Kodu:" + OdemeBitTarih, BilgiTipi.Kritik);
        //            }
        //            else
        //            {
        //                mdl.OdemeBitisTarihi = OdemeBasTarih != "" ? Convert.ToDateTime(OdemeBitTarih) : (DateTime?)null;
        //            }
        //            if (mdl.OdemeBaslangicTarihi.HasValue && mdl.OdemeBitisTarihi.HasValue)
        //            {
        //                if (mdl.OdemeBaslangicTarihi <= DateTime.Now.Date && mdl.OdemeBitisTarihi >= DateTime.Now.Date)
        //                    mdl.IsOdemeIslemiAcik = true;
        //                else
        //                {

        //                    mdl.AciklamaSelectedLng = "Sadece Ödeme Tarih Aralığında Ödeme İşlemi Yapılabilir.";
        //                }
        //            }
        //            else
        //            {
        //                mdl.AciklamaSelectedLng = "Sadece Ödeme Tarih Aralığında Ödeme İşlemi Yapılabilir.";
        //            }
        //        }
        //        else if (RetVal == 0)
        //        {
        //            //borcu yok
        //            mdl.AciklamaSelectedLng = "Borç Yok";

        //        }
        //        else if (RetVal == -1)
        //        {
        //            //borçlandırma yok
        //            mdl.AciklamaSelectedLng = "Boçlandırma Bulunamadı";
        //        }
        //        else if (RetVal == -2 || RetVal == -3)
        //        {

        //            mdl.AciklamaSelectedLng = "Hata Oluştu!";
        //            //sistem hatası
        //            Management.SistemBilgisiKaydet("Gsis Öğrenci Borcu Sorgulanırken Servis Hata Döndürdü! (" + mdl.AdSoyad + " [" + mdl.OgrenciNo + "])", "Management/GetOnlineOdemeProgramDetay/paymentControl Dönen Hata Kodu:" + RetVal, BilgiTipi.Kritik);
        //        }

        //    }
        //    return mdl;
        //}
        //public static bool SetPaymentInfoGsis(string OgrenciNo, string OdenenTutar, string DekontNo, DateTime DekontTarihi, out string Msj)
        //{
        //    var returnValue = false;
        //    var _msj = "";
        //    try
        //    {
        //        System.Net.ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };//SSL hatasını gidermek için 
        //        using (Ws_GsisOdeme.ServiceSoapClient wS = new Ws_GsisOdeme.ServiceSoapClient("ServiceSoap1"))
        //        {
        //            var vsRetVal = wS.setPaymentInfo(OgrenciNo, OdenenTutar, DekontNo, DekontTarihi.ToString("yyyy-MM-dd"));
        //            if (vsRetVal == "1")
        //            {
        //                returnValue = true;
        //            }
        //            else
        //            {
        //                _msj = OgrenciNo + " numaralı öğrencinin  " + DekontNo + " dekont numarası " + DekontTarihi.ToFormatDate() + " ödeme tarihi ve " + OdenenTutar + " TL ödenen tutar bilgisi! Gsis veb servisi ile işlenirken bir hata oluştu! Sevisten dönen değer:" + vsRetVal;
        //            }

        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        _msj = OgrenciNo + " numaralı öğrencinin  " + DekontNo + " dekont numarası " + DekontTarihi.ToFormatDate() + " ödeme tarihi ve " + OdenenTutar + " TL ödenen tutar bilgisi! Gsis veb servisine işlenirken bir hata oluştu! Hata:" + ex.ToExceptionMessage();

        //    }
        //    Msj = _msj;
        //    return returnValue;
        //}
    }
}