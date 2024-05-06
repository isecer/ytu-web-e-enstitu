using System.Linq;
using BiskaUtil;
using Entities.Entities;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.Helpers;

namespace LisansUstuBasvuruSistemi.Business
{
    public class RollerBus
    {
        public static Roller[] Roles { get; set; }

        public static Roller[] GetAllRoles()
        {
            if (Roles == null)
            {
                using (var entities = new LubsDbEntities())
                {
                    Roles = entities.Rollers.Include("Menulers").ToArray();
                }
            }
            return Roles;
        }

        public static void UpdateRoles()
        {
            var roleAttrs = Membership.Roles();
            var roleKeyGroup = roleAttrs.GroupBy(g => g.RolID).Select(s => new { s.Key, Count = s.Count(), Rolls = s.Select(sr => sr.RolAdi).ToList() });
            var duplicateKeys = roleKeyGroup.Where(p => p.Count > 1).ToList();

            if (duplicateKeys.Any())
            { 
                var duplicateStringList = duplicateKeys.Select(s => s.Key + " => " + string.Join(",", s.Rolls)).ToList();
                var duplicateString = string.Join("<br/>", duplicateStringList);
                SistemBilgilendirmeBus.SistemBilgisiKaydet("Sistem Rolleri güncellenirken Unique olmayan roll id bilgilerine rastlandı! \r\n " + duplicateString, ObjectExtensions.GetCurrentMethodPath(), BilgiTipiEnum.Kritik);
                return;
            }

            using (var entities = new LubsDbEntities())
            {
                var dbRoller = entities.Rollers.ToArray();
                foreach (var attr in roleAttrs)
                {
                    var dbrole = dbRoller.FirstOrDefault(p => p.RolID == attr.RolID);

                    if (dbrole == null)
                    {
                        SistemBilgilendirmeBus.SistemBilgisiKaydet("Eklenen Rol: " + attr.ToJson(), ObjectExtensions.GetCurrentMethodPath(), BilgiTipiEnum.Bilgi);
                        entities.Rollers.Add(new Roller
                        {
                            RolID = attr.RolID,
                            SiraNo = attr.SiraNo,
                            GorunurAdi = attr.GorunurAdi,
                            Aciklama = attr.Aciklama,
                            Kategori = attr.Kategori,
                            RolAdi = attr.RolAdi
                        });
                    }
                    else
                    {
                        dbrole.RolID = attr.RolID;
                        dbrole.SiraNo = attr.SiraNo;
                        dbrole.GorunurAdi = attr.GorunurAdi;
                        dbrole.Aciklama = attr.Aciklama;
                        dbrole.Kategori = attr.Kategori;
                        dbrole.RolAdi = attr.RolAdi;
                    }
                }

                var silinenRoller = dbRoller.Where(p => roleAttrs.All(a => a.RolID != p.RolID)).ToList();
                var silinenRolIds = silinenRoller.Select(s => s.RolID).ToList();
                var silinecekKullaniciRolleris = entities.Kullanicilars
                    .Where(p => p.Rollers.Any(a => silinenRolIds.Contains(a.RolID))).ToList();
                foreach (var kul in silinecekKullaniciRolleris)
                {
                    foreach (var rol in silinenRoller)
                    {
                        kul.Rollers.Remove(rol);
                    }
                }

                var yetkiGrupRolleris = entities.YetkiGrupRolleris.Where(p => silinenRolIds.Contains(p.RolID)).ToList();
                foreach (var ygItem in yetkiGrupRolleris)
                {
                    SistemBilgilendirmeBus.SistemBilgisiKaydet("Silinen Yetki Grubu Rolu: " + ygItem.ToJson(), ObjectExtensions.GetCurrentMethodPath(), BilgiTipiEnum.Bilgi);
                }
                entities.YetkiGrupRolleris.RemoveRange(yetkiGrupRolleris);
                foreach (var rol in silinenRoller)
                {
                    SistemBilgilendirmeBus.SistemBilgisiKaydet("Silinen Rol: " + rol.ToJson(), ObjectExtensions.GetCurrentMethodPath(), BilgiTipiEnum.Bilgi);

                    entities.Rollers.Remove(rol);

                }

                entities.SaveChanges();
                SistemBilgilendirmeBus.SistemBilgisiKaydet("UpdateRoles", ObjectExtensions.GetCurrentMethodPath(), BilgiTipiEnum.Bilgi);
            }
        }
    }
}