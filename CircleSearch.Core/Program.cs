class Program
{
    public static Bootstrap bootstrap;

    static void Main()
    {
        bootstrap = new Bootstrap();

        OnStated();

        OnClosed();
    }

    private static void OnStated()
    {
        bootstrap.OnStarted();
    }

    public static void OnClosed()
    {
        bootstrap.OnStopped();
    }
}