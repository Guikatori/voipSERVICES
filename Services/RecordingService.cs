using Models.CommandInterface;
using Helpers.HashHelper;
using Services.RecordingFinder;
using Helpers.ResponseHelper;
namespace Services;
public static class RecordingService
{
    public static async Task SendTheArchive(CommandInterface callData)
    {
        var recordingPath = RecordingsFinder.GetRecordingPath(callData);
        var file = RecordingsFinder.GetLastAudioFile(recordingPath);
        if (file == null || file.Name == null)
        {
            ResponseHelper.ResponseStatus("the file was not found", 400);
            return;
        }
        string archivePath = Path.Combine(recordingPath, file.Name);

        if (file == null || file.Extension.ToLower() != ".mp3")
        {
            ResponseHelper.ResponseStatus("Invalid or missing MP3 file.", 400);
            return;
        }
        var keyName = HashHelper.GenerateFileKey(file.Name);
        await SendFileService.MainSendFile(callData, archivePath, keyName);
        var bucketUrl = $"https://voipbucket.s3.sa-east-1.amazonaws.com/{keyName}";
        await RecordingCardService.PostRecording(callData, bucketUrl, recordingPath, file.Name);
    }
}