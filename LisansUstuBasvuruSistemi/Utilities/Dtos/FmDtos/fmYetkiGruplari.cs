using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BiskaUtil;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos.FmDtos
{
    public class fmYetkiGruplari : PagerOption
    {
        public int YetkiGrupID { get; set; }
        public string YetkiGrupAdi { get; set; }
        public IEnumerable<frYetkiGruplari> Data { get; set; }
    }
    public class frYetkiGruplari
    {
        public int YetkiGrupID { get; set; }
        public string YetkiGrupAdi { get; set; }
        public int YetkiSayisi { get; set; }
        public int FbeYetkiliSayisi { get; set; }
        public int SbeYetkiliSayisi { get; set; }
    }
}