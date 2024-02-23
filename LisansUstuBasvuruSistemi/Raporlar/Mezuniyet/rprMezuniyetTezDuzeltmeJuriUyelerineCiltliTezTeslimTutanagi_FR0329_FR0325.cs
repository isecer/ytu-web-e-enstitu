using System.Linq;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Business;
using Entities.Entities;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.Extensions;

namespace LisansUstuBasvuruSistemi.Raporlar.Mezuniyet
{
    public partial class RprMezuniyetTezDuzeltmeJuriUyelerineCiltliTezTeslimTutanagi_FR0329_FR0325 : DevExpress.XtraReports.UI.XtraReport
    {
        public RprMezuniyetTezDuzeltmeJuriUyelerineCiltliTezTeslimTutanagi_FR0329_FR0325(int srTalepId)
        {
            InitializeComponent();
            using (var entities = new LubsDbEntities())
            {

                var srTalep = entities.SRTalepleris.First(p => p.SRTalepID == srTalepId);
                var mBasvuru = srTalep.MezuniyetBasvurulari;



                xrCellEOYil.Text = xrCellEOYil.Text + mBasvuru.MezuniyetSureci.BaslangicYil.ToString() + "-" + mBasvuru.MezuniyetSureci.BitisYil.ToString();
                xrChkYariyilGuz.Checked = mBasvuru.MezuniyetSureci.DonemID == AkademikDonemEnum.GuzYariyili;
                xrChkYariyilBahar.Checked = mBasvuru.MezuniyetSureci.DonemID == AkademikDonemEnum.BaharYariyili;
                xrCellEnstituAdi.Text = mBasvuru.MezuniyetSureci.Enstituler.EnstituAd;
                xrCellAnabilimdaliAdi.Text = mBasvuru.Programlar.AnabilimDallari.AnabilimDaliAdi;
                xrCellProgramAdi.Text = mBasvuru.Programlar.ProgramAdi;
                xrCellOgrenciNo.Text = mBasvuru.OgrenciNo;
                xrCellOgrenciAdSoyad.Text = mBasvuru.Ad + " " + mBasvuru.Soyad;
                var joForm = mBasvuru.MezuniyetJuriOneriFormlaris.FirstOrDefault();

                var danismanBilgi = joForm.MezuniyetJuriOneriFormuJurileris.First(p => p.JuriTipAdi == "TezDanismani");
                xrCellTezDanismaniUnvaniAdSoyadi.Text = danismanBilgi.UnvanAdi + " " + danismanBilgi.AdSoyad;
                if (!mBasvuru.TezEsDanismanAdi.IsNullOrWhiteSpace())
                {
                    xrCellTezEsDanismaniUnvaniAdSoyadi.Text = mBasvuru.TezEsDanismanUnvani + "  " + mBasvuru.TezEsDanismanAdi;
                }
                else { xrCellTezEsDanismaniUnvaniAdSoyadi.Text = ""; }


                cellTezDili.Text = mBasvuru.IsTezDiliTr == true ? "Türkçe" : "İngilizce";
                var tezBasligiTr = "";
                var tezBasligiEn = "";

                tezBasligiTr = joForm.IsTezBasligiDegisti == true
                    ? joForm.YeniTezBaslikTr
                    : mBasvuru.TezBaslikTr;
                tezBasligiEn = joForm.IsTezBasligiDegisti == true
                    ? joForm.YeniTezBaslikEn
                    : mBasvuru.TezBaslikEn;

                cellTezBaslikTr.Text = tezBasligiTr;
                cellTezBaslikEn.Text = tezBasligiEn;

                string sinavTarihi = srTalep.Tarih.ToFormatDate();
                xrRichEdit.Html = "<table style='width:100%;table-layour:fixed;font:arial;font-size:11pt;'>" +
                                  "<tbody><tr><td style='font-weight:bold;  text-align:center;'>ENSTİTÜ MÜDÜRLÜĞÜNE </td></tr>" +
                                          "<tr><td>Yukarıda bilgileri verilen ve " + sinavTarihi + " tarihinde girdiği Yüksek Lisans tez savunma sınavından başarılı olan öğrenci; " +
                                          "<span style='color:red;'>üyelerinin gerekli gördüğü düzeltmeleri yapmış ve tezini her bir jüri üyesine ciltli olarak teslim etmiştir.</span>" +
                                          "<br><br>Gereğini bilgilerinize saygılarımızla arz ederiz. " +
                                          "</td></tr></tbody></table>";

                xrCellDanismanBilgi.Text = danismanBilgi.UnvanAdi + " " + danismanBilgi.AdSoyad;
                if (mBasvuru.OgrenimTipKod.IsDoktora())
                {
                    lblOgrenimTipAdi.Text = "DOKTORA";
                    xrCellUye1Baslik.Text = "Tik Üyesi";
                    xrCellUye2Baslik.Text = "Tik Üyesi";
                    xrJur3Row.Visible = false;
                    xrJur4Row.Visible = false;
                    lblFormNo.Text = "(Form No: FR-0325; Revizyon Tarihi: 25.02.2014; Revizyon No: 02)";
                    this.DisplayName = "FR-0325 Doktora Tez Duzeltme ve Juri Uyelerine Ciltli Tez Teslim Tutanagi";
                    var uyeler = joForm.MezuniyetJuriOneriFormuJurileris.Where(p => p.JuriTipAdi != "TezDanismani" && p.IsAsilOrYedek == true).ToList();
                    var tik1 = uyeler.First(p => p.JuriTipAdi == "TikUyesi1");
                    xrCellUye1Bilgi.Text = tik1.UnvanAdi + " " + tik1.AdSoyad;

                    var tik2 = uyeler.First(p => p.JuriTipAdi == "TikUyesi2");
                    xrCellUye2Bilgi.Text = tik2.UnvanAdi + " " + tik2.AdSoyad;

                    var uye3 = uyeler[2];
                    xrCellUye3Bilgi.Text = uye3.UnvanAdi + " " + uye3.AdSoyad;

                    var uye4 = uyeler[3];
                    xrCellUye4Bilgi.Text = uye4.UnvanAdi + " " + uye4.AdSoyad;
                }
                else
                {
                    lblOgrenimTipAdi.Text = "YÜKSEK LİSANS";
                    xrCellUye1Baslik.Text = "Üye";
                    xrCellUye2Baslik.Text = "Üye";
                    xrJur3Row.Visible = false;
                    xrJur4Row.Visible = false;
                    lblFormNo.Text = "(Form No: FR-0329; Revizyon Tarihi: 01.11.2013; Revizyon No:01)";
                    this.DisplayName = "FR-0329 Yükseklisans Tez Duzeltme ve Juri Uyelerine Ciltli Tez Teslim Tutanagi";
                    var uyeler = joForm.MezuniyetJuriOneriFormuJurileris.Where(p => p.JuriTipAdi != "TezDanismani" && p.IsAsilOrYedek == true).ToList();
                    var uye1 = uyeler.First();
                    xrCellUye1Bilgi.Text = uye1.UnvanAdi + " " + uye1.AdSoyad;

                    var uye2 = uyeler[1];
                    xrCellUye2Bilgi.Text = uye2.UnvanAdi + " " + uye2.AdSoyad;



                }
            }
        }


    }
}
