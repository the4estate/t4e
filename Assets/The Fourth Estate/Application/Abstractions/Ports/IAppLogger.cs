namespace T4E.App.Abstractions.Ports
{
    public interface IAppLogger
    {
        void Info(string msg);
        void Warn(string msg);
        void Error(string msg);
    }
}