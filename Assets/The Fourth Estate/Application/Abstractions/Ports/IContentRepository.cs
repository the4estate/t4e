using System.Collections.Generic;
using T4E.Domain.Core.CET;
namespace T4E.App.Abstractions
{
#nullable enable
    public interface IContentRepository
    {
        Rule[] GetRulesByTrigger(TriggerType trigger);
        T? Load<T>(string id) where T : class;
       
        IEnumerable<T> LoadAll<T>() where T : class;
    }
}