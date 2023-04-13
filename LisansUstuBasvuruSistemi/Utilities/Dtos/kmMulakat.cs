using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using LisansUstuBasvuruSistemi.Models; using LisansUstuBasvuruSistemi.Utilities.Dtos;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class KmMulakat : Mulakat
    {
        public List<int> MulakatDetayID { get; set; }
        public List<int> KampusID { get; set; }
        public List<int> MulakatSinavTurID { get; set; }
        public List<int> YuzdeOran { get; set; }
        public List<DateTime> SinavTarihi { get; set; }
        public List<string> YerAdi { get; set; }

        public List<int> MulakatJuriID { get; set; }
        public List<bool> IsAsil { get; set; }
        public List<int> SiraNo { get; set; }
        public List<string> JuriAdi { get; set; }

        public List<krMulakatDetay> MulakatDetayi { get; set; }
        public KmMulakat()
        {
            MulakatDetayID = new List<int>();
            KampusID = new List<int>();
            MulakatSinavTurID = new List<int>();
            YuzdeOran = new List<int>();
            SinavTarihi = new List<DateTime>();
            YerAdi = new List<string>();

            MulakatJuriID = new List<int>();
            IsAsil = new List<bool>();
            SiraNo = new List<int>();
            JuriAdi = new List<string>();

            MulakatDetayi = new List<krMulakatDetay>();
        }
    }
    public class krMulakatDetay : MulakatDetay
    {
        public int KullaniID { get; set; }
        public string DilKodu { get; set; }
        public string KampusAdi { get; set; }
        public string MulakatSinavTurAdi { get; set; }
        public string YuzdeOranStr { get; set; }
        public int? YuzdeOran { get; set; }
    }
}