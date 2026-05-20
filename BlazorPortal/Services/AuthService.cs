using BlazorPortal.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Serilog;

namespace BlazorPortal.Services;

public class AuthService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly OrderApiService _apiService;
    private readonly ProtectedSessionStorage _session;

    public AppUser? CurrentUser { get; private set; }
    public bool IsLoggedIn => CurrentUser != null;
    public bool IsAdmin => CurrentUser?.Role == "Admin";
    private static AppUser? _staticUser;

    public event Action? OnChange;

    public AuthService(
        UserManager<AppUser> userManager,
        OrderApiService apiService,
        ProtectedSessionStorage session)
    {
        _userManager = userManager;
        _apiService = apiService;
        _session = session;
    }

    //public async Task InitializeAsync()
    //{
    //    try
    //    {
    //        var result = await _session.GetAsync<string>("userId");
    //        if (result.Success && result.Value != null)
    //        {
    //            CurrentUser = await _userManager.FindByIdAsync(result.Value);
    //        }
    //    }
    //    catch { }
    //}

    //public async Task<bool> LoginAsync(string email, string password)
    //{
    //    var user = await _userManager.FindByEmailAsync(email);
    //    if (user == null)
    //    {
    //        Log.Warning("Login failed - user not found: {Email}", email);
    //        return false;
    //    }

    //    var isValid = await _userManager.CheckPasswordAsync(user, password);
    //    if (!isValid)
    //    {
    //        Log.Warning("Login failed - wrong password: {Email}", email);
    //        return false;
    //    }

    //    CurrentUser = user;

    //    // Save to session
    //    await _session.SetAsync("userId", user.Id);

    //    Log.Information("User logged in: {Email} Role: {Role}",
    //        email, user.Role);
    //    OnChange?.Invoke();
    //    return true;
    //}

    //public async Task<bool> RegisterAsync(
    //    string fullName, string email, string password)
    //{
    //    var user = new AppUser
    //    {
    //        UserName = email,
    //        Email = email,
    //        FullName = fullName,
    //        Role = "Customer"
    //    };

    //    var result = await _userManager.CreateAsync(user, password);
    //    if (!result.Succeeded) return false;

    //    var customer = await _apiService.CreateCustomerAsync(fullName, email);
    //    if (customer != null)
    //    {
    //        user.CustomerId = customer.Id;
    //        await _userManager.UpdateAsync(user);
    //    }

    //    CurrentUser = user;
    //    await _session.SetAsync("userId", user.Id);
    //    OnChange?.Invoke();
    //    return true;
    //}

    //public async Task LogoutAsync()
    //{
    //    CurrentUser = null;
    //    await _session.DeleteAsync("userId");
    //    OnChange?.Invoke();
    //}


    public async Task InitializeAsync()
    {
        // First try static backup
        if (_staticUser != null)
        {
            CurrentUser = _staticUser;
            return;
        }

        try
        {
            var result = await _session.GetAsync<string>("userId");
            if (result.Success && result.Value != null)
            {
                CurrentUser = await _userManager.FindByIdAsync(result.Value);
                _staticUser = CurrentUser;
            }
        }
        catch { }
    }

    public async Task<bool> LoginAsync(string email, string password)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null) return false;

        var isValid = await _userManager.CheckPasswordAsync(user, password);
        if (!isValid) return false;

        CurrentUser = user;
        _staticUser = user; // save static backup

        try { await _session.SetAsync("userId", user.Id); }
        catch { }

        Log.Information("User logged in: {Email} Role: {Role}", email, user.Role);
        OnChange?.Invoke();
        return true;
    }

    public async Task LogoutAsync()
    {
        CurrentUser = null;
        _staticUser = null;
        try { await _session.DeleteAsync("userId"); }
        catch { }
        OnChange?.Invoke();
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

        var customer = await _apiService.CreateCustomerAsync(fullName, email);
        if (customer != null)
        {
            user.CustomerId = customer.Id;
            await _userManager.UpdateAsync(user);
        }

        CurrentUser = user;
        _staticUser = user;
        try { await _session.SetAsync("userId", user.Id); }
        catch { }
        OnChange?.Invoke();
        return true;
    }
}