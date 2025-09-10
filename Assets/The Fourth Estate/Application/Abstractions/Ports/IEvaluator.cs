// Application/Abstractions/Ports/IEvaluator.cs
using System.Collections.Generic;
using T4E.Domain;
using T4E.Domain.Core.CET;

namespace T4E.App.Abstractions
{
    public interface IEvaluator
    {
        /// <summary>
        /// Returns the effects that should run, already deterministically ordered.
        /// Pure: must not mutate world.
        /// </summary>
        IReadOnlyList<EffectInvocation> Evaluate(TriggerContext trigger, in WorldSnapshot snapshot);
    }
}
