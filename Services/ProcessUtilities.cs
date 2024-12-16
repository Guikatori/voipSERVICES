namespace Services;

using System.Diagnostics;
using System.IO;
using Models.CommandInterface;
using Helpers.ResponseHelper;
using Helpers.DeleteAllFiles;
using Services.RecordingFinder;
using Services;
using NAudio.Wave;
using Helpers.Deletefile;

#pragma warning disable CA1416

public class ProcessUtilities
{
    private readonly string _localPath;

    public ProcessUtilities()
    {
        _localPath = LocalPathFinder.MicrosipPath();
    }

public async Task<IResult> MakeCall(CommandInterface callData)
{
    if (string.IsNullOrEmpty(callData.Phone) || callData.Phone == "0")
    {
        return ResponseHelper.ResponseStatus("Phone is required", 400);
    }

    if (callData.DealId == 0 && callData.ContactId == 0)
    {
        return ResponseHelper.ResponseStatus("CallId or ContactId is required", 400);
    }

    if (string.IsNullOrEmpty(_localPath))
    {
        return ResponseHelper.ResponseStatus("The LocalPath is Null", 400);
    }

    try
    {
        var procStartInfo = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/c {_localPath} {callData.Phone}",
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden
        };

        var processCalled = Process.Start(procStartInfo);

        if (processCalled == null)
        {
            return ResponseHelper.ResponseStatus("Failed to start the process", 500);
        }

        await Task.Delay(50000); 

        var recordingPath = RecordingsFinder.GetRecordingPath();
        await MonitorAudioFileAsync(callData, recordingPath);

        return ResponseHelper.ResponseStatus("Call made", 200);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
        return ResponseHelper.ResponseStatus(ex.Message, 400);
    }
}


   public static async Task MonitorAudioFileAsync(CommandInterface callData, string recordingPath)
{
    try
    {
        var fileInfo = RecordingsFinder.GetLastAudioFile(recordingPath);

        if (fileInfo == null)
        {
            Console.WriteLine("Nenhum arquivo de áudio encontrado.");
            await RecordingCardService.RecusedCall(callData);
            return;
        }

        string filePath = fileInfo.FullName;

        if (!File.Exists(filePath))
        {
            Console.WriteLine("Arquivo não encontrado.");
            await RecordingCardService.RecusedCall(callData);
            return;
        }

        DateTime lastChangeTime = DateTime.Now;

        while (true)
        {
            if (IsFileLocked(filePath))
            {
                Console.WriteLine("Arquivo ainda está em uso. Aguardando liberação...");
                await Task.Delay(2000);
                continue;
            }
            if ((DateTime.Now - lastChangeTime).TotalSeconds >= 5)
            {
                Console.WriteLine("Arquivo de áudio não teve mudanças. Finalizando...");

                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    using (var reader = new Mp3FileReader(stream))
                    {
                        TimeSpan duration = reader.TotalTime;
                        Console.WriteLine($"Duração do arquivo de áudio: {duration}");
                    }
                }
                await RecordingService.SendTheArchive(callData);
                break;
            }

            await Task.Delay(1000);
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Erro no monitoramento do arquivo: {ex.Message}");
    }
}

    private static bool IsFileLocked(string filePath)
    {
        try
        {
            using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                stream.Close();
            }
            return false; 
        }
        catch (IOException)
        {
            return true; 
        }
    }

 }
