using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LisansUstuBasvuruSistemi.Utilities.Dtos
{
    public class ResendMailsRequest
    {
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public List<string> Institutes { get; set; }
        public string AdditionalNote { get; set; }
        public int BatchSize { get; set; }
        public int BatchDelay { get; set; }
        public int SubjectPrefix { get; set; }
    }

    public class ResendMailsPreviewResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public List<string> InstituteNames { get; set; }
        public int TotalMailCount { get; set; }
        public int TotalRecipientCount { get; set; }
        public int BatchCount { get; set; }
        public int BatchSize { get; set; }
    }

    public class ResendProcessResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string ProcessId { get; set; }
    }

    public class ResendProgressResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int PercentComplete { get; set; }
        public string StatusMessage { get; set; }
        public bool IsComplete { get; set; }
        public bool HasErrors { get; set; }
        public int TotalMailCount { get; set; }
        public int ProcessedMailCount { get; set; }
        public int SuccessCount { get; set; }
        public int ErrorCount { get; set; }
    }
}