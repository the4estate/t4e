using UnityEngine;
using T4E.App.Abstractions;
namespace T4E.Infrastructure
{
    public sealed class UnityLogger : IAppLogger
    {
        public void Info(string m) => Debug.Log(m);
        public void Warn(string m) => Debug.LogWarning(m);
        public void Error(string m) => Debug.LogError(m);
    }
}
