using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class TercihRowModel
    {
        public bool IsNewRow { get; set; }
        public string UniqueID { get; set; }
        public int SiraNo { get; set; }
        public int OgrenimTipKod { get; set; }
        public string OgrenimTipAdi { get; set; }
        public string AnabilimDaliKod { get; set; }
        public string AnabilimDaliAdi { get; set; }
        public string ProgramKod { get; set; }
        public string ProgramAdi { get; set; }
        public int AlesTipID { get; set; }
        public string AlesTipAdi { get; set; }
        public bool Ingilizce { get; set; }
        public string MinAgnoAciklama { get; set; }
        public int AlanTipID { get; set; }
        public string AlanTipAdi { get; set; }
        public bool OrtakKota { get; set; }
        public int Kota { get; set; }
        public bool YLEgitimBilgisiIste { get; set; }
        public string MezuniyetBelgesiYolu { get; set; }
        public string MezuniyetBelgesiAdi { get; set; }
    }
}