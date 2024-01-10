using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using LisansUstuBasvuruSistemi.Models; using LisansUstuBasvuruSistemi.Utilities.Dtos;

namespace LisansUstuBasvuruSistemi.Utilities.Helpers
{
    public static class StaticDefinitions
    {
        public static string Tuz = "@BİSKAmcumu";
        public static  int UniversiteYtuKod { get; } = 67;
        public static  int PageSize = 15;
       
        public static List<int> GetOgrenimTurKods()
        {
            var oTurList = new List<int>
            {
                1, // - NORMAL ÖĞRETİM
                //oTurList.Add(2);// - İKİNCİ ÖĞRETİM
                3, // - UZAKTAN ÖĞRETİM
                4 // - AÇIK ÖĞRETİM
            };

            return oTurList;
        }

        public static List<int> GetUniversiteTurKods()
        {
            var uTurList = new List<int>
            {
                1, // - DEVLET ÜNİVERSİTELERİ
                //2,// - VAKIF ÜNİVERSİTELERİ
                //3,// - 4702 SAYILI KANUN İLE VAKFA BAĞLI KURULAN MYO'LAR 
                4, // - ASKERİ EĞİTİM VEREN OKULLAR
                5, // - POLİS AKADEMİSİ
                //6,// - KKTC'DE EĞİTİM VEREN ÜNİVERSİTELER 
                //7,// - TÜRKİ CUMHURİYETLERİNDE BULUNAN ÜNİVERSİTELER
                8, // - TODAİE
                9 // - DİĞER(SAĞLIK BAKANLIĞI, ADALET BAKANLIĞI, VAKIF GUREBA VB.)
            };

            return uTurList;
        }
        public static List<int> GetBirimTurKods()
        {
            var bTurList = new List<int>
            {
                0, //YÖK
                1, //-Üniversite
                2, //-Fakülte
                4, //-Enstitü
                5, //-Yüksekokul
                6, //-Meslek Yüksekokulu
                7, //-Eğitim Araştırma Hastanesi
                8, //-Uygulama ve Araştırma Merkezi
                9, //-Rektörlük
                10, //-Bölüm
                11, //-Anabilim Dalı
                12, //-Bilim Dalı
                13, //-Önlisans/Lisans Programı
                14, //-Sanat Dalı
                15, //-Anasanat Dalı
                16, //-Yüksek Lisans Programı
                17, //-Doktora Programı
                18, //-Sanatta Yeterlilik Programı
                19, //-Tıpta Uzmanlık Programı
                20, //-Önlisans Programı
                21, //-Disiplinlerarası Anabilim Dalı
                22, //-Disiplinlerarası Yüksek Lisans Programı
                23, //-Bütünleşik Doktora Programı
                24 //-Disiplinlerarası Doktora Programı
            };


            return bTurList;
        } 
    }
}