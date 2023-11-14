using BiskaUtil;
using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.MenuAndRoles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Mvc;
using System.Web.UI;
using System.Web.UI.WebControls;
using LisansUstuBasvuruSistemi.Business;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.SystemData;

namespace LisansUstuBasvuruSistemi.Controllers
{
    [System.Web.Mvc.OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    [Authorize(Roles = RoleNames.GelenTalepler)]
    public class TalepGelenTaleplerController : Controller
    {
        // GET: TalepGelenTalepler
        private readonly LisansustuBasvuruSistemiEntities _entities = new LisansustuBasvuruSistemiEntities();
        public ActionResult Index(string ekd)
        {
            var enstituKod = EnstituBus.GetSelectedEnstitu(ekd);
            var talepSurecId = Management.GetAktifTalepSurecId(enstituKod);
            return Index(new FmTalep() { PageSize = 15, TalepSurecID = talepSurecId, Expand = talepSurecId.HasValue }, ekd);
        }
        [HttpPost]
        public ActionResult Index(FmTalep model, string ekd, bool export = false)
        {

            var enstituKod = EnstituBus.GetSelectedEnstitu(ekd);
            var kulls = _entities.Kullanicilars.First(p => p.KullaniciID == UserIdentity.Current.Id);
            if (kulls.KullaniciEnstituYetkileris.All(a => a.EnstituKod != enstituKod))
            {
                enstituKod = "";
            }

            var bbModel = new IndexPageInfoDto();



            ViewBag.bModel = bbModel;

            #region data
            var q = from s in _entities.TalepGelenTaleplers
                    join ts in _entities.TalepSurecleris.Where(p => p.EnstituKod == enstituKod) on s.TalepSurecID equals ts.TalepSurecID
                    join kul in _entities.Kullanicilars on s.KullaniciID equals kul.KullaniciID
                    join tt in _entities.TalepTipleris on s.TalepTipID equals tt.TalepTipID
                    join td in _entities.TalepDurumlaris on s.TalepDurumID equals td.TalepDurumID
                    join ags in _entities.TalepArGorStatuleris on s.TalepArGorStatuID equals ags.TalepArGorStatuID into defAgs
                    from Ags in defAgs.DefaultIfEmpty()
                    join ot in _entities.OgrenimTipleris.Where(p => p.EnstituKod == enstituKod) on s.OgrenimTipKod equals ot.OgrenimTipKod into defO
                    from Ot in defO.DefaultIfEmpty()
                    join otl in _entities.OgrenimTipleris on Ot.OgrenimTipID equals otl.OgrenimTipID into defOtl
                    from Otl in defOtl.DefaultIfEmpty()
                    join prl in _entities.Programlars on s.ProgramKod equals prl.ProgramKod into defprl
                    from Prl in defprl.DefaultIfEmpty()
                    join abl in _entities.AnabilimDallaris on new { AnabilimDaliID = (Prl != null ? Prl.AnabilimDaliID : (int?)null) } equals new { AnabilimDaliID = (int?)abl.AnabilimDaliID } into defabl
                    from Abl in defabl.DefaultIfEmpty()

                    select new
                    {
                        s.TalepGelenTalepID,
                        s.TalepSurecID,
                        s.KullaniciID,
                        kul.KullaniciTipID,
                        kul.ResimAdi,
                        kul.EMail,
                        tt.IsBelgeYuklemeVar,
                        Tel = kul.CepTel,
                        kul.TcKimlikNo,
                        KayitDonemi = kul.YtuOgrencisi && kul.KayitYilBaslangic.HasValue ? kul.KayitYilBaslangic.Value + "/" + (kul.KayitYilBaslangic + 1) + " " + (kul.KayitDonemID == 1 ? "Güz" : "Bahar") : "",
                        kul.KayitTarihi,
                        s.TalepTipID,
                        tt.TalepTipAdi,
                        tt.TalepTipAciklama,
                        s.IsTaahut,
                        tt.TaahhutAciklamasi,
                        s.TalepDurumID,
                        s.TalepDurumAciklamasi,
                        td.TalepDurumAdi,
                        td.ClassName,
                        td.Color,
                        s.TalepTarihi,
                        s.AdSoyad,
                        YtuOgrencisi = s.ProgramKod != null,
                        s.OgrenciNo,
                        s.OgrenimTipID,
                        s.OgrenimTipKod,
                        s.IsTezOnerisiYapildi,
                        s.DoktoraTezOneriTarihi,
                        Otl.OgrenimTipAdi,
                        Abl.AnabilimDaliAdi,
                        Prl.ProgramAdi,
                        s.IsYtuArGor,
                        s.TalepArGorStatuID,
                        s.IsDersYukuTamamlandi,
                        s.IsHarcBorcuVar,
                        Ags.StatuAdi,
                        s.IslemTarihi,
                        s.IslemYapanID,
                        s.IslemYapanIP,
                        s.TalepGelenTalepBelgeleris
                    };
            var qQ = q;
            if (model.TalepSurecID.HasValue) q = q.Where(p => p.TalepSurecID == model.TalepSurecID);
            if (model.KullaniciTipID.HasValue) q = q.Where(p => p.KullaniciTipID == model.KullaniciTipID);
            if (model.OgrenimTipKod.HasValue) q = q.Where(p => p.OgrenimTipKod == model.OgrenimTipKod);
            if (model.TalepDurumID.HasValue) q = q.Where(p => p.TalepDurumID == model.TalepDurumID);
            if (model.TalepTipID.HasValue) q = q.Where(p => p.TalepTipID == model.TalepTipID);
            if (model.IsDersYukuTamamlandi.HasValue) q = q.Where(p => p.IsDersYukuTamamlandi == model.IsDersYukuTamamlandi);
            if (model.IsTezOnerisiYapildi.HasValue) q = q.Where(p => p.IsTezOnerisiYapildi == model.IsTezOnerisiYapildi);
            if (!model.AranacakKelime.IsNullOrWhiteSpace()) q = q.Where(p => p.OgrenciNo == model.AranacakKelime || p.TcKimlikNo == model.AranacakKelime || p.AdSoyad.Contains(model.AranacakKelime));

            if (qQ != q)
            {
                model.Expand = true;
                ViewBag.KTalepGelenTalepIDs = q.Where(p => p.TalepDurumID == TalepDurumuEnum.TalepYapildi).Select(s => s.TalepGelenTalepID).ToList();
            }
            else ViewBag.KTalepGelenTalepIDs = new List<int>(); 
            q = model.Sort.IsNullOrWhiteSpace() == false ? q.OrderBy(model.Sort) : q.OrderByDescending(o => o.TalepTarihi); 
            model.RowCount = q.Count(); 
            var indexModel = new MIndexBilgi();
            var btDurulari = TaleplerBus.GetTalepDurumList();
            foreach (var item in btDurulari)
            {
                var tipCount = q.Count(p => p.TalepDurumID == item.TalepDurumID);
                indexModel.ListB.Add(new mxRowModel { Key = item.TalepDurumAdi, ClassName = item.ClassName, Color = item.Color, Toplam = tipCount });
            }
            indexModel.Toplam = model.RowCount;
            model.Data = q.Skip(model.StartRowIndex).Take(model.PageSize).ToList().Select(item => new FrTalep()
            {
                TalepGelenTalepID = item.TalepGelenTalepID,
                IsbelgeYuklemesiVar = item.IsBelgeYuklemeVar,
                YtuOgrencisi = item.YtuOgrencisi,
                TalepDurumID = item.TalepDurumID,
                TalepDurumAciklamasi = item.TalepDurumAciklamasi,
                TalepSurecID = item.TalepSurecID,
                ClassName = item.ClassName,
                Color = item.Color,
                TalepTipID = item.TalepTipID,
                TalepTipAdi = item.TalepTipAdi,
                TalepTipAciklama = item.TalepTipID == TalepTipiEnum.LisansustuSureUzatmaTalebi ?
                                (item.OgrenimTipKod.IsDoktora() ?
                                    "Bu talep tipini seçecek öğrenciler, doktora tez önerisinden başarılı olmuş ve dönem harç ücreti varsa ödemiş olmaları gerekmektedir. Aksi takdirde talepleri kabul edilmeyecektir. "
                                    :
                                    "Bu talep tipini seçecek öğrenciler Senato Esaslarında belirtilen ders yükü tamamlama kurallarına göre ders aşamasını tamamlamış ve dönem harç ücreti varsa ödemiş olmaları gerekmektedir. Aksi takdirde talepleri kabul edilmeyecektir.")
                                 :
                                 item.TalepTipID == TalepTipiEnum.Covid19KayitDondurmaTalebi ?
                                 (item.OgrenimTipKod.IsDoktora() ?
                                    "Bu talep tipini seçecek olan öğrencilerimizden: doktora tez önerisinden başarılı olunmuş ise; COVID-19 sebebi ile kayıt dondurma işleminizin uygun olduğuna dair danışmanınıza ait imzalı dilekçenin yüklenmesi gerekmektedir. Aksi takdirde talebiniz kabul edilmeyecektir."
                                    :
                                    "Bu talep tipini seçecek olan öğrencilerimizden: YTÜ Lisansüstü Eğitim ve Öğretim Yönetmeliği Senato Esaslarında belirtilen ders yükü tamamlama kurallarına göre ders aşaması tamamlanmış ise; COVID-19 sebebi ile kayıt dondurma işleminizin uygun olduğuna dair danışmanınıza ait imzalı dilekçenin yüklenmesi gerekmektedir Aksi takdirde talebiniz kabul edilmeyecektir."
                                    ) : item.TalepTipAciklama,
                TaahhutAciklama = item.TaahhutAciklamasi,
                IsTaahut = item.IsTaahut,
                DurumListeAdi = item.TalepDurumAdi,
                KullaniciID = item.KullaniciID,
                ResimAdi = item.ResimAdi,
                TalepTarihi = item.TalepTarihi,
                AdSoyad = item.AdSoyad,
                OgrenciNo = item.OgrenciNo,
                OgrenimTipKod = item.OgrenimTipKod,
                OgrenimTipAdi = item.OgrenimTipAdi,
                IsTezOnerisiYapildi = item.IsTezOnerisiYapildi,
                DoktoraTezOneriTarihi = item.DoktoraTezOneriTarihi,
                AnabiliDaliAdi = item.AnabilimDaliAdi,
                ProgramAdi = item.ProgramAdi,
                IsYtuArGor = item.IsYtuArGor ?? false,
                TalepArGorStatuID = item.TalepArGorStatuID,
                StatuAdi = item.StatuAdi,
                IsDersYukuTamamlandi = item.IsDersYukuTamamlandi ?? false,
                IsHarcBorcuVar = item.IsHarcBorcuVar ?? false,
                IslemTarihi = item.IslemTarihi,
                IslemYapanID = item.IslemYapanID,
                IslemYapanIP = item.IslemYapanIP,
                TalepGelenTalepBelgeleris = item.TalepGelenTalepBelgeleris

            }).ToList();


            #endregion

            if (export && model.RowCount > 0)
            {
                GridView gv = new GridView();
                var qExp = q.ToList();

                gv.DataSource = qExp.Select(s => new
                {
                    s.TalepTipAdi,
                    s.TalepDurumAdi,
                    s.TalepTarihi,
                    s.OgrenciNo,
                    s.AdSoyad,
                    s.EMail,
                    s.Tel,
                    s.OgrenimTipAdi,
                    s.AnabilimDaliAdi,
                    s.ProgramAdi,
                    s.YtuOgrencisi,
                    s.KayitDonemi,
                    s.KayitTarihi,
                    TezOnerisiYapildiMi = s.IsTezOnerisiYapildi.HasValue ? (s.IsTezOnerisiYapildi.Value ? "Tez Önerisi Yapıldı" : "Tez Önerisi Yapılmadı") : "",
                    TezOneriTarihi = s.DoktoraTezOneriTarihi,
                    ArGorYTU = s.IsYtuArGor == true ? "YTÜ'de Ar. Gör." : "-",
                    ArGorStatuAdi = s.TalepArGorStatuID.HasValue ? s.StatuAdi : "",
                    DersYukuTamamlandiMi = (!s.OgrenimTipKod.HasValue || s.OgrenimTipKod.IsDoktora() ? "" : (s.IsDersYukuTamamlandi == true ? "Evet" : "Hayır")),
                    HarcBorcuVarMi = s.IsHarcBorcuVar.HasValue ? (s.IsHarcBorcuVar == true ? "Var" : "Yok") : "",
                }).ToList();
                gv.DataBind();

                Response.ContentType = "application/ms-excel";
                Response.ContentEncoding = System.Text.Encoding.UTF8;
                Response.BinaryWrite(System.Text.Encoding.UTF8.GetPreamble());
                StringWriter sw = new StringWriter();
                HtmlTextWriter htw = new HtmlTextWriter(sw);
                gv.RenderControl(htw);

                return File(System.Text.Encoding.UTF8.GetBytes(sw.ToString()), Response.ContentType, "Export_TalepListesi_" + DateTime.Now.ToFormatDate() + ".xls");
            }
            ViewBag.TalepSurecID = new SelectList(TaleplerBus.GetCmbTalepSurecleri(enstituKod, true), "Value", "Caption", model.TalepSurecID);
            ViewBag.KullaniciTipID = new SelectList(KullanicilarBus.GetCmbKullaniciTipleri(true), "Value", "Caption", model.KullaniciTipID);
            ViewBag.OgrenimTipKod = new SelectList(OgrenimTipleriBus.CmbAktifOgrenimTipleri(enstituKod, true, true), "Value", "Caption", model.OgrenimTipKod);
            ViewBag.TalepDurumID = new SelectList(TaleplerBus.GetCmbTalepDurumlari(true), "Value", "Caption", model.TalepDurumID);
            ViewBag.TalepTipID = new SelectList(TaleplerBus.GetCmbTalepTipleri(true), "Value", "Caption", model.TalepTipID);
            ViewBag.KTalepDurumID = new SelectList(TaleplerBus.GetCmbTalepDurumlari(false), "Value", "Caption");
            ViewBag.IsDersYukuTamamlandi = new SelectList(ComboData.GetCmbEvetHayirData(true), "Value", "Caption", model.IsDersYukuTamamlandi);
            ViewBag.IsTezOnerisiYapildi = new SelectList(ComboData.GetCmbEvetHayirData(true), "Value", "Caption", model.IsTezOnerisiYapildi);
            ViewBag.IndexModel = indexModel;
            return View(model);
        }



    }
}