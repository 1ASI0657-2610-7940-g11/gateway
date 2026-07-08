using Fuel.Reporting.Service.Features.Company.Domain;

namespace Fuel.Reporting.Service.Features.Company.Data;

public class InMemoryCompanyRepository : ICompanyRepository
{
    private readonly List<CompanyDetail> _companies = new()
    {
        new CompanyDetail
        {
            Id = "COM-001",
            Name = "Constructora Andina S.A.C.",
            Ruc = "20548796321",
            ContactName = "María Fernández",
            ContactEmail = "maria.fernandez@constructoraandina.pe",
            Phone = "+51 987 654 321",
            Address = "Av. Javier Prado Este 4200, Santiago de Surco, Lima",
            Status = "Active",
            TotalOrders = 3,
            TotalSpent = 92550.00,
            OrderHistory = new List<CompanyOrderHistoryItem>
            {
                new CompanyOrderHistoryItem
                {
                    OrderId = "ORD-001",
                    Code = "FT-2025-0001",
                    Status = "OnRoute",
                    FuelType = "Diesel B5",
                    QuantityGallons = 3500,
                    Date = "01/12/2025",
                    Amount = 48750.00
                },
                new CompanyOrderHistoryItem
                {
                    OrderId = "ORD-002",
                    Code = "FT-2025-0002",
                    Status = "Delivered",
                    FuelType = "Gasohol 95",
                    QuantityGallons = 2000,
                    Date = "25/11/2025",
                    Amount = 26800.00
                },
                new CompanyOrderHistoryItem
                {
                    OrderId = "ORD-003",
                    Code = "FT-2025-0003",
                    Status = "Scheduled",
                    FuelType = "Diesel B5",
                    QuantityGallons = 5000,
                    Date = "30/11/2025",
                    Amount = 17000.00
                }
            }
        },
        new CompanyDetail
        {
            Id = "COM-002",
            Name = "Transportes del Sur E.I.R.L.",
            Ruc = "20457896541",
            ContactName = "Luis Mendoza",
            ContactEmail = "operaciones@transportesdelsur.pe",
            Phone = "+51 956 321 478",
            Address = "Carretera Panamericana Sur km 32, Lurín, Lima",
            Status = "Active",
            TotalOrders = 2,
            TotalSpent = 53600.00,
            OrderHistory = new List<CompanyOrderHistoryItem>
            {
                new CompanyOrderHistoryItem
                {
                    OrderId = "ORD-004",
                    Code = "FT-2025-0004",
                    Status = "Delivered",
                    FuelType = "Diesel B5",
                    QuantityGallons = 3000,
                    Date = "18/11/2025",
                    Amount = 40200.00
                },
                new CompanyOrderHistoryItem
                {
                    OrderId = "ORD-005",
                    Code = "FT-2025-0005",
                    Status = "Cancelled",
                    FuelType = "Gasohol 90",
                    QuantityGallons = 1000,
                    Date = "15/11/2025",
                    Amount = 13400.00
                }
            }
        }
    };

    public Task<CompanyDetail?> GetCompanyDetailAsync(string id)
    {
        var company = _companies.FirstOrDefault(c =>
            c.Id.Equals(id, StringComparison.OrdinalIgnoreCase) ||
            c.Ruc.Equals(id, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(company);
    }
}