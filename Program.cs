using Models.CommandInterface;
using Models.FrontCommands;
using WebSocket;
using Services;
using Services.ReconectServer;
using Services.QuitServer;


var builder = WebApplication.CreateBuilder(args);

// Registrar controladores
builder.Services.AddControllers();

builder.Services.AddCors(options => options.AddPolicy("AllowAll", policy =>
    policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

builder.WebHost.UseKestrel(options =>
{
    options.ListenAnyIP(49169);
});

int maxAttempts = 30;
int attempt = 0;
int delayTime = 10000;
bool serverStarted = false;

var app = builder.Build();
app.UseCors("AllowAll");

app.MapControllers();  

app.MapGet("/", () => Results.Json(new { applicationStatus = true, statusCode = 200 }));

app.MapPost("/call", (CommandInterface callData) =>
{
    var processUtils = new ProcessUtilities();
    return processUtils.MakeCall(callData);
});


app.MapPost("/quit", async (FrontCommands frontCommands) =>
{
     await ServerQuit.QuitServer(frontCommands);
     return;
});


app.MapPost("/reconect", async (FrontCommands frontCommands) =>
{
     ReconectServers.Reconect(frontCommands);
     return;
});




Socket.openWebSocket(app, builder.Environment);

while (attempt < maxAttempts)
{
    try
    {
        Console.WriteLine($"Tentativa {attempt + 1} de iniciar o servidor...");
        app.Run();
        serverStarted = true;
        break;
    }
    catch (IOException ex)
    {
        Console.WriteLine($"Erro ao iniciar o servidor: {ex.Message}");
        attempt++;

        if (attempt > 15)
        {
            delayTime *= 2;
        }

        Console.WriteLine($"Tentando novamente em {delayTime / 1000} segundos...");
        await Task.Delay(delayTime);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Erro inesperado: {ex.Message}");
        break;
    }
}

if (!serverStarted)
{
    Console.WriteLine("Não foi possível iniciar o servidor após várias tentativas, verifique se a porta 49169 já está sendo usada");
}
else
{
    Console.WriteLine("Servidor iniciado com sucesso.");
}
