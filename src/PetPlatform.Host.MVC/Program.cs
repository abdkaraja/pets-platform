using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PetPlatform.Infrastructure.Identity;
using PetPlatform.Infrastructure.Persistence;
using PetPlatform.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Identity + EF Core ──────────────────────────────────────────────────
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
{
    // Email verification required
    options.SignIn.RequireConfirmedAccount = true;

    // Account lockout after 5 failed attempts (AUTH-05)
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);

    // Password policy
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders()
.AddDefaultUI();

// ── Authorization policies ──────────────────────────────────────────────
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("Permission:Users.View", policy => policy.RequireClaim("Permission", "Users.View"))
    .AddPolicy("Permission:Users.Manage", policy => policy.RequireClaim("Permission", "Users.Manage"))
    .AddPolicy("Permission:Roles.Create", policy => policy.RequireClaim("Permission", "Roles.Create"))
    .AddPolicy("Permission:Roles.Assign", policy => policy.RequireClaim("Permission", "Roles.Assign"))
    .AddPolicy("Permission:Pets.Manage", policy => policy.RequireClaim("Permission", "Pets.Manage"));

// ── Email sender (console-log stub for Phase 1) ────────────────────────
builder.Services.AddScoped(typeof(IEmailSender<>), typeof(EmailSender<>));

// ── MVC ─────────────────────────────────────────────────────────────────
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

var app = builder.Build();

// ── Seed roles + admin user on startup ──────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    await SeedData.InitializeAsync(scope.ServiceProvider);
}

// ── Middleware (canonical order — Pitfall #1) ───────────────────────────
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// ── Endpoints ───────────────────────────────────────────────────────────
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();
