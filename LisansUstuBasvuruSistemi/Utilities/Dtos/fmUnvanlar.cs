using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BiskaUtil;
using Entities.Entities;
using LisansUstuBasvuruSistemi.Utilities.Dtos;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class FmUnvanlar : PagerModel
    {
        public int? UnvanSiraNo { get; set; }
        public string UnvanAdi { get; set; }
        public bool? IsAktif { get; set; }
        public int? YetkiGrupID { get; set; }
        public IEnumerable<FrUnvanlar> data { get; set; }

    }

    public class FrUnvanlar : Unvanlar
    {
        public string YetkiGrupAdi { get; set; }
        public int UnvanUserCount { get; set; }
    }
}