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
    [Authorize(Roles = RoleNames.MezuniyetYonetmelikler)]
    public class MezuniyetYonetmeliklerController : Controller
    {
        private LisansustuBasvuruSistemiEntities db = new LisansustuBasvuruSistemiEntities();

        public ActionResult Index(string EKD)
        {
            return Index(new fmMezuniyetYonetmelikler() { PageSize = 10 }, EKD);
        }
        [HttpPost]
        public ActionResult Index(fmMezuniyetYonetmelikler model, string EKD)
        {
            var EnstKods = UserIdentity.Current.EnstituKods ?? new List<string>();

            var _EnstituKod = EnstituBus.GetSelectedEnstitu(EKD);
            var q = from s in db.MezuniyetYonetmelikleris
                    join e in db.Enstitulers on s.EnstituKod equals e.EnstituKod
                    join d in db.Donemlers on s.DonemID equals d.DonemID
                    join k in db.Kullanicilars on s.IslemYapanID equals k.KullaniciID
                    where EnstKods.Contains(e.EnstituKod) && s.EnstituKod == _EnstituKod
                    select new
                    {
                        s.MezuniyetYonetmelikID,
                        s.EnstituKod,
                        e.EnstituAd,
                        s.TarihKriterID,
                        TarihKriterAdi = s.TarihKriterID == TarihKriterSecim.SecilenTarihAraligi ? "Seçilen Tarih Aralığı" : (s.TarihKriterID == TarihKriterSecim.SecilenTarihVeOncesi ? "Seçilen Tarih ve Öncesi" : "Seçilen Tarih ve Sonrası"),
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

            if (!model.EnstituKod.IsNullOrWhiteSpace()) q = q.Where(p => p.EnstituKod == model.EnstituKod);
            if (model.TarihKriterID.HasValue) q = q.Where(p => p.TarihKriterID == model.TarihKriterID);

            model.RowCount = q.Count();
            var IndexModel = new MIndexBilgi();
            IndexModel.Toplam = model.RowCount;
            if (!model.Sort.IsNullOrWhiteSpace())
            {
                if (model.Sort.Contains("OgretimYiliB"))
                {
                    if (model.Sort.Contains(" DESC")) q = q.OrderByDescending(o => o.BaslangicYilB).ThenByDescending(t => t.DonemIDB);
                    else q = q.OrderBy(o => o.BaslangicYilB).ThenBy(t => t.DonemIDB);
                }
                else if (model.Sort.Contains("OgretimYili"))
                {
                    if (model.Sort.Contains(" DESC")) q = q.OrderByDescending(o => o.BaslangicYil).ThenByDescending(t => t.DonemID);
                    else q = q.OrderBy(o => o.BaslangicYil).ThenBy(t => t.DonemID);
                }
                else q = q.OrderBy(model.Sort);
            }
            else q = q.OrderByDescending(o => o.BaslangicYil).ThenByDescending(t => t.DonemID);

            var qdata = q.Skip(model.StartRowIndex).Take(model.PageSize).Select(s => new frMezuniyetYonetmelikler
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
            ViewBag.IndexModel = IndexModel;
            ViewBag.TarihKriterID = new SelectList(ComboData.GetCmbTarihKriterSecim(true), "Value", "Caption", model.TarihKriterID);

            return View(model);
        }
        public ActionResult Kayit(int? id, string dlgid, string EKD)
        {
            string _EnstituKod = EnstituBus.GetSelectedEnstitu(EKD);
            var MmMessage = new MmMessage();
            MmMessage.IsDialog = !dlgid.IsNullOrWhiteSpace();
            MmMessage.DialogID = dlgid;
            ViewBag.MmMessage = MmMessage;
            var model = new kmMezuniyetYonetmelik();
            model.IsAktif = true;
            model.EnstituKod = _EnstituKod;
            var yayinturCount = 5;
            if (id.HasValue && id > 0)
            {
                var data = db.MezuniyetYonetmelikleris.Where(p => p.MezuniyetYonetmelikID == id).FirstOrDefault();


                if (data != null)
                {
                    _EnstituKod = data.EnstituKod;
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
                    if (model.TarihKriterID == TarihKriterSecim.SecilenTarihAraligi) model.OgretimYiliB = data.BaslangicYilB + "/" + data.BitisYilB + "/" + data.DonemIDB;


                    model.krMezuniyetYonetmelikOT = (from o in db.OgrenimTipleris.Where(p => p.EnstituKod == _EnstituKod && p.IsAktif && p.IsMezuniyetBasvurusuYapabilir)
                                                     join yt in db.MezuniyetYayinTurleris on true equals yt.IsAktif
                                                     join s in db.MezuniyetYonetmelikleriOTs on new { o.OgrenimTipKod, MezuniyetYonetmelikID = id.Value, yt.MezuniyetYayinTurID } equals new { s.OgrenimTipKod, s.MezuniyetYonetmelikID, s.MezuniyetYayinTurID } into def1
                                                     from defS in def1.DefaultIfEmpty()
                                                     join ot in db.OgrenimTipleris on o.OgrenimTipID equals ot.OgrenimTipID
                                                     select new krMezuniyetYonetmelikOT
                                                     {
                                                         OgrenimTipKod = o.OgrenimTipKod,
                                                         OgrenimTipAdi = ot.OgrenimTipAdi,
                                                         MezuniyetYayinTurID = yt.MezuniyetYayinTurID,
                                                         MezuniyetYayinTurAdi = yt.MezuniyetYayinTurAdi,
                                                         IsGecerli = defS != null ? defS.IsGecerli : false,
                                                         IsZorunlu = defS != null ? defS.IsZorunlu : false,
                                                         GrupKodu = defS != null ? defS.GrupKodu : null,
                                                         IsVeOrVeya = defS != null ? defS.IsVeOrVeya : null,

                                                     }).OrderBy(o => o.OgrenimTipAdi).ThenBy(t => t.MezuniyetYayinTurAdi).ToList();
                }

            }
            else
            {
                model.krMezuniyetYonetmelikOT = (from o in db.OgrenimTipleris.Where(p => p.EnstituKod == _EnstituKod && p.IsAktif && p.IsMezuniyetBasvurusuYapabilir)
                                                 join yt in db.MezuniyetYayinTurleris on true equals yt.IsAktif
                                                 join s in db.MezuniyetYonetmelikleriOTs on new { o.OgrenimTipKod, MezuniyetYonetmelikID = id ?? 0, yt.MezuniyetYayinTurID } equals new { s.OgrenimTipKod, s.MezuniyetYonetmelikID, s.MezuniyetYayinTurID } into def1
                                                 from defS in def1.DefaultIfEmpty()
                                                 join ot in db.OgrenimTipleris on o.OgrenimTipID equals ot.OgrenimTipID

                                                 select new krMezuniyetYonetmelikOT
                                                 {
                                                     OgrenimTipKod = o.OgrenimTipKod,
                                                     OgrenimTipAdi = ot.OgrenimTipAdi,
                                                     MezuniyetYayinTurID = yt.MezuniyetYayinTurID,
                                                     MezuniyetYayinTurAdi = yt.MezuniyetYayinTurAdi,
                                                     IsGecerli = defS != null ? defS.IsGecerli : false,
                                                     IsZorunlu = defS != null ? defS.IsZorunlu : false,
                                                     GrupKodu = defS != null ? defS.GrupKodu : null,
                                                     IsVeOrVeya = defS != null ? defS.IsVeOrVeya : null,

                                                 }).OrderBy(o => o.OgrenimTipAdi).ThenBy(t => t.MezuniyetYayinTurAdi).ToList();
            }

            ViewBag.EnstituKod = new SelectList(EnstituBus.GetCmbAktifEnstituler(true), "Value", "Caption", model.EnstituKod ?? _EnstituKod);
            ViewBag.OgretimYili = new SelectList(DonemlerBus.GetCmbAkademikTarih(false, 5), "Value", "Caption", model.OgretimYili);
            ViewBag.OgretimYiliB = new SelectList(DonemlerBus.GetCmbAkademikTarih(false, 5), "Value", "Caption", model.OgretimYiliB);
            ViewBag.GrupKodu = ComboData.GetCmbGrupKod(yayinturCount, "Grup", true);
            ViewBag.VeVeya = ComboData.GecCmbVeVeya(true);
            ViewBag.TarihKriterID = new SelectList(ComboData.GetCmbTarihKriterSecim(false), "Value", "Caption", model.TarihKriterID);

            return View(model);
        }
        [HttpPost]
        public ActionResult Kayit(kmMezuniyetYonetmelik kModel, string dlgid = "")
        {
            var MmMessage = new MmMessage();
            MmMessage.IsDialog = !dlgid.IsNullOrWhiteSpace();
            MmMessage.DialogID = dlgid;
            var yayinturCount = 5;
            var qOtipKod = kModel._MezuniyetYayinTurID.Select((s, inx) => new { OgrenimTipKod = s.Split('_')[0].ToInt().Value, Inx = inx }).ToList();
            var qgID = kModel._MezuniyetYayinTurID.Select((s, inx) => new { OgrenimTipKod = s.Split('_')[0].ToInt().Value, MezuniyetYayinTurID = s.Split('_')[1].ToInt().Value, Inx = inx }).ToList();
            var qGecerli = kModel._IsGecerli.Select((s, inx) => new { OgrenimTipKod = s.Split('_')[0].ToInt().Value, MezuniyetYayinTurID = s.Split('_')[1].ToInt().Value, IsGecerli = s.Split('_')[2].toIntToBooleanObj(), Inx = inx }).ToList();
            var qZorunlu = kModel._IsZorunlu.Select((s, inx) => new { OgrenimTipKod = s.Split('_')[0].ToInt().Value, MezuniyetYayinTurID = s.Split('_')[1].ToInt().Value, IsZorunlu = s.Split('_')[2].toIntToBooleanObj(), Inx = inx }).ToList();
            var qGrpKodu = kModel._GrupKodu.Select((s, inx) => new { OgrenimTipKod = s.Split('_')[0].ToInt().Value, MezuniyetYayinTurID = s.Split('_')[1].ToInt().Value, GrupKodu = s.Split('_')[2].toStrObj(), Inx = inx }).ToList();
            var qVeVeya = kModel._IsVeOrVeya.Select((s, inx) => new { OgrenimTipKod = s.Split('_')[0].ToInt().Value, MezuniyetYayinTurID = s.Split('_')[1].ToInt().Value, IsVeOrVeya = s.Split('_')[2].toBooleanObj(), Inx = inx }).ToList();
            var qDetaylar = (from s in qOtipKod
                             join b in qgID on new { s.OgrenimTipKod, s.Inx } equals new { b.OgrenimTipKod, b.Inx }
                             join g in qGecerli on new { b.OgrenimTipKod, b.MezuniyetYayinTurID, s.Inx } equals new { g.OgrenimTipKod, g.MezuniyetYayinTurID, g.Inx }
                             join z in qZorunlu on new { g.OgrenimTipKod, g.MezuniyetYayinTurID, g.Inx } equals new { z.OgrenimTipKod, z.MezuniyetYayinTurID, z.Inx }
                             join gr in qGrpKodu on new { z.OgrenimTipKod, z.MezuniyetYayinTurID, z.Inx } equals new { gr.OgrenimTipKod, gr.MezuniyetYayinTurID, gr.Inx }
                             join v in qVeVeya on new { gr.OgrenimTipKod, gr.MezuniyetYayinTurID, gr.Inx } equals new { v.OgrenimTipKod, v.MezuniyetYayinTurID, v.Inx }
                             select new MezuniyetYonetmelikleriOT
                             {
                                 OgrenimTipKod = s.OgrenimTipKod,
                                 MezuniyetYayinTurID = b.MezuniyetYayinTurID,
                                 IsGecerli = g.IsGecerli ?? false,
                                 IsZorunlu = z.IsZorunlu ?? false,
                                 GrupKodu = gr.GrupKodu,
                                 IsVeOrVeya = v.IsVeOrVeya,
                             }).ToList();

            #region Kontrol
            if (kModel.EnstituKod.IsNullOrWhiteSpace())
            {
                string msg = "Enstitü Seçiniz";
                MmMessage.Messages.Add(msg);
                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "EnstituKod" });

            }
            else MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "EnstituKod" });

            if (kModel.TarihKriterID <= 0)
            {
                string msg = "Tarih Kriteri Seçiniz";
                MmMessage.Messages.Add(msg);
                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "EnstituKod" });
            }
            else
            {
                MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "TarihKriterID" });

                if (kModel.TarihKriterID == TarihKriterSecim.SecilenTarihAraligi)
                {
                    if (kModel.OgretimYili.IsNullOrWhiteSpace() || kModel.OgretimYiliB.IsNullOrWhiteSpace())
                    {
                        if (kModel.OgretimYili.IsNullOrWhiteSpace())
                        {
                            string msg = "Öğretim Yılı Seçiniz";
                            MmMessage.Messages.Add(msg);
                            MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "OgretimYili" });
                        }
                        else
                        {
                            MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "OgretimYili" });
                        }
                        if (kModel.OgretimYiliB.IsNullOrWhiteSpace())
                        {
                            string msg = "Öğretim Yılı Bitişi Seçiniz";
                            MmMessage.Messages.Add(msg);
                            MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "OgretimYiliB" });
                        }
                        else
                        {
                            MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "OgretimYiliB" });
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
                            string msg = "Öğretim Yılı Öğretim Yılı Bitişten büyük ya da eşit olamaz!";
                            MmMessage.Messages.Add(msg);
                            MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "OgretimYili" });
                            MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "OgretimYiliB" });
                        }
                        else
                        {
                            MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "OgretimYili" });
                            MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "OgretimYiliB" });
                        }
                    }
                }
                else
                {
                    var oyils = kModel.OgretimYili.Split('/');
                    kModel.BaslangicYil = oyils[0].ToInt().Value;
                    kModel.BitisYil = oyils[1].ToInt().Value;
                    kModel.DonemID = oyils[2].ToInt().Value;
                }
            }

            if (MmMessage.Messages.Count == 0)
            {
                bool IsContains = false;
                var baslangic = Convert.ToDecimal(kModel.BaslangicYil + "," + kModel.DonemID);
                if (kModel.TarihKriterID != TarihKriterSecim.SecilenTarihAraligi)
                {
                    IsContains = db.MezuniyetYonetmelikleris.Where(p => p.EnstituKod == kModel.EnstituKod && p.MezuniyetYonetmelikID != kModel.MezuniyetYonetmelikID).ToList().Any(a =>
                                                                      a.TarihKriterID == TarihKriterSecim.SecilenTarihVeOncesi ?
                                                                          (Convert.ToDecimal(a.BaslangicYil + "," + a.DonemID) >= baslangic)
                                                                            :
                                                                          (a.TarihKriterID == TarihKriterSecim.SecilenTarihVeSonrasi ?
                                                                                (Convert.ToDecimal(a.BaslangicYil + "," + a.DonemID) <= baslangic)
                                                                               :
                                                                                (
                                                                                    (Convert.ToDecimal(a.BaslangicYil + "," + a.DonemID) <= baslangic && Convert.ToDecimal(a.BaslangicYilB + "," + a.DonemIDB) >= baslangic)
                                                                                )
                                                                          )

                                                                );
                }
                else
                {
                    var baslangicB = Convert.ToDecimal(kModel.BaslangicYilB + "," + kModel.DonemIDB);
                    IsContains = db.MezuniyetYonetmelikleris.Where(p => p.EnstituKod == kModel.EnstituKod && p.MezuniyetYonetmelikID != kModel.MezuniyetYonetmelikID).ToList().Any(a =>
                                                                           (a.TarihKriterID == TarihKriterSecim.SecilenTarihVeOncesi ?
                                                                               (
                                                                                 Convert.ToDecimal(a.BaslangicYil + "," + a.DonemID) >= baslangic
                                                                               )
                                                                                 :
                                                                               (a.TarihKriterID == TarihKriterSecim.SecilenTarihVeSonrasi ?
                                                                                     (Convert.ToDecimal(a.BaslangicYil + "," + a.DonemID) <= baslangicB)
                                                                                    :
                                                                                     (
                                                                                         (Convert.ToDecimal(a.BaslangicYil + "," + a.DonemID) <= baslangic && Convert.ToDecimal(a.BaslangicYilB + "," + a.DonemIDB) >= baslangic)
                                                                                         ||
                                                                                         (Convert.ToDecimal(a.BaslangicYil + "," + a.DonemID) <= baslangicB && Convert.ToDecimal(a.BaslangicYilB + "," + a.DonemIDB) >= baslangicB)
                                                                                         ||
                                                                                         (baslangic <= Convert.ToDecimal(a.BaslangicYil + "," + a.DonemID) && baslangicB >= Convert.ToDecimal(a.BaslangicYil + "," + a.DonemID))
                                                                                         ||
                                                                                         (baslangic <= Convert.ToDecimal(a.BaslangicYilB + "," + a.DonemIDB) && baslangicB >= Convert.ToDecimal(a.BaslangicYilB + "," + a.DonemIDB))
                                                                                     )
                                                                               )
                                                                           )
                                                                );
                }

                if (IsContains)
                {
                    string msg = "Girmiş olduğunuz Öğretim yılı daha önceden kayıt edilmiş yönetmeliklerde geçen öğretim yılı ile çakışmaktadır!";
                    MmMessage.Messages.Add(msg);
                    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "OgretimYili" });
                    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "OgretimYiliB" });
                }
                else
                {
                    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "OgretimYili" });
                    MmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Success, PropertyName = "OgretimYiliB" });
                }
                var OgrenimTipleris = db.OgrenimTipleris.Where(p => p.EnstituKod == kModel.EnstituKod && p.IsAktif && p.IsMezuniyetBasvurusuYapabilir).ToList();
                foreach (var item in OgrenimTipleris)
                {
                    if (qDetaylar.Any(p => p.OgrenimTipKod == item.OgrenimTipKod && p.IsGecerli) == false)
                    {
                        string msg = "Kayıt işlemi yapabilmeniz için " + item.OgrenimTipAdi + " öğrenim tipinden en az bir yayın tipini geçerli kılmanız gerekmektedir!";
                        MmMessage.Messages.Add(msg);
                    }
                }


            }


            #endregion
            if (MmMessage.Messages.Count == 0)
            {
                bool IsnewOrEdit = kModel.MezuniyetYonetmelikID <= 0;
                kModel.IslemTarihi = DateTime.Now;
                kModel.IslemYapanID = UserIdentity.Current.Id;
                kModel.IslemYapanIP = UserIdentity.Ip;

                if (IsnewOrEdit)
                {
                    kModel.IsAktif = true;
                    var eklenen = db.MezuniyetYonetmelikleris.Add(new MezuniyetYonetmelikleri
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
                    db.SaveChanges();
                    kModel.MezuniyetYonetmelikID = eklenen.MezuniyetYonetmelikID;

                }
                else
                {
                    var data = db.MezuniyetYonetmelikleris.Where(p => p.MezuniyetYonetmelikID == kModel.MezuniyetYonetmelikID).First();
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
                    db.MezuniyetYonetmelikleriOTs.RemoveRange(mzOts);
                }

                foreach (var item in qDetaylar.Where(p => p.IsGecerli))
                {
                    db.MezuniyetYonetmelikleriOTs.Add(new MezuniyetYonetmelikleriOT
                    {
                        MezuniyetYonetmelikID = kModel.MezuniyetYonetmelikID,
                        OgrenimTipKod = item.OgrenimTipKod,
                        MezuniyetYayinTurID = item.MezuniyetYayinTurID,
                        IsGecerli = item.IsGecerli,
                        IsZorunlu = item.IsZorunlu,
                        GrupKodu = item.GrupKodu,
                        IsVeOrVeya = item.IsVeOrVeya
                    });

                }

                db.SaveChanges();

                return RedirectToAction("Index");
            }
            else
            {

                var qdata = (from o in db.OgrenimTipleris.Where(p => p.EnstituKod == kModel.EnstituKod && p.IsAktif && p.IsMezuniyetBasvurusuYapabilir)
                             join yt in db.MezuniyetYayinTurleris on true equals yt.IsAktif
                             join ot in db.OgrenimTipleris on o.OgrenimTipID equals ot.OgrenimTipID

                             select new krMezuniyetYonetmelikOT
                             {
                                 OgrenimTipKod = o.OgrenimTipKod,
                                 OgrenimTipAdi = ot.OgrenimTipAdi,
                                 MezuniyetYayinTurID = yt.MezuniyetYayinTurID,
                                 MezuniyetYayinTurAdi = yt.MezuniyetYayinTurAdi

                             }).OrderBy(o => o.OgrenimTipAdi).ThenBy(t => t.MezuniyetYayinTurAdi).ToList();
                foreach (var item in qdata)
                {
                    var qdetay = qDetaylar.Where(p => p.OgrenimTipKod == item.OgrenimTipKod && p.MezuniyetYayinTurID == item.MezuniyetYayinTurID).FirstOrDefault();
                    if (qdetay != null)
                    {
                        item.IsGecerli = qdetay.IsGecerli;
                        item.IsZorunlu = qdetay.IsZorunlu;
                        item.GrupKodu = qdetay.GrupKodu;
                        item.IsVeOrVeya = qdetay.IsVeOrVeya;
                    }

                }
                kModel.krMezuniyetYonetmelikOT = qdata;
                MessageBox.Show("Uyarı", MessageBox.MessageType.Warning, MmMessage.Messages.ToArray());
            }


            ViewBag.EnstituKod = new SelectList(EnstituBus.GetCmbAktifEnstituler(true), "Value", "Caption", kModel.EnstituKod);
            ViewBag.OgretimYili = new SelectList(DonemlerBus.GetCmbAkademikTarih(false, 5), "Value", "Caption", kModel.OgretimYili);
            ViewBag.OgretimYiliB = new SelectList(DonemlerBus.GetCmbAkademikTarih(false, 5), "Value", "Caption", kModel.OgretimYiliB);
            ViewBag.GrupKodu = ComboData.GetCmbGrupKod(yayinturCount, "Grup", true);
            ViewBag.VeVeya = ComboData.GecCmbVeVeya(true);
            ViewBag.TarihKriterID = new SelectList(ComboData.GetCmbTarihKriterSecim(false), "Value", "Caption");
            ViewBag.MmMessage = MmMessage;
            return View(kModel);
        }


        public ActionResult getMyDetail(int id)
        {

            var model = new kmMezuniyetYonetmelik();
            var data = db.MezuniyetYonetmelikleris.Where(p => p.MezuniyetYonetmelikID == id).FirstOrDefault();
            model.EnstituAdi = data.Enstituler.EnstituAd;
            model.MezuniyetYonetmelikID = data.MezuniyetYonetmelikID;
            model.EnstituKod = data.EnstituKod;
            model.TarihKriterID = data.TarihKriterID;
            model.BaslangicYil = data.BaslangicYil;
            model.BitisYil = data.BitisYil;
            model.DonemID = data.DonemID;
            model.IsAktif = data.IsAktif;
            model.OgretimYili = data.BaslangicYil + "/" + data.BitisYil + " " + data.Donemler.DonemAdi;
            if (data.TarihKriterID == TarihKriterSecim.SecilenTarihAraligi) model.OgretimYiliB = data.BaslangicYilB + "/" + data.BitisYilB + " " + data.Donemler1.DonemAdi;
            model.TarihKriterAdi = ComboData.GetCmbTarihKriterSecim().Where(p => p.Value == data.TarihKriterID).First().Caption;

            model.IslemTarihi = data.IslemTarihi;
            model.IslemYapanID = data.IslemYapanID;
            model.IslemYapanIP = data.IslemYapanIP;
            model.IslemYapan = data.Kullanicilar.KullaniciAdi;

            model.krMezuniyetYonetmelikOT = (from o in db.OgrenimTipleris.Where(p => p.EnstituKod == data.EnstituKod && p.IsAktif)
                                             join yt in db.MezuniyetYayinTurleris on true equals yt.IsAktif
                                             join s in db.MezuniyetYonetmelikleriOTs on new { o.OgrenimTipKod, MezuniyetYonetmelikID = id, yt.MezuniyetYayinTurID } equals new { s.OgrenimTipKod, s.MezuniyetYonetmelikID, s.MezuniyetYayinTurID } into def1
                                             from defS in def1.DefaultIfEmpty()
                                             join ot in db.OgrenimTipleris on o.OgrenimTipID equals ot.OgrenimTipID
                                             select new krMezuniyetYonetmelikOT
                                             {
                                                 OgrenimTipKod = o.OgrenimTipKod,
                                                 OgrenimTipAdi = ot.OgrenimTipAdi,
                                                 MezuniyetYayinTurID = yt.MezuniyetYayinTurID,
                                                 MezuniyetYayinTurAdi = yt.MezuniyetYayinTurAdi,
                                                 IsGecerli = defS != null ? defS.IsGecerli : false,
                                                 IsZorunlu = defS != null ? defS.IsZorunlu : false,
                                                 GrupKodu = defS != null ? defS.GrupKodu : null,
                                                 IsVeOrVeya = defS != null ? defS.IsVeOrVeya : null,

                                             }).OrderBy(o => o.OgrenimTipAdi).ThenBy(t => t.MezuniyetYayinTurAdi).ToList();

            return View(model);
        }



        public ActionResult getMsSubData(int id, int tbInx, bool IsDelete)
        {

            string page = "";
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
            if (tbInx == 1)
            {
                #region AnaBilgi
                var IndexModel = new MIndexBilgi();
                var btDurulari = MezuniyetBus.GetMezuniyetYayinDurumListe(new List<int>() { BasvuruDurumu.Taslak, BasvuruDurumu.Onaylandı });
                foreach (var item in btDurulari)
                {
                    var tipCount = db.MezuniyetBasvurularis.Where(p => p.MezuniyetSurecID == mdl.MezuniyetSurecID && p.MezuniyetYayinKontrolDurumID == item.MezuniyetYayinKontrolDurumID).Count();
                    IndexModel.ListB.Add(new mxRowModel { ID = item.MezuniyetYayinKontrolDurumID, Key = item.MezuniyetYayinKontrolDurumAdi, ClassName = item.ClassName, Color = item.Color, Toplam = tipCount });
                }

                //var bdrmG = db.MezuniyetYayinKontrolDurumlaris.Where(p =>  p.MezuniyetYayinKontrolDurumID == BasvuruDurumu.Gonderildi).First();
                //IndexModel.ListB.Add(new mxRowModel { ID = bdrmG.MezuniyetYayinKontrolDurumID, Key = bdrmG.MezuniyetYayinKontrolDurumAdi, ClassName = bdrmG.MezuniyetYayinKontrolDurumlari.ClassName, Color = bdrmG.MezuniyetYayinKontrolDurumlari.Color, Toplam = db.MulakatSonuclaris.Where(p => p.MezuniyetSurecID == id && p.KayitOldu == true).Count() });

                IndexModel.Toplam = IndexModel.ListB.Sum(s => s.Toplam);
                mdl.ToplamBasvuruBilgisi = IndexModel;

                #endregion
                page = ViewRenderHelper.RenderPartialView("MezuniyetSureci", "getMsDetAnaBilgi", mdl);
            }

            return Content(page, "text/html");
        }
       



        [Authorize(Roles = RoleNames.MezuniyetSureciKayıt)]
        public void YayinTurleriniKopyala(int mezuniyetSurecID, string EnstituKod)
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
                    MezuniyetYayinBelgeTurID = item.MezuniyetYayinBelgeTurID,
                    BelgeZorunlu = item.MezuniyetYayinBelgeTurID.HasValue ? item.BelgeZorunlu : false,
                    KaynakMezuniyetYayinLinkTurID = item.KaynakMezuniyetYayinLinkTurID,
                    KaynakLinkiZorunlu = item.KaynakMezuniyetYayinLinkTurID.HasValue ? item.KaynakLinkiZorunlu : false,
                    MezuniyetYayinMetinTurID = item.MezuniyetYayinMetinTurID,
                    MetinZorunlu = item.MezuniyetYayinMetinTurID.HasValue ? item.MetinZorunlu : false,
                    YayinMezuniyetYayinLinkTurID = item.YayinMezuniyetYayinLinkTurID,
                    YayinLinkiZorunlu = item.YayinMezuniyetYayinLinkTurID.HasValue ? item.YayinLinkiZorunlu : false,
                    YayinIndexTurIstensin = item.YayinIndexTurIstensin,
                    IsAktif = item.IsAktif,
                    IslemYapanID = UserIdentity.Current.Id,
                    IslemYapanIP = UserIdentity.Ip,
                    IslemTarihi = DateTime.Now
                });
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
                            join e in db.Enstitulers on new { s.EnstituKod } equals new { e.EnstituKod }
                            join d in db.Donemlers on new { s.DonemID } equals new { d.DonemID }
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