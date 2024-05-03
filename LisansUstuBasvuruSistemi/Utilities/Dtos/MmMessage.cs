using System.Collections.Generic;
using LisansUstuBasvuruSistemi.Utilities.Enums;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class MmMessage
    {
        public bool IsDialog { get; set; }
        public string DialogID { get; set; }
        public bool IsCloseDialog { get; set; }
        public bool IsSuccess { get; set; }
        public MsgTypeEnum MessageType { get; set; } = MsgTypeEnum.Nothing;

        public string Title { get; set; }
        public string ReturnUrl { get; set; }
        public int ReturnUrlTimeOut { get; set; } = 400;
        public int SiraNo { get; set; }
        public List<string> Messages { get; set; } = new List<string>();
        public List<MrMessage> MessagesDialog { get; set; } = new List<MrMessage>();
        public object Table { get; set; }
    }
    public class MrMessage
    {
         
        public bool IsSucces { get; set; } 
        public string Message { get; set; }
        public string PropertyName { get; set; } 
        public MsgTypeEnum MessageType { get; set; } = MsgTypeEnum.Nothing;
    }
}