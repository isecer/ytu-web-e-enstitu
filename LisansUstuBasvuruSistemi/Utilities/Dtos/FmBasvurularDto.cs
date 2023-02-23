using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Models; using LisansUstuBasvuruSistemi.Utilities.Dtos;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class FmBasvurularDto : PagerOption
    {
        public bool Expand { get; set; }
        public int? BelgeDetailBasvuruID { get; set; }
        public int? UyrukKod { get; set; }
        public string EnstituKod { get; set; }
        public int? BasvuruSurecID { get; set; }
        public int? KullaniciID { get; set; }
        public int? KullaniciTipID { get; set; }
        public string AdSoyad { get; set; }
        public int? ToplamBasvurulanProgram { get; set; }
        public int? BasvuruDurumID { get; set; }
        public int? MulakatSonucTipID { get; set; }
        public int? OgrenimTipKod { get; set; }
        public string ProgramKod { get; set; }
        public string AnabilimDaliKod { get; set; }
        public int? SinavTipKod { get; set; }
        public int? CinsiyetID { get; set; }
        public int? LOgrenimDurumID { get; set; }
        public bool? IsTaahhutVar { get; set; }

        public IEnumerable<FrBasvurularDto> Data { get; set; }
    }
    public class FrBasvurularDto : Basvurular
    {
        public string EnstituKod { get; set; }
        public string EnstituAdi { get; set; }
        public string BasvuruSurecAdi { get; set; } 
        public DateTime BasTar { get; set; }
        public DateTime BitTar { get; set; }
        public string KullaniciTipAdi { get; set; }
        public string AdSoyad { get; set; }
        public int TercihSayisi { get; set; }
        public string BasvuruDurumAdi { get; set; }
        public string DurumClassName { get; set; }
        public string DurumColor { get; set; }
        public bool IsNotDuzelt { get; set; }
        public bool KayitliTercihVar { get; set; }

    }
}