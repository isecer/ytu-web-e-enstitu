using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class KotaKontrolModel
    {
        public string RowClass { get; set; }
        public int AlanTipID { get; set; }
        public bool AlanIci { get; set; }
        public string AlanTipAdi { get; set; }
        public string AlanTipKisaAdi { get; set; }
        public string ProgramKod { get; set; }
        public int Kota { get; set; }
        public int AlesTipID { get; set; }
        public string AlesTipAdi { get; set; }
        public string AlesTipKisaAdi { get; set; }
        public bool AyniProgramBasvurusu { get; set; }
        public bool AlanDisiProgramKisitlamasiVar { get; set; }
        public string AlanDisiProgramKisitlamasiMsg { get; set; }
        public List<string> AlertInputNames { get; set; }
    }
}