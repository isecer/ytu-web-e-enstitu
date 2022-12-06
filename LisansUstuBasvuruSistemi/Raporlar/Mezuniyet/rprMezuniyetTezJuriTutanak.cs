using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using DevExpress.XtraReports.UI;

namespace LisansUstuBasvuruSistemi.Raporlar
{
    public partial class rprMezuniyetTezJuriTutanak : DevExpress.XtraReports.UI.XtraReport
    {
        public rprMezuniyetTezJuriTutanak(bool IsDoktoraOrYL)
        {
            InitializeComponent();

            if (!IsDoktoraOrYL)
            {
                trTik1.Visible = false;
                trTik2.Visible = false;
                Detail.HeightF = Detail.HeightF - 50;

            }
 
        }

    }
}
