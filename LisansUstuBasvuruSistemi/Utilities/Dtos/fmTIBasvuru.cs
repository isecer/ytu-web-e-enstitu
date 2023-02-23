using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Models; using LisansUstuBasvuruSistemi.Utilities.Dtos;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class fmTIBasvuru : PagerOption
    {
        public int? TIBasvuruID { get; set; }
        public int? KullaniciID { get; set; }
        public string Kod { get; set; }
        public Guid? IsDegerlendirme { get; set; }
        public bool Expand { get; set; }
        public int? UyrukKod { get; set; }
        public string EnstituKod { get; set; }
        public string KayitDonemi { get; set; }
        public int? KullaniciTipID { get; set; }
        public string AdSoyad { get; set; }
        public int? OgrenimTipKod { get; set; }
        public string ProgramKod { get; set; }
        public string AnabilimDaliKod { get; set; }
        public int? TIDurumID { get; set; }
        public DateTime? TIBaslangicTarihi { get; set; }
        public DateTime? TIBitisTarihi { get; set; }
        public bool AktifOgrenimIcinBasvuruVar { get; set; }
        public string AktifTIAraRaporDonemID { get; set; }
        public string TIAraRaporDonemID { get; set; }
        public int? AktifTIAraRaporRaporDurumID { get; set; }
        public int? TIAraRaporRaporDurumID { get; set; }
        public int? AktifAraRaporSayisi { get; set; }
        public int? TIAraRaporSayisi { get; set; }

        public IEnumerable<frTIBasvuru> Data { get; set; }
    }
    public class frTIBasvuru : TIBasvuru
    {
        public string EnstituAdi { get; set; }
        public string MezuniyetSurecAdi { get; set; }
        public string OgrenimTipAdi { get; set; }
        public string AnabilimdaliAdi { get; set; }
        public string ProgramAdi { get; set; } 
        public int SurecBaslangicYil { get; set; }
        public int DonemID { get; set; }
        public string DonemAdi { get; set; }
        public DateTime BasTar { get; set; }
        public DateTime BitTar { get; set; }
        public string KullaniciTipAdi { get; set; }
        public string AdSoyad { get; set; }
        public string EMail { get; set; }
        public string CepTel { get; set; }
        public DateTime? GsisKayitTarihi { get; set; }
        public string DurumClassName { get; set; }
        public string DurumColor { get; set; }
        public string IslemYapan { get; set; }
        public string FormNo { get; set; }
        public int? TIAraRaporRaporDurumID { get; set; }
        public string TIAraRaporRaporDurumAdi { get; set; }
        public int? AraRaporSayisi { get; set; }
        public bool? IsOyBirligiOrCouklugu { get; set; }
        public bool? IsBasariliOrBasarisiz { get; set; }
        public List<TIAraraporFiltreModel> tIAraraporFiltreModels { get; set; }
        public string TIAraRaporAktifDonemID { get; set; }
        public string TIAraRaporAktifDonemAdi { get; set; }

    }
    public class TIAraraporFiltreModel
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