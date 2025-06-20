using Microsoft.AspNetCore.Authentication.Cookies;
using EasyTrade_Crypto.Interfaces;
using EasyTrade_Crypto.Services;
using EasyTrade_Crypto.Managers;
using Interfaces;
using MSSQL;
using EasyTrade_Crypto.DAL.MSSQL;
using EasyTrade_Crypto.MSSQL;
using DTO;

var builder = WebApplication.CreateBuilder(args);

// var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                      // ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.HttpOnly = true;
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30); 
        options.LoginPath = "/Login";
        options.AccessDeniedPath = "/AccessDenied";
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();

builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizePage("/TradingView");
    options.Conventions.AuthorizePage("/Portfolio");
    options.Conventions.AuthorizePage("/AdminDashboard");
});

// Register connection string provider
builder.Services.AddSingleton<IDbConnectionStringProvider, EasyTrade_Crypto.Utilities.ConfigurationConnectionStringProvider>();

// Register DAL services
builder.Services.AddScoped<IAccountDAL, AccountSQL>();
builder.Services.AddScoped<IAccountManagerSQL, AccountManagerSQL>();
builder.Services.AddScoped<IPortfolioDAL, MSSQL.PortfolioDAL>();
builder.Services.AddScoped<IPortfolioSQL, MSSQL.PortfolioSQL>();
builder.Services.AddScoped<IAdminCryptoDAL, AdminCryptoSQL>();
builder.Services.AddScoped<ITradeDAL, MSSQL.TradeDAL>();

// Register business services
builder.Services.AddScoped<IAccountManager, AccountManager>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IRegistrationService, RegistrationService>();
builder.Services.AddScoped<IPortfolioService, PortfolioService>();
builder.Services.AddScoped<IReadOnlyCryptoService, AdminCryptoService>();
builder.Services.AddScoped<IManageCryptoService, AdminCryptoService>();
builder.Services.AddScoped<ITradeService, TradeService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.Run();