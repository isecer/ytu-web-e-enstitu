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

namespace LisansUstuBasvuruSistemi.Controllers
{
    [Authorize(Roles = RoleNames.SRGelenTalepler)]
    public class SRGelenTaleplerController : Controller
    {
        private LisansustuBasvuruSistemiEntities db = new LisansustuBasvuruSistemiEntities();
        public ActionResult Index(string EKD)
        {
            return Index(new fmTalepler { }, EKD);
        }
        [HttpPost]
        public ActionResult Index(fmTalepler model, string EKD)
        {
            
            var _EnstituKod = Management.getSelectedEnstitu(EKD);




            var q = from s in db.SRTalepleris
                    join tt in db.SRTalepTipleris on s.SRTalepTipID equals tt.SRTalepTipID
                    join e in db.Enstitulers on s.EnstituKod equals e.EnstituKod
                    join k in db.Kullanicilars on s.TalepYapanID equals k.KullaniciID
                    join kt in db.KullaniciTipleris on k.KullaniciTipID equals kt.KullaniciTipID
                    join salx in db.SRSalonlars on s.SRSalonID equals salx.SRSalonID into def1
                    from sal in def1.DefaultIfEmpty()
                    join hg in db.HaftaGunleris on s.HaftaGunID equals hg.HaftaGunID
                    join d in db.SRDurumlaris on s.SRDurumID equals d.SRDurumID
                    join ot in db.OgrenimTipleris on new { s.EnstituKod, s.Kullanicilar.OgrenimTipKod } equals new { ot.EnstituKod, OgrenimTipKod = (int?)ot.OgrenimTipKod } into defOl
                    from Ot in defOl.DefaultIfEmpty()
                    join otl in db.OgrenimTipleris on Ot.OgrenimTipID equals otl.OgrenimTipID into def2
                    from defOt in def2.DefaultIfEmpty()
                    where s.EnstituKod == _EnstituKod
                    select new
                    {

                        SRTalepID = s.SRTalepID,
                        s.MezuniyetBasvurulariID,
                        EnstituKod = e.EnstituKod,
                        EnstituAdi = e.EnstituAd,
                        TalepYapanID = s.TalepYapanID,
                        TalepTipAdi = tt.TalepTipAdi,
                        SRTalepTipID = s.SRTalepTipID,
                        OgrenciNo = k.OgrenciNo,
                        SicilNo = k.SicilNo,
                        TalepYapan = k.Ad + " " + k.Soyad,
                        ResimAdi = k.ResimAdi,
                        OgrenimTipAdi = defOt != null ? defOt.OgrenimTipAdi : "",
                        KullaniciTipAdi = kt.KullaniciTipAdi,
                        SRSalonID = s.SRSalonID,
                        SalonAdi = s.SRSalonID.HasValue ? sal.SalonAdi : s.SalonAdi,
                        Tarih = s.Tarih,
                        HaftaGunID = s.HaftaGunID,
                        HaftaGunAdi = hg.HaftaGunAdi,
                        BasSaat = s.BasSaat,
                        BitSaat = s.BitSaat, 
                        DanismanAdi = s.DanismanAdi,
                        EsDanismanAdi = s.EsDanismanAdi, 
                        s.IsOnline,
                        TezOzeti = s.TezOzeti,
                        SRDurumID = s.SRDurumID,
                        DurumAdi = d.DurumAdi,
                        DurumListeAdi = d.DurumAdi,
                        ClassName = d.ClassName,
                        Color = d.Color,
                        SRDurumAciklamasi = s.SRDurumAciklamasi,
                        IslemTarihi = s.IslemTarihi,
                        IslemYapanID = s.IslemYapanID,
                        IslemYapanIP = s.IslemYapanIP,
                        OrderInx = SqlFunctions.DateAdd("day", 0, (s.Tarih + " " + s.BasSaat)).Value > DateTime.Now ? (s.SRDurumID == SRTalepDurum.TalepEdildi ? 0 : (s.SRDurumID == SRTalepDurum.Onaylandı ? 1 : 2)) : (s.SRDurumID == SRTalepDurum.TalepEdildi ? 3 : (s.SRDurumID == SRTalepDurum.Onaylandı ? 4 : 5))
                    };
            // if (!model.EnstituKod.IsNullOrWhiteSpace()) q = q.Where(p => p.EnstituKod == model.EnstituKod);
            if (model.SRSalonID.HasValue) q = q.Where(p => p.SRSalonID == model.SRSalonID.Value);
            if (model.SRDurumID.HasValue) q = q.Where(p => p.SRDurumID == model.SRDurumID.Value);
            if (model.SRTalepTipID.HasValue) q = q.Where(p => p.SRTalepTipID == model.SRTalepTipID.Value);
            if (!model.Aciklama.IsNullOrWhiteSpace()) q = q.Where(p => p.TalepYapan.Contains(model.Aciklama));
            model.RowCount = q.Count();
            if (!model.Sort.IsNullOrWhiteSpace()) q = q.OrderBy(model.Sort);
            else q = q.OrderBy(o => o.OrderInx).ThenByDescending(t => t.Tarih).ThenByDescending(t => t.BasSaat).ThenByDescending(t => t.IslemTarihi);
            var IndexModel = new MIndexBilgi();
            var btDurulari = Management.SRDurumList();
            foreach (var item in btDurulari)
            {
                var tipCount = q.Where(p => p.SRDurumID == item.SRDurumID).Count();
                IndexModel.ListB.Add(new mxRowModel { Key = item.DurumAdi, ClassName = item.ClassName, Color = item.Color, Toplam = tipCount });
            }
            IndexModel.Toplam = model.RowCount;
            var PS = Management.setStartRowInx(model.StartRowIndex, model.PageIndex, model.PageCount, model.RowCount, model.PageSize);
            model.PageIndex = PS.PageIndex;
            model.data = q.Select(s => new frTalepler
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
            }).Skip(PS.StartRowIndex).Take(model.PageSize).ToArray();
           
            ViewBag.IndexModel = IndexModel;
            ViewBag.SRTalepTipID = new SelectList(Management.cmbSRTalepTipleri( true), "Value", "Caption", model.SRTalepTipID);
            ViewBag.SRSalonID = new SelectList(Management.cmbSalonlar(_EnstituKod ,true), "Value", "Caption", model.SRSalonID);
            ViewBag.SRDurumID = new SelectList(Management.cmbSRDurumListe( true), "Value", "Caption", model.SRDurumID);
            return View(model);
        }
        [Authorize(Roles = RoleNames.SRTalepSil)]
        public ActionResult Sil(int id)
        {
            var mmMessage = new MmMessage();
            //var mmMessage = Management.getBasvuruSilKontrol(id);

            //if (mmMmMessage.IsSuccess)
            //{
            var kayit = db.SRTalepleris.Where(p => p.SRTalepID == id).FirstOrDefault();
            var basvuruBilgi = kayit.Kullanicilar.Ad + " " + "" + kayit.Kullanicilar.Soyad + " Kullanıcısına ait <br/>" + kayit.Tarih.ToShortDateString() + " " + kayit.BasSaat + "-" + kayit.BitSaat + " tarihli rezervasyon talebi<br/>";
            try
            {
                mmMessage.Messages.Add(basvuruBilgi + "sistemden Silindi!");
                mmMessage.Title = "Bilgilendirme";
                db.SRTalepleris.Remove(kayit);
                db.SaveChanges();
                mmMessage.IsSuccess = true;
                mmMessage.MessageType = Msgtype.Success;
            }
            catch (Exception ex)
            {
                mmMessage.MessageType = Msgtype.Error;
                mmMessage.IsSuccess = false;
                mmMessage.Messages.Add(basvuruBilgi + "silinemedi!");
                mmMessage.Title = "Hata";
                Management.SistemBilgisiKaydet(basvuruBilgi + " Bilgi:" + ex.ToExceptionMessage(), "SRGelenTalepler/Sil<br/><br/>" + ex.ToExceptionStackTrace(), BilgiTipi.OnemsizHata);
            }

            //}
            var strView = Management.RenderPartialView("Ajax", "getMessage", mmMessage);
            return Json(new { IsSuccess = mmMessage.IsSuccess, Messages = strView }, "application/json", JsonRequestBehavior.AllowGet);
        }
       
        [Authorize(Roles = RoleNames.SRTalepDuzelt)]
      //  [ValidateInput(false)]
        public ActionResult Istenenkaydet(int id, int SRDurumID, string SRDurumAciklamasi)
        {
            string strView = "";
            string fWeight = "font-weight:";
            
            var talep = db.SRTalepleris.Where(p => p.SRTalepID == id).First();
            fWeight += Convert.ToDateTime(talep.Tarih.ToShortDateString() + " " + talep.BasSaat) > DateTime.Now ? "bold;" : "normal;";
            var _EnstituKod = talep.EnstituKod;
            bool sendMailJuri = false;
            bool sendMailTalepYapan = false;
            var sendMailAna = SRAyar.getAyarSR(SRAyar.SRIslemlerindeMailGonder, _EnstituKod).ToBoolean().Value;

            bool save = false;
            if (SRDurumID == SRTalepDurum.Onaylandı)
            {

                var qTalepEslesen = db.SRTalepleris.Where(a => a.SRTalepID != talep.SRTalepID && a.SRSalonID == talep.SRSalonID && a.Tarih == talep.Tarih &&
                                        (
                                          (a.BasSaat == talep.BasSaat || a.BitSaat == talep.BitSaat) ||
                                        (
                                            (a.BasSaat < talep.BasSaat && a.BitSaat > talep.BasSaat) || a.BasSaat < talep.BitSaat && a.BitSaat > talep.BitSaat) ||
                                            (a.BasSaat > talep.BasSaat && a.BasSaat < talep.BitSaat) || a.BitSaat > talep.BasSaat && a.BitSaat < talep.BitSaat)
                                        ).ToList();
                if (talep.SRSalonID.HasValue && qTalepEslesen.Any(p => p.SRDurumID == SRTalepDurum.Onaylandı))
                {
                    var salon = db.SRSalonlars.Where(p => p.SRSalonID == talep.SRSalonID ).First();
                    string msg = talep.Tarih.ToShortDateString() + " " + talep.BasSaat.ToString() + " - " + talep.BitSaat.ToString() + " Tarihi için '" + salon.SalonAdi + "' Salonu doludur bu rezervasyon onaylanamaz!";
                    var mmMessage = new MmMessage();
                    mmMessage.Messages.Add(msg);
                    mmMessage.IsSuccess = false;
                    mmMessage.MessageType = Msgtype.Error;
                    strView = Management.RenderPartialView("Ajax", "getMessage", mmMessage);
                }
                else
                {
                    save = true;
                    sendMailTalepYapan = true;
                    sendMailJuri = talep.SRDurumID != SRDurumID;

                }
            }
            else if (SRDurumID == SRTalepDurum.Reddedildi)
            {
                sendMailTalepYapan = talep.SRDurumID != SRDurumID;
                sendMailJuri = sendMailTalepYapan;
                talep.SRDurumAciklamasi = SRDurumAciklamasi;
                save = true;
            }
            else save = true;

            if (save)
            {
                talep.SRDurumID = SRDurumID;
                talep.IslemTarihi = DateTime.Now;
                talep.IslemYapanID = UserIdentity.Current.Id;
                talep.IslemYapanIP = UserIdentity.Ip;
                db.SaveChanges();
            }
            var qbDrm = talep.SRDurumlari;
            if (sendMailAna && sendMailTalepYapan && !talep.SRTalepTipleri.IsTezSinavi)
            {
                #region SendMail

                var enstLng = db.Enstitulers.Where(p => p.EnstituKod == _EnstituKod ).First();

                var salon = db.SRSalonlars.Where(p =>  p.SRSalonID == talep.SRSalonID).First();
                var juriler = db.SRTaleplerJuris.Where(p => p.SRTalepID == talep.SRTalepID).ToList();
                var haftaGunu = db.HaftaGunleris.Where(p =>  p.HaftaGunID == talep.HaftaGunID).First();
                var kullanıcı = db.Kullanicilars.Where(p => p.KullaniciID == talep.TalepYapanID).First();
                var mmmC = new mdlMailMainContent();
                var enstituAdi = db.Enstitulers.Where(p => p.EnstituKod == _EnstituKod ).First().EnstituAd;
                
                mmmC.EnstituAdi = enstituAdi;
                mmmC.UniversiteAdi = "Yıldız Teknik Üniversitesi";
                var mailBilgi = EnstituMailInfo.GetEnstituMailBilgisi(_EnstituKod);

                var mdl = new mailTableContent();
                mdl.AciklamaBasligi = SRDurumID == SRTalepDurum.Reddedildi ? "Salon rezervasyon talebi işleminiz kabul edilmemiştir." : "Salon rezervasyon talebi işleminiz onaylanmıştır";
                mdl.AciklamaTextAlingCenter = true;
                if (SRDurumID == SRTalepDurum.Reddedildi) mdl.AciklamaDetayi = "Kabul edilmeme nedeni:" + talep.SRDurumAciklamasi;
                mdl.GrupBasligi = "Rezervasyon talep detaylarınız";
                mdl.Detaylar.Add(new mailTableRow { Baslik = "Salon Adı", Aciklama = salon.SalonAdi });
                mdl.Detaylar.Add(new mailTableRow { Baslik = "Tarih", Aciklama = talep.Tarih.ToString("dd.MM.yyyyy") + " " + haftaGunu.HaftaGunAdi });
                mdl.Detaylar.Add(new mailTableRow { Baslik = "Saat", Aciklama = string.Format("{0:hh\\:mm}", talep.BasSaat) + "-" + string.Format("{0:hh\\:mm}", talep.BitSaat) });
                if (talep.SRTalepTipleri.IsTezSinavi)
                {
                    var tezBasligiTr = "";
                    if (talep.IsTezBasligiDegisti == true) tezBasligiTr = talep.YeniTezBaslikTr;
                    else if (talep.MezuniyetBasvurulari.MezuniyetJuriOneriFormlaris.First().IsTezBasligiDegisti == true)
                        tezBasligiTr = talep.MezuniyetBasvurulari.MezuniyetJuriOneriFormlaris.First().YeniTezBaslikTr;
                    else tezBasligiTr=talep.MezuniyetBasvurulari.TezBaslikTr;


                    mdl.Detaylar.Add(new mailTableRow { Baslik = "Tez Başlığı Türkçe", Aciklama = tezBasligiTr });
                    mdl.Detaylar.Add(new mailTableRow { Baslik = "Danışman Adı", Aciklama = talep.DanismanAdi });
                    if (talep.EsDanismanAdi.IsNullOrWhiteSpace() == false) mdl.Detaylar.Add(new mailTableRow { Baslik = "Eş Danışman Adı", Aciklama = talep.EsDanismanAdi });
                    if (talep.TezOzeti.IsNullOrWhiteSpace() == false) mdl.Detaylar.Add(new mailTableRow { Baslik = "Tez Özeti", Aciklama = talep.TezOzetiHtml });

                    var mtcSinavJ = new mailTableContent();
                    mtcSinavJ.IsJuriBilgi = false;
                    mtcSinavJ.GrupBasligi = "juri Bilgisi";
                    
                    foreach (var itemJr in juriler.Select((s, inx) => new { s, inx }).ToList())
                    {
                        mtcSinavJ.Detaylar.Add(new mailTableRow { SiraNo = (itemJr.inx + 1), Baslik = itemJr.s.JuriAdi, Aciklama = (itemJr.s.Telefon + " (" + itemJr.s.Email + ")"), });
                    }
                    mdl.Detaylar.Add(new mailTableRow
                    {
                        Colspan2 = true,
                        Aciklama = Management.RenderPartialView("Ajax", "getMailTableContent", mtcSinavJ)
                    });
                }
                else
                {
                    mdl.Detaylar.Add(new mailTableRow { Baslik = "Açıklama", Aciklama = talep.Aciklama });
                }
                string content = Management.RenderPartialView("Ajax", "getMailTableContent", mdl);
                mmmC.Content = content;
                string htmlMail = Management.RenderPartialView("Ajax", "getMailContent", mmmC);
                var User = mailBilgi.SmtpKullaniciAdi;
                var snded = MailManager.sendMail(mailBilgi.EnstituKod, enstituAdi, htmlMail, talep.Kullanicilar.EMail, null);
                if (snded)
                {

                    var kModel = new GonderilenMailler();
                    kModel.Tarih = DateTime.Now;
                    kModel.EnstituKod = enstituAdi;

                    kModel.EnstituKod = mailBilgi.EnstituKod;
                    kModel.MesajID = null;
                    kModel.IslemTarihi = DateTime.Now;
                    kModel.Konu = "Salon Rezervasyonu: " + talep.Kullanicilar.Ad + " " + talep.Kullanicilar.Soyad;
                    kModel.IslemYapanID = UserIdentity.Current.Id;
                    kModel.IslemYapanIP = UserIdentity.Ip;
                    kModel.Aciklama = "";
                    kModel.AciklamaHtml = htmlMail ?? "";
                    kModel.Gonderildi = true;
                    var eklenen = db.GonderilenMaillers.Add(kModel);
                    db.SaveChanges();
                    db.GonderilenMailKullanicilars.Add(new GonderilenMailKullanicilar { Email = talep.Kullanicilar.EMail, GonderilenMailID = kModel.GonderilenMailID, KullaniciID = talep.TalepYapanID });
                    eklenen.Gonderildi = true;
                    db.SaveChanges();
                }
                #endregion
            }
            if (sendMailAna && sendMailJuri && talep.SRTalepTipleri.IsTezSinavi)
            {
                var msgs = Management.sendMailMezuniyetSinavYerBilgisi(id, SRDurumID == SRTalepDurum.Onaylandı);
                if (msgs.Messages.Count > 0)
                {
                    strView = Management.RenderPartialView("Ajax", "getMessage", msgs);
                }
            }
            return new
            {
                IslemTipListeAdi = qbDrm.DurumAdi,
                ClassName = qbDrm.ClassName,
                Color = qbDrm.Color,
                FontWeight = fWeight,
                strView = strView
            }.toJsonResult();
        }



    }
}