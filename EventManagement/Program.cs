using EventManagement.Data;
using Microsoft.EntityFrameworkCore;
using NLog;
using NLog.Web;
using Microsoft.Extensions.Logging;

try
{
    Logger logger = LogManager.Setup().LoadConfigurationFromFile("nlog.config").GetCurrentClassLogger();

    var builder = WebApplication.CreateBuilder(args);

    // Configure NLog
    builder.Logging.ClearProviders();
    builder.Logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
    builder.Host.UseNLog();  // NLog: Setup NLog for Dependency injection

    // Configure database context with connection string
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

    // Add controllers
    builder.Services.AddControllers();

    // Configure Swagger/OpenAPI
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    // Build the application
    var app = builder.Build();

    // Configure the HTTP request pipeline
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    // Enable HTTPS redirection
    app.UseHttpsRedirection(); // Enable HTTPS redirection in both development and production

    app.UseAuthorization();
    app.MapControllers();

    // Run the application
    app.Run();
}
catch (Exception ex)
{
    var logger = LogManager.GetCurrentClassLogger();
    logger.Error(ex, "Application stopped because of an exception");
    throw;
}
