using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using LisansUstuBasvuruSistemi.Models;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class BasvuruDetayModelTDO : TDOBasvuru
    {
        public bool GelenBasvuru { get; set; }
        public string EnstituAdi { get; set; }
        public string OgrenimDurumAdi { get; set; }
        public string OgrenimTipAdi { get; set; }
        public string AnabilimdaliAdi { get; set; }
        public string ProgramAdi { get; set; }
        public string KayitDonemi { get; set; }
        public string BasvuruKayitSureciTarihi { get; set; }
        public string KullaniciTipAdi { get; set; }
        public string TezDanismanBilgiEslesen { get; set; }
        public string DurumClassName { get; set; }
        public string DurumColor { get; set; }
        public Guid? DegerlendirenUniqueID { get; set; }
        public bool TdoBasvurusuYapabilir { get; set; }
        public bool IsYeniDanismanOneriOrDegisiklik { get; set; }
        public List<TDOBasvuruDanismanModel> TDOBasvuruDanismanList { get; set; }
    }
    public class TDOBasvuruDanismanModel : TDOBasvuruDanisman
    {
        public string DonemAdi { get; set; }
        public string AdSoyad { get; set; }
        public bool IsDuzeltSilYapabilir { get; set; }
        public string VarolanDanismanAd { get; set; }
        public string TalepTipAdi { get; set; }
        public bool VarolanDanismanGozuksun { get; set; }
        public bool VarolanDanismanOnayIslemiAcik { get; set; }
        public bool YeniDanismanOnayIslemiAcik { get; set; }
        public bool IsYeniTezBasligiGozuksun { get; set; }
        public bool TdoEsBasvurusuYapabilir { get; set; }
        public bool IsYeniEsDanismanOneriOrDegisiklik { get; set; }
        public TDOBasvuruEsDanisman EsDanismanBilgi { get; set; }
    }
}