using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Models; using LisansUstuBasvuruSistemi.Utilities.Dtos;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class FmBelgeTalepleriDto : PagerModel
    {
        public bool Expand { get; set; }
        public string DilKodu { get; set; }
        public int? OgrenimDurumId { get; set; }
        public int? BelgeId { get; set; }
        public int? OgrenimTipKod { get; set; }
        public int? BelgeDurumId { get; set; }
        public int? BelgeTipId { get; set; }
        public string OgretimYili { get; set; }
        public string AranacakKelime { get; set; }
        public string ProgramKod { get; set; }
        public string BuGunkuKayitlar { get; set; }
        public IEnumerable<FrBelgeTalepleriDto> BelgeTalepleriDtos { get; set; }
    }
    public class FrBelgeTalepleriDto : BelgeTalepleri
    { 

        public string ResimAdi { get; set; }
        public Guid? UserKey { get; set; }
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