using BiskaUtil;
using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.MenuAndRoles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using LisansUstuBasvuruSistemi.Business;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.SystemData;

namespace LisansUstuBasvuruSistemi.Controllers
{
    [System.Web.Mvc.OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    [Authorize(Roles = RoleNames.AnabilimDallari)]
    public class AnabilimDallariController : Controller
    {
        private LisansustuBasvuruSistemiEntities _entities = new LisansustuBasvuruSistemiEntities();
        public ActionResult Index()
        {
            return Index(new FmAnabilimDallariDto { });
        }
        [HttpPost]
        public ActionResult Index(FmAnabilimDallariDto model)
        {
            var enstKods = UserIdentity.Current.EnstituKods ?? new List<string>();
            var q = from s in _entities.AnabilimDallaris
                    join e in _entities.Enstitulers on s.EnstituKod equals e.EnstituKod
                    where enstKods.Contains(s.EnstituKod)
                    select new FrAnabilimDallariDto
                    {
                        AnabilimDaliID = s.AnabilimDaliID,
                        EnstituKod = s.EnstituKod,
                        EnstituAd = e.EnstituAd,
                        AnabilimDaliKod = s.AnabilimDaliKod,
                        AnabilimDaliAdi = s.AnabilimDaliAdi,
                        IsAktif = s.IsAktif,
                        IslemTarihi = s.IslemTarihi,
                        IslemYapanID = s.IslemYapanID,
                        IslemYapanIP = s.IslemYapanIP,
                        IslemYapan = s.Kullanicilar.Ad + " " + s.Kullanicilar.Soyad,
                    };

            if (!model.EnstituKod.IsNullOrWhiteSpace()) q = q.Where(p => p.EnstituKod == model.EnstituKod);
            if (!model.AnabilimDaliKod.IsNullOrWhiteSpace()) q = q.Where(p => p.AnabilimDaliKod == model.AnabilimDaliKod);
            if (!model.AnabilimDaliAdi.IsNullOrWhiteSpace()) q = q.Where(p => p.AnabilimDaliAdi.Contains(model.AnabilimDaliAdi));
            if (model.IsAktif.HasValue) q = q.Where(p => p.IsAktif == model.IsAktif);
            model.RowCount = q.Count();
            q = !model.Sort.IsNullOrWhiteSpace() ? q.OrderBy(model.Sort) : q.OrderBy(o => o.AnabilimDaliAdi);
            var ps = Management.setStartRowInx(model.StartRowIndex, model.PageIndex, model.PageCount, model.RowCount, model.PageSize);
            model.PageIndex = ps.PageIndex;
            model.FrAnabilimDallaris = q.Skip(ps.StartRowIndex).Take(model.PageSize).ToArray();
            var indexModel = new MIndexBilgi
            {
                Toplam = model.RowCount,
                Aktif = q.Count(p => p.IsAktif),
                Pasif = q.Count(p => !p.IsAktif)
            };
            ViewBag.IndexModel = indexModel;
            ViewBag.EnstituKod = new SelectList(EnstituBus.GetCmbYetkiliEnstituler(true), "Value", "Caption", model.EnstituKod);
            ViewBag.IsAktif = new SelectList(ComboData.GetCmbAktifPasifData(true), "Value", "Caption", model.IsAktif);
            return View(model);
        }
        public ActionResult Kayit(int? id)
        {
            var mmMessage = new MmMessage();
            ViewBag.MmMessage = mmMessage;
            var model = new AnabilimDallari();
            if (id > 0)
            {
                var data = _entities.AnabilimDallaris.FirstOrDefault(p => p.AnabilimDaliID == id);
                if (data != null)
                {
                    model = data;

                }
            }
            ViewBag.EnstituKod = new SelectList(EnstituBus.GetCmbYetkiliEnstituler(true), "Value", "Caption", model.EnstituKod);
            ViewBag.IsAktif = new SelectList(ComboData.GetCmbAktifPasifData(true), "Value", "Caption", model.IsAktif);
            return View(model);
        }
        [HttpPost]
        public ActionResult Kayit(AnabilimDallari kModel)
        {
            var mmMessage = new MmMessage(); 
            #region Kontrol
            
            if (kModel.EnstituKod.IsNullOrWhiteSpace())
            { 
                mmMessage.Messages.Add("Enstitü seçiniz");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "EnstituKod"});
            }
            else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "EnstituKod" });
            if (kModel.AnabilimDaliKod.IsNullOrWhiteSpace())
            { 
                mmMessage.Messages.Add("Anabilim Dalı Kodunu Giriniz.");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "AnabilimDaliKod"});
            }
            else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "AnabilimDaliKod" });
            if (kModel.AnabilimDaliAdi.IsNullOrWhiteSpace())
            { 
                mmMessage.Messages.Add("Anabilim Dalı Adını Giriniz.");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "AnabilimDaliAdi"});
            }
            else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "AnabilimDaliAdi" });

            if (mmMessage.Messages.Count == 0)
            {
                var cnt = _entities.AnabilimDallaris.Count(p => p.AnabilimDaliKod == kModel.AnabilimDaliKod && p.EnstituKod == kModel.EnstituKod && p.AnabilimDaliID != kModel.AnabilimDaliID);
                if (cnt > 0)
                { 
                    mmMessage.Messages.Add("Tanımlamak istediğiniz Anabilim Dalı kodu daha önceden sisteme tanımlanmıştır.");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "AnabilimDaliKod"});
                }

            }

            #endregion
            if (mmMessage.Messages.Count == 0)
            {
                kModel.IslemYapanID = UserIdentity.Current.Id;
                kModel.IslemYapanIP = UserIdentity.Ip;
                kModel.IslemTarihi = DateTime.Now;
                if (kModel.AnabilimDaliID <= 0)
                {
                    kModel.IsAktif = true;
                    var enst = _entities.AnabilimDallaris.Add(kModel);
                }
                else
                {
                    var data = _entities.AnabilimDallaris.First(p => p.AnabilimDaliID == kModel.AnabilimDaliID);
                    data.EnstituKod = kModel.EnstituKod;
                    data.AnabilimDaliKod = kModel.AnabilimDaliKod;
                    data.AnabilimDaliAdi = kModel.AnabilimDaliAdi;
                    data.IsAktif = kModel.IsAktif;
                    data.IslemYapanID = UserIdentity.Current.Id;
                    data.IslemYapanIP = UserIdentity.Ip;
                    data.IslemTarihi = DateTime.Now; ;

                } 
                _entities.SaveChanges();
                return RedirectToAction("Index");
            }
            else
            {
                MessageBox.Show("Uyarı", MessageBox.MessageType.Warning, mmMessage.Messages.ToArray());
            }
            
            ViewBag.MmMessage = mmMessage;
            ViewBag.EnstituKod = new SelectList(EnstituBus.GetCmbYetkiliEnstituler(true), "Value", "Caption", kModel.EnstituKod); 
            ViewBag.IsAktif = new SelectList(ComboData.GetCmbAktifPasifData(true), "Value", "Caption", kModel.IsAktif);
            return View(kModel);
        }
        public ActionResult Sil(int id)
        {
            var kayit = _entities.AnabilimDallaris.FirstOrDefault(p => p.AnabilimDaliID == id); 
            string message = "";
            bool success = true;
            if (kayit != null)
            {

                try
                {
                    message = "'" + kayit.AnabilimDaliAdi + "' İsimli Anabilim Dalı Silindi!";
                    _entities.AnabilimDallaris.Remove(kayit);
                    _entities.SaveChanges();
                }
                catch (Exception ex)
                {
                    success = false;
                    message = "'" + kayit.AnabilimDaliAdi + "' İsimli Anabilim Dalı Silinemedi! <br/> Bilgi:" + ex.ToExceptionMessage();
                    SistemBilgilendirmeBus.SistemBilgisiKaydet(message, "AnabilimDallari/Sil<br/><br/>" + ex.ToExceptionStackTrace(), LogType.OnemsizHata);
                }
            }
            else
            {
                success = false;
                message = "Silmek istediğiniz Anabilim Dalı sistemde bulunamadı!";
            }
            return Json(new { success, message }, "application/json", JsonRequestBehavior.AllowGet);
        }
    }
}
