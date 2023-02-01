using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BiskaUtil;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class FmKotalarDto : PagerOption
    {
        public bool? MulakatSurecineGirecek { get; set; }
        public bool? IsAlesYerineDosyaNotuIstensin { get; set; }
        public string EnstituKod { get; set; }
        public string ProgramAdi { get; set; }
        public int? OgrenimTipKod { get; set; }
        public bool? IsAktif { get; set; }
        public IEnumerable<FrKotalarDto> KotalarDtos { get; set; }

    }
    public class FrKotalarDto : frProgramlar
    {
        public int KotaID { get; set; }
        public int OgrenimTipKod { get; set; }
        public string OgrenimTipAdi { get; set; }
        public bool MulakatSurecineGirecek { get; set; }
        public bool OrtakKota { get; set; }
        public int? OrtakKotaSayisi { get; set; }
        public int AlanIciKota { get; set; }
        public int AlanDisiKota { get; set; }
        public double? MinAles { get; set; }
        public double? MinAgno { get; set; }
        public object UnAdi { get; internal set; }
        public bool IsAlesYerineDosyaNotuIstensin { get; set; }

        public int AlanTipID { get; set; }
        public bool? KayitOldu { get; set; }
        public int MulakatSonucTipID { get; set; }
    }
}