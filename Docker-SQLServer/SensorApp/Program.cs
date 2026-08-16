using Microsoft.EntityFrameworkCore;
using SensorApp.Data;
using SensorApp.Services;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("SensorDb")
    ?? throw new InvalidOperationException("Connection string 'SensorDb' is not configured.");

builder.Services.AddDbContext<SensorDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddScoped<IReadingService, ReadingService>();
builder.Services.AddScoped<IDeviceService, DeviceService>();
builder.Services.AddScoped<IAuditService, AuditService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "SensorApp API", Version = "v1" });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<SensorDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DbInitializer");
    await DbInitializer.InitializeAsync(context, logger);
}

app.UseSwagger();
app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "SensorApp API v1"));
app.MapControllers();

app.Run();
