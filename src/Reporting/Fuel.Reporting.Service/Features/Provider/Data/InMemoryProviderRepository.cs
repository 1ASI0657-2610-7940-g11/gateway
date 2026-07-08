using System.Text;
using Fuel.Reporting.Service.Features.Provider.Domain;

namespace Fuel.Reporting.Service.Features.Provider.Data;

public class InMemoryProviderRepository : IProviderRepository
{
    private readonly List<SalesReportItem> _items = new()
    {
        new SalesReportItem
        {
            OrderCode = "FT-2026-0001",
            ClientName = "Constructora Andina S.A.C.",
            FuelType = "Diesel B5",
            QuantityGallons = 500,
            Amount = 7750m,
            Status = "Delivered",
            Date = "2026-06-01"
        },
        new SalesReportItem
        {
            OrderCode = "FT-2026-0002",
            ClientName = "Transportes Lima Sur",
            FuelType = "Gasohol 95",
            QuantityGallons = 320,
            Amount = 5600m,
            Status = "Scheduled",
            Date = "2026-06-03"
        },
        new SalesReportItem
        {
            OrderCode = "FT-2026-0003",
            ClientName = "Minería San Pedro",
            FuelType = "Diesel B5",
            QuantityGallons = 900,
            Amount = 13950m,
            Status = "Delivered",
            Date = "2026-06-05"
        },
        new SalesReportItem
        {
            OrderCode = "FT-2026-0004",
            ClientName = "Agroindustrial del Norte",
            FuelType = "GLP",
            QuantityGallons = 250,
            Amount = 3100m,
            Status = "Pending",
            Date = "2026-06-07"
        }
    };

    public Task<SalesReport> GetSalesReportAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var items = FilterByDate(fromDate, toDate).ToList();
        var report = new SalesReport
        {
            TotalSales = items.Sum(i => i.Amount),
            TotalOrders = items.Count,
            DeliveredOrders = items.Count(i => string.Equals(i.Status, "Delivered", StringComparison.OrdinalIgnoreCase)),
            PendingOrders = items.Count(i => string.Equals(i.Status, "Pending", StringComparison.OrdinalIgnoreCase)),
            TotalGallons = items.Sum(i => i.QuantityGallons),
            Period = BuildPeriod(fromDate, toDate),
            Items = items
        };
        return Task.FromResult(report);
    }

    public Task<IEnumerable<SalesChartPoint>> GetSalesChartAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var chart = FilterByDate(fromDate, toDate)
            .GroupBy(i => i.Date)
            .OrderBy(g => g.Key)
            .Select(g => new SalesChartPoint
            {
                Label = g.Key,
                TotalSales = g.Sum(i => i.Amount),
                TotalOrders = g.Count(),
                TotalGallons = g.Sum(i => i.QuantityGallons)
            });
        return Task.FromResult(chart);
    }

    public async Task<PdfReportResult> GetSalesReportPdfAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var report = await GetSalesReportAsync(fromDate, toDate);
        var content = BuildSimplePdf(report);
        return new PdfReportResult
        {
            FileName = $"fueltrack-sales-report-{DateTime.UtcNow:yyyyMMddHHmmss}.pdf",
            ContentType = "application/pdf",
            Content = content
        };
    }

    private IEnumerable<SalesReportItem> FilterByDate(DateTime? fromDate, DateTime? toDate)
    {
        return _items.Where(item =>
        {
            var itemDate = DateTime.Parse(item.Date);
            var matchesFrom = !fromDate.HasValue || itemDate.Date >= fromDate.Value.Date;
            var matchesTo = !toDate.HasValue || itemDate.Date <= toDate.Value.Date;
            return matchesFrom && matchesTo;
        });
    }

    private static string BuildPeriod(DateTime? fromDate, DateTime? toDate)
    {
        if (!fromDate.HasValue && !toDate.HasValue) return "All records";
        var from = fromDate?.ToString("yyyy-MM-dd") ?? "Start";
        var to = toDate?.ToString("yyyy-MM-dd") ?? "Today";
        return $"{from} to {to}";
    }

    private static byte[] BuildSimplePdf(SalesReport report)
    {
        var lines = new List<string>
        {
            "FuelTrack Sales Report",
            $"Period: {report.Period}",
            $"Total sales: S/ {report.TotalSales}",
            $"Total orders: {report.TotalOrders}",
            $"Delivered orders: {report.DeliveredOrders}",
            $"Pending orders: {report.PendingOrders}",
            $"Total gallons: {report.TotalGallons}",
            "",
            "Orders:"
        };
        lines.AddRange(report.Items.Select(item =>
            $"{item.OrderCode} - {item.ClientName} - {item.FuelType} - {item.QuantityGallons} gal - S/ {item.Amount} - {item.Status}"));

        var text = string.Join("\\n", lines).Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
        var stream = $"BT /F1 12 Tf 50 760 Td ({text}) Tj ET";
        var objects = new List<string>
        {
            "1 0 obj << /Type /Catalog /Pages 2 0 R >> endobj",
            "2 0 obj << /Type /Pages /Kids [3 0 R] /Count 1 >> endobj",
            "3 0 obj << /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >> endobj",
            "4 0 obj << /Type /Font /Subtype /Type1 /BaseFont /Helvetica >> endobj",
            $"5 0 obj << /Length {Encoding.ASCII.GetByteCount(stream)} >> stream\n{stream}\nendstream endobj"
        };

        var builder = new StringBuilder();
        builder.AppendLine("%PDF-1.4");
        var offsets = new List<int> { 0 };
        foreach (var obj in objects)
        {
            offsets.Add(Encoding.ASCII.GetByteCount(builder.ToString()));
            builder.AppendLine(obj);
        }
        var xrefOffset = Encoding.ASCII.GetByteCount(builder.ToString());
        builder.AppendLine("xref");
        builder.AppendLine($"0 {objects.Count + 1}");
        builder.AppendLine("0000000000 65535 f ");
        foreach (var offset in offsets.Skip(1))
            builder.AppendLine($"{offset:0000000000} 00000 n ");
        builder.AppendLine("trailer");
        builder.AppendLine($"<< /Size {objects.Count + 1} /Root 1 0 R >>");
        builder.AppendLine("startxref");
        builder.AppendLine(xrefOffset.ToString());
        builder.AppendLine("%%EOF");

        return Encoding.ASCII.GetBytes(builder.ToString());
    }
}