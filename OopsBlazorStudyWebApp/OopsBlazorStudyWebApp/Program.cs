using OopsBlazorStudyWebApp.Client.Pages;
using OopsBlazorStudyWebApp.Components;
using OopsBlazorStudyWebApp;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents()
    .Services.AddControllers();
builder.Services.AddScoped<IUserMasterRepository, SqlUserMasterRepository>();
builder.Services.AddScoped<IMenuMasterRepository, SqlMenuMasterRepository>();
builder.Services.AddScoped<IAuthRepository, SqlAuthRepository>();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/api/auth/logout";
        options.Cookie.Name = "SLNWarehouseAuth";
        options.SlidingExpiration = true;
    });
builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.Use(async (context, next) =>
{
    var path = context.Request.Path;
    var isLoginPath = path.StartsWithSegments("/login");
    var isAuthApi = path.StartsWithSegments("/api/auth");
    var isApi = path.StartsWithSegments("/api");
    var isFrameworkAsset = path.StartsWithSegments("/_framework") || path.StartsWithSegments("/_content");
    var isStaticFile = System.IO.Path.HasExtension(path.Value);
    var isPageGet = HttpMethods.IsGet(context.Request.Method) && !isApi && !isAuthApi && !isFrameworkAsset && !isStaticFile;

    if (isPageGet && !isLoginPath && context.User.Identity?.IsAuthenticated != true)
    {
        context.Response.Redirect("/login");
        return;
    }

    if (isPageGet && isLoginPath && context.User.Identity?.IsAuthenticated == true)
    {
        context.Response.Redirect("/");
        return;
    }

    await next();
});

app.UseAntiforgery();

app.MapStaticAssets();
app.MapControllers();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(OopsBlazorStudyWebApp.Client._Imports).Assembly);

app.Run();
