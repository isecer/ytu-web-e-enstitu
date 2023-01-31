using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using DevExpress.XtraReports.UI;
using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using BiskaUtil;
using System.Linq;
using System.Collections.Generic;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.Extensions;

namespace LisansUstuBasvuruSistemi.Raporlar
{
    public partial class rprMezuniyetTezTeslimFormu_FR0338 : DevExpress.XtraReports.UI.XtraReport
    {
        public rprMezuniyetTezTeslimFormu_FR0338(Guid RowID, bool IlkTeslimOrIkinciTeslim = true)
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




                var MBasvuru = db.MezuniyetBasvurularis.Where(p => p.RowID == RowID).First();
                var EnstL = MBasvuru.MezuniyetSureci.Enstituler;
                var PrgL = MBasvuru.Programlar;
                var AbdL = MBasvuru.Programlar.AnabilimDallari;
                var Os = db.OgrenimTipleris.Where(p => p.EnstituKod == EnstL.EnstituKod && p.OgrenimTipKod == MBasvuru.OgrenimTipKod).First();
                cellOgrenciNo.Text = MBasvuru.OgrenciNo;
                cellOgrenciAdSoyad.Text = MBasvuru.Ad + " " + MBasvuru.Soyad;
                cellOgrenciEnstituAdi.Text = EnstL.EnstituAd;
                cellOgrenciAnabilimDaliAdi.Text = AbdL.AnabilimDaliAdi;
                cellOgrenciProgramAdi.Text = PrgL.ProgramAdi;
                cellOgrenciOgrenimSeviyesi.Text = Os.OgrenimTipAdi;
                cellOgrenciKayitDonemi.Text = MBasvuru.KayitOgretimYiliBaslangic + " - " + (MBasvuru.KayitOgretimYiliBaslangic + 1) + " / " + (MBasvuru.KayitOgretimYiliDonemID == 1 ? "Güz" : "Bahar") + " (" + (MBasvuru.KayitOgretimYiliDonemID == 1 ? "Fall" : "Spring") + ")";

                cellImzaOgrenciAdSoyad.Text = MBasvuru.Ad + " " + MBasvuru.Soyad;


                cellImzaDanismanAdSoyad.Text = MBasvuru.TezDanismanUnvani + " " + MBasvuru.TezDanismanAdi;
                if (IlkTeslimOrIkinciTeslim)
                {
                    chkIlkTeslim.Checked = true;
                    cellTezDili.Text = MBasvuru.IsTezDiliTr == true ? "Türkçe" : "English";
                    cellTezBaslikTr.Text = MBasvuru.TezBaslikTr;
                    cellTezBaslikEn.Text = MBasvuru.TezBaslikEn;
                    cellImzaOgrenciTarih.Text = MBasvuru.BasvuruTarihi.ToFormatDateAndTime();
                    cellImzaDanismanTarih.Text = MBasvuru.DanismanOnayTarihi.ToFormatDateAndTime();
                }
                else
                {
                    var JoForm = MBasvuru.MezuniyetJuriOneriFormlaris.First();
                    chkIkinciTeslim.Checked = true;
                    var SrTalebi = MBasvuru.SRTalepleris.Where(p => p.JuriSonucMezuniyetSinavDurumID == MezuniyetSinavDurum.Uzatma).First();
                    cellTezDili.Text = MBasvuru.IsTezDiliTr == true ? "Türkçe" : "English";

                    var tezBasligi = "";
                    var tezBasligiCeviri = "";

                    if (SrTalebi.IsTezBasligiDegisti == true)
                    {
                        tezBasligi = MBasvuru.IsTezDiliTr == true ? SrTalebi.YeniTezBaslikTr : SrTalebi.YeniTezBaslikEn;
                        tezBasligiCeviri = MBasvuru.IsTezDiliTr == false ? SrTalebi.YeniTezBaslikTr : SrTalebi.YeniTezBaslikEn;
                    }
                    else if (JoForm.IsTezBasligiDegisti == true)
                    {
                        tezBasligi = MBasvuru.IsTezDiliTr == true ? JoForm.YeniTezBaslikTr : JoForm.YeniTezBaslikEn;
                        tezBasligiCeviri = MBasvuru.IsTezDiliTr == false ? JoForm.YeniTezBaslikTr : JoForm.YeniTezBaslikEn;
                    }
                    else
                    {
                        tezBasligi = MBasvuru.IsTezDiliTr == true ? MBasvuru.TezBaslikTr : MBasvuru.TezBaslikEn;
                        tezBasligiCeviri = MBasvuru.IsTezDiliTr == false ? MBasvuru.TezBaslikEn : MBasvuru.TezBaslikTr;
                    }

                    cellTezBaslikTr.Text = tezBasligi;
                    cellTezBaslikEn.Text = tezBasligiCeviri;
                    cellImzaOgrenciTarih.Text = SrTalebi.OgrenciOnayTarihi.ToFormatDateAndTime();
                    cellImzaDanismanTarih.Text = SrTalebi.DanismanOnayTarihi.ToFormatDateAndTime();
                }




                var Yayinlar = MBasvuru.MezuniyetBasvurulariYayins.ToList();
                chkUluslararasiMakale.Checked = Yayinlar.Any(a => a.MezuniyetYayinTurID == 5);
                chkUlusalMakale.Checked = Yayinlar.Any(a => a.MezuniyetYayinTurID == 4);


                var UlusalOrUluslararasiMakale = Yayinlar.Where(p => new List<int> { 4, 5 }.Contains(p.MezuniyetYayinTurID)).FirstOrDefault();
                if (UlusalOrUluslararasiMakale != null)
                {
                    cellYazarlarMakale.Text = UlusalOrUluslararasiMakale.YazarAdi;
                    cellMakaleBasligiMakale.Text = UlusalOrUluslararasiMakale.YayinBasligi;
                    cellDergiAdiMakale.Text = UlusalOrUluslararasiMakale.DergiAdi;
                    cellYilCiltSayiMakale.Text = UlusalOrUluslararasiMakale.YilCiltSayiSS;
                    cellDoiISSNMakale.Text = UlusalOrUluslararasiMakale.MezuniyetYayinLinki;

                    if (UlusalOrUluslararasiMakale.MezuniyetYayinTurID == 5)
                    {
                        chkYayinOnayUluslararasiMakale.Checked = true;
                        if (UlusalOrUluslararasiMakale.MezuniyetYayinIndexTurleri != null) cellUluslararasiMakaleIndex.Text = UlusalOrUluslararasiMakale.MezuniyetYayinIndexTurleri.IndexTurAdi;

                        if (UlusalOrUluslararasiMakale.DanismanIsmiVar == true) chkYayinOnayUluslararasiMakaleDanismanVar.Checked = true;
                        else chkYayinOnayUluslararasiMakaleDanismanYok.Checked = true;
                        if (UlusalOrUluslararasiMakale.TezIcerikUyumuVar == true) chkYayinOnayUluslararasiMakaleTezIcerikUygun.Checked = true;
                        else chkYayinOnayUluslararasiMakaleTezIcerikUygunDegil.Checked = true;

                    }
                    else
                    {
                        chkYayinOnayUlusalMakale.Checked = true;
                        if (UlusalOrUluslararasiMakale.MezuniyetYayinIndexTurleri != null) cellUlusalMakaleIndex.Text = UlusalOrUluslararasiMakale.MezuniyetYayinIndexTurleri.IndexTurAdi;

                        if (UlusalOrUluslararasiMakale.DanismanIsmiVar == true) chkYayinOnayUlusalMakaleDanismanVar.Checked = true;
                        else chkYayinOnayUlusalMakaleDanismanYok.Checked = true;
                        if (UlusalOrUluslararasiMakale.TezIcerikUyumuVar == true) chkYayinOnayUlusalMakaleTezIcerikUygun.Checked = true;
                        else chkYayinOnayUlusalMakaleTezIcerikUygunDegil.Checked = true;
                    }
                }

                chkUluslararasiBildiri.Checked = Yayinlar.Any(a => a.MezuniyetYayinTurID == 3);
                chkUlusalBildiri.Checked = Yayinlar.Any(a => a.MezuniyetYayinTurID == 2);

                var UlusalOrUluslararasiBildiri = Yayinlar.Where(p => new List<int> { 2, 3 }.Contains(p.MezuniyetYayinTurID)).FirstOrDefault();
                if (UlusalOrUluslararasiBildiri != null)
                {
                    cellYazarlarBildiri.Text = UlusalOrUluslararasiBildiri.YazarAdi;
                    cellMakaleBasligiBildiri.Text = UlusalOrUluslararasiBildiri.YayinBasligi;
                    cellEtkinlikAdiBildiri.Text = UlusalOrUluslararasiBildiri.EtkinlikAdi;
                    cellTarihBildiri.Text = UlusalOrUluslararasiBildiri.MezuniyetYayinTarih.ToDateString();
                    cellYerBildiri.Text = UlusalOrUluslararasiBildiri.YerBilgisi;
                    cellWebSayfasi.Text = UlusalOrUluslararasiBildiri.MezuniyetYayinKaynakLinki;

                    if (UlusalOrUluslararasiBildiri.MezuniyetYayinTurID == 3)
                    {
                        chkYayinOnayUluslararasiBildiri.Checked = true;

                        if (UlusalOrUluslararasiBildiri.DanismanIsmiVar == true) chkYayinOnayUluslararasiBildiriDanismanVar.Checked = true;
                        else chkYayinOnayUluslararasiMakaleDanismanYok.Checked = true;
                        if (UlusalOrUluslararasiBildiri.TezIcerikUyumuVar == true) chkYayinOnayUluslararasiBildiriTezIcerikUygun.Checked = true;
                        else chkYayinOnayUluslararasiBildiriTezIcerikUygunDegil.Checked = true;

                    }
                    else
                    {

                        chkYayinOnayUlusalBildiri.Checked = true;

                        if (UlusalOrUluslararasiBildiri.DanismanIsmiVar == true) chkYayinOnayUlusalBildiriDanismanVar.Checked = true;
                        else chkYayinOnayUlusalBildiriDanismanYok.Checked = true;
                        if (UlusalOrUluslararasiBildiri.TezIcerikUyumuVar == true) chkYayinOnayUlusalBildiriTezIcerikUygun.Checked = true;
                        else chkYayinOnayUlusalBildiriTezIcerikUygunDegil.Checked = true;
                    }
                }
                var Proje = Yayinlar.Where(a => a.MezuniyetYayinTurID == 7).FirstOrDefault();
                if (Proje != null)
                {

                    //1   TÜBİTAK
                    //2   BAP
                    //3   DİĞER
                    chkTubitakProje.Checked = Proje.MezuniyetYayinProjeTurID == 1;
                    chkBapProje.Checked = Proje.MezuniyetYayinProjeTurID == 2;
                    chkDigerProje.Checked = Proje.MezuniyetYayinProjeTurID == 3;

                    cellProjeEkibiProje.Text = Proje.ProjeEkibi;
                    cellProjeBasligiProje.Text = Proje.YayinBasligi;
                    cellTarihAraligiProje.Text = Proje.TarihAraligi;
                    cellMevcutDurumProje.Text = Proje.IsProjeTamamlandiOrDevamEdiyor == true ? "Tamamlandı" : "Devam ediyor ve en az bir ara rapor teslim edildi";
                    cellDestKurulusProje.Text = Proje.ProjeDeatKurulus;

                    if (Proje.MezuniyetYayinTurID == 3)
                    {
                        cellUlusalProjeIndex.Text = "";

                        chkYayinOnayUluslararasiProje.Checked = true;
                        if (Proje.DanismanIsmiVar == true) chkYayinOnayUluslararasiProjeDanismanVar.Checked = true;
                        else chkYayinOnayUluslararasiMakaleDanismanYok.Checked = true;
                        if (Proje.TezIcerikUyumuVar == true) chkYayinOnayUluslararasiProjeTezIcerikUygun.Checked = true;
                        else chkYayinOnayUluslararasiProjeTezIcerikUygunDegil.Checked = true;

                    }
                    else
                    {
                        cellUlusalProjeIndex.Text = "";

                        chkYayinOnayUlusalProje.Checked = true;
                        if (Proje.DanismanIsmiVar == true) chkYayinOnayUlusalProjeDanismanVar.Checked = true;
                        else chkYayinOnayUlusalProjeDanismanYok.Checked = true;
                        if (Proje.TezIcerikUyumuVar == true) chkYayinOnayUlusalProjeTezIcerikUygun.Checked = true;
                        else chkYayinOnayUlusalProjeTezIcerikUygunDegil.Checked = true;
                    }
                }




                cellFormKodu.Text = "Form Kodu: " + MBasvuru.TezTeslimFormKodu;
                var qrUlr = EnstL.SistemErisimAdresi + "/DosyaKontrol/Index?Kod=" + "MZTTF_" + MBasvuru.MezuniyetBasvurulariID + "_" + MBasvuru.TezTeslimUniqueID + "_" + (IlkTeslimOrIkinciTeslim ? 1 : 2);
                ImgQRCode.ImageUrl = qrUlr;
                ImgQRCode.Image = qrUlr.CreateQrCode();


                this.DisplayName = (MBasvuru.Ad + " " + MBasvuru.Soyad) + " FR-0338 Mezuniyet Tez Teslin Formu";

            }

            this.IlkTeslimOrIkinciTeslim = IlkTeslimOrIkinciTeslim;
        }

        public bool IlkTeslimOrIkinciTeslim { get; }
    }
}
