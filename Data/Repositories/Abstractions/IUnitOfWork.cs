using System.Threading;
using System.Threading.Tasks;

namespace HamBusLog.Data.Repositories.Abstractions;

public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

