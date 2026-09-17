namespace KeepAlive.Core;

public interface IActivityInputSimulator
{
    bool IsActive { get; }

    void Start();

    void Stop();
}
