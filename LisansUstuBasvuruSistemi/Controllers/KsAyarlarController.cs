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

            // Çoklu kullanıcı ID'lerini virgülle ayrılmış string olarak al
            var selectedHarcUserIds = KayitSilmeAyar.HarcBirimiOnaySorumlusuKullaniciId.GetAyar(enstituKod);
            var selectedKutuphaneUserIds = KayitSilmeAyar.KutuphaneBirimiOnaySorumlusuKullaniciId.GetAyar(enstituKod);

            // String'leri int listelerine çevir
            var harcUserIdList = !selectedHarcUserIds.IsNullOrWhiteSpace()
                ? selectedHarcUserIds.Split(',').Select(s => s.Trim().ToIntObj()).Where(id => id.HasValue).Select(id => id.Value).ToList()
                : new List<int>();

            var kutuphaneUserIdList = !selectedKutuphaneUserIds.IsNullOrWhiteSpace()
                ? selectedKutuphaneUserIds.Split(',').Select(s => s.Trim().ToIntObj()).Where(id => id.HasValue).Select(id => id.Value).ToList()
                : new List<int>();

            // Tüm kullanıcı ID'lerini birleştir
            var allSelectedUserIds = harcUserIdList.Union(kutuphaneUserIdList).Distinct().ToList();

            // Kullanıcı bilgilerini getir
            var users = allSelectedUserIds.Any()
                ? _entities.Kullanicilars.Where(k => allSelectedUserIds.Contains(k.KullaniciID)).ToList()
                : new List<Kullanicilar>();

            // Harc birimi kullanıcıları
            var harcUsers = users.Where(u => harcUserIdList.Contains(u.KullaniciID)).ToList();
            var harcUsersText = harcUsers.Any()
                ? string.Join(", ", harcUsers.Select(u => $"{u.Unvanlar.UnvanAdi} {u.Ad} {u.Soyad}"))
                : string.Empty;

            // Kütüphane birimi kullanıcıları
            var kutuphaneUsers = users.Where(u => kutuphaneUserIdList.Contains(u.KullaniciID)).ToList();
            var kutuphaneUsersText = kutuphaneUsers.Any()
                ? string.Join(", ", kutuphaneUsers.Select(u => $"{u.Unvanlar.UnvanAdi} {u.Ad} {u.Soyad}"))
                : string.Empty;

            var selectedOnaySorumlulari = new Dictionary<string, string>
            {
                [KayitSilmeAyar.HarcBirimiOnaySorumlusuKullaniciId.ToString()] = harcUsersText,
                [KayitSilmeAyar.KutuphaneBirimiOnaySorumlusuKullaniciId.ToString()] = kutuphaneUsersText
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
                    // Eski ve yeni kullanıcı ID'lerini al (çoklu)
                    var eskiKullaniciIds = !ayar.AyarDegeri.IsNullOrWhiteSpace()
                        ? ayar.AyarDegeri.Split(',').Select(s => s.Trim().ToIntObj()).Where(id => id.HasValue).Select(id => id.Value).ToList()
                        : new List<int>();

                    var yeniKullaniciIds = !item.AyarDegeri.IsNullOrWhiteSpace()
                        ? item.AyarDegeri.Split(',').Select(s => s.Trim().ToIntObj()).Where(id => id.HasValue).Select(id => id.Value).ToList()
                        : new List<int>();

                    // Kullanıcı ID listeleri değiştiyse rolleri güncelle
                    var eskiSet = new HashSet<int>(eskiKullaniciIds);
                    var yeniSet = new HashSet<int>(yeniKullaniciIds);

                    if (!eskiSet.SetEquals(yeniSet))
                    {
                        // Hangi ayar türü olduğunu belirle
                        bool isHarcBirimiAyar = item.AyarAdi == KayitSilmeAyar.HarcBirimiOnaySorumlusuKullaniciId.ToString();
                        bool isKutuphaneBirimiAyar = item.AyarAdi == KayitSilmeAyar.KutuphaneBirimiOnaySorumlusuKullaniciId.ToString();

                        // Kaldırılacak kullanıcılar (eski listede var, yeni listede yok)
                        var kaldırılacakKullanicilar = eskiKullaniciIds.Except(yeniKullaniciIds).ToList();

                        // Eklenecek kullanıcılar (yeni listede var, eski listede yok)
                        var eklenecekKullanicilar = yeniKullaniciIds.Except(eskiKullaniciIds).ToList();

                        // Eski kullanıcılardan rolleri kaldır
                        foreach (var kullaniciId in kaldırılacakKullanicilar)
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

                            UserBus.RemoveUserRoles(kullaniciId, rolesToRemove);
                        }

                        // Yeni kullanıcılara rolleri ekle
                        foreach (var kullaniciId in eklenecekKullanicilar)
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

                            UserBus.AddUserRoles(kullaniciId, rolesToAdd);
                        }
                    }

                    // Ayar değerini güncelle
                    ayar.AyarDegeri = item.AyarDegeri;
                }
            }

            _entities.SaveChanges();
            MessageBox.Show("Kayıt Silme Ayarları Güncellendi", MessageBox.MessageType.Success);

            // Güncellenmiş verileri tekrar yükle
            var data = _entities.KayitSilmeAyarlars.Where(p => p.EnstituKod == enstituKod ? UserIdentity.Current.EnstituKods.Contains(p.EnstituKod) && p.EnstituKod == enstituKod : p.EnstituKod == "").OrderBy(o => o.Kategori).ThenBy(t => t.SiraNo).ToList();

            var toggled = new Dictionary<string, bool>();
            foreach (var item in panelToggled)
            {
                var ptg = item.Replace("__", "◘").Split('◘');
                toggled.Add(ptg[0], ptg[1].ToBoolean().Value);
            }
            ViewBag.PanelToggled = toggled;

            // Çoklu kullanıcı bilgilerini tekrar yükle
            var selectedHarcUserIds = KayitSilmeAyar.HarcBirimiOnaySorumlusuKullaniciId.GetAyar(enstituKod);
            var selectedKutuphaneUserIds = KayitSilmeAyar.KutuphaneBirimiOnaySorumlusuKullaniciId.GetAyar(enstituKod);

            var harcUserIdList = !selectedHarcUserIds.IsNullOrWhiteSpace()
                ? selectedHarcUserIds.Split(',').Select(s => s.Trim().ToIntObj()).Where(id => id.HasValue).Select(id => id.Value).ToList()
                : new List<int>();

            var kutuphaneUserIdList = !selectedKutuphaneUserIds.IsNullOrWhiteSpace()
                ? selectedKutuphaneUserIds.Split(',').Select(s => s.Trim().ToIntObj()).Where(id => id.HasValue).Select(id => id.Value).ToList()
                : new List<int>();

            var allSelectedUserIds = harcUserIdList.Union(kutuphaneUserIdList).Distinct().ToList();

            var users = allSelectedUserIds.Any()
                ? _entities.Kullanicilars.Where(k => allSelectedUserIds.Contains(k.KullaniciID)).ToList()
                : new List<Kullanicilar>();

            var harcUsers = users.Where(u => harcUserIdList.Contains(u.KullaniciID)).ToList();
            var harcUsersText = harcUsers.Any()
                ? string.Join(", ", harcUsers.Select(u => $"{u.Unvanlar.UnvanAdi} {u.Ad} {u.Soyad}"))
                : string.Empty;

            var kutuphaneUsers = users.Where(u => kutuphaneUserIdList.Contains(u.KullaniciID)).ToList();
            var kutuphaneUsersText = kutuphaneUsers.Any()
                ? string.Join(", ", kutuphaneUsers.Select(u => $"{u.Unvanlar.UnvanAdi} {u.Ad} {u.Soyad}"))
                : string.Empty;

            var selectedOnaySorumlulari = new Dictionary<string, string>
            {
                [KayitSilmeAyar.HarcBirimiOnaySorumlusuKullaniciId.ToString()] = harcUsersText,
                [KayitSilmeAyar.KutuphaneBirimiOnaySorumlusuKullaniciId.ToString()] = kutuphaneUsersText
            };

            ViewBag.SelectedOnaySorumlulari = selectedOnaySorumlulari;

            return View(data);
        }
    }
}