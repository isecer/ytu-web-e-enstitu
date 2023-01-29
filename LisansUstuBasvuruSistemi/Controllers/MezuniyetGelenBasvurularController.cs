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
using LisansUstuBasvuruSistemi.Utilities.MenuAndRoles;

namespace LisansUstuBasvuruSistemi.Controllers
{
    [Authorize]
    [System.Web.Mvc.OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    public class MezuniyetGelenBasvurularController : Controller
    {
        private LisansustuBasvuruSistemiEntities db = new LisansustuBasvuruSistemiEntities();
        [Authorize(Roles = RoleNames.MezuniyetGelenBasvurular)]
        public ActionResult Index(string EKD, int? SMezuniyetBID, int? STabID)
        {
            var model = new fmMezuniyetBasvurulari() { PageSize = 50 };
            var JoFormKayitYetki = RoleNames.MezuniyetGelenBasvurularJuriOneriFormuKayit.InRoleCurrent();
            var MBGelenBKayitYetki = RoleNames.MezuniyetGelenBasvurularKayit.InRoleCurrent();
            if (MBGelenBKayitYetki)
            {
                model.MezuniyetSurecID = Management.getAktifMezuniyetSurecID(Management.getSelectedEnstitu(EKD));
            }

            model.Expand = model.MezuniyetSurecID.HasValue;
            model.MezuniyetDurumID = -1;
            model.SMezuniyetBID = SMezuniyetBID;
            model.STabID = STabID;
            return Index(model, EKD);

        }
        [HttpPost]
        [Authorize(Roles = RoleNames.MezuniyetGelenBasvurular)]
        public ActionResult Index(fmMezuniyetBasvurulari model, string EKD, bool export = false)
        {

            var _EnstituKod = Management.getSelectedEnstitu(EKD);

            var nowDate = DateTime.Now;
            string EnstituKod = Management.getSelectedEnstitu(EKD);
            var KullaniciID = UserIdentity.Current.Id;
            var q = from s in db.MezuniyetBasvurularis
                    join ms in db.MezuniyetSurecis on s.MezuniyetSurecID equals ms.MezuniyetSurecID
                    join kul in db.Kullanicilars on s.KullaniciID equals kul.KullaniciID
                    join mOT in db.MezuniyetSureciOgrenimTipKriterleris on new { s.MezuniyetSurecID, s.OgrenimTipKod } equals new { mOT.MezuniyetSurecID, mOT.OgrenimTipKod }
                    join o in db.OgrenimTipleris on new { s.OgrenimTipKod, ms.EnstituKod } equals new { o.OgrenimTipKod, o.EnstituKod }
                    join pr in db.Programlars on s.ProgramKod equals pr.ProgramKod
                    join abl in db.AnabilimDallaris on pr.AnabilimDaliID equals abl.AnabilimDaliID
                    join en in db.Enstitulers on s.MezuniyetSureci.EnstituKod equals en.EnstituKod
                    join bs in db.MezuniyetSurecis on s.MezuniyetSurecID equals bs.MezuniyetSurecID
                    join d in db.Donemlers on bs.DonemID equals d.DonemID
                    join ktip in db.KullaniciTipleris on s.Kullanicilar.KullaniciTipID equals ktip.KullaniciTipID
                    join dr in db.MezuniyetYayinKontrolDurumlaris on s.MezuniyetYayinKontrolDurumID equals dr.MezuniyetYayinKontrolDurumID
                    join qmsd in db.MezuniyetSinavDurumlaris on s.MezuniyetSinavDurumID equals qmsd.MezuniyetSinavDurumID into defMsd
                    from Msd in defMsd.DefaultIfEmpty()
                    join qjOf in db.MezuniyetJuriOneriFormlaris on s.MezuniyetBasvurulariID equals qjOf.MezuniyetBasvurulariID into defJof
                    from jOf in defJof.DefaultIfEmpty()
                    let SrT = s.SRTalepleris.OrderByDescending(os => os.SRTalepID).FirstOrDefault()
                    let TD = s.MezuniyetBasvurulariTezDosyalaris.OrderByDescending(os => os.MezuniyetBasvurulariTezDosyaID).FirstOrDefault()

                    where bs.Enstituler.EnstituKisaAd.Contains(EKD) && s.MezuniyetBasvurulariID == (model.SMezuniyetBID ?? s.MezuniyetBasvurulariID)
                    select new frMezuniyetBasvurulari
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
                        TcPasaPortNo = kul.TcKimlikNo != null ? kul.TcKimlikNo : kul.PasaportNo,
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
                        SrTalebi = SrT,
                        SRDurumID = SrT.SRDurumID,
                        TeslimFormDurumu = SrT != null ? SrT.SRTalepleriBezCiltFormus.Any() : false,
                        IsOnaylandiOrDuzeltme = TD != null ? TD.IsOnaylandiOrDuzeltme : null,
                        MezuniyetBasvurulariTezDosyasi = TD,
                        UzatmaSuresiGun = mOT.MBSinavUzatmaSuresiGun,
                        MezuniyetSuresiGun = mOT.MBSinavUzatmaSuresiGun,
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
                        MezuniyetSinavDurumID = Msd.MezuniyetSinavDurumID,
                        MezuniyetSinavDurumAdi = Msd != null ? Msd.MezuniyetSinavDurumAdi : "",
                        SDurumClassName = Msd != null ? Msd.ClassName : "",
                        SDurumColor = Msd != null ? Msd.Color : "",
                        MezuniyetYayinKontrolDurumAciklamasi = s.MezuniyetYayinKontrolDurumAciklamasi,


                    };
            var q2 = q;

            //Tez danışmanları sadece kendi öğrencilerini görsün
            var JoFormKayitYetki = RoleNames.MezuniyetGelenBasvurularJuriOneriFormuKayit.InRoleCurrent();
            var MBGelenBKayitYetki = RoleNames.MezuniyetGelenBasvurularKayit.InRoleCurrent();
            if (JoFormKayitYetki && !MBGelenBKayitYetki)
            {
                q = q.Where(p => p.TezDanismanID == UserIdentity.Current.Id);
            }
            if (model.MezuniyetSureci.HasValue)
            {
                int BasYil = model.MezuniyetSureci.ToString().Substring(0, 4).ToInt().Value;
                int DonemID = model.MezuniyetSureci.ToString().Substring(4, 1).ToInt().Value;
                q = q.Where(p => p.SurecBaslangicYil == BasYil && p.DonemID == DonemID);
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
                    var IsOnaylandiOrDuzeltme = (model.TDDurumID == 1);
                    q = q.Where(p => p.IsOnaylandiOrDuzeltme == IsOnaylandiOrDuzeltme);
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
                var IsMezunOldu = model.MezuniyetDurumID.HasValue ? (model.MezuniyetDurumID == 1 ? true : false) : (bool?)null;
                q = q.Where(p => p.IsMezunOldu == IsMezunOldu);

                if (IsMezunOldu == true)
                {
                    if (model.MBaslangicTarihi.HasValue && model.MBitisTarihi.HasValue) q = q.Where(p => model.MBaslangicTarihi <= p.MezuniyetTarihi && model.MBitisTarihi >= p.MezuniyetTarihi);
                    else if (model.MBaslangicTarihi.HasValue) q = q.Where(p => model.MBaslangicTarihi == p.MezuniyetTarihi);
                    else if (model.MBitisTarihi.HasValue) q = q.Where(p => model.MBitisTarihi == p.MezuniyetTarihi);
                }
            }
            if (!model.AdSoyad.IsNullOrWhiteSpace())
            {
                model.AdSoyad = model.AdSoyad.Trim();
                q = q.Where(p => p.AdSoyad.Contains(model.AdSoyad) || p.TcPasaPortNo == model.AdSoyad || p.OgrenciNo == model.AdSoyad || p.FormNo == model.AdSoyad || p.TezDanismanAdi.Contains(model.AdSoyad));
            }
            if (model.UyrukKod.HasValue) q = q.Where(p => p.UyrukKod == model.UyrukKod);
            bool isFiltered = false;
            if (q != q2)
                isFiltered = true;

            model.RowCount = q.Count();
            if (!model.Sort.IsNullOrWhiteSpace()) q = q.OrderBy(model.Sort);

            else
            {
                if (model.JuriOneriFormuDurumuID == 2)
                    q = q.OrderBy(o => o.MezuniyetJuriOneriFormu.EYKYaGonderildiIslemTarihi);
                else q = q.OrderByDescending(o => o.BasvuruTarihi);
            }
            var PS = Management.setStartRowInx(model.StartRowIndex, model.PageIndex, model.PageCount, model.RowCount, model.PageSize);
            model.PageIndex = PS.PageIndex;
            var qdata = q.Skip(PS.StartRowIndex).Take(model.PageSize).ToList();
            model.Data = qdata;
            #region export
            if (export && model.RowCount > 0)
            {
                var gv = new GridView();
                var qExp = q.ToList();
                gv.DataSource = (from s in qExp
                                 join td in db.Kullanicilars on s.TezDanismanID equals td.KullaniciID
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
                                     s.TcPasaPortNo,
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

            ViewBag.MezuniyetSurecID = new SelectList(Management.getmezuniyetSurecleri(EnstituKod, true), "Value", "Caption", model.MezuniyetSurecID);
            ViewBag.MezuniyetSureci = new SelectList(Management.getMezuniyetSurecGroup(EnstituKod, true), "Value", "Caption", model.MezuniyetSureci);
            ViewBag.OgrenimTipKod = new SelectList(Management.cmbAktifOgrenimTipleri(_EnstituKod, true, true), "Value", "Caption", model.OgrenimTipKod);
            ViewBag.MezuniyetYayinKontrolDurumID = new SelectList(Management.cmbMezuniyetYayinDurumListe(true, true), "Value", "Caption", model.MezuniyetYayinKontrolDurumID);
            ViewBag.JuriOneriFormuDurumuID = new SelectList(Management.cmbJuriOneriFormuDurumu(true), "Value", "Caption", model.JuriOneriFormuDurumuID);
            ViewBag.KayitDonemi = new SelectList(Management.getmezuniyetKayitDonemleri(EnstituKod, model.MezuniyetSurecID, true), "Value", "Caption", model.KayitDonemi);
            ViewBag.SRDurumID = new SelectList(Management.cmbSRDurumListe(true), "Value", "Caption", model.SRDurumID);
            ViewBag.TDDurumID = new SelectList(Management.cmbTDDurumListe(true), "Value", "Caption", model.TDDurumID);
            ViewBag.MezuniyetSinavDurumID = new SelectList(Management.cmbMzSinavDurumListe(true), "Value", "Caption", model.MezuniyetSinavDurumID);
            ViewBag.TeslimFormDurumu = new SelectList(Management.cmbTeslimFormDurumu(true), "Value", "Caption", model.TeslimFormDurumu);
            ViewBag.MezuniyetDurumID = new SelectList(Management.cmbMezuniyetDurumIDListe(true), "Value", "Caption", model.MezuniyetDurumID);
            return View(model);
        }


        [Authorize(Roles = RoleNames.MezuniyetGelenBasvurularKayit)]
        public ActionResult YayinKontrol(int id, int MezuniyetBasvurulariYayinID)
        {

            var model = Management.getSecilenBasvuruMezuniyetDetay(id, MezuniyetBasvurulariYayinID);
            return View(model);
        }
        [Authorize(Roles = RoleNames.MezuniyetGelenBasvurularKayit)]
        public ActionResult YayinKontrolPost(int id, bool? DanismanIsmiVar, bool? TezIcerikUyumuVar, bool? Onaylandi)
        {
            var mmMessage = new MmMessage();


            if (Onaylandi.HasValue)
            {
                if (DanismanIsmiVar.HasValue == false)
                {
                    mmMessage.Messages.Add("Onaylama işlemini yapabilmeniz için 'Danışman İsmi Var Mı' sorusunu cevaplayınız");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "DanismanIsmiVar" });
                }
                if (TezIcerikUyumuVar.HasValue == false)
                {
                    mmMessage.Messages.Add("Onaylama işlemini yapabilmeniz için 'Tez İçeriği ile Uyumlu mu' sorusunu cevaplayınız");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = Msgtype.Warning, PropertyName = "TezIcerikUyumuVar" });
                }
            }
            if (mmMessage.Messages.Count == 0)
            {
                var yayin = db.MezuniyetBasvurulariYayins.Where(p => p.MezuniyetBasvurulariYayinID == id).First();
                yayin.DanismanIsmiVar = DanismanIsmiVar;
                yayin.TezIcerikUyumuVar = TezIcerikUyumuVar;
                yayin.Onaylandi = Onaylandi;
                yayin.IslemTarihi = DateTime.Now;
                yayin.IslemYapanID = UserIdentity.Current.Id;
                yayin.IslemYapanIP = UserIdentity.Ip;
                db.SaveChanges();
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
        public ActionResult YayinIndexUpdate(int id, int IndexID)
        {
            var mmMessage = new MmMessage();
            var kayit = db.MezuniyetBasvurulariYayins.Where(p => p.MezuniyetBasvurulariYayinID == id).FirstOrDefault();
            try
            {
                kayit.MezuniyetYayinIndexTurID = IndexID;
                db.SaveChanges();
                LogIslemleri.LogEkle("MezuniyetBasvurulariYayins", IslemTipi.Update, kayit.ToJson());
                mmMessage.Messages.Add("Index Bilgisi Güncellendi");
                mmMessage.MessageType = Msgtype.Success;

            }
            catch (Exception ex)
            {
                mmMessage.MessageType = Msgtype.Error;
                mmMessage.IsSuccess = false;
                mmMessage.Messages.Add("Index Bilgisi Güncellenirken bir hata oluştu! Hata:" + ex.ToExceptionMessage());
                Management.SistemBilgisiKaydet(ex.ToExceptionMessage(), "MezuniyetGelenBasvurular/YayinIndexUpdate<br/><br/>" + ex.ToExceptionStackTrace(), BilgiTipi.OnemsizHata);
            }
            var strView = Management.RenderPartialView("Ajax", "getMessage", mmMessage);
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
                var mBasvur = db.MezuniyetBasvurularis.Where(p => p.MezuniyetBasvurulariID == id).First();
                var mgonder = false;
                if ((MezuniyetYayinKontrolDurumID == MezuniyetYayinKontrolDurumu.IptalEdildi || MezuniyetYayinKontrolDurumID == MezuniyetYayinKontrolDurumu.KabulEdildi) && MezuniyetYayinKontrolDurumID != mBasvur.MezuniyetYayinKontrolDurumID) mgonder = true;

                mBasvur.MezuniyetYayinKontrolDurumID = MezuniyetYayinKontrolDurumID.Value;
                mBasvur.MezuniyetYayinKontrolDurumAciklamasi = MezuniyetYayinKontrolDurumAciklamasi;
                mBasvur.IslemTarihi = DateTime.Now;
                mBasvur.IslemYapanID = UserIdentity.Current.Id;
                mBasvur.IslemYapanIP = UserIdentity.Ip;
                db.SaveChanges();
                LogIslemleri.LogEkle("MezuniyetBasvurulari", IslemTipi.Update, mBasvur.ToJson());
                mmMessage.IsSuccess = true;
                #region sendMail
                if (mgonder)
                {
                    var Enstitu = mBasvur.MezuniyetSureci.Enstituler;
                    var Sablonlar = db.MailSablonlaris.Where(p => p.EnstituKod == Enstitu.EnstituKod).ToList();


                    var mModel = new List<SablonMailModel>();
                    if (MezuniyetYayinKontrolDurumID != MezuniyetYayinKontrolDurumu.IptalEdildi)
                    {
                        var Danisman = db.Kullanicilars.Where(p => p.KullaniciID == mBasvur.TezDanismanID).First();
                        mModel.Add(
                            new SablonMailModel
                            {

                                MailSablonTipID = MailSablonTipi.Mez_YayinSartiSaglandiDanisman,
                                AdSoyad = Danisman.Ad + " " + Danisman.Soyad,
                                EMails = new List<MailSendList> { new MailSendList { EMail = Danisman.EMail, ToOrBcc = true } },
                                UnvanAdi = Danisman.Unvanlar.UnvanAdi
                            });
                    }
                    var OgrenciMailSablonID = 1;
                    if (MezuniyetYayinKontrolDurumID == MezuniyetYayinKontrolDurumu.IptalEdildi) OgrenciMailSablonID = MailSablonTipi.Mez_YayinSartiSaglanamadiOgrenci;
                    else if (mBasvur.OgrenimTipKod == OgrenimTipi.Doktra) OgrenciMailSablonID = MailSablonTipi.Mez_YayinSartiSaglandiOgrenciDoktora;
                    else OgrenciMailSablonID = MailSablonTipi.Mez_YayinSartiSaglandiOgrenciYL;
                    mModel.Add(new SablonMailModel
                    {

                        AdSoyad = mBasvur.Ad + " " + mBasvur.Soyad,
                        EMails = new List<MailSendList> { new MailSendList { EMail = mBasvur.Kullanicilar.EMail, ToOrBcc = true } },
                        MailSablonTipID = OgrenciMailSablonID,
                    });


                    foreach (var item in mModel)
                    {
                        var BasvuruDonemAdi = mBasvur.MezuniyetSureci.BaslangicYil + " " + mBasvur.MezuniyetSureci.BitisYil + " / " + mBasvur.MezuniyetSureci.Donemler.DonemAdi;
                        var EnstituL = mBasvur.MezuniyetSureci.Enstituler;

                        item.Sablon = Sablonlar.Where(p => p.MailSablonTipID == item.MailSablonTipID).First();
                        item.SablonParametreleri = item.Sablon.MailSablonTipleri.Parametreler.Split(',').ToList().Select(s => s.Trim()).ToList();

                        if (item.Sablon.GonderilecekEkEpostalar != null) item.EMails.AddRange(item.Sablon.GonderilecekEkEpostalar.Split(',').Select(s => new MailSendList { EMail = s.Trim(), ToOrBcc = false }).ToList());
                        var ParamereDegerleri = new List<MailReplaceParameterModel>();
                        if (item.SablonParametreleri.Any(a => a == "@EnstituAdi"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "EnstituAdi", Value = EnstituL.EnstituAd });
                        if (item.SablonParametreleri.Any(a => a == "@WebAdresi"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "WebAdresi", Value = Enstitu.WebAdresi, IsLink = true });
                        if (item.SablonParametreleri.Any(a => a == "@BasvuruDonemAdi"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "BasvuruDonemAdi", Value = BasvuruDonemAdi });
                        if (item.SablonParametreleri.Any(a => a == "@AdSoyad"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "AdSoyad", Value = item.AdSoyad });
                        if (item.SablonParametreleri.Any(a => a == "@UnvanAdi"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "UnvanAdi", Value = item.UnvanAdi });
                        if (item.SablonParametreleri.Any(a => a == "@OgrenciAdSoyad"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "OgrenciAdSoyad", Value = mBasvur.Ad + " " + mBasvur.Soyad });
                        if (item.SablonParametreleri.Any(a => a == "@IptalAciklamasi"))
                            ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "IptalAciklamasi", Value = mBasvur.MezuniyetYayinKontrolDurumAciklamasi });

                        var Attachs = new List<System.Net.Mail.Attachment>();

                        if (item.MailSablonTipID != MailSablonTipi.Mez_YayinSartiSaglandiDanisman && MezuniyetYayinKontrolDurumID != MezuniyetYayinKontrolDurumu.IptalEdildi)
                        {
                            Attachs = Management.exportRaporPdf(RaporTipleri.MezuniyetBasvuruRaporu, new List<int?> { mBasvur.MezuniyetBasvurulariID });
                        }
                        if (MezuniyetYayinKontrolDurumID == MezuniyetYayinKontrolDurumu.KabulEdildi)
                        {
                            var TTFP = Management.exportRaporPdf(RaporTipleri.MezuniyetTezTeslimFormu, new List<int?> { mBasvur.MezuniyetBasvurulariID, 1 });
                            Attachs.AddRange(TTFP);
                        }
                        var mCOntent = SystemMails.GetSystemMailContent(EnstituL.EnstituAd, item.Sablon.SablonHtml, item.Sablon.SablonAdi, ParamereDegerleri);
                        var snded = MailManager.sendMail(Enstitu.EnstituKod, mCOntent.Title, mCOntent.HtmlContent, item.EMails, Attachs);
                        if (snded)
                        {
                            var kModel = new GonderilenMailler();
                            kModel.Tarih = DateTime.Now;
                            kModel.EnstituKod = Enstitu.EnstituKod;
                            kModel.MesajID = null;
                            kModel.IslemTarihi = DateTime.Now;
                            kModel.Konu = item.Sablon.SablonAdi + " (" + item.AdSoyad + ")";
                            if (!item.JuriTipAdi.IsNullOrWhiteSpace()) kModel.Konu = kModel.Konu + " (" + item.JuriTipAdi + ")";
                            kModel.IslemYapanID = UserIdentity.Current == null || !UserIdentity.Current.IsAuthenticated ? 1 : UserIdentity.Current.Id;
                            kModel.IslemYapanIP = UserIdentity.Ip;
                            kModel.Aciklama = item.Sablon.Sablon ?? "";
                            kModel.AciklamaHtml = mCOntent.HtmlContent ?? "";
                            kModel.Gonderildi = true;
                            kModel.GonderilenMailEkleris = Attachs.Select(s => new GonderilenMailEkleri { EkAdi = s.Name, EkDosyaYolu = "" }).ToList();
                            kModel.GonderilenMailKullanicilars = item.EMails.Select(s => new GonderilenMailKullanicilar { Email = s.EMail }).ToList();
                            db.GonderilenMaillers.Add(kModel);
                            db.SaveChanges();
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
            var mmMessage = new MmMessage();

            mmMessage.Title = "Mezuniyet başvurusu danışman onay işlemi";
            var mBasvur = db.MezuniyetBasvurularis.Where(p => p.MezuniyetBasvurulariID == id).First();
            var KayitYetki = RoleNames.GelenBasvurularKayit.InRole();

            if (!KayitYetki)
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
                bool SendMail = false;
                if (mmMessage.Messages.Count == 0)
                {

                    if (IsDanismanOnay != mBasvur.IsDanismanOnay)
                    {
                        SendMail = true;
                        mBasvur.TezTeslimUniqueID = Guid.NewGuid();
                        mBasvur.TezTeslimFormKodu = Guid.NewGuid().ToString().Substring(0, 8);
                    }
                    mBasvur.IsDanismanOnay = IsDanismanOnay;
                    mBasvur.DanismanOnayAciklama = BasvuruDanismanOnayAciklama;
                    mBasvur.DanismanOnayTarihi = DateTime.Now;




                    db.SaveChanges();
                    LogIslemleri.LogEkle("MezuniyetBasvurulari", IslemTipi.Update, mBasvur.ToJson());
                    mmMessage.IsSuccess = true;
                    mmMessage.Messages.Add(IsDanismanOnay.HasValue ? (IsDanismanOnay.Value ? "Başvuru Onaylandı." : "Başvuru Ret Edildi.") : "Onaylama İşlemi Geril Alındı.");
                    if (SendMail)
                    {
                        #region sendMail
                        var Enstitu = mBasvur.MezuniyetSureci.Enstituler;
                        var SablonTipID = mBasvur.IsDanismanOnay == true ? MailSablonTipi.Mez_DanismanOnayladiOgrenci : MailSablonTipi.Mez_DanismanOnaylamadiOgrenci;
                        var Sablonlar = db.MailSablonlaris.Where(p => p.EnstituKod == Enstitu.EnstituKod).ToList();



                        var mModel = new List<SablonMailModel>();

                        mModel.Add(new SablonMailModel
                        {

                            AdSoyad = mBasvur.Ad + " " + mBasvur.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = mBasvur.Kullanicilar.EMail, ToOrBcc = true } },
                            MailSablonTipID = SablonTipID,
                        });

                        var Danisman = db.Kullanicilars.Where(p => p.KullaniciID == mBasvur.TezDanismanID).First();
                        foreach (var item in mModel)
                        {
                            var BasvuruDonemAdi = mBasvur.MezuniyetSureci.BaslangicYil + " " + mBasvur.MezuniyetSureci.BitisYil + " / " + mBasvur.MezuniyetSureci.Donemler.DonemAdi;
                            var EnstituL = mBasvur.MezuniyetSureci.Enstituler;

                            item.Sablon = Sablonlar.Where(p => p.MailSablonTipID == item.MailSablonTipID).First();
                            item.SablonParametreleri = item.Sablon.MailSablonTipleri.Parametreler.Split(',').ToList().Select(s => s.Trim()).ToList();

                            if (item.Sablon.GonderilecekEkEpostalar != null) item.EMails.AddRange(item.Sablon.GonderilecekEkEpostalar.Split(',').Select(s => new MailSendList { EMail = s.Trim(), ToOrBcc = false }).ToList());
                            var ParamereDegerleri = new List<MailReplaceParameterModel>();
                            if (item.SablonParametreleri.Any(a => a == "@EnstituAdi"))
                                ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "EnstituAdi", Value = EnstituL.EnstituAd });
                            if (item.SablonParametreleri.Any(a => a == "@WebAdresi"))
                                ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "WebAdresi", Value = Enstitu.WebAdresi, IsLink = true });
                            if (item.SablonParametreleri.Any(a => a == "@BasvuruDonemAdi"))
                                ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "BasvuruDonemAdi", Value = BasvuruDonemAdi });
                            if (item.SablonParametreleri.Any(a => a == "@DanismanAdSoyad"))
                                ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "DanismanAdSoyad", Value = Danisman.Ad + " " + Danisman.Soyad });
                            if (item.SablonParametreleri.Any(a => a == "@DanismanUnvanAdi"))
                                ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "DanismanUnvanAdi", Value = Danisman.Unvanlar.UnvanAdi });
                            if (item.SablonParametreleri.Any(a => a == "@OgrenciNo"))
                                ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "OgrenciNo", Value = mBasvur.OgrenciNo });
                            if (item.SablonParametreleri.Any(a => a == "@OgrenciAdSoyad"))
                                ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "OgrenciAdSoyad", Value = mBasvur.Ad + " " + mBasvur.Soyad });
                            if (item.SablonParametreleri.Any(a => a == "@RetAciklamasi"))
                                ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "RetAciklamasi", Value = mBasvur.DanismanOnayAciklama });

                            var Attachs = new List<System.Net.Mail.Attachment>();

                            var mCOntent = SystemMails.GetSystemMailContent(EnstituL.EnstituAd, item.Sablon.SablonHtml, item.Sablon.SablonAdi, ParamereDegerleri);
                            var snded = MailManager.sendMail(Enstitu.EnstituKod, mCOntent.Title, mCOntent.HtmlContent, item.EMails, Attachs);
                            if (snded)
                            {
                                var kModel = new GonderilenMailler();
                                kModel.Tarih = DateTime.Now;
                                kModel.EnstituKod = Enstitu.EnstituKod;
                                kModel.MesajID = null;
                                kModel.IslemTarihi = DateTime.Now;
                                kModel.Konu = item.Sablon.SablonAdi + " (" + item.AdSoyad + ")";
                                if (!item.JuriTipAdi.IsNullOrWhiteSpace()) kModel.Konu = kModel.Konu + " (" + item.JuriTipAdi + ")";
                                kModel.IslemYapanID = UserIdentity.Current == null || !UserIdentity.Current.IsAuthenticated ? 1 : UserIdentity.Current.Id;
                                kModel.IslemYapanIP = UserIdentity.Ip;
                                kModel.Aciklama = item.Sablon.Sablon ?? "";
                                kModel.AciklamaHtml = mCOntent.HtmlContent ?? "";
                                kModel.Gonderildi = true;
                                kModel.GonderilenMailEkleris = Attachs.Select(s => new GonderilenMailEkleri { EkAdi = s.Name, EkDosyaYolu = "" }).ToList();
                                kModel.GonderilenMailKullanicilars = item.EMails.Select(s => new GonderilenMailKullanicilar { Email = s.EMail }).ToList();
                                db.GonderilenMaillers.Add(kModel);
                                db.SaveChanges();
                            }
                        }

                        #endregion 
                    }



                }
            }
            mmMessage.MessageType = mmMessage.IsSuccess ? Msgtype.Success : Msgtype.Warning;
            return Json(new { Messages = mmMessage }, "application/json", JsonRequestBehavior.AllowGet);

        }

        public ActionResult DanismanUzatmaOnayKayit(int SRTalepID, bool? IsDanismanUzatmaSonrasiOnay, string DanismanUzatmaSonrasiOnayAciklama)
        {
            var mmMessage = new MmMessage();

            mmMessage.Title = "Uzatma sonrası danışman onay işlemi";
            var SrTalep = db.SRTalepleris.Where(p => p.SRTalepID == SRTalepID).First();
            var KayitYetki = RoleNames.GelenBasvurularKayit.InRole();

            if (!KayitYetki)
            {
                if (SrTalep.MezuniyetBasvurulari.TezDanismanID != UserIdentity.Current.Id)
                {
                    mmMessage.Messages.Add("Danışman olarak atanmadığını bir mezuniyet başvurusu için onay işlemi yapamazsınız!");
                }
            }
            if (db.SRTalepleris.Any(a => a.SRTalepID > SRTalepID && a.MezuniyetBasvurulariID == SrTalep.MezuniyetSinavDurumID))
            {
                mmMessage.Messages.Add("Öğrenci tarafından yeni sınav talebi oluşturuldu. Bu işlemi yapamazsınız.");
            }
            if (!mmMessage.Messages.Any())
            {

                if (IsDanismanUzatmaSonrasiOnay == false && DanismanUzatmaSonrasiOnayAciklama.IsNullOrWhiteSpace())
                {
                    mmMessage.Messages.Add("Öğrenci Başvurusunu Reddediyorum seçeneği seçilirse Açıklama girilmesi zorunludur.");
                }
                bool SendMail = false;
                if (mmMessage.Messages.Count == 0)
                {

                    if (IsDanismanUzatmaSonrasiOnay != SrTalep.IsDanismanUzatmaSonrasiOnay)
                    {
                        SendMail = true;
                    }

                    SrTalep.IsDanismanUzatmaSonrasiOnay = IsDanismanUzatmaSonrasiOnay;
                    SrTalep.DanismanUzatmaSonrasiOnayAciklama = DanismanUzatmaSonrasiOnayAciklama;
                    SrTalep.DanismanOnayTarihi = DateTime.Now;

                    db.SaveChanges();
                    LogIslemleri.LogEkle("SRTalebi", IslemTipi.Update, SrTalep.ToJson());
                    mmMessage.IsSuccess = true;
                    mmMessage.Messages.Add(IsDanismanUzatmaSonrasiOnay.HasValue ? (IsDanismanUzatmaSonrasiOnay.Value ? "Başvuru Onaylandı." : "Başvuru Ret Edildi.") : "Onaylama İşlemi Geril Alındı.");
                    if (SendMail && false)
                    {
                        #region sendMail
                        var MB = SrTalep.MezuniyetBasvurulari;
                        var BasvuruSurec = MB.MezuniyetSureci;
                        var Enstitu = BasvuruSurec.Enstituler;
                        var SablonTipID = SrTalep.IsOgrenciUzatmaSonrasiOnay == true ? MailSablonTipi.Mez_DanismanOnayladiOgrenci : MailSablonTipi.Mez_DanismanOnaylamadiOgrenci;
                        var Sablonlar = db.MailSablonlaris.Where(p => p.EnstituKod == Enstitu.EnstituKod).ToList();

                        var mModel = new List<SablonMailModel>();

                        mModel.Add(new SablonMailModel
                        {

                            AdSoyad = MB.Ad + " " + MB.Soyad,
                            EMails = new List<MailSendList> { new MailSendList { EMail = SrTalep.Kullanicilar.EMail, ToOrBcc = true } },
                            MailSablonTipID = SablonTipID,
                        });

                        var Danisman = db.Kullanicilars.Where(p => p.KullaniciID == MB.TezDanismanID).First();

                        foreach (var item in mModel)
                        {
                            var BasvuruDonemAdi = BasvuruSurec.BaslangicYil + " " + BasvuruSurec.BitisYil + " / " + BasvuruSurec.Donemler.DonemAdi;
                            var EnstituL = Enstitu;

                            item.Sablon = Sablonlar.Where(p => p.MailSablonTipID == item.MailSablonTipID).First();
                            item.SablonParametreleri = item.Sablon.MailSablonTipleri.Parametreler.Split(',').ToList().Select(s => s.Trim()).ToList();

                            if (item.Sablon.GonderilecekEkEpostalar != null) item.EMails.AddRange(item.Sablon.GonderilecekEkEpostalar.Split(',').Select(s => new MailSendList { EMail = s.Trim(), ToOrBcc = false }).ToList());
                            var ParamereDegerleri = new List<MailReplaceParameterModel>();
                            if (item.SablonParametreleri.Any(a => a == "@EnstituAdi"))
                                ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "EnstituAdi", Value = EnstituL.EnstituAd });
                            if (item.SablonParametreleri.Any(a => a == "@WebAdresi"))
                                ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "WebAdresi", Value = Enstitu.WebAdresi, IsLink = true });
                            if (item.SablonParametreleri.Any(a => a == "@BasvuruDonemAdi"))
                                ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "BasvuruDonemAdi", Value = BasvuruDonemAdi });
                            if (item.SablonParametreleri.Any(a => a == "@DanismanAdSoyad"))
                                ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "DanismanAdSoyad", Value = Danisman.Ad + " " + Danisman.Soyad });
                            if (item.SablonParametreleri.Any(a => a == "@DanismanUnvanAdi"))
                                ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "DanismanUnvanAdi", Value = Danisman.Unvanlar.UnvanAdi });
                            if (item.SablonParametreleri.Any(a => a == "@OgrenciNo"))
                                ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "OgrenciNo", Value = MB.OgrenciNo });
                            if (item.SablonParametreleri.Any(a => a == "@OgrenciAdSoyad"))
                                ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "OgrenciAdSoyad", Value = MB.Ad + " " + MB.Soyad });

                            var Attachs = new List<System.Net.Mail.Attachment>();

                            var mCOntent = SystemMails.GetSystemMailContent(EnstituL.EnstituAd, item.Sablon.SablonHtml, item.Sablon.SablonAdi, ParamereDegerleri);
                            var snded = MailManager.sendMail(Enstitu.EnstituKod, mCOntent.Title, mCOntent.HtmlContent, item.EMails, Attachs);
                            if (snded)
                            {
                                var kModel = new GonderilenMailler();
                                kModel.Tarih = DateTime.Now;
                                kModel.EnstituKod = Enstitu.EnstituKod;
                                kModel.MesajID = null;
                                kModel.IslemTarihi = DateTime.Now;
                                kModel.Konu = item.Sablon.SablonAdi + " (" + item.AdSoyad + ")";
                                if (!item.JuriTipAdi.IsNullOrWhiteSpace()) kModel.Konu = kModel.Konu + " (" + item.JuriTipAdi + ")";
                                kModel.IslemYapanID = UserIdentity.Current == null || !UserIdentity.Current.IsAuthenticated ? 1 : UserIdentity.Current.Id;
                                kModel.IslemYapanIP = UserIdentity.Ip;
                                kModel.Aciklama = item.Sablon.Sablon ?? "";
                                kModel.AciklamaHtml = mCOntent.HtmlContent ?? "";
                                kModel.Gonderildi = true;
                                kModel.GonderilenMailEkleris = Attachs.Select(s => new GonderilenMailEkleri { EkAdi = s.Name, EkDosyaYolu = "" }).ToList();
                                kModel.GonderilenMailKullanicilars = item.EMails.Select(s => new GonderilenMailKullanicilar { Email = s.EMail }).ToList();
                                db.GonderilenMaillers.Add(kModel);
                                db.SaveChanges();
                            }
                        }

                        #endregion 
                    }



                }
            }
            mmMessage.MessageType = mmMessage.IsSuccess ? Msgtype.Success : Msgtype.Warning;
            return Json(new { Messages = mmMessage }, "application/json", JsonRequestBehavior.AllowGet);

        }

        [Authorize(Roles = RoleNames.MezuniyetGelenBasvurularKayit)]
        public ActionResult SRDurumKaydet(int id, int SRDurumID, string SRDurumAciklamasi)
        {
            string strView = "";
            string fWeight = "font-weight:";

            var talep = db.SRTalepleris.Where(p => p.SRTalepID == id).First();


            fWeight += Convert.ToDateTime(talep.Tarih.ToShortDateString() + " " + talep.BasSaat) > DateTime.Now ? "bold;" : "normal;";


            if (SRDurumID == SRTalepDurum.Onaylandı && talep.SRSalonID.HasValue)
            {
                var qTalepEslesen = db.SRTalepleris.Where(a => a.SRTalepID != talep.SRTalepID && a.SRSalonID == talep.SRSalonID && a.Tarih == talep.Tarih &&
                                        (
                                          (a.BasSaat == talep.BasSaat || a.BitSaat == talep.BitSaat) ||
                                        (
                                            (a.BasSaat < talep.BasSaat && a.BitSaat > talep.BasSaat) || a.BasSaat < talep.BitSaat && a.BitSaat > talep.BitSaat) ||
                                            (a.BasSaat > talep.BasSaat && a.BasSaat < talep.BitSaat) || a.BitSaat > talep.BasSaat && a.BitSaat < talep.BitSaat)
                                        ).ToList();
                if (talep.MezuniyetBasvurulari.OgrenimTipKod != OgrenimTipi.TezliYuksekLisans && qTalepEslesen.Any(p => p.SRDurumID == SRTalepDurum.Onaylandı))
                {

                    var salon = db.SRSalonlars.Where(p => p.SRSalonID == talep.SRSalonID).First();
                    string msg = talep.Tarih.ToShortDateString() + " " + talep.BasSaat.ToString() + " - " + talep.BitSaat.ToString() + " Tarihi için '" + salon.SalonAdi + "' Salonu doludur bu rezervasyon onaylanamaz!";
                    var mmMessage = new MmMessage();
                    mmMessage.Messages.Add(msg);
                    mmMessage.IsSuccess = false;
                    mmMessage.MessageType = Msgtype.Error;
                    strView = Management.RenderPartialView("Ajax", "getMessage", mmMessage);
                }
            }

            bool SendMail = talep.SRDurumID != SRDurumID && new List<int> { SRTalepDurum.Reddedildi, SRTalepDurum.Onaylandı }.Contains(SRDurumID);
            talep.SRDurumID = SRDurumID;
            talep.IslemTarihi = DateTime.Now;
            talep.IslemYapanID = UserIdentity.Current.Id;
            talep.IslemYapanIP = UserIdentity.Ip;
            if (SRDurumID == SRTalepDurum.Reddedildi) talep.SRDurumAciklamasi = SRDurumAciklamasi;
            db.SaveChanges();
            LogIslemleri.LogEkle("SRTalepleri", IslemTipi.Update, talep.ToJson());
            var qbDrm = talep.SRDurumlari;

            if (talep.SRTalepTipleri.IsTezSinavi && SendMail)
            {
                var msgs = Management.sendMailMezuniyetSinavYerBilgisi(id, SRDurumID == SRTalepDurum.Onaylandı);
                if (msgs.Messages.Count > 0)
                {
                    strView = Management.RenderPartialView("Ajax", "getMessage", msgs);
                }
            }
            return new
            {
                IslemTipListeAdi = qbDrm.DurumAdi,
                ClassName = qbDrm.ClassName,
                Color = qbDrm.Color,
                FontWeight = fWeight,
                strView = strView
            }.toJsonResult();
        }
        [Authorize(Roles = RoleNames.MezuniyetGelenBasvurularKayit)]
        public ActionResult SRSinavDurumKaydet(int id, int MezuniyetSinavDurumID, DateTime? TezTeslimSonTarih)
        {
            var mmMessage = new MmMessage();
            mmMessage.IsSuccess = false;

            var talep = db.SRTalepleris.Where(p => p.SRTalepID == id).First();
            if (MezuniyetSinavDurumID == MezuniyetSinavDurum.Uzatma && talep.MezuniyetBasvurulari.SRTalepleris.Any(a => a.SRTalepID != id && a.SRDurumID == SRTalepDurum.Onaylandı && a.MezuniyetSinavDurumID == MezuniyetSinavDurum.Uzatma))
            {
                mmMessage.Messages.Add("Bu mezuniyet başvurusuna daha önceden uzatma hakkı verildiğinden tekrar uzatma hakkı verilemez!");
            }
            else if (talep.JuriSonucMezuniyetSinavDurumID.HasValue && MezuniyetSinavDurumID > MezuniyetSinavDurum.SonucGirilmedi && talep.JuriSonucMezuniyetSinavDurumID != MezuniyetSinavDurumID)
            {
                mmMessage.Messages.Add("Girdiğiniz sınav sonucu jürinin oylama sonucu ile aynı olması gerekmetkedir!");
            }
            if (!mmMessage.Messages.Any())
            {
                if (MezuniyetSinavDurumID == MezuniyetSinavDurum.Basarili)
                {
                    var MbOKriters = talep.MezuniyetBasvurulari.MezuniyetSureci.MezuniyetSureciOgrenimTipKriterleris.Where(p => p.OgrenimTipKod == talep.MezuniyetBasvurulari.OgrenimTipKod).First();
                    var TTEkSureYetki = RoleNames.MezuniyetGelenBasvurularTTEkSure.InRoleCurrent();

                    if (TezTeslimSonTarih.HasValue && !TTEkSureYetki && TezTeslimSonTarih.Value > talep.Tarih.AddDays(MbOKriters.MBTezTeslimSuresiGun))
                    {
                        mmMessage.Messages.Add("Tez teslim son tarih kriteri " + talep.Tarih.AddDays(MbOKriters.MBTezTeslimSuresiGun).ToDateString() + " tarihinden daha büyük olamaz!");
                    }
                }
                else TezTeslimSonTarih = null;
            }
            var strView = "";
            if (mmMessage.Messages.Count == 0)
            {
                bool SendMailSinav = talep.MezuniyetSinavDurumID != MezuniyetSinavDurumID && talep.MezuniyetSinavDurumID.HasValue;


                //if (talep.MezuniyetSinavDurumID != MezuniyetSinavDurumID)
                //{
                talep.MezuniyetSinavDurumID = MezuniyetSinavDurumID;
                talep.MezuniyetBasvurulari.MezuniyetSinavDurumID = MezuniyetSinavDurumID;
                talep.MezuniyetBasvurulari.TezTeslimSonTarih = TezTeslimSonTarih;
                talep.MezuniyetSinavDurumIslemTarihi = DateTime.Now;
                talep.MezuniyetBasvurulari.MezuniyetSinavDurumIslemTarihi = DateTime.Now;
                talep.MezuniyetSinavDurumIslemYapanID = UserIdentity.Current.Id;
                talep.MezuniyetBasvurulari.MezuniyetSinavDurumIslemYapanID = UserIdentity.Current.Id;
                db.SaveChanges();
                LogIslemleri.LogEkle("MezuniyetBasvurulari", IslemTipi.Update, talep.MezuniyetBasvurulari.ToJson());
                //}
                var drm = db.MezuniyetSinavDurumlaris.Where(p => p.MezuniyetSinavDurumID == MezuniyetSinavDurumID).First();
                mmMessage.IsSuccess = true;


                if (SendMailSinav && new List<int> { MezuniyetSinavDurum.Basarili, MezuniyetSinavDurum.Uzatma }.Contains(talep.MezuniyetSinavDurumID.Value))
                {
                    mmMessage = Management.sendMailMezuniyetSinavSonucu(id, talep.MezuniyetSinavDurumID.Value);

                }
            }
            mmMessage.Title = "Sınav durumu kayıt işlemi";
            mmMessage.MessageType = mmMessage.IsSuccess ? Msgtype.Success : Msgtype.Warning;
            strView = mmMessage.Messages.Count > 0 ? Management.RenderPartialView("Ajax", "getMessage", mmMessage) : "";

            return new
            {
                mmMessage.IsSuccess,
                Messages = strView
            }.toJsonResult();
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult TdDurumKaydet(MezuniyetBasvurulariTezDosyalari kModel)
        {
            var mmMessage = new MmMessage();
            mmMessage.IsSuccess = false;

            var talep = db.MezuniyetBasvurulariTezDosyalaris.Where(p => p.MezuniyetBasvurulariTezDosyaID == kModel.MezuniyetBasvurulariTezDosyaID).First();
            var KYetki = RoleNames.MezuniyetGelenBasvurularKayit.InRoleCurrent() || RoleNames.MezuniyetGelenBasvurularTezKontrol.InRoleCurrent();
            if (!KYetki)
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
                    db.SaveChanges();
                    LogIslemleri.LogEkle("MezuniyetBasvurulariTezDosyalari", IslemTipi.Update, talep.ToJson());
                    mmMessage.IsSuccess = true;
                    if (kModel.IsOnaylandiOrDuzeltme == true) mmMessage.Messages.AddRange(Management.sendMailMezuniyetTezSablonKontrol(talep.MezuniyetBasvurulariTezDosyaID, MailSablonTipi.Mez_TezKontrolTezDosyasiBasarili, kModel.Aciklama).Messages);
                    else if (kModel.IsOnaylandiOrDuzeltme == false) mmMessage.Messages.AddRange(Management.sendMailMezuniyetTezSablonKontrol(talep.MezuniyetBasvurulariTezDosyaID, MailSablonTipi.Mez_TezKontrolTezDosyasiOnaylanmadi, kModel.Aciklama).Messages);
                }
                catch (Exception ex)
                {
                    mmMessage.MessageType = Msgtype.Error;
                    mmMessage.IsSuccess = false;
                    mmMessage.Messages.Add("Tez dosyası kontrolü durum bilgisi kayıt edilirken bir hata oluştu! Hata:" + ex.ToExceptionMessage());
                    Management.SistemBilgisiKaydet(ex.ToExceptionMessage(), "MezuniyetGelenBasvurular/TdDurumKaydet<br/><br/>" + ex.ToExceptionStackTrace(), BilgiTipi.Kritik);
                }
            }
            mmMessage.Title = "Tez Kontrol Durumu Kayıt İşlemi";
            mmMessage.MessageType = mmMessage.IsSuccess ? Msgtype.Success : Msgtype.Warning;
            var strView = mmMessage.Messages.Count > 0 ? Management.RenderPartialView("Ajax", "getMessage", mmMessage) : "";
            return new
            {
                mmMessage.IsSuccess,
                Messages = strView,
            }.toJsonResult();
        }
        [Authorize(Roles = RoleNames.MezuniyetGelenBasvurularKayit)]
        public ActionResult MezuniyetDurumKaydet(int id, bool? IsMezunOldu, DateTime? Tarih)
        {
            var mmMessage = new MmMessage();
            mmMessage.IsSuccess = false;
            mmMessage.Title = "Mezuniyet durumu kayıt işlemi";
            mmMessage.MessageType = Msgtype.Warning;

            var talep = db.MezuniyetBasvurularis.Where(p => p.MezuniyetBasvurulariID == id).First();

            if (IsMezunOldu == true && Tarih.HasValue == false)
            {
                mmMessage.Messages.Add("Mezuniyet Tarihi giriniz.");
            }
            else if (IsMezunOldu == true)
            {
                var SonAlinanSr = talep.SRTalepleris.OrderByDescending(o => o.SRTalepID).First();

                if (SonAlinanSr.SRTalepleriBezCiltFormus.Any() == false)
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
                if (IsMezunOldu != true) Tarih = null;
                if (talep.IsMezunOldu != IsMezunOldu)
                {
                    talep.IsMezunOldu = IsMezunOldu;
                    talep.MezuniyetTarihi = Tarih;
                    var Kul = talep.Kullanicilar;


                    if (talep.ProgramKod == Kul.ProgramKod && talep.OgrenimTipKod == Kul.OgrenimTipKod) Kul.OgrenimDurumID = talep.IsMezunOldu == true ? OgrenimDurum.Mezun : OgrenimDurum.HalenOğrenci;


                    db.SaveChanges();
                    LogIslemleri.LogEkle("MezuniyetBasvurulari", IslemTipi.Update, talep.ToJson());
                }
                mmMessage.IsSuccess = true;
            }
            var strView = Management.RenderPartialView("Ajax", "getMessage", mmMessage);

            return new
            {
                mmMessage.IsSuccess,
                Messages = strView
            }.toJsonResult();
        }
        [Authorize(Roles = RoleNames.MezuniyetGelenBasvurularKayit)]
        public ActionResult EYKTarihiKaydet(int id, DateTime? EYKTarihi)
        {
            var MmMessage = new MmMessage();
            MmMessage.Title = "EYK Tarihi Güncelleme İşlemi";
            var Mb = db.MezuniyetBasvurularis.Where(p => p.MezuniyetBasvurulariID == id).FirstOrDefault();

            if (Mb != null)
            {
                DateTime? IlkSrMaxTarih = Mb.EYKTarihi;
                if (EYKTarihi != null) IlkSrMaxTarih = EYKTarihi;
                if (IlkSrMaxTarih != null)
                {
                    var IlkSrTalep = Mb.SRTalepleris.OrderBy(o => o.SRTalepID).FirstOrDefault();
                    if (IlkSrTalep != null)
                    {
                        var maxT = IlkSrTalep.Tarih;
                        if (maxT < EYKTarihi)
                        {
                            MmMessage.Messages.Add("Eyk tarihi öğrencinin almış olduğu ilk salon rezervasyonu tarihi için uygun değildir.");
                            MmMessage.Messages.Add("İlk salon rezervasyonu '" + IlkSrTalep.Tarih.ToDateString() + "' tarihinde alınmıştır.");
                            MmMessage.Messages.Add("Belirlenen kurallara göre EYK tarihi en son '" + maxT.ToDateString() + "' tarihi olabilir.");
                            MmMessage.IsSuccess = false;
                        }
                    }
                }

                if (MmMessage.Messages.Count == 0)
                {

                    Mb.EYKTarihi = EYKTarihi;
                    db.SaveChanges();
                    LogIslemleri.LogEkle("MezuniyetBasvurulari", IslemTipi.Update, Mb.ToJson());
                    MmMessage.Messages.Add("Eyk Tarihi Güncellendi");
                    MmMessage.IsSuccess = true;
                }
            }
            else
            {
                MmMessage.Messages.Add("İşlem yapmaya çalıştığınız mezuniyet başvurusu sistemde bulunamadı!");
                MmMessage.IsSuccess = false;
            }
            MmMessage.MessageType = MmMessage.IsSuccess ? Msgtype.Success : Msgtype.Error;
            return new
            {
                MmMessage.IsSuccess,
                MmMessage,
            }.toJsonResult();
        }
        [Authorize(Roles = RoleNames.MezuniyetGelenBasvurularKayit)]
        public ActionResult SinavTarihiKaydet(int id, string SinavTarihi)
        {
            var MmMessage = new MmMessage();
            MmMessage.Title = "Sınav Tarihi Güncelleme İşlemi";
            var SRTalep = db.SRTalepleris.Where(p => p.SRTalepID == id).First();
            var MB = SRTalep.MezuniyetBasvurulari;
            DateTime? Tarih = Convert.ToDateTime(SinavTarihi);
            if (!Tarih.HasValue)
            {
                MmMessage.Messages.Add("Sınav Tarihi Giriniz.");
            }
            else
            {
                var OtBilgiTarihBilgi = MB.MezuniyetSureci.MezuniyetSureciOgrenimTipKriterleris.Where(p => p.OgrenimTipKod == MB.OgrenimTipKod).First();
                if (SRTalep.MezuniyetSinavDurumID == MezuniyetSinavDurum.Uzatma || SRTalep.JuriSonucMezuniyetSinavDurumID == MezuniyetSinavDurum.Uzatma && SRTalep.SRDurumID == SRTalepDurum.Onaylandı)
                {
                    var UzatmaOncesiSrTalebi = MB.SRTalepleris.Where(p => p.MezuniyetSinavDurumID != MezuniyetSinavDurum.Uzatma && p.SRDurumID == SRTalepDurum.Onaylandı).OrderByDescending(o => o.SRTalepID).FirstOrDefault();
                    var UzatmaOncesiSrAlabilmeTarihi = UzatmaOncesiSrTalebi.Tarih.AddDays(OtBilgiTarihBilgi.MBSinavUzatmaSuresiGun);
                    if (Tarih.Value.Date > UzatmaOncesiSrAlabilmeTarihi)
                    {
                        MmMessage.Messages.Add("Mezuniyet sınavı sonucunda almış olduğunuz uzatma işlemi sonrası salon rezervasyonu işemi son tarihi olan '" + UzatmaOncesiSrAlabilmeTarihi.ToFormatDate() + "' tarihini aşamazsınız.");
                    }
                }
                else
                {
                    var SrBaslangicTarih = MB.EYKTarihi.Value.AddDays(OtBilgiTarihBilgi.MBSRTalebiKacGunSonraAlabilir);
                    if (Tarih.Value.Date < SrBaslangicTarih.Date)
                    {
                        MmMessage.Messages.Add("Talep tarihi " + SrBaslangicTarih.Date.ToString("yyyy-MM-dd") + " tarihinden küçük olamaz!");
                    }
                }
            }
            if (MmMessage.Messages.Count == 0)
            {

                SRTalep.Tarih = Tarih.Value;
                SRTalep.BasSaat = new TimeSpan(Tarih.Value.Hour, Tarih.Value.Minute, 0);
                SRTalep.BitSaat = new TimeSpan(Tarih.Value.Hour + 2, Tarih.Value.Minute, 0);

                db.SaveChanges();
                LogIslemleri.LogEkle("SRTalepleri", IslemTipi.Update, SRTalep.ToJson());
                MmMessage.Messages.Add("Sınav Tarihi Güncellendi");
                MmMessage.IsSuccess = true;
            }
            MmMessage.MessageType = MmMessage.IsSuccess ? Msgtype.Success : Msgtype.Error;
            return new
            {
                MmMessage.IsSuccess,
                MmMessage,
            }.toJsonResult();
        }
        [Authorize(Roles = RoleNames.MezuniyetGelenBasvurularKayit)]
        public ActionResult TezTeslimSonTarihiKaydet(int id, string tezTeslimSonTarih)
        {
            var mmMessage = new MmMessage
            {
                Title = "Tez Teslim Son Tarih Kriteri Güncelleme İşlemi"
            };
            var srTalep = db.SRTalepleris.First(p => p.SRTalepID == id);
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

                db.SaveChanges();
                LogIslemleri.LogEkle("MezuniyetBasvurulari", IslemTipi.Update, mb.ToJson());
                mmMessage.Messages.Add("Tez Teslim Son Tarih Kriteri Güncellendi");
                mmMessage.IsSuccess = true;
            }
            mmMessage.MessageType = mmMessage.IsSuccess ? Msgtype.Success : Msgtype.Error;
            return new
            {
                mmMessage.IsSuccess,
                MmMessage = mmMessage,
            }.toJsonResult();
        }

        public ActionResult GetJuriOneriFormu(int MezuniyetBasvurulariID)
        {
            var MB = db.MezuniyetBasvurularis.First(p => p.MezuniyetBasvurulariID == MezuniyetBasvurulariID);
            var cmbUnvanList = Management.cmbMezuniyetJofUnvanlar(true);
            var cmbUniversiteList = Management.cmbGetAktifUniversiteler(true);

            var Model = new MezuniyetJuriOneriFormuModel
            {
                MezuniyetBasvurulariID = MezuniyetBasvurulariID,
                IsTezDiliTr = MB.IsTezDiliTr == true,
                TezBaslikTr = MB.TezBaslikTr,
                TezBaslikEn = MB.TezBaslikEn,
                Danisman = db.Kullanicilars.First(p => p.KullaniciID == MB.TezDanismanID),
                SListUnvanAdi = new SelectList(cmbUnvanList, "Value", "Caption"),
                SListUniversiteID = new SelectList(cmbUniversiteList, "Value", "Caption"),
                IsDoktoraOrYL = MB.OgrenimTipKod == OgrenimTipi.Doktra,
            };

            var mMessage = new MmMessage();
            mMessage.MessageType = Msgtype.Success;
            mMessage.IsSuccess = true;
            string View = "";
            var MBJO = MB.MezuniyetJuriOneriFormlaris.FirstOrDefault();
            var ogrenciInfo = Management.StudentControl(MB.TcKimlikNo);

            if (!RoleNames.MezuniyetGelenBasvurularJuriOneriFormuKayit.InRoleCurrent())
            {
                mMessage.MessageType = Msgtype.Warning;
                mMessage.Messages.Add("Jüri öneri formu kayıt işlemi için yetkili değilsiniz.");
            }
            else if (!RoleNames.MezuniyetGelenBasvurularKayit.InRoleCurrent() && MB.TezDanismanID != UserIdentity.Current.Id)
            {
                mMessage.MessageType = Msgtype.Warning;
                mMessage.Messages.Add("Bu mezuniyet başvurusu için danışman olarak belirlenmediğiniz için jüri öneri formu oluşturamazsınız.");
            }
            if (mMessage.Messages.Count == 0 && MBJO == null)
            {
                if (ogrenciInfo.Hata)
                {
                    mMessage.Messages.Add("Obs sisteminden öğrenci bilgisi sorgulanırken bir hata oluştu!");
                }
                else
                {
                    if (ogrenciInfo.OgrenciInfo.DANISMAN_AD_SOYAD1.IsNullOrWhiteSpace())
                        mMessage.Messages.Add("Danışman Bilgisi Çekilemedi.");

                    if (Model.IsDoktoraOrYL)
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
                Model.OgrenciAdSoyad = MB.Ad + " " + MB.Soyad + " - " + MB.OgrenciNo;
                Model.OgrenciAnabilimdaliProgramAdi = MB.Programlar.AnabilimDallari.AnabilimDaliAdi + " - " + MB.Programlar.ProgramAdi;
                Model.MezuniyetJuriOneriFormID = MBJO?.MezuniyetJuriOneriFormID ?? 0;

                if (MBJO != null)
                {
                    Model.MezuniyetJuriOneriFormID = MBJO.MezuniyetJuriOneriFormID;
                    Model.YeniTezBaslikTr = MBJO.YeniTezBaslikTr;
                    Model.YeniTezBaslikEn = MBJO.YeniTezBaslikEn;
                    Model.IsTezBasligiDegisti = MBJO.IsTezBasligiDegisti;
                    Model.JoFormJuriList = MBJO.MezuniyetJuriOneriFormuJurileris.Select(s => new KrMezuniyetJuriOneriFormuJurileri
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
                            var tD = Model.JoFormJuriList.Where(p => p.JuriTipAdi == "TezDanismani").First();
                            tD.SListUniversiteID = new SelectList(cmbUniversiteList, "Value", "Caption", tD.UniversiteID);
                            tD.SlistUnvanAdi = new SelectList(cmbUnvanList, "Value", "Caption", tD.UnvanAdi);
                            if (tD.AdSoyad.ToUpper().Trim() != ogrenciInfo.OgrenciInfo.DANISMAN_AD_SOYAD1.ToUpper().ToUpper().Trim() || tD.UnvanAdi.ToUpper().Trim() != ogrenciInfo.OgrenciInfo.DANISMAN_UNVAN1.ToMezuniyetJuriUnvanAdi())
                            {
                                tD.AdSoyad = ogrenciInfo.OgrenciInfo.DANISMAN_AD_SOYAD1.ToUpper();
                                tD.UnvanAdi = ogrenciInfo.OgrenciInfo.DANISMAN_UNVAN1.ToMezuniyetJuriUnvanAdi();
                            }
                        }

                        var Tiks = ogrenciInfo.TezIzlJuriBilgileri.Where(p => p.TEZ_DANISMAN != "1").ToList();
                        if (Model.IsDoktoraOrYL && Tiks.Count >= 2)
                        {
                            var obsTik1 = Tiks[0];
                            var obsTik2 = Tiks[1];

                            var varOlanTik1 = Model.JoFormJuriList.Where(p => p.JuriTipAdi == "TikUyesi1").First();
                            varOlanTik1.SListUniversiteID =
                                new SelectList(cmbUniversiteList, "Value", "Caption", varOlanTik1.UniversiteID);
                            varOlanTik1.SlistUnvanAdi = new SelectList(cmbUnvanList, "Value", "Caption", varOlanTik1.UnvanAdi);
                            if (varOlanTik1.AdSoyad.ToUpper() != obsTik1.TEZ_IZLEME_JURI_ADSOY.ToUpper() ||
                                varOlanTik1.UnvanAdi.ToUpper().Trim() != obsTik1.TEZ_IZLEME_JURI_UNVAN.ToMezuniyetJuriUnvanAdi())
                            {
                                varOlanTik1.AdSoyad = obsTik1.TEZ_IZLEME_JURI_ADSOY.ToUpper();
                                varOlanTik1.UnvanAdi = obsTik1.TEZ_IZLEME_JURI_UNVAN.ToMezuniyetJuriUnvanAdi();
                            }
                            var varOlanTik2 = Model.JoFormJuriList.Where(p => p.JuriTipAdi == "TikUyesi2").First();
                            varOlanTik2.SListUniversiteID =
                                new SelectList(cmbUniversiteList, "Value", "Caption", varOlanTik2.UniversiteID);
                            varOlanTik2.SlistUnvanAdi = new SelectList(cmbUnvanList, "Value", "Caption", varOlanTik2.UnvanAdi);
                            if (varOlanTik2.AdSoyad.ToUpper() != obsTik2.TEZ_IZLEME_JURI_ADSOY.ToUpper() ||
                                varOlanTik2.UnvanAdi.ToUpper().Trim() != obsTik2.TEZ_IZLEME_JURI_UNVAN.ToMezuniyetJuriUnvanAdi())
                            {
                                varOlanTik2.AdSoyad = obsTik2.TEZ_IZLEME_JURI_ADSOY.ToUpper();
                                varOlanTik2.UnvanAdi = obsTik2.TEZ_IZLEME_JURI_UNVAN.ToMezuniyetJuriUnvanAdi();

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
                            var TdBilgi = new KrMezuniyetJuriOneriFormuJurileri
                            {
                                JuriTipAdi = "TezDanismani",
                                UnvanAdi = ogrenciInfo.OgrenciInfo.DANISMAN_UNVAN1.ToMezuniyetJuriUnvanAdi(),
                                AdSoyad = ogrenciInfo.OgrenciInfo.DANISMAN_AD_SOYAD1.ToUpper(),

                            };
                            TdBilgi.SlistUnvanAdi = new SelectList(cmbUnvanList, "Value", "Caption", TdBilgi.UnvanAdi);
                            TdBilgi.SListUniversiteID = new SelectList(cmbUniversiteList, "Value", "Caption", TdBilgi.UniversiteID);
                            Model.JoFormJuriList.Add(TdBilgi);
                        }
                        else
                        {
                            Model.JoFormJuriList.Add(new KrMezuniyetJuriOneriFormuJurileri { JuriTipAdi = "TezDanismani", SlistUnvanAdi = new SelectList(cmbUnvanList, "Value", "Caption"), SListUniversiteID = new SelectList(cmbUniversiteList, "Value", "Caption") });
                        }

                        var Tiks = ogrenciInfo.TezIzlJuriBilgileri.Where(p => p.TEZ_DANISMAN != "1").ToList();
                        if (Model.IsDoktoraOrYL && Tiks.Count >= 2)
                        {


                            var obsTik1 = Tiks[0];
                            var obsTik2 = Tiks[1];

                            var Tk1Bilgi = new KrMezuniyetJuriOneriFormuJurileri
                            {
                                JuriTipAdi = "TikUyesi1",
                                AdSoyad = obsTik1.TEZ_IZLEME_JURI_ADSOY,
                                UnvanAdi = obsTik1.TEZ_IZLEME_JURI_UNVAN.ToMezuniyetJuriUnvanAdi()
                            };
                            Tk1Bilgi.SlistUnvanAdi =
                                new SelectList(cmbUnvanList, "Value", "Caption", Tk1Bilgi.UnvanAdi);
                            Tk1Bilgi.SListUniversiteID = new SelectList(cmbUniversiteList, "Value", "Caption",
                                Tk1Bilgi.UniversiteID);
                            Model.JoFormJuriList.Add(Tk1Bilgi);



                            var Tk2Bilgi = new KrMezuniyetJuriOneriFormuJurileri
                            {
                                JuriTipAdi = "TikUyesi2",
                                AdSoyad = obsTik2.TEZ_IZLEME_JURI_ADSOY,
                                UnvanAdi = obsTik2.TEZ_IZLEME_JURI_UNVAN.ToMezuniyetJuriUnvanAdi()
                            };
                            Tk2Bilgi.SlistUnvanAdi =
                                new SelectList(cmbUnvanList, "Value", "Caption", Tk2Bilgi.UnvanAdi);
                            Tk2Bilgi.SListUniversiteID = new SelectList(cmbUniversiteList, "Value", "Caption",
                                Tk2Bilgi.UniversiteID);
                            Model.JoFormJuriList.Add(Tk2Bilgi);

                        }
                    }
                }


                View = Management.RenderPartialView("MezuniyetGelenBasvurular", "JuriOneriFormu", Model);
            }
            else { mMessage.IsSuccess = false; mMessage.MessageType = Msgtype.Warning; }
            var strView = Management.RenderPartialView("Ajax", "getMessage", mMessage);

            return new
            {
                mMessage.IsSuccess,
                Content = View,
                Messages = strView
            }.toJsonResult();
        }
        [ValidateInput(false)]
        public ActionResult JuriOneriFormuPost(MezuniyetJuriOneriFormuModel kModel, string PostDetayTabAdi = "", bool SaveData = false)
        {
            var mMessage = new MmMessage();
            mMessage.MessageType = Msgtype.Success;
            mMessage.IsSuccess = true;
            string SelectedAnaTabAdi = "";
            string SelectedDetayTabAdi = "";
            bool IsYeniJO = true;

            var MB = db.MezuniyetBasvurularis.Where(p => p.MezuniyetBasvurulariID == kModel.MezuniyetBasvurulariID).First();
            if (!RoleNames.MezuniyetGelenBasvurularJuriOneriFormuKayit.InRoleCurrent())
            {
                mMessage.Messages.Add("Jür öneri formu kayıt işlemi için yetkili değilsiniz.");
            }
            else if (!RoleNames.MezuniyetGelenBasvurularKayit.InRoleCurrent() && MB.TezDanismanID != UserIdentity.Current.Id)
            {
                mMessage.IsSuccess = false;
                mMessage.MessageType = Msgtype.Warning;
                mMessage.Messages.Add("Bu mezuniyet başvurusu için danışman olarak belirlenmediğiniz için jüri öneri formu oluşturamazsınız.");
            }
            else
            {
                var MBJO = MB.MezuniyetJuriOneriFormlaris.FirstOrDefault();

                bool IsDegisiklikVar = false;
                if (MBJO != null)
                {
                    IsYeniJO = false;
                    if (MBJO.EYKDaOnaylandi == true)
                        mMessage.Messages.Add("Jüri öneri formunuzun EYK'da onaylandığından Form üzerinden herhangi bir değişiklik yapamazsınız!");
                    else if (MBJO.EYKYaGonderildi == true)
                        mMessage.Messages.Add("Jüri öneri formunuzun EYK'ya gönderimi yapıldığından Form üzerinden herhangi bir değişiklik yapamazsınız!");

                    if (kModel.IsTezBasligiDegisti != MBJO.IsTezBasligiDegisti) IsDegisiklikVar = true;
                    else if (kModel.IsTezBasligiDegisti == true && (kModel.YeniTezBaslikTr.ToUpper() != MBJO.YeniTezBaslikEn.ToUpper() || kModel.YeniTezBaslikTr.ToUpper() != MBJO.YeniTezBaslikTr.ToUpper())) IsDegisiklikVar = true;
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
                    SelectedAnaTabAdi = PostDetayTabAdi;
                    SelectedDetayTabAdi = PostDetayTabAdi;
                }
                if (mMessage.Messages.Count == 0)
                {
                    var AnaTabAdis = kModel.AnaTabAdi.Select((s, i) => new { AnaTabAdi = s, Inx = (i + 1) }).ToList();
                    var DetayTabAdis = kModel.DetayTabAdi.Select((s, i) => new { DetayTabAdi = s, Inx = (i + 1) }).ToList();
                    var JuriTipAdis = kModel.JuriTipAdi.Select((s, i) => new { JuriTipAdi = s, Inx = (i + 1) }).ToList();
                    var AdSoyads = kModel.AdSoyad.Select((s, i) => new { AdSoyad = s, Inx = (i + 1) }).ToList();
                    var UnvanAdis = kModel.UnvanAdi.Select((s, i) => new { UnvanAdi = s, Inx = (i + 1) }).ToList();
                    var EMails = kModel.EMail.Select((s, i) => new { EMail = s.Trim(), Inx = (i + 1) }).ToList();
                    var UniversiteIDs = kModel.UniversiteID.Select((s, i) => new { UniversiteID = s, Inx = (i + 1) }).ToList();
                    var AnabilimdaliProgramAdis = kModel.AnabilimdaliProgramAdi.Select((s, i) => new { AnabilimdaliProgramAdi = s, Inx = (i + 1) }).ToList();
                    var UzmanlikAlanis = kModel.UzmanlikAlani.Select((s, i) => new { UzmanlikAlani = s, Inx = (i + 1) }).ToList();
                    var BilimselCalismalarAnahtarSozcuklers = kModel.BilimselCalismalarAnahtarSozcukler.Select((s, i) => new { BilimselCalismalarAnahtarSozcukler = s, Inx = (i + 1) }).ToList();
                    var DilSinavAdis = kModel.DilSinavAdi.Select((s, i) => new { DilSinavAdi = s, Inx = (i + 1) }).ToList();
                    var DilPuanis = kModel.DilPuani.Select((s, i) => new { DilPuani = s, Inx = (i + 1) }).ToList();


                    var qData = (from ad in AdSoyads
                                 join at in AnaTabAdis on ad.Inx equals at.Inx
                                 join dt in DetayTabAdis on ad.Inx equals dt.Inx
                                 join jt in JuriTipAdis on ad.Inx equals jt.Inx
                                 join un in UnvanAdis on ad.Inx equals un.Inx
                                 join em in EMails on ad.Inx equals em.Inx
                                 join uni in UniversiteIDs on ad.Inx equals uni.Inx
                                 join abd in AnabilimdaliProgramAdis on ad.Inx equals abd.Inx
                                 join ua in UzmanlikAlanis on ad.Inx equals ua.Inx
                                 join bc in BilimselCalismalarAnahtarSozcuklers on ad.Inx equals bc.Inx
                                 join ds in DilSinavAdis on ad.Inx equals ds.Inx
                                 join dp in DilPuanis on ad.Inx equals dp.Inx

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
                                      IsSuccessRow = s.JuriTipAdi.ToJOFormSuccessRow(kModel.IsTezDiliTr, s.AdSoyadSuccess, s.UnvanAdiSuccess, s.EMailSuccess, s.UniversiteIDSuccess, s.UzmanlikAlaniSuccess, s.BilimselCalismalarAnahtarSozcuklerSuccess, s.DilSinavAdiSuccess, s.DilPuaniSuccess)
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
                                  }).Where(p => (SaveData ? 1 == 1 : p.DetayTabAdi == PostDetayTabAdi)).ToList();
                    foreach (var item in qGroup.Where(p => p.JuriTipAdi != (MB.OgrenimTipKod == OgrenimTipi.Doktra ? "TikUyesi" : "")))
                    {

                        if (!item.IsSuccessRow)
                        {
                            if (item.JuriTipAdi == "TezDanismani") mMessage.Messages.Add("Danışman bilgilerinde eksik ya da hatalı veri girişleri mevcut!");
                            else if (item.JuriTipAdi == "TikUyesi1") mMessage.Messages.Add("Tik üyesi 1 bilgilerinde eksik ya da hatalı veri girişleri mevcut!");
                            else if (item.JuriTipAdi == "TikUyesi2") mMessage.Messages.Add("Tik üyesi 2 bilgilerinde eksik ya da hatalı veri girişleri mevcut!");
                            else if (item.JuriTipAdi == "YtuIciJuri1") mMessage.Messages.Add("YTU içi Jüri 1 bilgilerinde eksik ya da hatalı veri girişleri mevcut!");
                            else if (item.JuriTipAdi == "YtuIciJuri2") mMessage.Messages.Add("YTU içi Jüri 2 bilgilerinde eksik ya da hatalı veri girişleri mevcut!");
                            else if (item.JuriTipAdi == "YtuIciJuri3") mMessage.Messages.Add("YTU içi Jüri 3 bilgilerinde eksik ya da hatalı veri girişleri mevcut!");
                            else if (item.JuriTipAdi == "YtuIciJuri4") mMessage.Messages.Add("YTU içi Jüri 4 bilgilerinde eksik ya da hatalı veri girişleri mevcut!");
                            else if (item.JuriTipAdi == "YtuDisiJuri1") mMessage.Messages.Add("YTU dışı Jüri 1 bilgilerinde eksik ya da hatalı veri girişleri mevcut!");
                            else if (item.JuriTipAdi == "YtuDisiJuri2") mMessage.Messages.Add("YTU dışı Jüri 2 bilgilerinde eksik ya da hatalı veri girişleri mevcut!");
                            else if (item.JuriTipAdi == "YtuDisiJuri3") mMessage.Messages.Add("YTU dışı Jüri 3 bilgilerinde eksik ya da hatalı veri girişleri mevcut!");
                            else if (item.JuriTipAdi == "YtuDisiJuri4") mMessage.Messages.Add("YTU dışı Jüri 4 bilgilerinde eksik ya da hatalı veri girişleri mevcut!");
                            if (mMessage.Messages.Count > 0 && SelectedAnaTabAdi == "")
                            {
                                SelectedAnaTabAdi = item.AnaTabAdi;
                                SelectedDetayTabAdi = item.DetayTabAdi;
                            }
                        }
                        else if (SaveData == false)
                        {
                            SelectedAnaTabAdi = item.nextAnaTabAdi;
                            SelectedDetayTabAdi = item.nextDetayTabAdi;
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
                    if (mMessage.Messages.Count == 0 && SaveData)
                    {
                        MBJO = IsYeniJO ? new MezuniyetJuriOneriFormlari() : MBJO;
                        var Unilers = db.Universitelers.ToList();
                        //doktora öğrenim tipindeki başvurular için tik üyesi haricindeki bilgiler alınsın
                        var kData = qData.Where(p => p.JuriTipAdi != (MB.OgrenimTipKod == OgrenimTipi.Doktra ? "TikUyesi" : "")).ToList();
                        foreach (var item in kData)
                        {
                            var Rw = MBJO.MezuniyetJuriOneriFormuJurileris.Where(p => p.JuriTipAdi == item.JuriTipAdi).FirstOrDefault();
                            if (Rw != null)
                            {
                                if (item.AdSoyad.IsNullOrWhiteSpace() == false)
                                {
                                    var Uni = Unilers.Where(p => p.UniversiteID == item.UniversiteID).First();
                                    if (Rw.AdSoyad != item.AdSoyad || Rw.UnvanAdi != item.UnvanAdi || Rw.EMail != item.EMail || Rw.UniversiteID != item.UniversiteID || Rw.UzmanlikAlani != item.UzmanlikAlani || Rw.BilimselCalismalarAnahtarSozcukler != item.BilimselCalismalarAnahtarSozcukler || Rw.DilPuani != item.DilPuani || Rw.DilSinavAdi != item.DilSinavAdi) IsDegisiklikVar = true;
                                    Rw.UnvanAdi = item.UnvanAdi.ToUpper();
                                    Rw.AdSoyad = item.AdSoyad.ToUpper();
                                    Rw.EMail = item.EMail;
                                    Rw.UniversiteAdi = Uni.Ad;
                                    Rw.UniversiteID = item.UniversiteID;
                                    Rw.AnabilimdaliProgramAdi = item.AnabilimdaliProgramAdi;
                                    Rw.UzmanlikAlani = item.UzmanlikAlani;
                                    Rw.BilimselCalismalarAnahtarSozcukler = item.BilimselCalismalarAnahtarSozcukler;
                                    Rw.DilSinavAdi = item.DilSinavAdi;
                                    Rw.DilPuani = item.DilPuani;

                                    bool IsAsil = new List<string> { "TezDanismani", "TikUyesi1", "TikUyesi2" }.Contains(item.JuriTipAdi);
                                    if ((Rw.AdSoyad != item.AdSoyad && Rw.EMail != item.EMail) || IsAsil) Rw.IsAsilOrYedek = IsAsil ? true : (bool?)null;
                                }
                                else db.MezuniyetJuriOneriFormuJurileris.Remove(Rw);
                            }
                            else if (item.AdSoyad.IsNullOrWhiteSpace() == false)
                            {
                                var Uni = Unilers.Where(p => p.UniversiteID == item.UniversiteID).First();
                                MBJO.MezuniyetJuriOneriFormuJurileris.Add(
                                    new MezuniyetJuriOneriFormuJurileri
                                    {
                                        JuriTipAdi = item.JuriTipAdi,
                                        UnvanAdi = item.UnvanAdi.ToUpper(),
                                        AdSoyad = item.AdSoyad.ToUpper(),
                                        EMail = item.EMail,
                                        UniversiteID = item.UniversiteID,
                                        UniversiteAdi = Uni.Ad,
                                        AnabilimdaliProgramAdi = item.AnabilimdaliProgramAdi,
                                        UzmanlikAlani = item.UzmanlikAlani,
                                        BilimselCalismalarAnahtarSozcukler = item.BilimselCalismalarAnahtarSozcukler,
                                        DilSinavAdi = item.DilSinavAdi,
                                        DilPuani = item.DilPuani,
                                        IsAsilOrYedek = new List<string> { "TezDanismani", "TikUyesi1", "TikUyesi2" }.Contains(item.JuriTipAdi) ? true : (bool?)null

                                    });
                            }
                        }
                        if (IsYeniJO || IsDegisiklikVar || db.MezuniyetJuriOneriFormuJurileris.Where(p => p.MezuniyetJuriOneriFormID == kModel.MezuniyetJuriOneriFormID).Count() != kData.Where(p => p.AdSoyad.IsNullOrWhiteSpace() == false).Count())
                        {
                            var UniqueID = Guid.NewGuid().ToString().Replace("-", "").Substr(0, 8).ToUpper();
                            while (db.MezuniyetJuriOneriFormlaris.Any(a => a.UniqueID == UniqueID))
                            {
                                UniqueID = Guid.NewGuid().ToString().Replace("-", "").Substr(0, 8).ToUpper();
                            }
                            MBJO.UniqueID = UniqueID;
                        }
                        MBJO.MezuniyetBasvurulariID = kModel.MezuniyetBasvurulariID;

                        MBJO.IsTezBasligiDegisti = kModel.IsTezBasligiDegisti;
                        MBJO.YeniTezBaslikTr = kModel.IsTezBasligiDegisti == true ? kModel.YeniTezBaslikTr : null;
                        MBJO.YeniTezBaslikEn = kModel.IsTezBasligiDegisti == true ? kModel.YeniTezBaslikEn : null;


                        if (RoleNames.MezuniyetGelenBasvurularJuriOneriFormuOnay.InRoleCurrent())
                        {

                        }
                        else
                        {
                            MBJO.EYKYaGonderildi = null;
                            MBJO.EYKYaGonderildiIslemTarihi = null;
                            MBJO.EYKYaGonderildiIslemYapanID = null;
                            MBJO.EYKDaOnaylandi = null;
                        }

                        MBJO.EYKDaOnaylandiOnayTarihi = null;
                        MBJO.EYKDaOnaylandiIslemYapanID = null;
                        MBJO.IslemTarihi = DateTime.Now;
                        MBJO.IslemYapanID = UserIdentity.Current.Id;
                        MBJO.IslemYapanIP = UserIdentity.Ip;

                        if (IsYeniJO) db.MezuniyetJuriOneriFormlaris.Add(MBJO);

                        try
                        {
                            db.SaveChanges();
                        }
                        catch (Exception ex)
                        {
                            var hataMsj = "Kayıt işlemi sırasında bir hata oluştu! \r\nHata:" + ex.ToExceptionMessage();
                            mMessage.Messages.Add(hataMsj);
                            Management.SistemBilgisiKaydet(hataMsj, "MezuniyetGelenBasvurular/JuriOneriFormuPost", BilgiTipi.Hata);
                        }


                    }
                }

            }
            mMessage.IsSuccess = SaveData && mMessage.Messages.Count == 0;
            if (mMessage.Messages.Count > 0)
            {
                mMessage.Title = "Jüri Öneri Formu Aşağıdaki Sebeplerden Dolayı Oluşturulamadı.";
                mMessage.IsSuccess = false;
                mMessage.MessageType = Msgtype.Warning;
            }
            return new
            {
                mMessage = mMessage,
                IsYeniJO = IsYeniJO,
                SelectedAnaTabAdi = SelectedAnaTabAdi,
                SelectedDetayTabAdi = SelectedDetayTabAdi
            }.toJsonResult();
        }
        public ActionResult JuriOneriFormu()
        {

            return View();
        }

        public ActionResult JuriOneriFormuAsilYedekDurumKayit(int id, int MezuniyetJuriOneriFormID, string JuriTipAdi, bool? IsAsilOrYedek)
        {
            var mmMessage = new MmMessage();
            mmMessage.IsSuccess = false;
            mmMessage.Title = "Jüri öneri formu Asil/Yedek seçimi işlemi";
            mmMessage.MessageType = Msgtype.Warning;

            var MB = db.MezuniyetBasvurularis.Where(p => p.MezuniyetBasvurulariID == id).First();
            var JuriOneriFormu = MB.MezuniyetJuriOneriFormlaris.Where(p => p.MezuniyetJuriOneriFormID == MezuniyetJuriOneriFormID).FirstOrDefault();

            if (!RoleNames.MezuniyetGelenBasvurularJuriOneriFormuEYKOnay.InRoleCurrent())
            {
                mmMessage.Messages.Add("Jüri öneri formunda Asil/Yedek jüri adayı seçimi yetkisine sahip değilsiniz!");
            }
            else if (JuriOneriFormu == null)
            {
                mmMessage.Messages.Add("İşlem yapılmak istenen jüri öneri formu sistemde bulunamadı!");
            }
            else
            {
                if (JuriOneriFormu.EYKYaGonderildi == false)
                {
                    mmMessage.Messages.Add("İşlem yapılmak istenen jüri öneri formu EYK'ya gönderildi seçeneği ile kayıt edilmediğinden Asil/Yedek jüri adayı seçimi yapamazsınız!");
                }
                else if (JuriOneriFormu.EYKDaOnaylandi == true)
                {
                    mmMessage.Messages.Add("İşlem yapılmak istenen jüri öneri formu EYK'da onaylandı seçeneği ile kayıt edildiğinden Asil/Yedek jüri adayı seçimi yapamazsınız!");
                }
            }

            if (mmMessage.Messages.Count == 0 && IsAsilOrYedek.HasValue)
            {

                var AdayCount = JuriOneriFormu.MezuniyetJuriOneriFormuJurileris.Where(p => p.IsAsilOrYedek == IsAsilOrYedek.Value).Count();
                var countSize = MB.OgrenimTipKod == OgrenimTipi.TezliYuksekLisans ? (IsAsilOrYedek.Value ? 3 : 2) : (IsAsilOrYedek.Value ? 5 : 2);
                if (AdayCount >= countSize)
                    mmMessage.Messages.Add((IsAsilOrYedek.Value ? "Asil" : "Yedek") + " Jüri adayı önerisinden toplamda " + countSize + " aday seçilebilir.");



            }
            if (mmMessage.Messages.Count == 0)
            {
                var Juri = JuriOneriFormu.MezuniyetJuriOneriFormuJurileris.Where(p => p.JuriTipAdi == JuriTipAdi).First();
                Juri.IsAsilOrYedek = IsAsilOrYedek;
                db.SaveChanges();
                mmMessage.IsSuccess = true;
            }
            var strView = Management.RenderPartialView("Ajax", "getMessage", mmMessage);
            return new
            {
                mmMessage.IsSuccess,
                Messages = strView
            }.toJsonResult();
        }


        public ActionResult JuriOneriFormuOnayDurumKayit(int id, int MezuniyetJuriOneriFormID, bool EYKDaOnayOrEYKYaGonderim, bool? Onaylandi, string EYKDaOnaylanmadiDurumAciklamasi)
        {
            var mmMessage = new MmMessage();
            mmMessage.IsSuccess = false;
            mmMessage.Title = "Jüri öneri formu " + (EYKDaOnayOrEYKYaGonderim ? "EYK'da onay" : "EYK'ya gönderim") + " işlemi";
            mmMessage.MessageType = Msgtype.Warning;

            var MB = db.MezuniyetBasvurularis.Where(p => p.MezuniyetBasvurulariID == id).First();
            var JuriOneriFormu = MB.MezuniyetJuriOneriFormlaris.Where(p => p.MezuniyetJuriOneriFormID == MezuniyetJuriOneriFormID).FirstOrDefault();

            if (!EYKDaOnayOrEYKYaGonderim && !RoleNames.MezuniyetGelenBasvurularJuriOneriFormuOnay.InRoleCurrent())
            {
                mmMessage.Messages.Add("Jüri öneri formunda onay yetkisine sahip değilsiniz!");
            }
            else if (EYKDaOnayOrEYKYaGonderim && !RoleNames.MezuniyetGelenBasvurularJuriOneriFormuEYKOnay.InRoleCurrent())
            {
                mmMessage.Messages.Add("Jüri öneri formunda EYK'da onay yetkisine sahip değilsiniz!");
            }
            else if (JuriOneriFormu == null)
            {
                mmMessage.Messages.Add("İşlem yapılmak istenen jüri öneri formu sistemde bulunamadı!");
            }
            else
            {
                if (EYKDaOnayOrEYKYaGonderim)
                {
                    if (JuriOneriFormu.EYKYaGonderildi != true)
                    {
                        mmMessage.Messages.Add("EYK Ya gönderilmeyen jüri öneri formu üzerinde EYK Onayı işlemi yapılamaz!");
                    }
                    else if (Onaylandi == false && EYKDaOnaylanmadiDurumAciklamasi.IsNullOrWhiteSpace())
                    {
                        mmMessage.Messages.Add("EYK'da onaylanmama sebebini giriniz!");
                    }
                    else if (MB.EYKTarihi.HasValue && MB.SRTalepleris.Any(a => a.SRDurumID == SRTalepDurum.Onaylandı))
                    {
                        mmMessage.Messages.Add("Mezuniyet başvurusuna ait Salon rezervasyonu alındığı için jüri öneri formu onay işlemi yapılamaz!");
                    }
                }
                else
                {
                    if (JuriOneriFormu.EYKDaOnaylandi.HasValue)
                    {
                        mmMessage.Messages.Add("EYK onay işlemi yapılan bir form da ön onay işlemi gerçekleştirilemez!");
                    }
                }
            }
            if (mmMessage.Messages.Count == 0 && EYKDaOnayOrEYKYaGonderim && Onaylandi == true)
            {


                string msg = "";
                var AsilCount = JuriOneriFormu.MezuniyetJuriOneriFormuJurileris.Where(p => p.IsAsilOrYedek == true).Count();
                var YedekCount = JuriOneriFormu.MezuniyetJuriOneriFormuJurileris.Where(p => p.IsAsilOrYedek == false).Count();
                var countSizeAsil = MB.OgrenimTipKod == OgrenimTipi.TezliYuksekLisans ? 3 : 5;
                if (AsilCount != countSizeAsil)
                    msg += ("<br />* Jüri adayı önerisinden " + countSizeAsil + " Asil aday belirlemeniz gerekmektedi.");
                if (YedekCount != 2)
                    msg += ("<br />* Jüri adayı önerisinde 2 Yedek aday belirlemeniz gerekmektedi.");
                if (msg != "")
                {
                    mmMessage.Messages.Add("Jüri öneri formunda EYK'da onaylandı işlemini yapabilmeniz için: " + msg);
                }

            }
            if (mmMessage.Messages.Count == 0)
            {
                if (EYKDaOnayOrEYKYaGonderim)
                {
                    JuriOneriFormu.EYKDaOnaylandi = Onaylandi;
                    if (MB.EYKTarihi == null && Onaylandi == true) MB.EYKTarihi = DateTime.Now.Date;
                    JuriOneriFormu.EYKDaOnaylandiOnayTarihi = DateTime.Now;
                    JuriOneriFormu.EYKDaOnaylandiIslemYapanID = UserIdentity.Current.Id;
                    JuriOneriFormu.EYKDaOnaylanmadiDurumAciklamasi = Onaylandi == false ? EYKDaOnaylanmadiDurumAciklamasi : "";
                    bool SendMail = Onaylandi == true && MB.EYKTarihi.HasValue;
                    if (SendMail)
                    {

                        var DanismanSablonID = 0;
                        var AsilSablonID = 0;
                        var OgrenciSablonID = 0;
                        if (MB.OgrenimTipKod == OgrenimTipi.TezliYuksekLisans)
                        {
                            DanismanSablonID = MailSablonTipi.Mez_EykTarihiGirildiDanismanYL;
                            AsilSablonID = MailSablonTipi.Mez_EykTarihiGirildiJuriAsilYL;
                            OgrenciSablonID = MailSablonTipi.Mez_EykTarihiGirildiOgrenciYL;
                        }
                        else
                        {
                            DanismanSablonID = MailSablonTipi.Mez_EykTarihiGirildiDanismanDoktora;
                            AsilSablonID = MailSablonTipi.Mez_EykTarihiGirildiJuriAsilDoktora;
                            OgrenciSablonID = MailSablonTipi.Mez_EykTarihiGirildiOgrenciDoktora;
                        }
                        #region sendMail
                        if (SendMail)
                        {
                            var TezKonusu = "";
                            if (JuriOneriFormu.IsTezBasligiDegisti == true)
                            {
                                TezKonusu = MB.IsTezDiliTr == true
                                    ? JuriOneriFormu.YeniTezBaslikTr
                                    : JuriOneriFormu.YeniTezBaslikEn;
                            }
                            else TezKonusu = MB.IsTezDiliTr == true
                                ? MB.TezBaslikTr
                                : MB.TezBaslikEn;

                            var Enstitu = MB.MezuniyetSureci.Enstituler;
                            var Sablonlar = db.MailSablonlaris.Where(p => p.EnstituKod == Enstitu.EnstituKod).ToList();

                            var mModel = new List<SablonMailModel> {
                            new SablonMailModel {

                            AdSoyad =MB.Ad + " " + MB.Soyad,
                            EMails= new List<MailSendList> { new MailSendList { EMail =MB.Kullanicilar.EMail,ToOrBcc=true } },
                            MailSablonTipID=OgrenciSablonID
                            } };
                            var Juriler = JuriOneriFormu.MezuniyetJuriOneriFormuJurileris.Where(p => p.IsAsilOrYedek.HasValue).ToList();
                            foreach (var item in Juriler.Where(p => p.IsAsilOrYedek == true))
                            {
                                mModel.Add(new SablonMailModel
                                {

                                    AdSoyad = item.AdSoyad,
                                    EMails = new List<MailSendList> { new MailSendList { EMail = item.EMail, ToOrBcc = true } },
                                    MailSablonTipID = (item.JuriTipAdi == "TezDanismani" ? DanismanSablonID : AsilSablonID),
                                    JuriTipAdi = item.JuriTipAdi,
                                    UnvanAdi = item.UnvanAdi,
                                    MezuniyetJuriOneriFormuJuriID = item.MezuniyetJuriOneriFormuJuriID,
                                });
                                if (item.JuriTipAdi == "TezDanismani" && !MB.TezEsDanismanEMail.IsNullOrWhiteSpace())
                                {
                                    //Eş danışman var ise Danışmana giden mail eş danışmana da gönderilmesi için.
                                    mModel.Add(new SablonMailModel
                                    {

                                        AdSoyad = MB.TezEsDanismanAdi,
                                        EMails = new List<MailSendList> { new MailSendList { EMail = MB.TezEsDanismanEMail, ToOrBcc = true } },
                                        MailSablonTipID = DanismanSablonID,
                                        JuriTipAdi = item.JuriTipAdi,
                                        UnvanAdi = MB.TezEsDanismanUnvani,
                                    });
                                }
                            }
                            var Danisman = Juriler.Where(p => p.JuriTipAdi == "TezDanismani").First();


                            foreach (var item in mModel)
                            {
                                var EnstituL = MB.MezuniyetSureci.Enstituler;
                                var AbdL = MB.Programlar.AnabilimDallari;
                                var PrgL = MB.Programlar;
                                item.ProgramAdi = PrgL.ProgramAdi;
                                item.Sablon = Sablonlar.Where(p => p.MailSablonTipID == item.MailSablonTipID).First();
                                item.SablonParametreleri = item.Sablon.MailSablonTipleri.Parametreler.Split(',').ToList().Select(s => s.Trim()).ToList();

                                //Şablona ait ekler var ise attachmets e ekle
                                var gonderilenMailEkleri = new List<GonderilenMailEkleri>();
                                foreach (var itemSe in item.Sablon.MailSablonlariEkleris)
                                {
                                    var ekTamYol = Server.MapPath("~" + itemSe.EkDosyaYolu);
                                    if (System.IO.File.Exists(ekTamYol))
                                    {
                                        var FExtension = Path.GetExtension(ekTamYol);
                                        item.Attachments.Add(new System.Net.Mail.Attachment(new MemoryStream(System.IO.File.ReadAllBytes(ekTamYol)),
                                            itemSe.EkAdi.ToSetNameFileExtension(FExtension), System.Net.Mime.MediaTypeNames.Application.Octet));
                                        gonderilenMailEkleri.Add(new GonderilenMailEkleri { EkAdi = itemSe.EkAdi, EkDosyaYolu = itemSe.EkDosyaYolu });
                                    }
                                    else Management.SistemBilgisiKaydet("Mail gönderilirken eklenen dosya eki sistemde bulunamadı!<br/>Dosya Adı:" + itemSe.EkAdi + " <br/>Dosya Yolu:" + ekTamYol, "MezuniyetGelenBasvurular/JuriOneriFormuOnayDurumKayit", BilgiTipi.Uyarı);
                                }

                                if (item.Sablon.GonderilecekEkEpostalar != null) item.EMails.AddRange(item.Sablon.GonderilecekEkEpostalar.Split(',').Select(s => new MailSendList { EMail = s.Trim(), ToOrBcc = false }));
                                var ParamereDegerleri = new List<MailReplaceParameterModel>();

                                if (item.SablonParametreleri.Any(a => a == "@EnstituAdi"))
                                    ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "EnstituAdi", Value = EnstituL.EnstituAd });
                                if (item.SablonParametreleri.Any(a => a == "@WebAdresi"))
                                    ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "WebAdresi", Value = Enstitu.WebAdresi, IsLink = true });
                                if (item.SablonParametreleri.Any(a => a == "@EYKTarihi"))
                                    ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "EYKTarihi", Value = MB.EYKTarihi.Value.ToDateString() });
                                if (item.SablonParametreleri.Any(a => a == "@AdSoyad"))
                                    ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "AdSoyad", Value = item.AdSoyad });
                                if (item.SablonParametreleri.Any(a => a == "@UnvanAdi"))
                                    ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "UnvanAdi", Value = item.UnvanAdi });
                                if (item.SablonParametreleri.Any(a => a == "@OgrenciAdSoyad"))
                                    ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "OgrenciAdSoyad", Value = MB.Ad + " " + MB.Soyad });
                                if (item.SablonParametreleri.Any(a => a == "@OgrenciBilgi"))
                                    ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "OgrenciBilgi", Value = (MB.OgrenciNo + " " + MB.Ad + " " + MB.Soyad + " (" + AbdL.AnabilimDaliAdi + " / " + PrgL.ProgramAdi + ")") });

                                if (item.SablonParametreleri.Any(a => a == "@TezBaslikTr"))
                                    ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "TezBaslikTr", Value = TezKonusu });
                                if (item.SablonParametreleri.Any(a => a == "@DanismanBilgi"))
                                    ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "DanismanBilgi", Value = Danisman.UnvanAdi + " " + Danisman.AdSoyad });
                                foreach (var itemAsil in JuriOneriFormu.MezuniyetJuriOneriFormuJurileris.Where(p => p.JuriTipAdi != "TezDanismani" && p.IsAsilOrYedek == true).Select((s, inx) => new { s, inx = inx + 1 }))
                                    if (item.SablonParametreleri.Any(a => a == "@AsilBilgi" + itemAsil.inx))
                                    {
                                        string uniBilgi = "";
                                        if (itemAsil.s.JuriTipAdi.Contains("YtuDisiJuri"))
                                        {
                                            uniBilgi = " (" + (itemAsil.s.UniversiteID.HasValue ? itemAsil.s.Universiteler.KisaAd : itemAsil.s.UniversiteAdi) + ")";
                                        }
                                        ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "AsilBilgi" + itemAsil.inx, Value = itemAsil.s.UnvanAdi + " " + itemAsil.s.AdSoyad + uniBilgi });
                                    }
                                foreach (var itemYedek in JuriOneriFormu.MezuniyetJuriOneriFormuJurileris.Where(p => p.IsAsilOrYedek == false).Select((s, inx) => new { s, inx = inx + 1 }))
                                    if (item.SablonParametreleri.Any(a => a == "@YedekBilgi" + itemYedek.inx))
                                    {
                                        string uniBilgi = "";
                                        if (itemYedek.s.JuriTipAdi.Contains("YtuDisiJuri"))
                                        {
                                            uniBilgi = " (" + (itemYedek.s.UniversiteID.HasValue ? itemYedek.s.Universiteler.KisaAd : itemYedek.s.UniversiteAdi) + ")";
                                        }
                                        ParamereDegerleri.Add(new MailReplaceParameterModel { Key = "YedekBilgi" + itemYedek.inx, Value = itemYedek.s.UnvanAdi + " " + itemYedek.s.AdSoyad + uniBilgi });
                                    }
                                var mCOntent = SystemMails.GetSystemMailContent(EnstituL.EnstituAd, item.Sablon.SablonHtml, item.Sablon.SablonAdi, ParamereDegerleri);
                                // item.EMails = new List<MailSendList> { new MailSendList { EMail = "irfansecer@gmail.com", ToOrBCC = true } }; //test için
                                var snded = MailManager.sendMail(Enstitu.EnstituKod, mCOntent.Title, mCOntent.HtmlContent, item.EMails, item.Attachments);
                                if (snded)
                                {
                                    var kModel = new GonderilenMailler();
                                    kModel.Tarih = DateTime.Now;
                                    kModel.EnstituKod = Enstitu.EnstituKod;
                                    kModel.MesajID = null;
                                    kModel.IslemTarihi = DateTime.Now;
                                    kModel.Konu = item.Sablon.SablonAdi + " (" + item.AdSoyad + ")";
                                    if (!item.JuriTipAdi.IsNullOrWhiteSpace()) kModel.Konu = kModel.Konu + " (" + item.JuriTipAdi + ")";
                                    kModel.IslemYapanID = UserIdentity.Current == null || !UserIdentity.Current.IsAuthenticated ? 1 : UserIdentity.Current.Id;
                                    kModel.IslemYapanIP = UserIdentity.Ip;
                                    kModel.Aciklama = item.Sablon.Sablon ?? "";
                                    kModel.AciklamaHtml = mCOntent.HtmlContent ?? "";
                                    kModel.Gonderildi = true;
                                    kModel.GonderilenMailKullanicilars = item.EMails.Select(s => new GonderilenMailKullanicilar { Email = s.EMail }).ToList();
                                    gonderilenMailEkleri.AddRange(item.Attachments.Select(s => new GonderilenMailEkleri { EkAdi = s.Name, EkDosyaYolu = "" }).ToList());
                                    kModel.GonderilenMailEkleris = gonderilenMailEkleri;
                                    db.GonderilenMaillers.Add(kModel);
                                    db.SaveChanges();
                                }
                            }

                        }
                        #endregion
                    }
                }
                else
                {
                    JuriOneriFormu.EYKYaGonderildi = Onaylandi;
                    JuriOneriFormu.EYKYaGonderildiIslemTarihi = DateTime.Now;
                    JuriOneriFormu.EYKYaGonderildiIslemYapanID = UserIdentity.Current.Id;
                }
                db.SaveChanges();
                mmMessage.MessageType = Msgtype.Success;
                mmMessage.IsSuccess = true;

                mmMessage.Messages.Add("Form " + (Onaylandi.HasValue ? (Onaylandi.Value ? "'Onaylandı'" : "'Onaylanmadı'") : "İşlem bekliyor") + " şeklinde güncellendi...");
            }
            var strView = Management.RenderPartialView("Ajax", "getMessage", mmMessage);
            return new
            {
                mmMessage.IsSuccess,
                Messages = strView
            }.toJsonResult();
        }


        public ActionResult SRJuriDegistir(Guid UniqueID)
        {
            var SRTalep = db.SRTalepleris.Where(p => p.UniqueID == UniqueID).First();

            return View(SRTalep);
        }


        public ActionResult SRJuriDegistirPost(Guid UniqueID, int? YtuIciMezuniyetJuriOneriFormuJuriID, int? YtuDisiMezuniyetJuriOneriFormuJuriID)
        {
            var mmMessage = new MmMessage();
            mmMessage.IsSuccess = false;
            mmMessage.Title = "Tez Sınavı Jüri Değişiklik İşlemi";
            mmMessage.MessageType = Msgtype.Success;
            var SRTalep = db.SRTalepleris.Where(p => p.UniqueID == UniqueID).First();
            if (YtuIciMezuniyetJuriOneriFormuJuriID.HasValue)
            {
                var SRYtuIciJuri = SRTalep.SRTaleplerJuris.Where(p => p.JuriTipAdi.Contains("YtuIciJuri") && p.MezuniyetJuriOneriFormuJuriID != YtuIciMezuniyetJuriOneriFormuJuriID).FirstOrDefault();
                if (SRYtuIciJuri != null)
                {
                    var Juri = db.MezuniyetJuriOneriFormuJurileris.Where(p => p.MezuniyetJuriOneriFormuJuriID == YtuIciMezuniyetJuriOneriFormuJuriID).First();
                    SRYtuIciJuri.UniqueID = Guid.NewGuid();
                    SRYtuIciJuri.MezuniyetJuriOneriFormuJuriID = YtuDisiMezuniyetJuriOneriFormuJuriID;
                    SRYtuIciJuri.UniversiteAdi = Juri.UniversiteAdi;
                    SRYtuIciJuri.AnabilimdaliProgramAdi = Juri.AnabilimdaliProgramAdi;
                    SRYtuIciJuri.JuriTipAdi = Juri.JuriTipAdi;
                    SRYtuIciJuri.UnvanAdi = Juri.UnvanAdi;
                    SRYtuIciJuri.JuriAdi = Juri.AdSoyad;
                    SRYtuIciJuri.Email = Juri.EMail;
                    SRYtuIciJuri.IsLinkGonderildi = false;
                    SRYtuIciJuri.MezuniyetSinavDurumID = null;
                    SRYtuIciJuri.IslemTarihi = DateTime.Now;
                    SRYtuIciJuri.IslemYapanID = UserIdentity.Current.Id;
                    SRYtuIciJuri.IslemYapanIP = UserIdentity.Ip;
                }
                mmMessage.Messages.Add("YTÜ İçi Jüri Değişikliği Yapıldı.");
            }
            if (YtuDisiMezuniyetJuriOneriFormuJuriID.HasValue)
            {
                var YtuDisiJuri = SRTalep.SRTaleplerJuris.Where(p => p.JuriTipAdi.Contains("YtuDisiJuri") && p.MezuniyetJuriOneriFormuJuriID != YtuDisiMezuniyetJuriOneriFormuJuriID).FirstOrDefault();
                if (YtuDisiJuri != null)
                {
                    var Juri = db.MezuniyetJuriOneriFormuJurileris.Where(p => p.MezuniyetJuriOneriFormuJuriID == YtuDisiMezuniyetJuriOneriFormuJuriID).First();
                    YtuDisiJuri.UniqueID = Guid.NewGuid();
                    YtuDisiJuri.MezuniyetJuriOneriFormuJuriID = YtuDisiMezuniyetJuriOneriFormuJuriID;
                    YtuDisiJuri.UniversiteAdi = Juri.UniversiteAdi;
                    YtuDisiJuri.AnabilimdaliProgramAdi = Juri.AnabilimdaliProgramAdi;
                    YtuDisiJuri.JuriTipAdi = Juri.JuriTipAdi;
                    YtuDisiJuri.UnvanAdi = Juri.UnvanAdi;
                    YtuDisiJuri.JuriAdi = Juri.AdSoyad;
                    YtuDisiJuri.Email = Juri.EMail;
                    YtuDisiJuri.IsLinkGonderildi = false;
                    YtuDisiJuri.MezuniyetSinavDurumID = null;
                    YtuDisiJuri.IslemTarihi = DateTime.Now;
                    YtuDisiJuri.IslemYapanID = UserIdentity.Current.Id;
                    YtuDisiJuri.IslemYapanIP = UserIdentity.Ip;
                    mmMessage.Messages.Add("YTÜ Dışı Jüri Değişikliği Yapıldı.");
                }
            }
            db.SaveChanges();
            mmMessage.IsSuccess = true;
            mmMessage.MessageType = Msgtype.Success;
            return mmMessage.toJsonResult();
        }
        public ActionResult Sil(int id)
        {
            var mmMessage = Management.getMezuniyetBasvurusuSilKontrol(id);
            if (mmMessage.IsSuccess)
            {
                var kayit = db.MezuniyetBasvurularis.Where(p => p.MezuniyetBasvurulariID == id).FirstOrDefault();
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
                    db.MezuniyetBasvurularis.Remove(kayit);
                    db.SaveChanges();
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
                    Management.SistemBilgisiKaydet(ex.ToExceptionMessage(), "MezuniyetGelenBasvurular/Sil<br/><br/>" + ex.ToExceptionStackTrace(), BilgiTipi.OnemsizHata);
                }

            }
            var strView = Management.RenderPartialView("Ajax", "getMessage", mmMessage);
            return Json(new { IsSuccess = mmMessage.IsSuccess, Messages = strView }, "application/json", JsonRequestBehavior.AllowGet);
        }




        public ActionResult GetTutanakRaporu()
        {
            return View();
        }
        public ActionResult GetTutanakRaporuKontrolu(int RaporTipID, List<int> OgrenimTipKods, DateTime? BasTar, DateTime? BitTar, DateTime? RaporTarihi)
        {
            var mMessage = new MmMessage();
            mMessage.MessageType = Msgtype.Success;
            mMessage.IsSuccess = true;

            if (OgrenimTipKods == null || OgrenimTipKods.Count == 0)
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
            if (RaporTipID == RaporTipleri.MezuniyetTutanakRaporu && !RaporTarihi.HasValue && OgrenimTipKods != null && OgrenimTipKods.Any(a => a == OgrenimTipi.Doktra))
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

            return mMessage.toJsonResult();



        }
        public ActionResult GetTutanakRaporuExport(int RaporTipID, int OgrenimTipKods, string BasTar, string BitTar, string RaporTarihi, bool ExportWordOrExcel, string EKD)
        {

            var html = "";
            string RaporAdi = "";
            var _EnstituKod = Management.getSelectedEnstitu(EKD);
            var baslangicTarihi = BasTar.ToDate().Value;
            var bitisTarihi = BitTar.ToDate().Value;
            var qData = db.MezuniyetBasvurularis.Where(p => p.MezuniyetSureci.EnstituKod == _EnstituKod).AsQueryable();
            if (RaporTipID == RaporTipleri.MezuniyetTezJuriTutanakRaporu) qData = qData.Where(p => p.MezuniyetJuriOneriFormlaris.Any(a => a.EYKDaOnaylandi == true) && OgrenimTipKods == p.OgrenimTipKod && (p.EYKTarihi >= baslangicTarihi && p.EYKTarihi <= bitisTarihi)).OrderByDescending(o => o.OgrenimTipKod).ThenBy(t => t.EYKTarihi);
            else qData = qData.Where(p => p.IsMezunOldu == true && OgrenimTipKods == p.OgrenimTipKod && (p.MezuniyetTarihi >= baslangicTarihi && p.MezuniyetTarihi <= bitisTarihi)).OrderBy(o => o.MezuniyetTarihi);
            var Data = qData.ToList();



            if (RaporTipID == RaporTipleri.MezuniyetTezJuriTutanakRaporu)
            {
                var Model = new List<RprTutanakModel>();
                var RModel = new RprTutanakModel();
                RModel.IsDoktoraOrYL = OgrenimTipKods == OgrenimTipi.Doktra;
                RModel.TutanakAdi = RModel.IsDoktoraOrYL ? "Doktora - Tez Sınav Jürileri Atama Önerileri Hk." : "Yüksek Lisans - Tez Savunma Jüri Önerileri Hk.";
                RModel.Aciklama = RModel.IsDoktoraOrYL ?
                                    "Tezini tamamlayarak Enstitümüze teslim eden aşağıda adı, Anabilim Dalı/Programı belirtilen doktora öğrencilerinin tez sınav jürilerinin “YTÜ Lisansüstü Eğitim ve Öğretim Yönetmeliği” nin ilgili maddesi uyarınca, aşağıdaki öğretim üyelerinden oluşmasına oybirliği ile karar verildi. "
                                    :
                                   "Tezini tamamlayarak Enstitümüze teslim eden aşağıda adı, Anabilim Dalı/Programı belirtilen yüksek lisans öğrencilerinin tez sınav jürilerinin “YTÜ Lisansüstü Eğitim ve Öğretim Yönetmeliği” nin ilgili maddesi uyarınca, aşağıdaki öğretim üyelerinden oluşmasına oybirliği ile karar verildi.";


                foreach (var itemO in Data)
                {
                    var row = new RprTutanakRowModel();
                    var prgl = itemO.Programlar;
                    var abdl = itemO.Programlar.AnabilimDallari;
                    row.OgrenciBilgi = itemO.OgrenciNo + " " + itemO.Ad + " " + itemO.Soyad + " (" + abdl.AnabilimDaliAdi + " / " + prgl.ProgramAdi + ")";
                    var JOForm = itemO.MezuniyetJuriOneriFormlaris.First();
                    var Danisman = JOForm.MezuniyetJuriOneriFormuJurileris.Where(p => p.JuriTipAdi == "TezDanismani").First();
                    row.DanismanAdSoyad = Danisman.UnvanAdi + " " + Danisman.AdSoyad;
                    row.DanismanUni = Danisman.UniversiteID.HasValue ? Danisman.Universiteler.Ad : Danisman.UniversiteAdi;
                    if (RModel.IsDoktoraOrYL)
                    {
                        var Tik1 = JOForm.MezuniyetJuriOneriFormuJurileris.Where(p => p.JuriTipAdi == "TikUyesi1").First();
                        row.TikUyesi = Tik1.UnvanAdi + " " + Tik1.AdSoyad;
                        row.TikUyesiUni = Tik1.UniversiteID.HasValue ? Tik1.Universiteler.Ad : Tik1.UniversiteAdi;

                        var Tik2 = JOForm.MezuniyetJuriOneriFormuJurileris.Where(p => p.JuriTipAdi == "TikUyesi2").First();
                        row.TikUyesi2 = Tik2.UnvanAdi + " " + Tik2.AdSoyad;
                        row.TikUyesi2Uni = Tik2.UniversiteID.HasValue ? Tik2.Universiteler.Ad : Tik2.UniversiteAdi;
                    }
                    var jtList = new List<string> { "TezDanismani", "TikUyesi1", "TikUyesi2" };
                    var AsilUye = JOForm.MezuniyetJuriOneriFormuJurileris.Where(p => !jtList.Contains(p.JuriTipAdi) && p.IsAsilOrYedek == true).First();
                    row.AsilUye = AsilUye.UnvanAdi + " " + AsilUye.AdSoyad;
                    row.AsilUyeUni = AsilUye.UniversiteID.HasValue ? AsilUye.Universiteler.Ad : AsilUye.UniversiteAdi;
                    jtList.Add(AsilUye.JuriTipAdi);
                    var AsilUye2 = JOForm.MezuniyetJuriOneriFormuJurileris.Where(p => !jtList.Contains(p.JuriTipAdi) && p.IsAsilOrYedek == true).First();
                    row.AsilUye2 = AsilUye2.UnvanAdi + " " + AsilUye2.AdSoyad;
                    row.AsilUye2Uni = AsilUye2.UniversiteID.HasValue ? AsilUye2.Universiteler.Ad : AsilUye2.UniversiteAdi;
                    jtList.Add(AsilUye2.JuriTipAdi);
                    var yedekUye = JOForm.MezuniyetJuriOneriFormuJurileris.Where(p => !jtList.Contains(p.JuriTipAdi) && p.IsAsilOrYedek == false).First();
                    row.YedekUye = yedekUye.UnvanAdi + " " + yedekUye.AdSoyad;
                    row.YedekUyeUni = yedekUye.UniversiteID.HasValue ? yedekUye.Universiteler.Ad : yedekUye.UniversiteAdi;
                    jtList.Add(yedekUye.JuriTipAdi);
                    var yedekUye2 = JOForm.MezuniyetJuriOneriFormuJurileris.Where(p => !jtList.Contains(p.JuriTipAdi) && p.IsAsilOrYedek == false).First();
                    row.YedekUye2 = yedekUye2.UnvanAdi + " " + yedekUye2.AdSoyad;
                    row.YedekUye2Uni = yedekUye2.UniversiteID.HasValue ? yedekUye2.Universiteler.Ad : yedekUye2.UniversiteAdi;

                    var TezBasligi = "";
                    if (JOForm.IsTezBasligiDegisti == true)
                    {
                        TezBasligi = itemO.IsTezDiliTr == true ? JOForm.YeniTezBaslikTr : JOForm.YeniTezBaslikEn;
                    }
                    else TezBasligi = itemO.IsTezDiliTr == true ? itemO.TezBaslikTr : itemO.TezBaslikEn;
                    row.TezKonusu = TezBasligi;

                    RModel.DetayData.Add(row);

                    Model.Add(RModel);
                }


                rprMezuniyetTezJuriTutanak rpr = new rprMezuniyetTezJuriTutanak(RModel.IsDoktoraOrYL);
                rpr.DataSource = Model.Count > 0 ? Model[0] : new RprTutanakModel();
                rpr.CreateDocument();
                RaporAdi = (OgrenimTipKods == OgrenimTipi.Doktra ? "Doktra" : "Yüksek Lisans") + " Tez Sınav Jürileri Atama Önerileri";

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
                if (OgrenimTipKods == OgrenimTipi.Doktra)
                {
                    var Model = new List<RprMezuniyetTutanakModel>();
                    DateTime _RaporTarihi = RaporTarihi.ToDate().Value;
                    foreach (var itemO in Data)
                    {
                        var row = new RprMezuniyetTutanakModel();
                        var prgl = itemO.Programlar;
                        var abdl = itemO.Programlar.AnabilimDallari;
                        var sinav = itemO.SRTalepleris.Where(p => p.MezuniyetSinavDurumID == MezuniyetSinavDurum.Basarili).First();
                        var tezSonBilgi = sinav.SRTalepleriBezCiltFormus.First();
                        var danismanBilgi = "";
                        var JOForm = itemO.MezuniyetJuriOneriFormlaris.FirstOrDefault();
                        if (JOForm != null)
                        {
                            var Danisman = JOForm.MezuniyetJuriOneriFormuJurileris.Where(p => p.JuriTipAdi == "TezDanismani").First();
                            danismanBilgi = Danisman.UnvanAdi + " " + Danisman.AdSoyad;
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
                            + "<b>'DOKTORA DERECESİ'</b> verildiğini bildiren jüri ortak raporunun <b>" + _RaporTarihi.Date.ToString("dd.MM.yyyy") + "</b> tarihi itibariyle onanmasına ve Üniversite Senatosu'na sunulmak üzere Rektörlüğe arzına </b>oybirliğiyle</b> karar verildi.";
                        Model.Add(row);
                    }
                    rprMezuniyetMezunlarTutanakDR rpr = new rprMezuniyetMezunlarTutanakDR();
                    rpr.DataSource = Model;
                    rpr.CreateDocument();
                    RaporAdi = "Doktora Mezuniyet Tutanağı";

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
                    var Model = new RprMezuniyetTutanakModel();
                    Model.Konu = "Yüksek Lisans Mezuniyeti Hk";
                    Model.Aciklama1 = "“YTÜ Lisansüstü Eğitim ve Öğretim Yönetmeliği” nin yüksek lisans eğitimi ile ilgili tüm koşullarını yerine getiren, aşağıda adı - soyadı, "
                                       + "Anabilim Dalı/ Programı belirtilen Enstitümüz yüksek lisans programı öğrencilerinin, 1 Mart 2017 tarih ve 29994 sayılı Yüksek Öğretim Kurulu "
                                       + "Lisansüstü Eğitim ve Öğretim Yönetmeliğinde Değişiklik Yapılmasına Dair Yönetmelik:<b> Madde 2 - “Mezuniyet Tarihi tezin sınav jüri komisyonu tarafından "
                                       + "imzalı nüshasının teslim edildiği tarihtir.”</b> gereğince, " + baslangicTarihi.ToString("dd.MM.yyyy") + " ile " + bitisTarihi.ToString("dd.MM.yyyy") + " tarihleri arasında tezlerini Enstitümüze teslim eden öğrencilerin "
                                       + "aşağıda belirtilen tez teslim tarihinde mezuniyetlerine oybirliğiyle karar verildi.";

                    foreach (var itemO in Data)
                    {
                        var row = new RprMezuniyetTutanakRowModel();
                        var prgl = itemO.Programlar;
                        var abdl = itemO.Programlar.AnabilimDallari;
                        row.OgrenciBilgi = itemO.OgrenciNo + " " + itemO.Ad + " " + itemO.Soyad + " (" + abdl.AnabilimDaliAdi + " / " + prgl.ProgramAdi + ")";

                        var sinav = itemO.SRTalepleris.Where(p => p.MezuniyetSinavDurumID == MezuniyetSinavDurum.Basarili).First();
                        var tezSonBilgi = sinav.SRTalepleriBezCiltFormus.First();
                        var danismanBilgi = "";
                        var JOForm = itemO.MezuniyetJuriOneriFormlaris.FirstOrDefault();
                        if (JOForm != null)
                        {
                            var Danisman = JOForm.MezuniyetJuriOneriFormuJurileris.Where(p => p.JuriTipAdi == "TezDanismani").First();
                            danismanBilgi = Danisman.UnvanAdi + " " + Danisman.AdSoyad;
                        }
                        else
                        {
                            danismanBilgi = sinav.SRTaleplerJuris.First().JuriAdi.ToUpper();
                        }
                        row.DanismanAdSoyad = danismanBilgi;
                        row.TezKonusu = tezSonBilgi.IsTezDiliTr ? tezSonBilgi.TezBaslikTr : tezSonBilgi.TezBaslikEn;
                        row.SavunmaTarihi = sinav.Tarih.ToString("dd.MM.yyyy");
                        row.TezTeslimTarihi = itemO.MezuniyetTarihi.ToString("dd.MM.yyyy");

                        Model.Data.Add(row);

                    }
                    rprMezuniyetMezunlarTutanakYL rpr = new rprMezuniyetMezunlarTutanakYL();
                    rpr.DataSource = Model;
                    rpr.CreateDocument();
                    RaporAdi = "Yüksek Lisans Mezuniyet Tutanağı";

                    using (MemoryStream ms = new MemoryStream())
                    {
                        rpr.ExportToHtml(ms);
                        ms.Position = 0;
                        var sr = new StreamReader(ms);
                        html = sr.ReadToEnd();
                    }

                }

            }
            return File(System.Text.Encoding.UTF8.GetBytes(html), (ExportWordOrExcel ? "application/vnd.ms-word" : "application/ms-excel"), RaporAdi + " (" + BasTar.Replace("-", ".") + "-" + BitTar.Replace("-", ".") + ")." + (ExportWordOrExcel ? "doc" : "xls"));



        }


    }
}