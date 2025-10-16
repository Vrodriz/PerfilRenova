using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Reflection;
using System.Text;
using PerfilWeb.Api.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string não configurada");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseMySql(
        connectionString,
        ServerVersion.AutoDetect(connectionString),
        mySqlOptions => mySqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorNumbersToAdd: null
        )
    );
});

// ==============================
// Configuração JWT
// ==============================
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key não configurado no appsettings.json");

var jwtIssuer = builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException("Jwt:Issuer não configurado no appsettings.json");

var jwtAudience = builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException("Jwt:Audience não configurado no appsettings.json");

if (jwtKey.Length < 16)
    throw new InvalidOperationException("Jwt:Key deve ter pelo menos 16 caracteres");

var key = Encoding.ASCII.GetBytes(jwtKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ClockSkew = TimeSpan.Zero
    };
});

// ==============================
// Configuração CORS
// ==============================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.SetIsOriginAllowed(origin =>
        {
            // Permitir localhost (qualquer porta)
            if (origin.StartsWith("http://localhost:") ||
                origin.StartsWith("https://localhost:"))
                return true;

            // Permitir qualquer subdomínio da Vercel
            if (origin.EndsWith(".vercel.app", StringComparison.OrdinalIgnoreCase))
                return true;

            // Permitir qualquer subdomínio da Railway
            if (origin.EndsWith(".up.railway.app", StringComparison.OrdinalIgnoreCase))
                return true;

            // Permitir domínio próprio
            if (origin == "https://perfilrenova.com.br" ||
                origin == "http://perfilrenova.com.br" ||
                origin == "https://www.perfilrenova.com.br" ||
                origin == "http://www.perfilrenova.com.br")
                return true;

            return false;
        })
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();
    });
});

// ==============================
// Swagger / OpenAPI
// ==============================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "PerfilRenova API",
        Description = "API para gerenciamento de clientes e assinaturas do sistema PerfilRenova",
        Contact = new OpenApiContact
        {
            Name = "Equipe PerfilRenova",
            Email = "contato@perfilrenova.com"
        }
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Insira o token JWT no formato: Bearer {seu_token}"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
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

    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);
    if (File.Exists(xmlPath))
        options.IncludeXmlComments(xmlPath);
});

var app = builder.Build();

// ==============================
// Pipeline HTTP
// ==============================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "PerfilRenova API v1");
        options.RoutePrefix = string.Empty;
        options.DocumentTitle = "PerfilRenova API - Documentação";
    });
}

var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
app.Urls.Add($"http://0.0.0.0:{port}");



app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
