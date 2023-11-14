using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using LisansUstuBasvuruSistemi.Models;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class MulakatSinavYerJuriBilgiMailDto
    {
        public int MulakatID { get; set; }
        public int BasvuruSurecID { get; set; }
        public int OgrenimTipKod { get; set; }
        public string OgrenimTipAdi { get; set; }
        public string AnabilimDaliKod { get; set; }
        public string AnabilimDaliAdi { get; set; }
        public string ProgramKod { get; set; }
        public string ProgramAdi { get; set; }
        public List<KrMulakatDetay> MulakatDetayB { get; set; }
        public List<MulakatJuri> MulakatJuriB { get; set; }
        public List<CmbIntDto> GonderilecekMails { get; set; }
    }
    public class MulakatSinavYerJuriBilgiBolumMailDto
    {
        public string AnabilimDaliKod { get; set; }
        public string AnabilimDaliAdi { get; set; }
        public List<MulakatSinavYerJuriBilgiBolumDetayMailDto> MulakatSinavYerJuriBilgiBolumDetayMailDtos { get; set; }
        public List<string> GonderilecekMails { get; set; }
        public MulakatSinavYerJuriBilgiBolumMailDto()
        {
            MulakatSinavYerJuriBilgiBolumDetayMailDtos = new List<MulakatSinavYerJuriBilgiBolumDetayMailDto>();
        }
    }
    public class MulakatSinavYerJuriBilgiBolumDetayMailDto
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