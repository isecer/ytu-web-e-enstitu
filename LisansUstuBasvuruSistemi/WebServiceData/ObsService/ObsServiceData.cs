using System;
using System.Collections.Generic;
using System.Linq;
using Entities.Entities;
using LisansUstuBasvuruSistemi.Business;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.Helpers;
using LisansUstuBasvuruSistemi.Utilities.SystemSetting;
using LisansUstuBasvuruSistemi.Ws_ObsService;

namespace LisansUstuBasvuruSistemi.WebServiceData.ObsService
{
    public class ObsServiceData
    {
        public string UserName => "ProEnsMiner";
        public string Password => "+!Pro*22Ytu!23#-Ens+!";
        public StudentControl GetObsStudentControl(string tcOrOgrenciNo)
        {
            var model = new StudentControl();
            try
            {
                if (tcOrOgrenciNo.IsNullOrWhiteSpace()) throw new Exception("tcOrOgrenciNo boş geliyor!");
                tcOrOgrenciNo = tcOrOgrenciNo.RemoveNonAlphanumeric();
                using (var service =
                       new proliz_ytu_enstitu_minerSoapClient())
                {

                    var ogrencis = service.AktifOgrenciBilgiGetir(UserName, Password, tcOrOgrenciNo, null);

                    if (!ogrencis.Any() || !ogrencis[0].Sucess)
                    {
                        ogrencis = service.AktifOgrenciBilgiGetir(UserName, Password, null, tcOrOgrenciNo);
                    }

                    if (ogrencis.Any() && ogrencis[0].Sucess)
                    {

                        model.KayitVar = true;
                        var ogrenci = ogrencis[0].ogrenci.OrderBy(p => p.OGRENIMSEVIYE_ID == "4" ? 2 : 1).FirstOrDefault();
                        if (ogrenci != null)
                        {
                            if (!ogrenci.KAYIT_TARIHI.IsNullOrWhiteSpace())
                            {
                                model.KayitTarihi = ogrenci.KAYIT_TARIHI.ToDate().Value;
                                var parsedDonem = ogrenci.KAYITLI_DONEM.ParseObsDonem();
                                model.BaslangicYil = parsedDonem.BaslangicYil;
                                model.BitisYil = parsedDonem.BitisYil;
                                model.DonemID = parsedDonem.DonemNo;

                            }

                            model.OkuduguDonemNo = ogrenci.OKUDUGU_DNM_YENIKANUN.ToIntObj() ?? 0;
                            model.OgrenciInfo = ogrenci;
                            model.OgrenciInfo.OGRENIMSEVIYE_ID = model.OgrenciInfo.OGRENIMSEVIYE_ID.ToOgrenimTipKod().ToStrObj(); 
                            //enstitü ayarlaması 4 basamaklı olan obs enstitü kodu 3 basamaklı lubs enstitü koduna çevir
                            if (model.OgrenciInfo.ENSTITU_ID.Length > 3)
                                model.OgrenciInfo.ENSTITU_ID = model.OgrenciInfo.ENSTITU_ID.Remove(0, 1);


                            var ogrenciDersler = service.OgrenciDersBilgileriGetir(UserName, Password, ogrenci.OGR_NO, null, null);

                            if (ogrenciDersler[0].Sucess)
                            {
                                var ogrenciDers = ogrenciDersler[0].ogrencidersnot[0];
                                model.AktifDonemDers.ToplamKredi = ogrenciDers.SON_KREDI.ToIntObj(0);
                                model.AktifDonemDers.ToplamAkts = ogrenciDers.TOP_AKTS.ToDecimalObj().ToIntObj(0);
                                model.AktifDonemDers.Agno = ogrenciDers.TOP_AKTS.ToDoubleObj() ?? 0;
                                model.AktifDonemDers.EtikDersNotu = ogrenciDers.B_ETIK_DERS_NOTU;
                                model.AktifDonemDers.SeminerDersNotu = ogrenciDers.SEMINER_DERS_NOTU;
                                model.AktifDonemDers.ZorunluDersSayisi = ogrenciDers.ZORUN_DERS_SAYISI.ToIntObj() ?? 0;
                                model.AktifDonemDers.AbdDersSayisi = ogrenciDers.ANABILIMDALI_DERS_SAYISI.ToIntObj() ?? 0;
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



                            var ogrenciDersNots = service.OgrenciDersNotBilgileriGetir(UserName, Password, ogrenci.OGR_NO, null);
                            if (ogrenciDersNots.Any() && ogrenciDersNots[0].Sucess)
                            {


                                model.TumDonemDersNotlari = ogrenciDersNots[0].ogrencidersnot.Select(s =>
                                    new StudentDersNotModel
                                    {
                                        DonemId = s.DONEM_ID,
                                        HocaTc = s.HOCA_TCK,
                                        HocaUnvan = s.HOCA_UNVAN,
                                        HocaAdi = s.HOCA_AD_SOYAD,
                                        DonemAd = s.DONEM_AD,
                                        DersAdi = s.DERS_AD,
                                        DersKodu = s.DERS_KOD,
                                        DersKoduNum = s.DERS_KOD.Length > 4 ? s.DERS_KOD.Substring(s.DERS_KOD.Length - 4, 4) : s.DERS_KOD,
                                        DersNotu = s.HARF_KOD,
                                        NotDeger = s.NOT_DEGER


                                    }).ToList();

                                var aktifDonem = DateTime.Now.ToDonemProjesiDonemBilgi(model.OgrenciInfo.ENSTITU_ID);
                                var donemProjesiDersKodu = DonemProjesiAyar.DonemProjesiDersKodu.GetAyarDp(model.OgrenciInfo.ENSTITU_ID);
                                if (model.OgrenciInfo.OGRENIMSEVIYE_ID == OgrenimTipi.TezsizYuksekLisans.ToString() && !donemProjesiDersKodu.IsNullOrWhiteSpace())
                                {
                                    //Tezsiz yl için dönem projesi yürütücüsü bulunup danışman olarak atanıyor
                                    var donemProjesiDersi = model.TumDonemDersNotlari.FirstOrDefault(p =>
                                        p.DersKoduNum == donemProjesiDersKodu &&
                                        p.DonemId == aktifDonem.BaslangicYil + "" + aktifDonem.DonemId &&
                                        !p.HocaTc.IsNullOrWhiteSpace());
                                    if (donemProjesiDersi != null)
                                    {
                                        ogrenci.DANISMAN_TC1 = donemProjesiDersi.HocaTc;
                                        ogrenci.DANISMAN_AD_SOYAD1 = donemProjesiDersi.HocaAdi;
                                        ogrenci.DANISMAN_UNVAN1 = donemProjesiDersi.HocaUnvan;
                                    }
                                }
                            }

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
                model.HataMsj = "OBS sisteminden kayıt kontrolü başarısız oldu! Lütfen sistem yöneticisine başvurunuz!";
                SistemBilgilendirmeBus.SistemBilgisiKaydet(model.HataMsj + "\r\nHata:" + ex.ToExceptionMessage(), ex.ToExceptionStackTrace(), BilgiTipiEnum.Kritik);
            }

            if (model.OgrenciInfo == null) model.OgrenciInfo = new Ogrenci();
            if (model.OgrenciTez == null) model.OgrenciTez = new OgrenciTez();
            if (model.SonTezIzlemeBilgileri == null) model.SonTezIzlemeBilgileri = new TezIzlemeBilgileri();

            if (model.SonTezIzlemeBilgileri.tezIzljuribilgileri == null)
                model.SonTezIzlemeBilgileri.tezIzljuribilgileri = Array.Empty<TezIzlJuriBilgileri>();
            return model;
        }

        public List<string> ObsSutentList(List<string> ogrenciNos)
        {
            var returnogrenciNos = new List<string>();

            if (ogrenciNos == null) throw new Exception("ogrenciNos boş geliyor!");

            using (var service = new proliz_ytu_enstitu_minerSoapClient())
            {

                foreach (var ogrenciNo in ogrenciNos)
                {
                    var ogrencis = service.AktifOgrenciBilgiGetir(UserName, Password, ogrenciNo, null);
                    if (ogrencis.Any() && ogrencis[0].Sucess)
                        returnogrenciNos.Add(ogrenciNo);
                }
            }



            return returnogrenciNos;
        }
        public List<StudentControl> GetObsStudentControlX(string tcKimlikNo, string donemId)
        {
            var returnData = new List<StudentControl>();

            if (tcKimlikNo.IsNullOrWhiteSpace()) throw new Exception("tcOrOgrenciNo boş geliyor!");
            tcKimlikNo = tcKimlikNo.RemoveNonAlphanumeric();
            using (var service =
                   new proliz_ytu_enstitu_minerSoapClient())
            {

                var ogrencis = service.AktifOgrenciBilgiGetir(UserName, Password, null, tcKimlikNo);
                if (ogrencis.Any() && ogrencis[0].Sucess)
                {
                    foreach (var ogrenci in ogrencis[0].ogrenci)
                    {
                        var model = new StudentControl();


                        model.KayitVar = true;

                        if (!ogrenci.KAYIT_TARIHI.IsNullOrWhiteSpace())
                        {
                            model.KayitTarihi = ogrenci.KAYIT_TARIHI.ToDate().Value;
                            var parsedDonem = ogrenci.KAYITLI_DONEM.ParseObsDonem();
                            model.BaslangicYil = parsedDonem.BaslangicYil;
                            model.BitisYil = parsedDonem.BitisYil;
                            model.DonemID = parsedDonem.DonemNo;

                        }

                        model.OkuduguDonemNo = ogrenci.OKUDUGU_DNM_YENIKANUN.ToIntObj() ?? 0;
                        model.OgrenciInfo = ogrenci;

                        //öğrenim seviyesi ayarlaması obs öğrenim seviyelerini lubs öğrenim tip koduna çevir koduna çevir 
                        switch (model.OgrenciInfo.OGRENIMSEVIYE_ID)
                        {
                            case "2":
                                model.OgrenciInfo.OGRENIMSEVIYE_ID =
                                    OgrenimTipi.TezliYuksekLisans.ToString();
                                break;
                            case "3":
                                model.OgrenciInfo.OGRENIMSEVIYE_ID = OgrenimTipi.Doktra.ToString();
                                break;
                            case "4":
                                model.OgrenciInfo.OGRENIMSEVIYE_ID =
                                    OgrenimTipi.TezsizYuksekLisans.ToString();
                                break;
                            case "5":
                                model.OgrenciInfo.OGRENIMSEVIYE_ID =
                                    OgrenimTipi.SanattaYeterlilik.ToString();
                                break;
                            case "8":
                                model.OgrenciInfo.OGRENIMSEVIYE_ID =
                                    OgrenimTipi.ButunlesikDoktora.ToString();
                                break;
                        }


                        var ogrenciDersNots =
                            service.OgrenciDersNotBilgileriGetir(UserName, Password, ogrenci.OGR_NO, null);
                        if (ogrenciDersNots.Any() && ogrenciDersNots[0].Sucess)
                        {


                            model.TumDonemDersNotlari = ogrenciDersNots[0].ogrencidersnot.Select(s =>
                                new StudentDersNotModel
                                {
                                    DonemId = s.DONEM_ID,
                                    HocaTc = s.HOCA_TCK,
                                    HocaUnvan = s.HOCA_UNVAN,
                                    HocaAdi = s.HOCA_AD_SOYAD,
                                    DonemAd = s.DONEM_AD,
                                    DersAdi = s.DERS_AD,
                                    DersKodu = s.DERS_KOD,
                                    DersKoduNum = s.DERS_KOD.Length > 4 ? s.DERS_KOD.Substring(s.DERS_KOD.Length - 4, 4) : s.DERS_KOD,
                                    DersNotu = s.HARF_KOD,
                                    NotDeger = s.NOT_DEGER


                                }).ToList();

                            var aktifDonem = DateTime.Now.ToDonemProjesiDonemBilgi(model.OgrenciInfo.ENSTITU_ID);
                            var donemProjesiDersKodu = DonemProjesiAyar.DonemProjesiDersKodu.GetAyarDp(model.OgrenciInfo.ENSTITU_ID);
                            if (model.OgrenciInfo.OGRENIMSEVIYE_ID == OgrenimTipi.TezsizYuksekLisans.ToString() && !donemProjesiDersKodu.IsNullOrWhiteSpace())
                            {
                                //Tezsiz yl için dönem projesi yürütücüsü bulunup danışman olarak atanıyor
                                var donemProjesiDersi = model.TumDonemDersNotlari.FirstOrDefault(p =>
                                    p.DersKoduNum == donemProjesiDersKodu &&
                                    p.DonemId == aktifDonem.BaslangicYil + "" + aktifDonem.DonemId &&
                                    !p.HocaTc.IsNullOrWhiteSpace());
                                if (donemProjesiDersi != null)
                                {
                                    ogrenci.DANISMAN_TC1 = donemProjesiDersi.HocaTc;
                                    ogrenci.DANISMAN_AD_SOYAD1 = donemProjesiDersi.HocaAdi;
                                    ogrenci.DANISMAN_UNVAN1 = donemProjesiDersi.HocaUnvan;
                                }
                            }
                        }


                        returnData.Add(model);

                    }


                }
            }


            return returnData;
        }

        public class ProgramModel
        {
            public int OgrenimTipKod { get; set; }
            public int ProgramId { get; set; }
            public string ProgramAdi { get; set; }
        }
        public void GetAllStudent()
        {
            List<ProgramModel> programs = new List<ProgramModel>();
            using (var service =
                   new proliz_ytu_enstitu_minerSoapClient())
            {
                using (var entities = new LubsDbEntities())
                {
                    var kulls = entities.Kullanicilars.Where(p => p.YtuOgrencisi).Select(s => new { s.OgrenimTipKod, s.ProgramKod, s.TcKimlikNo }).ToList();
                    var kullTcsGroup = kulls.GroupBy(g => new { g.ProgramKod, g.OgrenimTipKod }).Select(s =>
                        new
                        {
                            s.Key.OgrenimTipKod,
                            s.Key.ProgramKod,
                            Tc = s.Select(s2 => s2.TcKimlikNo).FirstOrDefault()
                        }).ToList();
                    var tcNos = kullTcsGroup.Select(s => s.Tc).ToList();
                    foreach (var tc in tcNos)
                    {
                        var ogrencis = service.AktifOgrenciBilgiGetir(UserName, Password, "", tc);

                        foreach (var ogrenci in ogrencis.Where(p => p.Sucess).SelectMany(s => s.ogrenci))
                        {
                            if (programs.All(a => a.ProgramId != ogrenci.PROGRAM_ID.ToInt(0)))
                            {
                                programs.Add(new ProgramModel
                                {
                                    ProgramId = ogrenci.PROGRAM_ID.ToInt(0),
                                    OgrenimTipKod = ogrenci.OGRENIMSEVIYE_ID.ToOgrenimTipKod().Value,
                                    ProgramAdi = ogrenci.PROGRAM_AD
                                });
                            }
                        }
                    }
                }

                var jsonText = programs.ToJson();

            }
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
                        model.Ogrenci = ogrencis[0].ogrenci.OrderBy(p => p.OGRENIMSEVIYE_ID == "4" ? 2 : 1).FirstOrDefault();

                        var ogrenciDers = service.OgrenciDersBilgileriGetir(UserName, Password, model.Ogrenci?.OGR_NO, null, donemId);
                        if (ogrenciDers.Any() && ogrenciDers[0].Sucess)
                        {
                            model.OgrenciDersNot = ogrenciDers[0].ogrencidersnot[0];
                        }
                        var ogrenciDersNots = service.OgrenciDersNotBilgileriGetir(UserName, Password, model.Ogrenci?.OGR_NO, null);
                        if (ogrenciDersNots.Any() && ogrenciDersNots[0].Sucess)
                        {
                            model.OgrenciDersNotBilgis = ogrenciDersNots[0].ogrencidersnot.ToList();
                            // model.OgrenciDersNot = ogrenciDersNots[0].ogrencidersnot[0];
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