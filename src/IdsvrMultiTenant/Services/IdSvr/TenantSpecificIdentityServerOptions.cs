using System;
using IdentityServer4.Configuration;
using IdsvrMultiTenant.Services.MultiTenancy;
using Microsoft.AspNetCore.Http;

namespace IdsvrMultiTenant.Services.IdSvr
{
    public class TenantSpecificIdentityServerOptions : IdentityServerOptions
    {
        public TenantSpecificIdentityServerOptions(IHttpContextAccessor httpContextAccessor)
        {
            // get the current TenantContext (courtesy of Saaskit.Multitenancy)
            if(httpContextAccessor == null)
                throw new ArgumentNullException(nameof(httpContextAccessor));

            // just to be sure, we are in a tenant context
            var tenantContext = httpContextAccessor.HttpContext.GetTenantContext<IdsvrTenant>();
            if(tenantContext == null)
                throw new ArgumentNullException(nameof(tenantContext));

            // now we can have tenantspecific IdentityServerOptions

            // we scope the IdSvr cookie with the tenant name, to avoid potential conflicts
            AuthenticationOptions.AuthenticationScheme = "idsvr.tenants." + tenantContext.Tenant.Name;
        }
    }
}