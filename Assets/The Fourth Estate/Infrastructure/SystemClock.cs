using System;
using T4E.App.Abstractions;
namespace T4E.Infrastructure { public sealed class SystemClock : IClock { public DateTime UtcNow => DateTime.UtcNow; } }
