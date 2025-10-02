using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

var jwtKey = builder.Configuration["Jwt:Key"] 
    ?? throw new InvalidOperationException("Jwt:Key não configurado no appsettings.json");

var jwtIssuer = builder.Configuration["Jwt:Issuer"] 
    ?? throw new InvalidOperationException("Jwt:Issuer não configurado no appsettings.json");

var jwtAudience = builder.Configuration["Jwt:Audience"] 
    ?? throw new InvalidOperationException("Jwt:Audience não configurado no appsettings.json");

if (jwtKey.Length < 16)
    throw new InvalidOperationException("Jwt:Key deve ter pelo menos 16 caracteres");

var key = Encoding.ASCII.GetBytes(jwtKey);

builder.Services.AddAuthentication(option =>
{
    option.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    option.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false; // alterar quando for subir para a produção, habilitado para receber req http
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

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();