using Hive.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Hive.Services
{
    public class PermissionsAuthorizationHandler : AuthorizationHandler<PermissionsRequirement, PermissionContext>
    {
        private readonly PermissionsManager<PermissionContext> permissions;

        public PermissionsAuthorizationHandler(PermissionsManager<PermissionContext> perms)
        {
            permissions = perms;
        }

        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionsRequirement requirement, PermissionContext contextObj)
        {
            // TODO: Create our contextObj from the requirement. If we need to fetch mods, we grab them
            // This should be used in our permissions manager, when we check permissions.
            // actionParseState exists for each unique attribute, that is, for each unique requirement.Action.
            if (permissions.CanDo(requirement.Action, contextObj, ref requirement.actionParseState))
                context.Succeed(requirement);
            return Task.CompletedTask;
        }
    }
}