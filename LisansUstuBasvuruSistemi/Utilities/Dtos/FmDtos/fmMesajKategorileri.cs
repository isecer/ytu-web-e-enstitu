using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Models; using LisansUstuBasvuruSistemi.Models.FilterModel;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos.FmDtos
{
    public class fmMesajKategorileri : PagerOption
    {
        public string EnstituKod { get; set; }
        public string KategoriAdi { get; set; }
        public string KategoriAciklamasi { get; set; }
        public bool? IsAktif { get; set; }
        public IEnumerable<frMesajKategorileri> Data { get; set; }
    }
    public class frMesajKategorileri : MesajKategorileri
    {
        public string EnstituAd { get; set; }
        public string IslemYapan { get; set; }
    }
    public class SubMessages
    {
        public int KullaniciID { get; set; }
        public string EMail { get; set; }
        public DateTime Tarih { get; set; }
        public string ResimYolu { get; set; }
        public string AdSoyad { get; set; }
        public int MesajID { get; set; }
        public string Icerik { get; set; }
        public string IslemYapanIP { get; set; }
        public List<MesajEkleri> Ekler { get; set; }
        public List<GonderilenMailKullanicilar> Gonderilenler { get; set; }

    }
}