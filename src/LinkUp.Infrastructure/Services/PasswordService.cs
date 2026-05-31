using LinkUp.Application.Common.Interfaces;
using BCryptNet = BCrypt.Net.BCrypt;

namespace LinkUp.Infrastructure.Services;

public class PasswordService : IPasswordService
{
    public string Hash(string password) => BCryptNet.HashPassword(password, workFactor: 12);
    public bool Verify(string password, string hash) => BCryptNet.Verify(password, hash);
}
