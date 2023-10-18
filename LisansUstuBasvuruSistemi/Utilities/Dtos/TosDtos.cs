using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Models;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class FmTosBasvuru : PagerOption
    {
         
        public Guid? UniqueId { get; set; }
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
        public string AktifDonemID { get; set; }
        public string DonemID { get; set; }
        public int? AktifDurumID { get; set; }
        public int? DurumID { get; set; }
        public int? SavunmaNo { get; set; }

        public IEnumerable<FrTosBasvuru> Data { get; set; }
    }
    public class FrTosBasvuru : ToBasvuru
    {
        public string EnstituAdi { get; set; }
        public string OgrenimTipAdi { get; set; }
        public int? AnabilimDaliID { get; set; }
        public string AnabilimdaliAdi { get; set; }
        public string ProgramAdi { get; set; }
        public int DonemID { get; set; }
        public string DonemAdi { get; set; }
        public bool IsSinavBilgisiGirildi { get; set; }
        public bool IsDegerlendirmeSuvecinde { get; set; }
        public DateTime BasTar { get; set; }
        public DateTime BitTar { get; set; }
        public string ResimAdi { get; set; }
        public string TcKimlikNo { get; set; }
        public string AdSoyad { get; set; }
        public string CepTel { get; set; }
        public string EMail { get; set; }
        public string IslemYapan { get; set; }
        public string FormNo { get; set; }
        public DateTime? SavunmaBasvuruTarihi { get; set; }
        public int? AktifSavunmaNo { get; set; }
        public int? DurumID { get; set; }
        public string DurumAdi { get; set; }
        public bool? IsOyBirligiOrCoklugu { get; set; }
        public int? DanismanID { get; set; }
        public string AktifDonemID { get; set; }
        public string AktifDonemAdi { get; set; }
        public DateTime? ToplantiTarihi { get; set; }
        public TimeSpan? ToplantiSaati { get; set; }
        public TosDurumDto DurumModel { get; set; }

    }

    public class TosBasvuruDetayDto : ToBasvuru
    {
    
        public bool GelenBasvuru { get; set; }
        public string DonemHtmlString { get; set; }
        public string DurumHtmlString { get; set; }
        public string EnstituAdi { get; set; }
        public string ResimAdi { get; set; }
        public string TcKimlikNo { get; set; }
        public string Ad { get; set; }
        public string Soyad { get; set; }
        public string OgrenimTipAdi { get; set; }
        public string AnabilimdaliAdi { get; set; }
        public string ProgramAdi { get; set; }
        public string KayitDonemi { get; set; }
        public string TezDanismanBilgiEslesen { get; set; } 
        public int? ToplamBasarisizTezOneriSavunmaHak { get; set; }
        public int KalanTezOneriSavunmaHakki =>
              (this.ToplamBasarisizTezOneriSavunmaHak.HasValue
                ? (this.ToplamBasarisizTezOneriSavunmaHak.Value - BasarisizSavunmaSayisi)
                : 0);


        public int? IlkSavunmaHakkiAyKriter { get; set; }
        public int? IkinciSavunmaHakkiAyKriter { get; set; }
        public Guid? DegerlendirenUniqueID { get; set; }
        public List<ToBasvuruSavunmaDto> ToBasvuruSavunmaList { get; set; }
    }
    public class ToBasvuruSavunmaDto : ToBasvuruSavunma
    {
        public string DonemAdi { get; set; }
        public string DurumAdi { get; set; } 
        public TosDurumDto DurumModel { get; set; }
        public FrTalepler SRModel { get; set; }
    }

    public class ToBasvuruSavunmaKomiteDto : ToBasvuruSavunmaKomite
    {
        public string DurumAdi { get; set; }
    }


    public class ToSavunmaSaveModel : ToBasvuruSavunma
    {
        public string OgrenciProgramAdi { get; set; }
        public string OgrenciAdSoyad { get; set; }
        public bool? IsYokDrBursiyeriVar { get; set; }

        public List<int> TikNums { get; set; }
        public List<bool> IsTezDanismanis { get; set; }
        public List<string> UnvanAdis { get; set; }
        public List<string> AdSoyads { get; set; }
        public List<string> EMails { get; set; }
        public List<string> UniversiteAdis { get; set; }
        public List<string> AnabilimDaliAdis { get; set; }

        public SelectList SListDonemSecim { get; set; }
        public HttpPostedFileBase Dosya { get; set; }


    }
    public class TosDurumDto
    {
        public bool IsTezOnerisiVar { get; set; } 
        public bool? IsOyBirligiOrCoklugu { get; set; }
        public int? ToBasvuruSavunmaDurumID { get; set; }
        public bool IsSrTalebiYapildi { get; set; }

        public bool DegerlendirmeBasladi { get; set; }
    }

}