using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using DevExpress.XtraReports.UI;
using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using System.Linq;
using BiskaUtil;

namespace LisansUstuBasvuruSistemi.Raporlar
{
    public partial class rprMezuniyetTezDegerlendirmeFormu_FR0303 : DevExpress.XtraReports.UI.XtraReport
    {
        public rprMezuniyetTezDegerlendirmeFormu_FR0303(int MezuniyetJuriOneriFormID, int? MezuniyetJuriOneriFormuJuriID)
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

                xrCellOgrenciAdSoyad.Text = MBasvuru.Ad + " " + MBasvuru.Soyad;

                var DanismanBilgi = JoForm.MezuniyetJuriOneriFormuJurileris.Where(p => p.JuriTipAdi == "TezDanismani").First();
                xrCellTezDanismanBilgi.Text = DanismanBilgi.UnvanAdi.ToUpper() + " " + DanismanBilgi.AdSoyad.ToUpper();
                if (!MBasvuru.TezEsDanismanAdi.IsNullOrWhiteSpace())
                {
                    xrCellTezEsDanismanBilgi.Text = MBasvuru.TezEsDanismanUnvani.ToUpper() + " " + MBasvuru.TezEsDanismanAdi.ToUpper();
                }
                var tezBasligi = "";
                if (MBasvuru.IsTezDiliTr == true)
                {
                    tezBasligi = JoForm.IsTezBasligiDegisti == true
                        ? JoForm.YeniTezBaslikTr
                        : MBasvuru.TezBaslikTr;
                }
                else
                {
                    tezBasligi = JoForm.IsTezBasligiDegisti == true
                        ? JoForm.YeniTezBaslikEn
                        : MBasvuru.TezBaslikEn;
                }
                xrCellTezinBasligi.Text = tezBasligi;


                if (MezuniyetJuriOneriFormuJuriID.HasValue)
                {
                    var secilenJuri = JoForm.MezuniyetJuriOneriFormuJurileris.Where(p => p.MezuniyetJuriOneriFormuJuriID == MezuniyetJuriOneriFormuJuriID).First();
                    xrCellJuriAdSoyad.Text = secilenJuri.UnvanAdi + " " + secilenJuri.AdSoyad;
                    xrCellJuriUniversiteAdi.Text = secilenJuri.UniversiteID.HasValue ? secilenJuri.Universiteler.Ad.ToUpper() : secilenJuri.UniversiteAdi.ToUpper();
                    xrCellJuriTelefon.Text = "";
                    xrCellJuriFaks.Text = "";
                    xrCellJuriEPosta.Text = secilenJuri.EMail;
                }
                else
                {
                    xrCellJuriAdSoyad.Text = "";
                    xrCellJuriUniversiteAdi.Text = "";
                    xrCellJuriTelefon.Text = "";
                    xrCellJuriFaks.Text = "";
                    xrCellJuriEPosta.Text = "";
                }
                this.DisplayName = (MBasvuru.Ad + " " + MBasvuru.Soyad) + " FR-0303 Doktora Tez Değerlendirme Formu";

            }
        }

    }
}
