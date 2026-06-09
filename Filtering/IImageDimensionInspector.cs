namespace YedekParcaPortal.ImageTools.Filtering;

/// <summary>
/// İndirilen ham baytların gerçek piksel boyutunu okur (Image.Identify).
/// </summary>
public interface IImageDimensionInspector
{
    /// <returns>Boyut okunamazsa null.</returns>
    (int Width, int Height)? TryIdentify(ReadOnlySpan<byte> imageBytes);
}
