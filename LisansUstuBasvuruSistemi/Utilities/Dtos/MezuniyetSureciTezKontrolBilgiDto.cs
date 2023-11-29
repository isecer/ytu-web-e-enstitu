using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class MezuniyetSureciTezKontrolDto
    {
        public int MezuniyetSurecId { get; set; }
        public string DonemAdi { get; set; }
        public List<MezuniyetSureciTezKontrolBilgiDto> AktifMezuniyetSureciTezKontrolBilgiDtos { get; set; }
        public List<MezuniyetSureciTezKontrolBilgiDto> PasifMezuniyetSureciTezKontrolBilgiDtos { get; set; }
    }
    public class MezuniyetSureciTezKontrolBilgiDto
    {
        public int KullaniciId { get; set; }
        public string ResimAdi { get; set; }
        public Guid UserKey { get; set; }
        public string AdSoyad { get; set; }
        public int SurecToplamAtanan { get; set; }
        public int SurecToplamKendiOnayi { get; set; }
        public int SurecToplamOnay { get; set; }
        public int GenelToplamAtanan { get; set; }
        public int GenelToplamKendiOnayi { get; set; }
        public int GenelToplamOnay { get; set; }
    }
}