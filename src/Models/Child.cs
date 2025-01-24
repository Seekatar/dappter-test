using System.Text.Json.Serialization;
using Dapper.Contrib.Extensions;

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

class ChildWithComputed : Child {
    public ChildWithComputed()
    {

    }
    public int C { get; set; }

    [Computed]
    public string? ParentName { get; set; } = "hi";

    [Computed]
    public int? Init { get; set; } = 123;
}

class ChildWithComputedConstructor : Child {
    public ChildWithComputedConstructor(Guid Id, Guid parentWithGuidId, string ChildName, Int32 C)
    {
        this.Id = Id;
        this.ParentWithGuidId = parentWithGuidId;
        this.ChildName = ChildName;
        this.C = C;
    }
    public int C { get; set; }

    [Computed]
    public string? ParentName { get; set; } = "hi";

    [Computed]
    public int? Init { get; set; } = 123;
}
