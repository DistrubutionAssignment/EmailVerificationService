using Azure.Communication.Email;
using Azure.Identity;
using Azure.Messaging.ServiceBus;
using EmailVerificationService.Interface;
using EmailVerificationService.Services;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

//Lägg till Key Vault som konfig‐källa
var vaultUri = new Uri(builder.Configuration["KeyVault:VaultUri"]!);
builder.Configuration.AddAzureKeyVault(vaultUri, new DefaultAzureCredential());

//  Läs in hemligheterna
var acsConnString = builder.Configuration["ACS:ConnectionString"]!;
var acsSender = builder.Configuration["ACS:SenderAddress"]!;
var sbConn = builder.Configuration["ServiceBus:ConnectionString"]!;
var sbQueue = builder.Configuration["ServiceBus:QueueName"]!;

builder.Services.AddCors(o => o.AddPolicy("DefaultCors", p =>
    p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));



// Registrera EmailClient och ServiceBusClient
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();          
builder.Services.AddSwaggerGen(c =>                 
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "EmailVerificationService API",
        Version = "v1"
    });
});
builder.Services.AddOpenApi();

builder.Services.AddMemoryCache();

// Azure Communication Email
builder.Services.AddSingleton(_ => new EmailClient(acsConnString));

// Din egen Verifierings‐service
builder.Services.AddTransient<IVerificationService, VerificationService>();

// Service Bus
builder.Services.AddSingleton(_ => new ServiceBusClient(sbConn));

//CORS + Middleware
var app = builder.Build();

app.UseCors("DefaultCors");

app.UseHttpsRedirection();
app.UseCors(x => x.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());

app.UseAuthentication();
app.UseAuthorization();

//Swagger 
app.UseSwagger();             
app.UseSwaggerUI(c =>            
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "EmailVerificationService API V1");
    c.RoutePrefix = string.Empty;
});

app.MapOpenApi();
app.MapControllers();
app.Run();
