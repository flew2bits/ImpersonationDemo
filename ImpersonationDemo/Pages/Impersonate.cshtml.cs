using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ImpersonationDemo.Pages;

file record Impersonator(string? AuthenticationType, ImpersonatorClaim[] Claims);

file record ImpersonatorClaim(string Type, string Value);

public class Impersonate : PageModel
{
    [BindProperty]
    public string Email { get; set; } =string.Empty;

    [BindProperty] public string?[] Roles { get; set; } = Array.Empty<string>();
    
    [BindProperty]
    public string FirstName { get; set; } =string.Empty;
    
    [BindProperty]
    public string LastName { get; set; } =string.Empty;
    
    public void OnGet()
    {
        
    }

    public async Task<IActionResult> OnPost()
    {
        var impersonator = User.Identities.Select(i => new Impersonator(i.AuthenticationType, i.Claims.Select(c => new ImpersonatorClaim(c.Type, c.Value)).ToArray())).ToArray();
        var impersonatorJson = System.Text.Json.JsonSerializer.Serialize(impersonator);
        var impersonatorClaim = new Claim("_Impersonator", Convert.ToBase64String(Encoding.UTF8.GetBytes(impersonatorJson)));

        var impersonatedClaims = new List<Claim>
        {
            new(ClaimTypes.Upn, Email),
            new(ClaimTypes.Name, Email),
            new(ClaimTypes.Surname, LastName),
            new(ClaimTypes.GivenName, FirstName),
            impersonatorClaim
        };
        impersonatedClaims.AddRange(Roles.Where(r => r is not null).Select(r => new Claim(ClaimTypes.Role, r)));
        var newIdentity = new ClaimsIdentity(impersonatedClaims, "impersonation");
        var newUser = new ClaimsPrincipal(newIdentity);
        
        await HttpContext.SignOutAsync();
        await HttpContext.SignInAsync(newUser);

        return RedirectToPage("/Index");
    }

    public async Task<IActionResult> OnPostStopImpersonating()
    {
        var impersonatorClaim = User.FindFirstValue("_Impersonator");
        if (impersonatorClaim is null) return NotFound();

        var impersonatorJson = Encoding.UTF8.GetString(Convert.FromBase64String(impersonatorClaim));
        var impersonator = System.Text.Json.JsonSerializer.Deserialize<Impersonator[]>(impersonatorJson);

        if (impersonator is null) return NotFound();

        var identities = impersonator.Select(i =>
            new ClaimsIdentity(i.Claims.Select(c => new Claim(c.Type, c.Value)), i.AuthenticationType));
        var user = new ClaimsPrincipal(identities);

        await HttpContext.SignOutAsync();
        await HttpContext.SignInAsync(user);

        return RedirectToPage();
    }
}