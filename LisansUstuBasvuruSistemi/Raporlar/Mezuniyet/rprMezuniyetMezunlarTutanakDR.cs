namespace LisansUstuBasvuruSistemi.Raporlar.Mezuniyet
{
    public partial class RprMezuniyetMezunlarTutanakDr : DevExpress.XtraReports.UI.XtraReport
    {
        public RprMezuniyetMezunlarTutanakDr(string ogrenciNos = "")
        {
            InitializeComponent();
            ReportFooterOgrenciNo.Visible = ogrenciNos != "";
            cellOgrenciNos.Text = ogrenciNos;
        }

    }
}
