using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using AppSec_Assignment_2.Data;
using AppSec_Assignment_2.Middleware;
using AppSec_Assignment_2.Models;
using AppSec_Assignment_2.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

// Configure Entity Framework with SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure Data Protection for NRIC encryption
builder.Services.AddDataProtection();

// Register services
builder.Services.AddScoped<MemberProtectionService>();
builder.Services.AddScoped<AuditService>();
builder.Services.AddScoped<IPasswordHasher<Member>, PasswordHasher<Member>>();
builder.Services.AddScoped<TwoFactorService>();
builder.Services.AddScoped<EmailService>();

// Configure Google reCAPTCHA
builder.Services.Configure<GoogleReCaptchaOptions>(
    builder.Configuration.GetSection(GoogleReCaptchaOptions.SectionName));
builder.Services.AddHttpClient<ReCaptchaService>();

// Configure Email service
builder.Services.Configure<EmailOptions>(
    builder.Configuration.GetSection(EmailOptions.SectionName));

// Configure cookie authentication with session timeout
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Errors/Forbidden";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        
    // Fix SameSite cookie warnings for localhost
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() 
    ? CookieSecurePolicy.SameAsRequest  // Allow both HTTP and HTTPS in development
   : CookieSecurePolicy.Always;        // Force HTTPS in production
    
     options.Cookie.SameSite = SameSiteMode.Lax;  // Changed from Strict to Lax for better compatibility
    });

// Add antiforgery for CSRF protection
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    
    // Fix SameSite cookie warnings for localhost
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
   ? CookieSecurePolicy.SameAsRequest  // Allow both HTTP and HTTPS in development
        : CookieSecurePolicy.Always;     // Force HTTPS in production
    
    options.Cookie.SameSite = SameSiteMode.Lax;  // Changed from Strict to Lax
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

// Add security headers middleware
app.Use(async (context, next) =>
{
  // Content Security Policy to mitigate XSS attacks
 context.Response.Headers.Append("Content-Security-Policy", 
        "default-src 'self'; " +
   "script-src 'self' 'unsafe-inline' 'unsafe-eval' https://www.google.com https://www.gstatic.com; " +
        "style-src 'self' 'unsafe-inline'; " +
    "img-src 'self' data:; " +
        "font-src 'self'; " +
      "frame-src https://www.google.com; " +
        "connect-src 'self';");
    
    // Prevent clickjacking
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    
  // Prevent MIME type sniffing
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    
    // Enable XSS filter
    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
    
    // Referrer policy
 context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    
    await next();
});

// Custom error pages - route status codes to /Errors/StatusCode/{code}
app.UseStatusCodePagesWithReExecute("/Errors/StatusCode/{0}");

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Session validation middleware for single login enforcement
app.UseSessionValidation();

app.MapRazorPages();

// Note: Database migrations are already up to date
// Run "dotnet ef database update" manually if you add new migrations

app.Run();
