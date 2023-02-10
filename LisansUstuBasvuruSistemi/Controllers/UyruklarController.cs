using BiskaUtil;
using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.MenuAndRoles;
using System;
using System.Linq;
using System.Web.Mvc;
using LisansUstuBasvuruSistemi.Business;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.SystemData;

namespace LisansUstuBasvuruSistemi.Controllers
{
    [System.Web.Mvc.OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    [Authorize(Roles = RoleNames.Uyruklar)]
    public class UyruklarController : Controller
    {
        private LisansustuBasvuruSistemiEntities _entities = new LisansustuBasvuruSistemiEntities();
        public ActionResult Index()
        {
            return Index(new FmUyruklar { });
        }
        [HttpPost]
        public ActionResult Index(FmUyruklar model)
        {

            var q = from s in _entities.Uyruklars
                    select s;

            if (model.UyrukKod.HasValue) q = q.Where(p => p.UyrukKod == model.UyrukKod);
            if (!model.KisaAd.IsNullOrWhiteSpace()) q = q.Where(p => p.KisaAd.Contains(model.KisaAd));
            if (!model.Ad.IsNullOrWhiteSpace()) q = q.Where(p => p.Ad.Contains(model.Ad));
            if (model.IsAktif.HasValue) q = q.Where(p => p.IsAktif == model.IsAktif);
            model.RowCount = q.Count();
            q = !model.Sort.IsNullOrWhiteSpace() ? q.OrderBy(model.Sort) : q.OrderBy(o => o.Ad);
            var ps = Management.setStartRowInx(model.StartRowIndex, model.PageIndex, model.PageCount, model.RowCount, model.PageSize);
            model.PageIndex = ps.PageIndex;
            model.data = q.Skip(ps.StartRowIndex).Take(model.PageSize).ToArray();
            var indexModel = new MIndexBilgi
            {
                Toplam = model.RowCount,
                Aktif = q.Count(p => p.IsAktif),
                Pasif = q.Count(p => !p.IsAktif)
            };
            ViewBag.IndexModel = indexModel;
            ViewBag.IsAktif = new SelectList(ComboData.GetCmbAktifPasifData(true), "Value", "Caption", model.IsAktif);
            return View(model);
        }
        public ActionResult Kayit(int? id, string dlgid)
        {
            var mmMessage = new MmMessage
            {
                IsDialog = !dlgid.IsNullOrWhiteSpace(),
                DialogID = dlgid
            };
            ViewBag.MmMessage = mmMessage;
            var model = new Uyruklar();
            if (id > 0)
            {
                var data = _entities.Uyruklars.FirstOrDefault(p => p.UyrukKod == id);
                if (data != null) model = data;
            }
            ViewBag.OldID = model.UyrukKod;
            return View(model);
        }
        [HttpPost]
        public ActionResult Kayit(Uyruklar kModel, int oldId, string dlgid = "")
        {
            var mmMessage = new MmMessage
            {
                IsDialog = !dlgid.IsNullOrWhiteSpace(),
                DialogID = dlgid
            };

            #region Kontrol
            if (oldId <= 0)
            {
                if (kModel.UyrukKod <= 0)
                { 
                    mmMessage.Messages.Add("Uyruk kodu giriniz.");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "UyrukKod" });
                }
                else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "UyrukKod" });
            }
            if (kModel.KisaAd.IsNullOrWhiteSpace())
            { 
                mmMessage.Messages.Add("Uyruk kısa adı Boş bırakılamaz.");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "KisaAd" });
            }
            else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "KisaAd" });
            if (kModel.Ad.IsNullOrWhiteSpace())
            { 
                mmMessage.Messages.Add("Uyruk adı Boş bırakılamaz.");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "Ad" });
            }
            else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "Ad" });
            #endregion
            if (mmMessage.Messages.Count == 0)
            {
                if (oldId <= 0)
                {
                    kModel.IsAktif = true;
                    _entities.Uyruklars.Add(kModel);
                }
                else
                {
                    var data = _entities.Uyruklars.First(p => p.UyrukKod == oldId);
                    data.Ad = kModel.Ad;
                    data.KisaAd = kModel.KisaAd;
                    data.IsAktif = kModel.IsAktif;
                }
                _entities.SaveChanges();
                return RedirectToAction("Index");
            }
            else
            {
                MessageBox.Show("Uyarı", MessageBox.MessageType.Warning, mmMessage.Messages.ToArray());
            }

            ViewBag.MmMessage = mmMessage;
            ViewBag.OldID = oldId;
            return View(kModel);
        }
        public ActionResult Sil(int id)
        {
            var kayit = _entities.Uyruklars.FirstOrDefault(p => p.UyrukKod == id);
            string message = "";
            bool success = true;
            if (kayit != null)
            {

                try
                {
                    message = "'" + kayit.Ad + "' İsimli Uyruk Silindi!";
                    _entities.Uyruklars.Remove(kayit);
                    _entities.SaveChanges();
                }
                catch (Exception ex)
                {
                    success = false;
                    message = "'" + kayit.Ad + "' İsimli Uyruk Silinemedi! <br/> Bilgi:" + ex.ToExceptionMessage();
                    SistemBilgilendirmeBus.SistemBilgisiKaydet(message, "Uyruklar/Sil<br/><br/>" + ex.ToExceptionStackTrace(), LogType.OnemsizHata);
                }
            }
            else
            {
                success = false;
                message = "Silmek istediğiniz Uyruk sistemde bulunamadı!";
            }
            return Json(new { success, message }, "application/json", JsonRequestBehavior.AllowGet);
        }

    }
}
