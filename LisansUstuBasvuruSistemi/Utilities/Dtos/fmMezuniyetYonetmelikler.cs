using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BiskaUtil;
using Entities.Entities; using LisansUstuBasvuruSistemi.Utilities.Dtos;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class FmMezuniyetYonetmelikler : PagerModel
    {
        public string EnstituKod { get; set; }
        public int? TarihKriterID { get; set; }
        public IEnumerable<FrMezuniyetYonetmelikler> Data { get; set; }
    }
    public class FrMezuniyetYonetmelikler : MezuniyetYonetmelikleri
    {
        public string EnstituAdi { get; set; }
        public string DilAdi { get; set; }
        public string DilFlagClass { get; set; }
        public string IslemYapan { get; set; }
        public string TarihKriterAdi { get; set; }
        public string DonemAdi { get; set; }
        public string DonemAdiB { get; set; }

        public List<KrMezuniyetYonetmelikOt> MezuniyetYonetmelikData { get; set; }
        public FrMezuniyetYonetmelikler()
        {
            MezuniyetYonetmelikData = new List<KrMezuniyetYonetmelikOt>();
        }
    }
}