using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BiskaUtil;
using Entities.Entities; using LisansUstuBasvuruSistemi.Utilities.Dtos;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class FmOzelTanimlar : PagerModel
    { 
        public int? SROzelTanimTipID { get; set; }
        public string Aciklama { get; set; }
        public bool? IsAktif { get; set; }
        public IEnumerable<FrOzelTanimlar> FrOzelTanimlars { get; set; }

    }
    public class FrOzelTanimlar : SROzelTanimlar
    { 
        public string SROzelTanimTipAdi { get; set; }
        public string TalepTipAdi { get; set; }
        public string SalonAdi { get; set; }
        public string AyAdi { get; set; }
        public string IslemYapan { get; set; }
    }
}