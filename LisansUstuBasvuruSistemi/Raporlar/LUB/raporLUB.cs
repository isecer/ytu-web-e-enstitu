using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using DevExpress.XtraReports.UI;
using LisansUstuBasvuruSistemi.Models; using LisansUstuBasvuruSistemi.Models.FilterModel;
using LisansUstuBasvuruSistemi.Utilities.Enums;

namespace LisansUstuBasvuruSistemi.Raporlar
{
    public partial class raporLUB : DevExpress.XtraReports.UI.XtraReport
    {
        public raporLUB(int BasvuruSurecTipID)
        {
            InitializeComponent();
            if (BasvuruSurecTipID == BasvuruSurecTipi.LisansustuBasvuru) lblRaporAdi.Text = "LİSANSÜSTÜ BAŞVURU SONUÇLARI SAYISAL BİLGİSİ";
            else if (BasvuruSurecTipID == BasvuruSurecTipi.YatayGecisBasvuru) lblRaporAdi.Text = "LİSANSÜSTÜ YATAY GEÇİŞ BAŞVURU SONUÇLARI SAYISAL BİLGİSİ";
            else if (BasvuruSurecTipID == BasvuruSurecTipi.YTUYeniMezunDRBasvuru) lblRaporAdi.Text = "YTU YENİ MEZUN ÖĞRENCİ DOKTORA BAŞVURU SONUÇLARI SAYISAL BİLGİSİ";
        }

    }
}
