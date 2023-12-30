using BiskaUtil;
using LisansUstuBasvuruSistemi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using LisansUstuBasvuruSistemi.Utilities.SystemSetting;

namespace LisansUstuBasvuruSistemi.Utilities.Logs
{
    public class LogCrudType
    {
        public const string Insert = "Insert";
        public const string Update = "Update";
        public const string Delete = "Delete";
    }
    public static class LogIslemleri
    {
        public static void LogEkle(string tabloAdi, string islemTipi, string tableData, bool isSystemUser = false)
        {
            LogEkle(tabloAdi, null, islemTipi, tableData, isSystemUser);
        }
        public static void LogEkle(string tabloAdi, string aciklama, string islemTipi, string tableData, bool isSystemUser = false)
        {
            try
            {

                using (var db = new LisansustuBasvuruSistemiEntities())
                {

                    db.Logs.Add(new Log
                    {
                        TabloAdi = tabloAdi,
                        IslemTipi = islemTipi,
                        Aciklama = aciklama,
                        TableData = tableData,
                        IslemTarihi = DateTime.Now,
                        IslemYapanID = isSystemUser ? GlobalSistemSetting.SystemDefaultAdminKullaniciId : UserIdentity.Current.Id,
                        IslemYapanIP = isSystemUser ? UserIdentity.Ip : "::1"
                    });
                    db.SaveChanges();
                }
            }
            catch
            {
                // ignored
            }
        }
    }
}