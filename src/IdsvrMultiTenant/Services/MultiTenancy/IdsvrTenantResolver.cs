using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using SaasKit.Multitenancy;

namespace IdsvrMultiTenant.Services.MultiTenancy
{
    public class IdsvrTenantResolver : ITenantResolver<IdsvrTenant>
    {
        public Task<TenantContext<IdsvrTenant>> ResolveAsync(HttpContext context)
        {
            TenantContext<IdsvrTenant> tenantContext = null;

            ExtractTenantFromRequest(context, tenantName =>
            {
                tenantContext = new TenantContext<IdsvrTenant>(new IdsvrTenant() { Name = tenantName });
            });

            return Task.FromResult<TenantContext<IdsvrTenant>>(tenantContext);
        }

        private void ExtractTenantFromRequest(HttpContext context, Action<string> callBack)
        {
            var pattern = new Regex(@"\/(\w+)");
            var currentPath = context.Request.Path.Value ?? "";
            if (pattern.IsMatch(currentPath))
            {
                var matches = pattern.Matches(currentPath);

                callBack(matches[0].Groups[1].Value.ToLowerInvariant());
            }
        }
    }
}