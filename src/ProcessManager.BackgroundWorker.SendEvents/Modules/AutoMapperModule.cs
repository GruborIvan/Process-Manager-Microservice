using System.Reflection;
using Autofac;
using AutoMapper;
using ProcessManager.Domain.Models;
using ProcessManager.Infrastructure.Models;

namespace ProcessManager.BackgroundWorker.SendEvents.Modules
{
    public class AutoMapperModule : Autofac.Module
    {
        private readonly Assembly _profileAssemblies;

        public AutoMapperModule(Assembly profileAssemblies)
        {
            _profileAssemblies = profileAssemblies;
        }

        protected override void Load(ContainerBuilder builder)
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddMaps(_profileAssemblies);

                cfg.CreateMap<OutboxMessageDbo, OutboxMessage>().ReverseMap();
            });

            builder.RegisterInstance(config).As<IConfigurationProvider>().ExternallyOwned();
            builder.RegisterType<Mapper>().As<IMapper>();
        }
    }
}
