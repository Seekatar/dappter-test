using Dapper.FluentMap.Dommel.Mapping;

class ClientMap : DommelEntityMap<Client>
{
    public ClientMap()
    {
        ToTable("Client");
        Map(p => p.ClientId).IsKey();
    }
}
