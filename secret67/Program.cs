using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using secret67.Components;
using secret67.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddDbContextFactory<AppDbContext>(o =>
    o.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.LoginPath = "/login";
        o.ExpireTimeSpan = TimeSpan.FromDays(7);
        o.SlidingExpiration = true;
        // В Development есть профиль http, там Secure-cookie не отправится
        o.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
    });

// Ограничение на отправку форм входа и регистрации: 10 попыток в минуту с одного IP
builder.Services.AddRateLimiter(o =>
{
    o.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
    {
        var path = ctx.Request.Path;
        var isAuthPost = HttpMethods.IsPost(ctx.Request.Method) &&
                         (path.Equals("/login", StringComparison.OrdinalIgnoreCase) ||
                          path.Equals("/register", StringComparison.OrdinalIgnoreCase));
        if (!isAuthPost)
            return RateLimitPartition.GetNoLimiter("");

        return RateLimitPartition.GetFixedWindowLimiter(ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1) });
    });
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    // С телом ответа UseStatusCodePagesWithReExecute не подменит 429 на страницу "Not Found"
    o.OnRejected = (ctx, ct) => new ValueTask(
        ctx.HttpContext.Response.WriteAsync("Слишком много попыток, подождите минуту", ct));
});
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapPost("/logout", async (HttpContext ctx, IAntiforgery antiforgery) =>
{
    // Форму endpoint не читает, поэтому токен надо проверить явно, иначе выход уязвим к CSRF
    if (!await antiforgery.IsRequestValidAsync(ctx))
        return Results.BadRequest();

    await ctx.SignOutAsync();
    return Results.Redirect("/");
});

app.Run();