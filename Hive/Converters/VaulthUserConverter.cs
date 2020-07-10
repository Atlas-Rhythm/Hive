using Hive.Models;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Hive.Converters
{
    public class VaulthUserConverter : ValueConverter<User, string>
    {
        public VaulthUserConverter() : base(u => u.DumbId, s => new User { DumbId = s }, null)
        {
        }
    }
    public class VaulthUsersConverter : ValueConverter<List<User>, string[]>
    {
        public VaulthUsersConverter() : base(u => u.Select(u => u.DumbId).ToArray(), s => s.Select(s => new User { DumbId = s }).ToList(), null)
        {
        }
    }
}
