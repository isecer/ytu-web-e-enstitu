using BiskaUtil;
using LisansUstuBasvuruSistemi.Business;
using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.Helpers;
using LisansUstuBasvuruSistemi.Utilities.MenuAndRoles;
using LisansUstuBasvuruSistemi.Utilities.SystemData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace LisansUstuBasvuruSistemi.Controllers
{
    [System.Web.Mvc.OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    [Authorize(Roles = RoleNames.AnabilimDallari)]
    public class AnabilimDallariController : Controller
    {
        private readonly LisansustuBasvuruSistemiEntities _entities = new LisansustuBasvuruSistemiEntities();
        public ActionResult Index(string ekd)
        {
            return Index(new FmAnabilimDallariDto { },ekd);
        }
        [HttpPost]
        public ActionResult Index(FmAnabilimDallariDto model, string ekd)
        {
            model.EnstituKod = EnstituBus.GetSelectedEnstitu(ekd);
            var enstKods = UserIdentity.Current.EnstituKods ?? new List<string>();
            var q = from s in _entities.AnabilimDallaris
                    join e in _entities.Enstitulers on s.EnstituKod equals e.EnstituKod
                    where enstKods.Contains(s.EnstituKod)
                    select new FrAnabilimDallariDto
                    {
                        AnabilimDaliID = s.AnabilimDaliID,
                        EnstituKod = s.EnstituKod,
                        EnstituAd = e.EnstituAd,
                        AnabilimDaliKod = s.AnabilimDaliKod,
                        AnabilimDaliAdi = s.AnabilimDaliAdi,
                        IsEmailVar = s.EMail != null && s.EMail != "",
                        EMail = s.EMail,
                        KomiteIds = s.AnabilimDaliYeterlikKomiteUyeleris.Select(sk => sk.KullaniciID).ToList(),
                        YeterlikKomiteUyeCount = s.AnabilimDaliYeterlikKomiteUyeleris.Count,
                        IsAktif = s.IsAktif,
                        IslemTarihi = s.IslemTarihi,
                        IslemYapanID = s.IslemYapanID,
                        IslemYapanIP = s.IslemYapanIP,
                        IslemYapan = s.Kullanicilar.Ad + " " + s.Kullanicilar.Soyad,
                    };

            if (!model.EnstituKod.IsNullOrWhiteSpace()) q = q.Where(p => p.EnstituKod == model.EnstituKod);
            if (!model.AnabilimDaliKod.IsNullOrWhiteSpace()) q = q.Where(p => p.AnabilimDaliKod == model.AnabilimDaliKod);
            if (!model.AnabilimDaliAdi.IsNullOrWhiteSpace()) q = q.Where(p => p.AnabilimDaliAdi.Contains(model.AnabilimDaliAdi));
            if (model.IsKomiteUyesiVar.HasValue) q = q.Where(p => p.YeterlikKomiteUyeCount > 0 == model.IsKomiteUyesiVar.Value);
            if (model.IsAktif.HasValue) q = q.Where(p => p.IsAktif == model.IsAktif);
            if (model.IsEmailVar.HasValue) q = q.Where(p => p.IsEmailVar == model.IsEmailVar.Value);
            ViewBag.filteredKomiteIds = q.SelectMany(s => s.KomiteIds).Distinct().ToList();
            ViewBag.filteredAbdIds = q.Where(p=>p.IsEmailVar).Select(s => s.EMail).Distinct().ToList();
            model.RowCount = q.Count();
            q = !model.Sort.IsNullOrWhiteSpace() ? q.OrderBy(model.Sort) : q.OrderBy(o => o.AnabilimDaliAdi);
            model.FrAnabilimDallaris = q.Skip(model.StartRowIndex).Take(model.PageSize).ToArray();
            var indexModel = new MIndexBilgi
            {
                Toplam = model.RowCount,
                Aktif = q.Count(p => p.IsAktif),
                Pasif = q.Count(p => !p.IsAktif)
            };
            ViewBag.IndexModel = indexModel;
            ViewBag.EnstituKod = new SelectList(EnstituBus.GetCmbYetkiliEnstituler(true), "Value", "Caption", model.EnstituKod);
            ViewBag.IsKomiteUyesiVar = new SelectList(ComboData.GetCmbKomiteUyeKayitDurumData(true), "Value", "Caption", model.IsKomiteUyesiVar);
            ViewBag.IsEmailVar = new SelectList(ComboData.GetCmbVarYokData(true), "Value", "Caption", model.IsEmailVar);
            ViewBag.IsAktif = new SelectList(ComboData.GetCmbAktifPasifData(true), "Value", "Caption", model.IsAktif);
            return View(model);
        }
        public ActionResult Kayit(int? id)
        {
            var mmMessage = new MmMessage();
            ViewBag.MmMessage = mmMessage;
            var model = new AnabilimDallari();
            if (id > 0)
            {
                var data = _entities.AnabilimDallaris.FirstOrDefault(p => p.AnabilimDaliID == id);
                if (data != null)
                {
                    model = data;

                }
            }
            ViewBag.EnstituKod = new SelectList(EnstituBus.GetCmbYetkiliEnstituler(true), "Value", "Caption", model.EnstituKod);
            ViewBag.IsAktif = new SelectList(ComboData.GetCmbAktifPasifData(true), "Value", "Caption", model.IsAktif);
            return View(model);
        }
        [HttpPost]
        public ActionResult Kayit(AnabilimDallari kModel)
        {
            var mmMessage = new MmMessage();
            #region Kontrol

            if (kModel.EnstituKod.IsNullOrWhiteSpace())
            {
                mmMessage.Messages.Add("Enstitü seçiniz");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "EnstituKod" });
            }
            else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "EnstituKod" });
            if (kModel.AnabilimDaliKod.IsNullOrWhiteSpace())
            {
                mmMessage.Messages.Add("Kod Giriniz.");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "AnabilimDaliKod" });
            }
            else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "AnabilimDaliKod" });
            if (kModel.AnabilimDaliAdi.IsNullOrWhiteSpace())
            {
                mmMessage.Messages.Add("Ad Giriniz.");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "AnabilimDaliAdi" });
            }
            else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "AnabilimDaliAdi" });
            if (kModel.EMail.IsNullOrWhiteSpace())
            {
                mmMessage.Messages.Add("EMail Bilgisini Giriniz.");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "EMail" });
            }
            else
            {
                if (kModel.EMail.ToIsValidEmail())
                {
                    mmMessage.Messages.Add("EMail Formatı uygun değildir.");
                    mmMessage.MessagesDialog.Add(
                        new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "EMail" });
                }
                else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "EMail" });
            }
            if (mmMessage.Messages.Count == 0)
            {
                var cnt = _entities.AnabilimDallaris.Count(p => p.AnabilimDaliKod == kModel.AnabilimDaliKod && p.EnstituKod == kModel.EnstituKod && p.AnabilimDaliID != kModel.AnabilimDaliID);
                if (cnt > 0)
                {
                    mmMessage.Messages.Add("Tanımlamak istediğiniz Anabilim Dalı kodu daha önceden sisteme tanımlanmıştır.");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "AnabilimDaliKod" });
                }

            }

            #endregion
            if (mmMessage.Messages.Count == 0)
            {
                kModel.IslemYapanID = UserIdentity.Current.Id;
                kModel.IslemYapanIP = UserIdentity.Ip;
                kModel.IslemTarihi = DateTime.Now;
                if (kModel.AnabilimDaliID <= 0)
                {
                    kModel.IsAktif = true;
                    var enst = _entities.AnabilimDallaris.Add(kModel);
                }
                else
                {
                    var data = _entities.AnabilimDallaris.First(p => p.AnabilimDaliID == kModel.AnabilimDaliID);
                    data.EnstituKod = kModel.EnstituKod;
                    data.AnabilimDaliKod = kModel.AnabilimDaliKod;
                    data.AnabilimDaliAdi = kModel.AnabilimDaliAdi;
                    data.EMail = kModel.EMail;
                    data.IsAktif = kModel.IsAktif;
                    data.IslemYapanID = UserIdentity.Current.Id;
                    data.IslemYapanIP = UserIdentity.Ip;
                    data.IslemTarihi = DateTime.Now; ;

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

        public ActionResult YeterlikKomiteUyeleri(int id)
        {
            var surec = _entities.AnabilimDallaris.First(p => p.AnabilimDaliID == id);
            return View(surec);
        }
        public ActionResult YeterlikKomiteUyesiEkle(int anabilimDaliId, int? kullaniciId)
        {
            var success = false;
            var message = "";
            if (!kullaniciId.HasValue)
            {
                message = "Komite üyesi seçiniz.";
            }
            else if (_entities.AnabilimDaliYeterlikKomiteUyeleris.Any(p => p.AnabilimDaliID == anabilimDaliId && p.KullaniciID == kullaniciId.Value))
            {
                message = "Bu komisyon üyesi daha önce eklendi.";
            }
            else if (_entities.AnabilimDaliYeterlikKomiteUyeleris.Count(p => p.AnabilimDaliID == anabilimDaliId) >= 5)
            {
                message = "5 komison üyesinden daha fazla üye eklenemez.";
            }
            else
            {
                _entities.AnabilimDaliYeterlikKomiteUyeleris.Add(new AnabilimDaliYeterlikKomiteUyeleri
                {
                    AnabilimDaliID = anabilimDaliId,
                    KullaniciID = kullaniciId.Value,
                    IslemTarihi = DateTime.Now,
                    IslemYapanID = UserIdentity.Current.Id
                });
                _entities.SaveChanges();
                success = true;
            }
            return new { success, message }.ToJsonResult();

        }
        public ActionResult YeterlikKomiteUyesiSil(int anabilimDaliId, int kullaniciId)
        {

            if (_entities.AnabilimDaliYeterlikKomiteUyeleris.Any(p => p.AnabilimDaliID == anabilimDaliId && p.KullaniciID == kullaniciId))
            {
                var uye = _entities.AnabilimDaliYeterlikKomiteUyeleris.First(p =>
                    p.AnabilimDaliID == anabilimDaliId && p.KullaniciID == kullaniciId);
                _entities.AnabilimDaliYeterlikKomiteUyeleris.Remove(uye);
                _entities.SaveChanges();
            }

            return true.ToJsonResult();
        }
        public ActionResult GetFilterKullanici(string term)
        {

            var ogrenciList = _entities.Kullanicilars.Where(p => p.KullaniciTipID == KullaniciTipiEnum.AkademikPersonel && p.UnvanID.HasValue && ((p.Ad + " " + p.Soyad).Contains(term) || p.TcKimlikNo.StartsWith(term))).Select(s => new
            {
                s.KullaniciID,
                s.Ad,
                s.Soyad,
                s.Unvanlar.UnvanAdi,
                s.ResimAdi
            }).Take(15).ToList()
                .Select(s => new
                {
                    id = s.KullaniciID,
                    s.UnvanAdi,
                    AdSoyad = s.Ad + " " + s.Soyad,
                    text = s.UnvanAdi + " " + s.Ad + " " + s.Soyad,
                    Images = s.ResimAdi.ToKullaniciResim()
                }).ToList();
            return ogrenciList.ToJsonResult();
        }
        public ActionResult Sil(int id)
        {
            var kayit = _entities.AnabilimDallaris.FirstOrDefault(p => p.AnabilimDaliID == id);
            string message = "";
            bool success = true;
            if (kayit != null)
            {

                try
                {
                    message = "'" + kayit.AnabilimDaliAdi + "' İsimli Anabilim Dalı Silindi!";
                    _entities.AnabilimDallaris.Remove(kayit);
                    _entities.SaveChanges();
                }
                catch (Exception ex)
                {
                    success = false;
                    message = "'" + kayit.AnabilimDaliAdi + "' İsimli Anabilim Dalı Silinemedi! <br/> Bilgi:" + ex.ToExceptionMessage();
                    SistemBilgilendirmeBus.SistemBilgisiKaydet(message, "AnabilimDallari/Sil<br/><br/>" + ex.ToExceptionStackTrace(), LogTipiEnum.OnemsizHata);
                }
            }
            else
            {
                success = false;
                message = "Silmek istediğiniz Anabilim Dalı sistemde bulunamadı!";
            }
            return Json(new { success, message }, "application/json", JsonRequestBehavior.AllowGet);
        }


    }
}
