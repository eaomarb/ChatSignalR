using ChatSignalR.Hubs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddSignalR();

var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();

app.MapRazorPages();
app.MapHub<ChatHub>("/chatHub");

app.Run();