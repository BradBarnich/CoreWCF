using CoreWCF.Channels.Framing;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CoreWCF.Channels
{
    internal interface IConnectionReuseHandler
    {
        Task<bool> ReuseConnectionAsync(FramingConnection connection, CancellationToken cancellationToken);
    }
}
