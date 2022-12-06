using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MailManager.Models
{
    public class EmailSendModel
    {
        public string EMail { get; set; }
        public bool ToOrBcc { get; set; }

    }
}
