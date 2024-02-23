using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Entities.Entities;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class KrOgrenimTip : BasvuruSurecOgrenimTipleri
    {
        public string EnstituKod { get; set; }
        public bool? Success { get; set; }
        public bool OrjinalVeri { get; set; }
        public bool OTipiniAyir { get; set; }
        public string GrupAdi { get; set; }
        public string OgrenimTipAdi { get; set; }
        public List<int> SecilenBSOTIDs { get; set; }

        public KrOgrenimTip()
        {
            SecilenBSOTIDs = new List<int>();

        }
    }
}