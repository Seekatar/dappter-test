using System.Text.Json.Serialization;

class Child : IEquatable<Child> {
    public Guid Id { get; set; }
    [JsonIgnore]
    public Guid ParentWithGuidId { get; set; }
    public string ChildName { get; set; } = "";

    public bool Equals(Child? other)
    {
        return Id == (other?.Id ?? Guid.Empty);
    }
}
