using System.Linq;
using Entities.Entities;
using LisansUstuBasvuruSistemi.Utilities.Enums;

namespace LisansUstuBasvuruSistemi.Raporlar.LUB
{
    public partial class RprBasvuruOgrenciPuanList : DevExpress.XtraReports.UI.XtraReport
    {
        public RprBasvuruOgrenciPuanList(int id)
        {
            InitializeComponent();
            using (var  entities = new LubsDbEntities())
            {
                var bsurec = entities.BasvuruSurecs.First(p => p.BasvuruSurecID == id); 
                string surec = bsurec.BaslangicYil + " / " + bsurec.BitisYil + " " + bsurec.Donemler.DonemAdi;
                if (bsurec.BasvuruSurecTipID==BasvuruSurecTipiEnum.LisansustuBasvuru) surec += "Lisansüstü Başvuruları Değerlendirme Sonuç Listesi";
                else surec += " Lisansüstü Yatay Geçiş Başvuruları Değerlendirme Sonuç Listesi";
                lbllblDonemBilgi.Text = surec;
              
            }
        }
    }
}
