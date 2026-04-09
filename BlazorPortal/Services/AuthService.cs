using BlazorPortal.Data;
using Microsoft.AspNetCore.Identity;

namespace BlazorPortal.Services;

public class AuthService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly OrderApiService _apiService;

    public AppUser? CurrentUser { get; private set; }
    public bool IsLoggedIn => CurrentUser != null;
    public bool IsAdmin => CurrentUser?.Role == "Admin";

    public event Action? OnChange;

    public AuthService(
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager,
        OrderApiService apiService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _apiService = apiService;
    }

    public async Task<bool> RegisterAsync(
        string fullName, string email, string password)
    {
        var user = new AppUser
        {
            UserName = email,
            Email = email,
            FullName = fullName,
            Role = "Customer"
        };

        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded) return false;

        // Create customer in Order API
        var customer = await _apiService.CreateCustomerAsync(fullName, email);
        if (customer != null)
        {
            user.CustomerId = customer.Id;
            await _userManager.UpdateAsync(user);
        }

        CurrentUser = user;
        OnChange?.Invoke();
        return true;
    }

    public async Task<bool> LoginAsync(string email, string password)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null) return false;

        var result = await _signInManager
            .CheckPasswordSignInAsync(user, password, false);
        if (!result.Succeeded) return false;

        CurrentUser = user;
        OnChange?.Invoke();
        return true;
    }

    public void Logout()
    {
        CurrentUser = null;
        OnChange?.Invoke();
    }
}