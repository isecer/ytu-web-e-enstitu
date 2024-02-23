using System.Linq;
using Entities.Entities;
using LisansUstuBasvuruSistemi.Utilities.Enums;

namespace LisansUstuBasvuruSistemi.Raporlar.LUB
{
    public partial class RprBasvuruMulakatsizOgrenciList : DevExpress.XtraReports.UI.XtraReport
    {
        public RprBasvuruMulakatsizOgrenciList(int id)
        {
            InitializeComponent();
            using (var entities = new LubsDbEntities())
            {
                var bsurec = entities.BasvuruSurecs.First(p => p.BasvuruSurecID == id);
                string surec = bsurec.BaslangicYil + " / " + bsurec.BitisYil + " " + bsurec.Donemler.DonemAdi;
                if (bsurec.BasvuruSurecTipID == BasvuruSurecTipiEnum.LisansustuBasvuru) surec += " Lisansüstü Başvuruları Öğrenci Listesi";
                else if (bsurec.BasvuruSurecTipID == BasvuruSurecTipiEnum.YatayGecisBasvuru) surec += " Lisansüstü Yatay Geçiş Başvuruları Öğrenci Listesi";
                else surec += " YTÜ Yeni Mezun Doktora Başvuruları Öğrenci Listesi";
                lbllblDonemBilgi.Text = surec;
                string logoPath = "/Content/assets/images/ytu_logo_tr.png";
                rprLogo.ImageUrl = logoPath;
            }
        }
    }
}
