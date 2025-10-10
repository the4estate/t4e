using T4E.Bootstrap.Installers;

namespace T4E.Bootstrap.Installers
{
    public static class InfrastructureInstaller
    {
        // Existing call sites keep using this
        public static CoreContainer MakeContainer(string newsPath, string sourcesPath, int seed)
            => new CoreContainer(newsPath, sourcesPath, seed);

        // New overload that also wires leads.json when we start using it
        public static CoreContainer MakeContainer(string newsPath, string sourcesPath, string leadsPath, int seed)
            => new CoreContainer(newsPath, sourcesPath, leadsPath, seed);
    }
}
