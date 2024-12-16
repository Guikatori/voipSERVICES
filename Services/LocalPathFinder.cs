namespace Services;
using Services.LogsCloudWatch;
using Helpers.ResponseHelper;
using Models.CommandInterface;

    public class LocalPathFinder
    {
        public static string MicrosipPath(CommandInterface callData)
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            string targetPath = Path.Combine(appDataPath, "MicroSIP", "microsip.exe");
        try{
            if (File.Exists(targetPath))
            {
                Console.WriteLine("Caminho do arquivo encontrado: " + targetPath);
                return targetPath;
            }
            else
            {
                targetPath = SecondTypeOfPathVerification();
                return targetPath;
            }
        }catch(Exception){
               Task.Run(() => LogsCloudWatch.LogsCloudWatch.SendLogs(callData, "The LocalPath is Null"));
               return string.Empty;
        }
        }

        public static string SecondTypeOfPathVerification()
        {
            string programFilesPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            string program86x = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

            List<string> morePaths = new List<string>
            {
                Path.Combine(programFilesPath, "Microsip", "microsip.exe"),
                Path.Combine(program86x, "Microsip", "microsip.exe")
            };

            foreach (var path in morePaths)
            {
                if (File.Exists(path))
                {
                    Console.WriteLine("Caminho do arquivo encontrado: " + path);
                    return path;
                }
            }
            ResponseHelper.ResponseStatus("Is Not Possible To Find Microsip Path",400);
            return string.Empty;
        }
    }
