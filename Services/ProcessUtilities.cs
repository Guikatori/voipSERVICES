namespace Services;

using System.Diagnostics;
using System.IO;
using Models.CommandInterface;
using Helpers.ResponseHelper;
using Services.RecordingFinder;
using Services;
using NAudio.Wave;
using Helpers.Deletefile;
using Services.LogsCloudWatch;
using System.Timers;
#pragma warning disable CA1416

public class ProcessUtilities
{
    public ProcessUtilities()
    {

    }

    public async Task<IResult> MakeCall(CommandInterface callData)
    {

        var localPath = LocalPathFinder.MicrosipPath(callData);

        if (string.IsNullOrEmpty(callData.Phone) || callData.Phone == "0")
        {
            return ResponseHelper.ResponseStatus("Phone is required", 400);
        }

        if (callData.DealId == 0 && callData.ContactId == 0)
        {
            return ResponseHelper.ResponseStatus("CallId or ContactId is required", 400);
        }

        if (string.IsNullOrEmpty(localPath))
        {
            await LogsCloudWatch.LogsCloudWatch.SendLogs(callData, "The LocalPath is Null");
            return ResponseHelper.ResponseStatus("The LocalPath is Null", 400);
        }

        try
        {
            var procStartInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c {localPath} {callData.Phone}",
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
            await LogsCloudWatch.LogsCloudWatch.SendLogs(callData, "The LocalPath is Null");
            return ResponseHelper.ResponseStatus("Call made", 200);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return ResponseHelper.ResponseStatus(ex.Message, 400);
        }
    }


    public static async Task<dynamic> MonitorAudioFileAsync(CommandInterface callData, string recordingPath)
    {
        var taskCs = new TaskCompletionSource<bool>();
        var fileInfo = RecordingsFinder.GetLastAudioFile(recordingPath);

        if (fileInfo == null)
        {
            Console.WriteLine("Nenhum arquivo de áudio encontrado.");
            await RecordingCardService.RecusedCall(callData);
            taskCs.SetResult(true);
            return taskCs.Task;
        }

        string filePath = fileInfo.FullName;

        if (!File.Exists(filePath))
        {
            Console.WriteLine("Arquivo não encontrado.");
            await RecordingCardService.RecusedCall(callData);
            taskCs.SetResult(true);
            return taskCs.Task;
        }

        DateTime lastChangeTime = DateTime.Now;
        var timer = new Timer(1000); 

        timer.Elapsed += async (sender, e) =>
            {
                if (IsFileLocked(filePath))
                {
                    Console.WriteLine("Arquivo ainda está em uso. Aguardando liberação...");
                    return;
                }

                DateTime currentWriteTime = File.GetLastWriteTime(filePath);

                if (currentWriteTime > lastChangeTime)
                {
                    lastChangeTime = currentWriteTime;
                    Console.WriteLine($"Arquivo atualizado em: {currentWriteTime}");
                    return;
                }

                if ((DateTime.Now - lastChangeTime).TotalSeconds >= 5)
                {
                    Console.WriteLine("Arquivo de áudio não teve mudanças. Finalizando...");

                    timer.Stop();
                    timer.Dispose();

                    using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        using (var reader = new Mp3FileReader(stream))
                        {
                            TimeSpan duration = reader.TotalTime;
                            Console.WriteLine($"Duração do arquivo de áudio: {duration}");
                        }
                    }

                    await RecordingService.SendTheArchive(callData);
                    taskCs.SetResult(true);
                }
            };

        timer.Start();
        return taskCs.Task;
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
