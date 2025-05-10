namespace Neon.Persistence.Scripts;

public interface IDataGenerator
{
    Task PreloadDbData(CancellationToken ct = default);
}