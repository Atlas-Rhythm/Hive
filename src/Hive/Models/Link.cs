using System;
using System.Text.Json.Serialization;

namespace Hive.Models;

/// <summary>
/// Represented a named link, something with a readable identifier and a Uri location
/// </summary>
public readonly struct Link : IEquatable<Link>
{
    /// <summary>
    /// The name of the link.
    /// </summary>
    public string Name { get; } = string.Empty;

    /// <summary>
    /// The location (uRI) of the link.
    /// </summary>
    public Uri Location { get; } = null!;

    /// <summary>
    /// Construct a Link object with a name and Uri
    /// </summary>
    /// <param name="name"></param>
    /// <param name="location"></param>
    [JsonConstructor]
    public Link(string name, Uri location)
    {
        Name = name;
        Location = location;
    }

    /// <inheritdoc/>
    public override string ToString() => $"({Name})[{Location}]";

    /// <inheritdoc/>
    public bool Equals(Link other) => Name == other.Name && Location.Equals(other.Location);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is Link other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(Name, Location);

    /// <summary>
    /// Equals operator
    /// </summary>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <returns></returns>
    public static bool operator ==(Link left, Link right) => left.Equals(right);

    /// <summary>
    /// Not equals operator
    /// </summary>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <returns></returns>
    public static bool operator !=(Link left, Link right) => !left.Equals(right);
}
