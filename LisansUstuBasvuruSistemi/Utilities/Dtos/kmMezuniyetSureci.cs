using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using LisansUstuBasvuruSistemi.Models; using LisansUstuBasvuruSistemi.Utilities.Dtos;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class KmMezuniyetSureci : MezuniyetSureci
    {
        public string OgretimYili { get; set; }
        public List<int> GId { get; set; }
        public List<string> MezuniyetSurecOtoMailID { get; set; }
        public List<string> ZamanTipID { get; set; }
        public List<string> Zaman { get; set; }
        public List<string> MailSablonTipID { get; set; }



        public List<int?> MezuniyetSureciOgrenimTipKriterID { get; set; }
        public List<int?> OgrenimTipID { get; set; }
        public List<int?> OgrenimTipKod { get; set; }
        public List<string> MBasvuruSonDonemKaydiKontrolEdilecekDersKodlari { get; set; }
        public List<string> MBasvuruEtikNotKriteri { get; set; }
        public List<string> MBasvuruSeminerNotKriteri { get; set; }

        public List<int?> MBasvuruToplamKrediKriteri { get; set; }
        public List<double?> MBasvuruAgnoKriteri { get; set; }
        public List<int?> MBasvuruAktsKriteri { get; set; }
        public List<int?> MbsrTalebiKacGunSonraAlabilir { get; set; }
        public List<int?> MbSinavUzatmaOgrenciTaahhutMaxGun { get; set; }
        public List<int?> MbSinavUzatmaSinavAlmaSuresiMaxGun { get; set; }
        public List<int?> MbTezTeslimSuresiGun { get; set; }


        public KmMezuniyetSureciOgrenimTipModel OgrenimTipModel { get; set; }
        public KmMezuniyetSureci()
        {
            GId = new List<int>();
            MezuniyetSurecOtoMailID = new List<string>();
            ZamanTipID = new List<string>();
            Zaman = new List<string>();
            MailSablonTipID = new List<string>();
            OgrenimTipModel = new KmMezuniyetSureciOgrenimTipModel();
            MezuniyetSureciOgrenimTipKriterID = new List<int?>();
            OgrenimTipID = new List<int?>();
            OgrenimTipKod = new List<int?>();
            MBasvuruToplamKrediKriteri = new List<int?>();
            MBasvuruSonDonemKaydiKontrolEdilecekDersKodlari = new List<string>();
            MBasvuruAgnoKriteri = new List<double?>();
            MBasvuruAktsKriteri = new List<int?>();
            MBasvuruSeminerNotKriteri = new List<string>();
            MBasvuruEtikNotKriteri = new List<string>();
            MbSinavUzatmaSinavAlmaSuresiMaxGun = new List<int?>();
            MbTezTeslimSuresiGun = new List<int?>();
            MbsrTalebiKacGunSonraAlabilir = new List<int?>();

        }
    }
    public class KmMezuniyetSureciOgrenimTipModel
    {
        public List<KmMezuniyetSureciOgrenimTipKriterleri> OgrenimTipKriterList { get; set; }
    }
    public class KmMezuniyetSureciOgrenimTipKriterleri : MezuniyetSureciOgrenimTipKriterleri
    {
        public int? SelectedOgrenimTipID { get; set; }

        public SelectList SlistEtikNots { get; set; }
        public SelectList SlistSeminerNots { get; set; }
        public bool OrjinalVeri { get; set; }
        public string OgrenimTipAdi { get; set; }
    }
}