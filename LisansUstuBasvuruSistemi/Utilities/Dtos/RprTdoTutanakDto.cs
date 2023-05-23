using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class RprTdoTutanakDto
    {
        public string OgrenciBilgi { get; set; }
        public string VarolanDanisman { get; set; }
        public string YeniDanisman { get; set; }
        public string OncekiTezDili { get; set; }
        public string YeniTezDili { get; set; }
        public string OncekiTezBaslikTr { get; set; }
        public string OncekiTezBaslikEn { get; set; }
        public string YeniTezBaslikTr { get; set; }
        public string YeniTezBaslikEn { get; set; }
    }
}