using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using QualityInspection;
using Scalar.AspNetCore;

Console.WriteLine("Docs: http://localhost:5000/scalar/v1");
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins",
        b =>
        {
            b.AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader();
        });
});
builder.Services.AddOpenApi(opt => opt.AddDocumentTransformer<BearerSecuritySchemeTransformer>());
builder.Services.AddControllers();
builder.Services.AddDbService(builder.Configuration);
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
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

var app = builder.Build();
app.MapOpenApi();
app.UseAuthentication();
app.UseAuthorization();
app.MapScalarApiReference(options =>
{
    options
        .WithPreferredScheme("Bearer")
        .WithHttpBearerAuthentication(bearer => { bearer.Token = builder.Configuration["Jwt:Token"]; });
});
app.UseHttpsRedirection();
app.MapControllers();
app.UseCors("AllowAllOrigins");
app.Run();