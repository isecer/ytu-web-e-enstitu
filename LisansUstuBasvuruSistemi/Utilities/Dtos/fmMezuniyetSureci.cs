using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Models; using LisansUstuBasvuruSistemi.Utilities.Dtos;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class FmMezuniyetSureci : PagerOption
    {
        public string EnstituKod { get; set; }
        public IEnumerable<FrMezuniyetSureci> Data { get; set; }
    }
    public class FrMezuniyetSureci : MezuniyetSureci
    {
        public bool Hesaplandi { get; set; }
        public string EnstituAdi { get; set; }
        public string DilAdi { get; set; }
        public string DilFlagClass { get; set; }
        public string IslemYapan { get; set; }
        public string DonemAdi { get; set; }
        public int OTCount { get; set; }
    }
}