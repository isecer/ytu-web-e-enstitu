using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Models; using LisansUstuBasvuruSistemi.Utilities.Dtos;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class FmMulakatSureci : PagerOption
    {
        public string ProgramAdi { get; set; }
        public string ProgramKod { get; set; }
        public int? OgrenimTipKod { get; set; }
        public int? BasvuruSurecID { get; set; }
        public IEnumerable<FrMulakatSureci> Data { get; set; }
    }
    public class FrMulakatSureci : Mulakat
    {
        public DateTime BaslangicTarihi { get; set; }
        public DateTime BitisTarihi { get; set; }
        public DateTime? SonucGirisBaslangicTarihi { get; set; }
        public DateTime? SonucGirisBitisTarihi { get; set; }
        public string BasvuruSurecAdi { get; set; }
        public string AnabilimDaliAdi { get; set; }
        public string ProgramAdi { get; set; }
        public string OgrenimTipAdi { get; set; }
        public string IslemYapan { get; set; }
    }
}