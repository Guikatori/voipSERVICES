namespace Helpers.Deletefile;
public class DeleteFiles
{
    public static bool Deletefile(string path, string fileName)
    {
        string file = Path.Combine(path, fileName);

        try
        {
            if (File.Exists(file))
            {
                try
                {
                    using (var stream = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.None))
                    {
                        stream.Close();
                    }
                }
                catch (IOException)
                {
                    Console.WriteLine($"O arquivo está bloqueado: {file}. Não foi possível deletar.");
                    return false;
                }
                File.Delete(file);
                Console.WriteLine("File deleted.");
                return true;
            }
            else
            {
                Console.WriteLine("File not found");
                return false;
            }
        }
        catch (IOException ioExp)
        {
            Console.WriteLine(ioExp.Message);
            return false;
        }
    }
}