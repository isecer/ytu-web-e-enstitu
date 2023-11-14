using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LisansUstuBasvuruSistemi.Utilities.Enums
{
    public class HttpDurumKodEnum
    {
        public const int Continue = 100;
        public const int SwitchingProtocols = 101;
        public const int Processing = 102;
        public const int OK = 200;
        public const int Created = 201;
        public const int Accepted = 202;
        public const int NonAuthoritativeInformation = 203;
        public const int NoContent = 204;
        public const int ResetContent = 205;
        public const int PartialContent = 206;
        public const int MultiStatus = 207;
        public const int ContentDifferent = 210;
        public const int MultipleChoices = 300;
        public const int MovedPermanently = 301;
        public const int MovedTemporarily = 302;
        public const int SeeOther = 303;
        public const int NotModified = 304;
        public const int UseProxy = 305;
        public const int TemporaryRedirect = 307;
        public const int BadRequest = 400;
        public const int Unauthorized = 401;
        public const int PaymentRequired = 402;
        public const int Forbidden = 403;
        public const int NotFound = 404;
        public const int NotAccessMethod = 405;
        public const int NotAcceptable = 406;
        public const int UnLoginToProxyServer = 407;
        public const int RequestTimeOut = 408;
        public const int Conflict = 409;
        public const int Gone = 410;
        public const int LengthRequired = 411;
        public const int PreconditionAiled = 412;
        public const int RequestEntityTooLarge = 413;
        public const int RequestURITooLong = 414;
        public const int UnsupportedMediaType = 415;
        public const int RequestedrangeUnsatifiable = 416;
        public const int Expectationfailed = 417;
        public const int Unprocessableentity = 422;
        public const int Locked = 423;
        public const int Methodfailure = 424;
        public const int InternalServerError = 500;
        public const int Uygulanmamış = 501;
        public const int GeçersizAğGeçidi = 502;
        public const int HizmetYok = 503;
        public const int GatewayTimeout = 504;
        public const int HTTPVersionNotSupported = 505;
        public const int InsufficientStorage = 507;

    }
}