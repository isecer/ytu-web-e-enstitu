using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Mvc;
using System.Web.UI;
using System.Web.UI.WebControls;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Business;
using Entities.Entities;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.Helpers;
using LisansUstuBasvuruSistemi.Utilities.Logs;
using LisansUstuBasvuruSistemi.Utilities.MenuAndRoles;
using LisansUstuBasvuruSistemi.Utilities.SystemSetting;

namespace LisansUstuBasvuruSistemi.Controllers
{
    [Authorize(Roles = RoleNames.YeterlikGelenBasvurular)]
    [OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    public class YeterlikGelenBasvurularController : Controller
    {
        private readonly LubsDbEntities _entities = new LubsDbEntities();
        public ActionResult Index(string ekd)
        {
            var enstituKod = EnstituBus.GetSelectedEnstitu(ekd);
            var aktifSurecId = YeterlikBus.GetYeterlikAktifSurecId(enstituKod);
            return Index(new FmYeterlikBasvuruDto { PageSize = 50, YeterlikSurecID = aktifSurecId }, ekd, false);
        }
        [HttpPost]
        public ActionResult Index(FmYeterlikBasvuruDto model, string ekd, bool export)
        {
            var enstituKod = EnstituBus.GetSelectedEnstitu(ekd);
            var isOnayBekleyenKomite = model.BasvuruDurumID == YeterlikBasvuruFilterEnum.KomiteOnayiBekleyenler;
            var isOnayBekleyenJuri = model.BasvuruDurumID == YeterlikBasvuruFilterEnum.SinavSurecindeOlanlar;
            var kullaniciId = UserIdentity.Current.Id;
            var q = from yeterlikBasvuru in _entities.YeterlikBasvurus
                    join yeterlikSureci in _entities.YeterlikSurecis.Where(p => p.EnstituKod == enstituKod && UserIdentity.Current.EnstituKods.Contains(p.EnstituKod)) on yeterlikBasvuru.YeterlikSurecID equals yeterlikSureci.YeterlikSurecID
                    join kullanicilar in _entities.Kullanicilars on yeterlikBasvuru.KullaniciID equals kullanicilar.KullaniciID
                    join programlar in _entities.Programlars on yeterlikBasvuru.ProgramKod equals programlar.ProgramKod
                    join ogrenimTipleri in _entities.OgrenimTipleris on yeterlikBasvuru.OgrenimTipID equals ogrenimTipleri.OgrenimTipID
                    join tezDanismani in _entities.Kullanicilars on yeterlikBasvuru.TezDanismanID equals tezDanismani.KullaniciID
                    let birOncekiBasvuru = _entities.YeterlikBasvurus.Where(p => p.KullaniciID == yeterlikBasvuru.KullaniciID &&
                                                                                                                    p.ProgramKod == yeterlikBasvuru.ProgramKod &&
                                                                                                                    p.OgrenciNo == yeterlikBasvuru.OgrenciNo &&
                                                                                                                    p.YeterlikBasvuruID < yeterlikBasvuru.YeterlikBasvuruID).OrderByDescending(o => o.YeterlikBasvuruID).FirstOrDefault()
                    select new FrYeterlikBasvuruDto
                    {
                        YeterlikSurecID = yeterlikBasvuru.YeterlikSurecID,
                        DonemAdi = yeterlikSureci.BaslangicYil + "/" + yeterlikSureci.BitisYil + " " + yeterlikSureci.Donemler.DonemAdi,
                        YeterlikBasvuruID = yeterlikBasvuru.YeterlikBasvuruID,
                        UniqueID = yeterlikBasvuru.UniqueID,
                        BasvuruTarihi = yeterlikBasvuru.BasvuruTarihi,
                        ResimAdi = kullanicilar.ResimAdi,
                        EMail = kullanicilar.EMail,
                        CepTel = kullanicilar.CepTel,
                        KullaniciID = yeterlikBasvuru.KullaniciID,
                        IsDanismaniOlunanOgrenci = kullaniciId == yeterlikBasvuru.TezDanismanID,
                        UserKey = kullanicilar.UserKey,
                        AdSoyad = kullanicilar.Ad + " " + kullanicilar.Soyad,
                        TcKimlikNo = kullanicilar.TcKimlikNo,
                        OgrenciNo = yeterlikBasvuru.OgrenciNo,
                        OgrenimTipKod = yeterlikBasvuru.OgrenimTipleri.OgrenimTipKod,
                        OgrenimTipID = yeterlikBasvuru.OgrenimTipID,
                        OgrenimTipAdi = ogrenimTipleri.OgrenimTipAdi,
                        ProgramKod = programlar.ProgramKod,
                        ProgramAdi = programlar.ProgramAdi,
                        AnabilimDaliID = programlar.AnabilimDaliID,
                        AnabilimDaliAdi = programlar.AnabilimDallari.AnabilimDaliAdi,
                        KayitTarihi = yeterlikBasvuru.KayitTarihi,
                        OkuduguDonemNo = yeterlikBasvuru.OkuduguDonemNo,
                        OnayYapmayanKomiteIds = yeterlikBasvuru.YeterlikBasvuruKomitelers.Where(p => isOnayBekleyenKomite && !p.IsJuriOnaylandi.HasValue).Select(sk => sk.KullaniciID).ToList(),
                        OnayYapmayanJuriEmails = yeterlikBasvuru.YeterlikBasvuruJuriUyeleris.Where(p => isOnayBekleyenJuri && p.IsLinkGonderildi == true && !p.IsSonucOnaylandi.HasValue).Select(sj => sj.EMail).ToList(),
                        TezDanismanID = yeterlikBasvuru.TezDanismanID,
                        TezDanismanAdi = tezDanismani.Unvanlar.UnvanAdi + " " + tezDanismani.Ad + " " + tezDanismani.Soyad,
                        TezDanismanEmail = tezDanismani.EMail,
                        TezDanismanCepTel = tezDanismani.CepTel,
                        IsEnstituOnaylandi = yeterlikBasvuru.IsEnstituOnaylandi,
                        EnstituOnayAciklama = yeterlikBasvuru.EnstituOnayAciklama,
                        IsJuriOlusturuldu = yeterlikBasvuru.YeterlikBasvuruJuriUyeleris.Any(),
                        IsAbdKomitesiJuriyiOnayladi = yeterlikBasvuru.IsAbdKomitesiJuriyiOnayladi,
                        YaziliSinavTarihi = yeterlikBasvuru.YaziliSinavTarihi,
                        YaziliSinavYeri = yeterlikBasvuru.YaziliSinavYeri,
                        IsYaziliSinavinaKatildi = yeterlikBasvuru.IsYaziliSinavinaKatildi,
                        YaziliSinaviNotu = yeterlikBasvuru.YaziliSinaviNotu,
                        IsYaziliSinavBasarili = yeterlikBasvuru.IsYaziliSinavBasarili,
                        IsSozluSinavOnline = yeterlikBasvuru.IsSozluSinavOnline,
                        SozluSinavTarihi = yeterlikBasvuru.SozluSinavTarihi,
                        SozluSinavYeri = yeterlikBasvuru.SozluSinavYeri,
                        IsSozluSinavinaKatildi = yeterlikBasvuru.IsSozluSinavinaKatildi,
                        SozluSinaviOrtalamaNotu = yeterlikBasvuru.SozluSinaviOrtalamaNotu,
                        GenelBasariNotu = yeterlikBasvuru.GenelBasariNotu,
                        IsGenelSonucBasarili = yeterlikBasvuru.IsGenelSonucBasarili,
                        BirOncekiBasvuru = birOncekiBasvuru, 



                    };
            var q2 = q;
            if (model.YeterlikSurecID.HasValue) q = q.Where(p => p.YeterlikSurecID == model.YeterlikSurecID);
            if (model.OgrenimTipID.HasValue) q = q.Where(p => p.OgrenimTipID == model.OgrenimTipID);
            if (model.AnabilimDaliID.HasValue) q = q.Where(p => p.AnabilimDaliID == model.AnabilimDaliID);
            if (!model.AdSoyad.IsNullOrWhiteSpace())
                q = q.Where(p =>
                    p.AdSoyad.Contains(model.AdSoyad)
                    || p.OgrenciNo.Contains(model.AdSoyad)
                    || p.TezDanismanAdi.Contains(model.AdSoyad)
                    || p.ProgramAdi.Contains(model.AdSoyad)
                    || p.AnabilimDaliAdi.Contains(model.AdSoyad));
            if (model.BasvuruDurumID.HasValue)
            {
                if (model.BasvuruDurumID == YeterlikBasvuruFilterEnum.IslemGormeyenler) q = q.Where(p => !p.IsEnstituOnaylandi.HasValue);
                else if (model.BasvuruDurumID == YeterlikBasvuruFilterEnum.Onaylananlar) q = q.Where(p => p.IsEnstituOnaylandi == true);
                else if (model.BasvuruDurumID == YeterlikBasvuruFilterEnum.IptalEdilenler) q = q.Where(p => p.IsEnstituOnaylandi == false);
                else if (model.BasvuruDurumID == YeterlikBasvuruFilterEnum.JuriOlusturulmayanlar) q = q.Where(p => p.IsEnstituOnaylandi == true && p.IsJuriOlusturuldu == false);
                else if (model.BasvuruDurumID == YeterlikBasvuruFilterEnum.KomiteOnayiBekleyenler) q = q.Where(p => p.IsEnstituOnaylandi == true && p.IsJuriOlusturuldu && p.IsAbdKomitesiJuriyiOnayladi != true);
                else if (model.BasvuruDurumID == YeterlikBasvuruFilterEnum.KomiteOnayiTamamlananlar) q = q.Where(p => p.IsEnstituOnaylandi == true && p.IsJuriOlusturuldu && p.IsAbdKomitesiJuriyiOnayladi == true);
                else if (model.BasvuruDurumID == YeterlikBasvuruFilterEnum.SinavSureciniBaslatilmayanlar) q = q.Where(p => (!p.YaziliSinavTarihi.HasValue) && p.IsAbdKomitesiJuriyiOnayladi == true && !p.IsYaziliSinavBasarili.HasValue);
                else if (model.BasvuruDurumID == YeterlikBasvuruFilterEnum.SinavSurecindeOlanlar) q = q.Where(p => (p.YaziliSinavTarihi.HasValue || p.SozluSinavTarihi.HasValue) && p.IsAbdKomitesiJuriyiOnayladi == true && !p.IsGenelSonucBasarili.HasValue);
                else if (model.BasvuruDurumID == YeterlikBasvuruFilterEnum.BasariliOlanlar) q = q.Where(p => p.IsGenelSonucBasarili == true);
                else if (model.BasvuruDurumID == YeterlikBasvuruFilterEnum.BasarisizOlanlar) q = q.Where(p => p.IsGenelSonucBasarili == false);
            }

            if (model.IsDanismaniOlunanOgrenciler.HasValue)
                q = q.Where(p => p.IsDanismaniOlunanOgrenci == model.IsDanismaniOlunanOgrenciler.Value);
            var danismanYetkisi = RoleNames.YeterlikDanismanYetkisi.InRoleCurrent();
            var programYetkisi = RoleNames.YeterlikProgramYetkisi.InRoleCurrent();
            var tumOgrenciGormeYetkisi = RoleNames.YeterlikTumBasvurulariGormeYetkisi.InRoleCurrent();

            if (!tumOgrenciGormeYetkisi)
            {
                var yetkiliProgramlar = UserIdentity.Current.SelectedEnstituProgramKod(enstituKod);
                 
                q = q.Where(p =>
                    (programYetkisi && yetkiliProgramlar.Contains(p.ProgramKod)) ||
                    (danismanYetkisi && p.TezDanismanID == UserIdentity.Current.Id)
                );
            } 
            #region export
            if (export && model.RowCount > 0)
            {
                var gv = new GridView();

                var yeterlikData = (from s in q
                                    select new
                                    {
                                        s.DonemAdi,
                                        s.KullaniciID,
                                        s.AdSoyad,
                                        s.TcKimlikNo,
                                        s.OgrenciNo,
                                        s.EMail,
                                        s.CepTel,
                                        s.OgrenimTipKod,
                                        s.OgrenimTipID,
                                        s.OgrenimTipAdi,
                                        s.ProgramKod,
                                        s.AnabilimDaliAdi,
                                        s.ProgramAdi,
                                        s.KayitTarihi,
                                        s.OkuduguDonemNo,
                                        s.TezDanismanAdi,
                                        s.TezDanismanCepTel,
                                        s.TezDanismanEmail,
                                        s.SozluSinavTarihi,
                                        EnstituOnayDurum = s.IsEnstituOnaylandi.HasValue ? (s.IsEnstituOnaylandi == true ? "Onaylandı" : "İptal Edildi") : "İşlem Bekliyor",
                                        BasariDurumu = s.IsEnstituOnaylandi == true && s.IsGenelSonucBasarili.HasValue ? (s.IsGenelSonucBasarili == true ? "Başarılı" : "Başarısız") : "İşlem Bekliyor",
                                        BirOncekiBasvuruDonemAdi = s.BirOncekiBasvuru != null ? (s.BirOncekiBasvuru.YeterlikSureci.BaslangicYil + "/" + s.BirOncekiBasvuru.YeterlikSureci.BitisYil + " " + s.BirOncekiBasvuru.Donemler.DonemAdi) : "Başvuru Yok",
                                        BirOnekiBasvuruDurumu = s.BirOncekiBasvuru != null ? s.BirOncekiBasvuru.IsEnstituOnaylandi == true ? (s.BirOncekiBasvuru.IsGenelSonucBasarili.HasValue ? (s.BirOncekiBasvuru.IsGenelSonucBasarili == true ? "Başarılı" : "Başarısız") : "İşlem Bekliyor") : (s.BirOncekiBasvuru.IsEnstituOnaylandi == false ? "İptal Edildi" : "İşlem Bekliyor") : "Başvuru Yok",
                                    }).ToList();
                var kullaniciIds = yeterlikData.Select(s => s.KullaniciID).Distinct();
                var ogrenciNo = yeterlikData.Select(s => s.OgrenciNo).Distinct().ToList();

                var tezOneriIlkSavunmaHakkiAyKriter =
                    TiAyar.TezOneriIlkSavunmaHakkiAyKriter.GetAyar(enstituKod).ToInt(0);
                var tezOneriIkinciSavunmaHakkiAyKriter =
                    TiAyar.TezOneriIkinciSavunmaHakkiAyKriter.GetAyar(enstituKod).ToInt(0);
                var tezOneriToplamSavunmaHakkiAyKriter =
                    tezOneriIlkSavunmaHakkiAyKriter + tezOneriIkinciSavunmaHakkiAyKriter;

                var tosData = (from s in _entities.ToBasvurus.Where(p => kullaniciIds.Contains(p.KullaniciID) && ogrenciNo.Contains(p.OgrenciNo))
                               join e in _entities.Enstitulers on s.EnstituKod equals e.EnstituKod
                               join k in _entities.Kullanicilars on s.KullaniciID equals k.KullaniciID
                               join o in _entities.OgrenimTipleris on new { s.OgrenimTipKod, e.EnstituKod } equals new { o.OgrenimTipKod, o.EnstituKod }
                               join pr in _entities.Programlars on s.ProgramKod equals pr.ProgramKod
                               join ab in _entities.AnabilimDallaris on s.Programlar.AnabilimDaliKod equals ab.AnabilimDaliKod
                               let ard = _entities.ToBasvuruSavunmas.Where(p => p.ToBasvuruID == s.ToBasvuruID).OrderByDescending(ot => ot.ToBasvuruSavunmaID).FirstOrDefault()
                               select new
                               {
                                   s.KullaniciID,
                                   s.OgrenciNo,
                                   s.OgrenimTipKod,
                                   s.IlkOneriBitisTarihi,
                                   s.IkinciOneriBitisTarihi,
                                   IsSavunmaBasvurusuVar = ard != null,
                                   SavunmaBasvuruTarihi = ard == null ? (DateTime?)null : ard.SavunmaBasvuruTarihi,
                                   IsSinavBilgisiGirildi = ard != null && ard.SRTalepleris.Any(),
                                   IsDegerlendirmeSuvecinde = ard != null && ard.ToBasvuruSavunmaKomites.Any(a => a.ToBasvuruSavunmaDurumID.HasValue),
                                   AktifSavunmaNo = ard == null ? (int?)null : ard.SavunmaNo,
                                   AktifDonemAdi = ard == null ? "----" : (ard.DonemBaslangicYil + " / " + (ard.DonemBaslangicYil + 1) + " " + (ard.DonemID == 1 ? "Güz" : "Bahar")),

                                   IsTezOnerisiVar = ard != null,
                                   ard.ToBasvuruSavunmaDurumID,
                                   IsSrTalebiYapildi = ard != null && ard.SRTalepleris.Any(),
                                   DegerlendirmeBasladi = ard != null && ard.ToBasvuruSavunmaKomites.Any(a => a.ToBasvuruSavunmaDurumID.HasValue),
                                   IsOyBirligiOrCoklugu = ard.IsOyBirligiOrCoklugu ?? false

                               }).ToList();

                var qExportData = (from yet in yeterlikData
                                   join tos in tosData on new { yet.KullaniciID, yet.OgrenciNo } equals new { tos.KullaniciID, tos.OgrenciNo } into defTos
                                   from tosa in defTos.DefaultIfEmpty()
                                   select new
                                   {
                                       yet.DonemAdi,
                                       yet.AdSoyad,
                                       yet.TcKimlikNo,
                                       yet.OgrenciNo,
                                       yet.EMail,
                                       yet.CepTel,
                                       yet.OgrenimTipAdi,
                                       yet.ProgramKod,
                                       yet.AnabilimDaliAdi,
                                       yet.ProgramAdi,
                                       yet.KayitTarihi,
                                       yet.OkuduguDonemNo,
                                       yet.TezDanismanAdi,
                                       yet.TezDanismanCepTel,
                                       yet.TezDanismanEmail,
                                       yet.EnstituOnayDurum,
                                       yet.BasariDurumu,
                                       yet.BirOncekiBasvuruDonemAdi,
                                       yet.BirOnekiBasvuruDurumu,
                                       BirinciSavunmaBitisTarihi = tosa == null ? (DateTime?)null : tosa.IlkOneriBitisTarihi ?? (yet.SozluSinavTarihi ?? DateTime.Now).ToGetBitisTarihi(tezOneriIlkSavunmaHakkiAyKriter),
                                       IkinciSavunmaBitisTarihi = tosa == null ? (DateTime?)null : tosa.IkinciOneriBitisTarihi ?? (yet.SozluSinavTarihi ?? DateTime.Now).ToGetBitisTarihi(tezOneriToplamSavunmaHakkiAyKriter),
                                       //tosa.SavunmaBasvuruTarihi,
                                       //tosa.IsSinavBilgisiGirildi,
                                       //tosa.IsDegerlendirmeSuvecinde,
                                       //tosa.AktifSavunmaNo,
                                       //tosa.AktifDonemAdi,
                                       //tosa.ToBasvuruSavunmaDurumID,
                                       tosa?.SavunmaBasvuruTarihi,
                                       SavunmaNo = tosa?.AktifSavunmaNo,
                                       SavunmaDurumu = ToDurumString(tosa?.IsSavunmaBasvurusuVar ?? false, tosa?.IsOyBirligiOrCoklugu ?? false, tosa?.IsSinavBilgisiGirildi ?? false, tosa?.IsDegerlendirmeSuvecinde ?? false, tosa?.ToBasvuruSavunmaDurumID)
                                   }).ToList();


                gv.DataSource = qExportData;
                gv.DataBind();
                Response.ContentType = "application/ms-excel";
                Response.ContentEncoding = System.Text.Encoding.UTF8;
                Response.BinaryWrite(System.Text.Encoding.UTF8.GetPreamble());
                var sw = new StringWriter();
                var htw = new HtmlTextWriter(sw);
                gv.RenderControl(htw);
                return File(System.Text.Encoding.UTF8.GetBytes(sw.ToString()), Response.ContentType, "Export_YeterlikBasvuruListesi_" + DateTime.Now.ToFormatDate() + ".xls");
            }
            #endregion
            var isFiltered = !Equals(q, q2);
            model.RowCount = q.Count();
            q = !model.Sort.IsNullOrWhiteSpace() ? q.OrderBy(model.Sort) : q.OrderByDescending(o => o.BasvuruTarihi);
            model.Data = q.Skip(model.StartRowIndex).Take(model.PageSize).ToList();

            ViewBag.kontrolEdilmeyenBasvuruIds = isFiltered ? q.Where(p => !p.IsEnstituOnaylandi.HasValue).Select(s => s.YeterlikBasvuruID).ToList() : new List<int>();
            ViewBag.filteredOgrenciIds = isFiltered ? q.Select(s => s.KullaniciID).Distinct().ToList() : new List<int>();
            ViewBag.filteredDanismanIds = isFiltered ? q.Select(s => s.TezDanismanID).Distinct().Distinct().ToList() : new List<int>();
            ViewBag.onayYapmayanKomiteIds = isFiltered ? q.SelectMany(s => s.OnayYapmayanKomiteIds).Distinct().ToList() : new List<int>();
            ViewBag.onayYapmayanJuriEmails = isFiltered ? q.SelectMany(s => s.OnayYapmayanJuriEmails).Distinct().ToList() : new List<string>();


            ViewBag.IsDanismaniOlunanOgrenciler = new SelectList(YeterlikBus.GetCmbDanismanlikDurum(true), "Value", "Caption", model.YeterlikSurecID);
            ViewBag.YeterlikSurecID = new SelectList(YeterlikBus.GetCmbYeterlikSurecleri(enstituKod, true), "Value", "Caption", model.YeterlikSurecID);
            ViewBag.AnabilimDaliID = new SelectList(YeterlikBus.GetCmbFilterYeterlikAnabilimDallari(enstituKod, model.YeterlikSurecID, true), "Value", "Caption", model.AnabilimDaliID);
            ViewBag.BasvuruDurumID = new SelectList(YeterlikBus.GetCmbBasvuruDurumu(true), "Value", "Caption", model.BasvuruDurumID);
            ViewBag.OgrenimTipID = new SelectList(OgrenimTipleriBus.CmbAktifOgrenimTipIdDoktora(enstituKod, true), "Value", "Caption", model.OgrenimTipID);

            return View(model);
        }

        public string ToDurumString(bool isTezOnerisiVar, bool isOyBirligiOrCoklugu, bool isSrTalebiYapildi, bool degerlendirmeBasladi, int? toBasvuruSavunmaDurumId)
        {
            if (!isTezOnerisiVar)
                return "Tez Öneri Savunma Formu Oluşturulmadı.";
            if (!isSrTalebiYapildi)
                return "Tez Öneri Savunma Formu Oluşturuldu.";
            if (!degerlendirmeBasladi)
                return "Toplantı Bilgileri Girildi";
            if (!toBasvuruSavunmaDurumId.HasValue)
                return "Değerlendirme Süreci Başladı";

            var durumAdi = "Değerlendirme Süreci Tamamlandı\r\n";
            durumAdi += (isOyBirligiOrCoklugu ? "Oy Birliği İle" : "Oy Çokluğu İle");
            switch (toBasvuruSavunmaDurumId)
            {
                case ToBasvuruSavunmaDurumuEnum.KabulEdildi:
                    durumAdi += " Kabul Edildi";
                    break;
                case ToBasvuruSavunmaDurumuEnum.Reddedildi:
                    durumAdi += " Reddedildi";
                    break;
                case ToBasvuruSavunmaDurumuEnum.Duzeltme:
                    durumAdi += " Düzeltme Talep Edildi";
                    break;
            }
            return durumAdi;
        }
        [Authorize(Roles = RoleNames.YeterlikBasvuruOnayYetkisi)]
        public ActionResult EnstituOnay(Guid uniqueId, bool? enstituOnay, string enstituOnayAciklama)
        {
            var mmMessage = new MmMessage
            {
                Title = "Enstitu Başvuru Onay İşlemi",
                MessageType = MsgTypeEnum.Warning

            };
            if (enstituOnay == false && enstituOnayAciklama.IsNullOrWhiteSpace())
            {
                mmMessage.Messages.Add("İptal işlemi için İptal Açıklaması giriniz.");
            }

            if (!mmMessage.Messages.Any())
            {
                var basvuru = _entities.YeterlikBasvurus.First(p => p.UniqueID == uniqueId);
                var sendMail = enstituOnay.HasValue && basvuru.IsEnstituOnaylandi != enstituOnay;
                basvuru.IsEnstituOnaylandi = enstituOnay;
                basvuru.EnstituOnayAciklama = enstituOnayAciklama;
                basvuru.EnstituOnayTarihi = DateTime.Now;
                _entities.SaveChanges();
                mmMessage.IsSuccess = true;
                mmMessage.MessageType = MsgTypeEnum.Success;
                LogIslemleri.LogEkle("YeterlikBasvuru", LogCrudType.Update, basvuru.ToJson());
                if (sendMail)
                    YeterlikBus.SendMailBasvuruOnayi(basvuru.UniqueID);
            }
            var strView = ViewRenderHelper.RenderPartialView("Ajax", "GetMessage", mmMessage);
            return Json(new { mmMessage.IsSuccess, Messages = strView }, "application/json", JsonRequestBehavior.AllowGet);

        }

        [Authorize(Roles = RoleNames.YeterlikBasvuruOnayYetkisi)]
        public ActionResult EnstituTopluOnay(List<int> kontrolEdilmeyenTalepIds)
        {
            var success = true;
            string message;

            if (UserIdentity.Current.IsAdmin)
            {
                try
                {
                    var basvurus = _entities.YeterlikBasvurus.Where(p => !p.IsEnstituOnaylandi.HasValue && kontrolEdilmeyenTalepIds.Contains(p.YeterlikBasvuruID)).ToList();

                    var uniqueIds = new List<Guid>();
                    foreach (var item in basvurus)
                    {
                        uniqueIds.Add(item.UniqueID);
                        item.IsEnstituOnaylandi = true;
                        item.EnstituOnayTarihi = DateTime.Now;
                        item.IslemTarihi = DateTime.Now;
                        item.IslemYapanID = UserIdentity.Current.Id;
                        item.IslemYapanIP = UserIdentity.Ip;
                    }
                    _entities.SaveChanges();
                    foreach (var uniqueId in uniqueIds)
                    {
                        YeterlikBus.SendMailBasvuruOnayi(uniqueId);
                    }
                    message = basvurus.Count + " Yeterlik başvurusu onaylandı";
                    LogIslemleri.LogEkle("YeterlikBasvuru", LogCrudType.Update, basvurus.ToJson());

                }
                catch (Exception ex)
                {
                    success = false;
                    message = "Toplu Yeterlik başvuruları Onay işlemi yapılırken bir hata oluştu!";
                    SistemBilgilendirmeBus.SistemBilgisiKaydet("Toplu Yeterlik başvuruları Onay işlemi yapılırken bir hata oluştu! <br/><br/> Hata: " + ex.ToExceptionMessage(), ex.ToExceptionStackTrace(), BilgiTipiEnum.Hata);
                }
            }
            else
            {
                success = false;
                message = "Bu işlemi yapmaya yetkili değilsiniz.";
            }

            return new { success, message }.ToJsonResult();
        }
    }
}