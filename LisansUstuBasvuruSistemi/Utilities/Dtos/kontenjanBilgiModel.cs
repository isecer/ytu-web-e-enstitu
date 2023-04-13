using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class KontenjanBilgiModel
    {
        public string EnstituAdi { get; set; }
        public string EnstituKod { get; set; }
        public int OgrenimTipKod { get; set; }
        public bool GrupGoster { get; set; }
        public string GrupKodu { get; set; }
        public bool LEgitimBilgisiIste { get; set; }
        public bool YLEgitimBilgisiIste { get; set; }
        public int BasvuruSurecKontrolTipID { get; set; }
        public bool FarkliOgrenimTipiEklenemez { get; set; }
        public string FarkliOgrenimTipEklenemezAds { get; set; }
        public bool FarkliOgrenimTipiEklenemezAyniBasvuruda { get; set; }
        public bool IsAktif { get; set; }
        public string GrupAdi { get; set; }
        public string OgrenimTipAdi { get; set; }
        public int KontenjanBulunanBolumSayisi { get; set; }
        public int KontenjanBulunanProgramSayisi { get; set; }
        public int ToplamKota { get; set; }
        public int ToplamKalanKota { get; set; }
        public int Kota { get; set; }
        public int KalanKota { get; set; }
        public List<KontenjanBilgiModel> OBOgrenimTipleri { get; set; }

    }
   
}