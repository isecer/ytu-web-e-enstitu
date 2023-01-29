using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using LisansUstuBasvuruSistemi.Models; 

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class BelgeTipDetayKayitModel : BelgeTipDetay
    {
        public List<int> BelgeTipID { get; set; }
        public List<string> HaftaGunleri { get; set; }
        public List<TimeSpan> TalepBaslangicSaat { get; set; }
        public List<TimeSpan> TalepBitisSaat { get; set; }
        public List<int> EklenecekGun { get; set; }
        public List<TimeSpan> TeslimBaslangicSaat { get; set; }
        public List<TimeSpan> TeslimBitisSaat { get; set; }


        public List<BtSaatShowModel> Saatler { get; set; }
        public List<int> SeciliBelgeTipler { get; set; }
    }
    public class BtSaatShowModel : BelgeTipDetaySaatler
    {
        public List<CmbIntDto> GunNos { get; set; }
    }
}