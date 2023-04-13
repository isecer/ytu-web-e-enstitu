using BiskaUtil;
using LisansUstuBasvuruSistemi.Models;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using System;

namespace LisansUstuBasvuruSistemi.Business
{
    public static class SistemBilgilendirmeBus
    {
        public static void SistemBilgisiKaydet(Exception ex, byte bilgiTipi)
        {
            using (var db = new LisansustuBasvuruSistemiEntities())
            {


                db.SistemBilgilendirmes.Add(new SistemBilgilendirme
                {
                    BilgiTipi = bilgiTipi,
                    Message = ex.ToExceptionMessage(),
                    IslemYapanID = UserIdentity.Current != null && UserIdentity.Current.IsAuthenticated && UserIdentity.Current.Id > 0 ? UserIdentity.Current.Id : (int?)null,
                    IslemYapanIP = UserIdentity.Ip,
                    IslemTarihi = DateTime.Now,
                    StackTrace = ex.ToExceptionStackTrace()
                });
                db.SaveChanges();
            }
        }

        public static void SistemBilgisiKaydet(Exception ex, string message, byte bilgiTipi)
        {
            using (var db = new LisansustuBasvuruSistemiEntities())
            {


                db.SistemBilgilendirmes.Add(new SistemBilgilendirme
                {
                    BilgiTipi = bilgiTipi,
                    Message = message,
                    IslemYapanID = UserIdentity.Current != null && UserIdentity.Current.IsAuthenticated && UserIdentity.Current.Id > 0 ? UserIdentity.Current.Id : (int?)null,
                    IslemYapanIP = UserIdentity.Ip,
                    IslemTarihi = DateTime.Now,
                    StackTrace = ex.ToExceptionStackTrace()
                });
                db.SaveChanges();
            }
        }

        public static void SistemBilgisiKaydet(string mesaj, string stakTrace, byte bilgiTipi)
        {
            using (var db = new LisansustuBasvuruSistemiEntities())
            {


                db.SistemBilgilendirmes.Add(new SistemBilgilendirme
                {
                    Message = mesaj,
                    BilgiTipi = bilgiTipi,
                    IslemYapanID = UserIdentity.Current != null && UserIdentity.Current.IsAuthenticated && UserIdentity.Current.Id > 0 ? UserIdentity.Current.Id : (int?)null,
                    IslemYapanIP = UserIdentity.Ip,
                    IslemTarihi = DateTime.Now,
                    StackTrace = stakTrace
                });
                db.SaveChanges();
            }
        }

        public static void SistemBilgisiKaydet(string mesaj, string stakTrace, byte bilgiTipi, int? kullaniciId, string kullaniciIp)
        {
            using (var db = new LisansustuBasvuruSistemiEntities())
            {
                db.SistemBilgilendirmes.Add(new SistemBilgilendirme
                {
                    Message = mesaj,
                    BilgiTipi = bilgiTipi,
                    IslemYapanID = kullaniciId,
                    IslemYapanIP = !kullaniciIp.IsNullOrWhiteSpace() ? kullaniciIp : UserIdentity.Ip,
                    IslemTarihi = DateTime.Now,
                    StackTrace = stakTrace
                });
                db.SaveChanges();
            }
        }
    }
}