using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace KeepAlive.Infrastructure.Persistence;

public sealed class AtomicJsonFile(string path)
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public T? Read<T>()
    {
        if (!File.Exists(path))
        {
            return default;
        }

        try
        {
            string json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<T>(json, SerializerOptions);
        }
        catch (JsonException)
        {
            return default;
        }
    }

    public void Write<T>(T value)
    {
        string? directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string temporaryPath = $"{path}.{Guid.NewGuid():N}.tmp";

        try
        {
            string json = JsonSerializer.Serialize(value, SerializerOptions);
            File.WriteAllText(temporaryPath, json);

            if (File.Exists(path))
            {
                File.Replace(temporaryPath, path, null);
            }
            else
            {
                File.Move(temporaryPath, path);
            }
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }
}
