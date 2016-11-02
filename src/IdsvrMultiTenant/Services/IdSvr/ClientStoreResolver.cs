using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using IdentityModel;
using IdentityServer4;
using IdentityServer4.Models;
using IdentityServer4.Services.InMemory;
using IdentityServer4.Stores;
using IdentityServer4.Stores.InMemory;
using IdsvrMultiTenant.Services.MultiTenancy;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.Query.ExpressionTranslators.Internal;

namespace IdsvrMultiTenant.Services.IdSvr
{
    public class ClientStoreResolver : IClientStore
    {
        private readonly IClientStore _clientStoreImplementation;

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
                    RequireConsent = false
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
                    RequireConsent = false
                }
            };
        }
    }
}