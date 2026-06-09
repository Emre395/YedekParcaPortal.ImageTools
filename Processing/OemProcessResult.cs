namespace YedekParcaPortal.ImageTools.Processing;

public enum OemProcessStatus
{
    SkippedAlreadyExists,
    Saved,
    NotFound,
    Failed
}

public sealed record OemProcessResult(
    string OemCode,
    OemProcessStatus Status,
    string? SavedPath = null,
    string? Message = null);
