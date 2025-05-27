using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Business;
using Entities.Entities;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.MenuAndRoles;
using LisansUstuBasvuruSistemi.Utilities.SystemSetting;

namespace LisansUstuBasvuruSistemi.Controllers
{
    [OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
    [Authorize(Roles = RoleNames.KayitSilmeAyarları)]
    public class KsAyarlarController : Controller
    {
        private readonly LubsDbEntities _entities = new LubsDbEntities();
        public ActionResult Index(string ekd)
        {
            string enstituKod = EnstituBus.GetSelectedEnstitu(ekd);
            var data = _entities.KayitSilmeAyarlars.Where(p => p.EnstituKod == enstituKod ? UserIdentity.Current.EnstituKods.Contains(p.EnstituKod) && p.EnstituKod == enstituKod : p.EnstituKod == "").OrderBy(o => o.Kategori).ThenBy(t => t.SiraNo).ToList();
            var cats = data.Select(s => new { s.Kategori, Toggle = true }).Distinct().ToList();
            var panelToggled = cats.ToDictionary(item => item.Kategori, item => item.Toggle);
            ViewBag.PanelToggled = panelToggled;
            var selectedHarcUserId = KayitSilmeAyar.HarcBirimiOnaySorumlusuKullaniciId.GetAyar(enstituKod).ToIntObj();
            var selectedKutuphaneUserId = KayitSilmeAyar.KutuphaneBirimiOnaySorumlusuKullaniciId.GetAyar(enstituKod).ToIntObj();

            var selectedUserIds = new List<int?> { selectedHarcUserId, selectedKutuphaneUserId }
                .Where(id => id.HasValue)
                .Select(id => id.Value)
                .ToList();

            var users = _entities.Kullanicilars
                .Where(k => selectedUserIds.Contains(k.KullaniciID))
                .ToList();

            var harcUser = users.FirstOrDefault(u => u.KullaniciID == selectedHarcUserId);
            var kutuphaneUser = users.FirstOrDefault(u => u.KullaniciID == selectedKutuphaneUserId);

            var selectedOnaySorumlulari = new Dictionary<string, string>
            {
                [KayitSilmeAyar.HarcBirimiOnaySorumlusuKullaniciId.ToString()] = harcUser != null
                    ? $"{harcUser.Unvanlar.UnvanAdi} {harcUser.Ad} {harcUser.Soyad}"
                    : string.Empty,

                [KayitSilmeAyar.KutuphaneBirimiOnaySorumlusuKullaniciId.ToString()] = kutuphaneUser != null
                    ? $"{kutuphaneUser.Unvanlar.UnvanAdi} {kutuphaneUser.Ad} {kutuphaneUser.Soyad}"
                    : string.Empty
            };

            ViewBag.SelectedOnaySorumlulari = selectedOnaySorumlulari;

            return View(data);
        }
        [HttpPost]
        public ActionResult Index(List<string> ayarAdi, List<string> ayarDegeri, List<string> panelToggled, string ekd)
        {
            string enstituKod = EnstituBus.GetSelectedEnstitu(ekd);
            var qSistemAyarAdi = ayarAdi.Select((s, index) => new { inx = index, s }).ToList();
            var qSistemAyarDegeri = ayarDegeri.Select((s, index) => new { inx = index, s }).ToList();

            var qModel = (from sa in qSistemAyarAdi
                          join sad in qSistemAyarDegeri on sa.inx equals sad.inx
                          select new
                          {
                              RowID = sa.inx,
                              AyarAdi = sa.s,
                              AyarDegeri = sad.s,
                          }).ToList();
             
            foreach (var item in qModel)
            {
                var ayar = _entities.KayitSilmeAyarlars.FirstOrDefault(p => p.AyarAdi == item.AyarAdi && (p.EnstituKod == enstituKod || p.EnstituKod == ""));
                if (ayar != null)
                {
                    // Eski ve yeni kullanıcı ID'lerini al
                    int? eskiKullaniciId = !ayar.AyarDegeri.IsNullOrWhiteSpace() ? ayar.AyarDegeri.ToInt() : null;
                    int? yeniKullaniciId = !item.AyarDegeri.IsNullOrWhiteSpace() ? item.AyarDegeri.ToInt() : null;

                    // Kullanıcı ID değiştiyse veya yeni ID boşsa rolleri güncelle
                    if (eskiKullaniciId != yeniKullaniciId)
                    {
                        // Hangi ayar türü olduğunu belirle ve ona göre rolleri ata
                        bool isHarcBirimiAyar = item.AyarAdi == KayitSilmeAyar.HarcBirimiOnaySorumlusuKullaniciId.ToString();
                        bool isKutuphaneBirimiAyar = item.AyarAdi == KayitSilmeAyar.KutuphaneBirimiOnaySorumlusuKullaniciId.ToString();

                        // Eski kullanıcıdan rolleri kaldır (eğer eski kullanıcı varsa)
                        if (eskiKullaniciId.HasValue)
                        {
                            var rolesToRemove = new List<string> { RoleNames.KayitSilmeGelenBasvurular };

                            if (isHarcBirimiAyar)
                            {
                                rolesToRemove.Add(RoleNames.KayitSilmeHarcBirimiBasvuruOnayYetkisi);
                            }

                            if (isKutuphaneBirimiAyar)
                            {
                                rolesToRemove.Add(RoleNames.KayitSilmeKutuphaneBirimiBasvuruOnayYetkisi);
                            }

                            UserBus.RemoveUserRoles(eskiKullaniciId.Value, rolesToRemove);
                        }

                        // Yeni kullanıcıya rolleri ekle (eğer yeni kullanıcı varsa)
                        if (yeniKullaniciId.HasValue)
                        {
                            var rolesToAdd = new List<string> { RoleNames.KayitSilmeGelenBasvurular };

                            if (isHarcBirimiAyar)
                            {
                                rolesToAdd.Add(RoleNames.KayitSilmeHarcBirimiBasvuruOnayYetkisi);
                            }

                            if (isKutuphaneBirimiAyar)
                            {
                                rolesToAdd.Add(RoleNames.KayitSilmeKutuphaneBirimiBasvuruOnayYetkisi);
                            }

                            UserBus.AddUserRoles(yeniKullaniciId.Value, rolesToAdd);
                        }
                    }

                    // Ayar değerini güncelle
                    ayar.AyarDegeri = item.AyarDegeri;
                }
            }
            _entities.SaveChanges();
            MessageBox.Show("Kayıt Silme Ayarları Güncellendi", MessageBox.MessageType.Success);
            var data = _entities.KayitSilmeAyarlars.Where(p => p.EnstituKod == enstituKod ? UserIdentity.Current.EnstituKods.Contains(p.EnstituKod) && p.EnstituKod == enstituKod : p.EnstituKod == "").OrderBy(o => o.Kategori).ThenBy(t => t.SiraNo).ToList();
            var toggled = new Dictionary<string, bool>();
            foreach (var item in panelToggled)
            {
                var ptg = item.Replace("__", "◘").Split('◘');
                toggled.Add(ptg[0], ptg[1].ToBoolean().Value);
            }
            ViewBag.PanelToggled = toggled;
            var selectedHarcUserId = KayitSilmeAyar.HarcBirimiOnaySorumlusuKullaniciId.GetAyar(enstituKod).ToIntObj();
            var selectedKutuphaneUserId = KayitSilmeAyar.KutuphaneBirimiOnaySorumlusuKullaniciId.GetAyar(enstituKod).ToIntObj();

            var selectedUserIds = new List<int?> { selectedHarcUserId, selectedKutuphaneUserId }
                .Where(id => id.HasValue)
                .Select(id => id.Value)
                .ToList();

            var users = _entities.Kullanicilars
                .Where(k => selectedUserIds.Contains(k.KullaniciID))
                .ToList();

            var harcUser = users.FirstOrDefault(u => u.KullaniciID == selectedHarcUserId);
            var kutuphaneUser = users.FirstOrDefault(u => u.KullaniciID == selectedKutuphaneUserId);

            var selectedOnaySorumlulari = new Dictionary<string, string>
            {
                [KayitSilmeAyar.HarcBirimiOnaySorumlusuKullaniciId.ToString()] = harcUser != null
                    ? $"{harcUser.Unvanlar.UnvanAdi} {harcUser.Ad} {harcUser.Soyad}"
                    : string.Empty,

                [KayitSilmeAyar.KutuphaneBirimiOnaySorumlusuKullaniciId.ToString()] = kutuphaneUser != null
                    ? $"{kutuphaneUser.Unvanlar.UnvanAdi} {kutuphaneUser.Ad} {kutuphaneUser.Soyad}"
                    : string.Empty
            };

            ViewBag.SelectedOnaySorumlulari = selectedOnaySorumlulari;

            return View(data);
        }
    }
}