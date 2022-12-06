using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using DevExpress.XtraReports.UI;
using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Models.FilterModel;
using System.Linq;
using BiskaUtil;

namespace LisansUstuBasvuruSistemi.Raporlar
{
    public partial class rprTezSinavSonucTutanagi_FR0342_FR0377 : DevExpress.XtraReports.UI.XtraReport
    {
        public rprTezSinavSonucTutanagi_FR0342_FR0377(Guid UniqueID)
        {
            InitializeComponent();
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var SRTalebi = db.SRTalepleris.Where(p => p.UniqueID == UniqueID).First();
                var MBasvuru = SRTalebi.MezuniyetBasvurulari;

                var JoForm = MBasvuru.MezuniyetJuriOneriFormlaris.First();
                var EnstL = MBasvuru.MezuniyetSureci.Enstituler;
                var PrgL = MBasvuru.Programlar;
                var AbdL = MBasvuru.Programlar.AnabilimDallari;
                var Os = db.OgrenimTipleris.Where(p => p.EnstituKod == EnstL.EnstituKod && p.OgrenimTipKod == MBasvuru.OgrenimTipKod).First();
                cellOgrenciNo.Text = MBasvuru.OgrenciNo;
                cellOgrenciAdSoyad.Text = MBasvuru.Ad + " " + MBasvuru.Soyad;
                cellOgrenciEnstituAdi.Text = EnstL.EnstituAd;
                cellOgrenciAnabilimDaliAdi.Text = AbdL.AnabilimDaliAdi;
                cellOgrenciProgramAdi.Text = PrgL.ProgramAdi;
                cell100_2000YokBursiyeri.Text = SRTalebi.IsYokDrBursiyeriVar == true ? "Evet (Yes)" : "Hayır (No)";
                cell100_2000YokBursiyeriAltAlan.Text = SRTalebi.IsYokDrBursiyeriVar == true ? SRTalebi.YokDrOncelikliAlan : ""; 


                cellTezDili.Text = MBasvuru.IsTezDiliTr == true ? "Türkçe" : "English";
                cellTezBaslikTr.Text = JoForm.IsTezBasligiDegisti == true ? JoForm.YeniTezBaslikTr : MBasvuru.TezBaslikTr;
                cellTezBaslikEn.Text = JoForm.IsTezBasligiDegisti == true ? JoForm.YeniTezBaslikEn : MBasvuru.TezBaslikEn;
                if (SRTalebi.IsOnline)
                {
                    cellToplantiYeri.Text = "Online";
                }
                else
                {
                    if (SRTalebi.SRSalonID.HasValue)
                    {
                        cellToplantiYeri.Text = SRTalebi.SalonAdi;
                    }
                    else
                    {
                        cellToplantiYeri.Text = SRTalebi.SalonAdi;
                    }

                }
                cellToplantiTarihi.Text = SRTalebi.Tarih.ToFormatDate();
                cellToplantiSaati.Text = $"{SRTalebi.BasSaat:hh\\:mm}";


                if (SRTalebi.IsTezBasligiDegisti == true)
                {
                    cellTezBasligiDegisecek.Text = "Evet (Yes)";
                    cellYeniTezBaslikTr.Text = MBasvuru.IsTezDiliTr == true ? SRTalebi.YeniTezBaslikTr : SRTalebi.YeniTezBaslikEn; 
                    cellYeniTezBaslikEn.Text = MBasvuru.IsTezDiliTr == false ? SRTalebi.YeniTezBaslikTr : SRTalebi.YeniTezBaslikEn; ;
                }
                else
                {
                    cellTezBasligiDegisecek.Text = "";
                    cellYeniTezBaslikTr.Text = "";
                    cellYeniTezBaslikEn.Text = "";
                }

                var Uyeler = JoForm.MezuniyetJuriOneriFormuJurileris.Where(p => p.IsAsilOrYedek == true).ToList();
                if (MBasvuru.OgrenimTipKod == OgrenimTipi.TezliYuksekLisans)
                {
                    DetailReport.Visible = false;

                    var Danisman = Uyeler.Where(p => p.JuriTipAdi == "TezDanismani").First();
                    var Juriler = Uyeler.Where(p => p.JuriTipAdi.Contains("YtuIciJuri") || p.JuriTipAdi.Contains("YtuDisiJuri")).ToList();
                    var Juri1 = Juriler[0];
                    var Juri2 = Juriler[1];

                    cellYlDanismanUnvanAdSoyad.Text = Danisman.UnvanAdi + " " + Danisman.AdSoyad;
                    cellYlDanismanAbdUniversiteAdi.Text = Danisman.UniversiteAdi + "\r\n" + Danisman.AnabilimdaliProgramAdi;


                    cellYlJuriUyesi1AdSoyad.Text = Juri1.UnvanAdi + " " + Juri1.AdSoyad;
                    cellYlJuriUyesi1AbdUniversiteAdi.Text = Juri1.UniversiteAdi + "\r\n" + Juri1.AnabilimdaliProgramAdi;

                    cellYlJuriUyesi2AdSoyad.Text = Juri2.UnvanAdi + " " + Juri2.AdSoyad;
                    cellYlJuriUyesi2AbdUniversiteAdi.Text = Juri2.UniversiteAdi + "\r\n" + Juri2.AnabilimdaliProgramAdi;

                    lblRaporTitleTr.Text = "YÜKSEK LİSANS TEZ SINAV SONUÇ TUTANAĞI";
                    lblRaporTitleEn.Text = "MSc. THESIS DEFENSE RESULT REPORT";

                    lblFormNo.Text = "(Form No: FR-0342)";


                    cell_DuzeltmeSureEn.Text = "AN EXTENSION OF 3 MONTHS’ TIME  ";
                    cell_DuzeltmeSureTr.Text = "3 AY DÜZELTME";

                    this.DisplayName = (MBasvuru.Ad + " " + MBasvuru.Soyad) + " FR-0377 Yüksek Lisans Tez Sınavı Sonuç Tutanaği";

                }
                else
                {
                    DetailReport1.Visible = false;

                    var Danisman = Uyeler.Where(p => p.JuriTipAdi == "TezDanismani").First();
                    var Juriler = Uyeler.Where(p => p.JuriTipAdi.Contains("YtuIciJuri") || p.JuriTipAdi.Contains("YtuDisiJuri")).ToList();
                    var Juri1 = Juriler[0];
                    var Juri2 = Juriler[1];
                    var Tikler = Uyeler.Where(p => p.JuriTipAdi.Contains("TikUyesi")).ToList();
                    var Tik1 = Tikler[0];
                    var Tik2 = Tikler[1];

                    cellDrDanismanUnvanAdSoyad.Text = Danisman.UnvanAdi + " " + Danisman.AdSoyad;
                    cellDrDanismanAbdUniversiteAdi.Text = Danisman.UniversiteAdi + "\r\n" + Danisman.AnabilimdaliProgramAdi;

                    cellDrYtuIciTikAdSoyad.Text = Tik1.UnvanAdi + " " + Tik1.AdSoyad;
                    cellDrYtuIciTikAbdUniversiteAdi.Text = Tik1.UniversiteAdi + "\r\n" + Tik1.AnabilimdaliProgramAdi;

                    cellDrYtuDisiTikAdSoyad.Text = Tik2.UnvanAdi + " " + Tik2.AdSoyad;
                    cellDrYtuDisiTikAbdUniversiteAdi.Text = Tik2.UniversiteAdi + "\r\n" + Tik2.AnabilimdaliProgramAdi;

                    cellDrJuriUyesi1AdSoyad.Text = Juri1.UnvanAdi + " " + Juri1.AdSoyad;
                    cellDrJuriUyesi1AbdUniversiteAdi.Text = Juri1.UniversiteAdi + "\r\n" + Juri1.AnabilimdaliProgramAdi;

                    cellDrJuriUyesi2AdSoyad.Text = Juri2.UnvanAdi + " " + Juri2.AdSoyad;
                    cellDrJuriUyesi2AbdUniversiteAdi.Text = Juri2.UniversiteAdi + "\r\n" + Juri2.AnabilimdaliProgramAdi;


                    lblRaporTitleTr.Text = "DOKTORA TEZ SINAV SONUÇ TUTANAĞI";
                    lblRaporTitleEn.Text = "Ph.D. THESIS DEFENSE RESULT REPORT";

                    lblFormNo.Text = "(Form No: FR-0377)";

                    cell_DuzeltmeSureEn.Text = "AN EXTENSION OF 6 MONTHS’ TIME  ";
                    cell_DuzeltmeSureTr.Text = "6 AY DÜZELTME";

                    this.DisplayName = (MBasvuru.Ad + " " + MBasvuru.Soyad) + " FR-0377 Doktora Tez Sınavı Sonuç Tutanaği";
                }

                chkOyBirligi.Checked = SRTalebi.IsOyBirligiOrCouklugu == true;
                chkOyCoklugu.Checked = SRTalebi.IsOyBirligiOrCouklugu == false;
                chkBasarili.Checked = SRTalebi.JuriSonucMezuniyetSinavDurumID == MezuniyetSinavDurum.Basarili || SRTalebi.MezuniyetSinavDurumID == MezuniyetSinavDurum.Basarili;
                chkBasarisiz.Checked = SRTalebi.JuriSonucMezuniyetSinavDurumID == MezuniyetSinavDurum.Basarisiz || SRTalebi.MezuniyetSinavDurumID == MezuniyetSinavDurum.Basarisiz;
                chkUzatma.Checked = SRTalebi.JuriSonucMezuniyetSinavDurumID == MezuniyetSinavDurum.Uzatma || SRTalebi.MezuniyetSinavDurumID == MezuniyetSinavDurum.Uzatma;

                cellFormKodu.Text = "Form Kodu: " + SRTalebi.UniqueID.ToString().Substring(0, 8).ToUpper();
                var qrUlr = EnstL.SistemErisimAdresi + "/DosyaKontrol/Index?Kod=" + "MBBBC_" + MBasvuru.MezuniyetBasvurulariID + "_" + SRTalebi.UniqueID;
                xrQRCode.ImageUrl = qrUlr;
                xrQRCode.Image = qrUlr.CreateQrCode();




            }
        }

    }
}
