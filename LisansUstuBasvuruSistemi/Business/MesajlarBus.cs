using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Models.ObsService;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.SystemSetting;

namespace LisansUstuBasvuruSistemi.Business
{
    public class MesajlarBus
    {
        public static CmbIntDto GetCevaplanmamisMesajCount(string EnstituKod)
        {

            var model = new CmbIntDto();
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var EnstituKods = UserBus.GetUserEnstituKods(UserIdentity.Current.Id);
                var qListe = db.Mesajlars.Where(p => EnstituKods.Contains(p.EnstituKod) && p.EnstituKod == EnstituKod && p.UstMesajID.HasValue == false && !p.IsAktif && p.Silindi == false).OrderByDescending(o => (o.Mesajlar1.Any() ? o.Mesajlar1.Select(s => s.Tarih).Max() : o.Tarih)).AsQueryable();
                var Liste = qListe.Take(20).ToList();
                var htmlContent = "";
                foreach (var item in Liste)
                {

                    var kul = item.Kullanicilar;
                    htmlContent += "<a href=\"javascript:void(0);\" class=\"list-group-item\" style=\"padding-top:0px;padding-bottom:0px;padding-left:2px;padding-right:-1px;\">" +
                                   "<table style=\"table-layout:fixed;width:100%;\">" +
                                   "<tr>" +
                                   "<td width=\"40\"><img style=\"width:40px;height:40px;\" src=\"" + ((item.KullaniciID > 0 ? item.Kullanicilar.ResimAdi : "").ToKullaniciResim()) + "\" class=\"pull-left\"></td>" +
                                   "<td><span class=\"contacts-title\">" + item.AdSoyad + "</span><span style=\"float:right;font-size:8pt;\"><b>" + (item.Mesajlar1.Any() ? item.Mesajlar1.Select(s => s.Tarih).Max().ToFormatDateAndTime() : item.Tarih.ToFormatDateAndTime()) + "</b></span><p><b>Konu:</b> " + item.Konu + "</p></td>" +
                                   "</tr>" +
                                   "</table>" +
                                   "</a>";
                }
                model.Value = qListe.Count();
                model.Caption = htmlContent;
                return model;

            }
        }

        public static List<CmbIntDto> CmbGetMesajKategorileri(string EnstituKod = "", bool bosSecimVar = false, bool? IsAktif = null)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var qdata = db.MesajKategorileris.AsQueryable();
                if (IsAktif.HasValue) qdata = qdata.Where(p => p.IsAktif == IsAktif.Value);
                if (EnstituKod.IsNullOrWhiteSpace() == false) qdata = qdata.Where(p => p.EnstituKod == EnstituKod);
                var data = qdata.OrderBy(o => o.KategoriAdi).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.MesajKategoriID, Caption = item.Enstituler.EnstituKisaAd + " / " + item.KategoriAdi });
                }
            }
            return dct;

        }

        public static List<CmbIntDto> CmbGetMesajYillari(string EnstituKod = "", bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var qdata = db.Mesajlars.AsQueryable();
                if (EnstituKod.IsNullOrWhiteSpace() == false) qdata = qdata.Where(p => p.MesajKategorileri.EnstituKod == EnstituKod);
                var data = qdata.Select(s => s.Tarih.Year).Distinct().OrderByDescending(o => o).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item, Caption = item.ToString() + " Yılı" });
                }
            }
            return dct;

        }
    }
}