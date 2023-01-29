using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Models; using LisansUstuBasvuruSistemi.Utilities.Dtos;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class fmMailGonderme : PagerOption
    {
        public string EnstituKod { get; set; }
        public string Konu { get; set; }
        public DateTime? Tarih { get; set; }
        public string Aciklama { get; set; }
        public string MailGonderen { get; set; }
        public IEnumerable<frMailGonderme> Data { get; set; }
    }
    public class frMailGonderme : GonderilenMailler
    {
        public string EnstituAdi { get; set; }
        public string MailGonderen { get; set; }
        public int EkSayisi { get; set; }
        public int KisiSayisi { get; set; }

    }
}