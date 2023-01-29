using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class SinavSonucBilgiModel
    {
        public string EnstituKod { get; set; }
        public int SinavTipGrupID { get; set; }
        public int WsSinavCekimTipID { get; set; }
        public int? AlesTipID { get; set; }
        public int Durum { get; set; }
        public string SinavKodu { get; set; }
        public string SinavAdi { get; set; }
        public string TCKimlikNo { get; set; }
        public string SinavYili { get; set; }
        public string SinavDonemi { get; set; }
        public DateTime? AciklanmaTarihi { get; set; }
        public Double Puan { get; set; }
        public string WsXmlData { get; set; }
        public int? SinavDilID { get; set; }
        public SinavSonucDilXmlModel jSonValDilSinavi { get; set; }
        public SinavSonucAlesXmlModel jSonValAlesSinavi { get; set; }
        public List<int> SecilenAlesTipleri { get; set; }
        public int? WsSonucID { get; set; }
        public List<CmbIntDto> WsSinavSonucList { get; set; }
        public bool IsSinavSonucuVar { get; set; }
        public bool ShowIsTaahhutVar { get; set; }
        public bool IsTaahhutVar { get; set; }
        public string Aciklama { get; set; }
        public string MinNotAdi { get; set; }
        public SinavSonucBilgiModel()
        {
            WsSinavSonucList = new List<CmbIntDto>();
        }
    }
    public class SinavSonucDilXmlModel
    {
        public string TCK { get; set; }
        public string AD { get; set; }
        public string SOYAD { get; set; }
        public string ENGELDURUM { get; set; }
        public string DIL { get; set; }
        public string DILADI { get; set; }
        public string DIL_ADI { get; set; }
        public string DOGRU_SAY { get; set; }
        public string YANLIS_SAY { get; set; }
        public string PUAN { get; set; }
        public string DUZEY { get; set; }
        public string ASAYISI { get; set; }
        public string BSAYISI { get; set; }
        public string CSAYISI { get; set; }
        public string DSAYISI { get; set; }
        public string ESAYISI { get; set; }
        public string FSAYISI { get; set; }
        public string TOPLAMSAYI { get; set; }
        public object KURUMKOD { get; set; }
        public object KURUMAD { get; set; }
        public object YERKOD { get; set; }
        public object ILAD { get; set; }
        public object ILCEAD { get; set; }
        public string SGK { get; set; }

    }
 
}