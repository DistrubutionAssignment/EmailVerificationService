using Azure.Communication.Email;
using Azure.Messaging.ServiceBus;
using EmailVerificationService.Interface;
using EmailVerificationService.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddMemoryCache();
builder.Services.AddSingleton(x => new EmailClient(builder.Configuration["ACS:ConnectionString"]));
builder.Services.AddTransient<IVerificationService, VerificationService>();
builder.Services.AddSingleton(sp =>
{
    var sbConnection = builder.Configuration["ServiceBus:ConnectionString"];
    return new ServiceBusClient(sbConnection);
});


var app = builder.Build();
app.MapOpenApi();
app.UseHttpsRedirection();
app.UseCors(x => x.AllowAnyHeader().AllowAnyOrigin().AllowAnyMethod());

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
