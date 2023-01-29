using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using LisansUstuBasvuruSistemi.Models; using LisansUstuBasvuruSistemi.Utilities.Dtos;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class kmMezuniyetBasvuru : MezuniyetBasvurulari
    {
        public int SelectedTabIndex { get; set; }
        public bool Onaylandi { get; set; }
        public bool sbmtForm { get; set; }
        public int StepNo { get; set; }
        public int? SetSelectedStep { get; set; }
        public bool IsYerli { get; set; }
        public string EnstituKod { get; set; }
        public string KullaniciTipAdi { get; set; }
        public int YayinSayisi { get; set; }
        public string DanismanImzaliFormDosyaAdi2 { get; set; }
        public HttpPostedFileBase DanismanImzaliFormDosya { get; set; }
        public List<int> _MezuniyetBasvurulariYayinID { get; set; }
        public List<bool?> _Yayinlanmis { get; set; }
        public List<DateTime?> _MezuniyetYayinTarih { get; set; }
        public List<int> _MezuniyetYayinTurID { get; set; }
        public List<string> _YayinBasligi { get; set; }
        public List<HttpPostedFileBase> _MezuniyetYayinBelgesi { get; set; }
        public List<string> _MezuniyetYayinBelgesiAdi { get; set; }
        public List<string> _MezuniyetYayinKaynakLinki { get; set; }
        public List<HttpPostedFileBase> _YayinMetniBelgesi { get; set; }
        public List<string> _YayinMetniBelgesiAdi { get; set; }
        public List<string> _MezuniyetYayinLinki { get; set; }
        public List<int?> _MezuniyetYayinIndexTurID { get; set; }
        public List<string> _MezuniyetYayinKabulEdilmisMakaleAdi { get; set; }
        public List<HttpPostedFileBase> _MezuniyetYayinKabulEdilmisMakaleBelgesi { get; set; }

        public List<string> _YazarAdi { get; set; }
        public List<string> _DergiAdi { get; set; }
        public List<string> _YilCiltSayiSS { get; set; }
        public List<int?> _MezuniyetYayinProjeTurID { get; set; }
        public List<bool?> _IsProjeTamamlandiOrDevamEdiyor { get; set; }
        public List<string> _ProjeEkibi { get; set; }
        public List<string> _ProjeDeatKurulus { get; set; }
        public List<string> _TarihAraligi { get; set; }
        public List<string> _EtkinlikAdi { get; set; }
        public List<string> _YerBilgisi { get; set; }

        public string OgrenimTipAdi { get; set; }
        public string AnabilimdaliAdi { get; set; }
        public string ProgramAdi { get; set; }
        public string KayitDonemi { get; set; }
        public string DonemAdi { get; set; }

        public MezuniyetBasvurulariYayinDto YayinBilgisi { get; set; }

        public List<MezuniyetBasvurulariYayinDto> MezuniyetBasvuruYayinlari { get; set; }
        public kmMezuniyetBasvuru()
        {
            _MezuniyetBasvurulariYayinID = new List<int>();
            _Yayinlanmis = new List<bool?>();
            _MezuniyetYayinTarih = new List<DateTime?>();
            MezuniyetBasvuruYayinlari = new List<MezuniyetBasvurulariYayinDto>();
            _MezuniyetYayinTurID = new List<int>();
            _YayinBasligi = new List<string>();
            _MezuniyetYayinBelgesi = new List<HttpPostedFileBase>();
            _MezuniyetYayinBelgesiAdi = new List<string>();
            _MezuniyetYayinKaynakLinki = new List<string>();
            _YayinMetniBelgesi = new List<HttpPostedFileBase>();
            _YayinMetniBelgesiAdi = new List<string>();
            _MezuniyetYayinLinki = new List<string>();
            _MezuniyetYayinIndexTurID = new List<int?>();
            _MezuniyetYayinKabulEdilmisMakaleAdi = new List<string>();
            _MezuniyetYayinKabulEdilmisMakaleBelgesi = new List<HttpPostedFileBase>();

            _YazarAdi = new List<string>();
            _DergiAdi = new List<string>();
            _YilCiltSayiSS = new List<string>();
            _MezuniyetYayinProjeTurID = new List<int?>();
            _IsProjeTamamlandiOrDevamEdiyor = new List<bool?>();
            _ProjeEkibi = new List<string>();
            _ProjeDeatKurulus = new List<string>();
            _TarihAraligi = new List<string>();
            _EtkinlikAdi = new List<string>();
            _YerBilgisi = new List<string>();
        }
    }
}