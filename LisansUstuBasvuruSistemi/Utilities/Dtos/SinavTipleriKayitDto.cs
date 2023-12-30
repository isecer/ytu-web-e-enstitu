using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using LisansUstuBasvuruSistemi.Models; using LisansUstuBasvuruSistemi.Utilities.Dtos;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class SinavTipleriKayitDto : SinavTipleri
    { 

        public List<int> SinavTarihleriId { get; set; } = new List<int>();
        public List<DateTime> SinavTarihi { get; set; } = new List<DateTime>();

        public List<int> SinavNotlariId { get; set; } = new List<int>();
        public List<string> SinavNotAdi { get; set; } = new List<string>();
        public List<double> SinavNotDeger { get; set; } = new List<double>();


        public List<int> SubSinavAralikId { get; set; } = new List<int>();
        public List<string> SubSinavAralikAdi { get; set; } = new List<string>();
        public List<double> SubSinavMin { get; set; } = new List<double>();
        public List<double> SubSinavMax { get; set; } = new List<double>();

        public List<int> SinavTipDonemId { get; set; } = new List<int>();
        public List<int> Yil { get; set; } = new List<int>();

        public List<int> NaOgrenimTipKod { get; set; }
        public List<bool> NaIngilizce { get; set; }
        public List<int> NaIsGecerli { get; set; }
        public List<int> NaIsIstensin { get; set; }
        public List<double?> NaMin { get; set; }
        public List<double?> NaMax { get; set; }
        public List<string> IpProgramKod { get; set; } = new List<string>();
    }
}