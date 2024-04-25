using System.Collections.Generic;
using System.Linq;
using Entities.Entities;
using LisansUstuBasvuruSistemi.Utilities.Dtos;

namespace LisansUstuBasvuruSistemi.Business
{
    public static class UnvanlarBus
    {
        public static List<string> JuriUnvanList = new List<string> { "PROF.DR.", "DOÇ.DR.", "DR.ÖĞR.ÜYE." };
        public static List<string> EsDanismanUnvanList=new List<string>{ "ARŞ.GÖR.DR.", "ÖĞR.GÖR.DR.", "DR.", "PROF.DR.", "DOÇ.DR.", "DR.ÖĞR.ÜYE." };
        public static string ToJuriUnvanAdi(this string unvanAdi)
        {
            unvanAdi = unvanAdi.Trim().ToLower().Replace("  ", ".").Replace(". ", ".").Replace(" .", ".").Replace(" ", ".");
            var arGorUnvan = new List<string> { "ARŞ.GÖR.DR." };
            var ogrGrUnvan = new List<string> { "ÖĞR.GÖR.DR." };
            var drUnvan = new List<string> { "DR." };
            var profUnvan = new List<string> { "PROFESÖR".ToLower(), "PROFESÖR.DR".ToLower(), "PROF.DR.".ToLower(), "Prof.".ToLower() };
            var docUnvan = new List<string> { "DOÇENT".ToLower(), "DOÇENT.DR".ToLower(), "Doç.".ToLower() };
            var ogUyeUnvan = new List<string> { "DR.ÖĞR.ÜYE".ToLower(), "DR.ÖĞR.ÜYESİ".ToLower(), "DR.ÖĞRETİM.ÜYE".ToLower(), "DR.ÖĞRETİM.ÜYESİ".ToLower() };
            if (profUnvan.Any(a => a.Contains(unvanAdi))) return "PROF.DR.";
            if (docUnvan.Any(a => a.Contains(unvanAdi))) return "DOÇ.DR.";
            if (arGorUnvan.Any(a => a.Contains(unvanAdi))) return "ARŞ.GÖR.DR.";
            if (ogrGrUnvan.Any(a => a.Contains(unvanAdi))) return "ÖĞR.GÖR.DR.";
            if (drUnvan.Any(a => a.ToLower()==unvanAdi.ToLower())) return "DR.";
            return ogUyeUnvan.Any(a => a.Contains(unvanAdi)) ? "DR.ÖĞR.ÜYE." : unvanAdi.ToUpper();
        }

        public static List<CmbStringDto> GetCmbJuriUnvanlar(bool bosSecimVar = false)
        {
            var dct = new List<CmbStringDto>();
            if (bosSecimVar) dct.Add(new CmbStringDto { Value = "", Caption = "" });
            var unvanList = new List<string> { "PROF.DR.", "DOÇ.DR.", "DR.ÖĞR.ÜYE." };
            dct.AddRange(unvanList.Select(item => new CmbStringDto { Value = item, Caption = item }));
            return dct;

        }
        public static List<CmbStringDto> GetCmbEsDanismanUnvanlar(bool bosSecimVar = false)
        { 
            var dct = new List<CmbStringDto>();
            if (bosSecimVar) dct.Add(new CmbStringDto { Value = "", Caption = "" });
            var unvanList = new List<string> { "ARŞ.GÖR.DR.", "ÖĞR.GÖR.DR.","DR.", "PROF.DR.", "DOÇ.DR.", "DR.ÖĞR.ÜYE." };
            dct.AddRange(unvanList.Select(item => new CmbStringDto { Value = item, Caption = item }));
            return dct;

        }
        public static List<CmbIntDto> CmbUnvanlar(bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var entities = new LubsDbEntities())
            {
                var data = entities.Unvanlars.OrderBy(o => o.UnvanAdi).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.UnvanID, Caption = item.UnvanAdi });
                }
            }
            return dct;

        }
    }
}