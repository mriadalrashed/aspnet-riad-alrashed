using GymPortal.Infrastructure.Data;
using GymPortal.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddHttpContextAccessor();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddInfrastructureIdentity();
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/SignIn";
    options.LogoutPath = "/Account/SignOut";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromDays(6);
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy= CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
});
builder.Services.AddAuthentication() 
.AddGoogle(options =>
{
    options.ClientId = builder.Configuration["Authentication:Google:ClientId"] ?? throw new InvalidOperationException("Google Client ID is not configured.");
    options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? throw new InvalidOperationException("Google Client Secret is not configured.");
    options.CallbackPath = "/signin-google";
    options.SaveTokens = true;
    options.Scope.Add("profile");
    options.Scope.Add("email");
});


var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    await SeedData.InitializeAsync(scope.ServiceProvider);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseStatusCodePagesWithReExecute("/Home/Error404");
app.UseAuthentication();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
