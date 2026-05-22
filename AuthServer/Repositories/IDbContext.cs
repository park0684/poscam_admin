using System.Data;

namespace poscam.AuthServer.Repositories;

public interface IDbContext
{
    IDbConnection CreateConnection();
}
