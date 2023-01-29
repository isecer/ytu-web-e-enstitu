using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using LisansUstuBasvuruSistemi.Models;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class SRSaatlerMDL : SRSaatler
    {
        public List<CmbIntDto> GunNos { get; set; }
    }
    public class SRSaatKontrolModel
    {
        public string HaftaGunleri { get; set; }
        public List<int> GHaftaGunleri { get; set; }
        public TimeSpan? BasSaat { get; set; }
        public TimeSpan? BitSaat { get; set; }

        public List<string> HaftaGunleriList { get; set; }
        public List<TimeSpan> BasSaatList { get; set; }
        public List<TimeSpan> BitSaatList { get; set; }
    }
}