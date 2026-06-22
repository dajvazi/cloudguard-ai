using CloudGuard.Api.Data;
using CloudGuard.Api.Extensions;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "CloudGuard API", Version = "v1" });
});
builder.Services.AddDbContext<CloudGuardDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("CloudGuard")));
builder.Services.AddApplicationServices();
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

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "CloudGuard API v1");
        options.RoutePrefix = "swagger";
    });
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CloudGuardDbContext>();
    db.Database.Migrate();
}

app.UseCors("Frontend");
app.UseAuthorization();
app.MapControllers();

app.Run();
