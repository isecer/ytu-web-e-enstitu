using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.UI;
using System.Web.UI.WebControls;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Business;
using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.Helpers;
using LisansUstuBasvuruSistemi.Utilities.Logs;
using LisansUstuBasvuruSistemi.Utilities.MenuAndRoles;

namespace LisansUstuBasvuruSistemi.Controllers
{
    [Authorize(Roles = RoleNames.YeterlikGelenBasvurular)]
    public class YeterlikGelenBasvurularController : Controller
    {
        private readonly LisansustuBasvuruSistemiEntities _context = new LisansustuBasvuruSistemiEntities();
        public ActionResult Index(string ekd)
        {
            var enstituKod = EnstituBus.GetSelectedEnstitu(ekd);
            var aktifSurecId = YeterlikBus.GetYeterlikAktifSurecId(enstituKod);
            return Index(new FmYeterlikBasvuruDto { PageSize = 40, YeterlikSurecID = aktifSurecId }, ekd, false);
        }
        [HttpPost]
        public ActionResult Index(FmYeterlikBasvuruDto model, string ekd, bool export)
        {
            var enstituKod = EnstituBus.GetSelectedEnstitu(ekd);
            var q = from yeterlikBasvuru in _context.YeterlikBasvurus
                    join yeterlikSureci in _context.YeterlikSurecis on yeterlikBasvuru.YeterlikSurecID equals yeterlikSureci.YeterlikSurecID
                    join kullanicilar in _context.Kullanicilars on yeterlikBasvuru.KullaniciID equals kullanicilar.KullaniciID
                    join programlar in _context.Programlars on yeterlikBasvuru.ProgramKod equals programlar.ProgramKod
                    join ogrenimTipleri in _context.OgrenimTipleris on yeterlikBasvuru.OgrenimTipID equals ogrenimTipleri.OgrenimTipID
                    join tezDanismani in _context.Kullanicilars on yeterlikBasvuru.TezDanismanID equals tezDanismani.KullaniciID
                    select new FrYeterlikBasvuruDto
                    {
                        YeterlikSurecID = yeterlikBasvuru.YeterlikSurecID,
                        DonemAdi = yeterlikSureci.BaslangicYil + "/" + yeterlikSureci.BitisYil + " " + yeterlikSureci.Donemler.DonemAdi,
                        YeterlikBasvuruID = yeterlikBasvuru.YeterlikBasvuruID,
                        UniqueID = yeterlikBasvuru.UniqueID,
                        BasvuruTarihi = yeterlikBasvuru.BasvuruTarihi,
                        ResimAdi = kullanicilar.ResimAdi,
                        EMail = kullanicilar.EMail,
                        CepTel = kullanicilar.CepTel,
                        KullaniciID = yeterlikBasvuru.KullaniciID,
                        AdSoyad = kullanicilar.Ad + " " + kullanicilar.Soyad,
                        TcKimlikNo = kullanicilar.TcKimlikNo,
                        OgrenciNo = yeterlikBasvuru.OgrenciNo,
                        OgrenimTipID = yeterlikBasvuru.OgrenimTipID,
                        OgrenimTipAdi = ogrenimTipleri.OgrenimTipAdi,
                        ProgramAdi = programlar.ProgramAdi,
                        AnabilimDaliAdi = programlar.AnabilimDallari.AnabilimDaliAdi,
                        KayitTarihi = yeterlikBasvuru.KayitTarihi,
                        OkuduguDonemNo = yeterlikBasvuru.OkuduguDonemNo,
                        TezDanismanAdi = tezDanismani.Unvanlar.UnvanAdi + " " + tezDanismani.Ad + " " + tezDanismani.Soyad,
                        TezDanismanEmail = tezDanismani.EMail,
                        TezDanismanCepTel = tezDanismani.CepTel,
                        IsOnaylandi = yeterlikBasvuru.IsOnaylandi,
                        OnayAciklama = yeterlikBasvuru.OnayAciklama,
                        IsBasarili = yeterlikBasvuru.IsBasarili,

                    };
            if (model.YeterlikSurecID.HasValue) q = q.Where(p => p.YeterlikSurecID == model.YeterlikSurecID);
            if (model.OgrenimTipID.HasValue) q = q.Where(p => p.OgrenimTipID == model.OgrenimTipID);
            if (!model.AdSoyad.IsNullOrWhiteSpace()) q = q.Where(p => p.AdSoyad.Contains(model.AdSoyad) || p.OgrenciNo == model.AdSoyad || p.ProgramAdi.Contains(model.AdSoyad) || p.AnabilimDaliAdi.Contains(model.AdSoyad));
            if (model.BasvuruDurumID.HasValue)
            {
                if (model.BasvuruDurumID == 0) q = q.Where(p => !p.IsOnaylandi.HasValue);
                else if (model.BasvuruDurumID == 1) q = q.Where(p => p.IsOnaylandi == true);
                else if (model.BasvuruDurumID == 2) q = q.Where(p => p.IsOnaylandi == false);
            }
            var isFiltered = q.Expression.ToString().Contains("Where");
            ViewBag.kontrolEdilmeyenBasvuruIds = isFiltered ? q.Where(p => !p.IsOnaylandi.HasValue).Select(s => s.YeterlikBasvuruID).ToList() : new List<int>();

            model.RowCount = q.Count();
            q = !model.Sort.IsNullOrWhiteSpace() ? q.OrderBy(model.Sort) : q.OrderByDescending(o => o.BasvuruTarihi);
            var ps = Management.setStartRowInx(model.StartRowIndex, model.PageIndex, model.PageCount, model.RowCount, model.PageSize);
            model.PageIndex = ps.PageIndex;
            model.Data = q.Skip(ps.StartRowIndex).Take(model.PageSize).ToList();

            ViewBag.YeterlikSurecID = new SelectList(YeterlikBus.GetCmbYeterlikSurecleri(enstituKod, true), "Value", "Caption", model.YeterlikSurecID);
            ViewBag.BasvuruDurumID = new SelectList(YeterlikBus.GetCmbBasvuruDurumu(true), "Value", "Caption", model.BasvuruDurumID);
            ViewBag.OgrenimTipID = new SelectList(OgrenimTipleriBus.CmbAktifOgrenimTipIdDoktora(enstituKod, true), "Value", "Caption", model.OgrenimTipID);
            #region export
            if (export && model.RowCount > 0)
            {
                var gv = new GridView();
                gv.DataSource = (from s in q
                                 select new
                                 {
                                     s.DonemAdi,
                                     s.AdSoyad,
                                     s.TcKimlikNo,
                                     s.OgrenciNo,
                                     s.EMail,
                                     s.CepTel,
                                     s.OgrenimTipAdi,
                                     s.AnabilimDaliAdi,
                                     s.ProgramAdi,
                                     s.KayitTarihi,
                                     s.OkuduguDonemNo,
                                     s.TezDanismanAdi,
                                     s.TezDanismanCepTel,
                                     s.TezDanismanEmail,
                                     EnstituOnayDurum = s.IsOnaylandi.HasValue ? (s.IsOnaylandi == true ? "Onaylandı" : "İptal Edildi") : "İşlem Bekliyor"
                                 }).ToList();
                gv.DataBind();
                Response.ContentType = "application/ms-excel";
                Response.ContentEncoding = System.Text.Encoding.UTF8;
                Response.BinaryWrite(System.Text.Encoding.UTF8.GetPreamble());
                StringWriter sw = new StringWriter();
                HtmlTextWriter htw = new HtmlTextWriter(sw);
                gv.RenderControl(htw);

                return File(System.Text.Encoding.UTF8.GetBytes(sw.ToString()), Response.ContentType, "Export_YeterlikBasvuruListesi_" + DateTime.Now.ToString("dd.MM.yyyy") + ".xls");
            }
            #endregion
            return View(model);
        }


        [Authorize(Roles = RoleNames.YeterlikGelenBasvurularKayit)]
        public ActionResult EnstituOnay(Guid uniqueId, bool? enstituOnay, string enstituOnayAciklama)
        {
            var mmMessage = new MmMessage
            {
                Title = "Enstitu Başvuru Onay İşlemi",
                IsSuccess = false
            };
            if (enstituOnay == false && enstituOnayAciklama.IsNullOrWhiteSpace())
            {
                mmMessage.Messages.Add("İptal işlemi için İptal Açıklaması giriniz.");
            }

            if (!mmMessage.Messages.Any())
            {
                var basvuru = _context.YeterlikBasvurus.First(p => p.UniqueID == uniqueId);
                var sendMail = enstituOnay.HasValue && basvuru.IsOnaylandi != enstituOnay;
                basvuru.IsOnaylandi = enstituOnay;
                basvuru.OnayAciklama = enstituOnayAciklama;
                basvuru.OnayTarihi = DateTime.Now;
                _context.SaveChanges();
                mmMessage.IsSuccess = true;
                LogIslemleri.LogEkle("YeterlikBasvuru", IslemTipi.Update, basvuru.ToJson());
                if (sendMail)
                    YeterlikBus.SendMailYeterlikOnay(new List<int> { basvuru.YeterlikBasvuruID }, enstituOnay.Value);
            }
            var strView = ViewRenderHelper.RenderPartialView("Ajax", "GetMessage", mmMessage);
            return Json(new { mmMessage.IsSuccess, Messages = strView }, "application/json", JsonRequestBehavior.AllowGet);

        }

        [Authorize(Roles = RoleNames.YeterlikGelenBasvurularKayit)]
        public ActionResult EnstituTopluOnay(List<int> kontrolEdilmeyenTalepIds)
        {
            var success = true;
            var message = "";

            if (UserIdentity.Current.IsAdmin)
            {
                try
                {
                    var basvurus = _context.YeterlikBasvurus.Where(p => !p.IsOnaylandi.HasValue && kontrolEdilmeyenTalepIds.Contains(p.YeterlikBasvuruID)).ToList();

                    foreach (var item in basvurus)
                    {
                        item.IsOnaylandi = true;
                        item.OnayTarihi = DateTime.Now;
                        item.IslemTarihi = DateTime.Now;
                        item.IslemYapanID = UserIdentity.Current.Id;
                        item.IslemYapanIP = UserIdentity.Ip;
                    }
                    _context.SaveChanges();
                    YeterlikBus.SendMailYeterlikOnay(kontrolEdilmeyenTalepIds, true);
                    message = basvurus.Count + " Yeterlik başvurusu onaylandı";
                    LogIslemleri.LogEkle("YeterlikBasvuru", IslemTipi.Update, basvurus.ToJson());

                }
                catch (Exception ex)
                {
                    success = false;
                    message = "Toplu Yeterlik başvuruları Onay işlemi yapılırken bir hata oluştu!";
                    SistemBilgilendirmeBus.SistemBilgisiKaydet("Toplu Yeterlik başvuruları Onay işlemi yapılırken bir hata oluştu! <br/><br/> Hata: " + ex.ToExceptionMessage(), ex.ToExceptionStackTrace(), LogType.Hata);
                }
            }
            else
            {
                success = false;
                message = "Bu işlemi yapmaya yetkili değilsiniz.";
            }

            return new { success, message }.ToJsonResult();
        }
    }
}