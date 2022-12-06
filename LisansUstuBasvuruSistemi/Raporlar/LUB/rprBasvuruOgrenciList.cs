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
    public partial class rprBasvuruOgrenciList : DevExpress.XtraReports.UI.XtraReport
    {
        public rprBasvuruOgrenciList(int id)
        {
            InitializeComponent();
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var bsurec = db.BasvuruSurecs.Where(p => p.BasvuruSurecID == id).First();
                string surec = bsurec.BaslangicYil + " / " + bsurec.BitisYil + " " + bsurec.Donemler.DonemAdi;
                if (bsurec.BasvuruSurecTipID == BasvuruSurecTipi.LisansustuBasvuru) surec += "Lisansüstü Başvuruları Sınav Giriş Listesi";
                else if (bsurec.BasvuruSurecTipID == BasvuruSurecTipi.LisansustuBasvuru) surec += " Lisansüstü Yatay Geçiş Başvuruları Sınav Giriş Listesi";
                else surec += " YTU Yeni Mezun Doktora Başvuruları Sınav Giriş Listesi";
                lbllblDonemBilgi.Text = surec;
                string logoPath = "/Content/assets/images/ytu_logo_tr.png";
                rprLogo.ImageUrl = logoPath;

            }
        }
    }
}
