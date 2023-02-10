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
    [Authorize(Roles = RoleNames.Sehirler)]
    public class SehirlerController : Controller
    {
        private readonly LisansustuBasvuruSistemiEntities _entities = new LisansustuBasvuruSistemiEntities();
        public ActionResult Index()
        {
            return Index(new FmSehirler { });
        }
        [HttpPost]
        public ActionResult Index(FmSehirler model)
        {

            var q = from s in _entities.Sehirlers
                    select s;

            if (model.SehirKod.HasValue) q = q.Where(p => p.SehirKod == model.SehirKod);
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
            var model = new Sehirler();
            if (id.HasValue && id > 0)
            {
                var data = _entities.Sehirlers.FirstOrDefault(p => p.SehirKod == id);
                if (data != null) model = data;
            }
            ViewBag.OldID = model.SehirKod;
            return View(model);
        }
        [HttpPost]
        public ActionResult Kayit(Sehirler kModel, int oldId, string dlgid = "")
        {
            var mmMessage = new MmMessage
            {
                IsDialog = !dlgid.IsNullOrWhiteSpace(),
                DialogID = dlgid
            };

            #region Kontrol
            if (oldId <= 0)
            {
                if (kModel.SehirKod <= 0)
                { 
                    mmMessage.Messages.Add("Şehir kodu giriniz.");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "SehirKod" });
                }
                else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "SehirKod" });
            }
            if (kModel.Ad.IsNullOrWhiteSpace())
            { 
                mmMessage.Messages.Add("Şehir adı Boş bırakılamaz.");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "Ad" });
            }
            else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "Ad" });
            #endregion
            if (mmMessage.Messages.Count == 0)
            {
                kModel.IslemTarihi = DateTime.Now;
                kModel.IslemYapanID = UserIdentity.Current.Id;
                kModel.IslemYapanIP = UserIdentity.Ip;
                if (oldId <= 0)
                {
                    kModel.IsAktif = true;
                    _entities.Sehirlers.Add(kModel);
                }
                else
                {
                    var data = _entities.Sehirlers.First(p => p.SehirKod == oldId);
                    data.Ad = kModel.Ad;
                    data.IsAktif = kModel.IsAktif;
                    data.IslemYapanIP = kModel.IslemYapanIP;
                    data.IslemYapanID = kModel.IslemYapanID;
                    data.IslemTarihi = kModel.IslemTarihi;
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
            var kayit = _entities.Sehirlers.FirstOrDefault(p => p.SehirKod == id);
            string message = "";
            bool success = true;
            if (kayit != null)
            {

                try
                {
                    message = "'" + kayit.Ad + "' İsimli Şehir Silindi!";
                    _entities.Sehirlers.Remove(kayit);
                    _entities.SaveChanges();
                }
                catch (Exception ex)
                {
                    success = false;
                    message = "'" + kayit.Ad + "' İsimli Şehir Silinemedi! <br/> Bilgi:" + ex.ToExceptionMessage();
                    SistemBilgilendirmeBus.SistemBilgisiKaydet(message, "Sehirler/Sil<br/><br/>" + ex.ToExceptionStackTrace(), LogType.OnemsizHata);
                }
            }
            else
            {
                success = false;
                message = "Silmek istediğiniz Şehir sistemde bulunamadı!";
            }
            return Json(new { success, message }, "application/json", JsonRequestBehavior.AllowGet);
        }

    }
}
