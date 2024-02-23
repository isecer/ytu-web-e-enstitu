using System.Linq;
using Entities.Entities;
using LisansUstuBasvuruSistemi.Utilities.Enums;

namespace LisansUstuBasvuruSistemi.Raporlar.LUB
{
    public partial class RprBasvuruSonucList : DevExpress.XtraReports.UI.XtraReport
    {
        public RprBasvuruSonucList(int id, int EkBilgiTipID)
        {
            InitializeComponent();


            cpt_AdSoyad.Text = "Ad Soyad";
            cpt_Durum.Text = "Durum";
            cpt_Sira.Text = "S.No";
            cpt_Tel.Text = "Telefon";
            cpt_Durum.Text = "Durum";
            cpt_BasariNotu.Text = "Başarı Notu";
            if (EkBilgiTipID == 1) //İletişim
            {

                xrTable2.DeleteColumn(cpt_Tel);
                xrTable1.DeleteColumn(Rw_Tel);
                xrTable2.DeleteColumn(cpt_EMail);
                xrTable1.DeleteColumn(Rw_Email);
                xrTable2.DeleteColumn(cpt_BasariNotu);
                xrTable1.DeleteColumn(Rw_BasariNotu);
                cpt_Sira.WidthF = float.Parse("44,11");
                Rw_Sira.WidthF = float.Parse("44,11");
                cpt_AdSoyad.WidthF = float.Parse("545,08");
                Rw_AdSoyad.WidthF = float.Parse("545,08");
                cpt_Durum.WidthF = float.Parse("152,43");
                Rw_Durum.WidthF = float.Parse("152,43");
            }
            else if (EkBilgiTipID == 2) //GBNO
            {
                xrTable2.DeleteColumn(cpt_Tel);
                xrTable1.DeleteColumn(Rw_Tel);
                xrTable2.DeleteColumn(cpt_EMail);
                xrTable1.DeleteColumn(Rw_Email);
                cpt_Sira.WidthF = float.Parse("44,11");
                Rw_Sira.WidthF = float.Parse("44,11");
                cpt_AdSoyad.WidthF = float.Parse("545,08");
                Rw_AdSoyad.WidthF = float.Parse("545,08");
                cpt_Durum.WidthF = float.Parse("130");
                Rw_Durum.WidthF = float.Parse("130");
                cpt_BasariNotu.WidthF = float.Parse("60,11");
                Rw_BasariNotu.WidthF = float.Parse("60,11");
            }
            else
            {
                xrTable2.DeleteColumn(cpt_BasariNotu);
                xrTable1.DeleteColumn(Rw_BasariNotu);
            }
            using (var entities = new LubsDbEntities())
            {
                var bsurec = entities.BasvuruSurecs.First(p => p.BasvuruSurecID == id);
                var surecAdi = bsurec.BaslangicYil + " / " + bsurec.BitisYil + " " + bsurec.Donemler.DonemAdi;
                switch (bsurec.BasvuruSurecTipID)
                {
                    case BasvuruSurecTipiEnum.LisansustuBasvuru:
                        surecAdi += " Lisansüstü Başvuruları Değerlendirme Listesi";
                        break;
                    case BasvuruSurecTipiEnum.YatayGecisBasvuru:
                        surecAdi += " Lisansüstü Yatay Geçiş Başvuruları Değerlendirme Listesi";
                        break;
                    default:
                        surecAdi += " YTÜ Yeni Mezun Doktora Başvuruları Değerlendirme Listesi";
                        break;
                }
                lbllblDonemBilgi.Text = surecAdi;
                lblUniAdi.Text = "YILDIZ TEKNİK ÜNİVERSİTESİ";
                var logoPath = "/Content/assets/images/ytu_logo_tr.png";
                rprLogo.ImageUrl = logoPath;

            }
        }
    }
}
