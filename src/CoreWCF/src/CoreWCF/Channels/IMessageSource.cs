using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CoreWCF.Channels
{
    internal enum AsyncReceiveResult
    {
        Completed,
        Pending,
    }

    internal interface IMessageSource
    {
        Task<Message> ReceiveAsync(CancellationToken token);
        Task<bool> WaitForMessageAsync(CancellationToken token);
    }
}
