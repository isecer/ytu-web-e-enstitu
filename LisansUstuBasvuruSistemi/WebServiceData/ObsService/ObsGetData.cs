using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Business;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.Helpers;
using LisansUstuBasvuruSistemi.Ws_ObsService;

namespace LisansUstuBasvuruSistemi.Models.ObsService
{
    public class ObsGetData
    {
      

        public string UserName => "ProEnsMiner";
        public string Password => "+!Pro*22Ytu!23#-Ens+!";
        public StudentControl GetObsStudentControl(string tcKimlikNo, string donemId)
        {
            var model = new StudentControl();
            try
            {
                if (tcKimlikNo.IsNullOrWhiteSpace()) throw new Exception("Tc Kimlik No boş geliyor!");
                if (donemId.IsNullOrWhiteSpace()) throw new Exception("Dönem Bilgisi No boş geliyor!");
                using (var service =
                       new proliz_ytu_enstitu_minerSoapClient())
                {

                    var ogrencis = service.AktifOgrenciBilgiGetir(UserName, Password, null, tcKimlikNo);

                    if (!ogrencis.Any() || !ogrencis[0].Sucess)
                    {
                        ogrencis = service.AktifOgrenciBilgiGetir(UserName, Password, tcKimlikNo, null);
                    }

                    if (ogrencis.Any() && ogrencis[0].Sucess)
                    {

                        model.KayitVar = true;
                        var ogrenci = ogrencis[0].ogrenci.OrderBy(p => (p.OGRENIMSEVIYE_ID == "2" || p.OGRENIMSEVIYE_ID == "3" || p.OGRENIMSEVIYE_ID == "8") ? 1 : 2).FirstOrDefault();
                        if (ogrenci != null)
                        {
                            if (!ogrenci.KAYIT_TARIHI.IsNullOrWhiteSpace())
                            {
                                model.KayitTarihi = ogrenci.KAYIT_TARIHI.ToDate().Value;
                                var donem = model.KayitTarihi.Value.ToAraRaporDonemBilgi();
                                model.BaslangicYil = donem.BaslangicYil;
                                model.BitisYil = donem.BitisYil;
                                model.DonemID = donem.DonemID;

                            }

                            model.OkuduguDonemNo = ogrenci.OKUDUGU_DNM_YENIKANUN.ToIntObj() ?? 0;
                            model.OgrenciInfo = ogrenci;

                            switch (model.OgrenciInfo.OGRENIMSEVIYE_ID)
                            {
                                case "2":
                                    model.OgrenciInfo.OGRENIMSEVIYE_ID = "1";
                                    break;
                                case "3":
                                    model.OgrenciInfo.OGRENIMSEVIYE_ID = "3";
                                    break;
                                case "4":
                                    model.OgrenciInfo.OGRENIMSEVIYE_ID = "2";
                                    break;
                                case "5":
                                    model.OgrenciInfo.OGRENIMSEVIYE_ID = "4";
                                    break;
                                case "8":
                                    model.OgrenciInfo.OGRENIMSEVIYE_ID = "5";
                                    break;
                            }


                            var ogrenciDersler =
                                service.OgrenciDersBilgileriGetir(UserName, Password, ogrenci.OGR_NO, null, donemId);

                            if (ogrenciDersler[0].Sucess)
                            {
                                var ogrenciDers = ogrenciDersler[0].ogrencidersnot[0];
                                model.AktifDonemDers.ToplamKredi = ogrenciDers.SON_KREDI.ToIntObj().Value;
                                model.AktifDonemDers.ToplamAkts = ogrenciDers.TOP_AKTS.ToDecimalObj().ToIntObj().Value;
                                model.AktifDonemDers.Agno = ogrenciDers.TOP_AKTS.ToDoubleObj().Value;
                                model.AktifDonemDers.EtikDersNotu = ogrenciDers.B_ETIK_DERS_NOTU;
                                model.AktifDonemDers.SeminerDersNotu = ogrenciDers.SEMINER_DERS_NOTU;
                                model.AktifDonemDers.ZorunluDersSayisi = ogrenciDers.ZORUN_DERS_SAYISI.ToIntObj().Value;
                                model.AktifDonemDers.AbdDersSayisi = ogrenciDers.ANABILIMDALI_DERS_SAYISI.ToIntObj().Value;
                                var dersler = ogrenciDers.AKTIF_DNM_DERS.Split(',').Where(p => !p.IsNullOrWhiteSpace()).ToList();

                                if (dersler.Any())
                                {
                                    model.AktifDonemDers.DersKodus = dersler.Select(s => s).ToList();
                                    model.AktifDonemDers.DersKodNums = dersler.Select(s => s.Substring(s.Length - 4, 4)).ToList();
                                }
                            }
                            var ogrenciTez = service.OgrenciTezBilgileriGetir(UserName, Password, ogrenci.OGR_NO, null);
                            if (ogrenciTez.Any())
                            {
                                var tezBilgi = ogrenciTez[0];
                                if (tezBilgi.Sucess)
                                {
                                    var tez = tezBilgi.ogrencitez.LastOrDefault();
                                    if (tez != null)
                                    {
                                        model.IsTezDiliTr = tez.TEZ_DILI.ToLower().Contains("türkçe");
                                        model.OgrenciTez = tez;

                                        var sonTezIzlemeBilgiler = tez.tezizlemebilgileri
                                            .OrderByDescending(o => o.TEZ_IZL_SIRA.ToIntObj()).FirstOrDefault();
                                        if (sonTezIzlemeBilgiler != null)
                                            model.SonTezIzlemeBilgileri = sonTezIzlemeBilgiler;
                                    }

                                }

                            }
                            var tezJuri = service.OgrenciTezizlemeJuriBilgileriGetir(UserName, Password, ogrenci.OGR_NO, null);
                            model.TezIzlJuriBilgileri = tezJuri[0].Sucess ? tezJuri[0].tezIzljuribilgileri.ToList() : new List<TezIzlJuriBilgileri>();

                            if (!ogrenci.DANISMAN_TC1.IsNullOrWhiteSpace())
                            {
                                var danismanResult =
                                    service.AkademikPersonelBilgiGetir(UserName, Password, null, ogrenci.DANISMAN_TC1);
                                if (danismanResult.Any())
                                {
                                    var danismanBilgi = danismanResult[0];
                                    if (danismanBilgi.Sucess)
                                    {
                                        model.DanismanInfo = danismanBilgi.personel.FirstOrDefault();
                                    }
                                }
                            }
                            var ogrenciYeterResult = service.OgrenciDrYeterBilgileriGetir(UserName, Password, ogrenci.OGR_NO, null);
                            model.OgrenciYeters = ogrenciYeterResult[0].Sucess ? ogrenciYeterResult[0].ogrenciyeter.ToList() : new List<OgrenciYeter>();

                        }

                    }
                    if (ogrencis.Any() && !ogrencis[0].Sucess)
                    {
                        model.Hata = true;
                        model.HataMsj = ogrencis[0].Error;
                    }
                }

            }
            catch (Exception ex)
            {
                model.Hata = true;
                model.HataMsj = "OBS sisteminden kayıt kontrolü başarısız oldu! Lütfen sistem yöneticisine başvurunuz! Hata:" + ex.ToExceptionMessage();
                SistemBilgilendirmeBus.SistemBilgisiKaydet(model.HataMsj, "Management/studentControl\r\n" + ex.ToExceptionStackTrace(), LogTipiEnum.Kritik);
            }

            if (model.OgrenciInfo == null) model.OgrenciInfo = new Ogrenci();
            if (model.OgrenciTez == null) model.OgrenciTez = new OgrenciTez();
            if (model.SonTezIzlemeBilgileri == null) model.SonTezIzlemeBilgileri = new TezIzlemeBilgileri();

            if (model.SonTezIzlemeBilgileri.tezIzljuribilgileri == null)
                model.SonTezIzlemeBilgileri.tezIzljuribilgileri = Array.Empty<TezIzlJuriBilgileri>();
            return model;
        }


        public ObsOgrenciSorgulaModel GetOgrenciBilgi(string tcKimlikNo, string donemId)
        {
            var model = new ObsOgrenciSorgulaModel();
            try
            {
                using (var service = new proliz_ytu_enstitu_minerSoapClient())
                {
                    var ogrencis = service.AktifOgrenciBilgiGetir(UserName, Password, null, tcKimlikNo);
                    if (!ogrencis.Any() || !ogrencis[0].Sucess)
                    {
                        ogrencis = service.AktifOgrenciBilgiGetir(UserName, Password, tcKimlikNo, null);
                    }
                    if (ogrencis.Any() && ogrencis[0].Sucess)
                    {
                        model.Ogrenci = ogrencis[0].ogrenci.OrderBy(p => (p.OGRENIMSEVIYE_ID == "2" || p.OGRENIMSEVIYE_ID == "3" || p.OGRENIMSEVIYE_ID == "8") ? 1 : 2).FirstOrDefault();
                        var ogrenciDers = service.OgrenciDersBilgileriGetir(UserName, Password, model.Ogrenci?.OGR_NO, null, donemId);
                        if (ogrenciDers.Any() && ogrenciDers[0].Sucess)
                        {
                            model.OgrenciDersNot = ogrenciDers[0].ogrencidersnot[0];
                        }
                        var ogrenciTez = service.OgrenciTezBilgileriGetir(UserName, Password, model.Ogrenci?.OGR_NO, null);
                        if (ogrenciTez.Any() && ogrenciTez[0].Sucess)
                        {
                            model.OgrenciTez = ogrenciTez[0].ogrencitez[0];
                            model.OgrenciTez.tezizlemebilgileri = model.OgrenciTez.tezizlemebilgileri
                                .OrderByDescending(o => o.TEZ_IZL_SIRA.ToInt()).ToArray();
                        }

                        var ogrenciTezJuri = service.OgrenciTezizlemeJuriBilgileriGetir(UserName, Password, model.Ogrenci?.OGR_NO, null);
                        if (ogrenciTezJuri.Any() && ogrenciTezJuri[0].Sucess)
                        {
                            model.OgrenciTezJuri = ogrenciTezJuri[0].tezIzljuribilgileri.ToList();
                        }
                        var ogrenciYeterResult = service.OgrenciDrYeterBilgileriGetir(UserName, Password, model.Ogrenci?.OGR_NO, null);
                        model.OgrenciYeters = ogrenciYeterResult[0].Sucess ? ogrenciYeterResult[0].ogrenciyeter.ToList() : new List<OgrenciYeter>();
                    }
                }
            }
            catch (Exception)
            {
                // ignored
            }

            return model;
        }
    }
}