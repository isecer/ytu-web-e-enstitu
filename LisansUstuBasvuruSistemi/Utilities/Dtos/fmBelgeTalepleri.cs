using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Models; using LisansUstuBasvuruSistemi.Utilities.Dtos;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class fmBelgeTalepleri : PagerOption
    {
        public bool Expand { get; set; }
        public string DilKodu { get; set; }
        public int? OgrenimDurumID { get; set; }
        public int? BelgeID { get; set; }
        public int? OgrenimTipKod { get; set; }
        public int? BelgeDurumID { get; set; }
        public int? BelgeTipID { get; set; }
        public string OgretimYili { get; set; }
        public string AranacakKelime { get; set; }
        public string ProgramKod { get; set; }
        public string BuGunkuKayitlar { get; set; }
        public IEnumerable<frBelgeTalepleri> Data { get; set; }
    }
    public class frBelgeTalepleri : BelgeTalepleri
    {
        public int? KullaniciID { get; set; }
        public string ResimAdi { get; set; }
        public string ClassName { get; set; }
        public string Color { get; set; }
        public string OgrenimTipAdi { get; set; }
        public string DonemAdi { get; set; }
        public string DurumAdi { get; set; }
        public string DurumListeAdi { get; set; }
        public string BelgeTipAdi { get; set; }
        public string DilAdi { get; set; }
        public string DilFlagClass { get; set; }
    }
}