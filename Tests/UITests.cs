using Bunit;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using firstClaudeApp.Data;
using firstClaudeApp.Components.Pages.Account;
using Xunit;
using Moq;

namespace firstClaudeApp.Tests;

public class UITests : BunitContext
{
    public UITests()
    {
        // Configure in-memory database
        Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));

        // Configure Identity
        Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequiredLength = 6;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        Services.AddLogging();

        // Mock IHttpContextAccessor
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var mockHttpContext = new Mock<HttpContext>();
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(mockHttpContext.Object);
        Services.AddSingleton(mockHttpContextAccessor.Object);

        // Mock IAntiforgery
        var mockAntiforgery = new Mock<IAntiforgery>();
        Services.AddSingleton(mockAntiforgery.Object);

        Services.AddAuthentication();
    }

    #region Register Component UI Tests

    [Fact]
    public void Register_ShouldRenderFormWithAllFields()
    {
        // Act
        var cut = Render<Register>();

        // Assert - Check all form fields are present
        Assert.NotNull(cut.Find("input#displayName"));
        Assert.NotNull(cut.Find("input#email"));
        Assert.NotNull(cut.Find("input#password"));
        Assert.NotNull(cut.Find("input#confirmPassword"));
        Assert.NotNull(cut.Find("button[type='submit']"));
    }

    [Fact]
    public void Register_ShouldHaveCorrectPageTitle()
    {
        // Act
        var cut = Render<Register>();

        // Assert
        Assert.Contains("Register", cut.Markup);
        Assert.Contains("Create a new account", cut.Markup);
    }

    [Fact]
    public void Register_ShouldHaveLinkToLogin()
    {
        // Act
        var cut = Render<Register>();

        // Assert
        var loginLink = cut.Find("a[href='/Account/Login']");
        Assert.NotNull(loginLink);
        Assert.Contains("Login here", loginLink.TextContent);
    }

    [Fact]
    public void Register_PasswordFields_ShouldBeTypePassword()
    {
        // Act
        var cut = Render<Register>();

        // Assert
        var passwordInput = cut.Find("input#password");
        var confirmPasswordInput = cut.Find("input#confirmPassword");

        Assert.Equal("password", passwordInput.GetAttribute("type"));
        Assert.Equal("password", confirmPasswordInput.GetAttribute("type"));
    }

    [Fact]
    public void Register_SubmitButton_ShouldHaveCorrectText()
    {
        // Act
        var cut = Render<Register>();
        var button = cut.Find("button[type='submit']");

        // Assert
        Assert.Equal("Register", button.TextContent);
    }

    [Fact]
    public void Register_Form_ShouldPostToCorrectEndpoint()
    {
        // Act
        var cut = Render<Register>();
        var form = cut.Find("form");

        // Assert
        Assert.Equal("/Account/PerformRegister", form.GetAttribute("action"));
        Assert.Equal("post", form.GetAttribute("method"));
    }

    [Fact]
    public void Register_EmailField_ShouldBeRequired()
    {
        // Act
        var cut = Render<Register>();
        var emailInput = cut.Find("input#email");

        // Assert
        Assert.NotNull(emailInput.GetAttribute("required"));
    }

    [Fact]
    public void Register_PasswordField_ShouldHaveMinLength()
    {
        // Act
        var cut = Render<Register>();
        var passwordInput = cut.Find("input#password");

        // Assert
        Assert.Equal("6", passwordInput.GetAttribute("minlength"));
    }

    #endregion

    #region Login Component UI Tests

    [Fact]
    public void Login_ShouldRenderFormWithAllFields()
    {
        // Act
        var cut = Render<Login>();

        // Assert
        Assert.NotNull(cut.Find("input#email"));
        Assert.NotNull(cut.Find("input#password"));
        Assert.NotNull(cut.Find("input#rememberMe"));
        Assert.NotNull(cut.Find("button[type='submit']"));
    }

    [Fact]
    public void Login_ShouldHaveCorrectPageTitle()
    {
        // Act
        var cut = Render<Login>();

        // Assert
        Assert.Contains("Login", cut.Markup);
        Assert.Contains("Sign in with your account", cut.Markup);
    }

    [Fact]
    public void Login_ShouldHaveLinkToRegister()
    {
        // Act
        var cut = Render<Login>();

        // Assert
        var registerLink = cut.Find("a[href='/Account/Register']");
        Assert.NotNull(registerLink);
        Assert.Contains("Register here", registerLink.TextContent);
    }

    [Fact]
    public void Login_ShouldHaveGoogleSignInButton()
    {
        // Act
        var cut = Render<Login>();

        // Assert
        Assert.Contains("Sign in with Google", cut.Markup);
    }

    [Fact]
    public void Login_PasswordField_ShouldBeTypePassword()
    {
        // Act
        var cut = Render<Login>();
        var passwordInput = cut.Find("input#password");

        // Assert
        Assert.Equal("password", passwordInput.GetAttribute("type"));
    }

    [Fact]
    public void Login_RememberMeCheckbox_ShouldBeCheckbox()
    {
        // Act
        var cut = Render<Login>();
        var checkbox = cut.Find("input#rememberMe");

        // Assert
        Assert.Equal("checkbox", checkbox.GetAttribute("type"));
    }

    [Fact]
    public void Login_SubmitButton_ShouldHaveCorrectText()
    {
        // Act
        var cut = Render<Login>();
        var button = cut.Find("button[type='submit']");

        // Assert
        Assert.Equal("Login", button.TextContent);
    }

    [Fact]
    public void Login_Form_ShouldPostToCorrectEndpoint()
    {
        // Act
        var cut = Render<Login>();
        var form = cut.Find("form[action='/Account/PerformLogin']");

        // Assert
        Assert.NotNull(form);
        Assert.Equal("post", form.GetAttribute("method"));
    }

    [Fact]
    public void Login_GoogleButton_ShouldPostToExternalLogin()
    {
        // Act
        var cut = Render<Login>();
        var googleForm = cut.Find("form[action='/Account/ExternalLogin']");

        // Assert
        Assert.NotNull(googleForm);
        Assert.Equal("post", googleForm.GetAttribute("method"));
    }

    #endregion

    #region Logout Component UI Tests

    [Fact]
    public void Logout_ShouldRenderConfirmation()
    {
        // Act
        var cut = Render<Logout>();

        // Assert
        Assert.Contains("Are you sure you want to logout?", cut.Markup);
    }

    [Fact]
    public void Logout_ShouldHaveLogoutButton()
    {
        // Act
        var cut = Render<Logout>();
        var button = cut.Find("button[type='submit']");

        // Assert
        Assert.Equal("Logout", button.TextContent);
    }

    [Fact]
    public void Logout_ShouldHaveCancelLink()
    {
        // Act
        var cut = Render<Logout>();
        var cancelLink = cut.Find("a[href='/']");

        // Assert
        Assert.NotNull(cancelLink);
        Assert.Contains("Cancel", cancelLink.TextContent);
    }

    [Fact]
    public void Logout_Form_ShouldPostToCorrectEndpoint()
    {
        // Act
        var cut = Render<Logout>();
        var form = cut.Find("form[action='/Account/PerformLogout']");

        // Assert
        Assert.NotNull(form);
        Assert.Equal("post", form.GetAttribute("method"));
    }

    #endregion

    #region Accessibility Tests

    [Fact]
    public void Register_FormFields_ShouldHaveLabels()
    {
        // Act
        var cut = Render<Register>();

        // Assert
        Assert.NotNull(cut.Find("label[for='displayName']"));
        Assert.NotNull(cut.Find("label[for='email']"));
        Assert.NotNull(cut.Find("label[for='password']"));
        Assert.NotNull(cut.Find("label[for='confirmPassword']"));
    }

    [Fact]
    public void Login_FormFields_ShouldHaveLabels()
    {
        // Act
        var cut = Render<Login>();

        // Assert
        Assert.NotNull(cut.Find("label[for='email']"));
        Assert.NotNull(cut.Find("label[for='password']"));
        Assert.NotNull(cut.Find("label[for='rememberMe']"));
    }

    [Fact]
    public void Register_FormFields_ShouldHaveFormControlClass()
    {
        // Act
        var cut = Render<Register>();

        // Assert
        Assert.Contains("form-control", cut.Find("input#displayName").GetAttribute("class"));
        Assert.Contains("form-control", cut.Find("input#email").GetAttribute("class"));
        Assert.Contains("form-control", cut.Find("input#password").GetAttribute("class"));
        Assert.Contains("form-control", cut.Find("input#confirmPassword").GetAttribute("class"));
    }

    [Fact]
    public void Login_FormFields_ShouldHaveFormControlClass()
    {
        // Act
        var cut = Render<Login>();

        // Assert
        Assert.Contains("form-control", cut.Find("input#email").GetAttribute("class"));
        Assert.Contains("form-control", cut.Find("input#password").GetAttribute("class"));
    }

    #endregion
}
