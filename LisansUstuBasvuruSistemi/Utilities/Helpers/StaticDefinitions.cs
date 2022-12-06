using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using LisansUstuBasvuruSistemi.Models; using LisansUstuBasvuruSistemi.Models.FilterModel;

namespace LisansUstuBasvuruSistemi.Utilities.Helpers
{
    public static class StaticDefinitions
    {
        public static string Tuz = "@BİSKAmcumu";
        public static int UniversiteYtuKod { get; } = 67;
        public static int PageSize = 15;
        public static List<string> FExtensions()
        {
            return new List<string>() { ".jpg", ".jpeg", ".tif", ".bmp", ".png", ".txt", ".doc", ".docx", ".xls", ".xlsx", ".pdf", ".rtf", ".pptx" };
        }
        public static SistemDilleri[] SistemDilleris { get; set; }
        public static Enstituler[] Enstitulers { get; set; }
        public static List<int> GetOgrenimTurKods()
        {
            var oTurList = new List<int>();
            oTurList.Add(1);// - NORMAL ÖĞRETİM
                            //oTurList.Add(2);// - İKİNCİ ÖĞRETİM
            oTurList.Add(3);// - UZAKTAN ÖĞRETİM
            oTurList.Add(4);// - AÇIK ÖĞRETİM

            return oTurList;
        }

        public static List<int> GetUniversiteTurKods()
        {
            var uTurList = new List<int>();

            uTurList.Add(1);// - DEVLET ÜNİVERSİTELERİ
                            //uTurList.Add(2);// - VAKIF ÜNİVERSİTELERİ
                            //uTurList.Add(3);// - 4702 SAYILI KANUN İLE VAKFA BAĞLI KURULAN MYO'LAR 
            uTurList.Add(4);// - ASKERİ EĞİTİM VEREN OKULLAR
            uTurList.Add(5);// - POLİS AKADEMİSİ
                            //uTurList.Add(6);// - KKTC'DE EĞİTİM VEREN ÜNİVERSİTELER 
                            //uTurList.Add(7);// - TÜRKİ CUMHURİYETLERİNDE BULUNAN ÜNİVERSİTELER
            uTurList.Add(8);// - TODAİE
            uTurList.Add(9);// - DİĞER(SAĞLIK BAKANLIĞI, ADALET BAKANLIĞI, VAKIF GUREBA VB.)

            return uTurList;
        }
        public static List<int> GetBirimTurKods()
        {
            var bTurList = new List<int>();
            bTurList.Add(0);//YÖK
            bTurList.Add(1);//-Üniversite
            bTurList.Add(2);//-Fakülte
            bTurList.Add(4);//-Enstitü
            bTurList.Add(5);//-Yüksekokul
            bTurList.Add(6);//-Meslek Yüksekokulu
            bTurList.Add(7);//-Eğitim Araştırma Hastanesi
            bTurList.Add(8);//-Uygulama ve Araştırma Merkezi
            bTurList.Add(9);//-Rektörlük
            bTurList.Add(10);//-Bölüm
            bTurList.Add(11);//-Anabilim Dalı
            bTurList.Add(12);//-Bilim Dalı
            bTurList.Add(13);//-Önlisans/Lisans Programı
            bTurList.Add(14);//-Sanat Dalı
            bTurList.Add(15);//-Anasanat Dalı
            bTurList.Add(16);//-Yüksek Lisans Programı
            bTurList.Add(17);//-Doktora Programı
            bTurList.Add(18);//-Sanatta Yeterlilik Programı
            bTurList.Add(19);//-Tıpta Uzmanlık Programı
            bTurList.Add(20);//-Önlisans Programı
            bTurList.Add(21);//-Disiplinlerarası Anabilim Dalı
            bTurList.Add(22);//-Disiplinlerarası Yüksek Lisans Programı
            bTurList.Add(23);//-Bütünleşik Doktora Programı
            bTurList.Add(24);//-Disiplinlerarası Doktora Programı


            return bTurList;
        }
        public static Roller[] Roles { get; set; }
        public static Menuler[] Menulers { get; set; }
    }
}