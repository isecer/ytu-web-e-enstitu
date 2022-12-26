using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Models.FilterModel;
using LisansUstuBasvuruSistemi.Ws_ObsService;

namespace LisansUstuBasvuruSistemi.Models.ObsService
{
    public class ObsGetData
    {
        public string UserName => "ProEnsMiner";
        public string Password => "+!Pro*22Ytu!23#-Ens+!";
        public StudentControl GetObsStudentControl(string ogrenciNo, string tcKimlikNo = null)
        {
            var model = new StudentControl();
            try
            {
                using (Ws_ObsService.proliz_ytu_enstitu_minerSoapClient service =
                       new Ws_ObsService.proliz_ytu_enstitu_minerSoapClient())
                {
                    var ogrencis = service.AktifOgrenciBilgiGetir(UserName, Password, ogrenciNo, tcKimlikNo);

                    if (ogrencis.Any())
                    {

                        if (ogrencis[0].Sucess)
                        {
                            model.KayitVar = true;
                            var ogrenci = ogrencis[0].ogrenci[0];

                            if (!ogrenci.KAYIT_TARIHI.IsNullOrWhiteSpace())
                            {
                                model.KayitTarihi = ogrenci.KAYIT_TARIHI.ToDate().Value;
                                var donem = Management.ToAraRaporDonemBilgi(model.KayitTarihi.Value);
                                model.BaslangicYil = donem.BaslangicYil;
                                model.BitisYil = donem.BitisYil;
                                model.DonemID = donem.DonemID;
                            }

                            model.OgrenciInfo = ogrenci;

                            switch (model.OgrenciInfo.OGRENIMSEVIYE_ID)
                            {
                                case "2":
                                    model.OgrenciInfo.OGRENIMSEVIYE_ID = "1";
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
                                service.OgrenciDersBilgileriGetir(UserName, Password, ogrenciNo, tcKimlikNo);

                            if (ogrenciDersler[0].Sucess)
                            {
                                var ogrenciDers = ogrenciDersler[0].ogrencidersnot[0];
                                model.AktifDonemDers.ToplamKredi = ogrenciDers.TOP_KREDI.toIntObj().Value;
                                model.AktifDonemDers.ToplamAkts = ogrenciDers.TOP_AKTS.toDecimalObj().toIntObj().Value;
                                model.AktifDonemDers.Agno = ogrenciDers.TOP_AKTS.toDoubleObj().Value;
                                model.AktifDonemDers.EtikDersNotu = ogrenciDers.B_ETIK_DERS_NOTU;
                                model.AktifDonemDers.SeminerDersNotu = ogrenciDers.SEMINER_DERS_NOTU;
                                model.AktifDonemDers.ZorunluDersSayisi = ogrenciDers.ZORUN_DERS_SAYISI.toIntObj().Value;
                                model.AktifDonemDers.AbdDersSayisi = ogrenciDers.ANABILIMDALI_DERS_SAYISI.toIntObj().Value;
                                var dersler = ogrenciDers.AKTIF_DNM_DERS.Split(',').Where(p => !p.IsNullOrWhiteSpace()).ToList();

                                if (dersler.Any())
                                {
                                    model.AktifDonemDers.DersKodus = dersler.Select(s => s).ToList();
                                    model.AktifDonemDers.DersKodNums = dersler.Select(s => s.Substring(s.Length - 4, 4)).ToList();
                                }
                            }
                            var ogrenciTez = service.OgrenciTezBilgileriGetir(UserName, Password, ogrenciNo, tcKimlikNo);
                            if (ogrenciTez.Any())
                            {
                                var tezBilgi = ogrenciTez[0];
                                if (tezBilgi.Sucess)
                                {
                                    var tez = tezBilgi.ogrencitez.LastOrDefault();
                                    if (tez != null)
                                    {
                                        model.IsTezDiliTr = tez.TEZ_DILI.ToLower() == "türkçe";
                                        model.OgrenciTez = tez;

                                        var sonTezIzlemeBilgiler = tez.tezizlemebilgileri
                                            .OrderByDescending(o => o.TEZ_IZL_SIRA.toIntObj()).FirstOrDefault();
                                        if (sonTezIzlemeBilgiler != null)
                                            model.SonTezIzlemeBilgileri = sonTezIzlemeBilgiler;
                                    }

                                }

                            }
                            var tezJuri = service.OgrenciTezizlemeJuriBilgileriGetir(UserName, Password, ogrenciNo, tcKimlikNo);
                            model.TezIzlJuriBilgileri = tezJuri[0].Sucess ? tezJuri[0].tezIzljuribilgileri.ToList() : new List<TezIzlJuriBilgileri>(); 

                            if (!model.OgrenciInfo.DANISMAN_TC1.IsNullOrWhiteSpace())
                            {
                                var danismanResult =
                                    service.AkademikPersonelBilgiGetir(UserName, Password, null, model.OgrenciInfo.DANISMAN_TC1);
                                if (danismanResult.Any())
                                {
                                    var danismanBilgi = danismanResult[0];
                                    if (danismanBilgi.Sucess)
                                    {
                                        model.DanismanInfo = danismanBilgi.personel.FirstOrDefault();

                                    }
                                }
                            }

                        }

                    }

                }
            }
            catch (Exception ex)
            {
                model.Hata = true;
                model.HataMsj =
                    "OBS sisteminden kayıt kontrolü başarısız oldu! Lütfen sistem yöneticisine başvurunuz! Hata:" +
                    ex.ToExceptionMessage();
                Management.SistemBilgisiKaydet(model.HataMsj, "Management/studentControl\r\n" + ex.ToExceptionStackTrace(), BilgiTipi.Kritik);
            }

            if (model.OgrenciInfo == null) model.OgrenciInfo = new Ogrenci();
            if (model.OgrenciTez == null) model.OgrenciTez = new OgrenciTez();
            if (model.SonTezIzlemeBilgileri == null) model.SonTezIzlemeBilgileri = new TezIzlemeBilgileri();

            if (model.SonTezIzlemeBilgileri.tezIzljuribilgileri == null)
                model.SonTezIzlemeBilgileri.tezIzljuribilgileri = new TezIzlJuriBilgileri[0];
            return model;
        }
    }
}