using System;
using System.Linq;
using System.Web.Mvc;
using System.Web.Routing;
using LisansUstuBasvuruSistemi.Business;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
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
            webSite = webSite.Split('?')[0].TrimEnd('/') + "/";

            var apath = uri.AbsolutePath.Split('?')[0];
            var spl = apath.Split('/').Where(p => !string.IsNullOrEmpty(p)).ToList();
            var selectedEnstKisAd = (spl.Count == 0 ? "FBE" : (EnstituBus.IsContainsEnstitu(spl.First()) ? spl.First() : "FBE")).ToLower();

            model.Query = uri.Query;
            model.EnstituKisaAd = selectedEnstKisAd;
            var enst = (selectedEnstKisAd + "/").ToLower();
            model.FakeRoot = model.Root + enst;
            model.DefaultUri = webSite + enst;

            var tspl = spl.Where(p => !selectedEnstKisAd.Equals(p)).ToList();
            model.LastPath = string.Join("/", tspl.Where(p => !EnstituBus.IsContainsEnstitu(p)));
            model.AbsolutePath = $"{model.Root}{enst}{(tspl.Count > 0 ? tspl[0] : "home")}/Index".Replace("I", "i").ToLower();

            return model;
        } 
    }
 

}