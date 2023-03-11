using BiskaUtil;
using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.MenuAndRoles;
using LisansUstuBasvuruSistemi.Utilities.SystemSetting;
using System;
using System.Data.Entity.SqlServer;
using System.Linq;
using System.Web.Mvc;
using LisansUstuBasvuruSistemi.Business;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.Helpers;

namespace LisansUstuBasvuruSistemi.Controllers
{
    [Authorize(Roles = RoleNames.SrGelenTalepler)]
    public class SrGelenTaleplerController : Controller
    {
        private readonly LisansustuBasvuruSistemiEntities _entities = new LisansustuBasvuruSistemiEntities();
        public ActionResult Index(string ekd)
        {
            return Index(new FmTalepler { }, ekd);
        }
        [HttpPost]
        public ActionResult Index(FmTalepler model, string ekd)
        {
            
            var enstituKod = EnstituBus.GetSelectedEnstitu(ekd);




            var q = from s in _entities.SRTalepleris
                    join tt in _entities.SRTalepTipleris on s.SRTalepTipID equals tt.SRTalepTipID
                    join e in _entities.Enstitulers on s.EnstituKod equals e.EnstituKod
                    join k in _entities.Kullanicilars on s.TalepYapanID equals k.KullaniciID
                    join kt in _entities.KullaniciTipleris on k.KullaniciTipID equals kt.KullaniciTipID
                    join salx in _entities.SRSalonlars on s.SRSalonID equals salx.SRSalonID into def1
                    from sal in def1.DefaultIfEmpty()
                    join hg in _entities.HaftaGunleris on s.HaftaGunID equals hg.HaftaGunID
                    join d in _entities.SRDurumlaris on s.SRDurumID equals d.SRDurumID
                    join ot in _entities.OgrenimTipleris on new { s.EnstituKod, s.Kullanicilar.OgrenimTipKod } equals new { ot.EnstituKod, OgrenimTipKod = (int?)ot.OgrenimTipKod } into defOl
                    from Ot in defOl.DefaultIfEmpty()
                    join otl in _entities.OgrenimTipleris on Ot.OgrenimTipID equals otl.OgrenimTipID into def2
                    from defOt in def2.DefaultIfEmpty()
                    where s.EnstituKod == enstituKod
                    select new
                    {
                        s.SRTalepID,
                        s.MezuniyetBasvurulariID,
                        e.EnstituKod,
                        EnstituAdi = e.EnstituAd,
                        s.TalepYapanID,
                        tt.TalepTipAdi,
                        s.SRTalepTipID,
                        k.OgrenciNo,
                        k.SicilNo,
                        TalepYapan = k.Ad + " " + k.Soyad,
                        k.ResimAdi,
                        OgrenimTipAdi = defOt != null ? defOt.OgrenimTipAdi : "",
                        kt.KullaniciTipAdi,
                        s.SRSalonID,
                        SalonAdi = s.SRSalonID.HasValue ? sal.SalonAdi : s.SalonAdi,
                        s.Tarih,
                        s.HaftaGunID,
                        hg.HaftaGunAdi,
                        s.BasSaat,
                        s.BitSaat,
                        s.DanismanAdi,
                        s.EsDanismanAdi, 
                        s.IsOnline,
                        s.TezOzeti,
                        s.SRDurumID,
                        d.DurumAdi,
                        DurumListeAdi = d.DurumAdi,
                        d.ClassName,
                        d.Color,
                        s.SRDurumAciklamasi,
                        s.IslemTarihi,
                        s.IslemYapanID,
                        s.IslemYapanIP,
                        OrderInx = SqlFunctions.DateAdd("day", 0, (s.Tarih + " " + s.BasSaat)).Value > DateTime.Now ? (s.SRDurumID == SRTalepDurum.TalepEdildi ? 0 : (s.SRDurumID == SRTalepDurum.Onaylandı ? 1 : 2)) : (s.SRDurumID == SRTalepDurum.TalepEdildi ? 3 : (s.SRDurumID == SRTalepDurum.Onaylandı ? 4 : 5))
                    };
            // if (!model.EnstituKod.IsNullOrWhiteSpace()) q = q.Where(p => p.EnstituKod == model.EnstituKod);
            if (model.SRSalonID.HasValue) q = q.Where(p => p.SRSalonID == model.SRSalonID.Value);
            if (model.SRDurumID.HasValue) q = q.Where(p => p.SRDurumID == model.SRDurumID.Value);
            if (model.SRTalepTipID.HasValue) q = q.Where(p => p.SRTalepTipID == model.SRTalepTipID.Value);
            if (!model.Aciklama.IsNullOrWhiteSpace()) q = q.Where(p => p.TalepYapan.Contains(model.Aciklama));
            model.RowCount = q.Count();
            q = !model.Sort.IsNullOrWhiteSpace() ? q.OrderBy(model.Sort) : q.OrderBy(o => o.OrderInx).ThenByDescending(t => t.Tarih).ThenByDescending(t => t.BasSaat).ThenByDescending(t => t.IslemTarihi);
            var indexModel = new MIndexBilgi();
            var btDurulari = SrTalepleriBus.GetSrDurumList();
            foreach (var item in btDurulari)
            {
                var tipCount = q.Count(p => p.SRDurumID == item.SRDurumID);
                indexModel.ListB.Add(new mxRowModel { Key = item.DurumAdi, ClassName = item.ClassName, Color = item.Color, Toplam = tipCount });
            }
            indexModel.Toplam = model.RowCount; 
            model.data = q.Select(s => new FrTalepler
            { 
                SRTalepID = s.SRTalepID,
                EnstituKod = s.EnstituKod,
                EnstituAdi = s.EnstituAdi,
                MezuniyetBasvurulariID = s.MezuniyetBasvurulariID,
                TalepYapanID = s.TalepYapanID,
                TalepTipAdi = s.TalepTipAdi,
                SRTalepTipID = s.SRTalepTipID,
                OgrenciNo = s.OgrenciNo,
                SicilNo = s.SicilNo,
                TalepYapan = s.TalepYapan,
                ResimAdi = s.ResimAdi,
                OgrenimTipAdi = s.OgrenimTipAdi,
                KullaniciTipAdi = s.KullaniciTipAdi,
                SRSalonID = s.SRSalonID,
                SalonAdi = s.SalonAdi,
                Tarih = s.Tarih,
                HaftaGunID = s.HaftaGunID,
                HaftaGunAdi = s.HaftaGunAdi,
                BasSaat = s.BasSaat,
                BitSaat = s.BitSaat, 
                DanismanAdi = s.DanismanAdi,
                EsDanismanAdi = s.EsDanismanAdi,
                TezOzeti = s.TezOzeti,
                SRDurumID = s.SRDurumID,
                DurumAdi = s.DurumAdi,
                DurumListeAdi = s.DurumListeAdi,
                ClassName = s.ClassName,
                Color = s.Color,
                SRDurumAciklamasi = s.SRDurumAciklamasi,
                IsOnline=s.IsOnline,
                IslemTarihi = s.IslemTarihi,
                IslemYapanID = s.IslemYapanID,
                IslemYapanIP = s.IslemYapanIP
            }).Skip(model.StartRowIndex).Take(model.PageSize).ToArray();
           
            ViewBag.IndexModel = indexModel;
            ViewBag.SRTalepTipID = new SelectList(SrTalepleriBus.GetCmbSrTalepTipleri( true), "Value", "Caption", model.SRTalepTipID);
            ViewBag.SRSalonID = new SelectList(SrTalepleriBus.GetCmbSalonlar(enstituKod ,true), "Value", "Caption", model.SRSalonID);
            ViewBag.SRDurumID = new SelectList(SrTalepleriBus.GetCmbSrDurumListe( true), "Value", "Caption", model.SRDurumID);
            return View(model);
        }
        [Authorize(Roles = RoleNames.SrTalepSil)]
        public ActionResult Sil(int id)
        {
            var mmMessage = new MmMessage();
            //var mmMessage = Management.getBasvuruSilKontrol(id);

            //if (mmMmMessage.IsSuccess)
            //{
            var kayit = _entities.SRTalepleris.FirstOrDefault(p => p.SRTalepID == id);
            var basvuruBilgi = kayit.Kullanicilar.Ad + " " + "" + kayit.Kullanicilar.Soyad + " Kullanıcısına ait <br/>" + kayit.Tarih.ToShortDateString() + " " + kayit.BasSaat + "-" + kayit.BitSaat + " tarihli rezervasyon talebi<br/>";
            try
            {
                mmMessage.Messages.Add(basvuruBilgi + "sistemden Silindi!");
                mmMessage.Title = "Bilgilendirme";
                _entities.SRTalepleris.Remove(kayit);
                _entities.SaveChanges();
                mmMessage.IsSuccess = true;
                mmMessage.MessageType = Msgtype.Success;
            }
            catch (Exception ex)
            {
                mmMessage.MessageType = Msgtype.Error;
                mmMessage.IsSuccess = false;
                mmMessage.Messages.Add(basvuruBilgi + "silinemedi!");
                mmMessage.Title = "Hata";
                SistemBilgilendirmeBus.SistemBilgisiKaydet(basvuruBilgi + " Bilgi:" + ex.ToExceptionMessage(), "SRGelenTalepler/Sil<br/><br/>" + ex.ToExceptionStackTrace(), LogType.OnemsizHata);
            }

            //}
            var strView = ViewRenderHelper.RenderPartialView("Ajax", "getMessage", mmMessage);
            return Json(new { IsSuccess = mmMessage.IsSuccess, Messages = strView }, "application/json", JsonRequestBehavior.AllowGet);
        }
       
        [Authorize(Roles = RoleNames.SrTalepDuzelt)]
      //  [ValidateInput(false)]
        public ActionResult Istenenkaydet(int id, int srDurumId, string srDurumAciklamasi)
        {
            string strView = "";
            string fWeight = "font-weight:";
            
            var talep = _entities.SRTalepleris.First(p => p.SRTalepID == id);
            fWeight += Convert.ToDateTime(talep.Tarih.ToShortDateString() + " " + talep.BasSaat) > DateTime.Now ? "bold;" : "normal;";
            var enstituKod = talep.EnstituKod;
            bool sendMailJuri = false;
            bool sendMailTalepYapan = false;
            var sendMailAna = SrAyar.SrIslemlerindeMailGonder.GetAyarSr(enstituKod).ToBoolean().Value;

            bool save = false;
            if (srDurumId == SRTalepDurum.Onaylandı)
            {

                var qTalepEslesen = _entities.SRTalepleris.Where(a => a.SRTalepID != talep.SRTalepID && a.SRSalonID == talep.SRSalonID && a.Tarih == talep.Tarih &&
                                        (
                                          (a.BasSaat == talep.BasSaat || a.BitSaat == talep.BitSaat) ||
                                        (
                                            (a.BasSaat < talep.BasSaat && a.BitSaat > talep.BasSaat) || a.BasSaat < talep.BitSaat && a.BitSaat > talep.BitSaat) ||
                                            (a.BasSaat > talep.BasSaat && a.BasSaat < talep.BitSaat) || a.BitSaat > talep.BasSaat && a.BitSaat < talep.BitSaat)
                                        ).ToList();
                if (talep.SRSalonID.HasValue && qTalepEslesen.Any(p => p.SRDurumID == SRTalepDurum.Onaylandı))
                {
                    var salon = _entities.SRSalonlars.First(p => p.SRSalonID == talep.SRSalonID);
                    string msg = talep.Tarih.ToShortDateString() + " " + talep.BasSaat.ToString() + " - " + talep.BitSaat.ToString() + " Tarihi için '" + salon.SalonAdi + "' Salonu doludur bu rezervasyon onaylanamaz!";
                    var mmMessage = new MmMessage();
                    mmMessage.Messages.Add(msg);
                    mmMessage.IsSuccess = false;
                    mmMessage.MessageType = Msgtype.Error;
                    strView = ViewRenderHelper.RenderPartialView("Ajax", "getMessage", mmMessage);
                }
                else
                {
                    save = true;
                    sendMailTalepYapan = true;
                    sendMailJuri = talep.SRDurumID != srDurumId;

                }
            }
            else if (srDurumId == SRTalepDurum.Reddedildi)
            {
                sendMailTalepYapan = talep.SRDurumID != srDurumId;
                sendMailJuri = sendMailTalepYapan;
                talep.SRDurumAciklamasi = srDurumAciklamasi;
                save = true;
            }
            else save = true;

            if (save)
            {
                talep.SRDurumID = srDurumId;
                talep.IslemTarihi = DateTime.Now;
                talep.IslemYapanID = UserIdentity.Current.Id;
                talep.IslemYapanIP = UserIdentity.Ip;
                _entities.SaveChanges();
            }
            var qbDrm = talep.SRDurumlari;
            if (sendMailAna && sendMailTalepYapan && !talep.SRTalepTipleri.IsTezSinavi)
            {
                #region SendMail

                var enstLng = _entities.Enstitulers.First(p => p.EnstituKod == enstituKod);

                var salon = _entities.SRSalonlars.First(p => p.SRSalonID == talep.SRSalonID);
                var juriler = _entities.SRTaleplerJuris.Where(p => p.SRTalepID == talep.SRTalepID).ToList();
                var haftaGunu = _entities.HaftaGunleris.First(p => p.HaftaGunID == talep.HaftaGunID);
                var mmmC = new MailMainContentDto();
                var enstituAdi = _entities.Enstitulers.First(p => p.EnstituKod == enstituKod).EnstituAd;
                
                mmmC.EnstituAdi = enstituAdi;
                mmmC.UniversiteAdi = "Yıldız Teknik Üniversitesi";
                var mailBilgi = EnstituMailInfo.GetEnstituMailBilgisi(enstituKod);

                var mdl = new MailTableContentDto
                {
                    AciklamaBasligi = srDurumId == SRTalepDurum.Reddedildi ? "Salon rezervasyon talebi işleminiz kabul edilmemiştir." : "Salon rezervasyon talebi işleminiz onaylanmıştır",
                    AciklamaTextAlingCenter = true
                };
                if (srDurumId == SRTalepDurum.Reddedildi) mdl.AciklamaDetayi = "Kabul edilmeme nedeni:" + talep.SRDurumAciklamasi;
                mdl.GrupBasligi = "Rezervasyon talep detaylarınız";
                mdl.Detaylar.Add(new MailTableRowDto { Baslik = "Salon Adı", Aciklama = salon.SalonAdi });
                mdl.Detaylar.Add(new MailTableRowDto { Baslik = "Tarih", Aciklama = talep.Tarih.ToString("dd.MM.yyyyy") + " " + haftaGunu.HaftaGunAdi });
                mdl.Detaylar.Add(new MailTableRowDto { Baslik = "Saat", Aciklama = $"{talep.BasSaat:hh\\:mm}" + "-" + $"{talep.BitSaat:hh\\:mm}"
                });
                if (talep.SRTalepTipleri.IsTezSinavi)
                {
                    var tezBasligiTr = "";
                    if (talep.IsTezBasligiDegisti == true) tezBasligiTr = talep.YeniTezBaslikTr;
                    else if (talep.MezuniyetBasvurulari.MezuniyetJuriOneriFormlaris.First().IsTezBasligiDegisti == true)
                        tezBasligiTr = talep.MezuniyetBasvurulari.MezuniyetJuriOneriFormlaris.First().YeniTezBaslikTr;
                    else tezBasligiTr=talep.MezuniyetBasvurulari.TezBaslikTr;


                    mdl.Detaylar.Add(new MailTableRowDto { Baslik = "Tez Başlığı Türkçe", Aciklama = tezBasligiTr });
                    mdl.Detaylar.Add(new MailTableRowDto { Baslik = "Danışman Adı", Aciklama = talep.DanismanAdi });
                    if (talep.EsDanismanAdi.IsNullOrWhiteSpace() == false) mdl.Detaylar.Add(new MailTableRowDto { Baslik = "Eş Danışman Adı", Aciklama = talep.EsDanismanAdi });
                    if (talep.TezOzeti.IsNullOrWhiteSpace() == false) mdl.Detaylar.Add(new MailTableRowDto { Baslik = "Tez Özeti", Aciklama = talep.TezOzetiHtml });

                    var mtcSinavJ = new MailTableContentDto
                    {
                        IsJuriBilgi = false,
                        GrupBasligi = "juri Bilgisi"
                    };

                    foreach (var itemJr in juriler.Select((s, inx) => new { s, inx }).ToList())
                    {
                        mtcSinavJ.Detaylar.Add(new MailTableRowDto { SiraNo = (itemJr.inx + 1), Baslik = itemJr.s.JuriAdi, Aciklama = (itemJr.s.Telefon + " (" + itemJr.s.Email + ")"), });
                    }
                    mdl.Detaylar.Add(new MailTableRowDto
                    {
                        Colspan2 = true,
                        Aciklama = ViewRenderHelper.RenderPartialView("Ajax", "getMailTableContent", mtcSinavJ)
                    });
                }
                else
                {
                    mdl.Detaylar.Add(new MailTableRowDto { Baslik = "Açıklama", Aciklama = talep.Aciklama });
                }
                string content = ViewRenderHelper.RenderPartialView("Ajax", "getMailTableContent", mdl);
                mmmC.Content = content;
                string htmlMail = ViewRenderHelper.RenderPartialView("Ajax", "getMailContent", mmmC);
                var snded = MailManager.SendMail(mailBilgi.EnstituKod, enstituAdi, htmlMail, talep.Kullanicilar.EMail, null);
                if (snded)
                {

                    var kModel = new GonderilenMailler
                    {
                        Tarih = DateTime.Now,
                        EnstituKod = enstituAdi
                    };

                    kModel.EnstituKod = mailBilgi.EnstituKod;
                    kModel.MesajID = null;
                    kModel.IslemTarihi = DateTime.Now;
                    kModel.Konu = "Salon Rezervasyonu: " + talep.Kullanicilar.Ad + " " + talep.Kullanicilar.Soyad;
                    kModel.IslemYapanID = UserIdentity.Current.Id;
                    kModel.IslemYapanIP = UserIdentity.Ip;
                    kModel.Aciklama = "";
                    kModel.AciklamaHtml = htmlMail ?? "";
                    kModel.Gonderildi = true;
                    var eklenen = _entities.GonderilenMaillers.Add(kModel);
                    _entities.SaveChanges();
                    _entities.GonderilenMailKullanicilars.Add(new GonderilenMailKullanicilar { Email = talep.Kullanicilar.EMail, GonderilenMailID = kModel.GonderilenMailID, KullaniciID = talep.TalepYapanID });
                    eklenen.Gonderildi = true;
                    _entities.SaveChanges();
                }
                #endregion
            }
            if (sendMailAna && sendMailJuri && talep.SRTalepTipleri.IsTezSinavi)
            {
                var msgs = MezuniyetBus.SendMailMezuniyetSinavYerBilgisi(id, srDurumId == SRTalepDurum.Onaylandı);
                if (msgs.Messages.Count > 0)
                {
                    strView = ViewRenderHelper.RenderPartialView("Ajax", "getMessage", msgs);
                }
            }
            return new
            {
                IslemTipListeAdi = qbDrm.DurumAdi,
                qbDrm.ClassName,
                qbDrm.Color,
                FontWeight = fWeight,
                strView
            }.ToJsonResult();
        }



    }
}