using System;
using System.Linq;
using LisansUstuBasvuruSistemi.Business;
using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.Helpers;

namespace LisansUstuBasvuruSistemi.Raporlar.Mezuniyet
{
    public partial class RprTezSinavSonucTutanagi_FR0342_FR0377 : DevExpress.XtraReports.UI.XtraReport
    {
        public RprTezSinavSonucTutanagi_FR0342_FR0377(Guid uniqueId)
        {
            InitializeComponent();
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var srTalebi = db.SRTalepleris.First(p => p.UniqueID == uniqueId);
                var mBasvuru = srTalebi.MezuniyetBasvurulari;

                var joForm = mBasvuru.MezuniyetJuriOneriFormlaris.First();
                var enstL = mBasvuru.MezuniyetSureci.Enstituler;
                var prgL = mBasvuru.Programlar;
                var abdL = mBasvuru.Programlar.AnabilimDallari;
                cellOgrenciNo.Text = mBasvuru.OgrenciNo;
                cellOgrenciAdSoyad.Text = mBasvuru.Ad + " " + mBasvuru.Soyad;
                cellOgrenciEnstituAdi.Text = enstL.EnstituAd;
                cellOgrenciAnabilimDaliAdi.Text = abdL.AnabilimDaliAdi;
                cellOgrenciProgramAdi.Text = prgL.ProgramAdi;
                cell100_2000YokBursiyeri.Text = srTalebi.IsYokDrBursiyeriVar == true ? "Evet (Yes)" : "Hayır (No)";
                cell100_2000YokBursiyeriAltAlan.Text = srTalebi.IsYokDrBursiyeriVar == true ? srTalebi.YokDrOncelikliAlan : "";


                cellTezDili.Text = mBasvuru.IsTezDiliTr == true ? "Türkçe" : "English";
                cellTezBaslikTr.Text = joForm.IsTezBasligiDegisti == true ? joForm.YeniTezBaslikTr : mBasvuru.TezBaslikTr;
                cellTezBaslikEn.Text = joForm.IsTezBasligiDegisti == true ? joForm.YeniTezBaslikEn : mBasvuru.TezBaslikEn;
                if (srTalebi.IsOnline)
                {
                    cellToplantiYeri.Text = "Online";
                }
                else
                {
                    cellToplantiYeri.Text = srTalebi.SRSalonID.HasValue ? srTalebi.SalonAdi : srTalebi.SalonAdi;
                }
                cellToplantiTarihi.Text = srTalebi.Tarih.ToFormatDate();
                cellToplantiSaati.Text = $"{srTalebi.BasSaat:hh\\:mm}";


                if (srTalebi.IsTezBasligiDegisti == true)
                {
                    cellTezBasligiDegisecek.Text = "Evet (Yes)";
                    cellYeniTezBaslikTr.Text = srTalebi.YeniTezBaslikTr;
                    cellYeniTezBaslikEn.Text = srTalebi.YeniTezBaslikEn; ;
                }
                else
                {
                    cellTezBasligiDegisecek.Text = "Hayır (No)";
                    cellYeniTezBaslikTr.Text = "";
                    cellYeniTezBaslikEn.Text = "";
                }
                var uyeler = srTalebi.SRTaleplerJuris.ToList();
                if (mBasvuru.OgrenimTipKod.IsDoktora())
                {

                    DetailReport1.Visible = false;

                    var danisman = uyeler.First(p => p.JuriTipAdi == "TezDanismani");
                    var juriler = uyeler.Where(p => p.JuriTipAdi.Contains("YtuIciJuri") || p.JuriTipAdi.Contains("YtuDisiJuri")).ToList();
                    var juri1 = juriler[0];
                    var juri2 = juriler[1];
                    var tikler = uyeler.Where(p => p.JuriTipAdi.Contains("TikUyesi")).ToList();

                    if (!tikler.Any())
                    {
                        throw new Exception("Tez sınav sonuç tutanağı raporu için Tik üyeleri bilgileri bulunamadı!");
                    }
                    var tik1 = tikler[0];
                    var tik2 = tikler[1];

                    cellDrDanismanUnvanAdSoyad.Text = danisman.UnvanAdi + " " + danisman.JuriAdi;
                    cellDrDanismanAbdUniversiteAdi.Text = danisman.UniversiteAdi + "\r\n" + danisman.AnabilimdaliProgramAdi;

                    cellDrYtuIciTikAdSoyad.Text = tik1.UnvanAdi + " " + tik1.JuriAdi;
                    cellDrYtuIciTikAbdUniversiteAdi.Text = tik1.UniversiteAdi + "\r\n" + tik1.AnabilimdaliProgramAdi;

                    cellDrYtuDisiTikAdSoyad.Text = tik2.UnvanAdi + " " + tik2.JuriAdi;
                    cellDrYtuDisiTikAbdUniversiteAdi.Text = tik2.UniversiteAdi + "\r\n" + tik2.AnabilimdaliProgramAdi;

                    cellDrJuriUyesi1AdSoyad.Text = juri1.UnvanAdi + " " + juri1.JuriAdi;
                    cellDrJuriUyesi1AbdUniversiteAdi.Text = juri1.UniversiteAdi + "\r\n" + juri1.AnabilimdaliProgramAdi;

                    cellDrJuriUyesi2AdSoyad.Text = juri2.UnvanAdi + " " + juri2.JuriAdi;
                    cellDrJuriUyesi2AbdUniversiteAdi.Text = juri2.UniversiteAdi + "\r\n" + juri2.AnabilimdaliProgramAdi;


                    lblRaporTitleTr.Text = "DOKTORA TEZ SINAV SONUÇ TUTANAĞI";
                    lblRaporTitleEn.Text = "Ph.D. THESIS DEFENSE RESULT REPORT";

                    lblFormNo.Text = "(Form No: FR-0377)";

                    cell_DuzeltmeSureEn.Text = "AN EXTENSION OF 6 MONTHS’ TIME  ";
                    cell_DuzeltmeSureTr.Text = "6 AY DÜZELTME";

                    this.DisplayName = (mBasvuru.Ad + " " + mBasvuru.Soyad) + " FR-0377 Doktora Tez Sınavı Sonuç Tutanaği";
                }
                else
                {
                    DetailReport.Visible = false;

                    var danisman = uyeler.First(p => p.JuriTipAdi == "TezDanismani");
                    var juriler = uyeler.Where(p => p.JuriTipAdi.Contains("YtuIciJuri") || p.JuriTipAdi.Contains("YtuDisiJuri")).ToList();
                    var juri1 = juriler[0];
                    var juri2 = juriler[1];

                    cellYlDanismanUnvanAdSoyad.Text = danisman.UnvanAdi + " " + danisman.JuriAdi;
                    cellYlDanismanAbdUniversiteAdi.Text = danisman.UniversiteAdi + "\r\n" + danisman.AnabilimdaliProgramAdi;


                    cellYlJuriUyesi1AdSoyad.Text = juri1.UnvanAdi + " " + juri1.JuriAdi;
                    cellYlJuriUyesi1AbdUniversiteAdi.Text = juri1.UniversiteAdi + "\r\n" + juri1.AnabilimdaliProgramAdi;

                    cellYlJuriUyesi2AdSoyad.Text = juri2.UnvanAdi + " " + juri2.JuriAdi;
                    cellYlJuriUyesi2AbdUniversiteAdi.Text = juri2.UniversiteAdi + "\r\n" + juri2.AnabilimdaliProgramAdi;

                    lblRaporTitleTr.Text = "YÜKSEK LİSANS TEZ SINAV SONUÇ TUTANAĞI";
                    lblRaporTitleEn.Text = "MSc. THESIS DEFENSE RESULT REPORT";

                    lblFormNo.Text = "(Form No: FR-0342)";


                    cell_DuzeltmeSureEn.Text = "AN EXTENSION OF 3 MONTHS’ TIME  ";
                    cell_DuzeltmeSureTr.Text = "3 AY DÜZELTME";

                    this.DisplayName = (mBasvuru.Ad + " " + mBasvuru.Soyad) + " FR-0377 Yüksek Lisans Tez Sınavı Sonuç Tutanaği";
                }
                chkOyBirligi.Checked = srTalebi.IsOyBirligiOrCoklugu == true;
                chkOyCoklugu.Checked = srTalebi.IsOyBirligiOrCoklugu == false;
                chkBasarili.Checked = srTalebi.JuriSonucMezuniyetSinavDurumID == MezuniyetSinavDurum.Basarili || srTalebi.MezuniyetSinavDurumID == MezuniyetSinavDurum.Basarili;
                chkBasarisiz.Checked = srTalebi.JuriSonucMezuniyetSinavDurumID == MezuniyetSinavDurum.Basarisiz || srTalebi.MezuniyetSinavDurumID == MezuniyetSinavDurum.Basarisiz;
                chkUzatma.Checked = srTalebi.JuriSonucMezuniyetSinavDurumID == MezuniyetSinavDurum.Uzatma || srTalebi.MezuniyetSinavDurumID == MezuniyetSinavDurum.Uzatma;

                cellFormKodu.Text = "Form Kodu: " + srTalebi.UniqueID.ToString().Substring(0, 8).ToUpper();
                var qrUlr = enstL.SistemErisimAdresi + "/DosyaKontrol/Index?Kod=" + "MZTSS_" + mBasvuru.MezuniyetBasvurulariID + "_" + srTalebi.UniqueID;
                xrQRCode.ImageUrl = qrUlr;
                xrQRCode.Image = qrUlr.CreateQrCode();




            }
        }

    }
}
