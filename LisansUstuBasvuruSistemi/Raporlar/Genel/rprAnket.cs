using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using DevExpress.XtraReports.UI;
using DevExpress.XtraReports.Parameters;
using System.Linq;

namespace LisansUstuBasvuruSistemi.Raporlar
{
    public partial class rprAnket : DevExpress.XtraReports.UI.XtraReport
    { 
        public rprAnket( string EnstituAdi,string AnketAdi,string Tarih)
        {
            InitializeComponent();
            lblEnstituAdi.Text = EnstituAdi;
            lblRaporAdi.Text = AnketAdi;
            lblTarih.Text = Tarih;
          
        }

        private void rprAnket_BeforePrint(object sender, System.Drawing.Printing.PrintEventArgs e)
        {
            //System.Reflection.PropertyInfo pi = DetailReport.GetType().GetProperty("DisplayableRowCount", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            //int displayableRowCount = Convert.ToInt32(pi.GetValue(DetailReport, null));
            //if (displayableRowCount == 0)
            //    this.xrTable2.Visible = false;//Controls from group header
            //else
            //    this.xrTable2.Visible = true;

            //int displayableRowCount1 = Convert.ToInt32(pi.GetValue(DetailReport1, null));
            //if (displayableRowCount1 == 0)
            //    this.xrTable4.Visible = false;//Controls from group header
            //else
            //    this.xrTable4.Visible = true;

            
        }
    }
}
