using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.Helpers;
using LisansUstuBasvuruSistemi.Utilities.MenuAndRoles;
using LisansUstuBasvuruSistemi.Utilities.SystemSetting;

namespace LisansUstuBasvuruSistemi.Business
{
    public class SrTalepleriBus
    {
        public static SRSalonSaatlerModel GetSalonBosSaatler(int srSalonId, int srTalepTipId, DateTime tarih, int? srTalepId = null, int? srOzelTanimId = null, DateTime? minTarih = null)
        {
            var model = new SRSalonSaatlerModel();
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var nTarih = tarih.Date;
                var dofW = Convert.ToInt32(nTarih.DayOfWeek.ToString("d"));
                var haftaGunu = db.HaftaGunleris.First(p => p.HaftaGunID == dofW);
                var salon = db.SRSalonlars.First(p => p.SRSalonID == srSalonId);
                var secilenTarihRezervasyonlar = db.SRTalepleris.Where(p => p.SRSalonID == srSalonId && p.Tarih == nTarih && (p.SRDurumID == SrTalepDurumEnum.Onaylandı || p.SRDurumID == SrTalepDurumEnum.TalepEdildi)).ToList();
                var resmiTatilDegisen = db.SROzelTanimlars.FirstOrDefault(p => p.IsAktif && p.SROzelTanimTipID == SrOzelTanimTipiEnum.ResmiTatilDegisen && p.BasTarih.Value <= nTarih && p.BitTarih >= nTarih);
                var resmiTatilSabit = db.SROzelTanimlars.FirstOrDefault(p => p.IsAktif && p.SROzelTanimTipID == SrOzelTanimTipiEnum.ResmiTatilSabit && p.Ay.Value == nTarih.Month && p.Gun == nTarih.Day);
                var talepTip = db.SRTalepTipleris.First(p => p.SRTalepTipID == srTalepTipId);
                model.Tarih = nTarih;
                var salonSaatleri = db.SRSaatlers.Where(p => p.SRSalonID == srSalonId && p.HaftaGunID == haftaGunu.HaftaGunID).Select(s => new SRSalonSaatler
                {
                    SRSaatID = s.SRSaatID,
                    SRSalonID = s.SRSalonID,
                    HaftaGunID = s.HaftaGunID,
                    HaftaGunAdi = haftaGunu.HaftaGunAdi,
                    BasSaat = s.BasSaat,
                    BitSaat = s.BitSaat,
                    SalonDurumID = SrSalonDurumEnum.Boş,
                    Aciklama = "Rezervasyon için uygun"
                }).ToList();

                foreach (var item in secilenTarihRezervasyonlar)
                {
                    var talepTipiLng = item.SRTalepTipleri;
                    var aciklama = talepTipiLng.TalepTipAdi + ", " + item.Kullanicilar.Ad + " " + item.Kullanicilar.Soyad;
                    var salonSaat = salonSaatleri.FirstOrDefault(p => p.SRSalonID == item.SRSalonID && p.BasSaat == item.BasSaat && p.BitSaat == item.BitSaat);
                    if (salonSaat != null)
                    {
                        salonSaat.Checked = srTalepId == item.SRTalepID;
                        salonSaat.SalonDurumID = SrSalonDurumEnum.Alındı;
                        salonSaat.Aciklama = aciklama;

                    }
                    else
                    {
                        salonSaatleri.Add(new SRSalonSaatler
                        {
                            SRSalonID = item.SRSalonID.Value,
                            HaftaGunID = item.HaftaGunID,
                            HaftaGunAdi = haftaGunu.HaftaGunAdi,
                            BasSaat = item.BasSaat,
                            BitSaat = item.BitSaat,
                            SalonDurumID = SrSalonDurumEnum.Alındı,
                            Aciklama = aciklama,
                            Checked = true,
                        });
                    }
                }
                if (salonSaatleri.Count == 0)
                {
                    model.GenelAciklama = model.SRSalonAdi + " Salonu için " + model.Tarih.ToFormatDate() + " tarihi için rezervasyon alınamaz.";
                }

                model.HaftaGunID = haftaGunu.HaftaGunID;
                model.SRSalonID = salon.SRSalonID;
                model.SRSalonAdi = salon.SalonAdi;
                model.HaftaGunundeSaatlerVar = salonSaatleri.Count > 0;
                model.HaftaGunAdi = haftaGunu.HaftaGunAdi;
                //model.BosSaatSayisi = salonSaatleri.Where(a => a.Dolu == false).Count();
                //model.DoluSaatSayisi = salonSaatleri.Where(a => a.Dolu).Count();



                foreach (var item in salonSaatleri)
                {
                    var qGTalepEslesen = secilenTarihRezervasyonlar.FirstOrDefault(a => a.SRTalepID != (srTalepId ?? 0) &&
                    (
                        (a.BasSaat == item.BasSaat || a.BitSaat == item.BitSaat) ||
                        (
                            (a.BasSaat < item.BasSaat && a.BitSaat > item.BasSaat) || a.BasSaat < item.BitSaat && a.BitSaat > item.BitSaat) ||
                        (a.BasSaat > item.BasSaat && a.BasSaat < item.BitSaat) || a.BitSaat > item.BasSaat && a.BitSaat < item.BitSaat));
                    var nowDate = DateTime.Now;
                    if (minTarih.HasValue) nowDate = minTarih.Value;
                    var kTarih = Convert.ToDateTime(tarih.ToShortDateString() + " " + item.BasSaat.Hours + ":" + item.BasSaat.Minutes + ":" + item.BasSaat.Seconds);
                    if (qGTalepEslesen != null)
                    {

                        var rezTip = db.SRTalepTipleris.First(p => p.SRTalepTipID == qGTalepEslesen.SRTalepTipID);
                        item.SalonDurumID = qGTalepEslesen.SRDurumID == SrTalepDurumEnum.Onaylandı ? SrSalonDurumEnum.Dolu : SrSalonDurumEnum.OnTalep;
                        item.Disabled = true;
                        item.Aciklama = qGTalepEslesen.SRDurumID == SrTalepDurumEnum.Onaylandı ? rezTip.TalepTipAdi + ", " + qGTalepEslesen.Kullanicilar.Ad + " " + qGTalepEslesen.Kullanicilar.Soyad : "Onay bekliyor";

                    }
                    else if (resmiTatilDegisen != null)
                    {
                        item.SalonDurumID = SrSalonDurumEnum.ResmiTatil;
                        item.Disabled = true;
                        item.Aciklama = resmiTatilDegisen.Aciklama;
                    }
                    else if (resmiTatilSabit != null)
                    {
                        item.SalonDurumID = SrSalonDurumEnum.ResmiTatil;
                        item.Disabled = true;
                        item.Aciklama = resmiTatilSabit.Aciklama;
                    }
                    else if (kTarih < nowDate && item.SalonDurumID == SrSalonDurumEnum.Boş)
                    {
                        item.SalonDurumID = SrSalonDurumEnum.GecmisTarih;
                        item.Disabled = true;
                        item.Aciklama = "Geçmişe dönük rezervasyon alınamaz.";
                    }

                }

                var qData = (from s in salonSaatleri
                             join d in db.SRSalonDurumlaris on s.SalonDurumID equals d.SRSalonDurumID
                             select new SRSalonSaatler
                             {
                                 SRSaatID = s.SRSaatID,
                                 SRSalonID = s.SRSalonID,
                                 HaftaGunID = s.HaftaGunID,
                                 HaftaGunAdi = haftaGunu.HaftaGunAdi,
                                 BasSaat = s.BasSaat,
                                 BitSaat = s.BitSaat,
                                 SalonDurumID = s.SalonDurumID,
                                 SalonDurumAdi = d.SalonDurumAdi,
                                 Aciklama = s.Aciklama,
                                 Disabled = s.Disabled,
                                 Checked = s.Checked,
                                 Color = d.Color
                             }).OrderBy(o => o.BasSaat).ToList();

                model.Data = qData;
            }
            return model;
        }
        public static MmMessage SrKayitKontrol(int srSalonId, DateTime rezervasyonTarihi, TimeSpan baslangicSaati, TimeSpan bitisSaati, int? srTalepId = null, DateTime? minRezervasyonTarihi = null)
        {
            var mmMessage = new MmMessage();
            using (var db = new LisansustuBasvuruSistemiEntities())
            {


                var salon = db.SRSalonlars.First(p => p.SRSalonID == srSalonId);

                var resmiTatilDegisen = db.SROzelTanimlars.FirstOrDefault(p => p.IsAktif && p.SROzelTanimTipID == SrOzelTanimTipiEnum.ResmiTatilDegisen && p.BasTarih.Value <= rezervasyonTarihi.Date && p.BitTarih >= rezervasyonTarihi.Date);
                var resmiTatilSabit = db.SROzelTanimlars.FirstOrDefault(p => p.IsAktif && p.SROzelTanimTipID == SrOzelTanimTipiEnum.ResmiTatilSabit && p.Ay.Value == rezervasyonTarihi.Date.Month && p.Gun == rezervasyonTarihi.Date.Day);


                minRezervasyonTarihi = minRezervasyonTarihi ?? DateTime.Now;

                var kTarih = rezervasyonTarihi.Date.Add(baslangicSaati);

                var qTalepEslesen = db.SRTalepleris.Where(a => a.SRTalepID != (srTalepId ?? 0) &&
                                                                                             a.SRSalonID == srSalonId &&
                                                                                             a.Tarih == rezervasyonTarihi.Date &&
                                                                                                (
                                                                                                 (a.BasSaat <= baslangicSaati && a.BitSaat >= baslangicSaati) ||
                                                                                                 (a.BasSaat <= bitisSaati && a.BitSaat >= bitisSaati)
                                                                                                 )
                                                                              );

                if (qTalepEslesen.Any(p => p.SRDurumID == SrTalepDurumEnum.Onaylandı || p.SRDurumID == SrTalepDurumEnum.TalepEdildi))
                {
                    mmMessage.Messages.Add((rezervasyonTarihi.Date.ToShortDateString() + " " + baslangicSaati + " - " + bitisSaati + " Tarihi için " + salon.SalonAdi + " Salonu Doludur! Lütfen boş bir saat seçiniz."));
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "Tarih" });
                }
                if (resmiTatilDegisen != null || resmiTatilSabit != null)
                {
                    ;
                    mmMessage.Messages.Add("Resmi tatillerde rezervasyon alınamaz.");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "Tarih" });
                }

                else if (kTarih < minRezervasyonTarihi)
                {
                    mmMessage.Messages.Add("Geçmişe dönük rezervasyon alınamaz.");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "Tarih" });
                }
                else if (salon.SRSaatlers.Any(a => a.BasSaat == baslangicSaati && a.BitSaat == bitisSaati) == false)
                {
                    mmMessage.Messages.Add("Rezervasyon için seçilen sat uygun değildir.");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "Tarih" });
                }

            }
            return mmMessage;
        }

        public static List<CmbIntDto> GetCmbOzelTanimTipleri(bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.SROzelTanimTipleris.OrderBy(o => o.SROzelTanimTipID).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.SROzelTanimTipID, Caption = item.SROzelTanimTipAdi });
                }
            }
            return dct;
        }

        public static List<CmbIntDto> GetCmbTalepTipleri(bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var qdata = db.SRTalepTipleris.Where(p => p.IsTezSinavi == false).OrderBy(o => o.SRTalepTipID).AsQueryable();
                var data = qdata.ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.SRTalepTipID, Caption = item.TalepTipAdi });
                }
            }
            return dct;
        }


        public static List<SRDurumlari> GetSrDurumList()
        {
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.SRDurumlaris.Where(p => p.IsAktif).OrderBy(o => o.SRDurumID).ToList();
                return data;

            }
        }

        public static List<CmbIntDto> GetCmbSrDurumListe(bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.SRDurumlaris.Where(p => p.IsAktif).OrderBy(o => o.SRDurumID).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.SRDurumID, Caption = item.DurumAdi });
                }
            }
            return dct;

        }

        public static List<CmbIntDto> GetCmbSalonlar(string enstituKod, bool bosSecimVar = false, bool? isAktif = true)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var qdata = db.SRSalonlars.Where(p => p.IsAktif && p.EnstituKod == enstituKod);

                if (isAktif.HasValue) qdata = qdata.Where(p => p.IsAktif == isAktif.Value);
                var data = qdata.OrderBy(o => o.SalonAdi).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.SRSalonID, Caption = item.SalonAdi });
                }
            }
            return dct;

        }

        public static List<CmbIntDto> GetCmbSalonlar(string enstituKod, int srTalepTipId, bool bosSecimVar = false, bool? isAktif = true)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var qdata = db.SRSalonlars.Where(p => p.IsAktif && p.EnstituKod == enstituKod && p.SRSalonTalepTipleris.Any(a => a.SRTalepTipID == srTalepTipId));

                if (isAktif.HasValue) qdata = qdata.Where(p => p.IsAktif == isAktif.Value);
                var data = qdata.OrderBy(o => o.SalonAdi).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.SRSalonID, Caption = item.SalonAdi });
                }
            }
            return dct;

        }

        public static List<CmbIntDto> GetCmbAylar(bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.Aylars.OrderBy(o => o.AyID).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.AyID, Caption = item.AyAdi });
                }
            }
            return dct;

        }

        public static List<CmbIntDto> GetCmbHaftaGunleri(bool bosSecimVar = false, bool? isHaftaSonu = null)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var qdata = db.HaftaGunleris.AsQueryable();
                if (isHaftaSonu.HasValue) qdata = qdata.Where(p => p.IsHaftaSonu == isHaftaSonu);
                var data = qdata.OrderByDescending(o => o.HaftaGunID > 0).ThenBy(o => o.HaftaGunID).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.HaftaGunID, Caption = item.HaftaGunAdi });
                }
            }
            return dct;
        }

        public static List<CmbIntDto> GetCmbSrTalepTipleri(bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.SRTalepTipleris.OrderBy(o => o.SRTalepTipID).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.SRTalepTipID, Caption = item.TalepTipAdi });
                }
            }
            return dct;
        }

         

    }
}