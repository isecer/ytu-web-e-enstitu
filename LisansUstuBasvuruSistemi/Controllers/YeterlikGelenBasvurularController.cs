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
    [System.Web.Mvc.OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    public class YeterlikGelenBasvurularController : Controller
    {
        private readonly LisansustuBasvuruSistemiEntities _entities = new LisansustuBasvuruSistemiEntities();
        public ActionResult Index(string ekd)
        {
            var enstituKod = EnstituBus.GetSelectedEnstitu(ekd);
            var aktifSurecId = YeterlikBus.GetYeterlikAktifSurecId(enstituKod);
            return Index(new FmYeterlikBasvuruDto { PageSize = 50, YeterlikSurecID = aktifSurecId }, ekd, false);
        }
        [HttpPost]
        public ActionResult Index(FmYeterlikBasvuruDto model, string ekd, bool export)
        {
            var enstituKod = EnstituBus.GetSelectedEnstitu(ekd);
            var q = from yeterlikBasvuru in _entities.YeterlikBasvurus
                    join yeterlikSureci in _entities.YeterlikSurecis.Where(p => p.EnstituKod == enstituKod && UserIdentity.Current.EnstituKods.Contains(p.EnstituKod)) on yeterlikBasvuru.YeterlikSurecID equals yeterlikSureci.YeterlikSurecID
                    join kullanicilar in _entities.Kullanicilars on yeterlikBasvuru.KullaniciID equals kullanicilar.KullaniciID
                    join programlar in _entities.Programlars on yeterlikBasvuru.ProgramKod equals programlar.ProgramKod
                    join ogrenimTipleri in _entities.OgrenimTipleris on yeterlikBasvuru.OgrenimTipID equals ogrenimTipleri.OgrenimTipID
                    join tezDanismani in _entities.Kullanicilars on yeterlikBasvuru.TezDanismanID equals tezDanismani.KullaniciID
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
                        UserKey = kullanicilar.UserKey,
                        AdSoyad = kullanicilar.Ad + " " + kullanicilar.Soyad,
                        TcKimlikNo = kullanicilar.TcKimlikNo,
                        OgrenciNo = yeterlikBasvuru.OgrenciNo,
                        OgrenimTipID = yeterlikBasvuru.OgrenimTipID,
                        OgrenimTipAdi = ogrenimTipleri.OgrenimTipAdi,
                        ProgramAdi = programlar.ProgramAdi,
                        AnabilimDaliID = programlar.AnabilimDaliID,
                        AnabilimDaliAdi = programlar.AnabilimDallari.AnabilimDaliAdi,
                        KayitTarihi = yeterlikBasvuru.KayitTarihi,
                        OkuduguDonemNo = yeterlikBasvuru.OkuduguDonemNo,
                        TezDanismanID = yeterlikBasvuru.TezDanismanID,
                        TezDanismanAdi = tezDanismani.Unvanlar.UnvanAdi + " " + tezDanismani.Ad + " " + tezDanismani.Soyad,
                        TezDanismanEmail = tezDanismani.EMail,
                        TezDanismanCepTel = tezDanismani.CepTel,
                        IsEnstituOnaylandi = yeterlikBasvuru.IsEnstituOnaylandi,
                        EnstituOnayAciklama = yeterlikBasvuru.EnstituOnayAciklama,
                        IsJuriOlusturuldu = yeterlikBasvuru.YeterlikBasvuruJuriUyeleris.Any(),
                        IsAbdKomitesiJuriyiOnayladi = yeterlikBasvuru.IsAbdKomitesiJuriyiOnayladi,
                        YaziliSinavTarihi = yeterlikBasvuru.YaziliSinavTarihi,
                        YaziliSinavYeri = yeterlikBasvuru.YaziliSinavYeri,
                        IsYaziliSinavinaKatildi = yeterlikBasvuru.IsYaziliSinavinaKatildi,
                        YaziliSinaviNotu = yeterlikBasvuru.YaziliSinaviNotu,
                        IsYaziliSinavBasarili = yeterlikBasvuru.IsYaziliSinavBasarili,
                        IsSozluSinavOnline = yeterlikBasvuru.IsSozluSinavOnline,
                        SozluSinavTarihi = yeterlikBasvuru.SozluSinavTarihi,
                        SozluSinavYeri = yeterlikBasvuru.SozluSinavYeri,
                        IsSozluSinavinaKatildi = yeterlikBasvuru.IsSozluSinavinaKatildi,
                        SozluSinaviOrtalamaNotu = yeterlikBasvuru.SozluSinaviOrtalamaNotu,
                        GenelBasariNotu = yeterlikBasvuru.GenelBasariNotu,
                        IsGenelSonucBasarili = yeterlikBasvuru.IsGenelSonucBasarili

                    };
            var q2 = q;
            if (model.YeterlikSurecID.HasValue) q = q.Where(p => p.YeterlikSurecID == model.YeterlikSurecID);
            if (model.OgrenimTipID.HasValue) q = q.Where(p => p.OgrenimTipID == model.OgrenimTipID);
            if (model.AnabilimDaliID.HasValue) q = q.Where(p => p.AnabilimDaliID == model.AnabilimDaliID);
            if (!model.AdSoyad.IsNullOrWhiteSpace()) 
                q = q.Where(p =>
                    p.AdSoyad.Contains(model.AdSoyad) 
                    || p.OgrenciNo.Contains(model.AdSoyad) 
                    || p.TezDanismanAdi.Contains(model.AdSoyad)
                    || p.ProgramAdi.Contains(model.AdSoyad) 
                    || p.AnabilimDaliAdi.Contains(model.AdSoyad));
            if (model.BasvuruDurumID.HasValue)
            {
                if (model.BasvuruDurumID == YeterlikBasvuruFilterEnum.IslemGormeyenler) q = q.Where(p => !p.IsEnstituOnaylandi.HasValue);
                else if (model.BasvuruDurumID == YeterlikBasvuruFilterEnum.Onaylananlar) q = q.Where(p => p.IsEnstituOnaylandi == true);
                else if (model.BasvuruDurumID == YeterlikBasvuruFilterEnum.IptalEdilenler) q = q.Where(p => p.IsEnstituOnaylandi == false);
                else if (model.BasvuruDurumID == YeterlikBasvuruFilterEnum.JuriOlusturulmayanlar) q = q.Where(p => p.IsEnstituOnaylandi == true && p.IsJuriOlusturuldu == false);
                else if (model.BasvuruDurumID == YeterlikBasvuruFilterEnum.KomiteOnayiBekleyenler) q = q.Where(p => p.IsEnstituOnaylandi == true && p.IsJuriOlusturuldu && p.IsAbdKomitesiJuriyiOnayladi != true);
                else if (model.BasvuruDurumID == YeterlikBasvuruFilterEnum.KomiteOnayiTamamlananlar) q = q.Where(p => p.IsEnstituOnaylandi == true && p.IsJuriOlusturuldu && p.IsAbdKomitesiJuriyiOnayladi == true);
                else if (model.BasvuruDurumID == YeterlikBasvuruFilterEnum.SinavSureciniBaslatilmayanlar) q = q.Where(p => (!p.YaziliSinavTarihi.HasValue) && p.IsAbdKomitesiJuriyiOnayladi == true && !p.IsYaziliSinavBasarili.HasValue);
                else if (model.BasvuruDurumID == YeterlikBasvuruFilterEnum.SinavSurecindeOlanlar) q = q.Where(p => (p.YaziliSinavTarihi.HasValue || p.SozluSinavTarihi.HasValue) && p.IsAbdKomitesiJuriyiOnayladi == true && !p.IsGenelSonucBasarili.HasValue);
                else if (model.BasvuruDurumID == YeterlikBasvuruFilterEnum.BasariliOlanlar) q = q.Where(p => p.IsGenelSonucBasarili == true);
                else if (model.BasvuruDurumID == YeterlikBasvuruFilterEnum.BasarisizOlanlar) q = q.Where(p => p.IsGenelSonucBasarili == false);
            }
            var yeterlikGbKayitYetki = RoleNames.YeterlikGelenBasvurularKayit.InRoleCurrent();
            var yeterlikAbdJuriOnayDuzeltme = RoleNames.YeterlikAbdJuriOnayDuzeltme.InRoleCurrent();
            if (!yeterlikGbKayitYetki && !yeterlikAbdJuriOnayDuzeltme)
            {
                q = q.Where(p => p.TezDanismanID == UserIdentity.Current.Id);
            }
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
                                     EnstituOnayDurum = s.IsEnstituOnaylandi.HasValue ? (s.IsEnstituOnaylandi == true ? "Onaylandı" : "İptal Edildi") : "İşlem Bekliyor",
                                     BasariDurumu = s.IsEnstituOnaylandi == true && s.IsGenelSonucBasarili.HasValue ? (s.IsGenelSonucBasarili == true ? "Başarılı" : "Başarısız") : "İşlem Bekliyor",
                                 }).ToList();
                gv.DataBind();
                Response.ContentType = "application/ms-excel";
                Response.ContentEncoding = System.Text.Encoding.UTF8;
                Response.BinaryWrite(System.Text.Encoding.UTF8.GetPreamble());
                StringWriter sw = new StringWriter();
                HtmlTextWriter htw = new HtmlTextWriter(sw);
                gv.RenderControl(htw);
                return File(System.Text.Encoding.UTF8.GetBytes(sw.ToString()), Response.ContentType, "Export_YeterlikBasvuruListesi_" + DateTime.Now.ToFormatDate() + ".xls");
            }
            #endregion 
            var isFiltered = q2 != q;
            model.RowCount = q.Count();
            q = !model.Sort.IsNullOrWhiteSpace() ? q.OrderBy(model.Sort) : q.OrderByDescending(o => o.BasvuruTarihi);
            model.Data = q.Skip(model.StartRowIndex).Take(model.PageSize).ToList();

            ViewBag.kontrolEdilmeyenBasvuruIds = isFiltered ? q.Where(p => !p.IsEnstituOnaylandi.HasValue).Select(s => s.YeterlikBasvuruID).ToList() : new List<int>();
            ViewBag.filteredOgrenciIds = isFiltered ? q.Select(s => s.KullaniciID).ToList() : new List<int>();
            ViewBag.filteredDanismanIds = isFiltered ? q.Select(s => s.TezDanismanID).Distinct().ToList() : new List<int>();

            ViewBag.YeterlikSurecID = new SelectList(YeterlikBus.GetCmbYeterlikSurecleri(enstituKod, true), "Value", "Caption", model.YeterlikSurecID);
            ViewBag.AnabilimDaliID = new SelectList(YeterlikBus.GetCmbFilterYeterlikAnabilimDallari(enstituKod, model.YeterlikSurecID, true), "Value", "Caption", model.AnabilimDaliID);
            ViewBag.BasvuruDurumID = new SelectList(YeterlikBus.GetCmbBasvuruDurumu(true), "Value", "Caption", model.BasvuruDurumID);
            ViewBag.OgrenimTipID = new SelectList(OgrenimTipleriBus.CmbAktifOgrenimTipIdDoktora(enstituKod, true), "Value", "Caption", model.OgrenimTipID);
           
            return View(model);
        }


        [Authorize(Roles = RoleNames.YeterlikBasvuruOnayYetkisi)]
        public ActionResult EnstituOnay(Guid uniqueId, bool? enstituOnay, string enstituOnayAciklama)
        {
            var mmMessage = new MmMessage
            {
                Title = "Enstitu Başvuru Onay İşlemi",
                MessageType = MsgTypeEnum.Warning

            };
            if (enstituOnay == false && enstituOnayAciklama.IsNullOrWhiteSpace())
            {
                mmMessage.Messages.Add("İptal işlemi için İptal Açıklaması giriniz.");
            }

            if (!mmMessage.Messages.Any())
            {
                var basvuru = _entities.YeterlikBasvurus.First(p => p.UniqueID == uniqueId);
                var sendMail = enstituOnay.HasValue && basvuru.IsEnstituOnaylandi != enstituOnay;
                basvuru.IsEnstituOnaylandi = enstituOnay;
                basvuru.EnstituOnayAciklama = enstituOnayAciklama;
                basvuru.EnstituOnayTarihi = DateTime.Now;
                _entities.SaveChanges();
                if (sendMail)

                    mmMessage.IsSuccess = true;
                mmMessage.MessageType = MsgTypeEnum.Success;
                LogIslemleri.LogEkle("YeterlikBasvuru", LogCrudType.Update, basvuru.ToJson());
                if (sendMail)
                    YeterlikBus.SendMailBasvuruOnayi(basvuru.UniqueID);
            }
            var strView = ViewRenderHelper.RenderPartialView("Ajax", "GetMessage", mmMessage);
            return Json(new { mmMessage.IsSuccess, Messages = strView }, "application/json", JsonRequestBehavior.AllowGet);

        }

        [Authorize(Roles = RoleNames.YeterlikBasvuruOnayYetkisi)]
        public ActionResult EnstituTopluOnay(List<int> kontrolEdilmeyenTalepIds)
        {
            var success = true;
            var message = "";

            if (UserIdentity.Current.IsAdmin)
            {
                try
                {
                    var basvurus = _entities.YeterlikBasvurus.Where(p => !p.IsEnstituOnaylandi.HasValue && kontrolEdilmeyenTalepIds.Contains(p.YeterlikBasvuruID)).ToList();

                    var uniqueIds = new List<Guid>();
                    foreach (var item in basvurus)
                    {
                        uniqueIds.Add(item.UniqueID);
                        item.IsEnstituOnaylandi = true;
                        item.EnstituOnayTarihi = DateTime.Now;
                        item.IslemTarihi = DateTime.Now;
                        item.IslemYapanID = UserIdentity.Current.Id;
                        item.IslemYapanIP = UserIdentity.Ip;
                    }
                    _entities.SaveChanges();
                    foreach (var uniqueId in uniqueIds)
                    {
                        YeterlikBus.SendMailBasvuruOnayi(uniqueId);
                    }
                    message = basvurus.Count + " Yeterlik başvurusu onaylandı";
                    LogIslemleri.LogEkle("YeterlikBasvuru", LogCrudType.Update, basvurus.ToJson());

                }
                catch (Exception ex)
                {
                    success = false;
                    message = "Toplu Yeterlik başvuruları Onay işlemi yapılırken bir hata oluştu!";
                    SistemBilgilendirmeBus.SistemBilgisiKaydet("Toplu Yeterlik başvuruları Onay işlemi yapılırken bir hata oluştu! <br/><br/> Hata: " + ex.ToExceptionMessage(), ex.ToExceptionStackTrace(), LogTipiEnum.Hata);
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