using System.Linq;
using LisansUstuBasvuruSistemi.Business;
using Entities.Entities;

namespace LisansUstuBasvuruSistemi.Raporlar.Mezuniyet
{
    public partial class RprJuriUyelerineTezTeslimFormu_FR0341_FR0302 : DevExpress.XtraReports.UI.XtraReport
    {
        public RprJuriUyelerineTezTeslimFormu_FR0341_FR0302(int mezuniyetJuriOneriFormId)
        {
            InitializeComponent();
            using (var entities = new LubsDbEntities())
            {

                var joForm = entities.MezuniyetJuriOneriFormlaris.First(p => p.MezuniyetJuriOneriFormID == mezuniyetJuriOneriFormId);
                var mBasvuru = joForm.MezuniyetBasvurulari;


                xrCellEnstituAdi.Text = mBasvuru.MezuniyetSureci.Enstituler.EnstituAd.ToUpper();
                xrCellAnabilimdaliAdi.Text = mBasvuru.Programlar.AnabilimDallari.AnabilimDaliAdi.ToUpper();
                xrCellProgramAdi.Text = mBasvuru.Programlar.ProgramAdi.ToUpper();
                xrCellNumarasi.Text = mBasvuru.OgrenciNo;

                xrCellOgrenciAdSoyad.Text = (mBasvuru.Ad + " " + mBasvuru.Soyad).ToUpper();

                var danismanBilgi = joForm.MezuniyetJuriOneriFormuJurileris.First(p => p.JuriTipAdi == "TezDanismani");
                xrCellDanismanBilgi.Text = (danismanBilgi.UnvanAdi + " " + danismanBilgi.AdSoyad).ToUpper();
                xrCellDanismanBilgiUni.Text = danismanBilgi.UniversiteAdi.ToUpper();

                cellTezBaslikTr.Text =
                    joForm.IsTezBasligiDegisti == true ? joForm.YeniTezBaslikTr : mBasvuru.TezBaslikTr;
                cellTezBaslikEn.Text =
                    joForm.IsTezBasligiDegisti == true ? joForm.YeniTezBaslikEn : mBasvuru.TezBaslikEn;


                var uyeler = joForm.MezuniyetJuriOneriFormuJurileris.Where(p => p.JuriTipAdi != "TezDanismani" && p.IsAsilOrYedek == true).ToList();


                if (mBasvuru.OgrenimTipKod.IsDoktora())
                {
                    lblOgrenimTipAdi.Text = "DOKTORA";
                    this.DisplayName = (mBasvuru.Ad + " " + mBasvuru.Soyad) + " FR-0302 Doktora Jüri Üyelerine Tez Teslim Formu";
                    var nUyerler = uyeler.Where(p => !p.JuriTipAdi.Contains("Tik")).ToList();
                    var uye1 = nUyerler[0];
                    xrCellUye1.Text = uye1.UnvanAdi + " " + uye1.AdSoyad;
                    xrCellUye1Uni.Text = uye1.UniversiteAdi.ToUpper();
                    var uye2 = nUyerler[1];
                    xrCellUye2.Text = uye2.UnvanAdi + " " + uye2.AdSoyad;
                    xrCellUye2Uni.Text = uye2.UniversiteAdi.ToUpper();
                    var nTikler = uyeler.Where(p => p.JuriTipAdi.Contains("Tik")).ToList();
                    var tik1 = nTikler[0];
                    xrCellTik1.Text = tik1.UnvanAdi + " " + tik1.AdSoyad;
                    xrCellTik1Uni.Text = tik1.UniversiteAdi.ToUpper();
                    var tik2 = nTikler[1];
                    xrCellTik2.Text = tik2.UnvanAdi + " " + tik2.AdSoyad;
                    xrCellTik2Uni.Text = tik2.UniversiteAdi.ToUpper();

                    var yedekler = joForm.MezuniyetJuriOneriFormuJurileris.Where(p => p.IsAsilOrYedek == false).ToList();


                    var yedek1 = yedekler[0];
                    xrCellYedek1.Text = yedek1.UnvanAdi + ", " + yedek1.AdSoyad;
                    xrCellYedek1Uni.Text = yedek1.UniversiteAdi.ToUpper();
                    var yedek2 = yedekler[1];
                    xrCellYedek2.Text = yedek2.UnvanAdi + ", " + yedek2.AdSoyad;
                    xrCellYedek2Uni.Text = yedek2.UniversiteAdi.ToUpper();



                    lblFormNo.Text = "(Form No: FR-0302)";
                    xrRichKaraBigi.Html = "<table style='width:100%;table-layout:fixed;'><tbody><tr><td>" +
                                          "<b>YTÜ LİSANSÜSTÜ EĞİTİM ÖĞRETİM YÖNETMELİĞİ SENATO ESASLARI</b><br>" +
                                          "<b>MADDE 33- (4)</b> <span style='color:red;'> Doktora tez savunma sınavı, jüri üyelerinin belirlendiği tarihten itibaren en erken 15 (on beş) gün sonra ve en geç 1 (bir) ay içinde enstitülerin uygun göreceği sınav salonlarında yapılır. Sınavın ne zaman yapılacağı, danışmanın önerisi gözetilerek ilgili enstitü müdürlüğünce belirlenir ve doktora tez jüri üyelerine yazılı olarak bildirilir. Tez savunma sınav tarihi ve zamanı ilgili anabilim/anasanat dalı tarafından, öğretim elemanlarına, lisansüstü öğrencilerine ve alanın uzmanlarına dinleyici olarak katılabilmeleri için duyurulur.</span><br><br>" +
                                          "<b>(5)</b> <span style='color:red;'>Tez ve tezden üretilen yayınlar, doktora tez jürisinin ilgili EYK tarafından belirlendiği tarihten itibaren en geç bir hafta içerisinde öğrenci tarafından jüri üyelerine tutanak ile teslim edilir.</span>" +
                                          "</td></tr></tbody></table>";
                }
                else
                {
                    lblOgrenimTipAdi.Text = "YÜKSEK LİSANS";
                    this.DisplayName = (mBasvuru.Ad + " " + mBasvuru.Soyad) + " FR-0341 Yüksek Lisans Jüri Üyelerine Tez Teslim Formu";
                    var uye1 = uyeler[0];
                    xrCellUye1.Text = uye1.UnvanAdi + " " + uye1.AdSoyad;
                    xrCellUye1Uni.Text = uye1.UniversiteAdi.ToUpper();
                    var uye2 = uyeler[1];
                    xrCellUye2.Text = uye2.UnvanAdi + " " + uye2.AdSoyad;
                    xrCellUye2Uni.Text = uye2.UniversiteAdi.ToUpper();
                    xrTblRowTik1.Visible = false;
                    xrTblRowTik2.Visible = false;
                    lblFormNo.Text = "(Form No: FR-0341)";
                    xrRichKaraBigi.Html = "<table style='width:100%;table-layout:fixed;'><tbody><tr><td>" +
                                       "<b>YTÜ LİSANSÜSTÜ EĞİTİM ÖĞRETİM YÖNETMELİĞİ SENATO ESASLARI</b><br>" +
                                       "<b>MADDE 29- (3)</b> <span style='color:red;'> Tez jüri üyelerinin ilgili EYK tarafından belirlendiği tarihten itibaren en geç 1 (bir) hafta içerisinde, öğrenci hazırlamış olduğu tezinin bir kopyasını jüri üyelerine bir tutanakla teslim eder. Tez savunma sınavı, tezin jüri üyelerine tesliminden itibaren en erken 3 (üç) gün sonra ve en geç 1 (bir) ay içerisinde yapılmak üzere, sınav tarihi ve yerini belirten bir yazı danışman tarafından enstitüye iletilmek üzere ilgili anabilim/anasanat dalına verilir. Jüri üyeleri söz konusu tezin kendilerine teslim edildiği tarihten itibaren en geç bir ay içinde toplanarak öğrenciyi tez sınavına alır.</span>" +
                                       "</td></tr></tbody></table>";
                    var yedekler = joForm.MezuniyetJuriOneriFormuJurileris.Where(p => p.IsAsilOrYedek == false).ToList();


                    var yedek1 = yedekler[0];
                    xrCellYedek1.Text = yedek1.UnvanAdi + ", " + yedek1.AdSoyad;
                    xrCellYedek1Uni.Text = yedek1.UniversiteAdi.ToUpper();
                    var yedek2 = yedekler[1];
                    xrCellYedek2.Text = yedek2.UnvanAdi + ", " + yedek2.AdSoyad;
                    xrCellYedek2Uni.Text = yedek2.UniversiteAdi.ToUpper();

                }





            }
        }

    }
}
