using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using LisansUstuBasvuruSistemi.Business;
using LisansUstuBasvuruSistemi.Models; using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.SystemSetting;

namespace LisansUstuBasvuruSistemi.Utilities.Helpers
{
    public static class UrlInfoHelper
    {
        public static UrlInfoModel ToUrlInfo(this Uri uri)
        {
            var model = new UrlInfoModel
            {
                Root = GlobalSistemSetting.GetRoot()
            };
            var webSite = uri.AbsoluteUri.Replace(uri.AbsolutePath, "");

            webSite = webSite.IndexOf("?") > -1 ? webSite.Substring(0, webSite.IndexOf("?")) : webSite;
            webSite = webSite.EndsWith("/") ? webSite : webSite + "/";
            var apath = uri.AbsolutePath.IndexOf("?") > -1 ? uri.AbsolutePath.Substring(0, uri.AbsolutePath.IndexOf("?")) : uri.AbsolutePath;
            var spl = apath.Split('/').Where(p => p != "").Select((item, inx) => new { item, inx }).ToList();
            string selectedEnstKisAd = (spl.Count == 0 ? "FBE" : (EnstituBus.IsContainsEnstitu(spl.First().item) ? spl.First().item : "FBE")).ToLower();

            model.Query = uri.Query;
            model.EnstituKisaAd = selectedEnstKisAd;
            var enst = (selectedEnstKisAd + "/").ToLower();
            var tspl = new List<string>();
            model.FakeRoot = model.Root + enst;
            model.DefaultUri = webSite + enst;
            var lstNoEqLnq = new List<string>() { selectedEnstKisAd };
            var laspath = string.Join("/", spl.Where(p => !EnstituBus.IsContainsEnstitu(p.item)).Select(s => s.item));
            foreach (var item in spl.Where(p => !lstNoEqLnq.Contains(p.item)).Select(s => s.item))
            {
                tspl.Add(item);
            }
            if (tspl.Count > 0)
            {

                apath = model.Root + enst + tspl[0] + "/Index";

            }
            else
            {
                apath = model.Root + enst + "home/index";
            }
            model.LastPath = laspath;
            apath = apath.IndexOf("I") > -1 ? apath.Replace("I", "i").ToLower() : apath.ToLower();
            model.AbsolutePath = apath;

            return model;
        }
    }
}