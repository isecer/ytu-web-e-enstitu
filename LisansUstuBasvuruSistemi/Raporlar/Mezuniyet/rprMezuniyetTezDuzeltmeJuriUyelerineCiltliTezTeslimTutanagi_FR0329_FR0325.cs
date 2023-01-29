using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using DevExpress.XtraReports.UI;
using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Models.FilterModel;
using System.Linq;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Utilities.Enums;

namespace LisansUstuBasvuruSistemi.Raporlar
{
    public partial class rprMezuniyetTezDuzeltmeJuriUyelerineCiltliTezTeslimTutanagi_FR0329_FR0325 : DevExpress.XtraReports.UI.XtraReport
    {
        public rprMezuniyetTezDuzeltmeJuriUyelerineCiltliTezTeslimTutanagi_FR0329_FR0325(int SRTalepID)
        {
            InitializeComponent();
            using (var db = new LisansustuBasvuruSistemiEntities())
            {

                var SRTalep = db.SRTalepleris.Where(p => p.SRTalepID == SRTalepID).First();
                var MBasvuru = SRTalep.MezuniyetBasvurulari;

                bool IsYlOrDiger = MBasvuru.OgrenimTipKod == OgrenimTipi.TezliYuksekLisans;

                xrCellEOYil.Text = xrCellEOYil.Text + MBasvuru.MezuniyetSureci.BaslangicYil.ToString() + "-" + MBasvuru.MezuniyetSureci.BitisYil.ToString();
                xrChkYariyilGuz.Checked = MBasvuru.MezuniyetSureci.DonemID == DonemBilgi.GuzYariyili;
                xrChkYariyilBahar.Checked = MBasvuru.MezuniyetSureci.DonemID == DonemBilgi.BaharYariyili;
                xrCellEnstituAdi.Text = MBasvuru.MezuniyetSureci.Enstituler.EnstituAd;
                xrCellAnabilimdaliAdi.Text = MBasvuru.Programlar.AnabilimDallari.AnabilimDaliAdi;
                xrCellProgramAdi.Text = MBasvuru.Programlar.ProgramAdi;
                xrCellOgrenciNo.Text = MBasvuru.OgrenciNo;
                xrCellOgrenciAdSoyad.Text = MBasvuru.Ad + " " + MBasvuru.Soyad;
                var JoForm = MBasvuru.MezuniyetJuriOneriFormlaris.FirstOrDefault();

                var DanismanBilgi = JoForm.MezuniyetJuriOneriFormuJurileris.Where(p => p.JuriTipAdi == "TezDanismani").First();
                xrCellTezDanismaniUnvaniAdSoyadi.Text = DanismanBilgi.UnvanAdi + " " + DanismanBilgi.AdSoyad;
                if (!MBasvuru.TezEsDanismanAdi.IsNullOrWhiteSpace())
                {
                    xrCellTezEsDanismaniUnvaniAdSoyadi.Text = MBasvuru.TezEsDanismanUnvani + "  " + MBasvuru.TezEsDanismanAdi;
                }
                else { xrCellTezEsDanismaniUnvaniAdSoyadi.Text = ""; }
 

                cellTezDili.Text = MBasvuru.IsTezDiliTr == true ? "Türkçe": "İngilizce";
                var tezBasligiTr = "";
                var tezBasligiEn = "";

                tezBasligiTr = JoForm.IsTezBasligiDegisti == true
                    ? JoForm.YeniTezBaslikTr
                    : MBasvuru.TezBaslikTr;
                tezBasligiEn = JoForm.IsTezBasligiDegisti == true
                    ? JoForm.YeniTezBaslikEn
                    : MBasvuru.TezBaslikEn;

                cellTezBaslikTr.Text = tezBasligiTr;
                cellTezBaslikEn.Text = tezBasligiEn;

                string SinavTarihi = SRTalep.Tarih.ToString("dd.MM.yyyy");
                xrRichEdit.Html = "<table style='width:100%;table-layour:fixed;font:arial;font-size:11pt;'>" +
                                  "<tbody><tr><td style='font-weight:bold;  text-align:center;'>ENSTİTÜ MÜDÜRLÜĞÜNE </td></tr>" +
                                          "<tr><td>Yukarıda bilgileri verilen ve " + SinavTarihi + " tarihinde girdiği Yüksek Lisans tez savunma sınavından başarılı olan öğrenci; " +
                                          "<span style='color:red;'>üyelerinin gerekli gördüğü düzeltmeleri yapmış ve tezini her bir jüri üyesine ciltli olarak teslim etmiştir.</span>" +
                                          "<br><br>Gereğini bilgilerinize saygılarımızla arz ederiz. " +
                                          "</td></tr></tbody></table>";

                xrCellDanismanBilgi.Text = DanismanBilgi.UnvanAdi + " " + DanismanBilgi.AdSoyad;
                if (IsYlOrDiger)
                {
                    lblOgrenimTipAdi.Text = "YÜKSEK LİSANS";
                    xrCellUye1Baslik.Text = "Üye";
                    xrCellUye2Baslik.Text = "Üye";
                    xrJur3Row.Visible = false;
                    xrJur4Row.Visible = false;
                    lblFormNo.Text = "(Form No: FR-0329; Revizyon Tarihi: 01.11.2013; Revizyon No:01)";
                    this.DisplayName = "FR-0329 Yükseklisans Tez Duzeltme ve Juri Uyelerine Ciltli Tez Teslim Tutanagi";
                    var Uyeler = JoForm.MezuniyetJuriOneriFormuJurileris.Where(p => p.JuriTipAdi != "TezDanismani" && p.IsAsilOrYedek == true).ToList();
                    var Uye1 = Uyeler.First();
                    xrCellUye1Bilgi.Text = Uye1.UnvanAdi + " " + Uye1.AdSoyad;

                    var Uye2 = Uyeler[1];
                    xrCellUye2Bilgi.Text = Uye2.UnvanAdi + " " + Uye2.AdSoyad;



                }
                else
                {
                    lblOgrenimTipAdi.Text = "DOKTORA";
                    xrCellUye1Baslik.Text = "Tik Üyesi";
                    xrCellUye2Baslik.Text = "Tik Üyesi";
                    xrJur3Row.Visible = false;
                    xrJur4Row.Visible = false;
                    lblFormNo.Text = "(Form No: FR-0325; Revizyon Tarihi: 25.02.2014; Revizyon No: 02)";
                    this.DisplayName = "FR-0325 Doktora Tez Duzeltme ve Juri Uyelerine Ciltli Tez Teslim Tutanagi";
                    var Uyeler = JoForm.MezuniyetJuriOneriFormuJurileris.Where(p => p.JuriTipAdi != "TezDanismani" && p.IsAsilOrYedek == true).ToList();
                    var Tik1 = Uyeler.Where(p => p.JuriTipAdi == "TikUyesi1").First();
                    xrCellUye1Bilgi.Text = Tik1.UnvanAdi + " " + Tik1.AdSoyad;

                    var Tik2 = Uyeler.Where(p => p.JuriTipAdi == "TikUyesi2").First();
                    xrCellUye2Bilgi.Text = Tik2.UnvanAdi + " " + Tik2.AdSoyad;

                    var Uye3 = Uyeler[2];
                    xrCellUye3Bilgi.Text = Uye3.UnvanAdi + " " + Uye3.AdSoyad;

                    var Uye4 = Uyeler[3];
                    xrCellUye4Bilgi.Text = Uye4.UnvanAdi + " " + Uye4.AdSoyad;
                }
            }
        }


    }
}
