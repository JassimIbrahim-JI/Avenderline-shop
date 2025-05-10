using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using LavenderLine.Exceptions;
using LavenderLine.Settings;
using LavenderLine.Storage;
using LavenderLine.VerificationServices;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Npgsql;
using System.Reflection;
using System.Security.Claims;
using System.Text.Json;


var builder = WebApplication.CreateBuilder(args);


builder.Configuration.AddUserSecrets<Program>();



builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.Name = ".avenderline-shop.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddDistributedMemoryCache();
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase);
builder.Services.AddRazorPages();

var connectionString = string.Empty;

// Get Heroku's DATABASE_URL environment variable
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");

if (!string.IsNullOrEmpty(databaseUrl))
{
    // Parse Heroku PostgreSQL URL
    var databaseUri = new Uri(databaseUrl);
    var userInfo = databaseUri.UserInfo.Split(':');

    // Build Npgsql connection string
    var npgsqlBuilder = new NpgsqlConnectionStringBuilder
    {
        Host = databaseUri.Host,
        Port = databaseUri.Port,
        Username = userInfo[0],
        ApplicationName = "avenderline-shop",
        IncludeErrorDetail = true,
        Password = Uri.UnescapeDataString(userInfo[1]),
        Database = databaseUri.AbsolutePath.TrimStart('/'),
        SslMode = SslMode.Require,
        Pooling = true,
    };


    connectionString = npgsqlBuilder.ToString();
}
else
{
    connectionString = builder.Configuration.GetConnectionString("EcommerceConnection");
}

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
builder.Services.AddDbContext<EcommerceContext>(options =>
    options.UseNpgsql(connectionString, o =>
    {
        o.UseNodaTime();
    })
);

builder.Services.AddAutoMapper(Assembly.GetExecutingAssembly());


// Critical Security Fix: Token Provider Configuration 
builder.Services.Configure<DataProtectionTokenProviderOptions>(options =>
{
    options.TokenLifespan = TimeSpan.FromDays(7); 
});


builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.Configure<PhoneSettings>(builder.Configuration.GetSection("PhoneSettings"));
builder.Services.Configure<MyFatoorahSettings>(builder.Configuration.GetSection("MyFatoorah"));

builder.Services.AddScoped<IEmailSenderService, EmailSenderService>();
builder.Services.AddTransient<IPhoneVerificationService, PhoneVerificationService>();


//Add Identity services

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 8;

    options.Lockout.AllowedForNewUsers = true;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.SignIn.RequireConfirmedEmail = true;
    options.User.RequireUniqueEmail = true;

})
    .AddEntityFrameworkStores<EcommerceContext>()
    .AddDefaultTokenProviders();


//specific authorization policies. 
// restrict access to certain actions or controllers based on roles

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdminRole", policy => policy.RequireRole(Roles.Admin));
    options.AddPolicy("RequireManagerRole", policy => policy.RequireRole(Roles.Manager));
    options.AddPolicy("RequireManagerOrAdminRole", policy =>
           policy.RequireRole("Manager", "Admin"));
});


// Register the repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<INotificationService>(provider =>
    new NotificationService(
        provider.GetRequiredService<IUserService>(),
        provider.GetRequiredService<IEmailSenderService>(),
        provider.GetRequiredService<EcommerceContext>(),
        provider.GetRequiredService<IWebHostEnvironment>(),
        provider.GetRequiredService<IConfiguration>(),
        provider.GetRequiredService<IHttpContextAccessor>()
    ));


builder.Services.AddScoped<INewsLetterService, NewsLetterService>();

// Register the services

builder.Services.AddScoped<DatabaseSeeder>();
builder.Services.AddScoped<IOrderItemService, OrderItemService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddHttpClient<IPaymentGatewayService, MyFatoorahService>();
builder.Services.AddTransient<PaymentGatewayException>();
builder.Services.AddScoped<ICartService, CartService>();

builder.Services.AddScoped<ICarouselImageService, CarouselImageService>();
builder.Services.AddScoped<IInstagramPostService, InstagramPostService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IPromotionService, PromotionService>();
builder.Services.AddScoped<IWishlistService, WishlistService>();
builder.Services.AddScoped<AnalyticsService>(provider =>
    new AnalyticsService(
        provider.GetRequiredService<EcommerceContext>(),
        provider.GetRequiredService<IOrderService>(),
        provider.GetRequiredService<IPaymentService>(),
        provider.GetRequiredService<IUserService>()
    )
);

builder.Services.AddHttpContextAccessor();

builder.Services.AddAuthentication(options =>
{  
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.AccessDeniedPath = "/Home/AccessDenied";
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;

    options.Events = new CookieAuthenticationEvents
    {
        OnRedirectToAccessDenied = context =>
        {
            if (context.Request.Path.StartsWithSegments("/admin"))
            {
                context.Response.Redirect("/Admin/Login");
            }
            else
            {
                // Default behavior for non-admin paths
                context.Response.Redirect($"/Account/Login?returnUrl={Uri.EscapeDataString(context.Request.Path)}");
            }
            return Task.CompletedTask;
        },

        OnRedirectToLogin = context =>
        {
            // Preserve the return URL when redirecting to login
            context.Response.Redirect($"/Account/Login?returnUrl={Uri.EscapeDataString(context.Request.Path)}");
            return Task.CompletedTask;
        }
    };

})
.AddGoogle(options =>
{
    options.ClientId = builder.Configuration["Authentication:Google:ClientId"]!;
    options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]!;
    options.CallbackPath = "/signin-google";
    options.SaveTokens = true;
    options.Scope.Add("email");
    options.Scope.Add("profile");
    options.ClaimActions.MapJsonKey(ClaimTypes.GivenName, "given_name");
    options.ClaimActions.MapJsonKey(ClaimTypes.Surname, "family_name");
    options.ClaimActions.MapJsonKey(ClaimTypes.Name, "name");
    options.ClaimActions.MapJsonKey(ClaimTypes.Email, "email");

    // handling for failed Google authentication
    options.Events = new OAuthEvents
    {
        OnRemoteFailure = context =>
        {
            context.Response.Redirect("/Account/Login?error=GoogleAuthFailed");
            context.HandleResponse();
            return Task.CompletedTask;
        }
    };

});

builder.Services.AddSingleton<IImageStorageFactory, ImageStorageFactory>();

builder.Services.Configure<GoogleCloudStorageSettings>(
    builder.Configuration.GetSection("GoogleCloudStorage"));

if (builder.Environment.IsDevelopment())
{
    // Development: Use User Secrets
    var credentialJson = builder.Configuration["GoogleCloudStorage:CredentialJson"];
    builder.Services.AddSingleton(StorageClient.Create(
        GoogleCredential.FromJson(credentialJson)
    ));
}
else
{
    // Production: Use Environment Variable
    var credentialJson = Environment.GetEnvironmentVariable("GCP_CREDENTIALS_JSON");
    builder.Services.AddSingleton(StorageClient.Create(
        GoogleCredential.FromJson(credentialJson)
    ));
}

var app = builder.Build();

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost
});


if (args.Contains("--migrate"))
{
    using var scope = app.Services.CreateScope();
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<EcommerceContext>();
        db.Database.Migrate();
        Console.WriteLine("Migrations applied successfully");


        // Seed data in production
        var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
        await seeder.SeedAsync();
        Console.WriteLine("Database seeded successfully");

        return; // Exit application after migrations
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Migration failed: {ex}");
        Environment.Exit(1); // Ensure failure is reported to Heroku
    }
}

if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        await scope.ServiceProvider.GetRequiredService<DatabaseSeeder>().SeedAsync();
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Call the connection check
CheckDatabaseConnection(app.Services);

app.UseHttpsRedirection();


app.UseStaticFiles(); // for wwwroot
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(Directory.GetCurrentDirectory(), "Secret")),
    RequestPath = "/secret",
    // Block access to the folder
    OnPrepareResponse = ctx => ctx.Context.Response.StatusCode = 404
}); // for secret

app.Use((context, next) =>
{
    if (context.Request.Headers["X-Forwarded-Proto"] == "https")
    {
        context.Request.Scheme = "https";
    }
    return next();
});

app.UseRouting();

var supportedCultures = new[] { "en", "ar" };
app.UseRequestLocalization(options => {
    options.DefaultRequestCulture = new RequestCulture("en");
    options.AddSupportedCultures(supportedCultures);
    options.AddSupportedUICultures(supportedCultures);
});


app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "admin",
    pattern: "admin/{controller}/{action}/{id?}");

app.MapRazorPages();


var port = Environment.GetEnvironmentVariable("PORT");
if (string.IsNullOrEmpty(port))
{
    // Local dev 
    app.Run(); 
}
else
{
    // Heroku environment
    app.Run($"http://0.0.0.0:{port}");
}

// Method to check the database connection
void CheckDatabaseConnection(IServiceProvider serviceProvider)
{
    using var scope = serviceProvider.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<EcommerceContext>();

    try
    {
        dbContext.Database.CanConnect();
        Console.WriteLine("Database connection is valid.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Database connection failed: {ex.Message}");
    }
}
