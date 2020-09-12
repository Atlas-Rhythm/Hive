using Hive.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

namespace Hive.Converters
{
    public static class PropertyBuilderExtensions
    {
        public static PropertyBuilder<User> IsVaulthUser([DisallowNull] this PropertyBuilder<User> b)
        {
            if (b is null)
                throw new ArgumentNullException(nameof(b));
            b.HasConversion(new ValueConverter<User, string>(
                u => u.DumbId,
                s => new User { DumbId = s }, null));
            b.Metadata.SetValueComparer(new ValueComparer<User>(
                    (a, b) => a.DumbId == b.DumbId,
                    u => u.DumbId.GetHashCode(StringComparison.InvariantCulture),
                    u => new User { DumbId = u.DumbId }
                ));

            return b;
        }

        public static PropertyBuilder<IList<User>> IsVaulthUsers([DisallowNull] this PropertyBuilder<IList<User>> b)
        {
            if (b is null)
                throw new ArgumentNullException(nameof(b));
            b.HasConversion(new ValueConverter<IList<User>, string[]>(
                      u => u.Select(u => u.DumbId).ToArray(),
                      s => s.Select(s => new User { DumbId = s }).ToList(), null));
            b.Metadata.SetValueComparer(new ValueComparer<IList<User>>(
                    (a, b) => a.SequenceEqual(b, UserComparer.Instance),
                    l => l.Aggregate(0, (a, v) => HashCode.Combine(a, v.DumbId.GetHashCode(StringComparison.InvariantCulture))),
                    l => l.ToList() as IList<User> // DO NOT REMOVE THIS CAST! IT IS REQUIRED FOR THIS TO WORK.
                ));

            return b;
        }

        private sealed class UserComparer : IEqualityComparer<User>
        {
            public static readonly UserComparer Instance = new UserComparer();

            public bool Equals([AllowNull] User x, [AllowNull] User y)
                => x?.DumbId == y?.DumbId;

            public int GetHashCode([DisallowNull] User obj)
                => obj.DumbId.GetHashCode(StringComparison.InvariantCulture);
        }
    }
}