namespace CircleSearch.Overlay;

public partial class App : Application
{
    public Bootstrap? bootstrap;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        bootstrap = new Bootstrap(e.Args, this);

        bootstrap.OnStarted();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        bootstrap?.OnStopped();
        
        base.OnExit(e);
    }
}