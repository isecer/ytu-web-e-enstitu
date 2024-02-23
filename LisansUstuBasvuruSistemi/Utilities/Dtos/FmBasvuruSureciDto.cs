using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BiskaUtil;
using Entities.Entities; using LisansUstuBasvuruSistemi.Utilities.Dtos;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class FmBasvuruSureciDto : PagerModel
    {
        public string EnstituKod { get; set; }
        public IEnumerable<FrBasvuruSureciDto> FrBasvuruSureciDtos { get; set; }
    }
    public class FrBasvuruSureciDto : BasvuruSurec
    {
        public bool Hesaplandi { get; set; }
        public string EnstituAdi { get; set; }
        public string Kota_BasvuruSurecKontrolTipAdi { get; set; }
        public string DilAdi { get; set; }
        public string DilFlagClass { get; set; }
        public string IslemYapan { get; set; }
        public string DonemAdi { get; set; }
        public int OTCount { get; set; }
        public List<CmbIntDto> CmbOgrenimTipBilgi { get; set; }
    }
}