using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace LisansUstuBasvuruSistemi.Utilities.Extensions
{
    public static class ResultExtension
    {
        public static JsonResult toJsonResult(this object obj)
        {
            var jsr = new JsonResult();
            jsr.ContentEncoding = System.Text.Encoding.UTF8;
            jsr.ContentType = "application/json";
            jsr.JsonRequestBehavior = JsonRequestBehavior.AllowGet;
            jsr.Data = obj;
            return jsr;
        }
    }
}