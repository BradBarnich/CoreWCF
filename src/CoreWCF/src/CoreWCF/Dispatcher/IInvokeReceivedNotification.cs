using CoreWCF.Channels;

namespace CoreWCF.Dispatcher
{
    internal interface IInvokeReceivedNotification
    {
        void NotifyInvokeReceived();
        void NotifyInvokeReceived(RequestContext request);
    }
}