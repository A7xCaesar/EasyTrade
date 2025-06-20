using Microsoft.AspNetCore.Authentication.Cookies;
using EasyTrade_Crypto.Interfaces;
using EasyTrade_Crypto.Services;
using EasyTrade_Crypto.DAL.MSSQL;
using EasyTrade_Crypto.Managers;
using Interfaces;
using MSSQL;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                       ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddAuthentication("Cookies")
    .AddCookie("Cookies", options =>
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
    options.Conventions.AuthorizePage("/Trade");
    options.Conventions.AuthorizePage("/Portfolio");
    options.Conventions.AuthorizePage("/AdminDashboard");
});

// Register connection string provider
builder.Services.AddSingleton<IDbConnectionStringProvider, EasyTrade_Crypto.Utilities.ConfigurationConnectionStringProvider>();

// Register services
builder.Services.AddScoped<IAccountManager, AccountManager>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IPortfolioService, PortfolioService>();
builder.Services.AddScoped<IPortfolioDAL, MSSQL.PortfolioDAL>();

// Register admin services
builder.Services.AddScoped<IAdminCryptoDAL, AdminCryptoSQL>();
builder.Services.AddScoped<IReadOnlyCryptoService, AdminCryptoService>();
builder.Services.AddScoped<IManageCryptoService, AdminCryptoService>();

// Trade services
builder.Services.AddScoped<ITradeDAL, MSSQL.TradeDAL>();
builder.Services.AddScoped<ITradeService, TradeService>();

// Register RegistrationService
builder.Services.AddScoped<IRegistrationService, RegistrationService>();

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