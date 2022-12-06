using BiskaUtil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using LisansUstuBasvuruSistemi.Models;using LisansUstuBasvuruSistemi.Utilities.Dtos.KmDtos;using LisansUstuBasvuruSistemi.Utilities.Dtos.DmDtos;using LisansUstuBasvuruSistemi.Utilities.Dtos.FmDtos;using LisansUstuBasvuruSistemi.Utilities.Dtos.CmbDtos;

namespace LisansUstuBasvuruSistemi.Utilities.Helpers
{
    public static class LogType
    {
        public static string Insert { get { return "Insert"; } }
        public static string Update { get { return "Update"; } }
        public static string Delete { get { return "Delete"; } }
    }
    public static class LoggingHelper
    {
        public static void LogEkle(string TabloAdi, string IslemTipi, string TableData)
        {
            try
            {

                using (var db = new LubsDBEntities())
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