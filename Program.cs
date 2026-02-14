using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using firstClaudeApp.Components;
using firstClaudeApp.Data;

var builder = WebApplication.CreateBuilder(args);

// Add Entity Framework Core with SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add ASP.NET Core Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Add Authentication with Google OAuth
builder.Services.AddAuthentication()
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"] ?? "";
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? "";
    });

// Configure cookie settings
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
    app.UseStatusCodePagesWithReExecute("/not-found");
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Register endpoint
app.MapPost("/Account/PerformRegister", async (
    HttpContext context,
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager) =>
{
    var form = await context.Request.ReadFormAsync();
    var displayName = form["displayName"].ToString();
    var email = form["email"].ToString();
    var password = form["password"].ToString();
    var confirmPassword = form["confirmPassword"].ToString();

    if (password != confirmPassword)
    {
        return Results.Redirect("/Account/Register?error=Passwords+do+not+match");
    }

    var user = new ApplicationUser
    {
        UserName = email,
        Email = email,
        DisplayName = displayName
    };

    var result = await userManager.CreateAsync(user, password);

    if (result.Succeeded)
    {
        await signInManager.SignInAsync(user, isPersistent: false);
        return Results.Redirect("/");
    }

    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
    return Results.Redirect($"/Account/Register?error={Uri.EscapeDataString(errors)}");
});

// Login endpoint
app.MapPost("/Account/PerformLogin", async (
    HttpContext context,
    SignInManager<ApplicationUser> signInManager) =>
{
    var form = await context.Request.ReadFormAsync();
    var email = form["email"].ToString();
    var password = form["password"].ToString();
    var rememberMe = form["rememberMe"].ToString() == "true";
    var returnUrl = form["returnUrl"].ToString();

    if (string.IsNullOrEmpty(returnUrl)) returnUrl = "/";

    var result = await signInManager.PasswordSignInAsync(email, password, rememberMe, lockoutOnFailure: false);

    if (result.Succeeded)
    {
        return Results.Redirect(returnUrl);
    }

    return Results.Redirect($"/Account/Login?error=Invalid+login+attempt&returnUrl={Uri.EscapeDataString(returnUrl)}");
});

// Logout endpoint
app.MapPost("/Account/PerformLogout", async (SignInManager<ApplicationUser> signInManager) =>
{
    await signInManager.SignOutAsync();
    return Results.Redirect("/");
});

// External login endpoints
app.MapPost("/Account/ExternalLogin", async (HttpContext context, SignInManager<ApplicationUser> signInManager) =>
{
    var provider = context.Request.Form["provider"].ToString();
    var returnUrl = context.Request.Form["returnUrl"].ToString() ?? "/";

    var redirectUrl = $"/Account/ExternalLoginCallback?returnUrl={Uri.EscapeDataString(returnUrl)}";
    var properties = signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);

    return Results.Challenge(properties, [provider]);
});

app.MapGet("/Account/ExternalLoginCallback", async (
    HttpContext context,
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> userManager,
    string? returnUrl = null,
    string? remoteError = null) =>
{
    if (string.IsNullOrEmpty(returnUrl)) returnUrl = "/";

    if (remoteError != null)
    {
        return Results.Redirect($"/Account/Login?error={Uri.EscapeDataString(remoteError)}");
    }

    var info = await signInManager.GetExternalLoginInfoAsync();
    if (info == null)
    {
        return Results.Redirect("/Account/Login");
    }

    // Sign in the user with the external login provider
    var result = await signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);

    if (result.Succeeded)
    {
        return Results.Redirect(returnUrl);
    }

    // If the user does not have an account, create one
    var email = info.Principal.FindFirstValue(ClaimTypes.Email);
    var name = info.Principal.FindFirstValue(ClaimTypes.Name);

    if (email != null)
    {
        var user = await userManager.FindByEmailAsync(email);

        if (user == null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                DisplayName = name ?? email
            };

            await userManager.CreateAsync(user);
        }

        await userManager.AddLoginAsync(user, info);
        await signInManager.SignInAsync(user, isPersistent: false);

        return Results.Redirect(returnUrl);
    }

    return Results.Redirect("/Account/Login");
});

app.Run();
