using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Models; using LisansUstuBasvuruSistemi.Utilities.Dtos;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class FmMailSablonlariDto : PagerOption
    {
        public string EnstituKod { get; set; }
        public int? MailSablonTipID { get; set; }
        public string SablonAdi { get; set; }
        public DateTime? Tarih { get; set; }
        public string Sablon { get; set; }
        public bool? IsAktif { get; set; }
        public string DuyuruYapan { get; set; }
        public IEnumerable<FrMailSablonlariDto> MailSablonlariDtos { get; set; }
    }

    public class FrMailSablonlariDto : MailSablonlari
    {
        public string EnstituAdi { get; set; }
        public string SablonTipAdi { get; set; }
        public string Parametreler { get; set; }
        public string IslemYapan { get; set; }
        public int EkSayisi { get; set; }
    }
}