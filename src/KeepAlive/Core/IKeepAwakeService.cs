namespace KeepAlive.Core;

public interface IKeepAwakeService
{
    bool IsActive { get; }

    void Start();

    void Stop();
}
