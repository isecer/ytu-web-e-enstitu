using BiskaUtil;
using LisansUstuBasvuruSistemi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LisansUstuBasvuruSistemi.Utilities.Logs
{
    public static class IslemTipi
    {
        public static string Insert => "Insert";
        public static string Update => "Update";
        public static string Delete => "Delete";
    }
    public static class LogIslemleri
    {
        public static void LogEkle(string tabloAdi, string islemTipi, string tableData)
        {
            try
            {

                using (var db = new LisansustuBasvuruSistemiEntities())
                { 

                    db.Logs.Add(new Log
                    {
                        TabloAdi = tabloAdi, 
                        IslemTipi = islemTipi,
                        TableData = tableData,
                        IslemTarihi = DateTime.Now,
                        IslemYapanID = UserIdentity.Current.Id,
                        IslemYapanIP = UserIdentity.Ip
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