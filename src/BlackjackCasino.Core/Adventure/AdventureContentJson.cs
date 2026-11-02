using System.Text.Json;
using System.Text.Json.Serialization;

namespace BlackjackCasino.Core.Adventure;

public static class AdventureContentJson
{
    private static readonly JsonSerializerOptions Options = CreateOptions();

    public static AdventureDefinition Deserialize(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new ArgumentException("Adventure JSON cannot be empty.", nameof(json));

        AdventureDefinition definition = JsonSerializer.Deserialize<AdventureDefinition>(json, Options)
            ?? throw new JsonException("Adventure JSON did not contain a definition.");
        AdventureDefinitionValidator.Validate(definition);
        return definition;
    }

    public static string Serialize(AdventureDefinition definition)
    {
        AdventureDefinitionValidator.Validate(definition);
        return JsonSerializer.Serialize(definition, Options);
    }

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
        };
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        return options;
    }
}
