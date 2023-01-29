using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Models;  

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class fmTalepler : PagerOption
    {
        public string EnstituKod { get; set; }
        public int? SRTalepTipID { get; set; }
        public int? SRSalonID { get; set; }
        public int? HaftaGunID { get; set; }
        public TimeSpan? BasSaat { get; set; }
        public TimeSpan? BitSaat { get; set; }
        public int? SRDurumID { get; set; }
        public string Aciklama { get; set; }
        public IEnumerable<frTalepler> data { get; set; }

    }
    public class frTalepler : SRTalepleri
    {
        public string EnstituAdi { get; set; }
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
        public bool IsSonSRTalebi { get; set; }
        public bool IsOncedenUzatmaAlindi { get; set; }
        public SrDurumSelectList SrDurumSelectList = new SrDurumSelectList();
        public List<SRTaleplerJuri> JuriBilgi { get; set; }
        public DateTime UzatmaSonSRTarih { get; set; }
        public DateTime TeslimSonTarih { get; set; }

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