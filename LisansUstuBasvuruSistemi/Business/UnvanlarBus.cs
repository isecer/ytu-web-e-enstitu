using System.Collections.Generic;
using System.Linq;
using Entities.Entities;
using LisansUstuBasvuruSistemi.Utilities.Dtos;

namespace LisansUstuBasvuruSistemi.Business
{
    public static class UnvanlarBus
    {
        public static List<string> JuriUnvanList = new List<string> { "PROF.DR.", "DOÇ.DR.", "DR.ÖĞR.ÜYE." };

        public static string ToJuriUnvanAdi(this string unvanAdi)
        {
            unvanAdi = unvanAdi.Trim().ToLower().Replace("  ", ".").Replace(". ", ".").Replace(" .", ".").Replace(" ", ".");
            var profUnvan = new List<string> { "PROFESÖR".ToLower(), "PROFESÖR.DR".ToLower(), "PROF.DR.".ToLower(), "Prof.".ToLower() };
            var docUnvan = new List<string> { "DOÇENT".ToLower(), "DOÇENT.DR".ToLower(), "Doç.".ToLower() };
            var ogUyeUnvan = new List<string> { "DR.ÖĞR.ÜYE".ToLower(), "DR.ÖĞR.ÜYESİ".ToLower(), "DR.ÖĞRETİM.ÜYE".ToLower(), "DR.ÖĞRETİM.ÜYESİ".ToLower() };
            if (profUnvan.Any(a => a.Contains(unvanAdi))) return "PROF.DR.";
            if (docUnvan.Any(a => a.Contains(unvanAdi))) return "DOÇ.DR.";
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