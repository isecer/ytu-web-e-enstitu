using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Models; using LisansUstuBasvuruSistemi.Models.FilterModel;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos.FmDtos
{
    public class fmProgramlar : PagerOption
    {
        public bool Expand { get; set; }
        public string EnstituKod { get; set; }
        public string ProgramKod { get; set; }
        public int? BolumID { get; set; }
        public int? AlesTipID { get; set; }
        public int? KullaniciTipID { get; set; }
        public bool? Ucretli { get; set; }
        public string ProgramAdi { get; set; }
        public bool? AlesNotuYuksekOlanAlinsin { get; set; }
        public bool? LYLHerhangiBirindeGecenAlanIci { get; set; }
        public bool? ProgramSecimiEkBilgi { get; set; }
        public bool? IsAlandisiBolumKisitlamasi { get; set; }
        public bool? IsAktif { get; set; }
        public IEnumerable<frProgramlar> data { get; set; }

    }
    public class frProgramlar : Programlar
    {
        public string EnstituKod { get; set; }
        public string EnstituAd { get; set; }
        public string AnabilimDaliAdi { get; set; }

        public string AgnoAlimTipAdi { get; set; }
        public string AlesTipAdi { get; set; }
        public string IslemYapan { get; set; }
        public List<string> AlandisiBolumKisitListesi { get; set; }
        public string KullaniciTipAdi { get; set; }
        public List<string> SecilenAlesTipleri { get; set; }

    }
}