using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using LisansUstuBasvuruSistemi.Models;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class TiBasvuruDetayDto : TIBasvuru
    {
        public bool GelenBasvuru { get; set; }
        public string EnstituAdi { get; set; }
        public string ResimAdi { get; set; }
        public string TcKimlikNo { get; set; }
        public string Ad { get; set; }
        public string Soyad { get; set; }
        public string OgrenimTipAdi { get; set; }
        public string AnabilimdaliAdi { get; set; }
        public string ProgramAdi { get; set; }
        public string KayitDonemi { get; set; }
        public Guid? TezDanismaniUserKey{ get; set; }
        public string TezDanismanBilgiEslesen { get; set; }

        public string DurumClassName { get; set; }
        public string DurumColor { get; set; }
        public Guid? DegerlendirenUniqueID { get; set; }
        public List<TiBasvuruAraRaporDto> TIBasvuruAraRaporList { get; set; }
    }
    public class TiBasvuruAraRaporDto : TIBasvuruAraRapor
    {
        public string DonemAdi { get; set; }
        public string TIBasvuruAraaRaporDurumAdi { get; set; }
        public FrTalepler SRModel { get; set; }
    }
}