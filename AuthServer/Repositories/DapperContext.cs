using System.Data;
using MySqlConnector;

namespace poscam.AuthServer.Repositories;

public sealed class DapperContext : IDbContext
{
    private readonly string _connectionString;

    public DapperContext(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");
    }
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public IDbConnection CreateConnection()
    {
        return new MySqlConnection(_connectionString);
    }
}
