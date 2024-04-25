using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Entities.Entities;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class FmDonemProjesiBasvuruDto : PagerModel
    {
        public bool IsDonemProjesiBasvurusuAcik { get; set; }
        public bool IsFiltered { get; set; }
        public string EnstituAdi { get; set; }
        public string AkademikDonemID { get; set; }
        public int? DonemProjesiDurumID { get; set; }
        public int? DurumID { get; set; }
        public string AdSoyad { get; set; } 
        public string OgrenciNo { get; set; }
        public bool IsYtuOgrencisi { get; set; }
        public bool IsEnstituYetki { get; set; }
        public bool IsOgrenimSeviyeYetki { get; set; }
        public bool IsAktifOgrenimBasvuruVar { get; set; }
        public int? AnabilimDaliID { get; set; }
        public Guid? IsDegerlendirme { get; set; }
        public IEnumerable<FrDonemProjesiBasvuruDto> Data { get; set; }
        
    }
    public class FrDonemProjesiBasvuruDto : DonemProjesiBasvuru
    {
        public string EnstituKod { get; set; }
        public string DonemAdi { get; set; }
        public string DonemProjesiDurumAdi { get; set; }
        public int? TezDanismanID { get; set; }
        public string ResimAdi { get; set; }
        public Guid UserKey { get; set; }
        public string TcKimlikNo { get; set; }
        public string OgrenciNo { get; set; } 
        public string AdSoyad { get; set; }
        public string EMail { get; set; }
        public string OgrenimTipAdi { get; set; }
        public string ProgramAdi { get; set; }
        public int AnabilimDaliID { get; set; }
        public string AnabilimDaliAdi { get; set; }
        public string EnstituOnayDurumAdi { get; set; }
        public bool IsYeniBasvuruYapilabilir { get; set; }
        public bool IsJuriOlusturuldu { get; set; }
        public string AkademikDonemID { get; set; }
        public int KullaniciID { get; set; }

        public DateTime? ToplantiTarihi { get; set; }
        public TimeSpan? ToplantiSaati { get; set; }
        public List<string> OnayYapmayanJuriEmails { get; set; }
        public List<string> FilterJuriAdiKeys { get; set; }
        public DpBasvuruDurumDto SonBasvuruDurum { get; set; }
    }
}