namespace AsaSavegameToolkit.Plumbing.Properties;

/// <summary>
/// Strongly-typed property with generic value (no boxing for value types).
/// </summary>
public abstract class Property<T> : Property where T : notnull
{
    /// <summary>
    /// The parsed property value.
    /// Type T avoids boxing for value types (int, float, double, etc.).
    /// </summary>
    public required T Value { get; set; }

    public override object GetValue() => Value;

    public override string ToString() => $"{Tag}: {Value}";
}
