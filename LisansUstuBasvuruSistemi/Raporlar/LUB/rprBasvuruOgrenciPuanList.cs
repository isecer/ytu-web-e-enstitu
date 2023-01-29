using BiskaUtil;
using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using System.Linq;

namespace LisansUstuBasvuruSistemi.Raporlar
{
    public partial class rprBasvuruOgrenciPuanList : DevExpress.XtraReports.UI.XtraReport
    {
        public rprBasvuruOgrenciPuanList(int id)
        {
            InitializeComponent();
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var bsurec = db.BasvuruSurecs.Where(p => p.BasvuruSurecID == id).First(); 
                string surec = bsurec.BaslangicYil + " / " + bsurec.BitisYil + " " + bsurec.Donemler.DonemAdi;
                if (bsurec.BasvuruSurecTipID==BasvuruSurecTipi.LisansustuBasvuru) surec += "Lisansüstü Başvuruları Değerlendirme Sonuç Listesi";
                else surec += " Lisansüstü Yatay Geçiş Başvuruları Değerlendirme Sonuç Listesi";
                lbllblDonemBilgi.Text = surec;
              
            }
        }
    }
}
