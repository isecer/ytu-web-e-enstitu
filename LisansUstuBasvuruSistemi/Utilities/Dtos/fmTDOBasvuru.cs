using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Models; using LisansUstuBasvuruSistemi.Utilities.Dtos;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class fmTDOBasvuru : PagerOption
    {
        public int? TDOBasvuruID { get; set; }
        public int? KullaniciID { get; set; }
        public string Kod { get; set; }
        public bool Expand { get; set; }
        public int? UyrukKod { get; set; }
        public string EnstituKod { get; set; }
        public string KayitDonemi { get; set; }
        public int? KullaniciTipID { get; set; }
        public string AdSoyad { get; set; }
        public int? OgrenimTipKod { get; set; }
        public string ProgramKod { get; set; }
        public string AnabilimDaliKod { get; set; }
        public DateTime? TIBaslangicTarihi { get; set; }
        public DateTime? TIBitisTarihi { get; set; }
        public bool AktifOgrenimIcinBasvuruVar { get; set; }
        public string AktifDonemID { get; set; }
        public string DonemID { get; set; }
        public int? AktifDurumID { get; set; }
        public int? DurumID { get; set; }
        public int? AktifEsDurumID { get; set; }
        public int? EsDurumID { get; set; }
        public DateTime RowDate { get; set; }
        public IEnumerable<frTDOBasvuru> Data { get; set; }
    }
    public class frTDOBasvuru : TDOBasvuru
    {
        public int? TezDanismanID { get; set; }
        public string EnstituAdi { get; set; }
        public string OgrenimTipAdi { get; set; }
        public string AnabilimdaliAdi { get; set; }
        public string ProgramAdi { get; set; }
        public string TcPasaPortNo { get; set; }
        public string KullaniciTipAdi { get; set; }
        public string AdSoyad { get; set; }
        public string EMail { get; set; }
        public string CepTel { get; set; }
        public DateTime? GsisKayitTarihi { get; set; }
        public string DurumClassName { get; set; }
        public string DurumColor { get; set; }
        public string AktifDonemID { get; set; }
        public string AktifDonemAdi { get; set; }
        public bool? DanismanOnayladi { get; set; }
        public bool? EYKYaGonderildi { get; set; }
        public DateTime? EYKYaGonderildiIslemTarihi { get; set; }
        public DateTime? EYKYaGonderildiIslemTarihiES { get; set; }
        public bool? EYKDaOnaylandi { get; set; }
        public bool EsDanismanOnerisiVar { get; set; }
        public bool? Es_EYKYaGonderildi { get; set; }
        public bool? Es_EYKDaOnaylandi { get; set; }
        public int Sira { get; set; }
        public List<TDODanismanFiltreModel> TDODanismanDetayModels { get; set; }
        public DateTime RowDate { get; internal set; }
        public int? VarolanTezDanismanID { get; set; }
        public bool? VarolanDanismanOnayladi { get; internal set; }
    }
}