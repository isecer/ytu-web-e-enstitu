using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using LisansUstuBasvuruSistemi.Models;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class mailMulakatSinavYerJuriBilgi
    {
        public int MulakatID { get; set; }
        public int BasvuruSurecID { get; set; }
        public int OgrenimTipKod { get; set; }
        public string OgrenimTipAdi { get; set; }
        public string AnabilimDaliKod { get; set; }
        public string AnabilimDaliAdi { get; set; }
        public string ProgramKod { get; set; }
        public string ProgramAdi { get; set; }
        public List<krMulakatDetay> MulakatDetayB { get; set; }
        public List<MulakatJuri> MulakatJuriB { get; set; }
        public List<CmbIntDto> GonderilecekMails { get; set; }
    }
}