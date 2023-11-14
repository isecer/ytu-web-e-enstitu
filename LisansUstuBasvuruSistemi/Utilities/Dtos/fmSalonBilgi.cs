using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BiskaUtil;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class FmSalonBilgi : PagerModel
    {
        public string EnstituKod { get; set; }
        public int? SRTalepTipID { get; set; }
        public List<int> SRSalonID { get; set; }
        public List<int> HaftaGunID { get; set; }
        public DateTime? BasTarih { get; set; }
        public DateTime? BitTarih { get; set; }
        public TimeSpan? BasSaat { get; set; }
        public TimeSpan? BitSaat { get; set; }
        public bool? IsDolu { get; set; }
        public IEnumerable<frSalonBilgi> data { get; set; }

        public FmSalonBilgi()
        {
            SRSalonID = new List<int>();
            HaftaGunID = new List<int>();
        }

    }
    public class frSalonBilgi
    {
        public string EnstituKod { get; set; }
        public string EnstituAdi { get; set; }
        public bool IsOzelTanim { get; set; }
        public int KayitID { get; set; }
        public DateTime Tarih { get; set; }
        public int HaftaGunID { get; set; }
        public System.TimeSpan BasSaat { get; set; }
        public System.TimeSpan BitSaat { get; set; }
        public string HaftaGunAdi { get; set; }
        public int SRSalonID { get; set; }
        public string SalonAdi { get; set; }
        public string ClassName { get; set; }
        public string Color { get; set; }
        public int? GTID { get; set; }
        public int? OTID { get; set; }
        public bool RemoveRow { get; set; }
    }
}