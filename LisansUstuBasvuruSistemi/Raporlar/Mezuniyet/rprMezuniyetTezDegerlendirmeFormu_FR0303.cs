using Entities.Entities;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using System.Linq;

namespace LisansUstuBasvuruSistemi.Raporlar.Mezuniyet
{
    public partial class RprMezuniyetTezDegerlendirmeFormu_FR0303 : DevExpress.XtraReports.UI.XtraReport
    {
        public RprMezuniyetTezDegerlendirmeFormu_FR0303(int mezuniyetJuriOneriFormId, int? mezuniyetJuriOneriFormuJuriId)
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

                xrCellOgrenciAdSoyad.Text = mBasvuru.Ad + " " + mBasvuru.Soyad;

                var danismanBilgi = joForm.MezuniyetJuriOneriFormuJurileris.First(p => p.JuriTipAdi == "TezDanismani");
                xrCellTezDanismanBilgi.Text = danismanBilgi.UnvanAdi.ToUpper() + " " + danismanBilgi.AdSoyad.ToUpper();
                if (!mBasvuru.TezEsDanismanAdi.IsNullOrWhiteSpace())
                {
                    xrCellTezEsDanismanBilgi.Text = mBasvuru.TezEsDanismanUnvani.ToUpper() + " " + mBasvuru.TezEsDanismanAdi.ToUpper();
                }
                var tezBasligi = "";
                if (mBasvuru.IsTezDiliTr == true)
                {
                    tezBasligi = joForm.IsTezBasligiDegisti == true
                        ? joForm.YeniTezBaslikTr
                        : mBasvuru.TezBaslikTr;
                }
                else
                {
                    tezBasligi = joForm.IsTezBasligiDegisti == true
                        ? joForm.YeniTezBaslikEn
                        : mBasvuru.TezBaslikEn;
                }
                xrCellTezinBasligi.Text = tezBasligi;


                if (mezuniyetJuriOneriFormuJuriId.HasValue)
                {
                    var secilenJuri = joForm.MezuniyetJuriOneriFormuJurileris.First(p => p.MezuniyetJuriOneriFormuJuriID == mezuniyetJuriOneriFormuJuriId);
                    xrCellJuriAdSoyad.Text = secilenJuri.UnvanAdi + " " + secilenJuri.AdSoyad;
                    xrCellJuriUniversiteAdi.Text = secilenJuri.UniversiteAdi.ToUpper();
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
                DisplayName = (mBasvuru.Ad + " " + mBasvuru.Soyad) + " FR-0303 Doktora Tez Değerlendirme Formu";

            }
        }

    }
}
