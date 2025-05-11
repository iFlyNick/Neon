namespace Neon.Persistence.Worker.Scripts;

public interface IDataGenerator
{
    Task PreloadDbData(CancellationToken ct = default);
}