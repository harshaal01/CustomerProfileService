using CustomerProfileService.Domain.Entities;

public interface IAuthService
{
    Task RegisterAsync(User user);
    Task<User> LoginAsync(string email, string password);
}
