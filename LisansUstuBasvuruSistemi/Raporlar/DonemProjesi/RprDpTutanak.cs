namespace LisansUstuBasvuruSistemi.Raporlar.DonemProjesi
{
    public partial class RprDpTutanak : DevExpress.XtraReports.UI.XtraReport
    {
        public RprDpTutanak(string enstituAdi)
        {
            InitializeComponent();
            lblTitle.Text = lblTitle.Text.Replace("@EnstituAdi", enstituAdi);
        }

    }
}
