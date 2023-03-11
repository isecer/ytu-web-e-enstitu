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
    [Authorize(Roles = RoleNames.SrOzelTanimlar)]
    public class SrOzelTanimlarController : Controller
    {
        private readonly LisansustuBasvuruSistemiEntities _entities = new LisansustuBasvuruSistemiEntities();
        public ActionResult Index()
        {
            return Index(new FmOzelTanimlar { });
        }
        [HttpPost]
        public ActionResult Index(FmOzelTanimlar model)
        {

            var q = from s in _entities.SROzelTanimlars
                    join ens in _entities.Enstitulers on s.EnstituKod equals ens.EnstituKod
                    join a in _entities.Aylars on s.Ay equals a.AyID into def1
                    from def in def1.DefaultIfEmpty()
                    join k in _entities.Kullanicilars on s.IslemYapanID equals k.KullaniciID
                    join ott in _entities.SROzelTanimTipleris on s.SROzelTanimTipID equals ott.SROzelTanimTipID
                    join sln in _entities.SRSalonlars on s.SRSalonID equals sln.SRSalonID into defs
                    from defSln in defs.DefaultIfEmpty()
                    join tt in _entities.SRTalepTipleris on s.SRTalepTipID equals tt.SRTalepTipID into deft
                    from defTt in deft.DefaultIfEmpty()

                    select new FrOzelTanimlar
                    {
                        SROzelTanimID = s.SROzelTanimID,
                        SROzelTanimTipID = s.SROzelTanimTipID,
                        SROzelTanimTipAdi = ott.SROzelTanimTipAdi,
                        TalepTipAdi = defTt != null ? defTt.TalepTipAdi : "",
                        EnstituKod = s.EnstituKod,
                        EnstituAdi = ens.EnstituAd,
                        SRSalonID = s.SRSalonID,
                        SalonAdi = s.SRSalonID.HasValue ? defSln.SalonAdi : "",
                        SROzelTanimSaatlers = s.SROzelTanimSaatlers.ToList(),
                        SROzelTanimGunlers = s.SROzelTanimGunlers.ToList(),
                        Tarih = s.Tarih,
                        Ay = s.Ay,
                        AyAdi = def != null ? def.AyAdi : "",
                        Gun = s.Gun,
                        BasTarih = s.BasTarih,
                        BitTarih = s.BitTarih,
                        Aciklama = s.Aciklama,
                        IsAktif = s.IsAktif,
                        IslemTarihi = s.IslemTarihi,
                        IslemYapanID = s.IslemYapanID,
                        IslemYapan = k.Ad + " " + k.Soyad,
                        IslemYapanIP = s.IslemYapanIP
                    };

            if (model.EnstituKod.IsNullOrWhiteSpace() == false) q = q.Where(p => p.EnstituKod == model.EnstituKod);
            if (model.SROzelTanimTipID.HasValue) q = q.Where(p => p.SROzelTanimTipID == model.SROzelTanimTipID);
            if (model.IsAktif.HasValue) q = q.Where(p => p.IsAktif == model.IsAktif);
            if (model.Aciklama.IsNullOrWhiteSpace() == false) q = q.Where(p => p.Aciklama == model.Aciklama);
            model.RowCount = q.Count();
            if (!model.Sort.IsNullOrWhiteSpace())
            {
                if (model.Sort.Contains("TTarih"))
                {
                    q = model.Sort.Contains("DESC") == false ? q.OrderBy(o => o.Tarih).ThenBy(t => t.SROzelTanimSaatlers.Min(s => s.BasSaat)).ThenBy(t => t.BasTarih).ThenBy(t => t.Ay).ThenBy(t => t.Gun) : q.OrderByDescending(o => o.Tarih).ThenByDescending(t => t.SROzelTanimSaatlers.Min(s => s.BasSaat)).ThenByDescending(t => t.BasTarih).ThenByDescending(t => t.Ay).ThenByDescending(t => t.Gun);
                }
                else
                {
                    q = q.OrderBy(model.Sort);
                }

            }
            else q = q.OrderBy(o => o.SROzelTanimTipID == SROzelTanimTip.ResmiTatilSabit ? 0 : o.SROzelTanimTipID); 
            model.FrOzelTanimlars = q.Skip(model.StartRowIndex).Take(model.PageSize).ToArray();  
            ViewBag.IsAktif = new SelectList(ComboData.GetCmbAktifPasifData(true), "Value", "Caption", model.IsAktif);
            ViewBag.EnstituKod = new SelectList(EnstituBus.GetCmbAktifEnstituler(true), "Value", "Caption", model.EnstituKod);
            ViewBag.TT = _entities.SRTalepTipleris.ToList();
            ViewBag.SROzelTanimTipID = new SelectList(SrTalepleriBus.GetCmbOzelTanimTipleri(true), "Value", "Caption", model.SROzelTanimTipID);
            ViewBag.HaftaGunleri = _entities.HaftaGunleris.ToList();
            return View(model);
        }
        public ActionResult Kayit(int? id, string dlgid)
        {
            var mmMessage = new MmMessage
            {
                IsDialog = !dlgid.IsNullOrWhiteSpace(),
                DialogID = dlgid
            };
            ViewBag.MmMessage = mmMessage;
           
            var model = new SROzelTanimlar
            {
                Tarih = DateTime.Now
            };
            var hGSecilenler = new List<int>();
            if (id.HasValue)
            {
                model = _entities.SROzelTanimlars.FirstOrDefault(p => p.SROzelTanimID == id);
                hGSecilenler = model.SROzelTanimGunlers.Select(s => s.HaftaGunID).ToList();

            }
            ViewBag.EnstituKod = new SelectList(EnstituBus.GetCmbYetkiliEnstituler( true), "Value", "Caption", model.EnstituKod);
            ViewBag.SROzelTanimTipID = new SelectList(SrTalepleriBus.GetCmbOzelTanimTipleri( true), "Value", "Caption", model.SROzelTanimTipID);
            ViewBag.SRSalonID = new SelectList(SrTalepleriBus.GetCmbSalonlar(model.EnstituKod, model.SRTalepTipID ?? 0 ,true), "Value", "Caption", model.SRSalonID);
            ViewBag.Ay = new SelectList(SrTalepleriBus.GetCmbAylar( true), "Value", "Caption", model.Ay);
            ViewBag.SRTalepTipID = new SelectList(SrTalepleriBus.GetCmbTalepTipleri( null, true), "Value", "Caption", model.SRTalepTipID);

            var hGunler = SrTalepleriBus.GetCmbHaftaGunleri(false);
            ViewBag.HaftaGunleri = hGunler;
            ViewBag.hGSecilenler = hGSecilenler;
            return View(model);
        }
        [HttpPost]
        public ActionResult Kayit(SROzelTanimlar kModel, List<TimeSpan?> basSaat, List<TimeSpan?> bitSaat, DateTime? basTarih2, DateTime? bitTarih2, List<int> haftaGunIDs, string oldId, string dlgid = "")
        {
            haftaGunIDs = haftaGunIDs ?? new List<int>();
            var mmMessage = new MmMessage
            {
                IsDialog = !dlgid.IsNullOrWhiteSpace(),
                DialogID = dlgid
            };

            var saatler = new List<SROzelTanimSaatler>();
            #region Kontrol 
            if (kModel.EnstituKod.IsNullOrWhiteSpace())
            { 
                mmMessage.Messages.Add("Enstitü seçiniz");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "EnstituKod" });
            }
            if (kModel.SROzelTanimTipID <= 0)
            { 
                mmMessage.Messages.Add("Özel tanım tipini seçiniz");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "SROzelTanimTipID" });
            }
            else if (kModel.SROzelTanimTipID == SROzelTanimTip.ResmiTatilSabit)
            {
                if (kModel.Ay.HasValue == false)
                { 
                    mmMessage.Messages.Add("Ay seçiniz");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "Ay" });
                }
                if (kModel.Gun.HasValue == false)
                { 
                    mmMessage.Messages.Add("Gün seçiniz");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "Gun" });
                }
            }
            else if (kModel.SROzelTanimTipID == SROzelTanimTip.ResmiTatilDegisen)
            {
                if (kModel.BasTarih.HasValue == false)
                { 
                    mmMessage.Messages.Add("Başlangıç Tarihi seçiniz");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "BasTarih" });
                }
                if (kModel.BitTarih.HasValue == false)
                { 
                    mmMessage.Messages.Add("Bitiş Tarihi seçiniz");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "BitTarih" });
                }
                if (kModel.BasTarih.HasValue && kModel.BitTarih.HasValue)
                {
                    if (kModel.BasTarih > kModel.BitTarih)
                    { 
                        mmMessage.Messages.Add("Başlangıç tarihi bitiş tarihinden büyük olamaz!");
                        mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "BasTarih" });
                        mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "BitTarih" });
                    }
                }
            }
            else if (kModel.SROzelTanimTipID == SROzelTanimTip.Rezervasyon)
            {
                if (kModel.SRTalepTipID.HasValue == false)
                { 
                    mmMessage.Messages.Add("Rezervasyon tipini seçiniz!");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "SRTalepTipID" });
                }
                else if (kModel.SRSalonID.HasValue == false)
                { 
                    mmMessage.Messages.Add("Salon seçiniz");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "SRSalonID" });
                }
                else if (kModel.Tarih.HasValue == false)
                { 
                    mmMessage.Messages.Add("Rezervasyon tarihini seçiniz");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "Tarih" });
                }
                else
                {

                    basSaat = basSaat ?? new List<TimeSpan?>();
                    bitSaat = bitSaat ?? new List<TimeSpan?>();

                    var qBasSaat = basSaat.Where(p => p.HasValue).Select((s, inx) => new { s, inx }).ToList();
                    var qBitSaat = bitSaat.Where(p => p.HasValue).Select((s, inx) => new { s, inx }).ToList();

                    saatler = (from qba in qBasSaat
                               join qbi in qBitSaat on qba.inx equals qbi.inx
                               select new SROzelTanimSaatler
                               {
                                   BasSaat = qba.s.Value,
                                   BitSaat = qbi.s.Value,
                               }).ToList();

                    if (saatler.Count == 0)
                    { 
                        mmMessage.Messages.Add("Rezervasyonun yapılabilmesi için en az 1 uygun saat seçmeniz gerekmektedir.");
                        mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "Tarih" });
                    }
                }
            }
            else if (kModel.SROzelTanimTipID == SROzelTanimTip.Rezerve)
            {
                if (kModel.SRTalepTipID.HasValue == false)
                { 
                    mmMessage.Messages.Add("Rezervasyon tipini seçiniz!");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "SRTalepTipID" });
                }
                else if (kModel.SRSalonID.HasValue == false)
                { 
                    mmMessage.Messages.Add("Salon seçiniz");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "SRSalonID" });
                }
                kModel.BasTarih = basTarih2;
                kModel.BitTarih = bitTarih2;
                if (kModel.BasTarih.HasValue == false)
                { 
                    mmMessage.Messages.Add("Başlangıç Tarihi Seçiniz");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "BasTarih2" });
                }
                if (kModel.BitTarih.HasValue == false)
                { 
                    mmMessage.Messages.Add("Bitiş Tarihi Seçiniz");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "BitTarih2" });
                }
                if (kModel.BasTarih.HasValue && kModel.BitTarih.HasValue)
                {
                    if (kModel.BasTarih > kModel.BitTarih)
                    { 
                        mmMessage.Messages.Add("Başlangıç tarihi bitiş tarihinden büyük olamaz!");
                        mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "BasTarih" });
                        mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "BitTarih" });
                    }

                }
                //if (HaftaGunIDs.Count == 0)
                //{
                //    string msg = "En az 1 hafta günü seçmeniz gerekmektedir.";
                //    MmMessage.Messages.Add(msg);
                //    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "HaftaGunID" });
                //}
            }
            if (mmMessage.Messages.Count == 0 && (kModel.SROzelTanimTipID == SROzelTanimTip.Rezervasyon || kModel.SROzelTanimTipID == SROzelTanimTip.Rezerve))
            {
                if (kModel.SROzelTanimTipID == SROzelTanimTip.Rezerve)
                {
                    var msg = SrTalepleriBus.SrKayitKontrol(kModel.SRSalonID.Value, kModel.SRTalepTipID.Value, kModel.BasTarih.Value, saatler, null, kModel.SROzelTanimID, kModel.BitTarih, haftaGunIDs);
                    mmMessage.Messages.AddRange(msg.Messages);
                }
                else
                {
                    var msg = SrTalepleriBus.SrKayitKontrol(kModel.SRSalonID.Value, kModel.SRTalepTipID.Value, kModel.Tarih.Value, saatler, null, kModel.SROzelTanimID);
                    mmMessage.Messages.AddRange(msg.Messages);
                }


            }
            if (kModel.Aciklama.IsNullOrWhiteSpace())
            { 
                mmMessage.Messages.Add("Açıklama giriniz");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "Aciklama" });
            }
            #endregion
            if (mmMessage.Messages.Count == 0)
            {
                if (kModel.SROzelTanimID <= 0)
                {
                    kModel.IsAktif = true;
                    var insertM = new SROzelTanimlar
                    {
                        EnstituKod = kModel.EnstituKod,
                        SROzelTanimTipID = kModel.SROzelTanimTipID
                    };
                    if (kModel.SROzelTanimTipID == SROzelTanimTip.Rezervasyon)
                    {
                        insertM.Tarih = kModel.Tarih;
                        insertM.SRSalonID = kModel.SRSalonID;
                        insertM.SRTalepTipID = kModel.SRTalepTipID;
                        insertM.Ay = null;
                        insertM.Gun = null;
                        insertM.BasTarih = null;
                        insertM.BitTarih = null;
                    }
                    else if (kModel.SROzelTanimTipID == SROzelTanimTip.ResmiTatilSabit)
                    {
                        insertM.Tarih = null;
                        insertM.SRSalonID = null;
                        insertM.SRTalepTipID = null;
                        insertM.Ay = kModel.Ay;
                        insertM.Gun = kModel.Gun;
                        insertM.BasTarih = null;
                        insertM.BitTarih = null;

                    }
                    else if (kModel.SROzelTanimTipID == SROzelTanimTip.ResmiTatilDegisen)
                    {
                        insertM.Tarih = null;
                        insertM.SRSalonID = null;
                        insertM.SRTalepTipID = null;
                        insertM.Ay = null;
                        insertM.Gun = null;
                        insertM.BasTarih = kModel.BasTarih;
                        insertM.BitTarih = kModel.BitTarih;

                    }
                    else if (kModel.SROzelTanimTipID == SROzelTanimTip.Rezerve)
                    {
                        insertM.Tarih = null;
                        insertM.SRSalonID = kModel.SRSalonID;
                        insertM.SRTalepTipID = kModel.SRTalepTipID;
                        insertM.Ay = null;
                        insertM.Gun = null;
                        insertM.BasTarih = kModel.BasTarih;
                        insertM.BitTarih = kModel.BitTarih;
                    }

                    insertM.Aciklama = kModel.Aciklama;
                    insertM.IsAktif = kModel.IsAktif;
                    insertM.IslemYapanID = UserIdentity.Current.Id;
                    insertM.IslemYapanIP = UserIdentity.Ip;
                    insertM.IslemTarihi = DateTime.Now;
                    var ydst = _entities.SROzelTanimlars.Add(insertM);
                    _entities.SaveChanges();
                    kModel.SROzelTanimID = ydst.SROzelTanimID;
                }
                else
                {
                    var data = _entities.SROzelTanimlars.First(p => p.SROzelTanimID == kModel.SROzelTanimID);
                    data.EnstituKod = kModel.EnstituKod;
                    data.SROzelTanimTipID = kModel.SROzelTanimTipID;
                    if (kModel.SROzelTanimTipID == SROzelTanimTip.Rezervasyon)
                    {
                        data.Tarih = kModel.Tarih;
                        data.SRSalonID = kModel.SRSalonID;
                        data.SRTalepTipID = kModel.SRTalepTipID;
                        data.Ay = null;
                        data.Gun = null;
                        data.BasTarih = null;
                        data.BitTarih = null;
                    }
                    else if (kModel.SROzelTanimTipID == SROzelTanimTip.ResmiTatilSabit)
                    {
                        data.Tarih = null;
                        data.SRSalonID = null;
                        data.SRTalepTipID = null;
                        data.Ay = kModel.Ay;
                        data.Gun = kModel.Gun;
                        data.BasTarih = null;
                        data.BitTarih = null;

                    }
                    else if (kModel.SROzelTanimTipID == SROzelTanimTip.ResmiTatilDegisen)
                    {
                        data.Tarih = null;
                        data.SRSalonID = null;
                        data.SRTalepTipID = null;
                        data.Ay = null;
                        data.Gun = null;
                        data.BasTarih = kModel.BasTarih;
                        data.BitTarih = kModel.BitTarih;
                    }
                    else if (kModel.SROzelTanimTipID == SROzelTanimTip.Rezerve)
                    {
                        data.Tarih = null;
                        data.SRSalonID = kModel.SRSalonID;
                        data.SRTalepTipID = kModel.SRTalepTipID;
                        data.Ay = null;
                        data.Gun = null;
                        data.BasTarih = basTarih2;
                        data.BitTarih = bitTarih2;
                    }
                    data.IsAktif = kModel.IsAktif;
                    data.Aciklama = kModel.Aciklama;
                    data.IslemYapanID = UserIdentity.Current.Id;
                    data.IslemYapanIP = UserIdentity.Ip;
                    data.IslemTarihi = DateTime.Now;
                    if (kModel.SROzelTanimTipID == SROzelTanimTip.Rezervasyon)
                    {
                        var ots = _entities.SROzelTanimSaatlers.Where(p => p.SROzelTanimID == data.SROzelTanimID).ToList();
                        _entities.SROzelTanimSaatlers.RemoveRange(ots);
                    }
                    if (kModel.SROzelTanimTipID == SROzelTanimTip.Rezerve)
                    {
                        var hgs = _entities.SROzelTanimGunlers.Where(p => p.SROzelTanimID == data.SROzelTanimID).ToList();
                        _entities.SROzelTanimGunlers.RemoveRange(hgs);
                    }
                }
                if (kModel.SROzelTanimTipID == SROzelTanimTip.Rezervasyon)
                {
                    foreach (var item in saatler)
                    {
                        item.SROzelTanimID = kModel.SROzelTanimID;
                        _entities.SROzelTanimSaatlers.Add(item);
                    }
                }
                if (kModel.SROzelTanimTipID == SROzelTanimTip.Rezerve)
                {
                    foreach (var item in haftaGunIDs.Distinct())
                    {
                        _entities.SROzelTanimGunlers.Add(new SROzelTanimGunler { HaftaGunID = item, SROzelTanimID = kModel.SROzelTanimID });
                    }
                }
                _entities.SaveChanges();
                return RedirectToAction("Index");
            }
            else
            {
                MessageBox.Show("Uyarı", MessageBox.MessageType.Warning, mmMessage.Messages.ToArray());
            }
            ViewBag.MmMessage = mmMessage;
            ViewBag.EnstituKod = new SelectList(EnstituBus.GetCmbYetkiliEnstituler( true), "Value", "Caption", kModel.EnstituKod);
            ViewBag.Ay = new SelectList(SrTalepleriBus.GetCmbAylar(true), "Value", "Caption", kModel.Ay);
            ViewBag.SROzelTanimTipID = new SelectList(SrTalepleriBus.GetCmbOzelTanimTipleri( true), "Value", "Caption", kModel.SROzelTanimTipID);
            ViewBag.SRSalonID = new SelectList(SrTalepleriBus.GetCmbSalonlar(kModel.EnstituKod, kModel.SRTalepTipID ?? 0 ,true), "Value", "Caption", kModel.SRSalonID);
            ViewBag.SRTalepTipID = new SelectList(SrTalepleriBus.GetCmbTalepTipleri( null, true), "Value", "Caption", kModel.SRTalepTipID);
            var hGunler = SrTalepleriBus.GetCmbHaftaGunleri(false);
            ViewBag.HaftaGunleri = hGunler;
            ViewBag.hGSecilenler = haftaGunIDs;
            return View(kModel);
        }

        public ActionResult GetDetail(int id)
        {
            var q = (from s in _entities.SROzelTanimlars
                     join ens in _entities.Enstitulers on s.EnstituKod equals ens.EnstituKod
                     join a in _entities.Aylars on s.Ay equals a.AyID into def1
                     from def in def1.DefaultIfEmpty()
                     join k in _entities.Kullanicilars on s.IslemYapanID equals k.KullaniciID
                     join ott in _entities.SROzelTanimTipleris on s.SROzelTanimTipID equals ott.SROzelTanimTipID
                     join sln in _entities.SRSalonlars on s.SRSalonID equals sln.SRSalonID into defs
                     from defSln in defs.DefaultIfEmpty()
                     join tt in _entities.SRTalepTipleris on s.SRTalepTipID equals tt.SRTalepTipID into deft
                     from defTt in deft.DefaultIfEmpty()
                     where s.SROzelTanimID == id
                     select new FrOzelTanimlar
                     {
                         SROzelTanimID = s.SROzelTanimID,
                         SROzelTanimTipID = s.SROzelTanimTipID,
                         SROzelTanimTipAdi = ott.SROzelTanimTipAdi,
                         TalepTipAdi = defTt != null ? defTt.TalepTipAdi : "",
                         EnstituKod = s.EnstituKod,
                         EnstituAdi = ens.EnstituAd,
                         SRSalonID = s.SRSalonID,
                         SalonAdi = s.SRSalonID.HasValue ? defSln.SalonAdi : "",
                         SROzelTanimSaatlers = s.SROzelTanimSaatlers.ToList(),
                         SROzelTanimGunlers = s.SROzelTanimGunlers.ToList(),
                         Tarih = s.Tarih,
                         Ay = s.Ay,
                         AyAdi = def != null ? def.AyAdi : "",
                         Gun = s.Gun,
                         BasTarih = s.BasTarih,
                         BitTarih = s.BitTarih,
                         Aciklama = s.Aciklama,
                         IsAktif = s.IsAktif,
                         IslemTarihi = s.IslemTarihi,
                         IslemYapanID = s.IslemYapanID,
                         IslemYapan = k.Ad + " " + k.Soyad,
                         IslemYapanIP = s.IslemYapanIP
                     }).FirstOrDefault();
            ViewBag.HaftaGunleri = _entities.HaftaGunleris.ToList();
            return View(q);
        }

        public ActionResult GetSaatList(int srSalonId, int srTalepTipId, DateTime tarih, int? srOzelTanimId)
        { 
            var data = SrTalepleriBus.GetSalonBosSaatler(srSalonId, srTalepTipId, tarih, null, srOzelTanimId);
            var hcb = ViewRenderHelper.RenderPartialView("SROzelTanimlar", "getSaatlerView", data);
            return new { Deger = hcb }.ToJsonResult();
        }
        public ActionResult GetSaatlerView(SRSalonSaatlerModel model)
        {
            return View(model);
        }
        public ActionResult Sil(int id)
        {
            var enstKods = UserIdentity.Current.EnstituKods ?? new List<string>();

            var kayit = _entities.SROzelTanimlars.FirstOrDefault(p => p.SROzelTanimID == id && UserIdentity.Current.EnstituKods.Contains(p.EnstituKod));
            string message = "";
            bool success = true;
            if (kayit != null)
            {

                try
                {
                    message = "'" + kayit.Aciklama + "' Açıklamalı özel tanım sistemden silindi!";
                    _entities.SROzelTanimlars.Remove(kayit);
                    _entities.SaveChanges();
                }
                catch (Exception ex)
                {
                    success = false;
                    message = "'" + kayit.Aciklama + "' Açıklamalı özel tanım Silinemedi! <br/> Bilgi:" + ex.ToExceptionMessage();
                    SistemBilgilendirmeBus.SistemBilgisiKaydet(message, "SROzelTanimlar/Sil<br/><br/>" + ex.ToExceptionStackTrace(), LogType.OnemsizHata);
                }
            }
            else
            {
                success = false;
                message = "Silmek istediğiniz Özel Tanım sistemde bulunamadı!";
            }
            return Json(new { success, message }, "application/json", JsonRequestBehavior.AllowGet);
        }
    }
}