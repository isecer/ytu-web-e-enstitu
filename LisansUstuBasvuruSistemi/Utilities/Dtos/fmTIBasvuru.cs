using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BiskaUtil;
using Entities.Entities; using LisansUstuBasvuruSistemi.Utilities.Dtos;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class FmTiBasvuru : PagerModel
    {
        public int? TIBasvuruID { get; set; }
        public int? KullaniciID { get; set; }
        public int? AnabilimDaliID { get; set; }
        public string Kod { get; set; }
        public Guid? IsDegerlendirme { get; set; }
        public bool Expand { get; set; } 
        public string EnstituKod { get; set; }
        public string KayitDonemi { get; set; }
        public int? KullaniciTipID { get; set; }
        public string AdSoyad { get; set; }
        public int? OgrenimTipKod { get; set; }
        public string ProgramKod { get; set; }
        public string AnabilimDaliKod { get; set; } 
        public bool AktifOgrenimIcinBasvuruVar { get; set; }
        public string AktifTIAraRaporDonemID { get; set; }
        public string TIAraRaporDonemID { get; set; }
        public int? AktifTIAraRaporRaporDurumID { get; set; }
        public int? TIAraRaporRaporDurumID { get; set; }
        public int? AktifAraRaporSayisi { get; set; }
        public int? TIAraRaporSayisi { get; set; }

        public IEnumerable<FrTiBasvuru> Data { get; set; }
    }
    public class FrTiBasvuru : TIBasvuru
    {
        public string EnstituAdi { get; set; }
        public string OgrenimTipAdi { get; set; }
        public int? AnabilimDaliID { get; set; }
        public string AnabilimdaliAdi { get; set; }
        public string ProgramAdi { get; set; }  
        public int DonemID { get; set; }
        public string DonemAdi { get; set; }
        public DateTime BasTar { get; set; }
        public DateTime BitTar { get; set; }
        public Guid? UserKey { get; set; }
        public string ResimAdi { get; set; } 
        public  string TcKimlikNo { get; set; }
        public string AdSoyad { get; set; }
        public string CepTel { get; set; }
        public string EMail { get; set; }
        public string DurumClassName { get; set; }
        public string DurumColor { get; set; }
        public string IslemYapan { get; set; }
        public string FormNo { get; set; }
        public int? TIAraRaporRaporDurumID { get; set; }
        public string TIAraRaporRaporDurumAdi { get; set; }
        public int? AraRaporSayisi { get; set; }
        public bool? IsOyBirligiOrCoklugu { get; set; }
        public bool? IsBasariliOrBasarisiz { get; set; }
        public  DateTime? RaporTarihi { get; set; }
        public List<TiAraraporFiltreModel> tIAraraporFiltreModels { get; set; }
        public int? AraRaporDanismanID { get; set; }
        public string TIAraRaporAktifDonemID { get; set; }
        public string TiAraRaporAktifDonemAdi { get; set; }
        public DateTime? ToplantiTarihi { get; set; }
        public TimeSpan? ToplantiSaati { get; set; }
        public List<string> OnayYapmayanKomiteEmails { get; set; }

    }
    public class TiAraraporFiltreModel
    {
        public string FormKodu { get; set; }
        public string RaporDonemID { get; set; }
        public int AraRaporSayisi { get; set; }
        public int TezDanismanID { get; set; }
        public string DanismanAdSoyad { get; set; }
        public int TIBasvuruAraRaporDurumID { get; set; }
        public List<string> KomiteUyeleri { get; set; }
    }
}