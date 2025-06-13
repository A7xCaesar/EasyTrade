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



var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                       ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.HttpOnly = true;
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30); 
        options.LoginPath = "/Login";
        options.AccessDeniedPath = "/AccessDenied";
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
});


// Add services to the container.
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizePage("/Dashboard");
    options.Conventions.AuthorizePage("/Portfolio");
    options.Conventions.AuthorizePage("/Wallet");
    options.Conventions.AuthorizePage("/Watchlist");
    options.Conventions.AuthorizePage("/AdminDashboard", "Admin");
});

// Register interfaces and implementations
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IAccountDAL, AccountSQL>();
builder.Services.AddScoped<IAccountManagerSQL, AccountManagerSQL>();
builder.Services.AddScoped<IAccountManager, AccountManager>();
builder.Services.AddScoped<IRegistrationService, RegistrationService>();

// Register portfolio services
builder.Services.AddScoped<IPortfolioService, PortfolioService>();
builder.Services.AddScoped<IPortfolioDAL, PortfolioDAL>();

// Register admin services
builder.Services.AddScoped<IReadOnlyCryptoService, AdminCryptoService>();
builder.Services.AddScoped<IManageCryptoService, AdminCryptoService>();
builder.Services.AddScoped<IAdminCryptoDAL, AdminCryptoSQL>();

// Register trade services
builder.Services.AddScoped<ITradeDAL, MSSQL.TradeDAL>();
builder.Services.AddScoped<ITradeService, TradeService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication(); 
app.UseAuthorization();

app.MapRazorPages();

app.Run();