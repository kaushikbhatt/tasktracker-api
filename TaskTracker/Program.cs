using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using TaskTracker.Application.Interfaces;
using TaskTracker.Application.Services;
using TaskTracker.Application.Validation;
using TaskTracker.Filters;
using TaskTracker.Infrastructure.Persistence;
using TaskTracker.Infrastructure.Persistence.Repositories;
using TaskTracker.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers(options =>
{
	// Global model validation filter that throws AppValidationException for consistent error shape
	options.Filters.Add<ModelValidationFilter>();
});

// OpenAPI / Swagger UI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
	c.SwaggerDoc("v1", new OpenApiInfo
	{
		Title = "TaskTracker API",
		Version = "v1",
		Description = "Internal TaskItem tracking API"
	});
});

// Dev-only CORS for React dev server
builder.Services.AddCors(options =>
{
	options.AddPolicy("DevCors", policy =>
		policy.WithOrigins("http://localhost:5173", "https://localhost:5173")
			  .AllowAnyHeader()
			  .AllowAnyMethod());
});

// EF Core
builder.Services.AddDbContext<TaskTrackerDbContext>(options =>
	options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")
		?? "Data Source=tasktracker.db"));

// DI: Application services and repositories
builder.Services.AddScoped<ITaskItemService, TaskItemService>();
builder.Services.AddScoped<ITaskItemRepository, TaskItemRepository>();

// FluentValidation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<CreateTaskItemDtoValidator>();

// Consistent model validation behavior: disable automatic 400 from ApiController
builder.Services.Configure<ApiBehaviorOptions>(o =>
{
	o.SuppressModelStateInvalidFilter = true;
});

var app = builder.Build();

// Auto-apply EF Core migrations on startup in Development
if (app.Environment.IsDevelopment())
{
	using var scope = app.Services.CreateScope();
	var db = scope.ServiceProvider.GetRequiredService<TaskTrackerDbContext>();
	try { db.Database.Migrate(); }
	catch { /* For the assessment keep it simple; in real apps log errors */ }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI(c =>
	{
		c.SwaggerEndpoint("/swagger/v1/swagger.json", "TaskTracker API v1");
		c.DisplayRequestDuration();
		c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
	});
}

app.UseHttpsRedirection();
	// Enable CORS for React dev server only in Development
if (app.Environment.IsDevelopment())
{
	app.UseCors("DevCors");
}

// Global exception handling -> consistent error responses
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseAuthorization();

app.MapControllers();

app.Run();
