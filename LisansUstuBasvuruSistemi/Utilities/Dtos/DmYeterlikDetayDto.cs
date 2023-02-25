using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using LisansUstuBasvuruSistemi.Models;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class DmYeterlikDetayDto : YeterlikBasvuru
    { 
        public string KayitDonemi { get; set; } 
        public string ResimAdi { get; set; }
        public string AdSoyad { get; set; }
        public string OgrenimTipAdi { get; set; }
        public string AnabilimdaliAdi { get; set; }
        public string ProgramAdi { get; set; }
        public string DanismanAdi { get; set; }
    }
}