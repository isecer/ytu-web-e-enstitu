using System.Linq;
using BiskaUtil;
using LisansUstuBasvuruSistemi.Models;

namespace LisansUstuBasvuruSistemi.Business
{
    public class RollerBus
    {
        public static Roller[] Roles { get; set; }

        public static Roller[] GetAllRoles()
        {
            if (Roles == null)
            {
                using (var db = new LisansustuBasvuruSistemiEntities())
                {
                    Roles = db.Rollers.Include("Menulers").ToArray();
                }
            }
            return Roles;
        }

        public static void UpdateRoles()
        {
            var roleAttrs = Membership.Roles();
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                var dbRoller = db.Rollers.ToArray();
                foreach (var attr in roleAttrs)
                {
                    var dbrole = dbRoller.FirstOrDefault(p => p.RolID == attr.RolID);

                    if (dbrole == null)
                    {
                        db.Rollers.Add(new Roller
                        {
                            RolID = attr.RolID,
                            GorunurAdi = attr.GorunurAdi,
                            Aciklama = attr.Aciklama,
                            Kategori = attr.Kategori,
                            RolAdi = attr.RolAdi
                        });
                    }
                    else
                    {
                        dbrole.RolID = attr.RolID;
                        dbrole.GorunurAdi = attr.GorunurAdi;
                        dbrole.Aciklama = attr.Aciklama;
                        dbrole.Kategori = attr.Kategori;
                        dbrole.RolAdi = attr.RolAdi;
                    }
                }

                var silinenRoller = dbRoller.Where(p => roleAttrs.All(a => a.RolID != p.RolID)).ToList();
                var silinenRolIds = silinenRoller.Select(s => s.RolID).ToList();
                var silinecekKullaniciRolleris = db.Kullanicilars
                    .Where(p => p.Rollers.Any(a => silinenRolIds.Contains(a.RolID))).ToList();
                foreach (var kul in silinecekKullaniciRolleris)
                {
                    foreach (var rol in silinenRoller)
                    {
                        kul.Rollers.Remove(rol);
                    }
                }

                var yetkiGrupRolleris = db.YetkiGrupRolleris.Where(p => silinenRolIds.Contains(p.RolID)).ToList();
                db.YetkiGrupRolleris.RemoveRange(yetkiGrupRolleris);
                foreach (var rol in silinenRoller)
                { 
                    db.Rollers.Remove(rol);

                }

                db.SaveChanges();
            }
        }
    }
}