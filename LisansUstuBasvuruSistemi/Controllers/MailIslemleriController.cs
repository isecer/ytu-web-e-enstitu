using BiskaUtil;
using Entities.Entities;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.MenuAndRoles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Net.Mime;
using System.Web;
using System.Web.Mvc;
using LisansUstuBasvuruSistemi.Business;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.Helpers;
using LisansUstuBasvuruSistemi.Utilities.MailManager;

namespace LisansUstuBasvuruSistemi.Controllers
{
    [System.Web.Mvc.OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    [Authorize(Roles = RoleNames.MailIslemleri)]
    public class MailIslemleriController : Controller
    {
        private readonly LubsDbEntities _entities = new LubsDbEntities();
        public ActionResult Index()
        {
            return Index(new FmMailGondermeDto() { PageSize = 15 });
        }
        [HttpPost]
        public ActionResult Index(FmMailGondermeDto model)
        {
            var q = from s in _entities.GonderilenMaillers.Where(p => model.Aciklama == null || model.Aciklama.Trim() == "" || p.Aciklama.Contains(model.Aciklama))
                    join e in _entities.Enstitulers on s.EnstituKod equals e.EnstituKod
                    join k in _entities.Kullanicilars on s.IslemYapanID equals k.KullaniciID
                    where s.Silindi == false && UserIdentity.Current.EnstituKods.Contains(s.EnstituKod)
                    select new
                    {
                        s.GonderilenMailID,
                        s.Tarih,
                        s.EnstituKod,
                        e.EnstituAd,
                        s.Konu,
                        MailGonderen = k.Ad + " " + k.Soyad,
                        s.Gonderildi,
                        s.HataMesaji,
                        EkSayisi = s.GonderilenMailEkleris.Count
                    };

            if (!model.Konu.IsNullOrWhiteSpace()) q = q.Where(p => p.Konu.Contains(model.Konu));
            if (!model.EnstituKod.IsNullOrWhiteSpace()) q = q.Where(p => p.EnstituKod == model.EnstituKod);
            if (!model.MailGonderen.IsNullOrWhiteSpace()) q = q.Where(p => p.MailGonderen.Contains(model.MailGonderen));
            if (model.IsEkVar.HasValue) q = q.Where(p => p.EkSayisi > 0 == model.IsEkVar);
            if (model.Tarih.HasValue)
            {
                var trih = model.Tarih.Value.TodateToShortDate();
                q = q.Where(p => p.Tarih == trih);

            }
            model.RowCount = q.Count();
            q = !model.Sort.IsNullOrWhiteSpace() ? q.OrderBy(model.Sort) : q.OrderByDescending(o => o.Tarih);
            model.MailGondermeDtos = q.Skip(model.StartRowIndex).Take(model.PageSize).Select(s => new FrMailGondermeDto
            {
                GonderilenMailID = s.GonderilenMailID,
                Tarih = s.Tarih,
                EnstituAdi = s.EnstituAd,
                Konu = s.Konu,
                MailGonderen = s.MailGonderen,
                Gonderildi = s.Gonderildi,
                HataMesaji = s.HataMesaji,
                EkSayisi = s.EkSayisi

            }).ToList();
            ViewBag.IsEkVar = new SelectList(GonderilenMaillerBus.GetCmbMailEkKontrol(true), "Value", "Caption", model.IsEkVar);
            ViewBag.EnstituKod = new SelectList(EnstituBus.GetCmbAktifEnstituler(true), "Value", "Caption", model.EnstituKod);
            return View(model);
        }
        public ActionResult MailDetay(int gonderilenMailId)
        {

            var data = (from s in _entities.GonderilenMaillers
                        join e in _entities.Enstitulers on s.EnstituKod equals e.EnstituKod into def
                        from xDef in def.DefaultIfEmpty()
                        join k in _entities.Kullanicilars on s.IslemYapanID equals k.KullaniciID
                        where s.GonderilenMailID == gonderilenMailId
                        select new FrMailGondermeDto
                        {
                            GonderilenMailID = s.GonderilenMailID,
                            EnstituAdi = xDef != null ? xDef.EnstituAd : "Sistem",
                            Tarih = s.Tarih,
                            Konu = s.Konu,
                            Aciklama = s.Aciklama,
                            AciklamaHtml = s.AciklamaHtml,
                            MailGonderen = k.Ad + " " + k.Soyad,
                            UserKey = k.UserKey,
                            IslemYapanID = s.IslemYapanID,
                            IslemYapanIP = s.IslemYapanIP,
                            EkSayisi = s.GonderilenMailEkleris.Count,
                            KisiSayisi = s.GonderilenMailKullanicilars.Count,
                            GonderilenMailEkleris = s.GonderilenMailEkleris.ToList()

                        }).First();
            var dataK = (from s in _entities.GonderilenMailKullanicilars
                         orderby s.Kullanicilar.Ad, s.Kullanicilar.Soyad
                         where s.GonderilenMailID == gonderilenMailId
                         select new MailKullaniciBilgi
                         {
                             AdSoyad = s.Kullanicilar.Ad + " " + s.Kullanicilar.Soyad,
                             Email = s.Email
                         }).ToList();
            ViewBag.DataK = dataK;
            return View(data);
        }

        public ActionResult Gonder(int? id, List<int> kullaniciId, string ekd)
        {
            var model = new GonderilenMailler();
            var mmMessage = new MmMessage();
            string enstituKod = EnstituBus.GetSelectedEnstitu(ekd);
            ViewBag.MmMessage = mmMessage;

            var dataK = (from s in _entities.GonderilenMailKullanicilars
                         orderby s.Kullanicilar.Ad, s.Kullanicilar.Soyad
                         select new MailKullaniciBilgi
                         {
                             AdSoyad = s.Kullanicilar.Ad + " " + s.Kullanicilar.Soyad,
                             Email = s.Email
                         }).ToList();



            ViewBag.Kullanicilar = dataK;
            ViewBag.SelectedTab = 1;
            ViewBag.Alici = "";
            var eList = new List<CmbIntDto>();
            kullaniciId = kullaniciId ?? new List<int>();
            _entities.Kullanicilars.Where(p => kullaniciId.Contains(p.KullaniciID)).ToList().ForEach((k) => { eList.Add(new CmbIntDto { Value = k.KullaniciID, Caption = k.EMail }); });

            ViewBag.MailSablonlariID = new SelectList(MailSablonTipleriBus.GetCmbMailSablonlari(enstituKod, true, false), "Value", "Caption");
            ViewBag.EmailList = eList;
            return View(model);
        }
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Gonder(GonderilenMailler kModel, List<string> dosyaEkiAdi, List<HttpPostedFileBase> dosyaEki, List<int?> duyuruDosyaEkId, List<int> kullaniciIDs, string ekd, string alici = "")
        {
            var mmMessage = new MmMessage();
            string enstituKod = EnstituBus.GetSelectedEnstitu(ekd);
            duyuruDosyaEkId = duyuruDosyaEkId ?? new List<int?>();
            dosyaEki = dosyaEki ?? new List<HttpPostedFileBase>();
            dosyaEkiAdi = dosyaEkiAdi ?? new List<string>();
            kullaniciIDs = kullaniciIDs ?? new List<int>();
            var secilenAlicilar = new List<string>();
            if (alici.IsNullOrWhiteSpace() == false) alici.Split(',').ToList().ForEach((itm) => { secilenAlicilar.Add(itm); });
            var qDosyaEkAdi = dosyaEkiAdi.Select((s, inx) => new { s, inx }).ToList();
            var qDosyaEki = dosyaEki.Select((s, inx) => new { s, inx }).ToList();
            var qDuyuruDosyaEkId = duyuruDosyaEkId.Select((s, inx) => new { s, inx }).ToList();
            var qDosyalar = (from ekGirilenAd in qDosyaEkAdi
                             join eklenenEk in qDosyaEki on ekGirilenAd.inx equals eklenenEk.inx
                             select new
                             {
                                 ekGirilenAd.inx,
                                 DosyaEkAdi = ekGirilenAd.s,
                                 Dosya = eklenenEk.s,
                                 mDosyaAdi = ekGirilenAd + eklenenEk.s.FileName.GetFileExtension(),
                                 DosyaYolu = "/MailDosyalari/" + ekGirilenAd.s.ToFileNameAddGuid(eklenenEk.s.FileName.GetFileExtension())
                             }).ToList();

            #region Kontrol
            kModel.Tarih = DateTime.Now;

            if (kModel.Konu.IsNullOrWhiteSpace())
            {
                mmMessage.Messages.Add("Konu Giriniz.");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "Konu" });
            }
            else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "Konu" });

            if (kModel.Aciklama.IsNullOrWhiteSpace() && kModel.AciklamaHtml.IsNullOrWhiteSpace())
            {
                mmMessage.Messages.Add("İçerik Giriniz.");
            }


            #endregion
            if (mmMessage.Messages.Count == 0)
            {
                kModel.IslemTarihi = DateTime.Now;
                kModel.EnstituKod = enstituKod;
                kModel.IslemYapanID = UserIdentity.Current.Id;
                kModel.IslemYapanIP = UserIdentity.Ip;
                kModel.Aciklama = kModel.Aciklama ?? "";
                var eklenen = _entities.GonderilenMaillers.Add(kModel);

                foreach (var item in qDosyalar)
                {
                    item.Dosya.SaveAs(Server.MapPath("~" + item.DosyaYolu));
                    _entities.GonderilenMailEkleris.Add(new GonderilenMailEkleri
                    {

                        GonderilenMailID = eklenen.GonderilenMailID,
                        EkAdi = item.DosyaEkAdi,
                        EkDosyaYolu = item.DosyaYolu
                    });
                }
                var mailList = new List<GonderilenMailKullanicilar>();
                var tari = DateTime.Now;

                if (secilenAlicilar.Count > 0)
                {
                    var qscIDs = secilenAlicilar.Where(p => p.IsNumber()).Select(s => s.ToInt().Value).ToList();
                    var qscMails = secilenAlicilar.Where(p => p.IsNumber() == false).ToList();
                    var dataqx = (from s in _entities.Kullanicilars
                                  where qscIDs.Contains(s.KullaniciID)
                                  select new
                                  {
                                      Email = s.EMail,
                                      eklenen.GonderilenMailID,
                                      s.KullaniciID
                                  }).ToList();
                    foreach (var item in dataqx)
                    {
                        mailList.Add(new GonderilenMailKullanicilar
                        {
                            Email = item.Email,
                            GonderilenMailID = item.GonderilenMailID,
                            KullaniciID = item.KullaniciID
                        });
                    }
                    foreach (var item in qscMails)
                    {
                        mailList.Add(new GonderilenMailKullanicilar
                        {
                            Email = item,
                            GonderilenMailID = eklenen.GonderilenMailID,
                            KullaniciID = null
                        });
                    }
                }
                mailList = _entities.GonderilenMailKullanicilars.AddRange(mailList).ToList();
                _entities.SaveChanges();
                var attach = new List<Attachment>();

                foreach (var item in qDosyalar)
                {
                    var ekTamYol = Server.MapPath("~" + item.DosyaYolu);
                    var fExtension = Path.GetExtension(ekTamYol);
                    attach.Add(new Attachment(new MemoryStream(System.IO.File.ReadAllBytes(ekTamYol)), item.mDosyaAdi.ToSetNameFileExtension(fExtension), MediaTypeNames.Application.Octet));
                }
                MailManager.SendMail(eklenen.GonderilenMailID, kModel.Konu, kModel.AciklamaHtml, mailList.Select(s => new MailSendList { EMail = s.Email,KullaniciId =s.KullaniciID, ToOrBcc = true }).ToList(), attach);
                return RedirectToAction("Index");
            }
            else
            {
                MessageBox.Show("Uyarı", MessageBox.MessageType.Warning, mmMessage.Messages.ToArray());
            }
            ViewBag.MmMessage = mmMessage;


            var qKullanicilar = from k in _entities.Kullanicilars
                                join bi in _entities.Birimlers on k.BirimID equals bi.BirimID
                                where k.EMail.Contains("@")
                                orderby k.Ad, k.Soyad
                                select new MailKullaniciBilgi
                                {
                                    KullaniciID = k.KullaniciID,
                                    AdSoyad = k.Ad + " " + k.Soyad,
                                    BirimAdi = bi.BirimAdi,
                                    Email = k.EMail

                                };
            var kul = qKullanicilar.ToList();
            foreach (var item in kul)
            {
                if (kullaniciIDs.Contains(item.KullaniciID)) item.Checked = true;
            }

            ViewBag.Kullanicilar = kul;
            ViewBag.Alici = alici;
            ViewBag.MailSablonlariID = new SelectList(MailSablonTipleriBus.GetCmbMailSablonlari(enstituKod, true, false), "Value", "Caption");
            return View(kModel);
        }

        public ActionResult SecilenKullaniciCount(string personelTipId, string birimId, string unvanId)
        {
            var qx = (from s in _entities.Kullanicilars
                      join b in _entities.Birimlers on s.BirimID equals b.BirimID into def1
                      from def2 in def1.DefaultIfEmpty()
                      where s.EMail.Contains("@")
                      select new { s.KullaniciID, s.KullaniciTipID, s.UnvanID, s.BirimID, UstBirimID = def2 == null ? (int?)null : def2.UstBirimID });
            var toplamCount = qx.Count();
            if (personelTipId != "")
            {
                var kullaniciTipIDs = new List<int>();
                personelTipId.Split(',').ToList().ForEach((itm) => { kullaniciTipIDs.Add(itm.ToInt().Value); });
                qx = qx.Where(s => kullaniciTipIDs.Contains(s.KullaniciTipID));
            }

            if (birimId != "")
            {
                var birimIDs = new List<int>();
                birimId.Split(',').ToList().ForEach((itm) => { birimIDs.Add(itm.ToInt().Value); });
                qx = qx.Where(s => s.BirimID.HasValue && (birimIDs.Contains(s.BirimID.Value) || birimIDs.Contains(s.UstBirimID ?? -1)));
                //UnvanIDs.Contains(s.UnvanID) && PersonelTipIDs.Contains(s.PersonelTipID)
            }
            if (unvanId != "")
            {
                var unvanIDs = new List<int>();
                unvanId.Split(',').ToList().ForEach((itm) => { unvanIDs.Add(itm.ToInt().Value); });
                qx = qx.Where(s => s.UnvanID.HasValue && unvanIDs.Contains(s.UnvanID.Value));

            }


            var dataqx = qx.Count();
            var jsonWal = new { Toplam = toplamCount, Secilen = dataqx };
            return jsonWal.ToJsonResult();

        }
        public ActionResult TekrarGonder(int id)
        {
            var gm = _entities.GonderilenMaillers.FirstOrDefault(p => p.GonderilenMailID == id);
            var eMailList = gm.GonderilenMailKullanicilars.Select(s => new MailSendList { EMail = s.Email,KullaniciId =s.KullaniciID, ToOrBcc = true }).ToList();
            var gEk = gm.GonderilenMailEkleris.ToList();
            var attach = new List<Attachment>();
            foreach (var item in gEk)
            {
                var ekTamYol = Server.MapPath("~" + item.EkDosyaYolu);
                var fExtension = Path.GetExtension(ekTamYol);
                attach.Add(new Attachment(new MemoryStream(System.IO.File.ReadAllBytes(ekTamYol)), item.EkAdi.ToSetNameFileExtension(fExtension), MediaTypeNames.Application.Octet));

            }
            MailManager.SendMail(gm.GonderilenMailID, gm.Konu, gm.AciklamaHtml, eMailList, attach);
            return true.ToJsonResult();
        }



        public ActionResult Sil(int id)
        {
            var kayit = _entities.GonderilenMaillers.FirstOrDefault(p => p.GonderilenMailID == id);
            string message = "";
            bool success = true;
            if (kayit != null)
            {
                try
                {

                    if (message == "")
                    {
                        kayit.Silindi = true;
                        _entities.SaveChanges();
                        message = "'" + kayit.Konu + "' konulu email Silindi!";
                    }

                }
                catch (Exception ex)
                {
                    success = false;
                    message = "'" + kayit.Konu + "' Konulu Mail Silinemedi! <br/> Bilgi:" + ex.ToExceptionMessage();
                    SistemBilgilendirmeBus.SistemBilgisiKaydet(message,  ex.ToExceptionStackTrace(), BilgiTipiEnum.OnemsizHata);
                }
            }
            else
            {
                success = false;
                message = "Silmek istediğiniz mail bilgisi sistemde bulunamadı!";
            }
            return Json(new { success, message }, "application/json", JsonRequestBehavior.AllowGet);
        }

    }
}
