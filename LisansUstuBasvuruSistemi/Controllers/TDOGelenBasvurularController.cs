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
    [Authorize(Roles = RoleNames.TDOGelenBasvuru)]
    [System.Web.Mvc.OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    public class TDOGelenBasvurularController : Controller
    {
        // GET: TDOGelenBasvurular
        private LisansustuBasvuruSistemiEntities db = new LisansustuBasvuruSistemiEntities();
        public ActionResult Index(string EKD, int? TDOBasvuruID, int? KullaniciID)
        {

            return Index(new fmTDOBasvuru() { TDOBasvuruID = TDOBasvuruID, KullaniciID = KullaniciID, PageSize = 50 }, EKD);
        }
        [HttpPost]
        public ActionResult Index(fmTDOBasvuru model, string EKD, bool export = false)
        {

            var enstituKod = Management.getSelectedEnstitu(EKD);
            var nowDate = DateTime.Now;
            var tdoDanismanOnayYetkisi = RoleNames.TDODanismanOnayYetkisi.InRoleCurrent();
            var tdoGelenBasvuruKayit = RoleNames.TDOGelenBasvuruKayit.InRoleCurrent();

            var q = from s in db.TDOBasvurus
                    join e in db.Enstitulers on s.EnstituKod equals e.EnstituKod
                    join k in db.Kullanicilars on s.KullaniciID equals k.KullaniciID
                    join o in db.OgrenimTipleris on new { s.OgrenimTipKod, e.EnstituKod } equals new { o.OgrenimTipKod, o.EnstituKod }
                    join pr in db.Programlars on s.ProgramKod equals pr.ProgramKod
                    join ab in db.AnabilimDallaris on pr.AnabilimDaliKod equals ab.AnabilimDaliKod
                    join en in db.Enstitulers on e.EnstituKod equals en.EnstituKod
                    join ktip in db.KullaniciTipleris on k.KullaniciTipID equals ktip.KullaniciTipID
                    join ard in db.TDOBasvuruDanismen on s.AktifTDOBasvuruDanismanID equals ard.TDOBasvuruDanismanID into defard
                    from Ard in defard.DefaultIfEmpty()
                    let ardEs = db.TDOBasvuruEsDanismen.FirstOrDefault(p => p.TDOBasvuruDanismanID == Ard.TDOBasvuruDanismanID)
                    select new frTDOBasvuru
                    {
                        TezDanismanID = Ard.TezDanismanID,
                        TDOBasvuruID = s.TDOBasvuruID,
                        BasvuruTarihi = s.BasvuruTarihi,
                        EnstituKod = en.EnstituKod,
                        EnstituAdi = en.EnstituAd,
                        OgrenimTipAdi = o.OgrenimTipAdi,
                        AnabilimdaliAdi = ab.AnabilimDaliAdi,
                        ProgramAdi = pr.ProgramAdi,
                        KullaniciID = s.KullaniciID,
                        AdSoyad = k.Ad + " " + k.Soyad,
                        EMail = k.EMail,
                        CepTel = k.CepTel,
                        TcPasaPortNo = ktip.Yerli ? k.TcKimlikNo : k.PasaportNo,
                        OgrenciNo = s.OgrenciNo,
                        Kullanicilar = s.Kullanicilar,
                        ResimAdi = k.ResimAdi,
                        KullaniciTipID = k.KullaniciTipID,
                        KullaniciTipAdi = ktip.KullaniciTipAdi,
                        OgrenimTipKod = s.OgrenimTipKod,
                        KayitOgretimYiliBaslangic = s.KayitOgretimYiliBaslangic,
                        KayitOgretimYiliDonemID = s.KayitOgretimYiliDonemID,
                        AktifTDOBasvuruDanismanID = s.AktifTDOBasvuruDanismanID,
                        AktifDonemID = Ard == null ? null : (Ard.DonemBaslangicYil + "" + Ard.DonemID),
                        AktifDonemAdi = Ard == null ? "Danışman Önerisi Yok" : (Ard.DonemBaslangicYil + " / " + (Ard.DonemBaslangicYil + 1) + " " + (Ard.DonemID == 1 ? "Güz" : "Bahar")),
                        EYKYaGonderildiIslemTarihi = Ard == null ? null : Ard.EYKYaGonderildiIslemTarihi,
                        EYKYaGonderildiIslemTarihiES = ardEs == null ? null : ardEs.EYKYaGonderildiIslemTarihi,
                        TDOBasvuruDanisman = Ard, 
                        VarolanTezDanismanID = Ard != null ? Ard.VarolanTezDanismanID : null,
                        VarolanDanismanOnayladi = Ard != null ? Ard.VarolanDanismanOnayladi : null,
                        DanismanOnayladi = Ard != null ? Ard.DanismanOnayladi : null,
                        EYKYaGonderildi = Ard != null ? Ard.EYKYaGonderildi : null,

                        EYKDaOnaylandi = Ard != null ? Ard.EYKDaOnaylandi : null,
                        EsDanismanOnerisiVar = ardEs != null,
                        Es_EYKYaGonderildi = ardEs != null ? ardEs.EYKYaGonderildi : null,
                        Es_EYKDaOnaylandi = ardEs != null ? ardEs.EYKDaOnaylandi : null,
                        RowDate = (ardEs.EYKYaGonderildi == true && !ardEs.EYKDaOnaylandi.HasValue ? ardEs.EYKYaGonderildiIslemTarihi.Value : (Ard.EYKYaGonderildi == true && !Ard.EYKDaOnaylandi.HasValue ? Ard.EYKYaGonderildiIslemTarihi.Value : (Ard!=null?Ard.BasvuruTarihi:DateTime.MinValue))),
                        Sira = (Ard != null && (Ard.EYKYaGonderildi == true && Ard.EYKDaOnaylandi == null) || (ardEs != null && ardEs.EYKYaGonderildi == null)) ? 0 : 1,
                        TDODanismanDetayModels = (from x in s.TDOBasvuruDanismen
                                                  join xd in db.TDOBasvuruEsDanismen on x.TDOBasvuruDanismanID equals xd.TDOBasvuruDanismanID into defX
                                                  from xD in defX.DefaultIfEmpty()
                                                  select new TDODanismanFiltreModel
                                                  {
                                                      FormKodu = x.FormKodu,
                                                      RaporDonemID = x.DonemBaslangicYil + "" + x.DonemID,
                                                      DanismanAdSoyad = x.TDAdSoyad,
                                                      Es_DanismanAdSoyad = xD != null ? xD.AdSoyad : "",
                                                      TDODanismanTalepTipID = x.TDODanismanTalepTipID,
                                                      VarolanDanismanOnayladi = x.VarolanDanismanOnayladi,
                                                      DanismanOnayladi = x.DanismanOnayladi,
                                                      EYKYaGonderildi = x.EYKYaGonderildi,
                                                      EYKDaOnaylandi = x.EYKDaOnaylandi,
                                                      Es_EYKYaGonderildi = xD != null ? xD.EYKYaGonderildi : null,
                                                      Es_EYKDaOnaylandi = xD != null ? xD.EYKDaOnaylandi : null,
                                                      Es_FormKodu = xD != null ? xD.FormKodu : null
                                                  }).ToList(), 
                    };
            var q2 = q;
            if (tdoDanismanOnayYetkisi && !tdoGelenBasvuruKayit)
            {
                q = q.Where(p => p.TezDanismanID == UserIdentity.Current.Id || p.VarolanTezDanismanID == UserIdentity.Current.Id);
            }
            q = q.Where(p => p.EnstituKod == enstituKod);
            if (!model.AktifDonemID.IsNullOrWhiteSpace()) q = q.Where(p => p.AktifDonemID == model.AktifDonemID);
            if (model.AktifDurumID.HasValue)
            {
                if (model.AktifDurumID == TDODansimanDurumu.DanismanOnayiBekliyor) q = q.Where(p => !p.DanismanOnayladi.HasValue);
                else if (model.AktifDurumID == TDODansimanDurumu.DanismanTarafindanOnaylandi) q = q.Where(p => p.DanismanOnayladi == true && !p.EYKYaGonderildi.HasValue);
                else if (model.AktifDurumID == TDODansimanDurumu.DanismanTarafindanOnaylanmadi) q = q.Where(p => p.DanismanOnayladi == false && !p.EYKYaGonderildi.HasValue);
                else if (model.AktifDurumID == TDODansimanDurumu.EYKYaGonderimOnayiBekleniyor) q = q.Where(p => p.DanismanOnayladi == true && !p.EYKYaGonderildi.HasValue);
                else if (model.AktifDurumID == TDODansimanDurumu.EYKYaGonderimiOnaylandi) q = q.Where(p => p.EYKYaGonderildi == true && !p.EYKDaOnaylandi.HasValue);
                else if (model.AktifDurumID == TDODansimanDurumu.EYKYaGonderimiOnaylanmadi) q = q.Where(p => p.EYKYaGonderildi == false&& !p.EYKDaOnaylandi.HasValue);
                else if (model.AktifDurumID == TDODansimanDurumu.EYKDaOnayBekleniyor) q = q.Where(p => p.EYKYaGonderildi == true && !p.EYKDaOnaylandi.HasValue);
                else if (model.AktifDurumID == TDODansimanDurumu.EYKDaOnaylandi) q = q.Where(p => p.EYKDaOnaylandi == true);
                else if (model.AktifDurumID == TDODansimanDurumu.EYKDaOnaylanmadi) q = q.Where(p => p.EYKDaOnaylandi == false);
            }
            if (model.AktifEsDurumID.HasValue)
            {
                if (model.AktifEsDurumID == TDODansimanDurumu.EYKYaGonderimOnayiBekleniyor) q = q.Where(p => p.EsDanismanOnerisiVar && !p.Es_EYKYaGonderildi.HasValue);
                else if (model.AktifEsDurumID == TDODansimanDurumu.EYKYaGonderimiOnaylandi) q = q.Where(p => p.Es_EYKYaGonderildi == true);
                else if (model.AktifEsDurumID == TDODansimanDurumu.EYKYaGonderimiOnaylanmadi) q = q.Where(p => p.Es_EYKYaGonderildi == false);
                else if (model.AktifEsDurumID == TDODansimanDurumu.EYKDaOnayBekleniyor) q = q.Where(p => p.Es_EYKYaGonderildi == true && !p.Es_EYKDaOnaylandi.HasValue);
                else if (model.AktifEsDurumID == TDODansimanDurumu.EYKDaOnaylandi) q = q.Where(p => p.Es_EYKDaOnaylandi == true);
                else if (model.AktifEsDurumID == TDODansimanDurumu.EYKDaOnaylanmadi) q = q.Where(p => p.Es_EYKDaOnaylandi == false);
            }
            if (model.TDOBasvuruID.HasValue) q = q.Where(p => p.TDOBasvuruID == model.TDOBasvuruID);
            if (!model.DonemID.IsNullOrWhiteSpace()) q = q.Where(p => p.TDODanismanDetayModels.Any(a => a.RaporDonemID == model.DonemID));
            if (model.DurumID.HasValue)
            {
                if (model.DurumID == TDODansimanDurumu.DanismanOnayiBekliyor) q = q.Where(p => p.TDODanismanDetayModels.Any(p2 => !p2.DanismanOnayladi.HasValue));
                else if (model.DurumID == TDODansimanDurumu.DanismanTarafindanOnaylandi) q = q.Where(p => p.TDODanismanDetayModels.Any(p2 => p2.DanismanOnayladi == true));
                else if (model.DurumID == TDODansimanDurumu.DanismanTarafindanOnaylanmadi) q = q.Where(p => p.TDODanismanDetayModels.Any(p2 => p2.DanismanOnayladi == false));
                else if (model.DurumID == TDODansimanDurumu.EYKYaGonderimOnayiBekleniyor) q = q.Where(p => p.TDODanismanDetayModels.Any(p2 => p2.DanismanOnayladi == true && !p2.EYKYaGonderildi.HasValue));
                else if (model.DurumID == TDODansimanDurumu.EYKYaGonderimiOnaylandi) q = q.Where(p => p.TDODanismanDetayModels.Any(p2 => p2.EYKYaGonderildi == true));
                else if (model.DurumID == TDODansimanDurumu.EYKYaGonderimiOnaylanmadi) q = q.Where(p => p.TDODanismanDetayModels.Any(p2 => p2.EYKYaGonderildi == false));
                else if (model.DurumID == TDODansimanDurumu.EYKDaOnayBekleniyor) q = q.Where(p => p.TDODanismanDetayModels.Any(p2 => p2.EYKYaGonderildi == true && !p2.EYKDaOnaylandi.HasValue));
                else if (model.DurumID == TDODansimanDurumu.EYKDaOnaylandi) q = q.Where(p => p.TDODanismanDetayModels.Any(p2 => p2.EYKDaOnaylandi == true));
                else if (model.DurumID == TDODansimanDurumu.EYKDaOnaylanmadi) q = q.Where(p => p.TDODanismanDetayModels.Any(p2 => p2.EYKDaOnaylandi == false));
            }
            if (model.EsDurumID.HasValue)
            {
                if (model.EsDurumID == TDODansimanDurumu.EYKYaGonderimOnayiBekleniyor) q = q.Where(p => p.TDODanismanDetayModels.Any(p2 => !p2.Es_EYKYaGonderildi.HasValue));
                else if (model.EsDurumID == TDODansimanDurumu.EYKYaGonderimiOnaylandi) q = q.Where(p => p.TDODanismanDetayModels.Any(p2 => p2.Es_EYKYaGonderildi == true));
                else if (model.EsDurumID == TDODansimanDurumu.EYKYaGonderimiOnaylanmadi) q = q.Where(p => p.TDODanismanDetayModels.Any(p2 => p2.Es_EYKYaGonderildi == false));
                else if (model.EsDurumID == TDODansimanDurumu.EYKDaOnayBekleniyor) q = q.Where(p => p.TDODanismanDetayModels.Any(p2 => p2.Es_EYKYaGonderildi == true && !p2.Es_EYKDaOnaylandi.HasValue));
                else if (model.EsDurumID == TDODansimanDurumu.EYKDaOnaylandi) q = q.Where(p => p.TDODanismanDetayModels.Any(p2 => p2.Es_EYKDaOnaylandi == true));
                else if (model.EsDurumID == TDODansimanDurumu.EYKDaOnaylanmadi) q = q.Where(p => p.TDODanismanDetayModels.Any(p2 => p2.Es_EYKDaOnaylandi == false));
            }

            if (!model.AdSoyad.IsNullOrWhiteSpace()) q = q.Where(p => p.AdSoyad.Contains(model.AdSoyad) || p.OgrenciNo == model.AdSoyad || p.TcPasaPortNo == model.AdSoyad || p.TDODanismanDetayModels.Any(a => a.FormKodu == model.AdSoyad || a.Es_FormKodu == model.AdSoyad || a.DanismanAdSoyad.Contains(model.AdSoyad)));

            var isFiltered = q != q2;

            model.RowCount = q.Count(); 
            if (!model.Sort.IsNullOrWhiteSpace()) q = q.OrderBy(model.Sort);
            else if (model.AktifDurumID == 5 || model.DurumID == 5)
                q = q.OrderBy(o => o.EYKYaGonderildiIslemTarihi);
            else if (model.AktifEsDurumID == 5 || model.EsDurumID == 5)
                q = q.OrderBy(o => o.EYKYaGonderildiIslemTarihiES);
            else q = q.OrderBy(o => o.Sira).ThenByDescending(o => o.RowDate); 

            var ps = Management.setStartRowInx(model.StartRowIndex, model.PageIndex, model.PageCount, model.RowCount, model.PageSize);
            model.PageIndex = ps.PageIndex;
            var qdata = q.Skip(ps.StartRowIndex).Take(model.PageSize).ToList();

            #region export
            if (export && model.RowCount > 0)
            {
                var gv = new GridView();
                var qExp = q.ToList();
                gv.DataSource = qExp.Select(s => new
                {
                    s.AktifDonemAdi,
                    s.OgrenimTipAdi,
                    s.AnabilimdaliAdi,
                    s.ProgramAdi,
                    s.AdSoyad,
                    s.TcPasaPortNo,
                    s.OgrenciNo,
                    s.EMail,
                    s.CepTel,
                    DanismanAdSoyad = s.TDOBasvuruDanisman != null ? (s.TDOBasvuruDanisman.TDUnvanAdi + " " + s.TDOBasvuruDanisman.TDAdSoyad) : "Danışman Yok",
                    DanismanOnayladi = s.TDOBasvuruDanisman != null ? (s.DanismanOnayladi.HasValue ? (s.DanismanOnayladi.Value ? "Danışman Onayladı" : "Danışman Onaylamadı") : "Danışman Onayı Bekleniyor") : "Danışman Yok",
                    EYKYaGonderildi = s.TDOBasvuruDanisman != null ? (s.EYKYaGonderildi.HasValue ? (s.EYKYaGonderildi.Value ? "EYK'ya Gönderimi Onaylandı" : "EYK'ya Gönderimi Onaylanmadı") : "EYK'ya Gönderim Onayı Bekleniyor") : "Danışman Yok",
                    EYKDaOnaylandi = s.TDOBasvuruDanisman != null ? (s.EYKYaGonderildi.HasValue ? (s.EYKYaGonderildi.Value ? "EYK'ya Gönderimi Onaylandı" : "EYK'ya Gönderimi Onaylanmadı") : "EYK'ya Gönderim Onayı Bekleniyor") : "Danışman Yok",
                    EsDanismanAdSoyad = s.TDOBasvuruDanisman != null && s.TDOBasvuruDanisman.TDOBasvuruEsDanismen.Any() ? (s.TDOBasvuruDanisman.TDUnvanAdi + " " + s.TDOBasvuruDanisman.TDAdSoyad) : "Danışman Yok",
                    EsDanismanOnayladi = s.TDOBasvuruDanisman != null && s.TDOBasvuruDanisman.TDOBasvuruEsDanismen.Any() ? (s.DanismanOnayladi.HasValue ? (s.DanismanOnayladi.Value ? "Danışman Onayladı" : "Danışman Onaylamadı") : "Danışman Onayı Bekleniyor") : "Danışman Yok",
                    EsDanismanEYKYaGonderildi = s.TDOBasvuruDanisman != null && s.TDOBasvuruDanisman.TDOBasvuruEsDanismen.Any() ? (s.TDOBasvuruDanisman.TDOBasvuruEsDanismen.FirstOrDefault().EYKYaGonderildi.HasValue ? (s.TDOBasvuruDanisman.TDOBasvuruEsDanismen.FirstOrDefault().EYKYaGonderildi.Value ? "EYK'ya Gönderimi Onaylandı" : "EYK'ya Gönderimi Onaylanmadı") : "EYK'ya Gönderim Onayı Bekleniyor") : "Eş Danışman Yok",
                    EsDanismanEYKDaOnaylandi = s.TDOBasvuruDanisman != null && s.TDOBasvuruDanisman.TDOBasvuruEsDanismen.Any() ? (s.TDOBasvuruDanisman.TDOBasvuruEsDanismen.FirstOrDefault().EYKYaGonderildi.HasValue ? (s.TDOBasvuruDanisman.TDOBasvuruEsDanismen.FirstOrDefault().EYKYaGonderildi.Value ? "EYK'ya Gönderimi Onaylandı" : "EYK'ya Gönderimi Onaylanmadı") : "EYK'ya Gönderim Onayı Bekleniyor") : "Eş Danışman Yok",

                }).ToList();
                gv.DataBind();
                Response.ContentType = "application/ms-excel";
                Response.ContentEncoding = System.Text.Encoding.UTF8;
                Response.BinaryWrite(System.Text.Encoding.UTF8.GetPreamble());
                StringWriter sw = new StringWriter();
                HtmlTextWriter htw = new HtmlTextWriter(sw);
                gv.RenderControl(htw);

                return File(System.Text.Encoding.UTF8.GetBytes(sw.ToString()), Response.ContentType, "Export_DanışmanÖneriListesi_" + DateTime.Now.ToString("dd.MM.yyyy") + ".xls");
            }
            #endregion


            ViewBag.kIds = isFiltered ? q.Select(s => s.KullaniciID).ToList() : new List<int>();



            ViewBag.AktifDonemID = new SelectList(TezIzlemeBus.CmbTiAktifDonemListe(true), "Value", "Caption", model.AktifDonemID);
            ViewBag.DonemID = new SelectList(TezIzlemeBus.CmbTiAktifDonemListe(true), "Value", "Caption", model.DonemID);
            ViewBag.AktifDurumID = new SelectList(TezDanismanOneriBus.CmbTdoOneriDurumListe(true), "Value", "Caption", model.AktifDurumID);
            ViewBag.DurumID = new SelectList(TezDanismanOneriBus.CmbTdoOneriDurumListe(true), "Value", "Caption", model.DurumID);
            ViewBag.AktifEsDurumID = new SelectList(TezDanismanOneriBus.CmbTdoEsOneriDurumListe(true), "Value", "Caption", model.AktifEsDurumID);
            ViewBag.EsDurumID = new SelectList(TezDanismanOneriBus.CmbTdoEsOneriDurumListe(true), "Value", "Caption", model.EsDurumID);


            model.Data = qdata; 
            return View(model);
        }

        public ActionResult GetTutanakRaporu()
        {
            return View();
        }
        public ActionResult GetTutanakRaporuKontrolu(List<int> OgrenimTipKods, DateTime? BasTar, DateTime? BitTar)
        {
            var mMessage = new MmMessage();
            mMessage.MessageType = Msgtype.Success;
            mMessage.IsSuccess = true;


            if (!BasTar.HasValue)
            {
                mMessage.IsSuccess = false;
                mMessage.Messages.Add("Başlangıç tarihini giriniz.");
                mMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "BasTar" });
            }
            else mMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Nothing, PropertyName = "BasTar" });
            if (!BasTar.HasValue)
            {
                mMessage.IsSuccess = false;
                mMessage.Messages.Add("Bitiş tarihini giriniz.");
                mMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "BitTar" });
            }
            else mMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Nothing, PropertyName = "BitTar" });
            if (BasTar.HasValue && BitTar.HasValue)
            {
                if (BasTar > BitTar)
                {
                    mMessage.IsSuccess = false;
                    mMessage.Messages.Add("Başlangıç tarihi bitiş tarihinden büyük olamaz.");
                    mMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "BasTar" });
                    mMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "BitTar" });
                }
                else
                {
                    mMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Nothing, PropertyName = "BasTar" });
                    mMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Nothing, PropertyName = "BitTar" });
                }
            }


            if (!mMessage.IsSuccess)
            {

                mMessage.Title = "Tutanak çıktısı oluşturulamadı";
                mMessage.MessageType = Msgtype.Warning;
            }

            return mMessage.toJsonResult();



        }
        public ActionResult GetTutanakRaporuExport(string BasTar, string BitTar, string raporTarihi, int Sayi, string EKD)
        {

            string raporAdi = "";
            var enstituKod = Management.getSelectedEnstitu(EKD);
            var enstitu = db.Enstitulers.First(p => p.EnstituKod == enstituKod);
            var baslangicTarihi = BasTar.ToDate().Value;
            var bitisTarihi = BitTar.ToDate().Value; 


            var qData = db.TDOBasvurus.AsQueryable();

            qData = qData.Where(p => p.EnstituKod == enstituKod && p.TDOBasvuruDanismen.Any(a => a.EYKDaOnaylandi == true && (a.EYKDaOnaylandiOnayTarihi >= baslangicTarihi && a.EYKDaOnaylandiOnayTarihi <= bitisTarihi))).OrderByDescending(o => o.OgrenimTipKod);
            var data = qData.SelectMany(s => s.TDOBasvuruDanismen).Where(a => a.EYKDaOnaylandi == true && (a.EYKDaOnaylandiOnayTarihi >= baslangicTarihi && a.EYKDaOnaylandiOnayTarihi <= bitisTarihi)).OrderBy(o => o.TDAnabilimDaliAdi).ThenBy(t => t.TDProgramAdi).ThenBy(t => t.TDOBasvuru.Ad).ThenBy(t => t.TDOBasvuru.Soyad).ToList();
          
            RprTDOEYK rpr = new RprTDOEYK(raporTarihi.ToDate().Value, Sayi, enstitu.EnstituAd);
            rpr.DataSource = data.Select(s => new RprTDOEYKModel
            {
                OgrenciNo = s.TDOBasvuru.OgrenciNo,
                OgrenciAdSoyad = s.TDOBasvuru.Ad + " " + s.TDOBasvuru.Soyad,
                OgrenciAnabilimdaliProgram = s.TDOBasvuru.Programlar.AnabilimDallari.AnabilimDaliAdi + " / " + s.TDOBasvuru.Programlar.ProgramAdi,
                YL_DR = s.TDOBasvuru.OgrenimTipKod == OgrenimTipi.Doktra ? "D" : (s.TDOBasvuru.OgrenimTipKod == OgrenimTipi.ButunlesikDoktora ? "BD" : "YL"),
                DanismanAdSoyad = s.TDUnvanAdi + " " + s.TDAdSoyad,
                DanismanAnabilimDali = s.TDAnabilimDaliAdi,
                TezBaslikTr = s.TezBaslikTr,
                TezBaslikEn = s.TezBaslikEn,
                TezDili = s.IsTezDiliTr ? "Tür" : "İng",
                DanismanYukYlDrSayi = (s.TDOgrenciSayisiDR ?? 0) + (s.TDOgrenciSayisiYL ?? 0),
                MezunSayisi = (s.TDTezSayisiDR ?? 0) + (s.TDTezSayisiYL ?? 0),
            });
            rpr.CreateDocument();
            raporAdi = "Danışman Önerisi EYK Tutanak Çıktısı";
            var ms = new MemoryStream(); 
            rpr.ExportToXlsx(ms);
            return File(ms.ToArray(), "application/ms-excel", raporAdi + " (" + BasTar.Replace("-", ".") + "-" + BitTar.Replace("-", ".") + ")." + "xls"); 
        }

        [Authorize(Roles = RoleNames.TDOEYKdaOnayYetkisi)]
        public ActionResult EYKGonderimOnay(string aktifDonemId)
        {
            var qDanismans = (from s in db.TDOBasvurus
                              join Ard in db.TDOBasvuruDanismen on s.AktifTDOBasvuruDanismanID equals Ard.TDOBasvuruDanismanID
                              where Ard.DanismanOnayladi == true && !Ard.EYKYaGonderildi.HasValue && (Ard.DonemBaslangicYil + "" + Ard.DonemID) == aktifDonemId
                              select s.TDOBasvuruDanisman
                         ).ToList();
            foreach (var item in qDanismans)
            {
                item.EYKYaGonderildi = true;
                item.EYKYaGonderildiIslemTarihi = DateTime.Now;
                item.EYKYaGonderildiIslemYapanID = UserIdentity.Current.Id;
            }
            db.SaveChanges();
            return new { qDanismans.Count }.toJsonResult();
        }
    }
}