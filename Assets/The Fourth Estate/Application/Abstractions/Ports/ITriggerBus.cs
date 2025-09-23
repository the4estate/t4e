using System;
namespace T4E.App.Abstractions.Ports
{
    using T4E.Domain.Core.CET;

    /// <summary>
    /// Thin façade: adapters push TriggerContext here; CET engine subscribes.
    /// </summary>
    public interface ITriggerBus
    {
        event Action<TriggerContext> OnTriggered;
        void Publish(TriggerContext ctx);
    }
}
