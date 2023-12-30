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
    [OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    [Authorize(Roles = RoleNames.MezuniyetYonetmelikler)]
    public class MezuniyetYonetmeliklerController : Controller
    {
        private readonly LisansustuBasvuruSistemiEntities _entities = new LisansustuBasvuruSistemiEntities();

        public ActionResult Index(string ekd)
        {
            return Index(new FmMezuniyetYonetmelikler() { PageSize = 10 }, ekd);
        }
        [HttpPost]
        public ActionResult Index(FmMezuniyetYonetmelikler model, string ekd)
        {
            var enstKods = UserIdentity.Current.EnstituKods ?? new List<string>();

            model.EnstituKod = EnstituBus.GetSelectedEnstitu(ekd);
            var q = from s in _entities.MezuniyetYonetmelikleris
                    join e in _entities.Enstitulers on s.EnstituKod equals e.EnstituKod
                    join d in _entities.Donemlers on s.DonemID equals d.DonemID
                    join k in _entities.Kullanicilars on s.IslemYapanID equals k.KullaniciID
                    where enstKods.Contains(e.EnstituKod) && s.EnstituKod == model.EnstituKod
                    select new
                    {
                        s.MezuniyetYonetmelikID,
                        s.EnstituKod,
                        e.EnstituAd,
                        s.TarihKriterID,
                        TarihKriterAdi = s.TarihKriterID == TarihKriterSecimEnum.SecilenTarihAraligi ? "Seçilen Tarih Aralığı" : (s.TarihKriterID == TarihKriterSecimEnum.SecilenTarihVeOncesi ? "Seçilen Tarih ve Öncesi" : "Seçilen Tarih ve Sonrası"),
                        s.BaslangicYil,
                        s.BitisYil,
                        s.DonemID,
                        d.DonemAdi,
                        s.BaslangicYilB,
                        s.BitisYilB,
                        s.DonemIDB,
                        s.IsAktif,
                        s.IslemTarihi,
                        s.IslemYapanID,
                        IslemYapan = k.Ad + " " + k.Soyad,
                        s.IslemYapanIP

                    };

            if (model.TarihKriterID.HasValue) q = q.Where(p => p.TarihKriterID == model.TarihKriterID);

            model.RowCount = q.Count();
            var indexModel = new MIndexBilgi
            {
                Toplam = model.RowCount
            };
            if (!model.Sort.IsNullOrWhiteSpace())
            {
                if (model.Sort.Contains("OgretimYiliB"))
                {
                    q = model.Sort.Contains(" DESC") ? q.OrderByDescending(o => o.BaslangicYilB).ThenByDescending(t => t.DonemIDB)
                        : q.OrderBy(o => o.BaslangicYilB).ThenBy(t => t.DonemIDB);
                }
                else if (model.Sort.Contains("OgretimYili"))
                {
                    q = model.Sort.Contains(" DESC") ? q.OrderByDescending(o => o.BaslangicYil).ThenByDescending(t => t.DonemID)
                        : q.OrderBy(o => o.BaslangicYil).ThenBy(t => t.DonemID);
                }
                else q = q.OrderBy(model.Sort);
            }
            else q = q.OrderByDescending(o => o.BaslangicYil).ThenByDescending(t => t.DonemID);

            var qdata = q.Skip(model.StartRowIndex).Take(model.PageSize).Select(s => new FrMezuniyetYonetmelikler
            {
                EnstituKod = s.EnstituKod,
                EnstituAdi = s.EnstituAd,
                TarihKriterID = s.TarihKriterID,
                TarihKriterAdi = s.TarihKriterAdi,
                BaslangicYil = s.BaslangicYil,
                BitisYil = s.BitisYil,
                DonemID = s.DonemID,
                DonemAdi = s.DonemAdi,
                BaslangicYilB = s.BaslangicYilB,
                BitisYilB = s.BitisYilB,
                DonemIDB = s.DonemIDB,
                MezuniyetYonetmelikID = s.MezuniyetYonetmelikID,
                IsAktif = s.IsAktif,
                IslemTarihi = s.IslemTarihi,
                IslemYapanID = s.IslemYapanID,
                IslemYapan = s.IslemYapan,
                IslemYapanIP = s.IslemYapanIP
            }).ToList();

            model.Data = qdata;
            ViewBag.EnstituKod = new SelectList(EnstituBus.GetCmbAktifEnstituler(true), "Value", "Caption", model.EnstituKod);
            ViewBag.IndexModel = indexModel;
            ViewBag.TarihKriterID = new SelectList(ComboData.GetCmbTarihKriterSecim(true), "Value", "Caption", model.TarihKriterID);

            return View(model);
        }
        public ActionResult Kayit(int? id, string dlgid, string ekd)
        {
            string enstituKod = EnstituBus.GetSelectedEnstitu(ekd);
            var mmMessage = new MmMessage
            {
                IsDialog = !dlgid.IsNullOrWhiteSpace(),
                DialogID = dlgid
            };
            ViewBag.MmMessage = mmMessage;
            var model = new KmMezuniyetYonetmelik
            {
                IsAktif = true,
                EnstituKod = enstituKod
            };
            if (id > 0)
            {
                var data = _entities.MezuniyetYonetmelikleris.First(p => p.MezuniyetYonetmelikID == id);
                enstituKod = data.EnstituKod;
                model.MezuniyetYonetmelikID = id.Value;
                model.EnstituKod = data.EnstituKod;
                model.TarihKriterID = data.TarihKriterID;
                model.BaslangicYil = data.BaslangicYil;
                model.BitisYil = data.BitisYil;
                model.DonemID = data.DonemID;
                model.BaslangicYilB = data.BaslangicYilB;
                model.BitisYilB = data.BitisYilB;
                model.DonemIDB = data.DonemIDB;
                model.IsAktif = data.IsAktif;
                model.OgretimYili = data.BaslangicYil + "/" + data.BitisYil + "/" + data.DonemID;
                if (model.TarihKriterID == TarihKriterSecimEnum.SecilenTarihAraligi) model.OgretimYiliB = data.BaslangicYilB + "/" + data.BitisYilB + "/" + data.DonemIDB;




            }
            model.KrMezuniyetYonetmelikOt = (from o in _entities.OgrenimTipleris.Where(p => p.EnstituKod == enstituKod && p.IsAktif && p.IsMezuniyetBasvurusuYapabilir)
                                             join yt in _entities.MezuniyetYayinTurleris on true equals yt.IsAktif
                                             join s in _entities.MezuniyetYonetmelikleriOTs on new { o.OgrenimTipKod, MezuniyetYonetmelikID = id ?? 0, yt.MezuniyetYayinTurID } equals new { s.OgrenimTipKod, s.MezuniyetYonetmelikID, s.MezuniyetYayinTurID } into def1
                                             from defS in def1.DefaultIfEmpty()
                                             join ot in _entities.OgrenimTipleris on o.OgrenimTipID equals ot.OgrenimTipID

                                             select new KrMezuniyetYonetmelikOt
                                             {
                                                 OgrenimTipKod = o.OgrenimTipKod,
                                                 OgrenimTipAdi = ot.OgrenimTipAdi,
                                                 MezuniyetYayinTurID = yt.MezuniyetYayinTurID,
                                                 MezuniyetYayinTurAdi = yt.MezuniyetYayinTurAdi,
                                                 IsGecerli = defS != null && defS.IsGecerli,
                                                 IsZorunlu = defS != null && defS.IsZorunlu,
                                                 GrupKodu = defS != null ? defS.GrupKodu : "Grup1"

                                             }).OrderBy(o => o.OgrenimTipAdi).ThenBy(t => t.MezuniyetYayinTurAdi).ToList();


            ViewBag.EnstituKod = new SelectList(EnstituBus.GetCmbAktifEnstituler(true), "Value", "Caption", model.EnstituKod ?? enstituKod);
            ViewBag.OgretimYili = new SelectList(DonemlerBus.GetCmbAkademikTarih(false, 5), "Value", "Caption", model.OgretimYili);
            ViewBag.OgretimYiliB = new SelectList(DonemlerBus.GetCmbAkademikTarih(false, 5), "Value", "Caption", model.OgretimYiliB);
            ViewBag.GrupKodu = ComboData.GetCmbGrupKod(model.KrMezuniyetYonetmelikOt.Select(s=>s.MezuniyetYayinTurID).Distinct().Count());
            ViewBag.VeVeya = ComboData.GecCmbVeVeya();
            ViewBag.TarihKriterID = new SelectList(ComboData.GetCmbTarihKriterSecim(false), "Value", "Caption", model.TarihKriterID);

            return View(model);
        }
        [HttpPost]
        public ActionResult Kayit(KmMezuniyetYonetmelik kModel, string dlgid = "")
        {
            var mmMessage = new MmMessage
            {
                IsDialog = !dlgid.IsNullOrWhiteSpace(),
                DialogID = dlgid
            };
            var ogrenimTipKods = kModel.OgrenimTipKods.Select((s, inx) => new { OgrenimTipKod = s, Inx = inx }).ToList();
            var mezuniyetYayinTurIds = kModel.MezuniyetYayinTurIds.Select((s, inx) => new { MezuniyetYayinTurID = s, Inx = inx }).ToList();
            var isGecerlis = kModel.IsGecerlis.Select((s, inx) => new { IsGecerli = s, Inx = inx }).ToList();
            var isZorunlus = kModel.IsZorunlus.Select((s, inx) => new { IsZorunlu = s, Inx = inx }).ToList();
            var grupKodus = kModel.GrupKodus.Select((s, inx) => new { GrupKodu = s, Inx = inx }).ToList();
            var qDetaylar = (from ogrenimTipKod in ogrenimTipKods
                             join mezuniyetYayinTurId in mezuniyetYayinTurIds on ogrenimTipKod.Inx equals mezuniyetYayinTurId.Inx
                             join isGecerli in isGecerlis on new { ogrenimTipKod.Inx } equals new { isGecerli.Inx }
                             join isZorunlu in isZorunlus on new { ogrenimTipKod.Inx } equals new { isZorunlu.Inx }
                             join grupKodu in grupKodus on new { ogrenimTipKod.Inx } equals new { grupKodu.Inx }
                             select new MezuniyetYonetmelikleriOT
                             {
                                 OgrenimTipKod = ogrenimTipKod.OgrenimTipKod,
                                 MezuniyetYayinTurID = mezuniyetYayinTurId.MezuniyetYayinTurID,
                                 IsGecerli = isGecerli.IsGecerli ?? false,
                                 IsZorunlu = isZorunlu.IsZorunlu ?? false,
                                 GrupKodu = grupKodu.GrupKodu
                             }).ToList();
            #region Kontrol
            if (kModel.EnstituKod.IsNullOrWhiteSpace())
            {
                mmMessage.Messages.Add("Enstitü Seçiniz");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "EnstituKod" });

            }
            else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "EnstituKod" });

            if (kModel.TarihKriterID <= 0)
            {
                mmMessage.Messages.Add("Tarih Kriteri Seçiniz");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "EnstituKod" });
            }
            else
            {
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "TarihKriterID" });

                if (kModel.TarihKriterID == TarihKriterSecimEnum.SecilenTarihAraligi)
                {
                    if (kModel.OgretimYili.IsNullOrWhiteSpace() || kModel.OgretimYiliB.IsNullOrWhiteSpace())
                    {
                        if (kModel.OgretimYili.IsNullOrWhiteSpace())
                        {
                            mmMessage.Messages.Add("Öğretim Yılı Seçiniz");
                            mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "OgretimYili" });
                        }
                        else
                        {
                            mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "OgretimYili" });
                        }
                        if (kModel.OgretimYiliB.IsNullOrWhiteSpace())
                        {
                            mmMessage.Messages.Add("Öğretim Yılı Bitişi Seçiniz");
                            mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "OgretimYiliB" });
                        }
                        else
                        {
                            mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "OgretimYiliB" });
                        }
                    }
                    else
                    {
                        var oyils = kModel.OgretimYili.Split('/');
                        kModel.BaslangicYil = oyils[0].ToInt().Value;
                        kModel.BitisYil = oyils[1].ToInt().Value;
                        kModel.DonemID = oyils[2].ToInt().Value;
                        var oyilsB = kModel.OgretimYiliB.Split('/');
                        kModel.BaslangicYilB = oyilsB[0].ToInt().Value;
                        kModel.BitisYilB = oyilsB[1].ToInt().Value;
                        kModel.DonemIDB = oyilsB[2].ToInt().Value;

                        if (kModel.BaslangicYil >= kModel.BaslangicYilB && kModel.DonemID >= kModel.DonemIDB)
                        {
                            mmMessage.Messages.Add("Öğretim Yılı Öğretim Yılı Bitişten büyük ya da eşit olamaz!");
                            mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "OgretimYili" });
                            mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "OgretimYiliB" });
                        }
                        else
                        {
                            mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "OgretimYili" });
                            mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "OgretimYiliB" });
                        }
                    }
                }
                else
                {
                    var oyils = kModel.OgretimYili.Split('/');
                    kModel.BaslangicYil = oyils[0].ToInt(0);
                    kModel.BitisYil = oyils[1].ToInt(0);
                    kModel.DonemID = oyils[2].ToInt(0);
                }
            }

            if (mmMessage.Messages.Count == 0)
            {
                bool isContains = false;
                var baslangic = Convert.ToDecimal(kModel.BaslangicYil + "," + kModel.DonemID);
                if (kModel.TarihKriterID != TarihKriterSecimEnum.SecilenTarihAraligi)
                {
                    isContains = _entities.MezuniyetYonetmelikleris.Where(p => p.EnstituKod == kModel.EnstituKod && p.MezuniyetYonetmelikID != kModel.MezuniyetYonetmelikID).ToList().Any(a =>
                                                                      a.TarihKriterID == TarihKriterSecimEnum.SecilenTarihVeOncesi ?
                                                                          Convert.ToDecimal(a.BaslangicYil + "," + a.DonemID) >= baslangic
                                                                            :
                                                                          a.TarihKriterID == TarihKriterSecimEnum.SecilenTarihVeSonrasi ?
                                                                              Convert.ToDecimal(a.BaslangicYil + "," + a.DonemID) <= baslangic
                                                                              :
                                                                              Convert.ToDecimal(a.BaslangicYil + "," + a.DonemID) <= baslangic && Convert.ToDecimal(a.BaslangicYilB + "," + a.DonemIDB) >= baslangic

                                                                );
                }
                else
                {
                    var baslangicB = Convert.ToDecimal(kModel.BaslangicYilB + "," + kModel.DonemIDB);
                    isContains = _entities.MezuniyetYonetmelikleris.Where(p => p.EnstituKod == kModel.EnstituKod && p.MezuniyetYonetmelikID != kModel.MezuniyetYonetmelikID).ToList().Any(a =>
                                                                           a.TarihKriterID == TarihKriterSecimEnum.SecilenTarihVeOncesi ?
                                                                               Convert.ToDecimal(a.BaslangicYil + "," + a.DonemID) >= baslangic
                                                                               :
                                                                               a.TarihKriterID == TarihKriterSecimEnum.SecilenTarihVeSonrasi ?
                                                                                   Convert.ToDecimal(a.BaslangicYil + "," + a.DonemID) <= baslangicB
                                                                                   :
                                                                                   (Convert.ToDecimal(a.BaslangicYil + "," + a.DonemID) <= baslangic && Convert.ToDecimal(a.BaslangicYilB + "," + a.DonemIDB) >= baslangic)
                                                                                   ||
                                                                                   (Convert.ToDecimal(a.BaslangicYil + "," + a.DonemID) <= baslangicB && Convert.ToDecimal(a.BaslangicYilB + "," + a.DonemIDB) >= baslangicB)
                                                                                   ||
                                                                                   (baslangic <= Convert.ToDecimal(a.BaslangicYil + "," + a.DonemID) && baslangicB >= Convert.ToDecimal(a.BaslangicYil + "," + a.DonemID))
                                                                                   ||
                                                                                   (baslangic <= Convert.ToDecimal(a.BaslangicYilB + "," + a.DonemIDB) && baslangicB >= Convert.ToDecimal(a.BaslangicYilB + "," + a.DonemIDB))
                                                                );
                }

                if (isContains)
                {
                    mmMessage.Messages.Add("Girmiş olduğunuz Öğretim yılı daha önceden kayıt edilmiş yönetmeliklerde geçen öğretim yılı ile çakışmaktadır!");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "OgretimYili" });
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "OgretimYiliB" });
                }
                else
                {
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "OgretimYili" });
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "OgretimYiliB" });
                }
                var ogrenimTipleris = _entities.OgrenimTipleris.Where(p => p.EnstituKod == kModel.EnstituKod && p.IsAktif && p.IsMezuniyetBasvurusuYapabilir).ToList();
                foreach (var item in ogrenimTipleris)
                {
                    if (qDetaylar.Any(p => p.OgrenimTipKod == item.OgrenimTipKod && p.IsGecerli) == false)
                    {
                        mmMessage.Messages.Add("Kayıt işlemi yapabilmeniz için " + item.OgrenimTipAdi + " öğrenim tipinden en az bir yayın tipini geçerli kılmanız gerekmektedir!");
                    }
                }


            }


            #endregion
            if (mmMessage.Messages.Count == 0)
            {
                var isnewOrEdit = kModel.MezuniyetYonetmelikID <= 0;
                kModel.IslemTarihi = DateTime.Now;
                kModel.IslemYapanID = UserIdentity.Current.Id;
                kModel.IslemYapanIP = UserIdentity.Ip;

                if (isnewOrEdit)
                {
                    kModel.IsAktif = true;
                    var eklenen = _entities.MezuniyetYonetmelikleris.Add(new MezuniyetYonetmelikleri
                    {
                        EnstituKod = kModel.EnstituKod,
                        TarihKriterID = kModel.TarihKriterID,
                        BaslangicYil = kModel.BaslangicYil,
                        BitisYil = kModel.BitisYil,
                        DonemID = kModel.DonemID,
                        BaslangicYilB = kModel.BaslangicYilB,
                        BitisYilB = kModel.BitisYilB,
                        DonemIDB = kModel.DonemIDB,
                        IsAktif = kModel.IsAktif,
                        IslemTarihi = kModel.IslemTarihi,
                        IslemYapanID = kModel.IslemYapanID,
                        IslemYapanIP = kModel.IslemYapanIP
                    });
                    _entities.SaveChanges();
                    kModel.MezuniyetYonetmelikID = eklenen.MezuniyetYonetmelikID;

                }
                else
                {
                    var data = _entities.MezuniyetYonetmelikleris.First(p => p.MezuniyetYonetmelikID == kModel.MezuniyetYonetmelikID);
                    data.EnstituKod = kModel.EnstituKod;
                    data.TarihKriterID = kModel.TarihKriterID;
                    data.BaslangicYil = kModel.BaslangicYil;
                    data.BitisYil = kModel.BitisYil;
                    data.DonemID = kModel.DonemID;
                    data.BaslangicYilB = kModel.BaslangicYilB;
                    data.BitisYilB = kModel.BitisYilB;
                    data.DonemIDB = kModel.DonemIDB;
                    data.IsAktif = kModel.IsAktif;
                    data.IslemTarihi = DateTime.Now;
                    data.IslemYapanID = kModel.IslemYapanID;
                    data.IslemYapanIP = kModel.IslemYapanIP;

                    var mzOts = data.MezuniyetYonetmelikleriOTs.ToList();
                    _entities.MezuniyetYonetmelikleriOTs.RemoveRange(mzOts);
                }

                foreach (var item in qDetaylar.Where(p => p.IsGecerli))
                {
                    _entities.MezuniyetYonetmelikleriOTs.Add(new MezuniyetYonetmelikleriOT
                    {
                        MezuniyetYonetmelikID = kModel.MezuniyetYonetmelikID,
                        OgrenimTipKod = item.OgrenimTipKod,
                        MezuniyetYayinTurID = item.MezuniyetYayinTurID,
                        IsGecerli = item.IsGecerli,
                        IsZorunlu = item.IsZorunlu,
                        GrupKodu = item.GrupKodu
                    });

                }

                _entities.SaveChanges();

                return RedirectToAction("Index");
            }


            var qdata = (from o in _entities.OgrenimTipleris.Where(p => p.EnstituKod == kModel.EnstituKod && p.IsAktif && p.IsMezuniyetBasvurusuYapabilir)
                         join yt in _entities.MezuniyetYayinTurleris on true equals yt.IsAktif
                         join ot in _entities.OgrenimTipleris on o.OgrenimTipID equals ot.OgrenimTipID

                         select new KrMezuniyetYonetmelikOt
                         {
                             OgrenimTipKod = o.OgrenimTipKod,
                             OgrenimTipAdi = ot.OgrenimTipAdi,
                             MezuniyetYayinTurID = yt.MezuniyetYayinTurID,
                             MezuniyetYayinTurAdi = yt.MezuniyetYayinTurAdi

                         }).OrderBy(o => o.OgrenimTipAdi).ThenBy(t => t.MezuniyetYayinTurAdi).ToList();
            foreach (var item in qdata)
            {
                var qdetay = qDetaylar.FirstOrDefault(p => p.OgrenimTipKod == item.OgrenimTipKod && p.MezuniyetYayinTurID == item.MezuniyetYayinTurID);
                if (qdetay != null)
                {
                    item.IsGecerli = qdetay.IsGecerli;
                    item.IsZorunlu = qdetay.IsZorunlu;
                    item.GrupKodu = qdetay.GrupKodu;
                }

            }
            kModel.KrMezuniyetYonetmelikOt = qdata;
            MessageBox.Show("Uyarı", MessageBox.MessageType.Warning, mmMessage.Messages.ToArray());



            ViewBag.EnstituKod = new SelectList(EnstituBus.GetCmbAktifEnstituler(true), "Value", "Caption", kModel.EnstituKod);
            ViewBag.OgretimYili = new SelectList(DonemlerBus.GetCmbAkademikTarih(false, 5), "Value", "Caption", kModel.OgretimYili);
            ViewBag.OgretimYiliB = new SelectList(DonemlerBus.GetCmbAkademikTarih(false, 5), "Value", "Caption", kModel.OgretimYiliB);
            ViewBag.GrupKodu = ComboData.GetCmbGrupKod(qdata.Select(s=>s.MezuniyetYayinTurID).Distinct().Count());
            ViewBag.VeVeya = ComboData.GecCmbVeVeya();
            ViewBag.TarihKriterID = new SelectList(ComboData.GetCmbTarihKriterSecim(false), "Value", "Caption");
            ViewBag.MmMessage = mmMessage;
            return View(kModel);
        }


        public ActionResult GetMyDetail(int id)
        {

            var model = new KmMezuniyetYonetmelik();
            var data = _entities.MezuniyetYonetmelikleris.FirstOrDefault(p => p.MezuniyetYonetmelikID == id);
            model.EnstituAdi = data.Enstituler.EnstituAd;
            model.MezuniyetYonetmelikID = data.MezuniyetYonetmelikID;
            model.EnstituKod = data.EnstituKod;
            model.TarihKriterID = data.TarihKriterID;
            model.BaslangicYil = data.BaslangicYil;
            model.BitisYil = data.BitisYil;
            model.DonemID = data.DonemID;
            model.IsAktif = data.IsAktif;
            model.OgretimYili = data.BaslangicYil + "/" + data.BitisYil + " " + data.Donemler.DonemAdi;
            if (data.TarihKriterID == TarihKriterSecimEnum.SecilenTarihAraligi) model.OgretimYiliB = data.BaslangicYilB + "/" + data.BitisYilB + " " + data.Donemler1.DonemAdi;
            model.TarihKriterAdi = ComboData.GetCmbTarihKriterSecim().First(p => p.Value == data.TarihKriterID).Caption;

            model.IslemTarihi = data.IslemTarihi;
            model.IslemYapanID = data.IslemYapanID;
            model.IslemYapanIP = data.IslemYapanIP;
            model.IslemYapan = data.Kullanicilar.KullaniciAdi;

            model.KrMezuniyetYonetmelikOt = (from o in _entities.OgrenimTipleris.Where(p => p.EnstituKod == data.EnstituKod && p.IsAktif)
                                             join yt in _entities.MezuniyetYayinTurleris on true equals yt.IsAktif
                                             join s in _entities.MezuniyetYonetmelikleriOTs on new { o.OgrenimTipKod, MezuniyetYonetmelikID = id, yt.MezuniyetYayinTurID } equals new { s.OgrenimTipKod, s.MezuniyetYonetmelikID, s.MezuniyetYayinTurID } into def1
                                             from defS in def1.DefaultIfEmpty()
                                             join ot in _entities.OgrenimTipleris on o.OgrenimTipID equals ot.OgrenimTipID
                                             select new KrMezuniyetYonetmelikOt
                                             {
                                                 OgrenimTipKod = o.OgrenimTipKod,
                                                 OgrenimTipAdi = ot.OgrenimTipAdi,
                                                 MezuniyetYayinTurID = yt.MezuniyetYayinTurID,
                                                 MezuniyetYayinTurAdi = yt.MezuniyetYayinTurAdi,
                                                 IsGecerli = defS != null && defS.IsGecerli,
                                                 IsZorunlu = defS != null && defS.IsZorunlu,
                                                 GrupKodu = defS != null ? defS.GrupKodu : "Grup1",

                                             }).OrderBy(o => o.OgrenimTipAdi).ThenBy(t => t.MezuniyetYayinTurAdi).ToList();

            return View(model);
        }

        public ActionResult GetMsSubData(int id, int tbInx, bool isDelete)
        {

            string page = "";
            var mdl = (from s in _entities.MezuniyetSurecis.Where(p => p.MezuniyetSurecID == id)
                       join k in _entities.Kullanicilars on s.IslemYapanID equals k.KullaniciID
                       join e in _entities.Enstitulers on s.EnstituKod equals e.EnstituKod
                       join d in _entities.Donemlers on s.DonemID equals d.DonemID
                       select new MSurecDetay
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
            if (tbInx == 1)
            {
                #region AnaBilgi
                var indexModel = new MIndexBilgi();
                var btDurulari = MezuniyetBus.GetMezuniyetYayinDurumListe(new List<int>() { BasvuruDurumuEnum.Taslak, BasvuruDurumuEnum.Onaylandı });
                foreach (var item in btDurulari)
                {
                    var tipCount = _entities.MezuniyetBasvurularis.Count(p => p.MezuniyetSurecID == mdl.MezuniyetSurecID && p.MezuniyetYayinKontrolDurumID == item.MezuniyetYayinKontrolDurumID);
                    indexModel.ListB.Add(new mxRowModel { ID = item.MezuniyetYayinKontrolDurumID, Key = item.MezuniyetYayinKontrolDurumAdi, ClassName = item.ClassName, Color = item.Color, Toplam = tipCount });
                }
                indexModel.Toplam = indexModel.ListB.Sum(s => s.Toplam);
                mdl.ToplamBasvuruBilgisi = indexModel;

                #endregion
                page = ViewRenderHelper.RenderPartialView("MezuniyetSureci", "getMsDetAnaBilgi", mdl);
            }

            return Content(page, "text/html");
        }




        [Authorize(Roles = RoleNames.MezuniyetSureciKayıt)]
        public void YayinTurleriniKopyala(int mezuniyetSurecId, string enstituKod)
        {
            var mbsstOld = _entities.MezuniyetSureciYayinTurleris.Where(p => p.MezuniyetSurecID == mezuniyetSurecId).ToList();
            _entities.MezuniyetSureciYayinTurleris.RemoveRange(mbsstOld);
            var yturs = _entities.MezuniyetYayinTurleris.ToList();

            foreach (var item in yturs)
            {
                var bsst = _entities.MezuniyetSureciYayinTurleris.Add(new MezuniyetSureciYayinTurleri
                {
                    MezuniyetSurecID = mezuniyetSurecId,
                    MezuniyetYayinTurID = item.MezuniyetYayinTurID,
                    MezuniyetYayinBelgeTurID = item.MezuniyetYayinBelgeTurID,
                    BelgeZorunlu = item.MezuniyetYayinBelgeTurID.HasValue && item.BelgeZorunlu,
                    KaynakMezuniyetYayinLinkTurID = item.KaynakMezuniyetYayinLinkTurID,
                    KaynakLinkiZorunlu = item.KaynakMezuniyetYayinLinkTurID.HasValue && item.KaynakLinkiZorunlu,
                    MezuniyetYayinMetinTurID = item.MezuniyetYayinMetinTurID,
                    MetinZorunlu = item.MezuniyetYayinMetinTurID.HasValue && item.MetinZorunlu,
                    YayinMezuniyetYayinLinkTurID = item.YayinMezuniyetYayinLinkTurID,
                    YayinLinkiZorunlu = item.YayinMezuniyetYayinLinkTurID.HasValue && item.YayinLinkiZorunlu,
                    YayinIndexTurIstensin = item.YayinIndexTurIstensin,
                    IsAktif = item.IsAktif,
                    IslemYapanID = UserIdentity.Current.Id,
                    IslemYapanIP = UserIdentity.Ip,
                    IslemTarihi = DateTime.Now
                });
            }
            _entities.SaveChanges();
        }


    }
}