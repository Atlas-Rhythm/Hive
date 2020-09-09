using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Hive
{
    internal class RequirePermissionAttribute : AuthorizeAttribute
    {
        internal const string PolicyPrefix = "HivePermission";

        public RequirePermissionAttribute(string action) => Action = action;

        /// <summary>
        /// Gets or sets the action string used for this attribute
        /// </summary>
        public string? Action
        {
            get => Policy?.Substring(PolicyPrefix.Length);
            set => Policy = PolicyPrefix + value;
        }
    }
}