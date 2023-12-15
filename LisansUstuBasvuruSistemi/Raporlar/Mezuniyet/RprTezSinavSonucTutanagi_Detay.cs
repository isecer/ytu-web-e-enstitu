using System.Collections.Generic;
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
                                TikUyeleris = s.SRTaleplerJuris.Where(p => p.JuriTipAdi != "TezDanismani").ToList(),

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
                    var tikUyesi1 = data.TikUyeleris.First(p => p.JuriTipAdi == "TikUyesi1");
                    var tikUyesi2 = data.TikUyeleris.First(p => p.JuriTipAdi == "TikUyesi2");

                    this.DisplayName = "FR-0377 DOKTORA TEZ SAVUNMA SINAV SONUÇ TUTANAĞI EKİ";
                    cellTik1UnvanAdSoyad.Text = tikUyesi1.UnvanAdi + " \r\n" + tikUyesi1.JuriAdi;
                    cellTik1AbdUniversiteAdi.Text = tikUyesi1.AnabilimdaliProgramAdi.Trim() + "\r\n" + tikUyesi1.UniversiteAdi.Trim();
                    cellTik1DegerlendirmeSonucu.Text = tikUyesi1.MezuniyetSinavDurumID == MezuniyetSinavDurumEnum.Basarili ? "BAŞARILI (SUCCESSFUL)" : (tikUyesi1.MezuniyetSinavDurumID == MezuniyetSinavDurumEnum.Uzatma ? "DÜZELTME (REVISION)" : "BAŞARISIZ (UNSUCCESSFUL)");
                    cellTik1DegerlendirmeAciklama.Text = tikUyesi1.Aciklama.IsNullOrWhiteSpace() ? "" : tikUyesi1.Aciklama;
                    cellTik1TarihImza.Text = tikUyesi1.DegerlendirmeIslemTarihi.ToFormatDateAndTime() + " \r\nTarihinde Elektronik Olarak Onaylandı";

                    cellTik2UnvanAdSoyad.Text = tikUyesi2.UnvanAdi + "\r\n" + tikUyesi2.JuriAdi;
                    cellTik2AbdUniversiteAdi.Text = tikUyesi2.AnabilimdaliProgramAdi.Trim() + "\r\n" + tikUyesi2.UniversiteAdi.Trim();
                    cellTik2DegerlendirmeSonucu.Text = tikUyesi2.MezuniyetSinavDurumID == MezuniyetSinavDurumEnum.Basarili ? "BAŞARILI (SUCCESSFUL)" : (tikUyesi2.MezuniyetSinavDurumID == MezuniyetSinavDurumEnum.Uzatma ? "DÜZELTME (REVISION)" : "BAŞARISIZ (UNSUCCESSFUL)");
                    cellTik2DegerlendirmeAciklama.Text = tikUyesi2.Aciklama.IsNullOrWhiteSpace() ? "" : tikUyesi2.Aciklama;
                    cellTik2TarihImza.Text = tikUyesi2.DegerlendirmeIslemTarihi.ToFormatDateAndTime() + " \r\nTarihinde Elektronik Olarak Onaylandı";
                }
                else
                {

                    this.DisplayName = "FR-0342 YÜKSEK LİSANS SAVUNMA SINAV SONUÇ TUTANAĞI EKİ";
                    lblFormNo.Text = "Form No: FR-0342; Ek-1";
                    lblTutanakAdiTr.Text = "YÜKSEK LİSANS SAVUNMA SINAV SONUÇ TUTANAĞI EKİ";
                    lblTutanakAdiEn.Text = "MSc. THESIS DEFENSE EXAM RESULT REPORT ANNEX";

                }

                var juris = data.TikUyeleris.Where(p => p.JuriTipAdi.Contains("Juri")).ToList();
                var juriUyesi1 = juris[0];
                var juriUyesi2 = juris[1];

                cellJuri1UnvanAdSoyad.Text = juriUyesi1.UnvanAdi + " \r\n" + juriUyesi1.JuriAdi;
                cellJuri1AbdUniversiteAdi.Text = juriUyesi1.AnabilimdaliProgramAdi.Trim() + "\r\n" + juriUyesi1.UniversiteAdi.Trim();
                cellJuri1DegerlendirmeSonucu.Text = juriUyesi1.MezuniyetSinavDurumID == MezuniyetSinavDurumEnum.Basarili ? "BAŞARILI (SUCCESSFUL)" : (juriUyesi1.MezuniyetSinavDurumID == MezuniyetSinavDurumEnum.Uzatma ? "DÜZELTME (REVISION)" : "BAŞARISIZ (UNSUCCESSFUL)");
                cellJuri1DegerlendirmeAciklama.Text = juriUyesi1.Aciklama.IsNullOrWhiteSpace() ? "" : juriUyesi1.Aciklama;
                cellJuri1TarihImza.Text = juriUyesi1.DegerlendirmeIslemTarihi.ToFormatDateAndTime() + " \r\nTarihinde Elektronik Olarak Onaylandı";

                cellJuri2UnvanAdSoyad.Text = juriUyesi2.UnvanAdi + "\r\n" + juriUyesi2.JuriAdi;
                cellJuri2AbdUniversiteAdi.Text = juriUyesi2.AnabilimdaliProgramAdi.Trim() + "\r\n" + juriUyesi2.UniversiteAdi.Trim();
                cellJuri2DegerlendirmeSonucu.Text = juriUyesi2.MezuniyetSinavDurumID == MezuniyetSinavDurumEnum.Basarili ? "BAŞARILI (SUCCESSFUL)" : (juriUyesi2.MezuniyetSinavDurumID == MezuniyetSinavDurumEnum.Uzatma ? "DÜZELTME (REVISION)" : "BAŞARISIZ (UNSUCCESSFUL)");
                cellJuri2DegerlendirmeAciklama.Text = juriUyesi2.Aciklama.IsNullOrWhiteSpace() ? "" : juriUyesi2.Aciklama;
                cellJuri2TarihImza.Text = juriUyesi2.DegerlendirmeIslemTarihi.ToFormatDateAndTime() + "\r\nTarihinde Elektronik Olarak Onaylandı";

            }
        }

    }
}
