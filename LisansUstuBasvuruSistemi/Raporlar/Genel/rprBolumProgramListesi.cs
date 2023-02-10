namespace LisansUstuBasvuruSistemi.Raporlar.Genel
{
    public partial class RprBolumProgramListesi : DevExpress.XtraReports.UI.XtraReport
    {
        public RprBolumProgramListesi(string Enst)
        {
            InitializeComponent();
            lblEnstituAdi.Text = Enst;
        }

    }
}
