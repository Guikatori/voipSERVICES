
namespace Helpers.DeleteAllFiles;
using Services.RecordingFinder;

using System.IO;
using System;

public static class DeleteAllFiles
{
    public static bool ExcludesArch()
    {
        var filePath = RecordingsFinder.GetRecordingPath();
        if (!Directory.Exists(filePath))
        {
            Console.WriteLine("O diretório especificado não existe.");
            return true;
        }

        bool allFilesDeleted = true;

        foreach (var file in Directory.GetFiles(filePath))
        {
            try
            {
                File.Delete(file);
                Console.WriteLine($"Arquivo deletado: {file}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao deletar o arquivo {file}: {ex.Message}");
                allFilesDeleted = false;
            }
        }

        return allFilesDeleted;
    }
}