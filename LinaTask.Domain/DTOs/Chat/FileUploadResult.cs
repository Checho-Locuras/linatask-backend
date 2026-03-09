using System;
using System.Collections.Generic;
using System.Text;

namespace LinaTask.Domain.DTOs.Chat
{
    public class FileUploadResult
    {
        public string Url { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string ContentType { get; set; } = string.Empty;
    }
}
