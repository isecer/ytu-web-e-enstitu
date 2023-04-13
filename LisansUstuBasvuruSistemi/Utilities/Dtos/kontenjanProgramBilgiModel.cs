using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using LisansUstuBasvuruSistemi.Models;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class KontenjanProgramBilgiModel : Programlar
    {
        public int BasvuruSurecID { get; set; }
        public int BasvuruID { get; set; }
        public int KullaniciID { get; set; }
        public int OgrenimTipKod { get; set; }
        public string OgrenimTipAdi { get; set; }
        public string AnabilimDaliAdi { get; set; }
        public string ProgramAdi { get; set; }
        public string AlesTipAdi { get; set; }
        public int AlanTipID { get; set; }
        public bool OrtakKota { get; set; }
        public int? OrtakKotaSayisi { get; set; }
        public int AlanIciKota { get; set; }
        public int AlanDisiKota { get; set; }
        public double? MinAles { get; set; }
        public double? MinAgno { get; set; }
        public string MinAgnoAciklama { get; set; }

        public bool Kazandi { get; set; }
        public bool KayitEdildi { get; set; }

        public string UniqueID { get; set; }

        public KontenjanProgramBilgiModel()
        {
            Kazandi = false;
            KayitEdildi = false;
        }
    }
}