using BiskaUtil;
using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Raporlar;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.Logs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Mvc;
using System.Web.UI;
using System.Web.UI.WebControls;
using LisansUstuBasvuruSistemi.Business;
using LisansUstuBasvuruSistemi.Raporlar.Mezuniyet;
using LisansUstuBasvuruSistemi.Utilities.MenuAndRoles;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.Helpers;

namespace LisansUstuBasvuruSistemi.Controllers
{
    [Authorize]
    [System.Web.Mvc.OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    public class MezuniyetGelenBasvurularController : Controller
    {
        private readonly LisansustuBasvuruSistemiEntities _entities = new LisansustuBasvuruSistemiEntities();
        [Authorize(Roles = RoleNames.MezuniyetGelenBasvurular)]
        public ActionResult Index(string ekd, int? sMezuniyetBid, int? sTabId)
        {
            var model = new FmMezuniyetBasvurulari() { PageSize = 50 };
            var mbGelenBKayitYetki = RoleNames.MezuniyetGelenBasvurularKayit.InRoleCurrent();
            if (mbGelenBKayitYetki)
            {
                model.MezuniyetSurecID = MezuniyetBus.GetMezuniyetAktifSurecId(EnstituBus.GetSelectedEnstitu(ekd));
            }

            model.Expand = model.MezuniyetSurecID.HasValue;
            model.MezuniyetDurumID = -1;
            model.SMezuniyetBID = sMezuniyetBid;
            model.STabID = sTabId;
            return Index(model, ekd);

        }
        [HttpPost]
        [Authorize(Roles = RoleNames.MezuniyetGelenBasvurular)]
        public ActionResult Index(FmMezuniyetBasvurulari model, string ekd, bool export = false)
        {
            string enstituKod = EnstituBus.GetSelectedEnstitu(ekd);

            var q = from s in _entities.MezuniyetBasvurularis
                    join ms in _entities.MezuniyetSurecis on s.MezuniyetSurecID equals ms.MezuniyetSurecID
                    join kul in _entities.Kullanicilars on s.KullaniciID equals kul.KullaniciID
                    join mOt in _entities.MezuniyetSureciOgrenimTipKriterleris on new { s.MezuniyetSurecID, s.OgrenimTipKod } equals new { mOt.MezuniyetSurecID, mOt.OgrenimTipKod }
                    join o in _entities.OgrenimTipleris on new { s.OgrenimTipKod, ms.EnstituKod } equals new { o.OgrenimTipKod, o.EnstituKod }
                    join pr in _entities.Programlars on s.ProgramKod equals pr.ProgramKod
                    join abl in _entities.AnabilimDallaris on pr.AnabilimDaliID equals abl.AnabilimDaliID
                    join en in _entities.Enstitulers on s.MezuniyetSureci.EnstituKod equals en.EnstituKod
                    join bs in _entities.MezuniyetSurecis on s.MezuniyetSurecID equals bs.MezuniyetSurecID
                    join d in _entities.Donemlers on bs.DonemID equals d.DonemID
                    join ktip in _entities.KullaniciTipleris on s.Kullanicilar.KullaniciTipID equals ktip.KullaniciTipID
                    join dr in _entities.MezuniyetYayinKontrolDurumlaris on s.MezuniyetYayinKontrolDurumID equals dr.MezuniyetYayinKontrolDurumID
                    join qmsd in _entities.MezuniyetSinavDurumlaris on s.MezuniyetSinavDurumID equals qmsd.MezuniyetSinavDurumID into defMsd
                    from msd in defMsd.DefaultIfEmpty()
                    join qjOf in _entities.MezuniyetJuriOneriFormlaris on s.MezuniyetBasvurulariID equals qjOf.MezuniyetBasvurulariID into defJof
                    from jOf in defJof.DefaultIfEmpty()
                    let srT = s.SRTalepleris.OrderByDescending(os => os.SRTalepID).FirstOrDefault()
                    let td = s.MezuniyetBasvurulariTezDosyalaris.OrderByDescending(os => os.MezuniyetBasvurulariTezDosyaID).FirstOrDefault()

                    where bs.Enstituler.EnstituKisaAd.Contains(ekd) && s.MezuniyetBasvurulariID == (model.SMezuniyetBID ?? s.MezuniyetBasvurulariID)
                    select new FrMezuniyetBasvurulari
                    {

                        MezuniyetBasvurulariID = s.MezuniyetBasvurulariID,
                        TezDanismanID = s.TezDanismanID,
                        EnstituKod = en.EnstituKod,
                        EnstituAdi = en.EnstituAd,
                        OgrenimTipAdi = o.OgrenimTipAdi,
                        AnabilimdaliAdi = abl.AnabilimDaliAdi,
                        ProgramAdi = pr.ProgramAdi,
                        MezuniyetSurecID = s.MezuniyetSurecID,
                        SurecBaslangicYil = bs.BaslangicYil,
                        DonemID = bs.DonemID,
                        MezuniyetSurecAdi = bs.BaslangicYil + "/" + bs.BitisYil + " " + d.DonemAdi + " " + bs.SiraNo,
                        BasTar = bs.BaslangicTarihi,
                        BitTar = bs.BitisTarihi,
                        KullaniciID = s.KullaniciID,
                        TezBaslikTr = s.TezBaslikTr,
                        TezDanismanAdi = s.TezDanismanAdi,
                        TezDanismanUnvani = s.TezDanismanUnvani,
                        EMail = kul.EMail,
                        CepTel = kul.CepTel,
                        KayitTarihi = kul.KayitTarihi,
                        AdSoyad = kul.Ad + " " + kul.Soyad,
                        TcKimlikNo = kul.TcKimlikNo,
                        OgrenciNo = s.OgrenciNo,
                        ResimAdi = kul.ResimAdi,
                        KullaniciTipID = kul.KullaniciTipID,
                        KullaniciTipAdi = s.KullaniciTipID == KullaniciTipBilgi.YerliOgrenci ? "" : ktip.KullaniciTipAdi,
                        OgrenimTipKod = s.OgrenimTipKod,
                        KayitOgretimYiliBaslangic = s.KayitOgretimYiliBaslangic,
                        KayitOgretimYiliDonemID = s.KayitOgretimYiliDonemID,
                        BasvuruTarihi = s.BasvuruTarihi,
                        IsMezunOldu = s.IsMezunOldu,
                        MezuniyetTarihi = s.MezuniyetTarihi,
                        SrTalebi = srT,
                        SRDurumID = srT.SRDurumID,
                        TeslimFormDurumu = srT != null && srT.SRTalepleriBezCiltFormus.Any(),
                        IsOnaylandiOrDuzeltme = td != null ? td.IsOnaylandiOrDuzeltme : null,
                        MezuniyetBasvurulariTezDosyasi = td,
                        UzatmaSuresiGun = mOt.MBSinavUzatmaSuresiGun,
                        MezuniyetSuresiGun = mOt.MBSinavUzatmaSuresiGun,
                        EYKTarihi = s.EYKTarihi,
                        MBYayinTurIDs = s.MezuniyetBasvurulariYayins.Select(sy => sy.MezuniyetYayinTurID).ToList(),
                        FormNo = jOf != null ? jOf.UniqueID : "",
                        MezuniyetJuriOneriFormu = jOf,
                        TezTeslimSonTarih = s.TezTeslimSonTarih,
                        IsDanismanOnay = s.IsDanismanOnay,
                        DanismanOnayTarihi = s.DanismanOnayTarihi,
                        DanismanOnayAciklama = s.DanismanOnayAciklama,
                        MezuniyetYayinKontrolDurumID = s.MezuniyetYayinKontrolDurumID,
                        MezuniyetYayinKontrolDurumAdi = dr.MezuniyetYayinKontrolDurumAdi,
                        DurumClassName = dr.ClassName,
                        DurumColor = dr.Color,
                        MezuniyetSinavDurumID = msd.MezuniyetSinavDurumID,
                        MezuniyetSinavDurumAdi = msd != null ? msd.MezuniyetSinavDurumAdi : "",
                        SDurumClassName = msd != null ? msd.ClassName : "",
                        SDurumColor = msd != null ? msd.Color : "",
                        MezuniyetYayinKontrolDurumAciklamasi = s.MezuniyetYayinKontrolDurumAciklamasi,


                    };
            var q2 = q;

            //Tez danışmanları sadece kendi öğrencilerini görsün
            var joFormKayitYetki = RoleNames.MezuniyetGelenBasvurularJuriOneriFormuKayit.InRoleCurrent();
            var mbGelenBKayitYetki = RoleNames.MezuniyetGelenBasvurularKayit.InRoleCurrent();
            if (joFormKayitYetki && !mbGelenBKayitYetki)
            {
                q = q.Where(p => p.TezDanismanID == UserIdentity.Current.Id);
            }
            if (model.MezuniyetSureci.HasValue)
            {
                int basYil = model.MezuniyetSureci.ToString().Substring(0, 4).ToInt().Value;
                int donemId = model.MezuniyetSureci.ToString().Substring(4, 1).ToInt().Value;
                q = q.Where(p => p.SurecBaslangicYil == basYil && p.DonemID == donemId);
            }
            if (model.MezuniyetSurecID.HasValue) q = q.Where(p => p.MezuniyetSurecID == model.MezuniyetSurecID.Value);
            if (model.KayitDonemi.IsNullOrWhiteSpace() == false)
            {
                var yil = model.KayitDonemi.Split('_')[0].ToInt().Value;
                var donem = model.KayitDonemi.Split('_')[1].ToInt().Value;
                q = q.Where(p => p.KayitOgretimYiliBaslangic == yil && p.KayitOgretimYiliDonemID == donem);
            }
            if (model.MezuniyetYayinKontrolDurumID.HasValue)
            {
                q = q.Where(p => p.MezuniyetYayinKontrolDurumID == model.MezuniyetYayinKontrolDurumID);
            }
            if (model.OgrenimTipKod.HasValue) q = q.Where(p => p.OgrenimTipKod == model.OgrenimTipKod);
            if (model.JuriOneriFormuDurumuID.HasValue)
            {
                if (model.JuriOneriFormuDurumuID == 0)
                    q = q.Where(p => p.MezuniyetJuriOneriFormu == null);
                else if (model.JuriOneriFormuDurumuID == 1)
                    q = q.Where(p => p.MezuniyetJuriOneriFormu != null && !p.MezuniyetJuriOneriFormu.EYKDaOnaylandi.HasValue && !p.MezuniyetJuriOneriFormu.EYKYaGonderildi.HasValue);
                else if (model.JuriOneriFormuDurumuID == 2)
                    q = q.Where(p => p.MezuniyetJuriOneriFormu != null && p.MezuniyetJuriOneriFormu.EYKYaGonderildi == true && !p.MezuniyetJuriOneriFormu.EYKDaOnaylandi.HasValue);
                else if (model.JuriOneriFormuDurumuID == 3)
                    q = q.Where(p => p.MezuniyetJuriOneriFormu != null && p.MezuniyetJuriOneriFormu.EYKYaGonderildi == false);
                else if (model.JuriOneriFormuDurumuID == 4)
                    q = q.Where(p => p.MezuniyetJuriOneriFormu != null && p.MezuniyetJuriOneriFormu.EYKDaOnaylandi == true);
                else if (model.JuriOneriFormuDurumuID == 5)
                    q = q.Where(p => p.MezuniyetJuriOneriFormu != null && p.MezuniyetJuriOneriFormu.EYKDaOnaylandi == false);
            }
            if (model.SRDurumID.HasValue) q = q.Where(p => p.SRDurumID == model.SRDurumID.Value);
            if (model.TDDurumID.HasValue)
            {
                if (model.TDDurumID == 2)
                    q = q.Where(p => p.MezuniyetBasvurulariTezDosyasi != null && !p.IsOnaylandiOrDuzeltme.HasValue);
                else
                {
                    var isOnaylandiOrDuzeltme = (model.TDDurumID == 1);
                    q = q.Where(p => p.IsOnaylandiOrDuzeltme == isOnaylandiOrDuzeltme);
                }

            }
            if (model.MezuniyetSinavDurumID.HasValue)
            {
                if (model.MezuniyetSinavDurumID == MezuniyetSinavDurum.SonucGirilmedi) q = q.Where(p => !p.SrTalebi.MezuniyetSinavDurumID.HasValue || p.SrTalebi.MezuniyetSinavDurumID == model.MezuniyetSinavDurumID.Value);
                else q = q.Where(p => p.SrTalebi.MezuniyetSinavDurumID == model.MezuniyetSinavDurumID.Value);
            }
            if (model.TeslimFormDurumu.HasValue) q = q.Where(p => p.TeslimFormDurumu == model.TeslimFormDurumu.Value);
            if (model.MezuniyetDurumID != -1)
            {
                var isMezunOldu = model.MezuniyetDurumID.HasValue ? (model.MezuniyetDurumID == 1 ? true : false) : (bool?)null;
                q = q.Where(p => p.IsMezunOldu == isMezunOldu);

                if (isMezunOldu == true)
                {
                    if (model.MBaslangicTarihi.HasValue && model.MBitisTarihi.HasValue) q = q.Where(p => model.MBaslangicTarihi <= p.MezuniyetTarihi && model.MBitisTarihi >= p.MezuniyetTarihi);
                    else if (model.MBaslangicTarihi.HasValue) q = q.Where(p => model.MBaslangicTarihi == p.MezuniyetTarihi);
                    else if (model.MBitisTarihi.HasValue) q = q.Where(p => model.MBitisTarihi == p.MezuniyetTarihi);
                }
            }
            if (!model.AdSoyad.IsNullOrWhiteSpace())
            {
                model.AdSoyad = model.AdSoyad.Trim();
                q = q.Where(p => p.AdSoyad.Contains(model.AdSoyad) || p.TcKimlikNo == model.AdSoyad || p.OgrenciNo == model.AdSoyad || p.FormNo == model.AdSoyad || p.TezDanismanAdi.Contains(model.AdSoyad));
            }
            if (model.UyrukKod.HasValue) q = q.Where(p => p.UyrukKod == model.UyrukKod);
            bool isFiltered = q != q2;

            model.RowCount = q.Count();
            if (!model.Sort.IsNullOrWhiteSpace()) q = q.OrderBy(model.Sort);
            else
            {
                q = model.JuriOneriFormuDurumuID == 2 ? q.OrderBy(o => o.MezuniyetJuriOneriFormu.EYKYaGonderildiIslemTarihi) : q.OrderByDescending(o => o.BasvuruTarihi);
            }
            var qdata = q.Skip(model.StartRowIndex).Take(model.PageSize).ToList();
            model.Data = qdata;
            #region export
            if (export && model.RowCount > 0)
            {
                var gv = new GridView();
                var qExp = q.ToList();
                gv.DataSource = (from s in qExp
                                 join td in _entities.Kullanicilars on s.TezDanismanID equals td.KullaniciID
                                 select new
                                 {
                                     s.MezuniyetSurecAdi,
                                     s.OgrenimTipAdi,
                                     TezDanismanAdi = s.TezDanismanUnvani + " " + s.TezDanismanAdi,
                                     DanismanTel = td.CepTel,
                                     DanismanEmail = td.EMail,
                                     s.AnabilimdaliAdi,
                                     s.ProgramAdi,
                                     GsisKayitTarihi = s.KayitTarihi != null ? s.KayitTarihi.ToString("dd.MM.yyyy") : "",
                                     s.AdSoyad,
                                     s.TcKimlikNo,
                                     s.OgrenciNo,
                                     s.EMail,
                                     s.CepTel,
                                     YayinSarti = s.MBYayinTurIDs.Any() ? "Var" : "Yok",
                                     PatentSayisi = s.MBYayinTurIDs.Count(p => p == 6),
                                     ProjeSayisi = s.MBYayinTurIDs.Count(p => p == 7),
                                     UBildiriSayisi = s.MBYayinTurIDs.Count(p => p == 2),
                                     UMakaleSayisi = s.MBYayinTurIDs.Count(p => p == 4),
                                     UABildiriSayisi = s.MBYayinTurIDs.Count(p => p == 3),
                                     UAMakaleSayisi = s.MBYayinTurIDs.Count(p => p == 5),
                                     s.MezuniyetYayinKontrolDurumAdi,
                                     EYKTarihi = s.EYKTarihi != null ? s.EYKTarihi.Value.ToString("dd.MM.yyyy") : "",
                                     JOFTezbasligiDegisti = s.MezuniyetJuriOneriFormu != null ? (s.MezuniyetJuriOneriFormu.IsTezBasligiDegisti == true ? "Değişti" : "Değişmedi") : "-",
                                     JOFTezDili = s.MezuniyetJuriOneriFormu != null ? (s.IsTezDiliTr == true ? "Türkçe" : "İngilizce") : "",
                                     JOFTezBasligiTr = s.MezuniyetJuriOneriFormu != null ? s.TezBaslikTr : "-",
                                     JOFTezBasligiEn = s.MezuniyetJuriOneriFormu != null ? s.TezBaslikEn : "-",
                                     JOFYeniTezBaslikTr = s.MezuniyetJuriOneriFormu != null ? s.MezuniyetJuriOneriFormu.YeniTezBaslikTr : "-",
                                     JOFYeniTezBaslikEn = s.MezuniyetJuriOneriFormu != null ? s.MezuniyetJuriOneriFormu.YeniTezBaslikEn : "-",
                                     SinavTarihi = s.SrTalebi != null ? s.SrTalebi.Tarih.ToString("dd.MM.yyyy") : "",
                                     SinavdaTezbasligiDegisti = s.SrTalebi != null ? (s.SrTalebi.IsTezBasligiDegisti == true ? "Değişti" : "Değişmedi") : null,
                                     SinavTezDili = s.SrTalebi != null ? (s.IsTezDiliTr == true ? "Türkçe" : "İngilizce") : "",
                                     SinavTezBasligiTr = s.SrTalebi != null ? (s.SrTalebi.IsTezBasligiDegisti == true ? s.SrTalebi.YeniTezBaslikTr : (s.MezuniyetJuriOneriFormu.IsTezBasligiDegisti == true ? s.MezuniyetJuriOneriFormu.YeniTezBaslikTr : s.TezBaslikTr)) : "-",
                                     SinavTezBasligiEn = s.SrTalebi != null ? (s.SrTalebi.IsTezBasligiDegisti == true ? s.SrTalebi.YeniTezBaslikEn : (s.MezuniyetJuriOneriFormu.IsTezBasligiDegisti == true ? s.MezuniyetJuriOneriFormu.YeniTezBaslikEn : s.TezBaslikEn)) : "-",
                                     s.MezuniyetSinavDurumAdi,
                                     UzatmaTarihi = s.SrTalebi != null && s.MezuniyetSinavDurumID == MezuniyetSinavDurum.Uzatma ? s.SrTalebi.Tarih.AddDays(s.UzatmaSuresiGun).ToString("dd.MM.yyyy") : "",
                                     TezTeslimSonTarih = s.SrTalebi != null && s.MezuniyetSinavDurumID == MezuniyetSinavDurum.Basarili ? (s.TezTeslimSonTarih ?? s.SrTalebi.Tarih.AddDays(s.MezuniyetSuresiGun).Date).ToString("dd.MM.yyy") : "",
                                     MezuniyetDurumu = s.IsMezunOldu.HasValue ? (s.IsMezunOldu.Value ? "Mezun Oldu" : "Mezun Olamadı") : "İşlem Bekliyor",
                                     MezuniyetTarihi = s.IsMezunOldu == true ? s.MezuniyetTarihi.Value.ToString("dd.MM.yyyy") : "",
                                 }).ToList();
                gv.DataBind();
                Response.ContentType = "application/ms-excel";
                Response.ContentEncoding = System.Text.Encoding.UTF8;
                Response.BinaryWrite(System.Text.Encoding.UTF8.GetPreamble());
                StringWriter sw = new StringWriter();
                HtmlTextWriter htw = new HtmlTextWriter(sw);
                gv.RenderControl(htw);

                return File(System.Text.Encoding.UTF8.GetBytes(sw.ToString()), Response.ContentType, "Export_MezuniyetBasvuruListesi_" + DateTime.Now.ToString("dd.MM.yyyy") + ".xls");
            }
            #endregion
            if (isFiltered)
            {
                ViewBag.kIds = q.Select(s => s.KullaniciID).ToList();
            }
            else ViewBag.kIds = new List<int>();

            ViewBag.MezuniyetSurecID = new SelectList(MezuniyetBus.GetCmbMezuniyetSurecleri(enstituKod, true), "Value", "Caption", model.MezuniyetSurecID);
            ViewBag.MezuniyetSureci = new SelectList(MezuniyetBus.GetCmbMezuniyetSurecGroup(enstituKod, true), "Value", "Caption", model.MezuniyetSureci);
            ViewBag.OgrenimTipKod = new SelectList(OgrenimTipleriBus.CmbAktifOgrenimTipleri(enstituKod, true, true), "Value", "Caption", model.OgrenimTipKod);
            ViewBag.MezuniyetYayinKontrolDurumID = new SelectList(MezuniyetBus.GetCmbMezuniyetYayinDurumListe(true, true), "Value", "Caption", model.MezuniyetYayinKontrolDurumID);
            ViewBag.JuriOneriFormuDurumuID = new SelectList(MezuniyetBus.GetCmbJuriOneriFormuDurumu(true), "Value", "Caption", model.JuriOneriFormuDurumuID);
            ViewBag.KayitDonemi = new SelectList(MezuniyetBus.GetCmbMezuniyetKayitDonemleri(enstituKod, model.MezuniyetSurecID, true), "Value", "Caption", model.KayitDonemi);
            ViewBag.SRDurumID = new SelectList(SrTalepleriBus.GetCmbSrDurumListe(true), "Value", "Caption", model.SRDurumID);
            ViewBag.TDDurumID = new SelectList(MezuniyetBus.GetCmbTezDurumListe(true), "Value", "Caption", model.TDDurumID);
            ViewBag.MezuniyetSinavDurumID = new SelectList(MezuniyetBus.GetCmbMzSinavDurumListe(true), "Value", "Caption", model.MezuniyetSinavDurumID);
            ViewBag.TeslimFormDurumu = new SelectList(MezuniyetBus.GetCmbTeslimFormDurumu(true), "Value", "Caption", model.TeslimFormDurumu);
            ViewBag.MezuniyetDurumID = new SelectList(MezuniyetBus.GetCmbMezuniyetDurumId(true), "Value", "Caption", model.MezuniyetDurumID);
            return View(model);
        }


        [Authorize(Roles = RoleNames.MezuniyetGelenBasvurularKayit)]
        public ActionResult YayinKontrol(int id, int mezuniyetBasvurulariYayinId)
        {

            var model = MezuniyetBus.GetMezuniyetBasvuruDetayBilgi(id, mezuniyetBasvurulariYayinId);
            return View(model);
        }
        [Authorize(Roles = RoleNames.MezuniyetGelenBasvurularKayit)]
        public ActionResult YayinKontrolPost(int id, bool? danismanIsmiVar, bool? tezIcerikUyumuVar, bool? onaylandi)
        {
            var mmMessage = new MmMessage();


            if (onaylandi.HasValue)
            {
                if (danismanIsmiVar.HasValue == false)
                {
                    mmMessage.Messages.Add("Onaylama işlemini yapabilmeniz için 'Danışman İsmi Var Mı' sorusunu cevaplayınız");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "DanismanIsmiVar" });
                }
                if (tezIcerikUyumuVar.HasValue == false)
                {
                    mmMessage.Messages.Add("Onaylama işlemini yapabilmeniz için 'Tez İçeriği ile Uyumlu mu' sorusunu cevaplayınız");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "TezIcerikUyumuVar" });
                }
            }
            if (mmMessage.Messages.Count == 0)
            {
                var yayin = _entities.MezuniyetBasvurulariYayins.First(p => p.MezuniyetBasvurulariYayinID == id);
                yayin.DanismanIsmiVar = danismanIsmiVar;
                yayin.TezIcerikUyumuVar = tezIcerikUyumuVar;
                yayin.Onaylandi = onaylandi;
                yayin.IslemTarihi = DateTime.Now;
                yayin.IslemYapanID = UserIdentity.Current.Id;
                yayin.IslemYapanIP = UserIdentity.Ip;
                _entities.SaveChanges();
                LogIslemleri.LogEkle("MezuniyetBasvurulariYayins", IslemTipi.Update, yayin.ToJson());
                mmMessage.IsSuccess = true;
                mmMessage.Title = "Yayın bilgi kontrol işlemi";
                mmMessage.Messages.Add("Kayıt güncellendi");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Nothing, PropertyName = "DanismanIsmiVar" });
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Nothing, PropertyName = "TezIcerikUyumuVar" });

            }
            else
            {
                mmMessage.Title = "Yayın bilgi kontrol kaydını yapabilmek için aşağıdaki uyarıları kontrol ediniz.";
                mmMessage.IsSuccess = false;
            }
            mmMessage.MessageType = mmMessage.IsSuccess ? Msgtype.Success : Msgtype.Warning;
            return Json(new { Messages = mmMessage }, "application/json", JsonRequestBehavior.AllowGet);

        }

        [Authorize(Roles = RoleNames.MezuniyetGelenBasvurularKayit)]
        public ActionResult YayinIndexUpdate(int id, int indexId)
        {
            var mmMessage = new MmMessage();
            var kayit = _entities.MezuniyetBasvurulariYayins.FirstOrDefault(p => p.MezuniyetBasvurulariYayinID == id);
            try
            {
                kayit.MezuniyetYayinIndexTurID = indexId;
                _entities.SaveChanges();
                LogIslemleri.LogEkle("MezuniyetBasvurulariYayins", IslemTipi.Update, kayit.ToJson());
                mmMessage.Messages.Add("Index Bilgisi Güncellendi");
                mmMessage.MessageType = Msgtype.Success;

            }
            catch (Exception ex)
            {
                mmMessage.MessageType = Msgtype.Error;
                mmMessage.IsSuccess = false;
                mmMessage.Messages.Add("Index Bilgisi Güncellenirken bir hata oluştu! Hata:" + ex.ToExceptionMessage());
                SistemBilgilendirmeBus.SistemBilgisiKaydet(ex.ToExceptionMessage(), "MezuniyetGelenBasvurular/YayinIndexUpdate<br/><br/>" + ex.ToExceptionStackTrace(), LogType.OnemsizHata);
            }
            var strView = ViewRenderHelper.RenderPartialView("Ajax", "getMessage", mmMessage);
            return Json(new { IsSuccess = mmMessage.IsSuccess, Messages = strView }, "application/json", JsonRequestBehavior.AllowGet);
        }

        [Authorize(Roles = RoleNames.MezuniyetGelenBasvurularKayit)]
        public ActionResult DurumKayit(int id, int? MezuniyetYayinKontrolDurumID, string MezuniyetYayinKontrolDurumAciklamasi)
        {
            var mmMessage = new MmMessage();
            if (MezuniyetYayinKontrolDurumID.HasValue == false)
            {
                mmMessage.Messages.Add("Başvuru durumu seçiniz");
            }
            if (MezuniyetYayinKontrolDurumID == MezuniyetYayinKontrolDurumu.IptalEdildi && MezuniyetYayinKontrolDurumAciklamasi.IsNullOrWhiteSpace())
            {
                mmMessage.Messages.Add("Başvuru durumu iptal seçeneği seçilirse İptal açıklaması girilmesi zorunludur.");
            }
            if (mmMessage.Messages.Count == 0)
            {
                var mBasvur = _entities.MezuniyetBasvurularis.First(p => p.MezuniyetBasvurulariID == id);
                bool mgonder = (MezuniyetYayinKontrolDurumID == MezuniyetYayinKontrolDurumu.IptalEdildi || MezuniyetYayinKontrolDurumID == MezuniyetYayinKontrolDurumu.KabulEdildi) && MezuniyetYayinKontrolDurumID != mBasvur.MezuniyetYayinKontrolDurumID;

                mBasvur.MezuniyetYayinKontrolDurumID = MezuniyetYayinKontrolDurumID.Value;
                mBasvur.MezuniyetYayinKontrolDurumAciklamasi = MezuniyetYayinKontrolDurumAciklamasi;
                mBasvur.IslemTarihi = DateTime.Now;
                mBasvur.IslemYapanID = UserIdentity.Current.Id;
                mBasvur.IslemYapanIP = UserIdentity.Ip;
                _entities.SaveChanges();
                LogIslemleri.LogEkle("MezuniyetBasvurulari", IslemTipi.Update, mBasvur.ToJson());
                mmMessage.IsSuccess = true;
                #region sendMail
                if (mgonder)
                {
                    var enstitu = mBasvur.MezuniyetSureci.Enstituler;
                    var sablonlar = _entities.MailSablonlaris.Where(p => p.EnstituKod == enstitu.EnstituKod).ToList();


                    var mModel = new List<SablonMailModel>();
                    if (MezuniyetYayinKontrolDurumID != MezuniyetYayinKontrolDurumu.IptalEdildi)
                    {
                        var danisman = _entities.Kullanicilars.First(p => p.KullaniciID == mBasvur.TezDanismanID);
                        mModel.Add(
                            new SablonMailModel
                            {

                                MailSablonTipID = MailSablonTipi.Mez_YayinSartiSaglandiDanisman,
                                AdSoyad = danisman.Ad + " " + danisman.Soyad,
                                EMails = new List<MailSendList> { new MailSendList { EMail = danisman.EMail, ToOrBcc = true } },
                                UnvanAdi = danisman.Unvanlar.UnvanAdi
                            });
                    }
                    var ogrenciMailSablonId = 1;
                    if (MezuniyetYayinKontrolDurumID == MezuniyetYayinKontrolDurumu.IptalEdildi) ogrenciMailSablonId = MailSablonTipi.Mez_YayinSartiSaglanamadiOgrenci;
                    else if (mBasvur.OgrenimTipKod.IsDoktora()) ogrenciMailSablonId = MailSablonTipi.Mez_YayinSartiSaglandiOgrenciDoktora;
                    else ogrenciMailSablonId = MailSablonTipi.Mez_YayinSartiSaglandiOgrenciYL;
                    mModel.Add(new SablonMailModel
                    {

                        AdSoyad = mBasvur.Ad + " " + mBasvur.Soyad,
                        EMails = new List<MailSendList> { new MailSendList { EMail = mBasvur.Kullanicilar.EMail, ToOrBcc = true } },
                        MailSablonTipID = ogrenciMailSablonId,
                    });


                    foreach (var item in mModel)
                    {
                        var basvuruDonemAdi = mBasvur.MezuniyetSureci.BaslangicYil + " " + mBasvur.MezuniyetSureci.BitisYil + " / " + mBasvur.MezuniyetSureci.Donemler.DonemAdi;
                        var enstituL = mBasvur.MezuniyetSureci.Enstituler;

                        item.Sablon = sablonlar.First(p => p.MailSablonTipID == item.MailSablonTipID);
                        item.SablonParametreleri = item.Sablon.MailSablonTipleri.Parametreler.Split(',').ToList().Select(s => s.Trim()).ToList();

                        if (item.Sablon.GonderilecekEkEpostalar != null) item.EMails.AddRange(item.Sablon.GonderilecekEkEpostalar.Split(',').Select(s => new MailSendList { EMail = s.Trim(), ToOrBcc = false }).ToList());
                        var paramereDegerleri = new List<MailReplaceParameterDto>();
                        if (item.SablonParametreleri.Any(a => a == "@EnstituAdi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "EnstituAdi", Value = enstituL.EnstituAd });
                        if (item.SablonParametreleri.Any(a => a == "@WebAdresi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "WebAdresi", Value = enstitu.WebAdresi, IsLink = true });
                        if (item.SablonParametreleri.Any(a => a == "@BasvuruDonemAdi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "BasvuruDonemAdi", Value = basvuruDonemAdi });
                        if (item.SablonParametreleri.Any(a => a == "@AdSoyad"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "AdSoyad", Value = item.AdSoyad });
                        if (item.SablonParametreleri.Any(a => a == "@UnvanAdi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "UnvanAdi", Value = item.UnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciAdSoyad"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "OgrenciAdSoyad", Value = mBasvur.Ad + " " + mBasvur.Soyad });
                        if (item.SablonParametreleri.Any(a => a == "@IptalAciklamasi"))
                            paramereDegerleri.Add(new MailReplaceParameterDto { Key = "IptalAciklamasi", Value = mBasvur.MezuniyetYayinKontrolDurumAciklamasi });

                        var attachs = new List<System.Net.Mail.Attachment>();

                        if (item.MailSablonTipID != MailSablonTipi.Mez_YayinSartiSaglandiDanisman && MezuniyetYayinKontrolDurumID != MezuniyetYayinKontrolDurumu.IptalEdildi)
                        {
                            attachs = Management.exportRaporPdf(RaporTipleri.MezuniyetBasvuruRaporu, new List<int?> { mBasvur.MezuniyetBasvurulariID });
                        }
                        if (MezuniyetYayinKontrolDurumID == MezuniyetYayinKontrolDurumu.KabulEdildi)
                        {
                            var ttfp = Management.exportRaporPdf(RaporTipleri.MezuniyetTezTeslimFormu, new List<int?> { mBasvur.MezuniyetBasvurulariID, 1 });
                            attachs.AddRange(ttfp);
                        }
                        var mCOntent = SystemMails.GetSystemMailContent(enstituL.EnstituAd, item.Sablon.SablonHtml, item.Sablon.SablonAdi, paramereDegerleri);
                        var snded = MailManager.SendMail(enstitu.EnstituKod, mCOntent.Title, mCOntent.HtmlContent, item.EMails, attachs);
                        if (snded)
                        {
                            var kModel = new GonderilenMailler
                            {
                                Tarih = DateTime.Now,
                                EnstituKod = enstitu.EnstituKod,
                                MesajID = null,
                                IslemTarihi = DateTime.Now,
                                Konu = item.Sablon.SablonAdi + " (" + item.AdSoyad + ")"
                            };
                            if (!item.JuriTipAdi.IsNullOrWhiteSpace()) kModel.Konu = kModel.Konu + " (" + item.JuriTipAdi + ")";
                            kModel.IslemYapanID = UserIdentity.Current == null || !UserIdentity.Current.IsAuthenticated ? 1 : UserIdentity.Current.Id;
                            kModel.IslemYapanIP = UserIdentity.Ip;
                            kModel.Aciklama = item.Sablon.Sablon ?? "";
                            kModel.AciklamaHtml = mCOntent.HtmlContent ?? "";
                            kModel.Gonderildi = true;
                            kModel.GonderilenMailEkleris = attachs.Select(s => new GonderilenMailEkleri { EkAdi = s.Name, EkDosyaYolu = "" }).ToList();
                            kModel.GonderilenMailKullanicilars = item.EMails.Select(s => new GonderilenMailKullanicilar { Email = s.EMail }).ToList();
                            _entities.GonderilenMaillers.Add(kModel);
                            _entities.SaveChanges();
                        }
                    }

                }
                #endregion

            }
            else
            {
                mmMessage.Title = "Mezuniyet başvurusu durum kayıt işlemi";
                mmMessage.IsSuccess = false;
            }
            mmMessage.MessageType = mmMessage.IsSuccess ? Msgtype.Success : Msgtype.Warning;
            return Json(new { Messages = mmMessage }, "application/json", JsonRequestBehavior.AllowGet);

        }
        public ActionResult DanismanOnayKayit(int id, bool? IsDanismanOnay, string BasvuruDanismanOnayAciklama)
        {
            var mmMessage = new MmMessage
            {
                Title = "Mezuniyet başvurusu danışman onay işlemi"
            };

            var mBasvur = _entities.MezuniyetBasvurularis.First(p => p.MezuniyetBasvurulariID == id);
            var kayitYetki = RoleNames.GelenBasvurularKayit.InRole();

            if (!kayitYetki)
            {
                if (mBasvur.TezDanismanID != UserIdentity.Current.Id)
                {
                    mmMessage.Messages.Add("Danışman olarak atanmadığını bir mezuniyet başvurusu için onay işlemi yapamazsınız!");
                }
            }

            if (!mmMessage.Messages.Any())
            {

                if (IsDanismanOnay == false && BasvuruDanismanOnayAciklama.IsNullOrWhiteSpace())
                {
                    mmMessage.Messages.Add("Öğrenci Başvurusunu Reddediyorum seçeneği seçilirse Açıklama girilmesi zorunludur.");
                }
                bool sendMail = false;
                if (mmMessage.Messages.Count == 0)
                {

                    if (IsDanismanOnay != mBasvur.IsDanismanOnay)
                    {
                        sendMail = true;
                        mBasvur.TezTeslimUniqueID = Guid.NewGuid();
                        mBasvur.TezTeslimFormKodu = Guid.NewGuid().ToString().Substring(0, 8);
                    }
                    mBasvur.IsDanismanOnay = IsDanismanOnay;
                    mBasvur.DanismanOnayAciklama = BasvuruDanismanOnayAciklama;
                    mBasvur.DanismanOnayTarihi = DateTime.Now;




                    _entities.SaveChanges();
                    LogIslemleri.LogEkle("MezuniyetBasvurulari", IslemTipi.Update, mBasvur.ToJson());
                    mmMessage.IsSuccess = true;
                    mmMessage.Messages.Add(IsDanismanOnay.HasValue ? (IsDanismanOnay.Value ? "Başvuru Onaylandı." : "Başvuru Ret Edildi.") : "Onaylama İşlemi Geril Alındı.");
                    if (sendMail)
                    {
                        #region sendMail
                        var enstitu = mBasvur.MezuniyetSureci.Enstituler;
                        var sablonTipId = mBasvur.IsDanismanOnay == true ? MailSablonTipi.Mez_DanismanOnayladiOgrenci : MailSablonTipi.Mez_DanismanOnaylamadiOgrenci;
                        var sablonlar = _entities.MailSablonlaris.Where(p => p.EnstituKod == enstitu.EnstituKod).ToList();



                        var mModel = new List<SablonMailModel>
                        {
                            new SablonMailModel
                            {

                                AdSoyad = mBasvur.Ad + " " + mBasvur.Soyad,
                                EMails = new List<MailSendList> { new MailSendList { EMail = mBasvur.Kullanicilar.EMail, ToOrBcc = true } },
                                MailSablonTipID = sablonTipId,
                            }
                        };

                        var danisman = _entities.Kullanicilars.First(p => p.KullaniciID == mBasvur.TezDanismanID);
                        foreach (var item in mModel)
                        {
                            var basvuruDonemAdi = mBasvur.MezuniyetSureci.BaslangicYil + " " + mBasvur.MezuniyetSureci.BitisYil + " / " + mBasvur.MezuniyetSureci.Donemler.DonemAdi;
                            var enstituL = mBasvur.MezuniyetSureci.Enstituler;

                            item.Sablon = sablonlar.First(p => p.MailSablonTipID == item.MailSablonTipID);
                            item.SablonParametreleri = item.Sablon.MailSablonTipleri.Parametreler.Split(',').ToList().Select(s => s.Trim()).ToList();

                            if (item.Sablon.GonderilecekEkEpostalar != null) item.EMails.AddRange(item.Sablon.GonderilecekEkEpostalar.Split(',').Select(s => new MailSendList { EMail = s.Trim(), ToOrBcc = false }).ToList());
                            var paramereDegerleri = new List<MailReplaceParameterDto>();
                            if (item.SablonParametreleri.Any(a => a == "@EnstituAdi"))
                                paramereDegerleri.Add(new MailReplaceParameterDto { Key = "EnstituAdi", Value = enstituL.EnstituAd });
                            if (item.SablonParametreleri.Any(a => a == "@WebAdresi"))
                                paramereDegerleri.Add(new MailReplaceParameterDto { Key = "WebAdresi", Value = enstitu.WebAdresi, IsLink = true });
                            if (item.SablonParametreleri.Any(a => a == "@BasvuruDonemAdi"))
                                paramereDegerleri.Add(new MailReplaceParameterDto { Key = "BasvuruDonemAdi", Value = basvuruDonemAdi });
                            if (item.SablonParametreleri.Any(a => a == "@DanismanAdSoyad"))
                                paramereDegerleri.Add(new MailReplaceParameterDto { Key = "DanismanAdSoyad", Value = danisman.Ad + " " + danisman.Soyad });
                            if (item.SablonParametreleri.Any(a => a == "@DanismanUnvanAdi"))
                                paramereDegerleri.Add(new MailReplaceParameterDto { Key = "DanismanUnvanAdi", Value = danisman.Unvanlar.UnvanAdi });
                            if (item.SablonParametreleri.Any(a => a == "@OgrenciNo"))
                                paramereDegerleri.Add(new MailReplaceParameterDto { Key = "OgrenciNo", Value = mBasvur.OgrenciNo });
                            if (item.SablonParametreleri.Any(a => a == "@OgrenciAdSoyad"))
                                paramereDegerleri.Add(new MailReplaceParameterDto { Key = "OgrenciAdSoyad", Value = mBasvur.Ad + " " + mBasvur.Soyad });
                            if (item.SablonParametreleri.Any(a => a == "@RetAciklamasi"))
                                paramereDegerleri.Add(new MailReplaceParameterDto { Key = "RetAciklamasi", Value = mBasvur.DanismanOnayAciklama });

                            var attachs = new List<System.Net.Mail.Attachment>();

                            var mCOntent = SystemMails.GetSystemMailContent(enstituL.EnstituAd, item.Sablon.SablonHtml, item.Sablon.SablonAdi, paramereDegerleri);
                            var snded = MailManager.SendMail(enstitu.EnstituKod, mCOntent.Title, mCOntent.HtmlContent, item.EMails, attachs);
                            if (snded)
                            {
                                var kModel = new GonderilenMailler();
                                kModel.Tarih = DateTime.Now;
                                kModel.EnstituKod = enstitu.EnstituKod;
                                kModel.MesajID = null;
                                kModel.IslemTarihi = DateTime.Now;
                                kModel.Konu = item.Sablon.SablonAdi + " (" + item.AdSoyad + ")";
                                if (!item.JuriTipAdi.IsNullOrWhiteSpace()) kModel.Konu = kModel.Konu + " (" + item.JuriTipAdi + ")";
                                kModel.IslemYapanID = UserIdentity.Current == null || !UserIdentity.Current.IsAuthenticated ? 1 : UserIdentity.Current.Id;
                                kModel.IslemYapanIP = UserIdentity.Ip;
                                kModel.Aciklama = item.Sablon.Sablon ?? "";
                                kModel.AciklamaHtml = mCOntent.HtmlContent ?? "";
                                kModel.Gonderildi = true;
                                kModel.GonderilenMailEkleris = attachs.Select(s => new GonderilenMailEkleri { EkAdi = s.Name, EkDosyaYolu = "" }).ToList();
                                kModel.GonderilenMailKullanicilars = item.EMails.Select(s => new GonderilenMailKullanicilar { Email = s.EMail }).ToList();
                                _entities.GonderilenMaillers.Add(kModel);
                                _entities.SaveChanges();
                            }
                        }

                        #endregion 
                    }



                }
            }
            mmMessage.MessageType = mmMessage.IsSuccess ? Msgtype.Success : Msgtype.Warning;
            return Json(new { Messages = mmMessage }, "application/json", JsonRequestBehavior.AllowGet);

        }

        public ActionResult DanismanUzatmaOnayKayit(int srTalepId, bool? isDanismanUzatmaSonrasiOnay, string danismanUzatmaSonrasiOnayAciklama)
        {
            var mmMessage = new MmMessage
            {
                Title = "Uzatma sonrası danışman onay işlemi"
            };

            var srTalep = _entities.SRTalepleris.First(p => p.SRTalepID == srTalepId);
            var kayitYetki = RoleNames.GelenBasvurularKayit.InRole();

            if (!kayitYetki)
            {
                if (srTalep.MezuniyetBasvurulari.TezDanismanID != UserIdentity.Current.Id)
                {
                    mmMessage.Messages.Add("Danışman olarak atanmadığını bir mezuniyet başvurusu için onay işlemi yapamazsınız!");
                }
            }
            if (_entities.SRTalepleris.Any(a => a.SRTalepID > srTalepId && a.MezuniyetBasvurulariID == srTalep.MezuniyetSinavDurumID))
            {
                mmMessage.Messages.Add("Öğrenci tarafından yeni sınav talebi oluşturuldu. Bu işlemi yapamazsınız.");
            }
            if (!mmMessage.Messages.Any())
            {

                if (isDanismanUzatmaSonrasiOnay == false && danismanUzatmaSonrasiOnayAciklama.IsNullOrWhiteSpace())
                {
                    mmMessage.Messages.Add("Öğrenci Başvurusunu Reddediyorum seçeneği seçilirse Açıklama girilmesi zorunludur.");
                }
                if (mmMessage.Messages.Count == 0)
                {
                    srTalep.IsDanismanUzatmaSonrasiOnay = isDanismanUzatmaSonrasiOnay;
                    srTalep.DanismanUzatmaSonrasiOnayAciklama = danismanUzatmaSonrasiOnayAciklama;
                    srTalep.DanismanOnayTarihi = DateTime.Now;

                    _entities.SaveChanges();
                    LogIslemleri.LogEkle("SRTalebi", IslemTipi.Update, srTalep.ToJson());
                    mmMessage.IsSuccess = true;
                    mmMessage.Messages.Add(isDanismanUzatmaSonrasiOnay.HasValue ? (isDanismanUzatmaSonrasiOnay.Value ? "Başvuru Onaylandı." : "Başvuru Ret Edildi.") : "Onaylama İşlemi Geril Alındı.");
                }
            }
            mmMessage.MessageType = mmMessage.IsSuccess ? Msgtype.Success : Msgtype.Warning;
            return Json(new { Messages = mmMessage }, "application/json", JsonRequestBehavior.AllowGet);

        }

        [Authorize(Roles = RoleNames.MezuniyetGelenBasvurularKayit)]
        public ActionResult SrDurumKaydet(int id, int srDurumId, string srDurumAciklamasi)
        {
            string strView = "";
            string fWeight = "font-weight:";

            var talep = _entities.SRTalepleris.First(p => p.SRTalepID == id);


            fWeight += Convert.ToDateTime(talep.Tarih.ToShortDateString() + " " + talep.BasSaat) > DateTime.Now ? "bold;" : "normal;";


            if (srDurumId == SRTalepDurum.Onaylandı && talep.SRSalonID.HasValue)
            {
                var qTalepEslesen = _entities.SRTalepleris.Where(a => a.SRTalepID != talep.SRTalepID && a.SRSalonID == talep.SRSalonID && a.Tarih == talep.Tarih &&
                                        (
                                          (a.BasSaat == talep.BasSaat || a.BitSaat == talep.BitSaat) ||
                                        (
                                            (a.BasSaat < talep.BasSaat && a.BitSaat > talep.BasSaat) || a.BasSaat < talep.BitSaat && a.BitSaat > talep.BitSaat) ||
                                            (a.BasSaat > talep.BasSaat && a.BasSaat < talep.BitSaat) || a.BitSaat > talep.BasSaat && a.BitSaat < talep.BitSaat)
                                        ).ToList();
                if (talep.MezuniyetBasvurulari.OgrenimTipKod.IsDoktora() && qTalepEslesen.Any(p => p.SRDurumID == SRTalepDurum.Onaylandı))
                {

                    var salon = _entities.SRSalonlars.First(p => p.SRSalonID == talep.SRSalonID);
                    string msg = talep.Tarih.ToShortDateString() + " " + talep.BasSaat.ToString() + " - " + talep.BitSaat.ToString() + " Tarihi için '" + salon.SalonAdi + "' Salonu doludur bu rezervasyon onaylanamaz!";
                    var mmMessage = new MmMessage();
                    mmMessage.Messages.Add(msg);
                    mmMessage.IsSuccess = false;
                    mmMessage.MessageType = Msgtype.Error;
                    strView = ViewRenderHelper.RenderPartialView("Ajax", "getMessage", mmMessage);
                }
            }

            bool sendMail = talep.SRDurumID != srDurumId && new List<int> { SRTalepDurum.Reddedildi, SRTalepDurum.Onaylandı }.Contains(srDurumId);
            talep.SRDurumID = srDurumId;
            talep.IslemTarihi = DateTime.Now;
            talep.IslemYapanID = UserIdentity.Current.Id;
            talep.IslemYapanIP = UserIdentity.Ip;
            if (srDurumId == SRTalepDurum.Reddedildi) talep.SRDurumAciklamasi = srDurumAciklamasi;
            _entities.SaveChanges();
            LogIslemleri.LogEkle("SRTalepleri", IslemTipi.Update, talep.ToJson());
            var qbDrm = talep.SRDurumlari;

            if (talep.SRTalepTipleri.IsTezSinavi && sendMail)
            {
                var msgs = MezuniyetBus.SendMailMezuniyetSinavYerBilgisi(id, srDurumId == SRTalepDurum.Onaylandı);
                if (msgs.Messages.Count > 0)
                {
                    strView = ViewRenderHelper.RenderPartialView("Ajax", "getMessage", msgs);
                }
            }
            return new
            {
                IslemTipListeAdi = qbDrm.DurumAdi,
                qbDrm.ClassName,
                qbDrm.Color,
                FontWeight = fWeight,
                strView
            }.ToJsonResult();
        }
        [Authorize(Roles = RoleNames.MezuniyetGelenBasvurularKayit)]
        public ActionResult SrSinavDurumKaydet(int id, int mezuniyetSinavDurumId, DateTime? tezTeslimSonTarih)
        {
            var mmMessage = new MmMessage
            {
                IsSuccess = false
            };

            var talep = _entities.SRTalepleris.First(p => p.SRTalepID == id);
            if (mezuniyetSinavDurumId == MezuniyetSinavDurum.Uzatma && talep.MezuniyetBasvurulari.SRTalepleris.Any(a => a.SRTalepID != id && a.SRDurumID == SRTalepDurum.Onaylandı && a.MezuniyetSinavDurumID == MezuniyetSinavDurum.Uzatma))
            {
                mmMessage.Messages.Add("Bu mezuniyet başvurusuna daha önceden uzatma hakkı verildiğinden tekrar uzatma hakkı verilemez!");
            }
            else if (talep.JuriSonucMezuniyetSinavDurumID.HasValue && mezuniyetSinavDurumId > MezuniyetSinavDurum.SonucGirilmedi && talep.JuriSonucMezuniyetSinavDurumID != mezuniyetSinavDurumId)
            {
                mmMessage.Messages.Add("Girdiğiniz sınav sonucu jürinin oylama sonucu ile aynı olması gerekmetkedir!");
            }
            if (!mmMessage.Messages.Any())
            {
                if (mezuniyetSinavDurumId == MezuniyetSinavDurum.Basarili)
                {
                    var mbOKriters = talep.MezuniyetBasvurulari.MezuniyetSureci.MezuniyetSureciOgrenimTipKriterleris.First(p => p.OgrenimTipKod == talep.MezuniyetBasvurulari.OgrenimTipKod);
                    var ttEkSureYetki = RoleNames.MezuniyetGelenBasvurularTtEkSure.InRoleCurrent();

                    if (tezTeslimSonTarih.HasValue && !ttEkSureYetki && tezTeslimSonTarih.Value > talep.Tarih.AddDays(mbOKriters.MBTezTeslimSuresiGun))
                    {
                        mmMessage.Messages.Add("Tez teslim son tarih kriteri " + talep.Tarih.AddDays(mbOKriters.MBTezTeslimSuresiGun).ToDateString() + " tarihinden daha büyük olamaz!");
                    }
                }
                else tezTeslimSonTarih = null;
            }
            var strView = "";
            if (mmMessage.Messages.Count == 0)
            {
                bool sendMailSinav = talep.MezuniyetSinavDurumID != mezuniyetSinavDurumId && talep.MezuniyetSinavDurumID.HasValue;


                talep.MezuniyetSinavDurumID = mezuniyetSinavDurumId;
                talep.MezuniyetBasvurulari.MezuniyetSinavDurumID = mezuniyetSinavDurumId;
                talep.MezuniyetBasvurulari.TezTeslimSonTarih = tezTeslimSonTarih;
                talep.MezuniyetSinavDurumIslemTarihi = DateTime.Now;
                talep.MezuniyetBasvurulari.MezuniyetSinavDurumIslemTarihi = DateTime.Now;
                talep.MezuniyetSinavDurumIslemYapanID = UserIdentity.Current.Id;
                talep.MezuniyetBasvurulari.MezuniyetSinavDurumIslemYapanID = UserIdentity.Current.Id;
                _entities.SaveChanges();
                LogIslemleri.LogEkle("MezuniyetBasvurulari", IslemTipi.Update, talep.MezuniyetBasvurulari.ToJson());

                var drm = _entities.MezuniyetSinavDurumlaris.First(p => p.MezuniyetSinavDurumID == mezuniyetSinavDurumId);
                mmMessage.IsSuccess = true;


                if (sendMailSinav && new List<int> { MezuniyetSinavDurum.Basarili, MezuniyetSinavDurum.Uzatma }.Contains(talep.MezuniyetSinavDurumID.Value))
                {
                    mmMessage = MezuniyetBus.SendMailMezuniyetSinavSonucu(id, talep.MezuniyetSinavDurumID.Value);

                }
            }
            mmMessage.Title = "Sınav durumu kayıt işlemi";
            mmMessage.MessageType = mmMessage.IsSuccess ? Msgtype.Success : Msgtype.Warning;
            strView = mmMessage.Messages.Count > 0 ? ViewRenderHelper.RenderPartialView("Ajax", "getMessage", mmMessage) : "";

            return new
            {
                mmMessage.IsSuccess,
                Messages = strView
            }.ToJsonResult();
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult TdDurumKaydet(MezuniyetBasvurulariTezDosyalari kModel)
        {
            var mmMessage = new MmMessage
            {
                IsSuccess = false
            };

            var talep = _entities.MezuniyetBasvurulariTezDosyalaris.First(p => p.MezuniyetBasvurulariTezDosyaID == kModel.MezuniyetBasvurulariTezDosyaID);
            var kYetki = RoleNames.MezuniyetGelenBasvurularKayit.InRoleCurrent() || RoleNames.MezuniyetGelenBasvurularTezKontrol.InRoleCurrent();
            if (!kYetki)
            {
                mmMessage.Messages.Add("Bu işlemi yapmaya yetkili değilsiniz.");
            }
            else if (kModel.IsOnaylandiOrDuzeltme.HasValue)
            {
                if (kModel.IsOnaylandiOrDuzeltme == false && kModel.Aciklama.IsNullOrWhiteSpace()) mmMessage.Messages.Add("Düzeltme talebi için açıklama giriniz.");
            }
            if (mmMessage.Messages.Count == 0)
            {
                try
                {
                    if (kModel.IsOnaylandiOrDuzeltme.HasValue) talep.Aciklama = kModel.Aciklama.IsNullOrWhiteSpace() ? null : kModel.Aciklama.Trim();
                    else talep.Aciklama = null;
                    talep.IsOnaylandiOrDuzeltme = kModel.IsOnaylandiOrDuzeltme;
                    talep.OnayTarihi = DateTime.Now;
                    talep.OnayYapanID = UserIdentity.Current.Id;
                    talep.IslemTarihi = DateTime.Now;
                    talep.IslemYapanID = UserIdentity.Current.Id;
                    talep.IslemYapanIP = UserIdentity.Ip;
                    _entities.SaveChanges();
                    LogIslemleri.LogEkle("MezuniyetBasvurulariTezDosyalari", IslemTipi.Update, talep.ToJson());
                    mmMessage.IsSuccess = true;
                    if (kModel.IsOnaylandiOrDuzeltme == true) mmMessage.Messages.AddRange(MezuniyetBus.SendMailMezuniyetTezSablonKontrol(talep.MezuniyetBasvurulariTezDosyaID, MailSablonTipi.Mez_TezKontrolTezDosyasiBasarili, kModel.Aciklama).Messages);
                    else if (kModel.IsOnaylandiOrDuzeltme == false) mmMessage.Messages.AddRange(MezuniyetBus.SendMailMezuniyetTezSablonKontrol(talep.MezuniyetBasvurulariTezDosyaID, MailSablonTipi.Mez_TezKontrolTezDosyasiOnaylanmadi, kModel.Aciklama).Messages);
                }
                catch (Exception ex)
                {
                    mmMessage.MessageType = Msgtype.Error;
                    mmMessage.IsSuccess = false;
                    mmMessage.Messages.Add("Tez dosyası kontrolü durum bilgisi kayıt edilirken bir hata oluştu! Hata:" + ex.ToExceptionMessage());
                    SistemBilgilendirmeBus.SistemBilgisiKaydet(ex.ToExceptionMessage(), "MezuniyetGelenBasvurular/TdDurumKaydet<br/><br/>" + ex.ToExceptionStackTrace(), LogType.Kritik);
                }
            }
            mmMessage.Title = "Tez Kontrol Durumu Kayıt İşlemi";
            mmMessage.MessageType = mmMessage.IsSuccess ? Msgtype.Success : Msgtype.Warning;
            var strView = mmMessage.Messages.Count > 0 ? ViewRenderHelper.RenderPartialView("Ajax", "getMessage", mmMessage) : "";
            return new
            {
                mmMessage.IsSuccess,
                Messages = strView,
            }.ToJsonResult();
        }
        [Authorize(Roles = RoleNames.MezuniyetGelenBasvurularKayit)]
        public ActionResult MezuniyetDurumKaydet(int id, bool? isMezunOldu, DateTime? tarih)
        {
            var mmMessage = new MmMessage
            {
                IsSuccess = false,
                Title = "Mezuniyet durumu kayıt işlemi",
                MessageType = Msgtype.Warning
            };

            var talep = _entities.MezuniyetBasvurularis.First(p => p.MezuniyetBasvurulariID == id);

            if (isMezunOldu == true && tarih.HasValue == false)
            {
                mmMessage.Messages.Add("Mezuniyet Tarihi giriniz.");
            }
            else if (isMezunOldu == true)
            {
                var sonAlinanSr = talep.SRTalepleris.OrderByDescending(o => o.SRTalepID).First();

                if (sonAlinanSr.SRTalepleriBezCiltFormus.Any() == false)
                {
                    mmMessage.Messages.Add("Öğrencinin mezun olabilmesi için Tez Teslim formunun oluşturulması gerekmektedir.");

                }
                else
                {
                    //Düzenlenecek 
                    //var TezTeslimSonTarihi = SonAlinanSr.Tarih.AddDays(talep.MezuniyetSureci.TezTeslimSuresiGun);
                    //if (Tarih > TezTeslimSonTarihi)
                    //{
                    //    mmMessage.Messages.Add("Mezuniyeti onaylanacak öğrencinin mezuniyet tarihi tez teslim tarihinden büyük olamaz! Tez teslim son tarih:" + TezTeslimSonTarihi.ToString("dd.MM.yyyy"));
                    //}
                }


            }
            if (mmMessage.Messages.Count == 0)
            {
                if (isMezunOldu != true) tarih = null;
                if (talep.IsMezunOldu != isMezunOldu)
                {
                    talep.IsMezunOldu = isMezunOldu;
                    talep.MezuniyetTarihi = tarih;
                    var kul = talep.Kullanicilar;


                    if (talep.ProgramKod == kul.ProgramKod && talep.OgrenimTipKod == kul.OgrenimTipKod) kul.OgrenimDurumID = talep.IsMezunOldu == true ? OgrenimDurum.Mezun : OgrenimDurum.HalenOğrenci;


                    _entities.SaveChanges();
                    LogIslemleri.LogEkle("MezuniyetBasvurulari", IslemTipi.Update, talep.ToJson());
                }
                mmMessage.IsSuccess = true;
            }
            var strView = ViewRenderHelper.RenderPartialView("Ajax", "getMessage", mmMessage);

            return new
            {
                mmMessage.IsSuccess,
                Messages = strView
            }.ToJsonResult();
        }
        [Authorize(Roles = RoleNames.MezuniyetGelenBasvurularKayit)]
        public ActionResult EykTarihiKaydet(int id, DateTime? eykTarihi)
        {
            var mmMessage = new MmMessage
            {
                Title = "EYK Tarihi Güncelleme İşlemi"
            };
            var mb = _entities.MezuniyetBasvurularis.FirstOrDefault(p => p.MezuniyetBasvurulariID == id);

            if (mb != null)
            {
                DateTime? ilkSrMaxTarih = mb.EYKTarihi;
                if (eykTarihi != null) ilkSrMaxTarih = eykTarihi;
                if (ilkSrMaxTarih != null)
                {
                    var ilkSrTalep = mb.SRTalepleris.OrderBy(o => o.SRTalepID).FirstOrDefault();
                    if (ilkSrTalep != null)
                    {
                        var maxT = ilkSrTalep.Tarih;
                        if (maxT < eykTarihi)
                        {
                            mmMessage.Messages.Add("Eyk tarihi öğrencinin almış olduğu ilk salon rezervasyonu tarihi için uygun değildir.");
                            mmMessage.Messages.Add("İlk salon rezervasyonu '" + ilkSrTalep.Tarih.ToDateString() + "' tarihinde alınmıştır.");
                            mmMessage.Messages.Add("Belirlenen kurallara göre EYK tarihi en son '" + maxT.ToDateString() + "' tarihi olabilir.");
                            mmMessage.IsSuccess = false;
                        }
                    }
                }

                if (mmMessage.Messages.Count == 0)
                {

                    mb.EYKTarihi = eykTarihi;
                    _entities.SaveChanges();
                    LogIslemleri.LogEkle("MezuniyetBasvurulari", IslemTipi.Update, mb.ToJson());
                    mmMessage.Messages.Add("Eyk Tarihi Güncellendi");
                    mmMessage.IsSuccess = true;
                }
            }
            else
            {
                mmMessage.Messages.Add("İşlem yapmaya çalıştığınız mezuniyet başvurusu sistemde bulunamadı!");
                mmMessage.IsSuccess = false;
            }
            mmMessage.MessageType = mmMessage.IsSuccess ? Msgtype.Success : Msgtype.Error;
            return new
            {
                mmMessage.IsSuccess,
                MmMessage = mmMessage,
            }.ToJsonResult();
        }
        [Authorize(Roles = RoleNames.MezuniyetGelenBasvurularKayit)]
        public ActionResult SinavTarihiKaydet(int id, string sinavTarihi)
        {
            var mmMessage = new MmMessage
            {
                Title = "Sınav Tarihi Güncelleme İşlemi"
            };
            var srTalep = _entities.SRTalepleris.First(p => p.SRTalepID == id);
            var mb = srTalep.MezuniyetBasvurulari;
            DateTime? tarih = Convert.ToDateTime(sinavTarihi);
            if (!tarih.HasValue)
            {
                mmMessage.Messages.Add("Sınav Tarihi Giriniz.");
            }
            else
            {
                var otBilgiTarihBilgi = mb.MezuniyetSureci.MezuniyetSureciOgrenimTipKriterleris.First(p => p.OgrenimTipKod == mb.OgrenimTipKod);
                if (srTalep.MezuniyetSinavDurumID == MezuniyetSinavDurum.Uzatma || srTalep.JuriSonucMezuniyetSinavDurumID == MezuniyetSinavDurum.Uzatma && srTalep.SRDurumID == SRTalepDurum.Onaylandı)
                {
                    var uzatmaOncesiSrTalebi = mb.SRTalepleris.Where(p => p.MezuniyetSinavDurumID != MezuniyetSinavDurum.Uzatma && p.SRDurumID == SRTalepDurum.Onaylandı).OrderByDescending(o => o.SRTalepID).FirstOrDefault();
                    var uzatmaOncesiSrAlabilmeTarihi = uzatmaOncesiSrTalebi.Tarih.AddDays(otBilgiTarihBilgi.MBSinavUzatmaSuresiGun);
                    if (tarih.Value.Date > uzatmaOncesiSrAlabilmeTarihi)
                    {
                        mmMessage.Messages.Add("Mezuniyet sınavı sonucunda almış olduğunuz uzatma işlemi sonrası salon rezervasyonu işemi son tarihi olan '" + uzatmaOncesiSrAlabilmeTarihi.ToFormatDate() + "' tarihini aşamazsınız.");
                    }
                }
                else
                {
                    var srBaslangicTarih = mb.EYKTarihi.Value.AddDays(otBilgiTarihBilgi.MBSRTalebiKacGunSonraAlabilir);
                    if (tarih.Value.Date < srBaslangicTarih.Date)
                    {
                        mmMessage.Messages.Add("Talep tarihi " + srBaslangicTarih.Date.ToString("yyyy-MM-dd") + " tarihinden küçük olamaz!");
                    }
                }
            }
            if (mmMessage.Messages.Count == 0)
            {

                srTalep.Tarih = tarih.Value;
                srTalep.BasSaat = new TimeSpan(tarih.Value.Hour, tarih.Value.Minute, 0);
                srTalep.BitSaat = new TimeSpan(tarih.Value.Hour + 2, tarih.Value.Minute, 0);

                _entities.SaveChanges();
                LogIslemleri.LogEkle("SRTalepleri", IslemTipi.Update, srTalep.ToJson());
                mmMessage.Messages.Add("Sınav Tarihi Güncellendi");
                mmMessage.IsSuccess = true;
            }
            mmMessage.MessageType = mmMessage.IsSuccess ? Msgtype.Success : Msgtype.Error;
            return new
            {
                mmMessage.IsSuccess,
                MmMessage = mmMessage,
            }.ToJsonResult();
        }
        [Authorize(Roles = RoleNames.MezuniyetGelenBasvurularKayit)]
        public ActionResult TezTeslimSonTarihiKaydet(int id, string tezTeslimSonTarih)
        {
            var mmMessage = new MmMessage
            {
                Title = "Tez Teslim Son Tarih Kriteri Güncelleme İşlemi"
            };
            var srTalep = _entities.SRTalepleris.First(p => p.SRTalepID == id);
            var mb = srTalep.MezuniyetBasvurulari;
            DateTime? tarih = tezTeslimSonTarih.ToDate();
            if (!tarih.HasValue)
            {
                mmMessage.Messages.Add("Sınav Tarihi Giriniz.");
            }
            else
            {

            }
            if (mmMessage.Messages.Count == 0)
            {
                mb.TezTeslimSonTarih = tarih.Value;

                _entities.SaveChanges();
                LogIslemleri.LogEkle("MezuniyetBasvurulari", IslemTipi.Update, mb.ToJson());
                mmMessage.Messages.Add("Tez Teslim Son Tarih Kriteri Güncellendi");
                mmMessage.IsSuccess = true;
            }
            mmMessage.MessageType = mmMessage.IsSuccess ? Msgtype.Success : Msgtype.Error;
            return new
            {
                mmMessage.IsSuccess,
                MmMessage = mmMessage,
            }.ToJsonResult();
        }

        public ActionResult GetJuriOneriFormu(int mezuniyetBasvurulariId)
        {
            var mb = _entities.MezuniyetBasvurularis.First(p => p.MezuniyetBasvurulariID == mezuniyetBasvurulariId);
            var cmbUnvanList = UnvanlarBus.GetCmbJuriUnvanlar(true);
            var cmbUniversiteList = Management.cmbGetAktifUniversiteler(true);

            var model = new MezuniyetJuriOneriFormuKayitDto
            {
                MezuniyetBasvurulariID = mezuniyetBasvurulariId,
                IsTezDiliTr = mb.IsTezDiliTr == true,
                TezBaslikTr = mb.TezBaslikTr,
                TezBaslikEn = mb.TezBaslikEn,
                Danisman = _entities.Kullanicilars.First(p => p.KullaniciID == mb.TezDanismanID),
                SListUnvanAdi = new SelectList(cmbUnvanList, "Value", "Caption"),
                SListUniversiteID = new SelectList(cmbUniversiteList, "Value", "Caption"),
                IsDoktoraOrYL = mb.OgrenimTipKod.IsDoktora(),
            };

            var mMessage = new MmMessage
            {
                MessageType = Msgtype.Success,
                IsSuccess = true
            };
            string view = "";
            var mbjo = mb.MezuniyetJuriOneriFormlaris.FirstOrDefault();
            var ogrenciInfo = KullanicilarBus.StudentControl(mb.TcKimlikNo);

            if (!RoleNames.MezuniyetGelenBasvurularJuriOneriFormuKayit.InRoleCurrent())
            {
                mMessage.MessageType = Msgtype.Warning;
                mMessage.Messages.Add("Jüri öneri formu kayıt işlemi için yetkili değilsiniz.");
            }
            else if (!RoleNames.MezuniyetGelenBasvurularKayit.InRoleCurrent() && mb.TezDanismanID != UserIdentity.Current.Id)
            {
                mMessage.MessageType = Msgtype.Warning;
                mMessage.Messages.Add("Bu mezuniyet başvurusu için danışman olarak belirlenmediğiniz için jüri öneri formu oluşturamazsınız.");
            }
            if (mMessage.Messages.Count == 0 && mbjo == null)
            {
                if (ogrenciInfo.Hata)
                {
                    mMessage.Messages.Add("Obs sisteminden öğrenci bilgisi sorgulanırken bir hata oluştu!");
                }
                else
                {
                    if (ogrenciInfo.OgrenciInfo.DANISMAN_AD_SOYAD1.IsNullOrWhiteSpace())
                        mMessage.Messages.Add("Danışman Bilgisi Çekilemedi.");

                    if (model.IsDoktoraOrYL)
                    {
                        if (ogrenciInfo.TezIzlJuriBilgileri.Count == 1)
                        {
                            mMessage.Messages.Add("'1.Tik Üyesi' Bilgisi Çekilemedi.");
                        }
                        if (ogrenciInfo.TezIzlJuriBilgileri.Count == 2)
                            mMessage.Messages.Add("'2.Tik Üyesi' Bilgisi Çekilemedi.");
                    }
                    if (mMessage.Messages.Count > 0)
                    {
                        mMessage.MessageType = Msgtype.Warning;
                        mMessage.Messages.Add("Jüri öneri formunu oluşturabilmeniz için bu durumu enstitü yetkililerine iletiniz.");
                    }
                }

            }
            if (mMessage.Messages.Count == 0)
            {
                model.OgrenciAdSoyad = mb.Ad + " " + mb.Soyad + " - " + mb.OgrenciNo;
                model.OgrenciAnabilimdaliProgramAdi = mb.Programlar.AnabilimDallari.AnabilimDaliAdi + " - " + mb.Programlar.ProgramAdi;
                model.MezuniyetJuriOneriFormID = mbjo?.MezuniyetJuriOneriFormID ?? 0;

                if (mbjo != null)
                {
                    model.MezuniyetJuriOneriFormID = mbjo.MezuniyetJuriOneriFormID;
                    model.YeniTezBaslikTr = mbjo.YeniTezBaslikTr;
                    model.YeniTezBaslikEn = mbjo.YeniTezBaslikEn;
                    model.IsTezBasligiDegisti = mbjo.IsTezBasligiDegisti;
                    model.JoFormJuriList = mbjo.MezuniyetJuriOneriFormuJurileris.Select(s => new KrMezuniyetJuriOneriFormuJurileri
                    {
                        MezuniyetJuriOneriFormID = s.MezuniyetJuriOneriFormID,
                        MezuniyetJuriOneriFormuJuriID = s.MezuniyetJuriOneriFormuJuriID,
                        JuriTipAdi = s.JuriTipAdi,
                        UnvanAdi = s.UnvanAdi,
                        SlistUnvanAdi = new SelectList(cmbUnvanList, "Value", "Caption", s.UnvanAdi),
                        AdSoyad = s.AdSoyad,
                        EMail = s.EMail,
                        UniversiteID = s.UniversiteID,
                        SListUniversiteID = new SelectList(cmbUniversiteList, "Value", "Caption", s.UniversiteID),
                        UniversiteAdi = s.UniversiteAdi,
                        AnabilimdaliProgramAdi = s.AnabilimdaliProgramAdi,
                        UzmanlikAlani = s.UzmanlikAlani,
                        BilimselCalismalarAnahtarSozcukler = s.BilimselCalismalarAnahtarSozcukler,
                        DilSinavAdi = s.DilSinavAdi,
                        DilPuani = s.DilPuani,
                        IsAsilOrYedek = s.IsAsilOrYedek
                    }).ToList();
                    if (!ogrenciInfo.Hata)
                    {
                        if (!ogrenciInfo.OgrenciInfo.DANISMAN_AD_SOYAD1.IsNullOrWhiteSpace())
                        {
                            var tD = model.JoFormJuriList.First(p => p.JuriTipAdi == "TezDanismani");
                            tD.SListUniversiteID = new SelectList(cmbUniversiteList, "Value", "Caption", tD.UniversiteID);
                            tD.SlistUnvanAdi = new SelectList(cmbUnvanList, "Value", "Caption", tD.UnvanAdi);
                            if (tD.AdSoyad.ToUpper().Trim() != ogrenciInfo.OgrenciInfo.DANISMAN_AD_SOYAD1.ToUpper().ToUpper().Trim() || tD.UnvanAdi.ToUpper().Trim() != ogrenciInfo.OgrenciInfo.DANISMAN_UNVAN1.ToJuriUnvanAdi())
                            {
                                tD.AdSoyad = ogrenciInfo.OgrenciInfo.DANISMAN_AD_SOYAD1.ToUpper();
                                tD.UnvanAdi = ogrenciInfo.OgrenciInfo.DANISMAN_UNVAN1.ToJuriUnvanAdi();
                            }
                        }

                        var tiks = ogrenciInfo.TezIzlJuriBilgileri.Where(p => p.TEZ_DANISMAN != "1").ToList();
                        if (model.IsDoktoraOrYL && tiks.Count >= 2)
                        {
                            var obsTik1 = tiks[0];
                            var obsTik2 = tiks[1];

                            var varOlanTik1 = model.JoFormJuriList.First(p => p.JuriTipAdi == "TikUyesi1");
                            varOlanTik1.SListUniversiteID =
                                new SelectList(cmbUniversiteList, "Value", "Caption", varOlanTik1.UniversiteID);
                            varOlanTik1.SlistUnvanAdi = new SelectList(cmbUnvanList, "Value", "Caption", varOlanTik1.UnvanAdi);
                            if (varOlanTik1.AdSoyad.ToUpper() != obsTik1.TEZ_IZLEME_JURI_ADSOY.ToUpper() ||
                                varOlanTik1.UnvanAdi.ToUpper().Trim() != obsTik1.TEZ_IZLEME_JURI_UNVAN.ToJuriUnvanAdi())
                            {
                                varOlanTik1.AdSoyad = obsTik1.TEZ_IZLEME_JURI_ADSOY.ToUpper();
                                varOlanTik1.UnvanAdi = obsTik1.TEZ_IZLEME_JURI_UNVAN.ToJuriUnvanAdi();
                            }
                            var varOlanTik2 = model.JoFormJuriList.First(p => p.JuriTipAdi == "TikUyesi2");
                            varOlanTik2.SListUniversiteID =
                                new SelectList(cmbUniversiteList, "Value", "Caption", varOlanTik2.UniversiteID);
                            varOlanTik2.SlistUnvanAdi = new SelectList(cmbUnvanList, "Value", "Caption", varOlanTik2.UnvanAdi);
                            if (varOlanTik2.AdSoyad.ToUpper() != obsTik2.TEZ_IZLEME_JURI_ADSOY.ToUpper() ||
                                varOlanTik2.UnvanAdi.ToUpper().Trim() != obsTik2.TEZ_IZLEME_JURI_UNVAN.ToJuriUnvanAdi())
                            {
                                varOlanTik2.AdSoyad = obsTik2.TEZ_IZLEME_JURI_ADSOY.ToUpper();
                                varOlanTik2.UnvanAdi = obsTik2.TEZ_IZLEME_JURI_UNVAN.ToJuriUnvanAdi();

                            } 
                        }
                    }

                }
                else
                {

                    if (!ogrenciInfo.Hata)
                    {
                        if (!ogrenciInfo.OgrenciInfo.DANISMAN_AD_SOYAD1.IsNullOrWhiteSpace())
                        {
                            var tdBilgi = new KrMezuniyetJuriOneriFormuJurileri
                            {
                                JuriTipAdi = "TezDanismani",
                                UnvanAdi = ogrenciInfo.OgrenciInfo.DANISMAN_UNVAN1.ToJuriUnvanAdi(),
                                AdSoyad = ogrenciInfo.OgrenciInfo.DANISMAN_AD_SOYAD1.ToUpper(),

                            };
                            tdBilgi.SlistUnvanAdi = new SelectList(cmbUnvanList, "Value", "Caption", tdBilgi.UnvanAdi);
                            tdBilgi.SListUniversiteID = new SelectList(cmbUniversiteList, "Value", "Caption", tdBilgi.UniversiteID);
                            model.JoFormJuriList.Add(tdBilgi);
                        }
                        else
                        {
                            model.JoFormJuriList.Add(new KrMezuniyetJuriOneriFormuJurileri { JuriTipAdi = "TezDanismani", SlistUnvanAdi = new SelectList(cmbUnvanList, "Value", "Caption"), SListUniversiteID = new SelectList(cmbUniversiteList, "Value", "Caption") });
                        }

                        var tiks = ogrenciInfo.TezIzlJuriBilgileri.Where(p => p.TEZ_DANISMAN != "1").ToList();
                        if (model.IsDoktoraOrYL && tiks.Count >= 2)
                        {


                            var obsTik1 = tiks[0];
                            var obsTik2 = tiks[1];

                            var tk1Bilgi = new KrMezuniyetJuriOneriFormuJurileri
                            {
                                JuriTipAdi = "TikUyesi1",
                                AdSoyad = obsTik1.TEZ_IZLEME_JURI_ADSOY,
                                UnvanAdi = obsTik1.TEZ_IZLEME_JURI_UNVAN.ToJuriUnvanAdi()
                            };
                            tk1Bilgi.SlistUnvanAdi =
                                new SelectList(cmbUnvanList, "Value", "Caption", tk1Bilgi.UnvanAdi);
                            tk1Bilgi.SListUniversiteID = new SelectList(cmbUniversiteList, "Value", "Caption",
                                tk1Bilgi.UniversiteID);
                            model.JoFormJuriList.Add(tk1Bilgi);



                            var tk2Bilgi = new KrMezuniyetJuriOneriFormuJurileri
                            {
                                JuriTipAdi = "TikUyesi2",
                                AdSoyad = obsTik2.TEZ_IZLEME_JURI_ADSOY,
                                UnvanAdi = obsTik2.TEZ_IZLEME_JURI_UNVAN.ToJuriUnvanAdi()
                            };
                            tk2Bilgi.SlistUnvanAdi =
                                new SelectList(cmbUnvanList, "Value", "Caption", tk2Bilgi.UnvanAdi);
                            tk2Bilgi.SListUniversiteID = new SelectList(cmbUniversiteList, "Value", "Caption",
                                tk2Bilgi.UniversiteID);
                            model.JoFormJuriList.Add(tk2Bilgi);

                        }
                    }
                }


                view = ViewRenderHelper.RenderPartialView("MezuniyetGelenBasvurular", "JuriOneriFormu", model);
            }
            else { mMessage.IsSuccess = false; mMessage.MessageType = Msgtype.Warning; }
            var strView = ViewRenderHelper.RenderPartialView("Ajax", "getMessage", mMessage);

            return new
            {
                mMessage.IsSuccess,
                Content = view,
                Messages = strView
            }.ToJsonResult();
        }
        [ValidateInput(false)]
        public ActionResult JuriOneriFormuPost(MezuniyetJuriOneriFormuKayitDto kModel, string postDetayTabAdi = "", bool saveData = false)
        {
            var mMessage = new MmMessage
            {
                MessageType = Msgtype.Success,
                IsSuccess = true
            };
            string selectedAnaTabAdi = "";
            string selectedDetayTabAdi = "";
            bool isYeniJo = true;

            var mb = _entities.MezuniyetBasvurularis.First(p => p.MezuniyetBasvurulariID == kModel.MezuniyetBasvurulariID);
            if (!RoleNames.MezuniyetGelenBasvurularJuriOneriFormuKayit.InRoleCurrent())
            {
                mMessage.Messages.Add("Jür öneri formu kayıt işlemi için yetkili değilsiniz.");
            }
            else if (!RoleNames.MezuniyetGelenBasvurularKayit.InRoleCurrent() && mb.TezDanismanID != UserIdentity.Current.Id)
            {
                mMessage.IsSuccess = false;
                mMessage.MessageType = Msgtype.Warning;
                mMessage.Messages.Add("Bu mezuniyet başvurusu için danışman olarak belirlenmediğiniz için jüri öneri formu oluşturamazsınız.");
            }
            else
            {
                var mbjo = mb.MezuniyetJuriOneriFormlaris.FirstOrDefault();

                bool isDegisiklikVar = false;
                if (mbjo != null)
                {
                    isYeniJo = false;
                    if (mbjo.EYKDaOnaylandi == true)
                        mMessage.Messages.Add("Jüri öneri formunuzun EYK'da onaylandığından Form üzerinden herhangi bir değişiklik yapamazsınız!");
                    else if (mbjo.EYKYaGonderildi == true)
                        mMessage.Messages.Add("Jüri öneri formunuzun EYK'ya gönderimi yapıldığından Form üzerinden herhangi bir değişiklik yapamazsınız!");

                    if (kModel.IsTezBasligiDegisti != mbjo.IsTezBasligiDegisti) isDegisiklikVar = true;
                    else if (kModel.IsTezBasligiDegisti == true && (kModel.YeniTezBaslikTr.ToUpper() != mbjo.YeniTezBaslikEn.ToUpper() || kModel.YeniTezBaslikTr.ToUpper() != mbjo.YeniTezBaslikTr.ToUpper())) isDegisiklikVar = true;
                }
                if (mMessage.Messages.Count == 0)
                {
                    if (!kModel.IsTezBasligiDegisti.HasValue)
                    {
                        mMessage.Messages.Add("Sınavda Tez Başlığı Değişecek Mi? Sorusunu cevaplayınız.");
                    }
                    else
                    {
                        if (kModel.IsTezBasligiDegisti == true)
                        {
                            if (kModel.YeniTezBaslikTr.IsNullOrWhiteSpace())
                            {
                                mMessage.Messages.Add("Yeni Tez Başlığı Türkçe bilgisini giriniz.");
                            }
                            mMessage.MessagesDialog.Add(new MrMessage { MessageType = (!kModel.YeniTezBaslikTr.IsNullOrWhiteSpace() ? Msgtype.Success : Msgtype.Warning), PropertyName = "YeniTezBaslikTr" });
                            if (kModel.YeniTezBaslikEn.IsNullOrWhiteSpace())
                            {
                                mMessage.Messages.Add("Yeni Tez Başlığı İngilizce bilgisini giriniz.");
                            }
                            mMessage.MessagesDialog.Add(new MrMessage { MessageType = (!kModel.YeniTezBaslikEn.IsNullOrWhiteSpace() ? Msgtype.Success : Msgtype.Warning), PropertyName = "YeniTezBaslikEn" });
                        }
                    }
                    mMessage.MessagesDialog.Add(new MrMessage { MessageType = (kModel.IsTezBasligiDegisti.HasValue ? Msgtype.Success : Msgtype.Warning), PropertyName = "IsTezBasligiDegisti" });
                }
                if (mMessage.Messages.Count > 0)
                {
                    selectedAnaTabAdi = postDetayTabAdi;
                    selectedDetayTabAdi = postDetayTabAdi;
                }
                if (mMessage.Messages.Count == 0)
                {
                    var anaTabAdis = kModel.AnaTabAdi.Select((s, i) => new { AnaTabAdi = s, Inx = (i + 1) }).ToList();
                    var detayTabAdis = kModel.DetayTabAdi.Select((s, i) => new { DetayTabAdi = s, Inx = (i + 1) }).ToList();
                    var juriTipAdis = kModel.JuriTipAdi.Select((s, i) => new { JuriTipAdi = s, Inx = (i + 1) }).ToList();
                    var adSoyads = kModel.AdSoyad.Select((s, i) => new { AdSoyad = s, Inx = (i + 1) }).ToList();
                    var unvanAdis = kModel.UnvanAdi.Select((s, i) => new { UnvanAdi = s, Inx = (i + 1) }).ToList();
                    var eMails = kModel.EMail.Select((s, i) => new { EMail = s.Trim(), Inx = (i + 1) }).ToList();
                    var universiteIDs = kModel.UniversiteID.Select((s, i) => new { UniversiteID = s, Inx = (i + 1) }).ToList();
                    var anabilimdaliProgramAdis = kModel.AnabilimdaliProgramAdi.Select((s, i) => new { AnabilimdaliProgramAdi = s, Inx = (i + 1) }).ToList();
                    var uzmanlikAlanis = kModel.UzmanlikAlani.Select((s, i) => new { UzmanlikAlani = s, Inx = (i + 1) }).ToList();
                    var bilimselCalismalarAnahtarSozcuklers = kModel.BilimselCalismalarAnahtarSozcukler.Select((s, i) => new { BilimselCalismalarAnahtarSozcukler = s, Inx = (i + 1) }).ToList();
                    var dilSinavAdis = kModel.DilSinavAdi.Select((s, i) => new { DilSinavAdi = s, Inx = (i + 1) }).ToList();
                    var dilPuanis = kModel.DilPuani.Select((s, i) => new { DilPuani = s, Inx = (i + 1) }).ToList();


                    var qData = (from ad in adSoyads
                                 join at in anaTabAdis on ad.Inx equals at.Inx
                                 join dt in detayTabAdis on ad.Inx equals dt.Inx
                                 join jt in juriTipAdis on ad.Inx equals jt.Inx
                                 join un in unvanAdis on ad.Inx equals un.Inx
                                 join em in eMails on ad.Inx equals em.Inx
                                 join uni in universiteIDs on ad.Inx equals uni.Inx
                                 join abd in anabilimdaliProgramAdis on ad.Inx equals abd.Inx
                                 join ua in uzmanlikAlanis on ad.Inx equals ua.Inx
                                 join bc in bilimselCalismalarAnahtarSozcuklers on ad.Inx equals bc.Inx
                                 join ds in dilSinavAdis on ad.Inx equals ds.Inx
                                 join dp in dilPuanis on ad.Inx equals dp.Inx

                                 select new
                                 {
                                     ad.Inx,
                                     at.AnaTabAdi,
                                     dt.DetayTabAdi,
                                     jt.JuriTipAdi,
                                     ad.AdSoyad,
                                     AdSoyadSuccess = !ad.AdSoyad.IsNullOrWhiteSpace(),
                                     un.UnvanAdi,
                                     UnvanAdiSuccess = !un.UnvanAdi.IsNullOrWhiteSpace(),
                                     em.EMail,
                                     EMailSuccess = !em.EMail.IsNullOrWhiteSpace() && !em.EMail.ToIsValidEmail(),
                                     uni.UniversiteID,
                                     UniversiteIDSuccess = uni.UniversiteID.HasValue,
                                     abd.AnabilimdaliProgramAdi,
                                     AnabilimdaliProgramAdiSuccess = !abd.AnabilimdaliProgramAdi.IsNullOrWhiteSpace(),
                                     ua.UzmanlikAlani,
                                     UzmanlikAlaniSuccess = !ua.UzmanlikAlani.IsNullOrWhiteSpace(),
                                     bc.BilimselCalismalarAnahtarSozcukler,
                                     BilimselCalismalarAnahtarSozcuklerSuccess = !bc.BilimselCalismalarAnahtarSozcukler.IsNullOrWhiteSpace(),
                                     ds.DilSinavAdi,
                                     DilSinavAdiSuccess = kModel.IsTezDiliTr || !ds.DilSinavAdi.IsNullOrWhiteSpace(),
                                     dp.DilPuani,
                                     DilPuaniSuccess = kModel.IsTezDiliTr || !dp.DilPuani.IsNullOrWhiteSpace(),

                                 }).ToList();

                    var qGroup = (from s in qData
                                  group new { s } by new
                                  {
                                      s.Inx,
                                      s.AnaTabAdi,
                                      s.DetayTabAdi,
                                      s.JuriTipAdi,
                                      s.AdSoyadSuccess,
                                      s.UnvanAdiSuccess,
                                      s.EMailSuccess,
                                      s.UniversiteIDSuccess,
                                      s.UzmanlikAlaniSuccess,
                                      s.BilimselCalismalarAnahtarSozcuklerSuccess,
                                      s.DilSinavAdiSuccess,
                                      s.DilPuaniSuccess,
                                      IsSuccessRow = s.JuriTipAdi.ToJoFormSuccessRow(kModel.IsTezDiliTr, s.AdSoyadSuccess, s.UnvanAdiSuccess, s.EMailSuccess, s.UniversiteIDSuccess, s.UzmanlikAlaniSuccess, s.BilimselCalismalarAnahtarSozcuklerSuccess, s.DilSinavAdiSuccess, s.DilPuaniSuccess)
                                  }
                into g1
                                  select new
                                  {
                                      g1.Key.AnaTabAdi,
                                      g1.Key.DetayTabAdi,
                                      nextDetayTabAdi = qData.Where(p => p.Inx > g1.Key.Inx).Select(s2 => s2.DetayTabAdi).FirstOrDefault(),
                                      nextAnaTabAdi = qData.Where(p => p.Inx > g1.Key.Inx).Select(s2 => s2.AnaTabAdi).FirstOrDefault(),
                                      g1.Key.JuriTipAdi,
                                      g1.Key.IsSuccessRow,
                                      DetayData = g1.ToList()
                                  }).Where(p => (saveData || p.DetayTabAdi == postDetayTabAdi)).ToList();
                    foreach (var item in qGroup.Where(p => p.JuriTipAdi != (mb.OgrenimTipKod.IsDoktora() ? "TikUyesi" : "")))
                    {

                        if (!item.IsSuccessRow)
                        {
                            if (item.JuriTipAdi == "TezDanismani") mMessage.Messages.Add("Danışman bilgilerinde eksik ya da hatalı veri girişleri mevcut!");
                            else if (item.JuriTipAdi == "TikUyesi1") mMessage.Messages.Add("Tik üyesi 1 bilgilerinde eksik ya da hatalı veri girişleri mevcut!");
                            else if (item.JuriTipAdi == "TikUyesi2") mMessage.Messages.Add("Tik üyesi 2 bilgilerinde eksik ya da hatalı veri girişleri mevcut!");
                            else if (item.JuriTipAdi == "YtuIciJuri1") mMessage.Messages.Add("YTÜ içi Jüri 1 bilgilerinde eksik ya da hatalı veri girişleri mevcut!");
                            else if (item.JuriTipAdi == "YtuIciJuri2") mMessage.Messages.Add("YTÜ içi Jüri 2 bilgilerinde eksik ya da hatalı veri girişleri mevcut!");
                            else if (item.JuriTipAdi == "YtuIciJuri3") mMessage.Messages.Add("YTÜ içi Jüri 3 bilgilerinde eksik ya da hatalı veri girişleri mevcut!");
                            else if (item.JuriTipAdi == "YtuIciJuri4") mMessage.Messages.Add("YTÜ içi Jüri 4 bilgilerinde eksik ya da hatalı veri girişleri mevcut!");
                            else if (item.JuriTipAdi == "YtuDisiJuri1") mMessage.Messages.Add("YTÜ dışı Jüri 1 bilgilerinde eksik ya da hatalı veri girişleri mevcut!");
                            else if (item.JuriTipAdi == "YtuDisiJuri2") mMessage.Messages.Add("YTÜ dışı Jüri 2 bilgilerinde eksik ya da hatalı veri girişleri mevcut!");
                            else if (item.JuriTipAdi == "YtuDisiJuri3") mMessage.Messages.Add("YTÜ dışı Jüri 3 bilgilerinde eksik ya da hatalı veri girişleri mevcut!");
                            else if (item.JuriTipAdi == "YtuDisiJuri4") mMessage.Messages.Add("YTÜ dışı Jüri 4 bilgilerinde eksik ya da hatalı veri girişleri mevcut!");
                            if (mMessage.Messages.Count > 0 && selectedAnaTabAdi == "")
                            {
                                selectedAnaTabAdi = item.AnaTabAdi;
                                selectedDetayTabAdi = item.DetayTabAdi;
                            }
                        }
                        else if (saveData == false)
                        {
                            selectedAnaTabAdi = item.nextAnaTabAdi;
                            selectedDetayTabAdi = item.nextDetayTabAdi;
                        }
                        foreach (var item2 in item.DetayData)
                        {
                            mMessage.MessagesDialog.Add(new MrMessage { MessageType = (item2.s.AdSoyadSuccess ? Msgtype.Success : Msgtype.Warning), PropertyName = item2.s.JuriTipAdi + "AdSoyad" });
                            mMessage.MessagesDialog.Add(new MrMessage { MessageType = (item2.s.UnvanAdiSuccess ? Msgtype.Success : Msgtype.Warning), PropertyName = item2.s.JuriTipAdi + "UnvanAdi" });
                            mMessage.MessagesDialog.Add(new MrMessage { MessageType = (item2.s.EMailSuccess ? Msgtype.Success : Msgtype.Warning), PropertyName = item2.s.JuriTipAdi + "EMail" });
                            mMessage.MessagesDialog.Add(new MrMessage { MessageType = (item2.s.UniversiteIDSuccess ? Msgtype.Success : Msgtype.Warning), PropertyName = item2.s.JuriTipAdi + "UniversiteID" });
                            mMessage.MessagesDialog.Add(new MrMessage { MessageType = (item2.s.AnabilimdaliProgramAdiSuccess ? Msgtype.Success : Msgtype.Warning), PropertyName = item2.s.JuriTipAdi + "AnabilimdaliProgramAdi" });
                            mMessage.MessagesDialog.Add(new MrMessage { MessageType = (item2.s.UzmanlikAlaniSuccess ? Msgtype.Success : Msgtype.Warning), PropertyName = item2.s.JuriTipAdi + "UzmanlikAlani" });
                            mMessage.MessagesDialog.Add(new MrMessage { MessageType = (item2.s.BilimselCalismalarAnahtarSozcuklerSuccess ? Msgtype.Success : Msgtype.Warning), PropertyName = item2.s.JuriTipAdi + "BilimselCalismalarAnahtarSozcukler" });
                            if (kModel.IsTezDiliTr == false)
                            {
                                mMessage.MessagesDialog.Add(new MrMessage { MessageType = (item2.s.DilSinavAdiSuccess ? Msgtype.Success : Msgtype.Warning), PropertyName = item2.s.JuriTipAdi + "DilSinavAdi" });
                                mMessage.MessagesDialog.Add(new MrMessage { MessageType = (item2.s.DilPuaniSuccess ? Msgtype.Success : Msgtype.Warning), PropertyName = item2.s.JuriTipAdi + "DilPuani" });
                            }
                        }

                    }
                    if (mMessage.Messages.Count == 0 && saveData)
                    {
                        mbjo = isYeniJo ? new MezuniyetJuriOneriFormlari() : mbjo;
                        var unilers = _entities.Universitelers.ToList();
                        //doktora öğrenim tipindeki başvurular için tik üyesi haricindeki bilgiler alınsın
                        var kData = qData.Where(p => p.JuriTipAdi != (mb.OgrenimTipKod.IsDoktora() ? "TikUyesi" : "")).ToList();
                        foreach (var item in kData)
                        {
                            var rw = mbjo.MezuniyetJuriOneriFormuJurileris.FirstOrDefault(p => p.JuriTipAdi == item.JuriTipAdi);
                            if (rw != null)
                            {
                                if (item.AdSoyad.IsNullOrWhiteSpace() == false)
                                {
                                    var uni = unilers.First(p => p.UniversiteID == item.UniversiteID);
                                    if (rw.AdSoyad != item.AdSoyad || rw.UnvanAdi != item.UnvanAdi || rw.EMail != item.EMail || rw.UniversiteID != item.UniversiteID || rw.UzmanlikAlani != item.UzmanlikAlani || rw.BilimselCalismalarAnahtarSozcukler != item.BilimselCalismalarAnahtarSozcukler || rw.DilPuani != item.DilPuani || rw.DilSinavAdi != item.DilSinavAdi) isDegisiklikVar = true;
                                    rw.UnvanAdi = item.UnvanAdi.ToUpper();
                                    rw.AdSoyad = item.AdSoyad.ToUpper();
                                    rw.EMail = item.EMail;
                                    rw.UniversiteAdi = uni.Ad;
                                    rw.UniversiteID = item.UniversiteID;
                                    rw.AnabilimdaliProgramAdi = item.AnabilimdaliProgramAdi;
                                    rw.UzmanlikAlani = item.UzmanlikAlani;
                                    rw.BilimselCalismalarAnahtarSozcukler = item.BilimselCalismalarAnahtarSozcukler;
                                    rw.DilSinavAdi = item.DilSinavAdi;
                                    rw.DilPuani = item.DilPuani;

                                    bool isAsil = new List<string> { "TezDanismani", "TikUyesi1", "TikUyesi2" }.Contains(item.JuriTipAdi);
                                    if ((rw.AdSoyad != item.AdSoyad && rw.EMail != item.EMail) || isAsil) rw.IsAsilOrYedek = isAsil ? true : (bool?)null;
                                }
                                else _entities.MezuniyetJuriOneriFormuJurileris.Remove(rw);
                            }
                            else if (item.AdSoyad.IsNullOrWhiteSpace() == false)
                            {
                                var uni = unilers.First(p => p.UniversiteID == item.UniversiteID);
                                mbjo.MezuniyetJuriOneriFormuJurileris.Add(
                                    new MezuniyetJuriOneriFormuJurileri
                                    {
                                        JuriTipAdi = item.JuriTipAdi,
                                        UnvanAdi = item.UnvanAdi.ToUpper(),
                                        AdSoyad = item.AdSoyad.ToUpper(),
                                        EMail = item.EMail,
                                        UniversiteID = item.UniversiteID,
                                        UniversiteAdi = uni.Ad,
                                        AnabilimdaliProgramAdi = item.AnabilimdaliProgramAdi,
                                        UzmanlikAlani = item.UzmanlikAlani,
                                        BilimselCalismalarAnahtarSozcukler = item.BilimselCalismalarAnahtarSozcukler,
                                        DilSinavAdi = item.DilSinavAdi,
                                        DilPuani = item.DilPuani,
                                        IsAsilOrYedek = new List<string> { "TezDanismani", "TikUyesi1", "TikUyesi2" }.Contains(item.JuriTipAdi) ? true : (bool?)null

                                    });
                            }
                        }
                        if (isYeniJo || isDegisiklikVar || _entities.MezuniyetJuriOneriFormuJurileris.Count(p => p.MezuniyetJuriOneriFormID == kModel.MezuniyetJuriOneriFormID) != kData.Count(p => p.AdSoyad.IsNullOrWhiteSpace() == false))
                        {
                            var uniqueId = Guid.NewGuid().ToString().Replace("-", "").Substr(0, 8).ToUpper();
                            while (_entities.MezuniyetJuriOneriFormlaris.Any(a => a.UniqueID == uniqueId))
                            {
                                uniqueId = Guid.NewGuid().ToString().Replace("-", "").Substr(0, 8).ToUpper();
                            }
                            mbjo.UniqueID = uniqueId;
                        }
                        mbjo.MezuniyetBasvurulariID = kModel.MezuniyetBasvurulariID;

                        mbjo.IsTezBasligiDegisti = kModel.IsTezBasligiDegisti;
                        mbjo.YeniTezBaslikTr = kModel.IsTezBasligiDegisti == true ? kModel.YeniTezBaslikTr : null;
                        mbjo.YeniTezBaslikEn = kModel.IsTezBasligiDegisti == true ? kModel.YeniTezBaslikEn : null;


                        if (RoleNames.MezuniyetGelenBasvurularJuriOneriFormuOnay.InRoleCurrent())
                        {

                        }
                        else
                        {
                            mbjo.EYKYaGonderildi = null;
                            mbjo.EYKYaGonderildiIslemTarihi = null;
                            mbjo.EYKYaGonderildiIslemYapanID = null;
                            mbjo.EYKDaOnaylandi = null;
                        }

                        mbjo.EYKDaOnaylandiOnayTarihi = null;
                        mbjo.EYKDaOnaylandiIslemYapanID = null;
                        mbjo.IslemTarihi = DateTime.Now;
                        mbjo.IslemYapanID = UserIdentity.Current.Id;
                        mbjo.IslemYapanIP = UserIdentity.Ip;

                        if (isYeniJo) _entities.MezuniyetJuriOneriFormlaris.Add(mbjo);

                        try
                        {
                            _entities.SaveChanges();
                        }
                        catch (Exception ex)
                        {
                            var hataMsj = "Kayıt işlemi sırasında bir hata oluştu! \r\nHata:" + ex.ToExceptionMessage();
                            mMessage.Messages.Add(hataMsj);
                            SistemBilgilendirmeBus.SistemBilgisiKaydet(hataMsj, "MezuniyetGelenBasvurular/JuriOneriFormuPost", LogType.Hata);
                        }


                    }
                }

            }
            mMessage.IsSuccess = saveData && mMessage.Messages.Count == 0;
            if (mMessage.Messages.Count > 0)
            {
                mMessage.Title = "Jüri Öneri Formu Aşağıdaki Sebeplerden Dolayı Oluşturulamadı.";
                mMessage.IsSuccess = false;
                mMessage.MessageType = Msgtype.Warning;
            }
            return new
            {
                mMessage,
                IsYeniJO = isYeniJo,
                SelectedAnaTabAdi = selectedAnaTabAdi,
                SelectedDetayTabAdi = selectedDetayTabAdi
            }.ToJsonResult();
        }
        public ActionResult JuriOneriFormu()
        {

            return View();
        }

        public ActionResult JuriOneriFormuAsilYedekDurumKayit(int id, int mezuniyetJuriOneriFormId, string juriTipAdi, bool? isAsilOrYedek)
        {
            var mmMessage = new MmMessage
            {
                IsSuccess = false,
                Title = "Jüri öneri formu Asil/Yedek seçimi işlemi",
                MessageType = Msgtype.Warning
            };

            var mb = _entities.MezuniyetBasvurularis.First(p => p.MezuniyetBasvurulariID == id);
            var juriOneriFormu = mb.MezuniyetJuriOneriFormlaris.Where(p => p.MezuniyetJuriOneriFormID == mezuniyetJuriOneriFormId).FirstOrDefault();

            if (!RoleNames.MezuniyetGelenBasvurularJuriOneriFormuEykOnay.InRoleCurrent())
            {
                mmMessage.Messages.Add("Jüri öneri formunda Asil/Yedek jüri adayı seçimi yetkisine sahip değilsiniz!");
            }
            else if (juriOneriFormu == null)
            {
                mmMessage.Messages.Add("İşlem yapılmak istenen jüri öneri formu sistemde bulunamadı!");
            }
            else
            {
                if (juriOneriFormu.EYKYaGonderildi == false)
                {
                    mmMessage.Messages.Add("İşlem yapılmak istenen jüri öneri formu EYK'ya gönderildi seçeneği ile kayıt edilmediğinden Asil/Yedek jüri adayı seçimi yapamazsınız!");
                }
                else if (juriOneriFormu.EYKDaOnaylandi == true)
                {
                    mmMessage.Messages.Add("İşlem yapılmak istenen jüri öneri formu EYK'da onaylandı seçeneği ile kayıt edildiğinden Asil/Yedek jüri adayı seçimi yapamazsınız!");
                }
            }

            if (mmMessage.Messages.Count == 0 && isAsilOrYedek.HasValue)
            {

                var adayCount = juriOneriFormu.MezuniyetJuriOneriFormuJurileris.Count(p => p.IsAsilOrYedek == isAsilOrYedek.Value);
                var countSize = mb.OgrenimTipKod.IsDoktora() ? (isAsilOrYedek.Value ? 5 : 2) : (isAsilOrYedek.Value ? 3 : 2);
                if (adayCount >= countSize)
                    mmMessage.Messages.Add((isAsilOrYedek.Value ? "Asil" : "Yedek") + " Jüri adayı önerisinden toplamda " + countSize + " aday seçilebilir.");



            }
            if (mmMessage.Messages.Count == 0)
            {
                var juri = juriOneriFormu.MezuniyetJuriOneriFormuJurileris.First(p => p.JuriTipAdi == juriTipAdi);
                juri.IsAsilOrYedek = isAsilOrYedek;
                _entities.SaveChanges();
                mmMessage.IsSuccess = true;
            }
            var strView = ViewRenderHelper.RenderPartialView("Ajax", "getMessage", mmMessage);
            return new
            {
                mmMessage.IsSuccess,
                Messages = strView
            }.ToJsonResult();
        }


        public ActionResult JuriOneriFormuOnayDurumKayit(int id, int mezuniyetJuriOneriFormId, bool eykDaOnayOrEykYaGonderim, bool? onaylandi, string eykDaOnaylanmadiDurumAciklamasi)
        {
            var mmMessage = new MmMessage
            {
                IsSuccess = false,
                Title = "Jüri öneri formu " + (eykDaOnayOrEykYaGonderim ? "EYK'da onay" : "EYK'ya gönderim") + " işlemi",
                MessageType = Msgtype.Warning
            };

            var mb = _entities.MezuniyetBasvurularis.First(p => p.MezuniyetBasvurulariID == id);
            var juriOneriFormu = mb.MezuniyetJuriOneriFormlaris.FirstOrDefault(p => p.MezuniyetJuriOneriFormID == mezuniyetJuriOneriFormId);

            if (!eykDaOnayOrEykYaGonderim && !RoleNames.MezuniyetGelenBasvurularJuriOneriFormuOnay.InRoleCurrent())
            {
                mmMessage.Messages.Add("Jüri öneri formunda onay yetkisine sahip değilsiniz!");
            }
            else if (eykDaOnayOrEykYaGonderim && !RoleNames.MezuniyetGelenBasvurularJuriOneriFormuEykOnay.InRoleCurrent())
            {
                mmMessage.Messages.Add("Jüri öneri formunda EYK'da onay yetkisine sahip değilsiniz!");
            }
            else if (juriOneriFormu == null)
            {
                mmMessage.Messages.Add("İşlem yapılmak istenen jüri öneri formu sistemde bulunamadı!");
            }
            else
            {
                if (eykDaOnayOrEykYaGonderim)
                {
                    if (juriOneriFormu.EYKYaGonderildi != true)
                    {
                        mmMessage.Messages.Add("EYK Ya gönderilmeyen jüri öneri formu üzerinde EYK Onayı işlemi yapılamaz!");
                    }
                    else if (onaylandi == false && eykDaOnaylanmadiDurumAciklamasi.IsNullOrWhiteSpace())
                    {
                        mmMessage.Messages.Add("EYK'da onaylanmama sebebini giriniz!");
                    }
                    else if (mb.EYKTarihi.HasValue && mb.SRTalepleris.Any(a => a.SRDurumID == SRTalepDurum.Onaylandı))
                    {
                        mmMessage.Messages.Add("Mezuniyet başvurusuna ait Salon rezervasyonu alındığı için jüri öneri formu onay işlemi yapılamaz!");
                    }
                }
                else
                {
                    if (juriOneriFormu.EYKDaOnaylandi.HasValue)
                    {
                        mmMessage.Messages.Add("EYK onay işlemi yapılan bir form da ön onay işlemi gerçekleştirilemez!");
                    }
                }
            }
            if (mmMessage.Messages.Count == 0 && eykDaOnayOrEykYaGonderim && onaylandi == true)
            {


                string msg = "";
                var asilCount = juriOneriFormu.MezuniyetJuriOneriFormuJurileris.Count(p => p.IsAsilOrYedek == true);
                var yedekCount = juriOneriFormu.MezuniyetJuriOneriFormuJurileris.Count(p => p.IsAsilOrYedek == false);
                var countSizeAsil = mb.OgrenimTipKod.IsDoktora() ? 5 : 3;
                if (asilCount != countSizeAsil)
                    msg += ("<br />* Jüri adayı önerisinden " + countSizeAsil + " Asil aday belirlemeniz gerekmektedi.");
                if (yedekCount != 2)
                    msg += ("<br />* Jüri adayı önerisinde 2 Yedek aday belirlemeniz gerekmektedi.");
                if (msg != "")
                {
                    mmMessage.Messages.Add("Jüri öneri formunda EYK'da onaylandı işlemini yapabilmeniz için: " + msg);
                }

            }
            if (mmMessage.Messages.Count == 0)
            {
                if (eykDaOnayOrEykYaGonderim)
                {
                    juriOneriFormu.EYKDaOnaylandi = onaylandi;
                    if (mb.EYKTarihi == null && onaylandi == true) mb.EYKTarihi = DateTime.Now.Date;
                    juriOneriFormu.EYKDaOnaylandiOnayTarihi = DateTime.Now;
                    juriOneriFormu.EYKDaOnaylandiIslemYapanID = UserIdentity.Current.Id;
                    juriOneriFormu.EYKDaOnaylanmadiDurumAciklamasi = onaylandi == false ? eykDaOnaylanmadiDurumAciklamasi : "";
                    bool sendMail = onaylandi == true && mb.EYKTarihi.HasValue;
                    if (sendMail)
                    {

                        var danismanSablonId = 0;
                        var asilSablonId = 0;
                        var ogrenciSablonId = 0;
                        if (mb.OgrenimTipKod.IsDoktora())
                        {

                            danismanSablonId = MailSablonTipi.Mez_EykTarihiGirildiDanismanDoktora;
                            asilSablonId = MailSablonTipi.Mez_EykTarihiGirildiJuriAsilDoktora;
                            ogrenciSablonId = MailSablonTipi.Mez_EykTarihiGirildiOgrenciDoktora;
                        }
                        else
                        {
                            danismanSablonId = MailSablonTipi.Mez_EykTarihiGirildiDanismanYL;
                            asilSablonId = MailSablonTipi.Mez_EykTarihiGirildiJuriAsilYL;
                            ogrenciSablonId = MailSablonTipi.Mez_EykTarihiGirildiOgrenciYL;
                        }
                        #region sendMail
                        if (sendMail)
                        {
                            var tezKonusu = "";
                            if (juriOneriFormu.IsTezBasligiDegisti == true)
                            {
                                tezKonusu = mb.IsTezDiliTr == true
                                    ? juriOneriFormu.YeniTezBaslikTr
                                    : juriOneriFormu.YeniTezBaslikEn;
                            }
                            else tezKonusu = mb.IsTezDiliTr == true
                                ? mb.TezBaslikTr
                                : mb.TezBaslikEn;

                            var enstitu = mb.MezuniyetSureci.Enstituler;
                            var sablonlar = _entities.MailSablonlaris.Where(p => p.EnstituKod == enstitu.EnstituKod).ToList();

                            var mModel = new List<SablonMailModel> {
                            new SablonMailModel {

                            AdSoyad =mb.Ad + " " + mb.Soyad,
                            EMails= new List<MailSendList> { new MailSendList { EMail =mb.Kullanicilar.EMail,ToOrBcc=true } },
                            MailSablonTipID=ogrenciSablonId
                            } };
                            var juriler = juriOneriFormu.MezuniyetJuriOneriFormuJurileris.Where(p => p.IsAsilOrYedek.HasValue).ToList();
                            foreach (var item in juriler.Where(p => p.IsAsilOrYedek == true))
                            {
                                mModel.Add(new SablonMailModel
                                {

                                    AdSoyad = item.AdSoyad,
                                    EMails = new List<MailSendList> { new MailSendList { EMail = item.EMail, ToOrBcc = true } },
                                    MailSablonTipID = (item.JuriTipAdi == "TezDanismani" ? danismanSablonId : asilSablonId),
                                    JuriTipAdi = item.JuriTipAdi,
                                    UnvanAdi = item.UnvanAdi,
                                    MezuniyetJuriOneriFormuJuriID = item.MezuniyetJuriOneriFormuJuriID,
                                });
                                if (item.JuriTipAdi == "TezDanismani" && !mb.TezEsDanismanEMail.IsNullOrWhiteSpace())
                                {
                                    //Eş danışman var ise Danışmana giden mail eş danışmana da gönderilmesi için.
                                    mModel.Add(new SablonMailModel
                                    {

                                        AdSoyad = mb.TezEsDanismanAdi,
                                        EMails = new List<MailSendList> { new MailSendList { EMail = mb.TezEsDanismanEMail, ToOrBcc = true } },
                                        MailSablonTipID = danismanSablonId,
                                        JuriTipAdi = item.JuriTipAdi,
                                        UnvanAdi = mb.TezEsDanismanUnvani,
                                    });
                                }
                            }
                            var danisman = juriler.First(p => p.JuriTipAdi == "TezDanismani");


                            foreach (var item in mModel)
                            {
                                var enstituL = mb.MezuniyetSureci.Enstituler;
                                var abdL = mb.Programlar.AnabilimDallari;
                                var prgL = mb.Programlar;
                                item.ProgramAdi = prgL.ProgramAdi;
                                item.Sablon = sablonlar.First(p => p.MailSablonTipID == item.MailSablonTipID);
                                item.SablonParametreleri = item.Sablon.MailSablonTipleri.Parametreler.Split(',').ToList().Select(s => s.Trim()).ToList();

                                //Şablona ait ekler var ise attachmets e ekle
                                var gonderilenMailEkleri = new List<GonderilenMailEkleri>();
                                foreach (var itemSe in item.Sablon.MailSablonlariEkleris)
                                {
                                    var ekTamYol = Server.MapPath("~" + itemSe.EkDosyaYolu);
                                    if (System.IO.File.Exists(ekTamYol))
                                    {
                                        var fExtension = Path.GetExtension(ekTamYol);
                                        item.Attachments.Add(new System.Net.Mail.Attachment(new MemoryStream(System.IO.File.ReadAllBytes(ekTamYol)),
                                            itemSe.EkAdi.ToSetNameFileExtension(fExtension), System.Net.Mime.MediaTypeNames.Application.Octet));
                                        gonderilenMailEkleri.Add(new GonderilenMailEkleri { EkAdi = itemSe.EkAdi, EkDosyaYolu = itemSe.EkDosyaYolu });
                                    }
                                    else SistemBilgilendirmeBus.SistemBilgisiKaydet("Mail gönderilirken eklenen dosya eki sistemde bulunamadı!<br/>Dosya Adı:" + itemSe.EkAdi + " <br/>Dosya Yolu:" + ekTamYol, "MezuniyetGelenBasvurular/JuriOneriFormuOnayDurumKayit", LogType.Uyarı);
                                }

                                if (item.Sablon.GonderilecekEkEpostalar != null) item.EMails.AddRange(item.Sablon.GonderilecekEkEpostalar.Split(',').Select(s => new MailSendList { EMail = s.Trim(), ToOrBcc = false }));
                                var paramereDegerleri = new List<MailReplaceParameterDto>();

                                if (item.SablonParametreleri.Any(a => a == "@EnstituAdi"))
                                    paramereDegerleri.Add(new MailReplaceParameterDto { Key = "EnstituAdi", Value = enstituL.EnstituAd });
                                if (item.SablonParametreleri.Any(a => a == "@WebAdresi"))
                                    paramereDegerleri.Add(new MailReplaceParameterDto { Key = "WebAdresi", Value = enstitu.WebAdresi, IsLink = true });
                                if (item.SablonParametreleri.Any(a => a == "@EYKTarihi"))
                                    paramereDegerleri.Add(new MailReplaceParameterDto { Key = "EYKTarihi", Value = mb.EYKTarihi.Value.ToDateString() });
                                if (item.SablonParametreleri.Any(a => a == "@AdSoyad"))
                                    paramereDegerleri.Add(new MailReplaceParameterDto { Key = "AdSoyad", Value = item.AdSoyad });
                                if (item.SablonParametreleri.Any(a => a == "@UnvanAdi"))
                                    paramereDegerleri.Add(new MailReplaceParameterDto { Key = "UnvanAdi", Value = item.UnvanAdi });
                                if (item.SablonParametreleri.Any(a => a == "@OgrenciAdSoyad"))
                                    paramereDegerleri.Add(new MailReplaceParameterDto { Key = "OgrenciAdSoyad", Value = mb.Ad + " " + mb.Soyad });
                                if (item.SablonParametreleri.Any(a => a == "@OgrenciBilgi"))
                                    paramereDegerleri.Add(new MailReplaceParameterDto { Key = "OgrenciBilgi", Value = (mb.OgrenciNo + " " + mb.Ad + " " + mb.Soyad + " (" + abdL.AnabilimDaliAdi + " / " + prgL.ProgramAdi + ")") });

                                if (item.SablonParametreleri.Any(a => a == "@TezBaslikTr"))
                                    paramereDegerleri.Add(new MailReplaceParameterDto { Key = "TezBaslikTr", Value = tezKonusu });
                                if (item.SablonParametreleri.Any(a => a == "@DanismanBilgi"))
                                    paramereDegerleri.Add(new MailReplaceParameterDto { Key = "DanismanBilgi", Value = danisman.UnvanAdi + " " + danisman.AdSoyad });
                                foreach (var itemAsil in juriOneriFormu.MezuniyetJuriOneriFormuJurileris.Where(p => p.JuriTipAdi != "TezDanismani" && p.IsAsilOrYedek == true).Select((s, inx) => new { s, inx = inx + 1 }))
                                    if (item.SablonParametreleri.Any(a => a == "@AsilBilgi" + itemAsil.inx))
                                    {
                                        string uniBilgi = "";
                                        if (itemAsil.s.JuriTipAdi.Contains("YtuDisiJuri"))
                                        {
                                            uniBilgi = " (" + (itemAsil.s.UniversiteID.HasValue ? itemAsil.s.Universiteler.KisaAd : itemAsil.s.UniversiteAdi) + ")";
                                        }
                                        paramereDegerleri.Add(new MailReplaceParameterDto { Key = "AsilBilgi" + itemAsil.inx, Value = itemAsil.s.UnvanAdi + " " + itemAsil.s.AdSoyad + uniBilgi });
                                    }
                                foreach (var itemYedek in juriOneriFormu.MezuniyetJuriOneriFormuJurileris.Where(p => p.IsAsilOrYedek == false).Select((s, inx) => new { s, inx = inx + 1 }))
                                    if (item.SablonParametreleri.Any(a => a == "@YedekBilgi" + itemYedek.inx))
                                    {
                                        string uniBilgi = "";
                                        if (itemYedek.s.JuriTipAdi.Contains("YtuDisiJuri"))
                                        {
                                            uniBilgi = " (" + (itemYedek.s.UniversiteID.HasValue ? itemYedek.s.Universiteler.KisaAd : itemYedek.s.UniversiteAdi) + ")";
                                        }
                                        paramereDegerleri.Add(new MailReplaceParameterDto { Key = "YedekBilgi" + itemYedek.inx, Value = itemYedek.s.UnvanAdi + " " + itemYedek.s.AdSoyad + uniBilgi });
                                    }
                                var mCOntent = SystemMails.GetSystemMailContent(enstituL.EnstituAd, item.Sablon.SablonHtml, item.Sablon.SablonAdi, paramereDegerleri);
                                // item.EMails = new List<MailSendList> { new MailSendList { EMail = "irfansecer@gmail.com", ToOrBCC = true } }; //test için
                                var snded = MailManager.SendMail(enstitu.EnstituKod, mCOntent.Title, mCOntent.HtmlContent, item.EMails, item.Attachments);
                                if (snded)
                                {
                                    var kModel = new GonderilenMailler
                                    {
                                        Tarih = DateTime.Now,
                                        EnstituKod = enstitu.EnstituKod,
                                        MesajID = null,
                                        IslemTarihi = DateTime.Now,
                                        Konu = item.Sablon.SablonAdi + " (" + item.AdSoyad + ")"
                                    };
                                    if (!item.JuriTipAdi.IsNullOrWhiteSpace()) kModel.Konu = kModel.Konu + " (" + item.JuriTipAdi + ")";
                                    kModel.IslemYapanID = UserIdentity.Current == null || !UserIdentity.Current.IsAuthenticated ? 1 : UserIdentity.Current.Id;
                                    kModel.IslemYapanIP = UserIdentity.Ip;
                                    kModel.Aciklama = item.Sablon.Sablon ?? "";
                                    kModel.AciklamaHtml = mCOntent.HtmlContent ?? "";
                                    kModel.Gonderildi = true;
                                    kModel.GonderilenMailKullanicilars = item.EMails.Select(s => new GonderilenMailKullanicilar { Email = s.EMail }).ToList();
                                    gonderilenMailEkleri.AddRange(item.Attachments.Select(s => new GonderilenMailEkleri { EkAdi = s.Name, EkDosyaYolu = "" }).ToList());
                                    kModel.GonderilenMailEkleris = gonderilenMailEkleri;
                                    _entities.GonderilenMaillers.Add(kModel);
                                    _entities.SaveChanges();
                                }
                            }

                        }
                        #endregion
                    }
                }
                else
                {
                    juriOneriFormu.EYKYaGonderildi = onaylandi;
                    juriOneriFormu.EYKYaGonderildiIslemTarihi = DateTime.Now;
                    juriOneriFormu.EYKYaGonderildiIslemYapanID = UserIdentity.Current.Id;
                }
                _entities.SaveChanges();
                mmMessage.MessageType = Msgtype.Success;
                mmMessage.IsSuccess = true;

                mmMessage.Messages.Add("Form " + (onaylandi.HasValue ? (onaylandi.Value ? "'Onaylandı'" : "'Onaylanmadı'") : "İşlem bekliyor") + " şeklinde güncellendi...");
            }
            var strView = ViewRenderHelper.RenderPartialView("Ajax", "getMessage", mmMessage);
            return new
            {
                mmMessage.IsSuccess,
                Messages = strView
            }.ToJsonResult();
        }


        public ActionResult SrJuriDegistir(Guid uniqueId)
        {
            var srTalep = _entities.SRTalepleris.First(p => p.UniqueID == uniqueId);

            return View(srTalep);
        }


        public ActionResult SrJuriDegistirPost(Guid uniqueId, int? ytuIciMezuniyetJuriOneriFormuJuriId, int? ytuDisiMezuniyetJuriOneriFormuJuriId)
        {
            var mmMessage = new MmMessage
            {
                IsSuccess = false,
                Title = "Tez Sınavı Jüri Değişiklik İşlemi",
                MessageType = Msgtype.Success
            };
            var srTalep = _entities.SRTalepleris.First(p => p.UniqueID == uniqueId);
            if (ytuIciMezuniyetJuriOneriFormuJuriId.HasValue)
            {
                var srYtuIciJuri = srTalep.SRTaleplerJuris.FirstOrDefault(p => p.JuriTipAdi.Contains("YtuIciJuri") && p.MezuniyetJuriOneriFormuJuriID != ytuIciMezuniyetJuriOneriFormuJuriId);
                if (srYtuIciJuri != null)
                {
                    var juri = _entities.MezuniyetJuriOneriFormuJurileris.First(p => p.MezuniyetJuriOneriFormuJuriID == ytuIciMezuniyetJuriOneriFormuJuriId);
                    srYtuIciJuri.UniqueID = Guid.NewGuid();
                    srYtuIciJuri.MezuniyetJuriOneriFormuJuriID = ytuDisiMezuniyetJuriOneriFormuJuriId;
                    srYtuIciJuri.UniversiteAdi = juri.UniversiteAdi;
                    srYtuIciJuri.AnabilimdaliProgramAdi = juri.AnabilimdaliProgramAdi;
                    srYtuIciJuri.JuriTipAdi = juri.JuriTipAdi;
                    srYtuIciJuri.UnvanAdi = juri.UnvanAdi;
                    srYtuIciJuri.JuriAdi = juri.AdSoyad;
                    srYtuIciJuri.Email = juri.EMail;
                    srYtuIciJuri.IsLinkGonderildi = false;
                    srYtuIciJuri.MezuniyetSinavDurumID = null;
                    srYtuIciJuri.IslemTarihi = DateTime.Now;
                    srYtuIciJuri.IslemYapanID = UserIdentity.Current.Id;
                    srYtuIciJuri.IslemYapanIP = UserIdentity.Ip;
                }
                mmMessage.Messages.Add("YTÜ İçi Jüri Değişikliği Yapıldı.");
            }
            if (ytuDisiMezuniyetJuriOneriFormuJuriId.HasValue)
            {
                var ytuDisiJuri = srTalep.SRTaleplerJuris.FirstOrDefault(p => p.JuriTipAdi.Contains("YtuDisiJuri") && p.MezuniyetJuriOneriFormuJuriID != ytuDisiMezuniyetJuriOneriFormuJuriId);
                if (ytuDisiJuri != null)
                {
                    var juri = _entities.MezuniyetJuriOneriFormuJurileris.First(p => p.MezuniyetJuriOneriFormuJuriID == ytuDisiMezuniyetJuriOneriFormuJuriId);
                    ytuDisiJuri.UniqueID = Guid.NewGuid();
                    ytuDisiJuri.MezuniyetJuriOneriFormuJuriID = ytuDisiMezuniyetJuriOneriFormuJuriId;
                    ytuDisiJuri.UniversiteAdi = juri.UniversiteAdi;
                    ytuDisiJuri.AnabilimdaliProgramAdi = juri.AnabilimdaliProgramAdi;
                    ytuDisiJuri.JuriTipAdi = juri.JuriTipAdi;
                    ytuDisiJuri.UnvanAdi = juri.UnvanAdi;
                    ytuDisiJuri.JuriAdi = juri.AdSoyad;
                    ytuDisiJuri.Email = juri.EMail;
                    ytuDisiJuri.IsLinkGonderildi = false;
                    ytuDisiJuri.MezuniyetSinavDurumID = null;
                    ytuDisiJuri.IslemTarihi = DateTime.Now;
                    ytuDisiJuri.IslemYapanID = UserIdentity.Current.Id;
                    ytuDisiJuri.IslemYapanIP = UserIdentity.Ip;
                    mmMessage.Messages.Add("YTÜ Dışı Jüri Değişikliği Yapıldı.");
                }
            }
            _entities.SaveChanges();
            mmMessage.IsSuccess = true;
            mmMessage.MessageType = Msgtype.Success;
            return mmMessage.ToJsonResult();
        }
        public ActionResult Sil(int id)
        {
            var mmMessage = MezuniyetBus.MezuniyetBasvurusuSilKontrol(id);
            if (mmMessage.IsSuccess)
            {
                var kayit = _entities.MezuniyetBasvurularis.FirstOrDefault(p => p.MezuniyetBasvurulariID == id);
                var tarih = kayit.BasvuruTarihi.ToString();
                try
                {
                    var fFList = new List<string>();
                    foreach (var item in kayit.MezuniyetBasvurulariYayins)
                    {
                        if (item.MezuniyetYayinBelgeDosyaYolu.IsNullOrWhiteSpace() == false) fFList.Add(item.MezuniyetYayinBelgeDosyaYolu);
                        if (item.MezuniyetYayinMetniBelgeYolu.IsNullOrWhiteSpace() == false) fFList.Add(item.MezuniyetYayinMetniBelgeYolu);
                    }
                    mmMessage.Title = "Uyarı";
                    _entities.MezuniyetBasvurularis.Remove(kayit);
                    _entities.SaveChanges();
                    LogIslemleri.LogEkle("MezuniyetBasvurulari", IslemTipi.Delete, kayit.ToJson());
                    mmMessage.Messages.Add(tarih + " Tarihli başvuru silindi.");
                    mmMessage.MessageType = Msgtype.Success;
                    foreach (var item in fFList)
                    {
                        var path = Server.MapPath("~" + item);
                        if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
                    }
                }
                catch (Exception ex)
                {
                    mmMessage.MessageType = Msgtype.Error;
                    mmMessage.IsSuccess = false;
                    mmMessage.Messages.Add(tarih + " Tarihli başvuru silinemedi.");
                    mmMessage.Title = "Hata";
                    SistemBilgilendirmeBus.SistemBilgisiKaydet(ex.ToExceptionMessage(), "MezuniyetGelenBasvurular/Sil<br/><br/>" + ex.ToExceptionStackTrace(), LogType.OnemsizHata);
                }

            }
            var strView = ViewRenderHelper.RenderPartialView("Ajax", "getMessage", mmMessage);
            return Json(new { IsSuccess = mmMessage.IsSuccess, Messages = strView }, "application/json", JsonRequestBehavior.AllowGet);
        }




        public ActionResult GetTutanakRaporu()
        {
            return View();
        }
        public ActionResult GetTutanakRaporuKontrolu(int raporTipId, List<int> ogrenimTipKods, DateTime? BasTar, DateTime? BitTar, DateTime? RaporTarihi)
        {
            var mMessage = new MmMessage
            {
                MessageType = Msgtype.Success,
                IsSuccess = true
            };

            if (ogrenimTipKods == null || ogrenimTipKods.Count == 0)
            {
                mMessage.IsSuccess = false;
                mMessage.Messages.Add("Öğrenim tipi seçiniz.");
                mMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "OgrenimTipKods" });
            }
            else mMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Nothing, PropertyName = "OgrenimTipKods" });
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
            if (raporTipId == RaporTipleri.MezuniyetTutanakRaporu && !RaporTarihi.HasValue && ogrenimTipKods != null && ogrenimTipKods.Any(a => a.IsDoktora()))
            {
                mMessage.IsSuccess = false;
                mMessage.Messages.Add("Rapor tarihini giriniz.");
                mMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "RaporTarihi" });
            }
            else { mMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Nothing, PropertyName = "RaporTarihi" }); }

            if (!mMessage.IsSuccess)
            {

                mMessage.Title = "Tutanak çıktısı oluşturulamadı";
                mMessage.MessageType = Msgtype.Warning;
            }

            return mMessage.ToJsonResult();



        }
        public ActionResult GetTutanakRaporuExport(int raporTipId, int ogrenimTipKods, string basTar, string bitTar, string raporTarihi, bool exportWordOrExcel, string ekd)
        {

            var html = "";
            string raporAdi = "";
            var enstituKod = EnstituBus.GetSelectedEnstitu(ekd);
            var baslangicTarihi = basTar.ToDate().Value;
            var bitisTarihi = bitTar.ToDate().Value;
            var isDoktora = ogrenimTipKods.IsDoktora();
            var qData = _entities.MezuniyetBasvurularis.Where(p => p.MezuniyetSureci.EnstituKod == enstituKod).AsQueryable();
            qData = raporTipId == RaporTipleri.MezuniyetTezJuriTutanakRaporu ? qData.Where(p => p.SRTalepleris.Any(a => a.MezuniyetSinavDurumID == MezuniyetSinavDurum.Basarili) && (p.EYKTarihi >= baslangicTarihi && p.EYKTarihi <= bitisTarihi)).OrderByDescending(o => o.OgrenimTipKod).ThenBy(t => t.EYKTarihi)
                : qData.Where(p => p.IsMezunOldu == true && (p.MezuniyetTarihi >= baslangicTarihi && p.MezuniyetTarihi <= bitisTarihi)).OrderBy(o => o.MezuniyetTarihi);
            var data = qData.ToList().Where(p => p.OgrenimTipKod.IsDoktora() == isDoktora).ToList();



            if (raporTipId == RaporTipleri.MezuniyetTezJuriTutanakRaporu)
            {
                var model = new List<RprTutanakModel>();
                var rModel = new RprTutanakModel
                {
                    IsDoktoraOrYL = ogrenimTipKods.IsDoktora()
                };
                rModel.TutanakAdi = rModel.IsDoktoraOrYL ? "Doktora - Tez Sınav Jürileri Atama Önerileri Hk." : "Yüksek Lisans - Tez Savunma Jüri Önerileri Hk.";
                rModel.Aciklama = rModel.IsDoktoraOrYL ?
                                    "Tezini tamamlayarak Enstitümüze teslim eden aşağıda adı, Anabilim Dalı/Programı belirtilen doktora öğrencilerinin tez sınav jürilerinin “YTÜ Lisansüstü Eğitim ve Öğretim Yönetmeliği” nin ilgili maddesi uyarınca, aşağıdaki öğretim üyelerinden oluşmasına oybirliği ile karar verildi. "
                                    :
                                   "Tezini tamamlayarak Enstitümüze teslim eden aşağıda adı, Anabilim Dalı/Programı belirtilen yüksek lisans öğrencilerinin tez sınav jürilerinin “YTÜ Lisansüstü Eğitim ve Öğretim Yönetmeliği” nin ilgili maddesi uyarınca, aşağıdaki öğretim üyelerinden oluşmasına oybirliği ile karar verildi.";


                foreach (var itemO in data)
                {
                    var row = new RprTutanakRowModel();
                    var prgl = itemO.Programlar;
                    var abdl = itemO.Programlar.AnabilimDallari;
                    row.OgrenciBilgi = itemO.OgrenciNo + " " + itemO.Ad + " " + itemO.Soyad + " (" + abdl.AnabilimDaliAdi + " / " + prgl.ProgramAdi + ")";
                    var joForm = itemO.MezuniyetJuriOneriFormlaris.First();
                    var srTalep =
                        itemO.SRTalepleris.First(p => p.MezuniyetSinavDurumID == MezuniyetSinavDurum.Basarili);
                    var danisman = joForm.MezuniyetJuriOneriFormuJurileris.First(p => p.JuriTipAdi == "TezDanismani");
                    row.DanismanAdSoyad = danisman.UnvanAdi + " " + danisman.AdSoyad;
                    row.DanismanUni = danisman.UniversiteID.HasValue ? danisman.Universiteler.Ad : danisman.UniversiteAdi;
                    if (rModel.IsDoktoraOrYL)
                    {
                        var tik1 = joForm.MezuniyetJuriOneriFormuJurileris.First(p => p.JuriTipAdi == "TikUyesi1");
                        row.TikUyesi = tik1.UnvanAdi + " " + tik1.AdSoyad;
                        row.TikUyesiUni = tik1.UniversiteID.HasValue ? tik1.Universiteler.Ad : tik1.UniversiteAdi;

                        var tik2 = joForm.MezuniyetJuriOneriFormuJurileris.First(p => p.JuriTipAdi == "TikUyesi2");
                        row.TikUyesi2 = tik2.UnvanAdi + " " + tik2.AdSoyad;
                        row.TikUyesi2Uni = tik2.UniversiteID.HasValue ? tik2.Universiteler.Ad : tik2.UniversiteAdi;
                    }
                    var jtList = new List<string> { "TezDanismani", "TikUyesi1", "TikUyesi2" };

                    var asilUye = joForm.MezuniyetJuriOneriFormuJurileris.First(p => !jtList.Contains(p.JuriTipAdi) && p.IsAsilOrYedek == true);
                    row.AsilUye = asilUye.UnvanAdi + " " + asilUye.AdSoyad;
                    row.AsilUyeUni = asilUye.UniversiteID.HasValue ? asilUye.Universiteler.Ad : asilUye.UniversiteAdi;
                    jtList.Add(asilUye.JuriTipAdi);
                    var asilUye2 = joForm.MezuniyetJuriOneriFormuJurileris.First(p => !jtList.Contains(p.JuriTipAdi) && p.IsAsilOrYedek == true);
                    row.AsilUye2 = asilUye2.UnvanAdi + " " + asilUye2.AdSoyad;
                    row.AsilUye2Uni = asilUye2.UniversiteID.HasValue ? asilUye2.Universiteler.Ad : asilUye2.UniversiteAdi;
                    jtList.Add(asilUye2.JuriTipAdi);
                    var yedekUye = joForm.MezuniyetJuriOneriFormuJurileris.First(p => !jtList.Contains(p.JuriTipAdi) && p.IsAsilOrYedek == false);
                    row.YedekUye = yedekUye.UnvanAdi + " " + yedekUye.AdSoyad;
                    row.YedekUyeUni = yedekUye.UniversiteID.HasValue ? yedekUye.Universiteler.Ad : yedekUye.UniversiteAdi;
                    jtList.Add(yedekUye.JuriTipAdi);
                    var yedekUye2 = joForm.MezuniyetJuriOneriFormuJurileris.First(p => !jtList.Contains(p.JuriTipAdi) && p.IsAsilOrYedek == false);
                    row.YedekUye2 = yedekUye2.UnvanAdi + " " + yedekUye2.AdSoyad;
                    row.YedekUye2Uni = yedekUye2.UniversiteID.HasValue ? yedekUye2.Universiteler.Ad : yedekUye2.UniversiteAdi;

                    if (srTalep.IsTezBasligiDegisti == true)
                    {
                        row.TezKonusu = itemO.IsTezDiliTr == true ? srTalep.YeniTezBaslikTr : srTalep.YeniTezBaslikEn; 
                    }
                    else if (joForm.IsTezBasligiDegisti == true)
                    {
                        row.TezKonusu = itemO.IsTezDiliTr == true ? joForm.YeniTezBaslikTr : joForm.YeniTezBaslikEn; 
                    }
                    else
                    {
                        row.TezKonusu = itemO.IsTezDiliTr == true ? itemO.TezBaslikTr : itemO.TezBaslikEn;
                    }   
                    rModel.DetayData.Add(row);

                    model.Add(rModel);
                }


                RprMezuniyetTezJuriTutanak rpr = new RprMezuniyetTezJuriTutanak(rModel.IsDoktoraOrYL);
                rpr.DataSource = model.Count > 0 ? model[0] : new RprTutanakModel();
                rpr.CreateDocument();
                raporAdi = (ogrenimTipKods.IsDoktora() ? "Doktra" : "Yüksek Lisans") + " Tez Sınav Jürileri Atama Önerileri";

                using (MemoryStream ms = new MemoryStream())
                {
                    rpr.ExportToHtml(ms);
                    ms.Position = 0;
                    var sr = new StreamReader(ms);
                    html = sr.ReadToEnd();
                }
            }
            else
            {
                if (ogrenimTipKods.IsDoktora())
                {
                    var model = new List<RprMezuniyetTutanakModel>();
                    foreach (var itemO in data)
                    {
                        var row = new RprMezuniyetTutanakModel();
                        var prgl = itemO.Programlar;
                        var abdl = itemO.Programlar.AnabilimDallari;
                        var sinav = itemO.SRTalepleris.First(p => p.MezuniyetSinavDurumID == MezuniyetSinavDurum.Basarili);
                        var tezSonBilgi = sinav.SRTalepleriBezCiltFormus.First();
                        var danismanBilgi = "";
                        var joForm = itemO.MezuniyetJuriOneriFormlaris.FirstOrDefault();
                        if (joForm != null)
                        {
                            var danisman = joForm.MezuniyetJuriOneriFormuJurileris.First(p => p.JuriTipAdi == "TezDanismani");
                            danismanBilgi = danisman.UnvanAdi + " " + danisman.AdSoyad;
                        }
                        else
                        {
                            danismanBilgi = sinav.SRTaleplerJuris.First().JuriAdi.ToUpper();
                        }
                        row.Konu = itemO.Ad + " " + itemO.Soyad + " 'DOKTORA DERECESİ' alması Hk.";
                        row.Aciklama1 = "Enstitümüz " + abdl.AnabilimDaliAdi + " Anabilim Dalı " + prgl.ProgramAdi + " doktora programı öğrencisi <b>" + itemO.OgrenciNo + "</b> no’lu <b>" + itemO.Ad + " " + itemO.Soyad + ";</b> "
                                        + "21/12/2016 gün ve  29925 sayılı Resmi Gazete’de yayımlanarak yürürlüğe giren 'YTÜ Lisansüstü Eğitim - Öğretim Yönetmeliği’nin 24.maddesi uyarınca, "
                                        + "doktora eğitimi ile ilgili tüm koşullarını yerine getirdiğinden " + sinav.Tarih.Date.ToString("dd.MM.yyyy") + " tarihinde yapılan doktora tez sınavında <b>" + danismanBilgi + "</b> danışmanlığında hazırladığı "
                                        + "<b>“" + tezSonBilgi.TezBaslikTr + "”</b> başlıklı tezi başarılı bulunmuştur.";
                        row.Aciklama2 = "1 Mart 2017 tarih ve 29994 sayılı Yüksek Öğretim Kurulu Lisansüstü Eğitim ve Öğretim Yönetmeliğinde Değişiklik Yapılmasına Dair Yönetmelik:<b> Madde 2- “Mezuniyet Tarihi tezin sınav "
                            + "jüri komisyonu tarafından imzalı nüshasının teslim edildiği tarihtir.”</b> gereğince <b>" + itemO.MezuniyetTarihi.Value.Date.ToString("dd.MM.yyyy") + "</b> tarihinde tezini Enstitümüze teslim eden İlgili öğrencinin, tezinin kabul edildiğini ve kendisine "
                            + "<b>'DOKTORA DERECESİ'</b> verildiğini bildiren jüri ortak raporunun <b>" + raporTarihi.ToDate().Value.ToString("dd.MM.yyyy") + "</b> tarihi itibariyle onanmasına ve Üniversite Senatosu'na sunulmak üzere Rektörlüğe arzına </b>oybirliğiyle</b> karar verildi.";
                        model.Add(row);
                    }
                    RprMezuniyetMezunlarTutanakDr rpr = new RprMezuniyetMezunlarTutanakDr();
                    rpr.DataSource = model;
                    rpr.CreateDocument();
                    raporAdi = "Doktora Mezuniyet Tutanağı";

                    using (MemoryStream ms = new MemoryStream())
                    {
                        rpr.ExportToHtml(ms);
                        ms.Position = 0;
                        var sr = new StreamReader(ms);
                        html = sr.ReadToEnd();
                    }
                }
                else
                {
                    var model = new RprMezuniyetTutanakModel
                    {
                        Konu = "Yüksek Lisans Mezuniyeti Hk",
                        Aciklama1 = "“YTÜ Lisansüstü Eğitim ve Öğretim Yönetmeliği” nin yüksek lisans eğitimi ile ilgili tüm koşullarını yerine getiren, aşağıda adı - soyadı, "
                                    + "Anabilim Dalı/ Programı belirtilen Enstitümüz yüksek lisans programı öğrencilerinin, 1 Mart 2017 tarih ve 29994 sayılı Yüksek Öğretim Kurulu "
                                    + "Lisansüstü Eğitim ve Öğretim Yönetmeliğinde Değişiklik Yapılmasına Dair Yönetmelik:<b> Madde 2 - “Mezuniyet Tarihi tezin sınav jüri komisyonu tarafından "
                                    + "imzalı nüshasının teslim edildiği tarihtir.”</b> gereğince, " + baslangicTarihi.ToString("dd.MM.yyyy") + " ile " + bitisTarihi.ToString("dd.MM.yyyy") + " tarihleri arasında tezlerini Enstitümüze teslim eden öğrencilerin "
                                    + "aşağıda belirtilen tez teslim tarihinde mezuniyetlerine oybirliğiyle karar verildi."
                    };

                    foreach (var itemO in data)
                    {
                        var row = new RprMezuniyetTutanakRowModel();
                        var prgl = itemO.Programlar;
                        var abdl = itemO.Programlar.AnabilimDallari;
                        row.OgrenciBilgi = itemO.OgrenciNo + " " + itemO.Ad + " " + itemO.Soyad + " (" + abdl.AnabilimDaliAdi + " / " + prgl.ProgramAdi + ")";

                        var sinav = itemO.SRTalepleris.First(p => p.MezuniyetSinavDurumID == MezuniyetSinavDurum.Basarili);
                        var tezSonBilgi = sinav.SRTalepleriBezCiltFormus.First();
                        var danismanBilgi = "";
                        var joForm = itemO.MezuniyetJuriOneriFormlaris.FirstOrDefault();
                        if (joForm != null)
                        {
                            var danisman = joForm.MezuniyetJuriOneriFormuJurileris.First(p => p.JuriTipAdi == "TezDanismani");
                            danismanBilgi = danisman.UnvanAdi + " " + danisman.AdSoyad;
                        }
                        else
                        {
                            danismanBilgi = sinav.SRTaleplerJuris.First().JuriAdi.ToUpper();
                        }
                        row.DanismanAdSoyad = danismanBilgi;
                        row.TezKonusu = tezSonBilgi.IsTezDiliTr ? tezSonBilgi.TezBaslikTr : tezSonBilgi.TezBaslikEn;
                        row.SavunmaTarihi = sinav.Tarih.ToString("dd.MM.yyyy");
                        row.TezTeslimTarihi = itemO.MezuniyetTarihi.ToString("dd.MM.yyyy");

                        model.Data.Add(row);

                    }
                    RprMezuniyetMezunlarTutanakYL rpr = new RprMezuniyetMezunlarTutanakYL();
                    rpr.DataSource = model;
                    rpr.CreateDocument();
                    raporAdi = "Yüksek Lisans Mezuniyet Tutanağı";

                    using (MemoryStream ms = new MemoryStream())
                    {
                        rpr.ExportToHtml(ms);
                        ms.Position = 0;
                        var sr = new StreamReader(ms);
                        html = sr.ReadToEnd();
                    }

                }

            }
            return File(System.Text.Encoding.UTF8.GetBytes(html), (exportWordOrExcel ? "application/vnd.ms-word" : "application/ms-excel"), raporAdi + " (" + basTar.Replace("-", ".") + "-" + bitTar.Replace("-", ".") + ")." + (exportWordOrExcel ? "doc" : "xls"));



        }


    }
}