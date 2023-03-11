using BiskaUtil;
using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using System.Linq;

namespace LisansUstuBasvuruSistemi.Raporlar
{
    public partial class rprBasvuruSonucPuanList : DevExpress.XtraReports.UI.XtraReport
    {
        public rprBasvuruSonucPuanList(int id)
        {
            InitializeComponent();
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var bsurec = db.BasvuruSurecs.Where(p => p.BasvuruSurecID == id).First();
                string surec = bsurec.BaslangicYil + " / " + bsurec.BitisYil + " " + bsurec.Donemler.DonemAdi;
                if (bsurec.BasvuruSurecTipID == BasvuruSurecTipi.LisansustuBasvuru) surec += "Lisansüstü Başvuruları Değerlendirme Listesi";
                else if (bsurec.BasvuruSurecTipID == BasvuruSurecTipi.YatayGecisBasvuru) surec += " Lisansüstü Yatay Geçiş Başvuruları Değerlendirme Listesi";
                else surec += " YTÜ Yeni Mezun Doktora Başvuruları Değerlendirme Listesi";
                lbllblDonemBilgi.Text = surec;
                lblUniAdi.Text = "YILDIZ TEKNİK ÜNİVERSİTESİ";
                string logoPath = "/Content/assets/images/ytu_logo_tr.png";
                rprLogo.ImageUrl = logoPath;

                tCel_SNO.Text = "S.No";
                tCel_AdSoyad.Text = "Ad Soyad";
                tCel_AlesPuan.Text = "Ales P.";
                tCel_Agno.Text = "AGNO";
                tCel_YaziliNotu.Text = "Yazılı Notu";
                tCel_SozluNotu.Text = "Sözlü Notu";
                tCel_GirisSinavNotu.Text = "Giriş Sınav Notu";
                tCel_BasariNotu.Text ="Başarı Notu";
                tCel_TercihNo.Text ="Tercih No";
                tCel_Durum.Text = "Durum";
                gFtr_ProgramKontenjani.Text = "Program Kota";
            }
        }
    }
}
