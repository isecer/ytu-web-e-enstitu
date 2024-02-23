using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BiskaUtil;
using Entities.Entities; using LisansUstuBasvuruSistemi.Utilities.Dtos;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class FmMesajlarDto : PagerModel
    {
        public bool Expand { get; set; }
        public string EnstituKod { get; set; }
        public int? MesajKategoriID { get; set; }
        public string Konu { get; set; }
        public DateTime? Tarih { get; set; }
        public string Aciklama { get; set; }
        public bool? IsAktif { get; set; }
        public bool? IsDosyaEkDurum { get; set; }
        public string AdSoyad { get; set; }
        public int? MesajYili { get; set; }
        public IEnumerable<FrMesajlarDto> MesajlarDtos { get; set; }
    }

    public class FrMesajlarDto : Mesajlar
    {
        public int GrupNo { get; set; }
        public string GidenGelen { get; set; }
        public string EnstituAdi { get; set; }
        public string KategoriAdi { get; set; }
        public Guid? UserKey { get; set; }
        public string ResimAdi { get; set; }
        public string KullaniciTipAdi { get; set; }
        public string OgrenciNo { get; set; }
        public string KayitDonemAdi { get; set; }
        public DateTime? KayitTarihi { get; set; }
        public string OgrenimTipAdi { get; set; }
        public string AnabilimdaliAdi { get; set; }
        public string ProgramAdi { get; set; }

        public int EkSayisi { get; set; }
        public List<SubMessagesDto> SubMesajList { get; set; }

    }
}