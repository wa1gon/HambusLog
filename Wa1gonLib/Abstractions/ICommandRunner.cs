using System.Threading;
using System.Threading.Tasks;

namespace Wa1gonLib.Abstractions;

public interface ICommandRunner
{
    Task<CommandResult> RunAsync(string fileName, string arguments, CancellationToken cancellationToken = default);
}

public sealed record CommandResult(int ExitCode, string StandardOutput, string StandardError);

