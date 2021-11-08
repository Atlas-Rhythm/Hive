using System;
using System.ComponentModel.DataAnnotations;

namespace Hive.Configuration
{
    /// <summary>
    /// Represents configuration options for Uploads.
    /// </summary>
    public class UploadOptions
    {
        /// <summary>
        /// The maximum file size allowed for uploads, in bytes.
        /// </summary>
        [Range(1, long.MaxValue, ErrorMessage = "Max file size must be positive and non-zero!")]
        public long MaxFileSize { get; private set; } = 32 * 1024 * 1024;
    }
}
