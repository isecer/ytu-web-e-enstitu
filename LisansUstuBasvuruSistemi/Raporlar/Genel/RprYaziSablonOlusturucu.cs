using System.Linq;
using Entities.Entities;

namespace LisansUstuBasvuruSistemi.Raporlar.Genel
{
    public partial class RprYaziSablonOlusturucu : DevExpress.XtraReports.UI.XtraReport
    {
        public RprYaziSablonOlusturucu(Enstituler enstitu, string html, string displayName)
        {
            InitializeComponent();
            this.DisplayName = displayName;
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
