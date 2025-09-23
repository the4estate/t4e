namespace T4E.App.Abstractions.Ports
{
    public interface IRandom
    {
        void Reseed(int seed);
        int NextInt();
        int NextInt(int minInclusive, int maxExclusive);
        float NextFloat(); // 0..1
    }
}
