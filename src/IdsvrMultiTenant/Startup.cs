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

            // we replace the default IdentityServerOptions with a custom one that returns tenant specific
            // IdentityServerOptions
            identityServerBuilder.Services.AddTransient<IdentityServerOptions, TenantSpecificIdentityServerOptions>();

            // this is a required service by IdSvr4 - no need for a tenant specific version
            identityServerBuilder.Services.AddSingleton<IPersistedGrantStore, InMemoryPersistedGrantStore>();

            // clients are tenant specific
            identityServerBuilder.Services.AddScoped<IClientStore, ClientStoreResolver>();

            // scopes are not tenant specific, we can use 1 store for all tenants
            identityServerBuilder.Services.AddSingleton<IScopeStore>(_ => new InMemoryScopeStore(Scopes.Get()));

            // extraction of the IdSvr4 host example InMemoryLoginService so that we can resolve
            // this per tenant
            identityServerBuilder.Services.AddScoped<IUserLoginService, UserLoginResolver>();

            // just a development cert
            identityServerBuilder.SetTemporarySigningCredential();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            //if (env.IsDevelopment())
            //{
            //    app.UseDeveloperExceptionPage();
            //    app.UseDatabaseErrorPage();
            //    app.UseBrowserLink();
            //}
            //else
            //{
            //    app.UseExceptionHandler("/Home/Error");
            //}

            app.UseStaticFiles();

            // multitenant aware Idsvr is mounted on "/tenants/<tenant>/" via this construction
            app.UseMultiTenantIdSvr();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
