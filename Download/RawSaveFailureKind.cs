namespace YedekParcaPortal.ImageTools.Download;

/// <summary>
/// Ham kayıt denemesinin sonuç türü (Kademe 2 tetikleme kararı için).
/// </summary>
public enum RawSaveFailureKind
{
    None,
    NoCandidates,
    AllTransportErrors,
    QualityRejected
}
