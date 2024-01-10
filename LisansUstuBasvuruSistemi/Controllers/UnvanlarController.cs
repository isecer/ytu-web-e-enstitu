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
    [System.Web.Mvc.OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    [Authorize(Roles = RoleNames.Unvanlar)]
    public class UnvanlarController : Controller
    {
        private readonly LisansustuBasvuruSistemiEntities _entities = new LisansustuBasvuruSistemiEntities();
        [Authorize(Roles = RoleNames.Unvanlar)]
        public ActionResult Index()
        {
            return Index(new FmUnvanlar { });
        }
        [HttpPost]
        public ActionResult Index(FmUnvanlar model)
        {

            var q = from s in _entities.Unvanlars
                    select s;

            if (model.UnvanSiraNo.HasValue) q = q.Where(p => p.UnvanSiraNo == model.UnvanSiraNo);
            if (!model.UnvanAdi.IsNullOrWhiteSpace()) q = q.Where(p => p.UnvanAdi.Contains(model.UnvanAdi));
            if (model.IsAktif.HasValue) q = q.Where(p => p.IsAktif == model.IsAktif.Value);
            model.RowCount = q.Count();
            q = !model.Sort.IsNullOrWhiteSpace() ? q.OrderBy(model.Sort) : q.OrderBy(o => o.UnvanAdi); 
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
            var model = new Unvanlar();
            if (id > 0)
            {
                var data = _entities.Unvanlars.FirstOrDefault(p => p.UnvanID == id);
                if (data != null) model = data;
            }

            return View(model);
        }
        [HttpPost]
        public ActionResult Kayit(Unvanlar kModel, string dlgid = "")
        {
            var mmMessage = new MmMessage
            {
                IsDialog = !dlgid.IsNullOrWhiteSpace(),
                DialogID = dlgid
            };

            #region Kontrol

            //if (kModel.UnvanSiraNo <= 0)
            //{
            //    string msg = "Sıra Numarası 0 dan büyük olmalıdır.";
            //    MmMessage.Messages.Add(msg);
            //    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "UnvanSiraNo" });
            //}
            //else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "UnvanSiraNo" });
            if (kModel.UnvanAdi.IsNullOrWhiteSpace())
            { 
                mmMessage.Messages.Add("Ünvan Adı Boş bırakılamaz.");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "UnvanAdi" });
            }
            else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "UnvanAdi" });
            #endregion

            if (mmMessage.Messages.Count == 0)
            {

                if (kModel.UnvanID <= 0)
                {
                    kModel.IsAktif = true;
                    kModel.IslemYapanID = UserIdentity.Current.Id;
                    kModel.IslemYapanIP = UserIdentity.Ip;
                    kModel.IslemTarihi = DateTime.Now;

                    _entities.Unvanlars.Add(kModel);
                }
                else
                {
                    var data = _entities.Unvanlars.First(p => p.UnvanID == kModel.UnvanID);
                    //data.UnvanSiraNo = kModel.UnvanSiraNo;
                    data.UnvanAdi = kModel.UnvanAdi;
                    data.IsAktif = kModel.IsAktif;
                    data.IslemYapanID = UserIdentity.Current.Id;
                    data.IslemYapanIP = UserIdentity.Ip;
                    data.IslemTarihi = DateTime.Now;
                }
                _entities.SaveChanges();
                return RedirectToAction("Index");
            }
            else
            {
                MessageBox.Show("Uyarı", MessageBox.MessageType.Warning, mmMessage.Messages.ToArray());
            }

            ViewBag.MmMessage = mmMessage;
            return View(kModel);
        }
        public ActionResult Sil(int id)
        {
            var kayit = _entities.Unvanlars.FirstOrDefault(p => p.UnvanID == id);
            string message = "";
            bool success = true;
            if (kayit != null)
            {

                try
                {
                    message = "'" + kayit.UnvanAdi + "' İsimli Ünvan Silindi!";
                    _entities.Unvanlars.Remove(kayit);
                    _entities.SaveChanges();
                }
                catch (Exception ex)
                {
                    success = false;
                    message = "'" + kayit.UnvanAdi + "' İsimli Ünvan Silinemedi! <br/> Bilgi:" + ex.ToExceptionMessage();
                    SistemBilgilendirmeBus.SistemBilgisiKaydet(message,  ex.ToExceptionStackTrace(), BilgiTipiEnum.OnemsizHata);
                }
            }
            else
            {
                success = false;
                message = "Silmek istediğiniz Ünvan sistemde bulunamadı!";
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
