using System.Text.Json;

namespace YoutubePlaylist.Extensions;

public static class JsonExtensions
{
    public static IEnumerable<JsonElement> Descendants(this JsonElement element)
    {
        yield return element;

        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    foreach (var child in property.Value.Descendants())
                        yield return child;
                }
                break;

            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    foreach (var child in item.Descendants())
                        yield return child;
                }
                break;
        }
    }
}
