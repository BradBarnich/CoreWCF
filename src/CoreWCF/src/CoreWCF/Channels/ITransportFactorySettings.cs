namespace CoreWCF.Channels
{
    internal interface ITransportFactorySettings : IDefaultCommunicationTimeouts
    {
        bool ManualAddressing { get; }
        BufferManager BufferManager { get; }
        long MaxReceivedMessageSize { get; }
        MessageEncoderFactory MessageEncoderFactory { get; }
        MessageVersion MessageVersion { get; }
    }

    internal interface IHttpTransportFactorySettings : ITransportFactorySettings
    {
        int MaxBufferSize { get; }
        TransferMode TransferMode { get; }
        bool KeepAliveEnabled { get; set; }
        IAnonymousUriPrefixMatcher AnonymousUriPrefixMatcher { get; }
    }
}
