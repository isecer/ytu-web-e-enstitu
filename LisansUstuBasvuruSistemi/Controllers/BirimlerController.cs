using System;
using System.Linq;
using System.Web.Mvc;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Business;
using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.MenuAndRoles;
using LisansUstuBasvuruSistemi.Utilities.SystemData;
using LisansUstuBasvuruSistemi.Utilities.Helpers;

namespace LisansUstuBasvuruSistemi.Controllers
{
    [OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    [Authorize(Roles = RoleNames.Birimler)]
    public class BirimlerController : Controller
    {
        private readonly LisansustuBasvuruSistemiEntities _entities = new LisansustuBasvuruSistemiEntities();
        [Authorize(Roles = RoleNames.Unvanlar)]
        public ActionResult Index()
        {
            return Index(new FmBirimlerDto());
        }
        [HttpPost]
        public ActionResult Index(FmBirimlerDto model)
        {

            var q = from s in _entities.Birimlers
                    select s;

            if (model.BirimKod.IsNullOrWhiteSpace() == false) q = q.Where(p => p.BirimKod == model.BirimKod);
            if (!model.BirimAdi.IsNullOrWhiteSpace()) q = q.Where(p => p.BirimAdi.Contains(model.BirimAdi));
            if (model.IsAktif.HasValue) q = q.Where(p => p.IsAktif == model.IsAktif.Value);
            model.RowCount = q.Count();
            q = !model.Sort.IsNullOrWhiteSpace() ? q.OrderBy(model.Sort) : q.OrderBy(o => o.BirimAdi);
            model.Birimlers = q.Skip(model.StartRowIndex).Take(model.PageSize).ToArray();
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
            var model = new Birimler();
            if (id > 0)
            {
                var data = _entities.Birimlers.FirstOrDefault(p => p.BirimID == id);
                if (data != null) model = data;
            }

            ViewBag.UstBirimID = new SelectList(BirimlerBus.CmbBirimler(true), "Value", "Caption", model.UstBirimID);
            return View(model);
        }
        [HttpPost]
        public ActionResult Kayit(Birimler kModel, string dlgid = "")
        {
            var mmMessage = new MmMessage
            {
                IsDialog = !dlgid.IsNullOrWhiteSpace(),
                DialogID = dlgid
            };

            #region Kontrol

            if (kModel.BirimAdi.IsNullOrWhiteSpace())
            {
                const string msg = "Birim Adı Boş bırakılamaz.";
                mmMessage.Messages.Add(msg);
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "BirimAdi" });
            }
            else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "BirimAdi" });
            #endregion

            if (mmMessage.Messages.Count == 0)
            {
                kModel.IslemTarihi = DateTime.Now;
                kModel.IslemYapanID = UserIdentity.Current.Id;
                kModel.IslemYapanIP = UserIdentity.Ip;
                if (kModel.BirimID <= 0)
                {
                    kModel.IsAktif = true;
                    _entities.Birimlers.Add(kModel);
                }
                else
                {
                    var data = _entities.Birimlers.First(p => p.BirimID == kModel.BirimID);
                    data.BirimKod = kModel.BirimKod;
                    data.BirimAdi = kModel.BirimAdi;
                    data.UstBirimID = kModel.UstBirimID;
                    data.IsAktif = kModel.IsAktif;
                    data.IslemTarihi = kModel.IslemTarihi;
                    data.IslemYapanID = kModel.IslemYapanID;
                    data.IslemYapanIP = kModel.IslemYapanIP;
                }
                _entities.SaveChanges();
                return RedirectToAction("Index");
            }
            else
            {
                MessageBox.Show("Uyarı", MessageBox.MessageType.Warning, mmMessage.Messages.ToArray());
            }
            ViewBag.UstBirimID = new SelectList(BirimlerBus.CmbBirimler(true), "Value", "Caption", kModel.UstBirimID);
            ViewBag.MmMessage = mmMessage;
            return View(kModel);
        }
        public ActionResult Sil(int id)
        {
            var kayit = _entities.Birimlers.FirstOrDefault(p => p.BirimID == id);
            string message;
            var success = true;
            if (kayit != null)
            {

                try
                {
                    message = "'" + kayit.BirimAdi + "' İsimli Birim Silindi!";
                    _entities.Birimlers.Remove(kayit);
                    _entities.SaveChanges();
                }
                catch (Exception ex)
                {
                    success = false;
                    message = "'" + kayit.BirimAdi + "' İsimli Birim Silinemedi! <br/> Bilgi:" + ex.ToExceptionMessage();
                    SistemBilgilendirmeBus.SistemBilgisiKaydet(message, ex.ToExceptionStackTrace(), BilgiTipiEnum.OnemsizHata);
                }
            }
            else
            {
                success = false;
                message = "Silmek istediğiniz Birim sistemde bulunamadı!";
            }
            return Json(new { success, message }, "application/json", JsonRequestBehavior.AllowGet);
        }
        protected override void Dispose(bool disposing)
        {
            _entities.Dispose();
            base.Dispose(disposing);
        }

    }
}
