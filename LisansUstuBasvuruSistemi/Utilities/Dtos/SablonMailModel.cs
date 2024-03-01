using System;
using System.Collections.Generic;
using System.Linq;
using Entities.Entities;
using LisansUstuBasvuruSistemi.Utilities.Extensions;
using LisansUstuBasvuruSistemi.Utilities.MailManager;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class SablonMailModel
    {
        public int MailSablonTipId { get; set; }
        public string EnstituAdi { get; set; }
        public string SistemErisimAdresi { get; set; }
        public string WebAdresi { get; set; }
        public MailSablonlari Sablon { get; set; }
        public List<string> SablonParametreleri { get; set; } = new List<string>();
        public List<MailSablonlariEkleri> SablonEkleri { get; set; } = new List<MailSablonlariEkleri>();
        public List<MailSendList> EMails { get; set; } = new List<MailSendList>();
        public List<System.Net.Mail.Attachment> Attachments { get; set; } = new List<System.Net.Mail.Attachment>();
        public List<MailParameterDto> MailParameterDtos { get; set; } = new List<MailParameterDto>();
        public Guid? UniqueId { get; set; }
        public string AdSoyad { get; set; }
        public string UniversiteAdi { get; set; }
        public string UnvanAdi { get; set; }
        public string ProgramAdi { get; set; }
        public bool IsAsilOrYedek { get; set; }
        public string JuriTipAdi { get; set; }
        public int? MezuniyetJuriOneriFormuJuriId { get; set; }
        public int? TiBasvuruAraRaporKomiteId { get; set; }


        public List<GonderilenMailEkleri> GetGonderilenMailEkleris
        {
            get
            {
                var attahcMentEkler = Attachments.Select(s => new GonderilenMailEkleri { EkAdi = s.Name }).ToList();
                var sablonEkiDosyalari = SablonEkleri.Select(s => new GonderilenMailEkleri { EkAdi = s.EkAdi, EkDosyaYolu = s.EkDosyaYolu }).ToList();
                attahcMentEkler.AddRange(sablonEkiDosyalari);
                return attahcMentEkler;
            }
        }
        public List<GonderilenMailKullanicilar> GetGonderilenMailKullanicilaris
        {
            get
            {
                return EMails.Select(s => new GonderilenMailKullanicilar { Email = s.EMail, KullaniciID = s.KullaniciId }).ToList();
            }
        }


        public SablonMailModel SetMailModel(SablonMailModel model)
        {
            if (Sablon != null)
            {
                model.SablonEkleri.AddRange(Sablon.MailSablonlariEkleris);
                model.SablonParametreleri = Sablon.MailSablonTipleri.Parametreler.CustomSplit();
                model.EMails.AddRange(Sablon.GonderilecekEkEpostalar.ToSplitEmailSendList());
                model.Attachments.AddRange(SablonEkleri.Select(s => new FileAttachmentInfo { FileName = s.EkAdi, FilePath = s.EkDosyaYolu }).ToList().GetFileToAttachments());
            }

            return model;

        }

    }


}