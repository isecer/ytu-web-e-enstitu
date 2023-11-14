using System;
using System.Collections.Generic;
using System.Linq;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.Helpers;

namespace LisansUstuBasvuruSistemi.Raporlar.Mezuniyet
{
    public partial class RprMezuniyetTezTeslimFormu_FR0338 : DevExpress.XtraReports.UI.XtraReport
    {
        public RprMezuniyetTezTeslimFormu_FR0338(Guid rowId, bool ilkTeslimOrIkinciTeslim = true)
        {
            InitializeComponent();

            using (var db = new LisansustuBasvuruSistemiEntities())
            {

                //4   tr Ulusal Makale 
                //5   tr Uluslararası Makale 
                //6   tr Patent 
                //7   tr Proje 
                //2   tr Ulusal Bildiri 
                //3   tr Uluslararası Bildiri 




                var mBasvuru = db.MezuniyetBasvurularis.Where(p => p.RowID == rowId).First();
                var enstL = mBasvuru.MezuniyetSureci.Enstituler;
                var prgL = mBasvuru.Programlar;
                var abdL = mBasvuru.Programlar.AnabilimDallari;
                var os = db.OgrenimTipleris.First(p => p.EnstituKod == enstL.EnstituKod && p.OgrenimTipKod == mBasvuru.OgrenimTipKod);
                cellOgrenciNo.Text = mBasvuru.OgrenciNo;
                cellOgrenciAdSoyad.Text = mBasvuru.Ad + " " + mBasvuru.Soyad;
                cellOgrenciEnstituAdi.Text = enstL.EnstituAd;
                cellOgrenciAnabilimDaliAdi.Text = abdL.AnabilimDaliAdi;
                cellOgrenciProgramAdi.Text = prgL.ProgramAdi;
                cellOgrenciOgrenimSeviyesi.Text = os.OgrenimTipAdi;
                cellOgrenciKayitDonemi.Text = mBasvuru.KayitOgretimYiliBaslangic + " - " + (mBasvuru.KayitOgretimYiliBaslangic + 1) + " / " + (mBasvuru.KayitOgretimYiliDonemID == 1 ? "Güz" : "Bahar") + " (" + (mBasvuru.KayitOgretimYiliDonemID == 1 ? "Fall" : "Spring") + ")";

                cellImzaOgrenciAdSoyad.Text = mBasvuru.Ad + " " + mBasvuru.Soyad;


                cellImzaDanismanAdSoyad.Text = mBasvuru.TezDanismanUnvani + " " + mBasvuru.TezDanismanAdi;
                if (ilkTeslimOrIkinciTeslim)
                {
                    chkIlkTeslim.Checked = true;
                    cellTezDili.Text = mBasvuru.IsTezDiliTr == true ? "Türkçe" : "English";
                    cellTezBaslikTr.Text = mBasvuru.TezBaslikTr;
                    cellTezBaslikEn.Text = mBasvuru.TezBaslikEn;
                    cellImzaOgrenciTarih.Text = mBasvuru.BasvuruTarihi.ToFormatDateAndTime();
                    cellImzaDanismanTarih.Text = mBasvuru.DanismanOnayTarihi.ToFormatDateAndTime();
                }
                else
                {
                    var joForm = mBasvuru.MezuniyetJuriOneriFormlaris.First();
                    chkIkinciTeslim.Checked = true;
                    var srTalebi = mBasvuru.SRTalepleris.First(p => p.JuriSonucMezuniyetSinavDurumID == MezuniyetSinavDurumEnum.Uzatma);
                    cellTezDili.Text = mBasvuru.IsTezDiliTr == true ? "Türkçe" : "English";

                    var tezBasligi = "";
                    var tezBasligiCeviri = "";

                    if (srTalebi.IsTezBasligiDegisti == true)
                    {
                        tezBasligi = mBasvuru.IsTezDiliTr == true ? srTalebi.YeniTezBaslikTr : srTalebi.YeniTezBaslikEn;
                        tezBasligiCeviri = mBasvuru.IsTezDiliTr == false ? srTalebi.YeniTezBaslikTr : srTalebi.YeniTezBaslikEn;
                    }
                    else if (joForm.IsTezBasligiDegisti == true)
                    {
                        tezBasligi = mBasvuru.IsTezDiliTr == true ? joForm.YeniTezBaslikTr : joForm.YeniTezBaslikEn;
                        tezBasligiCeviri = mBasvuru.IsTezDiliTr == false ? joForm.YeniTezBaslikTr : joForm.YeniTezBaslikEn;
                    }
                    else
                    {
                        tezBasligi = mBasvuru.IsTezDiliTr == true ? mBasvuru.TezBaslikTr : mBasvuru.TezBaslikEn;
                        tezBasligiCeviri = mBasvuru.IsTezDiliTr == false ? mBasvuru.TezBaslikEn : mBasvuru.TezBaslikTr;
                    }

                    cellTezBaslikTr.Text = tezBasligi;
                    cellTezBaslikEn.Text = tezBasligiCeviri;
                    cellImzaOgrenciTarih.Text = srTalebi.OgrenciOnayTarihi.ToFormatDateAndTime();
                    cellImzaDanismanTarih.Text = srTalebi.DanismanOnayTarihi.ToFormatDateAndTime();
                }




                var yayinlar = mBasvuru.MezuniyetBasvurulariYayins.ToList();
                chkUluslararasiMakale.Checked = yayinlar.Any(a => a.MezuniyetYayinTurID == 5);
                chkUlusalMakale.Checked = yayinlar.Any(a => a.MezuniyetYayinTurID == 4);


                var ulusalOrUluslararasiMakale = yayinlar.FirstOrDefault(p => new List<int> { 4, 5 }.Contains(p.MezuniyetYayinTurID));
                if (ulusalOrUluslararasiMakale != null)
                {
                    cellYazarlarMakale.Text = ulusalOrUluslararasiMakale.YazarAdi;
                    cellMakaleBasligiMakale.Text = ulusalOrUluslararasiMakale.YayinBasligi;
                    cellDergiAdiMakale.Text = ulusalOrUluslararasiMakale.DergiAdi;
                    cellYilCiltSayiMakale.Text = ulusalOrUluslararasiMakale.YilCiltSayiSS;
                    cellDoiISSNMakale.Text = ulusalOrUluslararasiMakale.MezuniyetYayinLinki;

                    if (ulusalOrUluslararasiMakale.MezuniyetYayinTurID == 5)
                    {
                        chkYayinOnayUluslararasiMakale.Checked = true;
                        if (ulusalOrUluslararasiMakale.MezuniyetYayinIndexTurleri != null) cellUluslararasiMakaleIndex.Text = ulusalOrUluslararasiMakale.MezuniyetYayinIndexTurleri.IndexTurAdi;

                        if (ulusalOrUluslararasiMakale.DanismanIsmiVar == true) chkYayinOnayUluslararasiMakaleDanismanVar.Checked = true;
                        else chkYayinOnayUluslararasiMakaleDanismanYok.Checked = true;
                        if (ulusalOrUluslararasiMakale.TezIcerikUyumuVar == true) chkYayinOnayUluslararasiMakaleTezIcerikUygun.Checked = true;
                        else chkYayinOnayUluslararasiMakaleTezIcerikUygunDegil.Checked = true;

                    }
                    else
                    {
                        chkYayinOnayUlusalMakale.Checked = true;
                        if (ulusalOrUluslararasiMakale.MezuniyetYayinIndexTurleri != null) cellUlusalMakaleIndex.Text = ulusalOrUluslararasiMakale.MezuniyetYayinIndexTurleri.IndexTurAdi;

                        if (ulusalOrUluslararasiMakale.DanismanIsmiVar == true) chkYayinOnayUlusalMakaleDanismanVar.Checked = true;
                        else chkYayinOnayUlusalMakaleDanismanYok.Checked = true;
                        if (ulusalOrUluslararasiMakale.TezIcerikUyumuVar == true) chkYayinOnayUlusalMakaleTezIcerikUygun.Checked = true;
                        else chkYayinOnayUlusalMakaleTezIcerikUygunDegil.Checked = true;
                    }
                }

                chkUluslararasiBildiri.Checked = yayinlar.Any(a => a.MezuniyetYayinTurID == 3);
                chkUlusalBildiri.Checked = yayinlar.Any(a => a.MezuniyetYayinTurID == 2);

                var ulusalOrUluslararasiBildiri = yayinlar.FirstOrDefault(p => new List<int> { 2, 3 }.Contains(p.MezuniyetYayinTurID));
                if (ulusalOrUluslararasiBildiri != null)
                {
                    cellYazarlarBildiri.Text = ulusalOrUluslararasiBildiri.YazarAdi;
                    cellMakaleBasligiBildiri.Text = ulusalOrUluslararasiBildiri.YayinBasligi;
                    cellEtkinlikAdiBildiri.Text = ulusalOrUluslararasiBildiri.EtkinlikAdi;
                    cellTarihBildiri.Text = ulusalOrUluslararasiBildiri.MezuniyetYayinTarih.ToFormatDate();
                    cellYerBildiri.Text = ulusalOrUluslararasiBildiri.YerBilgisi;
                    cellWebSayfasi.Text = ulusalOrUluslararasiBildiri.MezuniyetYayinKaynakLinki;

                    if (ulusalOrUluslararasiBildiri.MezuniyetYayinTurID == 3)
                    {
                        chkYayinOnayUluslararasiBildiri.Checked = true;

                        if (ulusalOrUluslararasiBildiri.DanismanIsmiVar == true) chkYayinOnayUluslararasiBildiriDanismanVar.Checked = true;
                        else chkYayinOnayUluslararasiMakaleDanismanYok.Checked = true;
                        if (ulusalOrUluslararasiBildiri.TezIcerikUyumuVar == true) chkYayinOnayUluslararasiBildiriTezIcerikUygun.Checked = true;
                        else chkYayinOnayUluslararasiBildiriTezIcerikUygunDegil.Checked = true;

                    }
                    else
                    {

                        chkYayinOnayUlusalBildiri.Checked = true;

                        if (ulusalOrUluslararasiBildiri.DanismanIsmiVar == true) chkYayinOnayUlusalBildiriDanismanVar.Checked = true;
                        else chkYayinOnayUlusalBildiriDanismanYok.Checked = true;
                        if (ulusalOrUluslararasiBildiri.TezIcerikUyumuVar == true) chkYayinOnayUlusalBildiriTezIcerikUygun.Checked = true;
                        else chkYayinOnayUlusalBildiriTezIcerikUygunDegil.Checked = true;
                    }
                }
                var proje = yayinlar.FirstOrDefault(a => a.MezuniyetYayinTurID == 7);
                if (proje != null)
                {

                    //1   TÜBİTAK
                    //2   BAP
                    //3   DİĞER
                    chkTubitakProje.Checked = proje.MezuniyetYayinProjeTurID == 1;
                    chkBapProje.Checked = proje.MezuniyetYayinProjeTurID == 2;
                    chkDigerProje.Checked = proje.MezuniyetYayinProjeTurID == 3;

                    cellProjeEkibiProje.Text = proje.ProjeEkibi;
                    cellProjeBasligiProje.Text = proje.YayinBasligi;
                    cellTarihAraligiProje.Text = proje.TarihAraligi;
                    cellMevcutDurumProje.Text = proje.IsProjeTamamlandiOrDevamEdiyor == true ? "Tamamlandı" : "Devam ediyor ve en az bir ara rapor teslim edildi";
                    cellDestKurulusProje.Text = proje.ProjeDeatKurulus;

                    if (proje.MezuniyetYayinTurID == 3)
                    {
                        cellUlusalProjeIndex.Text = "";

                        chkYayinOnayUluslararasiProje.Checked = true;
                        if (proje.DanismanIsmiVar == true) chkYayinOnayUluslararasiProjeDanismanVar.Checked = true;
                        else chkYayinOnayUluslararasiMakaleDanismanYok.Checked = true;
                        if (proje.TezIcerikUyumuVar == true) chkYayinOnayUluslararasiProjeTezIcerikUygun.Checked = true;
                        else chkYayinOnayUluslararasiProjeTezIcerikUygunDegil.Checked = true;

                    }
                    else
                    {
                        cellUlusalProjeIndex.Text = "";

                        chkYayinOnayUlusalProje.Checked = true;
                        if (proje.DanismanIsmiVar == true) chkYayinOnayUlusalProjeDanismanVar.Checked = true;
                        else chkYayinOnayUlusalProjeDanismanYok.Checked = true;
                        if (proje.TezIcerikUyumuVar == true) chkYayinOnayUlusalProjeTezIcerikUygun.Checked = true;
                        else chkYayinOnayUlusalProjeTezIcerikUygunDegil.Checked = true;
                    }
                }




                cellFormKodu.Text = "Form Kodu: " + mBasvuru.TezTeslimFormKodu;
                var qrUlr = enstL.SistemErisimAdresi + "/DosyaKontrol/Index?Kod=" + "MZTTF_" + mBasvuru.MezuniyetBasvurulariID + "_" + mBasvuru.TezTeslimUniqueID + "_" + (ilkTeslimOrIkinciTeslim ? 1 : 2);
                ImgQRCode.ImageUrl = qrUlr;
                ImgQRCode.Image = qrUlr.CreateQrCode();


                this.DisplayName = (mBasvuru.Ad + " " + mBasvuru.Soyad) + " FR-0338 Mezuniyet Tez Teslin Formu";

            }

            this.IlkTeslimOrIkinciTeslim = ilkTeslimOrIkinciTeslim;
        }

        public bool IlkTeslimOrIkinciTeslim { get; }
    }
}
