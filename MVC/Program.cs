using Npgsql;
using Repositories.Implementations;
using Repositories.Interfaces;
using MVC.Filters;

using TaskTrackPro.MVC.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddSession(options=>
{
    options.IdleTimeout=TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddScoped<AuthFilter>();

builder.Services.AddSingleton<ITaskInterface, TaskRepository>();
builder.Services.AddSingleton<IUserInterface, UserRepository>();

builder.Services.AddSingleton<NpgsqlConnection>((UserRepository) => {
    var connectionString = UserRepository.GetRequiredService<IConfiguration>().GetConnectionString("pgconn");
    return new NpgsqlConnection(connectionString);
});

builder.Services.AddSignalR();


var app = builder.Build();

// In your app configuration section:
app.MapHub<ChatHub>("/chatHub");

app.UseSession();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
