using Autofac;
using AutoMapper;
using FiveDegrees.Messages.Orchestrator;
using FiveDegrees.Messages.Orchestrator.Interfaces;
using FiveDegrees.Messages.ProcessManager;
using FiveDegrees.Messages.ProcessManager.v2;
using ProcessManager.Domain.Commands;
using ProcessManager.Domain.Events;
using ProcessManager.Domain.Interfaces;
using ProcessManager.Domain.Models;
using ProcessManager.Domain.Models.Reporting;
using ProcessManager.Infrastructure.Models;
using Rebus.Pipeline;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ProcessManager.Domain.DomainEvents;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace ProcessManager.BackgroundWorker.Modules
{
    public class AutoMapperModule : Autofac.Module
    {
        private const string _xUserIdHeader = "x-user-id";
        private const string _xExternalIdHeader = "x-external-id";
        private const string _succeededStatus = "succeeded";
        private const string _completedStatus = "completed";

        private readonly Assembly _profileAssemblies;
        private readonly IContextAccessor _contextAccessor;

        public AutoMapperModule(Assembly profileAssemblies, IContextAccessor contextAccessor)
        {
            _profileAssemblies = profileAssemblies;
            _contextAccessor = contextAccessor;
        }

        protected override void Load(ContainerBuilder builder)
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddMaps(_profileAssemblies);

                cfg.CreateMap<StartActivityMsg, StartActivityCommand>()
                    .ForCtorParam("operationId", opt => opt.MapFrom(_ => _contextAccessor.GetRequestId()));

                cfg.CreateMap<StartActivityMsgV2, StartActivityCommand>()
                    .ForCtorParam("operationId", opt => opt.MapFrom(_ => _contextAccessor.GetRequestId()));

                cfg.CreateMap<EndActivityMsg, EndActivityCommand>()
                    .ForCtorParam("status", opt => opt.MapFrom(src => ChangeStatus(src.Status)));

                cfg.CreateMap<EndActivityMsgV2, EndActivityCommand>()
                    .ForCtorParam("status", opt => opt.MapFrom(src => ChangeStatus(src.Status)));

                cfg.CreateMap<UpdateProcessStatusMsg, UpdateProcessStatusCommand>()
                    .ForCtorParam("status", opt => opt.MapFrom(src => ChangeStatus(src.Status)))
                    .ForCtorParam("operationId", opt => opt.MapFrom(_ => _contextAccessor.GetRequestId()));
                    

                cfg.CreateMap<UpdateProcessStatusMsgV2, UpdateProcessStatusCommand>()
                    .ForCtorParam("status", opt => opt.MapFrom(src => ChangeStatus(src.Status)))
                    .ForCtorParam("operationId", opt => opt.MapFrom(_ => _contextAccessor.GetRequestId()));

                cfg.CreateMap<UpdateActivityMsg, UpdateActivityCommand>()
                    .ForCtorParam("status", opt => opt.MapFrom(src => ChangeStatus(src.Status)));

                cfg.CreateMap<UpdateActivityMsgV2, UpdateActivityCommand>()
                    .ForCtorParam("status", opt => opt.MapFrom(src => ChangeStatus(src.Status)));

                cfg.CreateMap<IStartProcessMsg, StartProcessCommand>()
                    .ConstructUsing(src => new StartProcessCommand(
                            src.ProcessKey,
                            src.ProcessName,
                            GetIdentity(),
                            src,
                            src.OperationId,
                            GetRelations(src.EntityRelations),
                            null
                        ));

                cfg.CreateMap<IStartProcessMsgV2, StartProcessCommand>()
                    .ConstructUsing(src => new StartProcessCommand(
                            src.ProcessKey,
                            src.ProcessName,
                            GetIdentity(),
                            src,
                            src.OperationId,
                            GetRelations(src.EntityRelations),
                            null
                        ));

                cfg.CreateMap<IStartProcessMsgV3, StartProcessCommand>()
                    .ConstructUsing(src => new StartProcessCommand(
                        src.ProcessKey,
                        src.ProcessName,
                        GetIdentity(),
                        src,
                        src.OperationId,
                        GetRelations(src.EntityRelations),
                        src.EnvironmentShortName
                    ));

                cfg.CreateMap<JObject, StartProcessCommand>()
                    .ConstructUsing(src => new StartProcessCommand(
                        src.GetValue("processKey", StringComparison.OrdinalIgnoreCase).ToString(),
                        src.GetValue("processName", StringComparison.OrdinalIgnoreCase).ToString(),
                        GetIdentity(),
                        JToken.FromObject(src),
                        Guid.Parse(src.GetValue("operationId", StringComparison.OrdinalIgnoreCase).ToString()),
                        src["entityRelations"] == null ? null : GetRelations(JsonConvert.DeserializeObject<IEnumerable<EntityRelation>>(src.GetValue("entityRelations", StringComparison.OrdinalIgnoreCase).ToString())),
                        TryGetEnvironmentShortName(src.GetValue("environmentShortName", StringComparison.OrdinalIgnoreCase))
                    ));

                cfg.CreateMap<StartActivityDomainSucceeded, StartActivitySucceeded>()
                    .ConstructUsing(src => new StartActivitySucceeded(
                        _contextAccessor.GetCorrelationId(),
                        _contextAccessor.GetRequestId(),
                        _contextAccessor.GetCommandId(),
                        src.Activity.OperationId,
                        src.Activity.ActivityId,
                        src.Activity.Name,
                        src.Activity.Status,
                        src.Activity.StartDate,
                        src.Activity.URI
                    ));

                cfg.CreateMap<(StartActivityMsg startActivityMsg, string message), StartActivityDomainFailed>()
                    .ConstructUsing(x => new StartActivityDomainFailed
                    (
                        x.startActivityMsg.CorrelationId,
                        x.startActivityMsg.ActivityId,
                        x.startActivityMsg.OperationId,
                        x.startActivityMsg.Name,
                        x.startActivityMsg.StartDate,
                        x.startActivityMsg.URI,
                        new Domain.Models.ErrorData(x.message, "", "process")
                    ));

                cfg.CreateMap<(StartActivityMsgV2 startActivityMsg, string message), StartActivityDomainFailed>()
                    .ConstructUsing(x => new StartActivityDomainFailed
                    (
                        _contextAccessor.GetRequestId(),
                        x.startActivityMsg.ActivityId,
                        x.startActivityMsg.OperationId,
                        x.startActivityMsg.Name,
                        x.startActivityMsg.StartDate,
                        x.startActivityMsg.URI,
                        new Domain.Models.ErrorData(x.message, "", "process")
                    ));

                cfg.CreateMap<StartActivityDomainFailed, StartActivityFailed>()
                    .ForCtorParam("requestId", opt => opt.MapFrom(_ => _contextAccessor.GetRequestId()))
                    .ForCtorParam("commandId", opt => opt.MapFrom(_ => _contextAccessor.GetCommandId()));

                cfg.CreateMap<UpdateActivityDomainSucceeded, UpdateActivitySucceeded>()
                    .ConstructUsing(src => new UpdateActivitySucceeded(
                        _contextAccessor.GetCorrelationId(),
                        _contextAccessor.GetRequestId(),
                        _contextAccessor.GetCommandId(),
                        src.Activity.ActivityId,
                        src.Activity.Name,
                        src.Activity.Status,
                        src.Activity.EndDate,
                        src.Activity.URI
                    ));

                cfg.CreateMap<(UpdateActivityMsg updateActivityMsg, string message), UpdateActivityDomainFailed>()
                    .ConstructUsing(x => new UpdateActivityDomainFailed
                    (
                        x.updateActivityMsg.CorrelationId,
                        x.updateActivityMsg.ActivityId,
                        x.updateActivityMsg.Status,
                        x.updateActivityMsg.URI,
                        new Domain.Models.ErrorData(x.message, "", "process")
                    ));

                cfg.CreateMap<(UpdateActivityMsgV2 updateActivityMsg, string message), UpdateActivityDomainFailed>()
                    .ConstructUsing(x => new UpdateActivityDomainFailed
                    (
                        _contextAccessor.GetRequestId(),
                        x.updateActivityMsg.ActivityId,
                        x.updateActivityMsg.Status,
                        x.updateActivityMsg.URI,
                        new Domain.Models.ErrorData(x.message, "", "process")
                    ));

                cfg.CreateMap<UpdateActivityDomainFailed, UpdateActivityFailed>()
                    .ForCtorParam("requestId", opt => opt.MapFrom(_ => _contextAccessor.GetRequestId()))
                    .ForCtorParam("commandId", opt => opt.MapFrom(_ => _contextAccessor.GetCommandId()));

                cfg.CreateMap<EndActivityDomainSucceeded, EndActivitySucceeded>()
                    .ConstructUsing(src => new EndActivitySucceeded(
                        _contextAccessor.GetCorrelationId(),
                        _contextAccessor.GetRequestId(),
                        _contextAccessor.GetCommandId(),
                        src.Activity.ActivityId,
                        src.Activity.Name,
                        src.Activity.Status,
                        src.Activity.EndDate,
                        src.Activity.URI
                    ));

                cfg.CreateMap<(EndActivityMsg endActivityMsg, string message), EndActivityDomainFailed>()
                    .ConstructUsing(x => new EndActivityDomainFailed
                    (
                        x.endActivityMsg.CorrelationId,
                        x.endActivityMsg.ActivityId,
                        x.endActivityMsg.Status,
                        x.endActivityMsg.EndDate,
                        x.endActivityMsg.URI,
                        new Domain.Models.ErrorData(x.message, "", "process")
                    ));

                cfg.CreateMap<(EndActivityMsgV2 endActivityMsg, string message), EndActivityDomainFailed>()
                    .ConstructUsing(x => new EndActivityDomainFailed
                    (
                        _contextAccessor.GetRequestId(),
                        x.endActivityMsg.ActivityId,
                        x.endActivityMsg.Status,
                        x.endActivityMsg.EndDate,
                        x.endActivityMsg.URI,
                        new Domain.Models.ErrorData(x.message, "", "process")
                    ));

                cfg.CreateMap<EndActivityDomainFailed, EndActivityFailed>()
                    .ForCtorParam("requestId", opt => opt.MapFrom(_ => _contextAccessor.GetRequestId()))
                    .ForCtorParam("commandId", opt => opt.MapFrom(_ => _contextAccessor.GetCommandId()));

                cfg.CreateMap<UpdateProcessStatusDomainSucceeded, UpdateProcessStatusSucceeded>()
                    .ConstructUsing(src => new UpdateProcessStatusSucceeded(
                        _contextAccessor.GetCorrelationId(),
                        _contextAccessor.GetRequestId(),
                        _contextAccessor.GetCommandId(),
                        src.WorkflowRun.OperationId
                    ));

                cfg.CreateMap<(UpdateProcessStatusMsg updateProcessStatusMsg, string message), UpdateProcessStatusDomainFailed>()
                    .ConstructUsing(x => new UpdateProcessStatusDomainFailed
                    (
                        x.updateProcessStatusMsg.CorrelationId,
                        x.updateProcessStatusMsg.OperationId,
                        new Domain.Models.ErrorData(x.message, "", "process")
                    ));

                cfg.CreateMap<(UpdateProcessStatusMsgV2 updateProcessStatusMsg, string message), UpdateProcessStatusDomainFailed>()
                    .ConstructUsing(x => new UpdateProcessStatusDomainFailed
                    (
                        _contextAccessor.GetRequestId(),
                        x.updateProcessStatusMsg.OperationId,
                        new Domain.Models.ErrorData(x.message, "", "process")
                    ));

                cfg.CreateMap<UpdateProcessStatusDomainFailed, UpdateProcessStatusFailed>()
                    .ForCtorParam("requestId", opt => opt.MapFrom(_ => _contextAccessor.GetRequestId()))
                    .ForCtorParam("commandId", opt => opt.MapFrom(_ => _contextAccessor.GetCommandId()));

                cfg.CreateMap<StartProcessDomainSucceeded, StartProcessSucceeded>()
                    .ConstructUsing(src => new StartProcessSucceeded(
                        _contextAccessor.GetCorrelationId(),
                        _contextAccessor.GetRequestId(),
                        _contextAccessor.GetCommandId(),
                        src.WorkflowRun.OperationId,
                        src.WorkflowRun.WorkflowRunName
                    ));

                cfg.CreateMap<(IStartProcessMsg startProcessMsg, string message), StartProcessDomainFailed>()
                    .ConstructUsing(x => new StartProcessDomainFailed
                    (
                        x.startProcessMsg.CorrelationId,
                        x.startProcessMsg.OperationId,
                        x.startProcessMsg.ProcessName,
                        x.message,
                        new Domain.Models.ErrorData(x.message, "", "process")
                    ));
                cfg.CreateMap<(IStartProcessMsgV2 startProcessMsg, string message), StartProcessDomainFailed>()
                    .ConstructUsing(x => new StartProcessDomainFailed
                    (
                        _contextAccessor.GetRequestId(), 
                        x.startProcessMsg.OperationId,
                        x.startProcessMsg.ProcessName,
                        x.message,
                        new Domain.Models.ErrorData(x.message, "", "process")
                    ));
                cfg.CreateMap<(IStartProcessMsgV3 startProcessMsg, string message), StartProcessDomainFailed>()
                    .ConstructUsing(x => new StartProcessDomainFailed
                    (
                        _contextAccessor.GetRequestId(),
                        x.startProcessMsg.OperationId,
                        x.startProcessMsg.ProcessName,
                        x.message,
                        new Domain.Models.ErrorData(x.message, "", "process")
                    ));

                cfg.CreateMap<(JObject startProcessMsg, string message), StartProcessDomainFailed>()
                    .ConstructUsing(x => new StartProcessDomainFailed(
                        _contextAccessor.GetRequestId(),
                        Guid.Parse(x.startProcessMsg.GetValue("operationId", StringComparison.OrdinalIgnoreCase).ToString()),
                        x.startProcessMsg.GetValue("processName", StringComparison.OrdinalIgnoreCase).ToString(),
                        x.message,
                        new Domain.Models.ErrorData(x.message, "", "process")
                    ));

                cfg.CreateMap<StartProcessDomainFailed, StartProcessFailed>()
                    .ForCtorParam("requestId", opt => opt.MapFrom(_ => _contextAccessor.GetRequestId()))
                    .ForCtorParam("commandId", opt => opt.MapFrom(_ => _contextAccessor.GetCommandId()));

                cfg.CreateMap<OperationDomainCompleted, ProcessFailed>()
                    .ForCtorParam("correlationId", opt => opt.MapFrom(_ => _contextAccessor.GetCorrelationId()))
                    .ForCtorParam("requestId", opt => opt.MapFrom(_ => _contextAccessor.GetRequestId()))
                    .ForCtorParam("commandId", opt => opt.MapFrom(_ => _contextAccessor.GetCommandId()));

                cfg.CreateMap<OperationDomainCompleted, ProcessSucceeded>()
                    .ForCtorParam("correlationId", opt => opt.MapFrom(_ => _contextAccessor.GetCorrelationId()))
                    .ForCtorParam("requestId", opt => opt.MapFrom(_ => _contextAccessor.GetRequestId()))
                    .ForCtorParam("commandId", opt => opt.MapFrom(_ => _contextAccessor.GetCommandId()));

                cfg.CreateMap<OperationDomainFailed, ProcessFailed>()
                    .ForCtorParam("correlationId", opt => opt.MapFrom(_ => _contextAccessor.GetCorrelationId()))
                    .ForCtorParam("requestId", opt => opt.MapFrom(_ => _contextAccessor.GetRequestId()))
                    .ForCtorParam("commandId", opt => opt.MapFrom(_ => _contextAccessor.GetCommandId()));

                cfg.CreateMap<FiveDegrees.Messages.ProcessManager.ErrorData, Domain.Models.ErrorData>();

                cfg.CreateMap<ReportingProcessManagerMsg, CreateReportCommand>();

                cfg.CreateMap<ActivityDbo, ActivityReport>();
                cfg.CreateMap<RelationDbo, RelationReport>();
                cfg.CreateMap<WorkflowRelationDbo, WorkflowRelationReport>();
                cfg.CreateMap<WorkflowRunDbo, WorkflowRunReport>();

                cfg.CreateMap<ActivityDbo, Activity>().ReverseMap();
                cfg.CreateMap<WorkflowRunDbo, WorkflowRun>().ReverseMap();
                cfg.CreateMap<RelationDbo, Relation>().ReverseMap();
                cfg.CreateMap<OutboxMessageDbo, OutboxMessage>().ReverseMap();

                cfg.CreateMap<InsertWorkflowRunMsg, InsertWorkflowRunCommand>();
                cfg.CreateMap<UnorchestratedRunDbo, UnorchestratedRun>().ReverseMap();

                cfg.CreateMap<(InsertWorkflowRunMsg insertWorkflowRunMsg, string message), InsertWorkflowRunDomainFailed>()
                    .ConstructUsing(x => new InsertWorkflowRunDomainFailed
                    (
                        _contextAccessor.GetRequestId(),
                        new Domain.Models.ErrorData(x.message, "", "process")
                    ));

                cfg.CreateMap<InsertWorkflowRunDomainFailed, InsertWorkflowRunFailed>()
                    .ForCtorParam("correlationId", opt => opt.MapFrom(_ => _contextAccessor.GetCorrelationId()))
                    .ForCtorParam("requestId", opt => opt.MapFrom(_ => _contextAccessor.GetRequestId()))
                    .ForCtorParam("commandId", opt => opt.MapFrom(_ => _contextAccessor.GetCommandId()));

                cfg.CreateMap<InsertWorkflowRunDomainCompleted, InsertWorkflowRunSucceeded>()
                    .ForCtorParam("correlationId", opt => opt.MapFrom(_ => _contextAccessor.GetCorrelationId()))
                    .ForCtorParam("requestId", opt => opt.MapFrom(_ => _contextAccessor.GetRequestId()))
                    .ForCtorParam("commandId", opt => opt.MapFrom(_ => _contextAccessor.GetCommandId()));

                cfg.CreateMap<InsertWorkflowRunDomainCompleted, InsertWorkflowRunSucceeded>()
                    .ConstructUsing(src => new InsertWorkflowRunSucceeded(
                        _contextAccessor.GetCorrelationId(),
                        _contextAccessor.GetRequestId(),
                        _contextAccessor.GetCommandId(),
                        src.UnorchestratedRun.OperationId,
                        src.UnorchestratedRun.UnorchestratedRunId,
                        src.UnorchestratedRun.EntityId,
                        src.UnorchestratedRun.WorkflowRunName,
                        src.UnorchestratedRun.WorkflowRunId
                    ));
            });

            builder.RegisterInstance(config).As<IConfigurationProvider>().ExternallyOwned();
            builder.RegisterType<Mapper>().As<IMapper>();
        }

        private string TryGetEnvironmentShortName(JToken token)
        {
            return token == null ? null : token.ToString();
        }

        private string ChangeStatus(string status) =>
            status.Equals(_succeededStatus, StringComparison.OrdinalIgnoreCase)
                ? _completedStatus
                : status;

        private List<Relation> GetRelations(IEnumerable<EntityRelation>? entityRelations)
        {
            var relations = entityRelations?.Select(x => new Relation
            {
                EntityId = x.EntityId,
                EntityType = x.EntityType
            }) ?? new List<Relation>();
            return relations.ToList();
        }

        private Guid GetIdentity()
        {
            var currentContext = MessageContext.Current;

            var createdBy = default(Guid);
            if (currentContext.Headers.TryGetValue(_xUserIdHeader, out var userId))
            {
                createdBy = Guid.Parse(userId);
            }
            // temporary fix until other teams start sending x-user-id
            if (currentContext.Headers.TryGetValue(_xExternalIdHeader, out var userId2))
            {
                createdBy = Guid.Parse(userId2);
            }

            return createdBy;
        }
    }
}
