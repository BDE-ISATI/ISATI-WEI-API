using IsatiWei.Api.Models;
using IsatiWei.Api.Services;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IsatiWei.Api.Helpers
{
    // This class manage the permissions
    public class AuthenticationMiddleware
    {
        private readonly AuthenticationService _authenticationService;
        private readonly RequestDelegate _next;

        public AuthenticationMiddleware(RequestDelegate next, AuthenticationService authenticationService)
        {
            _authenticationService = authenticationService;
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            // We can freely login
            if (context.Request.Path.StartsWithSegments("/api/authentication"))
            {
                await _next.Invoke(context);
            }
            else
            {
                string authHeader = context.Request.Headers["Authorization"];
                if (authHeader == null || !authHeader.StartsWith("Basic"))
                {
                    context.Response.StatusCode = 401;
                    return;
                }
                
                //Extract credentials
                string encodedUsernamePassword = authHeader.Substring("Basic ".Length).Trim();
                Encoding encoding = Encoding.GetEncoding("iso-8859-1");
                string idAndPassword = encoding.GetString(Convert.FromBase64String(encodedUsernamePassword));

                int seperatorIndex = idAndPassword.IndexOf(':');

                var id = idAndPassword.Substring(0, seperatorIndex);
                var passwordHash = idAndPassword.Substring(seperatorIndex + 1);

                // Check if the user exist and has the right password
                if (!await _authenticationService.CheckCredentialAsync(id, passwordHash, UserRoles.Default))
                {
                    context.Response.StatusCode = 401;
                    return;
                }

                // Only administrators can add/update/delete things
                if (context.Request.Path.Value.ToLower().Contains("add") ||
                    context.Request.Path.Value.ToLower().Contains("admin_update") ||
                    context.Request.Path.Value.ToLower().Contains("delete"))
                {
                    if (await _authenticationService.CheckCredentialAsync(id, passwordHash, UserRoles.Administrator))
                    {
                        await _next.Invoke(context);
                    }
                    else
                    {
                        context.Response.StatusCode = 401;
                        return;
                    }
                }
                else
                {
                    await CheckAuthorizationForGame(context, id, passwordHash);
                }
            }
        }

        private async Task CheckAuthorizationForGame(HttpContext context, string id, string passwordHash)
        {
            // Captain related request
            if (context.Request.Path.Value.ToLower().Contains("challenges/waiting") ||
                context.Request.Path.Value.ToLower().Contains("validate_for_user") ||
                context.Request.Path.Value.ToLower().Contains("validate_for_team") ||
                context.Request.Path.Value.ToLower().Contains("challenges/proof") ||
                context.Request.Path.Value.ToLower().Contains("add_user"))
            {
                if (!await _authenticationService.CheckCredentialAsync(id, passwordHash, UserRoles.Captain))
                {
                    context.Response.StatusCode = 401;
                    return;
                }
            }

            await _next.Invoke(context);
        }
    }
}
