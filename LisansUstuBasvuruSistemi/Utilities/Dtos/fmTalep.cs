using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Models; using LisansUstuBasvuruSistemi.Utilities.Dtos;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class FmTalep : PagerModel
    {
        public bool Expand { get; set; }
        public int? TalepSurecID { get; set; }
        public int? KullaniciTipID { get; set; }
        public int? TalepTipID { get; set; }
        public int? TalepDurumID { get; set; }
        public int? OgrenimTipKod { get; set; }
        public string AranacakKelime { get; set; }
        public string ProgramKod { get; set; }
        public bool? IsTezOnerisiYapildi { get; set; }
        public bool? IsDersYukuTamamlandi { get; set; }
        public IEnumerable<FrTalep> Data { get; set; }
    }
    public class FrTalep : TalepGelenTalepler
    {
        public bool YtuOgrencisi { get; set; }
        public bool IsbelgeYuklemesiVar { get; set; }
        public string TalepTipAdi { get; set; }
        public string ResimAdi { get; set; }
        public string ClassName { get; set; }
        public string Color { get; set; }
        public string OgrenimTipAdi { get; set; }
        public string AnabiliDaliAdi { get; set; }
        public string ProgramAdi { get; set; }
        public string StatuAdi { get; set; }
        public string DurumListeAdi { get; set; }
        public string TalepTipAciklama { get; set; }
        public string TaahhutAciklama { get; set; }
    }
}