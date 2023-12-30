using System.Collections.Generic;
using System.Linq;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Extensions;

namespace LisansUstuBasvuruSistemi.Business
{
    public class MesajlarBus
    {
        public static CmbIntDto GetCevaplanmamisMesajCount(string enstituKod)
        {

            var model = new CmbIntDto();
            using (var entities = new LisansustuBasvuruSistemiEntities())
            {
                var enstituKods = UserBus.GetUserEnstituKods(UserIdentity.Current.Id);
                var qListe = entities.Mesajlars.Where(p => enstituKods.Contains(p.EnstituKod) && p.EnstituKod == enstituKod && p.UstMesajID.HasValue == false && !p.IsAktif && p.Silindi == false).OrderByDescending(o => (o.Mesajlar1.Any() ? o.Mesajlar1.Select(s => s.Tarih).Max() : o.Tarih)).AsQueryable();
                var liste = qListe.Take(20).ToList();
                var htmlContent = "";
                foreach (var item in liste)
                {
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

        public static void MesajUpdate(int mesajId)
        {
            using (var entities=new LisansustuBasvuruSistemiEntities())
            {
                var mesaj = entities.Mesajlars.First(p => p.MesajID == mesajId);
                mesaj.IsAktif = true;
                if (mesaj.UstMesajID.HasValue)
                {
                    var ustMesaj = mesaj.Mesajlar2;
                    ustMesaj.ToplamEkSayisi = (ustMesaj.MesajEkleris.Count + ustMesaj.Mesajlar1.Sum(s => s.MesajEkleris.Count) + ustMesaj.GonderilenMaillers.Sum(s => s.GonderilenMailEkleris.Count));
                    ustMesaj.SonMesajTarihi = mesaj.Tarih;
                }
                else
                {

                    mesaj.SonMesajTarihi = mesaj.Mesajlar1.Any() ? mesaj.Mesajlar1.OrderByDescending(s2 => s2.Tarih).First().Tarih : mesaj.Tarih;
                    mesaj.ToplamEkSayisi = (mesaj.MesajEkleris.Count + mesaj.Mesajlar1.Sum(s => s.MesajEkleris.Count) + mesaj.GonderilenMaillers.Sum(s => s.GonderilenMailEkleris.Count));
                }
                entities.SaveChanges();
            }
        }
        public static List<CmbIntDto> CmbGetMesajKategorileri(string enstituKod = "", bool bosSecimVar = false, bool? isAktif = null)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var entities = new LisansustuBasvuruSistemiEntities())
            {
                var qdata = entities.MesajKategorileris.AsQueryable();
                if (isAktif.HasValue) qdata = qdata.Where(p => p.IsAktif == isAktif.Value);
                if (enstituKod.IsNullOrWhiteSpace() == false) qdata = qdata.Where(p => p.EnstituKod == enstituKod);
                var data = qdata.OrderBy(o => o.KategoriAdi).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.MesajKategoriID, Caption = item.Enstituler.EnstituKisaAd + " / " + item.KategoriAdi });
                }
            }
            return dct;

        }

        public static List<CmbIntDto> CmbGetMesajYillari(string enstituKod = "", bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var entities = new LisansustuBasvuruSistemiEntities())
            {
                var qdata = entities.Mesajlars.AsQueryable();
                if (enstituKod.IsNullOrWhiteSpace() == false) qdata = qdata.Where(p => p.MesajKategorileri.EnstituKod == enstituKod);
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