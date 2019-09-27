namespace CoreWCF.Channels
{
    internal interface IHttpTransportFactorySettings : ITransportFactorySettings
    {
        int MaxBufferSize { get; }
        TransferMode TransferMode { get; }
        bool KeepAliveEnabled { get; set; }
        IAnonymousUriPrefixMatcher AnonymousUriPrefixMatcher { get; }
    }
}
