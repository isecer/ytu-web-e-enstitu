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

namespace LisansUstuBasvuruSistemi.Controllers
{
    [Authorize(Roles = RoleNames.MesajlarKategorileri)]
    [System.Web.Mvc.OutputCache(NoStore = false, Duration = 0, VaryByParam = "*")]
    public class MesajKategorileriController : Controller
    {
        private LisansustuBasvuruSistemiEntities db = new LisansustuBasvuruSistemiEntities();
        public ActionResult Index()
        {
            return Index(new fmMesajKategorileri { });
        }
        [HttpPost]
        public ActionResult Index(fmMesajKategorileri model)
        {
            var EnstKods = UserIdentity.Current.EnstituKods ?? new List<string>();
            var q = from s in db.MesajKategorileris
                    join se in db.MesajKategorileris on new { s.MesajKategoriID } equals new { se.MesajKategoriID }
                    join e in db.Enstitulers on new { s.EnstituKod } equals new { e.EnstituKod }
                    where EnstKods.Contains(s.EnstituKod)
                    select new frMesajKategorileri
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
            var PS = Management.setStartRowInx(model.StartRowIndex, model.PageIndex, model.PageCount, model.RowCount, model.PageSize);
            model.PageIndex = PS.PageIndex;
            model.Data = q.Skip(PS.StartRowIndex).Take(model.PageSize).ToArray();
            var IndexModel = new MIndexBilgi();
            IndexModel.Toplam = model.RowCount;
            IndexModel.Aktif = q.Where(p => p.IsAktif).Count();
            IndexModel.Pasif = q.Where(p => !p.IsAktif).Count();
            ViewBag.IndexModel = IndexModel;
            ViewBag.EnstituKod = new SelectList(EnstituBus.GetCmbYetkiliEnstituler(true), "Value", "Caption", model.EnstituKod);
            ViewBag.IsAktif = new SelectList(Management.cmbAktifPasifData(true), "Value", "Caption", model.IsAktif);
            return View(model);
        }
        public ActionResult Kayit(int? id, string dlgid)
        {
            var MmMessage = new MmMessage();
            MmMessage.IsDialog = !dlgid.IsNullOrWhiteSpace();
            MmMessage.DialogID = dlgid;
            ViewBag.MmMessage = MmMessage;
            var model = new MesajKategorileri();
            if (id.HasValue)
            {
                var data = db.MesajKategorileris.Where(p => p.MesajKategoriID == id).FirstOrDefault();
                if (data != null)
                {
                    model = data;
                }
            }
            ViewBag.EnstituKod = new SelectList(EnstituBus.GetCmbYetkiliEnstituler(true), "Value", "Caption", model.EnstituKod);
            ViewBag.Diller = new SelectList(Management.GetDiller(true), "Value", "Caption");
            ViewBag.IsAktif = new SelectList(Management.cmbAktifPasifData(true), "Value", "Caption", model.IsAktif);
            return View(model);
        }
        [HttpPost]
        public ActionResult Kayit(MesajKategorileri kModel)
        {
            var MmMessage = new MmMessage();
            #region Kontrol
            if (kModel.KategoriAdi.IsNullOrWhiteSpace())
            {
                MmMessage.Messages.Add("Kategori Adı Giriniz.");
                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "KategoriAdi" });
            }
            else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "KategoriAdi" });
            if (kModel.EnstituKod.IsNullOrWhiteSpace())
            {
                MmMessage.Messages.Add("Enstitü Seçiniz.");
                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "EnstituKod" });
            }
            else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "EnstituKod" });



            #endregion
            if (MmMessage.Messages.Count == 0)
            {

                if (kModel.MesajKategoriID <= 0)
                {
                    kModel.IsAktif = true;
                    kModel.IslemYapanID = UserIdentity.Current.Id;
                    kModel.IslemYapanIP = UserIdentity.Ip;
                    kModel.IslemTarihi = DateTime.Now;
                    db.MesajKategorileris.Add(kModel);
                }
                else
                {
                    var data = db.MesajKategorileris.Where(p => p.MesajKategoriID == kModel.MesajKategoriID).First();
                    data.KategoriAdi = kModel.KategoriAdi;
                    data.KategoriAciklamasi = kModel.KategoriAciklamasi;
                    data.EnstituKod = kModel.EnstituKod;
                    data.IsAktif = kModel.IsAktif;
                    data.IslemYapanID = UserIdentity.Current.Id;
                    data.IslemYapanIP = UserIdentity.Ip;
                    data.IslemTarihi = DateTime.Now;


                }
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            else
            {
                MessageBox.Show("Uyarı", MessageBox.MessageType.Warning, MmMessage.Messages.ToArray());
            }

            ViewBag.MmMessage = MmMessage;
            ViewBag.EnstituKod = new SelectList(EnstituBus.GetCmbYetkiliEnstituler(true), "Value", "Caption", kModel.EnstituKod);
            ViewBag.IsAktif = new SelectList(Management.cmbAktifPasifData(true), "Value", "Caption", kModel.IsAktif);
            return View(kModel);
        }
        public ActionResult Sil(int id)
        {
            var kayit = db.MesajKategorileris.Where(p => p.MesajKategoriID == id).FirstOrDefault(); 
            string message = "";
            bool success = true;
            if (kayit != null)
            {

                try
                {
                    message = "'" + kayit.KategoriAdi + "' İsimli Kategori Silindi!";
                    db.MesajKategorileris.Remove(kayit);
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    success = false;
                    message = "'" + kayit.KategoriAdi + "' İsimli Kategori Silinemedi! <br/> Bilgi:" + ex.ToExceptionMessage();
                    Management.SistemBilgisiKaydet(message, "MesajKategorileri/Sil<br/><br/>" + ex.ToExceptionStackTrace(), LogType.OnemsizHata);
                }
            }
            else
            {
                success = false;
                message = "Silmek istediğiniz Kategori sistemde bulunamadı!";
            }
            return Json(new { success = success, message = message }, "application/json", JsonRequestBehavior.AllowGet);
        }

        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }
    }
}