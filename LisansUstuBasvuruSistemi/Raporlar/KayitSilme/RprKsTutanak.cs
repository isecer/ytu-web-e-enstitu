namespace LisansUstuBasvuruSistemi.Raporlar.KayitSilme
{
    public partial class RprKsTutanak : DevExpress.XtraReports.UI.XtraReport
    {
        public RprKsTutanak(string enstituAdi, bool isOnayMakamiEykOrEnstituMudur )
        {
            InitializeComponent();
            var title =
                "YTÜ \r\n@EnstituAdi\r\nKAYIT SİLDİRME BAŞVURULARI\r\n.../... GÜN VE SAYILI ENSTİTÜ YÖNETİM KURULU\r\n(EK-....)";

            if (isOnayMakamiEykOrEnstituMudur == false)
            {
                title =
                    "YTÜ \r\n@EnstituAdi\r\nKAYIT SİLDİRME BAŞVURULARI\r\n.../... TARİHLİ ENSTİTÜ MÜDÜRLÜĞÜ KARARLARI\r\n(EK-....)";
            }
            lblTitle.Text = title.Replace("@EnstituAdi", enstituAdi);
           
        }

    }
}
