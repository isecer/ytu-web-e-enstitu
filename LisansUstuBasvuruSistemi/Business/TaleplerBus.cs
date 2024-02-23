using Entities.Entities;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.MailManager;

namespace LisansUstuBasvuruSistemi.Business
{
    public class TaleplerBus
    {
        public static int? GetAktifTalepSurecId(string enstituKod, int? talepSurecId = null)
        {
            using (var entities = new LubsDbEntities())
            {
                var nowDate = DateTime.Now;
                var bf = entities.TalepSurecleris.FirstOrDefault(p => (p.BaslangicTarihi <= nowDate && p.BitisTarihi >= nowDate) && p.IsAktif && (p.EnstituKod == enstituKod) && p.TalepSurecID == (talepSurecId ?? p.TalepSurecID));

                return bf?.TalepSurecID;
            }
        }

        public static FrTalepSurec GetTalepSurec(int talepSurecId)
        {
            using (var entities = new LubsDbEntities())
            {
                var nowDate = DateTime.Now;
                var xD = (from s in entities.TalepSurecleris.Where(p => p.TalepSurecID == talepSurecId)
                          join k in entities.Kullanicilars on s.IslemYapanID equals k.KullaniciID
                          select new FrTalepSurec
                          {
                              TalepSurecID = s.TalepSurecID,
                              EnstituKod = s.EnstituKod,
                              BaslangicTarihi = s.BaslangicTarihi,
                              BitisTarihi = s.BitisTarihi,
                              IsAktif = s.IsAktif,
                              IslemYapanID = s.IslemYapanID,
                              IslemYapan = k.KullaniciAdi,
                              IslemTarihi = s.IslemTarihi,
                              IslemYapanIP = s.IslemYapanIP,
                              AktifSurec = (s.BaslangicTarihi <= nowDate && s.BitisTarihi >= nowDate)
                          }).FirstOrDefault();
                return xD;
            }
        }

        public static List<CmbIntDto> GetCmbTalepTipleriSurec(int talepSurecId, int talepTipId, bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var entities = new LubsDbEntities())
            {
                var surec = entities.TalepSurecleris.FirstOrDefault(p => p.TalepSurecID == talepSurecId);
                if (surec != null)
                {
                    var talepTipIDs = surec.TalepSureciTalepTipleris.Select(s => s.TalepTipID).ToList();
                    var data = entities.TalepTipleris.Where(p => (p.TalepTipID == talepTipId || talepTipIDs.Contains(p.TalepTipID))).OrderBy(o => o.TalepTipID).ToList();
                    foreach (var item in data)
                    {
                        dct.Add(new CmbIntDto { Value = item.TalepTipID, Caption = item.TalepTipAdi });
                    }
                }
            }
            return dct;
        }

        public static List<CmbIntDto> GetCmbArGorStatuleri(bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var entities = new LubsDbEntities())
            {
                var data = entities.TalepArGorStatuleris.OrderBy(o => o.TalepArGorStatuID).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.TalepArGorStatuID, Caption = item.StatuAdi });
                }
            }
            return dct;
        }

        public static List<CmbIntDto> GetCmbTalepDurumlari(bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var entities = new LubsDbEntities())
            {
                var data = entities.TalepDurumlaris.OrderBy(o => o.TalepDurumID).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.TalepDurumID, Caption = item.TalepDurumAdi });
                }
            }
            return dct;
        }

        public static MmMessage SendTopluBilgiMaili(List<int> talepGelenTalepIDs, string enstituKod, string aciklama = "")
        {
            return MailSenderTalep.SendTopluBilgiMaili(talepGelenTalepIDs, enstituKod, aciklama);
        }

        public static List<CmbIntDto> GetCmbTalepTipleri(bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var entities = new LubsDbEntities())
            {
                var data = entities.TalepTipleris.OrderBy(o => o.TalepTipID).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.TalepTipID, Caption = item.TalepTipAdi });
                }
            }
            return dct;
        }

        public static List<CmbIntDto> GetCmbTalepSurecleri(string enstituKod, bool bosSecimVar = false)
        {
            var lst = new List<CmbIntDto>();
            if (bosSecimVar) lst.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var entities = new LubsDbEntities())
            {
                var data = (from s in entities.TalepSurecleris.Where(p => p.EnstituKod == enstituKod)
                            orderby s.BaslangicTarihi descending
                            select new
                            {
                                s.TalepSurecID,
                                s.BaslangicTarihi,
                                s.BitisTarihi
                            }).ToList();
                foreach (var item in data)
                {
                    lst.Add(new CmbIntDto { Value = item.TalepSurecID, Caption = item.BaslangicTarihi.ToFormatDate() + " - " + item.BitisTarihi.ToFormatDate() });
                }
            }
            return lst;
        }

        public static List<TalepDurumlari> GetTalepDurumList()
        {
            using (var entities = new LubsDbEntities())
            {
                var data = entities.TalepDurumlaris.OrderBy(o => o.TalepDurumID).ToList();
                return data;

            }

        }
    }
}