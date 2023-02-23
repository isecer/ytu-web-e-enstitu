using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Mvc;
using System.Text;

namespace BiskaUtil
{
    [Serializable]
    public class PagerOption
    {
        private int pageSize = 25;
        public int[] PageSizes = new int[8]
        {
      10,
      15,
      20,
      25,
      30,
      50,
      75,
      100
        };

        public string Sort { get; set; }

        public string Sender { get; set; }

        public int PageIndex { get; set; }

        public string SelectedIds { get; set; }

        public int PageSize
        {
            get => this.pageSize;
            set
            {
                this.pageSize = value;
                if (((IEnumerable<int>)this.PageSizes).Contains<int>(this.pageSize))
                    return;
                List<int> list = ((IEnumerable<int>)this.PageSizes).ToList<int>();
                list.Add(this.pageSize);
                this.PageSizes = list.OrderBy<int, int>((Func<int, int>)(o => o)).ToArray<int>();
            }
        }

        public int RowCount { get; set; }

        public int StartRowIndex => (this.PageIndex - 1) * this.PageSize;

        public int PageCount
        {
            get
            {
                if (this.PageSize == 0)
                    return 0;
                int pageCount = this.RowCount / this.PageSize;
                if (this.RowCount % this.PageSize != 0)
                    ++pageCount;
                return pageCount;
            }
        }

        public int PrevPageIndex => this.PageIndex > 0 ? this.PageIndex - 1 : 1;

        public int NextPageIndex => this.PageIndex < this.PageCount ? this.PageIndex + 1 : this.PageCount;

        public int LastPageIndex => this.PageCount;

        public bool CanPrev => this.RowCount > 0 && this.PageIndex > 1;

        public bool CanNext => this.RowCount > 0 && this.PageIndex < this.PageCount;

        public bool CanFirst => this.RowCount > 0 && this.PageIndex > 1;

        public bool CanLast => this.RowCount > 0 && this.PageIndex != this.PageCount;

        public PagerOption()
        {
            this.PageIndex = 1;
            this.PageSize = 20;
            this.RowCount = 0;
        }

        public PagerOption Clone() => new PagerOption()
        {
            PageIndex = this.PageIndex,
            RowCount = this.RowCount,
            PageSize = this.PageSize
        };

        public Dictionary<string, object> GetValues()
        {
            PropertyInfo[] properties = this.GetType().UnderlyingSystemType.GetProperties();
            Dictionary<string, object> values = new Dictionary<string, object>();
            foreach (PropertyInfo propertyInfo in properties)
            {
                try
                {
                    object obj = propertyInfo.GetValue((object)this, (object[])null);
                    values.Add(propertyInfo.Name, obj);
                }
                catch
                {
                }
            }
            return values;
        }

        public MvcHtmlString ToPagerString(string Culture = "tr_TR")
        {
            string str1 = "<input type='hidden' id='Sort'      name='Sort' value='" + this.Sort + "'/><input type='hidden' id='Sender'    name='Sender' value='" + this.Sender + "'/><input type='hidden' id='PageIndex' name='PageIndex' value='" + (object)this.PageIndex + "'/><input type='hidden' id='PageSize'  name='PageSize' value='" + (object)this.PageSize + "'/><input type='hidden' id='RowCount'  name='RowCount' value='" + (object)this.RowCount + "'/><input type='hidden' id='SelectedIds'  name='SelectedIds' value='" + this.SelectedIds + "'/>";
            string str2 = "";
            foreach (int pageSiz in this.PageSizes)
                str2 = str2 + "<option " + (pageSiz == this.PageSize ? (object)"selected=selected" : (object)"") + " value=" + (object)pageSiz + ">" + (object)pageSiz + "</option>";
            string str3 = "Listelenen:";
            if (Culture == "en_US")
                str3 = "Listed:";
            return new MvcHtmlString("<div style='width:270px;float:left;'>" + str3 + " (" + (object)(this.RowCount > 0 ? this.StartRowIndex + 1 : this.StartRowIndex) + "-" + (object)(this.PageSize < this.RowCount ? (this.StartRowIndex + this.PageSize > this.RowCount ? this.RowCount : this.StartRowIndex + this.PageSize) : this.RowCount) + ")/" + (object)this.RowCount + "</div>" + "<div class='dataTables_paginate paging_simple_numbers pgrBiska'>" + str1 + "<a class='btn btn-default btn-xs  pgrIlk  " + (this.PageIndex < 2 || this.PageCount < 2 ? (object)"disabled" : (object)"") + "' href='javascript:void(0)'><i class='fa fa-fast-backward'></i></a><span><a class='btn btn-default btn-xs  pgrGeri " + (this.PageIndex < 2 || this.PageCount < 2 ? (object)"disabled" : (object)"") + "'><i class='fa fa-step-backward'></i></a><a class='btn btn-default btn-xs ' href='javascript:void(0)' style='padding: 0px;'><input type='text' class='pgrPageIndex' value=" + (object)this.PageIndex + " style='width: 30px;height:21px;'></a><a class='btn btn-default btn-xs  disabled' href='javascript:void(0)' style='padding-right: 1px; padding-left: 1px;'>/</a><a class='btn btn-default btn-xs  pgrToplamSayfa disabled' href='javascript:void(0)'>" + (object)this.PageCount + "</a><a class='btn btn-default btn-xs  pgrGit " + (this.PageCount < 2 ? (object)"disabled" : (object)"") + "'><i class='fa fa-play'></i></a><a class='btn btn-default btn-xs  pgrIleri " + (this.PageIndex >= this.PageCount || this.PageCount < 2 ? (object)"disabled" : (object)"") + "'><i class='fa fa-step-forward'></i></a></span><a class='btn btn-default btn-xs  pgrSon " + (this.PageIndex >= this.PageCount || this.PageCount < 2 ? (object)"disabled" : (object)"") + " href='javascript:void(0)'><i class='fa fa-fast-forward'></i></a><a class='btn btn-default btn-xs ' style='padding: 0px;'><select class='pgrSatirSayisi' style='padding: 1px; width: 50px;height:21px;'>" + str2 + "</select></a></div>");
        }
    }

}