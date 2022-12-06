using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using LisansUstuBasvuruSistemi.Models; using LisansUstuBasvuruSistemi.Models.FilterModel;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos.KmDtos
{
    public class kmMezuniyetSureci : MezuniyetSureci
    {
        public string OgretimYili { get; set; }
        public List<int> gID { get; set; }
        public List<string> MezuniyetSurecOtoMailID { get; set; }
        public List<string> ZamanTipID { get; set; }
        public List<string> Zaman { get; set; }
        public List<string> MailSablonTipID { get; set; }



        public List<int?> MezuniyetSureciOgrenimTipKriterID { get; set; }
        public List<int?> OgrenimTipID { get; set; }
        public List<int?> OgrenimTipKod { get; set; }
        public List<string> MBasvuruSonDonemKaydiKontrolEdilecekDersKodlari { get; set; }
        public List<int?> MBasvuruToplamKrediKriteri { get; set; }
        public List<double?> MBasvuruAGNOKriteri { get; set; }
        public List<int?> MBasvuruAKTSKriteri { get; set; }
        public List<int?> MBSinavUzatmaSuresiGun { get; set; }
        public List<int?> MBTezTeslimSuresiGun { get; set; }
        public List<int?> MBSRTalebiKacGunSonraAlabilir { get; set; }


        public kmMezuniyetSureciOgrenimTipModel OgrenimTipModel { get; set; }
        public kmMezuniyetSureci()
        {
            gID = new List<int>();
            MezuniyetSurecOtoMailID = new List<string>();
            ZamanTipID = new List<string>();
            Zaman = new List<string>();
            MailSablonTipID = new List<string>();
            OgrenimTipModel = new kmMezuniyetSureciOgrenimTipModel();
            MezuniyetSureciOgrenimTipKriterID = new List<int?>();
            OgrenimTipID = new List<int?>();
            OgrenimTipKod = new List<int?>();
            MBasvuruToplamKrediKriteri = new List<int?>();
            MBasvuruSonDonemKaydiKontrolEdilecekDersKodlari = new List<string>();
            MBasvuruAGNOKriteri = new List<double?>();
            MBasvuruAKTSKriteri = new List<int?>();
            MBSinavUzatmaSuresiGun = new List<int?>();
            MBTezTeslimSuresiGun = new List<int?>();
            MBSRTalebiKacGunSonraAlabilir = new List<int?>();

        }
    }
    public class kmMezuniyetSureciOgrenimTipModel
    {
        public List<kmMezuniyetSureciOgrenimTipKriterleri> OgrenimTipKriterList { get; set; }
    }
    public class kmMezuniyetSureciOgrenimTipKriterleri : MezuniyetSureciOgrenimTipKriterleri
    {
        public int? SelectedOgrenimTipID { get; set; }
        public bool OrjinalVeri { get; set; }
        public string OgrenimTipAdi { get; set; }
    }
}