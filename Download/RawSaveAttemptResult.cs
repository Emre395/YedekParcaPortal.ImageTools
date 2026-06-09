namespace YedekParcaPortal.ImageTools.Download;

public sealed record RawSaveAttemptResult(string? SavedPath, RawSaveFailureKind FailureKind)
{
    public static RawSaveAttemptResult Saved(string path) =>
        new(path, RawSaveFailureKind.None);

    public static RawSaveAttemptResult Failed(RawSaveFailureKind kind) =>
        new(null, kind);
}
