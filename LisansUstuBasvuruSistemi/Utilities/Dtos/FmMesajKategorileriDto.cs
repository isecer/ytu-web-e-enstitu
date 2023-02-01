using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Models; using LisansUstuBasvuruSistemi.Utilities.Dtos;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class FmMesajKategorileriDto : PagerOption
    {
        public string EnstituKod { get; set; }
        public string KategoriAdi { get; set; }
        public string KategoriAciklamasi { get; set; }
        public bool? IsAktif { get; set; }
        public IEnumerable<FrMesajKategorileriDto> MesajKategorileriDtos { get; set; }
    }
    public class FrMesajKategorileriDto : MesajKategorileri
    {
        public string EnstituAd { get; set; }
        public string IslemYapan { get; set; }
    }
    public class SubMessagesDto
    {
        public int KullaniciID { get; set; }
        public string EMail { get; set; }
        public DateTime Tarih { get; set; }
        public string ResimYolu { get; set; }
        public string AdSoyad { get; set; }
        public int MesajID { get; set; }
        public string Icerik { get; set; }
        public string IslemYapanIP { get; set; }
        public List<MesajEkleri> MesajEkleris { get; set; }
        public List<GonderilenMailKullanicilar> GonderilenMailKullanicilars { get; set; }

    }
}