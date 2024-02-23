using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using DevExpress.XtraReports.UI;
using Entities.Entities; using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Enums;

namespace LisansUstuBasvuruSistemi.Raporlar
{
    public partial class raporLUB : DevExpress.XtraReports.UI.XtraReport
    {
        public raporLUB(int BasvuruSurecTipID)
        {
            InitializeComponent();
            if (BasvuruSurecTipID == BasvuruSurecTipiEnum.LisansustuBasvuru) lblRaporAdi.Text = "LİSANSÜSTÜ BAŞVURU SONUÇLARI SAYISAL BİLGİSİ";
            else if (BasvuruSurecTipID == BasvuruSurecTipiEnum.YatayGecisBasvuru) lblRaporAdi.Text = "LİSANSÜSTÜ YATAY GEÇİŞ BAŞVURU SONUÇLARI SAYISAL BİLGİSİ";
            else if (BasvuruSurecTipID == BasvuruSurecTipiEnum.YTUYeniMezunDRBasvuru) lblRaporAdi.Text = "YTÜ YENİ MEZUN ÖĞRENCİ DOKTORA BAŞVURU SONUÇLARI SAYISAL BİLGİSİ";
        }

    }
}
