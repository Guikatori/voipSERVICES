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
using Helpers.TaskCompletionSource;

#pragma warning disable CA1416

public class ProcessUtilities
{
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
            var recordingPath = RecordingsFinder.GetRecordingPath();
            bool fileFound = await WaitForAudioFileAsync(callData, recordingPath, TimeSpan.FromSeconds(40));

            if (!fileFound)
            {
                Console.WriteLine("Nenhum arquivo de áudio encontrado.");
                await RecordingCardService.RecusedCall(callData);
                return ResponseHelper.ResponseStatus("No audio file found", 500);
            }

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

    private static async Task<bool> WaitForAudioFileAsync(CommandInterface callData, string directoryPath, TimeSpan timeout)
    {
        var tcs = new TaskCompletionSource<bool>();
        var timer = new Timer(2000) { AutoReset = true };
        DateTime startTime = DateTime.Now;

        var initialFiles = Directory.GetFiles(directoryPath).ToHashSet();

        timer.Elapsed += (sender, e) =>
        {
            var currentFiles = Directory.GetFiles(directoryPath);
            var newFile = currentFiles.FirstOrDefault(file => !initialFiles.Contains(file));

            if (newFile != null)
            {
                var creationTime = File.GetLastWriteTime(newFile);
                if (tcs.TrySetResult(true))
                {
                    TasksCs.disposeResources(timer);
                }
                return;
            }
            if ((DateTime.Now - startTime) >= timeout)
            {
                Console.WriteLine("Tempo limite atingido. Nenhum novo arquivo foi criado.");
                if (tcs.TrySetResult(false))
                {
                    TasksCs.disposeResources(timer);
                }
            }
        };

        timer.Start();
        var result = await tcs.Task;
        if (!result)
        {
            Console.WriteLine("Nenhum arquivo foi criado. Chamando RecordingCardService.RecusedCall...");
            await RecordingCardService.RecusedCall(callData);
        }

        return result;
    }

    public static async Task<dynamic> MonitorAudioFileAsync(CommandInterface callData, string recordingPath)
    {
        var tcs = new TaskCompletionSource<bool>();
        var fileInfo = RecordingsFinder.GetLastAudioFile(recordingPath);

        if (fileInfo == null)
        {
            Console.WriteLine("Nenhum arquivo de áudio encontrado.");
            tcs.SetResult(false);
            return tcs.Task;
        }

        string filePath = fileInfo.FullName;
        DateTime lastChangeTime = DateTime.Now;
        var timer = new Timer(4000) { AutoReset = true };

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
                TasksCs.disposeResources(timer);
                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var reader = new Mp3FileReader(stream))
                {
                    TimeSpan duration = reader.TotalTime;
                    Console.WriteLine($"Duração do arquivo de áudio: {duration}");
                }

                if (tcs.TrySetResult(true))
                {
                    await RecordingService.SendTheArchive(callData);
                }
            }
        };
        timer.Start();
        return tcs.Task;
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
