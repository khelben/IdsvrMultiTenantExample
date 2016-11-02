using System;
using IdentityServer4.Configuration;
using IdsvrMultiTenant.Services.MultiTenancy;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace IdsvrMultiTenant.Services
{
    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseMultiTenantIdSvr(this IApplicationBuilder app)
        {
            app.Map("/tenants", multiTenantIdsvrMountPoint =>
            {
                // Saaskit.Multitenancy
                multiTenantIdsvrMountPoint.UseMultitenancy<IdsvrTenant>();
                multiTenantIdsvrMountPoint.UsePerTenant<IdsvrTenant>((ctx, builder) =>
                {
                    var mountPath = "/" + ctx.Tenant.Name.ToLowerInvariant();
                    // we mount the tenant specific IdSvr4 app under /tenants/<tenant>/
                    builder.Map(mountPath, idsvrForTenantApp =>
                    {
                        var identityServerOptions = idsvrForTenantApp.ApplicationServices.GetRequiredService<IdentityServerOptions>();

                        // we use our own cookie middleware because Idsvr4 expects it to be included in the
                        // pipeline as we have changed the default authentication scheme
                        idsvrForTenantApp.UseCookieAuthentication(new CookieAuthenticationOptions()
                        {
                            AuthenticationScheme = identityServerOptions.AuthenticationOptions.AuthenticationScheme,
                            CookieName = identityServerOptions.AuthenticationOptions.AuthenticationScheme,
                            AutomaticAuthenticate = true,
                            SlidingExpiration = false,
                            ExpireTimeSpan = TimeSpan.FromHours(10)
                        });

                        idsvrForTenantApp.UseIdentityServer();

                        idsvrForTenantApp.UseMvc(routes =>
                        {
                            routes.MapRoute(name: "Account",
                                template: "account/{action=Index}",
                                defaults: new {controller = "Account"});
                        });
                    });
                });
            });
            return app;
        }
    }
}