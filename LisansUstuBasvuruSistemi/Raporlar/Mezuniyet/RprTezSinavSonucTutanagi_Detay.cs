using System.Linq;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Business;
using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.Extensions;

namespace LisansUstuBasvuruSistemi.Raporlar.Mezuniyet
{
    public partial class RprTezSinavSonucTutanagi_Detay : DevExpress.XtraReports.UI.XtraReport
    {
        public RprTezSinavSonucTutanagi_Detay(int srTalepId)
        {
            InitializeComponent();

            using (var db = new LisansustuBasvuruSistemiEntities())
            {

                var data = (from s in db.SRTalepleris
                            where s.SRTalepID == srTalepId
                            select new
                            {
                                s.MezuniyetBasvurulari.OgrenimTipKod,
                                s.IsYokDrBursiyeriVar,
                                Danisman = s.SRTaleplerJuris.FirstOrDefault(p => p.JuriTipAdi == "TezDanismani"),
                                TikUyesi1 = s.SRTaleplerJuris.FirstOrDefault(p => p.JuriTipAdi == "TikUyesi1"),
                                TikUyesi2 = s.SRTaleplerJuris.FirstOrDefault(p => p.JuriTipAdi == "TikUyesi2"),
                                JuriUyesi1 = s.SRTaleplerJuris.FirstOrDefault(p => p.JuriTipAdi.Contains("YtuIciJuri")),
                                JuriUyesi2 = s.SRTaleplerJuris.FirstOrDefault(p => p.JuriTipAdi.Contains("YtuDisiJuri")),
                            }).First();


                var isDoktora = data.OgrenimTipKod.IsDoktora();



                cellDanismanUnvanAdSoyad.Text = data.Danisman.UnvanAdi + "\r\n" + data.Danisman.JuriAdi;
                cellDanismanAbdUniversiteAdi.Text = data.Danisman.AnabilimdaliProgramAdi.Trim() + "\r\n" + data.Danisman.UniversiteAdi.Trim();
                cellDanismanDegerlendirmeSonucu.Text = data.Danisman.MezuniyetSinavDurumID == MezuniyetSinavDurumEnum.Basarili ? "BAŞARILI (SUCCESSFUL)" : (data.Danisman.MezuniyetSinavDurumID == MezuniyetSinavDurumEnum.Uzatma ? "DÜZELTME (REVISION)" : "BAŞARISIZ (UNSUCCESSFUL)");
                cellDanismanDegerlendirmeAciklama.Text = data.Danisman.Aciklama.IsNullOrWhiteSpace() ? "" : data.Danisman.Aciklama;
                cellDanismanTarihImza.Text = data.Danisman.DegerlendirmeIslemTarihi.ToFormatDateAndTime() + " \r\nTarihinde Elektronik Olarak Onaylandı";


                sbBandTikUyeleri.Visible = isDoktora;
                if (isDoktora)
                {
                    this.DisplayName = "FR-0377 DOKTORA TEZ SAVUNMA SINAV SONUÇ TUTANAĞI EKİ";
                    cellTik1UnvanAdSoyad.Text = data.TikUyesi1.UnvanAdi + " \r\n" + data.TikUyesi1.JuriAdi;
                    cellTik1AbdUniversiteAdi.Text = data.TikUyesi1.AnabilimdaliProgramAdi.Trim() + "\r\n" + data.TikUyesi1.UniversiteAdi.Trim();
                    cellTik1DegerlendirmeSonucu.Text = data.TikUyesi1.MezuniyetSinavDurumID == MezuniyetSinavDurumEnum.Basarili ? "BAŞARILI (SUCCESSFUL)" : (data.TikUyesi1.MezuniyetSinavDurumID == MezuniyetSinavDurumEnum.Uzatma ? "DÜZELTME (REVISION)" : "BAŞARISIZ (UNSUCCESSFUL)");
                    cellTik1DegerlendirmeAciklama.Text = data.TikUyesi1.Aciklama.IsNullOrWhiteSpace() ? "" : data.TikUyesi1.Aciklama;
                    cellTik1TarihImza.Text = data.TikUyesi1.DegerlendirmeIslemTarihi.ToFormatDateAndTime() + " \r\nTarihinde Elektronik Olarak Onaylandı";

                    cellTik2UnvanAdSoyad.Text = data.TikUyesi2.UnvanAdi + "\r\n" + data.TikUyesi2.JuriAdi;
                    cellTik2AbdUniversiteAdi.Text = data.TikUyesi2.AnabilimdaliProgramAdi.Trim() + "\r\n" + data.TikUyesi2.UniversiteAdi.Trim();
                    cellTik2DegerlendirmeSonucu.Text = data.TikUyesi2.MezuniyetSinavDurumID == MezuniyetSinavDurumEnum.Basarili ? "BAŞARILI (SUCCESSFUL)" : (data.TikUyesi2.MezuniyetSinavDurumID == MezuniyetSinavDurumEnum.Uzatma ? "DÜZELTME (REVISION)" : "BAŞARISIZ (UNSUCCESSFUL)");
                    cellTik2DegerlendirmeAciklama.Text = data.TikUyesi2.Aciklama.IsNullOrWhiteSpace() ? "" : data.TikUyesi2.Aciklama;
                    cellTik2TarihImza.Text = data.TikUyesi2.DegerlendirmeIslemTarihi.ToFormatDateAndTime() + " \r\nTarihinde Elektronik Olarak Onaylandı";
                }
                else
                {

                    this.DisplayName = "FR-0342 YÜKSEK LİSANS SAVUNMA SINAV SONUÇ TUTANAĞI EKİ";
                    lblFormNo.Text = "Form No: FR-0342; Ek-1";
                    lblTutanakAdiTr.Text = "YÜKSEK LİSANS SAVUNMA SINAV SONUÇ TUTANAĞI EKİ";
                    lblTutanakAdiEn.Text = "MSc. THESIS DEFENSE EXAM RESULT REPORT ANNEX";

                }
                cellJuri1UnvanAdSoyad.Text = data.JuriUyesi1.UnvanAdi + " \r\n" + data.JuriUyesi1.JuriAdi;
                cellJuri1AbdUniversiteAdi.Text = data.JuriUyesi1.AnabilimdaliProgramAdi.Trim() + "\r\n" + data.JuriUyesi1.UniversiteAdi.Trim();
                cellJuri1DegerlendirmeSonucu.Text = data.JuriUyesi1.MezuniyetSinavDurumID == MezuniyetSinavDurumEnum.Basarili ? "BAŞARILI (SUCCESSFUL)" : (data.JuriUyesi1.MezuniyetSinavDurumID == MezuniyetSinavDurumEnum.Uzatma ? "DÜZELTME (REVISION)" : "BAŞARISIZ (UNSUCCESSFUL)");
                cellJuri1DegerlendirmeAciklama.Text = data.JuriUyesi1.Aciklama.IsNullOrWhiteSpace() ? "" : data.JuriUyesi1.Aciklama;
                cellJuri1TarihImza.Text = data.JuriUyesi1.DegerlendirmeIslemTarihi.ToFormatDateAndTime() + " \r\nTarihinde Elektronik Olarak Onaylandı";

                cellJuri2UnvanAdSoyad.Text = data.JuriUyesi2.UnvanAdi + "\r\n" + data.JuriUyesi2.JuriAdi;
                cellJuri2AbdUniversiteAdi.Text = data.JuriUyesi2.AnabilimdaliProgramAdi.Trim() + "\r\n" + data.JuriUyesi2.UniversiteAdi.Trim();
                cellJuri2DegerlendirmeSonucu.Text = data.JuriUyesi2.MezuniyetSinavDurumID == MezuniyetSinavDurumEnum.Basarili ? "BAŞARILI (SUCCESSFUL)" : (data.JuriUyesi2.MezuniyetSinavDurumID == MezuniyetSinavDurumEnum.Uzatma ? "DÜZELTME (REVISION)" : "BAŞARISIZ (UNSUCCESSFUL)");
                cellJuri2DegerlendirmeAciklama.Text = data.JuriUyesi2.Aciklama.IsNullOrWhiteSpace() ? "" : data.JuriUyesi2.Aciklama;
                cellJuri2TarihImza.Text = data.JuriUyesi2.DegerlendirmeIslemTarihi.ToFormatDateAndTime() + "\r\nTarihinde Elektronik Olarak Onaylandı";

            }
        }

    }
}
