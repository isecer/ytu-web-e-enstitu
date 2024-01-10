using System;
using System.Linq;
using System.Web.Mvc;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Business;
using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.Helpers;
using LisansUstuBasvuruSistemi.Utilities.MenuAndRoles;
using LisansUstuBasvuruSistemi.Utilities.SystemData;

namespace LisansUstuBasvuruSistemi.Controllers
{
    [System.Web.Mvc.OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    [Authorize(Roles = RoleNames.BelgeTipleri)]
    public class BelgeTipleriController : Controller
    {
        private readonly LisansustuBasvuruSistemiEntities _entities = new LisansustuBasvuruSistemiEntities();
        public ActionResult Index()
        {
            return Index(new FmBelgeTipleriDto { });
        }
        [HttpPost]
        public ActionResult Index(FmBelgeTipleriDto model)
        {

            var q = from s in _entities.BelgeTipleris
                    select new FrBelgeTipleriDto
                    {
                        BelgeTipID = s.BelgeTipID,
                        BelgeTipAdi = s.BelgeTipAdi,
                        IsAktif = s.IsAktif,
                        IslemTarihi = s.IslemTarihi,
                        IslemYapanID = s.IslemYapanID,
                        IslemYapan = s.Kullanicilar.Ad + " " + s.Kullanicilar.Soyad,
                        IslemYapanIP = s.IslemYapanIP

                    };

            if (model.IsAktif.HasValue) q = q.Where(p => p.IsAktif == model.IsAktif);
            if (!model.BelgeTipAdi.IsNullOrWhiteSpace()) q = q.Where(p => p.BelgeTipAdi.Contains(model.BelgeTipAdi));
            model.RowCount = q.Count();
            q = !model.Sort.IsNullOrWhiteSpace() ? q.OrderBy(model.Sort) : q.OrderBy(o => o.BelgeTipAdi); 
            model.BelgeTipleriDtos = q.Skip(model.StartRowIndex).Take(model.PageSize).ToArray();
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
        public ActionResult Kayit(int? id )
        {
            var mmMessage = new MmMessage(); 
            ViewBag.MmMessage = mmMessage;
            var model = new BelgeTipleri();
            if (id.HasValue)
            {
                var data = _entities.BelgeTipleris.FirstOrDefault(p => p.BelgeTipID == id);
                if (data != null)
                {
                    model = data; 
                }
            } 
            return View(model);
        }
        [HttpPost]
        [Authorize(Roles = RoleNames.BelgeTipleriKayıt)]
        public ActionResult Kayit(BelgeTipleri kModel)
        {
            var mmMessage = new MmMessage(); 
            #region Kontrol
            if (kModel.BelgeTipAdi.IsNullOrWhiteSpace())
            {
                mmMessage.Messages.Add("Belge Tip Adını Giriniz.");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Error, PropertyName = "BelgeTipAdi" });
            }
            #endregion
            if (mmMessage.Messages.Count == 0)
            {
                if (kModel.BelgeTipID <= 0)
                {
                   kModel.IsAktif = true;
                    kModel.IslemTarihi = DateTime.Now;
                    kModel.IslemYapanID = UserIdentity.Current.Id;
                    kModel.IslemYapanIP = UserIdentity.Ip;
                   _entities.BelgeTipleris.Add(kModel);
                }
                else
                {
                    var data = _entities.BelgeTipleris.First(p => p.BelgeTipID == kModel.BelgeTipID);
                    data.BelgeTipAdi = kModel.BelgeTipAdi;
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
        [Authorize(Roles = RoleNames.BelgeTipleriSil)]
        public ActionResult Sil(int? id)
        {
            var data = _entities.BelgeTipleris.FirstOrDefault(p => p.BelgeTipID == id); 
            string message = "";
            bool success = true;
            if (data != null)
            {

                try
                {
                    message = "'" + data.BelgeTipAdi + "' İsimli belge tipi Silindi!";
                    _entities.BelgeTipleris.Remove(data);
                    _entities.SaveChanges();
                }
                catch (Exception ex)
                {
                    success = false;
                    message = "'" + data.BelgeTipAdi + "' İsimli belge tipi Silinemedi! <br/> Bilgi:" + ex.ToExceptionMessage();
                    SistemBilgilendirmeBus.SistemBilgisiKaydet(message, ex.ToExceptionStackTrace(), BilgiTipiEnum.OnemsizHata);
                }
            }
            else
            {
                success = false;
                message = "Silmek istediğiniz Belge tipi sistemde bulunamadı!";
            }
            return Json(new { success, message }, "application/json", JsonRequestBehavior.AllowGet);
        }
    }
}