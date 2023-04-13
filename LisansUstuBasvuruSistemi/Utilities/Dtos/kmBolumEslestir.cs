using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class KmBolumEslestir
    {
        public string EnstituKod { get; set; }
        public string EnstituAd { get; set; }
        public string AnabilimDaliAdi { get; set; }
        public string ProgramKod { get; set; }
        public string ProgramAdi { get; set; }
        public List<int> OgrenciBolumID { get; set; }

    }
}