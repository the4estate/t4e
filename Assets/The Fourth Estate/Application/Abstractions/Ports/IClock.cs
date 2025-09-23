using System;
namespace T4E.App.Abstractions.Ports
{
    public interface IClock { DateTime UtcNow { get; } }
}
