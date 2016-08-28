using Nancy;

namespace mbDaemon
{
    internal class MyBootstrapper : DefaultNancyBootstrapper
    {
        //protected override void ConfigureApplicationContainer(TinyIoCContainer container)
        //{
        //    base.ConfigureApplicationContainer(container);
        //    ResourceViewLocationProvider.RootNamespaces.Add(GetType().Assembly, GetType().Assembly.GetName().Name + ".Views");

        //}

        //protected override NancyInternalConfiguration InternalConfiguration
        //{
        //    get { return NancyInternalConfiguration.WithOverrides(OnConfigurationBuilder); }
        //}

        //private void OnConfigurationBuilder(NancyInternalConfiguration x)
        //{
        //    x.ViewLocationProvider = typeof(_ResourceViewLocationProvider);

        //}
    }
}