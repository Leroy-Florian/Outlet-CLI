namespace Outlet.Registry.Resilience;

/// <summary>How the delay between retry attempts grows.</summary>
public enum ResilienceBackoff
{
    /// <summary>The same delay before every retry.</summary>
    Constant,

    /// <summary>The delay grows linearly with the attempt number.</summary>
    Linear,

    /// <summary>The delay doubles on each attempt (exponential growth).</summary>
    Exponential,
}
