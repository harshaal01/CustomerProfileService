using CustomerProfileService.Domain.Entities;

namespace CustomerProfileService.Domain.Interfaces;

public interface ICustomerService
{
    Task<List<Customer>> GetAllAsync();
    Task CreateAsync(Customer customer);
    Task UpdateAsync(Customer customer);
    Task DeleteAsync(int id);
}
