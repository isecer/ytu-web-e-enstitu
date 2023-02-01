using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    
    public class SaatKontrolDto
    {
        public string HaftaGunleri { get; set; }
        public List<int> GHaftaGunleri { get; set; }
        public TimeSpan? TalepBaslangicSaat { get; set; }
        public TimeSpan? TalepBitisSaat { get; set; }
        public int? EklenecekGun { get; set; }
        public TimeSpan? TeslimBaslangicSaat { get; set; }
        public TimeSpan? TeslimBitisSaat { get; set; }

        public List<string> HaftaGunleriList { get; set; }
        public List<TimeSpan> TalepBaslangicSaatList { get; set; }
        public List<TimeSpan> TalepBitisSaatList { get; set; }
    }
 
}