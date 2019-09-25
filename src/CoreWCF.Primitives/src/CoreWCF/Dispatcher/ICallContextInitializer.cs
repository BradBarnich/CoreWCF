using CoreWCF.Channels;

namespace CoreWCF.Dispatcher
{
    public interface ICallContextInitializer
    {
        object BeforeInvoke(InstanceContext instanceContext, IClientChannel channel, Message message);
        void AfterInvoke(object correlationState);
    }
}