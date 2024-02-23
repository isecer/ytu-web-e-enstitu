using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Entities.Entities;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class MezuniyetBasvurulariYayinDto : MezuniyetBasvurulariYayin
    {
        public int? ShowDetayYayinID { get; set; }
        public bool DegerlendirmeAktif { get; set; }
        public bool DegerlendirmeKolonu { get; set; }
        public bool sonucGirisSureciAktif { get; set; }
        public int? RowNum { get; set; }
        public bool IsDataShow { get; set; }
        public string guID { get; set; }
        public int MezuniyetSurecID { get; set; }
        public string MezuniyetYayinTurAdi { get; set; }
        public bool MezuniyetYayinTarihZorunlu { get; set; }
        public string MezuniyetYayinBelgeTurAdi { get; set; }
        public bool MezuniyetYayinBelgeTurZorunlu { get; set; }
        public string MezuniyetYayinKaynakLinkTurAdi { get; set; }
        public bool MezuniyetYayinKaynakLinkIsUrl { get; set; }
        public bool MezuniyetYayinKaynakLinkTurZorunlu { get; set; }
        public string MezuniyetYayinMetinTurAdi { get; set; }
        public bool MezuniyetYayinMetinZorunlu { get; set; }
        public string MezuniyetYayinLinkTurAdi { get; set; }
        public bool MezuniyetYayinLinkiZorunlu { get; set; }
        public bool MezuniyetYayinLinkIsUrl { get; set; }
        public bool MezuniyetYayinIndexTurZorunlu { get; set; }
        public string MezuniyetYayinIndexTurAdi { get; set; }
        public bool MezuniyetKabulEdilmisMakaleZorunlu { get; set; }

        public bool YayinYazarlarIstensin { get; set; }
        public bool YayinDergiAdiIstensin { get; set; }
        public bool YayinYilCiltSayiIstensin { get; set; }
        public bool YayinProjeTurIstensin { get; set; }
        public bool YayinProjeEkibiIstensin { get; set; }
        public bool YayinMevcutDurumIstensin { get; set; }
        public bool YayinDeatKurulusIstensin { get; set; }
        public bool IsTarihAraligiIstensin { get; set; }
        public bool YayinEtkinlikAdiIstensin { get; set; }
        public bool YayinYerBilgisiIstensin { get; set; }

        public string ProjeTurAdi { get; set; }

        public List<MezuniyetYayinIndexTurleri> YayinIndexTurleri { get; set; }

        public List<MezuniyetYayinProjeTurleri> MezuniyetYayinProjeTurleris { get; set; }


        public MezuniyetBasvurulariYayinDto()
        {
            guID = Guid.NewGuid().ToString().Substring(0, 8);
            YayinIndexTurleri = new List<MezuniyetYayinIndexTurleri>();
            MezuniyetYayinProjeTurleris = new List<MezuniyetYayinProjeTurleri>();
        }

    }
}