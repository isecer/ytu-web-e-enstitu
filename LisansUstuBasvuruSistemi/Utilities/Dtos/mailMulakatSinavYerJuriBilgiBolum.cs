using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class mailMulakatSinavYerJuriBilgiBolum
    {
        public string AnabilimDaliKod { get; set; }
        public string AnabilimDaliAdi { get; set; }
        public List<mailMulakatSinavYerJuriBilgiBolumDetay> detay { get; set; }
        public List<string> GonderilecekMails { get; set; }
        public mailMulakatSinavYerJuriBilgiBolum()
        {
            detay = new List<mailMulakatSinavYerJuriBilgiBolumDetay>();
        }
    }
    public class mailMulakatSinavYerJuriBilgiBolumDetay
    {
        public bool EksikBilgiSinavYerBilgi { get; set; }
        public string ProgramKod { get; set; }
        public string ProgramAdi { get; set; }
        public int OgrenimTipKod { get; set; }
        public string OgrenimTipAdi { get; set; }
        public int AlaniciBasvuranCount { get; set; }
        public int AlandisiBasvuranCount { get; set; }
        public int AlaniciKota { get; set; }
        public int AlandisiKota { get; set; }
    }
}