using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Models; using LisansUstuBasvuruSistemi.Utilities.Dtos;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class fmTalepSurec : PagerOption
    {
        public bool? IsAktif { get; set; }
        public string EnstituKod { get; set; }
        public IEnumerable<frTalepSurec> Data { get; set; }
    }
    public class frTalepSurec : TalepSurecleri
    {
        public string EnstituAdi { get; set; }
        public string IslemYapan { get; set; }
        public bool AktifSurec { get; set; }
    }
}