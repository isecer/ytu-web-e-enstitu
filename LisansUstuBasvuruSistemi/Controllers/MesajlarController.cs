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
using LisansUstuBasvuruSistemi.Utilities.MenuAndRoles;
using LisansUstuBasvuruSistemi.Utilities.SystemData;
using System.Data.Entity;
using System.Threading.Tasks;

namespace LisansUstuBasvuruSistemi.Controllers
{
    [OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    [Authorize(Roles = RoleNames.Mesajlar)]
    public class MesajlarController : Controller
    {
        private readonly LubsDbEntities _entities = new LubsDbEntities();

        public async Task<ActionResult> Index(string ekd)
        {
            var enstituKod = EnstituBus.GetSelectedEnstitu(ekd);
            return await Index(new FmMesajlarDto() { PageSize = 10, Expand = true, EnstituKod = enstituKod });
        }

        [HttpPost]
        public async Task<ActionResult> Index(FmMesajlarDto model, bool export = false)
        {
            var filteredMesajsQuery = _entities.Mesajlars.Where(p =>
                UserIdentity.Current.EnstituKods.Contains(p.EnstituKod) && !p.UstMesajID.HasValue);
            if (!model.EnstituKod.IsNullOrWhiteSpace())
                filteredMesajsQuery = filteredMesajsQuery.Where(p => p.EnstituKod == model.EnstituKod);
            if (model.MesajKategoriID.HasValue)
                filteredMesajsQuery = filteredMesajsQuery.Where(p => p.MesajKategoriID == model.MesajKategoriID.Value);
            if (!model.Konu.IsNullOrWhiteSpace())
                filteredMesajsQuery = filteredMesajsQuery.Where(p => p.Konu.Contains(model.Konu));
            if (model.IsAktif.HasValue)
                filteredMesajsQuery = filteredMesajsQuery.Where(p => p.IsAktif == model.IsAktif);
            if (model.IsDosyaEkDurum.HasValue)
                filteredMesajsQuery =
                    filteredMesajsQuery.Where(p => p.MesajEkleris.Any() == model.IsDosyaEkDurum.Value);
            if (model.Tarih.HasValue)
            {
                var trih = model.Tarih.Value.TodateToShortDate();
                filteredMesajsQuery = filteredMesajsQuery.Where(p => p.Tarih == trih);

            }

            if (model.MesajYili.HasValue)
                filteredMesajsQuery = filteredMesajsQuery.Where(p => p.Tarih.Year == model.MesajYili);
            if (!model.Konu.IsNullOrWhiteSpace())
                filteredMesajsQuery = filteredMesajsQuery.Where(p => p.Aciklama.Contains(model.Konu));
            if (model.BaslangicTarihi.HasValue || model.BitisTarihi.HasValue)
            {
                if (model.BaslangicTarihi.HasValue && model.BitisTarihi.HasValue)
                {
                    filteredMesajsQuery = filteredMesajsQuery
                        .Where(p => DbFunctions.TruncateTime(p.SonMesajTarihi) >= DbFunctions.TruncateTime(model.BaslangicTarihi.Value) &&
                                    DbFunctions.TruncateTime(p.SonMesajTarihi) <= DbFunctions.TruncateTime(model.BitisTarihi.Value));
                }
                else if (model.BaslangicTarihi.HasValue)
                {
                    filteredMesajsQuery = filteredMesajsQuery
                        .Where(p => DbFunctions.TruncateTime(p.SonMesajTarihi) >= DbFunctions.TruncateTime(model.BaslangicTarihi.Value));
                }
                else if (model.BitisTarihi.HasValue)
                {
                    filteredMesajsQuery = filteredMesajsQuery
                        .Where(p => DbFunctions.TruncateTime(p.SonMesajTarihi) <= DbFunctions.TruncateTime(model.BitisTarihi.Value));
                }
            }

            var q = from s in filteredMesajsQuery
                    join ens in _entities.Enstitulers on new { s.EnstituKod } equals new { ens.EnstituKod }
                    join mk in _entities.MesajKategorileris on s.MesajKategoriID equals mk.MesajKategoriID
                    join k in _entities.Kullanicilars on s.KullaniciID equals k.KullaniciID into defK
                    from kul in defK.DefaultIfEmpty()
                    where s.Silindi == false
                    select new
                    {
                        s.EnstituKod,
                        ens.EnstituKisaAd,
                        ens.EnstituAd,
                        s.MesajKategoriID,
                        mk.KategoriAdi,
                        s.MesajID,
                        s.UstMesajID,
                        Tarih = s.SonMesajTarihi,
                        s.Konu,
                        Email = kul != null ? kul.EMail : s.Email,
                        s.AdSoyad,
                        ResimAdi = kul != null ? kul.ResimAdi : null,
                        s.IslemYapanIP,
                        EkSayisi = s.ToplamEkSayisi,
                        s.IsAktif,
                        s.KullaniciID,
                        UserKey = kul != null ? kul.UserKey : (Guid?)null,
                    };

            if (!model.AdSoyad.IsNullOrWhiteSpace())
                q = q.Where(p => p.AdSoyad.Contains(model.AdSoyad) || p.Email.Contains(model.AdSoyad));


            model.RowCount = q.Count();
            var indexModel = new MIndexBilgi
            {
                Toplam = model.RowCount,
                Aktif = model.IsAktif == true ? model.RowCount : q.Count(p => p.IsAktif)
            };
            indexModel.Pasif = model.IsAktif == false ? model.RowCount : (indexModel.Toplam - indexModel.Aktif);
            q = !model.Sort.IsNullOrWhiteSpace() ? q.OrderBy(model.Sort) : q.OrderByDescending(o => o.Tarih);


            if (export && model.RowCount > 0)
            {
                var mesajIDs = q.Select(s => s.MesajID).ToList();
                GridView gv = new GridView();


                var dataResult = await GetMesajDetailsAsync(mesajIDs);
                var data = dataResult.Select(s => new
                {
                    s.GrupNo,
                    GelenGiden = s.GidenGelen,
                    s.KategoriAdi,
                    s.AdSoyad,
                    s.Konu,
                    Mesaj = s.Aciklama,
                    s.Tarih,

                }).ToList();
                gv.DataSource = data;
                gv.DataBind();


                Response.ContentType = "application/ms-excel";
                Response.ContentEncoding = System.Text.Encoding.UTF8;
                Response.BinaryWrite(System.Text.Encoding.UTF8.GetPreamble());
                StringWriter sw = new StringWriter();
                HtmlTextWriter htw = new HtmlTextWriter(sw);
                gv.RenderControl(htw);

                return File(System.Text.Encoding.UTF8.GetBytes(sw.ToString()), Response.ContentType,
                    "Export_GelenMesajListesi_" + DateTime.Now.ToFormatDate() + ".xls");


            }

            model.MesajlarDtos = q.Skip(model.StartRowIndex).Take(model.PageSize).Select(s => new FrMesajlarDto
            {
                EnstituAdi = s.EnstituAd,
                EnstituKisaAd = s.EnstituKisaAd,
                EnstituKod = s.EnstituKod,
                MesajKategoriID = s.MesajKategoriID,
                KategoriAdi = s.KategoriAdi,
                MesajID = s.MesajID,
                Konu = s.Konu,
                Email = s.Email,
                Tarih = s.Tarih,
                AdSoyad = s.AdSoyad,
                ResimAdi = s.ResimAdi,
                EkSayisi = s.EkSayisi,
                KullaniciID = s.KullaniciID ?? 0,
                UserKey = s.UserKey,
                IslemYapanIP = s.IslemYapanIP,
                IsAktif = s.IsAktif
            }).ToList();
            ViewBag.EnstituKod = new SelectList(EnstituBus.GetCmbYetkiliEnstituler(true), "Value", "Caption",
                model.EnstituKod);
            ViewBag.MesajKategoriID = new SelectList(MesajlarBus.CmbGetMesajKategorileri(model.EnstituKod, true),
                "Value", "Caption", model.MesajKategoriID);
            ViewBag.IndexModel = indexModel;
            ViewBag.IsAktif = new SelectList(ComboData.GetCmbAcikKapaliData(true), "Value", "Caption", model.IsAktif);
            ViewBag.IsDosyaEkDurum = new SelectList(ComboData.GetCmbDosyaEkiDurumData(true), "Value", "Caption",
                model.IsDosyaEkDurum);
            ViewBag.MesajYili = new SelectList(MesajlarBus.CmbGetMesajYillari(model.EnstituKod, true), "Value",
                "Caption", model.MesajYili);

            return View(model);
        }

        public ActionResult GetAcikMsjCount(string enstituKod)
        {
            var model = MesajlarBus.GetCevaplanmamisMesajCount(enstituKod);
            return new { mCount = model.Value.Value, HtmlContent = model.Caption }.ToJsonResult();
        }

        public ActionResult DurumKayit(int id, bool isAktif, bool? mainFilter)
        {
            var kayit = _entities.Mesajlars.FirstOrDefault(p => p.MesajID == id);
            string message;
            var success = true;
            try
            {
                message = "'" + kayit.Konu + "' Konulu Mesaj durumu " + (!isAktif ? "Açık" : "Kapalı") +
                          " olarak İşaretlendi";
                kayit.IsAktif = isAktif;
                _entities.SaveChanges();
            }
            catch (Exception ex)
            {
                success = false;
                message = "'" + kayit.Konu + "'Konulu Mesaj Durumu Güncellenemedi! <br/> Bilgi:" +
                          ex.ToExceptionMessage();
                SistemBilgilendirmeBus.SistemBilgisiKaydet(message, ex.ToExceptionStackTrace(),
                    BilgiTipiEnum.OnemsizHata);
            }

            return Json(new { success, message }, "application/json", JsonRequestBehavior.AllowGet);
        }

        public ActionResult Sil(int id)
        {
            var mSilYetki = RoleNames.MesajlarSil.InRoleCurrent();
            string message;
            var success = true;

            if (mSilYetki)
            {
                var kayit = _entities.Mesajlars.FirstOrDefault(p => p.MesajID == id);
                if (kayit != null)
                {
                    try
                    {
                        message = "'" + kayit.Konu + "' Konulu Mesaj Silindi!";
                        kayit.Silindi = true;
                        var mails = _entities.GonderilenMaillers.Where(p => p.MesajID == kayit.MesajID).ToList();
                        foreach (var item in mails)
                        {
                            item.Silindi = true;
                        }

                        _entities.SaveChanges();

                    }
                    catch (Exception ex)
                    {
                        success = false;
                        message = "'" + kayit.Konu + "' Başlıklı Konu! <br/> Bilgi:" + ex.ToExceptionMessage();
                        SistemBilgilendirmeBus.SistemBilgisiKaydet(message, ex.ToExceptionStackTrace(),
                            BilgiTipiEnum.OnemsizHata);
                    }
                }
                else
                {
                    success = false;
                    message = "Silmek istediğiniz Mesaj sistemde bulunamadı!";
                }
            }
            else
            {
                success = false;
                message = "Mesaj silmeye yetkiniz bulunmuyor!";
            }

            return Json(new { success, message }, "application/json", JsonRequestBehavior.AllowGet);
        }


        public ActionResult GetMesajDetay(int mesajId)
        {
            var mesaj = _entities.Mesajlars.Where(p => p.UstMesajID.HasValue == false && p.MesajID == mesajId).Select(
                s => new FrMesajlarDto
                {
                    MesajKategoriID = s.MesajKategoriID,
                    MesajID = s.MesajID,
                    Konu = s.Konu,
                    KullaniciID = s.KullaniciID,
                    UserKey = s.KullaniciID.HasValue ? s.Kullanicilar.UserKey : (Guid?)null,
                    Email = s.KullaniciID.HasValue ? s.Kullanicilar.EMail : s.Email,
                    Aciklama = s.Aciklama,
                    AciklamaHtml = s.AciklamaHtml,
                    Tarih = s.Tarih,
                    AdSoyad = s.AdSoyad,
                    ResimAdi = s.KullaniciID.HasValue ? s.Kullanicilar.ResimAdi : null,
                    IslemYapanIP = s.IslemYapanIP,
                    IsAktif = s.IsAktif,
                    MesajEkleris = s.MesajEkleris.ToList()
                }).First();

            var groupMesajs = _entities.Mesajlars.Where(p => p.UstMesajID == mesaj.MesajID).ToList().Select(s =>
                new SubMessagesDto
                {
                    MesajID = s.MesajID,
                    KullaniciID = s.KullaniciID ?? 0,
                    UserKey = s.KullaniciID.HasValue ? s.Kullanicilar.UserKey : (Guid?)null,
                    EMail = s.KullaniciID.HasValue ? s.Kullanicilar.EMail : s.Email,
                    AdSoyad = s.AdSoyad,
                    Tarih = s.Tarih,
                    Icerik = s.AciklamaHtml,
                    ResimYolu = s.KullaniciID.HasValue ? s.Kullanicilar.ResimAdi : "",
                    IslemYapanIP = s.IslemYapanIP,
                    GonderilenMailKullanicilars = new List<GonderilenMailKullanicilar>(),
                    MesajEkleris = s.MesajEkleris.ToList()


                }).ToList();
            groupMesajs.Add(new SubMessagesDto
            {
                MesajID = mesaj.MesajID,
                KullaniciID = mesaj.KullaniciID ?? 0,
                UserKey = mesaj.UserKey,
                EMail = mesaj.Email,
                AdSoyad = mesaj.AdSoyad,
                Tarih = mesaj.Tarih,
                Icerik = mesaj.Aciklama,
                ResimYolu = mesaj.ResimAdi,
                IslemYapanIP = mesaj.IslemYapanIP,
                GonderilenMailKullanicilars = new List<GonderilenMailKullanicilar>(),
                MesajEkleris = mesaj.MesajEkleris
                    .Select(s => new MesajEkleri { EkAdi = s.EkAdi, EkDosyaYolu = s.EkDosyaYolu }).ToList(),
            });
            var gMesajs = _entities.GonderilenMaillers.Where(p => p.MesajID == mesaj.MesajID).ToList();
            foreach (var item in gMesajs)
            {
                var kul = item.Kullanicilar;
                groupMesajs.Add(new SubMessagesDto
                {
                    MesajID = item.MesajID.Value,
                    UserKey = kul.UserKey,
                    KullaniciID = kul.KullaniciID,
                    EMail = kul.EMail,
                    AdSoyad = kul.Ad + " " + kul.Soyad,
                    Tarih = item.Tarih,
                    MesajEkleris = item.GonderilenMailEkleris.Select(s2 => new MesajEkleri
                    { MesajEkiID = 1, EkAdi = s2.EkAdi, EkDosyaYolu = s2.EkDosyaYolu }).ToList(),
                    Icerik = item.AciklamaHtml,
                    ResimYolu = kul.ResimAdi,
                    GonderilenMailKullanicilars = item.GonderilenMailKullanicilars.ToList(),
                    IslemYapanIP = item.IslemYapanIP,

                });
            }

            mesaj.SubMesajList = groupMesajs.OrderByDescending(o => o.Tarih).ToList();
            return View(mesaj);
        }

        public List<FrMesajlarDto> GetMesajDetails(List<int> mesajId)
        {
            var mesajList = new List<FrMesajlarDto>();
            var mesajLar =
                (from mesaj in _entities.Mesajlars.Where(p =>
                        p.UstMesajID.HasValue == false && mesajId.Contains(p.MesajID))
                 join mesajKategorisi in _entities.MesajKategorileris on new { mesaj.MesajKategoriID } equals new
                 { mesajKategorisi.MesajKategoriID }
                 join kullanici in _entities.Kullanicilars on mesaj.KullaniciID equals kullanici.KullaniciID into
                     defk
                 from kullaniciDef in defk.DefaultIfEmpty()
                 join kullaniciTipi in _entities.KullaniciTipleris on kullaniciDef.KullaniciTipID equals
                     kullaniciTipi.KullaniciTipID into defkt
                 from kullaniciTipiDef in defkt.DefaultIfEmpty()
                 join donem in _entities.Donemlers on kullaniciDef.KayitDonemID equals donem.DonemID into defkd
                 from donemDef in defkd.DefaultIfEmpty()
                 join ogrenimTipi in _entities.OgrenimTipleris on kullaniciDef.OgrenimTipKod equals ogrenimTipi
                     .OgrenimTipKod into defot
                 from ogrenimTipiDef in defot.DefaultIfEmpty()
                 join program in _entities.Programlars on kullaniciDef.ProgramKod equals program.ProgramKod into
                     defpr
                 from programDef in defpr.DefaultIfEmpty()
                 join anabilimDali in _entities.AnabilimDallaris on programDef.AnabilimDaliKod equals anabilimDali
                     .AnabilimDaliKod into defab
                 from anabilimDaliDef in defab.DefaultIfEmpty()
                 select new FrMesajlarDto
                 {
                     GidenGelen = "Gelen Mesaj",
                     MesajKategoriID = mesaj.MesajKategoriID,
                     KategoriAdi = mesajKategorisi.KategoriAdi,
                     MesajID = mesaj.MesajID,
                     Konu = mesaj.Konu,
                     Aciklama = mesaj.Aciklama,
                     Tarih = mesaj.Tarih,
                     KullaniciTipAdi = kullaniciTipiDef != null ? kullaniciTipiDef.KullaniciTipAdi : "",
                     AdSoyad = mesaj.AdSoyad,
                     OgrenciNo = kullaniciDef.OgrenciNo,
                     KayitDonemAdi = kullaniciDef.KayitYilBaslangic.HasValue
                         ? kullaniciDef.KayitYilBaslangic + "/" + (kullaniciDef.KayitYilBaslangic + 1) + " " +
                           donemDef.DonemAdi
                         : "",
                     KayitTarihi = kullaniciDef.KayitTarihi,
                     OgrenimTipAdi = ogrenimTipiDef != null ? ogrenimTipiDef.OgrenimTipAdi : "",
                     AnabilimdaliAdi = anabilimDaliDef != null ? anabilimDaliDef.AnabilimDaliAdi : "",
                     ProgramAdi = programDef != null ? programDef.ProgramAdi : ""
                 }).ToList();
            var altMesaj = (from mesaj in _entities.Mesajlars.Where(p => mesajId.Contains(p.UstMesajID ?? 0))
                            join kullanici in _entities.Kullanicilars on mesaj.KullaniciID equals kullanici.KullaniciID into defk
                            from kullaniciDef in defk.DefaultIfEmpty()
                            join kullaniciTipi in _entities.KullaniciTipleris on kullaniciDef.KullaniciTipID equals kullaniciTipi
                                .KullaniciTipID into defkt
                            from kullaniciTipiDef in defkt.DefaultIfEmpty()
                            join donem in _entities.Donemlers on kullaniciDef.KayitDonemID equals donem.DonemID into defkd
                            from donemDef in defkd.DefaultIfEmpty()
                            join ogrenimTipi in _entities.OgrenimTipleris on kullaniciDef.OgrenimTipKod equals ogrenimTipi
                                .OgrenimTipKod into defot
                            from ogrenimTipiDef in defot.DefaultIfEmpty()
                            join program in _entities.Programlars on kullaniciDef.ProgramKod equals program.ProgramKod into defpr
                            from programDef in defpr.DefaultIfEmpty()
                            join anabilimDali in _entities.AnabilimDallaris on programDef.AnabilimDaliKod equals anabilimDali
                                .AnabilimDaliKod into defab
                            from anabilimDaliDef in defab.DefaultIfEmpty()
                            select new FrMesajlarDto
                            {
                                GidenGelen = "Gelen Mesaj",
                                MesajID = mesaj.MesajID,
                                Konu = mesaj.Konu,
                                Aciklama = mesaj.Aciklama,
                                Tarih = mesaj.Tarih,
                                KullaniciTipAdi = kullaniciTipiDef != null ? kullaniciTipiDef.KullaniciTipAdi : "",
                                AdSoyad = mesaj.AdSoyad,
                                OgrenciNo = kullaniciDef.OgrenciNo,
                                KayitDonemAdi = kullaniciDef.KayitYilBaslangic.HasValue
                                    ? kullaniciDef.KayitYilBaslangic + "/" + (kullaniciDef.KayitYilBaslangic + 1) + " " +
                                      donemDef.DonemAdi
                                    : "",
                                KayitTarihi = kullaniciDef.KayitTarihi,
                                OgrenimTipAdi = ogrenimTipiDef != null ? ogrenimTipiDef.OgrenimTipAdi : "",
                                AnabilimdaliAdi = anabilimDaliDef != null ? anabilimDaliDef.AnabilimDaliAdi : "",
                                ProgramAdi = programDef != null ? programDef.ProgramAdi : ""
                            }).ToList();

            altMesaj.AddRange(_entities.GonderilenMaillers.Where(p => mesajId.Contains(p.MesajID ?? 0)).Select(s =>
                new FrMesajlarDto
                {
                    GidenGelen = "Giden Mail",
                    MesajID = s.MesajID.Value,
                    Tarih = s.Tarih,
                    AdSoyad = s.Kullanicilar.Ad + " " + s.Kullanicilar.Soyad,
                    Aciklama = s.Aciklama,

                }).ToList());
            int inx = 1;
            foreach (var item in mesajLar)
            {
                item.GrupNo = inx;
                mesajList.Add(item);
                var secilenler = altMesaj.Where(p => p.MesajID == item.MesajID || p.UstMesajID == item.MesajID)
                    .ToList();
                foreach (var item2 in secilenler.OrderByDescending(o => o.Tarih))
                {
                    mesajList.Add(new FrMesajlarDto
                    {
                        GidenGelen = item2.GidenGelen,
                        GrupNo = item.GrupNo,
                        KategoriAdi = item.KategoriAdi,
                        Konu = item.Konu,
                        Email = item2.Email,
                        Aciklama = item2.Aciklama,
                        Tarih = item2.Tarih,
                        KullaniciTipAdi = item2.KullaniciTipAdi,
                        AdSoyad = item2.AdSoyad,
                        OgrenciNo = item2.OgrenciNo,
                        KayitDonemAdi = item2.KayitDonemAdi,
                        KayitTarihi = item2.KayitTarihi,
                        OgrenimTipAdi = item2.OgrenimTipAdi,
                        AnabilimdaliAdi = item2.AnabilimdaliAdi,
                        ProgramAdi = item2.ProgramAdi

                    });
                }

                inx++;

            }

            return mesajList;

        }

        public async Task<List<FrMesajlarDto>> GetMesajDetailsAsync(List<int> mesajId)
        {
            var mesajList = new List<FrMesajlarDto>();

            var qmesajLarTask = Task.Run(async () =>
            {
                using (var db = new LubsDbEntities())
                {
                    return await (from mesaj in db.Mesajlars.AsNoTracking()
                                  .Where(p => !p.UstMesajID.HasValue && mesajId.Contains(p.MesajID))
                                  join mesajKategorisi in db.MesajKategorileris.AsNoTracking()
                                      on mesaj.MesajKategoriID equals mesajKategorisi.MesajKategoriID
                                  select new FrMesajlarDto
                                  {
                                      GidenGelen = "Gelen Mesaj",
                                      MesajKategoriID = mesaj.MesajKategoriID,
                                      KategoriAdi = mesajKategorisi.KategoriAdi,
                                      MesajID = mesaj.MesajID,
                                      Konu = mesaj.Konu,
                                      Aciklama = mesaj.Aciklama,
                                      Tarih = mesaj.Tarih,
                                      AdSoyad = mesaj.AdSoyad,
                                      UstMesajID = mesaj.UstMesajID
                                  }).ToListAsync();
                }
            });

            var qaltMesajTask = Task.Run(async () =>
            {
                using (var db = new LubsDbEntities())
                {
                    return await (from mesaj in db.Mesajlars.AsNoTracking()
                                  .Where(p => p.UstMesajID.HasValue && mesajId.Contains(p.UstMesajID.Value))
                                  select new FrMesajlarDto
                                  {
                                      GidenGelen = "Gelen Mesaj",
                                      MesajID = mesaj.MesajID,
                                      UstMesajID = mesaj.UstMesajID,
                                      Konu = mesaj.Konu,
                                      Aciklama = mesaj.Aciklama,
                                      Tarih = mesaj.Tarih,
                                      AdSoyad = mesaj.AdSoyad,
                                  }).ToListAsync();
                }
            });

            var gidenMaillerTask = Task.Run(async () =>
            {
                using (var db = new LubsDbEntities())
                {
                    return await (from gm in db.GonderilenMaillers.AsNoTracking()
                                  .Where(p => p.MesajID.HasValue && mesajId.Contains(p.MesajID.Value))
                                  join kullanici in db.Kullanicilars.AsNoTracking() on gm.IslemYapanID equals kullanici.KullaniciID
                                  select new FrMesajlarDto
                                  {
                                      GidenGelen = "Giden Mail",
                                      MesajID = gm.MesajID.Value,
                                      UstMesajID = gm.MesajID,
                                      Tarih = gm.Tarih,
                                      AdSoyad = kullanici.Ad + " " + kullanici.Soyad,
                                      Aciklama = gm.Aciklama,
                                  }).ToListAsync();
                }
            });

           
            var mesajLar = await qmesajLarTask;
            var altMesaj = await qaltMesajTask;
            var gidenMaillerResult = await gidenMaillerTask;


            // Alt mesajları ve giden mailleri birleştir
            altMesaj.AddRange(gidenMaillerResult);

            // UstMesajID'ye göre indeksleme yapalım
            var altMesajDict = altMesaj
                .GroupBy(m => m.UstMesajID)
                .ToDictionary(g => g.Key ?? 0, g => g.OrderByDescending(m => m.Tarih).ToList());

            int inx = 1;

            // Son birleştirme işlemi
            foreach (var item in mesajLar)
            {
                item.GrupNo = inx;
                mesajList.Add(item);
                List<FrMesajlarDto> secilenler;
                if (altMesajDict.TryGetValue(item.MesajID, out secilenler))
                {
                    foreach (var item2 in secilenler)
                    {
                        mesajList.Add(new FrMesajlarDto
                        {
                            GidenGelen = item2.GidenGelen,
                            GrupNo = item.GrupNo,
                            KategoriAdi = item.KategoriAdi,
                            Konu = item.Konu,
                            Email = item2.Email,
                            Aciklama = item2.Aciklama,
                            Tarih = item2.Tarih,
                            KullaniciTipAdi = item2.KullaniciTipAdi,
                            AdSoyad = item2.AdSoyad,
                            OgrenciNo = item2.OgrenciNo,
                            KayitDonemAdi = item2.KayitDonemAdi,
                            KayitTarihi = item2.KayitTarihi,
                            OgrenimTipAdi = item2.OgrenimTipAdi,
                            AnabilimdaliAdi = item2.AnabilimdaliAdi,
                            ProgramAdi = item2.ProgramAdi
                        });
                    }
                }

                inx++;
            }

            return mesajList;
        }
         

    }
}