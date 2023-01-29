using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BiskaUtil;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class ChkListModel
    {
        public string PanelTitle { get; set; }
        public string TableID { get; set; }
        public string InputName { get; set; }
        public IEnumerable<CheckObject<ChkListDataModel>> Data { get; set; }
        public bool AllDataChecked
        {
            get
            {

                return Data.Any() && Data.Select(s => s.Value).Count() == Data.Where(p => p.Checked == true).Select(s => s.Value).Count();

            }
        }
        public ChkListModel(string InputName = "")
        {
            this.InputName = InputName;
            var ID = Guid.NewGuid().ToString().Substr(0, 4);
            TableID = ID;
        }
    }
    public class ChkListDataModel
    {
        public int ID { get; set; }
        public string Code { get; set; }
        public string Caption { get; set; }
        public string Detail { get; set; }
    }
}