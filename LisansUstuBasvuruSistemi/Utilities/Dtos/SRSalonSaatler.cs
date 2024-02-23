using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Entities.Entities;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class SRSalonSaatler : SRSaatler
    {
        public string HaftaGunAdi { get; set; }
        public int SalonDurumID { get; set; }
        public bool Checked { get; set; }
        public string SalonDurumAdi { get; set; }
        public string Color { get; set; }
        public bool Disabled { get; set; }
        public string Aciklama { get; set; }
    }
    public class SRSalonSaatlerModel
    {
        public bool IsPopupFrame { get; set; }
        public string DilKodu { get; set; }
        public int SRSalonID { get; set; }
        public string SRSalonAdi { get; set; }
        public bool HaftaGunundeSaatlerVar { get; set; }
        public DateTime Tarih { get; set; }
        public int HaftaGunID { get; set; }
        public string HaftaGunAdi { get; set; }
        public int BosSaatSayisi { get; set; }
        public int DoluSaatSayisi { get; set; }
        public string GenelAciklama { get; set; }
        public List<SRSalonSaatler> Data { get; set; }
        public string MzRowID { get; set; }
        public SRSalonSaatlerModel()
        {
            Data = new List<SRSalonSaatler>();
        }
    }
}