using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Hive.Configuration;

/// <summary>
/// Represents configuration for configuring Hive specific attributes
/// </summary>
public class WebOptions
{
    /// <summary>
    /// The config header to look under for this configuration instance.
    /// </summary>
    public const string ConfigHeader = "Web";

    /// <summary>
    /// Enable or disable CORS.
    /// </summary>
    public bool CORS { get; set; }

    /// <summary>
    /// The URLs to allow when processing CORS.
    /// </summary>
    [Required]
    [SuppressMessage("Design", "CA1002:Do not expose generic lists", Justification = "Needed for proper deserialization")]
    public List<string> AllowedOrigins { get; private set; } = new();

    /// <summary>
    /// The methods to allow when processing CORS. If this is not filled, it will use any method.
    /// </summary>
    [SuppressMessage("Design", "CA1002:Do not expose generic lists", Justification = "Needed for proper deserialization")]
    public List<string>? AllowedMethods { get; private set; }

    /// <summary>
    /// The headers to allow when processing CORS. If this is not filled, it will use any header.
    /// </summary>
    [SuppressMessage("Design", "CA1002:Do not expose generic lists", Justification = "Needed for proper deserialization")]
    public List<string>? AllowedHeaders { get; private set; }

    /// <summary>
    /// The CORS policy name. Defaults to "_hiveOrigins"
    /// </summary>
    public string PolicyName { get; set; } = "_hiveOrigins";

    /// <summary>
    /// Enables https redirection. Defaults to true.
    /// </summary>
    public bool? HTTPSRedirection { get; private set; }
}
