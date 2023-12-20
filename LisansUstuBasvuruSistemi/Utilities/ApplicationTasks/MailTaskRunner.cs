using System;
using System.Threading;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.MailManager;
using LisansUstuBasvuruSistemi.Utilities.SystemSetting;

namespace LisansUstuBasvuruSistemi.Utilities.ApplicationTasks
{
    public class MailTaskRunner
    {
        private Timer _timer;

        public void Start()
        {
            _timer = new Timer(ExecuteTask, null, TimeSpan.Zero, TimeSpan.FromHours(1));
        }

        private static void ExecuteTask(object state)
        {
            StartMezuniyetOtoMails();
        }

        private static async void StartMezuniyetOtoMails()
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

        public void Stop()
        {
            // Timer'ı durdurun
            _timer?.Change(Timeout.Infinite, Timeout.Infinite);
        }

        public void Restart()
        {
            // Timer'ı durdurun ve tekrar başlatın
            _timer?.Change(TimeSpan.Zero, TimeSpan.FromHours(1));
        }

        public void Dispose()
        {
            // Timer'ı temizleyin
            _timer?.Dispose();
        }
    }

}