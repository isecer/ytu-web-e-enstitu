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
    [OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    [Authorize(Roles = RoleNames.MezuniyetSureci)]
    public class MezuniyetSureciController : Controller
    {
        private readonly LisansustuBasvuruSistemiEntities _entities = new LisansustuBasvuruSistemiEntities();
        public ActionResult Index(string ekd)
        {

            return Index(new FmMezuniyetSureci() { PageSize = 15 }, ekd);
        }
        [HttpPost]
        public ActionResult Index(FmMezuniyetSureci model, string ekd)
        {
            var enstKods = UserIdentity.Current.EnstituKods ?? new List<string>();
            model.EnstituKod = EnstituBus.GetSelectedEnstitu(ekd);
            var q = from s in _entities.MezuniyetSurecis
                    join e in _entities.Enstitulers on new { s.EnstituKod } equals new { e.EnstituKod }
                    join d in _entities.Donemlers on new { s.DonemID } equals new { d.DonemID }
                    join k in _entities.Kullanicilars on s.IslemYapanID equals k.KullaniciID
                    where enstKods.Contains(e.EnstituKod)
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
            var indexModel = new MIndexBilgi
            {
                Toplam = model.RowCount
            };
            q = !model.Sort.IsNullOrWhiteSpace() ? q.OrderBy(model.Sort) : q.OrderByDescending(o => o.BaslangicTarihi);
            var qdata = q.Skip(model.StartRowIndex).Take(model.PageSize).Select(s => new FrMezuniyetSureci
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
            ViewBag.IndexModel = indexModel;


            return View(model);
        }
        [Authorize(Roles = RoleNames.MezuniyetSureciKayıt)]
        public ActionResult Kayit(int? id, string dlgid, string ekd)
        {
            string enstituKod = EnstituBus.GetSelectedEnstitu(ekd);
            var mmMessage = new MmMessage
            {
                IsDialog = !dlgid.IsNullOrWhiteSpace(),
                DialogID = dlgid
            };
            ViewBag.MmMessage = mmMessage;
            var model = new KmMezuniyetSureci
            {
                IsAktif = true
            };

            var eoY = DateTime.Now.ToEgitimOgretimYilBilgi();
            model.OgretimYili = eoY.BaslangicYili + "/" + eoY.BitisYili + "/" + eoY.Donem; 
            if (id > 0)
            {
                var data = _entities.MezuniyetSurecis.First(p => p.MezuniyetSurecID == id);
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
            model.OgrenimTipModel = MezuniyetBus.GetMezuniyetOgrenimTipKriterleri(enstituKod, model.MezuniyetSurecID);
            ViewBag.EnstituKod = new SelectList(EnstituBus.GetCmbAktifEnstituler(true), "Value", "Caption", model.EnstituKod ?? enstituKod);
            ViewBag.OgretimYili = new SelectList(DonemlerBus.GetCmbAkademikTarih(), "Value", "Caption", model.OgretimYili);
            ViewBag.AnketID = new SelectList(AnketlerBus.CmbGetAktifAnketler(enstituKod, true, model.AnketID), "Value", "Caption", model.AnketID);

            return View(model);
        }
        [HttpPost]
        [Authorize(Roles = RoleNames.MezuniyetSureciKayıt)]
        public ActionResult Kayit(KmMezuniyetSureci kModel, bool? isYonetmelikKopyala)
        {
            var mmMessage = new MmMessage();
            var mezuniyetSureciOgrenimTipKriterId = kModel.MezuniyetSureciOgrenimTipKriterId.Select((s, inx) => new { Inx = inx, MezuniyetSureciOgrenimTipKriterID = s }).ToList();
            var ogrenimTipId = kModel.OgrenimTipId.Select((s, inx) => new { Inx = inx, OgrenimTipID = s }).ToList();
            var ogrenimTipKod = kModel.OgrenimTipKod.Select((s, inx) => new { Inx = inx, OgrenimTipKod = s }).ToList();
            var aktifDonemMaxKriteri = kModel.AktifDonemMaxKriteri.Select((s, inx) => new { Inx = inx, AktifDonemMaxKriteri = s }).ToList();
            var aktifDonemDersKodKriteri = kModel.AktifDonemDersKodKriteri.Select((s, inx) => new { Inx = inx, AktifDonemDersKodKriteri = s }).ToList();
            var aktifDonemEtikNotKriteri = kModel.AktifDonemEtikNotKriteri.Select((s, inx) => new { Inx = inx, AktifDonemEtikNotKriteri = s }).ToList();
            var aktifDonemSeminerNotKriteri = kModel.AktifDonemSeminerNotKriteri.Select((s, inx) => new { Inx = inx, AktifDonemSeminerNotKriteri = s }).ToList();
            var aktifDonemToplamKrediKriteri = kModel.AktifDonemToplamKrediKriteri.Select((s, inx) => new { Inx = inx, AktifDonemToplamKrediKriteri = s }).ToList();
            var aktifDonemAgnoKriteri = kModel.AktifDonemAgnoKriteri.Select((s, inx) => new { Inx = inx, AktifDonemAgnoKriteri = s }).ToList();
            var aktifDonemAktsKriteri = kModel.AktifDonemAktsKriteri.Select((s, inx) => new { Inx = inx, AktifDonemAktsKriteri = s }).ToList();
            var sinavUzatmaOgrenciTaahhutMaxGun = kModel.SinavUzatmaOgrenciTaahhutMaxGun.Select((s, inx) => new { Inx = inx, SinavUzatmaOgrenciTaahhutMaxGun = s }).ToList();
            var sinavUzatmaSinavAlmaSuresiMaxGun = kModel.SinavUzatmaSinavAlmaSuresiMaxGun.Select((s, inx) => new { Inx = inx, SinavUzatmaSinavAlmaSuresiMaxGun = s }).ToList();
            var tezTeslimSuresiGun = kModel.TezTeslimSuresiGun.Select((s, inx) => new { Inx = inx, TezTeslimSuresiGun = s }).ToList();
            var sinavKacGunSonraAlabilir = kModel.SinavKacGunSonraAlabilir.Select((s, inx) => new { Inx = inx, SinavKacGunSonraAlabilir = s }).ToList();

            var ogrenimTipleriLngs = _entities.OgrenimTipleris.Where(p => p.EnstituKod == kModel.EnstituKod).ToList();
            var mezuniyetSureciOgrenimTipKriterleri = (from kr in mezuniyetSureciOgrenimTipKriterId
                                                       join ot in ogrenimTipId on kr.Inx equals ot.Inx
                                                       join otk in ogrenimTipKod on kr.Inx equals otk.Inx
                                                       join amx in aktifDonemMaxKriteri on kr.Inx equals amx.Inx
                                                       join dk in aktifDonemDersKodKriteri on kr.Inx equals dk.Inx
                                                       join enk in aktifDonemEtikNotKriteri on kr.Inx equals enk.Inx
                                                       join snk in aktifDonemSeminerNotKriteri on kr.Inx equals snk.Inx
                                                       join kk in aktifDonemToplamKrediKriteri on kr.Inx equals kk.Inx
                                                       join agk in aktifDonemAgnoKriteri on kr.Inx equals agk.Inx
                                                       join akts in aktifDonemAktsKriteri on kr.Inx equals akts.Inx
                                                       join uzt in sinavUzatmaOgrenciTaahhutMaxGun on kr.Inx equals uzt.Inx
                                                       join uzs in sinavUzatmaSinavAlmaSuresiMaxGun on kr.Inx equals uzs.Inx
                                                       join tts in tezTeslimSuresiGun on kr.Inx equals tts.Inx
                                                       join srg in sinavKacGunSonraAlabilir on kr.Inx equals srg.Inx
                                                       join otl in ogrenimTipleriLngs on ot.OgrenimTipID equals otl.OgrenimTipID
                                                       select new
                                                       {
                                                           kr.Inx,
                                                           kr.MezuniyetSureciOgrenimTipKriterID,
                                                           ot.OgrenimTipID,
                                                           otk.OgrenimTipKod,
                                                           otl.OgrenimTipAdi,
                                                           amx.AktifDonemMaxKriteri,
                                                           dk.AktifDonemDersKodKriteri,
                                                           enk.AktifDonemEtikNotKriteri,
                                                           snk.AktifDonemSeminerNotKriteri,
                                                           kk.AktifDonemToplamKrediKriteri,
                                                           agk.AktifDonemAgnoKriteri,
                                                           akts.AktifDonemAktsKriteri,
                                                           srg.SinavKacGunSonraAlabilir,
                                                           uzt.SinavUzatmaOgrenciTaahhutMaxGun,
                                                           uzs.SinavUzatmaSinavAlmaSuresiMaxGun,
                                                           tts.TezTeslimSuresiGun
                                                       }).ToList();


            #region Kontrol
            if (kModel.EnstituKod.IsNullOrWhiteSpace())
            {
                mmMessage.Messages.Add("Enstitü Seçiniz");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "EnstituKod" });
            }
            else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "EnstituKod" });

            if (kModel.BaslangicTarihi == DateTime.MinValue || kModel.BitisTarihi == DateTime.MinValue)
            {
                if (kModel.BaslangicTarihi == DateTime.MinValue)
                {
                    mmMessage.Messages.Add("Geçerli Bir Başlangıç Tarih Giriniz.");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "BaslangicTarihi" });
                }
                else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "BaslangicTarihi" });
                if (kModel.BitisTarihi == DateTime.MinValue)
                {
                    mmMessage.Messages.Add("Geçerli Bir Bitiş Tarih Giriniz.");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "BitisTarihi" });
                }
                else mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "BitisTarihi" });
            }
            else if (kModel.BaslangicTarihi >= kModel.BitisTarihi)
            {
                mmMessage.Messages.Add("Başlangıç tarihi bitiş tarihinden büyük olamaz!");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "BaslangicTarihi" });
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "BitisTarihi" });
            }
            else
            {
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "BaslangicTarihi" });
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "BitisTarihi" });
            }

            var eOyilBilgi = new EgitimOgretimDonemDto();
            if (kModel.OgretimYili.IsNullOrWhiteSpace() == false)
            {
                var oy = kModel.OgretimYili.Split('/').ToList();
                eOyilBilgi.BaslangicYili = oy[0].ToInt().Value;
                eOyilBilgi.BitisYili = oy[1].ToInt().Value;
                eOyilBilgi.Donem = oy[2].ToInt().Value;
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Success, PropertyName = "OgretimYili" });
            }
            else
            {
                mmMessage.Messages.Add("Öğretim yılı seçiniz");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "OgretimYili" });
            }

            if (mmMessage.Messages.Count == 0)
            {

                var qBasS = _entities.MezuniyetSurecis.Count(p => p.EnstituKod == kModel.EnstituKod && p.MezuniyetSurecID != kModel.MezuniyetSurecID &&
                                                                 (
                                                                     (p.BaslangicTarihi <= kModel.BaslangicTarihi && p.BitisTarihi >= kModel.BaslangicTarihi)
                                                                     ||
                                                                     (p.BaslangicTarihi <= kModel.BitisTarihi && p.BitisTarihi >= kModel.BitisTarihi)
                                                                     ||
                                                                     (kModel.BaslangicTarihi <= p.BaslangicTarihi && kModel.BitisTarihi >= p.BaslangicTarihi)
                                                                     ||
                                                                     (kModel.BaslangicTarihi <= p.BitisTarihi && kModel.BitisTarihi >= p.BitisTarihi)
                                                                 ));
                if (qBasS > 0)
                {
                    mmMessage.Messages.Add("Girmiş olduğunuz tarihler için daha önceden mezuniyet süreci kayıt edilmiştir.");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "BaslangicTarihi" });
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "BitisTarihi" });
                }
            }
            if (mmMessage.Messages.Count == 0)
            {
                foreach (var item in mezuniyetSureciOgrenimTipKriterleri)
                {
                    if (!item.AktifDonemToplamKrediKriteri.HasValue || item.AktifDonemToplamKrediKriteri <= 0)
                    {
                        mmMessage.Messages.Add(item.OgrenimTipAdi + " Öğrenim tipi için Min Kredi bilgisi 0 dan büyük olmalı.");
                    }
                    if (!item.AktifDonemAgnoKriteri.HasValue || !(item.AktifDonemAgnoKriteri > 0 && item.AktifDonemAgnoKriteri <= 4))
                    {
                        mmMessage.Messages.Add(item.OgrenimTipAdi + " Öğrenim tipi için Min Agno bilgisi 1 ile 4 arasında olmalı.");
                    }
                    if (!item.AktifDonemAktsKriteri.HasValue || item.AktifDonemAktsKriteri <= 0)
                    {
                        mmMessage.Messages.Add(item.OgrenimTipAdi + " Öğrenim tipi için Min Akts bilgisi 0 dan büyük olmalı.");
                    }
                    if (!item.SinavUzatmaOgrenciTaahhutMaxGun.HasValue || item.SinavUzatmaOgrenciTaahhutMaxGun <= 0)
                    {
                        mmMessage.Messages.Add(item.OgrenimTipAdi + " Öğrenim tipi için U.S.T.T bilgisi 0 dan büyük olmalı.");
                    }
                    if (!item.SinavUzatmaSinavAlmaSuresiMaxGun.HasValue || item.SinavUzatmaSinavAlmaSuresiMaxGun <= 0)
                    {
                        mmMessage.Messages.Add(item.OgrenimTipAdi + " Öğrenim tipi için U.S.S.R bilgisi 0 dan büyük olmalı.");
                    }
                    if (!item.TezTeslimSuresiGun.HasValue || item.TezTeslimSuresiGun <= 0)
                    {
                        mmMessage.Messages.Add(item.OgrenimTipAdi + " Öğrenim tipi için T.T.S bilgisi 0 dan büyük olmalı.");
                    }
                    if (!item.SinavKacGunSonraAlabilir.HasValue || item.SinavKacGunSonraAlabilir <= 0)
                    {
                        mmMessage.Messages.Add(item.OgrenimTipAdi + " Öğrenim tipi için S.R.G bilgisi 0 dan büyük olmalı.");
                    }
                }
            }

            #endregion
            if (mmMessage.Messages.Count == 0)
            {
                var isnewOrEdit = kModel.MezuniyetSurecID <= 0;
                kModel.IslemTarihi = DateTime.Now;
                kModel.IslemYapanID = UserIdentity.Current.Id;
                kModel.IslemYapanIP = UserIdentity.Ip;
                kModel.BaslangicYil = eOyilBilgi.BaslangicYili;
                kModel.BitisYil = eOyilBilgi.BitisYili;
                kModel.DonemID = eOyilBilgi.Donem;

                if (kModel.MezuniyetSurecID <= 0)
                {
                    var eklenen = _entities.MezuniyetSurecis.Add(new MezuniyetSureci
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
                    _entities.SaveChanges();
                    kModel.MezuniyetSurecID = eklenen.MezuniyetSurecID;
                    MezuniyetSureciBus.MezuniyetSureciOtoMailOlustur(eklenen.MezuniyetSurecID);

                }
                else
                {
                    var data = _entities.MezuniyetSurecis.First(p => p.MezuniyetSurecID == kModel.MezuniyetSurecID);
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
                    _entities.MezuniyetSureciOgrenimTipKriterleris.RemoveRange(data.MezuniyetSureciOgrenimTipKriterleris);
                }

                _entities.MezuniyetSureciOgrenimTipKriterleris.AddRange(mezuniyetSureciOgrenimTipKriterleri.Select(s => new Models.MezuniyetSureciOgrenimTipKriterleri
                {
                    MezuniyetSurecID = kModel.MezuniyetSurecID,
                    OgrenimTipID = s.OgrenimTipID.Value,
                    OgrenimTipKod = s.OgrenimTipKod.Value,
                    AktifDonemMaxKriteri = s.AktifDonemMaxKriteri,
                    AktifDonemDersKodKriteri = s.AktifDonemDersKodKriteri,
                    AktifDonemEtikNotKriteri = s.AktifDonemEtikNotKriteri,
                    AktifDonemSeminerNotKriteri = s.AktifDonemSeminerNotKriteri,
                    AktifDonemToplamKrediKriteri = s.AktifDonemToplamKrediKriteri.Value,
                    AktifDonemAgnoKriteri = s.AktifDonemAgnoKriteri.Value,
                    AktifDonemAktsKriteri = s.AktifDonemAktsKriteri.Value,
                    SinavUzatmaOgrenciTaahhutMaxGun = s.SinavUzatmaOgrenciTaahhutMaxGun.Value,
                    SinavUzatmaSinavAlmaSuresiMaxGun = s.SinavUzatmaSinavAlmaSuresiMaxGun.Value,
                    TezTeslimSuresiGun = s.TezTeslimSuresiGun.Value,
                    SinavKacGunSonraAlabilir = s.SinavKacGunSonraAlabilir.Value,
                    IslemTarihi = DateTime.Now,
                    IslemYapanID = UserIdentity.Current.Id,
                    IslemYapanIP = UserIdentity.Ip


                }));
                _entities.SaveChanges();
                SiraNoVer();
                if (isnewOrEdit || (isYonetmelikKopyala.HasValue && isYonetmelikKopyala.Value)) { YonetmelikKopyala(kModel.MezuniyetSurecID, kModel.EnstituKod); }

                return RedirectToAction("Index");
            }

            MessageBox.Show("Uyarı", MessageBox.MessageType.Warning, mmMessage.Messages.ToArray());

            kModel.OgrenimTipModel = MezuniyetBus.GetMezuniyetOgrenimTipKriterleri(kModel.EnstituKod, kModel.MezuniyetSurecID);

            foreach (var item in kModel.OgrenimTipModel.OgrenimTipKriterList)
            {
                var sItem = mezuniyetSureciOgrenimTipKriterleri.First(p => p.OgrenimTipID == item.OgrenimTipID);

                item.AktifDonemMaxKriteri = sItem.AktifDonemMaxKriteri;
                item.AktifDonemDersKodKriteri = sItem.AktifDonemDersKodKriteri;
                item.AktifDonemToplamKrediKriteri = sItem.AktifDonemToplamKrediKriteri ?? 0;
                item.AktifDonemAgnoKriteri = sItem.AktifDonemAgnoKriteri ?? 0;
                item.AktifDonemAktsKriteri = sItem.AktifDonemAktsKriteri ?? 0;
                item.SinavUzatmaOgrenciTaahhutMaxGun = sItem.SinavUzatmaOgrenciTaahhutMaxGun ?? 0;
                item.SinavUzatmaSinavAlmaSuresiMaxGun = sItem.SinavUzatmaSinavAlmaSuresiMaxGun ?? 0;
                item.TezTeslimSuresiGun = sItem.TezTeslimSuresiGun ?? 0;
                item.SinavKacGunSonraAlabilir = sItem.SinavKacGunSonraAlabilir ?? 0;
                item.AktifDonemEtikNotKriteri = sItem.AktifDonemEtikNotKriteri;
                item.AktifDonemSeminerNotKriteri = sItem.AktifDonemSeminerNotKriteri;

            }
            ViewBag.EnstituKod = new SelectList(EnstituBus.GetCmbAktifEnstituler(true), "Value", "Caption", kModel.EnstituKod);
            ViewBag.OgretimYili = new SelectList(DonemlerBus.GetCmbAkademikTarih(), "Value", "Caption", kModel.OgretimYili);
            ViewBag.AnketID = new SelectList(AnketlerBus.CmbGetAktifAnketler(kModel.EnstituKod, true, kModel.AnketID), "Value", "Caption", kModel.AnketID);
            ViewBag.MmMessage = mmMessage;
            return View(kModel);
        }
        public void SiraNoVer()
        {
            var surecs = (from s in _entities.MezuniyetSurecis
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
                    var src = _entities.MezuniyetSurecis.First(p => p.MezuniyetSurecID == item2.MezuniyetSurecID);
                    src.SiraNo = inx;
                    inx++;
                }
            }
            _entities.SaveChanges();
        }


        public ActionResult GetOtBilgiM(string enstituKod, int mezuniyetSurecId)
        {
            var model = MezuniyetBus.GetMezuniyetOgrenimTipKriterleri(enstituKod, mezuniyetSurecId);
            return View(model);
        }
        public ActionResult GetMsDetail(int id, int tbInx)
        {

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

            mdl.SelectedTabIndex = tbInx;

            return View(mdl);
        }

        public ActionResult GetYonetmelikBilgi(IEnumerable<FrMezuniyetYonetmelikler> model)
        {

            return View(model);
        }


        public ActionResult GetMsSubData(int id, int tbInx)
        {

            string page = "";

            if (tbInx == 1)
            {
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
                               IslemYapanIP = s.IslemYapanIP,
                               MezuniyetSureciOgrenimTipKriterleris = s.MezuniyetSureciOgrenimTipKriterleris
                           }).First();
                #region AnaBilgi
                var indexModel = new MIndexBilgi();
                var btDurulari = MezuniyetBus.GetMezuniyetYayinDurumListe();
                foreach (var item in btDurulari)
                {
                    var tipCount = _entities.MezuniyetBasvurularis.Count(p => p.MezuniyetSurecID == mdl.MezuniyetSurecID && p.MezuniyetYayinKontrolDurumID == item.MezuniyetYayinKontrolDurumID);
                    indexModel.ListB.Add(new mxRowModel { ID = item.MezuniyetYayinKontrolDurumID, Key = item.MezuniyetYayinKontrolDurumAdi, ClassName = item.ClassName, Color = item.Color, Toplam = tipCount });
                }
                indexModel.Toplam = indexModel.ListB.Sum(s => s.Toplam);
                mdl.ToplamBasvuruBilgisi = indexModel;

                #endregion
                page = ViewRenderHelper.RenderPartialView("MezuniyetSureci", "GetMsDetAnaBilgi", mdl);
            }
            if (tbInx == 2)
            {
                #region Yonetmelikler
                var qData = (from s in _entities.MezuniyetSureciYonetmelikleris.Where(p => p.MezuniyetSurecID == id)
                             join e in _entities.Enstitulers on new { s.EnstituKod } equals new { e.EnstituKod }
                             join d in _entities.Donemlers on s.DonemID equals d.DonemID
                             join d2 in _entities.Donemlers on s.DonemIDB equals d2.DonemID into def
                             from defD2 in def.DefaultIfEmpty()
                             join k in _entities.Kullanicilars on s.IslemYapanID equals k.KullaniciID
                             orderby s.BaslangicYil descending, s.DonemID descending
                             select new FrMezuniyetYonetmelikler
                             {
                                 MezuniyetYonetmelikID = s.MezuniyetSureciYonetmelikID,
                                 EnstituKod = s.EnstituKod,
                                 EnstituAdi = e.EnstituAd,
                                 TarihKriterID = s.TarihKriterID,
                                 TarihKriterAdi = s.TarihKriterID == TarihKriterSecimEnum.SecilenTarihAraligi ? "Seçilen Tarih Aralığı" : (s.TarihKriterID == TarihKriterSecimEnum.SecilenTarihVeOncesi ? "Seçilen Tarih ve Öncesi" : "Seçilen Tarih ve Sonrası"),
                                 BaslangicYil = s.BaslangicYil,
                                 BitisYil = s.BitisYil,
                                 DonemID = s.DonemID,
                                 DonemAdi = d.DonemAdi,
                                 BaslangicYilB = s.BaslangicYilB,
                                 BitisYilB = s.BitisYilB,
                                 DonemIDB = s.DonemIDB,
                                 DonemAdiB = defD2 != null ? defD2.DonemAdi : "",
                                 MezuniyetYonetmelikData = (from mzs in _entities.MezuniyetSureciYonetmelikleriOTs.Where(p => p.MezuniyetSureciYonetmelikleri.MezuniyetSurecID == id && p.MezuniyetSureciYonetmelikID == s.MezuniyetSureciYonetmelikID)
                                                            join yt in _entities.MezuniyetYayinTurleris on mzs.MezuniyetYayinTurID equals yt.MezuniyetYayinTurID
                                                            join ot in _entities.OgrenimTipleris.Where(p => p.EnstituKod == s.EnstituKod) on mzs.OgrenimTipKod equals ot.OgrenimTipKod
                                                            select new KrMezuniyetYonetmelikOt
                                                            {
                                                                OgrenimTipKod = mzs.OgrenimTipKod,
                                                                OgrenimTipAdi = ot.OgrenimTipAdi,
                                                                MezuniyetYayinTurID = yt.MezuniyetYayinTurID,
                                                                MezuniyetYayinTurAdi = yt.MezuniyetYayinTurAdi,
                                                                IsGecerli = mzs.IsGecerli,
                                                                IsZorunlu = mzs.IsZorunlu,
                                                                GrupKodu = mzs.GrupKodu, 
                                                            }).OrderBy(o => o.OgrenimTipAdi).ThenBy(t => t.MezuniyetYayinTurAdi).ToList()

                             }).ToList();

                #endregion
                page = ViewRenderHelper.RenderPartialView("MezuniyetSureci", "GetYonetmelikBilgi", qData);
            }
            if (tbInx == 3)
            {

                var surec = _entities.MezuniyetSurecis.First(f => f.MezuniyetSurecID == id);



                var aktifMezuniyetSureciTezKontrolBilgiDtos = (from kul in _entities.Kullanicilars.Where(p => p.YetkiGrupID == YetkiGrupBus.TezKontrolYetkiGrupId && p.IsAktif && p.EnstituKod == surec.EnstituKod)

                                                               select new MezuniyetSureciTezKontrolBilgiDto
                                                               {
                                                                   KullaniciId = kul.KullaniciID,
                                                                   UserKey = kul.UserKey,
                                                                   ResimAdi = kul.ResimAdi,
                                                                   AdSoyad = kul.Ad + " " + kul.Soyad,
                                                                   SurecToplamAtanan = _entities.MezuniyetBasvurularis.Count(c => c.MezuniyetSurecID == surec.MezuniyetSurecID && c.TezKontrolKullaniciID == kul.KullaniciID),
                                                                   SurecToplamKendiOnayi = _entities.MezuniyetBasvurularis.Count(c => c.MezuniyetSurecID == surec.MezuniyetSurecID && c.TezKontrolKullaniciID == kul.KullaniciID && c.MezuniyetBasvurulariTezDosyalaris.Any(a => a.IsOnaylandiOrDuzeltme == true)),
                                                                   SurecToplamOnay = _entities.MezuniyetBasvurularis.Count(c => c.MezuniyetSurecID == surec.MezuniyetSurecID && c.MezuniyetBasvurulariTezDosyalaris.Any(a => a.OnayYapanID == kul.KullaniciID && a.IsOnaylandiOrDuzeltme == true)),
                                                                   GenelToplamAtanan = _entities.MezuniyetBasvurularis.Count(c => c.TezKontrolKullaniciID == kul.KullaniciID),
                                                                   GenelToplamKendiOnayi = _entities.MezuniyetBasvurularis.Count(c => c.TezKontrolKullaniciID == kul.KullaniciID && c.MezuniyetBasvurulariTezDosyalaris.Any(a => a.IsOnaylandiOrDuzeltme == true)),
                                                                   GenelToplamOnay = _entities.MezuniyetBasvurularis.Count(c => c.MezuniyetBasvurulariTezDosyalaris.Any(a => a.IsOnaylandiOrDuzeltme == true && a.OnayYapanID == kul.KullaniciID)),
                                                               }).OrderByDescending(o => o.GenelToplamOnay).ToList();
                var aktifKullaniciIds = aktifMezuniyetSureciTezKontrolBilgiDtos.Select(s => s.KullaniciId).ToList();

                var digerMezuniyetBasvurulariTezDosyalaKontrolYapanIds = _entities.MezuniyetBasvurulariTezDosyalaris
                    .Where(p => p.IsOnaylandiOrDuzeltme == true && p.OnayYapanID.HasValue && p.MezuniyetBasvurulari.MezuniyetSureci.EnstituKod == surec.EnstituKod && !aktifKullaniciIds.Contains(p.OnayYapanID.Value)).Select(s => s.OnayYapanID.Value).Distinct()
                    .ToList();
                var digerMezuniyetBasvuruTezDosyaKontrolSorumluId = _entities.MezuniyetBasvurularis
                    .Where(p => p.MezuniyetSureci.EnstituKod == surec.EnstituKod && p.TezKontrolKullaniciID.HasValue &&
                                !aktifKullaniciIds.Contains(p.TezKontrolKullaniciID.Value))
                    .Select(s => s.TezKontrolKullaniciID.Value).Distinct().ToList();

                var secilenDigerKullaniciIds = digerMezuniyetBasvurulariTezDosyalaKontrolYapanIds;
                secilenDigerKullaniciIds.AddRange(digerMezuniyetBasvuruTezDosyaKontrolSorumluId);
                secilenDigerKullaniciIds = secilenDigerKullaniciIds.Distinct().ToList();


                var pasifMezuniyetSureciTezKontrolBilgiDtos = (from kul in _entities.Kullanicilars.Where(p => secilenDigerKullaniciIds.Contains(p.KullaniciID))

                                                               select new MezuniyetSureciTezKontrolBilgiDto
                                                               {
                                                                   KullaniciId = kul.KullaniciID,
                                                                   UserKey = kul.UserKey,
                                                                   ResimAdi = kul.ResimAdi,
                                                                   AdSoyad = kul.Ad + " " + kul.Soyad,
                                                                   SurecToplamAtanan = _entities.MezuniyetBasvurularis.Count(c => c.MezuniyetSurecID == surec.MezuniyetSurecID && c.TezKontrolKullaniciID == kul.KullaniciID),
                                                                   SurecToplamKendiOnayi = _entities.MezuniyetBasvurularis.Count(c => c.MezuniyetSurecID == surec.MezuniyetSurecID && c.TezKontrolKullaniciID == kul.KullaniciID && c.MezuniyetBasvurulariTezDosyalaris.Any(a => a.IsOnaylandiOrDuzeltme == true)),
                                                                   SurecToplamOnay = _entities.MezuniyetBasvurularis.Count(c => c.MezuniyetSurecID == surec.MezuniyetSurecID && c.MezuniyetBasvurulariTezDosyalaris.Any(a => a.OnayYapanID == kul.KullaniciID && a.IsOnaylandiOrDuzeltme == true)),
                                                                   GenelToplamAtanan = _entities.MezuniyetBasvurularis.Count(c => c.TezKontrolKullaniciID == kul.KullaniciID),
                                                                   GenelToplamKendiOnayi = _entities.MezuniyetBasvurularis.Count(c => c.TezKontrolKullaniciID == kul.KullaniciID && c.MezuniyetBasvurulariTezDosyalaris.Any(a => a.IsOnaylandiOrDuzeltme == true)),
                                                                   GenelToplamOnay = _entities.MezuniyetBasvurularis.Count(c => c.MezuniyetBasvurulariTezDosyalaris.Any(a => a.IsOnaylandiOrDuzeltme == true && a.OnayYapanID == kul.KullaniciID)),
                                                               }).OrderByDescending(o => o.GenelToplamOnay).ToList();
                var model = new MezuniyetSureciTezKontrolDto
                {
                    DonemAdi = surec.BaslangicYil + " - " + surec.BitisYil + " " + surec.Donemler.DonemAdi + " " + surec.SiraNo,
                    MezuniyetSurecId = surec.MezuniyetSurecID,
                    AktifMezuniyetSureciTezKontrolBilgiDtos = aktifMezuniyetSureciTezKontrolBilgiDtos,
                    PasifMezuniyetSureciTezKontrolBilgiDtos = pasifMezuniyetSureciTezKontrolBilgiDtos

                };
                page = ViewRenderHelper.RenderPartialView("MezuniyetSureci", "GetMsTezKontrolBilgileri", model);
            }
            return Content(page, "text/html");
        }
        public ActionResult GetOtoMailAyarView(int id)
        {
            var surec = _entities.MezuniyetSurecis.First(p => p.MezuniyetSurecID == id);
            var otoMailData = MezuniyetSureciBus.GetOtoMailData();
            var otoMails = (from surecOtoMail in surec.MezuniyetSureciOtoMails.ToList()
                join otoMail in otoMailData on surecOtoMail.OtoMailID equals otoMail.OtoMailID
                select new MezuniyetOtoMailDto
                {
                    MezuniyetSurecID = id,
                    MezuniyetSureciOtoMailID = surecOtoMail.MezuniyetSureciOtoMailID,
                    OtoMailID = surecOtoMail.OtoMailID,
                    Aciklama = otoMail.Aciklama,
                    IsAktif = surecOtoMail.IsAktif
                }).ToList();
            ViewBag.OtoMailData = otoMails;
            return View(surec);
        }
        [Authorize(Roles = RoleNames.MezuniyetSureciKayıt)]
        public ActionResult OtoMailAyarGuncelle(int mezuniyetSureciOtoMailId, bool isAktif)
        {
            var otoMail = _entities.MezuniyetSureciOtoMails.First(f => f.MezuniyetSureciOtoMailID == mezuniyetSureciOtoMailId);
            otoMail.IsAktif = isAktif;
            _entities.SaveChanges();
            return true.ToJsonResult();

        }
        public ActionResult KriterMuafOgrenciler(int id)
        {
            var surec = _entities.MezuniyetSurecis.First(p => p.MezuniyetSurecID == id);
            return View(surec);
        }
        public ActionResult KriterMuafOgrenciEkle(int mezuniyetSurecId, int? ogrenciId)
        {
            var success = false;
            var message = "";
            if (!ogrenciId.HasValue)
            {
                message = "Öğrenci seçiniz.";
            }
            else if (_entities.MezuniyetSureciKriterMuafOgrencilers.Any(p => p.MezuniyetSurecID == mezuniyetSurecId && p.KullaniciID == ogrenciId.Value))
            {
                message = "Bu öğrenci daha önce eklendi.";
            }
            else
            {
                _entities.MezuniyetSureciKriterMuafOgrencilers.Add(new MezuniyetSureciKriterMuafOgrenciler
                {
                    MezuniyetSurecID = mezuniyetSurecId,
                    KullaniciID = ogrenciId.Value,
                    IslemTarihi = DateTime.Now,
                    IslemYapanID = UserIdentity.Current.Id
                });
                _entities.SaveChanges();
                success = true;
            }
            return new { success, message }.ToJsonResult();

        }
        public ActionResult KriterMuafOgrenciSil(int mezuniyetSurecId, int ogrenciId)
        {

            if (_entities.MezuniyetSureciKriterMuafOgrencilers.Any(p => p.MezuniyetSurecID == mezuniyetSurecId && p.KullaniciID == ogrenciId))
            {
                var ogrenci = _entities.MezuniyetSureciKriterMuafOgrencilers.First(p =>
                    p.MezuniyetSurecID == mezuniyetSurecId && p.KullaniciID == ogrenciId);
                _entities.MezuniyetSureciKriterMuafOgrencilers.Remove(ogrenci);
                _entities.SaveChanges();
            }

            return true.ToJsonResult();
        }
        public ActionResult GetFilterKullanici(string term, string ekd)
        {
            var enstituKod = EnstituBus.GetSelectedEnstitu(ekd);

            return KullanicilarBus.GetFilterOgrenciJsonResult(term, enstituKod);
        }


        [Authorize(Roles = RoleNames.MezuniyetSureciKayıt)]
        public void YonetmelikKopyala(int mezuniyetSurecId, string enstituKod)
        {

            var mbsstOld = _entities.MezuniyetSureciYayinTurleris.Where(p => p.MezuniyetSurecID == mezuniyetSurecId).ToList();
            _entities.MezuniyetSureciYayinTurleris.RemoveRange(mbsstOld);
            var yturs = _entities.MezuniyetYayinTurleris.ToList();

            foreach (var item in yturs)
            {
                _entities.MezuniyetSureciYayinTurleris.Add(new MezuniyetSureciYayinTurleri
                {
                    MezuniyetSurecID = mezuniyetSurecId,
                    MezuniyetYayinTurID = item.MezuniyetYayinTurID,
                    TarihIstensin = item.TarihIstensin,
                    MezuniyetYayinBelgeTurID = item.MezuniyetYayinBelgeTurID,
                    BelgeZorunlu = item.MezuniyetYayinBelgeTurID.HasValue && item.BelgeZorunlu,
                    KaynakMezuniyetYayinLinkTurID = item.KaynakMezuniyetYayinLinkTurID,
                    KaynakLinkiZorunlu = item.KaynakMezuniyetYayinLinkTurID.HasValue && item.KaynakLinkiZorunlu,
                    MezuniyetYayinMetinTurID = item.MezuniyetYayinMetinTurID,
                    MetinZorunlu = item.MezuniyetYayinMetinTurID.HasValue && item.MetinZorunlu,
                    YayinMezuniyetYayinLinkTurID = item.YayinMezuniyetYayinLinkTurID,
                    YayinLinkiZorunlu = item.YayinMezuniyetYayinLinkTurID.HasValue && item.YayinLinkiZorunlu,
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
                    YayinEtkinlikAdiIstensin = item.YayinEtkinlikAdiIstensin,
                    YayinYerBilgisiIstensin = item.YayinYerBilgisiIstensin,
                    IsAktif = item.IsAktif,
                    IslemYapanID = UserIdentity.Current.Id,
                    IslemYapanIP = UserIdentity.Ip,
                    IslemTarihi = DateTime.Now

                });
            }


            var yonetmeliks = _entities.MezuniyetYonetmelikleris.Where(p => p.EnstituKod == enstituKod && p.IsAktif).ToList();
            var oldY = _entities.MezuniyetSureciYonetmelikleris.Where(p => p.MezuniyetSurecID == mezuniyetSurecId).ToList();
            _entities.MezuniyetSureciYonetmelikleris.RemoveRange(oldY);

            foreach (var item in yonetmeliks)
            {
                var mznytAdd = _entities.MezuniyetSureciYonetmelikleris.Add(new MezuniyetSureciYonetmelikleri
                {
                    MezuniyetSurecID = mezuniyetSurecId,
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
                _entities.SaveChanges();
                foreach (var item2 in item.MezuniyetYonetmelikleriOTs)
                {
                    _entities.MezuniyetSureciYonetmelikleriOTs.Add(new MezuniyetSureciYonetmelikleriOT
                    {
                        MezuniyetSureciYonetmelikID = mznytAdd.MezuniyetSureciYonetmelikID,
                        OgrenimTipKod = item2.OgrenimTipKod,
                        MezuniyetYayinTurID = item2.MezuniyetYayinTurID,
                        IsGecerli = item2.IsGecerli,
                        IsZorunlu = item2.IsZorunlu,
                        GrupKodu = item2.GrupKodu 

                    });
                }
            }
            _entities.SaveChanges();
        }
        [Authorize(Roles = RoleNames.MezuniyetSureciSil)]
        public ActionResult Sil(int id)
        {
            var mmMessage = new MmMessage();

            var kayit = _entities.MezuniyetSurecis.FirstOrDefault(p => p.MezuniyetSurecID == id);

            string message;
            if (kayit != null)
            {
                var qBil = (from s in _entities.MezuniyetSurecis
                            join e in _entities.Enstitulers on new { s.EnstituKod } equals new { e.EnstituKod }
                            join d in _entities.Donemlers on new { s.DonemID } equals new { d.DonemID }
                            join k in _entities.Kullanicilars on s.IslemYapanID equals k.KullaniciID
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
                    _entities.MezuniyetSurecis.Remove(kayit);
                    _entities.SaveChanges();
                    mmMessage.Title = "Uyarı";
                    mmMessage.Messages.Add(message);
                    mmMessage.MessageType = MsgTypeEnum.Success;
                    mmMessage.IsSuccess = true;
                }
                catch (Exception ex)
                {
                    message = "'" + qBil.BaslangicYil + "/" + qBil.BitisYil + " " + qBil.DonemAdi + "' Dönemine ait mezuniyet süreci silinirken bir hata oluştu! </br> Hata:" + ex.ToExceptionMessage();
                    SistemBilgilendirmeBus.SistemBilgisiKaydet(message, ex.ToExceptionStackTrace(), BilgiTipiEnum.OnemsizHata);
                    mmMessage.Title = "Hata";
                    mmMessage.Messages.Add(message);
                    mmMessage.MessageType = MsgTypeEnum.Error;
                    mmMessage.IsSuccess = false;
                }
            }
            else
            {
                message = "Silmek istediğiniz mezuniyet süreci sistemde bulunamadı!";
                mmMessage.Title = "Hata";
                mmMessage.Messages.Add(message);
                mmMessage.MessageType = MsgTypeEnum.Error;
                mmMessage.IsSuccess = true;
            }
            var strView = ViewRenderHelper.RenderPartialView("Ajax", "getMessage", mmMessage);
            return Json(new { mmMessage.IsSuccess, Messages = strView }, "application/json", JsonRequestBehavior.AllowGet);
        }
    }
}