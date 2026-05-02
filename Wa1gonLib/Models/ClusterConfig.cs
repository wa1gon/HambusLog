namespace HamBusLog.Wa1gonLib.Models;

/// <summary>
/// Configuration for connecting to and interacting with a DX cluster endpoint.
/// </summary>
public sealed class ClusterConfig
{
    /// <summary>
    /// DNS host name or IP address of the cluster server.
    /// </summary>
    public string Hostname { get; set; } = "127.0.0.1";

    /// <summary>
    /// TCP port used by the cluster server.
    /// </summary>
    public int TcpPort { get; set; } = 7300;

    /// <summary>
    /// Callsign used to authenticate/log in to the cluster.
    /// </summary>
    public string Callsign { get; set; } = string.Empty;

    /// <summary>
    /// Optional password sent to the cluster after connect.
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Optional command sent after login (for example, filters).
    /// </summary>
    public string Command { get; set; } = string.Empty;

    /// <summary>
    /// Maximum in-memory queue length for received spots/messages.
    /// </summary>
    public int QueueLength { get; set; } = 500;
}

