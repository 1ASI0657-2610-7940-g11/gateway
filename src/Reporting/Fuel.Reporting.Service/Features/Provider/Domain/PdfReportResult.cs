namespace Fuel.Reporting.Service.Features.Provider.Domain;

public class PdfReportResult
{
    public string FileName { get; set; } = default!;
    public string ContentType { get; set; } = "application/pdf";
    public byte[] Content { get; set; } = Array.Empty<byte>();
}