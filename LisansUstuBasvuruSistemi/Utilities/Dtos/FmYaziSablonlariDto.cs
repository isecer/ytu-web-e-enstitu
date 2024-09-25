using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BiskaUtil;
using Entities.Entities; using LisansUstuBasvuruSistemi.Utilities.Dtos;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class FmYaziSablonlariDto : PagerModel
    {
        public string EnstituKod { get; set; }
        public int? YaziSablonTipID { get; set; }
        public string Konu { get; set; }
        public DateTime? Tarih { get; set; }
        public string Sablon { get; set; }
        public bool? IsAktif { get; set; }
        public string DuyuruYapan { get; set; }
        public IEnumerable<FrYaziSablonlariDto> YaziSablonlariDtos { get; set; }
    }

    public class FrYaziSablonlariDto : YaziSablonlari
    {
        public string EnstituAdi { get; set; }
        public string SablonTipAdi { get; set; }
        public string Parametreler { get; set; }
        public string IslemYapan { get; set; } 
    }
}