using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using LisansUstuBasvuruSistemi.Models; using LisansUstuBasvuruSistemi.Utilities.Dtos;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class KmMulakat : Mulakat
    {
        public List<int> MulakatDetayId { get; set; } = new List<int>();
        public List<int> KampusId { get; set; } = new List<int>();
        public List<int> MulakatSinavTurId { get; set; } = new List<int>();
        public List<int> YuzdeOran { get; set; } = new List<int>();
        public List<DateTime> SinavTarihi { get; set; } = new List<DateTime>();
        public List<string> YerAdi { get; set; } = new List<string>();

        public List<int> MulakatJuriId { get; set; } = new List<int>();
        public List<bool> IsAsil { get; set; } = new List<bool>();
        public List<int> SiraNo { get; set; } = new List<int>();
        public List<string> JuriAdi { get; set; } = new List<string>();

        public List<KrMulakatDetay> MulakatDetayi { get; set; } = new List<KrMulakatDetay>();
    }
    public class KrMulakatDetay : MulakatDetay
    {
        public int KullaniId { get; set; }
        public string DilKodu { get; set; }
        public string KampusAdi { get; set; }
        public string MulakatSinavTurAdi { get; set; }
        public string YuzdeOranStr { get; set; }
        public new int? YuzdeOran { get; set; }
    }
}