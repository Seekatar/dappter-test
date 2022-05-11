class ParentWithGuid : IEquatable<ParentWithGuid>
{
    public Guid Id { get; set; }
    public string S { get; set; } = "";
    public int I { get; set; }

    public IList<Child> Children { get; set; } = new List<Child>();

    public bool Equals(ParentWithGuid? other)
    {
        return Id == (other?.Id ?? Guid.Empty);
    }
}
