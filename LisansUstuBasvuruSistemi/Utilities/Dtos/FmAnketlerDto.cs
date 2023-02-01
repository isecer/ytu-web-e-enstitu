using BiskaUtil;
using LisansUstuBasvuruSistemi.Models;
using System.Collections.Generic;
namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class FmAnketlerDto : PagerOption
    {
        public string EnstituKod { get; set; }
        public string AnketAdi { get; set; }
        public IEnumerable<FrAnketlerDto> FrAnketlers { get; set; }
    }

    public class FrAnketlerDto : Anket
    {
        public string EnstituAdi { get; set; }
        public string DilAdi { get; set; }
        public string DilFlagClass { get; set; }
        public string IslemYapan { get; set; }
        public int SoruSayisi { get; set; }
    }
    public class FrAnketDetayDto : AnketSoru
    {
        public string DilAdi { get; set; }
        public string DilFlagClass { get; set; }
        public int SecenekSayisi { get; set; }
        public string Aciklama { get; set; }
        public string CevapHtml { get; set; }
        public List<FrAnketSecenekDetayDto> FrAnketSecenekDetay { get; set; }

        public List<AnketSeceneklerDetayDto> AnketSeceneklerDetays { get; set; }
        public List<AnketTableDetayDto> AnketTableDetays { get; set; }
        public FrAnketDetayDto()
        {
            AnketSeceneklerDetays = new List<AnketSeceneklerDetayDto>();
            AnketTableDetays = new List<AnketTableDetayDto>();
            FrAnketSecenekDetay = new List<FrAnketSecenekDetayDto>();
        }
    }
    public class AnketSeceneklerDetayDto
    {
        public int SiraNo { get; set; }
        public string SecenekAdi { get; set; }
        public Dictionary<int, string> EkAciklama { get; set; }
        public int Count { get; set; }
    }
    public class FrAnketSecenekDetayDto : AnketSoruSecenek
    {
        public int Count { get; set; }

    }
    public class AnketTableDetayDto
    {
        public string SiraNo { get; set; }
        public string TabloVeri1 { get; set; }
        public string TabloVeri2 { get; set; }
        public string TabloVeri3 { get; set; }
        public string TabloVeri4 { get; set; }
    }
}