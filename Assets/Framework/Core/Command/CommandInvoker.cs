using System;
using System.Collections.Generic;

namespace Framework.Core.Command
{
    public class CommandInvoker : ICommand
    {
        private readonly Queue<ICommand> _registerCommandQueue;
        private readonly Stack<ICommand> _invokedCommandStack = new();

        private CommandInvoker(Queue<ICommand> commandQueue)
        {
            _registerCommandQueue = commandQueue;
        }

        public void Execute()
        {
            while (_registerCommandQueue.TryDequeue(out var command))
            {
                command.Execute();
                _invokedCommandStack.Push(command);
            }
        }

        public void Undo()
        {
            while (_invokedCommandStack.TryPop(out var command))
            {
                command.Undo();
            }
        }

        public class Builder
        {
            private readonly Queue<ICommand> _tempCommandQueue = new();

            public Builder AppendCommand(ICommand command)
            {
                _tempCommandQueue.Enqueue(command);
                return this;
            }

            public CommandInvoker Build()
            {
                return new CommandInvoker(_tempCommandQueue);
            }
        }
    }
}