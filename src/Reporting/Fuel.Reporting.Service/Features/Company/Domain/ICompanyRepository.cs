namespace Fuel.Reporting.Service.Features.Company.Domain;

public interface ICompanyRepository
{
    Task<CompanyDetail?> GetCompanyDetailAsync(string id);
}