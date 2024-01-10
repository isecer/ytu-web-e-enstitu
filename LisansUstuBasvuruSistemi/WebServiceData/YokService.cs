using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Web;
using LisansUstuBasvuruSistemi.Business;
using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.Helpers;
using LisansUstuBasvuruSistemi.Utilities.SystemSetting;
using Newtonsoft.Json;

namespace LisansUstuBasvuruSistemi.WebServiceData
{
    public static class YokService
    {
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
                    var BirimKods = StaticDefinitions.GetBirimTurKods();
                    var UniKods = StaticDefinitions.GetUniversiteTurKods();
                    var OtKods = StaticDefinitions.GetOgrenimTurKods();
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
                    SistemBilgilendirmeBus.SistemBilgisiKaydet(model.Mesaj, ObjectExtensions.GetCurrentMethodPath(), BilgiTipiEnum.Kritik);

                }
            }
            catch (Exception ex)
            {
                model.KayitVar = false;
                model.Hata = true;
                model.Mesaj = "YÖK Servisinden Öğrenci Bilgisi kontrol edilirken bir hata oluştu.Hata:" + ex.ToExceptionMessage();
                SistemBilgilendirmeBus.SistemBilgisiKaydet(model.Mesaj, ex.ToExceptionStackTrace(), BilgiTipiEnum.Kritik);
            }

            return model;
        }
    }
}