using CryptoSim.Core.Entities;
using CryptoSim.Core.Interfaces;

public class UserRepository : IUserRepository
{
    // Implement all IUserRepository members here
    public Task<User> GetByIdAsync(Guid id)
    {
        throw new NotImplementedException();
    }

    public Task<User> GetByUsernameAsync(string username)
    {
        throw new NotImplementedException();
    }

    public Task<User> GetByEmailAsync(string email)
    {
        throw new NotImplementedException();
    }

    public Task<User> CreateAsync(User user)
    {
        throw new NotImplementedException();
    }

    public Task<User> UpdateAsync(User user)
    {
        throw new NotImplementedException();
    }

    public Task<bool> DeleteAsync(Guid id)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<User>> GetAllAsync()
    {
        throw new NotImplementedException();
    }

    public Task<decimal> GetBalanceAsync(Guid userId)
    {
        throw new NotImplementedException();
    }

    public Task UpdateBalanceAsync(Guid userId, decimal amount)
    {
        throw new NotImplementedException();
    }
}