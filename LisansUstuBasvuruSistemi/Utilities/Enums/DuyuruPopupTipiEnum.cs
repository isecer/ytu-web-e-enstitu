using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LisansUstuBasvuruSistemi.Utilities.Enums
{
    public class DuyuruPopupTipiEnum
    {
        //--1	Anasayfa
        //--2	Belge talebi
        //--3	Talep yapma
        //--4	Lisansustu başvuru
        //--5	Tez danışman/dil/konu önerisi
        //--6	Yeterlik başvurusu
        //--7	Tez jüri önerisi
        //--8	Tez savunması
        //--9	Tez ara rapor girişi
        //--10	Mezuniyet başvurusu
        //--11	Dönem Projesi başvurusu
        public const int AnaSayfa = 1;
        public const int BelgeTalebi = 2;
        public const int TalepYap = 3;
        public const int LisansustuBasvuru = 4;
        public const int TdoBasvuru = 5;
        public const int YeterlikBasvuru = 6;
        public const int TijOneri = 7;
        public const int TezOneriSavunma = 8;
        public const int TiBasvuru = 9;
        public const int MezuniyetBasvuru = 10;
        public const int DonemProjesiBasvuru = 11;

    }
}