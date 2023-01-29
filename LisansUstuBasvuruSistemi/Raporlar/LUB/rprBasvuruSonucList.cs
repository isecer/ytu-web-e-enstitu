using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using DevExpress.XtraReports.UI;
using LisansUstuBasvuruSistemi.Models; using LisansUstuBasvuruSistemi.Utilities.Dtos;
using BiskaUtil;
using System.Linq;
using LisansUstuBasvuruSistemi.Utilities.Enums;

namespace LisansUstuBasvuruSistemi.Raporlar
{
    public partial class rprBasvuruSonucList : DevExpress.XtraReports.UI.XtraReport
    {
        public rprBasvuruSonucList(int id, int EkBilgiTipID)
        {
            InitializeComponent();


            cpt_AdSoyad.Text = "Ad Soyad";
            cpt_Durum.Text = "Durum";
            cpt_Sira.Text = "S.No";
            cpt_Tel.Text = "Telefon";
            cpt_Durum.Text = "Durum";
            cpt_BasariNotu.Text ="Başarı Notu";
            if (EkBilgiTipID == 1) //İletişim
            {

                xrTable2.DeleteColumn(cpt_Tel);
                xrTable1.DeleteColumn(Rw_Tel);
                xrTable2.DeleteColumn(cpt_EMail);
                xrTable1.DeleteColumn(Rw_Email);
                xrTable2.DeleteColumn(cpt_BasariNotu);
                xrTable1.DeleteColumn(Rw_BasariNotu);
                cpt_Sira.WidthF = float.Parse("44,11");
                Rw_Sira.WidthF = float.Parse("44,11");
                cpt_AdSoyad.WidthF = float.Parse("545,08");
                Rw_AdSoyad.WidthF = float.Parse("545,08");
                cpt_Durum.WidthF = float.Parse("152,43");
                Rw_Durum.WidthF = float.Parse("152,43");
            }
            else if (EkBilgiTipID == 2) //GBNO
            {
                xrTable2.DeleteColumn(cpt_Tel);
                xrTable1.DeleteColumn(Rw_Tel);
                xrTable2.DeleteColumn(cpt_EMail);
                xrTable1.DeleteColumn(Rw_Email);
                cpt_Sira.WidthF = float.Parse("44,11");
                Rw_Sira.WidthF = float.Parse("44,11");
                cpt_AdSoyad.WidthF = float.Parse("545,08");
                Rw_AdSoyad.WidthF = float.Parse("545,08");
                cpt_Durum.WidthF = float.Parse("130");
                Rw_Durum.WidthF = float.Parse("130");
                cpt_BasariNotu.WidthF = float.Parse("60,11");
                Rw_BasariNotu.WidthF = float.Parse("60,11");
            }
            else
            {
                xrTable2.DeleteColumn(cpt_BasariNotu);
                xrTable1.DeleteColumn(Rw_BasariNotu);
            }
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var bsurec = db.BasvuruSurecs.Where(p => p.BasvuruSurecID == id).First();
                string surec = bsurec.BaslangicYil + " / " + bsurec.BitisYil + " " + bsurec.Donemler.DonemAdi;
                if (bsurec.BasvuruSurecTipID == BasvuruSurecTipi.LisansustuBasvuru) surec += "Lisansüstü Başvuruları Değerlendirme Listesi";
                else if (bsurec.BasvuruSurecTipID == BasvuruSurecTipi.YatayGecisBasvuru) surec += " Lisansüstü Yatay Geçiş Başvuruları Değerlendirme Listesi";
                else surec += " YTU Yeni Mezun Doktora Başvuruları Değerlendirme Listesi";
                lbllblDonemBilgi.Text = surec;
                lblUniAdi.Text = "YILDIZ TEKNİK ÜNİVERSİTESİ";
                string logoPath = "/Content/assets/images/ytu_logo_tr.png";
                rprLogo.ImageUrl = logoPath;

            }
        }
    }
}
