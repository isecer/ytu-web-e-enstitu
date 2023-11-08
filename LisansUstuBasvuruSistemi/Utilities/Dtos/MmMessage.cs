using System;
using System.Collections.Generic;
using System.Linq;
using System.Web; 
using LisansUstuBasvuruSistemi.Utilities.Enums;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class MmMessage
    {
        public bool IsDialog { get; set; }
        public string DialogID { get; set; }
        public bool IsCloseDialog { get; set; }
        public bool IsSuccess { get; set; }
        public Msgtype MessageType { get; set; }

        public string Title { get; set; }
        public string ReturnUrl { get; set; }
        public int ReturnUrlTimeOut { get; set; }
        public int SiraNo { get; set; }
        public List<string> Messages { get; set; }
        public List<MrMessage> MessagesDialog { get; set; }
        public object Table { get; set; }
        public MmMessage()
        {
            MessageType = Msgtype.Nothing;
            Messages = new List<string>();
            MessagesDialog = new List<MrMessage>();
            ReturnUrlTimeOut = 400;
        }

    }
    public class MrMessage
    {

        public string DialogID { get; set; }
        public bool IsSucces { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public string PropertyName { get; set; }
        public bool AddIcon { get; set; }
        public string HtmlData { get; set; }
        public List<int> ReturnIds { get; set; }
        public Msgtype MessageType { get; set; }
        public MrMessage()
        {
            AddIcon = true;
            MessageType = Msgtype.Nothing;
        }
    }
    public class MrMesajBilgi
    {

        public int MesajlarID { get; set; }
        public int KullaniciID { get; set; }
        public DateTime Tarih { get; set; }
        public string Konu { get; set; }
        public string Aciklama { get; set; }

    }
}