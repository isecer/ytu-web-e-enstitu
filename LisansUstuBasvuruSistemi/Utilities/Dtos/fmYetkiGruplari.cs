using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BiskaUtil;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class FmYetkiGruplari : PagerOption
    {
        public int YetkiGrupID { get; set; }
        public string YetkiGrupAdi { get; set; }
        public IEnumerable<FrYetkiGruplari> Data { get; set; }
    }
    public class FrYetkiGruplari
    {
        public int YetkiGrupID { get; set; }
        public string YetkiGrupAdi { get; set; }
        public int YetkiSayisi { get; set; }
        public int FbeYetkiliSayisi { get; set; }
        public int SbeYetkiliSayisi { get; set; }
    }
}