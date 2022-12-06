using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using DevExpress.XtraReports.UI;
using LisansUstuBasvuruSistemi.Models; using LisansUstuBasvuruSistemi.Models.FilterModel;
using BiskaUtil;
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
