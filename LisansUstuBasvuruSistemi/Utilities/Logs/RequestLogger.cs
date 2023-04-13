using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace LisansUstuBasvuruSistemi.Utilities.Logs
{

    public class RequestLogger : ActionFilterAttribute
    {
        private const string LogFilePath = @"C:\lisansutuActionLogs.txt";

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var request = filterContext.HttpContext.Request;
            var url = request.Url.AbsoluteUri;
            var method = request.HttpMethod;
            var ip = request.UserHostAddress;
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

            var logMessage = $"{method} {url} from {ip}";
            File.AppendAllText(LogFilePath, $"{timestamp} | {logMessage}\n"); 
            base.OnActionExecuting(filterContext);
        }
    }
}