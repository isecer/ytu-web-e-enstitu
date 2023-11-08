using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LisansUstuBasvuruSistemi.Utilities.Enums
{
    public static class DuyuruPopupTipi
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
        public static int AnaSayfa = 1;
        public static int BelgeTalebi = 2;
        public static int TalepYap = 3;
        public static int LisansustuBasvuru = 4;
        public static int TdoBasvuru = 5;
        public static int YeterlikBasvuru = 6;
        public static int TijOneri = 7;
        public static int TezOneriSavunma = 8;
        public static int TiBasvuru = 9;
        public static int MezuniyetBasvuru = 10;

    }
}