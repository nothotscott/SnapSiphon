using TagLib;

namespace SnapSiphon.Services;

public static class MetadataWriterService
{
    public static void ApplyTimestamp(string filePath, DateTime utcDateTime)
    {
        System.IO.File.SetCreationTimeUtc(filePath, utcDateTime);
        System.IO.File.SetLastWriteTimeUtc(filePath, utcDateTime);
    }

    public static bool TryWriteGps(string filePath, double latitude, double longitude)
    {
        try
        {
            var ext = Path.GetExtension(filePath).ToLowerInvariant();
            if (ext is ".jpg" or ".jpeg" or ".png")
                WriteImageGps(filePath, latitude, longitude);
            else if (ext is ".mp4")
                WriteMp4Gps(filePath, latitude, longitude);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void WriteImageGps(string filePath, double latitude, double longitude)
    {
        using var file = TagLib.File.Create(filePath);
        if (file is not TagLib.Image.File imageFile) return;

        imageFile.ImageTag.Latitude = latitude;
        imageFile.ImageTag.Longitude = longitude;
        imageFile.Save();
    }

    private static void WriteMp4Gps(string filePath, double latitude, double longitude)
    {
        using var file = TagLib.File.Create(filePath);
        var appleTag = file.GetTag(TagTypes.Apple, create: true) as TagLib.Mpeg4.AppleTag;
        if (appleTag is null) return;

        // ISO 6709 annex H format: ±DD.DDDD±DDD.DDDD/
        var iso6709 = $"{latitude:+0.0000;-0.0000}{longitude:+000.0000;-000.0000}/";

        // ©xyz box key bytes: 0xA9 0x78 0x79 0x7A
        var key = new ByteVector(new byte[] { 0xA9, 0x78, 0x79, 0x7A });
        appleTag.SetText(key, iso6709);
        file.Save();
    }
}
