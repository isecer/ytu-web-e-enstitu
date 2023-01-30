using System;
using System.Collections.Generic;
using System.Data.Entity.Validation;
using System.Linq;
using System.Text;

namespace LisansUstuBasvuruSistemi.Utilities.Extensions
{
   public static class ExceptionExtension
    {
        public static string ToExceptionMessage(this Exception ex)
        {
            var ix = 1;
            var msgs = new Dictionary<int, string>() { { ix, ex.Message } };
            var innException = ex;
            while ((innException = innException.InnerException) != null)
            {
                ix++;
                msgs.Add(ix, innException.Message);
            }
            var returnMsg = string.Join("\r\n", msgs.Select(s => s.Key + "- " + s.Value).ToArray());

            if (ex is DbEntityValidationException)
            {
                var msgsVex = new List<string>();
                var exV = (DbEntityValidationException)ex;
                foreach (var eve in exV.EntityValidationErrors)
                {
                    foreach (var ve in eve.ValidationErrors)
                    {
                        msgsVex.Add($"State: {eve.Entry.State} Property: {ve.PropertyName}, Error: {ve.ErrorMessage}");
                    }
                }
                if (msgsVex.Any())
                {
                    msgsVex.Insert(0, "Veri Giriş Hataları:");
                    returnMsg += "\r\n" + string.Join("\r\n", msgsVex);
                }
            }

            return returnMsg;
        }
        public static string ToExceptionStackTrace(this Exception ex)
        {
            Dictionary<int, string> stck = new Dictionary<int, string>();

            int ix = 1;
            var innException = ex;
            stck.Add(ix, ex.StackTrace);
            while ((innException = innException.InnerException) != null)
            {
                ix++;
                stck.Add(ix, innException.StackTrace);
            }
            return string.Join("\r\n", stck.Select(s => s.Key + "- " + s.Value).ToArray());
        }
    }
}
