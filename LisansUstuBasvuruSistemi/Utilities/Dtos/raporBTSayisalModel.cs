using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class raporBTSayisalModel
    {
        public int Yil { get; set; }
        public int Ay { get; set; }
        public int BelgeTipID { get; set; }
        public string BelgeTipAdi { get; set; }
        public int Toplam { get; set; }
        public int TalepEdilen { get; set; }
        public int Verilen { get; set; }
        public int Kapatilan { get; set; }
        public int IptalEdilen { get; set; }
    }
}