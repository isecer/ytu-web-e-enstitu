using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Entities.Entities;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class FmKayitSilmeBasvuruDto : PagerModel
    {
        public bool IsKayitSilmeBasvurusuAcik { get; set; }
        public bool IsFiltered { get; set; }
        public string EnstituKod { get; set; }
        public string EnstituAdi { get; set; }
        public string AkademikDonemID { get; set; }
        public bool? IsOnayMakamiEykOrEnstituMudur { get; set; }
        public int? KayitSilmeDurumID { get; set; } 
        public string AdSoyad { get; set; }
        public string OgrenciNo { get; set; } 
        public string ProgramKod { get; set; }
        public Guid? ShowBasvuruUniqueId { get; set; } 
        public IEnumerable<FrKayitSilmeBasvuruDto> Data { get; set; } 
        public bool IsAktifOgrenimBasvuruVar { get; set; }
        public List<int> SelectedKayitSilmeBasvurulariIds { get; set; } = new List<int>();
        public int? OgrenimTipKod { get; set; }
    }
    public class FrKayitSilmeBasvuruDto : KayitSilmeBasvuru
    {
        public string AkademikDonemID { get; set; }
        public string DonemAdi { get; set; }
        public string KayitSilmeDurumAdi { get; set; } 
        public string ResimAdi { get; set; }
        public Guid UserKey { get; set; }
        public string TcKimlikNo { get; set; } 
        public string AdSoyad { get; set; }
        public string EMail { get; set; }
        public string OgrenimTipAdi { get; set; }
        public string ProgramAdi { get; set; }
        public int AnabilimDaliId { get; set; }
        public string AnabilimDaliAdi { get; set; }  
          
    }
    public class KmKayitSilmeBasvuruDto : KayitSilmeBasvuru
    {
        public Guid? UniqueID { get; set; }
        public string AdSoyad { get; set; }
        public string OgrenimTipAdi { get; set; }
        public string ProgramAdi { get; set; }
        public string AnabilimdaliAdi { get; set; }
        public HttpPostedFileBase DosyaNufusKayitOrnegi { get; set; }
    }

    public class KsBasvuruDetayDto : KayitSilmeBasvuru
    { 
        public bool GelenBasvuru { get; set; }
        public string EnstituAdi { get; set; }
        public string DonemAdi { get; set; }
        public string ResimAdi { get; set; }
        public string TcKimlikNo { get; set; }
        public string Ad { get; set; }
        public string Soyad { get; set; }
        public string OgrenimTipAdi { get; set; }
        public string AnabilimdaliAdi { get; set; }
        public string ProgramAdi { get; set; }
        public string KayitDonemi { get; set; } 

        public string DurumClassName { get; set; }
        public string DurumColor { get; set; }
        public string HarcBirimiOnayYapanKullanici { get; set; }
        public string KutuphaneBirimiOnayYapanKullanici { get; set; }
    }
    public class KsTutanakDto
    {

        public string OgrenciNo { get; set; }
        public string OgrenciAdSoyad { get; set; }
        public string OgrenimSeviyesiAdi { get; set; }
        public string AnabilimDaliAdi { get; set; }
        public string ProgramAdi { get; set; } 
        public string KayitSilmeDonemAdi { get; set; }
        public DateTime? KayitSilmeEykTarihi { get; set; }

    }
}