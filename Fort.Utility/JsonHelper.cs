using System.Text.Json;

namespace Fort.Utility;

public class JsonHelper
{
    private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
    };

    public static T Load<T>(string path)
    {
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<T>(json, Options)!;
    }

    public static void Save<T>(string path, T dataToSave)
    {
        var json = JsonSerializer.Serialize(dataToSave, Options);
        File.WriteAllText(path, json);
    }
}
