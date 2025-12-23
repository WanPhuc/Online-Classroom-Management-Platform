using System.Net;
using System.Text.Json.Serialization;
using BTL_QuanLyLopHocTrucTuyen.Authorizations;
using BTL_QuanLyLopHocTrucTuyen.Data;
using BTL_QuanLyLopHocTrucTuyen.Helpers;
using BTL_QuanLyLopHocTrucTuyen.Middlewares;
using BTL_QuanLyLopHocTrucTuyen.Repositories;
using BTL_QuanLyLopHocTrucTuyen.Services;
using BTL_QuanLyLopHocTrucTuyen.Repositories.MySql;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Caching.Memory;
using BTL_QuanLyLopHocTrucTuyen.Repositories.SqlServer;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });


builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "AuthCookie";
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Events = new CookieAuthenticationEvents
        {
            OnRedirectToLogin = async ctx =>
            {
                if (ctx.Request.Path.StartsWithSegments("/api"))
                {
                    ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return;
                }

                ctx.HttpContext.Response.Redirect("/Home/Login");
            },
            OnRedirectToAccessDenied = async ctx =>
            {
                if (ctx.Request.Path.StartsWithSegments("/api"))
                {
                    ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return;
                }
                var cache = ctx.HttpContext.RequestServices.GetRequiredService<IMemoryCache>();
                var userId = ctx.HttpContext.User.GetUserId();
                cache.Remove(userId);

                await ctx.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                ctx.HttpContext.Response.Redirect("/Home/Login");
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.DefaultPolicy = new AuthorizationPolicyBuilder(CookieAuthenticationDefaults.AuthenticationScheme)
        .RequireAuthenticatedUser()
        .Build();
});


builder.Services.AddSingleton<IAuthorizationHandler, UserPermissionAuthorizationHandler>();

// builder.Services.AddDbContext<ApplicationDbContext, MySqlDbContext>(options =>
// {
//     var connectionString = builder.Configuration.GetConnectionString("MySqlConnection");
//     options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
// });

builder.Services.AddDbContext<ApplicationDbContext, SqlServerDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("SqlServerConnection")));

// builder.Services.AddScoped<ICourseRepository, MySqlCourseRepository>();
// builder.Services.AddScoped<IUserRepository, MySqlUserRepository>();
// builder.Services.AddScoped<ITenantRepository, MySqlTenantRepository>();
// builder.Services.AddScoped<IRoleRepository, MySqlRoleRepository>();
// builder.Services.AddScoped<IEnrollmentRepository, MySqlEnrollmentRepository>();
// builder.Services.AddScoped<ILessonRepository, MySqlLessonRepository>();
// builder.Services.AddScoped<IAssignmentRepository, MySqlAssignmentRepository>();
// builder.Services.AddScoped<IMaterialRepository, MySqlMaterialRepository>();
// builder.Services.AddScoped<ISubmissionRepository, MySqlSubmissionRepository>();

builder.Services.AddScoped<ICourseRepository, SqlServerCourseRepository>();
builder.Services.AddScoped<IUserRepository, SqlServerUserRepository>();
builder.Services.AddScoped<ITenantRepository, SqlServerTenantRepository>();
builder.Services.AddScoped<IRoleRepository, SqlServerRoleRepository>();
builder.Services.AddScoped<IEnrollmentRepository, SqlServerEnrollmentRepository>();
builder.Services.AddScoped<ILessonRepository, SqlServerLessonRepository>();
builder.Services.AddScoped<IAssignmentRepository, SqlServerAssignmentRepository>();
builder.Services.AddScoped<IMaterialRepository, SqlServerMaterialRepository>();
builder.Services.AddScoped<ISubmissionRepository, SqlServerSubmissionRepository>();

builder.Services.AddMemoryCache();
builder.Services.AddSingleton<SupabaseStorageService>();
builder.Services.AddSingleton<IFileUploadService, FileUploadService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    // Seed chỉ chạy khi DEV hoặc bạn bật RUN_SEED=true trên Render
    var runSeed = app.Environment.IsDevelopment() ||
                 string.Equals(Environment.GetEnvironmentVariable("RUN_SEED"), "true",
                               StringComparison.OrdinalIgnoreCase);

    if (runSeed)
    {
        try
        {
            SeedData.Initialize(services);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Seed failed: " + ex);
        }
    }
}


// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.MapStaticAssets();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<SingleSessionMiddleware>();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
// Add Controllers
builder.Services.AddControllers()
    .AddJsonOptions(options => {
        options.JsonSerializerOptions.PropertyNamingPolicy =
            System.Text.Json.JsonNamingPolicy.CamelCase;
    });

// Add Antiforgery
builder.Services.AddAntiforgery(options => {
    options.HeaderName = "RequestVerificationToken";
});

// Map API routes (sau app.MapControllerRoute)
app.MapControllers();