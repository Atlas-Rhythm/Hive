using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Hive.Models
{
    public class Channel
    {
        // this could also act as a primary key
        [Key]
        public string Name { get; set; } = null!;

        // like Mod's
        public JsonElement AdditionalData { get; set; }

        // TODO: is there anything else that needs to be on any particular channel object?

        public static void Configure([DisallowNull] ModelBuilder b)
        {
            if (b is null)
                throw new ArgumentNullException(nameof(b));
            b.Entity<Channel>();
        }

        [SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "It is validated, though")]
        public bool Equals(Channel? other) => other is not null && other.Name == Name;

        public override bool Equals(object? obj) => Equals(obj as Channel);

        public override int GetHashCode() => Name.GetHashCode(StringComparison.InvariantCulture);
    }
}