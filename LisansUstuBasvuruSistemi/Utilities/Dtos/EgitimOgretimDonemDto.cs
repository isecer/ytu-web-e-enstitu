using System;
using System.Collections.Generic;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class EgitimOgretimDonemDto
    {
        public List<int> BaharDonemiAylar { get; set; } = new List<int> { 1, 2, 3, 4, 5 };
        public List<int> GuzDonemiAylar { get; set; } = new List<int> { 6, 7, 8, 9, 10, 11, 12 };
        public int BaslangicYil { get; set; }
        public int BitisYil => BaslangicYil > 0 ? BaslangicYil + 1 : 0;

        public int DonemId { get; set; }
        public string DonemAdi { get; set; }
        public string DonemAdiLong => BaslangicYil + " - " + (BaslangicYil + 1) + " " + DonemAdi;
    }
    public class EgitimOgretimDonemDetayDto
    {
        public int BaslangicYil { get; set; }
        public int BitisYil => BaslangicYil > 0 ? BaslangicYil + 1 : 0;
        public int DonemId { get; set; }
        public string DonemAdi { get; set; } 
        public string DonemAdiLong => BaslangicYil + " - " + (BaslangicYil + 1) + " " + DonemAdi;
        public DateTime BaslangicTarihi { get; set; }
        public DateTime BitisTarihi { get; set; } 
    }
}