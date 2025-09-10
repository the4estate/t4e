using System;
using System.Collections.Generic;
using T4E.App.Abstractions;            
using T4E.Domain.Core.CET;

namespace T4E.App.UseCases
{
    /// <summary>
    /// Thin adapter: forwards staged CET effects to the game's IWorldCommands.Apply(effect).
    /// All scheduling/state mutation is handled by your command layer.
    /// </summary>
    public sealed class EffectApplier : IEffectApplier
    {
        private readonly IWorldCommands _cmds;
        private readonly IAppLogger _log;

        public EffectApplier(IWorldCommands cmds, IAppLogger log)
        {
            _cmds = cmds;
            _log = log;
        }

        public int Apply(IReadOnlyList<EffectInvocation> effects)
        {
            int applied = 0;
            for (int i = 0; i < effects.Count; i++)
            {
                var inv = effects[i];
                try
                {
                    _cmds.Apply(inv.Effect);
                    applied++;
                }
                catch (Exception ex)
                {
                    _log.Error("EffectApplier failed for " + inv.SourceRule.EventId +
                               "#" + inv.SourceRule.RuleIndex + ": " + ex.Message);
                    // continue to next; if you prefer all-or-nothing,
                    // throw here instead and mark/idempotency only on success.
                }
            }
            return applied;
        }
    }
}
