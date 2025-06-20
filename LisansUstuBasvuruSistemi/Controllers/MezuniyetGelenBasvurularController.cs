using BiskaUtil;
using Entities.Entities;
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
using DevExpress.XtraPrinting.BarCode;
using LisansUstuBasvuruSistemi.Business;
using LisansUstuBasvuruSistemi.Raporlar.Mezuniyet;
using LisansUstuBasvuruSistemi.Utilities.MenuAndRoles;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.Helpers;
using OfficeOpenXml;

namespace LisansUstuBasvuruSistemi.Controllers
{
    [Authorize]
    [OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    public class MezuniyetGelenBasvurularController : Controller
    {
        private readonly LubsDbEntities _entities = new LubsDbEntities();
        [Authorize(Roles = RoleNames.MezuniyetGelenBasvurular)]
        public ActionResult Index(string ekd, int? sMezuniyetBid, int? sTabId)
        {
            var model = new FmMezuniyetBasvurulari() { PageSize = 50 };
            var mbGelenBKayitYetki = RoleNames.MezuniyetGelenBasvurularKayit.InRoleCurrent();
            if (mbGelenBKayitYetki && !sMezuniyetBid.HasValue)
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
                    join mOt in _entities.MezuniyetSureciOgrenimTipKriterleris on new
                    { s.MezuniyetSurecID, s.OgrenimTipKod } equals new { mOt.MezuniyetSurecID, mOt.OgrenimTipKod }
                    join o in _entities.OgrenimTipleris on new { s.OgrenimTipKod, ms.EnstituKod } equals new
                    { o.OgrenimTipKod, o.EnstituKod }
                    join pr in _entities.Programlars on s.ProgramKod equals pr.ProgramKod
                    join abl in _entities.AnabilimDallaris on pr.AnabilimDaliID equals abl.AnabilimDaliID
                    join en in _entities.Enstitulers on s.MezuniyetSureci.EnstituKod equals en.EnstituKod
                    join bs in _entities.MezuniyetSurecis on s.MezuniyetSurecID equals bs.MezuniyetSurecID
                    join d in _entities.Donemlers on bs.DonemID equals d.DonemID
                    join ktip in _entities.KullaniciTipleris on s.Kullanicilar.KullaniciTipID equals ktip.KullaniciTipID
                    join dr in _entities.MezuniyetYayinKontrolDurumlaris on s.MezuniyetYayinKontrolDurumID equals dr
                        .MezuniyetYayinKontrolDurumID
                    join qmsd in _entities.MezuniyetSinavDurumlaris on s.MezuniyetSinavDurumID equals qmsd
                        .MezuniyetSinavDurumID into defMsd
                    from msd in defMsd.DefaultIfEmpty()
                    join qjOf in _entities.MezuniyetJuriOneriFormlaris on s.MezuniyetBasvurulariID equals qjOf
                        .MezuniyetBasvurulariID into defJof
                    from jOf in defJof.DefaultIfEmpty()
                    let srT = s.SRTalepleris.OrderByDescending(os => os.SRTalepID).FirstOrDefault()
                    let td = s.MezuniyetBasvurulariTezDosyalaris.OrderByDescending(os => os.MezuniyetBasvurulariTezDosyaID)
                        .FirstOrDefault()

                    where bs.Enstituler.EnstituKisaAd.Contains(ekd) &&
                          s.MezuniyetBasvurulariID == (model.SMezuniyetBID ?? s.MezuniyetBasvurulariID)
                    select new FrMezuniyetBasvurulari
                    {

                        MezuniyetBasvurulariID = s.MezuniyetBasvurulariID,
                        TezDanismanID = s.TezDanismanID,
                        EnstituKod = en.EnstituKod,
                        EnstituAdi = en.EnstituAd,
                        OgrenimTipKod = s.OgrenimTipKod,
                        OgrenimTipAdi = o.OgrenimTipAdi,
                        IsTezDiliTr = s.IsTezDiliTr,
                        AnabilimDaliId = abl.AnabilimDaliID,
                        AnabilimdaliAdi = abl.AnabilimDaliAdi,
                        ProgramKod = pr.ProgramKod,
                        ProgramAdi = pr.ProgramAdi,
                        MezuniyetSurecID = s.MezuniyetSurecID,
                        SurecBaslangicYil = bs.BaslangicYil,
                        DonemID = bs.DonemID,
                        MezuniyetSurecAdi = bs.BaslangicYil + "/" + bs.BitisYil + " " + d.DonemAdi + " " + bs.SiraNo,
                        BasTar = bs.BaslangicTarihi,
                        BitTar = bs.BitisTarihi,
                        KullaniciID = s.KullaniciID,
                        UserKey = kul.UserKey,
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
                        KullaniciTipAdi = s.KullaniciTipID == KullaniciTipiEnum.YerliOgrenci ? "" : ktip.KullaniciTipAdi,
                        KayitOgretimYiliBaslangic = s.KayitOgretimYiliBaslangic,
                        KayitOgretimYiliDonemID = s.KayitOgretimYiliDonemID,
                        BasvuruTarihi = s.BasvuruTarihi,
                        IsMezunOldu = s.IsMezunOldu,
                        MezuniyetTarihi = s.MezuniyetTarihi,
                        SrTalebi = srT,
                        SRDurumID = srT.SRDurumID,
                        TezKontrolKullaniciID = s.TezKontrolKullaniciID,
                        TeslimFormDurumu = srT != null && s.MezuniyetBasvurulariTezTeslimFormlaris.Any(),
                        IsOnaylandiOrDuzeltme = td != null ? td.IsOnaylandiOrDuzeltme : null,
                        TezDosyasiIlkKezKontrolBekliyor = td != null && !td.IsOnaylandiOrDuzeltme.HasValue && s.MezuniyetBasvurulariTezDosyalaris.Count == 1,
                        MezuniyetBasvurulariTezDosyasi = td,
                        UzatmaSuresiGun = mOt.SinavUzatmaSinavAlmaSuresiMaxAy,
                        MezuniyetSuresiGun = mOt.SinavUzatmaSinavAlmaSuresiMaxAy,
                        EYKTarihi = s.EYKTarihi,
                        EYKSayisi = s.EYKSayisi,
                        MBYayinTurIDs = s.MezuniyetBasvurulariYayins.Where(p => p.MezuniyetBasvurulariID == s.MezuniyetBasvurulariID).Select(sy => sy.MezuniyetYayinTurID).ToList(),

                        FormNo = jOf != null ? jOf.UniqueID : "",
                        MezuniyetJuriOneriFormu = jOf,
                        CiltliTezTeslimUzatmaTalebi = s.CiltliTezTeslimUzatmaTalebi,
                        CiltliTezTeslimUzatmaTalebiDanismanOnay = s.CiltliTezTeslimUzatmaTalebiDanismanOnay,
                        CiltliTezTeslimUzatmaTalebiEykDaOnay = s.CiltliTezTeslimUzatmaTalebiEykDaOnay,
                        CiltliTezTeslimUzatmaTalebiEykDaOnayEYKSayisi = s.CiltliTezTeslimUzatmaTalebiEykDaOnayEYKSayisi,
                        TezTeslimSonTarih = s.TezTeslimSonTarih,
                        IsDanismanOnay = s.IsDanismanOnay,
                        DanismanOnayTarihi = s.DanismanOnayTarihi,
                        DanismanOnayAciklama = s.DanismanOnayAciklama,
                        MezuniyetYayinKontrolDurumID = s.MezuniyetYayinKontrolDurumID,
                        MezuniyetYayinKontrolDurumAdi = dr.MezuniyetYayinKontrolDurumAdi,
                        MezuniyetYayinKontrolDurumOnayTarihi = s.MezuniyetYayinKontrolDurumOnayTarihi,
                        MezuniyetYayinKontrolDurumOnayYapanKullaniciID = s.MezuniyetYayinKontrolDurumOnayYapanKullaniciID,
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
                int basYil = model.MezuniyetSureci.ToString().Substring(0, 4).ToInt(0);
                int donemId = model.MezuniyetSureci.ToString().Substring(4, 1).ToInt(0);
                q = q.Where(p => p.SurecBaslangicYil == basYil && p.DonemID == donemId);
            }

            if (model.MezuniyetSurecID.HasValue) q = q.Where(p => p.MezuniyetSurecID == model.MezuniyetSurecID.Value);
            if (model.KayitDonemi.IsNullOrWhiteSpace() == false)
            {
                var yil = model.KayitDonemi.Split('_')[0].ToInt(0);
                var donem = model.KayitDonemi.Split('_')[1].ToInt(0);
                q = q.Where(p => p.KayitOgretimYiliBaslangic == yil && p.KayitOgretimYiliDonemID == donem);
            }

            if (model.AnabilimDaliID.HasValue)
            {
                q = q.Where(p => p.AnabilimDaliId == model.AnabilimDaliID);
            }
            if (model.MezuniyetYayinKontrolDurumID.HasValue)
            {
                if (model.MezuniyetYayinKontrolDurumID == MezuniyetYayinKontrolDurumuEnum.DanismanOnayiBekleniyor)
                {
                    q = q.Where(p => p.MezuniyetYayinKontrolDurumID == MezuniyetYayinKontrolDurumuEnum.Onaylandi && !p.IsDanismanOnay.HasValue);
                }
                else if (model.MezuniyetYayinKontrolDurumID == MezuniyetYayinKontrolDurumuEnum.EnstituOnayiBekleniyor)
                {
                    q = q.Where(p => p.MezuniyetYayinKontrolDurumID == MezuniyetYayinKontrolDurumuEnum.Onaylandi && p.IsDanismanOnay == true);
                }
                else q = q.Where(p => p.MezuniyetYayinKontrolDurumID == model.MezuniyetYayinKontrolDurumID);
            }
            if (model.IsMezuniyetYayinKontrolAciklamasiVar.HasValue)
            {
                q = model.IsMezuniyetYayinKontrolAciklamasiVar.Value ?
                    q.Where(p => p.MezuniyetYayinKontrolDurumAciklamasi != null && p.MezuniyetYayinKontrolDurumAciklamasi.Trim() != "") :
                    q.Where(p => p.MezuniyetYayinKontrolDurumAciklamasi == null || p.MezuniyetYayinKontrolDurumAciklamasi.Trim() == "");
            }
            if (model.OgrenimTipKod.HasValue) q = q.Where(p => p.OgrenimTipKod == model.OgrenimTipKod);
            if (model.IsTezDiliTr.HasValue) q = q.Where(p => p.IsTezDiliTr == model.IsTezDiliTr);
            if (model.JuriOneriFormuDurumuID.HasValue)
            {
                q = q.Where(p => p.MezuniyetYayinKontrolDurumID == MezuniyetYayinKontrolDurumuEnum.KabulEdildi);
                if (model.JuriOneriFormuDurumuID == MezuniyetJofDurumuEnum.FormOlusturulmadi) q = q.Where(p => p.MezuniyetJuriOneriFormu == null);
                else if (model.JuriOneriFormuDurumuID == MezuniyetJofDurumuEnum.FormOlusturuldu) q = q.Where(p => p.MezuniyetJuriOneriFormu != null && !p.MezuniyetJuriOneriFormu.EYKYaGonderildi.HasValue);
                else if (model.JuriOneriFormuDurumuID == MezuniyetJofDurumuEnum.EykYaGonderimiOnaylandi) q = q.Where(p => p.MezuniyetJuriOneriFormu != null && p.MezuniyetJuriOneriFormu.EYKYaGonderildi == true && !p.MezuniyetJuriOneriFormu.EYKYaHazirlandi.HasValue);
                else if (model.JuriOneriFormuDurumuID == MezuniyetJofDurumuEnum.EykYaGonderimiOnaylanmadi) q = q.Where(p => p.MezuniyetJuriOneriFormu != null && p.MezuniyetJuriOneriFormu.EYKYaGonderildi == false);
                else if (model.JuriOneriFormuDurumuID == MezuniyetJofDurumuEnum.EykYaHazirlandi) q = q.Where(p => p.MezuniyetJuriOneriFormu != null && p.MezuniyetJuriOneriFormu.EYKYaHazirlandi == true && !p.MezuniyetJuriOneriFormu.EYKDaOnaylandi.HasValue);
                else if (model.JuriOneriFormuDurumuID == MezuniyetJofDurumuEnum.EykDaOnaylandi) q = q.Where(p => p.MezuniyetJuriOneriFormu != null && p.MezuniyetJuriOneriFormu.EYKDaOnaylandi == true && !p.SRDurumID.HasValue);
                else if (model.JuriOneriFormuDurumuID == MezuniyetJofDurumuEnum.EykDaOnaylanmadi) q = q.Where(p => p.MezuniyetJuriOneriFormu != null && p.MezuniyetJuriOneriFormu.EYKDaOnaylandi == false);


            }
            if (model.CiltliTezTeslimUzatmaTalepDurumuID.HasValue)
            {
                q = q.Where(p => p.MezuniyetYayinKontrolDurumID == MezuniyetYayinKontrolDurumuEnum.KabulEdildi && p.SRDurumID.HasValue);
                if (model.CiltliTezTeslimUzatmaTalepDurumuID == MezuniyetTezTeslimUzatmaDurumuEnum.TalepOlusturulmadi) q = q.Where(p => p.CiltliTezTeslimUzatmaTalebi != true && !p.IsMezunOldu.HasValue);
                else if (model.CiltliTezTeslimUzatmaTalepDurumuID == MezuniyetTezTeslimUzatmaDurumuEnum.TalepOlusturuldu) q = q.Where(p => p.CiltliTezTeslimUzatmaTalebi == true && !p.CiltliTezTeslimUzatmaTalebiDanismanOnay.HasValue);
                else if (model.CiltliTezTeslimUzatmaTalepDurumuID == MezuniyetTezTeslimUzatmaDurumuEnum.DanismanOnayladi) q = q.Where(p => p.CiltliTezTeslimUzatmaTalebi == true && p.CiltliTezTeslimUzatmaTalebiDanismanOnay == true && !p.CiltliTezTeslimUzatmaTalebiEykDaOnay.HasValue && !p.IsMezunOldu.HasValue);
                else if (model.CiltliTezTeslimUzatmaTalepDurumuID == MezuniyetTezTeslimUzatmaDurumuEnum.DanismanOnaylamadi) q = q.Where(p => p.CiltliTezTeslimUzatmaTalebi == true && p.CiltliTezTeslimUzatmaTalebiDanismanOnay == false);
                else if (model.CiltliTezTeslimUzatmaTalepDurumuID == MezuniyetTezTeslimUzatmaDurumuEnum.EykDaOnaylandi) q = q.Where(p => p.CiltliTezTeslimUzatmaTalebiDanismanOnay == true && p.CiltliTezTeslimUzatmaTalebiEykDaOnay == true);
                else if (model.CiltliTezTeslimUzatmaTalepDurumuID == MezuniyetTezTeslimUzatmaDurumuEnum.EykDaOnaylanmadi) q = q.Where(p => p.CiltliTezTeslimUzatmaTalebiDanismanOnay == true && p.CiltliTezTeslimUzatmaTalebiEykDaOnay == false);


            }

            if (model.SRDurumID.HasValue)
            {
                q = model.SRDurumID != -1 ? q.Where(p => p.SRDurumID == model.SRDurumID.Value) : q.Where(p => p.SRDurumID.HasValue);
            }

            if (model.TDDurumID.HasValue)
            {
                if (model.TDDurumID == TezKontrolDurumEnum.IlkKezKontrolBekleyenler)
                {
                    q = q.Where(p => p.TezDosyasiIlkKezKontrolBekliyor == true);
                }
                else if (model.TDDurumID == TezKontrolDurumEnum.IslemBekleyenler) // işlem bekliyor
                    q = q.Where(p => p.MezuniyetBasvurulariTezDosyasi != null && !p.IsOnaylandiOrDuzeltme.HasValue && !p.IsMezunOldu.HasValue && p.MezuniyetYayinKontrolDurumID == MezuniyetYayinKontrolDurumuEnum.KabulEdildi);
                else if (model.TDDurumID == TezKontrolDurumEnum.Onaylananlar)
                {
                    q = q.Where(p => p.IsOnaylandiOrDuzeltme == true);
                }
                else if (model.TDDurumID == TezKontrolDurumEnum.DuzeltmeTalepEdildi)
                {
                    q = q.Where(p => p.IsOnaylandiOrDuzeltme == false);

                }
            }
            if (model.MezuniyetSinavDurumID.HasValue)
            {
                if (model.MezuniyetSinavDurumID == -50)
                {
                    q = q.Where(p => p.SrTalebi != null && p.SrTalebi.JuriSonucMezuniyetSinavDurumID == MezuniyetSinavDurumEnum.Basarili);
                }
                else
                {
                    q = model.MezuniyetSinavDurumID == MezuniyetSinavDurumEnum.SonucGirilmedi
                        ? q.Where(p => !p.SrTalebi.MezuniyetSinavDurumID.HasValue || p.SrTalebi.MezuniyetSinavDurumID == model.MezuniyetSinavDurumID.Value)
                        : q.Where(p => p.SrTalebi.MezuniyetSinavDurumID == model.MezuniyetSinavDurumID.Value);
                }

            }

            if (model.TezKontrolKullaniciId.HasValue) q = q.Where(p => p.TezKontrolKullaniciID == model.TezKontrolKullaniciId);
            if (model.TeslimFormDurumu.HasValue) q = q.Where(p => p.TeslimFormDurumu == model.TeslimFormDurumu.Value);
            if (model.MezuniyetDurumID != -1)
            {
                var isMezunOldu = model.MezuniyetDurumID.HasValue ? (model.MezuniyetDurumID == 1) : (bool?)null;
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
                q = q.Where(p => p.AdSoyad.Contains(model.AdSoyad) || p.OgrenciNo.Contains(model.AdSoyad) || p.FormNo == model.AdSoyad || p.TezDanismanAdi.Contains(model.AdSoyad) || p.ProgramAdi.Contains(model.AdSoyad) || p.AnabilimdaliAdi.Contains(model.AdSoyad));
            }

            if (!model.EykSayisi.IsNullOrWhiteSpace())
            {
                model.EykSayisi = model.EykSayisi.Trim();
                q = q.Where(p => p.EYKSayisi == model.EykSayisi);
            }
            if (!model.CiltliTezTeslimUzatmaTalebiEykDaOnayEYKSayisi.IsNullOrWhiteSpace())
            {
                model.CiltliTezTeslimUzatmaTalebiEykDaOnayEYKSayisi = model.CiltliTezTeslimUzatmaTalebiEykDaOnayEYKSayisi.Trim();
                q = q.Where(p => p.CiltliTezTeslimUzatmaTalebiEykDaOnayEYKSayisi == model.CiltliTezTeslimUzatmaTalebiEykDaOnayEYKSayisi);
            }
            if (model.UyrukKod.HasValue) q = q.Where(p => p.UyrukKod == model.UyrukKod);
            var isFiltered = !Equals(q, q2);

            model.RowCount = q.Count();
            if (!model.Sort.IsNullOrWhiteSpace()) q = q.OrderBy(model.Sort);
            else
            {
                q = model.JuriOneriFormuDurumuID == MezuniyetJofDurumuEnum.FormOlusturuldu ? q.OrderBy(o => o.MezuniyetJuriOneriFormu.EYKYaGonderildiIslemTarihi) : q.OrderByDescending(o => o.BasvuruTarihi);
            }

            if (model.JuriOneriFormuDurumuID == MezuniyetJofDurumuEnum.EykYaHazirlandi) model.SelectedMezuniyetBasvurulariIds = q.Select(s => s.MezuniyetBasvurulariID).ToList();
            else if (model.CiltliTezTeslimUzatmaTalepDurumuID == MezuniyetTezTeslimUzatmaDurumuEnum.DanismanOnayladi) model.SelectedMezuniyetBasvurulariIds = q.Select(s => s.MezuniyetBasvurulariID).ToList();
            var qdata = q.Skip(model.StartRowIndex).Take(model.PageSize).ToList();
            model.Data = qdata;
            #region export
            if (export && model.RowCount > 0)
            {
                var gv = new GridView();
                var qExp = q.ToList();
                const int batchSize = 1000;
                var basvuruIds = qExp.Select(x => x.MezuniyetBasvurulariID).ToList();
                var yayinlar = new Dictionary<int, string>();
                for (int i = 0; i < basvuruIds.Count; i += batchSize)
                {
                    var batch = basvuruIds.Skip(i).Take(batchSize).ToList();

                    var batchYayinlar = (from q1 in _entities.MezuniyetBasvurulariYayins
                                         join qx in _entities.MezuniyetYayinTurleris on q1.MezuniyetYayinTurID equals qx.MezuniyetYayinTurID
                                         where batch.Contains(q1.MezuniyetBasvurulariID)
                                         select new
                                         {
                                             q1.MezuniyetBasvurulariID,
                                             YayinBilgisi = q1.YayinBasligi + " (" + qx.MezuniyetYayinTurAdi + ")"
                                         })
                        .ToList()
                        .GroupBy(x => x.MezuniyetBasvurulariID)
                        .ToDictionary(g => g.Key, g => string.Join(", ", g.Select(x => x.YayinBilgisi)));

                    foreach (var kvp in batchYayinlar)
                    {
                        yayinlar[kvp.Key] = kvp.Value;
                    }
                }
                gv.DataSource = (from s in qExp
                                 join td in _entities.Kullanicilars on s.TezDanismanID equals td.KullaniciID
                                 join ey in _entities.Kullanicilars on s.MezuniyetYayinKontrolDurumOnayYapanKullaniciID equals ey.KullaniciID into defEy
                                 from Ey in defEy.DefaultIfEmpty()
                                 select new
                                 {
                                     s.MezuniyetSurecAdi,
                                     s.OgrenimTipAdi,
                                     TezDanismanAdi = s.TezDanismanUnvani + " " + s.TezDanismanAdi,
                                     DanismanTel = td.CepTel,
                                     DanismanEmail = td.EMail,
                                     s.AnabilimdaliAdi,
                                     s.ProgramAdi,
                                     GsisKayitTarihi = s.KayitTarihi != null ? s.KayitTarihi.ToFormatDate() : "",
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
                                     // Yayınları virgülle ayrılmış string olarak al
                                     Yayinlar = yayinlar.ContainsKey(s.MezuniyetBasvurulariID)
                                               ? yayinlar[s.MezuniyetBasvurulariID]
                                               : "",
                                     BasvuruDurumu=s.MezuniyetYayinKontrolDurumAdi,
                                     BasvuruOnayTarihi=s.MezuniyetYayinKontrolDurumOnayTarihi,
                                     BasvuruyuOnaylayan = Ey != null ? (Ey.Ad + " " + Ey.Soyad) : "",
                                     EYKTarihi = s.EYKTarihi != null ? s.EYKTarihi.Value.ToFormatDate() : "",
                                     JOFTezbasligiDegisti = s.MezuniyetJuriOneriFormu != null ? (s.MezuniyetJuriOneriFormu.IsTezBasligiDegisti == true ? "Değişti" : "Değişmedi") : "-",
                                     JOFTezDili = s.MezuniyetJuriOneriFormu != null ? (s.IsTezDiliTr == true ? "Türkçe" : "İngilizce") : "",
                                     JOFTezBasligiTr = s.MezuniyetJuriOneriFormu != null ? s.TezBaslikTr : "-",
                                     JOFTezBasligiEn = s.MezuniyetJuriOneriFormu != null ? s.TezBaslikEn : "-",
                                     JOFYeniTezBaslikTr = s.MezuniyetJuriOneriFormu != null ? s.MezuniyetJuriOneriFormu.YeniTezBaslikTr : "-",
                                     JOFYeniTezBaslikEn = s.MezuniyetJuriOneriFormu != null ? s.MezuniyetJuriOneriFormu.YeniTezBaslikEn : "-",
                                     SinavTarihi = s.SrTalebi != null ? s.SrTalebi.Tarih.ToFormatDate() : "",
                                     SinavdaTezbasligiDegisti = s.SrTalebi != null ? (s.SrTalebi.IsTezBasligiDegisti == true ? "Değişti" : "Değişmedi") : null,
                                     SinavTezDili = s.SrTalebi != null ? (s.IsTezDiliTr == true ? "Türkçe" : "İngilizce") : "",
                                     SinavTezBasligiTr = s.SrTalebi != null ? (s.SrTalebi.IsTezBasligiDegisti == true ? s.SrTalebi.YeniTezBaslikTr : (s.MezuniyetJuriOneriFormu.IsTezBasligiDegisti == true ? s.MezuniyetJuriOneriFormu.YeniTezBaslikTr : s.TezBaslikTr)) : "-",
                                     SinavTezBasligiEn = s.SrTalebi != null ? (s.SrTalebi.IsTezBasligiDegisti == true ? s.SrTalebi.YeniTezBaslikEn : (s.MezuniyetJuriOneriFormu.IsTezBasligiDegisti == true ? s.MezuniyetJuriOneriFormu.YeniTezBaslikEn : s.TezBaslikEn)) : "-",
                                     s.MezuniyetSinavDurumAdi,
                                     UzatmaTarihi = s.SrTalebi != null && s.MezuniyetSinavDurumID == MezuniyetSinavDurumEnum.Uzatma ? s.SrTalebi.Tarih.AddDays(s.UzatmaSuresiGun).ToFormatDate() : "",
                                     TezTeslimSonTarih = s.SrTalebi != null && s.MezuniyetSinavDurumID == MezuniyetSinavDurumEnum.Basarili ? (s.TezTeslimSonTarih ?? s.SrTalebi.Tarih.AddDays(s.MezuniyetSuresiGun).Date).ToString("dd.MM.yyy") : "",
                                     MezuniyetDurumu = s.IsMezunOldu.HasValue ? (s.IsMezunOldu.Value ? "Ciltli Son Tez Teslimini Yapmıştır" : "Ciltli Son Tez Teslimini Yapmamıştır") : "İşlem Bekliyor",
                                     MezuniyetTarihi = s.IsMezunOldu == true ? s.MezuniyetTarihi.Value.ToFormatDate() : "",
                                 }).ToList();
                gv.DataBind();
                Response.ContentType = "application/ms-excel";
                Response.ContentEncoding = System.Text.Encoding.UTF8;
                Response.BinaryWrite(System.Text.Encoding.UTF8.GetPreamble());
                StringWriter sw = new StringWriter();
                HtmlTextWriter htw = new HtmlTextWriter(sw);
                gv.RenderControl(htw);

                return File(System.Text.Encoding.UTF8.GetBytes(sw.ToString()), Response.ContentType, "Export_MezuniyetBasvuruListesi_" + DateTime.Now.ToFormatDate() + ".xls");
            }
            #endregion
            ViewBag.filteredOgrenciIds = isFiltered ? q.Select(s => s.KullaniciID).Distinct().ToList() : new List<int>();
            ViewBag.filteredDanismanIds = isFiltered ? q.Where(p => p.TezDanismanID.HasValue).Select(s => s.TezDanismanID.Value).Distinct().ToList() : new List<int>();

            ViewBag.MezuniyetSurecID = new SelectList(MezuniyetBus.GetCmbMezuniyetSurecleri(enstituKod, true), "Value", "Caption", model.MezuniyetSurecID);
            ViewBag.MezuniyetSureci = new SelectList(MezuniyetBus.GetCmbMezuniyetSurecGroup(enstituKod, true), "Value", "Caption", model.MezuniyetSureci);
            ViewBag.OgrenimTipKod = new SelectList(OgrenimTipleriBus.CmbAktifOgrenimTipleri(enstituKod, true), "Value", "Caption", model.OgrenimTipKod);

            ViewBag.MezuniyetYayinKontrolDurumID = new SelectList(MezuniyetBus.GetCmbMezuniyetYayinDurumListe(true, true), "Value", "Caption", model.MezuniyetYayinKontrolDurumID);
            ViewBag.IsMezuniyetYayinKontrolAciklamasiVar = new SelectList(MezuniyetBus.GetCmbMezuniyetYayinKontrolAciklamaDurumListe(true), "Value", "Caption", model.MezuniyetYayinKontrolDurumID);
            ViewBag.JuriOneriFormuDurumuID = new SelectList(MezuniyetBus.GetCmbJuriOneriFormuDurumu(true), "Value", "Caption", model.JuriOneriFormuDurumuID);
            ViewBag.CiltliTezTeslimUzatmaTalepDurumuID = new SelectList(MezuniyetBus.GetCmbCiltliTezTeslimUzatmaTalepDurumu(true), "Value", "Caption", model.CiltliTezTeslimUzatmaTalepDurumuID);
            ViewBag.KayitDonemi = new SelectList(MezuniyetBus.GetCmbMezuniyetKayitDonemleri(enstituKod, model.MezuniyetSurecID, true), "Value", "Caption", model.KayitDonemi);
            var srDurms = SrTalepleriBus.GetCmbSrDurumListe(true);
            srDurms.Add(new CmbIntDto() { Value = -1, Caption = "Tüm Rezervasyonlar" });
            ViewBag.SRDurumID = new SelectList(srDurms, "Value", "Caption", model.SRDurumID);
            ViewBag.TDDurumID = new SelectList(MezuniyetBus.GetCmbTezDurumListe(true), "Value", "Caption", model.TDDurumID);
            ViewBag.IsTezDiliTr = new SelectList(MezuniyetBus.GetCmbTezDili(true), "Value", "Caption", model.IsTezDiliTr);
            var cmbSinavDurumListe = MezuniyetBus.GetCmbMzSinavDurumListe(true);
            cmbSinavDurumListe.Add(new CmbIntDto { Value = -50, Caption = "Sınav Durumu Başarılı Olanlar" });
            ViewBag.MezuniyetSinavDurumID = new SelectList(cmbSinavDurumListe, "Value", "Caption", model.MezuniyetSinavDurumID);
            ViewBag.TezKontrolKullaniciId = new SelectList(MezuniyetBus.GetCmbAktifTezKontrolSorumlulari(enstituKod, true), "Value", "Caption", model.TezKontrolKullaniciId);
            ViewBag.TeslimFormDurumu = new SelectList(MezuniyetBus.GetCmbTeslimFormDurumu(true), "Value", "Caption", model.TeslimFormDurumu);
            ViewBag.MezuniyetDurumID = new SelectList(MezuniyetBus.GetCmbMezuniyetDurumId(true), "Value", "Caption", model.MezuniyetDurumID);
            ViewBag.AnabilimDaliID = new SelectList(MezuniyetBus.GetCmbFilterMezuniyetAnabilimDallari(enstituKod, model.MezuniyetSurecID, true), "Value", "Caption", model.AnabilimDaliID);
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

            var yayin = _entities.MezuniyetBasvurulariYayins.First(p => p.MezuniyetBasvurulariYayinID == id);


            if (yayin.MezuniyetBasvurulari.MezuniyetYayinKontrolDurumID >= MezuniyetYayinKontrolDurumuEnum.KabulEdildi)
            {
                mmMessage.Messages.Add("Baivuru durumu Enstitü Tarafından Kabul Edildi olan başvurular için Yayın Onay işlemleri yapılamaz. İşlem yapılabilmesi için Başvuru durumu Taslak ya da Öğrenci Başvurusunu Onayladı durumunda olmalıdır.");
            }

            if (!mmMessage.Messages.Any() && onaylandi.HasValue)
            {
                if (danismanIsmiVar.HasValue == false)
                {
                    mmMessage.Messages.Add("Onaylama işlemini yapabilmeniz için 'Danışman İsmi Var Mı' sorusunu cevaplayınız");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "DanismanIsmiVar" });
                }
                if (tezIcerikUyumuVar.HasValue == false)
                {
                    mmMessage.Messages.Add("Onaylama işlemini yapabilmeniz için 'Tez İçeriği ile Uyumlu mu' sorusunu cevaplayınız");
                    mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "TezIcerikUyumuVar" });
                }
            }
            if (mmMessage.Messages.Count == 0)
            {
                yayin.DanismanIsmiVar = danismanIsmiVar;
                yayin.TezIcerikUyumuVar = tezIcerikUyumuVar;
                yayin.Onaylandi = onaylandi;
                yayin.IslemTarihi = DateTime.Now;
                yayin.IslemYapanID = UserIdentity.Current.Id;
                yayin.IslemYapanIP = UserIdentity.Ip;
                _entities.SaveChanges();
                LogIslemleri.LogEkle("MezuniyetBasvurulariYayins", LogCrudType.Update, yayin.ToJson());
                mmMessage.IsSuccess = true;
                mmMessage.Title = "Yayın bilgi kontrol işlemi";
                mmMessage.Messages.Add("Kayıt güncellendi");
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Nothing, PropertyName = "DanismanIsmiVar" });
                mmMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Nothing, PropertyName = "TezIcerikUyumuVar" });

            }
            else
            {
                mmMessage.Title = "Yayın bilgi kontrol kaydını yapabilmek için aşağıdaki uyarıları kontrol ediniz.";
                mmMessage.IsSuccess = false;
            }
            mmMessage.MessageType = mmMessage.IsSuccess ? MsgTypeEnum.Success : MsgTypeEnum.Warning;
            return new { Messages = mmMessage }.ToJsonResult();

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
                LogIslemleri.LogEkle("MezuniyetBasvurulariYayins", LogCrudType.Update, kayit.ToJson());
                mmMessage.Messages.Add("Index Bilgisi Güncellendi");
                mmMessage.MessageType = MsgTypeEnum.Success;

            }
            catch (Exception ex)
            {
                mmMessage.MessageType = MsgTypeEnum.Error;
                mmMessage.IsSuccess = false;
                mmMessage.Messages.Add("Index Bilgisi Güncellenirken bir hata oluştu! Hata:" + ex.ToExceptionMessage());
                SistemBilgilendirmeBus.SistemBilgisiKaydet(ex.ToExceptionMessage(), ex.ToExceptionStackTrace(), BilgiTipiEnum.OnemsizHata);
            }
            var strView = ViewRenderHelper.RenderPartialView("Ajax", "GetMessage", mmMessage);
            return Json(new { mmMessage.IsSuccess, Messages = strView }, "application/json", JsonRequestBehavior.AllowGet);
        }

        [Authorize(Roles = RoleNames.MezuniyetGelenBasvurularKayit)]
        public ActionResult DurumKayit(int id, int? mezuniyetYayinKontrolDurumId, int? tekKaynakOrani, int? toplamKaynakOrani, string mezuniyetYayinKontrolDurumAciklamasi, bool? yayinKontrolKabulTaahhutEdildi)
        {
            var mmMessage = new MmMessage { Title = "Mezuniyet başvurusu durum değişiklik işlemi" };
            var mBasvur = _entities.MezuniyetBasvurularis.First(p => p.MezuniyetBasvurulariID == id);
            var ogrenimTipKrt =
                mBasvur.MezuniyetSureci.MezuniyetSureciOgrenimTipKriterleris.FirstOrDefault(f =>
                    f.OgrenimTipKod == mBasvur.OgrenimTipKod);
            if (mezuniyetYayinKontrolDurumId.HasValue == false)
            {
                mmMessage.Messages.Add("Başvuru durumu seçiniz");
            }
            else if (mezuniyetYayinKontrolDurumId == MezuniyetYayinKontrolDurumuEnum.IptalEdildi && mBasvur.IsMezunOldu.HasValue)
            {
                mmMessage.Messages.Add("Mezuniyet durumu girilen bir öğrencinin başurusu iptal edilemez.");
            }
            else if (mezuniyetYayinKontrolDurumId == MezuniyetYayinKontrolDurumuEnum.KaydiSilindi && mBasvur.IsMezunOldu.HasValue)
            {
                mmMessage.Messages.Add("Mezuniyet durumu girilen bir öğrencinin başurusu kaydı silindi olarak çevrilemez.");
            }
            else if (mezuniyetYayinKontrolDurumId == MezuniyetYayinKontrolDurumuEnum.Taslak && mBasvur.MezuniyetJuriOneriFormlaris.Any())
            {
                mmMessage.Messages.Add("Jüri öneri formu oluşturulan bir başvuru taslağa çevrilemez.");
            }
            else if ((mezuniyetYayinKontrolDurumId == MezuniyetYayinKontrolDurumuEnum.IptalEdildi || mezuniyetYayinKontrolDurumId == MezuniyetYayinKontrolDurumuEnum.KaydiSilindi) && mezuniyetYayinKontrolDurumAciklamasi.IsNullOrWhiteSpace())
            {
                mmMessage.Messages.Add("Seçilen başvuru durumu için açıklaması girilmesi zorunludur.");
            }
            else if (mezuniyetYayinKontrolDurumId == MezuniyetYayinKontrolDurumuEnum.KabulEdildi)
            {

                if (mBasvur.IsDanismanOnay != true)
                {
                    mmMessage.Messages.Add("Başvurunun kabul edilebilmesi için  öncelikle danışman onayı yapılması gerekmektedir.");
                }
                else if (mBasvur.MezuniyetBasvurulariYayins.Any(a => !a.Onaylandi.HasValue))
                {
                    mmMessage.Messages.Add("Başvurunun kabul edilebilmesi için  öncelikle yayın onaylarının yapılması gerekmektedir.");
                }
                else
                {

                    if (ogrenimTipKrt.TekKaynakOrani.HasValue)
                    {
                        if (!tekKaynakOrani.HasValue)
                            mmMessage.Messages.Add("Tek Kaynak Benzerlik Oranı bilgisi giriniz.");

                        else if (tekKaynakOrani.Value > ogrenimTipKrt.TekKaynakOrani.Value || tekKaynakOrani.Value < 0)
                            mmMessage.Messages.Add($"En fazla Tek Kaynak Benzerlik Oranı bilgisi 0 ile {ogrenimTipKrt.TekKaynakOrani} değerleri arasında olmalıdır.");
                    }
                    if (!mmMessage.Messages.Any() && ogrenimTipKrt.ToplamKaynakOrani.HasValue)
                    {
                        if (!toplamKaynakOrani.HasValue)
                            mmMessage.Messages.Add("Toplam Benzerlik Oranı bilgisi giriniz.");
                        else if (toplamKaynakOrani.Value > ogrenimTipKrt.ToplamKaynakOrani.Value || toplamKaynakOrani.Value < 0)
                            mmMessage.Messages.Add($"Toplam Benzerlik Oranı bilgisi 0 ile {ogrenimTipKrt.ToplamKaynakOrani} değerleri arasında olmalıdır.");
                    }

                    if (!mmMessage.Messages.Any() && yayinKontrolKabulTaahhutEdildi != true)
                    {
                        mmMessage.Messages.Add("Taahhüt metnini onaylayınız.");

                    }
                }

            }
            if (mmMessage.Messages.Count == 0)
            {
                var mgonder = (mezuniyetYayinKontrolDurumId == MezuniyetYayinKontrolDurumuEnum.IptalEdildi || mezuniyetYayinKontrolDurumId == MezuniyetYayinKontrolDurumuEnum.KabulEdildi) && mezuniyetYayinKontrolDurumId != mBasvur.MezuniyetYayinKontrolDurumID;


                if (mezuniyetYayinKontrolDurumId == MezuniyetYayinKontrolDurumuEnum.Taslak)
                {
                    mBasvur.IsDanismanOnay = null;
                    mBasvur.DanismanOnayTarihi = null;
                    foreach (var itemYayin in mBasvur.MezuniyetBasvurulariYayins)
                    {
                        itemYayin.Onaylandi = null;
                        itemYayin.IslemTarihi = DateTime.Now;
                        itemYayin.IslemYapanIP = UserIdentity.Ip;
                    }

                }

                if (ogrenimTipKrt.TekKaynakOrani.HasValue) mBasvur.TekKaynakOrani = tekKaynakOrani;
                if (ogrenimTipKrt.ToplamKaynakOrani.HasValue) mBasvur.ToplamKaynakOrani = toplamKaynakOrani;

                mBasvur.MezuniyetYayinKontrolDurumID = mezuniyetYayinKontrolDurumId.Value;
                mBasvur.MezuniyetYayinKontrolDurumOnayYapanKullaniciID = UserIdentity.Current.Id;
                mBasvur.MezuniyetYayinKontrolDurumOnayTarihi = DateTime.Now;
                mBasvur.MezuniyetYayinKontrolDurumAciklamasi = mezuniyetYayinKontrolDurumAciklamasi;
                mBasvur.YayinKontrolKabulTaahhutEdildi = yayinKontrolKabulTaahhutEdildi;
                _entities.SaveChanges();

                mmMessage.IsSuccess = true;
                mmMessage.Messages.Add("Başvuru durum değişikliği gerçekleştirildi.");
                LogIslemleri.LogEkle("MezuniyetBasvurulari", LogCrudType.Update, mBasvur.ToJson());

                #region sendMail
                if (mgonder)
                {
                    MezuniyetBus.SendMailBasvuruDurum(mBasvur.MezuniyetBasvurulariID);
                }
                #endregion

            }
            else
            {
                mmMessage.IsSuccess = false;
            }

            var message = mmMessage.Messages.Any() ? mmMessage.Messages[0] : "";
            return new
            {
                success = mmMessage.IsSuccess,
                title = mmMessage.Title,
                message
            }.ToJsonResult();

        }
        public ActionResult DanismanOnayKayit(int id, bool? isDanismanOnay, string basvuruDanismanOnayAciklama)
        {
            var mmMessage = new MmMessage
            {
                Title = "Mezuniyet başvurusu danışman onay işlemi"
            };

            var mBasvur = _entities.MezuniyetBasvurularis.First(p => p.MezuniyetBasvurulariID == id);
            var kayitYetki = RoleNames.MezuniyetGelenBasvurularKayit.InRole();

            if (!kayitYetki)
            {
                if (mBasvur.TezDanismanID != UserIdentity.Current.Id)
                {
                    mmMessage.Messages.Add("Danışman olarak atanmadığını bir mezuniyet başvurusu için onay işlemi yapamazsınız!");
                }
            }

            if (!mmMessage.Messages.Any())
            {

                if (isDanismanOnay == false && basvuruDanismanOnayAciklama.IsNullOrWhiteSpace())
                {
                    mmMessage.Messages.Add("Öğrenci Başvurusunu Reddediyorum seçeneği seçilirse Açıklama girilmesi zorunludur.");
                }
                var sendMail = false;
                if (mmMessage.Messages.Count == 0)
                {

                    if (isDanismanOnay != mBasvur.IsDanismanOnay)
                    {
                        sendMail = true;
                        mBasvur.TezTeslimUniqueID = Guid.NewGuid();
                        mBasvur.TezTeslimFormKodu = Guid.NewGuid().ToString().Substring(0, 8);
                    }
                    mBasvur.IsDanismanOnay = isDanismanOnay;
                    mBasvur.DanismanOnayAciklama = basvuruDanismanOnayAciklama;
                    mBasvur.DanismanOnayTarihi = DateTime.Now;




                    _entities.SaveChanges();
                    LogIslemleri.LogEkle("MezuniyetBasvurulari", LogCrudType.Update, mBasvur.ToJson());
                    mmMessage.IsSuccess = true;
                    mmMessage.Messages.Add(isDanismanOnay.HasValue ? (isDanismanOnay.Value ? "Başvuru Onaylandı." : "Başvuru Reddedildi.") : "Onaylama İşlemi Geril Alındı.");
                    if (sendMail)
                    {
                        MezuniyetBus.SendMailBasvuruDanismanOnay(mBasvur.MezuniyetBasvurulariID);
                    }


                    // yayın kontrolü için eklenmişti düzeltme yapılacak
                    //sendMail = sendMail && !mBasvur.TezKontrolKullaniciID.HasValue && mBasvur.IsDanismanOnay == true;
                    //if (sendMail)
                    //{
                    //    MezuniyetBus.TezDosyasiKontrolYetkilisiAta(mBasvur.MezuniyetBasvurulariID);
                    //    mBasvur = _entities.MezuniyetBasvurularis.First(p => p.MezuniyetBasvurulariID == id);
                    //    if (mBasvur.TezKontrolKullaniciID.HasValue)
                    //    {
                    //        MezuniyetBus.SendMailMezuniyetTezKontrolYetkilisi(mBasvur.MezuniyetBasvurulariID);
                    //    }
                    //}


                }
            }
            mmMessage.MessageType = mmMessage.IsSuccess ? MsgTypeEnum.Success : MsgTypeEnum.Warning;
            return Json(new { Messages = mmMessage }, "application/json", JsonRequestBehavior.AllowGet);

        }

        public ActionResult DanismanUzatmaOnayKayit(int srTalepId, bool? isDanismanUzatmaSonrasiOnay, string danismanUzatmaSonrasiOnayAciklama)
        {
            var mmMessage = new MmMessage
            {
                Title = "Uzatma sonrası danışman onay işlemi"
            };

            var srTalep = _entities.SRTalepleris.First(p => p.SRTalepID == srTalepId);
            var kayitYetki = RoleNames.MezuniyetGelenBasvurularKayit.InRole();
            var onayTarihi = DateTime.Now;
            if (!kayitYetki)
            {
                if (srTalep.MezuniyetBasvurulari.TezDanismanID != UserIdentity.Current.Id)
                {
                    mmMessage.Messages.Add("Danışman olarak atanmadığını bir mezuniyet başvurusu için onay işlemi yapamazsınız!");
                }
            }
            if (srTalep.MezuniyetBasvurulari.MezuniyetYayinKontrolDurumID != MezuniyetYayinKontrolDurumuEnum.KabulEdildi)
            {
                mmMessage.Messages.Add("Mezuniyet başvuru durumu Kabul Edildi olan başvurularda işlem yapılabilir.");
            }
            else
            {
                if (_entities.SRTalepleris.Any(a => a.SRTalepID > srTalepId && a.MezuniyetBasvurulariID == srTalep.MezuniyetSinavDurumID))
                {
                    mmMessage.Messages.Add("Öğrenci tarafından yeni sınav talebi oluşturuldu. Bu işlemi yapamazsınız.");
                }
                else
                {
                    var mezuniyetSureciOgrenimTip = srTalep.MezuniyetBasvurulari.MezuniyetSureci.MezuniyetSureciOgrenimTipKriterleris.First(p => p.OgrenimTipKod == srTalep.MezuniyetBasvurulari.OgrenimTipKod);
                    var uzatmaSonrasiYeniSinavTalebiSonTarih = srTalep.UzatmaSonrasiYeniSinavTalebiSonTarih ?? srTalep.Tarih.AddMonths(mezuniyetSureciOgrenimTip.SinavUzatmaSinavAlmaSuresiMaxAy);
                    if (onayTarihi > uzatmaSonrasiYeniSinavTalebiSonTarih)
                    {
                        mmMessage.Messages.Add("Mezuniyet sınavı sonucunda uzatma işlemi sonrası yeni sınav alma işemi için son tarihi olan '" + uzatmaSonrasiYeniSinavTalebiSonTarih.ToFormatDate() + "' tarihini aşıldığı için tez kontrol taahhüt onay işlemi yapamazsınız.");
                    }
                }
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
                    srTalep.DanismanOnayTarihi = onayTarihi;

                    _entities.SaveChanges();
                    LogIslemleri.LogEkle("SRTalebi", LogCrudType.Update, srTalep.ToJson());
                    mmMessage.IsSuccess = true;
                    mmMessage.Messages.Add(isDanismanUzatmaSonrasiOnay.HasValue ? (isDanismanUzatmaSonrasiOnay.Value ? "Başvuru Onaylandı." : "Başvuru Reddedildi.") : "Onaylama İşlemi Geril Alındı.");
                }
            }
            mmMessage.MessageType = mmMessage.IsSuccess ? MsgTypeEnum.Success : MsgTypeEnum.Warning;
            return Json(new { Messages = mmMessage }, "application/json", JsonRequestBehavior.AllowGet);

        }

        [Authorize(Roles = RoleNames.MezuniyetGelenBasvurularKayit)]
        public ActionResult SrDurumKaydet(int id, int srDurumId, string srDurumAciklamasi)
        {
            string strView = "";
            string fWeight = "font-weight:";

            var srTalep = _entities.SRTalepleris.First(p => p.SRTalepID == id);


            fWeight += Convert.ToDateTime(srTalep.Tarih.ToShortDateString() + " " + srTalep.BasSaat) > DateTime.Now ? "bold;" : "normal;";
            if (srDurumId == SrTalepDurumEnum.Onaylandı && srTalep.SRSalonID.HasValue)
            {
                var qTalepEslesen = _entities.SRTalepleris.Where(a => a.SRTalepID != srTalep.SRTalepID && a.SRSalonID == srTalep.SRSalonID && a.Tarih == srTalep.Tarih &&
                                        (
                                          (a.BasSaat == srTalep.BasSaat || a.BitSaat == srTalep.BitSaat) ||
                                        (
                                            (a.BasSaat < srTalep.BasSaat && a.BitSaat > srTalep.BasSaat) || a.BasSaat < srTalep.BitSaat && a.BitSaat > srTalep.BitSaat) ||
                                            (a.BasSaat > srTalep.BasSaat && a.BasSaat < srTalep.BitSaat) || a.BitSaat > srTalep.BasSaat && a.BitSaat < srTalep.BitSaat)
                                        ).ToList();
                if (srTalep.MezuniyetBasvurulari.OgrenimTipKod.IsDoktora() && qTalepEslesen.Any(p => p.SRDurumID == SrTalepDurumEnum.Onaylandı))
                {

                    var salon = _entities.SRSalonlars.First(p => p.SRSalonID == srTalep.SRSalonID);
                    string msg = srTalep.Tarih.ToShortDateString() + " " + srTalep.BasSaat.ToString() + " - " + srTalep.BitSaat.ToString() + " Tarihi için '" + salon.SalonAdi + "' Salonu doludur bu rezervasyon onaylanamaz!";
                    var mmMessage = new MmMessage();
                    mmMessage.Messages.Add(msg);
                    mmMessage.IsSuccess = false;
                    mmMessage.MessageType = MsgTypeEnum.Error;
                    strView = ViewRenderHelper.RenderPartialView("Ajax", "GetMessage", mmMessage);
                }
            }

            bool sendMail = srTalep.SRDurumID != srDurumId && new List<int> { SrTalepDurumEnum.Reddedildi, SrTalepDurumEnum.Onaylandı }.Contains(srDurumId);
            srTalep.SRDurumID = srDurumId;
            srTalep.IslemTarihi = DateTime.Now;
            srTalep.IslemYapanID = UserIdentity.Current.Id;
            srTalep.IslemYapanIP = UserIdentity.Ip;
            if (srDurumId == SrTalepDurumEnum.Reddedildi) srTalep.SRDurumAciklamasi = srDurumAciklamasi;
            _entities.SaveChanges();
            LogIslemleri.LogEkle("SRTalepleri", LogCrudType.Update, srTalep.ToJson());
            var qbDrm = srTalep.SRDurumlari;

            if (srTalep.SRTalepTipleri.IsTezSinavi && sendMail)
            {
                var msgs = MezuniyetBus.SendMailMezuniyetSinavYerBilgisi(id, srDurumId == SrTalepDurumEnum.Onaylandı);
                if (msgs.Messages.Count > 0)
                {
                    strView = ViewRenderHelper.RenderPartialView("Ajax", "GetMessage", msgs);
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

            var srTalep = _entities.SRTalepleris.First(p => p.SRTalepID == id);
            if (srTalep.MezuniyetBasvurulari.MezuniyetYayinKontrolDurumID != MezuniyetYayinKontrolDurumuEnum.KabulEdildi)
            {
                mmMessage.Messages.Add("Mezuniyet başvuru durumu Kabul Edildi olan başvurularda işlem yapılabilir.");
            }
            else if (mezuniyetSinavDurumId == MezuniyetSinavDurumEnum.Uzatma && srTalep.MezuniyetBasvurulari.SRTalepleris.Any(a => a.SRTalepID != id && a.SRDurumID == SrTalepDurumEnum.Onaylandı && a.MezuniyetSinavDurumID == MezuniyetSinavDurumEnum.Uzatma))
            {
                mmMessage.Messages.Add("Bu mezuniyet başvurusuna daha önceden uzatma hakkı verildiğinden tekrar uzatma hakkı verilemez!");
            }
            else if (srTalep.JuriSonucMezuniyetSinavDurumID.HasValue && mezuniyetSinavDurumId > MezuniyetSinavDurumEnum.SonucGirilmedi && srTalep.JuriSonucMezuniyetSinavDurumID != mezuniyetSinavDurumId)
            {
                mmMessage.Messages.Add("Girdiğiniz sınav sonucu jürinin oylama sonucu ile aynı olması gerekmektedir!");
            }
            if (!mmMessage.Messages.Any())
            {
                if (mezuniyetSinavDurumId == MezuniyetSinavDurumEnum.Basarili)
                {
                    var ogrenimTipi = srTalep.MezuniyetBasvurulari.MezuniyetSureci.MezuniyetSureciOgrenimTipKriterleris.First(p => p.OgrenimTipKod == srTalep.MezuniyetBasvurulari.OgrenimTipKod);
                    var ttEkSureYetki = RoleNames.MezuniyetGelenBasvurularTtEkSure.InRoleCurrent();

                    if (!ttEkSureYetki)
                    {
                        if (tezTeslimSonTarih.HasValue && tezTeslimSonTarih.Value > srTalep.Tarih.AddMonths(ogrenimTipi.TezTeslimSuresiAy))
                        {
                            mmMessage.Messages.Add("Tez teslim son tarih kriteri " + srTalep.Tarih.AddMonths(ogrenimTipi.TezTeslimSuresiAy).ToFormatDate() + " tarihinden daha büyük olamaz!");
                        }
                    }

                    var mezuniyet = srTalep.MezuniyetBasvurulari;

                    if (mezuniyet.CiltliTezTeslimUzatmaTalebi == true && mezuniyet.CiltliTezTeslimUzatmaTalebiDanismanOnay != false && mezuniyet.CiltliTezTeslimUzatmaTalebiEykDaOnay != false)
                    {
                        var maxTezTeslimSonTarih = srTalep.Tarih.AddMonths(ogrenimTipi.TezTeslimSuresiAy + 1);

                        if (tezTeslimSonTarih < maxTezTeslimSonTarih)
                        {
                            mmMessage.Messages.Add("Öğrencinin ciltli tez teslimi için ek süre talebi bulunduğundan, tez teslim son tarihi " + maxTezTeslimSonTarih.Date.ToFormatDate() + " tarihinden önce olamaz.");
                        }

                    }


                }
                else tezTeslimSonTarih = null;
            }

            if (mmMessage.Messages.Count == 0)
            {
                bool sendMailSinav = srTalep.MezuniyetSinavDurumID != mezuniyetSinavDurumId && srTalep.MezuniyetSinavDurumID.HasValue;


                srTalep.MezuniyetSinavDurumID = mezuniyetSinavDurumId;
                srTalep.MezuniyetBasvurulari.MezuniyetSinavDurumID = mezuniyetSinavDurumId;
                srTalep.MezuniyetBasvurulari.TezTeslimSonTarih = tezTeslimSonTarih;
                srTalep.MezuniyetSinavDurumIslemTarihi = DateTime.Now;
                srTalep.MezuniyetBasvurulari.MezuniyetSinavDurumIslemTarihi = DateTime.Now;
                srTalep.MezuniyetSinavDurumIslemYapanID = UserIdentity.Current.Id;
                srTalep.MezuniyetBasvurulari.MezuniyetSinavDurumIslemYapanID = UserIdentity.Current.Id;
                _entities.SaveChanges();
                LogIslemleri.LogEkle("MezuniyetBasvurulari", LogCrudType.Update, srTalep.MezuniyetBasvurulari.ToJson());

                mmMessage.IsSuccess = true;


                if (sendMailSinav && new List<int> { MezuniyetSinavDurumEnum.Basarili, MezuniyetSinavDurumEnum.Uzatma }.Contains(srTalep.MezuniyetSinavDurumID.Value))
                {
                    mmMessage = MezuniyetBus.SendMailMezuniyetSinavSonucu(id, srTalep.MezuniyetSinavDurumID.Value);

                }
            }
            mmMessage.Title = "Sınav durumu kayıt işlemi";
            mmMessage.MessageType = mmMessage.IsSuccess ? MsgTypeEnum.Success : MsgTypeEnum.Warning;
            var strView = mmMessage.Messages.Count > 0 ? ViewRenderHelper.RenderPartialView("Ajax", "GetMessage", mmMessage) : "";

            return new
            {
                mmMessage.IsSuccess,
                Messages = strView
            }.ToJsonResult();
        }
        [Authorize(Roles = RoleNames.MezuniyetGelenBasvurularTezKontrolYetkiliAtama)]
        public ActionResult TezKontrolYetkilisiKaydet(int mezuniyetBasvurulariId, int? tezKontrolKullaniciId)
        {
            var mmMessage = new MmMessage
            {
                IsSuccess = false,
                Title = "Mezuniyet tez kontrol yetkilisi atama işlemi"
            };

            if (!RoleNames.MezuniyetGelenBasvurularTezKontrolYetkiliAtama.InRoleCurrent())
            {
                mmMessage.Messages.Add("Tez Kontrol Yetkilisi ataması için yetkiniz yok.");
            }
            else
            {
                var mezuniyetBasvurusu =
                    _entities.MezuniyetBasvurularis.First(p => p.MezuniyetBasvurulariID == mezuniyetBasvurulariId);

                if (mezuniyetBasvurusu.MezuniyetBasvurulariTezDosyalaris.All(a => a.IsOnaylandiOrDuzeltme == true))
                {
                    mmMessage.Messages.Add("Tezi onaylanan bir başvuru için Tez Kontrol Yetkilisi atayamazsınız.");
                }

                if (mmMessage.Messages.Count == 0)
                {
                    var sendMailKontrolyetkilisi = tezKontrolKullaniciId != mezuniyetBasvurusu.TezKontrolKullaniciID &&
                                                   tezKontrolKullaniciId.HasValue &&
                                                   mezuniyetBasvurusu.MezuniyetBasvurulariTezDosyalaris.Any();
                    mezuniyetBasvurusu.TezKontrolKullaniciID = tezKontrolKullaniciId;
                    mezuniyetBasvurusu.IslemYapanID = UserIdentity.Current.Id;
                    mezuniyetBasvurusu.IslemYapanIP = UserIdentity.Ip;
                    mezuniyetBasvurusu.IslemTarihi = DateTime.Now;
                    _entities.SaveChanges();
                    if (sendMailKontrolyetkilisi)
                    {
                        var mezuniyetBasvurulariTezDosyaId = mezuniyetBasvurusu.MezuniyetBasvurulariTezDosyalaris
                            .OrderByDescending(o => o.MezuniyetBasvurulariTezDosyaID)
                            .Select(s => s.MezuniyetBasvurulariTezDosyaID).First();
                        MezuniyetBus.SendMailMezuniyetTezSablonKontrol(mezuniyetBasvurulariTezDosyaId,
                            MailSablonTipiEnum.MezTezKontrolTezDosyasiYuklendi);
                    }

                    mmMessage.Messages.Add("Tez Kontrol Yetkilisi güncellendi.");
                    LogIslemleri.LogEkle("MezuniyetBasvurulari", "Tez Kontrol Yetkilisi Kayıt İşlemi",
                        LogCrudType.Update, mezuniyetBasvurusu.ToJson());
                    mmMessage.IsSuccess = true;
                }
            }

            return new
            {
                mmMessage.IsSuccess,
                MmMessage = mmMessage
            }.ToJsonResult();
        }
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult TdDurumKaydet(MezuniyetBasvurulariTezDosyalari kModel, int? sonTekKaynakOrani, int? sonToplamKaynakOrani)
        {
            var mmMessage = new MmMessage
            {
                Title = "Tez Kontrol Durum Kayıt İşlemi",
                IsSuccess = false
            };

            var talep = _entities.MezuniyetBasvurulariTezDosyalaris.First(p => p.MezuniyetBasvurulariTezDosyaID == kModel.MezuniyetBasvurulariTezDosyaID);
            var kYetki = RoleNames.MezuniyetGelenBasvurularTezKontrol.InRoleCurrent();
            if (!kYetki)
            {
                mmMessage.Messages.Add("Bu işlemi yapmaya yetkili değilsiniz.");
            }
            else if (kModel.IsOnaylandiOrDuzeltme.HasValue)
            {
                if (kModel.IsOnaylandiOrDuzeltme == false && kModel.Aciklama.IsNullOrWhiteSpace()) mmMessage.Messages.Add("Düzeltme talebi için açıklama giriniz.");
            }

            if (kModel.IsOnaylandiOrDuzeltme == true && (sonTekKaynakOrani.HasValue || sonToplamKaynakOrani.HasValue))
            {
                var ogrenimTipKrt =
                    talep.MezuniyetBasvurulari.MezuniyetSureci.MezuniyetSureciOgrenimTipKriterleris.FirstOrDefault(f =>
                        f.OgrenimTipKod == talep.MezuniyetBasvurulari.OgrenimTipKod);
                if (sonTekKaynakOrani.HasValue)
                {
                    if (sonTekKaynakOrani.Value > ogrenimTipKrt.TekKaynakOrani.Value || sonTekKaynakOrani.Value < 0)
                        mmMessage.Messages.Add($"En fazla Tek Kaynak Benzerlik Oranı bilgisi 0 ile {ogrenimTipKrt.TekKaynakOrani} değerleri arasında olmalıdır.");
                }
                if (!mmMessage.Messages.Any() && ogrenimTipKrt.ToplamKaynakOrani.HasValue)
                {
                    if (sonToplamKaynakOrani.Value > ogrenimTipKrt.ToplamKaynakOrani.Value || sonToplamKaynakOrani.Value < 0)
                        mmMessage.Messages.Add($"Toplam Benzerlik Oranı bilgisi 0 ile {ogrenimTipKrt.ToplamKaynakOrani} değerleri arasında olmalıdır.");
                }
            }
            if (mmMessage.Messages.Count == 0)
            {
                try
                {

                    if (kModel.IsOnaylandiOrDuzeltme.HasValue) talep.Aciklama = kModel.Aciklama.IsNullOrWhiteSpace() ? null : kModel.Aciklama.Trim();
                    else talep.Aciklama = null;

                    if (kModel.IsOnaylandiOrDuzeltme == true)
                    {
                        talep.MezuniyetBasvurulari.SonTekKaynakOrani = sonTekKaynakOrani;
                        talep.MezuniyetBasvurulari.SonToplamKaynakOrani = sonToplamKaynakOrani;
                    }

                    talep.IsOnaylandiOrDuzeltme = kModel.IsOnaylandiOrDuzeltme;
                    talep.OnayTarihi = DateTime.Now;
                    talep.OnayYapanID = UserIdentity.Current.Id;
                    talep.IslemTarihi = DateTime.Now;
                    talep.IslemYapanID = UserIdentity.Current.Id;
                    talep.IslemYapanIP = UserIdentity.Ip;

                    mmMessage.Messages.Add("Tez kontrol bilgisi kayıt edildi.");
                    _entities.SaveChanges();
                    LogIslemleri.LogEkle("MezuniyetBasvurulariTezDosyalari", LogCrudType.Update, talep.ToJson());
                    mmMessage.IsSuccess = true;
                    if (kModel.IsOnaylandiOrDuzeltme == true) mmMessage.Messages.AddRange(MezuniyetBus.SendMailMezuniyetTezSablonKontrol(talep.MezuniyetBasvurulariTezDosyaID, MailSablonTipiEnum.MezTezKontrolTezDosyasiBasarili, kModel.Aciklama).Messages);
                    else if (kModel.IsOnaylandiOrDuzeltme == false) mmMessage.Messages.AddRange(MezuniyetBus.SendMailMezuniyetTezSablonKontrol(talep.MezuniyetBasvurulariTezDosyaID, MailSablonTipiEnum.MezTezKontrolTezDosyasiOnaylanmadi, kModel.Aciklama).Messages);
                }
                catch (Exception ex)
                {
                    mmMessage.MessageType = MsgTypeEnum.Error;
                    mmMessage.IsSuccess = false;
                    mmMessage.Messages.Add("Tez dosyası kontrolü durum bilgisi kayıt edilirken bir hata oluştu! Hata:" + ex.ToExceptionMessage());
                    SistemBilgilendirmeBus.SistemBilgisiKaydet(ex.ToExceptionMessage(), ex.ToExceptionStackTrace(), BilgiTipiEnum.Kritik);
                }
            }
            mmMessage.MessageType = mmMessage.IsSuccess ? MsgTypeEnum.Success : MsgTypeEnum.Warning;
            var strView = mmMessage.Messages.Count > 0 ? ViewRenderHelper.RenderPartialView("Ajax", "GetMessage", mmMessage) : "";
            return new
            {
                mmMessage,
                mmMessage.IsSuccess,
                Messages = strView,
            }.ToJsonResult();
        }
        [Authorize(Roles = RoleNames.MezuniyetGelenBasvurularKayit)]
        public ActionResult MezuniyetDurumKaydet(int id, bool? isMezunOldu, int? sonTekKaynakOrani, int? sonToplamKaynakOrani, DateTime? tarih)
        {
            var mmMessage = new MmMessage
            {
                IsSuccess = false,
                Title = "Mezuniyet durumu kayıt işlemi",
                MessageType = MsgTypeEnum.Warning
            };

            var mBasvur = _entities.MezuniyetBasvurularis.First(p => p.MezuniyetBasvurulariID == id);
            var ogrenimTipKrt =
                mBasvur.MezuniyetSureci.MezuniyetSureciOgrenimTipKriterleris.FirstOrDefault(f =>
                    f.OgrenimTipKod == mBasvur.OgrenimTipKod);

            if (isMezunOldu == true)
            {

                if (mBasvur.MezuniyetBasvurulariTezTeslimFormlaris.Any() == false)
                {
                    mmMessage.Messages.Add("Öğrencinin mezun olabilmesi için Tez Teslim formunun oluşturulması gerekmektedir.");

                }
                else
                {

                    if (ogrenimTipKrt.TekKaynakOrani.HasValue)
                    {
                        if (!sonTekKaynakOrani.HasValue)
                            mmMessage.Messages.Add("Tek Kaynak Benzerlik Oranı bilgisi giriniz.");

                        else if (sonTekKaynakOrani.Value > ogrenimTipKrt.TekKaynakOrani.Value || sonTekKaynakOrani.Value < 0)
                            mmMessage.Messages.Add($"En fazla Tek Kaynak Benzerlik Oranı bilgisi 0 ile {ogrenimTipKrt.TekKaynakOrani} değerleri arasında olmalıdır.");
                    }
                    if (!mmMessage.Messages.Any() && ogrenimTipKrt.ToplamKaynakOrani.HasValue)
                    {
                        if (!sonToplamKaynakOrani.HasValue)
                            mmMessage.Messages.Add("Toplam Benzerlik Oranı bilgisi giriniz.");
                        else if (sonToplamKaynakOrani.Value > ogrenimTipKrt.ToplamKaynakOrani.Value || sonToplamKaynakOrani.Value < 0)
                            mmMessage.Messages.Add($"Toplam Benzerlik Oranı bilgisi 0 ile {ogrenimTipKrt.ToplamKaynakOrani} değerleri arasında olmalıdır.");
                    }

                    if (!mmMessage.Messages.Any() && tarih.HasValue == false)
                    {
                        mmMessage.Messages.Add("Mezuniyet Tarihi giriniz.");
                    }
                }


            }
            if (mmMessage.Messages.Count == 0)
            {
                if (isMezunOldu != true) tarih = null;

                mBasvur.IsMezunOldu = isMezunOldu;
                mBasvur.MezuniyetTarihi = tarih;
                if (ogrenimTipKrt.TekKaynakOrani.HasValue) mBasvur.SonTekKaynakOrani = sonTekKaynakOrani;
                if (ogrenimTipKrt.ToplamKaynakOrani.HasValue) mBasvur.SonToplamKaynakOrani = sonToplamKaynakOrani;

                var kul = mBasvur.Kullanicilar;
                if (mBasvur.ProgramKod == kul.ProgramKod && mBasvur.OgrenimTipKod == kul.OgrenimTipKod) kul.OgrenimDurumID = mBasvur.IsMezunOldu == true ? OgrenimDurumEnum.Mezun : OgrenimDurumEnum.HalenOğrenci;


                _entities.SaveChanges();
                LogIslemleri.LogEkle("MezuniyetBasvurulari", LogCrudType.Update, mBasvur.ToJson());

                mmMessage.IsSuccess = true;
            }
            var strView = ViewRenderHelper.RenderPartialView("Ajax", "GetMessage", mmMessage);

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
                if (mb.MezuniyetYayinKontrolDurumID != MezuniyetYayinKontrolDurumuEnum.KabulEdildi)
                {
                    mmMessage.Messages.Add("Mezuniyet başvuru durumu Kabul Edildi olan başvurularda işlem yapılabilir.");
                }
                else
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
                                mmMessage.Messages.Add(
                                    "Eyk tarihi öğrencinin almış olduğu ilk salon rezervasyonu tarihi için uygun değildir.");
                                mmMessage.Messages.Add("İlk salon rezervasyonu '" + ilkSrTalep.Tarih.ToFormatDate() +
                                                       "' tarihinde alınmıştır.");
                                mmMessage.Messages.Add("Belirlenen kurallara göre EYK tarihi en son '" +
                                                       maxT.ToFormatDate() + "' tarihi olabilir.");
                                mmMessage.IsSuccess = false;
                            }
                        }
                    }
                }

                if (mmMessage.Messages.Count == 0)
                {
                    mb.EYKTarihi = eykTarihi;
                    _entities.SaveChanges();
                    LogIslemleri.LogEkle("MezuniyetBasvurulari", LogCrudType.Update, mb.ToJson());
                    mmMessage.Messages.Add("Eyk Tarihi Güncellendi");
                    mmMessage.IsSuccess = true;
                }
            }
            else
            {
                mmMessage.Messages.Add("İşlem yapmaya çalıştığınız mezuniyet başvurusu sistemde bulunamadı!");
                mmMessage.IsSuccess = false;
            }
            mmMessage.MessageType = mmMessage.IsSuccess ? MsgTypeEnum.Success : MsgTypeEnum.Error;
            return new
            {
                mmMessage.IsSuccess,
                MmMessage = mmMessage,
            }.ToJsonResult();
        }
        [Authorize(Roles = RoleNames.MezuniyetGelenBasvurularKayit)]
        public ActionResult EykSayisiKaydet(int id, string eykSayisi)
        {
            var mmMessage = new MmMessage
            {
                Title = "EYK Sayısı Güncelleme İşlemi"
            };
            var mb = _entities.MezuniyetBasvurularis.FirstOrDefault(p => p.MezuniyetBasvurulariID == id);

            if (mb != null)
            {
                if (mb.MezuniyetYayinKontrolDurumID != MezuniyetYayinKontrolDurumuEnum.KabulEdildi)
                {
                    mmMessage.Messages.Add("Mezuniyet başvuru durumu Kabul Edildi olan başvurularda işlem yapılabilir.");
                }
                if (mmMessage.Messages.Count == 0)
                {
                    mb.EYKSayisi = eykSayisi;
                    _entities.SaveChanges();
                    LogIslemleri.LogEkle("MezuniyetBasvurulari", LogCrudType.Update, mb.ToJson());
                    mmMessage.Messages.Add("Eyk Sayısı Güncellendi");
                    mmMessage.IsSuccess = true;
                }
            }
            else
            {
                mmMessage.Messages.Add("İşlem yapmaya çalıştığınız mezuniyet başvurusu sistemde bulunamadı!");
                mmMessage.IsSuccess = false;
            }
            mmMessage.MessageType = mmMessage.IsSuccess ? MsgTypeEnum.Success : MsgTypeEnum.Error;
            return new
            {
                mmMessage.IsSuccess,
                MmMessage = mmMessage,
            }.ToJsonResult();
        }
        [Authorize(Roles = RoleNames.MezuniyetGelenBasvurularJuriOneriFormuEykOnay)]
        public ActionResult TezDiliKaydet(int id, bool isTezDiliTr)
        {
            var mmMessage = new MmMessage
            {
                Title = "Tez Dili Güncelleme İşlemi"
            };
            var mb = _entities.MezuniyetBasvurularis.FirstOrDefault(p => p.MezuniyetBasvurulariID == id);

            if (mb != null)
            {
                mb.IsTezDiliTr = isTezDiliTr;
                mb.IslemTarihi = DateTime.Now;
                mb.IslemYapanID = UserIdentity.Current.Id;
                mb.IslemYapanIP = UserIdentity.Ip;
                _entities.SaveChanges();
                LogIslemleri.LogEkle("MezuniyetBasvurulari", LogCrudType.Update, new { mb.IsTezDiliTr, mb.IslemTarihi, mb.IslemYapanID, mb.IslemYapanIP }.ToJson());
                mmMessage.Messages.Add("Tez Dili Günellendi");
                mmMessage.IsSuccess = true;

            }
            else
            {
                mmMessage.Messages.Add("İşlem yapmaya çalıştığınız mezuniyet başvurusu sistemde bulunamadı!");
                mmMessage.IsSuccess = false;
            }
            mmMessage.MessageType = mmMessage.IsSuccess ? MsgTypeEnum.Success : MsgTypeEnum.Error;
            return new
            {
                mmMessage.IsSuccess,
                MmMessage = mmMessage,
            }.ToJsonResult();
        }
        [Authorize(Roles = RoleNames.MezuniyetGelenBasvurularKayit)]
        public ActionResult SinavTarihiKaydet(int id, DateTime? sinavTarihi)
        {
            var mmMessage = new MmMessage
            {
                Title = "Sınav Tarihi Güncelleme İşlemi"
            };
            var srTalep = _entities.SRTalepleris.First(p => p.SRTalepID == id);
            var mb = srTalep.MezuniyetBasvurulari;
            if (mb.MezuniyetYayinKontrolDurumID != MezuniyetYayinKontrolDurumuEnum.KabulEdildi)
            {
                mmMessage.Messages.Add("Mezuniyet başvuru durumu Kabul Edildi olan başvurularda işlem yapılabilir.");
            }
            else if (!sinavTarihi.HasValue)
            {
                mmMessage.Messages.Add("Sınav Tarihi Giriniz.");
            }
            else
            {
                var otBilgiTarihBilgi = mb.MezuniyetSureci.MezuniyetSureciOgrenimTipKriterleris.First(p => p.OgrenimTipKod == mb.OgrenimTipKod);
                var uzatmaOncesiSrTalebi = mb.SRTalepleris.Where(p => p.MezuniyetSinavDurumID != MezuniyetSinavDurumEnum.Uzatma && p.SRDurumID == SrTalepDurumEnum.Onaylandı).OrderByDescending(o => o.SRTalepID).FirstOrDefault();
                if (uzatmaOncesiSrTalebi != null && (srTalep.MezuniyetSinavDurumID == MezuniyetSinavDurumEnum.Uzatma || srTalep.JuriSonucMezuniyetSinavDurumID == MezuniyetSinavDurumEnum.Uzatma && srTalep.SRDurumID == SrTalepDurumEnum.Onaylandı))
                {
                    var uzatmaOncesiSrAlabilmeTarihi = uzatmaOncesiSrTalebi.Tarih.AddMonths(otBilgiTarihBilgi.SinavUzatmaSinavAlmaSuresiMaxAy);
                    if (sinavTarihi.Value.Date > uzatmaOncesiSrAlabilmeTarihi)
                    {
                        mmMessage.Messages.Add("Mezuniyet sınavı sonucunda almış olduğunuz uzatma işlemi sonrası salon rezervasyonu işemi son tarihi olan '" + uzatmaOncesiSrAlabilmeTarihi.ToFormatDate() + "' tarihini aşamazsınız.");
                    }
                }
                else
                {
                    var srBaslangicTarih = mb.EYKTarihi.Value.AddDays(otBilgiTarihBilgi.SinavKacGunSonraAlabilir);
                    if (sinavTarihi.Value.Date < srBaslangicTarih.Date)
                    {

                        mmMessage.Messages.Add($"Sınav tarihi Eyk tarihi olan {mb.EYKTarihi.Value.Date.ToFormatDate()} tarihinden {otBilgiTarihBilgi.SinavKacGunSonraAlabilir} gün sonrasından büyük bir tarih olmalıdır. {srBaslangicTarih.Date.ToFormatDate()} tarihinden küçük olamaz");
                    }
                }

                if (mmMessage.Messages.Count == 0)
                {

                    srTalep.BasSaat = new TimeSpan(sinavTarihi.Value.Hour, sinavTarihi.Value.Minute, 0);
                    srTalep.BitSaat = new TimeSpan(sinavTarihi.Value.Hour + 2, sinavTarihi.Value.Minute, 0);
                    srTalep.Tarih = sinavTarihi.Value.Date;
                    _entities.SaveChanges();
                    LogIslemleri.LogEkle("SRTalepleri", LogCrudType.Update, srTalep.ToJson());
                    mmMessage.Messages.Add("Sınav Tarihi Güncellendi");
                    mmMessage.IsSuccess = true;
                }
            }

            mmMessage.MessageType = mmMessage.IsSuccess ? MsgTypeEnum.Success : MsgTypeEnum.Error;
            return new
            {
                mmMessage.IsSuccess,
                MmMessage = mmMessage,
            }.ToJsonResult();
        }

        [Authorize(Roles = RoleNames.MezuniyetGelenBasvurularKayit)]
        public ActionResult UzatmaSonrasiOgrenciTaahhutTarihiKaydet(int id, DateTime? taahhutSonTarih)
        {
            var mmMessage = new MmMessage
            {
                Title = "Uzatma sonrası öğrenci taahhütü son tarih güncellemesi"
            };
            var srTalep = _entities.SRTalepleris.First(p => p.SRTalepID == id);

            if (srTalep.MezuniyetBasvurulari.MezuniyetYayinKontrolDurumID != MezuniyetYayinKontrolDurumuEnum.KabulEdildi)
            {
                mmMessage.Messages.Add("Mezuniyet başvuru durumu Kabul Edildi olan başvurularda işlem yapılabilir.");
            }
            else if (taahhutSonTarih.HasValue && taahhutSonTarih.Value.Date < DateTime.Now.Date)
            {
                mmMessage.Messages.Add("Taahhüt onayı son tarihi günümüz tarihinden küçük olamaz!");
            }
            else if (taahhutSonTarih.HasValue && taahhutSonTarih.Value.Date < srTalep.Tarih.Date)
            {
                mmMessage.Messages.Add("Taahhüt onayı son tarihi uzatma alınan sınav tarihinden küçük olamaz!");
            }
            if (mmMessage.Messages.Count == 0)
            {
                srTalep.UzatmaSonrasiOgrenciTaahhutSonTarih = taahhutSonTarih?.Date;
                srTalep.UzatmaSonrasiOgrenciTaahhutSonTarihIslemTarihi = DateTime.Now;
                srTalep.UzatmaSonrasiOgrenciTaahhutSonTarihIslemYapanID = UserIdentity.Current.Id;

                _entities.SaveChanges();
                LogIslemleri.LogEkle("SRTalepleri", LogCrudType.Update, mmMessage.Title, srTalep.ToJson());
                mmMessage.Messages.Add("Uzatma sonrası öğrenci taahhütü son tarih kriteri güncellendi.");
                mmMessage.IsSuccess = true;
            }

            mmMessage.MessageType = mmMessage.IsSuccess ? MsgTypeEnum.Success : MsgTypeEnum.Error;
            return new
            {
                mmMessage.IsSuccess,
                MmMessage = mmMessage,
            }.ToJsonResult();
        }
        [Authorize(Roles = RoleNames.MezuniyetGelenBasvurularKayit)]
        public ActionResult UzatmaSonrasiSonSinavTarihiKaydet(int id, DateTime? sonSinavTarih)
        {
            var mmMessage = new MmMessage
            {
                Title = "Uzatma sonrası sınav talebi son tarih güncellemesi"
            };
            var srTalep = _entities.SRTalepleris.First(p => p.SRTalepID == id);

            if (srTalep.MezuniyetBasvurulari.MezuniyetYayinKontrolDurumID != MezuniyetYayinKontrolDurumuEnum.KabulEdildi)
            {
                var durumAdi = srTalep.MezuniyetBasvurulari.MezuniyetYayinKontrolDurumlari
                    .MezuniyetYayinKontrolDurumAdi;
                mmMessage.Messages.Add("Mezuniyet başvuru durumu Kabul Edildi olan başvurularda işlem yapılabilir. Başvuru durumunuz: '" + durumAdi + "' olarak gözükmektedir.");
            }
            else if (sonSinavTarih.HasValue && sonSinavTarih.Value.Date < DateTime.Now.Date)
            {
                mmMessage.Messages.Add("Uzatma sonrası sınav talebi son tarihi günümüz tarihinden küçük olamaz!");
            }
            else if (sonSinavTarih.HasValue && sonSinavTarih.Value.Date < srTalep.Tarih.Date)
            {
                mmMessage.Messages.Add("Uzatma sonrası sınav talebi son tarihi uzatma alınan sınav tarihinden küçük olamaz!");
            }

            if (mmMessage.Messages.Count == 0)
            {
                srTalep.UzatmaSonrasiYeniSinavTalebiSonTarih = sonSinavTarih?.Date;
                srTalep.UzatmaSonrasiYeniSinavTalebiSonTarihIslemTarihi = DateTime.Now;
                srTalep.UzatmaSonrasiYeniSinavTalebiSonTarihIslemYapanID = UserIdentity.Current.Id;

                _entities.SaveChanges();
                LogIslemleri.LogEkle("SRTalepleri", LogCrudType.Update, mmMessage.Title, srTalep.ToJson());
                mmMessage.Messages.Add("Uzatma sonrası sınav talebi son tarih kriteri güncellendi.");
                mmMessage.IsSuccess = true;
            }
            mmMessage.MessageType = mmMessage.IsSuccess ? MsgTypeEnum.Success : MsgTypeEnum.Error;
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
                Title = "Tez teslim son tarih kriteri güncelleme işlemi"
            };
            var srTalep = _entities.SRTalepleris.First(p => p.SRTalepID == id);
            var mb = srTalep.MezuniyetBasvurulari;
            var tarih = tezTeslimSonTarih.ToDate();
            if (srTalep.MezuniyetBasvurulari.MezuniyetYayinKontrolDurumID != MezuniyetYayinKontrolDurumuEnum.KabulEdildi)
            {
                mmMessage.Messages.Add("Mezuniyet başvuru durumu Kabul Edildi olan başvurularda işlem yapılabilir.");
            }

            if (mmMessage.Messages.Count == 0)
            {
                mb.TezTeslimSonTarih = tarih;

                _entities.SaveChanges();
                LogIslemleri.LogEkle("MezuniyetBasvurulari", LogCrudType.Update, mmMessage.Title, mb.ToJson());
                mmMessage.Messages.Add(!tarih.HasValue
                    ? "Tez teslim son tarihi kaldırıldı. (Sistem otomatik hesaplayacak)"
                    : "Tez Teslim son Tarih Kriteri Güncellendi");
                mmMessage.IsSuccess = true;
            }
            mmMessage.MessageType = mmMessage.IsSuccess ? MsgTypeEnum.Success : MsgTypeEnum.Error;
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
            var ogrenciInfo = KullanicilarBus.OgrenciKontrol(mb.OgrenciNo);

            var model = new MezuniyetJuriOneriFormuKayitDto
            {
                MezuniyetBasvurulariID = mezuniyetBasvurulariId,
                IsTezDiliTr = ogrenciInfo.IsTezDiliTr,
                TezBaslikTr = mb.TezBaslikTr,
                TezBaslikEn = mb.TezBaslikEn,
                Danisman = _entities.Kullanicilars.First(p => p.KullaniciID == mb.TezDanismanID),
                SListUnvanAdi = new SelectList(cmbUnvanList, "Value", "Caption"),
                IsDoktoraOrYL = mb.OgrenimTipKod.IsDoktora(),
            };

            var mMessage = new MmMessage
            {
                MessageType = MsgTypeEnum.Success,
                IsSuccess = true
            };
            var view = "";
            var mbjo = mb.MezuniyetJuriOneriFormlaris.FirstOrDefault();

            if (!RoleNames.MezuniyetGelenBasvurularJuriOneriFormuKayit.InRoleCurrent())
            {
                mMessage.MessageType = MsgTypeEnum.Warning;
                mMessage.Messages.Add("Jüri öneri formu kayıt işlemi için yetkili değilsiniz.");
            }
            else if (!RoleNames.MezuniyetGelenBasvurularKayit.InRoleCurrent() && mb.TezDanismanID != UserIdentity.Current.Id)
            {
                mMessage.MessageType = MsgTypeEnum.Warning;
                mMessage.Messages.Add("Bu mezuniyet başvurusu için danışman olarak belirlenmediğiniz için jüri öneri formu oluşturamazsınız.");
            }
            if (mMessage.Messages.Count == 0 && mbjo == null)
            {
                if (ogrenciInfo.Hata)
                {
                    mMessage.Messages.Add("Obs sisteminden öğrenci bilgisi sorgulanırken bir hata oluştu! " + ogrenciInfo.HataMsj);
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
                        mMessage.MessageType = MsgTypeEnum.Warning;
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
                        UniversiteAdi = s.UniversiteAdi,
                        AnabilimdaliProgramAdi = s.AnabilimdaliProgramAdi,
                        UzmanlikAlani = s.UzmanlikAlani,
                        IsAsilOrYedek = s.IsAsilOrYedek
                    }).ToList();
                    if (!ogrenciInfo.Hata)
                    {
                        if (!ogrenciInfo.OgrenciInfo.DANISMAN_AD_SOYAD1.IsNullOrWhiteSpace())
                        {
                            var tD = model.JoFormJuriList.First(p => p.JuriTipAdi == "TezDanismani");
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
                            varOlanTik1.SlistUnvanAdi = new SelectList(cmbUnvanList, "Value", "Caption", varOlanTik1.UnvanAdi);
                            if (varOlanTik1.AdSoyad.ToUpper() != obsTik1.TEZ_IZLEME_JURI_ADSOY.ToUpper() ||
                                varOlanTik1.UnvanAdi.ToUpper().Trim() != obsTik1.TEZ_IZLEME_JURI_UNVAN.ToJuriUnvanAdi())
                            {
                                varOlanTik1.AdSoyad = obsTik1.TEZ_IZLEME_JURI_ADSOY.ToUpper();
                                varOlanTik1.UnvanAdi = obsTik1.TEZ_IZLEME_JURI_UNVAN.ToJuriUnvanAdi();
                            }
                            var varOlanTik2 = model.JoFormJuriList.First(p => p.JuriTipAdi == "TikUyesi2");
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
                                EMail = !ogrenciInfo.OgrenciInfo.DANISMAN_EPOSTA1.ToIsValidEmail() ? "" : ogrenciInfo.OgrenciInfo.DANISMAN_EPOSTA1,
                                UniversiteAdi = "Yıldız Teknik Üniversitesi"

                            };
                            tdBilgi.SlistUnvanAdi = new SelectList(cmbUnvanList, "Value", "Caption", tdBilgi.UnvanAdi);
                            model.JoFormJuriList.Add(tdBilgi);
                        }
                        else
                        {
                            model.JoFormJuriList.Add(new KrMezuniyetJuriOneriFormuJurileri { JuriTipAdi = "TezDanismani", SlistUnvanAdi = new SelectList(cmbUnvanList, "Value", "Caption") });
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
                                UnvanAdi = obsTik1.TEZ_IZLEME_JURI_UNVAN.ToJuriUnvanAdi(),
                                EMail = !obsTik1.TEZ_IZLEME_JURI_EPOSTA.ToIsValidEmail() ? "" : obsTik1.TEZ_IZLEME_JURI_EPOSTA,
                                UniversiteAdi = obsTik1.TEZ_IZLEME_JURI_UNIVER,
                                AnabilimdaliProgramAdi = obsTik1.TEZ_IZLEME_JURI_ANABLMDAL
                            };
                            tk1Bilgi.SlistUnvanAdi =
                                new SelectList(cmbUnvanList, "Value", "Caption", tk1Bilgi.UnvanAdi);
                            model.JoFormJuriList.Add(tk1Bilgi);



                            var tk2Bilgi = new KrMezuniyetJuriOneriFormuJurileri
                            {
                                JuriTipAdi = "TikUyesi2",
                                AdSoyad = obsTik2.TEZ_IZLEME_JURI_ADSOY,
                                UnvanAdi = obsTik2.TEZ_IZLEME_JURI_UNVAN.ToJuriUnvanAdi(),
                                EMail = !obsTik2.TEZ_IZLEME_JURI_EPOSTA.ToIsValidEmail() ? "" : obsTik2.TEZ_IZLEME_JURI_EPOSTA,
                                UniversiteAdi = obsTik2.TEZ_IZLEME_JURI_UNIVER,
                                AnabilimdaliProgramAdi = obsTik2.TEZ_IZLEME_JURI_ANABLMDAL
                            };
                            tk2Bilgi.SlistUnvanAdi =
                                new SelectList(cmbUnvanList, "Value", "Caption", tk2Bilgi.UnvanAdi);
                            model.JoFormJuriList.Add(tk2Bilgi);

                        }
                    }
                }


                view = ViewRenderHelper.RenderPartialView("MezuniyetGelenBasvurular", "JuriOneriFormu", model);
            }
            else { mMessage.IsSuccess = false; mMessage.MessageType = MsgTypeEnum.Warning; }
            var strView = ViewRenderHelper.RenderPartialView("Ajax", "GetMessage", mMessage);

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
                MessageType = MsgTypeEnum.Success,
                IsSuccess = true
            };
            string selectedAnaTabAdi = "";
            string selectedDetayTabAdi = "";
            bool isYeniJo = true;

            var mb = _entities.MezuniyetBasvurularis.First(p => p.MezuniyetBasvurulariID == kModel.MezuniyetBasvurulariID);
            if (!RoleNames.MezuniyetGelenBasvurularJuriOneriFormuKayit.InRoleCurrent())
            {
                mMessage.Messages.Add("Jüri öneri formu kayıt işlemi için yetkili değilsiniz.");
            }
            else if (!RoleNames.MezuniyetGelenBasvurularKayit.InRoleCurrent() && mb.TezDanismanID != UserIdentity.Current.Id)
            {
                mMessage.IsSuccess = false;
                mMessage.MessageType = MsgTypeEnum.Warning;
                mMessage.Messages.Add("Bu mezuniyet başvurusu için danışman olarak belirlenmediğiniz için jüri öneri formu oluşturamazsınız.");
            }
            else if (mb.MezuniyetYayinKontrolDurumID != MezuniyetYayinKontrolDurumuEnum.KabulEdildi)
            {
                mMessage.Messages.Add("Mezuniyet başvuru durumu Kabul Edildi olan başvurularda işlem yapılabilir.");
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
                            mMessage.MessagesDialog.Add(new MrMessage { MessageType = (!kModel.YeniTezBaslikTr.IsNullOrWhiteSpace() ? MsgTypeEnum.Success : MsgTypeEnum.Warning), PropertyName = "YeniTezBaslikTr" });
                            if (kModel.YeniTezBaslikEn.IsNullOrWhiteSpace())
                            {
                                mMessage.Messages.Add("Yeni Tez Başlığı İngilizce bilgisini giriniz.");
                            }
                            mMessage.MessagesDialog.Add(new MrMessage { MessageType = (!kModel.YeniTezBaslikEn.IsNullOrWhiteSpace() ? MsgTypeEnum.Success : MsgTypeEnum.Warning), PropertyName = "YeniTezBaslikEn" });
                        }
                    }
                    mMessage.MessagesDialog.Add(new MrMessage { MessageType = (kModel.IsTezBasligiDegisti.HasValue ? MsgTypeEnum.Success : MsgTypeEnum.Warning), PropertyName = "IsTezBasligiDegisti" });
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
                    var universiteAdis = kModel.UniversiteAdi.Select((s, i) => new { UniversiteAdi = s, Inx = (i + 1) }).ToList();
                    var anabilimdaliProgramAdis = kModel.AnabilimdaliProgramAdi.Select((s, i) => new { AnabilimdaliProgramAdi = s, Inx = (i + 1) }).ToList();
                    var uzmanlikAlanis = kModel.UzmanlikAlani.Select((s, i) => new { UzmanlikAlani = s, Inx = (i + 1) }).ToList();


                    var qData = (from ad in adSoyads
                                 join at in anaTabAdis on ad.Inx equals at.Inx
                                 join dt in detayTabAdis on ad.Inx equals dt.Inx
                                 join jt in juriTipAdis on ad.Inx equals jt.Inx
                                 join un in unvanAdis on ad.Inx equals un.Inx
                                 join em in eMails on ad.Inx equals em.Inx
                                 join uni in universiteAdis on ad.Inx equals uni.Inx
                                 join abd in anabilimdaliProgramAdis on ad.Inx equals abd.Inx
                                 join ua in uzmanlikAlanis on ad.Inx equals ua.Inx

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
                                     EMailSuccess = !em.EMail.IsNullOrWhiteSpace() && em.EMail.ToIsValidEmail(),
                                     uni.UniversiteAdi,
                                     UniversiteAdiSuccess = !uni.UniversiteAdi.IsNullOrWhiteSpace(),
                                     abd.AnabilimdaliProgramAdi,
                                     AnabilimdaliProgramAdiSuccess = !abd.AnabilimdaliProgramAdi.IsNullOrWhiteSpace(),
                                     ua.UzmanlikAlani,
                                     UzmanlikAlaniSuccess = !ua.UzmanlikAlani.IsNullOrWhiteSpace()
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
                                      s.UniversiteAdiSuccess,
                                      s.UzmanlikAlaniSuccess,
                                      IsSuccessRow = s.JuriTipAdi.ToJoFormSuccessRow(kModel.IsTezDiliTr, s.AdSoyadSuccess, s.UnvanAdiSuccess, s.EMailSuccess, s.UniversiteAdiSuccess, s.UzmanlikAlaniSuccess)
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
                            mMessage.MessagesDialog.Add(new MrMessage { MessageType = (item2.s.AdSoyadSuccess ? MsgTypeEnum.Success : MsgTypeEnum.Warning), PropertyName = item2.s.JuriTipAdi + "AdSoyad" });
                            mMessage.MessagesDialog.Add(new MrMessage { MessageType = (item2.s.UnvanAdiSuccess ? MsgTypeEnum.Success : MsgTypeEnum.Warning), PropertyName = item2.s.JuriTipAdi + "UnvanAdi" });
                            mMessage.MessagesDialog.Add(new MrMessage { MessageType = (item2.s.EMailSuccess ? MsgTypeEnum.Success : MsgTypeEnum.Warning), PropertyName = item2.s.JuriTipAdi + "EMail" });
                            mMessage.MessagesDialog.Add(new MrMessage { MessageType = (item2.s.UniversiteAdiSuccess ? MsgTypeEnum.Success : MsgTypeEnum.Warning), PropertyName = item2.s.JuriTipAdi + "UniversiteAdi" });
                            mMessage.MessagesDialog.Add(new MrMessage { MessageType = (item2.s.AnabilimdaliProgramAdiSuccess ? MsgTypeEnum.Success : MsgTypeEnum.Warning), PropertyName = item2.s.JuriTipAdi + "AnabilimdaliProgramAdi" });
                            mMessage.MessagesDialog.Add(new MrMessage { MessageType = (item2.s.UzmanlikAlaniSuccess ? MsgTypeEnum.Success : MsgTypeEnum.Warning), PropertyName = item2.s.JuriTipAdi + "UzmanlikAlani" });

                        }

                    }
                    if (mMessage.Messages.Count == 0 && saveData)
                    {
                        var unvanlar = qGroup.SelectMany(s => s.DetayData)
                            .Select(s => s.s.UnvanAdi)
                            .Where(x => !string.IsNullOrEmpty(x))
                            .ToList();

                        // Dinamik olarak kontrol edilmesi gereken unvanlar
                        var gerekliUnvanlar = new List<string>
                        {
                            UnvanlarBus.ProfDr,
                            UnvanlarBus.DocDr
                            // Gerekirse buraya başka unvanlar da eklenebilir
                        };

                        // Gerekli unvanlardan herhangi biri mevcut mu?
                        bool kriterSaglanmis = unvanlar.Intersect(gerekliUnvanlar).Any();

                        if (!kriterSaglanmis)
                        {
                            string unvanListesi = string.Join(" veya ", gerekliUnvanlar);
                            mMessage.Messages.Add($"Jüri öneri formu oluşturulabilmesi için önerilen jüri üyeleri arasında en az bir adet {unvanListesi} unvanına sahip kişi bulunmalıdır.");
                        }
                    }
                    if (mMessage.Messages.Count == 0 && saveData)
                    {
                        var juriUyeleri = qGroup.SelectMany(s => s.DetayData)
                            .Where(d => !string.IsNullOrWhiteSpace(d.s.AdSoyad) && !string.IsNullOrWhiteSpace(d.s.UnvanAdi))
                            .Select(d => new
                            {
                                AdSoyad = d.s.AdSoyad.Trim().ToUpper(),
                                UnvanAdi = d.s.UnvanAdi.Trim().ToUpper(),
                                EMail = d.s.EMail?.Trim().ToLower(),
                                JuriTipAdi = d.s.JuriTipAdi,
                                KisiKimlik = $"{d.s.AdSoyad.Trim().ToUpper()} - {d.s.UnvanAdi.Trim().ToUpper()}"
                            })
                            .ToList();

                        var mukerrerKisiler = juriUyeleri
                            .GroupBy(j => new { j.AdSoyad, j.UnvanAdi })
                            .Where(g => g.Count() > 1)
                            .Select(g => new
                            {
                                AdSoyad = g.Key.AdSoyad,
                                UnvanAdi = g.Key.UnvanAdi,
                                KisiKimlik = $"{g.Key.AdSoyad} - {g.Key.UnvanAdi}",
                                JuriTipleri = g.Select(x => x.JuriTipAdi).ToList(),
                                EMailler = g.Select(x => x.EMail).Where(e => !string.IsNullOrWhiteSpace(e)).Distinct().ToList()
                            })
                            .ToList();

                        foreach (var mukerrerKisi in mukerrerKisiler)
                        {
                            var juriTipAdlari = string.Join(", ", mukerrerKisi.JuriTipleri.Select(GetJuriTipDisplayName));
                            mMessage.Messages.Add($"Aynı kişi ({mukerrerKisi.KisiKimlik}) birden fazla jüri pozisyonu için önerilmiş: {juriTipAdlari}");
                        }

                        if (mukerrerKisiler.Any())
                        {
                            if (selectedAnaTabAdi == "")
                            {
                                var ilkMukerrer = mukerrerKisiler.FirstOrDefault()?.JuriTipleri.FirstOrDefault();

                                if (!string.IsNullOrEmpty(ilkMukerrer))
                                {
                                    var ilkMukerrerItem = qGroup.FirstOrDefault(q => q.DetayData.Any(d => d.s.JuriTipAdi == ilkMukerrer));
                                    if (ilkMukerrerItem != null)
                                    {
                                        selectedAnaTabAdi = ilkMukerrerItem.AnaTabAdi;
                                        selectedDetayTabAdi = ilkMukerrerItem.DetayTabAdi;
                                    }
                                }
                            }
                        }
                    }
                    if (mMessage.Messages.Count == 0 && saveData)
                    {
                        mbjo = isYeniJo ? new MezuniyetJuriOneriFormlari() : mbjo;
                        //doktora öğrenim tipindeki başvurular için tik üyesi haricindeki bilgiler alınsın
                        var kData = qData.Where(p => p.JuriTipAdi != (mb.OgrenimTipKod.IsDoktora() ? "TikUyesi" : "")).ToList();
                        foreach (var item in kData)
                        {
                            var rw = mbjo.MezuniyetJuriOneriFormuJurileris.FirstOrDefault(p => p.JuriTipAdi == item.JuriTipAdi);
                            if (rw != null)
                            {
                                if (item.AdSoyad.IsNullOrWhiteSpace() == false)
                                {
                                    if (rw.AdSoyad != item.AdSoyad || rw.UnvanAdi != item.UnvanAdi || rw.EMail != item.EMail || rw.UniversiteAdi != item.UniversiteAdi || rw.UzmanlikAlani != item.UzmanlikAlani) isDegisiklikVar = true;
                                    rw.UnvanAdi = item.UnvanAdi.ToUpper();
                                    rw.AdSoyad = item.AdSoyad.ToUpper();
                                    rw.EMail = item.EMail;
                                    rw.UniversiteAdi = item.UniversiteAdi;
                                    rw.AnabilimdaliProgramAdi = item.AnabilimdaliProgramAdi;
                                    rw.UzmanlikAlani = item.UzmanlikAlani;

                                    var isAsil = new List<string> { "TezDanismani", "TikUyesi1", "TikUyesi2" }.Contains(item.JuriTipAdi);
                                    if ((rw.AdSoyad != item.AdSoyad && rw.EMail != item.EMail) || isAsil) rw.IsAsilOrYedek = isAsil ? true : (bool?)null;
                                }
                                else _entities.MezuniyetJuriOneriFormuJurileris.Remove(rw);
                            }
                            else if (item.AdSoyad.IsNullOrWhiteSpace() == false)
                            {
                                mbjo.MezuniyetJuriOneriFormuJurileris.Add(
                                   new MezuniyetJuriOneriFormuJurileri
                                   {
                                       JuriTipAdi = item.JuriTipAdi,
                                       UnvanAdi = item.UnvanAdi.ToUpper(),
                                       AdSoyad = item.AdSoyad.ToUpper(),
                                       EMail = item.EMail,
                                       UniversiteAdi = item.UniversiteAdi,
                                       AnabilimdaliProgramAdi = item.AnabilimdaliProgramAdi,
                                       UzmanlikAlani = item.UzmanlikAlani,
                                       IsAsilOrYedek = new List<string> { "TezDanismani", "TikUyesi1", "TikUyesi2" }.Contains(item.JuriTipAdi) ? true : (bool?)null

                                   });
                            }
                        }
                        if (isYeniJo || isDegisiklikVar || _entities.MezuniyetJuriOneriFormuJurileris.Count(p => p.MezuniyetJuriOneriFormID == kModel.MezuniyetJuriOneriFormID) != kData.Count(p => p.AdSoyad.IsNullOrWhiteSpace() == false))
                        {
                            var uniqueId = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 8).ToUpper();
                            while (_entities.MezuniyetJuriOneriFormlaris.Any(a => a.UniqueID == uniqueId))
                            {
                                uniqueId = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 8).ToUpper();
                            }
                            mbjo.UniqueID = uniqueId;
                        }
                        mbjo.MezuniyetBasvurulariID = kModel.MezuniyetBasvurulariID;
                        if (mb.IsTezDiliTr.HasValue && mb.IsTezDiliTr != kModel.IsTezDiliTr)
                        {
                            // tez dili değişti ve mezuniuyet başvurusundaki tez dilinden farklı ise mezuniyet başvurusundaki başlıkları kontrol et ve tez dilini güncelle

                            mb.TezBaslikTr = mb.TezBaslikTr.IsNullOrWhiteSpace() ? mb.TezBaslikEn : mb.TezBaslikTr;
                            mb.TezBaslikEn = mb.TezBaslikEn.IsNullOrWhiteSpace() ? mb.TezBaslikTr : mb.TezBaslikEn;
                            mb.IsTezDiliTr = kModel.IsTezDiliTr;
                        }
                        mbjo.IsTezBasligiDegisti = kModel.IsTezBasligiDegisti;
                        mbjo.YeniTezBaslikTr = kModel.IsTezBasligiDegisti == true ? kModel.YeniTezBaslikTr : null;
                        mbjo.YeniTezBaslikEn = kModel.IsTezBasligiDegisti == true ? kModel.YeniTezBaslikEn : null;


                        if (!RoleNames.MezuniyetGelenBasvurularJuriOneriFormuOnay.InRoleCurrent())
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
                            SistemBilgilendirmeBus.SistemBilgisiKaydet(hataMsj, ObjectExtensions.GetCurrentMethodPath(), BilgiTipiEnum.Hata);
                        }


                    }
                }

            }
            mMessage.IsSuccess = saveData && mMessage.Messages.Count == 0;
            if (mMessage.Messages.Count > 0)
            {
                mMessage.Title = "Jüri Öneri Formu Aşağıdaki Sebeplerden Dolayı Oluşturulamadı.";
                mMessage.IsSuccess = false;
                mMessage.MessageType = MsgTypeEnum.Warning;
            }
            return new
            {
                mMessage,
                IsYeniJO = isYeniJo,
                SelectedAnaTabAdi = selectedAnaTabAdi,
                SelectedDetayTabAdi = selectedDetayTabAdi
            }.ToJsonResult();
        }
        private string GetJuriTipDisplayName(string juriTipAdi)
        {
            switch (juriTipAdi)
            {
                case "TezDanismani":
                    return "Danışman";
                case "TikUyesi1":
                    return "Tik Üyesi 1";
                case "TikUyesi2":
                    return "Tik Üyesi 2";
                case "YtuIciJuri1":
                    return "YTÜ İçi Jüri 1";
                case "YtuIciJuri2":
                    return "YTÜ İçi Jüri 2";
                case "YtuIciJuri3":
                    return "YTÜ İçi Jüri 3";
                case "YtuIciJuri4":
                    return "YTÜ İçi Jüri 4";
                case "YtuDisiJuri1":
                    return "YTÜ Dışı Jüri 1";
                case "YtuDisiJuri2":
                    return "YTÜ Dışı Jüri 2";
                case "YtuDisiJuri3":
                    return "YTÜ Dışı Jüri 3";
                case "YtuDisiJuri4":
                    return "YTÜ Dışı Jüri 4";
                default:
                    return juriTipAdi;
            }
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
                MessageType = MsgTypeEnum.Warning
            };

            var mb = _entities.MezuniyetBasvurularis.First(p => p.MezuniyetBasvurulariID == id);
            var juriOneriFormu = mb.MezuniyetJuriOneriFormlaris.FirstOrDefault(p => p.MezuniyetJuriOneriFormID == mezuniyetJuriOneriFormId);

            if (!RoleNames.MezuniyetGelenBasvurularJuriOneriFormuEykOnay.InRoleCurrent())
            {
                mmMessage.Messages.Add("Jüri öneri formunda Asil/Yedek jüri adayı seçimi yetkisine sahip değilsiniz!");
            }
            else if (juriOneriFormu == null)
            {
                mmMessage.Messages.Add("İşlem yapılmak istenen jüri öneri formu sistemde bulunamadı!");
            }
            else if (mb.MezuniyetYayinKontrolDurumID != MezuniyetYayinKontrolDurumuEnum.KabulEdildi)
            {
                mmMessage.Messages.Add("Mezuniyet başvuru durumu Kabul Edildi olan başvurularda işlem yapılabilir.");
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
            var strView = ViewRenderHelper.RenderPartialView("Ajax", "GetMessage", mmMessage);
            return new
            {
                mmMessage.IsSuccess,
                Messages = strView
            }.ToJsonResult();
        }



        public ActionResult EykYaGonderimPost(int mezuniyetJuriOneriFormId, bool? eykYaGonderildi, string eykSayisi, string eykYaGonderimDurumAciklamasi)
        {
            var mMessage = new MmMessage
            {
                MessageType = MsgTypeEnum.Success,
                Title = "Jüri Öneri Formu EYK'ya Gönderim İşlemi"
            };
            var juriOneriFormu = _entities.MezuniyetJuriOneriFormlaris.First(p => p.MezuniyetJuriOneriFormID == mezuniyetJuriOneriFormId);



            if (!RoleNames.MezuniyetGelenBasvurularJuriOneriFormuOnay.InRoleCurrent())
            {
                mMessage.Messages.Add("EYK'ya gönderme yetkiniz bulunmamaktadır.");
            }
            else if (juriOneriFormu.MezuniyetBasvurulari.EYKTarihi.HasValue && juriOneriFormu.MezuniyetBasvurulari.SRTalepleris.Any(a => a.SRDurumID == SrTalepDurumEnum.Onaylandı))
            {
                mMessage.Messages.Add("Mezuniyet başvurusuna ait Salon rezervasyonu alındığı için jüri öneri formu EYK'ya gönderim işlemi yapılamaz.");
            }
            else if (juriOneriFormu.EYKDaOnaylandi.HasValue)
            {
                mMessage.Messages.Add("Enstitü tarafından EYK'da onaylandı işlemi yapılan jüri öneri formu üzerinden EYK'ya gönderim işlemi yapılamaz.");
            }
            else if (juriOneriFormu.EYKYaHazirlandi.HasValue)
            {
                mMessage.Messages.Add("Enstitü tarafından EYK'ya hazırlanan jüri öneri formu üzerinden EYK'ya gönderim işlemi yapılamaz.");
            }
            else if (eykYaGonderildi == false && eykYaGonderimDurumAciklamasi.IsNullOrWhiteSpace())
            {
                mMessage.Messages.Add("EYK'ya gönderilmeme sebebi açıklaması giriniz.");
            }

            if (eykYaGonderildi == true && eykSayisi.IsNullOrWhiteSpace())
            {
                mMessage.Messages.Add("EYK Sayısı giriniz.");
            }

            if (!mMessage.Messages.Any())
            {


                if (juriOneriFormu.EYKYaGonderildi.HasValue || eykYaGonderildi != false)
                {

                    var ogrenciObsBilgi =
                        KullanicilarBus.OgrenciBilgisiGuncelleObs(juriOneriFormu.MezuniyetBasvurulari.KullaniciID);

                    if (!ogrenciObsBilgi.KayitVar)
                    {
                        mMessage.Messages.Add(
                            "Öğrenci OBS sisteminde aktif öğrenci olarak gözükmemektedir. Onay işlemi yapılamaz.");
                    }
                    else if (juriOneriFormu.MezuniyetBasvurulari.OgrenciNo != ogrenciObsBilgi.OgrenciInfo.OGR_NO)
                    {
                        mMessage.Messages.Add(
                            "Ana başvurunuzdaki öğrenci numarası ile güncel öğrenci numarası uyuşmuyor. Öğrencinin kaydı silinip farklı bir programa kaydolmuş olabilir ya da numarası değişmiş olabilir.");
                    }
                }
            }
            if (!mMessage.Messages.Any())
            {
                var sendMail = juriOneriFormu.EYKYaGonderildi != eykYaGonderildi && eykYaGonderildi == false;
                juriOneriFormu.EYKYaGonderildi = eykYaGonderildi;
                juriOneriFormu.MezuniyetBasvurulari.EYKSayisi = eykSayisi;
                juriOneriFormu.EYKYaGonderildiIslemTarihi = DateTime.Now;
                juriOneriFormu.EYKYaGonderildiIslemYapanID = UserIdentity.Current.Id;
                juriOneriFormu.EYKYaGonderimDurumAciklamasi =
                    juriOneriFormu.EYKYaGonderildi == false ? eykYaGonderimDurumAciklamasi : "";
                juriOneriFormu.IslemTarihi = DateTime.Now;
                juriOneriFormu.IslemYapanID = UserIdentity.Current.Id;
                juriOneriFormu.IslemYapanIP = UserIdentity.Ip;
                _entities.SaveChanges();
                LogIslemleri.LogEkle("MezuniyetJuriOneriFormlari", LogCrudType.Update, juriOneriFormu.ToJson());
                if (sendMail)
                {
                    MezuniyetBus.SendMailJuriOneriFormuEykYaGonderimRet(mezuniyetJuriOneriFormId);
                }
                mMessage.IsSuccess = true;
            }

            mMessage.MessageType = mMessage.IsSuccess ? MsgTypeEnum.Success : MsgTypeEnum.Error;
            return mMessage.ToJsonResult();
        }

        public ActionResult EykYaHazirlaPost(int mezuniyetJuriOneriFormId, bool? eykYaHazirlandi, string eykSayisi)
        {
            var mMessage = new MmMessage
            {
                MessageType = MsgTypeEnum.Success,
                Title = "Jüri Öneri Formu EYK'ya Hazırlama İşlemi"
            };
            var juriOneriFormu = _entities.MezuniyetJuriOneriFormlaris.First(p => p.MezuniyetJuriOneriFormID == mezuniyetJuriOneriFormId);
            if (!RoleNames.TdoEykyaGonderimYetkisi.InRoleCurrent())
            {
                mMessage.Messages.Add("EYK'ya hazırlama yetkiniz bulunmamaktadır.");
            }
            else if (juriOneriFormu.EYKYaGonderildi != true)
            {
                mMessage.Messages.Add("Enstitü tarafından Eyk'ya gönderim işlemi yapılmayan Danışman öneri formu üzerinden EYK'ya hazırlandı işlemi yapılamaz.");
            }
            else if (juriOneriFormu.EYKDaOnaylandi.HasValue)
            {
                mMessage.Messages.Add("Enstitü tarafından EYK'da onaylandı işlemi yapılan Danışman öneri formu üzerinden EYK'ya hazırlandı işlemi yapılamaz.");
            }
            if (eykYaHazirlandi == true && eykSayisi.IsNullOrWhiteSpace())
            {
                mMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Error, PropertyName = "EYKDaOnaySayi_" + mezuniyetJuriOneriFormId });
                mMessage.Messages.Add("EYK Sayısı giriniz.");
            }
            if (!mMessage.Messages.Any())
            {
                if (eykYaHazirlandi == true)
                {
                    var ogrenciObsBilgi =
                        KullanicilarBus.OgrenciBilgisiGuncelleObs(juriOneriFormu.MezuniyetBasvurulari.KullaniciID);

                    if (!ogrenciObsBilgi.KayitVar)
                    {
                        mMessage.Messages.Add(
                            "Öğrenci OBS sisteminde aktif öğrenci olarak gözükmemektedir. Onay işlemi yapılamaz.");
                    }
                    else if (juriOneriFormu.MezuniyetBasvurulari.OgrenciNo != ogrenciObsBilgi.OgrenciInfo.OGR_NO)
                    {
                        mMessage.Messages.Add(
                            "Ana başvurunuzdaki öğrenci numarası ile güncel öğrenci numarası uyuşmuyor. Öğrencinin kaydı silinip farklı bir programa kaydolmuş olabilir ya da numarası değişmiş olabilir.");
                    }
                }
            }

            if (!mMessage.Messages.Any())
            {

                juriOneriFormu.EYKYaHazirlandi = eykYaHazirlandi;
                juriOneriFormu.MezuniyetBasvurulari.EYKSayisi = eykSayisi;
                juriOneriFormu.EYKYaHazirlandiIslemTarihi = DateTime.Now;
                juriOneriFormu.EYKYaHazirlandiIslemYapanID = UserIdentity.Current.Id;
                juriOneriFormu.IslemTarihi = DateTime.Now;
                juriOneriFormu.IslemYapanID = UserIdentity.Current.Id;
                juriOneriFormu.IslemYapanIP = UserIdentity.Ip;
                _entities.SaveChanges();
                LogIslemleri.LogEkle("MezuniyetJuriOneriFormlari", LogCrudType.Update, juriOneriFormu.ToJson());
                mMessage.IsSuccess = true;
            }

            mMessage.MessageType = mMessage.IsSuccess ? MsgTypeEnum.Success : MsgTypeEnum.Error;
            return mMessage.ToJsonResult();
        }
        public ActionResult EykDaOnayPost(int mezuniyetJuriOneriFormId, bool? eykDaOnaylandi, DateTime? eykDaOnaylandiOnayTarihi, string eykSayisi, string eykDaOnaylanmadiDurumAciklamasi)
        {
            var mMessage = new MmMessage
            {
                MessageType = MsgTypeEnum.Success,
                Title = "Jüri Öneri Formu EYK'Da Onay İşlemi"
            };
            var juriOneriFormu = _entities.MezuniyetJuriOneriFormlaris.First(p => p.MezuniyetJuriOneriFormID == mezuniyetJuriOneriFormId);
            if (!RoleNames.TdoEykyaGonderimYetkisi.InRoleCurrent())
            {
                mMessage.Messages.Add("EYK'da onay yetkiniz bulunmamaktadır.");
            }
            else if (juriOneriFormu.EYKYaGonderildi != true)
            {
                mMessage.Messages.Add("Enstitü tarafından EYK'ya gönderildi işlemi yapılmayan Danışman öneri formu üzerinden EYK onay işlemi yapılamaz.");
            }
            else if (juriOneriFormu.MezuniyetBasvurulari.EYKTarihi.HasValue && juriOneriFormu.MezuniyetBasvurulari.SRTalepleris.Any(a => a.SRDurumID == SrTalepDurumEnum.Onaylandı))
            {
                mMessage.Messages.Add("Mezuniyet başvurusuna ait Salon rezervasyonu alındığı için jüri öneri formu EYK'ya gönderim işlemi yapılamaz.");
            }
            if (eykDaOnaylandi == false)
            {
                if (eykDaOnaylanmadiDurumAciklamasi.IsNullOrWhiteSpace())
                {
                    mMessage.Messages.Add("Onaylanmama durumu için açıklama giriniz.");

                }
                mMessage.MessagesDialog.Add(new MrMessage { MessageType = eykSayisi.IsNullOrWhiteSpace() ? MsgTypeEnum.Error : MsgTypeEnum.Success, PropertyName = "EYKDaOnaylanmadiDurumAciklamasi_" + mezuniyetJuriOneriFormId });

            }
            else mMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Nothing, PropertyName = "EYKDaOnaylanmadiDurumAciklamasi_" + mezuniyetJuriOneriFormId });

            if (eykDaOnaylandi == true)
            {
                if (!eykDaOnaylandiOnayTarihi.HasValue)
                {
                    mMessage.Messages.Add("EYK'Da onaylanma tarihini giriniz.");
                }
                mMessage.MessagesDialog.Add(new MrMessage { MessageType = eykSayisi.IsNullOrWhiteSpace() ? MsgTypeEnum.Error : MsgTypeEnum.Success, PropertyName = "EYKDaOnaylandiOnayTarihi_" + mezuniyetJuriOneriFormId });
                if (eykSayisi.IsNullOrWhiteSpace())
                {
                    mMessage.Messages.Add("EYK Sayısı Giriniz.");
                }
                mMessage.MessagesDialog.Add(new MrMessage { MessageType = eykSayisi.IsNullOrWhiteSpace() ? MsgTypeEnum.Error : MsgTypeEnum.Success, PropertyName = "EYKSayisiO_" + mezuniyetJuriOneriFormId });
            }
            if (!mMessage.Messages.Any() && eykDaOnaylandi == true)
            {
                string msg = "";
                var asilCount = juriOneriFormu.MezuniyetJuriOneriFormuJurileris.Count(p => p.IsAsilOrYedek == true);
                var yedekCount = juriOneriFormu.MezuniyetJuriOneriFormuJurileris.Count(p => p.IsAsilOrYedek == false);
                var countSizeAsil = juriOneriFormu.MezuniyetBasvurulari.OgrenimTipKod.IsDoktora() ? 5 : 3;
                if (asilCount != countSizeAsil)
                    msg += ("<br />* Jüri adayı önerisinden " + countSizeAsil + " Asil aday belirlemeniz gerekmektedi.");
                if (yedekCount != 2)
                    msg += ("<br />* Jüri adayı önerisinde 2 Yedek aday belirlemeniz gerekmektedi.");
                if (msg != "")
                {
                    mMessage.Messages.Add("Jüri öneri formunda EYK'ya onaylandı işlemini yapabilmeniz için: " + msg);
                }
            }
            if (!mMessage.Messages.Any())
            {
                if (eykDaOnaylandi != false)
                {
                    var ogrenciObsBilgi =
                        KullanicilarBus.OgrenciBilgisiGuncelleObs(juriOneriFormu.MezuniyetBasvurulari.KullaniciID);

                    if (!ogrenciObsBilgi.KayitVar)
                    {
                        mMessage.Messages.Add(
                            "Öğrenci OBS sisteminde aktif öğrenci olarak gözükmemektedir. Onay işlemi yapılamaz.");
                    }
                    else if (juriOneriFormu.MezuniyetBasvurulari.OgrenciNo != ogrenciObsBilgi.OgrenciInfo.OGR_NO)
                    {
                        mMessage.Messages.Add(
                            "Ana başvurunuzdaki öğrenci numarası ile güncel öğrenci numarası uyuşmuyor. Öğrencinin kaydı silinip farklı bir programa kaydolmuş olabilir ya da numarası değişmiş olabilir.");
                    }
                }
            }
            if (!mMessage.Messages.Any())
            {

                var sendMail = eykDaOnaylandi.HasValue && eykDaOnaylandi != juriOneriFormu.EYKDaOnaylandi;
                juriOneriFormu.EYKDaOnaylandi = eykDaOnaylandi;
                juriOneriFormu.EYKDaOnaylandiOnayTarihi = DateTime.Now;
                juriOneriFormu.MezuniyetBasvurulari.EYKSayisi = eykSayisi;
                if (eykDaOnaylandi == true)
                {
                    juriOneriFormu.MezuniyetBasvurulari.EYKTarihi = eykDaOnaylandiOnayTarihi.Value.Date;
                }
                juriOneriFormu.EYKDaOnaylandiIslemYapanID = UserIdentity.Current.Id;
                if (eykDaOnaylandi == false) juriOneriFormu.EYKDaOnaylanmadiDurumAciklamasi = eykDaOnaylanmadiDurumAciklamasi;

                juriOneriFormu.IslemTarihi = DateTime.Now;
                juriOneriFormu.IslemYapanID = UserIdentity.Current.Id;
                juriOneriFormu.IslemYapanIP = UserIdentity.Ip;
                _entities.SaveChanges();
                mMessage.IsSuccess = true;
                LogIslemleri.LogEkle("MezuniyetJuriOneriFormlari", LogCrudType.Update, juriOneriFormu.ToJson());
                if (sendMail)
                {
                    MezuniyetBus.SendMailJuriOneriFormuEykOnay(mezuniyetJuriOneriFormId, eykDaOnaylandi == true);
                }
            }

            mMessage.MessageType = mMessage.IsSuccess ? MsgTypeEnum.Success : MsgTypeEnum.Error;
            return mMessage.ToJsonResult();
        }

        public ActionResult CiltliTezTeslimEkSureTalepPost(int mezuniyetBasvurulariId, bool? ciltliTezTeslimUzatmaTalebi)
        {
            var mMessage = new MmMessage
            {
                MessageType = MsgTypeEnum.Success,
                Title = "Ciltli Tez Teslimi Ek Süre Talep İşlemi"
            };
            var basvuru = _entities.MezuniyetBasvurularis.First(p => p.MezuniyetBasvurulariID == mezuniyetBasvurulariId);
            var sinav = basvuru.SRTalepleris.Where(p => p.MezuniyetSinavDurumID == MezuniyetSinavDurumEnum.Basarili).OrderByDescending(o => o.SRTalepID).FirstOrDefault();

            if (!RoleNames.MezuniyetGelenBasvurularKayit.InRoleCurrent() && basvuru.KullaniciID != UserIdentity.Current.Id)
            {
                mMessage.Messages.Add("Ek süre talebi için yetkiniz bulunmamaktadır.");
            }
            else if (basvuru.CiltliTezTeslimUzatmaTalebiDanismanOnay.HasValue)
            {
                mMessage.Messages.Add("Ciltli tez teslimi için ek süre talebi danışman tarafından onaylandığı için bu işlemde güncelleme yapılamaz.");
            }
            else if (basvuru.IsMezunOldu.HasValue)
            {
                mMessage.Messages.Add("Mezuniyet işlemi tamamlanmış başvurular için ciltli tez teslimi ek süre talebi yapılamaz.");
            }
            else if (sinav == null)
            {
                mMessage.Messages.Add("Sınav süreci başarılı bir şekilde tamamlanmadan ciltli tez teslimi için ek süre talebi yapılamaz.");
            }
            else if (ciltliTezTeslimUzatmaTalebi == true && basvuru.CiltliTezTeslimUzatmaTalebi != true)
            {
                var ogrenimTipi = basvuru.MezuniyetSureci.MezuniyetSureciOgrenimTipKriterleris.First(f => f.OgrenimTipKod == basvuru.OgrenimTipKod);
                var maxTezTeslimSonTarih = sinav.Tarih.AddMonths(ogrenimTipi.TezTeslimSuresiAy + 1);
                if (basvuru.TezTeslimSonTarih.HasValue)
                {
                    if (basvuru.TezTeslimSonTarih.Value > maxTezTeslimSonTarih)
                    {
                        mMessage.Messages.Add($"Enstitü, tez teslim son tarihini talep edilebilecek maksimum süreden daha ileri bir tarih olan {basvuru.TezTeslimSonTarih.ToFormatDate()} olarak belirlemiştir. Bu nedenle, ciltli tez teslimi için ek süre talep edilemez.");
                    }
                }
            }
            if (!mMessage.Messages.Any())
            {

                var sendMail = ciltliTezTeslimUzatmaTalebi != basvuru.CiltliTezTeslimUzatmaTalebi && ciltliTezTeslimUzatmaTalebi == true;
                basvuru.CiltliTezTeslimUzatmaTalebi = ciltliTezTeslimUzatmaTalebi == true ? true : (bool?)null;
                basvuru.CiltliTezTeslimUzatmaTalebiTarih = DateTime.Now;
                basvuru.IslemTarihi = DateTime.Now;
                basvuru.IslemYapanID = UserIdentity.Current.Id;
                basvuru.IslemYapanIP = UserIdentity.Ip;
                _entities.SaveChanges();
                mMessage.IsSuccess = true;
                LogIslemleri.LogEkle("MezuniyetBasvurulari", LogCrudType.Update, basvuru.ToJson());
                if (sendMail)
                {
                    MezuniyetBus.SendMailCiltliTezTeslimEkSureTalebiYapildi(mezuniyetBasvurulariId);
                }
            }

            mMessage.MessageType = mMessage.IsSuccess ? MsgTypeEnum.Success : MsgTypeEnum.Error;
            return mMessage.ToJsonResult();
        }

        public ActionResult CiltliTezTeslimEkSureDanismanOnayPost(int mezuniyetBasvurulariId, bool? isOnaylandi, string aciklama)
        {
            var mMessage = new MmMessage
            {
                MessageType = MsgTypeEnum.Success,
                Title = "Ciltli Tez Teslimi Ek Süre Talep Onay İşlemi"
            };
            var basvuru = _entities.MezuniyetBasvurularis.First(p => p.MezuniyetBasvurulariID == mezuniyetBasvurulariId);

            if (!RoleNames.MezuniyetGelenBasvurularKayit.InRoleCurrent() && basvuru.TezDanismanID != UserIdentity.Current.Id)
            {
                mMessage.Messages.Add("Ek süre talep onayı için yetkiniz bulunmamaktadır.");
            }
            else if (basvuru.CiltliTezTeslimUzatmaTalebi != true)
            {
                mMessage.Messages.Add("Öğrenci tarafından yapılmış bir ciltli tez teslimi ek süre talebi bulunmamaktadır.");
            }
            else if (basvuru.CiltliTezTeslimUzatmaTalebiEykDaOnay.HasValue)
            {
                mMessage.Messages.Add("Ciltli tez teslimi ek süre talebi, Enstitü tarafından onaylandığı için güncelleme yapılamaz.");
            }
            else if (isOnaylandi == false && aciklama.IsNullOrWhiteSpace())
            {
                mMessage.Messages.Add("Talebi onaylamama sebebini belirtiniz.");
            }
            if (!mMessage.Messages.Any())
            {

                var sendMail = isOnaylandi.HasValue && isOnaylandi != basvuru.CiltliTezTeslimUzatmaTalebiDanismanOnay;
                basvuru.CiltliTezTeslimUzatmaTalebiDanismanOnay = isOnaylandi;
                basvuru.CiltliTezTeslimUzatmaTalebiDanismanOnayTarih = DateTime.Now;
                basvuru.CiltliTezTeslimUzatmaTalebiDanismanOnayAciklama = aciklama;
                basvuru.IslemTarihi = DateTime.Now;
                basvuru.IslemYapanID = UserIdentity.Current.Id;
                basvuru.IslemYapanIP = UserIdentity.Ip;
                _entities.SaveChanges();
                mMessage.IsSuccess = true;
                LogIslemleri.LogEkle("MezuniyetBasvurulari", LogCrudType.Update, basvuru.ToJson());
                if (sendMail)
                {
                    MezuniyetBus.SendMailCiltliTezTeslimEkSureTalebiDanismanOnay(mezuniyetBasvurulariId);
                }
            }

            mMessage.MessageType = mMessage.IsSuccess ? MsgTypeEnum.Success : MsgTypeEnum.Error;
            return mMessage.ToJsonResult();
        }

        public ActionResult CiltliTezTeslimEkSureEykOnayPost(int mezuniyetBasvurulariId, bool? isOnaylandi, DateTime? eykTarihi, string eykSayisi, string aciklama)
        {
            var mMessage = new MmMessage
            {
                MessageType = MsgTypeEnum.Success,
                Title = "Ciltli Tez Teslimi Ek Süre EYK Onay İşlemi"
            };
            var basvuru = _entities.MezuniyetBasvurularis.First(p => p.MezuniyetBasvurulariID == mezuniyetBasvurulariId);

            if (!RoleNames.MezuniyetGelenBasvurularKayit.InRoleCurrent())
            {
                mMessage.Messages.Add("Ek süre talep onayı için yetkiniz bulunmamaktadır.");
            }
            else if (basvuru.CiltliTezTeslimUzatmaTalebiDanismanOnay != true)
            {
                mMessage.Messages.Add("Danışman tarafından onaylanmamış ciltli tez teslimi ek süre talebi için EYK onay işlemi yapılamaz.");
            }
            else if (isOnaylandi == false && aciklama.IsNullOrWhiteSpace())
            {
                mMessage.Messages.Add("Talebi onaylamama sebebini belirtiniz.");
            }

            if (isOnaylandi == true)
            {
                if (!eykTarihi.HasValue)
                {
                    mMessage.Messages.Add("EYK tarihini giriniz.");
                }
                mMessage.MessagesDialog.Add(new MrMessage { MessageType = eykSayisi.IsNullOrWhiteSpace() ? MsgTypeEnum.Error : MsgTypeEnum.Success, PropertyName = "CiltliTezTeslimUzatmaEykDaOnayEykTarihi_" + mezuniyetBasvurulariId });
                if (eykSayisi.IsNullOrWhiteSpace())
                {
                    mMessage.Messages.Add("EYK Sayısı Giriniz.");
                }
                mMessage.MessagesDialog.Add(new MrMessage { MessageType = eykSayisi.IsNullOrWhiteSpace() ? MsgTypeEnum.Error : MsgTypeEnum.Success, PropertyName = "CiltliTezTeslimUzatmaEykDaOnayEykSayisi_" + mezuniyetBasvurulariId });
            }
            if (!mMessage.Messages.Any())
            {

                var sendMail = isOnaylandi.HasValue && isOnaylandi != basvuru.CiltliTezTeslimUzatmaTalebiEykDaOnay;
                basvuru.CiltliTezTeslimUzatmaTalebiEykDaOnay = isOnaylandi;
                basvuru.CiltliTezTeslimUzatmaTalebiEykDaOnayTarih = DateTime.Now;
                basvuru.CiltliTezTeslimUzatmaTalebiEykDaOnayAciklama = aciklama;
                if (isOnaylandi == true)
                {
                    basvuru.CiltliTezTeslimUzatmaTalebiEykDaOnayEYKTarihi = eykTarihi.Value.Date;
                    basvuru.CiltliTezTeslimUzatmaTalebiEykDaOnayEYKSayisi = eykSayisi;
                }

                var ogrenimTipi = basvuru.MezuniyetSureci.MezuniyetSureciOgrenimTipKriterleris.First(f => f.OgrenimTipKod == basvuru.OgrenimTipKod);
                var sinav = basvuru.SRTalepleris.Where(p => p.MezuniyetSinavDurumID == MezuniyetSinavDurumEnum.Basarili).OrderByDescending(o => o.SRTalepID).FirstOrDefault();
                var maxTezTeslimSonTarih = sinav.Tarih.AddMonths(ogrenimTipi.TezTeslimSuresiAy + 1);
                if (isOnaylandi == true && basvuru.TezTeslimSonTarih.HasValue && basvuru.TezTeslimSonTarih.Value < maxTezTeslimSonTarih)
                {
                    basvuru.TezTeslimSonTarih = null;
                }
                basvuru.IslemTarihi = DateTime.Now;
                basvuru.IslemYapanID = UserIdentity.Current.Id;
                basvuru.IslemYapanIP = UserIdentity.Ip;
                _entities.SaveChanges();
                mMessage.IsSuccess = true;
                LogIslemleri.LogEkle("MezuniyetBasvurulari", LogCrudType.Update, basvuru.ToJson());
                if (sendMail)
                {
                    MezuniyetBus.SendMailCiltliTezTeslimEkSureTalebiEYKOnay(mezuniyetBasvurulariId);
                }
            }

            mMessage.MessageType = mMessage.IsSuccess ? MsgTypeEnum.Success : MsgTypeEnum.Error;
            return mMessage.ToJsonResult();
        }
        [Authorize(Roles = RoleNames.MezuniyetGelenBasvurularJuriOneriFormuEykOnay)]
        [HttpPost]
        public ActionResult EYKDaOnayEkSure(List<int> mezuniyetBasvurulariIds, DateTime? eykTarihi, string eykSayisi)
        {
            mezuniyetBasvurulariIds = mezuniyetBasvurulariIds ?? new List<int>();
            var eykDaOnaylanacakBasvurular = _entities.MezuniyetBasvurularis.Where(p =>
                mezuniyetBasvurulariIds.Contains(p.MezuniyetBasvurulariID) && p.CiltliTezTeslimUzatmaTalebiDanismanOnay == true && !p.CiltliTezTeslimUzatmaTalebiEykDaOnay.HasValue
            ).ToList();
            foreach (var item in eykDaOnaylanacakBasvurular)
            {
                item.CiltliTezTeslimUzatmaTalebiEykDaOnay = true;
                item.CiltliTezTeslimUzatmaTalebiEykDaOnayTarih = DateTime.Now;
                item.CiltliTezTeslimUzatmaTalebiEykDaOnayEYKTarihi = eykTarihi;
                item.CiltliTezTeslimUzatmaTalebiEykDaOnayEYKSayisi = eykSayisi;
                item.IslemTarihi = DateTime.Now;
                item.IslemYapanID = UserIdentity.Current.Id;
                item.IslemYapanIP = UserIdentity.Ip;
            }
            _entities.SaveChanges();
            foreach (var item in eykDaOnaylanacakBasvurular)
            {
                LogIslemleri.LogEkle("MezuniyetBasvurulari", LogCrudType.Update, item.ToJson());
                MezuniyetBus.SendMailCiltliTezTeslimEkSureTalebiEYKOnay(item.MezuniyetBasvurulariID);
            }
            return new { eykDaOnaylanacakBasvurular.Count }.ToJsonResult();
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
                MessageType = MsgTypeEnum.Error
            };
            var srTalep = _entities.SRTalepleris.First(p => p.UniqueID == uniqueId);
            if (srTalep.MezuniyetBasvurulari.MezuniyetYayinKontrolDurumID != MezuniyetYayinKontrolDurumuEnum.KabulEdildi)
            {
                mmMessage.Messages.Add("Mezuniyet başvuru durumu Kabul Edildi olan başvurularda işlem yapılabilir.");
            }
            else
            {
                if (ytuIciMezuniyetJuriOneriFormuJuriId.HasValue)
                {
                    var srYtuIciJuri = srTalep.SRTaleplerJuris.FirstOrDefault(p =>
                        p.JuriTipAdi.Contains("YtuIciJuri") &&
                        p.MezuniyetJuriOneriFormuJuriID != ytuIciMezuniyetJuriOneriFormuJuriId);
                    if (srYtuIciJuri != null)
                    {
                        var juri = _entities.MezuniyetJuriOneriFormuJurileris.First(p =>
                            p.MezuniyetJuriOneriFormuJuriID == ytuIciMezuniyetJuriOneriFormuJuriId);
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
                    var ytuDisiJuri = srTalep.SRTaleplerJuris.FirstOrDefault(p =>
                        p.JuriTipAdi.Contains("YtuDisiJuri") &&
                        p.MezuniyetJuriOneriFormuJuriID != ytuDisiMezuniyetJuriOneriFormuJuriId);
                    if (ytuDisiJuri != null)
                    {
                        var juri = _entities.MezuniyetJuriOneriFormuJurileris.First(p =>
                            p.MezuniyetJuriOneriFormuJuriID == ytuDisiMezuniyetJuriOneriFormuJuriId);
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
                mmMessage.MessageType = MsgTypeEnum.Success;
            }

            return mmMessage.ToJsonResult();
        }

        public ActionResult TezTeslimFormuSil(int mezuniyetBasvurulariId)
        {
            var mmMessage = new MmMessage
            {
                IsSuccess = false,
                Title = "Tez Teslim Formu Silme İşlemi",
                MessageType = MsgTypeEnum.Warning
            };
            var yetkili = RoleNames.MezuniyetGelenBasvurularKayit.InRoleCurrent();
            var mezuniyetBasvurusu = _entities.MezuniyetBasvurularis.First(f => f.MezuniyetBasvurulariID == mezuniyetBasvurulariId);
            var tezTeslimFormu = mezuniyetBasvurusu.MezuniyetBasvurulariTezTeslimFormlaris.FirstOrDefault();
            if (mezuniyetBasvurusu.KullaniciID != UserIdentity.Current.Id && !yetkili)
            {
                mmMessage.Messages.Add("Başka bir kullanıcı tez teslim formu oluşturmaya yetkili değilsiniz!");
            }
            else if (mezuniyetBasvurusu.MezuniyetYayinKontrolDurumID != MezuniyetYayinKontrolDurumuEnum.KabulEdildi)
            {
                mmMessage.Messages.Add("Mezuniyet başvuru durumu Kabul Edildi olan başvurularda işlem yapılabilir.");
            }
            else if (mezuniyetBasvurusu.IsMezunOldu.HasValue)
            {
                mmMessage.Messages.Add("Mezuniyet sonuç bilgisi girilildikten sonra Tez teslim formu üzerinde herhangi bir işlemi yapılamaz!");

            }
            else if (tezTeslimFormu == null)
            {
                mmMessage.Messages.Add("Silinecek herhangi bir tezteslim formu bulunamadı!");
            }

            if (mmMessage.Messages.Count == 0)
            {
                try
                {
                    _entities.MezuniyetBasvurulariTezTeslimFormlaris.Remove(tezTeslimFormu);
                    _entities.SaveChanges();
                    mmMessage.IsSuccess = true;
                    mmMessage.Messages.Add("Tez Teslim Formu Silindi.");

                }
                catch (Exception ex)
                {
                    mmMessage.IsSuccess = false;
                    mmMessage.MessageType = MsgTypeEnum.Error;
                    mmMessage.Messages.Add("Hata: </br> " + ex.ToExceptionMessage());
                }
            }
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
                    LogIslemleri.LogEkle("MezuniyetBasvurulari", LogCrudType.Delete, kayit.ToJson());
                    mmMessage.Messages.Add(tarih + " Tarihli başvuru silindi.");
                    mmMessage.MessageType = MsgTypeEnum.Success;
                    FileHelper.DeleteFiles(fFList);
                }
                catch (Exception ex)
                {
                    mmMessage.MessageType = MsgTypeEnum.Error;
                    mmMessage.IsSuccess = false;
                    mmMessage.Messages.Add(tarih + " Tarihli başvuru silinemedi.");
                    mmMessage.Title = "Hata";
                    SistemBilgilendirmeBus.SistemBilgisiKaydet(ex.ToExceptionMessage(), ex.ToExceptionStackTrace(), BilgiTipiEnum.OnemsizHata);
                }

            }
            var strView = ViewRenderHelper.RenderPartialView("Ajax", "GetMessage", mmMessage);
            return Json(new { mmMessage.IsSuccess, Messages = strView }, "application/json", JsonRequestBehavior.AllowGet);
        }


        [Authorize(Roles = RoleNames.MezuniyetGelenBasvurularJuriOneriFormuEykOnay)]
        [HttpPost]
        public ActionResult EYKDaOnay(List<int> mezuniyetBasvurulariIds, DateTime? eykTarihi, string eykSayisi)
        {
            mezuniyetBasvurulariIds = mezuniyetBasvurulariIds ?? new List<int>();
            var eykDaOnaylanacakBasvurular = _entities.MezuniyetJuriOneriFormlaris.Where(p =>
                mezuniyetBasvurulariIds.Contains(p.MezuniyetBasvurulariID)
                && p.EYKYaHazirlandi == true && !p.EYKDaOnaylandi.HasValue
            ).ToList();
            foreach (var item in eykDaOnaylanacakBasvurular)
            {
                item.EYKDaOnaylandi = true;
                item.EYKDaOnaylandiOnayTarihi = DateTime.Now;
                item.MezuniyetBasvurulari.EYKTarihi = eykTarihi;
                if (!eykSayisi.IsNullOrWhiteSpace())
                {
                    item.MezuniyetBasvurulari.EYKSayisi = eykSayisi;
                }
                item.EYKDaOnaylandiIslemYapanID = UserIdentity.Current.Id;
            }
            _entities.SaveChanges();
            foreach (var item in eykDaOnaylanacakBasvurular)
            {
                LogIslemleri.LogEkle("MezuniyetJuriOneriFormlari", LogCrudType.Update, item.ToJson());
                MezuniyetBus.SendMailJuriOneriFormuEykOnay(item.MezuniyetJuriOneriFormID, true);
            }
            return new { eykDaOnaylanacakBasvurular.Count }.ToJsonResult();
        }

        public ActionResult GetTutanakRaporu()
        {
            return View();
        }
        public ActionResult GetTutanakRaporuKontrolu(int raporTipId, List<int> ogrenimTipKods, DateTime? basTar, DateTime? bitTar, DateTime? raporTarihi)
        {
            var mMessage = new MmMessage
            {
                MessageType = MsgTypeEnum.Success,
                IsSuccess = true
            };

            if (ogrenimTipKods == null || ogrenimTipKods.Count == 0)
            {
                mMessage.IsSuccess = false;
                mMessage.Messages.Add("Öğrenim tipi seçiniz.");
                mMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "OgrenimTipKods" });
            }
            else mMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Nothing, PropertyName = "OgrenimTipKods" });
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
            if (raporTipId == RaporTipiEnum.MezuniyetTutanakRaporu && !raporTarihi.HasValue && ogrenimTipKods != null && ogrenimTipKods.Any(a => a.IsDoktora()))
            {
                mMessage.IsSuccess = false;
                mMessage.Messages.Add("Rapor tarihini giriniz.");
                mMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Warning, PropertyName = "RaporTarihi" });
            }
            else { mMessage.MessagesDialog.Add(new MrMessage { MessageType = MsgTypeEnum.Nothing, PropertyName = "RaporTarihi" }); }

            if (!mMessage.IsSuccess)
            {

                mMessage.Title = "Tutanak çıktısı oluşturulamadı";
                mMessage.MessageType = MsgTypeEnum.Warning;
            }

            return mMessage.ToJsonResult();



        }
        public ActionResult GetTutanakRaporuExport(int raporTipId, int ogrenimTipKods, int? enstituOnayDurumId, int? ciltliTezOnayDurumId, string basTar, string bitTar, string raporTarihi, bool exportWordOrExcel, string ekd)
        {

            string html;
            string raporAdi;
            var enstituKod = EnstituBus.GetSelectedEnstitu(ekd);
            var baslangicTarihi = basTar.ToDate().Value;
            var bitisTarihi = bitTar.ToDate().Value;
            var isDoktora = ogrenimTipKods.IsDoktora();



            if (raporTipId == RaporTipiEnum.MezuniyetTezJuriTutanakRaporu)
            {
                var qds = _entities.MezuniyetBasvurularis.Where(p => p.MezuniyetSureci.EnstituKod == enstituKod);

                if (enstituOnayDurumId == 1)
                {
                    qds = qds.Where(p => p.MezuniyetJuriOneriFormlaris.Any(a => a.EYKYaGonderildi == true && !a.EYKYaHazirlandi.HasValue && a.EYKYaGonderildiIslemTarihi >= baslangicTarihi && a.EYKYaGonderildiIslemTarihi <= bitisTarihi));

                }
                else if (enstituOnayDurumId == 2)
                {
                    qds = qds.Where(p => p.MezuniyetJuriOneriFormlaris.Any(a => a.EYKYaHazirlandi == true && !a.EYKDaOnaylandi.HasValue && a.EYKYaHazirlandiIslemTarihi >= baslangicTarihi && a.EYKYaHazirlandiIslemTarihi <= bitisTarihi));

                }
                else if (enstituOnayDurumId == 3)
                {
                    qds = qds.Where(p => p.MezuniyetJuriOneriFormlaris.Any(a => a.EYKDaOnaylandi == true) && p.EYKTarihi >= baslangicTarihi && p.EYKTarihi <= bitisTarihi);


                }
                var data = qds.OrderByDescending(o => o.OgrenimTipKod).ThenBy(t => t.EYKTarihi).ToList()
                    .Where(p => p.OgrenimTipKod.IsDoktora() == isDoktora).ToList();

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
                    var danisman = joForm.MezuniyetJuriOneriFormuJurileris.First(p => p.JuriTipAdi == "TezDanismani");
                    row.DanismanAdSoyad = danisman.UnvanAdi + " " + danisman.AdSoyad;
                    row.DanismanUni = danisman.UniversiteAdi;
                    if (rModel.IsDoktoraOrYL)
                    {
                        var tik1 = joForm.MezuniyetJuriOneriFormuJurileris.First(p => p.JuriTipAdi == "TikUyesi1");
                        row.TikUyesi = tik1.UnvanAdi + " " + tik1.AdSoyad;
                        row.TikUyesiUni = tik1.UniversiteAdi;

                        var tik2 = joForm.MezuniyetJuriOneriFormuJurileris.First(p => p.JuriTipAdi == "TikUyesi2");
                        row.TikUyesi2 = tik2.UnvanAdi + " " + tik2.AdSoyad;
                        row.TikUyesi2Uni = tik2.UniversiteAdi;
                    }
                    var jtList = new List<string> { "TezDanismani", "TikUyesi1", "TikUyesi2" };

                    var asilUye = joForm.MezuniyetJuriOneriFormuJurileris.First(p => !jtList.Contains(p.JuriTipAdi) && p.IsAsilOrYedek == true);
                    row.AsilUye = asilUye.UnvanAdi + " " + asilUye.AdSoyad;
                    row.AsilUyeUni = asilUye.UniversiteAdi;
                    jtList.Add(asilUye.JuriTipAdi);
                    var asilUye2 = joForm.MezuniyetJuriOneriFormuJurileris.First(p => !jtList.Contains(p.JuriTipAdi) && p.IsAsilOrYedek == true);
                    row.AsilUye2 = asilUye2.UnvanAdi + " " + asilUye2.AdSoyad;
                    row.AsilUye2Uni = asilUye2.UniversiteAdi;
                    jtList.Add(asilUye2.JuriTipAdi);
                    var yedekUye = joForm.MezuniyetJuriOneriFormuJurileris.First(p => !jtList.Contains(p.JuriTipAdi) && p.IsAsilOrYedek == false);
                    row.YedekUye = yedekUye.UnvanAdi + " " + yedekUye.AdSoyad;
                    row.YedekUyeUni = yedekUye.UniversiteAdi;
                    jtList.Add(yedekUye.JuriTipAdi);
                    var yedekUye2 = joForm.MezuniyetJuriOneriFormuJurileris.First(p => !jtList.Contains(p.JuriTipAdi) && p.IsAsilOrYedek == false);
                    row.YedekUye2 = yedekUye2.UnvanAdi + " " + yedekUye2.AdSoyad;
                    row.YedekUye2Uni = yedekUye2.UniversiteAdi;

                    if (joForm.IsTezBasligiDegisti == true)
                    {
                        row.TezKonusu = itemO.IsTezDiliTr == true ? joForm.YeniTezBaslikTr : joForm.YeniTezBaslikEn;
                    }
                    else if (joForm.IsTezBasligiDegisti == true)
                    {
                        row.TezKonusu = itemO.IsTezDiliTr == true ? joForm.YeniTezBaslikTr : joForm.YeniTezBaslikEn;
                    }
                    else
                    {
                        row.TezKonusu = itemO.IsTezDiliTr == true ? itemO.TezBaslikTr : itemO.TezBaslikEn;
                    }

                    row.TezDili = itemO.IsTezDiliTr == true ? "Türkçe" : "İngilizce";
                    rModel.DetayData.Add(row);

                    model.Add(rModel);
                }


                RprMezuniyetTezJuriTutanak rpr = new RprMezuniyetTezJuriTutanak(rModel.IsDoktoraOrYL);
                rpr.DataSource = model.Count > 0 ? model[0] : new RprTutanakModel();
                rpr.CreateDocument();
                raporAdi = $"{(enstituOnayDurumId == 2 ? "EykYa HAZIRLANAN" : (enstituOnayDurumId == 3 ? "EYKda ONAYLANAN" : "EYKya GÖNDERİLEN"))} {(ogrenimTipKods.IsDoktora() ? "Doktra" : "Yüksek Lisans")} Tez Sınav Jürileri Atama Önerileri";

                using (MemoryStream ms = new MemoryStream())
                {
                    rpr.ExportToHtml(ms);
                    ms.Position = 0;
                    var sr = new StreamReader(ms);
                    html = sr.ReadToEnd();
                }
                return File(System.Text.Encoding.UTF8.GetBytes(html), (exportWordOrExcel ? "application/vnd.ms-word" : "application/ms-excel"), raporAdi + " (" + basTar.Replace("-", ".") + "-" + bitTar.Replace("-", ".") + ")." + (exportWordOrExcel ? "doc" : "xls"));

            }
            if (raporTipId == RaporTipiEnum.MezuniyetTutanakRaporu)
            {
                var data = _entities.MezuniyetBasvurularis.Where(p =>
                        p.MezuniyetSureci.EnstituKod == enstituKod &&
                        p.IsMezunOldu == true &&
                        p.MezuniyetTarihi >= baslangicTarihi &&
                        p.MezuniyetTarihi <= bitisTarihi
                    )
                    .OrderBy(o => o.MezuniyetTarihi).ToList()
                    .Where(p => p.OgrenimTipKod.IsDoktora() == isDoktora).ToList();

                if (ogrenimTipKods.IsDoktora())
                {
                    var model = new List<RprMezuniyetTutanakModel>();
                    foreach (var itemO in data)
                    {
                        var row = new RprMezuniyetTutanakModel();
                        var prgl = itemO.Programlar;
                        var abdl = itemO.Programlar.AnabilimDallari;
                        var sinav = itemO.SRTalepleris.First(p => p.MezuniyetSinavDurumID == MezuniyetSinavDurumEnum.Basarili);

                        var tezBasligiDegisenSinav = itemO.SRTalepleris.OrderByDescending(o => o.SRTalepID).FirstOrDefault(a =>
                            a.MezuniyetSinavDurumID != MezuniyetSinavDurumEnum.SonucGirilmedi &&
                            a.IsTezBasligiDegisti == true);
                        string danismanBilgi;
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
                        var baslikTr = tezBasligiDegisenSinav == null ? (joForm.IsTezBasligiDegisti == true ? joForm.YeniTezBaslikTr : joForm.MezuniyetBasvurulari.TezBaslikTr) : tezBasligiDegisenSinav.YeniTezBaslikTr;
                        var baslikEn = tezBasligiDegisenSinav == null ? (joForm.IsTezBasligiDegisti == true ? joForm.YeniTezBaslikEn : joForm.MezuniyetBasvurulari.TezBaslikEn) : tezBasligiDegisenSinav.YeniTezBaslikEn;
                        var tezBaslik = joForm.MezuniyetBasvurulari.IsTezDiliTr == true ? baslikTr : baslikEn;

                        row.OgrenciNo = itemO.OgrenciNo;
                        row.Konu = itemO.Ad + " " + itemO.Soyad + " 'DOKTORA DERECESİ' alması Hk.";
                        row.Aciklama1 = "Enstitümüz " + abdl.AnabilimDaliAdi + " Anabilim Dalı " + prgl.ProgramAdi + " doktora programı öğrencisi <b>" + itemO.OgrenciNo + "</b> no’lu <b>" + itemO.Ad + " " + itemO.Soyad + ";</b> "
                                        + "21/12/2016 gün ve  29925 sayılı Resmi Gazete’de yayımlanarak yürürlüğe giren 'YTÜ Lisansüstü Eğitim - Öğretim Yönetmeliği’nin 24.maddesi uyarınca, "
                                        + "doktora eğitimi ile ilgili tüm koşullarını yerine getirdiğinden " + sinav.Tarih.Date.ToFormatDate() + " tarihinde yapılan doktora tez sınavında <b>" + danismanBilgi + "</b> danışmanlığında hazırladığı "
                                        + "<b>“" + tezBaslik + "”</b> başlıklı tezi başarılı bulunmuştur.";
                        row.Aciklama2 = "1 Mart 2017 tarih ve 29994 sayılı Yüksek Öğretim Kurulu Lisansüstü Eğitim ve Öğretim Yönetmeliğinde Değişiklik Yapılmasına Dair Yönetmelik:<b> Madde 2- “Mezuniyet Tarihi tezin sınav "
                            + "jüri komisyonu tarafından imzalı nüshasının teslim edildiği tarihtir.”</b> gereğince <b>" + itemO.MezuniyetTarihi.Value.Date.ToFormatDate() + "</b> tarihinde tezini Enstitümüze teslim eden İlgili öğrencinin, tezinin kabul edildiğini ve kendisine "
                            + "<b>'DOKTORA DERECESİ'</b> verildiğini bildiren jüri ortak raporunun <b>" + raporTarihi.ToDate().Value.ToFormatDate() + "</b> tarihi itibariyle onanmasına ve Üniversite Senatosu'na sunulmak üzere Rektörlüğe arzına </b>oybirliğiyle</b> karar verildi.";
                        model.Add(row);
                    }
                    var strOgrenciNos = "";
                    if (!exportWordOrExcel)
                    {
                        var ogrenciNos = data.Select(s => s.OgrenciNo).Distinct().Where(p => !p.IsNullOrWhiteSpace()).ToList();
                        strOgrenciNos = string.Join(" ", ogrenciNos);
                    }
                    RprMezuniyetMezunlarTutanakDr rpr = new RprMezuniyetMezunlarTutanakDr(strOgrenciNos);
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
                                    + "imzalı nüshasının teslim edildiği tarihtir.”</b> gereğince, " + baslangicTarihi.ToFormatDate() + " ile " + bitisTarihi.ToFormatDate() + " tarihleri arasında tezlerini Enstitümüze teslim eden öğrencilerin "
                                    + "aşağıda belirtilen tez teslim tarihinde mezuniyetlerine oybirliğiyle karar verildi."
                    };
                    foreach (var itemO in data)
                    {
                        var row = new RprMezuniyetTutanakRowModel();
                        var prgl = itemO.Programlar;
                        var abdl = itemO.Programlar.AnabilimDallari;
                        row.OgrenciBilgi = itemO.OgrenciNo + " " + itemO.Ad + " " + itemO.Soyad + " (" + abdl.AnabilimDaliAdi + " / " + prgl.ProgramAdi + ")";

                        var sinav = itemO.SRTalepleris.First(p => p.MezuniyetSinavDurumID == MezuniyetSinavDurumEnum.Basarili);
                        var tezSonBilgi = sinav.MezuniyetBasvurulari.MezuniyetBasvurulariTezTeslimFormlaris.First();
                        string danismanBilgi;
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
                        row.OgrenciNo = itemO.OgrenciNo;
                        row.DanismanAdSoyad = danismanBilgi;
                        row.TezKonusu = tezSonBilgi.IsTezDiliTr ? tezSonBilgi.TezBaslikTr : tezSonBilgi.TezBaslikEn;
                        row.SavunmaTarihi = sinav.Tarih.ToFormatDate();
                        row.TezTeslimTarihi = itemO.MezuniyetTarihi.ToFormatDate();

                        model.Data.Add(row);

                    }
                    var strOgrenciNos = "";
                    if (!exportWordOrExcel)
                    {
                        var ogrenciNos = data.Select(s => s.OgrenciNo).Distinct().Where(p => !p.IsNullOrWhiteSpace()).ToList();
                        strOgrenciNos = string.Join(" ", ogrenciNos);
                    }
                    RprMezuniyetMezunlarTutanakYL rpr = new RprMezuniyetMezunlarTutanakYL(strOgrenciNos);
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
                return File(System.Text.Encoding.UTF8.GetBytes(html), (exportWordOrExcel ? "application/vnd.ms-word" : "application/ms-excel"), raporAdi + " (" + basTar.Replace("-", ".") + "-" + bitTar.Replace("-", ".") + ")." + (exportWordOrExcel ? "doc" : "xls"));


            }


            //raporTipId == RaporTipiEnum.MezuniyetCiltliTezTeslikEkSureTalebiTutanak
            var q = from s in _entities.MezuniyetBasvurularis
                    join ms in _entities.MezuniyetSurecis.Where(p => p.EnstituKod == enstituKod) on s.MezuniyetSurecID equals ms.MezuniyetSurecID
                    join ogrenci in _entities.Kullanicilars on s.KullaniciID equals ogrenci.KullaniciID
                    join mOt in _entities.MezuniyetSureciOgrenimTipKriterleris on new { s.MezuniyetSurecID, s.OgrenimTipKod } equals new { mOt.MezuniyetSurecID, mOt.OgrenimTipKod }
                    join o in _entities.OgrenimTipleris on new { s.OgrenimTipKod, ms.EnstituKod } equals new { o.OgrenimTipKod, o.EnstituKod }
                    join pr in _entities.Programlars on s.ProgramKod equals pr.ProgramKod
                    join abl in _entities.AnabilimDallaris on pr.AnabilimDaliID equals abl.AnabilimDaliID
                    let srT = s.SRTalepleris.Where(p => p.MezuniyetSinavDurumID == MezuniyetSinavDurumEnum.Basarili).OrderByDescending(os => os.SRTalepID).FirstOrDefault()
                    where srT != null
                    select new
                    {
                        o.OgrenimTipAdi,
                        s.OgrenciNo,
                        AdSoyad = ogrenci.Ad + " " + ogrenci.Soyad,
                        AnabilimdaliAdi = abl.AnabilimDaliAdi,
                        pr.ProgramAdi,
                        DanismanAdSoyad = s.TezDanismanUnvani + " " + s.TezDanismanAdi,
                        BasariliOlunanSinavTarihi = srT.Tarih,
                        mOt.TezTeslimSuresiAy,
                        s.CiltliTezTeslimUzatmaTalebiDanismanOnay,
                        s.CiltliTezTeslimUzatmaTalebiDanismanOnayTarih,
                        s.CiltliTezTeslimUzatmaTalebiEykDaOnay,
                        s.CiltliTezTeslimUzatmaTalebiEykDaOnayEYKTarihi,
                        s.CiltliTezTeslimUzatmaTalebiEykDaOnayEYKSayisi,
                        s.IsMezunOldu,
                        FiterTarih = ciltliTezOnayDurumId == MezuniyetTezTeslimUzatmaDurumuEnum.DanismanOnayladi ? s.CiltliTezTeslimUzatmaTalebiDanismanOnayTarih : s.CiltliTezTeslimUzatmaTalebiEykDaOnayEYKTarihi
                    };
            if (ciltliTezOnayDurumId == MezuniyetTezTeslimUzatmaDurumuEnum.DanismanOnayladi)
            {
                q = q.Where(p =>
                    p.CiltliTezTeslimUzatmaTalebiDanismanOnay == true &&
                    !p.CiltliTezTeslimUzatmaTalebiEykDaOnay.HasValue );
            }
            else
            {
                q = q.Where(p =>
                    p.CiltliTezTeslimUzatmaTalebiEykDaOnay == true );
            }
           
            q = q.Where(p => p.FiterTarih.HasValue && p.FiterTarih.Value >= baslangicTarihi && p.FiterTarih.Value <= bitisTarihi);
             
            var dataExport = q.ToList();
            using (var package = new ExcelPackage())
            {

                var firstRow = dataExport
                    .Select(s => new { s.CiltliTezTeslimUzatmaTalebiEykDaOnayEYKTarihi, s.CiltliTezTeslimUzatmaTalebiEykDaOnayEYKSayisi })
                    .FirstOrDefault();

                var enstituAdi = EnstituBus.GetEnstitu(enstituKod)?.EnstituAd ?? "Bilinmeyen Enstitü";
                var eykTarihi = firstRow?.CiltliTezTeslimUzatmaTalebiEykDaOnayEYKTarihi?.ToString("dd.MM.yyyy") ?? "----";
                var eykSayisi = firstRow?.CiltliTezTeslimUzatmaTalebiEykDaOnayEYKSayisi ?? "----";

                var worksheet = package.Workbook.Worksheets.Add("Tutanak Raporu");
                var currentRow = 1;
                int totalColumns = 12;

                string ustBaslik = $"YILDIZ TEKNİK ÜNİVERSİTESİ {enstituAdi.ToUpper()}\n" +
                                  $"CİLTLİ SON TEZ TESLİMİ İÇİN EK SÜRE\n" +
                                  $"{eykTarihi} / {eykSayisi}";


                worksheet.Cells[currentRow, 1, currentRow, totalColumns].Merge = true;
                worksheet.Cells[currentRow, 1].Value = ustBaslik;
                worksheet.Cells[currentRow, 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                worksheet.Cells[currentRow, 1].Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;
                worksheet.Cells[currentRow, 1].Style.Font.Bold = true;
                worksheet.Cells[currentRow, 1].Style.Font.Size = 14;
                worksheet.Cells[currentRow, 1].Style.WrapText = true;
                worksheet.Row(currentRow).Height = 160;

                // Başlık satırı için boşluk bırak
                currentRow += 2;

                // Sütun başlıklarını ekleme
                worksheet.Cells[currentRow, 1].Value = "Öğrenci No";
                worksheet.Cells[currentRow, 2].Value = "Ad Soyad";
                worksheet.Cells[currentRow, 3].Value = "Öğrenim Seviyesi";
                worksheet.Cells[currentRow, 4].Value = "Anabilim Dalı";
                worksheet.Cells[currentRow, 5].Value = "Programı";
                worksheet.Cells[currentRow, 6].Value = "Danışman Ünvanı ve Adı Soyadı";
                worksheet.Cells[currentRow, 7].Value = "Başarılı Olunan Tez Savunma Sınav Tarihi";
                worksheet.Cells[currentRow, 8].Value = "Tez Teslim Tarihi";
                worksheet.Cells[currentRow, 9].Value = "Ek Süre Eklenmiş Olan Son Teslim Tarihi";
                worksheet.Cells[currentRow, 10].Value = "EYK Tarihi";
                worksheet.Cells[currentRow, 11].Value = "EYK Sayısı";

                using (var range = worksheet.Cells[currentRow, 1, currentRow, totalColumns])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                }

                // Veri satırlarını ekleme
                foreach (var item in dataExport)
                {
                    currentRow++;
                    worksheet.Cells[currentRow, 1].Value = item.OgrenciNo;
                    worksheet.Cells[currentRow, 2].Value = item.AdSoyad;
                    worksheet.Cells[currentRow, 3].Value = item.OgrenimTipAdi;
                    worksheet.Cells[currentRow, 4].Value = item.AnabilimdaliAdi;
                    worksheet.Cells[currentRow, 5].Value = item.ProgramAdi;
                    worksheet.Cells[currentRow, 6].Value = item.DanismanAdSoyad;
                    worksheet.Cells[currentRow, 7].Value = item.BasariliOlunanSinavTarihi;
                    worksheet.Cells[currentRow, 7].Style.Numberformat.Format = "dd.MM.yyyy";
                    worksheet.Cells[currentRow, 8].Value = item.BasariliOlunanSinavTarihi.AddMonths(item.TezTeslimSuresiAy);
                    worksheet.Cells[currentRow, 8].Style.Numberformat.Format = "dd.MM.yyyy";
                    worksheet.Cells[currentRow, 9].Value = item.BasariliOlunanSinavTarihi.AddMonths(item.TezTeslimSuresiAy + 1);
                    worksheet.Cells[currentRow, 9].Style.Numberformat.Format = "dd.MM.yyyy";
                    worksheet.Cells[currentRow, 10].Value = item.CiltliTezTeslimUzatmaTalebiEykDaOnayEYKTarihi;
                    worksheet.Cells[currentRow, 10].Style.Numberformat.Format = "dd.MM.yyyy";
                    worksheet.Cells[currentRow, 11].Value = item.CiltliTezTeslimUzatmaTalebiEykDaOnayEYKSayisi;
                }

                worksheet.Cells.AutoFitColumns();

                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;

                var fileName = $"Ek Süre Talebi Tutanak Raporu {basTar} {bitTar}.xlsx";
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }

        }



    }
}