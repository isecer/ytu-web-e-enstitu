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
    [Authorize(Roles = RoleNames.SRSalonBilgi)]
    public class SRSalonBilgiController : Controller
    {
        private LisansustuBasvuruSistemiEntities db = new LisansustuBasvuruSistemiEntities();
        public ActionResult Index(string EKD)
        {

            return Index(new fmSalonBilgi { }, EKD);
        }
        [HttpPost]
        public ActionResult Index(fmSalonBilgi model, string EKD)
        {
            var _EnstituKod = Management.getSelectedEnstitu(EKD);
            model.EnstituKod = _EnstituKod;
            
            DateTime? t1 = null;
            DateTime? t2 = null;
            if (!model.BasTarih.HasValue && !model.BitTarih.HasValue)
            {
                t1 = DateTime.Now.TodateToShortDate();
                model.BasTarih = t1;
                t2 = DateTime.Now.TodateToShortDate();
            }
            else if (model.BasTarih.HasValue && model.BitTarih.HasValue == false)
            {
                t1 = model.BasTarih.Value;
                t2 = t1;
            }
            else if (model.BitTarih.HasValue && model.BasTarih.HasValue == false)
            {
                t2 = model.BasTarih.Value;
                t1 = t2;
            }
            else
            {
                t1 = model.BasTarih;
                t2 = model.BitTarih;
            }


            if (t1 > t2)
            {
                t1 = t2;
            }



            var data = new List<frSalonBilgi>();

            for (DateTime i = t1.Value; i <= t2.Value; i = i.AddDays(1))
            {
                var doV = i.DayOfWeek.ToString("d").ToInt().Value;

                var qGT = from s in db.SRSalonlars 
                          join ss in db.SRSaatlers.Where(p => p.HaftaGunID == doV) on s.SRSalonID equals ss.SRSalonID
                          join hg in db.HaftaGunleris on ss.HaftaGunID equals hg.HaftaGunID
                          join e in db.Enstitulers on s.EnstituKod equals e.EnstituKod
                          select new frSalonBilgi
                          {
                              EnstituKod = s.EnstituKod,
                              EnstituAdi = e.EnstituAd,
                              Tarih = i,
                              HaftaGunID = ss.HaftaGunID,
                              HaftaGunAdi = hg.HaftaGunAdi,
                              BasSaat = ss.BasSaat,
                              BitSaat = ss.BitSaat,
                              SRSalonID = s.SRSalonID,
                              SalonAdi = s.SalonAdi,
                              GTID = db.SRTalepleris.Where(a => a.SRSalonID == s.SRSalonID && (a.SRDurumID == SRTalepDurum.Onaylandı || a.SRDurumID == SRTalepDurum.TalepEdildi) && a.Tarih == i &&
                                         (
                                           (a.BasSaat == ss.BasSaat || a.BitSaat == ss.BitSaat) ||
                                         (
                                             (a.BasSaat < ss.BasSaat && a.BitSaat > ss.BasSaat) || a.BasSaat < ss.BitSaat && a.BitSaat > ss.BitSaat) ||
                                             (a.BasSaat > ss.BasSaat && a.BasSaat < ss.BitSaat) || a.BitSaat > ss.BasSaat && a.BitSaat < ss.BitSaat)
                                        ).Select(s2 => s2.SRTalepID).FirstOrDefault(),
                              OTID = db.SROzelTanimlars.Where(p => p.IsAktif && ((p.SROzelTanimTipID == SROzelTanimTip.ResmiTatilDegisen && p.BasTarih.Value <= i && p.BitTarih >= i) ||
                                                                                  (p.SROzelTanimTipID == SROzelTanimTip.ResmiTatilSabit && p.Ay.Value == i.Month && p.Gun == i.Day) ||
                                                                                  (p.SROzelTanimTipID == SROzelTanimTip.Rezervasyon && p.SRSalonID == s.SRSalonID && p.Tarih == i && p.SROzelTanimSaatlers.Any(a =>
                                                                                    (
                                                                                         (a.BasSaat == ss.BasSaat || a.BitSaat == ss.BitSaat) ||
                                                                                         (
                                                                                           (a.BasSaat < ss.BasSaat && a.BitSaat > ss.BasSaat) || a.BasSaat < ss.BitSaat && a.BitSaat > ss.BitSaat) ||
                                                                                           (a.BasSaat > ss.BasSaat && a.BasSaat < ss.BitSaat) || a.BitSaat > ss.BasSaat && a.BitSaat < ss.BitSaat)
                                                                                         )
                                                                                    ) ||
                                                                                 (p.SROzelTanimTipID == SROzelTanimTip.Rezerve && p.SRSalonID == s.SRSalonID && p.SROzelTanimGunlers.Any(a => a.HaftaGunID == ss.HaftaGunID) && p.BasTarih.Value <= i && p.BitTarih >= i)
                                                )).Select(s2 => s2.SROzelTanimID).FirstOrDefault(),

                              RemoveRow = db.SROzelTanimlars.Any(p => p.IsAktif && ((p.SROzelTanimTipID == SROzelTanimTip.ResmiTatilDegisen && p.BasTarih.Value <= i && p.BitTarih >= i) ||
                                                                                    (p.SROzelTanimTipID == SROzelTanimTip.ResmiTatilSabit && p.Ay.Value == i.Month && p.Gun == i.Day)))


                          };

                if (!model.EnstituKod.IsNullOrWhiteSpace()) qGT = qGT.Where(p => p.EnstituKod == model.EnstituKod);
                if (model.SRSalonID.Count > 0) qGT = qGT.Where(p => model.SRSalonID.Contains(p.SRSalonID));
                if (model.HaftaGunID.Count > 0) qGT = qGT.Where(p => model.HaftaGunID.Contains(p.HaftaGunID));
                if (model.IsDolu.HasValue)
                {
                    qGT = qGT.Where(p => (p.GTID > 0 || p.OTID > 0) == model.IsDolu.Value);
                }



                data.AddRange(qGT.Where(p => p.RemoveRow == false));
               


            }

            model.RowCount = data.Count();

            var PS = Management.setStartRowInx(model.StartRowIndex, model.PageIndex, model.PageCount, model.RowCount, model.PageSize);
            model.PageIndex = PS.PageIndex;
            model.data = data.Skip(PS.StartRowIndex).Take(model.PageSize).OrderBy(o => o.Tarih).ThenBy(t => t.BasSaat).ToList();




            var gunler = Management.cmbGetHaftaGunleri(false, false);
            ViewBag.HaftaGunleri = gunler;

            var salonlar = Management.cmbSalonlar(model.EnstituKod, false, null);
            ViewBag.Salonlar = salonlar;
            ViewBag.IsDolu = new SelectList(Management.cmbDoluBosData(true), "Value", "Caption", model.IsDolu);


            return View(model);
        }

    }
}
