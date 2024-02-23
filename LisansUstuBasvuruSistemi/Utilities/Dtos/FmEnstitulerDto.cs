using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BiskaUtil;
using Entities.Entities; using LisansUstuBasvuruSistemi.Utilities.Dtos;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class FmEnstitulerDto : PagerModel
    {
        public string EnstituKod { get; set; }
        public string EnstituAd { get; set; }
        public bool? IsAktif { get; set; }
        public IEnumerable<FrEnstitulerDto> EnstitulerDtos { get; set; }

    }
    public class FrEnstitulerDto : Enstituler
    {
        public string IslemYapan { get; set; }
    }
}