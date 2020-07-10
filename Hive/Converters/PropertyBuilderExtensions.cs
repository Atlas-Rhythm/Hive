using Hive.Models;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Hive.Converters
{
    public static class PropertyBuilderExtensions
    {
        public static PropertyBuilder<User> IsVaulthUser(this PropertyBuilder<User> b)
            => b.HasConversion(new VaulthUserConverter());
        public static PropertyBuilder<List<User>> IsVaulthUsers(this PropertyBuilder<List<User>> b)
            => b.HasConversion(new VaulthUsersConverter());
    }
}
