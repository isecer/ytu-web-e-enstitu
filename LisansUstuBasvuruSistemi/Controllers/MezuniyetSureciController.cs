using BiskaUtil;
using DevExpress.XtraCharts;
using Entities.Entities;
using LisansUstuBasvuruSistemi.Business;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.Helpers;
using LisansUstuBasvuruSistemi.Utilities.MenuAndRoles;
using LisansUstuBasvuruSistemi.Utilities.SystemSetting;
using LisansUstuBasvuruSistemi.WebServiceData.ObsRestData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace LisansUstuBasvuruSistemi.Controllers
{
    [OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    [Authorize(Roles = RoleNames.MezuniyetSureci)]
    public class MezuniyetSureciController : Controller
    {
        private readonly LubsDbEntities _entities = new LubsDbEntities();
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
        public async Task<ActionResult> Kayit(int? id, string dlgid, string ekd)
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

            var eoY = DateTime.Now.ToAkademikDonemBilgi();
            model.OgretimYili = eoY.BaslangicYil + "/" + eoY.BitisYil + "/" + eoY.DonemId;
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
                model.DonemKontrolObsDonemId = data.DonemKontrolObsDonemId;
                model.DersKontrolObsDonemId = data.DersKontrolObsDonemId;
                model.IsAktif = data.IsAktif;
                model.AnketID = data.AnketID;
                model.OgretimYili = data.BaslangicYil + "/" + data.BitisYil + "/" + data.DonemID;

            }

            var obsDonemler = await ObsRestApiService.GetCmbDonemler();
            ViewBag.DonemKontrolObsDonemId = new SelectList(obsDonemler, "Value", "Caption", model.DonemKontrolObsDonemId);
            ViewBag.DersKontrolObsDonemId = new SelectList(obsDonemler, "Value", "Caption", model.DersKontrolObsDonemId);


            model.OgrenimTipModel = MezuniyetBus.GetMezuniyetOgrenimTipKriterleri(enstituKod, model.MezuniyetSurecID);
            ViewBag.EnstituKod = new SelectList(EnstituBus.GetCmbAktifEnstituler(true), "Value", "Caption", model.EnstituKod ?? enstituKod);
            ViewBag.OgretimYili = new SelectList(DonemlerBus.GetCmbAkademikTarih(), "Value", "Caption", model.OgretimYili);
            ViewBag.AnketID = new SelectList(AnketlerBus.CmbGetAktifAnketler(enstituKod, true, model.AnketID), "Value", "Caption", model.AnketID);

            return View(model);
        }
        [HttpPost]
        [Authorize(Roles = RoleNames.MezuniyetSureciKayıt)]
        public async Task<ActionResult> Kayit(KmMezuniyetSureci kModel, bool? isYonetmelikKopyala)
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
            var toplamKaynakOraniKriteri = kModel.ToplamKaynakOraniKriteri.Select((s, inx) => new { Inx = inx, ToplamKaynakOraniKriteri = s }).ToList();
            var tekKaynakOraniKriteri = kModel.TekKaynakOraniKriteri.Select((s, inx) => new { Inx = inx, TekKaynakOraniKriteri = s }).ToList();
            var sinavUzatmaOgrenciTaahhutMaxAy = kModel.SinavUzatmaOgrenciTaahhutMaxAy.Select((s, inx) => new { Inx = inx, SinavUzatmaOgrenciTaahhutMaxAy = s }).ToList();
            var sinavUzatmaSinavAlmaSuresiMaxAy = kModel.SinavUzatmaSinavAlmaSuresiMaxAy.Select((s, inx) => new { Inx = inx, SinavUzatmaSinavAlmaSuresiMaxAy = s }).ToList();
            var tezTeslimSuresiAy = kModel.TezTeslimSuresiAy.Select((s, inx) => new { Inx = inx, TezTeslimSuresiAy = s }).ToList();
            var sinavKacGunSonraAlabilir = kModel.SinavKacGunSonraAlabilir.Select((s, inx) => new { Inx = inx, SinavKacGunSonraAlabilir = s }).ToList();
            var sinavEnGecKacAySonraAlabilir = kModel.SinavEnGecKacAySonraAlabilir.Select((s, inx) => new { Inx = inx, SinavEnGecKacAySonraAlabilir = s }).ToList();

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
                                                       join tpko in toplamKaynakOraniKriteri on kr.Inx equals tpko.Inx
                                                       join tkko in tekKaynakOraniKriteri on kr.Inx equals tkko.Inx
                                                       join uzt in sinavUzatmaOgrenciTaahhutMaxAy on kr.Inx equals uzt.Inx
                                                       join uzs in sinavUzatmaSinavAlmaSuresiMaxAy on kr.Inx equals uzs.Inx
                                                       join tts in tezTeslimSuresiAy on kr.Inx equals tts.Inx
                                                       join srg in sinavKacGunSonraAlabilir on kr.Inx equals srg.Inx
                                                       join srmg in sinavEnGecKacAySonraAlabilir on kr.Inx equals srmg.Inx
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
                                                           tpko.ToplamKaynakOraniKriteri,
                                                           tkko.TekKaynakOraniKriteri,
                                                           srg.SinavKacGunSonraAlabilir,
                                                           srmg.SinavEnGecKacAySonraAlabilir,
                                                           uzt.SinavUzatmaOgrenciTaahhutMaxAy,
                                                           uzs.SinavUzatmaSinavAlmaSuresiMaxAy,
                                                           tts.TezTeslimSuresiAy
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
                eOyilBilgi.BaslangicYil = oy[0].ToInt().Value;
                eOyilBilgi.DonemId = oy[2].ToInt().Value;
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
                    if (item.TekKaynakOraniKriteri.HasValue && (item.TekKaynakOraniKriteri <= 0 || item.TekKaynakOraniKriteri > 100))
                    {
                        mmMessage.Messages.Add(item.OgrenimTipAdi + " Öğrenim tipi için Tek Kaynak Benzerlik Oranı bilgisi 1 ile 100 arasında bir değer olabilir.");
                    }
                    if (item.ToplamKaynakOraniKriteri.HasValue && (item.ToplamKaynakOraniKriteri <= 0 || item.ToplamKaynakOraniKriteri > 100))
                    {
                        mmMessage.Messages.Add(item.OgrenimTipAdi + " Öğrenim tipi için Topla Benzerlik Oranı bilgisi 1 ile 100 arasında bir değer olabilir.");
                    }
                    if (!item.SinavUzatmaOgrenciTaahhutMaxAy.HasValue || item.SinavUzatmaOgrenciTaahhutMaxAy <= 0)
                    {
                        mmMessage.Messages.Add(item.OgrenimTipAdi + " Öğrenim tipi için U.S.T.T bilgisi 0 dan büyük olmalı.");
                    }
                    if (!item.SinavUzatmaSinavAlmaSuresiMaxAy.HasValue || item.SinavUzatmaSinavAlmaSuresiMaxAy <= 0)
                    {
                        mmMessage.Messages.Add(item.OgrenimTipAdi + " Öğrenim tipi için U.S.S.R bilgisi 0 dan büyük olmalı.");
                    }
                    if (!item.TezTeslimSuresiAy.HasValue || item.TezTeslimSuresiAy <= 0)
                    {
                        mmMessage.Messages.Add(item.OgrenimTipAdi + " Öğrenim tipi için T.T.S bilgisi 0 dan büyük olmalı.");
                    }
                    if (!item.SinavKacGunSonraAlabilir.HasValue || item.SinavKacGunSonraAlabilir <= 0)
                    {
                        mmMessage.Messages.Add(item.OgrenimTipAdi + " Öğrenim tipi için S.R.G bilgisi 0 dan büyük olmalı.");
                    }
                    if (!item.SinavEnGecKacAySonraAlabilir.HasValue || item.SinavEnGecKacAySonraAlabilir <= 0)
                    {
                        mmMessage.Messages.Add(item.OgrenimTipAdi + " Öğrenim tipi için S.R.S.G bilgisi 0 dan büyük olmalı.");
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
                kModel.BaslangicYil = eOyilBilgi.BaslangicYil;
                kModel.BitisYil = eOyilBilgi.BitisYil;
                kModel.DonemID = eOyilBilgi.DonemId;

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
                        DonemKontrolObsDonemId = kModel.DonemKontrolObsDonemId,
                        DersKontrolObsDonemId = kModel.DersKontrolObsDonemId,
                        AnketID = kModel.AnketID,
                        IsAktif = true,
                        IslemTarihi = kModel.IslemTarihi,
                        IslemYapanID = kModel.IslemYapanID,
                        IslemYapanIP = kModel.IslemYapanIP
                    });
                    await _entities.SaveChangesAsync();
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
                    data.DonemKontrolObsDonemId = kModel.DonemKontrolObsDonemId;
                    data.DersKontrolObsDonemId = kModel.DersKontrolObsDonemId;
                    data.AnketID = kModel.AnketID;
                    data.IslemTarihi = DateTime.Now;
                    data.IslemYapanID = kModel.IslemYapanID;
                    data.IslemYapanIP = kModel.IslemYapanIP;
                    _entities.MezuniyetSureciOgrenimTipKriterleris.RemoveRange(data.MezuniyetSureciOgrenimTipKriterleris);
                    if (!data.MezuniyetSureciOtoMails.Any()) MezuniyetSureciBus.MezuniyetSureciOtoMailOlustur(data.MezuniyetSurecID);
                }

                _entities.MezuniyetSureciOgrenimTipKriterleris.AddRange(mezuniyetSureciOgrenimTipKriterleri.Select(s => new MezuniyetSureciOgrenimTipKriterleri
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
                    ToplamKaynakOrani = s.ToplamKaynakOraniKriteri,
                    TekKaynakOrani = s.TekKaynakOraniKriteri,
                    SinavUzatmaOgrenciTaahhutMaxAy = s.SinavUzatmaOgrenciTaahhutMaxAy.Value,
                    SinavUzatmaSinavAlmaSuresiMaxAy = s.SinavUzatmaSinavAlmaSuresiMaxAy.Value,
                    TezTeslimSuresiAy = s.TezTeslimSuresiAy.Value,
                    SinavKacGunSonraAlabilir = s.SinavKacGunSonraAlabilir.Value,
                    SinavEnGecKacAySonraAlabilir = s.SinavEnGecKacAySonraAlabilir.Value,
                    IslemTarihi = DateTime.Now,
                    IslemYapanID = UserIdentity.Current.Id,
                    IslemYapanIP = UserIdentity.Ip


                }));
                await _entities.SaveChangesAsync();
                SiraNoVer(kModel.EnstituKod);
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
                item.SinavUzatmaOgrenciTaahhutMaxAy = sItem.SinavUzatmaOgrenciTaahhutMaxAy ?? 0;
                item.SinavUzatmaSinavAlmaSuresiMaxAy = sItem.SinavUzatmaSinavAlmaSuresiMaxAy ?? 0;
                item.TezTeslimSuresiAy = sItem.TezTeslimSuresiAy ?? 0;
                item.SinavKacGunSonraAlabilir = sItem.SinavKacGunSonraAlabilir ?? 0;
                item.SinavEnGecKacAySonraAlabilir = sItem.SinavEnGecKacAySonraAlabilir ?? 0;
                item.AktifDonemEtikNotKriteri = sItem.AktifDonemEtikNotKriteri;
                item.AktifDonemSeminerNotKriteri = sItem.AktifDonemSeminerNotKriteri;
                item.ToplamKaynakOrani = sItem.ToplamKaynakOraniKriteri;
                item.TekKaynakOrani = sItem.TekKaynakOraniKriteri;

            }
            var obsDonemler = await ObsRestApiService.GetCmbDonemler();
            ViewBag.DonemKontrolObsDonemId = new SelectList(obsDonemler, "Value", "Caption", kModel.DonemKontrolObsDonemId);
            ViewBag.DersKontrolObsDonemId = new SelectList(obsDonemler, "Value", "Caption", kModel.DersKontrolObsDonemId);

            ViewBag.EnstituKod = new SelectList(EnstituBus.GetCmbAktifEnstituler(true), "Value", "Caption", kModel.EnstituKod);
            ViewBag.OgretimYili = new SelectList(DonemlerBus.GetCmbAkademikTarih(), "Value", "Caption", kModel.OgretimYili);
            ViewBag.AnketID = new SelectList(AnketlerBus.CmbGetAktifAnketler(kModel.EnstituKod, true, kModel.AnketID), "Value", "Caption", kModel.AnketID);
            ViewBag.MmMessage = mmMessage;
            return View(kModel);
        }
        public void SiraNoVer(string enstituKod)
        {
            var surecs = (from s in _entities.MezuniyetSurecis.Where(p => p.EnstituKod == enstituKod)
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
                page = RenderTezKontrolTabView(id);
            }
            return Content(page, "text/html");
        }

        private string RenderTezKontrolTabView(int mezuniyetSurecId)
        {

            var surec = _entities.MezuniyetSurecis.First(f => f.MezuniyetSurecID == mezuniyetSurecId);
            var enstituKod = surec.EnstituKod;
            var nowDate = DateTime.Now;
            var bugunBaslangic = nowDate.Date;

            // ─────────────────────────────────────────────────────
            //  Ayarları oku
            // ─────────────────────────────────────────────────────

            var atamaYontemiDeger = MezuniyetAyar.TezAtamaYontemi.GetAyar(enstituKod, "En Az Atanan — Genel");
            var isSiraylaAta = atamaYontemiDeger.StartsWith("Sırayla");
            var isDonemsel = atamaYontemiDeger.Contains("Dönemsel");
            var isZamanPencereli = atamaYontemiDeger.Contains("Belirlenen Gün Sayısına Göre");

            var gunSiniri = isZamanPencereli
                ? MezuniyetAyar.TezAtamaGunSiniri.GetAyar(enstituKod).ToInt(7)
                : 0;

            var gunlukTavan = MezuniyetAyar.TezAtamaGunlukTavan.GetAyar(enstituKod).ToInt(0);

            var isProgramOnceliklendirme = MezuniyetAyar.MezuniyetBasvurusunuIlgiliTezSorumlusunaAta
                .GetAyar(enstituKod).ToBoolean(false);

            var isKontrolBekleyenDahil = MezuniyetAyar.TezAtamadaKontrolBekleyenleriIsYukuneDahilEt
                .GetAyar(enstituKod).ToBoolean(false);
            var isDuzeltmeBekleyenDahil = MezuniyetAyar.TezAtamadaDuzeltmedeBekleyenleriIsYukuneDahilEt
                .GetAyar(enstituKod).ToBoolean(false);

            var isBekleyenIsYukuAktif = isKontrolBekleyenDahil || isDuzeltmeBekleyenDahil;

            DateTime? pencereBaslangic = gunSiniri > 0 ? nowDate.AddDays(-gunSiniri) : (DateTime?)null;

            // ─────────────────────────────────────────────────────
            //  Strateji açıklama metni
            // ─────────────────────────────────────────────────────

            var stratejiParcalar = new List<string>();

            if (isSiraylaAta)
            {
                stratejiParcalar.Add("Sırayla Atama (Round-Robin)");
            }
            else
            {
                stratejiParcalar.Add("En Az Atanan");
                stratejiParcalar.Add(isDonemsel ? "Aktif Süreç" : "Tüm Süreçler");
                stratejiParcalar.Add(gunSiniri > 0 ? "Son " + gunSiniri + " Gün" : "Tüm Zaman");

                if (gunSiniri > 0)
                    stratejiParcalar.Add("İzin Normalizasyonu Aktif");
            }

            if (gunlukTavan > 0)
                stratejiParcalar.Add("Günlük Tavan: " + gunlukTavan);

            if (isKontrolBekleyenDahil)
                stratejiParcalar.Add("Bekleyen Kontrol: Aktif");
            if (isDuzeltmeBekleyenDahil)
                stratejiParcalar.Add("Bekleyen Düzeltme: Aktif");

            if (isProgramOnceliklendirme)
                stratejiParcalar.Add("Program Önceliklendirme Açık");

            var stratejiAciklama = string.Join(" | ", stratejiParcalar);

            // ─────────────────────────────────────────────────────
            //  Round-Robin: Sıradaki yetkili hesabı
            // ─────────────────────────────────────────────────────

            int? siradakiKullaniciId = null;
            if (isSiraylaAta)
            {
                var sonAtananId = _entities.MezuniyetBasvurularis
                    .Where(m => m.TezKontrolKullaniciID.HasValue && m.MezuniyetSureci.EnstituKod == enstituKod)
                    .OrderByDescending(m => m.MezuniyetBasvurulariID)
                    .Select(m => m.TezKontrolKullaniciID)
                    .FirstOrDefault();

                var aktifYetkiliIds = _entities.Kullanicilars
                    .Where(p =>
                        p.YetkiGrupID == YetkiGrupBus.TezKontrolYetkiGrupId &&
                        p.IsAktif &&
                        (p.IsTezAtamaAcik == null || p.IsTezAtamaAcik == true) &&
                        p.EnstituKod == enstituKod &&
                        !(
                            p.IzinBaslamaTarihi.HasValue &&
                            p.IzinBaslamaTarihi <= nowDate && p.IzinBitisTarihi >= nowDate
                        ))
                    .OrderBy(p => p.KullaniciID)
                    .Select(p => p.KullaniciID)
                    .ToList();

                if (aktifYetkiliIds.Any())
                {
                    if (!sonAtananId.HasValue)
                    {
                        siradakiKullaniciId = aktifYetkiliIds.First();
                    }
                    else
                    {
                        var idx = aktifYetkiliIds.IndexOf(sonAtananId.Value);
                        siradakiKullaniciId = (idx < 0 || idx >= aktifYetkiliIds.Count - 1)
                            ? aktifYetkiliIds.First()
                            : aktifYetkiliIds[idx + 1];
                    }
                }
            }

            // ─────────────────────────────────────────────────────
            //  Bekleyen iş yükü: Sorumluya atanmış ve henüz kontrol
            //  edilmemiş dosya bekleyen başvuru sayıları
            // ─────────────────────────────────────────────────────
             
            Dictionary<int, int> bekleyenIslerDict = new Dictionary<int, int>();
            if (isBekleyenIsYukuAktif)
            {
                var bekleyenDegerler = new List<bool?>();
                if (isKontrolBekleyenDahil) bekleyenDegerler.Add(null);
                if (isDuzeltmeBekleyenDahil) bekleyenDegerler.Add(false);

                bekleyenIslerDict = _entities.MezuniyetBasvurularis
                    .Where(m =>
                        m.TezKontrolKullaniciID.HasValue &&
                        m.MezuniyetSureci.EnstituKod == enstituKod &&
                        m.MezuniyetBasvurulariTezDosyalaris
                            .OrderByDescending(d => d.SiraNo)
                            .FirstOrDefault() != null &&
                        bekleyenDegerler.Contains(m.MezuniyetBasvurulariTezDosyalaris
                            .OrderByDescending(d => d.SiraNo)
                            .FirstOrDefault().IsOnaylandiOrDuzeltme))
                                   .GroupBy(m => m.TezKontrolKullaniciID.Value)
                       .Select(g => new { Id = g.Key, Sayi = g.Count() })
                       .ToDictionary(x => x.Id, x => x.Sayi);
            }

            // ─────────────────────────────────────────────────────
            //  Bugünkü atama sayıları (günlük tavan göstergesi için)
            // ─────────────────────────────────────────────────────

            Dictionary<int, int> bugunkuAtamalarDict = new Dictionary<int, int>();
            if (gunlukTavan > 0)
            {
                bugunkuAtamalarDict = _entities.MezuniyetBasvurularis
                    .Where(m =>
                        m.TezKontrolKullaniciID.HasValue &&
                        m.MezuniyetSureci.EnstituKod == enstituKod &&
                        m.TezKontrolAtamaTarihi >= bugunBaslangic)
                    .GroupBy(m => m.TezKontrolKullaniciID.Value)
                    .Select(g => new { Id = g.Key, Sayi = g.Count() })
                    .ToDictionary(x => x.Id, x => x.Sayi);
            }

            // ─────────────────────────────────────────────────────
            //  Aktif yetkililer sorgusu
            // ─────────────────────────────────────────────────────

            var aktifMezuniyetSureciTezKontrolBilgiDtos = (
                from kul in _entities.Kullanicilars.Where(p =>
                    p.YetkiGrupID == YetkiGrupBus.TezKontrolYetkiGrupId && p.IsAktif && p.EnstituKod == surec.EnstituKod)
                select new MezuniyetSureciTezKontrolBilgiDto
                {
                    KullaniciId = kul.KullaniciID,
                    IsTezAtamaAcik = kul.IsTezAtamaAcik == null || kul.IsTezAtamaAcik == true,
                    IsIzinde = kul.IzinBaslamaTarihi.HasValue && kul.IzinBaslamaTarihi <= nowDate && kul.IzinBitisTarihi >= nowDate,
                    IzinBaslamaTarihi = kul.IzinBaslamaTarihi,
                    IzinBitisTarihi = kul.IzinBitisTarihi,
                    UserKey = kul.UserKey,
                    ResimAdi = kul.ResimAdi,
                    AdSoyad = kul.Ad + " " + kul.Soyad,

                    // Süreç bazlı
                    SurecToplamAtanan = _entities.MezuniyetBasvurularis.Count(c =>
                        c.MezuniyetSurecID == surec.MezuniyetSurecID && c.TezKontrolKullaniciID == kul.KullaniciID),
                    SurecToplamKendiOnayi = _entities.MezuniyetBasvurularis.Count(c =>
                        c.MezuniyetSurecID == surec.MezuniyetSurecID && c.TezKontrolKullaniciID == kul.KullaniciID &&
                        c.MezuniyetBasvurulariTezDosyalaris.Any(a => a.IsOnaylandiOrDuzeltme == true)),
                    SurecToplamOnay = _entities.MezuniyetBasvurularis.Count(c =>
                        c.MezuniyetSurecID == surec.MezuniyetSurecID &&
                        c.MezuniyetBasvurulariTezDosyalaris.Any(a => a.OnayYapanID == kul.KullaniciID && a.IsOnaylandiOrDuzeltme == true)),

                    // Genel
                    GenelToplamAtanan = _entities.MezuniyetBasvurularis.Count(c =>
                        c.TezKontrolKullaniciID == kul.KullaniciID),
                    GenelToplamKendiOnayi = _entities.MezuniyetBasvurularis.Count(c =>
                        c.TezKontrolKullaniciID == kul.KullaniciID &&
                        c.MezuniyetBasvurulariTezDosyalaris.Any(a => a.IsOnaylandiOrDuzeltme == true)),
                    GenelToplamOnay = _entities.MezuniyetBasvurularis.Count(c =>
                        c.MezuniyetBasvurulariTezDosyalaris.Any(a => a.IsOnaylandiOrDuzeltme == true && a.OnayYapanID == kul.KullaniciID)),

                    // V2: Gün penceresi atama sayısı
                    GunPenceresiAtanan = pencereBaslangic.HasValue
                        ? _entities.MezuniyetBasvurularis.Count(c =>
                            c.TezKontrolKullaniciID == kul.KullaniciID &&
                            c.MezuniyetSureci.EnstituKod == surec.EnstituKod &&
                            c.TezKontrolAtamaTarihi >= pencereBaslangic)
                        : 0,

                }).OrderByDescending(o => o.GenelToplamOnay).ToList();

            // ─────────────────────────────────────────────────────
            //  Post-processing: Normalizasyon, iş yükü, skor hesabı
            //  (LINQ to Entities'de yapılamayan C# hesapları)
            // ─────────────────────────────────────────────────────


            foreach (var dto in aktifMezuniyetSureciTezKontrolBilgiDtos)
            {
                // Bekleyen iş yükü
                if (isBekleyenIsYukuAktif && bekleyenIslerDict.ContainsKey(dto.KullaniciId))
                    dto.BekleyenIsYuku = bekleyenIslerDict[dto.KullaniciId];

                // Bugünkü atama ve tavan kontrolü
                if (gunlukTavan > 0)
                {
                    dto.BugunkuAtamaSayisi = bugunkuAtamalarDict.ContainsKey(dto.KullaniciId)
                        ? bugunkuAtamalarDict[dto.KullaniciId]
                        : 0;
                    dto.IsTavanda = dto.BugunkuAtamaSayisi >= gunlukTavan;
                }

                // İzin normalizasyonu ve skor (sadece Belirlenen Gün Sayısına Göre + skorlama modu)
                if (gunSiniri > 0 && !isSiraylaAta)
                {
                    var izinliGun = 0;
                    if (dto.IzinBaslamaTarihi.HasValue && dto.IzinBitisTarihi.HasValue && pencereBaslangic.HasValue)
                    {
                        var kesisimBaslangic = dto.IzinBaslamaTarihi.Value > pencereBaslangic.Value
                            ? dto.IzinBaslamaTarihi.Value : pencereBaslangic.Value;
                        var kesisimBitis = dto.IzinBitisTarihi.Value < nowDate
                            ? dto.IzinBitisTarihi.Value : nowDate;
                        izinliGun = (int)Math.Max(0, (kesisimBitis - kesisimBaslangic).TotalDays + 1);
                    }

                    // Aktif gün = pencere - izinli günler (0 olabilir)
                    dto.AktifGunSayisi = gunSiniri - izinliGun;

                    if (dto.AktifGunSayisi > 0)
                    {
                        dto.NormalizeOran = (double)dto.GunPenceresiAtanan / dto.AktifGunSayisi;
                        dto.FinalSkor = dto.NormalizeOran + (dto.BekleyenIsYuku * TezKontrolYetkilisiAtama.BekleyenIsKatsayisi);
                    }
                    else
                    {
                        // Pencerede aktif günü yok → atamaya dahil edilmez
                        // Dashboard'da skor "—" olarak gösterilecek
                        dto.NormalizeOran = -1; // View'da -1 ise "—" gösterilir
                        dto.FinalSkor = -1;
                    }
                }
                else if (!isSiraylaAta)
                {
                    // Pencere yok: mutlak sayı üzerinden skor
                    dto.AktifGunSayisi = 0; // gösterilmez
                    dto.NormalizeOran = 0;  // gösterilmez

                    // Kriter kolonuna göre atama sayısını al
                    var kriterAtama = isDonemsel ? dto.SurecToplamAtanan : dto.GenelToplamAtanan;
                    dto.FinalSkor = kriterAtama + (dto.BekleyenIsYuku * TezKontrolYetkilisiAtama.BekleyenIsKatsayisi);
                }
            }

            // Sıradaki işaretlemesi
            if (siradakiKullaniciId.HasValue)
            {
                var siradaki = aktifMezuniyetSureciTezKontrolBilgiDtos
                    .FirstOrDefault(f => f.KullaniciId == siradakiKullaniciId.Value);
                if (siradaki != null) siradaki.IsSiradaki = true;
            }

            // ─────────────────────────────────────────────────────
            //  Pasif yetkililer (değişmedi, mevcut mantık korunuyor)
            // ─────────────────────────────────────────────────────

            var aktifKullaniciIds = aktifMezuniyetSureciTezKontrolBilgiDtos.Select(s => s.KullaniciId).ToList();

            var digerMezuniyetBasvurulariTezDosyalaKontrolYapanIds = _entities.MezuniyetBasvurulariTezDosyalaris
                .Where(p => p.IsOnaylandiOrDuzeltme == true && p.OnayYapanID.HasValue &&
                            p.MezuniyetBasvurulari.MezuniyetSureci.EnstituKod == surec.EnstituKod &&
                            !aktifKullaniciIds.Contains(p.OnayYapanID.Value))
                .Select(s => s.OnayYapanID.Value).Distinct().ToList();

            var digerMezuniyetBasvuruTezDosyaKontrolSorumluId = _entities.MezuniyetBasvurularis
                .Where(p => p.MezuniyetSureci.EnstituKod == surec.EnstituKod &&
                            p.TezKontrolKullaniciID.HasValue &&
                            !aktifKullaniciIds.Contains(p.TezKontrolKullaniciID.Value))
                .Select(s => s.TezKontrolKullaniciID.Value).Distinct().ToList();

            var secilenDigerKullaniciIds = digerMezuniyetBasvurulariTezDosyalaKontrolYapanIds;
            secilenDigerKullaniciIds.AddRange(digerMezuniyetBasvuruTezDosyaKontrolSorumluId);
            secilenDigerKullaniciIds = secilenDigerKullaniciIds.Distinct().ToList();

            var pasifMezuniyetSureciTezKontrolBilgiDtos = (
                from kul in _entities.Kullanicilars.Where(p => secilenDigerKullaniciIds.Contains(p.KullaniciID))
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

            // ─────────────────────────────────────────────────────
            //  Model oluştur
            // ─────────────────────────────────────────────────────

            // Atanmamış tez sayısı:
            // Durum: KabulEdildi + tez dosyası yüklenmiş + son dosyada onay/düzeltme kararı yok + sorumlu atanmamış
            // View'daki "Tez Dosyası Yüklendi — Enstitü onayı bekleniyor" durumuna karşılık gelir.
            var atanmamisTezSayisi = _entities.MezuniyetBasvurularis.Count(m =>
                m.MezuniyetSureci.EnstituKod == surec.EnstituKod &&
                m.MezuniyetYayinKontrolDurumID == (int)MezuniyetYayinKontrolDurumuEnum.KabulEdildi &&
                !m.TezKontrolKullaniciID.HasValue &&
                m.MezuniyetBasvurulariTezDosyalaris.Any() &&
                m.MezuniyetBasvurulariTezDosyalaris
                    .OrderByDescending(d => d.SiraNo)
                    .FirstOrDefault().IsOnaylandiOrDuzeltme == null);

            var isTopluAtamaAktif = MezuniyetAyar.TezKontrolTopluAtamaAktif
                .GetAyar(enstituKod).ToBoolean(false);

            // Yetki kontrolü: Bu ekrandaki işlemleri (aktif/pasif, toplu atama) yapabilme yetkisi
            var isYetkili = User.IsInRole(RoleNames.MezuniyetSureciKayıt);
            var bekleyenAciklamaParcalar = new List<string>();
            if (isKontrolBekleyenDahil) bekleyenAciklamaParcalar.Add("kontrol bekleyen");
            if (isDuzeltmeBekleyenDahil) bekleyenAciklamaParcalar.Add("düzeltme bekleyen");
            var bekleyenAciklama = bekleyenAciklamaParcalar.Any()
                ? string.Join(" + ", bekleyenAciklamaParcalar) + " başvuru sayısı"
                : "";
            var model = new MezuniyetSureciTezKontrolDto
            {
                DonemAdi = surec.BaslangicYil + " - " + surec.BitisYil + " " + surec.Donemler.DonemAdi + " " + surec.SiraNo,
                MezuniyetSurecId = surec.MezuniyetSurecID,
                AktifMezuniyetSureciTezKontrolBilgiDtos = aktifMezuniyetSureciTezKontrolBilgiDtos,
                PasifMezuniyetSureciTezKontrolBilgiDtos = pasifMezuniyetSureciTezKontrolBilgiDtos,
                AtamaYontemi = atamaYontemiDeger,
                AktifStratejiAciklama = stratejiAciklama,
                BekleyenAciklama = bekleyenAciklama,
                GunSiniri = gunSiniri,
                GunlukTavan = gunlukTavan,
                IsDonemsel = isDonemsel,
                IsSiraylaAtama = isSiraylaAta,
                IsProgramOnceliklendirme = isProgramOnceliklendirme,
                IsBekleyenIsYukuAktif = isBekleyenIsYukuAktif,
                IsZamanPencereli = isZamanPencereli,
                AtanmamisTezSayisi = atanmamisTezSayisi,
                IsTopluAtamaAktif = isTopluAtamaAktif,
                IsYetkili = isYetkili
            };

            return ViewRenderHelper.RenderPartialView("MezuniyetSureci", "GetMsTezKontrolBilgileri", model);

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
            var strView = ViewRenderHelper.RenderPartialView("Ajax", "GetMessage", mmMessage);
            return Json(new { mmMessage.IsSuccess, Messages = strView }, "application/json", JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult TezAtamaDurumuGuncelle(int kullaniciId, bool isTezAtamaAcik)
        {
            // Yetki kontrolü
            if (!RoleNames.MezuniyetSureciKayıt.InRole())
            {
                return Json(new { isSuccess = false, message = "Bu işlem için yetkiniz bulunmamaktadır." });
            }

            try
            {
                var kullanici = _entities.Kullanicilars.FirstOrDefault(k => k.KullaniciID == kullaniciId);
                if (kullanici == null)
                {
                    return Json(new { isSuccess = false, message = "Kullanıcı bulunamadı." });
                }

                kullanici.IsTezAtamaAcik = isTezAtamaAcik;
                _entities.SaveChanges();

                return Json(new
                {
                    isSuccess = true,
                    message = kullanici.Ad + " " + kullanici.Soyad + " — tez atama durumu " +
                              (isTezAtamaAcik ? "açıldı." : "kapatıldı.")
                });
            }
            catch
            {
                return Json(new { isSuccess = false, message = "İşlem sırasında hata oluştu." });
            }
        }
        [HttpPost]
        public ActionResult TumTezAtamaDurumuGuncelle(bool isTezAtamaAcik, string ekd)
        {
            // Yetki kontrolü
            if (!RoleNames.MezuniyetSureciKayıt.InRole())
            {
                return Json(new { isSuccess = false, message = "Bu işlem için yetkiniz bulunmamaktadır." });
            }

            try
            {
                string enstituKod = EnstituBus.GetSelectedEnstitu(ekd);
                var yetkililer = _entities.Kullanicilars.Where(k =>
                    k.YetkiGrupID == YetkiGrupBus.TezKontrolYetkiGrupId &&
                    k.IsAktif &&
                    k.EnstituKod == enstituKod).ToList();

                foreach (var kul in yetkililer)
                {
                    kul.IsTezAtamaAcik = isTezAtamaAcik;
                }

                _entities.SaveChanges();

                return Json(new
                {
                    isSuccess = true,
                    message = yetkililer.Count + " yetkilinin tez atama durumu " +
                              (isTezAtamaAcik ? "açıldı." : "kapatıldı.")
                });
            }
            catch
            {
                return Json(new { isSuccess = false, message = "Toplu güncelleme sırasında hata oluştu." });
            }
        }
        // <summary>
        /// Tez kontrol sorumlusu atanmamış başvuruları mevcut algoritmaya göre toplu dağıtır.
        /// Her başvuru için TezDosyasiKontrolYetkilisiAta çağrılır — böylece
        /// aktif strateji (skorlama/round-robin, tavan, normalizasyon) aynen uygulanır.
        /// </summary>
        [HttpPost]
        public ActionResult TopluTezAtamaYap(int mezuniyetSurecId)
        {
            // Yetki kontrolü
            if (!User.IsInRole(RoleNames.MezuniyetSureciKayıt))
            {
                return Json(new { isSuccess = false, message = "Bu işlem için yetkiniz bulunmamaktadır." });
            }

            try
            {
                var surec = _entities.MezuniyetSurecis.First(f => f.MezuniyetSurecID == mezuniyetSurecId);
                var enstituKod = surec.EnstituKod;

                // Toplu atama ayarı aktif mi?
                if (!MezuniyetAyar.TezKontrolTopluAtamaAktif.GetAyar(enstituKod).ToBoolean(false))
                {
                    return Json(new { isSuccess = false, message = "Toplu atama özelliği bu enstitü için aktif değildir." });
                }

                // Atanmamış başvuruları bul:
                // Durum: KabulEdildi + tez dosyası yüklenmiş + son dosyada onay yok + sorumlu atanmamış
                // "Tez Dosyası Yüklendi — Enstitü onayı bekleniyor" durumundaki başvurular
                var atanmamisBasvuruIds = _entities.MezuniyetBasvurularis
                    .Where(m =>
                        m.MezuniyetSurecID == mezuniyetSurecId &&
                        m.MezuniyetYayinKontrolDurumID == (int)MezuniyetYayinKontrolDurumuEnum.KabulEdildi &&
                        !m.TezKontrolKullaniciID.HasValue &&
                        m.MezuniyetBasvurulariTezDosyalaris.Any() &&
                        m.MezuniyetBasvurulariTezDosyalaris
                            .OrderByDescending(d => d.SiraNo)
                            .FirstOrDefault().IsOnaylandiOrDuzeltme == null)
                    .Select(m => m.MezuniyetBasvurulariID)
                    .ToList();

                if (!atanmamisBasvuruIds.Any())
                {
                    return Json(new { isSuccess = true, message = "Atanmamış başvuru bulunmamaktadır.", atananSayi = 0 });
                }

                var basariliSayi = 0;
                var hataliSayi = 0;

                // Her başvuru için mevcut algoritmayı çağır
                // Böylece tavan, normalizasyon, bekleyen iş yükü hepsi uygulanır.
                foreach (var basvuruId in atanmamisBasvuruIds)
                {
                    try
                    {
                        // Ana atama metodunu çağır — V2 algoritması aynen çalışır
                        TezKontrolYetkilisiAtama.TezDosyasiKontrolYetkilisiAta(basvuruId);
                        basariliSayi++;
                    }
                    catch
                    {
                        hataliSayi++;
                    }
                }

                var mesaj = basariliSayi + " başvuruya tez kontrol sorumlusu atandı.";
                if (hataliSayi > 0)
                {
                    mesaj += " " + hataliSayi + " başvuruda hata oluştu.";
                }

                return Json(new
                {
                    isSuccess = true,
                    message = mesaj,
                    atananSayi = basariliSayi,
                    hataliSayi = hataliSayi
                });
            }
            catch (Exception ex)
            {
                return Json(new { isSuccess = false, message = "Toplu atama sırasında hata oluştu: " + ex.Message });
            }
        }
    }
}