using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Models; 
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.MenuAndRoles;

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
                var secilenTarihRezervasyonlar = db.SRTalepleris.Where(p => p.SRSalonID == srSalonId && p.Tarih == nTarih && (p.SRDurumID == SRTalepDurum.Onaylandı || p.SRDurumID == SRTalepDurum.TalepEdildi)).ToList();
                var resmiTatilDegisen = db.SROzelTanimlars.FirstOrDefault(p => p.IsAktif && p.SROzelTanimTipID == SROzelTanimTip.ResmiTatilDegisen && p.BasTarih.Value <= nTarih && p.BitTarih >= nTarih);
                var resmiTatilSabit = db.SROzelTanimlars.FirstOrDefault(p => p.IsAktif && p.SROzelTanimTipID == SROzelTanimTip.ResmiTatilSabit && p.Ay.Value == nTarih.Month && p.Gun == nTarih.Day);
                var rezervasyonlar = db.SROzelTanimlars.FirstOrDefault(p => p.IsAktif && p.SROzelTanimTipID == SROzelTanimTip.Rezervasyon && p.SRSalonID == srSalonId && p.Tarih == nTarih);
                var rezerve = db.SROzelTanimlars.FirstOrDefault(p => p.SROzelTanimGunlers.Any(a => a.HaftaGunID == dofW) && p.SROzelTanimID != (srOzelTanimId.HasValue ? srOzelTanimId.Value : 0) && p.IsAktif && p.SROzelTanimTipID == SROzelTanimTip.Rezerve && p.SRSalonID == srSalonId && p.BasTarih.Value <= nTarih && p.BitTarih >= nTarih);
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
                    SalonDurumID = SRSalonDurum.Boş,
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
                        salonSaat.SalonDurumID = SRSalonDurum.Alındı;
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
                            SalonDurumID = SRSalonDurum.Alındı,
                            Aciklama = aciklama,
                            Checked = true,
                        });
                    }
                }


                if (srOzelTanimId.HasValue) //gelentalepGuncellemeIse
                {
                    var talep = db.SROzelTanimlars.First(p => p.SROzelTanimID == srOzelTanimId.Value);
                    if (tarih == talep.Tarih && talep.SRSalonID == srSalonId)
                    {
                        var rezTip = db.SRTalepTipleris.First(p => p.SRTalepTipID == talep.SRTalepTipID);
                        foreach (var item in talep.SROzelTanimSaatlers)
                        {

                            if (!salonSaatleri.Any(a => a.BasSaat == item.BasSaat && a.BitSaat == item.BitSaat))
                            {
                                var _rw = new SRSalonSaatler();
                                _rw.SRSalonID = talep.SRSalonID.Value;
                                _rw.HaftaGunID = dofW;
                                _rw.HaftaGunAdi = haftaGunu.HaftaGunAdi;
                                _rw.BasSaat = item.BasSaat;
                                _rw.BitSaat = item.BitSaat;
                                _rw.SalonDurumID = SRSalonDurum.Alındı;
                                _rw.Aciklama = rezTip.TalepTipAdi + ", " + talep.Aciklama;
                                salonSaatleri.Add(_rw);
                            }
                            else
                            {
                                var qdata = salonSaatleri.FirstOrDefault(p => p.BasSaat == item.BasSaat && p.BitSaat == item.BitSaat);
                                if (tarih == talep.Tarih && qdata != null)
                                {
                                    qdata.SalonDurumID = SRSalonDurum.Alındı;
                                    qdata.Aciklama = rezTip.TalepTipAdi + ", " + talep.Aciklama;
                                }

                            }
                        }
                    }

                }
                if (talepTip.SRTalepTipleriAktifAylars.Any(a => a.AyID == nTarih.Month) == false && UserIdentity.Current.IsAdmin == false)
                {
                    salonSaatleri.Clear();
                    var syLst = talepTip.SRTalepTipleriAktifAylars.SelectMany(s => s.Aylar.AyAdi).ToList(); 
                    model.GenelAciklama = talepTip.TalepTipAdi + " talep tipi için talep yapılabilecek aylar: '" + string.Join(", ", syLst) + "' Bu ayların dışında sistem rezervasyon işlemine kapalıdır.";
                }
                else if (salonSaatleri.Count == 0)
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
                        item.SalonDurumID = qGTalepEslesen.SRDurumID == SRTalepDurum.Onaylandı ? SRSalonDurum.Dolu : SRSalonDurum.OnTalep;
                        item.Disabled = true;
                        item.Aciklama = qGTalepEslesen.SRDurumID == SRTalepDurum.Onaylandı ? rezTip.TalepTipAdi + ", " + qGTalepEslesen.Kullanicilar.Ad + " " + qGTalepEslesen.Kullanicilar.Soyad : "Onay bekliyor";

                    }
                    else if (resmiTatilDegisen != null)
                    {
                        item.SalonDurumID = SRSalonDurum.ResmiTatil;
                        item.Disabled = true;
                        item.Aciklama = resmiTatilDegisen.Aciklama;
                    }
                    else if (resmiTatilSabit != null)
                    {
                        item.SalonDurumID = SRSalonDurum.ResmiTatil;
                        item.Disabled = true;
                        item.Aciklama = resmiTatilSabit.Aciklama;
                    }
                    else if (rezerve != null)
                    {
                        var rezTip = db.SRTalepTipleris.First(p => p.SRTalepTipID == rezerve.SRTalepTipID);
                        item.SalonDurumID = SRSalonDurum.Dolu;
                        item.Disabled = true;
                        item.Aciklama = rezTip.TalepTipAdi + ", " + rezerve.Aciklama;
                    }
                    else if (rezervasyonlar != null)
                    {
                        var qRez = rezervasyonlar.SROzelTanimSaatlers.FirstOrDefault(a => a.SROzelTanimID != (srOzelTanimId ?? 0) &&
                        (
                            (a.BasSaat == item.BasSaat || a.BitSaat == item.BitSaat) ||
                            (
                                (a.BasSaat < item.BasSaat && a.BitSaat > item.BasSaat) || a.BasSaat < item.BitSaat && a.BitSaat > item.BitSaat) ||
                            (a.BasSaat > item.BasSaat && a.BasSaat < item.BitSaat) || a.BitSaat > item.BasSaat && a.BitSaat < item.BitSaat));
                        if (qRez != null)
                        {
                            var rezTip = db.SRTalepTipleris.First(p => p.SRTalepTipID == qRez.SROzelTanimlar.SRTalepTipID);
                            item.SalonDurumID = SRSalonDurum.Dolu;
                            item.Disabled = true;
                            item.Aciklama = rezTip.TalepTipAdi + ", " + rezervasyonlar.Aciklama;
                        }
                        else if (kTarih < nowDate && item.SalonDurumID == SRSalonDurum.Boş)
                        {
                            item.SalonDurumID = SRSalonDurum.GecmisTarih;
                            item.Disabled = true;
                            item.Aciklama = "Geçmişe dönük rezervasyon alınamaz.";
                        }
                    }
                    else if (kTarih < nowDate && item.SalonDurumID == SRSalonDurum.Boş)
                    {
                        item.SalonDurumID = SRSalonDurum.GecmisTarih;
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
        public static MmMessage SrKayitKontrol(int srSalonId, int srTalepTipId, DateTime tarih, List<SROzelTanimSaatler> saatler, int? srTalepId = null, int? srOzelTanimId = null, DateTime? tarih2 = null, List<int> haftaGunId = null, DateTime? minTarih = null)
        {
            var mmMessage = new MmMessage();
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var bitTar = tarih2 ?? tarih;
                haftaGunId = haftaGunId ?? new List<int>();
                for (var date = tarih; date <= bitTar; date = date.AddDays(1.0))
                {
                    var nTarih = date.ToShortDateString().ToDate().Value;
                    var dofW = nTarih.DayOfWeek.ToString("d").ToInt().Value;

                    if (!haftaGunId.Contains(dofW) && haftaGunId.Count > 0)
                    {
                        continue;
                    }
                    var salon = db.SRSalonlars.First(p => p.SRSalonID == srSalonId);

                    var haftaGunu = db.HaftaGunleris.First(p => p.HaftaGunID == dofW);
                    var resmiTatilDegisen = db.SROzelTanimlars.FirstOrDefault(p => p.IsAktif && p.SROzelTanimTipID == SROzelTanimTip.ResmiTatilDegisen && p.BasTarih.Value <= nTarih && p.BitTarih >= nTarih);
                    var resmiTatilSabit = db.SROzelTanimlars.FirstOrDefault(p => p.IsAktif && p.SROzelTanimTipID == SROzelTanimTip.ResmiTatilSabit && p.Ay.Value == nTarih.Month && p.Gun == nTarih.Day);
                    var rezervasyonlar = db.SROzelTanimlars.Where(p => p.SROzelTanimID != (srOzelTanimId ?? 0) && p.IsAktif && p.SROzelTanimTipID == SROzelTanimTip.Rezervasyon && p.SRSalonID == srSalonId && p.Tarih == nTarih).ToList();
                    var rezerve = db.SROzelTanimlars.FirstOrDefault(p => p.SROzelTanimGunlers.Any(a => a.HaftaGunID == dofW) && p.SROzelTanimID != (srOzelTanimId ?? 0) && p.IsAktif && p.SROzelTanimTipID == SROzelTanimTip.Rezerve && p.SRSalonID == srSalonId && p.BasTarih.Value <= nTarih && p.BitTarih >= nTarih);
                    var tTip = db.SRTalepTipleris.First(p => p.SRTalepTipID == srTalepTipId);


                    if (tTip.SRTalepTipleriAktifAylars.Any(a => a.AyID == nTarih.Month) == false && RoleNames.SrGelenTalepler.InRoleCurrent() == false)
                    {
                        var syLst = tTip.SRTalepTipleriAktifAylars.SelectMany(s => s.Aylar.AyAdi).ToList(); 
                        mmMessage.Messages.Add(tTip.TalepTipAdi + " talep tipi için talep yapılabilecek aylar: '" + string.Join(", ", syLst) + "' Bu ayların dışında sistem rezervasyon işlemine kapalıdır.");
                        mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "Tarih" });

                    }

                    else
                    {

                        if (tarih2.HasValue)
                        {

                            var qTalepEslesen = db.SRTalepleris.Where(a => a.SRSalonID == srSalonId && a.Tarih == nTarih).Any(p => p.SRDurumID == SRTalepDurum.Onaylandı || p.SRDurumID == SRTalepDurum.TalepEdildi);
                            if (qTalepEslesen)
                            {
                                mmMessage.Messages.Add(nTarih.ToShortDateString() + "Tarihi için " + salon.SalonAdi + " Salonu için dolu saatler var!");
                                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "Tarih" });
                            }
                            if (resmiTatilDegisen != null || resmiTatilSabit != null)
                            {

                                mmMessage.Messages.Add("Resmi tatillerde rezervasyon alınamaz.");
                                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "Tarih" });
                            }
                            else if (rezerve != null)
                            {

                                mmMessage.Messages.Add(nTarih.ToShortDateString() + " Tarihinde " + salon.SalonAdi + " Salonu doludur!");
                                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "Tarih" });

                            }
                            else if (rezervasyonlar.Count > 0)
                            {

                                mmMessage.Messages.Add(nTarih.ToShortDateString() + " Tarihinde " + salon.SalonAdi + " Salonu doludur!");
                                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "Tarih" });

                            }
                        }
                        else
                        {
                            foreach (var item in saatler)
                            {

                                var nowDate = DateTime.Now;
                                if (minTarih.HasValue) nowDate = minTarih.Value;
                                var kTarih = Convert.ToDateTime(tarih.ToShortDateString() + " " + item.BasSaat.Hours + ":" + item.BasSaat.Minutes + ":" + item.BasSaat.Seconds);

                                var qTalepEslesen = db.SRTalepleris.Where(a => a.SRTalepID != (srTalepId ?? 0) && a.SRSalonID == srSalonId && a.Tarih == nTarih &&
                                                                               (
                                                                                   (a.BasSaat == item.BasSaat || a.BitSaat == item.BitSaat) ||
                                                                                   (
                                                                                       (a.BasSaat < item.BasSaat && a.BitSaat > item.BasSaat) || a.BasSaat < item.BitSaat && a.BitSaat > item.BitSaat) ||
                                                                                   (a.BasSaat > item.BasSaat && a.BasSaat < item.BitSaat) || a.BitSaat > item.BasSaat && a.BitSaat < item.BitSaat)
                                                 );

                                if (qTalepEslesen.Any(p => p.SRDurumID == SRTalepDurum.Onaylandı || p.SRDurumID == SRTalepDurum.TalepEdildi))
                                {
                                    mmMessage.Messages.Add((nTarih.ToShortDateString() + " " + item.BasSaat.ToString() + " - " + item.BitSaat.ToString()) + " Tarihi için " + salon.SalonAdi + " Salonu Doludur! Lütfen boş bir saat seçiniz.");
                                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "Tarih" });
                                }
                                if (resmiTatilDegisen != null || resmiTatilSabit != null)
                                {
                                    ;
                                    mmMessage.Messages.Add("Resmi tatillerde rezervasyon alınamaz.");
                                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "Tarih" });
                                }
                                else if (rezerve != null)
                                {
                                    mmMessage.Messages.Add(item.BasSaat.ToString() + " - " + item.BitSaat.ToString() + " Tarihinde " + salon.SalonAdi + " Salonu doludur!");
                                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "Tarih" });

                                }
                                else if (rezervasyonlar.Count > 0)
                                {
                                    foreach (var itemRo in rezervasyonlar)
                                    {


                                        var qRez = itemRo.SROzelTanimSaatlers.FirstOrDefault(a => (
                                            (a.BasSaat == item.BasSaat || a.BitSaat == item.BitSaat) ||
                                            (
                                                (a.BasSaat < item.BasSaat && a.BitSaat > item.BasSaat) || a.BasSaat < item.BitSaat && a.BitSaat > item.BitSaat) ||
                                            (a.BasSaat > item.BasSaat && a.BasSaat < item.BitSaat) || a.BitSaat > item.BasSaat && a.BitSaat < item.BitSaat));
                                        if (qRez != null)
                                        {
                                            mmMessage.Messages.Add(item.BasSaat.ToString() + " - " + item.BitSaat.ToString() + " Tarihinde " + salon.SalonAdi + " Salonu doludur!");
                                            mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "Tarih" });
                                        }
                                    }
                                }
                                else if (kTarih < nowDate)
                                {
                                    mmMessage.Messages.Add("Geçmişe dönük rezervasyon alınamaz.");
                                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "Tarih" });
                                }
                                else if (salon.SRSaatlers.Any(a => a.BasSaat == item.BasSaat && a.BitSaat == item.BitSaat) == false)
                                {
                                    mmMessage.Messages.Add("Rezervasyon için seçilen sat uygun değildir.");
                                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "Tarih" });
                                }
                            }
                        }
                    }
                }

            }
            return mmMessage;
        }

        public static CmbMultyTypeDto GetSrKotaBilgi(int talepYapanId, int srTalepTipId, int? id = null)
        {
            var cmbMd = new CmbMultyTypeDto
            {
                ValueB = true
            };
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var ttip = db.SRTalepTipleris.First(p => p.SRTalepTipID == srTalepTipId);
                if (ttip.MaxCevaplanmamisTalep.HasValue)
                {
                    var q = db.SRTalepleris.Where(p => p.TalepYapanID == talepYapanId && p.SRTalepTipID == srTalepTipId && p.SRDurumID == SRTalepDurum.TalepEdildi);
                    if (id.HasValue) q = q.Where(p => p.SRTalepID != id.Value);
                    var kayitlar = q.ToList();
                    var cnt = kayitlar.Count;
                    cmbMd.Value = cnt;
                    if (ttip.MaxCevaplanmamisTalep.Value <= cnt && !id.HasValue)
                    {
                        cmbMd.ValueS = ttip.TalepTipAdi + " talep tipi için yapabileceğiniz rezervasyon talebi sayısı " + ttip.MaxCevaplanmamisTalep.Value + " adettir, daha önceden yapmış olduğunuz " + cnt + "  adet işlem bekleyen rezervasyon talebiniz bulunmaktadır. İşlem bekleyen rezervasyon talepleriniz işlem görene kadar yeni rezervasyon talebi yapamazsınız!";
                        cmbMd.ValueB = false;
                    }
                    else
                    {
                        cmbMd.ValueS = ttip.TalepTipAdi + " talep tipi için yapabileceğiniz rezervasyon talebi sayısı " + ttip.MaxCevaplanmamisTalep.Value + " adettir, daha önceden yapmış olduğunuz " + kayitlar + "  adet işlem bekleyen rezervasyon talebiniz bulunmaktadır. " + ttip.MaxCevaplanmamisTalep.Value + " adet yeni rezervasyon talebi yapabilirsiniz!";
                        cmbMd.ValueB = true;
                    }

                }
            }
            return cmbMd;
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

        public static List<CmbIntDto> GetCmbTalepTipleri(int? kullaniciTipId, bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var qdata = db.SRTalepTipleris.Where(p => p.IsTezSinavi == false).OrderBy(o => o.SRTalepTipID).AsQueryable();
                if (kullaniciTipId.HasValue) qdata = qdata.Where(p => p.SRTalepTipKullanicilars.Any(a => a.KullaniciTipID == kullaniciTipId));
                var data = qdata.ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.SRTalepTipID, Caption = item.TalepTipAdi });
                }
            }
            return dct;
        }

        public static List<CmbIntDto> GetCmbSrDurum(bool bosSecimVar = false, bool yonetici = false, bool yeniKayit = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.SRDurumlaris.Where(p => p.SRDurumID == (yeniKayit ? BelgeTalepDurum.TalepEdildi : p.SRDurumID) && p.IsAktif && (yonetici || p.TalepEdenGorsun == true)).OrderBy(o => o.SRDurumID).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.SRDurumID, Caption = item.DurumAdi });
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