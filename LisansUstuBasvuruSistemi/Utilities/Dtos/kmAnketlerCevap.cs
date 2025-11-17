using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class KmAnketlerCevap
    {
        public int AnketTipID { get; set; }
        public string RowID { get; set; }
        public int BasvuruSurecID { get; set; }
        public int AnketID { get; set; }
        public string AnketAdi { get; set; }
        public string JsonStringData { get; set; }
        public List<AnketCevapDto> AnketCevapModel { get; set; }
        public List<int> AnketSoruID { get; set; }
        public List<int?> AnketSoruSecenekID { get; set; }
        public List<string> TabloVeri1 { get; set; }
        public List<string> TabloVeri2 { get; set; }
        public List<string> TabloVeri3 { get; set; }
        public List<string> TabloVeri4 { get; set; }
        public List<string> TabloVeri5 { get; set; }
        public List<string> AnketSoruSecenekAciklama { get; set; }
        public KmAnketlerCevap()
        {
            AnketCevapModel = new List<AnketCevapDto>();
            AnketSoruID = new List<int>();
            AnketSoruSecenekID = new List<int?>();
            TabloVeri1 = new List<string>();
            TabloVeri2 = new List<string>();
            TabloVeri3 = new List<string>();
            TabloVeri4 = new List<string>();
            TabloVeri5 = new List<string>();
            AnketSoruSecenekAciklama = new List<string>();
        }
    }
    public class AnketPostGroupModel
    {
        public int inx { get; set; }
        public int AnketID { get; set; }
        public int AnketSoruID { get; set; }
        public bool IsTabloVeriGirisi { get; set; }
        public int? IsTabloVeriMaxSatir { get; set; }
        public int SecenekCount { get; set; }
        public int? AnketSoruSecenekID { get; set; }
        public string AnketSoruSecenekAciklama { get; set; }
        public bool SoruCevabiYanlis { get; set; }
        public bool IsEkAciklamaGir { get; set; }

        public List<AnketTabloVeriGirisModel> TabloVerileri { get; set; }
        public AnketPostGroupModel()
        {
            TabloVerileri = new List<AnketTabloVeriGirisModel>();
        }
    }
    public class AnketTabloVeriGirisModel
    {
        public string TabloVeri1 { get; set; }
        public string TabloVeri2 { get; set; }
        public string TabloVeri3 { get; set; }
        public string TabloVeri4 { get; set; }
        public string TabloVeri5 { get; set; }
        public bool InsertTablerRow { get; set; }
    }

}