using System;
using T4E.App.Abstractions;
using T4E.Domain.Core.CET;

namespace T4E.Infrastructure
{
    public sealed class TriggerBus : ITriggerBus
    {
        public event Action<TriggerContext> OnTriggered;

        public void Publish(TriggerContext ctx)
        {
            OnTriggered?.Invoke(ctx);
        }
    }
}
