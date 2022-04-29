/*
Copyright(c) 2009 - 2018, smtp4dev project contributors All rights reserved.
Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:
    * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
    * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
    * Neither the name of smtp4dev nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.
THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS;
OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
source: https://github.com/rnwood/smtp4dev/tree/master/Rnwood.Smtp4dev/Server
*/

using System.Collections.Concurrent;

namespace LocalSmtp.Server.Application.Services;

public class TaskQueue : Abstractions.ITaskQueue
{
    private readonly BlockingCollection<Action> processingQueue = new();
    private readonly BlockingCollection<Action> priorityProcessingQueue = new();

    public Task QueueTask(Action action, bool priority)
    {
        var tcs = new TaskCompletionSource<object>();

        Action wrapper = () =>
        {
            try
            {
                action();
                tcs.SetResult(null);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());

                tcs.SetException(e);
            }
        };


        if (priority)
        {
            priorityProcessingQueue.Add(wrapper);
        }
        else
        {
            processingQueue.Add(wrapper);
        }

        return tcs.Task;
    }

    private Task ProcessingTaskWork()
    {
        while (!processingQueue.IsCompleted && !priorityProcessingQueue.IsCompleted)
        {
            Action nextItem;
            try
            {
                BlockingCollection<Action>.TakeFromAny(new[] { priorityProcessingQueue, processingQueue }, out nextItem);
            }
            catch (InvalidOperationException)
            {
                if (processingQueue.IsCompleted || priorityProcessingQueue.IsCompleted)
                {
                    break;
                }

                throw;
            }

            nextItem();
        }

        return Task.CompletedTask;
    }

    public void Start()
    {
        Task.Run(ProcessingTaskWork);
    }
}