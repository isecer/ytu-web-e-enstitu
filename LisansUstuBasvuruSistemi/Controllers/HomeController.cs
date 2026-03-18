using BiskaUtil;
using Entities.Entities;
using LisansUstuBasvuruSistemi.Business;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.SystemSetting;
using System;
using System.Linq;
using System.Web.Mvc;

namespace LisansUstuBasvuruSistemi.Controllers
{
    [OutputCache(Duration = 0, VaryByParam = "*")]
    public class HomeController : Controller
    {
        private readonly LubsDbEntities _entities = new LubsDbEntities();



        public ActionResult Index(string ekd, string mesajGroupId, int? basvuruId, string rowId, bool isMesajGonder = false)
        {

            var enstitu = _entities.Enstitulers.First(p => p.EnstituKisaAd.Contains(ekd));
            ViewBag.Konum = enstitu.Konum;

            #region duyurular 
            var q = from s in _entities.Duyurulars
                    join e in _entities.Enstitulers on new { s.EnstituKod } equals new { e.EnstituKod }
                    join k in _entities.Kullanicilars on s.IslemYapanID equals k.KullaniciID
                    where s.IsAktif && s.Tarih <= DateTime.Now && (!s.YayinSonTarih.HasValue || s.YayinSonTarih.Value >= DateTime.Now) && e.EnstituKisaAd.Contains(ekd) && s.AnaSayfadaGozuksun
                    select new
                    {
                        s.EnstituKod,
                        e.EnstituAd,
                        s.DuyuruID,
                        s.Tarih,
                        s.Baslik,
                        s.Aciklama,
                        s.AciklamaHtml,
                        DuyuruYapan = k.Ad + " " + k.Soyad,
                        s.IslemYapanIP,
                        EkSayisi = s.DuyuruEkleris.Count,
                        Ekler = s.DuyuruEkleris,
                        s.AnaSayfadaGozuksun,
                        AnaSayfaPopupAc = s.DuyuruPopuplars.Any(a => a.DuyuruPopupTipID == DuyuruPopupTipiEnum.AnaSayfa),
                        s.YayinSonTarih,
                        s.IsEnUsteSabitle,
                        s.IsAktif
                    };

            var data = q.Select(s => new FrDuyurularDto
            {
                EnstituAdi = s.EnstituAd,
                EnstituKod = s.EnstituKod,
                DuyuruID = s.DuyuruID,
                Baslik = s.Baslik,
                Aciklama = s.Aciklama,
                AciklamaHtml = s.AciklamaHtml,
                Tarih = s.Tarih,
                DuyuruYapan = s.DuyuruYapan,
                IslemYapanIP = s.IslemYapanIP,
                EkSayisi = s.EkSayisi,
                DuyuruEkleris = s.Ekler,
                YayinSonTarih = s.YayinSonTarih,
                IsEnUsteSabitle = s.IsEnUsteSabitle,
            }).OrderBy(o => o.IsEnUsteSabitle ? 1 : 2).ThenByDescending(o => o.Tarih).ToList();
            ViewBag.Duyurular = data;
            #endregion

            if (mesajGroupId.IsNullOrWhiteSpace() == false)
            {
                var secilenMesaj = _entities.Mesajlars.FirstOrDefault(p => p.GroupID == mesajGroupId);
                ViewBag.MesajGroupID = secilenMesaj != null ? mesajGroupId : "";
            }
            else ViewBag.MesajGroupID = "";

            if (basvuruId.HasValue && rowId.IsNullOrWhiteSpace() == false)
            {
                var nRwId = new Guid(rowId);
                var basvuru = _entities.Basvurulars
                    .FirstOrDefault(p => p.BasvuruID == basvuruId.Value && p.RowID == nRwId);

                if (basvuru?.BasvuruSurec.KayitOlmayanlarAnketID != null &&
                    basvuru.AnketCevaplaris.All(p => p.AnketID != basvuru.BasvuruSurec.KayitOlmayanlarAnketID))
                {
                    var anketId = basvuru.BasvuruSurec.KayitOlmayanlarAnketID.Value;
                     
                    var anketViewHtml = AnketlerBus.GetAnketView(
                        anketId: anketId,
                        anketTipId: AnketTipiEnum.KayitHakkiKazananKayitYaptirmayanAnketi,
                        basvuruId: basvuruId,
                        rowId: rowId
                    );

                    // ViewBag'e setle
                    ViewBag.AnketGiris = anketViewHtml;
                }
            }

            ViewBag.IsMesajGonder = isMesajGonder;

            // new ObsServiceData().GetAllStudent();
            #region DavetGaleriOlustur

            if (MezuniyetAyar.TezSinaviDavetKartlariniAnaSayfadaGoster
                .GetAyar(enstitu.EnstituKod).ToBoolean(false))
            {
                var take = MezuniyetAyar.TezSinaviDavetListesindeGosterilecekKisiSayisi
                    .GetAyar(enstitu.EnstituKod, "20").ToInt().Value;

                var now = DateTime.Now;

                // DB'den sadece gerekli alanları çek
                var rawData = _entities.SRTalepleris
                    .Where(p => p.EnstituKod == enstitu.EnstituKod
                                && p.MezuniyetBasvurulariID.HasValue
                                && p.MezuniyetBasvurulari.MezuniyetYayinKontrolDurumID == MezuniyetYayinKontrolDurumuEnum.KabulEdildi
                                && p.SRDurumID == SrTalepDurumEnum.Onaylandı
                                && !string.IsNullOrEmpty(p.DavetResimYolu))
                    .Select(p => new
                    {
                        p.DavetResimYolu,
                        p.IslemTarihi,
                        p.Tarih,
                        p.BasSaat
                    })
                    .ToList();

                // Bellekte tam zamanı oluştur
                var withDate = rawData
                    .Select(p => new
                    {
                        p.DavetResimYolu,
                        p.IslemTarihi,
                        FullDateTime = p.Tarih.Add(p.BasSaat)
                    })
                    .ToList();

                // 1) Bugün/gelecek olanları al (en yakın gelecekten başlayarak)
                var future = withDate
                    .Where(x => x.FullDateTime >= now)
                    .OrderBy(x => x.FullDateTime) // küçükten büyüğe => en yakın gelecek önce
                    .Take(take)
                    .ToList();

                // 2) Eğer eksik varsa geçmişten tamamla (günümüze en yakın olan önce)
                if (future.Count < take)
                {
                    var remaining = take - future.Count;

                    var past = withDate
                        .Where(x => x.FullDateTime < now)
                        .OrderByDescending(x => x.FullDateTime)  
                        .Take(remaining)
                        .ToList();

                    future.AddRange(past.OrderBy(o=>o.FullDateTime).ToList());
                }

                // 3) Sonuç: future içindeki sıralama zaten doğru (gelecek: en yakın->uzak ; ardından geçmiş: en yakın->uzak)
                //ViewBag.GaleryUrls = future.Select(x => x.DavetResimYolu).ToList();
                ViewBag.GalleryItems = future.Select(x => new GaleryItem
                {
                    Url = x.DavetResimYolu,
                    Version = x.IslemTarihi.Ticks
                }).ToList();
            }
            #endregion

            return View(enstitu);
        }

        public ActionResult AuthenticatedControl()
        {
            if (Request.Browser.IsMobileDevice) { }
            return Json(UserIdentity.Current.IsAuthenticated, "application/json", JsonRequestBehavior.AllowGet);
        }




    }
}