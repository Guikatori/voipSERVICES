using Models.CommandInterface;

namespace Services.RecordingFinder;
public static class RecordingsFinder
{
    public static string GetRecordingPath(CommandInterface callData)
    {
        var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        var recordingPath = Path.Combine(desktopPath, "Recordings");

        if (!Directory.Exists(recordingPath))
        {
            recordingPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Recordings");
            if (!Directory.Exists(recordingPath))
            {
                Task.Run(() => LogsCloudWatch.LogsCloudWatch.SendLogs(callData, "Critical: Is not Possible to Find The Recordings Folder"));
                return string.Empty;
            }
        }
        return string.Empty;
    }
    public static FileInfo? GetLastAudioFile(string path)
    {

        if (string.IsNullOrEmpty(path))
        {
            Console.WriteLine("Recording Path is Null");
            return null;
        }
        return new DirectoryInfo(path).GetFiles("*.mp3")
            .OrderByDescending(f => f.CreationTime)
            .FirstOrDefault();
    }
}