using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using LisansUstuBasvuruSistemi.Business;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.Helpers;
using LisansUstuBasvuruSistemi.Utilities.SystemSetting;
using LisansUstuBasvuruSistemi.WebServiceData.ObsService.ObsDtos;
using LisansUstuBasvuruSistemi.Ws_ObsService;
using Newtonsoft.Json;

namespace LisansUstuBasvuruSistemi.WebServiceData.ObsService
{
    public class ObsData
    {
        public string UserName => "ProEnsMiner";
        public string Password => "+!Pro*22Ytu!23#-Ens+!";
        public ObsStudentDto GetObsStudentControl(string tcKimlikNo)
        {
            var model = new ObsStudentDto();

            try
            {
                if (tcKimlikNo.IsNullOrWhiteSpace()) throw new Exception("Tc Kimlik Numarası boş geliyor!");
                model.TcKimlikNo = tcKimlikNo.RemoveNonAlphanumeric();
                using (var service = new proliz_ytu_enstitu_minerSoapClient())
                {
                    var ogrencis = service.AktifOgrenciBilgiGetir(UserName, Password, null, tcKimlikNo);
                    if (!ogrencis.Any() || !ogrencis[0].Sucess)
                    {
                        model.Hata = true;
                        model.HataMsj = ogrencis[0].Error;
                        return model;
                    }
                    model.KayitVar = true;
                    foreach (var obsOgrenci in ogrencis[0].ogrenci)
                    {
                        model.Ad = obsOgrenci.AD;
                        model.Soyad = obsOgrenci.SOYAD;
                        model.EPosta = obsOgrenci.E_POSTA1.IsNullOrWhiteSpace() ? obsOgrenci.E_POSTA2 : obsOgrenci.E_POSTA1;
                        model.CepTel = obsOgrenci.GSM1.IsNullOrWhiteSpace() ? obsOgrenci.GSM2 : obsOgrenci.GSM1;

                        var ogrenciProgramKayitDonem = obsOgrenci.KAYITLI_DONEM.ParseObsDonem();

                        var ogrenimDto = new ObsStudentOgrenimDto
                        {
                            OgrenciNo = obsOgrenci.OGR_NO,
                            EnstituAd = obsOgrenci.ENSTITU_AD,
                            EnstituKod = obsOgrenci.ENSTITU_ID.Length > 3 ? obsOgrenci.ENSTITU_ID.Remove(0, 1) : obsOgrenci.ENSTITU_ID,
                            KayitTarihi = obsOgrenci.KAYIT_TARIHI.ToDate().Value,
                            BaslangicYil = ogrenciProgramKayitDonem.BaslangicYil,
                            BitisYil = ogrenciProgramKayitDonem.BitisYil,
                            DonemId = ogrenciProgramKayitDonem.DonemNo,
                            OkuduguDonemNo = obsOgrenci.OKUDUGU_DNM_YENIKANUN.ToIntObj() ?? 0,
                            OgrenimTipKod = obsOgrenci.OGRENIMSEVIYE_ID.ToOgrenimTipKod().Value,
                            OgrenimSeviyeAdi = obsOgrenci.OGRENIMSEVIYE_AD,
                            AnabilimDaliId = obsOgrenci.ANABILIMDALI_ID,
                            AnabilimDaliAdi = obsOgrenci.ANABILIMDALI_AD,
                            ProgramId = obsOgrenci.PROGRAM_ID.ToInt().Value,
                            ProgramAdi = obsOgrenci.PROGRAM_AD
                        };

                        var ogrenciDersNotResult = service.OgrenciDersBilgileriGetir(UserName, Password, obsOgrenci.OGR_NO, null, null);

                        if (ogrenciDersNotResult[0].Sucess)
                        {
                            var ogrenciDersNotGenelBilgi = ogrenciDersNotResult[0].ogrencidersnot[0];
                            ogrenimDto.AktifDonemDersGenelInfo.ToplamKredi = ogrenciDersNotGenelBilgi.SON_KREDI.ToIntObj(0);
                            ogrenimDto.AktifDonemDersGenelInfo.ToplamAkts = ogrenciDersNotGenelBilgi.TOP_AKTS.ToDecimalObj().ToIntObj(0);
                            ogrenimDto.AktifDonemDersGenelInfo.Agno = ogrenciDersNotGenelBilgi.TOP_AKTS.ToDoubleObj() ?? 0;
                            ogrenimDto.AktifDonemDersGenelInfo.EtikDersNotu = ogrenciDersNotGenelBilgi.B_ETIK_DERS_NOTU;
                            ogrenimDto.AktifDonemDersGenelInfo.SeminerDersNotu = ogrenciDersNotGenelBilgi.SEMINER_DERS_NOTU;
                            ogrenimDto.AktifDonemDersGenelInfo.ZorunluDersSayisi = ogrenciDersNotGenelBilgi.ZORUN_DERS_SAYISI.ToIntObj() ?? 0;
                            ogrenimDto.AktifDonemDersGenelInfo.AbdDersSayisi = ogrenciDersNotGenelBilgi.ANABILIMDALI_DERS_SAYISI.ToIntObj() ?? 0;
                            var aktifDonemDersKodlaris = ogrenciDersNotGenelBilgi.AKTIF_DNM_DERS.Split(',').Where(p => !p.IsNullOrWhiteSpace()).ToList();

                            if (aktifDonemDersKodlaris.Any())
                            {
                                ogrenimDto.AktifDonemDersGenelInfo.DersKodus = aktifDonemDersKodlaris.Select(s => s).ToList();
                                ogrenimDto.AktifDonemDersGenelInfo.DersKodNums = aktifDonemDersKodlaris.Select(s => s.Substring(s.Length - 4, 4)).ToList();
                            }
                        }

                        var ogrenciTezBilgisiResult = service.OgrenciTezBilgileriGetir(UserName, Password, obsOgrenci.OGR_NO, null);
                        if (ogrenciTezBilgisiResult.Any())
                        {
                            var tezBilgi = ogrenciTezBilgisiResult[0];
                            if (tezBilgi.Sucess)
                            {

                                var sonTezBilgi = tezBilgi.ogrencitez.LastOrDefault();
                                if (sonTezBilgi != null)
                                {
                                    var ogrenciTezInfo = new ObsOgrenciTezInfoDto
                                    {
                                        TezId = sonTezBilgi.TEZ_ID.ToInt(0),
                                        IsTezDiliTr = sonTezBilgi.TEZ_DILI.ToLower().Contains("türkçe"),
                                        TezBasligi = sonTezBilgi.TEZ_BASLIK,
                                        TezBasligiEn = sonTezBilgi.TEZ_BASLIK_ENG,
                                        TezIzlemeSayisi = sonTezBilgi.TIZL_SAYI.ToInt(0)
                                    };

                                    var sonTezIzlemeBilgiler = sonTezBilgi.tezizlemebilgileri.OrderByDescending(o => o.TEZ_IZL_SIRA.ToIntObj()).FirstOrDefault();
                                    if (sonTezIzlemeBilgiler != null)
                                    {
                                        ogrenciTezInfo.SonTezIzlemeInfo = new ObsStudentTezIzlemeDto
                                        {
                                            Id = sonTezIzlemeBilgiler.TEZ_IZL_ID.ToInt(0),
                                            Sira = sonTezIzlemeBilgiler.TEZ_IZL_SIRA.ToInt(0),
                                            Tarih = sonTezIzlemeBilgiler.TEZ_IZL_TARIH.ToDate().Value,
                                            DonemAdi = sonTezIzlemeBilgiler.TEZ_IZL_DONEM,
                                            Durum = sonTezIzlemeBilgiler.TEZ_IZL_DURUM
                                        };
                                        var tezJuri = service.OgrenciTezizlemeJuriBilgileriGetir(UserName, Password, obsOgrenci.OGR_NO, null);
                                        if (tezJuri[0].Sucess)
                                        {
                                            ogrenciTezInfo.SonTezIzlemeInfo.TezIzlemeJurileri = tezJuri[0]
                                                .tezIzljuribilgileri.Select(s => new ObsStudentTezIzlemeJuriDto
                                                {
                                                    AdSoyad = s.TEZ_IZLEME_JURI_ADSOY,
                                                    UnvanAdi = s.TEZ_IZLEME_JURI_UNVAN,
                                                    UnvanKod = s.TEZ_IZLEME_JURI_UNVANKOD,
                                                    UniversiteAdi = s.TEZ_IZLEME_JURI_UNIVER,
                                                    EPosta = s.TEZ_IZLEME_JURI_EPOSTA,
                                                    AnabilimDaliAdi = s.TEZ_IZLEME_JURI_ANABLMDAL,
                                                    CepTel = s.TEZ_IZLEME_JURI_GSM,

                                                }).ToList();
                                        }
                                    }
                                } 
                            } 
                        } 
                        var ogrenciDersNots = service.OgrenciDersNotBilgileriGetir(UserName, Password, obsOgrenci.OGR_NO, null);
                        if (ogrenciDersNots.Any() && ogrenciDersNots[0].Sucess)
                        {


                            ogrenimDto.DersNotlari = ogrenciDersNots[0].ogrencidersnot.Select(s =>
                                new ObsStudentDersNotDto
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

                            var aktifDonem = DateTime.Now.ToDonemProjesiDonemBilgi(ogrenimDto.EnstituKod);
                            var donemProjesiDersKodu = DonemProjesiAyar.DonemProjesiDersKodu.GetAyar(ogrenimDto.EnstituKod);
                            if (ogrenimDto.OgrenimTipKod == OgrenimTipi.TezsizYuksekLisans && !donemProjesiDersKodu.IsNullOrWhiteSpace())
                            {
                                //Tezsiz yl için dönem projesi yürütücüsü bulunup danışman olarak atanıyor
                                var donemProjesiDersi = ogrenimDto.DersNotlari.FirstOrDefault(p =>
                                    p.DersKoduNum == donemProjesiDersKodu &&
                                    p.DonemId == aktifDonem.BaslangicYil + "" + aktifDonem.DonemId &&
                                    !p.HocaTc.IsNullOrWhiteSpace());
                                if (donemProjesiDersi != null)
                                {
                                    obsOgrenci.DANISMAN_TC1 = donemProjesiDersi.HocaTc;
                                    obsOgrenci.DANISMAN_AD_SOYAD1 = donemProjesiDersi.HocaAdi;
                                    obsOgrenci.DANISMAN_UNVAN1 = donemProjesiDersi.HocaUnvan;
                                }
                            }

                        }

                        if (!obsOgrenci.DANISMAN_TC1.IsNullOrWhiteSpace())
                        {
                            var danismanResult = service.AkademikPersonelBilgiGetir(UserName, Password, null, obsOgrenci.DANISMAN_TC1);
                            if (danismanResult.Any())
                            {
                                var danismanBilgi = danismanResult[0];
                                if (danismanBilgi.Sucess)
                                {
                                    ogrenimDto.DanismanInfo = danismanBilgi.personel.Select(s => new ObsStudentDanismanInfo
                                    {
                                        TcKimlikNo = s.TCKIMLIKNO,
                                        SicilNo = s.SICIL_NO,
                                        Ad = s.AD,
                                        Soyad = s.SOYAD,
                                        UnvanAdi = s.UNVAN_AD,
                                        UnvanId = s.UNVAN_ID.ToInt(0),
                                        EPosta = s.E_POSTA1,
                                        Ceptel = s.GSM1,
                                        YlOgrenciSayisiDanismanlik = s.DANISMAN_OLUNAN_YL_SAYI1.ToInt(0),
                                        YlOgrenciSayisiMezunOlan = s.DANISMAN_MEZUN_YL_SAYI1.ToInt(0),
                                        DrOgrenciSayisiDanismanlik = s.DANISMAN_OLUNAN_DR_SAYI1.ToInt(0),
                                        DrOgrenciSayisiMezunOlan = s.DANISMAN_MEZUN_DR_SAYI1.ToInt(0),
                                    }).FirstOrDefault();
                                }
                            }

                        }

                        var ogrenciYeterResult = service.OgrenciDrYeterBilgileriGetir(UserName, Password, obsOgrenci.OGR_NO, null);
                        if (ogrenciYeterResult[0].Sucess)
                            ogrenimDto.YeterlikInfos = ogrenciYeterResult[0].ogrenciyeter.Select(s =>
                                new ObsStudentYeterlikDto
                                {
                                    SozluSinavTarihi = s.DR_YET_SOZ_SNV_TARIH.ToDate(),
                                    SozluSinavYeri = s.DR_YET_SOZ_SNV_YERI,
                                    SozluSinavDurumu = s.DR_YET_SOZ_SNV_DURUM,
                                    YaziliSinavTarihi = s.DR_YET_YAZ_SNV_TARIH.ToDate(),
                                    YaziliSinavYeri = s.DR_YET_YAZ_SNV_YERI,
                                    YaziliSinavDurumu = s.DR_YET_YAZ_SNV_DURUM,
                                    GenelSinavDurum = s.DR_YET_GNL_SNV_DURUM

                                }).ToList();

                        model.AktifOgrenimler.Add(ogrenimDto);
                    }

                }

            }
            catch (Exception ex)
            {
                model.Hata = true;
                model.HataMsj = "OBS sisteminden kayıt kontrolü başarısız oldu! Lütfen sistem yöneticisine başvurunuz!";
                SistemBilgilendirmeBus.SistemBilgisiKaydet(model.HataMsj + "\r\nHata:" + ex.ToExceptionMessage(), ex.ToExceptionStackTrace(), BilgiTipiEnum.Kritik);
            }
            return model;
        }

        
      
    }
}