using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Models; using LisansUstuBasvuruSistemi.Models.FilterModel;
using LisansUstuBasvuruSistemi.Utilities.Dtos.CmbDtos;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos.FmDtos
{
    public class fmBasvuruSureci : PagerOption
    {
        public string EnstituKod { get; set; }
        public IEnumerable<frBasvuruSureci> Data { get; set; }
    }
    public class frBasvuruSureci : BasvuruSurec
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