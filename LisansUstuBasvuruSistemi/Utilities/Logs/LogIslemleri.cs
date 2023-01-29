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
        public static string Insert { get { return "Insert"; } }
        public static string Update { get { return "Update"; } }
        public static string Delete { get { return "Delete"; } }
    }
    public static class LogIslemleri
    {
        public static void LogEkle(string TabloAdi, string IslemTipi, string TableData)
        {
            try
            {

                using (var db = new LisansustuBasvuruSistemiEntities())
                { 

                    db.Logs.Add(new Log
                    {
                        TabloAdi = TabloAdi, 
                        IslemTipi = IslemTipi,
                        TableData = TableData,
                        IslemTarihi = DateTime.Now,
                        IslemYapanID = UserIdentity.Current.Id,
                        IslemYapanIP = UserIdentity.Ip
                    });
                    db.SaveChanges();
                }
            }
            catch 
            {

            }
        }
    }
}