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
        /// The config header to look under for this configuration instance.
        /// </summary>
        public const string ConfigHeader = "Uploads";

        [Required]
        [Range(1, long.MaxValue, ErrorMessage = "Max file size must be positive and non-zero!")]
        public long MaxFileSize { get; private set; } = 32 * 1024 * 1024;
    }
}
