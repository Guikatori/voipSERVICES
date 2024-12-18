namespace Services.QuitServer;
#pragma warning disable
using Models;
using System.Diagnostics;
using Models.FrontCommands;

public class ServerQuit
{

    public async static Task QuitServer(FrontCommands frontCommands)
    {

        if (frontCommands.Message == "Quit")
        {
            Process.GetCurrentProcess().Kill();
        }
        else
        {
            Console.WriteLine("Mensagem Errada, O Server Continuará");
        }
    }
}