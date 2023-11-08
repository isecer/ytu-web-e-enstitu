using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Utilities.Dtos;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class FmDuyurularDto : PagerOption
    {
        public string EnstituKod { get; set; }
        public string Baslik { get; set; }
        public DateTime? Tarih { get; set; }
        public string Aciklama { get; set; }
        public bool? IsAktif { get; set; }
        public string DuyuruYapan { get; set; }
        public IEnumerable<FrDuyurularDto> DuyurularDtos { get; set; }
    }

    public class FrDuyurularDto : Duyurular
    {
        public string EnstituAdi { get; set; } 
        public string DuyuruYapan { get; set; }
        public int EkSayisi { get; set; } 
        public List<FrDuyuruPopupTipleri> SeciliDuyuruPopupTips { get; set; }
        public List<DuyuruEkleri> Ekler { get; set; }
    }

    public class FrDuyuruPopupTipleri : DuyuruPopupTipleri
    {
        public bool IsChecked { get; set; }
    }
}