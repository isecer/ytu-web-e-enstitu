using Entities.Entities;
using System;
using System.Collections.Generic;
namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public sealed class SrTalepleriKayitDto : SRTalepleri
    {
        public Guid BasvuruUniqueId { get; set; }
        public new bool? IsOnline { get; set; }
        public bool YetkisizErisim { get; set; }
        public string AdSoyad { get; set; }
        public string OgrenciNo { get; set; }
        public List<string> JuriAdi { get; set; }
        public List<string> Telefon { get; set; }
        public List<string> Email { get; set; }
        private TimeSpan _bitSaat;
        public new TimeSpan BitSaat
        {
            get
            {
                // ReSharper disable once ArrangeAccessorOwnerBody
                return _bitSaat.Days > 0 ? _bitSaat.Subtract(TimeSpan.FromDays(_bitSaat.Days)) : _bitSaat;
            }
            // ReSharper disable once ArrangeAccessorOwnerBody
            set { _bitSaat = value; }
        }
        public new TimeSpan BasSaat { get; set; }
        public string MzRowId { get; set; }
        public bool IsSalonSecilsin { get; set; }

        public SrTalepleriKayitDto()
        {
            JuriAdi = new List<string>();
            Telefon = new List<string>();
            Email = new List<string>();
            SRTaleplerJuris = new List<SRTaleplerJuri>();
        }
    }
}