using DevExpress.XtraReports.UI;
using Entities.Entities;
using LisansUstuBasvuruSistemi.Raporlar.Genel;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.MailManager;
using System;
using System.Collections.Generic;
using System.Linq;
using LisansUstuBasvuruSistemi.Utilities.SystemSetting;

namespace LisansUstuBasvuruSistemi.Business
{
    public class SrTalepleriBus
    {
        public static SRSalonSaatlerModel GetSalonBosSaatler(int srSalonId, int srTalepTipId, DateTime tarih, int? srTalepId = null, int? srOzelTanimId = null, DateTime? minTarih = null)
        {
            var model = new SRSalonSaatlerModel();
            using (var entities = new LubsDbEntities())
            {
                var nTarih = tarih.Date;
                var dofW = Convert.ToInt32(nTarih.DayOfWeek.ToString("d"));
                var haftaGunu = entities.HaftaGunleris.First(p => p.HaftaGunID == dofW);
                var salon = entities.SRSalonlars.First(p => p.SRSalonID == srSalonId);
                var secilenTarihRezervasyonlar = entities.SRTalepleris.Where(p => p.SRSalonID == srSalonId && p.Tarih == nTarih && (p.SRDurumID == SrTalepDurumEnum.Onaylandı || p.SRDurumID == SrTalepDurumEnum.TalepEdildi)).ToList();
                var resmiTatilDegisen = entities.SROzelTanimlars.FirstOrDefault(p => p.IsAktif && p.SROzelTanimTipID == SrOzelTanimTipiEnum.ResmiTatilDegisen && p.BasTarih.Value <= nTarih && p.BitTarih >= nTarih);
                var resmiTatilSabit = entities.SROzelTanimlars.FirstOrDefault(p => p.IsAktif && p.SROzelTanimTipID == SrOzelTanimTipiEnum.ResmiTatilSabit && p.Ay.Value == nTarih.Month && p.Gun == nTarih.Day);
                model.Tarih = nTarih;
                var salonSaatleri = entities.SRSaatlers.Where(p => p.SRSalonID == srSalonId && p.HaftaGunID == haftaGunu.HaftaGunID).Select(s => new SRSalonSaatler
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

                        var rezTip = entities.SRTalepTipleris.First(p => p.SRTalepTipID == qGTalepEslesen.SRTalepTipID);
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
                             join d in entities.SRSalonDurumlaris on s.SalonDurumID equals d.SRSalonDurumID
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
            using (var entities = new LubsDbEntities())
            {


                var salon = entities.SRSalonlars.First(p => p.SRSalonID == srSalonId);

                var resmiTatilDegisen = entities.SROzelTanimlars.FirstOrDefault(p => p.IsAktif && p.SROzelTanimTipID == SrOzelTanimTipiEnum.ResmiTatilDegisen && p.BasTarih.Value <= rezervasyonTarihi.Date && p.BitTarih >= rezervasyonTarihi.Date);
                var resmiTatilSabit = entities.SROzelTanimlars.FirstOrDefault(p => p.IsAktif && p.SROzelTanimTipID == SrOzelTanimTipiEnum.ResmiTatilSabit && p.Ay.Value == rezervasyonTarihi.Date.Month && p.Gun == rezervasyonTarihi.Date.Day);


                minRezervasyonTarihi = minRezervasyonTarihi ?? DateTime.Now;

                var kTarih = rezervasyonTarihi.Date.Add(baslangicSaati);

                var qTalepEslesen = entities.SRTalepleris.Where(a => a.SRTalepID != (srTalepId ?? 0) &&
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


        public static XtraReport MezuniyetSinavSureciDoktoraSinavBilgilendirmeYazilari(int srTalepId)
        {
            using (var entities = new LubsDbEntities())
            {

                var srTalep = entities.SRTalepleris.First(f => f.SRTalepID == srTalepId);
                var mezuniyetBasvuru = srTalep.MezuniyetBasvurulari;

                var mezuniyetSureci = mezuniyetBasvuru.MezuniyetSureci;
                var enstitu = mezuniyetSureci.Enstituler;
                var anabilimDaliAdi = mezuniyetBasvuru.Programlar.AnabilimDallari.AnabilimDaliAdi.IlkHarfiBuyut();
                var programAdi = mezuniyetBasvuru.Programlar.ProgramAdi.IlkHarfiBuyut();
                var ogrenciNo = mezuniyetBasvuru.OgrenciNo;
                var ogrenciAdSoyad = (mezuniyetBasvuru.Kullanicilar.Ad).IlkHarfiBuyut() + " " + mezuniyetBasvuru.Kullanicilar.Soyad.ToUpper();

                var jof = mezuniyetBasvuru.MezuniyetJuriOneriFormlaris.First();

                var tezBasligiDegisenSinav = mezuniyetBasvuru.SRTalepleris.OrderByDescending(o => o.SRTalepID).FirstOrDefault(a =>
                    a.MezuniyetSinavDurumID != MezuniyetSinavDurumEnum.SonucGirilmedi &&
                    a.IsTezBasligiDegisti == true);


                var baslikTr = tezBasligiDegisenSinav == null ? (jof.IsTezBasligiDegisti == true ? jof.YeniTezBaslikTr : jof.MezuniyetBasvurulari.TezBaslikTr) : tezBasligiDegisenSinav.YeniTezBaslikTr;
                var baslikEn = tezBasligiDegisenSinav == null ? (jof.IsTezBasligiDegisti == true ? jof.YeniTezBaslikEn : jof.MezuniyetBasvurulari.TezBaslikEn) : tezBasligiDegisenSinav.YeniTezBaslikEn;
                var isTezBaslikTr = jof.MezuniyetBasvurulari.IsTezDiliTr == true;
                var tezBaslik = isTezBaslikTr ? baslikTr : baslikEn;


                var sablonInx = 0;
                XtraReport rprX = null;
                var tumJuriler = srTalep.SRTaleplerJuris.OrderBy(o => o.JuriTipAdi.Contains("TezDanismani") ? 1 : 2).ThenBy(t => t.JuriTipAdi.Contains("Tik") ? 1 : 2).ThenBy(t => t.JuriTipAdi.Contains("YtuIci") ? 1 : 2).ThenBy(o => o.JuriTipAdi).ToList();

                var tezDanisman = tumJuriler.First(f => f.JuriTipAdi == "TezDanismani");
                var juriUyeleri = tumJuriler.Where(p => !p.JuriTipAdi.Contains("TezDanismani")).ToList();

                var sablonTipIds = new List<int>
                    {
                        YaziSablonTipiEnum.MezuniyetSinavSureciDrSinavBilgilendirmeYazisiAbd,
                        YaziSablonTipiEnum.MezuniyetSinavSureciDrSinavBilgilendirmeYazisiDanisman,
                        YaziSablonTipiEnum.MezuniyetSinavSureciDrSinavBilgilendirmeYazisiTumJuriler

                };


                var sablonlar = entities.YaziSablonlaris.Where(p => sablonTipIds.Contains(p.YaziSablonTipID) && p.EnstituKod == enstitu.EnstituKod && p.IsAktif).ToList();
                var sablonModel = new List<KeyValuePair<YaziSablonlari, SRTaleplerJuri>>();

                // sablonTipIds koleksiyonunu LINQ ile işliyoruz
                foreach (var sablonTipId in sablonTipIds)
                {
                    var sablon = sablonlar.FirstOrDefault(f => f.YaziSablonTipID == sablonTipId);
                    if (sablon == null) continue;
                    if (sablon.YaziSablonTipID == YaziSablonTipiEnum.MezuniyetSinavSureciDrSinavBilgilendirmeYazisiDanisman)
                    {
                        sablonModel.Add(new KeyValuePair<YaziSablonlari, SRTaleplerJuri>(sablon, tezDanisman));
                    }
                    else if (sablon.YaziSablonTipID == YaziSablonTipiEnum.MezuniyetSinavSureciDrSinavBilgilendirmeYazisiTumJuriler)
                    {
                        juriUyeleri.ForEach(item => sablonModel.Add(new KeyValuePair<YaziSablonlari, SRTaleplerJuri>(sablon, item)));
                    }
                    else sablonModel.Add(new KeyValuePair<YaziSablonlari, SRTaleplerJuri>(sablon, new SRTaleplerJuri()));
                }

                var salonAdi = srTalep.SRSalonID.HasValue ? srTalep.SRSalonlar.SalonAdi : srTalep.SalonAdi;

                foreach (var sablon in sablonModel)
                {

                    var parameters = new List<MailParameterDto>
                    {
                        new MailParameterDto { Key = "AnabilimDaliAdi", Value = anabilimDaliAdi.IlkHarfiBuyut() },
                        new MailParameterDto { Key = "ProgramAdi", Value = programAdi.IlkHarfiBuyut() },
                        new MailParameterDto { Key = "OgrenciNo", Value = ogrenciNo },
                        new MailParameterDto { Key = "OgrenciAdSoyad", Value = ogrenciAdSoyad.IlkHarfiBuyut() },
                        new MailParameterDto { Key = "DanismanUnvan", Value = tezDanisman.UnvanAdi.IlkHarfiBuyut() },
                        new MailParameterDto { Key = "DanismanAdSoyad", Value = tezDanisman.JuriAdi.IlkHarfiBuyut() },
                        new MailParameterDto { Key = "TezBaslik", Value =tezBaslik },
                        new MailParameterDto { Key = "SinavTarihi", Value =srTalep.Tarih.ToFormatDate()+" "+$"{srTalep.BasSaat:hh\\:mm}" + "-" + $"{srTalep.BitSaat:hh\\:mm}" },
                        new MailParameterDto { Key = "SalonAdi", Value =salonAdi.IlkHarfiBuyut() },
                        new MailParameterDto { Key = "SeciliKomiteUyesiUnvan", Value = sablon.Value.UnvanAdi.IlkHarfiBuyut()},
                        new MailParameterDto { Key = "SeciliKomiteUyesiAdSoyad", Value =  sablon.Value.JuriAdi.IlkHarfiBuyut()},
                        new MailParameterDto { Key = "SeciliKomiteUyesiUniversite", Value =  sablon.Value.UniversiteAdi.IlkHarfiBuyut()}
                    };
                    parameters.AddRange(SetParameterJuris(juriUyeleri, "AsilKomiteUyesi"));
                    var html = ValueReplaceExtension.ProcessHtmlContent(sablon.Key.SablonHtml, parameters);
                    var htmlFooter = ValueReplaceExtension.ProcessHtmlContent(sablon.Key.SablonFooterHtml, parameters);
                    if (sablonInx == 0)
                    {
                        rprX = new RprYaziSablonOlusturucu(enstitu, html, htmlFooter, sablon.Key.Konu);
                        rprX.CreateDocument();
                    }
                    else
                    {
                        var rapor = new RprYaziSablonOlusturucu(enstitu, html, htmlFooter, sablon.Key.Konu);
                        rapor.CreateDocument();
                        rprX.Pages.AddRange(rapor.Pages);
                    }


                    sablonInx++;
                }
                return rprX;

            }
        }
        private static List<MailParameterDto> SetParameterJuris(List<SRTaleplerJuri> juris, string key)
        {
            var inx = 0;
            var parameters = new List<MailParameterDto>();
            foreach (var itemJuri in juris)
            {


                inx++;
                parameters.AddRange(new List<MailParameterDto>{
                    new MailParameterDto { Key = $"{key}{inx}Unvan", Value = itemJuri.UnvanAdi.IlkHarfiBuyut() },
                    new MailParameterDto { Key = $"{key}{inx}AdSoyad", Value = itemJuri.JuriAdi.IlkHarfiBuyut() },
                    new MailParameterDto { Key = $"{key}{inx}Universite", Value = itemJuri.UniversiteAdi.IlkHarfiBuyut() },
                });
            }

            return parameters;
        }

        public static List<CmbIntDto> GetCmbOzelTanimTipleri(bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var entities = new LubsDbEntities())
            {
                var data = entities.SROzelTanimTipleris.OrderBy(o => o.SROzelTanimTipID).ToList();
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
            using (var entities = new LubsDbEntities())
            {
                var qdata = entities.SRTalepTipleris.Where(p => p.IsTezSinavi == false).OrderBy(o => o.SRTalepTipID).AsQueryable();
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
            using (var entities = new LubsDbEntities())
            {
                var data = entities.SRDurumlaris.Where(p => p.IsAktif).OrderBy(o => o.SRDurumID).ToList();
                return data;

            }
        }

        public static List<CmbIntDto> GetCmbSrDurumListe(bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var entities = new LubsDbEntities())
            {
                var data = entities.SRDurumlaris.Where(p => p.IsAktif).OrderBy(o => o.SRDurumID).ToList();
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
            using (var entities = new LubsDbEntities())
            {
                var qdata = entities.SRSalonlars.Where(p => p.IsAktif && p.EnstituKod == enstituKod);

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
            using (var entities = new LubsDbEntities())
            {
                var qdata = entities.SRSalonlars.Where(p => p.IsAktif && p.EnstituKod == enstituKod && p.SRSalonTalepTipleris.Any(a => a.SRTalepTipID == srTalepTipId));

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
            using (var entities = new LubsDbEntities())
            {
                var data = entities.Aylars.OrderBy(o => o.AyID).ToList();
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
            using (var entities = new LubsDbEntities())
            {
                var qdata = entities.HaftaGunleris.AsQueryable();
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
            using (var entities = new LubsDbEntities())
            {
                var data = entities.SRTalepTipleris.OrderBy(o => o.SRTalepTipID).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.SRTalepTipID, Caption = item.TalepTipAdi });
                }
            }
            return dct;
        }


        public static List<ThesisInviteVm> GetSonSrTalebiDavetData(Enstituler enstitu, int? srTalepId = null)
        {
            using (var entities = new LubsDbEntities())
            {
                var take = MezuniyetAyar.TezSinaviDavetListesindeGosterilecekKisiSayisi
                    .GetAyar(enstitu.EnstituKod, "20").ToInt().Value;

                var now = DateTime.Now;

                // İlk sorgu: Veritabanı tarafında yapılabilecek filtreleme ve sıralama
                var raw = (from s in entities.SRTalepleris.Where(p => p.SRTalepID == (srTalepId ?? p.SRTalepID))
                           join sal in entities.SRSalonlars on s.SRSalonID equals sal.SRSalonID into defSal
                           from salon in defSal.DefaultIfEmpty()
                           let mb = s.MezuniyetBasvurulari
                           let jof = mb.MezuniyetJuriOneriFormlaris.FirstOrDefault()
                           where s.EnstituKod == enstitu.EnstituKod
                                 && s.MezuniyetBasvurulariID.HasValue
                                 && s.SRDurumID == SrTalepDurumEnum.Onaylandı
                                 && OgrenimTipi.DoktoraOgretimleri.Contains(mb.OgrenimTipKod)
                           orderby s.Tarih, s.BasSaat  // Önce tarihe, sonra saate göre sırala
                           select new
                           {
                               s.SRTalepID,
                               // kişi & program
                               FullName = mb.Ad + " " + mb.Soyad,
                               Department = mb.Programlar.AnabilimDallari.AnabilimDaliAdi,
                               Program = mb.Programlar.ProgramAdi,

                               // danışman
                               Advisor = (mb.TezDanismanUnvani ?? "") +
                                         (string.IsNullOrEmpty(mb.TezDanismanUnvani) ? "" : " ") +
                                         (mb.TezDanismanAdi ?? ""),

                               // başlık (TR/EN + değişiklik önceliği: SR -> JOF -> MB)
                               IsTezDiliTr = (mb.IsTezDiliTr ?? true),
                               SrIsDegisti = (s.IsTezBasligiDegisti ?? false),
                               SrYeniTr = s.YeniTezBaslikTr,
                               SrYeniEn = s.YeniTezBaslikEn,
                               JofIsDegisti = (jof != null && jof.IsTezBasligiDegisti == true),
                               JofYeniTr = (jof != null ? jof.YeniTezBaslikTr : null),
                               JofYeniEn = (jof != null ? jof.YeniTezBaslikEn : null),
                               MbTr = mb.TezBaslikTr,
                               MbEn = mb.TezBaslikEn,

                               // zaman & yer
                               Tarih = s.Tarih,
                               BasSaat = s.BasSaat,
                               BitSaat = s.BitSaat,
                               SalonAdi = s.SRSalonID.HasValue ? salon.SalonAdi : s.SalonAdi,

                               // avatar (varsa)
                               AvatarFile = mb.Kullanicilar.ResimAdi
                           })
                           .ToList(); // Veritabanından çek

                // Şimdi şimdiki zamana en yakın olanları bul (memory'de)
                var sortedByProximity = raw
                    .Select(r => new
                    {
                        Data = r,
                        FullDateTime = r.Tarih.Add(r.BasSaat),
                        AbsoluteMinutes = Math.Abs((r.Tarih.Add(r.BasSaat) - now).TotalMinutes)
                    })
                    .OrderBy(x => x.AbsoluteMinutes) // Şimdiki zamana en yakın
                    .Take(take) // İlk N tanesini al
                    .Select(x => x.Data)
                    .ToList();

                // EF tarafında string formatlamayı zorlamayıp C# tarafında son hale getiriyoruz.
                var list = new List<ThesisInviteVm>();

                foreach (var r in sortedByProximity)
                {
                    // Tez başlığı seçim mantığı:
                    // 1) SR talepte yeni başlık varsa onu kullan
                    // 2) yoksa JOF'taki yeni başlık varsa onu kullan
                    // 3) yoksa başvurudaki (MB) başlığı kullan
                    string titleTr = r.SrIsDegisti ? (r.SrYeniTr ?? r.JofYeniTr ?? r.MbTr)
                                   : r.JofIsDegisti ? (r.JofYeniTr ?? r.MbTr)
                                   : r.MbTr;

                    string titleEn = r.SrIsDegisti ? (r.SrYeniEn ?? r.JofYeniEn ?? r.MbEn)
                                   : r.JofIsDegisti ? (r.JofYeniEn ?? r.MbEn)
                                   : r.MbEn;

                    string finalTitle = (r.IsTezDiliTr ? titleTr : (titleEn ?? titleTr)) ?? ""; // emniyet

                    // Tarih/Saat formatları—UI'da gerekirse değiştirilebilir
                    string dateText = r.Tarih.ToFormatDateDay();
                    string timeText = $"{r.BasSaat:hh\\:mm} - {r.BitSaat:hh\\:mm}";

                    string avatarPath = string.IsNullOrWhiteSpace(r.AvatarFile)
                        ? null
                        : r.AvatarFile.ToKullaniciResim();

                    list.Add(new ThesisInviteVm
                    {
                        EnstituKod = enstitu.EnstituKod,
                        TableId = r.SRTalepID,
                        FullName = r.FullName,
                        Department = r.Department,
                        Program = r.Program,
                        ThesisTitle = finalTitle,
                        Advisor = r.Advisor,
                        Date = dateText,
                        Time = timeText,
                        Place = r.SalonAdi ?? "",
                        AvatarPath = avatarPath
                    });
                }

                return list;
            }
        }
    }
}