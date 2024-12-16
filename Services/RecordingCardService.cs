namespace Services;
using Helpers.ResponseHelper;
using Models.CommandInterface;
using Helpers.Deletefile;



public static class RecordingCardService
{
    public static async Task PostRecording(CommandInterface callData, string bucketUrl, string recPath, string fileName)
    {
        if (callData.DealId == 0 && callData.ContactId == 0) return;

        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {callData.ApiKey}");
        client.DefaultRequestHeaders.Add("User-Key", callData.ApiKey);

        var payload = new Dictionary<string, dynamic>
        {
            { "ContactId", callData.ContactId ?? 0},
            { "Content", $"<div>Ligação realizada via <strong>Suncall</strong></div> " +
                            $"<audio controls style='width: 300px; height: 70px;'>" +
                            $"<source src='{bucketUrl}' type='audio/mpeg'>" +
                            "Seu navegador não suporta o elemento de áudio.</audio> " +
                            $"<img src='https://stgploomescrmprd01.blob.core.windows.net/crm-prd/A841002849BF/Images/0bf06332101544d28bedac7b827d272f.png'/>"},
            { "DealId", callData.DealId ?? 0},
            { "TypeId", 1 }
        };

        var response = await client.PostAsJsonAsync("https://api2.ploomes.com/InteractionRecords", payload);

        if(response.IsSuccessStatusCode){
            ResponseHelper.ResponseStatus("The archive was post in Ploomes With Sucess",200);
            
            bool fileDeleted = DeleteFiles.Deletefile(recPath, fileName);
            if(fileDeleted){
                ResponseHelper.ResponseStatus("File Deleted With Sucess",200);
            }else{
                ResponseHelper.ResponseStatus("File was not Deleted",404);
            }
        }        
        else{
            ResponseHelper.ResponseStatus("The archive wasn't post in Ploomes", (int)response.StatusCode);
        }
        
        Console.WriteLine(response.IsSuccessStatusCode ? "Recording posted successfully." : "Failed to post recording.");
    }



    public static async Task RecusedCall(CommandInterface callData)
    {
        if (callData.DealId == 0 && callData.ContactId == 0) return;

        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {callData.ApiKey}");
        client.DefaultRequestHeaders.Add("User-Key", callData.ApiKey);

        var recusedPayload = new Dictionary<string, dynamic>
        {
            { "ContactId", callData.ContactId ?? 0},
            { "Content"," <div>Tentativa de ligação via <strong>Suncall</strong> : cliente não atendeu</div>"},
            { "DealId", callData.DealId ?? 0},
            { "TypeId", 1 }
        };

        var response = await client.PostAsJsonAsync("https://api2.ploomes.com/InteractionRecords", recusedPayload);      
        Console.WriteLine(response.IsSuccessStatusCode ? "Recording posted successfully." : "Failed to post recording.");
    }

}