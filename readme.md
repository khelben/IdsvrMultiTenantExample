# Multi-tenant IdentityServer4 example application

This is an example of how we use [IdentityServer4](https://github.com/IdentityServer/IdentityServer4) 
in a multi-tenant environment.

The example consists of 2 parts.

* the multi-tenant aware IdSvr which is actually just a gateway to external providers
* the external provider which is also an Idsvr4 implementation, 
based on [IdentityServer4.AspIdentity](https://github.com/IdentityServer/IdentityServer4.AspNetIdentity)