using Dapper.FluentMap.Dommel.Mapping;

class ChildMap : DommelEntityMap<Child>
{
    public ChildMap()
    {
        ToTable("Children");
    }
}
