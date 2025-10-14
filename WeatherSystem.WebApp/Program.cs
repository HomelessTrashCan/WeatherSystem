using WeatherSystem.WebApp.Components;
using WeatherSystem.WebApp.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();



// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Register weather data services
builder.Services.AddSingleton<IWeatherDataCollectionService, WeatherDataCollectionService>();
builder.Services.AddHostedService<WeatherDataHostedService>();

var app = builder.Build();

app.MapDefaultEndpoints();



// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();


app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
