using Dapper.FluentMap.Dommel.Mapping;

class IntMap : DommelEntityMap<ParentWithInt>
{
    public IntMap()
    {
        ToTable("IntKey");
    }
}