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
using LisansUstuBasvuruSistemi.Utilities.Helpers;
using LisansUstuBasvuruSistemi.Utilities.SystemData;

namespace LisansUstuBasvuruSistemi.Controllers
{
    [System.Web.Mvc.OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    [Authorize(Roles = RoleNames.TalepSureci)]
    public class TalepSureciController : Controller
    {
        // GET: FRDonemIslemleri
        private readonly LisansustuBasvuruSistemiEntities _entities = new LisansustuBasvuruSistemiEntities();
        public ActionResult Index()
        {
            return Index(new FmTalepSurec() { PageSize = 15 });
        }
        [HttpPost]
        public ActionResult Index(FmTalepSurec model)
        {
            var enstKods = UserIdentity.Current.EnstituKods ?? new List<string>();
            var nowDate = DateTime.Now;
            var q = from s in _entities.TalepSurecleris
                    join el in _entities.Enstitulers on s.EnstituKod equals el.EnstituKod
                    join k in _entities.Kullanicilars on s.IslemYapanID equals k.KullaniciID
                    where enstKods.Contains(s.EnstituKod)
                    select new FrTalepSurec()
                    {
                        EnstituAdi = el.EnstituAd,
                        TalepSurecID = s.TalepSurecID,
                        BaslangicTarihi = s.BaslangicTarihi,
                        BitisTarihi = s.BitisTarihi,
                        IsAktif = s.IsAktif,
                        IslemYapanID = s.IslemYapanID,
                        IslemYapan = k.KullaniciAdi,
                        IslemTarihi = s.IslemTarihi,
                        IslemYapanIP = s.IslemYapanIP,
                        TalepSureciTalepTipleris = s.TalepSureciTalepTipleris,
                        AktifSurec = (s.BaslangicTarihi <= nowDate && s.BitisTarihi >= nowDate)
                    };
            model.RowCount = q.Count();
            q = !model.Sort.IsNullOrWhiteSpace() ? q.OrderBy(model.Sort) : q.OrderByDescending(t => t.BaslangicTarihi);
            model.Data = q.Skip(model.StartRowIndex).Take(model.PageSize).ToList();
            var indexModel = new MIndexBilgi() { Toplam = model.RowCount, Pasif = q.Count(p => !p.IsAktif) };
            indexModel.Aktif = indexModel.Toplam - indexModel.Pasif;
            ViewBag.IndexModel = indexModel;
            ViewBag.IsAktif = new SelectList(ComboData.GetCmbAktifPasifData(), "Value", "Caption", model.IsAktif);
            return View(model);
        }
        public ActionResult Kayit(int? id, string ekd)
        {
            string enstituKod = EnstituBus.GetSelectedEnstitu(ekd);
            var mmMessage = new MmMessage();
            ViewBag.MmMessage = mmMessage;
            var model = new TalepSurecleri
            {
                IsAktif = true
            };
            if (id.HasValue && id > 0)
            {
                var data = _entities.TalepSurecleris.FirstOrDefault(p => p.TalepSurecID == id);

                if (data != null)
                {
                    model.EnstituKod = data.EnstituKod;
                    model.TalepSurecID = data.TalepSurecID;
                    model.BaslangicTarihi = data.BaslangicTarihi;
                    model.BitisTarihi = data.BitisTarihi;
                    model.IsAktif = data.IsAktif;
                    model.IslemTarihi = DateTime.Now;
                    model.IslemYapanID = data.IslemYapanID;
                    model.IslemYapanIP = data.IslemYapanIP;
                    model.TalepSureciTalepTipleris = data.TalepSureciTalepTipleris;
                }

            }
            else model.TalepSureciTalepTipleris = new List<TalepSureciTalepTipleri>();
            ViewBag.TalepTipID = model.TalepSureciTalepTipleris.Select(s => s.TalepTipID).ToList();
            ViewBag.TalepTipleris = _entities.TalepTipleris.ToList();
            ViewBag.IsAktif = new SelectList(ComboData.GetCmbAktifPasifData(), "Value", "Caption", model.IsAktif);
            ViewBag.EnstituKod = new SelectList(EnstituBus.GetCmbAktifEnstituler(true), "Value", "Caption", model.EnstituKod ?? enstituKod);
            ViewBag.TalepTipID = model.TalepSureciTalepTipleris.Select(s => s.TalepTipID).ToList();
            return View(model);
        }
        [HttpPost]

        public ActionResult Kayit(TalepSurecleri kModel, List<int> talepTipId)
        {
            var mmMessage = new MmMessage();
            talepTipId = talepTipId ?? new List<int>();
            #region Kontrol  
            if (kModel.EnstituKod.IsNullOrWhiteSpace())
            { 
                mmMessage.Messages.Add("Enstitü Seçiniz");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "EnstituKod" });

            }
            else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "EnstituKod" });
            if (kModel.BaslangicTarihi == DateTime.MinValue || kModel.BitisTarihi == DateTime.MinValue)
            {
                if (kModel.BaslangicTarihi == DateTime.MinValue)
                {
                    mmMessage.Messages.Add("Başlangıç Tarihi Seçiniz.");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "BaslangicTarihi" });

                }
                if (kModel.BitisTarihi == DateTime.MinValue)
                {
                    mmMessage.Messages.Add("Bitiş Tarihi Seçiniz.");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "BitisTarihi" });

                }

            }
            else if (kModel.BaslangicTarihi >= kModel.BitisTarihi)
            {
                mmMessage.Messages.Add("Başlangıç Tarihi Bitiş Tarihinden Büyükya daEşit Olamaz.");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "BitisTarihi" });
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "BaslangicTarihi" });
            }
            else
            {
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "BaslangicTarihi" });
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "BitisTarihi" });
            }

            if (talepTipId.Count == 0)
            { 
                mmMessage.Messages.Add("En az bir talep tipi seçilmelidir");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "TalepTipID" });

            }
            else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "TalepTipID" });
            if (mmMessage.Messages.Count == 0)
            {
                if (_entities.TalepSurecleris.Any(a => a.EnstituKod == kModel.EnstituKod &&
                                                a.TalepSurecID != kModel.TalepSurecID &&
                                                (
                                                    (a.BaslangicTarihi <= kModel.BaslangicTarihi && a.BitisTarihi >= kModel.BaslangicTarihi) ||
                                                    (a.BaslangicTarihi <= kModel.BitisTarihi && a.BitisTarihi >= kModel.BitisTarihi)))
                                                )
                {
                    mmMessage.Messages.Add("Seçilen başlangıç bitiş tarihleri daha önceden kayıt edilen süreçlerle çakışmaması gerekmektedir.");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "BitisTarihi" });
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "BaslangicTarihi" });
                }
            }
            #endregion

            if (mmMessage.Messages.Count == 0)
            {
                kModel.IslemTarihi = DateTime.Now;
                kModel.IslemYapanID = UserIdentity.Current.Id;
                kModel.IslemYapanIP = UserIdentity.Ip;
                TalepSurecleri table;
                var talepTipleris = _entities.TalepTipleris.Where(p => talepTipId.Contains(p.TalepTipID)).ToList();
                var talepSureciTalepTipleris =
                     talepTipleris.Select(s => new TalepSureciTalepTipleri { TalepTipID = s.TalepTipID, IsBelgeYuklemeVar = s.IsBelgeYuklemeVar, IsTaahhutIsteniyor = s.IsTaahhutIsteniyor }).ToList();
                if (kModel.TalepSurecID <= 0)
                {
                    table = _entities.TalepSurecleris.Add(new TalepSurecleri()
                    {
                        EnstituKod = kModel.EnstituKod,
                        BaslangicTarihi = kModel.BaslangicTarihi,
                        BitisTarihi = kModel.BitisTarihi,
                        IsAktif = kModel.IsAktif,
                        IslemTarihi = kModel.IslemTarihi,
                        IslemYapanID = kModel.IslemYapanID,
                        IslemYapanIP = kModel.IslemYapanIP,
                        TalepSureciTalepTipleris = talepSureciTalepTipleris

                    });
                }
                else
                {
                    table = _entities.TalepSurecleris.First(p => p.TalepSurecID == kModel.TalepSurecID);
                    table.EnstituKod = kModel.EnstituKod;
                    table.BaslangicTarihi = kModel.BaslangicTarihi;
                    table.BitisTarihi = kModel.BitisTarihi;
                    table.IsAktif = kModel.IsAktif;
                    table.IslemTarihi = DateTime.Now;
                    table.IslemYapanID = kModel.IslemYapanID;
                    table.IslemYapanIP = kModel.IslemYapanIP;
                    _entities.TalepSureciTalepTipleris.RemoveRange(table.TalepSureciTalepTipleris);
                    table.TalepSureciTalepTipleris = talepSureciTalepTipleris;


                }
                _entities.SaveChanges();

                return RedirectToAction("Index");
            }
            else
            {
                MessageBox.Show("Uyarı", MessageBox.MessageType.Warning, mmMessage.Messages.ToArray());
            }
            ViewBag.TalepTipleris = _entities.TalepTipleris.ToList();
            ViewBag.TalepTipID = talepTipId;
            ViewBag.MmMessage = mmMessage;
            ViewBag.EnstituKod = new SelectList(EnstituBus.GetCmbAktifEnstituler(true), "Value", "Caption", kModel.EnstituKod);
            ViewBag.IsAktif = new SelectList(ComboData.GetCmbAktifPasifData(), "Value", "Caption", kModel.IsAktif);
            return View(kModel);
        }

        public ActionResult Sil(int id)
        {
            var mmMessage = new MmMessage();

            var kayit = _entities.TalepSurecleris.FirstOrDefault(p => p.TalepSurecID == id);

            string message = "";
            if (kayit != null)
            {
                try
                {
                    message = "'" + kayit.BaslangicTarihi.ToFormatDateAndTime() + " / " + kayit.BitisTarihi.ToFormatDateAndTime() + "' Tarihli Talep Süreci Silindi!";
                    _entities.TalepSurecleris.Remove(kayit);
                    _entities.SaveChanges();
                    mmMessage.Title = "Uyarı";
                    mmMessage.Messages.Add(message);
                    mmMessage.MessageType = MsgTypeEnum.Success;
                    mmMessage.IsSuccess = true;
                }
                catch (Exception ex)
                {
                    message = "'" + kayit.BaslangicTarihi.ToFormatDateAndTime() + " / " + kayit.BitisTarihi.ToFormatDateAndTime() + "' Tarihli Talep Süreci Silinirken Bir Hata Oluştu! </br> Hata:" + ex.ToExceptionMessage();
                    SistemBilgilendirmeBus.SistemBilgisiKaydet(message, ex.ToExceptionStackTrace(), BilgiTipiEnum.OnemsizHata);
                    mmMessage.Title = "Hata";
                    mmMessage.Messages.Add(message);
                    mmMessage.MessageType = MsgTypeEnum.Error;
                    mmMessage.IsSuccess = false;
                }
            }
            else
            {
                message = "Silmek İstediğiniz Talep Süreci Sistemde Bulunamadı!";
                mmMessage.Title = "Hata";
                mmMessage.Messages.Add(message);
                mmMessage.MessageType = MsgTypeEnum.Error;
                mmMessage.IsSuccess = true;
            }
            var strView = ViewRenderHelper.RenderPartialView("Ajax", "getMessage", mmMessage);
            return Json(new { mmMessage.IsSuccess, Messages = strView }, "application/json", JsonRequestBehavior.AllowGet);
        }
    }
}