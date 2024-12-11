using BiskaUtil;
using Entities.Entities;
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
        private readonly LubsDbEntities _entities = new LubsDbEntities();
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
                    join k in _entities.Kullanicilars on s.TalepYapanID equals k.KullaniciID
                    join hg in _entities.HaftaGunleris on s.HaftaGunID equals hg.HaftaGunID
                    join d in _entities.SRDurumlaris on s.SRDurumID equals d.SRDurumID
                    join ot in _entities.OgrenimTipleris.Where(p => p.EnstituKod == enstituKod)
                        on s.TIBasvuruAraRaporID.HasValue ?
                            s.TIBasvuruAraRapor.TIBasvuru.OgrenimTipKod :
                            (s.MezuniyetBasvurulariID.HasValue ?
                                s.MezuniyetBasvurulari.OgrenimTipKod :
                                (s.ToBasvuruSavunmaID.HasValue ?
                                    s.ToBasvuruSavunma.ToBasvuru.OgrenimTipKod : k.YtuOgrencisi ? k.OgrenimTipKod : null)) equals ot.OgrenimTipKod into defOt
                    from ot in defOt.DefaultIfEmpty()
                    where s.EnstituKod == enstituKod
                    select new
                    {
                        s.SRTalepID,
                        s.MezuniyetBasvurulariID,
                        s.TalepYapanID,
                        tt.TalepTipAdi,
                        s.SRTalepTipID,
                        k.UserKey,
                        OgrenimTipKod = ot != null ? ot.OgrenimTipKod : (int?)null,
                        OgrenimTipAdi = ot != null ? ot.OgrenimTipAdi : "",
                        k.OgrenciNo,
                        k.SicilNo,
                        TalepYapan = k.Ad + " " + k.Soyad,
                        k.ResimAdi,
                        s.SRSalonID,
                        SalonAdi = s.SRSalonID.HasValue ? s.SRSalonlar.SalonAdi : s.SalonAdi,
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
                        OrderInx = SqlFunctions.DateAdd("day", 0, (s.Tarih + " " + s.BasSaat)).Value > DateTime.Now ? (s.SRDurumID == SrTalepDurumEnum.TalepEdildi ? 0 : (s.SRDurumID == SrTalepDurumEnum.Onaylandı ? 1 : 2)) : (s.SRDurumID == SrTalepDurumEnum.TalepEdildi ? 3 : (s.SRDurumID == SrTalepDurumEnum.Onaylandı ? 4 : 5))
                    };
            if (model.OgrenimTipKod.HasValue) q = q.Where(p => p.OgrenimTipKod == model.OgrenimTipKod.Value);
            if (model.SRSalonID.HasValue) q = q.Where(p => p.SRSalonID == model.SRSalonID.Value);
            if (model.SRDurumID.HasValue) q = q.Where(p => p.SRDurumID == model.SRDurumID.Value);
            if (model.SRTalepTipID.HasValue) q = q.Where(p => p.SRTalepTipID == model.SRTalepTipID.Value);
            if (!model.Aranan.IsNullOrWhiteSpace()) q = q.Where(p => p.TalepYapan.Contains(model.Aranan) || p.OgrenciNo.StartsWith(model.Aranan));
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
                MezuniyetBasvurulariID = s.MezuniyetBasvurulariID,
                TalepYapanID = s.TalepYapanID,
                TalepTipAdi = s.TalepTipAdi,
                SRTalepTipID = s.SRTalepTipID,
                OgrenimTipAdi = s.OgrenimTipAdi,
                UserKey = s.UserKey,
                OgrenciNo = s.OgrenciNo,
                SicilNo = s.SicilNo,
                TalepYapan = s.TalepYapan,
                ResimAdi = s.ResimAdi,
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
                IsOnline = s.IsOnline,
                IslemTarihi = s.IslemTarihi,
                IslemYapanID = s.IslemYapanID,
                IslemYapanIP = s.IslemYapanIP
            }).Skip(model.StartRowIndex).Take(model.PageSize).ToArray();

            ViewBag.IndexModel = indexModel;
            ViewBag.SRTalepTipID = new SelectList(SrTalepleriBus.GetCmbSrTalepTipleri(true), "Value", "Caption", model.SRTalepTipID);
            ViewBag.SRSalonID = new SelectList(SrTalepleriBus.GetCmbSalonlar(enstituKod, true), "Value", "Caption", model.SRSalonID);
            ViewBag.SRDurumID = new SelectList(SrTalepleriBus.GetCmbSrDurumListe(true), "Value", "Caption", model.SRDurumID);
            ViewBag.OgrenimTipKod = new SelectList(OgrenimTipleriBus.CmbAktifOgrenimTipleri(enstituKod, true), "Value", "Caption", model.OgrenimTipKod);
            return View(model);
        }


        public ActionResult GetDetail(int id)
        {

            var srTalep = _entities.SRTalepleris.First(f => f.SRTalepID == id);
            var q = (from s in _entities.SRTalepleris
                     join tt in _entities.SRTalepTipleris on s.SRTalepTipID equals tt.SRTalepTipID
                     join e in _entities.Enstitulers on s.EnstituKod equals e.EnstituKod
                     join k in _entities.Kullanicilars on s.TalepYapanID equals k.KullaniciID
                     join kt in _entities.KullaniciTipleris on k.KullaniciTipID equals kt.KullaniciTipID
                     join sal in _entities.SRSalonlars on s.SRSalonID equals sal.SRSalonID into def1
                     from defSal in def1.DefaultIfEmpty()
                     join hg in _entities.HaftaGunleris on s.HaftaGunID equals hg.HaftaGunID
                     join d in _entities.SRDurumlaris on s.SRDurumID equals d.SRDurumID
                     join ot in _entities.OgrenimTipleris on new { k.EnstituKod, k.OgrenimTipKod } equals new { ot.EnstituKod, OgrenimTipKod = (int?)ot.OgrenimTipKod } into defOt
                     from Ot in defOt.DefaultIfEmpty()
                     join otl in _entities.OgrenimTipleris on Ot.OgrenimTipID equals otl.OgrenimTipID into def2
                     from defOtl in def2.DefaultIfEmpty()
                     where s.SRTalepID == id
                     select new FrTalepler
                     {
                         SRTalepID = s.SRTalepID,
                         EnstituKod = e.EnstituKod,
                         EnstituAdi = e.EnstituAd,
                         TalepYapanID = s.TalepYapanID,
                         SRTalepTipID = s.SRTalepTipID,
                         TalepTipAdi = tt.TalepTipAdi,
                         IsTezSinavi = tt.IsTezSinavi,
                         OgrenciNo = k.OgrenciNo,
                         TalepYapan = k.Ad + " " + k.Soyad,
                         ResimAdi = k.ResimAdi,
                         OgrenimTipKod = defOtl != null ? defOtl.OgrenimTipKod : (int?)null,
                         OgrenimTipAdi = defOtl != null ? defOtl.OgrenimTipAdi : "",
                         KullaniciTipAdi = kt.KullaniciTipAdi,
                         SRSalonID = s.SRSalonID,
                         SalonAdi = s.SRSalonID.HasValue ? defSal.SalonAdi : s.SalonAdi,
                         Tarih = s.Tarih,
                         HaftaGunID = s.HaftaGunID,
                         HaftaGunAdi = hg.HaftaGunAdi,
                         BasSaat = s.BasSaat,
                         BitSaat = s.BitSaat,
                         DanismanAdi = s.DanismanAdi,
                         EsDanismanAdi = s.EsDanismanAdi,
                         TezOzeti = s.TezOzeti,
                         TezOzetiHtml = s.TezOzetiHtml,
                         Aciklama = s.Aciklama,
                         SRDurumID = s.SRDurumID,
                         DurumAdi = d.DurumAdi,
                         DurumListeAdi = d.DurumAdi,
                         ClassName = d.ClassName,
                         Color = d.Color,
                         SRDurumAciklamasi = s.SRDurumAciklamasi,
                         IslemTarihi = s.IslemTarihi,
                         IslemYapanID = s.IslemYapanID,
                         IslemYapanIP = s.IslemYapanIP,
                         SRTalepTipleri = s.SRTalepTipleri,
                         JuriBilgi = s.SRTaleplerJuris.ToList()
                     }).First();
            var srTalepBasvuranInfoDto = new SrTalepBasvuranInfoDto();


            if (srTalep.MezuniyetBasvurulariID.HasValue)
            {
                var mezuniyetBasvurusu = srTalep.MezuniyetBasvurulari;
                var ogrenimTip = _entities.OgrenimTipleris.First(p => p.OgrenimTipKod == mezuniyetBasvurusu.OgrenimTipKod && p.EnstituKod == srTalep.EnstituKod);
                srTalepBasvuranInfoDto.AdSoyad =
                    mezuniyetBasvurusu.Kullanicilar.Ad + " " + mezuniyetBasvurusu.Kullanicilar.Soyad;
                srTalepBasvuranInfoDto.OgrenciNo = mezuniyetBasvurusu.OgrenciNo;
                srTalepBasvuranInfoDto.OgrenimTipAdi = ogrenimTip.OgrenimTipAdi;
                srTalepBasvuranInfoDto.ProgramAdi = mezuniyetBasvurusu.Programlar.ProgramAdi;
                srTalepBasvuranInfoDto.AnabilimDaliAdi = mezuniyetBasvurusu.Programlar.AnabilimDallari.AnabilimDaliAdi;
                srTalepBasvuranInfoDto.ShowSaveButon = !srTalep.IsOnline && !mezuniyetBasvurusu.IsMezunOldu.HasValue &&
                                                        !(mezuniyetBasvurusu.MezuniyetSinavDurumID > 1);
            }
            else if (srTalep.TIBasvuruAraRaporID.HasValue)
            {
                var tiBasvuruAraRapor = srTalep.TIBasvuruAraRapor;
                var tibasvuru = tiBasvuruAraRapor.TIBasvuru;
                var ogrenimTip = _entities.OgrenimTipleris.First(p => p.OgrenimTipKod == tibasvuru.OgrenimTipKod && p.EnstituKod == srTalep.EnstituKod);
                srTalepBasvuranInfoDto.AdSoyad =
                    tibasvuru.Kullanicilar.Ad + " " + tibasvuru.Kullanicilar.Soyad;
                srTalepBasvuranInfoDto.OgrenciNo = tibasvuru.OgrenciNo;
                srTalepBasvuranInfoDto.OgrenimTipAdi = ogrenimTip.OgrenimTipAdi;
                srTalepBasvuranInfoDto.ProgramAdi = tibasvuru.Programlar.ProgramAdi;
                srTalepBasvuranInfoDto.AnabilimDaliAdi = tibasvuru.Programlar.AnabilimDallari.AnabilimDaliAdi;
                srTalepBasvuranInfoDto.ShowSaveButon = false;// tiBasvuruAraRapor.TIBasvuruAraRaporDurumID == TiAraRaporDurumuEnum.ToplantiBilgileriGirildi;
            }
            else if (srTalep.ToBasvuruSavunmaID.HasValue)
            {
                var toBasvuruSavunma = srTalep.ToBasvuruSavunma;
                var toBasvuru = toBasvuruSavunma.ToBasvuru;
                var ogrenimTip = _entities.OgrenimTipleris.First(p => p.OgrenimTipKod == toBasvuru.OgrenimTipKod && p.EnstituKod == srTalep.EnstituKod);
                srTalepBasvuranInfoDto.AdSoyad =
                    toBasvuru.Kullanicilar.Ad + " " + toBasvuru.Kullanicilar.Soyad;
                srTalepBasvuranInfoDto.OgrenciNo = toBasvuru.OgrenciNo;
                srTalepBasvuranInfoDto.OgrenimTipAdi = ogrenimTip.OgrenimTipAdi;
                srTalepBasvuranInfoDto.ProgramAdi = toBasvuru.Programlar.ProgramAdi;
                srTalepBasvuranInfoDto.AnabilimDaliAdi = toBasvuru.Programlar.AnabilimDallari.AnabilimDaliAdi;
                srTalepBasvuranInfoDto.ShowSaveButon = false;// !toBasvuruSavunma.ToBasvuruSavunmaKomites.Any(a => a.ToBasvuruSavunmaDurumID.HasValue);
            }
            ViewBag.SrTalepBasvuranInfoDto = srTalepBasvuranInfoDto;
            ViewBag.SRDurumID = new SelectList(SrTalepleriBus.GetCmbSrDurumListe(true), "Value", "Caption", q.SRDurumID);

            return View(q);
        }



        [Authorize(Roles = RoleNames.SrTalepDuzelt)]

        public ActionResult RezervasyonDurumKayit(int id, int srDurumId, string srDurumAciklamasi)
        {

            var mmMessage = new MmMessage();
            var talep = _entities.SRTalepleris.First(p => p.SRTalepID == id);

            if (srDurumId == SrTalepDurumEnum.Onaylandı)
            {
                if (talep.SRSalonID.HasValue)
                {
                    var isSecilenTarihUygunDegil = _entities.SRTalepleris.Any(a =>
                                                                                    a.SRTalepID != talep.SRTalepID &&
                                                                                    a.SRSalonID == talep.SRSalonID &&
                                                                                    a.Tarih == talep.Tarih &&
                                                                                        (
                                                                                            (a.BasSaat <= talep.BasSaat && a.BitSaat >= talep.BitSaat) ||
                                                                                            (a.BasSaat <= talep.BitSaat && a.BitSaat >= talep.BitSaat) ||
                                                                                            (talep.BasSaat <= a.BitSaat && talep.BitSaat >= a.BitSaat) ||
                                                                                            (talep.BasSaat <= a.BitSaat && talep.BitSaat >= a.BitSaat)
                                                                                        ) &&
                                                                                    a.SRDurumID == SrTalepDurumEnum.Onaylandı
                                                                                    );
                    if (isSecilenTarihUygunDegil)
                    {
                        var salon = _entities.SRSalonlars.First(p => p.SRSalonID == talep.SRSalonID);
                        mmMessage.Messages.Add(talep.Tarih.ToShortDateString() + " " + talep.BasSaat + " - " + talep.BitSaat + " Tarihi için '" + salon.SalonAdi + "' Salonu doludur bu rezervasyon onaylanamaz!");

                    }
                }

            }
            else if (srDurumId == SrTalepDurumEnum.Reddedildi && srDurumAciklamasi.IsNullOrWhiteSpace())
            {
                mmMessage.Messages.Add("Ret açıklaması giriniz.");
            }

            if (talep.SRDurumID != srDurumId && !mmMessage.Messages.Any())
            {
                talep.SRDurumID = srDurumId;
                talep.IslemTarihi = DateTime.Now;
                talep.IslemYapanID = UserIdentity.Current.Id;
                talep.IslemYapanIP = UserIdentity.Ip;
                talep.SRDurumAciklamasi = srDurumAciklamasi;
                _entities.SaveChanges();
                mmMessage.IsSuccess = true;
                if ((srDurumId == SrTalepDurumEnum.Onaylandı || srDurumId == SrTalepDurumEnum.Reddedildi) && talep.SRTalepTipleri.IsTezSinavi)
                {
                    mmMessage = MezuniyetBus.SendMailMezuniyetSinavYerBilgisi(id, srDurumId == SrTalepDurumEnum.Onaylandı);
                }

            }

            var messageView = "";
            if (mmMessage.Messages.Any()) messageView = ViewRenderHelper.RenderPartialView("Ajax", "GetMessage", mmMessage);

            return new
            {
                mmMessage.IsSuccess,
                IslemTipListeAdi = talep.SRDurumlari.DurumAdi,
                talep.SRDurumlari.ClassName,
                talep.SRDurumlari.Color,
                IsFontWeightBold = talep.Tarih.Date.Add(talep.BitSaat) > DateTime.Now,
                messageView
            }.ToJsonResult();
        }



    }
}