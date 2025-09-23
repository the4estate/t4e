using T4E.App.Abstractions.Ports;

namespace T4E.Infrastructure
{
    // XorShift32 for zero-alloc + deterministic
    public sealed class DeterministicRandom : IRandom
    {
        private uint _state = 2463534242u;
        public void Reseed(int seed) { _state = (uint)seed == 0 ? 1u : (uint)seed; }
        private uint NextUInt() { uint x = _state; x^=x<<13; x^=x>>17; x^=x<<5; _state=x; return x; }
        public int NextInt() { return (int)(NextUInt() & 0x7FFFFFFF); }
        public int NextInt(int minInclusive, int maxExclusive)
        { var span = (uint)(maxExclusive - minInclusive); return minInclusive + (int)(NextUInt() % span); }
        public float NextFloat() { return (NextUInt() & 0xFFFFFF) / (float)0x1000000; }
    }
}
