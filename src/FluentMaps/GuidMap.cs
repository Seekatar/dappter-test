using Dapper.FluentMap.Dommel.Mapping;

class GuidMap : DommelEntityMap<ParentWithGuid>
{
    public GuidMap()
    {
        ToTable("GuidKey");
    }
}
