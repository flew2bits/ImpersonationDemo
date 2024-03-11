using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

var builder = WebApplication.CreateBuilder(args);

var impersonators = builder.Configuration.GetSection("CanImpersonate").Get<string[]>() ?? Array.Empty<string>();

builder.Services.AddAuthentication(opt =>
    {
        opt.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        opt.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
    })
    .AddCookie()
    .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, opt =>
    {
        opt.ClientId = "6c1151b8-e4fa-41c1-b6cf-b08740169d93";
        opt.Authority = "https://login.microsoft.com/277d564c-30a9-4bce-a18d-afc8e54540e5";
    });
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("CanImpersonate",
        policy => policy.RequireAssertion(ctx =>
            impersonators.Contains(ctx.User.FindFirstValue(ClaimTypes.Upn)) ||
            ctx.User.HasClaim(c => c.Type == "_Impersonator")));

builder.Services.AddRazorPages(opt =>
{
    opt.Conventions.AuthorizePage("/Index");
    opt.Conventions.AuthorizePage("/Impersonate", "CanImpersonate");
});

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();

app.Run();