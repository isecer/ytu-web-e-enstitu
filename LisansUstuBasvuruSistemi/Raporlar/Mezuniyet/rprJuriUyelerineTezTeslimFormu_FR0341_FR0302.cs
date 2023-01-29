using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using DevExpress.XtraReports.UI;
using LisansUstuBasvuruSistemi.Models; using LisansUstuBasvuruSistemi.Models.FilterModel;
using System.Linq;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Utilities.Enums;

namespace LisansUstuBasvuruSistemi.Raporlar
{
    public partial class rprJuriUyelerineTezTeslimFormu_FR0341_FR0302 : DevExpress.XtraReports.UI.XtraReport
    {
        public rprJuriUyelerineTezTeslimFormu_FR0341_FR0302(int MezuniyetJuriOneriFormID)
        {
            InitializeComponent();
            using (var db = new LisansustuBasvuruSistemiEntities())
            {

                var JoForm = db.MezuniyetJuriOneriFormlaris.Where(p => p.MezuniyetJuriOneriFormID == MezuniyetJuriOneriFormID).First();
                var MBasvuru = JoForm.MezuniyetBasvurulari;


                xrCellEnstituAdi.Text = MBasvuru.MezuniyetSureci.Enstituler.EnstituAd.ToUpper();
                xrCellAnabilimdaliAdi.Text = MBasvuru.Programlar.AnabilimDallari.AnabilimDaliAdi.ToUpper();
                xrCellProgramAdi.Text = MBasvuru.Programlar.ProgramAdi.ToUpper();
                xrCellNumarasi.Text = MBasvuru.OgrenciNo;

                xrCellOgrenciAdSoyad.Text = (MBasvuru.Ad + " " + MBasvuru.Soyad).ToUpper();

                var DanismanBilgi = JoForm.MezuniyetJuriOneriFormuJurileris.Where(p => p.JuriTipAdi == "TezDanismani").First();
                xrCellDanismanBilgi.Text = (DanismanBilgi.UnvanAdi + " " + DanismanBilgi.AdSoyad).ToUpper();
                xrCellDanismanBilgiUni.Text = DanismanBilgi.UniversiteID.HasValue ? DanismanBilgi.Universiteler.Ad.ToUpper() : DanismanBilgi.UniversiteAdi.ToUpper();
              
                cellTezBaslikTr.Text =
                    JoForm.IsTezBasligiDegisti == true ? JoForm.YeniTezBaslikTr : MBasvuru.TezBaslikTr;
                cellTezBaslikEn.Text =
                    JoForm.IsTezBasligiDegisti == true ? JoForm.YeniTezBaslikEn : MBasvuru.TezBaslikEn;


                var Uyeler = JoForm.MezuniyetJuriOneriFormuJurileris.Where(p => p.JuriTipAdi != "TezDanismani" && p.IsAsilOrYedek == true).ToList();


                if (MBasvuru.OgrenimTipKod == OgrenimTipi.TezliYuksekLisans)
                {
                    lblOgrenimTipAdi.Text = "YÜKSEK LİSANS";
                    this.DisplayName = (MBasvuru.Ad + " " + MBasvuru.Soyad) + " FR-0341 Yüksek Lisans Jüri Üyelerine Tez Teslim Formu";
                    var Uye1 = Uyeler[0];
                    xrCellUye1.Text = Uye1.UnvanAdi + " " + Uye1.AdSoyad;
                    xrCellUye1Uni.Text = Uye1.UniversiteID.HasValue ? Uye1.Universiteler.Ad : Uye1.UniversiteAdi;
                    var Uye2 = Uyeler[1];
                    xrCellUye2.Text = Uye2.UnvanAdi + " " + Uye2.AdSoyad;
                    xrCellUye2Uni.Text = Uye2.UniversiteID.HasValue ? Uye2.Universiteler.Ad : Uye2.UniversiteAdi;
                    xrTblRowTik1.Visible = false;
                    xrTblRowTik2.Visible = false;
                    lblFormNo.Text = "(Form No: FR-0341; Revizyon Tarihi: 27.11.2019; Revizyon No:02)";
                    xrRichKaraBigi.Html = "<table style='width:100%;table-layout:fixed;'><tbody><tr><td>" +
                                       "<b>YTÜ LİSANSÜSTÜ EĞİTİM ÖĞRETİM YÖNETMELİĞİ SENATO ESASLARI</b><br>" +
                                       "<b>MADDE 29- (3)</b> <span style='color:red;'> Tez jüri üyelerinin ilgili EYK tarafından belirlendiği tarihten itibaren en geç 1 (bir) hafta içerisinde, öğrenci hazırlamış olduğu tezinin bir kopyasını jüri üyelerine bir tutanakla teslim eder. Tez savunma sınavı, tezin jüri üyelerine tesliminden itibaren en erken 3 (üç) gün sonra ve en geç 1 (bir) ay içerisinde yapılmak üzere, sınav tarihi ve yerini belirten bir yazı danışman tarafından enstitüye iletilmek üzere ilgili anabilim/anasanat dalına verilir. Jüri üyeleri söz konusu tezin kendilerine teslim edildiği tarihten itibaren en geç bir ay içinde toplanarak öğrenciyi tez sınavına alır.</span>" +
                                       "</td></tr></tbody></table>";
                    var yedekler = JoForm.MezuniyetJuriOneriFormuJurileris.Where(p => p.IsAsilOrYedek == false).ToList();


                    var yedek1 = yedekler[0];
                    xrCellYedek1.Text = yedek1.UnvanAdi + ", " + yedek1.AdSoyad;
                    xrCellYedek1Uni.Text = yedek1.UniversiteID.HasValue ? yedek1.Universiteler.Ad : yedek1.UniversiteAdi;
                    var yedek2 = yedekler[1];
                    xrCellYedek2.Text = yedek2.UnvanAdi + ", " + yedek2.AdSoyad;
                    xrCellYedek2Uni.Text = yedek2.UniversiteID.HasValue ? yedek2.Universiteler.Ad : yedek2.UniversiteAdi;

                }
                else
                {
                    lblOgrenimTipAdi.Text = "DOKTORA";
                    this.DisplayName =  (MBasvuru.Ad + " " + MBasvuru.Soyad) + " FR-0302 Doktora Jüri Üyelerine Tez Teslim Formu";
                    var nUyerler = Uyeler.Where(p => !p.JuriTipAdi.Contains("Tik")).ToList();
                    var Uye1 = nUyerler[0];
                    xrCellUye1.Text = Uye1.UnvanAdi + " " + Uye1.AdSoyad;
                    xrCellUye1Uni.Text = Uye1.UniversiteID.HasValue ? Uye1.Universiteler.Ad.ToUpper() : Uye1.UniversiteAdi.ToUpper();
                    var Uye2 = nUyerler[1];
                    xrCellUye2.Text = Uye2.UnvanAdi + " " + Uye2.AdSoyad;
                    xrCellUye2Uni.Text = Uye2.UniversiteID.HasValue ? Uye2.Universiteler.Ad.ToUpper() : Uye2.UniversiteAdi.ToUpper();
                    var nTikler = Uyeler.Where(p => p.JuriTipAdi.Contains("Tik")).ToList();
                    var Tik1 = nTikler[0];
                    xrCellTik1.Text = Tik1.UnvanAdi + " " + Tik1.AdSoyad;
                    xrCellTik1Uni.Text = Tik1.UniversiteID.HasValue ? Tik1.Universiteler.Ad.ToUpper() : Tik1.UniversiteAdi.ToUpper();
                    var Tik2 = nTikler[1];
                    xrCellTik2.Text = Tik2.UnvanAdi + " " + Tik2.AdSoyad;
                    xrCellTik2Uni.Text = Tik2.UniversiteID.HasValue ? Tik2.Universiteler.Ad.ToUpper() : Tik2.UniversiteAdi.ToUpper();

                    var yedekler = JoForm.MezuniyetJuriOneriFormuJurileris.Where(p => p.IsAsilOrYedek == false).ToList();


                    var yedek1 = yedekler[0];
                    xrCellYedek1.Text = yedek1.UnvanAdi + ", " + yedek1.AdSoyad;
                    xrCellYedek1Uni.Text = yedek1.UniversiteID.HasValue ? yedek1.Universiteler.Ad.ToUpper() : yedek1.UniversiteAdi.ToUpper();
                    var yedek2 = yedekler[1];
                    xrCellYedek2.Text = yedek2.UnvanAdi + ", " + yedek2.AdSoyad;
                    xrCellYedek2Uni.Text = yedek2.UniversiteID.HasValue ? yedek2.Universiteler.Ad.ToUpper() : yedek2.UniversiteAdi.ToUpper();



                    lblFormNo.Text = "(Form No: FR-0302; Revizyon Tarihi: 27.11.2019; Revizyon No:03)";
                    xrRichKaraBigi.Html = "<table style='width:100%;table-layout:fixed;'><tbody><tr><td>" +
                                          "<b>YTÜ LİSANSÜSTÜ EĞİTİM ÖĞRETİM YÖNETMELİĞİ SENATO ESASLARI</b><br>" +
                                          "<b>MADDE 33- (4)</b> <span style='color:red;'> Doktora tez savunma sınavı, jüri üyelerinin belirlendiği tarihten itibaren en erken 15 (on beş) gün sonra ve en geç 1 (bir) ay içinde enstitülerin uygun göreceği sınav salonlarında yapılır. Sınavın ne zaman yapılacağı, danışmanın önerisi gözetilerek ilgili enstitü müdürlüğünce belirlenir ve doktora tez jüri üyelerine yazılı olarak bildirilir. Tez savunma sınav tarihi ve zamanı ilgili anabilim/anasanat dalı tarafından, öğretim elemanlarına, lisansüstü öğrencilerine ve alanın uzmanlarına dinleyici olarak katılabilmeleri için duyurulur.</span><br><br>" +
                                          "<b>(5)</b> <span style='color:red;'>Tez ve tezden üretilen yayınlar, doktora tez jürisinin ilgili EYK tarafından belirlendiği tarihten itibaren en geç bir hafta içerisinde öğrenci tarafından jüri üyelerine tutanak ile teslim edilir.</span>" +
                                          "</td></tr></tbody></table>";


                }





            }
        }

    }
}
