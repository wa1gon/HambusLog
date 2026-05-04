namespace HamBusLog.Data;

[Obsolete("Use HamBusLog.Wa1gonLib.Exchange.JadeTransferService instead.")]
public static class JadeTransferService
{
    public static Task ExportExampleToFileAsync(
        string filePath,
        CancellationToken cancellationToken = default)
        => HamBusLog.Wa1gonLib.Exchange.JadeTransferService.ExportExampleToFileAsync(filePath, cancellationToken);

    public static Task ExportSchemaToFileAsync(
        string filePath,
        CancellationToken cancellationToken = default)
        => HamBusLog.Wa1gonLib.Exchange.JadeTransferService.ExportSchemaToFileAsync(filePath, cancellationToken);

    public static Task<int> ExportToFileAsync(
        string filePath,
        AdifImportOptions? options = null,
        CancellationToken cancellationToken = default)
        => HamBusLog.Wa1gonLib.Exchange.JadeTransferService.ExportToFileAsync(filePath, options, cancellationToken);

    public static Task<int> ImportFromFileAsync(
        string filePath,
        AdifImportOptions? options = null,
        CancellationToken cancellationToken = default)
        => HamBusLog.Wa1gonLib.Exchange.JadeTransferService.ImportFromFileAsync(filePath, options, cancellationToken);
}

