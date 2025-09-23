namespace T4E.App.Abstractions.Ports
{
#nullable enable
    public interface ISaveRepository
    {
        void Save(SaveBlob save);
        SaveBlob? Load();
    }
}
