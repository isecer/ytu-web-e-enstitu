namespace LisansUstuBasvuruSistemi.Raporlar.Mezuniyet
{
    public partial class RprMezuniyetMezunlarTutanakYL : DevExpress.XtraReports.UI.XtraReport
    {
        public RprMezuniyetMezunlarTutanakYL(string ogrenciNos = "")
        {
            InitializeComponent();
            ReportFooterOgrenciNo.Visible = ogrenciNos != "";
            cellOgrenciNos.Text = ogrenciNos;
        }

    }
}
