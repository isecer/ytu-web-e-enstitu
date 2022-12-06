using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Models; using LisansUstuBasvuruSistemi.Models.FilterModel;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos.FmDtos
{
    public class fmMezuniyetSureci : PagerOption
    {
        public string EnstituKod { get; set; }
        public IEnumerable<frMezuniyetSureci> Data { get; set; }
    }
    public class frMezuniyetSureci : MezuniyetSureci
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