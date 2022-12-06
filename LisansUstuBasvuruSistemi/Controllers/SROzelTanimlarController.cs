using BiskaUtil;
using LisansUstuBasvuruSistemi.Models; using LisansUstuBasvuruSistemi.Models.FilterModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;


namespace LisansUstuBasvuruSistemi.Controllers
{
    [System.Web.Mvc.OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    [Authorize(Roles = RoleNames.SROzelTanimlar)]
    public class SROzelTanimlarController : Controller
    {
        private LisansustuBasvuruSistemiEntities db = new LisansustuBasvuruSistemiEntities();
        public ActionResult Index()
        {
            return Index(new fmOzelTanimlar { });
        }
        [HttpPost]
        public ActionResult Index(fmOzelTanimlar model)
        {

            var q = from s in db.SROzelTanimlars
                    join ens in db.Enstitulers on s.EnstituKod equals ens.EnstituKod
                    join a in db.Aylars on s.Ay equals a.AyID into def1
                    from def in def1.DefaultIfEmpty()
                    join k in db.Kullanicilars on s.IslemYapanID equals k.KullaniciID
                    join ott in db.SROzelTanimTipleris on s.SROzelTanimTipID equals ott.SROzelTanimTipID
                    join sln in db.SRSalonlars on s.SRSalonID equals sln.SRSalonID into defs
                    from defSln in defs.DefaultIfEmpty()
                    join tt in db.SRTalepTipleris on s.SRTalepTipID equals tt.SRTalepTipID into deft
                    from defTT in deft.DefaultIfEmpty()

                    select new frOzelTanimlar
                    {
                        SROzelTanimID = s.SROzelTanimID,
                        SROzelTanimTipID = s.SROzelTanimTipID,
                        SROzelTanimTipAdi = ott.SROzelTanimTipAdi,
                        TalepTipAdi = defTT != null ? defTT.TalepTipAdi : "",
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
                    if (model.Sort.Contains("DESC") == false)
                    {
                        q = q.OrderBy(o => o.Tarih).ThenBy(t => t.SROzelTanimSaatlers.Min(s => s.BasSaat)).ThenBy(t => t.BasTarih).ThenBy(t => t.Ay).ThenBy(t => t.Gun);
                    }
                    else
                    {
                        q = q.OrderByDescending(o => o.Tarih).ThenByDescending(t => t.SROzelTanimSaatlers.Min(s => s.BasSaat)).ThenByDescending(t => t.BasTarih).ThenByDescending(t => t.Ay).ThenByDescending(t => t.Gun);
                    }

                }
                else
                {
                    q = q.OrderBy(model.Sort);
                }

            }
            else q = q.OrderBy(o => o.SROzelTanimTipID == SROzelTanimTip.ResmiTatilSabit ? 0 : o.SROzelTanimTipID);
            var PS = Management.setStartRowInx(model.StartRowIndex, model.PageIndex, model.PageCount, model.RowCount, model.PageSize);
            model.PageIndex = PS.PageIndex;
            model.data = q.Skip(PS.StartRowIndex).Take(model.PageSize).ToArray();
            var IndexModel = new MIndexBilgi();
            IndexModel.Toplam = model.RowCount;
            IndexModel.Aktif = q.Where(p => p.IsAktif).Count();
            IndexModel.Pasif = q.Where(p => !p.IsAktif).Count();
            ViewBag.IndexModel = IndexModel;
            ViewBag.IsAktif = new SelectList(Management.cmbAktifPasifData(true), "Value", "Caption", model.IsAktif);
            ViewBag.EnstituKod = new SelectList(Management.cmbGetAktifEnstituler(true), "Value", "Caption", model.EnstituKod);
            ViewBag.TT = db.SRTalepTipleris.ToList();
            ViewBag.SROzelTanimTipID = new SelectList(Management.cmbOzelTanimTipleri(true), "Value", "Caption", model.SROzelTanimTipID);
            ViewBag.HaftaGunleri = db.HaftaGunleris.ToList();
            return View(model);
        }
        public ActionResult Kayit(int? id, string dlgid)
        {
            var MmMessage = new MmMessage();
            MmMessage.IsDialog = !dlgid.IsNullOrWhiteSpace();
            MmMessage.DialogID = dlgid;
            ViewBag.MmMessage = MmMessage;
           
            var model = new SROzelTanimlar();
            model.Tarih = DateTime.Now;
            var hGSecilenler = new List<int>();
            if (id.HasValue)
            {
                model = db.SROzelTanimlars.Where(p => p.SROzelTanimID == id).FirstOrDefault();
                hGSecilenler = model.SROzelTanimGunlers.Select(s => s.HaftaGunID).ToList();

            }
            ViewBag.EnstituKod = new SelectList(Management.cmbGetYetkiliEnstituler( true), "Value", "Caption", model.EnstituKod);
            ViewBag.SROzelTanimTipID = new SelectList(Management.cmbOzelTanimTipleri( true), "Value", "Caption", model.SROzelTanimTipID);
            ViewBag.SRSalonID = new SelectList(Management.cmbSalonlar(model.EnstituKod, model.SRTalepTipID ?? 0 ,true), "Value", "Caption", model.SRSalonID);
            ViewBag.Ay = new SelectList(Management.cmbAylar( true), "Value", "Caption", model.Ay);
            ViewBag.SRTalepTipID = new SelectList(Management.cmbTalepTipleri( null, true), "Value", "Caption", model.SRTalepTipID);

            var hGunler = Management.cmbGetHaftaGunleri(false);
            ViewBag.HaftaGunleri = hGunler;
            ViewBag.hGSecilenler = hGSecilenler;
            return View(model);
        }
        [HttpPost]
        public ActionResult Kayit(SROzelTanimlar kModel, List<TimeSpan?> BasSaat, List<TimeSpan?> BitSaat, DateTime? BasTarih2, DateTime? BitTarih2, List<int> HaftaGunIDs, string OldID, string dlgid = "")
        {
            HaftaGunIDs = HaftaGunIDs ?? new List<int>();
            var MmMessage = new MmMessage();
            MmMessage.IsDialog = !dlgid.IsNullOrWhiteSpace();
            MmMessage.DialogID = dlgid;
           
            var saatler = new List<SROzelTanimSaatler>();
            #region Kontrol 
            if (kModel.EnstituKod.IsNullOrWhiteSpace())
            {
                string msg = "Enstitü seçiniz";
                MmMessage.Messages.Add(msg);
                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "EnstituKod" });
            }
            if (kModel.SROzelTanimTipID <= 0)
            {
                string msg = "Özel tanım tipini seçiniz";
                MmMessage.Messages.Add(msg);
                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "SROzelTanimTipID" });
            }
            else if (kModel.SROzelTanimTipID == SROzelTanimTip.ResmiTatilSabit)
            {
                if (kModel.Ay.HasValue == false)
                {
                    string msg = "Ay seçiniz";
                    MmMessage.Messages.Add(msg);
                    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "Ay" });
                }
                if (kModel.Gun.HasValue == false)
                {
                    string msg = "Gün seçiniz";
                    MmMessage.Messages.Add(msg);
                    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "Gun" });
                }
            }
            else if (kModel.SROzelTanimTipID == SROzelTanimTip.ResmiTatilDegisen)
            {
                if (kModel.BasTarih.HasValue == false)
                {
                    string msg = "Başlangıç Tarihi seçiniz";
                    MmMessage.Messages.Add(msg);
                    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "BasTarih" });
                }
                if (kModel.BitTarih.HasValue == false)
                {
                    string msg = "Bitiş Tarihi seçiniz";
                    MmMessage.Messages.Add(msg);
                    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "BitTarih" });
                }
                if (kModel.BasTarih.HasValue && kModel.BitTarih.HasValue)
                {
                    if (kModel.BasTarih > kModel.BitTarih)
                    {
                        string msg = "Başlangıç tarihi bitiş tarihinden büyük olamaz!";
                        MmMessage.Messages.Add(msg);
                        MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "BasTarih" });
                        MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "BitTarih" });
                    }
                }
            }
            else if (kModel.SROzelTanimTipID == SROzelTanimTip.Rezervasyon)
            {
                if (kModel.SRTalepTipID.HasValue == false)
                {
                    string msg = "Rezervasyon tipini seçiniz!";
                    MmMessage.Messages.Add(msg);
                    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "SRTalepTipID" });
                }
                else if (kModel.SRSalonID.HasValue == false)
                {
                    string msg = "Salon seçiniz";
                    MmMessage.Messages.Add(msg);
                    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "SRSalonID" });
                }
                else if (kModel.Tarih.HasValue == false)
                {
                    string msg = "Rezervasyon tarihini seçiniz";
                    MmMessage.Messages.Add(msg);
                    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "Tarih" });
                }
                else
                {

                    BasSaat = BasSaat ?? new List<TimeSpan?>();
                    BitSaat = BitSaat ?? new List<TimeSpan?>();

                    var qBasSaat = BasSaat.Where(p => p.HasValue).Select((s, inx) => new { s, inx }).ToList();
                    var qBitSaat = BitSaat.Where(p => p.HasValue).Select((s, inx) => new { s, inx }).ToList();

                    saatler = (from qba in qBasSaat
                               join qbi in qBitSaat on qba.inx equals qbi.inx
                               select new SROzelTanimSaatler
                               {
                                   BasSaat = qba.s.Value,
                                   BitSaat = qbi.s.Value,
                               }).ToList();

                    if (saatler.Count == 0)
                    {
                        string msg = "Rezervasyonun yapılabilmesi için en az 1 uygun saat seçmeniz gerekmektedir.";
                        MmMessage.Messages.Add(msg);
                        MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "Tarih" });
                    }
                }
            }
            else if (kModel.SROzelTanimTipID == SROzelTanimTip.Rezerve)
            {
                if (kModel.SRTalepTipID.HasValue == false)
                {
                    string msg = "Rezervasyon tipini seçiniz!";
                    MmMessage.Messages.Add(msg);
                    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "SRTalepTipID" });
                }
                else if (kModel.SRSalonID.HasValue == false)
                {
                    string msg = "Salon seçiniz";
                    MmMessage.Messages.Add(msg);
                    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "SRSalonID" });
                }
                kModel.BasTarih = BasTarih2;
                kModel.BitTarih = BitTarih2;
                if (kModel.BasTarih.HasValue == false)
                {
                    string msg = "Başlangıç Tarihi Seçiniz";
                    MmMessage.Messages.Add(msg);
                    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "BasTarih2" });
                }
                if (kModel.BitTarih.HasValue == false)
                {
                    string msg = "Bitiş Tarihi Seçiniz";
                    MmMessage.Messages.Add(msg);
                    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "BitTarih2" });
                }
                if (kModel.BasTarih.HasValue && kModel.BitTarih.HasValue)
                {
                    if (kModel.BasTarih > kModel.BitTarih)
                    {
                        string msg = "Başlangıç tarihi bitiş tarihinden büyük olamaz!";
                        MmMessage.Messages.Add(msg);
                        MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "BasTarih" });
                        MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "BitTarih" });
                    }

                }
                //if (HaftaGunIDs.Count == 0)
                //{
                //    string msg = "En az 1 hafta günü seçmeniz gerekmektedir.";
                //    MmMessage.Messages.Add(msg);
                //    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "HaftaGunID" });
                //}
            }
            if (MmMessage.Messages.Count == 0 && (kModel.SROzelTanimTipID == SROzelTanimTip.Rezervasyon || kModel.SROzelTanimTipID == SROzelTanimTip.Rezerve))
            {
                if (kModel.SROzelTanimTipID == SROzelTanimTip.Rezerve)
                {
                    var msg = Management.SRKayitKontrol(kModel.SRSalonID.Value, kModel.SRTalepTipID.Value, kModel.BasTarih.Value, saatler, null, kModel.SROzelTanimID, kModel.BitTarih, HaftaGunIDs);
                    MmMessage.Messages.AddRange(msg.Messages);
                }
                else
                {
                    var msg = Management.SRKayitKontrol(kModel.SRSalonID.Value, kModel.SRTalepTipID.Value, kModel.Tarih.Value, saatler, null, kModel.SROzelTanimID);
                    MmMessage.Messages.AddRange(msg.Messages);
                }


            }
            if (kModel.Aciklama.IsNullOrWhiteSpace())
            {
                string msg = "Açıklama giriniz";
                MmMessage.Messages.Add(msg);
                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "Aciklama" });
            }
            #endregion
            if (MmMessage.Messages.Count == 0)
            {
                if (kModel.SROzelTanimID <= 0)
                {
                    kModel.IsAktif = true;
                    var insertM = new SROzelTanimlar();
                    insertM.EnstituKod = kModel.EnstituKod;
                    insertM.SROzelTanimTipID = kModel.SROzelTanimTipID;
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
                    var ydst = db.SROzelTanimlars.Add(insertM);
                    db.SaveChanges();
                    kModel.SROzelTanimID = ydst.SROzelTanimID;
                }
                else
                {
                    var data = db.SROzelTanimlars.Where(p => p.SROzelTanimID == kModel.SROzelTanimID).First();
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
                        data.BasTarih = BasTarih2;
                        data.BitTarih = BitTarih2;
                    }
                    data.IsAktif = kModel.IsAktif;
                    data.Aciklama = kModel.Aciklama;
                    data.IslemYapanID = UserIdentity.Current.Id;
                    data.IslemYapanIP = UserIdentity.Ip;
                    data.IslemTarihi = DateTime.Now;
                    if (kModel.SROzelTanimTipID == SROzelTanimTip.Rezervasyon)
                    {
                        var ots = db.SROzelTanimSaatlers.Where(p => p.SROzelTanimID == data.SROzelTanimID).ToList();
                        db.SROzelTanimSaatlers.RemoveRange(ots);
                    }
                    if (kModel.SROzelTanimTipID == SROzelTanimTip.Rezerve)
                    {
                        var hgs = db.SROzelTanimGunlers.Where(p => p.SROzelTanimID == data.SROzelTanimID).ToList();
                        db.SROzelTanimGunlers.RemoveRange(hgs);
                    }
                }
                if (kModel.SROzelTanimTipID == SROzelTanimTip.Rezervasyon)
                {
                    foreach (var item in saatler)
                    {
                        item.SROzelTanimID = kModel.SROzelTanimID;
                        db.SROzelTanimSaatlers.Add(item);
                    }
                }
                if (kModel.SROzelTanimTipID == SROzelTanimTip.Rezerve)
                {
                    foreach (var item in HaftaGunIDs.Distinct())
                    {
                        db.SROzelTanimGunlers.Add(new SROzelTanimGunler { HaftaGunID = item, SROzelTanimID = kModel.SROzelTanimID });
                    }
                }
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            else
            {
                MessageBox.Show("Uyarı", MessageBox.MessageType.Warning, MmMessage.Messages.ToArray());
            }
            ViewBag.MmMessage = MmMessage;
            ViewBag.EnstituKod = new SelectList(Management.cmbGetYetkiliEnstituler( true), "Value", "Caption", kModel.EnstituKod);
            ViewBag.Ay = new SelectList(Management.cmbAylar(true), "Value", "Caption", kModel.Ay);
            ViewBag.SROzelTanimTipID = new SelectList(Management.cmbOzelTanimTipleri( true), "Value", "Caption", kModel.SROzelTanimTipID);
            ViewBag.SRSalonID = new SelectList(Management.cmbSalonlar(kModel.EnstituKod, kModel.SRTalepTipID ?? 0 ,true), "Value", "Caption", kModel.SRSalonID);
            ViewBag.SRTalepTipID = new SelectList(Management.cmbTalepTipleri( null, true), "Value", "Caption", kModel.SRTalepTipID);
            var hGunler = Management.cmbGetHaftaGunleri(false);
            ViewBag.HaftaGunleri = hGunler;
            ViewBag.hGSecilenler = HaftaGunIDs;
            return View(kModel);
        }

        public ActionResult getDetail(int id)
        {
            var q = (from s in db.SROzelTanimlars
                     join ens in db.Enstitulers on s.EnstituKod equals ens.EnstituKod
                     join a in db.Aylars on s.Ay equals a.AyID into def1
                     from def in def1.DefaultIfEmpty()
                     join k in db.Kullanicilars on s.IslemYapanID equals k.KullaniciID
                     join ott in db.SROzelTanimTipleris on s.SROzelTanimTipID equals ott.SROzelTanimTipID
                     join sln in db.SRSalonlars on s.SRSalonID equals sln.SRSalonID into defs
                     from defSln in defs.DefaultIfEmpty()
                     join tt in db.SRTalepTipleris on s.SRTalepTipID equals tt.SRTalepTipID into deft
                     from defTT in deft.DefaultIfEmpty()
                     where s.SROzelTanimID == id
                     select new frOzelTanimlar
                     {
                         SROzelTanimID = s.SROzelTanimID,
                         SROzelTanimTipID = s.SROzelTanimTipID,
                         SROzelTanimTipAdi = ott.SROzelTanimTipAdi,
                         TalepTipAdi = defTT != null ? defTT.TalepTipAdi : "",
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
            ViewBag.HaftaGunleri = db.HaftaGunleris.ToList();
            return View(q);
        }

        public ActionResult getSaatList(int SRSalonID, int SRTalepTipID, DateTime Tarih, int? SROzelTanimID)
        { 
            var data = Management.getSalonBosSaatler(SRSalonID, SRTalepTipID, Tarih, null, SROzelTanimID);
            var HCB = Management.RenderPartialView("SROzelTanimlar", "getSaatlerView", data);
            return new { Deger = HCB }.toJsonResult();
        }
        public ActionResult getSaatlerView(SRSalonSaatlerModel model)
        {
            return View(model);
        }
        public ActionResult Sil(int id)
        {
            var EnstKods = UserIdentity.Current.EnstituKods ?? new List<string>();

            var kayit = db.SROzelTanimlars.Where(p => p.SROzelTanimID == id && UserIdentity.Current.EnstituKods.Contains(p.EnstituKod)).FirstOrDefault();
            string message = "";
            bool success = true;
            if (kayit != null)
            {

                try
                {
                    message = "'" + kayit.Aciklama + "' Açıklamalı özel tanım sistemden silindi!";
                    db.SROzelTanimlars.Remove(kayit);
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    success = false;
                    message = "'" + kayit.Aciklama + "' Açıklamalı özel tanım Silinemedi! <br/> Bilgi:" + ex.ToExceptionMessage();
                    Management.SistemBilgisiKaydet(message, "SROzelTanimlar/Sil<br/><br/>" + ex.ToExceptionStackTrace(), BilgiTipi.OnemsizHata);
                }
            }
            else
            {
                success = false;
                message = "Silmek istediğiniz Özel Tanım sistemde bulunamadı!";
            }
            return Json(new { success = success, message = message }, "application/json", JsonRequestBehavior.AllowGet);
        }
    }
}