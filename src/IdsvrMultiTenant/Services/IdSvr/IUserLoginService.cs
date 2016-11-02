using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using IdentityServer4.Services.InMemory;
using IdsvrMultiTenant.Services.MultiTenancy;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System.Security.Claims;
using IdentityModel;
using IdentityServer4;

namespace IdsvrMultiTenant.Services.IdSvr
{
    /// <summary>
    /// Extraction of the IdSvr4 Host example for validating in memory users.
    /// This is needed because we need to resolve the users per tenant.
    /// </summary>
    public interface IUserLoginService
    {
        bool ValidateCredentials(string username, string password);
        InMemoryUser FindByUsername(string username);
        InMemoryUser FindByExternalProvider(string provider, string userId);
        InMemoryUser AutoProvisionUser(string provider, string userId, List<Claim> claims);
    }

    public class UserLoginResolver : IUserLoginService
    {
        private readonly IUserLoginService _userLoginServiceImplementation;

        public UserLoginResolver(IHttpContextAccessor httpContextAccessor)
        {
            if(httpContextAccessor == null)
                throw new ArgumentNullException(nameof(httpContextAccessor));

            // just to be sure, we are in a tenant context
            var tenantContext = httpContextAccessor.HttpContext.GetTenantContext<IdsvrTenant>();
            if(tenantContext == null)
                throw new ArgumentNullException(nameof(tenantContext));

            if (tenantContext.Tenant.Name == "first")
            {
                _userLoginServiceImplementation = new InMemoryUserLoginService(GetUsersForFirstTenant());
            }
            else if (tenantContext.Tenant.Name == "second")
            {
                _userLoginServiceImplementation = new InMemoryUserLoginService(GetUsersForSecondTenant());
            }
            else
            {
                _userLoginServiceImplementation = new InMemoryUserLoginService(new List<InMemoryUser>());
            }
        }

        public bool ValidateCredentials(string username, string password)
        {
            return _userLoginServiceImplementation.ValidateCredentials(username, password);
        }

        public InMemoryUser FindByUsername(string username)
        {
            return _userLoginServiceImplementation.FindByUsername(username);
        }

        public InMemoryUser FindByExternalProvider(string provider, string userId)
        {
            return _userLoginServiceImplementation.FindByExternalProvider(provider, userId);
        }

        public InMemoryUser AutoProvisionUser(string provider, string userId, List<Claim> claims)
        {
            return _userLoginServiceImplementation.AutoProvisionUser(provider, userId, claims);
        }

        private List<InMemoryUser> GetUsersForFirstTenant()
        {
            return new List<InMemoryUser>()
            {
                new InMemoryUser{Subject = "818727", Username = "alice", Password = "alice",
                    Claims = new Claim[]
                    {
                        new Claim(JwtClaimTypes.Name, "Alice Smith"),
                        new Claim(JwtClaimTypes.GivenName, "Alice"),
                        new Claim(JwtClaimTypes.FamilyName, "Smith"),
                        new Claim(JwtClaimTypes.Email, "AliceSmith@email.com"),
                        new Claim(JwtClaimTypes.EmailVerified, "true", ClaimValueTypes.Boolean),
                        new Claim(JwtClaimTypes.Role, "Admin"),
                        new Claim(JwtClaimTypes.Role, "Geek"),
                        new Claim(JwtClaimTypes.WebSite, "http://alice.com"),
                        new Claim(JwtClaimTypes.Address, @"{ 'street_address': 'One Hacker Way', 'locality': 'Heidelberg', 'postal_code': 69118, 'country': 'Germany' }", IdentityServerConstants.ClaimValueTypes.Json)
                    }
                }
            };
        }

        private List<InMemoryUser> GetUsersForSecondTenant()
        {
            return new List<InMemoryUser>()
            {
                new InMemoryUser{Subject = "88421113", Username = "bob", Password = "bob",
                    Claims = new Claim[]
                    {
                        new Claim(JwtClaimTypes.Name, "Bob Smith"),
                        new Claim(JwtClaimTypes.GivenName, "Bob"),
                        new Claim(JwtClaimTypes.FamilyName, "Smith"),
                        new Claim(JwtClaimTypes.Email, "BobSmith@email.com"),
                        new Claim(JwtClaimTypes.EmailVerified, "true", ClaimValueTypes.Boolean),
                        new Claim(JwtClaimTypes.Role, "Developer"),
                        new Claim(JwtClaimTypes.Role, "Geek"),
                        new Claim(JwtClaimTypes.WebSite, "http://bob.com"),
                        new Claim(JwtClaimTypes.Address, @"{ 'street_address': 'One Hacker Way', 'locality': 'Heidelberg', 'postal_code': 69118, 'country': 'Germany' }", IdentityServerConstants.ClaimValueTypes.Json)
                    }
                }
            };
        }
    }

    public class InMemoryUserLoginService : IUserLoginService
    {
        public List<InMemoryUser> _users;

        public InMemoryUserLoginService(List<InMemoryUser> users)
        {
            _users = users;
        }

        public bool ValidateCredentials(string username, string password)
        {
            var user = FindByUsername(username);
            if (user != null)
            {
                return user.Password.Equals(password);
            }

            return false;
        }

        public InMemoryUser FindByUsername(string username)
        {
            return _users.FirstOrDefault(x=>x.Username.Equals(username, System.StringComparison.OrdinalIgnoreCase));
        }

        public InMemoryUser FindByExternalProvider(string provider, string userId)
        {
            return _users.FirstOrDefault(x =>
                x.Provider == provider &&
                x.ProviderId == userId);
        }

        public InMemoryUser AutoProvisionUser(string provider, string userId, List<Claim> claims)
        {
            // create a list of claims that we want to transfer into our store
            var filtered = new List<Claim>();

            foreach(var claim in claims)
            {
                // if the external system sends a display name - translate that to the standard OIDC name claim
                if (claim.Type == ClaimTypes.Name)
                {
                    filtered.Add(new Claim(JwtClaimTypes.Name, claim.Value));
                }
                // if the JWT handler has an outbound mapping to an OIDC claim use that
                else if (JwtSecurityTokenHandler.DefaultOutboundClaimTypeMap.ContainsKey(claim.Type))
                {
                    filtered.Add(new Claim(JwtSecurityTokenHandler.DefaultOutboundClaimTypeMap[claim.Type], claim.Value));
                }
                // copy the claim as-is
                else
                {
                    filtered.Add(claim);
                }
            }

            // if no display name was provided, try to construct by first and/or last name
            if (!filtered.Any(x=>x.Type == JwtClaimTypes.Name))
            {
                var first = filtered.FirstOrDefault(x => x.Type == JwtClaimTypes.GivenName)?.Value;
                var last = filtered.FirstOrDefault(x => x.Type == JwtClaimTypes.FamilyName)?.Value;
                if (first != null && last != null)
                {
                    filtered.Add(new Claim(JwtClaimTypes.Name, first + " " + last));
                }
                else if (first != null)
                {
                    filtered.Add(new Claim(JwtClaimTypes.Name, first));
                }
                else if (last != null)
                {
                    filtered.Add(new Claim(JwtClaimTypes.Name, last));
                }
            }

            // create a new unique subject id
            var sub = CryptoRandom.CreateUniqueId();

            // check if a display name is available, otherwise fallback to subject id
            var name = filtered.FirstOrDefault(c => c.Type == JwtClaimTypes.Name)?.Value ?? sub;

            // create new user
            var user = new InMemoryUser()
            {
                Enabled = true,
                Subject = sub,
                Username = name,
                Provider = provider,
                ProviderId = userId,
                Claims = filtered
            };

            // add user to in-memory store
            _users.Add(user);

            return user;
        }
    }
}