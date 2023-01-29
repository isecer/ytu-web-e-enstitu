using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Models; using LisansUstuBasvuruSistemi.Utilities.Dtos;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class fmDuyurular : PagerOption
    {
        public string EnstituKod { get; set; }
        public string Baslik { get; set; }
        public DateTime? Tarih { get; set; }
        public string Aciklama { get; set; }
        public bool? IsAktif { get; set; }
        public string DuyuruYapan { get; set; }
        public IEnumerable<frDuyurular> Data { get; set; }
    }

    public class frDuyurular : Duyurular
    {
        public string EnstituAdi { get; set; }
        public string DuyuruYapan { get; set; }
        public int EkSayisi { get; set; }
        public List<DuyuruEkleri> Ekler { get; set; }
    }
}