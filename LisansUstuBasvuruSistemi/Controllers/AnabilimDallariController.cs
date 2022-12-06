using LisansUstuBasvuruSistemi.Models; using LisansUstuBasvuruSistemi.Models.FilterModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using BiskaUtil;

namespace LisansUstuBasvuruSistemi.Controllers
{
    [System.Web.Mvc.OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    [Authorize(Roles = RoleNames.AnabilimDallari)]
    public class AnabilimDallariController : Controller
    {
        private LisansustuBasvuruSistemiEntities db = new LisansustuBasvuruSistemiEntities();
        public ActionResult Index()
        {
            return Index(new fmAnabilimDallari { });
        }
        [HttpPost]
        public ActionResult Index(fmAnabilimDallari model)
        {
            var EnstKods = UserIdentity.Current.EnstituKods ?? new List<string>();
            var q = from s in db.AnabilimDallaris
                    join e in db.Enstitulers on s.EnstituKod equals e.EnstituKod
                    where EnstKods.Contains(s.EnstituKod)
                    select new frAnabilimDallari
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
            if (!model.Sort.IsNullOrWhiteSpace()) q = q.OrderBy(model.Sort);
            else q = q.OrderBy(o => o.AnabilimDaliAdi);
            var PS = Management.setStartRowInx(model.StartRowIndex, model.PageIndex, model.PageCount, model.RowCount, model.PageSize);
            model.PageIndex = PS.PageIndex;
            model.data = q.Skip(PS.StartRowIndex).Take(model.PageSize).ToArray();
            var IndexModel = new MIndexBilgi();
            IndexModel.Toplam = model.RowCount;
            IndexModel.Aktif = q.Where(p => p.IsAktif).Count();
            IndexModel.Pasif = q.Where(p => !p.IsAktif).Count();
            ViewBag.IndexModel = IndexModel;
            ViewBag.EnstituKod = new SelectList(Management.cmbGetYetkiliEnstituler(true), "Value", "Caption", model.EnstituKod);
            ViewBag.IsAktif = new SelectList(Management.cmbAktifPasifData(true), "Value", "Caption", model.IsAktif);
            return View(model);
        }
        public ActionResult Kayit(int? id)
        {
            var MmMessage = new MmMessage();
            ViewBag.MmMessage = MmMessage;
            var model = new AnabilimDallari();
            if (id > 0)
            {
                var data = db.AnabilimDallaris.Where(p => p.AnabilimDaliID == id).FirstOrDefault();
                if (data != null)
                {
                    model = data;

                }
            }
            ViewBag.EnstituKod = new SelectList(Management.cmbGetYetkiliEnstituler(true), "Value", "Caption", model.EnstituKod);
            ViewBag.IsAktif = new SelectList(Management.cmbAktifPasifData(true), "Value", "Caption", model.IsAktif);
            return View(model);
        }
        [HttpPost]
        public ActionResult Kayit(AnabilimDallari kModel)
        {
            var MmMessage = new MmMessage(); 
            #region Kontrol
            
            if (kModel.EnstituKod.IsNullOrWhiteSpace())
            {
                string msg = "Enstitü seçiniz";
                MmMessage.Messages.Add(msg);
                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "EnstituKod"});
            }
            else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "EnstituKod" });
            if (kModel.AnabilimDaliKod.IsNullOrWhiteSpace())
            {
                string msg = "Anabilim Dalı Kodunu Giriniz.";
                MmMessage.Messages.Add(msg);
                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "AnabilimDaliKod"});
            }
            else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "AnabilimDaliKod" });
            if (kModel.AnabilimDaliAdi.IsNullOrWhiteSpace())
            {
                string msg = "Anabilim Dalı Adını Giriniz.";
                MmMessage.Messages.Add(msg);
                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "AnabilimDaliAdi"});
            }
            else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "AnabilimDaliAdi" });

            if (MmMessage.Messages.Count == 0)
            {
                var cnt = db.AnabilimDallaris.Where(p => p.AnabilimDaliKod == kModel.AnabilimDaliKod && p.EnstituKod == kModel.EnstituKod && p.AnabilimDaliID != kModel.AnabilimDaliID).Count();
                if (cnt > 0)
                {
                    string msg = "Tanımlamak istediğiniz Anabilim Dalı kodu daha önceden sisteme tanımlanmıştır.";
                    MmMessage.Messages.Add(msg);
                    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "AnabilimDaliKod"});
                }

            }

            #endregion
            if (MmMessage.Messages.Count == 0)
            {
                kModel.IslemYapanID = UserIdentity.Current.Id;
                kModel.IslemYapanIP = UserIdentity.Ip;
                kModel.IslemTarihi = DateTime.Now;
                if (kModel.AnabilimDaliID <= 0)
                {
                    kModel.IsAktif = true;
                    var enst = db.AnabilimDallaris.Add(kModel);
                }
                else
                {
                    var data = db.AnabilimDallaris.Where(p => p.AnabilimDaliID == kModel.AnabilimDaliID).First();
                    data.EnstituKod = kModel.EnstituKod;
                    data.AnabilimDaliKod = kModel.AnabilimDaliKod;
                    data.AnabilimDaliAdi = kModel.AnabilimDaliAdi;
                    data.IsAktif = kModel.IsAktif;
                    data.IslemYapanID = UserIdentity.Current.Id;
                    data.IslemYapanIP = UserIdentity.Ip;
                    data.IslemTarihi = DateTime.Now; ;

                } 
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            else
            {
                MessageBox.Show("Uyarı", MessageBox.MessageType.Warning, MmMessage.Messages.ToArray());
            }
            
            ViewBag.MmMessage = MmMessage;
            ViewBag.EnstituKod = new SelectList(Management.cmbGetYetkiliEnstituler(true), "Value", "Caption", kModel.EnstituKod); 
            ViewBag.IsAktif = new SelectList(Management.cmbAktifPasifData(true), "Value", "Caption", kModel.IsAktif);
            return View(kModel);
        }
        public ActionResult Sil(int id)
        {
            var kayit = db.AnabilimDallaris.Where(p => p.AnabilimDaliID == id).FirstOrDefault(); 
            string message = "";
            bool success = true;
            if (kayit != null)
            {

                try
                {
                    message = "'" + kayit.AnabilimDaliAdi + "' İsimli Anabilim Dalı Silindi!";
                    db.AnabilimDallaris.Remove(kayit);
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    success = false;
                    message = "'" + kayit.AnabilimDaliAdi + "' İsimli Anabilim Dalı Silinemedi! <br/> Bilgi:" + ex.ToExceptionMessage();
                    Management.SistemBilgisiKaydet(message, "AnabilimDallari/Sil<br/><br/>" + ex.ToExceptionStackTrace(), BilgiTipi.OnemsizHata);
                }
            }
            else
            {
                success = false;
                message = "Silmek istediğiniz Anabilim Dalı sistemde bulunamadı!";
            }
            return Json(new { success = success, message = message }, "application/json", JsonRequestBehavior.AllowGet);
        }
    }
}
