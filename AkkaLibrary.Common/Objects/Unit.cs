namespace AkkaLibrary
{
    /// <summary>
    /// Enumeration that defines a number of Units
    /// </summary>
    public enum Unit
    {
        // Default is NotSet which should be an error value
        NotSet,

        // When no unit is applicable
        None,
        
        // SI Units
        Metres,
        Kilograms,
        Seconds,
    }
}