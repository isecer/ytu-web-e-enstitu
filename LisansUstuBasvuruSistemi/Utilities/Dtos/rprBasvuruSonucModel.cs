using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class rprBasvuruSonucModel
    {
        public int SiraNo { get; set; }
        public Guid RowID { get; set; }
        public int? PSiraNo { get; set; }
        public string EnstituKod { get; set; }
        public string WsXmlData { get; set; }
        public bool AlesNotuYuksekOlanAlinsin { get; set; }
        public int BasvuruSurecID { get; set; }
        public string AnabilimDaliKod { get; set; }
        public string AnabilimDaliAdi { get; set; }
        public string ProgramKod { get; set; }
        public string ProgramAdi { get; set; }
        public bool Ingilizce { get; set; }
        public string ProgramGrupAdi { get; set; }
        public int AlesTipID { get; set; }
        public int OgrenimTipKod { get; set; }
        public string OgrenimTipAdi { get; set; }
        public int AlanTipID { get; set; }
        public string AlanTipAdi { get; set; }
        public int Kota { get; set; }
        public int EkKota { get; set; }
        public bool? KayitOldu { get; set; }



        public int TercihID { get; set; }
        public string AdSoyad { get; set; }
        public string Telefon { get; set; }
        public string EMail { get; set; }
        public double? AlesNotu { get; set; }
        public double? GirisSinavNotu { get; set; }
        public double? Agno { get; set; }
        public double? YaziliNotu { get; set; }
        public double? SozluNotu { get; set; }
        public double? GenelBasariNotu { get; set; }
        public int TercihNo { get; set; }
        public int MulakatSonucTipID { get; set; }
        public string MulakatSonucTipAdi { get; set; }

        public int? MulakatID { get; set; }
        public bool? SinavaGirmediY { get; set; }
        public bool? SinavaGirmediS { get; set; }
    }

}