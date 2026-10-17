using TransportPipes.Database.Entities;

namespace TransportPipes.Database.Repositories;

public class CalculationPackage
{
    public int GlobalConnectionId { get; private set; }
    public ProvidersAndConsumersRepository ProvidersAndConsumersRepository { get; private set; }
    public DbGameObject StartGameObject { get; private set; }
    public DateTime LastCalculatedDateTime { get; private set; }

    public CalculationPackage(int globalConnectionId, ProvidersAndConsumersRepository providersAndConsumersRepository, DbGameObject startGameObject, DateTime lastCalculatedDateTime)
    {
        this.GlobalConnectionId = globalConnectionId;
        this.ProvidersAndConsumersRepository = providersAndConsumersRepository;
        this.StartGameObject = startGameObject;
        this.LastCalculatedDateTime = lastCalculatedDateTime;
    }
}
