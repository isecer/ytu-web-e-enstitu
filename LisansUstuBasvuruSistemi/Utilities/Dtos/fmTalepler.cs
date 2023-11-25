using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Models;  

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class FmTalepler : PagerModel
    {
        public string EnstituKod { get; set; }
        public int? SRTalepTipID { get; set; }
        public int? SRSalonID { get; set; }
        public int? HaftaGunID { get; set; }
        public TimeSpan? BasSaat { get; set; }
        public TimeSpan? BitSaat { get; set; }
        public int? SRDurumID { get; set; }
        public string Aciklama { get; set; }
        public IEnumerable<FrTalepler> data { get; set; }

    }
    public class FrTalepler : SRTalepleri
    {
        public string EnstituAdi { get; set; }
        public Guid? UserKey { get; set; }
        public string OgrenciNo { get; set; }
        public string SicilNo { get; set; }
        public string TalepYapan { get; set; }
        public string ResimAdi { get; set; }
        public string KullaniciTipAdi { get; set; }
        public string TalepTipAdi { get; set; }
        public bool IsTezSinavi { get; set; }
        public string OgrenimTipAdi { get; set; }
        public string HaftaGunAdi { get; set; }
        public string SDurumAdi { get; set; }
        public string SDurumListeAdi { get; set; }
        public string SClassName { get; set; }
        public string SColor { get; set; }
        public string DurumAdi { get; set; }
        public string DurumListeAdi { get; set; }
        public string ClassName { get; set; }
        public string Color { get; set; }
        public string IslemYapan { get; set; }
        public bool IsSonSrTalebi { get; set; }
        public bool IsOncedenUzatmaAlindi { get; set; }
        public SrDurumSelectList SrDurumSelectList = new SrDurumSelectList();
        public List<SRTaleplerJuri> JuriBilgi { get; set; }
        public DateTime UzatmaTaahhutSonTarih { get; set; }
        public DateTime UzatmaSonSrTarih { get; set; }
        public DateTime TezTeslimSonTarih { get; set; }

        public string JuriSonucMezuniyetSinavDurumAdi { get; set; }
        public bool IsTezDiliTr { get; set; }
        public string TezBaslikTr { get; set; }
        public string TezBaslikEn { get; set; }
    }
    public class SrDurumSelectList
    {
        public SelectList SRDurumID { get; set; }
        public SelectList MezuniyetSinavDurumID { get; set; }
        public SelectList IsMezunOldu { get; set; }
    }
}