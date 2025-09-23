using System.Collections.Generic;
using T4E.Domain.Core.CET;

namespace T4E.App.Abstractions.Ports
{ 
    public interface IEffectApplier
    {
        /// <summary>
        /// Applies staged effects by delegating to IWorldCommands.Apply(effect).
        /// Returns the number of effects successfully applied.
        /// </summary>
        int Apply(IReadOnlyList<EffectInvocation> effects);
    }
}
