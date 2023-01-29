using LisansUstuBasvuruSistemi.Models; using LisansUstuBasvuruSistemi.Models.FilterModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using BiskaUtil;
using System.Data.Entity.Core.Objects;
using LisansUstuBasvuruSistemi.Utilities.Enums;

namespace LisansUstuBasvuruSistemi.Controllers
{
    [System.Web.Mvc.OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    [Authorize]
    public class GelenBelgeTalepleriController : Controller
    {
        private LisansustuBasvuruSistemiEntities db = new LisansustuBasvuruSistemiEntities();
        [Authorize(Roles = RoleNames.GelenBelgeTalepleri)]
        public ActionResult Index(string EKD)
        {
            return Index(new fmBelgeTalepleri() { PageSize = 10, Expand = false }, EKD);
        }

        [HttpPost]
        [Authorize(Roles = RoleNames.GelenBelgeTalepleri)]
        public ActionResult Index(fmBelgeTalepleri model, string EKD)
        {
            var _EnstituKod = Management.getSelectedEnstitu(EKD);
            #region data
            var q = from s in db.BelgeTalepleris
                    join ibt in db.BelgeTipleris on s.BelgeTipID equals ibt.BelgeTipID
                    join bdrm in db.BelgeDurumlaris on s.BelgeDurumID equals bdrm.BelgeDurumID
                    join d in db.Donemlers on s.DonemID equals d.DonemID
                    join dk in db.SistemDilleris on s.BelgeDilKodu equals dk.DilKodu
                    join ot in db.OgrenimTipleris on new { s.EnstituKod, s.OgrenimTipKod } equals new { ot.EnstituKod, ot.OgrenimTipKod }
                    join od in db.OgrenimDurumlaris on s.OgrenimDurumID equals od.OgrenimDurumID
                    join kul in db.Kullanicilars on s.OgrenciNo equals kul.OgrenciNo into defk
                    where s.EnstituKod == _EnstituKod && UserIdentity.Current.EnstituKods.Contains(s.EnstituKod)
                    from kl in defk.DefaultIfEmpty()
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

            if (model.OgrenimDurumID.HasValue) q = q.Where(p => p.OgrenimDurumID == model.OgrenimDurumID);
            if (model.AranacakKelime.IsNullOrWhiteSpace() == false) q = q.Where(p => p.AdiSoyadi.Contains(model.AranacakKelime) || p.Telefon == model.AranacakKelime || p.Email.Contains(model.AranacakKelime) || p.OgrenciNo == model.AranacakKelime);
            if (model.BelgeTipID.HasValue) q = q.Where(p => p.BelgeTipID == model.BelgeTipID);
            if (model.OgrenimTipKod.HasValue) q = q.Where(p => p.OgrenimTipKod == model.OgrenimTipKod);
            if (model.ProgramKod.IsNullOrWhiteSpace() == false) q = q.Where(p => p.ProgramKod == model.ProgramKod);
            if (model.BelgeID.HasValue) q = q.Where(p => p.BelgeTalepID == model.BelgeID.Value);
            if (model.BelgeDurumID.HasValue) q = q.Where(p => p.BelgeDurumID == model.BelgeDurumID.Value);
            if (model.OgretimYili.IsNullOrWhiteSpace() == false)
            {
                var oy = model.OgretimYili.Split('/').ToList();
                var bas = oy[0].ToInt().Value;
                var bit = oy[1].ToInt().Value;
                var done = oy[2].ToInt().Value;
                q = q.Where(p => p.OgretimYiliBaslangic == bas && p.OgretimYiliBitis == bit && p.DonemID == done);
            }

            if (model.BuGunkuKayitlar.IsNullOrWhiteSpace() == false)
            {
                var guncTar = Convert.ToDateTime("01.03.2016 22:00:00");
                if (model.BuGunkuKayitlar == "00:00-23:59")
                {
                    var t1 = DateTime.Now.TodateToShortDate();
                    var t2 = Convert.ToDateTime((DateTime.Now.ToShortDateString() + " 23:59:59"));
                    q = q.Where(p => EntityFunctions.AddDays(p.TalepTarihi, p.EklenecekGun) >= t1 && EntityFunctions.AddDays(p.TalepTarihi, p.EklenecekGun) <= t2);
                }
                else
                {
                    var t1 = DateTime.Now.TodateToShortDate();
                    var t2 = Convert.ToDateTime((DateTime.Now.ToShortDateString() + " 23:59:59"));
                    var basS = TimeSpan.Parse(model.BuGunkuKayitlar.Split('-')[0]);
                    var BitS = TimeSpan.Parse(model.BuGunkuKayitlar.Split('-')[1]);


                    q = q.Where(p => p.TalepTarihi >= guncTar &&
                                   (
                                    (EntityFunctions.AddDays(p.TalepTarihi, p.EklenecekGun) >= t1 && EntityFunctions.AddDays(p.TalepTarihi, p.EklenecekGun) <= t2)
                                    && p.TeslimBaslangicSaat == basS && p.TeslimBitisSaat == BitS
                                    )
                              );
                }
            }

            if (model.Sort.IsNullOrWhiteSpace() == false) q = q.OrderBy(model.Sort);
            else q = q.OrderByDescending(o => o.TalepTarihi);

            model.RowCount = q.Count();
            var PS = Management.setStartRowInx(model.StartRowIndex, model.PageIndex, model.PageCount, model.RowCount, model.PageSize);
            model.PageIndex = PS.PageIndex;

            var IndexModel = new MIndexBilgi();
            var btDurulari = Management.BelgeTalepDurumList();
            foreach (var item in btDurulari)
            {
                var tipCount = q.Where(p => p.BelgeDurumID == item.BelgeDurumID).Count();
                IndexModel.ListB.Add(new mxRowModel { Key = item.DurumAdi, ClassName = item.ClassName, Color = item.Color, Toplam = tipCount });
            }
            IndexModel.Toplam = model.RowCount;
            model.Data = q.Skip(PS.StartRowIndex).Take(model.PageSize).Select(item => new frBelgeTalepleri
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
            ViewBag.IndexModel = IndexModel;
            ViewBag.BelgeTipID = new SelectList(Management.cmbBelgeTipleri(true), "Value", "Caption", model.BelgeTipID);
            ViewBag.OgretimYili = new SelectList(Management.getAkademikTarih(true, 0), "Value", "Caption", model.OgretimYili);
            ViewBag.BelgeDurumID = new SelectList(Management.cmbBelgeTalepDurumListe(true), "Value", "Caption", model.BelgeDurumID);
            ViewBag.OgrenimDurumID = new SelectList(Management.cmbAktifOgrenimDurumu(true, IsHesapKayittaGozuksun: true), "Value", "Caption", model.OgrenimDurumID);
            ViewBag.OgrenimTipKod = new SelectList(Management.cmbAktifOgrenimTipleri(_EnstituKod, true), "Value", "Caption", model.OgrenimTipKod);
            ViewBag.ProgramKod = new SelectList(Management.cmbGetAktifProgramlar(_EnstituKod, true), "Value", "Caption", model.ProgramKod);
            ViewBag.DilKodu = new SelectList(Management.GetDiller(true), "Value", "Caption", model.DilKodu);
            ViewBag.BuGunkuKayitlar = new SelectList(Management.getBelgeTeslimSaatler(), "Value", "Caption", model.BuGunkuKayitlar);
            return View(model);
        }

        [Authorize(Roles = RoleNames.BelgeTalebiSil)]
        public ActionResult Sil(int id)
        {
            var mmMessage = new MmMessage();
            var kayit = db.BelgeTalepleris.Where(p => p.BelgeTalepID == id).FirstOrDefault();
            try
            {
                db.BelgeTalepleris.Remove(kayit);
                db.SaveChanges();
                mmMessage.Messages.Add("Belge Talebi Silindi.");
                mmMessage.IsSuccess = true;
                mmMessage.MessageType = Msgtype.Success;
            }
            catch (Exception ex)
            {
                mmMessage.MessageType = Msgtype.Error;
                mmMessage.IsSuccess = false;
                mmMessage.Messages.Add("Belge Talebi Silinemedi.");
                Management.SistemBilgisiKaydet(ex, BilgiTipi.OnemsizHata);
            }
            return mmMessage.toJsonResult();
        }
    }
}