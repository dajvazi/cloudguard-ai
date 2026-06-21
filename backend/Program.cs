using CloudGuard.Api.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddDbContext<CloudGuardDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("CloudGuard")));
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CloudGuardDbContext>();
    db.Database.Migrate();
}

app.UseCors("Frontend");
app.UseAuthorization();
app.MapControllers();

app.Run();
