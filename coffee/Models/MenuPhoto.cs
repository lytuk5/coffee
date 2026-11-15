namespace CafeDuaHau.Models;

public class MenuPhoto
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string MenuItemId { get; set; } = "";
    public int Position { get; set; }
    public string ContentType { get; set; } = "";
    public byte[] Content { get; set; } = [];

    public static string? DetectContentType(ReadOnlySpan<byte> bytes)
    {
        if (bytes.StartsWith(new byte[] { 0xff, 0xd8, 0xff }))
            return "image/jpeg";
        if (bytes.StartsWith(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }))
            return "image/png";
        if (bytes.Length >= 12 && bytes[..4].SequenceEqual("RIFF"u8) &&
            bytes[8..12].SequenceEqual("WEBP"u8))
            return "image/webp";

        return null;
    }
}
