using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using LisansUstuBasvuruSistemi.Models;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class BasvuruSurecDetayDto : BasvuruSurec
    {
        public int SelectedTabIndex { get; set; }
        public bool IsDelete { get; set; }
        public string EnstituAdi { get; set; }
        public string Kota_BasvuruSurecKontrolTipAdi { get; set; }
        public string DilAdi { get; set; }
        public string DilFlagClass { get; set; }
        public string IslemYapan { get; set; }
        public string DonemAdi { get; set; }
        public MIndexBilgi ToplamBasvuruBilgisi { get; set; }
        public List<mulakatSturModel> MulakatSTurModel { get; set; }
        public List<krOgrenimTip> OgrenimTipleriLst { get; set; }
        public List<frKotalar> ProgramKotaLst { get; set; }
        public List<krSinavTipleri> SinavTipleri { get; set; }
        public fmMulakatNotGiris MulakatBilgi { get; set; }
        public fmMulakatSonuc MulakatSonucu { get; set; }
        public List<frAnketDetay> AnketDetay { get; set; }
        public int ToplamOnaylananBasvuru { get; set; }
        public List<CmbIntDto> CmbOgrenimTipBilgi { get; set; }
        public BasvuruSurecDetayDto()
        {
            CmbOgrenimTipBilgi = new List<CmbIntDto>();
            MulakatSonucu = new fmMulakatSonuc();
            MulakatBilgi = new fmMulakatNotGiris();
            AnketDetay = new List<frAnketDetay>();
        }
    }
}