namespace HBAbstractions;

public interface IRigControlClient
{
    public interface IHamLibRigCtlClient
    {
        Task OpenAsync();
        Task SetFreqAsync(long freq);
        Task SetModeAsync(string mode);
        Task SendCommandAsync(string command);
        void Dispose();
    }
}
