using DirtyOlives.Client.Pages;
using DirtyOlives.Components;
using DirtyOlives.Data;
using DirtyOlives.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddControllers();

builder.Services.AddDbContext<MartiniDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("MartiniDb")
                      ?? "Data Source=martinis.db"));

builder.Services.AddScoped<MartiniRatingService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MartiniDbContext>();
    db.Database.EnsureCreated();

    // EnsureCreated leaves existing databases untouched, so patch in newer optional columns.
    SqliteSchemaUpdater.EnsureOptionalColumns(db);
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
