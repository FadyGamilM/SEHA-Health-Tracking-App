using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SehaNotebook.DAL.Data;
using SehaNotebook.DAL.IConfiguration;
using SehaNotebook.Authentication.Configurations;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Identity;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.


builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//! Inject my own services
//*=> configure jwt settings 
builder.Services.Configure<JwtConfig>(
    builder.Configuration.GetSection("JwtConfig")
);

//*=> connect to postgres through the AppDbContext
builder.Services.AddEntityFrameworkNpgsql().AddDbContext<AppDbContext>(
    options => options.UseNpgsql(
        builder.Configuration.GetConnectionString("conn")
    )
);

//*=> Inject the User repository
// builder.Services.AddScoped<IUserRepo, UserRepo>();

//*=> Inject the UnitOfWork serivce
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

//*=> utilize the api versioning nuget package
builder.Services.AddApiVersioning(
    options => 
    {
        // provide our client by the different api versions that we have
        options.ReportApiVersions = true;
        // allow the api to automatically provide a default version
        options.AssumeDefaultVersionWhenUnspecified = true;
        // the default version
        options.DefaultApiVersion= ApiVersion.Default;
    }
);

//*=> Configure the authentication mechanism
builder.Services
    .AddAuthentication(
        options => {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
    .AddJwtBearer(
        jwt=>{
            var secretKey = Encoding.ASCII.GetBytes(builder.Configuration["JwtConfig:Secret"]);
            // after authorization, save this token inside the authentication property
            jwt.SaveToken = true;
            jwt.TokenValidationParameters = new TokenValidationParameters()
            {
                //! accept the only tokens that is validated against our secret key 
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(secretKey),
                ValidateIssuer = false, // for dev
                ValidateAudience = false, // for dev
                RequireExpirationTime = false, // for dev -- needs to be updated when refresh token is added
                ValidateLifetime = true
            };
        }
    )
    ;
//*=> utilize the default identity provider from .net core
builder.Services.AddDefaultIdentity<IdentityUser>(
    options => options.SignIn.RequireConfirmedAccount=true
    ).AddEntityFrameworkStores<AppDbContext>();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

//*=> add authentication middle ware
app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
