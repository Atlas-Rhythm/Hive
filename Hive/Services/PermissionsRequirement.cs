using Hive.Permissions;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Hive.Services
{
    /// <summary>
    /// An <see cref="IAuthorizationRequirement"/> that declares a given permission string must evaluate to true with <see cref="PermissionContext"/>
    /// </summary>
    public class PermissionsRequirement : IAuthorizationRequirement
    {
        internal string Action { get; }
        internal PermissionActionParseState actionParseState;

        public PermissionsRequirement(string action)
        {
            Action = action;
        }
    }
}