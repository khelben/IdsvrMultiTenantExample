using System;
using IdentityServer4.Configuration;
using IdentityServer4.Services.Default;
using IdentityServer4.Stores;
using IdentityServer4.Stores.InMemory;
using IdsvrMultiTenant.Services;
using IdsvrMultiTenant.Services.IdSvr;
using IdsvrMultiTenant.Services.MultiTenancy;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace IdsvrMultiTenant
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true);

            if (env.IsDevelopment())
            {
                // For more details on using the user secret store see http://go.microsoft.com/fwlink/?LinkID=532709
                builder.AddUserSecrets();
            }

            builder.AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();

            // Saaskit multitenant applications
            services.AddMultitenancy<IdsvrTenant, IdsvrTenantResolver>();
            var identityServerBuilder = services.AddIdentityServer();

            // ----------------------------------------------------------------------------------------------
            // standard IdSvr services that we need to define because we don't use AddDeveloperIdentityServer
            //
            // this is a required service by IdSvr4 - no need for a tenant specific version
            identityServerBuilder.Services.AddSingleton<IPersistedGrantStore, InMemoryPersistedGrantStore>();
            // scopes are not tenant specific, we can use 1 store for all tenants
            identityServerBuilder.Services.AddSingleton<IScopeStore>(_ => new InMemoryScopeStore(Scopes.Get()));
            // just a development cert, remember it's best to restart your clients when you restart the server
            identityServerBuilder.SetTemporarySigningCredential();


            // ---------------------------------------------------------------------------------------------
            // tenant specific services
            //
            // we replace the default IdentityServerOptions with a custom one that returns tenant specific
            // IdentityServerOptions
            identityServerBuilder.Services.AddTransient<IdentityServerOptions, TenantSpecificIdentityServerOptions>();
            // clients are tenant specific
            identityServerBuilder.Services.AddScoped<IClientStore, ClientStoreResolver>();
            // extraction of the IdSvr4 host example InMemoryLoginService so that we can resolve
            // this per tenant
            identityServerBuilder.Services.AddScoped<IUserLoginService, UserLoginResolver>();

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            app.UseStaticFiles();

            // multitenant aware Idsvr is mounted on "/tenants/<tenant>/" via this construction
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

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
