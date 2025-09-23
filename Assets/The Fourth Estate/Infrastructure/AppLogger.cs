using UnityEngine;
using T4E.App.Abstractions.Ports;
namespace T4E.Infrastructure
{
    public sealed class AppLogger : IAppLogger
    {
        public void Info(string m) => Debug.Log(m);
        public void Warn(string m) => Debug.LogWarning(m);
        public void Error(string m) => Debug.LogError(m);
    }
}
