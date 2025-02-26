using BiskaUtil;
using Entities.Entities;
using LisansUstuBasvuruSistemi.Utilities.Dtos;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Web;
using LisansUstuBasvuruSistemi.Utilities.MenuAndRoles;
using LisansUstuBasvuruSistemi.Utilities.SystemSetting;

namespace LisansUstuBasvuruSistemi.Business
{
    public static class UserBus
    {
        public static FrKullanicilarDto GetUser(string userName = null)
        {
            var identityName = userName ?? HttpContext.Current.User.Identity.Name;
            using (var entities = new LubsDbEntities())
            {

                var q = from s in entities.Kullanicilars
                        where s.KullaniciAdi == identityName
                        select new FrKullanicilarDto
                        {
                            KullaniciID = s.KullaniciID,
                            UserKey = s.UserKey,
                            EnstituKod = s.EnstituKod,
                            ResimAdi = s.ResimAdi,
                            YetkiGrupID = s.YetkiGrupID,
                            KullaniciTipID = s.KullaniciTipID,
                            KullaniciTipAdi = s.KullaniciTipleri.KullaniciTipAdi,
                            Sifre = s.Sifre,
                            SicilNo = s.SicilNo,
                            Ad = s.Ad,
                            Soyad = s.Soyad,
                            UnvanID = s.UnvanID,
                            BirimID = s.BirimID,
                            CinsiyetID = s.CinsiyetID,
                            TcKimlikNo = s.TcKimlikNo,
                            CepTel = s.CepTel,
                            EMail = s.EMail,
                            Adres = s.Adres,
                            YtuOgrencisi = s.YtuOgrencisi,
                            OgrenimTipKod = s.OgrenimTipKod,
                            ProgramKod = s.ProgramKod,
                            OgrenimDurumID = s.OgrenimDurumID,
                            FixedHeader = s.FixedHeader,
                            FixedSidebar = s.FixedSidebar,
                            ScrollSidebar = s.ScrollSidebar,
                            RightSidebar = s.RightSidebar,
                            CustomNavigation = s.CustomNavigation,
                            ToggledNavigation = s.ToggledNavigation,
                            BoxedOrFullWidth = s.BoxedOrFullWidth,
                            ThemeName = s.ThemeName,
                            BackgroundImage = s.BackgroundImage,
                            SifresiniDegistirsin = s.SifresiniDegistirsin,
                            IsAktif = s.IsAktif,
                            IsActiveDirectoryUser = s.IsActiveDirectoryUser,
                            IsAdmin = s.IsAdmin,
                            IsOnline = s.IsOnline,
                            Aciklama = s.Aciklama,
                            ParolaSifirlamaKodu = s.ParolaSifirlamaKodu,
                            ParolaSifirlamGecerlilikTarihi = s.ParolaSifirlamGecerlilikTarihi,
                            OlusturmaTarihi = s.OlusturmaTarihi,
                            LastLogonDate = s.LastLogonDate,
                            LastLogonIP = s.LastLogonIP,
                            IslemTarihi = s.IslemTarihi,
                            IslemYapanIP = s.IslemYapanIP
                        };
                var kull = q.FirstOrDefault();
                return kull;
            }
        }
        public static FrKullanicilarDto GetUser(int kullaniciId)
        {

            using (var entities = new LubsDbEntities())
            {
                var q = from s in entities.Kullanicilars
                        where s.KullaniciID == kullaniciId
                        select new FrKullanicilarDto
                        {
                            EnstituKod = s.EnstituKod,
                            KullaniciID = s.KullaniciID,
                            UserKey = s.UserKey,
                            ResimAdi = s.ResimAdi,
                            YetkiGrupID = s.YetkiGrupID,
                            KullaniciAdi = s.KullaniciAdi,
                            KullaniciTipID = s.KullaniciTipID,
                            KullaniciTipAdi = s.KullaniciTipleri.KullaniciTipAdi,
                            Sifre = s.Sifre,
                            SicilNo = s.SicilNo,
                            Ad = s.Ad,
                            Soyad = s.Soyad,
                            UnvanID = s.UnvanID,
                            BirimID = s.BirimID,
                            CinsiyetID = s.CinsiyetID,
                            TcKimlikNo = s.TcKimlikNo,
                            CepTel = s.CepTel,
                            EMail = s.EMail,
                            Adres = s.Adres,
                            YtuOgrencisi = s.YtuOgrencisi,
                            OgrenimTipKod = s.OgrenimTipKod,
                            ProgramKod = s.ProgramKod,
                            OgrenimDurumID = s.OgrenimDurumID,
                            FixedHeader = s.FixedHeader,
                            FixedSidebar = s.FixedSidebar,
                            ScrollSidebar = s.ScrollSidebar,
                            RightSidebar = s.RightSidebar,
                            CustomNavigation = s.CustomNavigation,
                            ToggledNavigation = s.ToggledNavigation,
                            BoxedOrFullWidth = s.BoxedOrFullWidth,
                            ThemeName = s.ThemeName,
                            BackgroundImage = s.BackgroundImage,
                            SifresiniDegistirsin = s.SifresiniDegistirsin,
                            IsAktif = s.IsAktif,
                            IsActiveDirectoryUser = s.IsActiveDirectoryUser,
                            IsAdmin = s.IsAdmin,
                            IsOnline = s.IsOnline,
                            Aciklama = s.Aciklama,
                            ParolaSifirlamaKodu = s.ParolaSifirlamaKodu,
                            ParolaSifirlamGecerlilikTarihi = s.ParolaSifirlamGecerlilikTarihi,
                            OlusturmaTarihi = s.OlusturmaTarihi,
                            LastLogonDate = s.LastLogonDate,
                            LastLogonIP = s.LastLogonIP,
                            IslemTarihi = s.IslemTarihi,
                            IslemYapanIP = s.IslemYapanIP
                        };
                var kull = q.FirstOrDefault();
                return kull;
            }
        }
        public static FrKullanicilarDto GetLoginUser(string kullaniciAdi)
        {
            using (var entities = new LubsDbEntities())
            {
                var q = from s in entities.Kullanicilars.Where(p => p.IsAktif)
                        join ktl in entities.KullaniciTipleris on new { s.KullaniciTipID } equals new { ktl.KullaniciTipID }
                        select new FrKullanicilarDto
                        {
                            EnstituKod = s.EnstituKod,
                            KullaniciID = s.KullaniciID,
                            UserKey = s.UserKey,
                            ResimAdi = s.ResimAdi,
                            YetkiGrupID = s.YetkiGrupID,
                            KullaniciTipID = s.KullaniciTipID,
                            KullaniciTipAdi = ktl.KullaniciTipAdi,
                            KullaniciAdi = s.KullaniciAdi,
                            Sifre = s.Sifre,
                            SicilNo = s.SicilNo,
                            Ad = s.Ad,
                            Soyad = s.Soyad,
                            UnvanID = s.UnvanID,
                            BirimID = s.BirimID,
                            CinsiyetID = s.CinsiyetID,
                            TcKimlikNo = s.TcKimlikNo,
                            CepTel = s.CepTel,
                            EMail = s.EMail,
                            Adres = s.Adres,
                            YtuOgrencisi = s.YtuOgrencisi,
                            OgrenciNo = s.OgrenciNo,
                            OgrenimTipKod = s.OgrenimTipKod,
                            ProgramKod = s.ProgramKod,
                            OgrenimDurumID = s.OgrenimDurumID,
                            FixedHeader = s.FixedHeader,
                            FixedSidebar = s.FixedSidebar,
                            ScrollSidebar = s.ScrollSidebar,
                            RightSidebar = s.RightSidebar,
                            CustomNavigation = s.CustomNavigation,
                            ToggledNavigation = s.ToggledNavigation,
                            BoxedOrFullWidth = s.BoxedOrFullWidth,
                            ThemeName = s.ThemeName,
                            BackgroundImage = s.BackgroundImage,
                            SifresiniDegistirsin = s.SifresiniDegistirsin,
                            IsAktif = s.IsAktif,
                            IsActiveDirectoryUser = s.IsActiveDirectoryUser,
                            IsAdmin = s.IsAdmin,
                            IsOnline = s.IsOnline,
                            Aciklama = s.Aciklama,
                            ParolaSifirlamaKodu = s.ParolaSifirlamaKodu,
                            ParolaSifirlamGecerlilikTarihi = s.ParolaSifirlamGecerlilikTarihi,
                            OlusturmaTarihi = s.OlusturmaTarihi,
                            LastLogonDate = s.LastLogonDate,
                            LastLogonIP = s.LastLogonIP,
                            IslemTarihi = s.IslemTarihi,
                            IslemYapanIP = s.IslemYapanIP
                        };
                var kull = q.FirstOrDefault(p => p.KullaniciAdi == kullaniciAdi || p.TcKimlikNo == kullaniciAdi || p.EMail == kullaniciAdi);
                return kull;
            }
        }
        public static Kullanicilar Login(string kullaniciAdi, string pwd)
        {
            using (var entities = new LubsDbEntities())
            {
                var sifre = pwd.ComputeHash(GlobalSistemSetting.Tuz);
                var kullanici = entities.Kullanicilars.Where(p=>p.IsAktif).FirstOrDefault(p => p.KullaniciAdi == kullaniciAdi || p.TcKimlikNo == kullaniciAdi || p.EMail == kullaniciAdi);
                if (kullanici != null)
                {
                    return kullanici.Sifre == sifre ? kullanici : null;
                }
                return null;
            }
        }
        public static Enstituler[] GetKullaniciEnstituler(int kullaniciId)
        {
            using (var entities = new LubsDbEntities())
            {
                return entities.Enstitulers.Where(p => p.IsAktif && entities.KullaniciEnstituYetkileris.Any(a => p.EnstituKod == a.EnstituKod && a.KullaniciID == kullaniciId)).OrderBy(o => o.EnstituAd).ToArray();

            }
        }
        public static Menuler[] GetUserMenus()
        {
            var userName = HttpContext.Current.User.Identity.Name;

            if (userName.IsNullOrWhiteSpace()) return new Menuler[] { };
            using (var entities = new LubsDbEntities())
            {
                var menus = new List<Menuler>();
                var kull = entities.Kullanicilars.FirstOrDefault(p => p.KullaniciAdi == userName);
                if (kull == null)
                {
                    FormsAuthenticationUtil.SignOut();
                    return new Menuler[] { };
                } 
                var allMenus = MenulerBus.GetAllMenu();
                var kullRollIds = kull.YetkiGruplari.YetkiGrupRolleris.Select(s => s.RolID).ToList();
                kullRollIds.AddRange(kull.Rollers.Select(s=>s.RolID).ToList());
                var kullaniciMenuleris = allMenus.Where(p=>p.YetkisizErisim==true || p.Rollers.Any(a=>  kullRollIds.Contains(a.RolID))).OrderBy(o => o.SiraNo).ToList(); 
                menus.AddRange(kullaniciMenuleris);
                return menus.ToArray();
            }

        }

        public static UserRoleDto GetUserRoles(string userName = null)
        {
            var identityName = userName ?? HttpContext.Current.User.Identity.Name;
            var rolls = new UserRoleDto();
            using (var entities = new LubsDbEntities())
            {
                var kull = entities.Kullanicilars.FirstOrDefault(p => p.KullaniciAdi == identityName);
                if (kull != null)
                {
                    var kullRoll = kull.Rollers.ToList();

                    var ygRols = kull.YetkiGruplari.YetkiGrupRolleris.Select(s => s.Roller).ToList();
                    rolls.YetkiGrupID = kull.YetkiGrupID;
                    rolls.YetkiGrupAdi = kull.YetkiGruplari.YetkiGrupAdi;
                    rolls.YetkiGrupRolleri = ygRols;
                    rolls.TumRoller.AddRange(ygRols);
                    rolls.TumRoller.AddRange(kullRoll.Where(p => ygRols.All(a => a.RolID != p.RolID)));
                    rolls.EklenenRoller.AddRange(rolls.TumRoller.Where(p => rolls.YetkiGrupRolleri.Any(a => a.RolID == p.RolID) == false));
                    return rolls;


                }

                FormsAuthenticationUtil.SignOut();
                throw new SecurityException("Kullanıcı Tanımlı Değil");

            }
        }
        public static UserRoleDto GetUserRoles(int kullaniciId)
        {
            var rolls = new UserRoleDto();
            using (var entities = new LubsDbEntities())
            {
                var kull = entities.Kullanicilars.FirstOrDefault(p => p.KullaniciID == kullaniciId);
                if (kull == null) throw new SecurityException("Kullanıcı Tanımlı Değil");

                var dRoll = kull.Rollers.ToList();

                var ygRols = kull.YetkiGruplari.YetkiGrupRolleris.Select(s => s.Roller).ToList();
                rolls.YetkiGrupID = kull.YetkiGrupID;
                rolls.YetkiGrupAdi = kull.YetkiGruplari.YetkiGrupAdi;
                rolls.YetkiGrupRolleri = ygRols;
                rolls.TumRoller.AddRange(ygRols);
                rolls.TumRoller.AddRange(dRoll.Where(p => ygRols.All(a => a.RolID != p.RolID)).Distinct());
                rolls.EklenenRoller.AddRange(rolls.TumRoller.Where(p => rolls.YetkiGrupRolleri.Any(a => a.RolID == p.RolID) == false));
                return rolls;


            }
        }
        public static bool InRoleCurrent(this string roleName)
        {
            if (UserIdentity.Current != null && UserIdentity.Current.Roles != null)
            {
                return UserIdentity.Current.Roles.Any(a => a == roleName);
            }

            return false;
        }
        public static void SetUserRoles(int kullaniciId, List<int> rolIDs, int yetkiGrupId)
        {
            using (var entities = new LubsDbEntities())
            {
                var k = entities.Kullanicilars.FirstOrDefault(p => p.KullaniciID == kullaniciId);
                if (k != null)
                {

                    var droles = k.Rollers.ToArray();
                    foreach (var drole in droles)
                        k.Rollers.Remove(drole);
                    k.YetkiGrupID = yetkiGrupId;
                    entities.SaveChanges();
                    var uRoles = UserBus.GetUserRoles(k.KullaniciID);
                    rolIDs = rolIDs.Where(p => uRoles.YetkiGrupRolleri.All(a => a.RolID != p)).ToList();

                    if (rolIDs.Count > 0)
                    {
                        var newRoles = entities.Rollers.Where(p => rolIDs.Contains(p.RolID));
                        foreach (var nr in newRoles)
                            k.Rollers.Add(nr);
                        entities.SaveChanges();
                    }
                }
                else
                    throw new SecurityException("Kullanıcı Tanımlı Değil");
            }
        }
        public static List<Kullanicilar> GetRoluOlanKullanicilar(List<string> rolAdi, string enstituKod = null)
        {
            using (var entities = new LubsDbEntities())
            {
                var qRolKuls = entities.Kullanicilars.Include("KullaniciProgramlaris").Include("Birimler").Include("KullaniciTipleri").Where(p => p.YetkiGruplari.YetkiGrupRolleris.Any(a => rolAdi.Contains(a.Roller.RolAdi)) || p.Rollers.Any(a => rolAdi.Contains(a.RolAdi))).AsQueryable();

                if (enstituKod.IsNullOrWhiteSpace() == false) qRolKuls = qRolKuls.Where(p => p.EnstituKod == enstituKod);
                var data = qRolKuls.OrderByDescending(o => o.Ad).ThenBy(t => t.Soyad).ToList();
                return data;
            }
        }
        public static List<string> GetUserEnstituKods(int kullaniciId)
        {
            using (var entities = new LubsDbEntities())
            {
                return entities.KullaniciEnstituYetkileris.Where(a => a.KullaniciID == kullaniciId).Select(s => s.EnstituKod).ToList();

            }
        }
        public static List<string> GetUserProgramKods(int kullaniciId, string enstituKod)
        {
            using (var entities = new LubsDbEntities())
            {
                var kullProg = (from kp in entities.KullaniciProgramlaris.Where(a => a.KullaniciID == kullaniciId)
                                join s in entities.Programlars on kp.ProgramKod equals s.ProgramKod
                                join b in entities.AnabilimDallaris on s.AnabilimDaliKod equals b.AnabilimDaliKod
                                where b.EnstituKod == enstituKod
                                select s.ProgramKod
                    ).ToList();
                return kullProg;

            }
        }
        public static UserIdentity GetUserIdentity(string userName)
        {

            var kull = GetUser(userName);
            if (kull == null)
            {
                FormsAuthenticationUtil.SignOut();
                return null;
            }


            var roller = UserBus.GetUserRoles(kull.KullaniciID);

            var ui = new UserIdentity(userName)
            {
                NameSurname = kull.Ad + " " + kull.Soyad,
                Id = kull.KullaniciID,
                YetkiGrupId = kull.YetkiGrupID,
                UserKey = kull.UserKey,
                Description = kull.EMail,
                IsAdmin = kull.IsAdmin,
                //ui.Password = kull.Sifre;
                //ui.Domain = "";
                HasToChahgePassword = kull.SifresiniDegistirsin,
                IsActiveDirectoryImpersonateWorking = false,
                IsActiveDirectoryUser = kull.IsActiveDirectoryUser,

            };

            ui.Roles.AddRange(roller.TumRoller.Select(s => s.RolAdi).ToArray());
            ui.ImagePath = kull.ResimAdi.ToKullaniciResim();
            ui.Informations.Add("FixedHeader", kull.FixedHeader);
            ui.Informations.Add("FixedSidebar", kull.FixedSidebar);
            ui.Informations.Add("ScrollSidebar", kull.ScrollSidebar);
            ui.Informations.Add("RightSidebar", kull.RightSidebar);
            ui.Informations.Add("CustomNavigation", kull.CustomNavigation);
            ui.Informations.Add("ToggledNavigation", kull.ToggledNavigation);
            ui.Informations.Add("BoxedOrFullWidth", kull.BoxedOrFullWidth);
            ui.Informations.Add("ThemeName", kull.ThemeName);
            ui.Informations.Add("BackgroundImage", kull.BackgroundImage);
            ui.KullaniciTipId = kull.KullaniciTipID;

            ui.EnstituKods = UserBus.GetUserEnstituKods(kull.KullaniciID);
            ui.SeciliEnstituKodu = kull.EnstituKod;


            ui.IsYetkiliTij = ui.Roles.Any(a =>
                a == RoleNames.TiJuriOnerileriEykYaGonder || a == RoleNames.TiJuriOnerileriEykDaOnay);

            #region Last Logon Information
            UserBus.SetLastLogon();
            #endregion
            return ui;
        }
        public static void SetLastLogon()
        {
            var userName = HttpContext.Current.User.Identity.Name;
            using (var entities = new LubsDbEntities())
            {
                var kull = entities.Kullanicilars.FirstOrDefault(p => p.KullaniciAdi == userName);
                if (kull == null) return;
                kull.LastLogonDate = DateTime.Now;
                kull.LastLogonIP = UserIdentity.Ip;
                entities.SaveChanges();
            }
        }
    }
}