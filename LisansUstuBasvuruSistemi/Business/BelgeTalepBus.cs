using System;
using System.Collections.Generic;
using System.Linq;
using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.SystemSetting;

namespace LisansUstuBasvuruSistemi.Business
{
    public class BelgeTalepBus
    {
        public static BelgeTipDetay GetBelgeTipDetay(int belgeTipId, int ogrenimDurumId, string enstituKod)
        {
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var btip = db.BelgeTipDetays.First(p => p.BelgeTipDetayBelgelers.Any(a => a.BelgeTipID == belgeTipId) && p.OgrenimDurumID == ogrenimDurumId && p.EnstituKod == enstituKod);
                return btip;
            }
        }

        public static List<CmbStringDto> GetCmbBelgeTeslimSaatler()
        {
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var saatler = db.BelgeTipDetaySaatlers.OrderBy(o => o.TalepBaslangicSaat).Select(s => new { s.TeslimBaslangicSaat, s.TeslimBitisSaat }).Distinct().ToList();
                var lst = new List<CmbStringDto>
                {
                    new CmbStringDto { Caption = "" },
                    new CmbStringDto { Value = "00:00-23:59", Caption = "Bugün verilecekler (Tümü)" }
                };
                foreach (var item in saatler)
                {
                    var bsSt = $"{item.TeslimBaslangicSaat:hh\\:mm}";
                    var btSt = $"{item.TeslimBitisSaat:hh\\:mm}";
                    lst.Add(new CmbStringDto { Value = (bsSt + "-" + btSt), Caption = "Bugün verilecekler (" + (bsSt + "-" + btSt) + ")" });
                }

                return lst;
            }
        }
        public static BelgeTipDetaySaatler GetCmbSelectedSaat(DateTime islemTarihi, int belgeTipId, int ogrenimDurumId, string enstituKod)
        {
            var rtatilDurum = BelgeTalepAyar.BelgeTalebiResmiTatilDurum.GetAyarBt(enstituKod, "0").ToBoolean() ?? false;
            TimeSpan talepZamani = new TimeSpan(islemTarihi.Hour, islemTarihi.Minute, islemTarihi.Second);
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var dofW =  islemTarihi.DayOfWeek.ToString("d").ToInt();
                var bastarih = islemTarihi.TodateToShortDate();
                var tarih = islemTarihi.TodateToShortDate();
                tekrarKontrol:
                var btSaat = db.BelgeTipDetaySaatlers.Include("BelgeTipDetay").First(p => p.BelgeTipDetay.OgrenimDurumID == ogrenimDurumId && p.HaftaGunID == dofW && p.BelgeTipDetay.BelgeTipDetayBelgelers.Any(a => a.BelgeTipID == belgeTipId) && p.TalepBaslangicSaat <= talepZamani && p.TalepBitisSaat >= talepZamani);
                tarih = tarih.AddDays(btSaat.EklenecekGun);

                if (rtatilDurum)
                {
                    var uygunlukKontrol = GetCmbUygunKontrol(tarih);
                    if (uygunlukKontrol.Value.Value == false)
                    {
                        tarih = uygunlukKontrol.Caption.Value;
                        dofW = tarih.DayOfWeek.ToString("d").ToInt().Value;

                        talepZamani = new TimeSpan(1, 1, 1);
                        goto tekrarKontrol;
                    }
                    btSaat.EklenecekGun = (tarih - bastarih).TotalDays.ToIntObj().Value;
                }
                return btSaat;
            }
        }
        public static CmbBoolDatetimeDto GetCmbUygunKontrol(DateTime nTarih)
        {
            var mdl = new CmbBoolDatetimeDto
            {
                Value = true,
                Caption = nTarih
            };
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var success = false;
                while (success == false)
                {
                    success = true;
                    var resmiTatilDegisen = db.SROzelTanimlars.FirstOrDefault(p => p.IsAktif && p.SROzelTanimTipID == SrOzelTanimTipiEnum.ResmiTatilDegisen && p.BasTarih.Value <= mdl.Caption && p.BitTarih >= mdl.Caption);
                    if (resmiTatilDegisen != null)
                    {
                        success = false;
                        mdl.Value = false;
                        mdl.Caption = nTarih = resmiTatilDegisen.BitTarih.Value.AddDays(1);
                    }
                    else
                    {
                        var resmiTatilSabit = db.SROzelTanimlars.FirstOrDefault(p => p.IsAktif && p.SROzelTanimTipID == SrOzelTanimTipiEnum.ResmiTatilSabit && p.Ay.Value == mdl.Caption.Value.Month && p.Gun == mdl.Caption.Value.Day);
                        if (resmiTatilSabit != null)
                        {
                            success = false;
                            mdl.Value = false;
                            mdl.Caption = nTarih = nTarih.AddDays(1);
                        }
                    }
                }
            }
            return mdl;
        }

        public static CmbIntDto GetBelgeVerilmeBilgisi(int belgeTalepId, string islemTipListeAdi)
        {
            var html = "";
            var mdl = new CmbIntDto();
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var belge = db.BelgeTalepleris.First(p => p.BelgeTalepID == belgeTalepId);
                if (belge.BelgeDurumID == BelgeTalepDurumEnum.TalepEdildi || belge.BelgeDurumID == BelgeTalepDurumEnum.Hazirlaniyor || belge.BelgeDurumID == BelgeTalepDurumEnum.Hazirlandi)
                {
                    var verilecekTarih = belge.TalepTarihi.AddDays(belge.EklenecekGun).TodateToShortDate();
                    var days = (verilecekTarih - DateTime.Now.TodateToShortDate());
                    var day = Convert.ToInt32(days.Days);
                    var gelecek = (days.Days > 0);
                    mdl.Value = days.Days;
                    var saatAralik = belge.TeslimBaslangicSaat.Value.ToString(@"hh\:mm") + "-" + belge.TeslimBitisSaat.Value.ToString(@"hh\:mm");
                    var durum = "(" + belge.BelgeDurumlari.DurumAdi + ")";
                    if (verilecekTarih == DateTime.Now.TodateToShortDate())
                    {
                        html += "<span style='font-size:9pt;font-weight:bold;'>" + verilecekTarih.ToFormatDate() + " " + saatAralik + "</span> <br /><span style='font-size:8.5pt;'>Bu Gün Verilecek " + durum + "</span>";
                    }
                    else if (day == 1)
                    {
                        html += "<span style='font-size:9pt;font-weight:bold;'>" + verilecekTarih.ToFormatDate() + " " + saatAralik + "</span> <br /><span style='font-size:8.5pt;'>Yarın Verilecek " + durum + "</span>";
                    }
                    else
                    {
                        if (gelecek)
                        {
                            html += "<span style='font-size:9pt;font-weight:bold;'>" + verilecekTarih.ToFormatDate() + " " + saatAralik + "</span> <br /><span style='font-size:8.5pt;'>" + day + " Gün Sonra Verilecek " + durum + "</span>";
                        }
                        else
                        {
                            html += "<span style='font-size:9pt;font-weight:bold;'>" + verilecekTarih.ToFormatDate() + " " + saatAralik + "</span> <br /><span style='font-size:8.5pt;'>" + Math.Abs(day) + " Gün Önce Verilmeliydi" + durum + "</span>";

                        }
                    }
                }
                else if (belge.BelgeDurumID == BelgeTalepDurumEnum.Verildi)
                {
                    html += "<span style='font-size:9pt;font-weight:bold;'>" + belge.IslemTarihi.ToString("dd-MM-yyyy HH:mm:ss") + "</span> <br /><span style='font-size:8.5pt;'>Tarihinde Verildi</span>";

                }
                else
                {
                    html += "<span style='font-size:9pt;font-weight:bold;'>" + belge.IslemTarihi.ToString("dd-MM-yyyy HH:mm:ss") + "</span> <br /><span style='font-size:8.5pt;'>Tarihinde " + islemTipListeAdi + "</span>";
                }
                mdl.Caption = html;
                return mdl;
            }
        }

        public static List<CmbIntDto> GetCmbBelgeTipleri(bool bosSecimVar = false, int? ogrenimDurumId = null, string enstituKod = null)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.BelgeTipleris.Where(p => (!ogrenimDurumId.HasValue || p.BelgeTipDetayBelgelers.Any(a => a.BelgeTipID == p.BelgeTipID && a.BelgeTipDetay.OgrenimDurumID == ogrenimDurumId.Value && a.BelgeTipDetay.EnstituKod == enstituKod && a.BelgeTipDetay.IsAktif)) && p.IsAktif).OrderBy(o => o.BelgeTipID).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.BelgeTipID, Caption = item.BelgeTipAdi });
                }
            }
            return dct;

        }

        public static List<CmbIntDto> GetCmbBelgeTalepDurum(bool bosSecimVar = false, bool yonetici = false, bool yeniKayit = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.BelgeDurumlaris.Where(p => p.BelgeDurumID == (yeniKayit ? BelgeTalepDurumEnum.TalepEdildi : p.BelgeDurumID) && p.IsAktif && (yonetici || p.TalepEdenGorsun == true)).OrderBy(o => o.BelgeDurumID).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.BelgeDurumID, Caption = item.DurumAdi });
                }
            }
            return dct;

        }

        public static List<CmbStringDto> GetDiller(bool bosSecimVar = false)
        {
            var dct = new List<CmbStringDto>();
            if (bosSecimVar) dct.Add(new CmbStringDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var diller = db.SistemDilleris.ToList();
                foreach (var item in diller)
                {
                    dct.Add(new CmbStringDto { Value = item.DilKodu, Caption = item.DilAdi });
                }
            }
            return dct;

        }

        public static List<BelgeDurumlari> GetBelgeTalepDurumList()
        {
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.BelgeDurumlaris.Where(p => p.IsAktif).OrderBy(o => o.BelgeDurumID).ToList();
                return data;

            }

        }

        public static List<CmbIntDto> GetCmbBelgeTalepDurumListe(bool bosSecimVar = false)
        {
            var dct = new List<CmbIntDto>();
            if (bosSecimVar) dct.Add(new CmbIntDto { Value = null, Caption = "" });
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var data = db.BelgeDurumlaris.Where(p => p.IsAktif).OrderBy(o => o.BelgeDurumID).ToList();
                foreach (var item in data)
                {
                    dct.Add(new CmbIntDto { Value = item.BelgeDurumID, Caption = item.DurumAdi });
                }
            }
            return dct;

        }
    }
}