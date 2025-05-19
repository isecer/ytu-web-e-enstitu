namespace LisansUstuBasvuruSistemi.Raporlar.KayitSilme
{
    public partial class RprKsTutanak : DevExpress.XtraReports.UI.XtraReport
    {
        public RprKsTutanak(string enstituAdi)
        {
            InitializeComponent();
            lblTitle.Text = lblTitle.Text.Replace("@EnstituAdi", enstituAdi);
        }

    }
}
