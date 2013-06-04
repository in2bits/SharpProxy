namespace SharpProxy
{
    public interface IProxyListener
    {
        void Start();
        void Stop();
        bool IsListening { get; }
    }
}