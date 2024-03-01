using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Entities.Entities; using LisansUstuBasvuruSistemi.Utilities.Dtos;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class KmMezuniyetBasvuru : MezuniyetBasvurulari
    {
        public int SelectedTabIndex { get; set; }
        public bool Onaylandi { get; set; }
        public bool SbmtForm { get; set; }
        public int StepNo { get; set; }
        public int? SetSelectedStep { get; set; }
        public bool IsYerli { get; set; }
        public string EnstituKod { get; set; }
        public string KullaniciTipAdi { get; set; }
        public int YayinSayisi { get; set; } 
        public List<int> _MezuniyetBasvurulariYayinID { get; set; } = new List<int>();
        public List<bool?> _Yayinlanmis { get; set; } = new List<bool?>();
        public List<DateTime?> _MezuniyetYayinTarih { get; set; } = new List<DateTime?>();
        public List<int> _MezuniyetYayinTurID { get; set; } = new List<int>();
        public List<string> _YayinBasligi { get; set; } = new List<string>();
        public List<HttpPostedFileBase> _MezuniyetYayinBelgesi { get; set; } = new List<HttpPostedFileBase>();
        public List<string> _MezuniyetYayinBelgesiAdi { get; set; } = new List<string>();
        public List<string> _MezuniyetYayinKaynakLinki { get; set; } = new List<string>();
        public List<HttpPostedFileBase> _YayinMetniBelgesi { get; set; } = new List<HttpPostedFileBase>();
        public List<string> _YayinMetniBelgesiAdi { get; set; } = new List<string>();
        public List<string> _MezuniyetYayinLinki { get; set; } = new List<string>();
        public List<int?> _MezuniyetYayinIndexTurID { get; set; } = new List<int?>();
        public List<string> _MezuniyetYayinKabulEdilmisMakaleAdi { get; set; } = new List<string>();
        public List<HttpPostedFileBase> _MezuniyetYayinKabulEdilmisMakaleBelgesi { get; set; } = new List<HttpPostedFileBase>();

        public List<string> _YazarAdi { get; set; } = new List<string>();
        public List<string> _DergiAdi { get; set; } = new List<string>();
        public List<string> _YilCiltSayiSS { get; set; } = new List<string>();
        public List<int?> _MezuniyetYayinProjeTurID { get; set; } = new List<int?>();
        public List<bool?> _IsProjeTamamlandiOrDevamEdiyor { get; set; } = new List<bool?>();
        public List<string> _ProjeEkibi { get; set; } = new List<string>();
        public List<string> _ProjeDeatKurulus { get; set; } = new List<string>();
        public List<string> _TarihAraligi { get; set; } = new List<string>();
        public List<string> _EtkinlikAdi { get; set; } = new List<string>();
        public List<string> _YerBilgisi { get; set; } = new List<string>();

        public string OgrenimTipAdi { get; set; }
        public string AnabilimdaliAdi { get; set; }
        public string ProgramAdi { get; set; }
        public string KayitDonemi { get; set; }
        public string DonemAdi { get; set; }

        public MezuniyetBasvurulariYayinDto YayinBilgisi { get; set; }

        public List<MezuniyetBasvurulariYayinDto> MezuniyetBasvuruYayinlari { get; set; } = new List<MezuniyetBasvurulariYayinDto>();
    }
}