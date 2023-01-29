using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using LisansUstuBasvuruSistemi.Models; using LisansUstuBasvuruSistemi.Models.FilterModel;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Utilities.Enums;

namespace LisansUstuBasvuruSistemi.Controllers
{
    [System.Web.Mvc.OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    [Authorize(Roles = RoleNames.TalepSureci)]
    public class TalepSureciController : Controller
    {
        // GET: FRDonemIslemleri
        private LisansustuBasvuruSistemiEntities db = new LisansustuBasvuruSistemiEntities();
        public ActionResult Index()
        {
            return Index(new fmTalepSurec() { PageSize = 15 });
        }
        [HttpPost]
        public ActionResult Index(fmTalepSurec model)
        {
            var EnstKods = UserIdentity.Current.EnstituKods ?? new List<string>();
            var nowDate = DateTime.Now;
            var q = from s in db.TalepSurecleris
                    join el in db.Enstitulers on s.EnstituKod equals el.EnstituKod
                    join k in db.Kullanicilars on s.IslemYapanID equals k.KullaniciID
                    where EnstKods.Contains(s.EnstituKod)
                    select new frTalepSurec()
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
            if (!model.Sort.IsNullOrWhiteSpace()) q = q.OrderBy(model.Sort);
            else q = q.OrderByDescending(t => t.BaslangicTarihi);
            model.Data = q.Skip(model.StartRowIndex).Take(model.PageSize).ToList();
            var IndexModel = new MIndexBilgi() { Toplam = model.RowCount, Pasif = q.Where(p => !p.IsAktif).Count() };
            IndexModel.Aktif = IndexModel.Toplam - IndexModel.Pasif;
            ViewBag.IndexModel = IndexModel;
            ViewBag.IsAktif = new SelectList(Management.cmbAktifPasifData(), "Value", "Caption", model.IsAktif);
            return View(model);
        }
        public ActionResult Kayit(int? id, string EKD)
        {
            string _EnstituKod = Management.getSelectedEnstitu(EKD);
            var MmMessage = new MmMessage();
            ViewBag.MmMessage = MmMessage;
            var model = new TalepSurecleri();
            model.IsAktif = true;
            if (id.HasValue && id > 0)
            {
                var data = db.TalepSurecleris.Where(p => p.TalepSurecID == id).FirstOrDefault();

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
            ViewBag.TalepTipleris = db.TalepTipleris.ToList();
            ViewBag.IsAktif = new SelectList(Management.cmbAktifPasifData(), "Value", "Caption", model.IsAktif);
            ViewBag.EnstituKod = new SelectList(Management.cmbGetAktifEnstituler(true), "Value", "Caption", model.EnstituKod ?? _EnstituKod);
            ViewBag.TalepTipID = model.TalepSureciTalepTipleris.Select(s => s.TalepTipID).ToList();
            return View(model);
        }
        [HttpPost]

        public ActionResult Kayit(TalepSurecleri kModel, List<int> TalepTipID)
        {
            var MmMessage = new MmMessage();
            TalepTipID = TalepTipID ?? new List<int>();
            #region Kontrol  
            if (kModel.EnstituKod.IsNullOrWhiteSpace())
            {
                string msg = "Enstitü Seçiniz";
                MmMessage.Messages.Add(msg);
                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "EnstituKod" });

            }
            else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "EnstituKod" });
            if (kModel.BaslangicTarihi == DateTime.MinValue || kModel.BitisTarihi == DateTime.MinValue)
            {
                if (kModel.BaslangicTarihi == DateTime.MinValue)
                {
                    MmMessage.Messages.Add("Başlangıç Tarihi Seçiniz.");
                    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "BaslangicTarihi" });

                }
                if (kModel.BitisTarihi == DateTime.MinValue)
                {
                    MmMessage.Messages.Add("Bitiş Tarihi Seçiniz.");
                    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "BitisTarihi" });

                }

            }
            else if (kModel.BaslangicTarihi >= kModel.BitisTarihi)
            {
                MmMessage.Messages.Add("Başlangıç Tarihi Bitiş Tarihinden Büyükya daEşit Olamaz.");
                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "BitisTarihi" });
                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "BaslangicTarihi" });
            }
            else
            {
                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "BaslangicTarihi" });
                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "BitisTarihi" });
            }

            if (TalepTipID.Count == 0)
            {
                string msg = "En az bir talep tipi seçilmelidir";
                MmMessage.Messages.Add(msg);
                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "TalepTipID" });

            }
            else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "TalepTipID" });
            if (MmMessage.Messages.Count == 0)
            {
                if (db.TalepSurecleris.Any(a => a.EnstituKod == kModel.EnstituKod &&
                                                a.TalepSurecID != kModel.TalepSurecID &&
                                                (
                                                    (a.BaslangicTarihi <= kModel.BaslangicTarihi && a.BitisTarihi >= kModel.BaslangicTarihi) ||
                                                    (a.BaslangicTarihi <= kModel.BitisTarihi && a.BitisTarihi >= kModel.BitisTarihi)))
                                                )
                {
                    MmMessage.Messages.Add("Seçilen başlangıç bitiş tarihleri daha önceden kayıt edilen süreçlerle çakışmaması gerekmektedir.");
                    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "BitisTarihi" });
                    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "BaslangicTarihi" });
                }
            }
            #endregion

            if (MmMessage.Messages.Count == 0)
            {
                kModel.IslemTarihi = DateTime.Now;
                kModel.IslemYapanID = UserIdentity.Current.Id;
                kModel.IslemYapanIP = UserIdentity.Ip;
                var Table = new TalepSurecleri();
                var TalepTipleris = db.TalepTipleris.Where(p => TalepTipID.Contains(p.TalepTipID)).ToList();
                var TalepSureciTalepTipleris =
                     TalepTipleris.Select(s => new TalepSureciTalepTipleri { TalepTipID = s.TalepTipID, IsBelgeYuklemeVar = s.IsBelgeYuklemeVar, IsTaahhutIsteniyor = s.IsTaahhutIsteniyor }).ToList();
                if (kModel.TalepSurecID <= 0)
                {
                    Table = db.TalepSurecleris.Add(new TalepSurecleri()
                    {
                        EnstituKod = kModel.EnstituKod,
                        BaslangicTarihi = kModel.BaslangicTarihi,
                        BitisTarihi = kModel.BitisTarihi,
                        IsAktif = kModel.IsAktif,
                        IslemTarihi = kModel.IslemTarihi,
                        IslemYapanID = kModel.IslemYapanID,
                        IslemYapanIP = kModel.IslemYapanIP,
                        TalepSureciTalepTipleris = TalepSureciTalepTipleris

                    });
                }
                else
                {
                    Table = db.TalepSurecleris.Where(p => p.TalepSurecID == kModel.TalepSurecID).First();
                    Table.EnstituKod = kModel.EnstituKod;
                    Table.BaslangicTarihi = kModel.BaslangicTarihi;
                    Table.BitisTarihi = kModel.BitisTarihi;
                    Table.IsAktif = kModel.IsAktif;
                    Table.IslemTarihi = DateTime.Now;
                    Table.IslemYapanID = kModel.IslemYapanID;
                    Table.IslemYapanIP = kModel.IslemYapanIP;
                    db.TalepSureciTalepTipleris.RemoveRange(Table.TalepSureciTalepTipleris);
                    Table.TalepSureciTalepTipleris = TalepSureciTalepTipleris;


                }
                db.SaveChanges();

                return RedirectToAction("Index");
            }
            else
            {
                MessageBox.Show("Uyarı", MessageBox.MessageType.Warning, MmMessage.Messages.ToArray());
            }
            ViewBag.TalepTipleris = db.TalepTipleris.ToList();
            ViewBag.TalepTipID = TalepTipID;
            ViewBag.MmMessage = MmMessage;
            ViewBag.EnstituKod = new SelectList(Management.cmbGetAktifEnstituler(true), "Value", "Caption", kModel.EnstituKod);
            ViewBag.IsAktif = new SelectList(Management.cmbAktifPasifData(), "Value", "Caption", kModel.IsAktif);
            return View(kModel);
        }

        public ActionResult Sil(int id)
        {
            var mmMessage = new MmMessage();

            var kayit = db.TalepSurecleris.Where(p => p.TalepSurecID == id).FirstOrDefault();

            string message = "";
            if (kayit != null)
            {
                try
                {
                    message = "'" + kayit.BaslangicTarihi.ToFormatDateAndTime() + " / " + kayit.BitisTarihi.ToFormatDateAndTime() + "' Tarihli Talep Süreci Silindi!";
                    db.TalepSurecleris.Remove(kayit);
                    db.SaveChanges();
                    mmMessage.Title = "Uyarı";
                    mmMessage.Messages.Add(message);
                    mmMessage.MessageType = Msgtype.Success;
                    mmMessage.IsSuccess = true;
                }
                catch (Exception ex)
                {
                    message = "'" + kayit.BaslangicTarihi.ToFormatDateAndTime() + " / " + kayit.BitisTarihi.ToFormatDateAndTime() + "' Tarihli Talep Süreci Silinirken Bir Hata Oluştu! </br> Hata:" + ex.ToExceptionMessage();
                    Management.SistemBilgisiKaydet(message, "TalepSureci/Sil<br/><br/>" + ex.ToExceptionStackTrace(), BilgiTipi.OnemsizHata);
                    mmMessage.Title = "Hata";
                    mmMessage.Messages.Add(message);
                    mmMessage.MessageType = Msgtype.Error;
                    mmMessage.IsSuccess = false;
                }
            }
            else
            {
                message = "Silmek İstediğiniz Talep Süreci Sistemde Bulunamadı!";
                mmMessage.Title = "Hata";
                mmMessage.Messages.Add(message);
                mmMessage.MessageType = Msgtype.Error;
                mmMessage.IsSuccess = true;
            }
            var strView = Management.RenderPartialView("Ajax", "getMessage", mmMessage);
            return Json(new { IsSuccess = mmMessage.IsSuccess, Messages = strView }, "application/json", JsonRequestBehavior.AllowGet);
        }
    }
}