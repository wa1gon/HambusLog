namespace Wa1gonLib.Internal;

internal interface ICommandRunner
{
    Task<CommandResult> RunAsync(string fileName, string arguments, CancellationToken cancellationToken = default);
}

internal sealed record CommandResult(int ExitCode, string StandardOutput, string StandardError);

