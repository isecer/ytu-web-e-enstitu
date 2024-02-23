using System.Linq;
using BiskaUtil;
using Entities.Entities;
using LisansUstuBasvuruSistemi.Utilities.Extensions;

namespace LisansUstuBasvuruSistemi.Raporlar.TezIzleme
{
    public partial class RprTiDegerlendirmeFormuDetay_FR0307 : DevExpress.XtraReports.UI.XtraReport
    {
        public RprTiDegerlendirmeFormuDetay_FR0307(int tiBasvuruAraRaporId)
        {
            InitializeComponent();

            using (var  entities = new LubsDbEntities())
            {
                var data = (from s in entities.TIBasvuruAraRapors
                            where s.TIBasvuruAraRaporID == tiBasvuruAraRaporId
                            select new
                            {
                                s.IsYokDrBursiyeriVar,
                                Danisman = s.TIBasvuruAraRaporKomites.FirstOrDefault(p => p.JuriTipAdi == "TezDanismani"),
                                TikUyesi1 = s.TIBasvuruAraRaporKomites.FirstOrDefault(p => p.JuriTipAdi == "TikUyesi1"),
                                TikUyesi2 = s.TIBasvuruAraRaporKomites.FirstOrDefault(p => p.JuriTipAdi == "TikUyesi2"),
                            }).First();

                this.DisplayName = "FR-0307 DOKTORA TEZ İZLEME RAPOR FORMU DEĞERLENDİRME EKİ";

                cellDanismanUnvanAdSoyad.Text = data.Danisman.UnvanAdi + "\r\n" + data.Danisman.AdSoyad;
                cellDanismanAbdUniversiteAdi.Text = data.Danisman.AnabilimdaliProgramAdi + "\r\n" + data.Danisman.UniversiteAdi;
                cellDanismanTarihImza.Text = "";
                cellDanismanTezOneriUyum.Text = data.Danisman.IsTezIzlemeRaporuTezOnerisiUygun == true ? "UYGUN (COMPATIBLE)" : "UYGUN DEĞİL (INCOMPATIBLE)";
                cellDanismanAlanUyum.Text = data.Danisman.IsTezIzlemeRaporuAltAlanUygun == true ? "UYGUN (COMPATIBLE)" : "UYGUN DEĞİL (INCOMPATIBLE)";
                cellDanismanDegerlendirmeSonucu.Text = data.Danisman.IsBasarili == true ? "BAŞARILI (SUCCESSFUL)" : "BAŞARISIZ (UNSUCCESSFUL)";
                cellDanismanDegerlendirmeAciklama.Text = data.Danisman.Aciklama.IsNullOrWhiteSpace() ? "" : data.Danisman.Aciklama;
                cellDanismanTarihImza.Text = data.Danisman.DegerlendirmeIslemTarihi.ToFormatDateAndTime();

                RwAltAlanSoru.Visible = data.IsYokDrBursiyeriVar;
                if (!data.IsYokDrBursiyeriVar)
                {
                    cellSoru3.Text = cellSoru3.Text.Replace("3.", "2.");
                    cellSoru4.Text = cellSoru4.Text.Replace("4.", "3."); 
                }

                cellTik1UnvanAdSoyad.Text = data.TikUyesi1.UnvanAdi + " \r\n" + data.TikUyesi1.AdSoyad;
                cellTik1AbdUniversiteAdi.Text = data.TikUyesi1.AnabilimdaliProgramAdi + "\r\n" + data.TikUyesi1.UniversiteAdi;
                cellTik1TarihImza.Text = "";
                cellTik1TezOneriUyum.Text = data.TikUyesi1.IsTezIzlemeRaporuTezOnerisiUygun == true ? "UYGUN (COMPATIBLE)" : "UYGUN DEĞİL (INCOMPATIBLE)";
                cellTik1DegerlendirmeSonucu.Text = data.TikUyesi1.IsBasarili == true ? "BAŞARILI (SUCCESSFUL)" : "BAŞARISIZ (UNSUCCESSFUL)";
                cellTik1DegerlendirmeAciklama.Text = data.TikUyesi1.Aciklama.IsNullOrWhiteSpace() ? "" : data.TikUyesi1.Aciklama;
                cellTik1TarihImza.Text = data.TikUyesi1.DegerlendirmeIslemTarihi.ToFormatDateAndTime();

                cellTik2UnvanAdSoyad.Text = data.TikUyesi2.UnvanAdi + "\r\n" + data.TikUyesi2.AdSoyad;
                cellTik2AbdUniversiteAdi.Text = data.TikUyesi2.AnabilimdaliProgramAdi + "\r\n" + data.TikUyesi2.UniversiteAdi;
                cellTik2TarihImza.Text = "";
                cellTik2TezOneriUyum.Text = data.TikUyesi2.IsTezIzlemeRaporuTezOnerisiUygun == true ? "UYGUN (COMPATIBLE)" : "UYGUN DEĞİL (INCOMPATIBLE)";
                cellTik2DegerlendirmeSonucu.Text = data.TikUyesi2.IsBasarili == true ? "BAŞARILI (SUCCESSFUL)" : "BAŞARISIZ (UNSUCCESSFUL)";
                cellTik2DegerlendirmeAciklama.Text = data.TikUyesi2.Aciklama.IsNullOrWhiteSpace() ? "" : data.TikUyesi2.Aciklama;
                cellTik2TarihImza.Text = data.TikUyesi2.DegerlendirmeIslemTarihi.ToFormatDateAndTime();
            }
        }

    }
}
