using Amazon.CloudWatchLogs;
using Amazon.CloudWatchLogs.Model;
using Models.CommandInterface;
using Amazon.Runtime;
using credentials;

namespace Services.LogsCloudWatch;

public class LogsCloudWatch
{

    public static AmazonCloudWatchLogsClient client = new AmazonCloudWatchLogsClient(
            new BasicAWSCredentials(awsKeys.AwsKey, awsKeys.AwsSecretKey),
            Amazon.RegionEndpoint.SAEast1
        );

    public static string logGroupName = "ServerVoip";

    public async static Task<bool> verifyStreamLog(CommandInterface CallData)
    {
        try
        {
            string logStreamName = $"VoipLogs-{CallData.AccountName}";

            var describeStreamsResponse = await DescribeLogStreamsAsync(logStreamName);
            var describes = describeStreamsResponse.LogStreams;
            bool hasStream = StreamExists(describes, logStreamName);
            if (!hasStream)
            {
                await CreateLogStream(logStreamName);
                Console.WriteLine("Log Stream criado com sucesso.");
                return true;
            }
            else
            {
                Console.WriteLine("Log Stream já existe.");
                return true;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return false;
        }
    }

    public static bool StreamExists(List<LogStream> describes, string logStreamName){

        foreach(var stream in describes){
            if(stream.LogStreamName == logStreamName){
                return true;
            }
        }
        return false;
    }


    public static async Task<DescribeLogStreamsResponse> DescribeLogStreamsAsync(string logStreamName)
    {
        var describeStreamsRequest = new DescribeLogStreamsRequest
        {
            LogGroupName = logGroupName,
            LogStreamNamePrefix = logStreamName
        };
        return await client.DescribeLogStreamsAsync(describeStreamsRequest);

    }

    public static async Task CreateLogStream(string logStreamName)
    {

        var createStreamRequest = new CreateLogStreamRequest
        {
            LogGroupName = logGroupName,
            LogStreamName = logStreamName
        };

        await client.CreateLogStreamAsync(createStreamRequest);
    }


    public static async Task SendLogs(CommandInterface CallData, string message)
    {

        bool isStreamExist = await verifyStreamLog(CallData);
        if (isStreamExist == false)
        {
            Console.WriteLine("Error, the Stream Log doesnt Exist, and is Not Possible To Create");
            return;
        }

        string logStreamName = $"VoipLogs-{CallData.AccountName}";


        string sequenceToken = "";
        var describeStreamsResponse = await DescribeLogStreamsAsync(logStreamName);

        if (describeStreamsResponse.LogStreams.Count > 0)
        {
            sequenceToken = describeStreamsResponse.LogStreams[0].UploadSequenceToken;
        }

        var logEvents = new List<InputLogEvent>
      {
        new InputLogEvent
        {
            Message = message,
            Timestamp = DateTime.UtcNow
        }
        };

        var putLogEventsRequest = new PutLogEventsRequest
        {
            LogGroupName = logGroupName,
            LogStreamName = logStreamName,
            LogEvents = logEvents,
            SequenceToken = sequenceToken
        };

        try
        {
            await client.PutLogEventsAsync(putLogEventsRequest);
            Console.WriteLine("Log Foi enviado Com Sucesso");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao enviar log: {ex.Message}");
        }

    }
}