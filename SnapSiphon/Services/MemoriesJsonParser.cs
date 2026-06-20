using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using SnapSiphon.Models;

namespace SnapSiphon.Services;

public static class MemoriesJsonParser
{
    private static readonly Regex LocationRegex =
        new(@"Latitude,\s*Longitude:\s*(-?\d+\.?\d*),\s*(-?\d+\.?\d*)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static async Task<List<MemoryEntry>> ParseAsync(string jsonPath, CancellationToken ct = default)
    {
        await using var stream = File.OpenRead(jsonPath);
        var root = await JsonSerializer.DeserializeAsync<JsonRoot>(stream,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
            cancellationToken: ct);

        return root?.SavedMedia?.Select(Parse).ToList() ?? [];
    }

    private static MemoryEntry Parse(SavedMediaItem item)
    {
        DateTime.TryParseExact(
            item.Date, "yyyy-MM-dd HH:mm:ss 'UTC'",
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
            out var dateUtc);

        double? lat = null, lon = null;
        if (!string.IsNullOrWhiteSpace(item.Location))
        {
            var m = LocationRegex.Match(item.Location);
            if (m.Success)
            {
                lat = double.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
                lon = double.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture);
            }
        }

        return new MemoryEntry
        {
            DateUtc = dateUtc,
            MediaType = item.MediaType ?? "",
            Latitude = lat,
            Longitude = lon
        };
    }

    private sealed class JsonRoot
    {
        [JsonPropertyName("Saved Media")]
        public List<SavedMediaItem>? SavedMedia { get; set; }
    }

    private sealed class SavedMediaItem
    {
        [JsonPropertyName("Date")]
        public string? Date { get; set; }

        [JsonPropertyName("Media Type")]
        public string? MediaType { get; set; }

        [JsonPropertyName("Location")]
        public string? Location { get; set; }
    }
}
