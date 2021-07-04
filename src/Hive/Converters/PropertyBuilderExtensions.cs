using Hive.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Hive.Converters
{
    /// <summary>
    /// <see cref="PropertyBuilder{TProperty}"/> extension methods.
    /// </summary>
    public static class PropertyBuilderExtensions
    {
        /// <summary>
        /// Compares to check if input is a vaulth user.
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        public static PropertyBuilder<User> IsVaulthUser([DisallowNull] this PropertyBuilder<User> b)
        {
            if (b is null)
                throw new ArgumentNullException(nameof(b));
            b.HasConversion(new ValueConverter<User, string>(
                    u => u.Username,
                    s => new User { Username = s }, null)
                ).Metadata.SetValueComparer(new ValueComparer<User>(
                    (a, b) => a.Username == b.Username,
                    u => u.Username.GetHashCode(StringComparison.InvariantCulture),
                    u => new User { Username = u.Username }
                ));

            return b;
        }

        /// <summary>
        /// Compares to check if input are all valid users
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        [SuppressMessage("Style", "IDE0004:Remove Unnecessary Cast", Justification = "Cast is required for EF")]
        public static PropertyBuilder<IList<User>> IsValidUsers([DisallowNull] this PropertyBuilder<IList<User>> b)
        {
            if (b is null)
                throw new ArgumentNullException(nameof(b));
            b.HasConversion(new ValueConverter<IList<User>, string[]>(
                      u => u.Select(u => u.Username).ToArray(),
                      s => s.Select(s => new User { Username = s }).ToList(), null)
                ).Metadata.SetValueComparer(new ValueComparer<IList<User>>(
                    (a, b) => a.SequenceEqual(b, UserComparer.Instance),
                    l => l.Aggregate(0, (a, v) => HashCode.Combine(a, v.Username.GetHashCode(StringComparison.InvariantCulture))),
                    l => l.ToList() as IList<User> // DO NOT REMOVE THIS CAST! IT IS REQUIRED FOR THIS TO WORK.
                ));

            return b;
        }

        private sealed class UserComparer : IEqualityComparer<User>
        {
            public static readonly UserComparer Instance = new();

            public bool Equals([AllowNull] User x, [AllowNull] User y)
                => x?.Username == y?.Username;

            public int GetHashCode([DisallowNull] User obj)
                => obj.Username.GetHashCode(StringComparison.InvariantCulture);
        }
    }
}
