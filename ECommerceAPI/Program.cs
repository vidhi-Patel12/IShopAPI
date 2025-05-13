using ECommerceAPI.Data;
using ECommerceAPI.Interface;
using ECommerceAPI.Repository;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddHttpContextAccessor();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "ECommerceAPI",
        Version = "v1"
    });

    c.EnableAnnotations(); // <-- Add this line
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

var apiUrl = builder.Configuration["ApiSettings:APIURL"];

builder.Services.AddScoped<ICoupan, CoupanRepository>();
builder.Services.AddScoped<IOrder, OrderRepository>();
builder.Services.AddScoped<IProduct, ProductRepository>();
builder.Services.AddScoped<IDashboard, DashboardRepository>();



builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
.AddCookie(options =>
{

});

builder.Services.AddCors(options =>
{
    options.AddPolicy("MyPolicyToAllowAnyOne", builder =>
    {
        builder.AllowAnyMethod();
        builder.AllowAnyHeader();
        builder.AllowAnyOrigin();
    });
});

builder.Services.AddHttpClient("MyHttpClient")
           .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
           {
               ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
           });


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.ValidatorUrl(null);

    });
}

app.UseHttpsRedirection();

app.UseRouting();
app.UseAuthorization();

app.UseCors("MyPolicyToAllowAnyOne");

app.MapControllers();

app.Run();
