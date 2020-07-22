using Hive.Permissions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Hive.Services
{
    public class PermissionsService
    {
        private readonly PermissionsManager<PermissionContext> permissions;
        private readonly Dictionary<string, PermissionActionParseState> cachedStates = new Dictionary<string, PermissionActionParseState>();

        public PermissionsService(PermissionsManager<PermissionContext> manager)
        {
            permissions = manager;
        }

        public bool CanDo(string perm, PermissionContext context)
        {
            if (!cachedStates.TryGetValue(perm, out var state))
                state = new PermissionActionParseState();
            var tmp = permissions.CanDo(perm, context, ref state);
            cachedStates[perm] = state;
            return tmp;
        }
    }
}