using BiskaUtil;
using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.MenuAndRoles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace LisansUstuBasvuruSistemi.Controllers
{
    [System.Web.Mvc.OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    [Authorize(Roles = RoleNames.BelgeTipleri)]
    public class BelgeTipDetayController : Controller
    {
        private LisansustuBasvuruSistemiEntities db = new LisansustuBasvuruSistemiEntities();
        public ActionResult Index(string EKD)
        {
            return Index(new fmBelgeTipDetay { }, EKD);
        }
        [HttpPost]
        public ActionResult Index(fmBelgeTipDetay model, string EKD)
        {

            var _EnstituKod = Management.getSelectedEnstitu(EKD);
            var q = (from s in db.BelgeTipDetays
                     join so in db.OgrenimDurumlaris on new { s.OgrenimDurumID } equals new { so.OgrenimDurumID }
                     where s.EnstituKod == _EnstituKod
                     select new frBelgeTipDetay
                     {
                         BelgeTipDetayID = s.BelgeTipDetayID,
                         EnstituKod = s.EnstituKod,
                         OgrenimDurumID = s.OgrenimDurumID,
                         OgrenimDurumAdi = so.OgrenimDurumAdi,
                         BelgeTipAdi = db.BelgeTipleris.Where(p => s.BelgeTipDetayBelgelers.Any(a => a.BelgeTipID == p.BelgeTipID)).Select(s => s.BelgeTipAdi).ToList(),
                         UcretAlimiVar = s.UcretAlimiVar,
                         BelgeFiyati = s.BelgeFiyati,
                         UcretsizMiktar = s.UcretsizMiktar,
                         DonemlikKota = s.DonemlikKota,
                         UcretAciklamasiLink = s.UcretAciklamasiLink,
                         Saatler = (from s in db.BelgeTipDetaySaatlers.Where(p => p.BelgeTipDetayID == s.BelgeTipDetayID)
                                    join gn in db.HaftaGunleris on s.HaftaGunID equals gn.HaftaGunID
                                    group new { s.TalepBaslangicSaat, s.TalepBitisSaat, s.HaftaGunID, gn.HaftaGunAdi, s.TeslimBaslangicSaat, s.TeslimBitisSaat, s.EklenecekGun }
                                       by new
                                       {
                                           s.TalepBaslangicSaat,
                                           s.TalepBitisSaat,
                                           s.TeslimBaslangicSaat,
                                           s.TeslimBitisSaat,
                                           s.EklenecekGun

                                       } into g1
                                    select new BtSaatShowModel
                                    {
                                        TalepBaslangicSaat = g1.Key.TalepBaslangicSaat,
                                        TalepBitisSaat = g1.Key.TalepBitisSaat,
                                        TeslimBaslangicSaat = g1.Key.TeslimBaslangicSaat,
                                        TeslimBitisSaat = g1.Key.TeslimBitisSaat,
                                        EklenecekGun = g1.Key.EklenecekGun,
                                        GunNos = g1.Where(p => p.TalepBaslangicSaat == g1.Key.TalepBaslangicSaat && p.TalepBitisSaat == g1.Key.TalepBitisSaat && p.TeslimBaslangicSaat == g1.Key.TeslimBaslangicSaat && p.TeslimBitisSaat == g1.Key.TeslimBitisSaat && p.EklenecekGun == g1.Key.EklenecekGun).Select(s2 => new CmbIntDto { Value = s2.HaftaGunID, Caption = s2.HaftaGunAdi }).OrderByDescending(o => o.Value > 0).ThenBy(t => t.Value.Value).ToList()
                                    }).OrderBy(t => t.GunNos.Min(m => m.Value)).ThenBy(t => t.TalepBaslangicSaat).ToList(),
                         IsAktif = s.IsAktif,
                         IslemTarihi = s.IslemTarihi,
                         IslemYapanID = s.IslemYapanID,
                         IslemYapan = s.Kullanicilar.Ad + " " + s.Kullanicilar.Soyad,
                         IslemYapanIP = s.IslemYapanIP
                     });


            if (!model.OgrenimDurumAdi.IsNullOrWhiteSpace()) q = q.Where(p => p.OgrenimDurumAdi.Contains(model.OgrenimDurumAdi));
            if (!model.BelgeTipAdi.IsNullOrWhiteSpace()) q = q.Where(p => p.BelgeTipAdi.Contains(model.BelgeTipAdi));
            model.RowCount = q.Count();
            if (!model.Sort.IsNullOrWhiteSpace())
            {
                q = q.OrderBy(model.Sort);
            }
            else q = q.OrderBy(o => o.IslemTarihi);
            var PS = Management.setStartRowInx(model.StartRowIndex, model.PageIndex, model.PageCount, model.RowCount, model.PageSize);
            model.PageIndex = PS.PageIndex;
            model.data = q.Skip(PS.StartRowIndex).Take(model.PageSize).ToArray();
            var IndexModel = new MIndexBilgi();
            IndexModel.Toplam = model.RowCount;
            IndexModel.Aktif = q.Where(p => p.IsAktif).Count();
            IndexModel.Pasif = q.Where(p => !p.IsAktif).Count();
            ViewBag.IndexModel = IndexModel;
            ViewBag.IsAktif = new SelectList(Management.cmbAktifPasifData(true), "Value", "Caption", model.IsAktif);
            return View(model);
        }


        public ActionResult Kayit(int? id)
        {
            var MmMessage = new MmMessage();
            ViewBag.MmMessage = MmMessage;
            var model = new BelgeTipDetayKayitModel();
            if (id.HasValue)
            {
                var data = db.BelgeTipDetays.Where(p => p.BelgeTipDetayID == id).FirstOrDefault();
                if (data != null)
                {
                    model.BelgeTipDetayID = data.BelgeTipDetayID;
                    model.EnstituKod = data.EnstituKod;
                    model.OgrenimDurumID = data.OgrenimDurumID;
                    model.UcretAlimiVar = data.UcretAlimiVar;
                    model.BelgeFiyati = data.BelgeFiyati;
                    model.UcretsizMiktar = data.UcretsizMiktar;
                    model.DonemlikKota = data.DonemlikKota;
                    model.UcretAciklamasiLink = data.UcretAciklamasiLink;
                    model.IsAktif = data.IsAktif;
                    model.IslemTarihi = data.IslemTarihi;
                    model.IslemYapanID = data.IslemYapanID;
                    model.IslemYapanIP = data.IslemYapanIP;
                }
            }

            var haftaGunleri = Management.cmbGetHaftaGunleri(false);
            ViewBag.HaftaGunleri = haftaGunleri;
            ViewBag.BelgeTipleriList = Management.cmbBelgeTipleri(false);
            model.SeciliBelgeTipler = db.BelgeTipDetayBelgelers.Where(p => p.BelgeTipDetayID == model.BelgeTipDetayID).Select(s => s.BelgeTipID).ToList();
            model.Saatler = (from s in db.BelgeTipDetaySaatlers.Where(p => p.BelgeTipDetayID == model.BelgeTipDetayID)
                             join gn in db.HaftaGunleris on s.HaftaGunID equals gn.HaftaGunID
                             group new { s.TalepBaslangicSaat, s.TalepBitisSaat, s.HaftaGunID, gn.HaftaGunAdi, s.TeslimBaslangicSaat, s.TeslimBitisSaat, s.EklenecekGun }
                                by new
                                {
                                    s.TalepBaslangicSaat,
                                    s.TalepBitisSaat,
                                    s.TeslimBaslangicSaat,
                                    s.TeslimBitisSaat,
                                    s.EklenecekGun

                                } into g1
                             select new BtSaatShowModel
                             {
                                 TalepBaslangicSaat = g1.Key.TalepBaslangicSaat,
                                 TalepBitisSaat = g1.Key.TalepBitisSaat,
                                 TeslimBaslangicSaat = g1.Key.TeslimBaslangicSaat,
                                 TeslimBitisSaat = g1.Key.TeslimBitisSaat,
                                 EklenecekGun = g1.Key.EklenecekGun,
                                 GunNos = g1.Where(p => p.TalepBaslangicSaat == g1.Key.TalepBaslangicSaat && p.TalepBitisSaat == g1.Key.TalepBitisSaat && p.TeslimBaslangicSaat == g1.Key.TeslimBaslangicSaat && p.TeslimBitisSaat == g1.Key.TeslimBitisSaat && p.EklenecekGun == g1.Key.EklenecekGun).Select(s2 => new CmbIntDto { Value = s2.HaftaGunID, Caption = s2.HaftaGunAdi }).OrderByDescending(o => o.Value > 0).ThenBy(t => t.Value.Value).ToList()
                             }).OrderBy(t => t.GunNos.Min(m => m.Value)).ThenBy(t => t.TalepBaslangicSaat).ToList();

            ViewBag.OgrenimDurumID = new SelectList(Management.cmbAktifOgrenimDurumu(true, IsHesapKayittaGozuksun: true), "Value", "Caption", model.OgrenimDurumID);
            return View(model);
        }
        [HttpPost]
        [Authorize(Roles = RoleNames.BelgeTipleriKayıt)]
        public ActionResult Kayit(BelgeTipDetayKayitModel kModel, string EKD)
        {
            var _EnstituKod = Management.getSelectedEnstitu(EKD);
            kModel.EnstituKod = _EnstituKod;
            var MmMessage = new MmMessage(); 
            kModel.TalepBaslangicSaat = kModel.TalepBaslangicSaat ?? new List<TimeSpan>();
            kModel.TalepBitisSaat = kModel.TalepBitisSaat ?? new List<TimeSpan>();
            kModel.EklenecekGun = kModel.EklenecekGun ?? new List<int>();
            kModel.TeslimBaslangicSaat = kModel.TeslimBaslangicSaat ?? new List<TimeSpan>();
            kModel.TeslimBitisSaat = kModel.TeslimBitisSaat ?? new List<TimeSpan>();
            kModel.HaftaGunleri = kModel.HaftaGunleri ?? new List<string>();
            kModel.BelgeTipID = kModel.BelgeTipID ?? new List<int>();
            var qTalepBaslangicSaat = kModel.TalepBaslangicSaat.Select((s, inx) => new { s, inx }).ToList();
            var qTalepBitisSaat = kModel.TalepBitisSaat.Select((s, inx) => new { s, inx }).ToList();
            var qEklenecekGun = kModel.EklenecekGun.Select((s, inx) => new { s, inx }).ToList();
            var qTeslimBaslangicSaat = kModel.TeslimBaslangicSaat.Select((s, inx) => new { s, inx }).ToList();
            var qTeslimBitisSaat = kModel.TeslimBitisSaat.Select((s, inx) => new { s, inx }).ToList();
            var qHaftaGunleri = kModel.HaftaGunleri.Select((s, inx) => new { s = s.Split(',').Select(s2 => s2.ToInt().Value).ToList(), inx }).ToList();
            var qSaatler = (from qTbs in qTalepBaslangicSaat
                            join qTbt in qTalepBitisSaat on qTbs.inx equals qTbt.inx
                            join qEg in qEklenecekGun on qTbt.inx equals qEg.inx
                            join qtsb in qTeslimBaslangicSaat on qTbs.inx equals qtsb.inx
                            join qtsbt in qTeslimBitisSaat on qTbs.inx equals qtsbt.inx
                            join qhg in qHaftaGunleri on qTbs.inx equals qhg.inx

                            select new BtSaatShowModel
                            {
                                TalepBaslangicSaat = qTbs.s,
                                TalepBitisSaat = qTbt.s,
                                TeslimBaslangicSaat = qtsb.s,
                                TeslimBitisSaat = qtsbt.s,
                                EklenecekGun = qEg.s,
                                GunNos = db.HaftaGunleris.Where(p => qhg.s.Contains(p.HaftaGunID)).Select(s => new CmbIntDto { Value = s.HaftaGunID, Caption = s.HaftaGunAdi }).OrderByDescending(o => o.Value > 0).ThenBy(t => t.Value.Value).ToList()
                            }).OrderBy(t => t.GunNos.Min(m => m.Value)).ThenBy(t => t.TalepBaslangicSaat).ToList();

            #region Kontrol 
            if (kModel.BelgeTipID.Count == 0)
            {
                string msg = "Kayıt işlemini yapabilmeniz belge tipini seçmeniz gerekmektedir!";
                MmMessage.Messages.Add(msg);
                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "BelgeTipID" });
            }
            else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "BelgeTipID" });
            if (kModel.OgrenimDurumID <= 0)
            {
                string msg = "Kayıt işlemini yapabilmeniz öğrenim durumunu seçmeniz gerekmektedir!";
                MmMessage.Messages.Add(msg);
                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "OgrenimDurumID" });
            }
            else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "OgrenimDurumID" });

            if (kModel.UcretAlimiVar)
            {
                if (kModel.BelgeFiyati.HasValue == false || kModel.BelgeFiyati <= 0)
                {
                    string msg = "Kayıt işlemini yapabilmeniz belge fiyatının 0 dan büyük bir değeri olması gerekmektedir!";
                    MmMessage.Messages.Add(msg);
                    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "BelgeFiyati" });
                }
                else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "BelgeFiyati" });
            }

            if (qSaatler.Count == 0)
            {
                string msg = "Kayıt işlemini yapabilmeniz saat kriterlerini tanımlayınız!";
                MmMessage.Messages.Add(msg);
            }
            if (MmMessage.Messages.Count == 0)
            {
                var anyData = db.BelgeTipDetayBelgelers.Where(a => kModel.BelgeTipID.Contains(a.BelgeTipID) && a.BelgeTipDetay.OgrenimDurumID == kModel.OgrenimDurumID && a.BelgeTipDetayID != kModel.BelgeTipDetayID).ToList();
                if (anyData.Count > 0)
                {
                    var ids = anyData.Select(s => s.BelgeTipID).ToList();
                    var belgeTipleri = db.BelgeTipleris.Where(p => ids.Contains(p.BelgeTipID)).Select(s => s.BelgeTipAdi).ToList();
                    string msg = "Eklemeye çalıştığınız '" + string.Join(", ", belgeTipleri) + "' belge tipleri sistemde zaten tanımlıdır!";
                    MmMessage.Messages.Add(msg);
                    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "BelgeTipID" });
                }
                else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "BelgeTipID" });
            }
            #endregion
            if (MmMessage.Messages.Count == 0)
            {
                kModel.IsAktif = kModel.IsAktif;
                kModel.IslemYapanID = UserIdentity.Current.Id;
                kModel.IslemYapanIP = UserIdentity.Ip;
                kModel.IslemTarihi = DateTime.Now;
                if (kModel.UcretAlimiVar == false)
                {
                    kModel.BelgeFiyati = null;
                    kModel.UcretsizMiktar = null;
                    kModel.UcretAciklamasiLink = null;
                }
                if (kModel.BelgeTipDetayID <= 0)
                {
                    kModel.IsAktif = true;
                    var eklenen = db.BelgeTipDetays.Add(new BelgeTipDetay
                    {
                        EnstituKod = kModel.EnstituKod,
                        OgrenimDurumID = kModel.OgrenimDurumID,
                        UcretAlimiVar = kModel.UcretAlimiVar,
                        BelgeFiyati = kModel.BelgeFiyati,
                        UcretsizMiktar = kModel.UcretsizMiktar,
                        DonemlikKota = kModel.DonemlikKota,
                        UcretAciklamasiLink = kModel.UcretAciklamasiLink,
                        IsAktif = kModel.IsAktif,
                        IslemTarihi = kModel.IslemTarihi,
                        IslemYapanID = kModel.IslemYapanID,
                        IslemYapanIP = kModel.IslemYapanIP

                    });
                    db.SaveChanges();
                    kModel.BelgeTipDetayID = eklenen.BelgeTipDetayID;
                }
                else
                {
                    var data = db.BelgeTipDetays.Where(p => p.BelgeTipDetayID == kModel.BelgeTipDetayID).First();
                    data.OgrenimDurumID = kModel.OgrenimDurumID;
                    data.UcretAlimiVar = kModel.UcretAlimiVar;
                    data.BelgeFiyati = kModel.BelgeFiyati <= 0 ? null : kModel.BelgeFiyati;
                    data.UcretsizMiktar = kModel.UcretsizMiktar <= 0 ? null : kModel.UcretsizMiktar;
                    data.DonemlikKota = kModel.DonemlikKota <= 0 ? null : kModel.DonemlikKota;
                    data.UcretAciklamasiLink = kModel.UcretAciklamasiLink;
                    data.IsAktif = kModel.IsAktif;
                    data.IslemYapanID = UserIdentity.Current.Id;
                    data.IslemYapanIP = UserIdentity.Ip;
                    data.IslemTarihi = DateTime.Now;

                    var _saatler = db.BelgeTipDetaySaatlers.Where(p => p.BelgeTipDetayID == data.BelgeTipDetayID).ToList();
                    db.BelgeTipDetaySaatlers.RemoveRange(_saatler);

                    var _btipler = db.BelgeTipDetayBelgelers.Where(p => p.BelgeTipDetayID == data.BelgeTipDetayID).ToList();
                    db.BelgeTipDetayBelgelers.RemoveRange(_btipler);
                    db.SaveChanges();
                }

                foreach (var item in kModel.BelgeTipID)
                {
                    db.BelgeTipDetayBelgelers.Add(new BelgeTipDetayBelgeler { BelgeTipDetayID = kModel.BelgeTipDetayID, BelgeTipID = item });
                }
                foreach (var item in qSaatler)
                {
                    foreach (var item2 in item.GunNos)
                    {
                        db.BelgeTipDetaySaatlers.Add(new BelgeTipDetaySaatler
                        {
                            BelgeTipDetayID = kModel.BelgeTipDetayID,
                            HaftaGunID = item2.Value.Value,
                            TalepBaslangicSaat = item.TalepBaslangicSaat,
                            TalepBitisSaat = item.TalepBitisSaat,
                            EklenecekGun = item.EklenecekGun,
                            TeslimBaslangicSaat = item.TeslimBaslangicSaat,
                            TeslimBitisSaat = item.TeslimBitisSaat
                        });

                    }
                }
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            else
            {
                MessageBox.Show("Uyarı", MessageBox.MessageType.Warning, MmMessage.Messages.ToArray());
            }
            kModel.Saatler = qSaatler;
            kModel.SeciliBelgeTipler = kModel.BelgeTipID;
            var haftaGunleri = Management.cmbGetHaftaGunleri(false);
            ViewBag.HaftaGunleri = haftaGunleri;
            ViewBag.BelgeTipleriList = Management.cmbBelgeTipleri(false);
            ViewBag.MmMessage = MmMessage;
            ViewBag.OgrenimDurumID = new SelectList(Management.cmbAktifOgrenimDurumu(true, IsHesapKayittaGozuksun: true), "Value", "Caption", kModel.OgrenimDurumID);
            return View(kModel);
        }


        public ActionResult saatEkleKontrol(BTSaatKontrolModel model)
        {
            var mmMessage = new MmMessage();
            mmMessage.IsSuccess = true;
            model.GHaftaGunleri = model.HaftaGunleri.IsNullOrWhiteSpace() ? new List<int>() : model.HaftaGunleri.Split(',').Select(s => s.ToInt().Value).ToList();

            model.HaftaGunleriList = model.HaftaGunleriList ?? new List<string>();
            model.TalepBaslangicSaatList = model.TalepBaslangicSaatList ?? new List<TimeSpan>();
            model.TalepBitisSaatList = model.TalepBitisSaatList ?? new List<TimeSpan>();
            var _TalepBaslangcSaati = model.TalepBaslangicSaatList.Select((s, inx) => new { s, inx }).ToList();
            var _TalepBitisSaati = model.TalepBitisSaatList.Select((s, inx) => new { s, inx }).ToList();
            var _HaftaGunleriList = model.HaftaGunleriList.Select((s, inx) => new { s, inx }).ToList();
            var _GHaftaGunleriList = model.HaftaGunleriList.Select((s, inx) => new { s = s.Split(',').Select(s2 => s2.ToInt().Value).ToList(), inx }).ToList();
            var qSaatler = (from tbs in _TalepBaslangcSaati
                            join tbt in _TalepBitisSaati on tbs.inx equals tbt.inx
                            join hgl in _HaftaGunleriList on tbs.inx equals hgl.inx
                            join ghgl in _GHaftaGunleriList on tbs.inx equals ghgl.inx

                            select new
                            {
                                Inx = tbs.inx,
                                _TalepBaslangcSaati = tbs.s,
                                _TalepBitisSaati = tbt.s,
                                _HaftaGunleriList = hgl.s,
                                _GHaftaGunleriList = ghgl.s
                            }).ToList();

            if (model.TalepBaslangicSaat.HasValue == false)
            {
                mmMessage.IsSuccess = false;
                mmMessage.Messages.Add("Belge talebi başlangıç saati boş bırakılamaz!");

            }
            if (model.TalepBitisSaat.HasValue == false)
            {
                mmMessage.IsSuccess = false;
                mmMessage.Messages.Add("Belge talebi bitiş saati boş bırakılamaz!");

            }
            if (model.EklenecekGun.HasValue == false)
            {
                mmMessage.IsSuccess = false;
                mmMessage.Messages.Add("Eklenecek gün seçiniz!");

            }
            if (model.TeslimBaslangicSaat.HasValue == false)
            {
                mmMessage.IsSuccess = false;
                mmMessage.Messages.Add("Belge teslimi için başlangıç saati boş bırakılamaz!");

            }
            if (model.TeslimBitisSaat.HasValue == false)
            {
                mmMessage.IsSuccess = false;
                mmMessage.Messages.Add("Belge teslimi için bitiş saati boş bırakılamaz!");

            }
            if (mmMessage.IsSuccess)
            {
                if (model.TalepBaslangicSaat >= model.TalepBitisSaat)
                {
                    mmMessage.IsSuccess = false;
                    mmMessage.Messages.Add("Belge talebi başlangıç saati bitiş saatinden büyük ya da eşit olamaz!");

                }
                if (model.TeslimBaslangicSaat >= model.TeslimBitisSaat)
                {
                    mmMessage.IsSuccess = false;
                    mmMessage.Messages.Add("Belge teslimi başlangıç saati bitiş saatinden büyük ya da eşit olamaz!");

                }
            }
            if (mmMessage.IsSuccess)
            {
                var varolanlar = qSaatler.Where(a => a._GHaftaGunleriList.Intersect(model.GHaftaGunleri).Any() && ((a._TalepBaslangcSaati <= model.TalepBaslangicSaat && a._TalepBitisSaati >= model.TalepBaslangicSaat) || (a._TalepBaslangcSaati <= model.TalepBitisSaat && a._TalepBitisSaati >= model.TalepBitisSaat))).ToList();
                if (varolanlar.Count > 0)
                {
                    var gunler = Management.cmbGetHaftaGunleri(false);
                    mmMessage.IsSuccess = false;
                    mmMessage.Messages.Add("Eklemeye çalıştığınız günlere ait saat aralıkları zaten bulunmaktadır talep saatleri zaten eklidir! Tekrar eklenemez!");
                }
            }
            mmMessage.MessageType = mmMessage.IsSuccess ? Msgtype.Success : Msgtype.Error;
            var strView = Management.RenderPartialView("Ajax", "getMessage", mmMessage);
            return Json(new { IsSuccess = mmMessage.IsSuccess, Messages = strView }, "application/json", JsonRequestBehavior.AllowGet);
        }
        [Authorize(Roles = RoleNames.BelgeTipleriSil)]
        public ActionResult Sil(int? id)
        {
            var data = db.BelgeTipDetays.Where(p => p.BelgeTipDetayID == id).FirstOrDefault();
            string message = "";
            bool success = true;
            if (data != null)
            {

                try
                {
                    message = "Belge Tip detay kaydı silindi!";
                    db.BelgeTipDetays.Remove(data);
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    success = false;
                    message = "Belge Tip detay kaydı silinemedi! <br/> Bilgi:" + ex.ToExceptionMessage();
                    Management.SistemBilgisiKaydet(message, "BelgeTipDetay/Sil<br/><br/>" + ex.ToExceptionStackTrace(), LogType.OnemsizHata);
                }
            }
            else
            {
                success = false;
                message = "Silmek istediğiniz Belge tip detayı sistemde bulunamadı!";
            }
            return Json(new { success = success, message = message }, "application/json", JsonRequestBehavior.AllowGet);
        }
    }
}