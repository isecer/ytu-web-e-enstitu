using System.Collections.Generic;
using System.Linq;
using Entities.Entities;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.MailManager;

namespace LisansUstuBasvuruSistemi.Raporlar.Genel
{
    public partial class RprYaziSablonOlusturucu : DevExpress.XtraReports.UI.XtraReport
    {

        public RprYaziSablonOlusturucu(Enstituler enstitu, string html, string displayName)
        {
            InitializeComponent();
            this.DisplayName = displayName;
            using (var entities = new LubsDbEntities())
            {
                var isEnstituMururOrVekil = !enstitu.EnstituMudurVekilId.HasValue;
                var mudurId = isEnstituMururOrVekil ? enstitu.EnstituMudurId : enstitu.EnstituMudurVekilId;
                var mudurAdi = "";
                var mudurUnvanAdi = "";
                if (mudurId.HasValue)
                {
                    var mudur = entities.Kullanicilars.First(f => f.KullaniciID == mudurId);
                    mudurUnvanAdi = mudur.Unvanlar.UnvanAdi.IlkHarfiBuyut();
                    mudurAdi = mudur.Ad.IlkHarfiBuyut() + " " + mudur.Soyad.ToUpper();
                }

                var parameters = new List<MailParameterDto>
                {
                    new MailParameterDto { Key = "EnstituMudurUnvan", Value = mudurUnvanAdi },
                    new MailParameterDto { Key = "EnstituMudurAdSoyad", Value = mudurAdi },
                    new MailParameterDto { Key = "EnstituMudurTitle", Value = isEnstituMururOrVekil ? "Enstitü Müdürü" : "Enstitü Müdür V." },
                };
                html = ValueReplaceExtension.ProcessHtmlContent(html, parameters);
            }

            lblEnstituBaslik.Text = enstitu.EnstituAd.ToUpper();
            richContent.Html = html;
            richContent.CanGrow = true;
            richContent.CanShrink = true;
            Detail.CanGrow = true;
            Detail.CanShrink = true;
            cellAdres.Text = enstitu.Adres;
            cellTel.Text = enstitu.Tel;
            cellEnstituAdi.Text = enstitu.EnstituAd;
            cellWebAdresi.Text = enstitu.WebAdresi;
            cellEposta.Text = enstitu.EPosta;

        }



    }
}
