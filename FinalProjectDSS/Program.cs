using FinalProjectDSS.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Text.Json;

namespace FinalProjectDSS
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Create a new WebApplication builder
            var builder = WebApplication.CreateBuilder(args);

            // If running inside a Docker container, override configuration for services
            if (Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true")
            {
                // Use Docker service names for connections
                builder.Configuration["ConnectionStrings:DefaultConnection"] = "Host=db;Port=5432;Database=TodoDb;Username=postgres;Password=postgres";
                builder.Configuration["RedisConnection"] = "redis:6379,abortConnect=false";
                builder.Configuration["RabbitHost"] = "rabbitmq";
                builder.Configuration["Jwt:Key"] = "SuperSecretKeyThatIsAtLeast32CharactersLong!";
            }

            // Add controllers and configure JSON serialization (camelCase)
            builder.Services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                });

            // Configure Redis distributed cache (for both local and Docker)
            builder.Services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = builder.Configuration["RedisConnection"] ?? "localhost:6379,abortConnect=false,connectTimeout=10000";
            });

            // Register RabbitMQ service as a singleton
            builder.Services.AddSingleton<FinalProjectDSS.Services.RabbitMqService>();

            // Configure CORS to allow any origin, method, and header
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });
            });

            //  SWAGGER CONFIGURATION
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Todo API", Version = "v1" });

                // Add JWT Bearer authentication to Swagger UI
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "Enter JWT token",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer"
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });

            // Register EF Core DbContext with PostgreSQL
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

            // Configure JWT authentication
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = builder.Configuration["Jwt:Issuer"],
                        ValidAudience = builder.Configuration["Jwt:Audience"],
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
                    };
                });
            builder.Services.AddAuthorization();
            // Build the application
            var app = builder.Build();

            // Apply any pending EF Core migrations at startup
            using (var scope = app.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<FinalProjectDSS.Data.AppDbContext>();
                dbContext.Database.Migrate();
            }

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                // Enable Swagger UI in development
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Todo API v1"));
            }

            app.UseHttpsRedirection();

            // Enable CORS policy
            app.UseCors("AllowAll");

            // Enable authentication and authorization middleware
            app.UseAuthentication();
            app.UseAuthorization();

            // Map controller routes
            app.MapControllers();

            // Start the application
            app.Run();
        }
    }
}