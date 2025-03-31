using System.Diagnostics;
using System.Reflection;
using System.Runtime.Versioning;
using System.Text.Json;
using System.Text.Json.Serialization;
using DartWing.ErpNext;
using DartWing.KeyCloak;
using DartWing.Microsoft;
using DartWing.Web.Api;
using DartWing.Web.Auth;
using DartWing.Web.Files;
using DartWing.Web.Logging;
using DartWing.Web.Users;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.OpenApi.Models;

var sw = Stopwatch.StartNew();

var builder = WebApplication.CreateBuilder(args);

builder.AddLogging();
// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddHttpContextAccessor();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddHttpClient("Azure");

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("project",
        new OpenApiInfo { Title = "DartWing", Version = "v1" });

    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "API Documentation",
        Version = "v1.0",
        Description = ""
    });
    options.ResolveConflictingActions(x => x.First());
    options.AddSecurityDefinition("Keycloak", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.OAuth2,
        Flows = new OpenApiOAuthFlows
        {
            Implicit = new OpenApiOAuthFlow
            {
                AuthorizationUrl = new Uri($"https://{builder.Configuration["KeyCloak:Domain"]}/realms/{builder.Configuration["KeyCloak:RealmName"]}/protocol/openid-connect/auth"),
                Scopes = new Dictionary<string, string>
                {
                    {"openid", "OpenId"},
                    {"profile", "Profile"},
                    {"email", "Email"},
                    {"offline_access", "Offline Access"},
                }
            }
        }
    });
    
    OpenApiSecurityScheme keycloakSecurityScheme = new()
    {
        Reference = new OpenApiReference
        {
            Id = "Keycloak",
            Type = ReferenceType.SecurityScheme,
        },
        In = ParameterLocation.Header,
        Name = "Bearer",
        Scheme = "Bearer",
    };

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { keycloakSecurityScheme, Array.Empty<string>() },
    });
});

builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

builder.Services.AddAuthenticationLogic(builder.Configuration);
builder.Services.AddKeyCloak(builder.Configuration);
builder.Services.AddDartWing(builder.Configuration);
builder.Services.AddMicrosoft(builder.Configuration);

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseSwagger();
app.UseSwaggerUI(settings =>
{
    settings.SwaggerEndpoint("/swagger/v1/swagger.json", "API v1.0");
    settings.OAuthClientId(app.Configuration["KeyCloak:ClientId"]);
    settings.OAuthUsePkce();
});


app.RegisterAzureApiEndpoints();
app.RegisterUserApiEndpoints();
app.RegisterCompanyApiEndpoints();
app.RegisterFolderApiEndpoints();
app.UseAuthentication();
app.UseAuthorization();

var t =
    $"v.{typeof(Program).Assembly.GetName().Version}; {typeof(Program).Assembly.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName}; {System.Runtime.InteropServices.RuntimeInformation.OSDescription}";

app.Logger.LogInformation("DartWing WebApp started {t} {sw}", t, sw.Elapsed);

app.Run();
