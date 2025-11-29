using Autofac;
using AutoMapper;
using ProcessManager.API.Models;
using ProcessManager.Domain.Models;
using ProcessManager.Infrastructure.Models;

namespace ProcessManager.API.Modules
{
    public class AutoMapperModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<WorkflowRun, WorkflowRunDto>()
                    .ForMember(dest => dest.CreatedDateTime, opt => opt.MapFrom(src => src.CreatedDate))
                    .ForMember(dest => dest.LastActionDateTime, opt => opt.MapFrom(src => src.ChangedDate));

                cfg.CreateMap<ActivityDbo, Activity>();
                cfg.CreateMap<WorkflowRunDbo, WorkflowRun>();
            });

            builder.RegisterInstance(config).As<IConfigurationProvider>().ExternallyOwned();
            builder.RegisterType<Mapper>().As<IMapper>();
        }
    }
}
