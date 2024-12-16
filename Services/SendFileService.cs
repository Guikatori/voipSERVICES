namespace Services;

using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Helpers;
using Helpers.ResponseHelper;
using credentials;

public static class SendFileService
{
    public static String bucketName = "voipbucket";
    private static RegionEndpoint bucketRegion = RegionEndpoint.SAEast1;
    private static IAmazonS3? s3Client;


    public static async Task<string?> MainSendFile(string filePath, string stringKeyConcat)
    {
        s3Client = new AmazonS3Client(awsKeys.AwsKey, awsKeys.AwsSecretKey, bucketRegion);

        if (string.IsNullOrEmpty(filePath) || string.IsNullOrEmpty(stringKeyConcat))
        {
            ResponseHelper.ResponseStatus("FilePath or Key is missing", 400);
            return string.Empty;
        }
        try
        {
            var response = await UploadFileAsync(filePath, stringKeyConcat);
            if(string.IsNullOrEmpty(response)){
                    return string.Empty;
                }
            ResponseHelper.ResponseStatus("The File was Send", 200);
            return response;

        }
        catch (AmazonS3Exception ex)
        {
            ResponseHelper.ResponseStatus($"AWS S3 Error: {ex.Message}", 400);
            return string.Empty;
        }
        catch (Exception ex)
        {
            ResponseHelper.ResponseStatus($"Unexpected Error: {ex.Message}", 400);
            return string.Empty;

        }
    }


    private static async Task<dynamic?> UploadFileAsync(string filePath, string stringKeyConcat)
    {
        try
        {
            var putRequest = new PutObjectRequest
            {
                BucketName = bucketName,
                Key = stringKeyConcat,
                FilePath = filePath,
                ContentType = "audio/mpeg"
            };
            if (s3Client == null)
            {
                ResponseHelper.ResponseStatus("The S3Client is null", 400);
                return string.Empty;
            }
            PutObjectResponse response = await s3Client.PutObjectAsync(putRequest);
            ResponseHelper.ResponseStatus("Archive posted in Aws", (int)response.HttpStatusCode);
            Console.WriteLine($"https://voipbucket.s3.sa-east-1.amazonaws.com/{stringKeyConcat}");

            return response;
        }
        catch (AmazonS3Exception s3Ex){
            Console.WriteLine($"Error Message: {s3Ex.Message} | AWS Error Code: {s3Ex.ErrorCode}");
            ResponseHelper.ResponseStatus("AWS S3 Error: " + s3Ex.Message, 500);
            return string.Empty;

        }

        catch(Exception ex)
        {
            Console.WriteLine($"Error Message: {ex.Message} | AWS Error Code: {ex.StackTrace}");
            ResponseHelper.ResponseStatus($"Unexpected Error: {ex.Message}", 400);
            return string.Empty;
        }
    }
}