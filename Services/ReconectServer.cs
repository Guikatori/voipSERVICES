namespace Services.ReconectServer;

using System.Diagnostics;
using Models.FrontCommands;
#pragma warning disable

public static class ReconectServers
{
    public static void Reconect(FrontCommands frontCommands)
    {
        if(frontCommands.Message == "Reconect"){

        ProcessStartInfo processStart = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = "run",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        if (processStart == null)
        {
            Console.WriteLine("error to Reconect the server");
            return;
        }
        Process process = Process.Start(processStart);

        if (process == null)
        {
            Console.WriteLine("Error To Reconect");
            return;

        }
        if (process != null)
        {
            process.WaitForExit(); 
            Console.WriteLine("Aplicação .NET iniciada com sucesso!");
        }
    }else{
        Console.WriteLine("The Message is Not 'Reconect'");
        return;
    }
}
}