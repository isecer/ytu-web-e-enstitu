using System.Linq;
using System.Web.Mvc;
using LisansUstuBasvuruSistemi.Business;
using Entities.Entities;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.Helpers;
using LisansUstuBasvuruSistemi.Utilities.MenuAndRoles;
using System.IO;
using System;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Collections.Generic;
using BiskaUtil;
using DevExpress.XtraPrinting;
using DevExpress.XtraReports.UI;
using LisansUstuBasvuruSistemi.Raporlar.KayitSilme;
using LisansUstuBasvuruSistemi.Utilities.Logs;
using LisansUstuBasvuruSistemi.Utilities.SystemSetting;

namespace LisansUstuBasvuruSistemi.Controllers
{
    [Authorize(Roles = RoleNames.KayitSilmeGelenBasvurular)]
    [OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    public class KsGelenBasvurularController : Controller
    {
        private readonly LubsDbEntities _entities = new LubsDbEntities();
        public ActionResult Index(string ekd)
        {

            return Index(new FmKayitSilmeBasvuruDto() { PageSize = 50 }, ekd);
        }
        [HttpPost]
        public ActionResult Index(FmKayitSilmeBasvuruDto model, string ekd, bool export = false)
        {
            var enstituKod = EnstituBus.GetSelectedEnstitu(ekd);
            var enstituYetkileri = UserIdentity.Current.EnstituKods;

            var selectedHarcUserId = KayitSilmeAyar.HarcBirimiOnaySorumlusuKullaniciId.GetAyar(enstituKod).ToIntObj();
            var isBirimOnaySorumlusu = selectedHarcUserId.HasValue && selectedHarcUserId == UserIdentity.Current.Id;
            if (!isBirimOnaySorumlusu)
            {
                var selectedKutuphaneUserId = KayitSilmeAyar.KutuphaneBirimiOnaySorumlusuKullaniciId.GetAyar(enstituKod).ToIntObj();

                isBirimOnaySorumlusu = selectedKutuphaneUserId.HasValue && selectedHarcUserId == UserIdentity.Current.Id;
                if (isBirimOnaySorumlusu)
                    enstituYetkileri.AddRange(new List<string>
                    {
                        EnstituKodlariEnum.FenBilimleri,
                        EnstituKodlariEnum.SosyalBilimleri,
                        EnstituKodlariEnum.TemizEnerjiTeknolojileri
                    });
            }


            var q =
                    from kayitSilme in _entities.KayitSilmeBasvurus
                    join kullanicilar in _entities.Kullanicilars on kayitSilme.KullaniciID equals kullanicilar.KullaniciID
                    join programlar in _entities.Programlars on kayitSilme.ProgramKod equals programlar.ProgramKod
                    join ogrenimTipleri in _entities.OgrenimTipleris on new { kayitSilme.EnstituKod, kayitSilme.OgrenimTipKod } equals new { ogrenimTipleri.EnstituKod, ogrenimTipleri.OgrenimTipKod }
                    where kayitSilme.EnstituKod == enstituKod && enstituYetkileri.Contains(kayitSilme.EnstituKod)
                    select new FrKayitSilmeBasvuruDto
                    {
                        KayitSilmeBasvuruID = kayitSilme.KayitSilmeBasvuruID,
                        UniqueID = kayitSilme.UniqueID,
                        KullaniciID = kayitSilme.KullaniciID,
                        KayitSilmeDurumID = kayitSilme.KayitSilmeDurumID,
                        DonemID = kayitSilme.DonemID,
                        DonemAdi = kayitSilme.Donemler.DonemAdi,
                        AkademikDonemID = kayitSilme.OgretimYiliBaslangic + "" + kayitSilme.DonemID,
                        BasvuruTarihi = kayitSilme.BasvuruTarihi,
                        ResimAdi = kullanicilar.ResimAdi,
                        UserKey = kullanicilar.UserKey,
                        AdSoyad = kullanicilar.Ad + " " + kullanicilar.Soyad,
                        OgrenciNo = kayitSilme.OgrenciNo,
                        TcKimlikNo = kullanicilar.TcKimlikNo,
                        OgrenimTipKod = kayitSilme.OgrenimTipKod,
                        OgrenimTipAdi = ogrenimTipleri.OgrenimTipAdi,
                        ProgramKod = kayitSilme.ProgramKod,
                        ProgramAdi = programlar.ProgramAdi,
                        AnabilimDaliAdi = programlar.AnabilimDallari.AnabilimDaliAdi,
                        OgretimYiliBaslangic = kayitSilme.OgretimYiliBaslangic,
                        IsHarcBirimiOnayladi = kayitSilme.IsHarcBirimiOnayladi,
                        IsKutuphaneBirimiOnayladi = kayitSilme.IsKutuphaneBirimiOnayladi,
                        EYKYaGonderildi = kayitSilme.EYKYaGonderildi,
                        EYKYaGonderimDurumAciklamasi = kayitSilme.EYKYaGonderimDurumAciklamasi,
                        EYKYaHazirlandi = kayitSilme.EYKYaHazirlandi,
                        EYKDaOnaylandi = kayitSilme.EYKDaOnaylandi,
                        EYKDaOnaylanmadiDurumAciklamasi = kayitSilme.EYKDaOnaylanmadiDurumAciklamasi,
                        EYKTarihi = kayitSilme.EYKTarihi,
                        EYKSayisi = kayitSilme.EYKSayisi
                    };
            var q2 = q;
            if (!model.AkademikDonemID.IsNullOrWhiteSpace()) q = q.Where(p => p.AkademikDonemID == model.AkademikDonemID);
            if (model.OgrenimTipKod.HasValue) q = q.Where(p => p.OgrenimTipKod == model.OgrenimTipKod);
            if (!model.ProgramKod.IsNullOrWhiteSpace()) q = q.Where(p => p.ProgramKod == model.ProgramKod);

            if (model.KayitSilmeDurumID.HasValue)
            {
                if (model.KayitSilmeDurumID == KsFilterDurumEnums.HarcBirimiOnayBekleniyor) q = q.Where(p => p.KayitSilmeDurumID == KayitSilmeDurumEnums.HarcBirimiOnaySureci && !p.IsHarcBirimiOnayladi.HasValue);
                else if (model.KayitSilmeDurumID == KsFilterDurumEnums.HarcBirimiTarafindanOnaylandi) q = q.Where(p => p.KayitSilmeDurumID == KayitSilmeDurumEnums.HarcBirimiOnaySureci && p.IsHarcBirimiOnayladi == true);
                else if (model.KayitSilmeDurumID == KsFilterDurumEnums.HarcBirimiTarafindanReddedildi) q = q.Where(p => p.KayitSilmeDurumID == KayitSilmeDurumEnums.HarcBirimiOnaySureci && p.IsHarcBirimiOnayladi.HasValue && p.IsHarcBirimiOnayladi == false);
                else if (model.KayitSilmeDurumID == KsFilterDurumEnums.KutuphaneBirimiOnayBekleniyor) q = q.Where(p => p.KayitSilmeDurumID == KayitSilmeDurumEnums.KutuphaneBirimiOnaySureci && !p.IsKutuphaneBirimiOnayladi.HasValue);
                else if (model.KayitSilmeDurumID == KsFilterDurumEnums.KutuphaneBirimiTarafindanOnaylandi) q = q.Where(p => p.KayitSilmeDurumID == KayitSilmeDurumEnums.KutuphaneBirimiOnaySureci && p.IsKutuphaneBirimiOnayladi == true);
                else if (model.KayitSilmeDurumID == KsFilterDurumEnums.KutuphaneBirimiTarafindanReddedildi) q = q.Where(p => p.KayitSilmeDurumID == KayitSilmeDurumEnums.KutuphaneBirimiOnaySureci && p.IsKutuphaneBirimiOnayladi == false);
                else if (model.KayitSilmeDurumID == KsFilterDurumEnums.EykYaGonderimOnayiBekleniyor) q = q.Where(p => p.KayitSilmeDurumID == KayitSilmeDurumEnums.EnstituYonetimKuruluSureci && !p.EYKYaGonderildi.HasValue && p.IsKutuphaneBirimiOnayladi == true);
                else if (model.KayitSilmeDurumID == KsFilterDurumEnums.EykYaGonderimiOnaylandi) q = q.Where(p => p.KayitSilmeDurumID == KayitSilmeDurumEnums.EnstituYonetimKuruluSureci && p.EYKYaGonderildi == true && !p.EYKYaHazirlandi.HasValue);
                else if (model.KayitSilmeDurumID == KsFilterDurumEnums.EykYaGonderimiOnaylanmadi) q = q.Where(p => p.KayitSilmeDurumID == KayitSilmeDurumEnums.EnstituYonetimKuruluSureci && p.EYKYaGonderildi == false && !p.EYKYaHazirlandi.HasValue);
                else if (model.KayitSilmeDurumID == KsFilterDurumEnums.EykYaHazirlandi) q = q.Where(p => p.KayitSilmeDurumID == KayitSilmeDurumEnums.EnstituYonetimKuruluSureci && p.EYKYaHazirlandi == true && !p.EYKDaOnaylandi.HasValue);
                else if (model.KayitSilmeDurumID == KsFilterDurumEnums.EykDaOnaylandi) q = q.Where(p => p.KayitSilmeDurumID == KayitSilmeDurumEnums.EnstituYonetimKuruluSureci && p.EYKDaOnaylandi == true && p.EYKYaHazirlandi.HasValue);
                else if (model.KayitSilmeDurumID == KsFilterDurumEnums.EykDaOnaylanmadi) q = q.Where(p => p.KayitSilmeDurumID == KayitSilmeDurumEnums.EnstituYonetimKuruluSureci && p.EYKDaOnaylandi == false && p.EYKYaHazirlandi.HasValue);
            }

            if (!model.AdSoyad.IsNullOrWhiteSpace())
            {
                q = q.Where(p =>
                    p.AdSoyad.Contains(model.AdSoyad)
                    || p.OgrenciNo.StartsWith(model.AdSoyad)
                    || p.TcKimlikNo.StartsWith(model.AdSoyad)
                    || p.EYKSayisi==model.AdSoyad
                );

            }
            if (model.KayitSilmeDurumID == KsFilterDurumEnums.EykYaHazirlandi) model.SelectedKayitSilmeBasvurulariIds = q.Select(s => s.KayitSilmeBasvuruID).ToList();

            model.IsFiltered = !Equals(q, q2);
            model.RowCount = q.Count();
            #region export
            if (export && model.RowCount > 0)
            {
                var gv = new GridView();
                var data = q.Select(s => new
                {
                    s.KullaniciID,
                    s.OgrenciNo,
                    s.TcKimlikNo,
                    s.AdSoyad,
                    s.EMail,
                    s.AnabilimDaliAdi,
                    s.ProgramAdi,
                    s.DonemAdi,
                    s.KayitSilmeDurumAdi,
                    HarcBirimiOnayDurumu = s.IsHarcBirimiOnayladi.HasValue ? (s.IsHarcBirimiOnayladi == true ? "Onaylandı" : "Reddedildi") : "İşlem Bekliyor",
                    KutuphaneBirimiOnayDurumu = s.IsKutuphaneBirimiOnayladi.HasValue ? (s.IsKutuphaneBirimiOnayladi == true ? "Onaylandı" : "Reddedildi") : "İşlem Bekliyor",
                    EYKYaGonderildi = s.EYKYaGonderildi.HasValue ? s.EYKYaGonderildi.Value ? "Eyk'ya gönderildi" : "Eyk'ya gönderilmedi" : "İşlem Bekliyor",
                    EYKYaHazirlandi = s.EYKYaHazirlandi.HasValue ? "Eyk'ya hazırlandı" : "İşlem Bekliyor",
                    EYKTarihi = s.EYKDaOnaylandi.HasValue ? s.EYKTarihi : null,
                    EYKDaOnaylandi = s.EYKDaOnaylandi.HasValue ? s.EYKDaOnaylandi.Value ? "Eyk'da onaylandı" : "Eyk'ya onaylanmadı" : "İşlem Bekliyor",

                }).ToList();

                var exportData = (from s in data
                                  select new
                                  {
                                      BasvuruDonemi = s.DonemAdi,
                                      BasvuruDurumu = s.KayitSilmeDurumAdi,
                                      s.OgrenciNo,
                                      s.TcKimlikNo,
                                      s.AdSoyad,
                                      s.EMail,
                                      s.AnabilimDaliAdi,
                                      s.ProgramAdi,
                                      s.HarcBirimiOnayDurumu,
                                      s.KutuphaneBirimiOnayDurumu,
                                      s.EYKYaGonderildi,
                                      s.EYKYaHazirlandi,
                                      s.EYKDaOnaylandi,
                                      s.EYKTarihi
                                  }).ToList();

                gv.DataSource = exportData;
                gv.DataBind();
                Response.ContentType = "application/ms-excel";
                Response.ContentEncoding = System.Text.Encoding.UTF8;
                Response.BinaryWrite(System.Text.Encoding.UTF8.GetPreamble());
                var sw = new StringWriter();
                var htw = new HtmlTextWriter(sw);
                gv.RenderControl(htw);

                return File(System.Text.Encoding.UTF8.GetBytes(sw.ToString()), Response.ContentType, "Export_KayitSilmeBasvuruListesi_" + DateTime.Now.ToFormatDate() + ".xls");
            }
            #endregion
            if (model.ShowBasvuruUniqueId.HasValue)
                q = q.OrderBy(o => o.UniqueID == model.ShowBasvuruUniqueId ? 1 : 2).ThenBy(t => t.BasvuruTarihi);
            else q = !model.Sort.IsNullOrWhiteSpace() ? q.OrderBy(model.Sort) : q.OrderByDescending(o => o.BasvuruTarihi);
            model.Data = q.Skip(model.StartRowIndex).Take(model.PageSize).ToList();

            ViewBag.filteredOgrenciIds = model.IsFiltered ? q.Select(s => s.KullaniciID).ToList() : new List<int>();
          
            ViewBag.AkademikDonemID = new SelectList(KayitSilmeBus.CmbKsDonemListe(enstituKod, true), "Value", "Caption", model.AkademikDonemID);
            ViewBag.OgrenimTipKod = new SelectList(KayitSilmeBus.CmbKsOgrenimTipleri(enstituKod, true), "Value", "Caption", model.OgrenimTipKod);
            ViewBag.KayitSilmeDurumID = new SelectList(KayitSilmeBus.CmbKsDurumListe(true), "Value", "Caption", model.KayitSilmeDurumID);
            ViewBag.ProgramKod = new SelectList(KayitSilmeBus.GetCmbFilterKsProgramlar(enstituKod,model.OgrenimTipKod, true), "Value", "Caption", model.ProgramKod);
            return View(model);
        }

        [Authorize(Roles = RoleNames.KayitSilmeEykDaOnay)]
        [HttpPost]
        public ActionResult EYKDaOnay(List<int> kayitSilmeBasvuruIds, DateTime? eykTarihi, string eykSayisi)
        {
            kayitSilmeBasvuruIds = kayitSilmeBasvuruIds ?? new List<int>();
            var eykDaOnaylanacakBasvurular = _entities.KayitSilmeBasvurus.Where(p =>
                kayitSilmeBasvuruIds.Contains(p.KayitSilmeBasvuruID)
                && p.EYKYaHazirlandi == true && !p.EYKDaOnaylandi.HasValue
            ).ToList();
            foreach (var item in eykDaOnaylanacakBasvurular)
            {
                item.EYKDaOnaylandi = true;
                item.EYKDaOnaylandiOnayTarihi = DateTime.Now;
                item.EYKTarihi = eykTarihi;
                if (!eykSayisi.IsNullOrWhiteSpace())
                {
                    item.EYKSayisi = eykSayisi;
                }
                item.EYKDaOnaylandiIslemYapanID = UserIdentity.Current.Id;
            }
            _entities.SaveChanges();
            foreach (var item in eykDaOnaylanacakBasvurular)
            {
                LogIslemleri.LogEkle("KayitSilmeBasvuru", LogCrudType.Update, item.ToJson());
                // MezuniyetBus.SendMailJuriOneriFormuEykOnay(item.MezuniyetJuriOneriFormID, true);
            }
            return new { eykDaOnaylanacakBasvurular.Count }.ToJsonResult();
        }


        public ActionResult GetTutanakRaporu()
        {
            return View();
        }
        public ActionResult GetTutanakRaporuKontrolu(DateTime? basTar, DateTime? bitTar)
        {
            var mMessage = new MmMessage
            {
                MessageType = MsgTypeEnum.Success,
                IsSuccess = true
            };
            if (!basTar.HasValue)
            {
                mMessage.IsSuccess = false;
                mMessage.Messages.Add("Başlangıç tarihini giriniz.");
                mMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "BasTar" });
            }
            else mMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Nothing, PropertyName = "BasTar" });
            if (!basTar.HasValue)
            {
                mMessage.IsSuccess = false;
                mMessage.Messages.Add("Bitiş tarihini giriniz.");
                mMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "BitTar" });
            }
            else mMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Nothing, PropertyName = "BitTar" });
            if (basTar.HasValue && bitTar.HasValue)
            {
                if (basTar > bitTar)
                {
                    mMessage.IsSuccess = false;
                    mMessage.Messages.Add("Başlangıç tarihi bitiş tarihinden büyük olamaz.");
                    mMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "BasTar" });
                    mMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "BitTar" });
                }
                else
                {
                    mMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Nothing, PropertyName = "BasTar" });
                    mMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Nothing, PropertyName = "BitTar" });
                }
            }
            if (!mMessage.IsSuccess)
            {

                mMessage.Title = "Tutanak çıktısı oluşturulamadı";
                mMessage.MessageType = MsgTypeEnum.Warning;
            }
            return mMessage.ToJsonResult();
        }
        public ActionResult GetTutanakRaporuExport(string basTar, string bitTar, bool exportWordOrExcel, int enstituOnayDurumId, string ekd)
        {
            var enstituKod = EnstituBus.GetSelectedEnstitu(ekd);
            var enstitu = _entities.Enstitulers.First(f => f.EnstituKod == enstituKod);
            var baslangicTarihi = basTar.ToDate(DateTime.Now);
            var bitisTarihi = bitTar.ToDate(DateTime.Now);

            var tutanakData = (from ksBasvuru in _entities.KayitSilmeBasvurus
                               join program in _entities.Programlars on ksBasvuru.ProgramKod equals program.ProgramKod
                               join anabilimDali in _entities.AnabilimDallaris on program.AnabilimDaliID equals anabilimDali.AnabilimDaliID
                               join ogrenimSeviyesi in _entities.OgrenimTipleris on new {ksBasvuru.OgrenimTipKod,ksBasvuru.EnstituKod}equals new { ogrenimSeviyesi.OgrenimTipKod, ogrenimSeviyesi.EnstituKod }
                               join ogrenci in _entities.Kullanicilars on ksBasvuru.KullaniciID equals ogrenci.KullaniciID
                               where ksBasvuru.EnstituKod == enstituKod &&
                                     (enstituOnayDurumId == 2 ? ksBasvuru.EYKYaHazirlandi == true &&
                                                                !ksBasvuru.EYKDaOnaylandi.HasValue &&
                                                                ksBasvuru.EYKYaHazirlandiIslemTarihi >= baslangicTarihi &&
                                                                ksBasvuru.EYKYaHazirlandiIslemTarihi <= bitisTarihi
                                         :
                                         ksBasvuru.EYKDaOnaylandi == true &&
                                         ksBasvuru.EYKYaHazirlandi == true &&
                                         ksBasvuru.EYKTarihi >= baslangicTarihi &&
                                         ksBasvuru.EYKTarihi <= bitisTarihi)
                               select new KsTutanakDto
                               {

                                   OgrenciNo = ogrenci.OgrenciNo,
                                   OgrenciAdSoyad = ogrenci.Ad + " " + ogrenci.Soyad,
                                   OgrenimSeviyesiAdi = ogrenimSeviyesi.OgrenimTipAdi,
                                   AnabilimDaliAdi = anabilimDali.AnabilimDaliAdi,
                                   ProgramAdi = program.ProgramAdi,
                                   KayitSilmeDonemAdi = ksBasvuru.OgretimYiliBaslangic + "/" + (ksBasvuru.OgretimYiliBaslangic + 1) + " " + ksBasvuru.Donemler.DonemAdi,
                                   KayitSilmeEykTarihi = ksBasvuru.EYKTarihi,

                               }).OrderBy(o => o.KayitSilmeEykTarihi).ToList();








            var report = new XtraReport();


            var rpr = new RprKsTutanak(enstitu.EnstituAd);
            rpr.DataSource = tutanakData;
            rpr.CreateDocument();
            report.Pages.AddRange(rpr.Pages);

            report.ExportOptions.Html.ExportMode = HtmlExportMode.SingleFilePageByPage;
            using (MemoryStream ms = new MemoryStream())
            {
                report.ExportToHtml(ms);
                ms.Position = 0;
                var sr = new StreamReader(ms);
                var html = sr.ReadToEnd();
                var raporAdi = $"Kayıt Silme Tutanağı - {(enstituOnayDurumId == 2 ? "EYKya Hazırlananlar" : "EYKda Onaylananlar")}";
                return File(System.Text.Encoding.UTF8.GetBytes(html), (exportWordOrExcel ? "application/vnd.ms-word" : "application/ms-excel"), raporAdi + " (" + basTar.Replace("-", ".") + "-" + bitTar.Replace("-", ".") + ")." + (exportWordOrExcel ? "doc" : "xls"));

            }
        }
    }
}