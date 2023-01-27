using DevExpress.XtraReports.UI;
using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Models.FilterModel;
using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Linq;


namespace LisansUstuBasvuruSistemi.Raporlar
{
    public partial class rprTezDanismaniDegisiklikFormu_FR0308 : DevExpress.XtraReports.UI.XtraReport
    {
        public rprTezDanismaniDegisiklikFormu_FR0308(int id)
        {
            InitializeComponent();

            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                this.DisplayName = "FR-0308 TEZ DANIŞMANI KONU DİL DEĞİŞİKLİK FORMU";

                var basvuru = db.TDOBasvuruDanismen.First(p => p.TDOBasvuruDanismanID == id);


                var q = (from s in db.TDOBasvuruDanismen.Where(p => p.TDOBasvuruDanismanID == id)
                         join b in db.TDOBasvurus on s.TDOBasvuruID equals b.TDOBasvuruID
                         join k in db.Kullanicilars on b.KullaniciID equals k.KullaniciID
                         join e in db.Enstitulers on b.EnstituKod equals e.EnstituKod
                         join prg in db.Programlars on b.ProgramKod equals prg.ProgramKod
                         join abd in db.AnabilimDallaris on prg.AnabilimDaliKod equals abd.AnabilimDaliKod
                         join ot in db.OgrenimTipleris on new { b.OgrenimTipKod, b.EnstituKod } equals new { ot.OgrenimTipKod, ot.EnstituKod }
                         select new
                         {
                             s.TDODanismanTalepTipID,
                             isDrBasvurusu = b.OgrenimTipKod == OgrenimTipi.Doktra || b.OgrenimTipKod == OgrenimTipi.ButunlesikDoktora,
                             s.BasvuruTarihi,
                             s.FormKodu,
                             urlAdd = e.SistemErisimAdresi + "/DosyaKontrol/Index?Kod=" + "TDOF_" + s.TDOBasvuruDanismanID + "_" + s.UniqueID,
                             s.UniqueID,
                             b.OgrenciNo,
                             AdSoyad = b.Ad + " " + b.Soyad,
                             e.EnstituAd,
                             abd.AnabilimDaliAdi,
                             prg.ProgramAdi,
                             ot.OgrenimTipAdi,
                             OgrenciKayitDonemi = b.KayitOgretimYiliBaslangic + " - " + (b.KayitOgretimYiliBaslangic + 1) + " / " + (b.KayitOgretimYiliDonemID == 1 ? "Güz" : "Bahar") + " (" + (b.KayitOgretimYiliDonemID == 1 ? "Fall" : "Spring") + ")",
                             s.IsTezDiliTr,
                             s.TezBaslikTr,
                             s.TezBaslikEn,
                             s.YeniTezBaslikTr,
                             s.YeniTezBaslikEn,
                             s.TDUnvanAdi,
                             s.TDAdSoyad,
                             s.TDAnabilimDaliAdi,
                             s.TDProgramAdi,
                             s.TDTezSayisiDR,
                             s.TDTezSayisiYL,
                             s.TDOgrenciSayisiDR,
                             s.TDOgrenciSayisiYL,
                             s.VarolanTDAdSoyad,
                             s.VarolanTDUnvanAdi,
                             s.VarolanTDAnabilimDaliAdi,
                             s.VarolanTDProgramAdi,
                             s.SinavAdi,
                             s.SinavYili,
                             s.SinavPuani,
                             s.TDSinavTipID,
                             s.TDSinavAdi,
                             s.TDSinavYili,
                             s.TDSinavPuani,
                             s.DanismanOnayTarihi

                         }).First();



                this.cellFormKodu.Text = "Form Kodu: " + q.FormKodu;
                this.xrQRCode.ImageUrl = q.urlAdd;
                this.xrQRCode.Image = q.urlAdd.CreateQrCode(360, 360);
                this.cellOgrenciNo.Text = q.OgrenciNo;
                this.cellOgrenciAdSoyad.Text = q.AdSoyad;
                this.cellOgrenciEnstituAdi.Text = q.EnstituAd;
                this.cellOgrenciAnabilimDaliAdi.Text = q.AnabilimDaliAdi;
                this.cellOgrenciProgramAdi.Text = q.ProgramAdi;
                this.cellOgrenciOgrenimSeviyesi.Text = q.OgrenimTipAdi;

                this.cellOgrenciKayitDonemi.Text = q.OgrenciKayitDonemi;
                this.cellTezDili.Text = q.IsTezDiliTr ? "Türkçe (Turkish)" : "İngilizce (English)";
                this.cellTezBaslikTr.Text = q.TezBaslikTr;
                this.cellTezBaslikEn.Text = q.TezBaslikEn;

                this.chkDanismanDegisecekEvet.Checked = q.TDODanismanTalepTipID == TDODanismanTalepTip.TezDanismaniDegisikligi || q.TDODanismanTalepTipID == TDODanismanTalepTip.TezDanismaniVeBaslikDegisikligi;
                this.chkDanismanDegisecekHayir.Checked = !this.chkDanismanDegisecekEvet.Checked;
                if (this.chkDanismanDegisecekEvet.Checked)
                { 
                    this.cellDanismanUnvan.Text = q.TDUnvanAdi;
                    this.cellDanismanAdSoyad.Text = q.TDAdSoyad;
                    this.cellDanismanAnabilimDaliAdi.Text = q.TDAnabilimDaliAdi;
                    this.cellDanismanProgramAdi.Text = q.TDProgramAdi;
                    this.cellBasariIleTamamlanmisTezSayisiDR.Text = q.TDTezSayisiDR.ToString();
                    this.cellBasariIleTamamlanmisTezSayisiYL.Text = q.TDTezSayisiYL.ToString();
                    this.cellUzerineKayitliOgrenciSayisiDR.Text = q.TDOgrenciSayisiDR.ToString();
                    this.cellUzerineKayitliOgrenciSayisiYL.Text = q.TDOgrenciSayisiYL.ToString();
                }
                chkTezBasligiDegisecekEvet.Checked = q.TDODanismanTalepTipID == TDODanismanTalepTip.TezBasligiDegisikligi || q.TDODanismanTalepTipID == TDODanismanTalepTip.TezDanismaniVeBaslikDegisikligi;
                chkTezBasligiDegisecekHayir.Checked = !chkTezBasligiDegisecekEvet.Checked;
                if (chkTezBasligiDegisecekEvet.Checked)
                {
                    cellTezdili2.Text = q.IsTezDiliTr ? "Türkçe (Turkish)" : "İngilizce (English)";
                    cellYeniTezBaslikTr.Text = q.YeniTezBaslikTr;
                    cellYeniTezBaslikEn.Text = q.YeniTezBaslikEn;
                }
                this.detGrupTezDiliDr.Visible = q.isDrBasvurusu;
                this.rwTezOneriTarihDr.Visible = q.isDrBasvurusu;
                this.rwTezOneriYapildiDr.Visible = q.isDrBasvurusu;
                this.rwTezSayisiDr.Visible = q.isDrBasvurusu;

                this.CellMevcutDanismanAd.Text = q.VarolanTDUnvanAdi + " " + q.VarolanTDAdSoyad;
                this.cellMevcutDanismanAnabilimDali.Text = q.VarolanTDAnabilimDaliAdi;
                this.cellMevcutDanismanProgram.Text = q.VarolanTDProgramAdi;

                this.cellImzaOgrenciAdSoyad.Text = q.AdSoyad;

                this.cellImzaOgrenciAdSoyad.Text = q.AdSoyad;
                this.cellImzaMevcutDanismanAdSoyad.Text = q.VarolanTDUnvanAdi + " " + q.VarolanTDAdSoyad;
                this.cellImzaOnerilenDanismanAdSoyad.Text = q.TDUnvanAdi + " " + q.TDAdSoyad;

            }
        }


    }
}