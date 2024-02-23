using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BiskaUtil;
using Entities.Entities; using LisansUstuBasvuruSistemi.Utilities.Dtos;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class FmTalepSurec : PagerModel
    {
        public bool? IsAktif { get; set; }
        public string EnstituKod { get; set; }
        public IEnumerable<FrTalepSurec> Data { get; set; }
    }
    public class FrTalepSurec : TalepSurecleri
    {
        public string EnstituAdi { get; set; }
        public string IslemYapan { get; set; }
        public bool AktifSurec { get; set; }
    }
}