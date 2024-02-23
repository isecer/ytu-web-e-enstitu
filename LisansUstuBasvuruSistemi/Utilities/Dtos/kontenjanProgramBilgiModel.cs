using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Entities.Entities;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class KontenjanProgramBilgiModel : Programlar
    {
        public int BasvuruSurecId { get; set; }
        public int BasvuruId { get; set; }
        public int KullaniciId { get; set; }
        public int OgrenimTipKod { get; set; }
        public string OgrenimTipAdi { get; set; }
        public string AnabilimDaliAdi { get; set; }
        public new string ProgramAdi { get; set; }
        public string AlesTipAdi { get; set; }
        public int AlanTipId { get; set; }
        public bool OrtakKota { get; set; }
        public int? OrtakKotaSayisi { get; set; }
        public int AlanIciKota { get; set; }
        public int AlanDisiKota { get; set; }
        public double? MinAles { get; set; }
        public double? MinAgno { get; set; }
        public string MinAgnoAciklama { get; set; }

        public bool Kazandi { get; set; }
        public bool KayitEdildi { get; set; }

        public string UniqueId { get; set; }

        public KontenjanProgramBilgiModel()
        {
            Kazandi = false;
            KayitEdildi = false;
        }
    }
}