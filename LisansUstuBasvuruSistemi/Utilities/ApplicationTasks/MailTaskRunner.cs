using System;
using System.Threading;
using LisansUstuBasvuruSistemi.Business;
using LisansUstuBasvuruSistemi.Utilities.Enums;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.MailManager;
using LisansUstuBasvuruSistemi.Utilities.SystemSetting;

namespace LisansUstuBasvuruSistemi.Utilities.ApplicationTasks
{
    public class MailTaskRunner
    {
        private static Timer _timer;

        public static void Start()
        {
            SistemBilgilendirmeBus.SistemBilgisiKaydet("MailTaskRunner.Start()", ObjectExtensions.GetCurrentMethodPath(), BilgiTipiEnum.Bilgi);
            _timer = new Timer(ExecuteTask, null, TimeSpan.Zero, TimeSpan.FromHours(1));
        }

        private static void ExecuteTask(object state)
        {
            var runTasks = SistemAyar.OtomatikMailBilgilendirmeServisiniCalistir.GetAyar().ToBooleanObj() ?? false;
            if (!runTasks) return;
            StartMezuniyetOtoMailsAsync();
            StartDpOtoMailsAsync();
        }

        private static async void StartMezuniyetOtoMailsAsync()
        { 
            await MailSenderMezuniyet.SendTaslakBasvuruOgrenciye();
            await MailSenderMezuniyet.SendMailMezuniyetEykTarihineGoreSrAlinmaliOgrenciyeDanismana();
            await MailSenderMezuniyet.SendMailMezuniyetEykTarihineGoreSrAlinmadiEnstituye();
            await MailSenderMezuniyet.SendMailMezuniyetDanismanDegerlendirmeHatirlatmaDanismana();
            await MailSenderMezuniyet.SendMailMezuniyetSinavSonucuGirilmediOgrenciyeDanismana();
            await MailSenderMezuniyet.SendMailMezuniyetTezKontrolTezDosyasiYuklenmeliOgrenciye();
            await MailSenderMezuniyet.SendMailCiltliTezTeslimYapilmaliOgrenciye();
            await MailSenderMezuniyet.SendMailCiltliTezTeslimYapilmadiEnstituye(); 
        }
        private static async void StartDpOtoMailsAsync()
        { 
            await MailSenderDp.SendMailDegerlendirmeHatirlatma();
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