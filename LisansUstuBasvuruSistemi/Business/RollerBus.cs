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
            var roleAttrs = MenulerBus.Roles();
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
                    db.SaveChanges();
                }
            }
        }
    }
}