# Multi-tenant IdentityServer4 example application

This is an example of how we use [IdentityServer4](https://github.com/IdentityServer/IdentityServer4) 
in a multi-tenant environment.

## Why?

I see a lot of questions on how to use [IdentityServer4](https://github.com/IdentityServer/IdentityServer4) 
as an authentication provider in a multi tenant environment. 
This is a way of showing how we solved this problem and to get feedback from you.

## What needed to be solved?

Well, a lot of things actually, although not all related to [IdentityServer4](https://github.com/IdentityServer/IdentityServer4) 
but it needed solving like:

* how to do multitenancy?
* how to handle tenant specific IdSvr services?
* ...

## How did we solved it?

### Multi tenancy

For this we've used [Saaskit.MultiTenancy](https://github.com/saaskit/saaskit).  This is a great library for creating
multi tenant applications in .NetCore.

This is a snippet ([Startup.cs](https://github.com/khelben/IdsvrMultiTenantExample/blob/master/src/IdsvrMultiTenant/Startup.cs)) 
on how we mount the IdentityServer in a tenant aware way 
(we mount the IdSvr under the path `/tenants/<tenant>/`)

```csharp
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
```

### Tenant specific IdSvr services

Some services need to be tenant aware (e.g. ClientStore,...).  To solve this, we've used a pattern where a general service
will delegate all methods to a tenant specific service.

Example (taken from [src/IdsvrMultiTenant/Services/IdSvr/ClientStoreResolver.cs](https://github.com/khelben/IdsvrMultiTenantExample/blob/master/src/IdsvrMultiTenant/Services/IdSvr/ClientStoreResolver.cs))

```csharp
    public class ClientStoreResolver : IClientStore
    {
        private IClientStore _clientStoreImplementation;


        public ClientStoreResolver(IHttpContextAccessor httpContextAccessor)
        {
            if(httpContextAccessor == null)
                throw new ArgumentNullException(nameof(httpContextAccessor));

            // just to be sure, we are in a tenant context
            var tenantContext = httpContextAccessor.HttpContext.GetTenantContext<IdsvrTenant>();
            if(tenantContext == null)
                throw new ArgumentNullException(nameof(tenantContext));

            // based on the current tenant, we can redirect to the proper client store
            if (tenantContext.Tenant.Name == "first")
            {
                _clientStoreImplementation = new InMemoryClientStore(GetClientsForFirstClient());
            }
            else if (tenantContext.Tenant.Name == "second")
            {
                _clientStoreImplementation = new InMemoryClientStore(GetClientsForSecondClient());
            }
            else
            {
                // all other tenants have no clients registered in this example
                _clientStoreImplementation = new InMemoryClientStore(new List<Client>());
            }
        }

        public Task<Client> FindClientByIdAsync(string clientId)
        {
            return _clientStoreImplementation.FindClientByIdAsync(clientId);
        }

        private List<Client> GetClientsForFirstClient()
        {
            return new List<Client>
            {
                new Client()
                {
                    ClientId  = "FirstTenantClient",
                    AllowedGrantTypes = new []{ GrantType.AuthorizationCode },
                    RedirectUris = new List<string>()
                    {
                        "http://localhost:5000/signin-oidc"
                    },
                    ClientSecrets = new List<Secret>()
                    {
                        new Secret()
                        {
                            Value = "FirstTenant-ClientSecret".Sha256()
                        }
                    },
                    AllowedScopes = { StandardScopes.OpenId.Name, StandardScopes.Profile.Name },
                    RequireConsent = false,
                    PostLogoutRedirectUris = new List<string>()
                    {
                        "http://localhost:5000/"
                    }
                }
            };
        }

        private List<Client> GetClientsForSecondClient()
        {
            return new List<Client>
            {
                new Client()
                {
                    ClientId  = "SecondTenantClient",
                    AllowedGrantTypes = new []{ GrantType.AuthorizationCode },
                    RedirectUris = new List<string>()
                    {
                        "http://localhost:5001/signin-oidc"
                    },
                    ClientSecrets = new List<Secret>()
                    {
                        new Secret()
                        {
                            Value = "SecondTenant-ClientSecret".Sha256()
                        }
                    },
                    AllowedScopes = { StandardScopes.OpenId.Name, StandardScopes.Profile.Name },
                    RequireConsent = false,
                    PostLogoutRedirectUris = new List<string>()
                    {
                        "http://localhost:5001/"
                    }
                }
            };
        }
    }
```

## What's included?

This example exists of:

* a hosted multi tenant aware IdSvr4 in [src/IdSvrMultitenant](https://github.com/khelben/IdsvrMultiTenantExample/tree/master/src/IdsvrMultiTenant)
* two client apps [src/FirstTenantClientApp](https://github.com/khelben/IdsvrMultiTenantExample/tree/master/src/FirstTenantClientApp) 
and [src/SecondTenantClientApp](https://github.com/khelben/IdsvrMultiTenantExample/tree/master/src/SecondTenantClientApp)
that connect to `/tenants/first/...` and `/tenants/second/...` resp.

In order to run, just execute `dotnet run` in the three folders.

## Your input

For any questions or remarks, please enter a new issue [Issues](https://github.com/khelben/IdsvrMultiTenantExample/issues)

Feel free to make a pull request for any suggestion you'd have [Pull requests](https://github.com/khelben/IdsvrMultiTenantExample/pulls)

