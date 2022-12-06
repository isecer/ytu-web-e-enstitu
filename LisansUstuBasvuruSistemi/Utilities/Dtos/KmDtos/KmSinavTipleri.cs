using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using LisansUstuBasvuruSistemi.Models; using LisansUstuBasvuruSistemi.Models.FilterModel;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos.KmDtos
{
    public class KmSinavTipleri : SinavTipleri
    {
        public List<int> SinavDilIDs { get; set; }

        public List<int> SinavTarihleriID { get; set; }
        public List<DateTime> SinavTarihi { get; set; }

        public List<int> SinavNotlariID { get; set; }
        public List<string> SinavNotAdi { get; set; }
        public List<double> SinavNotDeger { get; set; }


        public List<int> SubSinavAralikID { get; set; }
        public List<string> SubSinavAralikAdi { get; set; }
        public List<double> SubSinavMin { get; set; }
        public List<double> SubSinavMax { get; set; }
        public List<bool> SubNotDonusum { get; set; }
        public List<string> SubNotDonusumFormulu { get; set; }

        public List<int> SinavTipDonemID { get; set; }
        public List<int> Yil { get; set; }
        public List<string> WsDonemKod { get; set; }
        public List<bool> IsTaahhutVar { get; set; }

        public List<int> NAOgrenimTipKod { get; set; }
        public List<bool> NAIngilizce { get; set; }
        public List<int> NAIsGecerli { get; set; }
        public List<int> NAIsIstensin { get; set; }
        public List<double?> NAMin { get; set; }
        public List<double?> NAMax { get; set; }
        public List<string> IPProgramKod { get; set; }

        public KmSinavTipleri()
        {
            IPProgramKod = new List<string>();
            SinavDilIDs = new List<int>();
            IsTaahhutVar = new List<bool>();


            SinavTipDonemID = new List<int>();
            Yil = new List<int>();
            WsDonemKod = new List<string>();

            SinavTarihleriID = new List<int>();
            SinavTarihi = new List<DateTime>();
            SinavNotlariID = new List<int>();
            SinavNotAdi = new List<string>();
            SinavNotDeger = new List<double>();

            SubSinavAralikID = new List<int>();
            SubSinavAralikAdi = new List<string>();
            SubSinavMin = new List<double>();
            SubSinavMax = new List<double>();
            SubNotDonusum = new List<bool>();
            SubNotDonusumFormulu = new List<string>();
        }
    }
}