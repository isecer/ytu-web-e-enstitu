using System;
using System.Threading;
using System.Threading.Tasks;
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
            StartMezuniyetOtoMailsAsync();
        }

        private static async void StartMezuniyetOtoMailsAsync()
        {
            var runTasks = SistemAyar.OtomatikMailBilgilendirmeServisiniCalistir.GetAyar().ToBooleanObj() ?? false;
            if (!runTasks) return;

            await MailSenderMezuniyet.SendTaslakBasvuruOgrenciye();
            await MailSenderMezuniyet.SendMailMezuniyetEykTarihineGoreSrAlinmaliOgrenciyeDanismana();
            await MailSenderMezuniyet.SendMailMezuniyetEykTarihineGoreSrAlinmadiEnstituye();
            await MailSenderMezuniyet.SendMailMezuniyetDanismanDegerlendirmeHatirlatmaDanismana();
            await MailSenderMezuniyet.SendMailMezuniyetSinavSonucuGirilmediOgrenciyeDanismana();
            await MailSenderMezuniyet.SendMailMezuniyetTezKontrolTezDosyasiYuklenmeliOgrenciye();
            await MailSenderMezuniyet.SendMailCiltliTezTeslimYapilmaliOgrenciye();
            await MailSenderMezuniyet.SendMailCiltliTezTeslimYapilmadiEnstituye();
        }

        public static void Stop()
        {
            // Timer'ı durdurun
            _timer?.Change(Timeout.Infinite, Timeout.Infinite);
        }

        public static void Restart()
        {
            // Timer'ı durdurun ve tekrar başlatın
            _timer?.Change(TimeSpan.Zero, TimeSpan.FromHours(1));
        }

        public static void Dispose()
        {
            // Timer'ı temizleyin
            _timer?.Dispose();
        }
    }

}