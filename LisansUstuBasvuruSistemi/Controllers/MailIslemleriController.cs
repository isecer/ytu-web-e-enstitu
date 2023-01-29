using BiskaUtil;
using LisansUstuBasvuruSistemi.Models;
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

namespace LisansUstuBasvuruSistemi.Controllers
{
    [System.Web.Mvc.OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    [Authorize(Roles = RoleNames.MailIslemleri)]
    public class MailIslemleriController : Controller
    {
        private LisansustuBasvuruSistemiEntities db = new LisansustuBasvuruSistemiEntities();
        public ActionResult Index()
        {
            return Index(new fmMailGonderme() { PageSize = 15 });
        }
        [HttpPost]
        public ActionResult Index(fmMailGonderme model)
        {
            var q = from s in db.GonderilenMaillers.Where(p => model.Aciklama != null && model.Aciklama.Trim() != "" ? p.Aciklama.Contains(model.Aciklama): true)
                    join e in db.Enstitulers on s.EnstituKod equals e.EnstituKod
                    join k in db.Kullanicilars on s.IslemYapanID equals k.KullaniciID
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
                        s.HataMesaji
                    };

            if (!model.Konu.IsNullOrWhiteSpace()) q = q.Where(p => p.Konu.Contains(model.Konu));
            if (!model.EnstituKod.IsNullOrWhiteSpace()) q = q.Where(p => p.EnstituKod == model.EnstituKod);
            if (!model.MailGonderen.IsNullOrWhiteSpace()) q = q.Where(p => p.MailGonderen.Contains(model.MailGonderen));
            if (model.Tarih.HasValue)
            {
                var trih = model.Tarih.Value.TodateToShortDate();
                q = q.Where(p => p.Tarih == trih);

            }
            model.RowCount = q.Count();
            if (!model.Sort.IsNullOrWhiteSpace()) q = q.OrderBy(model.Sort);
            else q = q.OrderByDescending(o => o.Tarih);
            model.Data = q.Skip(model.StartRowIndex).Take(model.PageSize).Select(s => new frMailGonderme
            {
                GonderilenMailID = s.GonderilenMailID,
                Tarih = s.Tarih,
                EnstituAdi = s.EnstituAd,
                Konu = s.Konu,
                MailGonderen = s.MailGonderen,
                Gonderildi = s.Gonderildi,
                HataMesaji = s.HataMesaji

            }).ToList();
            ViewBag.EnstituKod = new SelectList(Management.cmbGetAktifEnstituler(true), "Value", "Caption", model.EnstituKod);
            return View(model);
        }
        public ActionResult MailDetay(int GonderilenMailID)
        {

            var data = (from s in db.GonderilenMaillers
                        join e in db.Enstitulers on s.EnstituKod equals e.EnstituKod into def
                        from xDef in def.DefaultIfEmpty()
                        join k in db.Kullanicilars on s.IslemYapanID equals k.KullaniciID
                        where s.GonderilenMailID == GonderilenMailID
                        select new frMailGonderme
                        {
                            GonderilenMailID = s.GonderilenMailID,
                            EnstituAdi = xDef != null ? xDef.EnstituAd : "Sistem",
                            Tarih = s.Tarih,
                            Konu = s.Konu,
                            Aciklama = s.Aciklama,
                            AciklamaHtml = s.AciklamaHtml,
                            MailGonderen = k.Ad + " " + k.Soyad,
                            IslemYapanID = s.IslemYapanID,
                            IslemYapanIP = s.IslemYapanIP,
                            EkSayisi = s.GonderilenMailEkleris.Count,
                            KisiSayisi = s.GonderilenMailKullanicilars.Count,
                            GonderilenMailEkleris = s.GonderilenMailEkleris.ToList()

                        }).First();
            var dataK = (from s in db.GonderilenMailKullanicilars
                         orderby s.Kullanicilar.Ad, s.Kullanicilar.Soyad
                         where s.GonderilenMailID == GonderilenMailID
                         select new MailKullaniciBilgi
                         {
                             AdSoyad = s.Kullanicilar.Ad + " " + s.Kullanicilar.Soyad,
                             Email = s.Email
                         }).ToList();
            ViewBag.DataK = dataK;
            return View(data);
        }

        public ActionResult Gonder(int? id, List<int> KullaniciID, string EKD)
        {
            var model = new GonderilenMailler();
            var MmMessage = new MmMessage();
            string _EnstituKod = Management.getSelectedEnstitu(EKD);
            ViewBag.MmMessage = MmMessage;

            var dataK = (from s in db.GonderilenMailKullanicilars
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
            KullaniciID = KullaniciID ?? new List<int>();
            db.Kullanicilars.Where(p => KullaniciID.Contains(p.KullaniciID)).ToList().ForEach((k) => { eList.Add(new CmbIntDto { Value = k.KullaniciID, Caption = k.EMail }); });

            ViewBag.MailSablonlariID = new SelectList(Management.cmbMailSablonlari(_EnstituKod, true, false), "Value", "Caption");
            ViewBag.EmailList = eList;
            return View(model);
        }
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Gonder(GonderilenMailler kModel, List<string> DosyaEkiAdi, List<HttpPostedFileBase> DosyaEki, List<int?> DuyuruDosyaEkID, List<int> KullaniciIDs, string EKD, string Alici = "")
        {
            var MmMessage = new MmMessage();
            string _EnstituKod = Management.getSelectedEnstitu(EKD);
            DuyuruDosyaEkID = DuyuruDosyaEkID == null ? new List<int?>() : DuyuruDosyaEkID;
            DosyaEki = DosyaEki == null ? new List<HttpPostedFileBase>() : DosyaEki;
            DosyaEkiAdi = DosyaEkiAdi == null ? new List<string>() : DosyaEkiAdi;
            KullaniciIDs = KullaniciIDs == null ? new List<int>() : KullaniciIDs;
            var secilenAlicilar = new List<string>();
            if (Alici.IsNullOrWhiteSpace() == false) Alici.Split(',').ToList().ForEach((itm) => { secilenAlicilar.Add(itm); });
            var qDosyaEkAdi = DosyaEkiAdi.Select((s, inx) => new { s, inx }).ToList();
            var qDosyaEki = DosyaEki.Select((s, inx) => new { s, inx }).ToList();
            var qDuyuruDosyaEkID = DuyuruDosyaEkID.Select((s, inx) => new { s, inx }).ToList();
            var qDosyalar = (from EkGirilenAd in qDosyaEkAdi
                             join EklenenEk in qDosyaEki on EkGirilenAd.inx equals EklenenEk.inx
                             select new
                             {
                                 EkGirilenAd.inx,
                                 DosyaEkAdi = EkGirilenAd.s,
                                 Dosya = EklenenEk.s,
                                 mDosyaAdi = EkGirilenAd + EklenenEk.s.FileName.GetFileExtension(),
                                 DosyaYolu = "/MailDosyalari/" + EkGirilenAd.s.ToFileNameAddGuid(EklenenEk.s.FileName.GetFileExtension())
                             }).ToList();

            var qVarolanlar = (from s in qDosyaEkAdi
                               join sid in qDuyuruDosyaEkID on s.inx equals sid.inx
                               select new { s.inx, DosyaEkAdi = s.s, DuyuruDosyaEkID = sid.s });
            #region Kontrol
            kModel.Tarih = DateTime.Now;

            if (kModel.Konu.IsNullOrWhiteSpace())
            {
                string msg = "Konu Giriniz.";
                MmMessage.Messages.Add(msg);
                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "Konu" });
            }
            else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "Konu" });

            if (kModel.Aciklama.IsNullOrWhiteSpace() && kModel.AciklamaHtml.IsNullOrWhiteSpace())
            {
                string msg = "İçerik Giriniz.";
                MmMessage.Messages.Add(msg);
            }


            #endregion
            if (MmMessage.Messages.Count == 0)
            {
                kModel.IslemTarihi = DateTime.Now;
                kModel.EnstituKod = _EnstituKod;
                kModel.IslemYapanID = UserIdentity.Current.Id;
                kModel.IslemYapanIP = UserIdentity.Ip;
                kModel.Aciklama = kModel.Aciklama ?? "";
                var eklenen = db.GonderilenMaillers.Add(kModel);

                foreach (var item in qDosyalar)
                {
                    item.Dosya.SaveAs(Server.MapPath("~" + item.DosyaYolu));
                    db.GonderilenMailEkleris.Add(new GonderilenMailEkleri
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
                    var dataqx = (from s in db.Kullanicilars
                                  where qscIDs.Contains(s.KullaniciID)
                                  select new
                                  {
                                      Email = s.EMail,
                                      GonderilenMailID = eklenen.GonderilenMailID,
                                      KullaniciID = s.KullaniciID
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
                mailList = db.GonderilenMailKullanicilars.AddRange(mailList).ToList();
                db.SaveChanges();
                var attach = new List<Attachment>();

                foreach (var item in qDosyalar)
                {
                    var ekTamYol = Server.MapPath("~" + item.DosyaYolu);
                    var FExtension = Path.GetExtension(ekTamYol);
                    attach.Add(new Attachment(new MemoryStream(System.IO.File.ReadAllBytes(ekTamYol)), item.mDosyaAdi.ToSetNameFileExtension(FExtension), MediaTypeNames.Application.Octet));
                }
                MailManager.sendMail(eklenen.GonderilenMailID, kModel.Konu, kModel.AciklamaHtml, mailList.Select(s => new MailSendList { EMail = s.Email, ToOrBcc = true }).ToList(), attach);
                return RedirectToAction("Index");
            }
            else
            {
                MessageBox.Show("Uyarı", MessageBox.MessageType.Warning, MmMessage.Messages.ToArray());
            }
            ViewBag.MmMessage = MmMessage;


            var qKullanicilar = from k in db.Kullanicilars
                                join bi in db.Birimlers on k.BirimID equals bi.BirimID
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
                if (KullaniciIDs.Contains(item.KullaniciID)) item.Checked = true;
            }

            ViewBag.Kullanicilar = kul;
            ViewBag.Alici = Alici;
            ViewBag.MailSablonlariID = new SelectList(Management.cmbMailSablonlari(_EnstituKod, true, false), "Value", "Caption");
            return View(kModel);
        }

        public ActionResult SecilenKullaniciCount(string PersonelTipID, string BirimID, string UnvanID)
        {
            var qx = (from s in db.Kullanicilars
                      join b in db.Birimlers on s.BirimID equals b.BirimID into def1
                      from def2 in def1.DefaultIfEmpty()
                      where s.EMail.Contains("@")
                      select new { s.KullaniciID, s.KullaniciTipID, s.UnvanID, s.BirimID, UstBirimID = def2 == null ? (int?)null : def2.UstBirimID });
            var toplamCount = qx.Count();
            if (PersonelTipID != "")
            {
                var KullaniciTipIDs = new List<int>();
                PersonelTipID.Split(',').ToList().ForEach((itm) => { KullaniciTipIDs.Add(itm.ToInt().Value); });
                qx = qx.Where(s => KullaniciTipIDs.Contains(s.KullaniciTipID));
            }

            if (BirimID != "")
            {
                var BirimIDs = new List<int>();
                BirimID.Split(',').ToList().ForEach((itm) => { BirimIDs.Add(itm.ToInt().Value); });
                qx = qx.Where(s => s.BirimID.HasValue && (BirimIDs.Contains(s.BirimID.Value) || BirimIDs.Contains(s.UstBirimID ?? -1)));
                //UnvanIDs.Contains(s.UnvanID) && PersonelTipIDs.Contains(s.PersonelTipID)
            }
            if (UnvanID != "")
            {
                var UnvanIDs = new List<int>();
                UnvanID.Split(',').ToList().ForEach((itm) => { UnvanIDs.Add(itm.ToInt().Value); });
                qx = qx.Where(s => s.UnvanID.HasValue && UnvanIDs.Contains(s.UnvanID.Value));

            }


            var dataqx = qx.Count();
            var jsonWal = new { Toplam = toplamCount, Secilen = dataqx };
            return jsonWal.toJsonResult();

        }
        public ActionResult TekrarGonder(int id)
        {
            var gm = db.GonderilenMaillers.Where(p => p.GonderilenMailID == id).FirstOrDefault();
            var EMailList = gm.GonderilenMailKullanicilars.Select(s => new MailSendList { EMail = s.Email, ToOrBcc = true }).ToList();
            var gEk = gm.GonderilenMailEkleris.ToList();
            var attach = new List<Attachment>();
            foreach (var item in gEk)
            {
                var ekTamYol = Server.MapPath("~" + item.EkDosyaYolu);
                var FExtension = Path.GetExtension(ekTamYol);
                attach.Add(new Attachment(new MemoryStream(System.IO.File.ReadAllBytes(ekTamYol)), item.EkAdi.ToSetNameFileExtension(FExtension), MediaTypeNames.Application.Octet));

            }
            MailManager.sendMail(gm.GonderilenMailID, gm.Konu, gm.AciklamaHtml, EMailList, attach);
            return true.toJsonResult();
        }



        public ActionResult Sil(int id)
        {
            var kayit = db.GonderilenMaillers.Where(p => p.GonderilenMailID == id).FirstOrDefault();
            string message = "";
            bool success = true;
            if (kayit != null)
            {
                try
                {

                    if (message == "")
                    {
                        kayit.Silindi = true;
                        db.SaveChanges();
                        message = "'" + kayit.Konu + "' konulu email Silindi!";
                    }

                }
                catch (Exception ex)
                {
                    success = false;
                    message = "'" + kayit.Konu + "' Konulu Mail Silinemedi! <br/> Bilgi:" + ex.ToExceptionMessage();
                    Management.SistemBilgisiKaydet(message, "MailGonder/Sil<br/><br/>" + ex.ToExceptionStackTrace(), BilgiTipi.OnemsizHata);
                }
            }
            else
            {
                success = false;
                message = "Silmek istediğiniz mail bilgisi sistemde bulunamadı!";
            }
            return Json(new { success = success, message = message }, "application/json", JsonRequestBehavior.AllowGet);
        }

    }
}
