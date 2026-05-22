using System.Data;

namespace poscam.AuthServer.Repositories;

/// <summary>
/// 모든 Repository의 공통 기반 클래스.
/// 
/// Repository에서 DB 연결을 매번 직접 생성하는 코드를 줄이기 위해 사용한다.
/// Service에서 트랜잭션을 직접 제어해야 하는 경우에는
/// 각 Repository 메서드에 IDbConnection, IDbTransaction을 직접 전달하는 방식도 함께 사용한다.
/// </summary>
public abstract class RepositoryBase
{
    protected readonly IDbContext DbContext;

    protected RepositoryBase(IDbContext dbContext)
    {
        DbContext = dbContext;
    }

    /// <summary>
    /// 단일 DB 작업을 실행할 때 사용하는 공통 메서드.
    /// 
    /// 이 메서드는 connection을 생성하고 작업 후 dispose한다.
    /// 단, 여러 Repository를 하나의 트랜잭션으로 묶어야 하는 경우에는
    /// Service에서 직접 connection과 transaction을 생성해야 한다.
    /// </summary>
    protected async Task<T> WithConnectionAsync<T>(Func<IDbConnection, Task<T>> action)
    {
        using var connection = DbContext.CreateConnection();
        return await action(connection);
    }

    /// <summary>
    /// 반환값이 없는 DB 작업을 실행할 때 사용하는 공통 메서드.
    /// </summary>
    protected async Task WithConnectionAsync(Func<IDbConnection, Task> action)
    {
        using var connection = DbContext.CreateConnection();
        await action(connection);
    }
}