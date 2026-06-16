using System.Net.Http;
using System.Web.Mvc;
using Autofac;
using Autofac.Integration.Mvc;
using LuxuryCar.Data;
using LuxuryCar.Infrastructure;
using LuxuryCar.Services;
using Owin;

namespace LuxuryCar.Infrastructure.Startup
{
    public static class AutofacConfig
    {
        private static IContainer _container;

        public static void Register()
        {
            var builder = CreateBuilder();
            _container = builder.Build();
            DependencyResolver.SetResolver(new AutofacDependencyResolver(_container));
        }

        public static void ConfigureOwin(IAppBuilder app)
        {
            if (_container == null)
            {
                Register();
            }
        }

        private static ContainerBuilder CreateBuilder()
        {
            var builder = new ContainerBuilder();
            builder.RegisterControllers(typeof(MvcApplication).Assembly);

            builder.RegisterType<ApplicationDbContext>().InstancePerRequest();
            builder.RegisterType<AppSettingService>().As<IAppSettingService>().InstancePerRequest();
            builder.RegisterType<BookingNumberService>().As<IBookingNumberService>().InstancePerRequest();
            builder.RegisterType<QuoteService>().As<IQuoteService>().InstancePerRequest();
            builder.RegisterType<EmailService>().As<IEmailService>().InstancePerRequest();
            builder.RegisterType<CloudinaryMediaStorageService>().As<IMediaStorageService>().InstancePerRequest();

            builder.RegisterType<AppConfiguration>().As<IAppConfiguration>().SingleInstance();
            builder.RegisterType<MemoryRuntimeCache>().As<IRuntimeCache>().SingleInstance();
            builder.RegisterType<DefaultHttpClientFactory>().As<IHttpClientFactory>().SingleInstance();
            builder.RegisterGeneric(typeof(TraceLogger<>)).As(typeof(IAppLogger<>)).SingleInstance();

            return builder;
        }
    }
}
