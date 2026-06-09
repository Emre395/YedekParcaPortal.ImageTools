using SixLabors.ImageSharp;

namespace YedekParcaPortal.ImageTools.Filtering;

public sealed class ImageSharpDimensionInspector : IImageDimensionInspector
{
    public (int Width, int Height)? TryIdentify(ReadOnlySpan<byte> imageBytes)
    {
        try
        {
            var info = Image.Identify(imageBytes);
            if (info == null) return null;
            return (info.Width, info.Height);
        }
        catch
        {
            return null;
        }
    }
}
