using DirtyOlives.Client.Pages;
using DirtyOlives.Client.Services;
using DirtyOlives.Components;
using DirtyOlives.Data;
using DirtyOlives.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddControllers();

// Postgres (Neon/Render) when a Postgres connection string is configured, otherwise a local SQLite file.
var connectionString = builder.Configuration.GetConnectionString("MartiniDb")
                       ?? "Data Source=martinis.db";

builder.Services.AddDbContext<MartiniDbContext>(options =>
{
    if (DatabaseConnection.IsPostgres(connectionString))
    {
        options.UseNpgsql(DatabaseConnection.Normalize(connectionString),
            npgsql => npgsql.EnableRetryOnFailure());
    }
    else
    {
        options.UseSqlite(connectionString);
    }
});

builder.Services.AddScoped<MartiniRatingService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddSingleton<DatabaseStartupReport>();

// MainLayout is prerendered on the server, so its injected client services must
// also resolve here. They are only actually used once the component is interactive.
builder.Services.AddScoped(_ => new HttpClient());
builder.Services.AddScoped<DatabaseHealthService>();
builder.Services.AddScoped<UserSessionService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MartiniDbContext>();
    var report = app.Services.GetRequiredService<DatabaseStartupReport>();

    var provider = db.Database.ProviderName ?? "unknown";
    var dataSource = db.Database.GetDbConnection().DataSource ?? "unknown";

    try
    {
        db.Database.EnsureCreated();

        // EnsureCreated leaves existing databases untouched, so patch in newer optional columns.
        DatabaseSchemaUpdater.EnsureOptionalColumns(db);

        // Give the owner of any pre-existing ratings a name to sign in with.
        scope.ServiceProvider.GetRequiredService<UserService>()
            .EnsureDefaultUserAsync("Nick")
            .GetAwaiter()
            .GetResult();

        report.RecordSuccess(provider, dataSource);
        app.Logger.LogInformation("Database ready ({Provider} @ {DataSource}).", provider, dataSource);
    }
    catch (Exception ex)
    {
        // Stay up so the UI can report the failure instead of the host just dying.
        report.RecordFailure(provider, dataSource, ex);
        app.Logger.LogError(ex.GetBaseException(),
            "Database unavailable at startup ({Provider} @ {DataSource}).", provider, dataSource);
    }
}

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
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapControllers();
app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(DirtyOlives.Client._Imports).Assembly);

app.Run();
