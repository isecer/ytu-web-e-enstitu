using System;
using System.Collections.Generic;
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
    [Authorize(Roles = RoleNames.MesajlarKategorileri)]
    [System.Web.Mvc.OutputCache(NoStore = false, Duration = 0, VaryByParam = "*")]
    public class MesajKategorileriController : Controller
    {
        private readonly LisansustuBasvuruSistemiEntities _entities = new LisansustuBasvuruSistemiEntities();
        public ActionResult Index()
        {
            return Index(new FmMesajKategorileriDto { });
        }
        [HttpPost]
        public ActionResult Index(FmMesajKategorileriDto model)
        {
            var enstKods = UserIdentity.Current.EnstituKods ?? new List<string>();
            var q = from s in _entities.MesajKategorileris
                    join se in _entities.MesajKategorileris on new { s.MesajKategoriID } equals new { se.MesajKategoriID }
                    join e in _entities.Enstitulers on new { s.EnstituKod } equals new { e.EnstituKod }
                    where enstKods.Contains(s.EnstituKod)
                    select new FrMesajKategorileriDto
                    {
                        MesajKategoriID = s.MesajKategoriID,
                        EnstituKod = s.EnstituKod,
                        EnstituAd = e.EnstituAd,
                        KategoriAdi = se.KategoriAdi,
                        KategoriAciklamasi = se.KategoriAciklamasi,
                        IsAktif = s.IsAktif,
                        IslemTarihi = s.IslemTarihi,
                        IslemYapanID = s.IslemYapanID,
                        IslemYapanIP = s.IslemYapanIP,
                        IslemYapan = s.Kullanicilar.Ad + " " + s.Kullanicilar.Soyad
                    };

            if (!model.EnstituKod.IsNullOrWhiteSpace()) q = q.Where(p => p.EnstituKod == model.EnstituKod);
            if (!model.KategoriAdi.IsNullOrWhiteSpace()) q = q.Where(p => p.KategoriAdi.Contains(model.KategoriAdi));
            if (!model.KategoriAciklamasi.IsNullOrWhiteSpace()) q = q.Where(p => p.KategoriAciklamasi.Contains(model.KategoriAciklamasi));
            if (model.IsAktif.HasValue) q = q.Where(p => p.IsAktif == model.IsAktif);
            model.RowCount = q.Count();
            if (!model.Sort.IsNullOrWhiteSpace()) q = q.OrderBy(model.Sort); q = q.OrderBy(o => o.KategoriAdi); 
            model.MesajKategorileriDtos = q.Skip(model.StartRowIndex).Take(model.PageSize).ToArray();
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
        public ActionResult Kayit(int? id, string dlgid)
        {
            var mmMessage = new MmMessage
            {
                IsDialog = !dlgid.IsNullOrWhiteSpace(),
                DialogID = dlgid
            };
            ViewBag.MmMessage = mmMessage;
            var model = new MesajKategorileri();
            if (id.HasValue)
            {
                var data = _entities.MesajKategorileris.FirstOrDefault(p => p.MesajKategoriID == id);
                if (data != null)
                {
                    model = data;
                }
            }
            ViewBag.EnstituKod = new SelectList(EnstituBus.GetCmbYetkiliEnstituler(true), "Value", "Caption", model.EnstituKod);
            ViewBag.Diller = new SelectList(Management.GetDiller(true), "Value", "Caption");
            ViewBag.IsAktif = new SelectList(ComboData.GetCmbAktifPasifData(true), "Value", "Caption", model.IsAktif);
            return View(model);
        }
        [HttpPost]
        public ActionResult Kayit(MesajKategorileri kModel)
        {
            var mmMessage = new MmMessage();
            #region Kontrol
            if (kModel.KategoriAdi.IsNullOrWhiteSpace())
            {
                mmMessage.Messages.Add("Kategori Adı Giriniz.");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "KategoriAdi" });
            }
            else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "KategoriAdi" });
            if (kModel.EnstituKod.IsNullOrWhiteSpace())
            {
                mmMessage.Messages.Add("Enstitü Seçiniz.");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "EnstituKod" });
            }
            else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "EnstituKod" });



            #endregion
            if (mmMessage.Messages.Count == 0)
            {

                if (kModel.MesajKategoriID <= 0)
                {
                    kModel.IsAktif = true;
                    kModel.IslemYapanID = UserIdentity.Current.Id;
                    kModel.IslemYapanIP = UserIdentity.Ip;
                    kModel.IslemTarihi = DateTime.Now;
                    _entities.MesajKategorileris.Add(kModel);
                }
                else
                {
                    var data = _entities.MesajKategorileris.First(p => p.MesajKategoriID == kModel.MesajKategoriID);
                    data.KategoriAdi = kModel.KategoriAdi;
                    data.KategoriAciklamasi = kModel.KategoriAciklamasi;
                    data.EnstituKod = kModel.EnstituKod;
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
            ViewBag.EnstituKod = new SelectList(EnstituBus.GetCmbYetkiliEnstituler(true), "Value", "Caption", kModel.EnstituKod);
            ViewBag.IsAktif = new SelectList(ComboData.GetCmbAktifPasifData(true), "Value", "Caption", kModel.IsAktif);
            return View(kModel);
        }
        public ActionResult Sil(int id)
        {
            var kayit = _entities.MesajKategorileris.FirstOrDefault(p => p.MesajKategoriID == id); 
            string message = "";
            bool success = true;
            if (kayit != null)
            {

                try
                {
                    message = "'" + kayit.KategoriAdi + "' İsimli Kategori Silindi!";
                    _entities.MesajKategorileris.Remove(kayit);
                    _entities.SaveChanges();
                }
                catch (Exception ex)
                {
                    success = false;
                    message = "'" + kayit.KategoriAdi + "' İsimli Kategori Silinemedi! <br/> Bilgi:" + ex.ToExceptionMessage();
                    SistemBilgilendirmeBus.SistemBilgisiKaydet(message,  ex.ToExceptionStackTrace(), LogTipiEnum.OnemsizHata);
                }
            }
            else
            {
                success = false;
                message = "Silmek istediğiniz Kategori sistemde bulunamadı!";
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