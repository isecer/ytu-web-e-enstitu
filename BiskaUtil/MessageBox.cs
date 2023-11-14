using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BiskaUtil
{
    public static class MessageBox
    {
        public class  Msg
        {
           public string[] Message { get; set; }
           public string Title { get; set; }
           public MessageType MsgType { get; set; }            
        }
        public enum MessageType { Information,Error,Warning,Custom,Success}
      
        #region Queue
        public static List<Msg> Queues {
            get
            {
                if (!UserIdentity.Current.Informations.ContainsKey("Queues"))
                {
                    List<Msg> msgsx = new List<Msg>();
                    UserIdentity.Current.Informations["Queues"] = msgsx;
                }
                var msgs = (List<Msg>)UserIdentity.Current.Informations["Queues"];
                return msgs;
            }
        }

        public static void Show(string message)
        {
            Show(message, "", MessageType.Information);
        }
        public static void Show(string message, string title) 
        {
            Show(message, title, MessageType.Information);
        }
        public static void Show(string message,MessageType msgType)
        {
            Show(message, "", msgType);
        }
        public static void Show(string message, string title, MessageType msgType)
        {
            Queues.Add(new Msg { Message =new string[]{message}, Title = title, MsgType = msgType });
        }
        public static void Show(string title, MessageType msgType,params string[] messages)
        {
            Queues.Add(new Msg { Message = messages, Title = title, MsgType = msgType });
        }
        public static Msg GetShow()
        {
            var fi = Queues.FirstOrDefault();
            if (fi != null) Queues.RemoveAt(0);
            return fi;
        }
        public static Msg[] GetShows()
        {
            var lst = Queues.ToArray();
            Queues.Clear();
            return lst;
        }
        #endregion

    }
}
