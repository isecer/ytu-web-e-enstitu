using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using DevExpress.XtraReports.UI;

namespace LisansUstuBasvuruSistemi.Raporlar
{
    public partial class rprBolumProgramListesi : DevExpress.XtraReports.UI.XtraReport
    {
        public rprBolumProgramListesi(string Enst)
        {
            InitializeComponent();
            lblEnstituAdi.Text = Enst;
        }

    }
}
