using MediatR;
using System;

namespace ProcessManager.Domain.Commands
{
    public interface ICommand : IRequest
    {
        Guid CommandId { get; }
    }

    public interface ICommandHandler<in TCommand> : IRequestHandler<TCommand> where TCommand : class, ICommand
    {
    }

    public interface ICommand<out TResponse> : IRequest<TResponse>
    {
        Guid CommandId { get; }
    }

    public interface ICommandHandler<in TCommand, TResult> : IRequestHandler<TCommand, TResult> where TCommand : class, ICommand<TResult>
    {
    }
}
