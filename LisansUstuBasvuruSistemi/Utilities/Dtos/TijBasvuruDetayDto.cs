using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Entities.Entities;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class TijBasvuruDetayDto : TijBasvuru
    {

        public bool GelenBasvuru { get; set; }
        public string DurumHtmlString { get; set; }
        public string DonemHtmlString { get; set; }
        public string EnstituAdi { get; set; }
        public string ResimAdi { get; set; }
        public string TcKimlikNo { get; set; }
        public string Ad { get; set; }
        public string Soyad { get; set; }
        public string OgrenimTipAdi { get; set; }
        public string AnabilimdaliAdi { get; set; }
        public string ProgramAdi { get; set; }
        public string KayitDonemi { get; set; }
        public Guid? TezDanismaniUserKey { get; set; }
        public string TezDanismanBilgiEslesen { get; set; }

        public string DurumClassName { get; set; }
        public string DurumColor { get; set; }
        public List<TijBasvuruOneriDetayDto> TijBasvuruOneriList { get; set; }
    }
    public class TijBasvuruOneriDetayDto : TijBasvuruOneri
    {
        public bool IsSonBasvuru { get; set; }
        public string DonemAdi { get; set; }
        public Guid? TezDanismaniUserKey { get; set; }
        public string DanismanAdi { get; set; }
        public string TijFormTipAdi{ get; set; }
        public string TijDegisiklikTipAdi { get; set; } 
    }
}