using BiskaUtil;
using LisansUstuBasvuruSistemi.Business;
using Entities.Entities;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.Helpers;
using LisansUstuBasvuruSistemi.Utilities.MenuAndRoles;
using System;
using System.Data.Entity; 
using System.Linq;
using System.Web.Mvc;

namespace LisansUstuBasvuruSistemi.Controllers
{
    [OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    [Authorize]
    public class GelenBelgeTalepleriController : Controller
    {
        private readonly LubsDbEntities _entities = new LubsDbEntities();
        [Authorize(Roles = RoleNames.GelenBelgeTalepleri)]
        public ActionResult Index(string ekd)
        {
            return Index(new FmBelgeTalepleriDto() { PageSize = 10, Expand = false }, ekd);
        }

        [HttpPost]
        [Authorize(Roles = RoleNames.GelenBelgeTalepleri)] 
        public ActionResult Index(FmBelgeTalepleriDto model, string ekd)
        {
            var enstituKod = EnstituBus.GetSelectedEnstitu(ekd);
            #region data
            var q = from s in _entities.BelgeTalepleris
                    join ibt in _entities.BelgeTipleris on s.BelgeTipID equals ibt.BelgeTipID
                    join bdrm in _entities.BelgeDurumlaris on s.BelgeDurumID equals bdrm.BelgeDurumID
                    join d in _entities.Donemlers on s.DonemID equals d.DonemID
                    join dk in _entities.SistemDilleris on s.BelgeDilKodu equals dk.DilKodu
                    join ot in _entities.OgrenimTipleris on new { s.EnstituKod, s.OgrenimTipKod } equals new { ot.EnstituKod, ot.OgrenimTipKod }
                    join od in _entities.OgrenimDurumlaris on s.OgrenimDurumID equals od.OgrenimDurumID
                    join kul in _entities.Kullanicilars on s.OgrenciNo equals kul.OgrenciNo into defk
                    from kl in defk.DefaultIfEmpty()
                    where s.EnstituKod == enstituKod && UserIdentity.Current.EnstituKods.Contains(s.EnstituKod)
                    select new
                    {
                        s.BelgeTalepID,

                        bdrm.ClassName,
                        bdrm.Color,
                        s.EnstituKod,
                        s.BelgeDurumID,
                        bdrm.DurumAdi,
                        s.BelgeDurumAciklamasi,
                        s.BelgeTipID,
                        ibt.BelgeTipAdi,
                        s.OgrenimDurumID,
                        od.OgrenimDurumAdi,
                        s.BelgeDilKodu,
                        dk.DilAdi,
                        dk.DilFlagClass,
                        s.OgrenimTipKod,
                        ot.OgrenimTipAdi,
                        s.OgretimYiliBaslangic,
                        s.OgretimYiliBitis,
                        s.TalepTarihi,
                        s.DonemID,
                        d.DonemAdi,
                        s.AdiSoyadi,
                        ResimAdi = kl != null ? kl.ResimAdi : "",
                        KullaniciID = kl != null ? kl.KullaniciID : (int?)null,
                        UserKey = kl != null ? kl.UserKey : (Guid?)null,
                        s.OgrenciNo,
                        s.ErisimKodu,
                        s.ProgramKod,
                        s.Email,
                        s.Telefon,
                        s.IstenenBelgeSayisi,
                        s.BelgeAdi,
                        s.BelgeAciklamasi,
                        s.IslemTarihi,
                        s.TeslimBaslangicSaat,
                        s.TeslimBitisSaat,
                        s.IslemYapanID,
                        s.IslemYapanIp,
                        s.VerilenBelgeSayisi,
                        s.BelgeFiyati,
                        s.VerilenBelgeTutar,
                        s.EklenecekGun
                    };

            if (model.OgrenimDurumId.HasValue) q = q.Where(p => p.OgrenimDurumID == model.OgrenimDurumId);
            if (model.AranacakKelime.IsNullOrWhiteSpace() == false) q = q.Where(p => p.AdiSoyadi.Contains(model.AranacakKelime) || p.Telefon == model.AranacakKelime || p.Email.Contains(model.AranacakKelime) || p.OgrenciNo == model.AranacakKelime);
            if (model.BelgeTipId.HasValue) q = q.Where(p => p.BelgeTipID == model.BelgeTipId);
            if (model.OgrenimTipKod.HasValue) q = q.Where(p => p.OgrenimTipKod == model.OgrenimTipKod);
            if (model.ProgramKod.IsNullOrWhiteSpace() == false) q = q.Where(p => p.ProgramKod == model.ProgramKod);
            if (model.BelgeId.HasValue) q = q.Where(p => p.BelgeTalepID == model.BelgeId.Value);
            if (model.BelgeDurumId.HasValue) q = q.Where(p => p.BelgeDurumID == model.BelgeDurumId.Value);
            if (model.OgretimYili.IsNullOrWhiteSpace() == false)
            { 
                var oy = model.OgretimYili.Split('/').Select(s=>s.ToInt(0)).ToList();
                var bas = oy[0];
                var bit = oy[1];
                var done = oy[2];
                q = q.Where(p => p.OgretimYiliBaslangic == bas && p.OgretimYiliBitis == bit && p.DonemID == done);
            }

            if (model.BaslangicTarihi.HasValue || model.BitisTarihi.HasValue)
            {
                q = q.Where(x =>
                    (x.BelgeDurumID == BelgeTalepDurumEnum.Hazirlaniyor ||
                     x.BelgeDurumID == BelgeTalepDurumEnum.Hazirlandi ||
                     x.BelgeDurumID == BelgeTalepDurumEnum.Verildi ||
                     x.BelgeDurumID == BelgeTalepDurumEnum.Kapatildi ||
                     x.BelgeDurumID == BelgeTalepDurumEnum.IptalEdildi)  
                        ?
                        (
                            (!model.BaslangicTarihi.HasValue || x.IslemTarihi >= model.BaslangicTarihi.Value) &&
                            (!model.BitisTarihi.HasValue || x.IslemTarihi <= model.BitisTarihi.Value)
                        )
                        :
                        (
                            (!model.BaslangicTarihi.HasValue || x.TalepTarihi >= model.BaslangicTarihi.Value) &&
                            (!model.BitisTarihi.HasValue || x.TalepTarihi <= model.BitisTarihi.Value)
                        )
                );
            }

            if (model.BuGunkuKayitlar.IsNullOrWhiteSpace() == false)
            {
                var guncTar = Convert.ToDateTime("01.03.2016 22:00:00");
                if (model.BuGunkuKayitlar == "00:00-23:59")
                {
                    var t1 = DateTime.Now.TodateToShortDate();
                    var t2 = Convert.ToDateTime((DateTime.Now.ToShortDateString() + " 23:59:59"));
                    q = q.Where(p => DbFunctions.AddDays(p.TalepTarihi, p.EklenecekGun) >= t1 && DbFunctions.AddDays(p.TalepTarihi, p.EklenecekGun) <= t2);
                }
                else
                {
                    var t1 = DateTime.Now.TodateToShortDate();
                    var t2 = Convert.ToDateTime((DateTime.Now.ToShortDateString() + " 23:59:59"));
                    var basS = TimeSpan.Parse(model.BuGunkuKayitlar.Split('-')[0]);
                    var bitS = TimeSpan.Parse(model.BuGunkuKayitlar.Split('-')[1]);


                    q = q.Where(p => p.TalepTarihi >= guncTar &&
                                   (
                                    (DbFunctions.AddDays(p.TalepTarihi, p.EklenecekGun) >= t1 && DbFunctions.AddDays(p.TalepTarihi, p.EklenecekGun) <= t2)
                                    && p.TeslimBaslangicSaat == basS && p.TeslimBitisSaat == bitS
                                    )
                              );
                }
            }

            q = model.Sort.IsNullOrWhiteSpace() == false ? q.OrderBy(model.Sort) : q.OrderByDescending(o => o.TalepTarihi); 
            model.RowCount = q.Count(); 
            var indexModel = new MIndexBilgi();
            var btDurulari = BelgeTalepBus.GetBelgeTalepDurumList();
            foreach (var item in btDurulari)
            {
                var tipCount = q.Count(p => p.BelgeDurumID == item.BelgeDurumID);
                indexModel.ListB.Add(new mxRowModel { Key = item.DurumAdi, ClassName = item.ClassName, Color = item.Color, Toplam = tipCount });
            }
            indexModel.Toplam = model.RowCount;
            model.BelgeTalepleriDtos = q.Skip(model.StartRowIndex).Take(model.PageSize).Select(item => new FrBelgeTalepleriDto
            {
                BelgeTalepID = item.BelgeTalepID,
                BelgeDurumID = item.BelgeDurumID,
                TalepTarihi = item.TalepTarihi,
                DurumAdi = item.DurumAdi,
                DurumListeAdi = item.DurumAdi,
                ClassName = item.ClassName,
                Color = item.Color,
                BelgeTipID = item.BelgeTipID,
                BelgeTipAdi = item.BelgeTipAdi,
                OgrenimTipKod = item.OgrenimTipKod,
                OgrenimTipAdi = item.OgrenimTipAdi,
                OgretimYiliBaslangic = item.OgretimYiliBaslangic,
                OgretimYiliBitis = item.OgretimYiliBitis,
                DonemID = item.DonemID,
                DonemAdi = item.DonemAdi,
                AdiSoyadi = item.AdiSoyadi,
                ResimAdi = item.ResimAdi,
                UserKey = item.UserKey,
                KullaniciID = item.KullaniciID,
                OgrenciNo = item.OgrenciNo,
                ProgramKod = item.ProgramKod,
                Email = item.Email,
                Telefon = item.Telefon,
                IstenenBelgeSayisi = item.IstenenBelgeSayisi,
                IslemTarihi = item.IslemTarihi,
                IslemYapanID = item.IslemYapanID,
                IslemYapanIp = item.IslemYapanIp,
                VerilenBelgeSayisi = item.VerilenBelgeSayisi,
                BelgeFiyati = item.BelgeFiyati,
                VerilenBelgeTutar = item.VerilenBelgeTutar,
                BelgeDilKodu = item.BelgeDilKodu,
                DilAdi = item.DilAdi,
                DilFlagClass = item.DilFlagClass,
                BelgeAciklamasi = item.BelgeAciklamasi,
                BelgeAdi = item.BelgeAdi

            }).ToList();



            #endregion
            ViewBag.IndexModel = indexModel;
            ViewBag.BelgeTipID = new SelectList(BelgeTalepBus.GetCmbBelgeTipleri(true), "Value", "Caption", model.BelgeTipId);
            ViewBag.OgretimYili = new SelectList(DonemlerBus.GetCmbAkademikTarih(true, 0), "Value", "Caption", model.OgretimYili);
            ViewBag.BelgeDurumID = new SelectList(BelgeTalepBus.GetCmbBelgeTalepDurumListe(true), "Value", "Caption", model.BelgeDurumId);
            ViewBag.OgrenimDurumID = new SelectList(KullanicilarBus.CmbAktifOgrenimDurumu(true, isHesapKayittaGozuksun: true), "Value", "Caption", model.OgrenimDurumId);
            ViewBag.OgrenimTipKod = new SelectList(OgrenimTipleriBus.CmbAktifOgrenimTipleri(enstituKod, true), "Value", "Caption", model.OgrenimTipKod);
            ViewBag.ProgramKod = new SelectList(ProgramlarBus.CmbGetAktifProgramlar(enstituKod, true), "Value", "Caption", model.ProgramKod);
            ViewBag.DilKodu = new SelectList(BelgeTalepBus.GetDiller(true), "Value", "Caption", model.DilKodu);
            ViewBag.BuGunkuKayitlar = new SelectList(BelgeTalepBus.GetCmbBelgeTeslimSaatler(), "Value", "Caption", model.BuGunkuKayitlar);
            return View(model);
        }

        [Authorize(Roles = RoleNames.BelgeTalebiSil)]
        public ActionResult Sil(int id)
        {
            var mmMessage = new MmMessage();
            var kayit = _entities.BelgeTalepleris.First(p => p.BelgeTalepID == id);
            if (kayit != null && kayit.AnketCevaplaris.Any())
            {
                mmMessage.IsSuccess = false;
                mmMessage.Messages.Add("Bu belge talebi için anket verisi doldurulduğundan silinemez.");
            }
            try
            {
                _entities.BelgeTalepleris.Remove(kayit);
                _entities.SaveChanges();
                mmMessage.Messages.Add("Belge Talebi Silindi.");
                mmMessage.IsSuccess = true;
                mmMessage.MessageType = MsgTypeEnum.Success;
            }
            catch (Exception ex)
            {
                mmMessage.MessageType = MsgTypeEnum.Error;
                mmMessage.IsSuccess = false;
                mmMessage.Messages.Add("Belge Talebi Silinemedi.");
                SistemBilgilendirmeBus.SistemBilgisiKaydet(ex, BilgiTipiEnum.OnemsizHata);
            }
            return mmMessage.ToJsonResult();
        }
    }
}