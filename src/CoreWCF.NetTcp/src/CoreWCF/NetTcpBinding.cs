// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;
using System.Xml;
using CoreWCF.Channels;
using CoreWCF.Xml;
using XmlDictionaryReaderQuotas = CoreWCF.Xml.XmlDictionaryReaderQuotas;

namespace CoreWCF
{
    public class NetTcpBinding : Binding
    {
        // private BindingElements
        private TcpTransportBindingElement _transport;
        private BinaryMessageEncodingBindingElement _encoding;
        private NetTcpSecurity _security = new NetTcpSecurity();

        public NetTcpBinding() { Initialize(); }
        public NetTcpBinding(SecurityMode securityMode)
            : this()
        {
            _security.Mode = securityMode;
        }

        public TransferMode TransferMode
        {
            get { return _transport.TransferMode; }
            set { _transport.TransferMode = value; }
        }

        public HostNameComparisonMode HostNameComparisonMode
        {
            get { return _transport.HostNameComparisonMode; }
            set { _transport.HostNameComparisonMode = value; }
        }

        [DefaultValue(TransportDefaults.MaxBufferPoolSize)]
        public long MaxBufferPoolSize
        {
            get { return _transport.MaxBufferPoolSize; }
            set
            {
                _transport.MaxBufferPoolSize = value;
            }
        }

        public int MaxBufferSize
        {
            get { return _transport.MaxBufferSize; }
            set { _transport.MaxBufferSize = value; }
        }

        public int MaxConnections
        {
            get { return _transport.MaxPendingConnections; }
            set
            {
                _transport.MaxPendingConnections = value;
            }
        }

        internal bool IsMaxConnectionsSet
        {
            get { return _transport.IsMaxPendingConnectionsSet; }
        }

        public int ListenBacklog
        {
            get { return _transport.ListenBacklog; }
            set { _transport.ListenBacklog = value; }
        }

        public long MaxReceivedMessageSize
        {
            get { return _transport.MaxReceivedMessageSize; }
            set { _transport.MaxReceivedMessageSize = value; }
        }

        public XmlDictionaryReaderQuotas ReaderQuotas
        {
            get { return _encoding.ReaderQuotas; }
            set
            {
                if (value == null)
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull(nameof(value));
                }

                value.CopyTo(_encoding.ReaderQuotas);
            }
        }

        //TODO: Work out if we want IBindingRuntimePreferences. Probably not as we're aiming for 100% async here
        //bool IBindingRuntimePreferences.ReceiveSynchronously
        //{
        //    get { return false; }
        //}

        public override string Scheme { get { return _transport.Scheme; } }

        public EnvelopeVersion EnvelopeVersion
        {
            get { return EnvelopeVersion.Soap12; }
        }

        public NetTcpSecurity Security
        {
            get { return _security; }
            set
            {
                _security = value ?? throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull(nameof(value));
            }
        }

        private void Initialize()
        {
            _transport = new TcpTransportBindingElement();
            _encoding = new BinaryMessageEncodingBindingElement();
        }

        private void CheckSettings()
        {
            NetTcpSecurity security = Security;
            if (security == null)
            {
                return;
            }

            SecurityMode mode = security.Mode;
            if (mode == SecurityMode.None)
            {
                return;
            }
            else if (mode == SecurityMode.Message)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new NotSupportedException(SR.Format(SR.UnsupportedSecuritySetting, "Mode", mode)));
            }

            // Message.ClientCredentialType = Certificate, IssuedToken or Windows are not supported.
            if (mode == SecurityMode.TransportWithMessageCredential)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new NotSupportedException(SR.Format(SR.UnsupportedSecuritySetting, "Mode", mode)));
            }
        }

        public override BindingElementCollection CreateBindingElements()
        {
            CheckSettings();

            // return collection of BindingElements
            BindingElementCollection bindingElements = new BindingElementCollection
            {
                // order of BindingElements is important
                // add encoding
                _encoding
            };
            // add transport security
            BindingElement transportSecurity = CreateTransportSecurity();
            if (transportSecurity != null)
            {
                bindingElements.Add(transportSecurity);
            }
            // TODO: Add ExtendedProtectionPolicy
            _transport.ExtendedProtectionPolicy = _security.Transport.ExtendedProtectionPolicy;
            // add transport (tcp)
            bindingElements.Add(_transport);

            return bindingElements.Clone();
        }

        private BindingElement CreateTransportSecurity()
        {
            return _security.CreateTransportSecurity();
        }
    }
}
