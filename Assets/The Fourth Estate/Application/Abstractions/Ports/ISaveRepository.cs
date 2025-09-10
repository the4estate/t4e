namespace T4E.App.Abstractions
{
#nullable enable
    public interface ISaveRepository
    {
        void Save(SaveBlob save);
        SaveBlob? Load();
    }
}
