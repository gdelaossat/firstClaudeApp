using firstClaudeApp.Components;
using firstClaudeApp.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Substack Chat Summarizer services
builder.Services.AddSingleton<SettingsService>();
builder.Services.AddSingleton<SummaryStore>();
builder.Services.AddHttpClient<SubstackChatService>();
builder.Services.AddHttpClient<ClaudeSummaryService>();
builder.Services.AddHostedService<ChatSummaryBackgroundService>();

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

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
