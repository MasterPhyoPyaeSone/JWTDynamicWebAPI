using System.Text;
using JWTDynamicRBACWebAPI.Database.AppDbContextModels;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore; // 💡 Scalar UI အသစ်အတွက်
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// CORS — BlazorUI frontend ကို ခေါ်ယူနိုင်ရန်
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazorUI", policy =>
    {
        policy.WithOrigins(
                    "http://localhost:5283",
                    "https://localhost:7288",
                    "http://localhost:5197",
                    "https://localhost:7197"
                )
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ==========================================
// 💡 .NET 10 ၏ OpenAPI နှင့် JWT Token လုံခြုံရေးချိတ်ဆက်ခြင်း
// ==========================================
builder.Services.AddOpenApi(options =>
{
    // Scalar UI တွင် JWT Token ထည့်ရန်နေရာ (Authorize Box) ဖန်တီးပေးခြင်း
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        // 💡 .NET 10 / Microsoft.OpenApi 2.x API: တစ်ခ်ကွန်အပိုင် IOpenApiSecurityScheme ဆာသာထာသည့်
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes = new Dictionary<string, IOpenApiSecurityScheme>
        {
            ["Bearer"] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "သင့် JWT Token ကို ဤနေရာတွင် ထည့်ပါ"
            }
        };

        // 💡 OpenApiSecuritySchemeReference ကိုအသား (OpenApiReference သာသဂဥ်မဟုတဲ)
        document.Security ??= new List<OpenApiSecurityRequirement>();
        document.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)] = []
        });
        return Task.CompletedTask;
    });
});

// ၂။ JWT Authentication စနစ်
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

// ၃။ Dynamic Policy မှတ်ပုံတင်ခြင်း
var serviceProvider = builder.Services.BuildServiceProvider();
using (var scope = serviceProvider.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        var permissions = dbContext.Permissions.Select(p => p.PermissionName).ToList();
        builder.Services.AddAuthorization(options =>
        {
            foreach (var permission in permissions)
            {
                options.AddPolicy(permission, policy => policy.RequireClaim("Permission", permission));
            }
        });
    }
    catch { builder.Services.AddAuthorization(); }
}

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // 💡 .NET 10 ပုံစံဖြင့် OpenAPI နှင့် Scalar UI ကို အသက်သွင်းခြင်း
    app.MapOpenApi();
    app.MapScalarApiReference(); // Swagger အစား ယခု UI အသစ်ကို သုံးမည်
}

app.UseCors("AllowBlazorUI");

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();