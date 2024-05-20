using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BiskaUtil;
using Entities.Entities; using LisansUstuBasvuruSistemi.Utilities.Dtos;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class FmMailGondermeDto : PagerModel
    {
        public string EnstituKod { get; set; }
        public string Konu { get; set; }
        public DateTime? Tarih { get; set; }
        public string Aciklama { get; set; }
        public string MailGonderen { get; set; }
        public bool? IsEkVar { get; set; }
        public IEnumerable<FrMailGondermeDto> MailGondermeDtos { get; set; }
    }
    public class FrMailGondermeDto : GonderilenMailler
    {
        public string EnstituAdi { get; set; }
        public Guid? UserKey { get; set; }
        public string MailGonderen { get; set; }
        public int EkSayisi { get; set; }
        public int KisiSayisi { get; set; }

    }
    public class FmMailIstatistikDto : PagerModel
    {
        public int Yil { get; set; }
        public int? AyId { get; set; }
        public string EnstituKod { get; set; }
        public IEnumerable<FrIstatistikDto> Data { get; set; }
    }
    public class FrIstatistikDto
    {
        public DateTime Tarih { get; set; }
        public int FbeCount { get; set; }
        public int SbeCount { get; set; }
        public int TetCount { get; set; }
        public int ToplamCount { get; set; }
    }

    
}