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
        public PagerOption()
        {
            PageIndex = 1;
            PageSize = 20;
        }
        public string SelectedIds { get; set; }

        public string Sort { get; set; }

        public string Sender { get; set; }

        private int _pageIndex = 1;
        public List<int> PageSizes => new List<int> { 10, 15, 20, 30, 50, 75, 100 };
        public int PageIndex
        {
            get
            {
                if ((decimal)RowCount / (decimal)_pageIndex == 0 || PageCount < _pageIndex) _pageIndex = 1;
                return _pageIndex;

            }
            set => _pageIndex = value;
        }


        public int PageCount
        {
            get
            {
                if (PageSize == 0) return 0;
                var pg = (RowCount / PageSize);
                if (RowCount % PageSize != 0) pg++;
                return pg;
            }
        }
        public int PageSize { get; set; }

        public int RowCount { get; set; }
        public int StartRowIndex => PageIndex > 0 && RowCount > 0 ? (PageSize * (PageIndex - 1)) : 0;
        public int EndRowIndex => (PageSize < RowCount) ? (((StartRowIndex + PageSize) > RowCount) ? RowCount : PageSize * PageIndex) : RowCount;
        public bool CanPrev => RowCount > 0 && PageIndex > 1;
        public bool CanNext => RowCount > 0 && PageIndex < PageCount;
        public bool CanFirst => RowCount > 0 && PageIndex > 1;
        public bool CanLast => RowCount > 0 && PageIndex != PageCount;
        public MvcHtmlString ToPagerString()
        {
            var str1 = "<input type='hidden' id='Sort'      name='Sort' value='" + Sort + "'/><input type='hidden' id='Sender'    name='Sender' value='" + Sender + "'/><input type='hidden' id='PageIndex' name='PageIndex' value='" + PageIndex + "'/><input type='hidden' id='PageSize'  name='PageSize' value='" + PageSize + "'/><input type='hidden' id='RowCount'  name='RowCount' value='" + RowCount + "'/><input type='hidden' id='SelectedIds'  name='SelectedIds' value='" + SelectedIds + "'/>";
            var str2 = "";
            foreach (int pageSiz in PageSizes)
                str2 = str2 + "<option " + (pageSiz == PageSize ? "selected=selected" : "") + " value=" + pageSiz + ">" + pageSiz + "</option>";

            return new MvcHtmlString("<div style='width:270px;float:left;'>Listelenen: (" + (RowCount > 0 ? (StartRowIndex + 1) : 0) + "-" + EndRowIndex + ")/" + RowCount + "</div>" + "<div class='dataTables_paginate paging_simple_numbers pgrBiska'>" + str1 + "<a class='btn btn-default btn-xs  pgrIlk  " + (PageIndex < 2 || PageCount < 2 ? "disabled" : "") + "' href='javascript:void(0)'><i class='fa fa-fast-backward'></i></a><span><a class='btn btn-default btn-xs  pgrGeri " + (PageIndex < 2 || PageCount < 2 ? "disabled" : "") + "'><i class='fa fa-step-backward'></i></a><a class='btn btn-default btn-xs ' href='javascript:void(0)' style='padding: 0px;'><input type='text' class='pgrPageIndex' value=" + PageIndex + " style='width: 30px;height:21px;'></a><a class='btn btn-default btn-xs  disabled' href='javascript:void(0)' style='padding-right: 1px; padding-left: 1px;'>/</a><a class='btn btn-default btn-xs  pgrToplamSayfa disabled' href='javascript:void(0)'>" + PageCount + "</a><a class='btn btn-default btn-xs  pgrGit " + (PageCount < 2 ? "disabled" : "") + "'><i class='fa fa-play'></i></a><a class='btn btn-default btn-xs  pgrIleri " + (PageIndex >= PageCount || PageCount < 2 ? "disabled" : "") + "'><i class='fa fa-step-forward'></i></a></span><a class='btn btn-default btn-xs  pgrSon " + (PageIndex >= PageCount || PageCount < 2 ? "disabled" : "") + " href='javascript:void(0)'><i class='fa fa-fast-forward'></i></a><a class='btn btn-default btn-xs ' style='padding: 0px;'><select class='pgrSatirSayisi' style='padding: 1px; width: 50px;height:21px;'>" + str2 + "</select></a></div>");
        }
    }

}