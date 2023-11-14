using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Models; 
namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class FmKullanicilarDto : PagerModel
    {
        public bool Expand { get; set; }
        public int KullaniciID { get; set; }
        public int? YetkiGrupID { get; set; }
        public string EnstituKod { get; set; }
        public int? OgrenimTipKod { get; set; }
        public int? SehirKod { get; set; }
        public string ProgramKod { get; set; }
        public int? OgrenimDurumID { get; set; }
        public int? CinsiyetID { get; set; }
        public int? BirimID { get; set; }
        public int? KullaniciTipID { get; set; }
        public int? Cinsiyet { get; set; }
        public string KullaniciAdi { get; set; }
        public string AdSoyad { get; set; }
        public string EMail { get; set; }
        public string Telefon { get; set; }
        public bool? IsActiveDirectoryUser { get; set; }
        public bool? IsAktif { get; set; }
        public bool? IsAdmin { get; set; }
        public string Aciklama { get; set; }
        public IEnumerable<FrKullanicilarDto> KullanicilarDtos { get; set; }
        public FmKullanicilarDto()
        {
            KullanicilarDtos = new FrKullanicilarDto[0];
        }
    }
    public class FrKullanicilarDto : Kullanicilar
    {
        public bool KtipBasvuruYapabilir { get; set; }
        public string EnstituAdi { get; set; }
        public string KullaniciTipAdi { get; set; }
        public string YetkiGrupAdi { get; set; }
    }
}