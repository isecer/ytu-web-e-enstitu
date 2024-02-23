using System;
using System.Linq;
using System.Web.Mvc;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Business;
using Entities.Entities;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.MenuAndRoles;
using LisansUstuBasvuruSistemi.Utilities.SystemData;
using LisansUstuBasvuruSistemi.Utilities.Helpers;

namespace LisansUstuBasvuruSistemi.Controllers
{
    [System.Web.Mvc.OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    [Authorize(Roles = RoleNames.Universiteler)]
    public class UniversitelerController : Controller
    {
        private readonly LubsDbEntities _entities = new LubsDbEntities();
        public ActionResult Index()
        {
            return Index(new FmUniversiteler { });
        }
        [HttpPost]
        public ActionResult Index(FmUniversiteler model)
        {

            var q = from s in _entities.Universitelers
                    select s;

            if (model.UniversiteID.HasValue) q = q.Where(p => p.UniversiteID == model.UniversiteID);
            if (!model.KisaAd.IsNullOrWhiteSpace()) q = q.Where(p => p.KisaAd.Contains(model.KisaAd));
            if (!model.Ad.IsNullOrWhiteSpace()) q = q.Where(p => p.Ad.Contains(model.Ad));
            model.RowCount = q.Count();
            q = !model.Sort.IsNullOrWhiteSpace() ? q.OrderBy(model.Sort) : q.OrderBy(o => o.Ad); 
            model.data = q.Skip(model.StartRowIndex).Take(model.PageSize).ToArray();
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
            var model = new Universiteler();
            if (id > 0)
            {
                var data = _entities.Universitelers.FirstOrDefault(p => p.UniversiteID == id);
                if (data != null) model = data;
            }
            ViewBag.OldID = model.UniversiteID;
            return View(model);
        }
        [HttpPost]
        public ActionResult Kayit(Universiteler kModel, int oldId, string dlgid = "")
        {
            var mmMessage = new MmMessage
            {
                IsDialog = !dlgid.IsNullOrWhiteSpace(),
                DialogID = dlgid
            };

            #region Kontrol
            if (oldId <= 0)
            {
                if (kModel.UniversiteID <= 0)
                { 
                    mmMessage.Messages.Add("Üniversite kodu giriniz.");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "UniversiteID" });
                }
                else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "UniversiteID" });
            }
            if (kModel.KisaAd.IsNullOrWhiteSpace())
            { 
                mmMessage.Messages.Add("Üniversite kısa adı Boş bırakılamaz.");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "KisaAd" });
            }
            else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "KisaAd" });
            if (kModel.Ad.IsNullOrWhiteSpace())
            { 
                mmMessage.Messages.Add("Üniversite adı Boş bırakılamaz.");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "Ad" });
            }
            else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "Ad" });
            #endregion
            if (mmMessage.Messages.Count == 0)
            {
                kModel.IslemTarihi = DateTime.Now;
                kModel.IslemYapanID = UserIdentity.Current.Id;
                kModel.IslemYapanIP = UserIdentity.Ip;
                if (oldId <= 0)
                {
                    kModel.IsAktif = true;
                    _entities.Universitelers.Add(kModel);
                }
                else
                {
                    var data = _entities.Universitelers.First(p => p.UniversiteID == oldId);
                    data.Ad = kModel.Ad;
                    data.KisaAd = kModel.KisaAd;
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
            var kayit = _entities.Universitelers.FirstOrDefault(p => p.UniversiteID == id);
            string message = "";
            bool success = true;
            if (kayit != null)
            {

                try
                {
                    message = "'" + kayit.Ad + "' İsimli Üniversite Silindi!";
                    _entities.Universitelers.Remove(kayit);
                    _entities.SaveChanges();
                }
                catch (Exception ex)
                {
                    success = false;
                    message = "'" + kayit.Ad + "' İsimli Üniversite Silinemedi! <br/> Bilgi:" + ex.ToExceptionMessage(); 
                    SistemBilgilendirmeBus.SistemBilgisiKaydet(message,  ex.ToExceptionStackTrace(), BilgiTipiEnum.OnemsizHata);
                }
            }
            else
            {
                success = false;
                message = "Silmek istediğiniz Üniversite sistemde bulunamadı!";
            }
            return Json(new { success, message }, "application/json", JsonRequestBehavior.AllowGet);
        }
    }
}
