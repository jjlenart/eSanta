using eSanta.Common;
using eSanta.Components;
using eSanta.Components.Account;
using eSanta.Components.Shared;
using eSanta.Data;
using eSanta.Features.Account;
using eSanta.Features.Administration;
using eSanta.Features.Attendance;
using eSanta.Features.FamilyWishlists;
using eSanta.Features.Registration;
using eSanta.Features.ShoppingList;
using eSanta.Features.Shoutbox;
using eSanta.Features.Wishlist;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();
builder.Services.AddHostedService<DatabaseMigrationService>();

builder.Services.AddMudServices();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddScoped<IFormDialogService, FormDialogService>();
builder.Services.AddScoped<IOperationFeedback, SnackbarOperationFeedback>();

builder.Services.AddSingleton<IModelValidator, DataAnnotationsModelValidator>();
builder.Services.AddScoped<IAttendeeService, AttendeeService>();
builder.Services.AddScoped<IRegistrationService, RegistrationService>();
builder.Services.AddScoped<IAliasService, AliasService>();
builder.Services.AddScoped<IWishlistService, WishlistService>();
builder.Services.AddScoped<IFamilyWishlistService, FamilyWishlistService>();
builder.Services.AddScoped<IShoppingListService, ShoppingListService>();
builder.Services.AddScoped<IUserAdministrationService, UserAdministrationService>();
builder.Services.AddScoped<IShoutboxService, ShoutboxService>();
builder.Services.AddSingleton<IShoutboxNotifier, ShoutboxNotifier>();
builder.Services.AddSingleton<IMentionParser, MentionParser>();
builder.Services.AddSingleton<IMentionSuggester, MentionSuggester>();
builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddScoped<IAuthorizationHandler, AdminAuthorizationHandler>();
builder.Services.AddAuthorizationBuilder()
    .AddPolicy(AdminRequirement.PolicyName, policy => policy
        .RequireAuthenticatedUser()
        .AddRequirements(new AdminRequirement()));

builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        // Users register with a first name instead of an email, so there is nothing to confirm.
        options.SignIn.RequireConfirmedAccount = false;
        // First names may contain Polish letters and spaces.
        options.User.AllowedUserNameCharacters = string.Empty;
        options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddErrorDescriber<PolishIdentityErrorDescriber>()
    .AddClaimsPrincipalFactory<AliasClaimsPrincipalFactory>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

app.Run();
