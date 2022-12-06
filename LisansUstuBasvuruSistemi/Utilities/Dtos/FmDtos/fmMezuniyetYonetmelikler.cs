using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Models; using LisansUstuBasvuruSistemi.Models.FilterModel;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos.FmDtos
{
    public class fmMezuniyetYonetmelikler : PagerOption
    {
        public string EnstituKod { get; set; }
        public int? TarihKriterID { get; set; }
        public IEnumerable<frMezuniyetYonetmelikler> Data { get; set; }
    }
    public class frMezuniyetYonetmelikler : MezuniyetYonetmelikleri
    {
        public string EnstituAdi { get; set; }
        public string DilAdi { get; set; }
        public string DilFlagClass { get; set; }
        public string IslemYapan { get; set; }
        public string TarihKriterAdi { get; set; }
        public string DonemAdi { get; set; }
        public string DonemAdiB { get; set; }

        public List<krMezuniyetYonetmelikOT> MezuniyetYonetmelikData { get; set; }
        public frMezuniyetYonetmelikler()
        {
            MezuniyetYonetmelikData = new List<krMezuniyetYonetmelikOT>();
        }
    }
}