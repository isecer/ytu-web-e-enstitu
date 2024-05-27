using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Entities.Entities;
using LisansUstuBasvuruSistemi.Business;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.SystemSetting;
using LisansUstuBasvuruSistemi.WebServiceData.ObsService;

namespace LisansUstuBasvuruSistemi.Utilities.ApplicationTasks
{
    public class ObsStudentControlTaskRunner
    {
        private static Timer _timer;

        public static void Start()
        {
            SistemBilgilendirmeBus.SistemBilgisiKaydet("ObsStudentControlTaskRunner.Start()", ObjectExtensions.GetCurrentMethodPath(), BilgiTipiEnum.Bilgi);
            _timer = new Timer(ExecuteTask, null, TimeSpan.Zero, TimeSpan.FromHours(1));
        }

        private static void ExecuteTask(object state)
        {
            var runTasks = SistemAyar.OtomatikObsOgrenciKontrolServisiniCalistir.GetAyar().ToBooleanObj() ?? false;
            if (!runTasks) return;
            StartOtoMailsAsync();
        }

        private static async void StartOtoMailsAsync()
        {
            if (DateTime.Now.Hour == 2)
                await OgrenciObsKontrol();

        }

        public static async Task OgrenciObsKontrol()
        {
            try
            {
                SistemBilgilendirmeBus.SistemBilgisiKaydet("ObsStudentControlTaskRunner.OgrenciObsKontrol()", ObjectExtensions.GetCurrentMethodPath(), BilgiTipiEnum.Bilgi);
                using (var entities = new LubsDbEntities())
                {
                    var ogrenciler = entities.Kullanicilars.Where(p => p.YtuOgrencisi && p.DanismanID.HasValue)
                        .ToList();


                    var ogrenciDct = ogrenciler.Select(s => new { s.KullaniciID, s.OgrenciNo }).ToList();
                    var ogrenciNos = ogrenciDct.Select(s => s.OgrenciNo).ToList();
                    var ogrenciObsList = new ObsServiceData().ObsSutentList(ogrenciNos);

                    var aktifOlmayanlar = ogrenciDct.Where(p => ogrenciObsList.All(a => a != p.OgrenciNo))
                        .Select(s => s.KullaniciID).ToList();

                    if (aktifOlmayanlar.Any())
                    {
                       var kaydiSilinenler= ogrenciNos.Where(p => ogrenciObsList.All(a => a != p)).ToList();
                        foreach (var aktifOlmayanOgrenciId in aktifOlmayanlar)
                        {
                            var ogrenci = ogrenciler.First(f => f.KullaniciID == aktifOlmayanOgrenciId);
                            ogrenci.YtuOgrencisi = false;
                        }

                        await entities.SaveChangesAsync();
                        SistemBilgilendirmeBus.SistemBilgisiKaydet("ObsStudentControlTaskRunner => " + aktifOlmayanlar.Count + " öğrenci obs sisteminde pasif gözükmekte. \r\n " + string.Join(",", kaydiSilinenler) + " bu kullanıcılar ytü öğrencisi statüsünden çıkarıldı.", ObjectExtensions.GetCurrentMethodPath(), BilgiTipiEnum.Bilgi);

                    }
                    else SistemBilgilendirmeBus.SistemBilgisiKaydet("ObsStudentControlTaskRunner.OgrenciObsKontrol() servisi kontrol işlemi tamamlandı. Obs de aktif olarak okumayan herhangi bir öğrenci kaydı bulunamadı.", ObjectExtensions.GetCurrentMethodPath(), BilgiTipiEnum.Bilgi);


                }
            }
            catch (Exception e)
            {
                SistemBilgilendirmeBus.SistemBilgisiKaydet("ObsStudentControlTaskRunner.OgrenciObsKontrol() servisi çalıştırılırken bir hata oluştu! hata:" + e.ToExceptionMessage(), ObjectExtensions.GetCurrentMethodPath(), BilgiTipiEnum.Bilgi);
            }
        }

        public static void Stop()
        {
            // Timer'ı durdur
            _timer?.Change(Timeout.Infinite, Timeout.Infinite);
        }

        public static void Restart()
        {
            // Timer'ı durdur ve tekrar başlat
            _timer?.Change(TimeSpan.Zero, TimeSpan.FromHours(1));
        }

        public static void Dispose()
        {
            // Timer'ı temizle
            _timer?.Dispose();
        }
    }

}