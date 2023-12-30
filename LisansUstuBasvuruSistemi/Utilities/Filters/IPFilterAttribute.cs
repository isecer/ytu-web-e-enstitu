using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace LisansUstuBasvuruSistemi.Utilities.Filters
{
    public class IpFilterAttribute : ActionFilterAttribute
    {
        private readonly string[] _ipAddressesToBlock;

        public IpFilterAttribute(params string[] ipAddressesToBlock)
        {
            _ipAddressesToBlock = ipAddressesToBlock;
            _ipAddressesToBlock = new List<string> { "194.180.174.109" }.ToArray();
        }

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var request = filterContext.HttpContext.Request;
            var userIpAddress = request.UserHostAddress;

            if (_ipAddressesToBlock.Contains(userIpAddress))
            {
                filterContext.Result = new HttpStatusCodeResult(HttpStatusCode.Forbidden);
            }
        }
    }
}