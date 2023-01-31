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

namespace LisansUstuBasvuruSistemi.Controllers
{
    [System.Web.Mvc.OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    [Authorize(Roles = RoleNames.MezuniyetSureci)]
    public class MezuniyetSureciController : Controller
    {
        private LisansustuBasvuruSistemiEntities db = new LisansustuBasvuruSistemiEntities();
        public ActionResult Index()
        {

            return Index(new fmMezuniyetSureci() { PageSize = 15 });
        }
        [HttpPost]
        public ActionResult Index(fmMezuniyetSureci model)
        {
            var EnstKods = UserIdentity.Current.EnstituKods ?? new List<string>();
           
            var q = from s in db.MezuniyetSurecis
                    join e in db.Enstitulers on new { s.EnstituKod} equals new { e.EnstituKod}
                    join d in db.Donemlers on new { s.DonemID} equals new { d.DonemID}
                    join k in db.Kullanicilars on s.IslemYapanID equals k.KullaniciID
                    where EnstKods.Contains(e.EnstituKod)
                    select new
                    {
                        s.EnstituKod,
                        e.EnstituAd,
                        s.BaslangicYil,
                        s.BitisYil,
                        s.DonemID,
                        d.DonemAdi,
                        s.SiraNo,
                        s.MezuniyetSurecID,
                        s.BaslangicTarihi,
                        s.BitisTarihi,
                        s.IsAktif,
                        s.IslemTarihi,
                        s.IslemYapanID,
                        IslemYapan = k.Ad + " " + k.Soyad,
                        s.IslemYapanIP

                    };

            if (!model.EnstituKod.IsNullOrWhiteSpace()) q = q.Where(p => p.EnstituKod == model.EnstituKod);

            model.RowCount = q.Count();
            var IndexModel = new MIndexBilgi();
            IndexModel.Toplam = model.RowCount;
            if (!model.Sort.IsNullOrWhiteSpace()) q = q.OrderBy(model.Sort);
            else q = q.OrderByDescending(o => o.BaslangicTarihi);
            var qdata = q.Skip(model.StartRowIndex).Take(model.PageSize).Select(s => new frMezuniyetSureci
            {
                EnstituKod = s.EnstituKod,
                EnstituAdi = s.EnstituAd,
                BaslangicYil = s.BaslangicYil,
                BitisYil = s.BitisYil,
                DonemID = s.DonemID,
                DonemAdi = s.DonemAdi,
                SiraNo = s.SiraNo,
                MezuniyetSurecID = s.MezuniyetSurecID,
                BaslangicTarihi = s.BaslangicTarihi,
                BitisTarihi = s.BitisTarihi,
                IsAktif = s.IsAktif,
                IslemTarihi = s.IslemTarihi,
                IslemYapanID = s.IslemYapanID,
                IslemYapan = s.IslemYapan,
                IslemYapanIP = s.IslemYapanIP
            }).ToList();

            model.Data = qdata;
            ViewBag.EnstituKod = new SelectList(EnstituBus.GetCmbAktifEnstituler(true), "Value", "Caption", model.EnstituKod);
            ViewBag.IndexModel = IndexModel;

           
            return View(model);
        }
        [Authorize(Roles = RoleNames.MezuniyetSureciKayıt)]
        public ActionResult Kayit(int? id, string dlgid, string EKD)
        {
            string _EnstituKod = EnstituBus.GetSelectedEnstitu(EKD);
            var MmMessage = new MmMessage();
            MmMessage.IsDialog = !dlgid.IsNullOrWhiteSpace();
            MmMessage.DialogID = dlgid;
            ViewBag.MmMessage = MmMessage;
            var model = new kmMezuniyetSureci();

            model.IsAktif = true;
            var eoY = DateTime.Now.ToEgitimOgretimYilBilgi();
            model.OgretimYili = eoY.BaslangicYili + "/" + eoY.BitisYili + "/" + eoY.Donem; 
            var mzMList = Management.getZmMailZamanData(!id.HasValue || id <= 0);
            if (id.HasValue && id > 0)
            {
                var data = db.MezuniyetSurecis.Where(p => p.MezuniyetSurecID == id).FirstOrDefault();

                var bsmailData = data.MezuniyetSurecOtoMails.ToList();
                foreach (var item in mzMList)
                {
                    var bsm = bsmailData.Where(p => p.ZamanTipID == item.ZamanTipID && p.MailSablonTipID == item.MailSablonTipID && p.Zaman == item.Zaman).FirstOrDefault();
                    if (bsm != null)
                    {
                        item.Checked = true;
                        item.Zaman = bsm.Zaman;
                        item.ZamanTipID = bsm.ZamanTipID;
                        item.MezuniyetSurecOtoMailID = bsm.MezuniyetSurecOtoMailID;
                        item.Gonderildi = bsm.Gonderildi;
                        item.GonderilenCount = bsm.GonderilenCount;
                    }

                }

                if (data != null)
                {
                    model.MezuniyetSurecID = id.Value;
                    model.EnstituKod = data.EnstituKod;
                    model.BaslangicYil = data.BaslangicYil;
                    model.BitisYil = data.BitisYil;
                    model.BaslangicTarihi = data.BaslangicTarihi;
                    model.BitisTarihi = data.BitisTarihi;
                    model.DonemID = data.DonemID;
                    model.IsAktif = data.IsAktif;
                    model.AnketID = data.AnketID;
                    model.OgretimYili = data.BaslangicYil + "/" + data.BitisYil + "/" + data.DonemID;
                }

            }
            model.OgrenimTipModel = MezuniyetBus.GetMezuniyetOgrenimTipKriterleri(_EnstituKod, model.MezuniyetSurecID);
            ViewBag.EnstituKod = new SelectList(EnstituBus.GetCmbAktifEnstituler(true), "Value", "Caption", model.EnstituKod ?? _EnstituKod);
            ViewBag.OgretimYili = new SelectList(DonemlerBus.GetCmbAkademikTarih(), "Value", "Caption", model.OgretimYili);
            ViewBag.OgrenimTipleri = Management.cmbAktifOgrenimTipleri(_EnstituKod, false, true);
            ViewBag.AnketID = new SelectList(Management.cmbGetAktifAnketler(_EnstituKod, true, model.AnketID), "Value", "Caption", model.AnketID);
            ViewBag.kmMzOtoMail = mzMList;
            return View(model);
        }
        [HttpPost]
        [Authorize(Roles = RoleNames.MezuniyetSureciKayıt)]
        public ActionResult Kayit(kmMezuniyetSureci kModel, bool? IsYonetmelikKopyala, string dlgid = "")
        {
            var MmMessage = new MmMessage();
            MmMessage.IsDialog = !dlgid.IsNullOrWhiteSpace();
            MmMessage.DialogID = dlgid;


            var qgID = kModel.gID.Select((s, inx) => new { Key = s, Inx = inx }).ToList();
            var qZamanTipID = kModel.ZamanTipID.Select((s, inx) => new { gID = s.Split('_')[0].ToInt().Value, Key = s.Split('_')[1].ToInt(), Inx = inx }).ToList();
            var qMailSablonTipID = kModel.MailSablonTipID.Select((s, inx) => new { gID = s.Split('_')[0].ToInt().Value, Key = s.Split('_')[1].ToInt(), Inx = inx }).ToList();
            var qZaman = kModel.Zaman.Select((s, inx) => new { gID = s.Split('_')[0].ToInt().Value, Key = s.Split('_')[1], Inx = inx }).ToList();
            var qMailZamanlari = (from s in qgID
                                  join z in qZamanTipID on s.Key equals z.gID
                                  join tt in qMailSablonTipID on s.Key equals tt.gID
                                  join za in qZaman on s.Key equals za.gID
                                  select new kmMzOtoMail
                                  {
                                      ZamanTipID = z.Key.Value,
                                      Zaman = za.Key.ToInt().Value,
                                      MailSablonTipID = tt.Key,
                                  }).ToList();

            var _MezuniyetSureciOgrenimTipKriterID = kModel.MezuniyetSureciOgrenimTipKriterID.Select((s, inx) => new { Inx = inx, MezuniyetSureciOgrenimTipKriterID = s }).ToList();
            var _OgrenimTipID = kModel.OgrenimTipID.Select((s, inx) => new { Inx = inx, OgrenimTipID = s }).ToList();
            var _OgrenimTipKod = kModel.OgrenimTipKod.Select((s, inx) => new { Inx = inx, OgrenimTipKod = s }).ToList();
            var _MBasvuruSonDonemKaydiKontrolEdilecekDersKodlari = kModel.MBasvuruSonDonemKaydiKontrolEdilecekDersKodlari.Select((s, inx) => new { Inx = inx, MBasvuruSonDonemKaydiKontrolEdilecekDersKodlari = s }).ToList();
            var _MBasvuruToplamKrediKriteri = kModel.MBasvuruToplamKrediKriteri.Select((s, inx) => new { Inx = inx, MBasvuruToplamKrediKriteri = s }).ToList();
            var _MBasvuruAGNOKriteri = kModel.MBasvuruAGNOKriteri.Select((s, inx) => new { Inx = inx, MBasvuruAGNOKriteri = s }).ToList();
            var _MBasvuruAKTSKriteri = kModel.MBasvuruAKTSKriteri.Select((s, inx) => new { Inx = inx, MBasvuruAKTSKriteri = s }).ToList();
            var _MBSinavUzatmaSuresiGun = kModel.MBSinavUzatmaSuresiGun.Select((s, inx) => new { Inx = inx, MBSinavUzatmaSuresiGun = s }).ToList();
            var _MBTezTeslimSuresiGun = kModel.MBTezTeslimSuresiGun.Select((s, inx) => new { Inx = inx, MBTezTeslimSuresiGun = s }).ToList();
            var _MBSRTalebiKacGunSonraAlabilir = kModel.MBSRTalebiKacGunSonraAlabilir.Select((s, inx) => new { Inx = inx, MBSRTalebiKacGunSonraAlabilir = s }).ToList();

            var OgrenimTipleriLngs = db.OgrenimTipleris.Where(p => p.EnstituKod == kModel.EnstituKod ).ToList();
            var MezuniyetSureciOgrenimTipKriterleri = (from Kr in _MezuniyetSureciOgrenimTipKriterID
                                                       join Ot in _OgrenimTipID on Kr.Inx equals Ot.Inx
                                                       join Otk in _OgrenimTipKod on Kr.Inx equals Otk.Inx
                                                       join Dk in _MBasvuruSonDonemKaydiKontrolEdilecekDersKodlari on Kr.Inx equals Dk.Inx
                                                       join Kk in _MBasvuruToplamKrediKriteri on Kr.Inx equals Kk.Inx
                                                       join Agk in _MBasvuruAGNOKriteri on Kr.Inx equals Agk.Inx
                                                       join Akts in _MBasvuruAKTSKriteri on Kr.Inx equals Akts.Inx
                                                       join Uzs in _MBSinavUzatmaSuresiGun on Kr.Inx equals Uzs.Inx
                                                       join Tts in _MBTezTeslimSuresiGun on Kr.Inx equals Tts.Inx
                                                       join Srg in _MBSRTalebiKacGunSonraAlabilir on Kr.Inx equals Srg.Inx
                                                       join otl in OgrenimTipleriLngs on Ot.OgrenimTipID equals otl.OgrenimTipID
                                                       select new
                                                       {
                                                           Kr.Inx,
                                                           Kr.MezuniyetSureciOgrenimTipKriterID,
                                                           Ot.OgrenimTipID,
                                                           Otk.OgrenimTipKod,
                                                           Dk.MBasvuruSonDonemKaydiKontrolEdilecekDersKodlari,
                                                           Kk.MBasvuruToplamKrediKriteri,
                                                           Agk.MBasvuruAGNOKriteri,
                                                           Akts.MBasvuruAKTSKriteri,
                                                           Uzs.MBSinavUzatmaSuresiGun,
                                                           Tts.MBTezTeslimSuresiGun,
                                                           Srg.MBSRTalebiKacGunSonraAlabilir,
                                                           otl.OgrenimTipAdi,
                                                       }).ToList();


            #region Kontrol
            if (kModel.EnstituKod.IsNullOrWhiteSpace())
            {
                string msg = "Duyurunun Yayınlanacağı Enstitüyü Seçiniz";
                MmMessage.Messages.Add(msg);
                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "EnstituKod" });

            }
            else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "EnstituKod" });

            if (kModel.BaslangicTarihi == DateTime.MinValue || kModel.BitisTarihi == DateTime.MinValue)
            {
                if (kModel.BaslangicTarihi == DateTime.MinValue)
                {
                    string msg = "Geçerli Bir Başlangıç Tarih Giriniz.";
                    MmMessage.Messages.Add(msg);
                    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "BaslangicTarihi" });
                }
                else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "BaslangicTarihi" });
                if (kModel.BitisTarihi == DateTime.MinValue)
                {
                    string msg = "Geçerli Bir Bitiş Tarih Giriniz.";
                    MmMessage.Messages.Add(msg);
                    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "BitisTarihi" });
                }
                else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "BitisTarihi" });
            }
            else if (kModel.BaslangicTarihi >= kModel.BitisTarihi)
            {
                string msg = "Başlangıç tarihi bitiş tarihinden büyük olamaz!";
                MmMessage.Messages.Add(msg);
                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "BaslangicTarihi" });
                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "BitisTarihi" });
            }
            else
            {
                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "BaslangicTarihi" });
                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "BitisTarihi" });
            }

            var _EOyilBilgi = new EOyilBilgi();
            if (kModel.OgretimYili.IsNullOrWhiteSpace() == false)
            {
                var oy = kModel.OgretimYili.Split('/').ToList();
                _EOyilBilgi.BaslangicYili = oy[0].ToInt().Value;
                _EOyilBilgi.BitisYili = oy[1].ToInt().Value;
                _EOyilBilgi.Donem = oy[2].ToInt().Value;
                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "OgretimYili" });
            }
            else
            {

                string msg = "Öğretim yılı seçiniz";
                MmMessage.Messages.Add(msg);
                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "OgretimYili" });

            }

            if (MmMessage.Messages.Count == 0)
            {

                var qBasS = db.MezuniyetSurecis.Where(p => p.EnstituKod == kModel.EnstituKod && p.MezuniyetSurecID != kModel.MezuniyetSurecID &&
                    (
                     (p.BaslangicTarihi <= kModel.BaslangicTarihi && p.BitisTarihi >= kModel.BaslangicTarihi)
                       ||
                     (p.BaslangicTarihi <= kModel.BitisTarihi && p.BitisTarihi >= kModel.BitisTarihi)
                       ||
                     (kModel.BaslangicTarihi <= p.BaslangicTarihi && kModel.BitisTarihi >= p.BaslangicTarihi)
                       ||
                     (kModel.BaslangicTarihi <= p.BitisTarihi && kModel.BitisTarihi >= p.BitisTarihi)
                    )).Count();
                if (qBasS > 0)
                {
                    string msg = "Girmiş olduğunuz tarihler için daha önceden mezuniyet süreci kayıt edilmiştir.";
                    MmMessage.Messages.Add(msg);
                    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "BaslangicTarihi" });
                    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "BitisTarihi" });
                }
            }
            if (MmMessage.Messages.Count == 0)
            {
                foreach (var item in MezuniyetSureciOgrenimTipKriterleri)
                {
                    if (!item.MBasvuruToplamKrediKriteri.HasValue || item.MBasvuruToplamKrediKriteri <= 0)
                    {
                        string msg = item.OgrenimTipAdi + " Öğrenim tipi için Min Kredi bilgisi 0 dan büyük olmalı.";
                        MmMessage.Messages.Add(msg);
                    }
                    if (!item.MBasvuruAGNOKriteri.HasValue || !(item.MBasvuruAGNOKriteri > 0 && item.MBasvuruAGNOKriteri <= 4))
                    {
                        string msg = item.OgrenimTipAdi + " Öğrenim tipi için Min Agno bilgisi 1 ile 4 arasında olmalı.";
                        MmMessage.Messages.Add(msg);
                    }
                    if (!item.MBasvuruAKTSKriteri.HasValue || item.MBasvuruAKTSKriteri <= 0)
                    {
                        string msg = item.OgrenimTipAdi + " Öğrenim tipi için Min Akts bilgisi 0 dan büyük olmalı.";
                        MmMessage.Messages.Add(msg);
                    }
                    if (!item.MBSinavUzatmaSuresiGun.HasValue || item.MBSinavUzatmaSuresiGun <= 0)
                    {
                        string msg = item.OgrenimTipAdi + " Öğrenim tipi için T.S.U.S bilgisi 0 dan büyük olmalı.";
                        MmMessage.Messages.Add(msg);
                    }
                    if (!item.MBTezTeslimSuresiGun.HasValue || item.MBTezTeslimSuresiGun <= 0)
                    {
                        string msg = item.OgrenimTipAdi + " Öğrenim tipi için T.T.S bilgisi 0 dan büyük olmalı.";
                        MmMessage.Messages.Add(msg);
                    }
                    if (!item.MBSRTalebiKacGunSonraAlabilir.HasValue || item.MBSRTalebiKacGunSonraAlabilir <= 0)
                    {
                        string msg = item.OgrenimTipAdi + " Öğrenim tipi için S.R.G bilgisi 0 dan büyük olmalı.";
                        MmMessage.Messages.Add(msg);
                    }
                }
            }

            #endregion
            if (MmMessage.Messages.Count == 0)
            {
                int OldBSurecID = kModel.MezuniyetSurecID;
                bool IsnewOrEdit = kModel.MezuniyetSurecID <= 0;
                kModel.IslemTarihi = DateTime.Now;
                kModel.IslemYapanID = UserIdentity.Current.Id;
                kModel.IslemYapanIP = UserIdentity.Ip;
                kModel.BaslangicYil = _EOyilBilgi.BaslangicYili;
                kModel.BitisYil = _EOyilBilgi.BitisYili;
                kModel.DonemID = _EOyilBilgi.Donem;

                if (kModel.MezuniyetSurecID <= 0)
                {
                    var eklenen = db.MezuniyetSurecis.Add(new MezuniyetSureci
                    {
                        EnstituKod = kModel.EnstituKod,
                        BaslangicYil = kModel.BaslangicYil,
                        BitisYil = kModel.BitisYil,
                        DonemID = kModel.DonemID,
                        BaslangicTarihi = kModel.BaslangicTarihi,
                        BitisTarihi = kModel.BitisTarihi,
                        AnketID = kModel.AnketID,
                        IsAktif = true,
                        IslemTarihi = kModel.IslemTarihi,
                        IslemYapanID = kModel.IslemYapanID,
                        IslemYapanIP = kModel.IslemYapanIP
                    });
                    db.SaveChanges();
                    kModel.MezuniyetSurecID = eklenen.MezuniyetSurecID;

                }
                else
                {
                    var data = db.MezuniyetSurecis.Where(p => p.MezuniyetSurecID == kModel.MezuniyetSurecID).First();
                    data.EnstituKod = kModel.EnstituKod;
                    data.BaslangicYil = kModel.BaslangicYil;
                    data.BitisYil = kModel.BitisYil;
                    data.DonemID = kModel.DonemID;
                    data.IsAktif = kModel.IsAktif;
                    data.BaslangicTarihi = kModel.BaslangicTarihi;
                    data.BitisTarihi = kModel.BitisTarihi;
                    data.AnketID = kModel.AnketID;
                    data.IslemTarihi = DateTime.Now;
                    data.IslemYapanID = kModel.IslemYapanID;
                    data.IslemYapanIP = kModel.IslemYapanIP;

                    db.MezuniyetSureciOgrenimTipKriterleris.RemoveRange(data.MezuniyetSureciOgrenimTipKriterleris);
                }

                var OtoMailList = db.MezuniyetSurecOtoMails.Where(p => p.MezuniyetSurecID == kModel.MezuniyetSurecID).ToList();
                var Silinecekler = OtoMailList.Where(p => p.Gonderildi == false && !qMailZamanlari.Any(a => a.ZamanTipID == p.ZamanTipID && a.MailSablonTipID == p.MailSablonTipID && a.Zaman == p.Zaman)).ToList();
                var Guncellenecekler = qMailZamanlari.Where(p => OtoMailList.Any(a => a.ZamanTipID == p.ZamanTipID && a.MailSablonTipID == p.MailSablonTipID && a.Zaman == p.Zaman)).ToList();
                var Eklenecekler = qMailZamanlari.Where(p => !OtoMailList.Any(a => a.ZamanTipID == p.ZamanTipID && a.MailSablonTipID == p.MailSablonTipID && a.Zaman == p.Zaman)).ToList();
                foreach (var item in Guncellenecekler)
                {
                    var qzaman = OtoMailList.Where(p => p.ZamanTipID == item.ZamanTipID && p.MailSablonTipID == item.MailSablonTipID && p.Zaman == item.Zaman).FirstOrDefault();
                    if (qzaman != null)
                    {
                        qzaman.ZamanTipID = item.ZamanTipID;
                        qzaman.Zaman = item.Zaman;
                        qzaman.MailSablonTipID = item.MailSablonTipID;

                    }
                }
                if (Silinecekler.Count > 0) db.MezuniyetSurecOtoMails.RemoveRange(Silinecekler);
                foreach (var item in Eklenecekler)
                {
                    db.MezuniyetSurecOtoMails.Add(new MezuniyetSurecOtoMail
                    {
                        MezuniyetSurecID = kModel.MezuniyetSurecID,
                        ZamanTipID = item.ZamanTipID,
                        MailSablonTipID = item.MailSablonTipID,
                        Zaman = item.Zaman
                    });

                }
                db.MezuniyetSureciOgrenimTipKriterleris.AddRange(MezuniyetSureciOgrenimTipKriterleri.Select(s => new Models.MezuniyetSureciOgrenimTipKriterleri
                {
                    MezuniyetSurecID = kModel.MezuniyetSurecID,
                    OgrenimTipID = s.OgrenimTipID.Value,
                    OgrenimTipKod = s.OgrenimTipKod.Value,
                    MBasvuruSonDonemKaydiKontrolEdilecekDersKodlari = s.MBasvuruSonDonemKaydiKontrolEdilecekDersKodlari,
                    MBasvuruToplamKrediKriteri = s.MBasvuruToplamKrediKriteri.Value,
                    MBasvuruAGNOKriteri = s.MBasvuruAGNOKriteri.Value,
                    MBasvuruAKTSKriteri = s.MBasvuruAKTSKriteri.Value,
                    MBSinavUzatmaSuresiGun = s.MBSinavUzatmaSuresiGun.Value,
                    MBTezTeslimSuresiGun = s.MBTezTeslimSuresiGun.Value,
                    MBSRTalebiKacGunSonraAlabilir = s.MBSRTalebiKacGunSonraAlabilir.Value,
                    IslemTarihi = DateTime.Now,
                    IslemYapanID = UserIdentity.Current.Id,
                    IslemYapanIP = UserIdentity.Ip


                }));
                db.SaveChanges();
                SiraNoVer();
                if (IsnewOrEdit || (IsYonetmelikKopyala.HasValue && IsYonetmelikKopyala.Value)) { YonetmelikKopyala(kModel.MezuniyetSurecID, kModel.EnstituKod); }

                return RedirectToAction("Index");
            }
            else
            {
                MessageBox.Show("Uyarı", MessageBox.MessageType.Warning, MmMessage.Messages.ToArray());
            }

            kModel.OgrenimTipModel = MezuniyetBus.GetMezuniyetOgrenimTipKriterleri(kModel.EnstituKod, kModel.MezuniyetSurecID);
            foreach (var item in kModel.OgrenimTipModel.OgrenimTipKriterList)
            {
                var sItem = MezuniyetSureciOgrenimTipKriterleri.Where(p => p.OgrenimTipID == item.OgrenimTipID).First();
                item.MBasvuruToplamKrediKriteri = sItem.MBasvuruToplamKrediKriteri ?? 0;
                item.MBasvuruAGNOKriteri = sItem.MBasvuruAGNOKriteri ?? 0;
                item.MBasvuruAKTSKriteri = sItem.MBasvuruAKTSKriteri ?? 0;
                item.MBSinavUzatmaSuresiGun = sItem.MBSinavUzatmaSuresiGun ?? 0;
                item.MBTezTeslimSuresiGun = sItem.MBTezTeslimSuresiGun ?? 0;
                item.MBSRTalebiKacGunSonraAlabilir = sItem.MBSRTalebiKacGunSonraAlabilir ?? 0;
            }
            var zmMList = Management.getZmMailZamanData();

            if (qMailZamanlari.Any())
                foreach (var item in zmMList)
                    item.Checked = qMailZamanlari.Any(a => a.MailSablonTipID == item.MailSablonTipID && a.Zaman == item.Zaman && a.ZamanTipID == item.ZamanTipID);

            ViewBag.kmMzOtoMail = zmMList;
            ViewBag.EnstituKod = new SelectList(EnstituBus.GetCmbAktifEnstituler(true), "Value", "Caption", kModel.EnstituKod);
            ViewBag.OgretimYili = new SelectList(DonemlerBus.GetCmbAkademikTarih(), "Value", "Caption", kModel.OgretimYili);
            ViewBag.OgrenimTipleri = Management.cmbAktifOgrenimTipleri(kModel.EnstituKod, false, true);
            ViewBag.AnketID = new SelectList(Management.cmbGetAktifAnketler(kModel.EnstituKod, true, kModel.AnketID), "Value", "Caption", kModel.AnketID);
            ViewBag.MmMessage = MmMessage;
            return View(kModel);
        }
        public void SiraNoVer()
        {
            var surecs = (from s in db.MezuniyetSurecis
                          group new { s.MezuniyetSurecID, s.BaslangicYil, s.BitisYil, s.BaslangicTarihi, s.BitisTarihi } by new { s.BaslangicYil, s.BitisYil, s.DonemID } into g1
                          select new
                          {
                              g1.Key.BaslangicYil,
                              g1.Key.DonemID,
                              data = g1.OrderBy(o => o.BaslangicTarihi).ToList()
                          }).ToList();
            foreach (var item in surecs)
            {
                int inx = 1;
                foreach (var item2 in item.data)
                {
                    var src = db.MezuniyetSurecis.Where(p => p.MezuniyetSurecID == item2.MezuniyetSurecID).First();
                    src.SiraNo = inx;
                    inx++;
                }
            }
            db.SaveChanges();
        }

        public ActionResult getOtBilgiM(string EnstituKod, int MezuniyetSurecID)
        {
            var model = MezuniyetBus.GetMezuniyetOgrenimTipKriterleri(EnstituKod, MezuniyetSurecID);
            return View(model);
        }
        public ActionResult getMsDetail(int id, int tbInx, bool IsDelete)
        {
            
            var mdl = (from s in db.MezuniyetSurecis.Where(p => p.MezuniyetSurecID == id)
                       join k in db.Kullanicilars on s.IslemYapanID equals k.KullaniciID
                       join e in db.Enstitulers on s.EnstituKod equals e.EnstituKod
                       join d in db.Donemlers on s.DonemID equals d.DonemID
                       select new msUrecDetay
                       {
                           MezuniyetSurecID = s.MezuniyetSurecID,
                           EnstituKod = s.EnstituKod,
                           EnstituAdi = e.EnstituAd,
                           BaslangicYil = s.BaslangicYil,
                           BitisYil = s.BitisYil,
                           DonemID = s.DonemID,
                           DonemAdi = d.DonemAdi,
                           BaslangicTarihi = s.BaslangicTarihi,
                           BitisTarihi = s.BitisTarihi,
                           IsAktif = s.IsAktif,
                           IslemTarihi = s.IslemTarihi,
                           IslemYapanID = s.IslemYapanID,
                           IslemYapan = (k.Ad + " " + k.Soyad),
                           IslemYapanIP = s.IslemYapanIP
                       }).First();

            ViewBag.IsDelete = IsDelete;
            mdl.SelectedTabIndex = tbInx;

            return View(mdl);
        }

        public ActionResult getYonetmelikBilgi(IEnumerable<frMezuniyetYonetmelikler> model)
        {

            return View(model);
        }


        public ActionResult getMsSubData(int id, int tbInx)
        {
            
            string page = "";

            if (tbInx == 1)
            {
                var mdl = (from s in db.MezuniyetSurecis.Where(p => p.MezuniyetSurecID == id)
                           join k in db.Kullanicilars on s.IslemYapanID equals k.KullaniciID
                           join e in db.Enstitulers on s.EnstituKod equals e.EnstituKod
                           join d in db.Donemlers on s.DonemID equals d.DonemID
                           select new msUrecDetay
                           {
                               MezuniyetSurecID = s.MezuniyetSurecID,
                               EnstituKod = s.EnstituKod,
                               EnstituAdi = e.EnstituAd,
                               BaslangicYil = s.BaslangicYil,
                               BitisYil = s.BitisYil,
                               DonemID = s.DonemID,
                               DonemAdi = d.DonemAdi,
                               BaslangicTarihi = s.BaslangicTarihi,
                               BitisTarihi = s.BitisTarihi,
                               IsAktif = s.IsAktif,
                               IslemTarihi = s.IslemTarihi,
                               IslemYapanID = s.IslemYapanID,
                               IslemYapan = (k.Ad + " " + k.Soyad),
                               IslemYapanIP = s.IslemYapanIP,
                               MezuniyetSureciOgrenimTipKriterleris = s.MezuniyetSureciOgrenimTipKriterleris
                           }).First();
                #region AnaBilgi
                var IndexModel = new MIndexBilgi();
                var btDurulari = MezuniyetBus.GetMezuniyetYayinDurumListe();
                foreach (var item in btDurulari)
                {
                    var tipCount = db.MezuniyetBasvurularis.Where(p => p.MezuniyetSurecID == mdl.MezuniyetSurecID && p.MezuniyetYayinKontrolDurumID == item.MezuniyetYayinKontrolDurumID).Count();
                    IndexModel.ListB.Add(new mxRowModel { ID = item.MezuniyetYayinKontrolDurumID, Key = item.MezuniyetYayinKontrolDurumAdi, ClassName = item.ClassName, Color = item.Color, Toplam = tipCount });
                }

                //var bdrmG = db.BasvuruDurumlaris.Where(p =>  p.BasvuruDurumID == BasvuruDurumu.Gonderildi).First();
                //IndexModel.ListB.Add(new mxRowModel { ID = bdrmG.BasvuruDurumID, Key = bdrmG.BasvuruDurumAdi, ClassName = bdrmG.BasvuruDurumlari.ClassName, Color = bdrmG.BasvuruDurumlari.Color, Toplam = db.MulakatSonuclaris.Where(p => p.BasvuruSurecID == id && p.KayitOldu == true).Count() });

                IndexModel.Toplam = IndexModel.ListB.Sum(s => s.Toplam);
                mdl.ToplamBasvuruBilgisi = IndexModel;

                #endregion
                page = ViewRenderHelper.RenderPartialView("MezuniyetSureci", "getMsDetAnaBilgi", mdl);
            }
            if (tbInx == 2)
            {
                #region Yonetmelikler
                var qData = (from s in db.MezuniyetSureciYonetmelikleris.Where(p => p.MezuniyetSurecID == id)
                             join e in db.Enstitulers on new { s.EnstituKod} equals new { e.EnstituKod}
                             join d in db.Donemlers on s.DonemID equals  d.DonemID
                             join d2 in db.Donemlers on  s.DonemIDB equals  d2.DonemID into def
                             from defD2 in def.DefaultIfEmpty()
                             join k in db.Kullanicilars on s.IslemYapanID equals k.KullaniciID
                             orderby s.BaslangicYil descending, s.DonemID descending
                             select new frMezuniyetYonetmelikler
                             {
                                 MezuniyetYonetmelikID = s.MezuniyetSureciYonetmelikID,
                                 EnstituKod = s.EnstituKod,
                                 EnstituAdi = e.EnstituAd,
                                 TarihKriterID = s.TarihKriterID,
                                 TarihKriterAdi = s.TarihKriterID == TarihKriterSecim.SecilenTarihAraligi ? "Seçilen Tarih Aralığı" : (s.TarihKriterID == TarihKriterSecim.SecilenTarihVeOncesi ? "Seçilen Tarih ve Öncesi" : "Seçilen Tarih ve Sonrası"),
                                 BaslangicYil = s.BaslangicYil,
                                 BitisYil = s.BitisYil,
                                 DonemID = s.DonemID,
                                 DonemAdi = d.DonemAdi,
                                 BaslangicYilB = s.BaslangicYilB,
                                 BitisYilB = s.BitisYilB,
                                 DonemIDB = s.DonemIDB,
                                 DonemAdiB = defD2 != null ? defD2.DonemAdi : "",
                                 MezuniyetYonetmelikData = (from mzs in db.MezuniyetSureciYonetmelikleriOTs.Where(p => p.MezuniyetSureciYonetmelikleri.MezuniyetSurecID == id && p.MezuniyetSureciYonetmelikID == s.MezuniyetSureciYonetmelikID)
                                                            join yt in db.MezuniyetYayinTurleris on mzs.MezuniyetYayinTurID equals yt.MezuniyetYayinTurID
                                                            join ot in db.OgrenimTipleris.Where(p => p.EnstituKod == s.EnstituKod ) on mzs.OgrenimTipKod equals ot.OgrenimTipKod
                                                            select new krMezuniyetYonetmelikOT
                                                            {
                                                                OgrenimTipKod = mzs.OgrenimTipKod,
                                                                OgrenimTipAdi = ot.OgrenimTipAdi,
                                                                MezuniyetYayinTurID = yt.MezuniyetYayinTurID,
                                                                MezuniyetYayinTurAdi = yt.MezuniyetYayinTurAdi,
                                                                IsGecerli = mzs.IsGecerli,
                                                                IsZorunlu = mzs.IsZorunlu,
                                                                GrupKodu = mzs.GrupKodu,
                                                                IsVeOrVeya = mzs.IsVeOrVeya
                                                            }).OrderBy(o => o.OgrenimTipAdi).ThenBy(t => t.MezuniyetYayinTurAdi).ToList()

                             }).ToList();

                #endregion
                page = ViewRenderHelper.RenderPartialView("MezuniyetSureci", "getYonetmelikBilgi", qData);
            }
            return Content(page, "text/html");
        }
        




        [Authorize(Roles = RoleNames.MezuniyetSureciKayıt)]
        public void YonetmelikKopyala(int mezuniyetSurecID, string EnstituKod)
        {

            var mbsstOld = db.MezuniyetSureciYayinTurleris.Where(p => p.MezuniyetSurecID == mezuniyetSurecID).ToList();
            db.MezuniyetSureciYayinTurleris.RemoveRange(mbsstOld);
            var yturs = db.MezuniyetYayinTurleris.ToList();

            foreach (var item in yturs)
            {
                var bsst = db.MezuniyetSureciYayinTurleris.Add(new MezuniyetSureciYayinTurleri
                {
                    MezuniyetSurecID = mezuniyetSurecID,
                    MezuniyetYayinTurID = item.MezuniyetYayinTurID,
                    TarihIstensin = item.TarihIstensin,
                    MezuniyetYayinBelgeTurID = item.MezuniyetYayinBelgeTurID,
                    BelgeZorunlu = item.MezuniyetYayinBelgeTurID.HasValue ? item.BelgeZorunlu : false,
                    KaynakMezuniyetYayinLinkTurID = item.KaynakMezuniyetYayinLinkTurID,
                    KaynakLinkiZorunlu = item.KaynakMezuniyetYayinLinkTurID.HasValue ? item.KaynakLinkiZorunlu : false,
                    MezuniyetYayinMetinTurID = item.MezuniyetYayinMetinTurID,
                    MetinZorunlu = item.MezuniyetYayinMetinTurID.HasValue ? item.MetinZorunlu : false,
                    YayinMezuniyetYayinLinkTurID = item.YayinMezuniyetYayinLinkTurID,
                    YayinLinkiZorunlu = item.YayinMezuniyetYayinLinkTurID.HasValue ? item.YayinLinkiZorunlu : false,
                    YayinIndexTurIstensin = item.YayinIndexTurIstensin,
                    YayinKabulEdilmisMakaleIstensin = item.YayinKabulEdilmisMakaleIstensin,

                    YayinDeatKurulusIstensin = item.YayinDeatKurulusIstensin,
                    YayinDergiAdiIstensin = item.YayinDergiAdiIstensin,
                    YayinMevcutDurumIstensin = item.YayinMevcutDurumIstensin,
                    YayinProjeTurIstensin = item.YayinProjeTurIstensin,
                    YayinProjeEkibiIstensin = item.YayinProjeEkibiIstensin,
                    YayinYazarlarIstensin = item.YayinYazarlarIstensin,
                    YayinYilCiltSayiIstensin = item.YayinYilCiltSayiIstensin,
                    IsTarihAraligiIstensin = item.IsTarihAraligiIstensin,
                    YayinEtkinlikAdiIstensin=item.YayinEtkinlikAdiIstensin,
                    YayinYerBilgisiIstensin=item.YayinYerBilgisiIstensin,
                    IsAktif = item.IsAktif,
                    IslemYapanID = UserIdentity.Current.Id,
                    IslemYapanIP = UserIdentity.Ip,
                    IslemTarihi = DateTime.Now

                });
            }


            var yonetmeliks = db.MezuniyetYonetmelikleris.Where(p => p.EnstituKod == EnstituKod && p.IsAktif).ToList();
            var oldY = db.MezuniyetSureciYonetmelikleris.Where(p => p.MezuniyetSurecID == mezuniyetSurecID).ToList();
            db.MezuniyetSureciYonetmelikleris.RemoveRange(oldY);

            foreach (var item in yonetmeliks)
            {
                var mznytAdd = db.MezuniyetSureciYonetmelikleris.Add(new MezuniyetSureciYonetmelikleri
                {
                    MezuniyetSurecID = mezuniyetSurecID,
                    EnstituKod = item.EnstituKod,
                    TarihKriterID = item.TarihKriterID,
                    BaslangicYil = item.BaslangicYil,
                    BitisYil = item.BitisYil,
                    DonemID = item.DonemID,
                    BaslangicYilB = item.BaslangicYilB,
                    BitisYilB = item.BitisYilB,
                    DonemIDB = item.DonemIDB,
                    IslemYapanID = UserIdentity.Current.Id,
                    IslemYapanIP = UserIdentity.Ip,
                    IslemTarihi = DateTime.Now
                });
                db.SaveChanges();
                foreach (var item2 in item.MezuniyetYonetmelikleriOTs)
                {
                    db.MezuniyetSureciYonetmelikleriOTs.Add(new MezuniyetSureciYonetmelikleriOT
                    {
                        MezuniyetSureciYonetmelikID = mznytAdd.MezuniyetSureciYonetmelikID,
                        OgrenimTipKod = item2.OgrenimTipKod,
                        MezuniyetYayinTurID = item2.MezuniyetYayinTurID,
                        IsGecerli = item2.IsGecerli,
                        IsZorunlu = item2.IsZorunlu,
                        GrupKodu = item2.GrupKodu,
                        IsVeOrVeya = item2.IsVeOrVeya

                    });
                }
            }
            db.SaveChanges();
        }




        [Authorize(Roles = RoleNames.MezuniyetSureciSil)]
        public ActionResult Sil(int id)
        {
            var mmMessage = new MmMessage();

            var kayit = db.MezuniyetSurecis.Where(p => p.MezuniyetSurecID == id).FirstOrDefault();

            string message = "";
            if (kayit != null)
            {
                var qBil = (from s in db.MezuniyetSurecis
                            join e in db.Enstitulers on new { s.EnstituKod} equals new { e.EnstituKod}
                            join d in db.Donemlers on new { s.DonemID} equals new { d.DonemID}
                            join k in db.Kullanicilars on s.IslemYapanID equals k.KullaniciID
                            where s.MezuniyetSurecID == id
                            select new
                            {
                                s.BaslangicYil,
                                s.BitisYil,
                                d.DonemAdi
                            }).First();
                try
                {
                    message = "'" + qBil.BaslangicYil + "/" + qBil.BitisYil + " " + qBil.DonemAdi + "' Dönemine ait mezuniyet süreci silindi!";
                    db.MezuniyetSurecis.Remove(kayit);
                    db.SaveChanges();
                    mmMessage.Title = "Uyarı";
                    mmMessage.Messages.Add(message);
                    mmMessage.MessageType = Msgtype.Success;
                    mmMessage.IsSuccess = true;
                }
                catch (Exception ex)
                {
                    message = "'" + qBil.BaslangicYil + "/" + qBil.BitisYil + " " + qBil.DonemAdi + "' Dönemine ait mezuniyet süreci silinirken bir hata oluştu! </br> Hata:" + ex.ToExceptionMessage();
                    Management.SistemBilgisiKaydet(message, "MezuniyetSureci/Sil<br/><br/>" + ex.ToExceptionStackTrace(), LogType.OnemsizHata);
                    mmMessage.Title = "Hata";
                    mmMessage.Messages.Add(message);
                    mmMessage.MessageType = Msgtype.Error;
                    mmMessage.IsSuccess = false;
                }
            }
            else
            {
                message = "Silmek istediğiniz mezuniyet süreci sistemde bulunamadı!";
                mmMessage.Title = "Hata";
                mmMessage.Messages.Add(message);
                mmMessage.MessageType = Msgtype.Error;
                mmMessage.IsSuccess = true;
            }
            var strView = ViewRenderHelper.RenderPartialView("Ajax", "getMessage", mmMessage);
            return Json(new { IsSuccess = mmMessage.IsSuccess, Messages = strView }, "application/json", JsonRequestBehavior.AllowGet);
        }
    }
}