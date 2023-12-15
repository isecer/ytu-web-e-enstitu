using System;
using System.Collections.Generic;
using System.Data.Entity.Validation;
using System.Diagnostics;
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
            var stck = new Dictionary<int, string>();

            var ix = 1;
            var innException = ex;
            stck.Add(ix, ex.StackTrace);
            while ((innException = innException.InnerException) != null)
            {
                ix++;
                stck.Add(ix, innException.StackTrace);
            }
            var returnStakCtraceStr = string.Join("\r\n", stck.Select(s => s.Key + "- " + s.Value).ToArray());

            var lineNumberStr = ExceptionLines(ex);
            if (!lineNumberStr.IsNullOrWhiteSpace())
                returnStakCtraceStr = lineNumberStr;
            return returnStakCtraceStr;
        }

        private static string ExceptionLines(Exception ex)
        {
            try
            { 
                var stackTrace = new StackTrace(ex, true);
                var linenumbers = (stackTrace.GetFrames() ?? Array.Empty<StackFrame>())
                    .Where(p => p.GetFileLineNumber() > 0).Select(s =>
                        s.GetFileName() +
                        (s.GetMethod().Name.IsNullOrWhiteSpace() ? "" : "\\" + (s.GetMethod()?.Name + "(" + string.Join(", ", s.GetMethod().GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}")) + ")")) +
                        " : " + s.GetFileLineNumber()
                    ).ToList();
                if (linenumbers.Any())
                {
                    var lineArray = linenumbers.Select(s => s.Split('\\')).ToArray();
                    var lineArrayIndexs = lineArray.Select(s => new { s, Index = Array.IndexOf(s, "LisansUstuBasvuruSistemi") })
                        .ToList();
                    var lines = lineArrayIndexs.Select(p => string.Join(".", p.s.Where((p2, inx) => p.Index < inx).ToList())).ToList();
                    var lineNumberStr = string.Join("\r\n", lines);
                    return lineNumberStr;
                }
            }
            catch (Exception e)
            {
                // ignored
            }

            return "";
        }
    }
}
