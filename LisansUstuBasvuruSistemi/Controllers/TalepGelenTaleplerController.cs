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

namespace LisansUstuBasvuruSistemi.Controllers
{
    [System.Web.Mvc.OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    [Authorize(Roles = RoleNames.GelenTalepler)]
    public class TalepGelenTaleplerController : Controller
    {
        // GET: TalepGelenTalepler
        private LisansustuBasvuruSistemiEntities db = new LisansustuBasvuruSistemiEntities();
        public ActionResult Index(string EKD)
        {
            var _EnstituKod = EnstituBus.GetSelectedEnstitu(EKD);
            var TalepSurecID = Management.getAktifTalepSurecID(_EnstituKod);
            return Index(new fmTalep() { PageSize = 15, TalepSurecID = TalepSurecID, Expand = TalepSurecID.HasValue }, EKD);
        }
        [HttpPost]
        public ActionResult Index(fmTalep model, string EKD, bool export = false)
        {
            
            var _EnstituKod = EnstituBus.GetSelectedEnstitu(EKD);
            var kulls = db.Kullanicilars.Where(p => p.KullaniciID == UserIdentity.Current.Id).First();
            if (!kulls.KullaniciEnstituYetkileris.Any(a => a.EnstituKod == _EnstituKod))
            {
                _EnstituKod = "";
            }

            var bbModel = new IndexPageInfoDto();


            
            ViewBag.bModel = bbModel;

            #region data
            var q = from s in db.TalepGelenTaleplers
                    join ts in db.TalepSurecleris.Where(p => p.EnstituKod == _EnstituKod) on s.TalepSurecID equals ts.TalepSurecID
                    join kul in db.Kullanicilars on s.KullaniciID equals kul.KullaniciID
                    join tt in db.TalepTipleris on s.TalepTipID equals tt.TalepTipID
                    join td in db.TalepDurumlaris on s.TalepDurumID equals td.TalepDurumID
                    join ags in db.TalepArGorStatuleris on s.TalepArGorStatuID equals ags.TalepArGorStatuID into defAgs
                    from Ags in defAgs.DefaultIfEmpty()
                    join ot in db.OgrenimTipleris.Where(p => p.EnstituKod == _EnstituKod) on s.OgrenimTipKod equals ot.OgrenimTipKod into defO
                    from Ot in defO.DefaultIfEmpty()
                    join otl in db.OgrenimTipleris on Ot.OgrenimTipID equals otl.OgrenimTipID into defOtl
                    from Otl in defOtl.DefaultIfEmpty()
                    join prl in db.Programlars on s.ProgramKod equals prl.ProgramKod into defprl
                    from Prl in defprl.DefaultIfEmpty()
                    join abl in db.AnabilimDallaris on new { AnabilimDaliID = (Prl != null ? Prl.AnabilimDaliID : (int?)null) } equals new { AnabilimDaliID = (int?)abl.AnabilimDaliID } into defabl
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
                        TcOrPasaportNo = kul.KullaniciTipleri.Yerli ? kul.TcKimlikNo : kul.PasaportNo,
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
                        YtuOgrencisi = s.ProgramKod == null ? false : true,
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
            if (!model.AranacakKelime.IsNullOrWhiteSpace()) q = q.Where(p => p.OgrenciNo == model.AranacakKelime || p.TcOrPasaportNo == model.AranacakKelime || p.AdSoyad.Contains(model.AranacakKelime));

            if (qQ != q)
            {
                model.Expand = true;
                ViewBag.KTalepGelenTalepIDs = q.Where(p => p.TalepDurumID == TalepDurumu.TalepYapildi).Select(s => s.TalepGelenTalepID).ToList();
            }
            else ViewBag.KTalepGelenTalepIDs = new List<int>();

            if (model.Sort.IsNullOrWhiteSpace() == false) q = q.OrderBy(model.Sort);
            else q = q.OrderByDescending(o => o.TalepTarihi);


            model.RowCount = q.Count();
            var PS = Management.setStartRowInx(model.StartRowIndex, model.PageIndex, model.PageCount, model.RowCount, model.PageSize);
            model.PageIndex = PS.PageIndex;

            var IndexModel = new MIndexBilgi();
            var btDurulari = Management.TalepDurumList();
            foreach (var item in btDurulari)
            {
                var tipCount = q.Where(p => p.TalepDurumID == item.TalepDurumID).Count();
                IndexModel.ListB.Add(new mxRowModel { Key = item.TalepDurumAdi, ClassName = item.ClassName, Color = item.Color, Toplam = tipCount });
            }
            IndexModel.Toplam = model.RowCount;
            model.Data = q.Skip(PS.StartRowIndex).Take(model.PageSize).Select(item => new frTalep()
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
                TalepTipAciklama = item.TalepTipID == TalepTipi.LisansustuSureUzatmaTalebi ?
                                (item.OgrenimTipKod == OgrenimTipi.Doktra ?
                                    "Bu talep tipini seçecek öğrenciler, doktora tez önerisinden başarılı olmuş ve dönem harç ücreti varsa ödemiş olmaları gerekmektedir. Aksi takdirde talepleri kabul edilmeyecektir. "
                                    :
                                    "Bu talep tipini seçecek öğrenciler Senato Esaslarında belirtilen ders yükü tamamlama kurallarına göre ders aşamasını tamamlamış ve dönem harç ücreti varsa ödemiş olmaları gerekmektedir. Aksi takdirde talepleri kabul edilmeyecektir.")
                                 :
                                 item.TalepTipID == TalepTipi.Covid19KayitDondurmaTalebi ?
                                 (item.OgrenimTipKod == OgrenimTipi.Doktra ?
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
                var qExp = q.AsQueryable();

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
                    TezOnerisiYapildiMi = s.IsTezOnerisiYapildi.HasValue ? (s.IsTezOnerisiYapildi.Value ? "Tez Önerisi Yapıldı" : "Tez Önerisi Yapılmadı") : "",
                    TezOneriTarihi = s.DoktoraTezOneriTarihi,
                    ArGorYTU = s.IsYtuArGor == true ? "YTU'de Ar. Gör." : "-",
                    ArGorStatuAdi = s.TalepArGorStatuID.HasValue ? s.StatuAdi : "",
                    DersYukuTamamlandiMi = (!s.OgrenimTipKod.HasValue || s.OgrenimTipKod == OgrenimTipi.Doktra ? "" : (s.IsDersYukuTamamlandi == true ? "Evet" : "Hayır")),
                    HarcBorcuVarMi = s.IsHarcBorcuVar.HasValue ? (s.IsHarcBorcuVar == true ? "Var" : "Yok") : "",
                }).ToList();
                gv.DataBind();

                Response.ContentType = "application/ms-excel";
                Response.ContentEncoding = System.Text.Encoding.UTF8;
                Response.BinaryWrite(System.Text.Encoding.UTF8.GetPreamble());
                StringWriter sw = new StringWriter();
                HtmlTextWriter htw = new HtmlTextWriter(sw);
                gv.RenderControl(htw);

                return File(System.Text.Encoding.UTF8.GetBytes(sw.ToString()), Response.ContentType, "Export_TalepListesi_" + DateTime.Now.ToString("dd.MM.yyyy") + ".xls");
            }
            ViewBag.TalepSurecID = new SelectList(Management.getTalepSurecleri(_EnstituKod ,true), "Value", "Caption", model.TalepSurecID);
            ViewBag.KullaniciTipID = new SelectList(KullanicilarBus.GetCmbKullaniciTipleri(true), "Value", "Caption", model.KullaniciTipID);
            ViewBag.OgrenimTipKod = new SelectList(Management.cmbAktifOgrenimTipleri(_EnstituKod ,true, true), "Value", "Caption", model.OgrenimTipKod);
            ViewBag.TalepDurumID = new SelectList(Management.cmbTalepDurumlari( true), "Value", "Caption", model.TalepDurumID);
            ViewBag.TalepTipID = new SelectList(Management.cmbTalepTipleri( true), "Value", "Caption", model.TalepTipID);
            ViewBag.KTalepDurumID = new SelectList(Management.cmbTalepDurumlari(false), "Value", "Caption");
            ViewBag.IsDersYukuTamamlandi = new SelectList(Management.cmbEvetHayirData(true), "Value", "Caption", model.IsDersYukuTamamlandi);
            ViewBag.IsTezOnerisiYapildi = new SelectList(Management.cmbEvetHayirData(true), "Value", "Caption", model.IsTezOnerisiYapildi);
            ViewBag.IndexModel = IndexModel;
            return View(model);
        }



    }
}