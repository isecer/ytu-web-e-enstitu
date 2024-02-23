using BiskaUtil;
using Entities.Entities;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.MenuAndRoles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using LisansUstuBasvuruSistemi.Business;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.Helpers;

namespace LisansUstuBasvuruSistemi.Controllers
{
    [System.Web.Mvc.OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    [Authorize(Roles = RoleNames.Anketler)]
    public class AnketlerController : Controller
    {
        // GET: Anketler
        private readonly LubsDbEntities _entities = new LubsDbEntities();
        public ActionResult Index(string EKD)
        {
            var sEkod = EnstituBus.GetSelectedEnstitu(EKD);
            return Index(new FmAnketlerDto { PageSize = 15, EnstituKod = sEkod });
        }
        [HttpPost]
        public ActionResult Index(FmAnketlerDto model)
        {
            var EnstKods = UserIdentity.Current.EnstituKods ?? new List<string>();
            var q = from a in _entities.Ankets
                    join enst in _entities.Enstitulers on new { a.EnstituKod } equals new { enst.EnstituKod }
                    join k in _entities.Kullanicilars on a.IslemYapanID equals k.KullaniciID
                    where EnstKods.Contains(a.EnstituKod)
                    select new FrAnketlerDto
                    {
                        AnketID = a.AnketID,
                        EnstituKod = a.EnstituKod,
                        EnstituAdi = enst.EnstituAd,
                        AnketAdi = a.AnketAdi,
                        IsAktif = a.IsAktif,
                        IslemTarihi = a.IslemTarihi,
                        IslemYapanID = a.IslemYapanID,
                        IslemYapanIP = a.IslemYapanIP,
                        IslemYapan = k.Ad + " " + k.Soyad
                    };
            if (!model.AnketAdi.IsNullOrWhiteSpace()) q = q.Where(p => p.AnketAdi.Contains(model.AnketAdi));
            if (!model.EnstituKod.IsNullOrWhiteSpace()) q = q.Where(p => p.EnstituKod == model.EnstituKod);
            model.RowCount = q.Count();
            q = !model.Sort.IsNullOrWhiteSpace() ? q.OrderBy(model.Sort) : q.OrderBy(o => o.AnketAdi);
            model.FrAnketlers = q.Skip(model.StartRowIndex).Take(model.PageSize).ToArray();
            var indexModel = new MIndexBilgi
            {
                Toplam = model.RowCount
            };
            ViewBag.IndexModel = indexModel;

            ViewBag.EnstituKod = new SelectList(EnstituBus.GetCmbYetkiliEnstituler(true), "Value", "Caption", model.EnstituKod);
            return View(model);
        }
        public ActionResult Kayit(int? id, string dlgid)
        {

            var model = new Anket();
            if (id.HasValue)
            {
                var data = _entities.Ankets.Where(p => p.AnketID == id).FirstOrDefault();
                if (data != null)
                {
                    model = data;
                }
            }

            ViewBag.EnstituKod = new SelectList(EnstituBus.GetCmbYetkiliEnstituler(true), "Value", "Caption", model.EnstituKod);
            return View(model);
        }
        [HttpPost]
        public ActionResult Kayit(Anket kModel)
        {
            var MmMessage = new MmMessage();
            if (kModel.EnstituKod.IsNullOrWhiteSpace())
            {
                MmMessage.Messages.Add("Enstitü Seçiniz");
                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "EnstituKod" });
            }
            else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "EnstituKod" });
            if (kModel.AnketAdi.IsNullOrWhiteSpace())
            {
                MmMessage.Messages.Add("Anket Adı Giriniz.");
                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "AnketAdi" });
            }
            else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "AnketAdi" });
            if (MmMessage.Messages.Count == 0)
            {
                kModel.IslemTarihi = DateTime.Now;
                kModel.IslemYapanID = UserIdentity.Current.Id;
                kModel.IslemYapanIP = UserIdentity.Ip;
                if (kModel.AnketID <= 0)
                {
                    var anket = _entities.Ankets.Add(kModel);
                }
                else
                {
                    var anket = _entities.Ankets.Where(p => p.AnketID == kModel.AnketID).First();
                    anket.EnstituKod = kModel.EnstituKod;
                    anket.AnketAdi = kModel.AnketAdi;
                    anket.IslemYapanID = UserIdentity.Current.Id;
                    anket.IslemYapanIP = UserIdentity.Ip;
                    anket.IslemTarihi = DateTime.Now;
                    anket.IsAktif = kModel.IsAktif;

                }
                _entities.SaveChanges();
                return RedirectToAction("Index");
            }
            else
            {
                MessageBox.Show("Uyarı", MessageBox.MessageType.Warning, MmMessage.Messages.ToArray());
            }
            ViewBag.EnstituKod = new SelectList(EnstituBus.GetCmbYetkiliEnstituler(true), "Value", "Caption", kModel.EnstituKod);
            return View(kModel);
        }

        public ActionResult GetDetail(int AnketID)
        {
            var qModel = (from s in _entities.Ankets.Where(p => p.AnketID == AnketID)
                          join sa in _entities.AnketSorus on s.AnketID equals sa.AnketID
                          select new FrAnketDetayDto
                          {
                              AnketSoruID = sa.AnketSoruID,
                              AnketID = sa.AnketID,
                              SoruAdi = sa.SoruAdi,
                              SiraNo = sa.SiraNo,
                              IsTabloVeriGirisi = sa.IsTabloVeriGirisi,
                              IsTabloVeriMaxSatir = sa.IsTabloVeriMaxSatir,
                              SecenekSayisi = sa.AnketSoruSeceneks.Count,
                              FrAnketSecenekDetay = (from ss in sa.AnketSoruSeceneks
                                                     select new FrAnketSecenekDetayDto
                                                     {
                                                         AnketSoruID = ss.AnketSoruID,
                                                         AnketSoruSecenekID = ss.AnketSoruSecenekID,
                                                         SiraNo = ss.SiraNo,
                                                         IsEkAciklamaGir = ss.IsEkAciklamaGir,
                                                         SecenekAdi = ss.SecenekAdi
                                                     }
                                                   ).OrderBy(o => o.SiraNo).ToList()
                          }).OrderBy(o => o.SiraNo).ToList();
            var page = ViewRenderHelper.RenderPartialView("Anketler", "DetaySablon", qModel);
            return Json(new { page = page }, "application/json", JsonRequestBehavior.AllowGet);
        }
        public ActionResult DetaySablon()
        {
            return View();
        }

        public ActionResult GetDetail2(int anketSoruId)
        {
            var qModel = (from s in _entities.AnketSorus.Where(p => p.AnketSoruID == anketSoruId)
                          join sa in _entities.AnketSoruSeceneks on s.AnketSoruID equals sa.AnketSoruID
                          select new FrAnketSecenekDetayDto
                          {
                              AnketSoruSecenekID = sa.AnketSoruSecenekID,
                              AnketSoruID = sa.AnketSoruID,
                              SecenekAdi = sa.SecenekAdi,
                              SiraNo = sa.SiraNo,
                              IsEkAciklamaGir = sa.IsEkAciklamaGir
                          }).OrderBy(o => o.SiraNo).ToList();
            var page = ViewRenderHelper.RenderPartialView("Anketler", "DetaySablon2", qModel);
            return Json(new { page = page }, "application/json", JsonRequestBehavior.AllowGet);
        }
        public ActionResult DetaySablon2()
        {
            return View();
        }

        public ActionResult GetSoruEkle(int AnketID, int? AnketSoruID)
        {
            var model = new AnketSoru();
            model.AnketID = AnketID;
            var Anket = _entities.Ankets.Where(p => p.AnketID == AnketID).First();
            if (AnketSoruID.HasValue)
            {
                var data = _entities.AnketSorus.Where(p => p.AnketSoruID == AnketSoruID).FirstOrDefault();
                if (data != null)
                {
                    model = data;

                }
            }
            else model.SiraNo = Anket.AnketSorus.Count + 1;

            var page = ViewRenderHelper.RenderPartialView("Anketler", "SoruEkle", model);
            return Json(new { page = page }, "application/json", JsonRequestBehavior.AllowGet);
        }
        public ActionResult SoruEkle()
        {
            return View();
        }
        [HttpPost]
        public ActionResult SoruEklePost(AnketSoru kModel)
        {
            //var kModel = new kmAnketlerSoru();
            var mMessage = new MmMessage();
            mMessage.MessageType = MsgTypeEnum.Error;
            mMessage.IsSuccess = false;
            mMessage.Title = "Anket sorusu kaydetme işlemi başarısız";


            if (kModel.SoruAdi.IsNullOrWhiteSpace())
            {
                mMessage.Messages.Add("Anket Sorusunu Giriniz.");
                mMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "SoruAdi" });
            }
            else mMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "SoruAdi" });

            if (kModel.IsTabloVeriGirisi)
            {
                if (kModel.IsTabloVeriMaxSatir.HasValue == false || kModel.IsTabloVeriMaxSatir.Value <= 0)
                {
                    mMessage.Messages.Add("Tablo olarak veri girişi seçilen sorular için Max Cevap sayısı 0 dan büyük bir değer olmalıdır.");
                    mMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "IsTabloVeriMaxSatir" });
                }
                else mMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "IsTabloVeriMaxSatir" });
            }
            if (mMessage.Messages.Count == 0)
            {
                try
                {

                    if (kModel.AnketSoruID <= 0)
                    {
                        var anketS = _entities.AnketSorus.Add(kModel);
                        mMessage.SiraNo = _entities.AnketSorus.Where(p => p.AnketID == kModel.AnketID).Count() + 2;
                    }
                    else
                    {
                        var anketS = _entities.AnketSorus.Where(p => p.AnketSoruID == kModel.AnketSoruID).First();
                        anketS.SiraNo = kModel.SiraNo;
                        anketS.SoruAdi = kModel.SoruAdi;
                        anketS.IsTabloVeriGirisi = kModel.IsTabloVeriGirisi;
                        anketS.IsTabloVeriMaxSatir = kModel.IsTabloVeriGirisi ? kModel.IsTabloVeriMaxSatir : null;

                    }
                    _entities.SaveChanges();

                    mMessage.Title = "Anket sorusu kaydetme işlemi başarılı";
                    mMessage.MessageType = MsgTypeEnum.Success;
                    mMessage.IsSuccess = true;
                }
                catch (Exception ex)
                {

                    mMessage.Messages.Add("Hata:" + ex.ToExceptionMessage());
                }
            }
            return mMessage.ToJsonResult();
        }
        public ActionResult GetSoruSecenekEkle(int AnketSoruID, int? AnketSoruSecenekID)
        {
            var model = new AnketSoruSecenek();
            model.AnketSoruID = AnketSoruID;
            model.AnketSoruID = AnketSoruID;
            model.IsYaziOrSayi = true;
            if (AnketSoruSecenekID.HasValue)
            {
                var data = _entities.AnketSoruSeceneks.Where(p => p.AnketSoruSecenekID == AnketSoruSecenekID).FirstOrDefault();
                if (data != null)
                {
                    model = data;
                }
            }
            else model.SiraNo = model.AnketSoru.AnketSoruSeceneks.Count + 1;

            var page = ViewRenderHelper.RenderPartialView("Anketler", "SoruSecenekEkle", model);
            return Json(new { page = page }, "application/json", JsonRequestBehavior.AllowGet);
        }
        public ActionResult SoruSecenekEkle()
        {
            return View();
        }
        [HttpPost]
        public ActionResult SoruSecenekEklePost(AnketSoruSecenek kModel)
        {
            //var kModel = new kmAnketlerSoru();
            var mMessage = new MmMessage();
            mMessage.MessageType = MsgTypeEnum.Error;
            mMessage.IsSuccess = false;
            mMessage.Title = "Anket sorusu şıkkı kaydetme işlemi başarısız";
            if (kModel.SecenekAdi.IsNullOrWhiteSpace())
            {
                mMessage.Messages.Add("Seçenek Bilgisini Giriniz.");
                mMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "SecenekAdi" });
            }
            else mMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "SecenekAdi" });



            if (mMessage.Messages.Count == 0)
            {
                try
                {

                    if (kModel.AnketSoruSecenekID <= 0)
                    {
                        _entities.AnketSoruSeceneks.Add(kModel);
                        mMessage.SiraNo = _entities.AnketSoruSeceneks.Where(p => p.AnketSoruID == kModel.AnketSoruID).Count() + 2;
                    }
                    else
                    {
                        var anketSs = _entities.AnketSoruSeceneks.Where(p => p.AnketSoruSecenekID == kModel.AnketSoruSecenekID).First();
                        anketSs.SiraNo = kModel.SiraNo;
                        anketSs.SecenekAdi = kModel.SecenekAdi;
                        anketSs.IsEkAciklamaGir = kModel.IsEkAciklamaGir;
                        anketSs.IsYaziOrSayi = kModel.IsYaziOrSayi;

                    }
                    _entities.SaveChanges();

                    mMessage.Title = "Anket sorusu şıkkı kaydetme işlemi başarılı";
                    mMessage.MessageType = MsgTypeEnum.Success;
                    mMessage.IsSuccess = true;
                }
                catch (Exception ex)
                {

                    mMessage.Messages.Add("Hata:" + ex.ToExceptionMessage());
                }
            }
            return mMessage.ToJsonResult();
        }
        public ActionResult Sil(int id)
        {
            var kayit = _entities.Ankets.Where(p => p.AnketID == id).FirstOrDefault();
            string message = "";
            bool success = true;
            if (kayit != null)
            {
                try
                {
                    message = "'" + kayit.AnketAdi + "' İsimli Anket Silindi!";
                    _entities.Ankets.Remove(kayit);
                    _entities.SaveChanges();
                }
                catch (Exception ex)
                {
                    success = false;
                    message = "'" + kayit.AnketAdi + "' İsimli Anket Silinemedi! <br/> Bilgi:" + ex.ToExceptionMessage();
                    SistemBilgilendirmeBus.SistemBilgisiKaydet(message, ex.ToExceptionStackTrace(), BilgiTipiEnum.OnemsizHata);
                }
            }
            else
            {
                success = false;
                message = "Silmek istediğiniz Anket sistemde bulunamadı!";
            }
            return Json(new { success = success, message = message }, "application/json", JsonRequestBehavior.AllowGet);
        }
        public ActionResult SilSoru(int id)
        {
            var kayit = _entities.AnketSorus.Where(p => p.AnketSoruID == id).FirstOrDefault();
            string message = "";
            bool success = true;
            if (kayit != null)
            {
                try
                {
                    message = "'" + kayit.SoruAdi + "' İsimli Anket Sorusu Silindi!";
                    _entities.AnketSorus.Remove(kayit);
                    _entities.SaveChanges();
                }
                catch (Exception ex)
                {
                    success = false;
                    message = "'" + kayit.SoruAdi + "' İsimli Anket Sorusu Silinemedi! <br/> Bilgi:" + ex.ToExceptionMessage();
                    SistemBilgilendirmeBus.SistemBilgisiKaydet(message, ex.ToExceptionStackTrace(), BilgiTipiEnum.OnemsizHata);
                }
            }
            else
            {
                success = false;
                message = "Silmek istediğiniz Anket Sorusu sistemde bulunamadı!";
            }
            return Json(new { success = success, message = message }, "application/json", JsonRequestBehavior.AllowGet);
        }
        public ActionResult SilSoruSecenek(int id)
        {
            var kayit = _entities.AnketSoruSeceneks.Where(p => p.AnketSoruSecenekID == id).FirstOrDefault();
            string message = "";
            bool success = true;
            if (kayit != null)
            {
                try
                {

                    message = "'" + kayit.SecenekAdi + "' İsimli Anket Sorusu Şıkkı Silindi!";
                    _entities.AnketSoruSeceneks.Remove(kayit);
                    _entities.SaveChanges();
                }
                catch (Exception ex)
                {
                    success = false;
                    message = "'" + kayit.SecenekAdi + "' İsimli Anket Sorusu Şıkkı Silinemedi! <br/> Bilgi:" + ex.ToExceptionMessage();
                    SistemBilgilendirmeBus.SistemBilgisiKaydet(message, ex.ToExceptionStackTrace(), BilgiTipiEnum.OnemsizHata);
                }
            }
            else
            {
                success = false;
                message = "Silmek istediğiniz Anket Sorusu Şıkkı sistemde bulunamadı!";
            }
            return Json(new { success = success, message = message }, "application/json", JsonRequestBehavior.AllowGet);
        }

    }
}