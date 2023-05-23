namespace LisansUstuBasvuruSistemi.Raporlar.Mezuniyetx
{
    public partial class RprMezuniyetTezJuriTutanak : DevExpress.XtraReports.UI.XtraReport
    {
        public RprMezuniyetTezJuriTutanak(bool isDoktoraOrYl)
        {
            InitializeComponent();

            if (!isDoktoraOrYl)
            {
                trTik1.Visible = false;
                trTik2.Visible = false;
                Detail.HeightF = Detail.HeightF - 50;

            }
 
        }

    }
}
