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
    [Authorize(Roles = RoleNames.Unvanlar)]
    public class UnvanlarController : Controller
    {
        private readonly LubsDbEntities _entities = new LubsDbEntities();
        [Authorize(Roles = RoleNames.Unvanlar)]
        public ActionResult Index()
        {
            return Index(new FmUnvanlar());
        }
        [HttpPost]
        public ActionResult Index(FmUnvanlar model)
        {

            var q = from s in _entities.Unvanlars
                    select s;

            if (model.UnvanSiraNo.HasValue) q = q.Where(p => p.UnvanSiraNo == model.UnvanSiraNo);
            if (!model.UnvanAdi.IsNullOrWhiteSpace()) q = q.Where(p => p.UnvanAdi.Contains(model.UnvanAdi));
            if (model.YetkiGrupID.HasValue) q = q.Where(p => p.YetkiGrupID == model.YetkiGrupID.Value);
            if (model.IsAktif.HasValue) q = q.Where(p => p.IsAktif == model.IsAktif.Value);
            model.RowCount = q.Count();
            q = !model.Sort.IsNullOrWhiteSpace() ? q.OrderBy(model.Sort) : q.OrderBy(o => o.UnvanAdi);
            model.data = q.Skip(model.StartRowIndex).Take(model.PageSize).Select(s => new FrUnvanlar
            {
                UnvanID = s.UnvanID,
                UnvanAdi = s.UnvanAdi,
                UnvanSiraNo = s.UnvanSiraNo,
                YetkiGrupID = s.YetkiGrupID,
                YetkiGrupAdi = s.YetkiGrupID.HasValue ? s.YetkiGruplari.YetkiGrupAdi : "",
                UnvanUserCount = s.YetkiGrupID.HasValue ? s.Kullanicilars.Count(p => p.KullaniciTipleri.KurumIci) : 0,
                IsAktif = s.IsAktif,
                IslemTarihi = s.IslemTarihi,
                IslemYapanID = s.IslemYapanID,
                IslemYapanIP = s.IslemYapanIP,

            }).ToArray();
            var indexModel = new MIndexBilgi
            {
                Toplam = model.RowCount,
                Aktif = q.Count(p => p.IsAktif),
                Pasif = q.Count(p => !p.IsAktif)
            };
            ViewBag.IndexModel = indexModel;
            ViewBag.IsAktif = new SelectList(ComboData.GetCmbAktifPasifData(true), "Value", "Caption", model.IsAktif);
            ViewBag.YetkiGrupID = new SelectList(YetkiGrupBus.CmbYetkiGruplari(true), "Value", "Caption", model.YetkiGrupID);
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

            ViewBag.YetkiGrupID = new SelectList(YetkiGrupBus.CmbYetkiGruplari(true), "Value", "Caption", model.YetkiGrupID);
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
                    if (UserIdentity.Current.IsAdmin) data.YetkiGrupID = kModel.YetkiGrupID;
                    data.IsAktif = kModel.IsAktif;
                    data.IslemYapanID = UserIdentity.Current.Id;
                    data.IslemYapanIP = UserIdentity.Ip;
                    data.IslemTarihi = DateTime.Now;
                }
                _entities.SaveChanges();
                return RedirectToAction("Index");
            }

            MessageBox.Show("Uyarı", MessageBox.MessageType.Warning, mmMessage.Messages.ToArray());

            ViewBag.YetkiGrupID = new SelectList(YetkiGrupBus.CmbYetkiGruplari(true), "Value", "Caption", kModel.YetkiGrupID);
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
                    SistemBilgilendirmeBus.SistemBilgisiKaydet(message, ex.ToExceptionStackTrace(), BilgiTipiEnum.OnemsizHata);
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

        public ActionResult UpdateUnvanYetkiGrubu(int unvanId, int yetkiGrupId)
        {

            if (!UserIdentity.Current.IsAdmin) return new { IsSuccess = false, message = "Bu işlemi yapmak için yetkili değilsiniz" }.ToJsonResult();

            var unvan = _entities.Unvanlars.First(f => f.UnvanID == unvanId);
            var yetkiGrubu = _entities.YetkiGruplaris.First(f => f.YetkiGrupID == yetkiGrupId);

            var users = _entities.Kullanicilars.Where(p =>
                p.UnvanID == unvanId && p.KullaniciTipleri.KurumIci).ToList();

            foreach (var user in users)
            {
                user.YetkiGrupID = yetkiGrupId;
                user.IslemYapanID = UserIdentity.Current.Id;
                user.IslemYapanIP = UserIdentity.Ip;
                user.IslemTarihi = DateTime.Now;
            }
            _entities.SaveChanges();
            var message = unvan.UnvanAdi + " ünvanındaki " + users.Count + " kullanıcının yetki grubu '" + yetkiGrubu.YetkiGrupAdi + "' olarak güncellendi";
            if (users.Count == 0) message = "yetki grubu güncellenecek kullanıcı yok";
            return new { IsSuccess = true, message }.ToJsonResult();
        }
    }
}
