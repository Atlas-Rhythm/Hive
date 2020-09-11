using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Hive.Services
{
    internal class PermissionsPolicyProvider : IAuthorizationPolicyProvider
    {
        public Task<AuthorizationPolicy> GetDefaultPolicyAsync()
        {
            var policy = new AuthorizationPolicyBuilder(CookieAuthenticationDefaults.AuthenticationScheme);
            return Task.FromResult(policy.Build());
        }

        public async Task<AuthorizationPolicy?> GetFallbackPolicyAsync()
        {
            return await GetDefaultPolicyAsync().ConfigureAwait(false);
        }

        public async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
        {
            var policy = new AuthorizationPolicyBuilder(CookieAuthenticationDefaults.AuthenticationScheme);
            if (policyName.StartsWith(RequirePermissionAttribute.PolicyPrefix, StringComparison.OrdinalIgnoreCase))
            {
                var action = policyName.Substring(RequirePermissionAttribute.PolicyPrefix.Length);
                policy.AddRequirements(new PermissionsRequirement(action));
                return policy.Build();
            }
            return await GetDefaultPolicyAsync().ConfigureAwait(false);
        }
    }
}