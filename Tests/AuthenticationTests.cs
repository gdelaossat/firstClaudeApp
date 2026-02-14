using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using firstClaudeApp.Data;
using Xunit;

namespace firstClaudeApp.Tests;

public class AuthenticationTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ApplicationDbContext _dbContext;

    public AuthenticationTests()
    {
        var services = new ServiceCollection();

        // Configure in-memory database
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));

        // Configure Identity
        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequiredLength = 6;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        // Add required services for SignInManager
        services.AddLogging();
        services.AddHttpContextAccessor();
        services.AddAuthentication();

        _serviceProvider = services.BuildServiceProvider();
        _userManager = _serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        _signInManager = _serviceProvider.GetRequiredService<SignInManager<ApplicationUser>>();
        _dbContext = _serviceProvider.GetRequiredService<ApplicationDbContext>();
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _serviceProvider.Dispose();
    }

    #region Registration Tests

    [Fact]
    public async Task Register_WithValidData_ShouldCreateUser()
    {
        // Arrange
        var email = "test@example.com";
        var password = "Test123!";
        var displayName = "Test User";

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            DisplayName = displayName
        };

        // Act
        var result = await _userManager.CreateAsync(user, password);

        // Assert
        Assert.True(result.Succeeded);
        var createdUser = await _userManager.FindByEmailAsync(email);
        Assert.NotNull(createdUser);
        Assert.Equal(email, createdUser.Email);
        Assert.Equal(displayName, createdUser.DisplayName);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ShouldFail()
    {
        // Arrange
        var email = "duplicate@example.com";
        var password = "Test123!";

        var user1 = new ApplicationUser { UserName = email, Email = email };
        var user2 = new ApplicationUser { UserName = email, Email = email };

        // Act
        await _userManager.CreateAsync(user1, password);
        var result = await _userManager.CreateAsync(user2, password);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Code == "DuplicateUserName");
    }

    [Theory]
    [InlineData("weak")] // Too short and missing requirements
    [InlineData("nouppercase1")] // Missing uppercase
    [InlineData("NOLOWERCASE1")] // Missing lowercase
    [InlineData("NoDigits")] // Missing digit
    public async Task Register_WithWeakPassword_ShouldFail(string weakPassword)
    {
        // Arrange
        var user = new ApplicationUser
        {
            UserName = "test@example.com",
            Email = "test@example.com"
        };

        // Act
        var result = await _userManager.CreateAsync(user, weakPassword);

        // Assert
        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task Register_ShouldSetCreatedAtToCurrentTime()
    {
        // Arrange
        var beforeCreation = DateTime.UtcNow.AddSeconds(-1);
        var user = new ApplicationUser
        {
            UserName = "timestamp@example.com",
            Email = "timestamp@example.com"
        };

        // Act
        await _userManager.CreateAsync(user, "Test123!");
        var afterCreation = DateTime.UtcNow.AddSeconds(1);

        // Assert
        var createdUser = await _userManager.FindByEmailAsync("timestamp@example.com");
        Assert.NotNull(createdUser);
        Assert.True(createdUser.CreatedAt >= beforeCreation);
        Assert.True(createdUser.CreatedAt <= afterCreation);
    }

    [Fact]
    public async Task Register_WithValidPassword_ShouldHashPassword()
    {
        // Arrange
        var email = "hash@example.com";
        var password = "Test123!";
        var user = new ApplicationUser { UserName = email, Email = email };

        // Act
        await _userManager.CreateAsync(user, password);

        // Assert
        var createdUser = await _userManager.FindByEmailAsync(email);
        Assert.NotNull(createdUser);
        Assert.NotNull(createdUser.PasswordHash);
        Assert.NotEqual(password, createdUser.PasswordHash);
    }

    #endregion

    #region Login Tests

    [Fact]
    public async Task Login_WithValidCredentials_ShouldSucceed()
    {
        // Arrange
        var email = "login@example.com";
        var password = "Test123!";
        var user = new ApplicationUser { UserName = email, Email = email };
        await _userManager.CreateAsync(user, password);

        // Act
        var isValidPassword = await _userManager.CheckPasswordAsync(user, password);

        // Assert
        Assert.True(isValidPassword);
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ShouldFail()
    {
        // Arrange
        var email = "loginwrong@example.com";
        var password = "Test123!";
        var wrongPassword = "WrongPass123!";
        var user = new ApplicationUser { UserName = email, Email = email };
        await _userManager.CreateAsync(user, password);

        // Act
        var isValidPassword = await _userManager.CheckPasswordAsync(user, wrongPassword);

        // Assert
        Assert.False(isValidPassword);
    }

    [Fact]
    public async Task Login_WithNonExistentUser_ShouldReturnNull()
    {
        // Arrange
        var email = "nonexistent@example.com";

        // Act
        var user = await _userManager.FindByEmailAsync(email);

        // Assert
        Assert.Null(user);
    }

    [Fact]
    public async Task Login_ShouldFindUserByEmail()
    {
        // Arrange
        var email = "findme@example.com";
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            DisplayName = "Find Me"
        };
        await _userManager.CreateAsync(user, "Test123!");

        // Act
        var foundUser = await _userManager.FindByEmailAsync(email);

        // Assert
        Assert.NotNull(foundUser);
        Assert.Equal("Find Me", foundUser.DisplayName);
    }

    [Fact]
    public async Task Login_ShouldFindUserByUserName()
    {
        // Arrange
        var email = "username@example.com";
        var user = new ApplicationUser { UserName = email, Email = email };
        await _userManager.CreateAsync(user, "Test123!");

        // Act
        var foundUser = await _userManager.FindByNameAsync(email);

        // Assert
        Assert.NotNull(foundUser);
        Assert.Equal(email, foundUser.UserName);
    }

    #endregion

    #region Password Validation Tests

    [Fact]
    public async Task PasswordValidator_MinimumLength6_ShouldEnforce()
    {
        // Arrange
        var user = new ApplicationUser { UserName = "short@example.com", Email = "short@example.com" };
        var shortPassword = "Te1!"; // Only 4 characters

        // Act
        var result = await _userManager.CreateAsync(user, shortPassword);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Code == "PasswordTooShort");
    }

    [Fact]
    public async Task PasswordValidator_RequiresDigit_ShouldEnforce()
    {
        // Arrange
        var user = new ApplicationUser { UserName = "nodigit@example.com", Email = "nodigit@example.com" };
        var noDigitPassword = "TestPassword!";

        // Act
        var result = await _userManager.CreateAsync(user, noDigitPassword);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Code == "PasswordRequiresDigit");
    }

    #endregion

    #region User Properties Tests

    [Fact]
    public async Task User_DisplayName_ShouldBePersisted()
    {
        // Arrange
        var displayName = "John Doe";
        var user = new ApplicationUser
        {
            UserName = "john@example.com",
            Email = "john@example.com",
            DisplayName = displayName
        };

        // Act
        await _userManager.CreateAsync(user, "Test123!");
        var retrievedUser = await _userManager.FindByEmailAsync("john@example.com");

        // Assert
        Assert.NotNull(retrievedUser);
        Assert.Equal(displayName, retrievedUser.DisplayName);
    }

    [Fact]
    public async Task User_EmailConfirmed_ShouldDefaultToFalse()
    {
        // Arrange
        var user = new ApplicationUser
        {
            UserName = "unconfirmed@example.com",
            Email = "unconfirmed@example.com"
        };

        // Act
        await _userManager.CreateAsync(user, "Test123!");
        var retrievedUser = await _userManager.FindByEmailAsync("unconfirmed@example.com");

        // Assert
        Assert.NotNull(retrievedUser);
        Assert.False(retrievedUser.EmailConfirmed);
    }

    #endregion
}
