﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Xml;
using CoreWCF.Channels;
using CoreWCF.Runtime;
using Microsoft.Extensions.ObjectPool;

namespace CoreWCF.Dispatcher
{
    internal delegate ValueTask MessageRpcProcessor(MessageRpc rpc);

    internal delegate ValueTask MessageRpcErrorHandler(MessageRpc rpc);

    // TODO: Pool MessageRpc objects. These are zero cost on .NET Framework as it's a struct but passing things by ref is problematic
    // when using async/await. This causes an allocation per request so pool them to remove that allocation.
    internal class MessageRpc
    {
        internal ServiceChannel Channel;
        internal ChannelHandler ChannelHandler;
        internal object[] Correlation;
        internal ServiceHostBase Host;
        internal OperationContext OperationContext;
        //internal ServiceModelActivity Activity;
        // internal Guid ResponseActivityId;
        internal bool CanSendReply;
        internal bool SuccessfullySendReply;
        internal object[] InputParameters;
        internal object[] OutputParameters;
        internal object ReturnParameter;
        internal bool ParametersDisposed;
        internal bool DidDeserializeRequestBody;
        //internal TransactionMessageProperty TransactionMessageProperty;
        //internal TransactedBatchContext TransactedBatchContext;
        internal Exception Error;
        internal MessageRpcErrorHandler ErrorProcessor;
        internal ErrorHandlerFaultInfo FaultInfo;
        internal bool HasSecurityContext;
        internal object Instance;
        internal bool MessageRpcOwnsInstanceContextThrottle;
        internal MessageRpcProcessor AsyncProcessor;
        internal Collection<MessageHeaderInfo> NotUnderstoodHeaders;
        internal DispatchOperationRuntime Operation;
        internal Message Request;
        internal RequestContext RequestContext;
        internal bool RequestContextThrewOnReply;
        internal UniqueId RequestID;
        internal Message Reply;
        internal TimeoutHelper ReplyTimeoutHelper;
        internal RequestReplyCorrelator.ReplyToInfo ReplyToInfo;
        internal MessageVersion RequestVersion;
        internal ServiceSecurityContext SecurityContext;
        internal InstanceContext InstanceContext;
        internal bool SuccessfullyBoundInstance;
        internal bool SuccessfullyIncrementedActivity;
        internal bool SuccessfullyLockedInstance;
        //internal TransactionRpcFacet transaction;
        //internal IAspNetMessageProperty HostingProperty;
        //internal MessageRpcInvokeNotification InvokeNotification;
        //internal EventTraceActivity EventTraceActivity;
        internal bool _processCallReturned;
        private bool _isInstanceContextSingleton;
        // private SignalGate<IAsyncResult> _invokeContinueGate;

        public MessageRpc()
        {
            Clear();
        }

        internal MessageRpc(RequestContext requestContext, Message request, DispatchOperationRuntime operation,
            ServiceChannel channel, ServiceHostBase host, ChannelHandler channelHandler, bool cleanThread,
            OperationContext operationContext, InstanceContext instanceContext/*, EventTraceActivity eventTraceActivity*/)
        {
            Clear();
            Initialize(requestContext, request, operation, channel, host, channelHandler, cleanThread, operationContext, instanceContext);
        }

        internal void Clear()
        {
            CanSendReply = true;
            Channel = null;
            ChannelHandler = null;
            Correlation = Array.Empty<object>();
            DidDeserializeRequestBody = false;
            Error = null;
            ErrorProcessor = null;
            FaultInfo = default;
            HasSecurityContext = false;
            Host = null;
            Instance = null;
            AsyncProcessor = null;
            NotUnderstoodHeaders = null;
            Operation = null;
            OperationContext = null;
            IsPaused = false;
            ParametersDisposed = false;
            Request = null;
            RequestContext = null;
            RequestContextThrewOnReply = false;
            SuccessfullySendReply = false;
            RequestVersion = null;
            Reply = null;
            ReplyTimeoutHelper = new TimeoutHelper();
            SecurityContext = null;
            InstanceContext = null;
            SuccessfullyBoundInstance = false;
            SuccessfullyIncrementedActivity = false;
            SuccessfullyLockedInstance = false;
            SwitchedThreads = false;
            InputParameters = null;
            OutputParameters = null;
            ReturnParameter = null;
        }

        internal void Initialize(RequestContext requestContext, Message request, DispatchOperationRuntime operation,
            ServiceChannel channel, ServiceHostBase host, ChannelHandler channelHandler, bool cleanThread,
            OperationContext operationContext, InstanceContext instanceContext)
        {
            Fx.Assert((operationContext != null), "correwcf.Dispatcher.MessageRpc.MessageRpc(), operationContext == null");
            Channel = channel;
            ChannelHandler = channelHandler;
            Correlation = EmptyArray.Allocate(operation.Parent.CorrelationCount);
            FaultInfo = new ErrorHandlerFaultInfo(request.Version.Addressing.DefaultFaultAction);
            Host = host;
            Operation = operation;
            OperationContext = operationContext;
            Request = request;
            RequestContext = requestContext;
            RequestVersion = request.Version;
            InstanceContext = instanceContext;
            SwitchedThreads = !cleanThread;
            _isInstanceContextSingleton = InstanceContextProviderBase.IsProviderSingleton(Channel.DispatchRuntime.InstanceContextProvider);

            if (!operation.IsOneWay && !operation.Parent.ManualAddressing)
            {
                RequestID = request.Headers.MessageId;
                ReplyToInfo = new RequestReplyCorrelator.ReplyToInfo(request);
            }
            else
            {
                RequestID = null;
                ReplyToInfo = new RequestReplyCorrelator.ReplyToInfo();
            }

            //if (DiagnosticUtility.ShouldUseActivity)
            //{
            //    this.Activity = TraceUtility.ExtractActivity(this.Request);
            //}

            //if (DiagnosticUtility.ShouldUseActivity || TraceUtility.ShouldPropagateActivity)
            //{
            //    this.ResponseActivityId = ActivityIdHeader.ExtractActivityId(this.Request);
            //}
            //else
            //{
            //ResponseActivityId = Guid.Empty;
            //}

            //if (this.EventTraceActivity == null && FxTrace.Trace.IsEnd2EndActivityTracingEnabled)
            //{
            //    if (this.Request != null)
            //    {
            //        this.EventTraceActivity = EventTraceActivityHelper.TryExtractActivity(this.Request, true);
            //    }
            //}
        }

        internal bool IsPaused { get; private set; }

        internal bool SwitchedThreads { get; private set; }

        // internal bool IsInstanceContextSingleton
        // {
        //     set
        //     {
        //         _isInstanceContextSingleton = value;
        //     }
        // }

        //internal TransactionRpcFacet Transaction
        //{
        //    get
        //    {
        //        if (this.transaction == null)
        //        {
        //            this.transaction = new TransactionRpcFacet(ref this);
        //        }
        //        return this.transaction;
        //    }
        //}

        internal void Abort()
        {
            AbortRequestContext();
            AbortChannel();
            AbortInstanceContext();
        }

        private void AbortRequestContext(RequestContext requestContext)
        {
            try
            {
                requestContext.Abort();
            }
            catch (Exception e)
            {
                if (Fx.IsFatal(e))
                {
                    throw;
                }

                ChannelHandler.HandleError(e);
            }
        }

        internal void AbortRequestContext()
        {
            if (OperationContext.RequestContext != null)
            {
                AbortRequestContext(OperationContext.RequestContext);
            }
            if ((RequestContext != null) && (RequestContext != OperationContext.RequestContext))
            {
                AbortRequestContext(RequestContext);
            }
            TraceCallDurationInDispatcherIfNecessary(false);
        }

        private void TraceCallDurationInDispatcherIfNecessary(bool requestContextWasClosedSuccessfully)
        {
            // only need to trace once (either for the failure or success case)
            //if (TD.DispatchFailedIsEnabled())
            //{
            //    if (requestContextWasClosedSuccessfully)
            //    {
            //        TD.DispatchSuccessful(this.EventTraceActivity, this.Operation.Name);
            //    }
            //    else
            //    {
            //        TD.DispatchFailed(this.EventTraceActivity, this.Operation.Name);
            //    }
            //}
        }

        internal void CloseRequestContext()
        {
            if (OperationContext.RequestContext != null)
            {
                DisposeRequestContext(OperationContext.RequestContext);
            }
            if ((RequestContext != null) && (RequestContext != OperationContext.RequestContext))
            {
                DisposeRequestContext(RequestContext);
            }
            TraceCallDurationInDispatcherIfNecessary(true);
        }

        private void DisposeRequestContext(RequestContext context)
        {
            try
            {
                context.CloseAsync().GetAwaiter().GetResult();
            }
            catch (Exception e)
            {
                if (Fx.IsFatal(e))
                {
                    throw;
                }

                AbortRequestContext(context);
                ChannelHandler.HandleError(e);
            }
        }

        internal void AbortChannel()
        {
            if ((Channel != null) && Channel.HasSession)
            {
                try
                {
                    Channel.Abort();
                }
                catch (Exception e)
                {
                    if (Fx.IsFatal(e))
                    {
                        throw;
                    }

                    ChannelHandler.HandleError(e);
                }
            }
        }

        // TODO: Make async
        internal void CloseChannel()
        {
            if ((Channel != null) && Channel.HasSession)
            {
                try
                {
                    var helper = new TimeoutHelper(ChannelHandler.CloseAfterFaultTimeout);
                    Channel.CloseAsync(helper.GetCancellationToken()).GetAwaiter().GetResult();
                }
                catch (Exception e)
                {
                    if (Fx.IsFatal(e))
                    {
                        throw;
                    }

                    ChannelHandler.HandleError(e);
                }
            }
        }

        internal void AbortInstanceContext()
        {
            if (InstanceContext != null && !_isInstanceContextSingleton)
            {
                try
                {
                    InstanceContext.Abort();
                }
                catch (Exception e)
                {
                    if (Fx.IsFatal(e))
                    {
                        throw;
                    }

                    ChannelHandler.HandleError(e);
                }
            }
        }

        internal void EnsureReceive()
        {
            //using (ServiceModelActivity.BoundOperation(this.Activity))
            //{
            ChannelHandler.EnsureReceive();
            //}
        }

        private bool ProcessError(Exception e)
        {
            MessageRpcErrorHandler handler = ErrorProcessor;
            try
            {
                Type exceptionType = e.GetType();

                if (exceptionType.IsAssignableFrom(typeof(FaultException)))
                {
                    DiagnosticUtility.TraceHandledException(e, TraceEventType.Information);
                }
                else
                {
                    DiagnosticUtility.TraceHandledException(e, TraceEventType.Error);
                }

                //if (TraceUtility.MessageFlowTracingOnly)
                //{
                //    TraceUtility.SetActivityId(this.Request.Properties);
                //    if (Guid.Empty == DiagnosticTraceBase.ActivityId)
                //    {
                //        Guid receivedActivityId = TraceUtility.ExtractActivityId(this.Request);
                //        if (Guid.Empty != receivedActivityId)
                //        {
                //            DiagnosticTraceBase.ActivityId = receivedActivityId;
                //        }
                //    }
                //}


                Error = e;

                if (ErrorProcessor != null)
                {
                    ErrorProcessor(this);
                }

                return (Error == null);
            }
            catch (Exception e2)
            {
                if (Fx.IsFatal(e2))
                {
                    throw;
                }

                return ((handler != ErrorProcessor) && ProcessError(e2));
            }
        }

        internal void DisposeParameters(bool excludeInput)
        {
            if (Operation.DisposeParameters)
            {
                DisposeParametersCore(excludeInput);
            }
        }

        internal void DisposeParametersCore(bool excludeInput)
        {
            if (!ParametersDisposed)
            {
                if (!excludeInput)
                {
                    DisposeParameterList(InputParameters);
                }

                DisposeParameterList(OutputParameters);

                if (ReturnParameter is IDisposable disposableParameter)
                {
                    try
                    {
                        disposableParameter.Dispose();
                    }
                    catch (Exception e)
                    {
                        if (Fx.IsFatal(e))
                        {
                            throw;
                        }

                        ChannelHandler.HandleError(e);
                    }
                }

                ParametersDisposed = true;
            }
        }

        private void DisposeParameterList(object[] parameters)
        {
            if (parameters != null)
            {
                foreach (object obj in parameters)
                {
                    if (obj is IDisposable disposableParameter)
                    {
                        try
                        {
                            disposableParameter.Dispose();
                        }
                        catch (Exception e)
                        {
                            if (Fx.IsFatal(e))
                            {
                                throw;
                            }

                            ChannelHandler.HandleError(e);
                        }
                    }
                }
            }
        }

        internal async ValueTask ProcessAsync(bool isOperationContextSet)
        {
            //using (ServiceModelActivity.BoundOperation(this.Activity))
            //{
            // bool completed = true;

            OperationContext originalContext;
            OperationContext.Holder contextHolder;
            if (!isOperationContextSet)
            {
                contextHolder = OperationContext.CurrentHolder;
                originalContext = contextHolder.Context;
            }
            else
            {
                contextHolder = null;
                originalContext = null;
            }
            IncrementBusyCount();

            try
            {
                if (!isOperationContextSet)
                {
                    contextHolder.Context = OperationContext;
                }

                await AsyncProcessor(this);

                OperationContext.SetClientReply(null, false);
            }
            catch (Exception e)
            {
                if (Fx.IsFatal(e))
                {
                    throw;
                }
                if (!ProcessError(e) && FaultInfo.Fault == null)
                {
                    Abort();
                }
            }
            finally
            {
                try
                {
                    DecrementBusyCount();

                    if (!isOperationContextSet)
                    {
                        contextHolder.Context = originalContext;
                    }

                    OperationContext.ClearClientReplyNoThrow();
                }
                catch (Exception e)
                {
                    if (Fx.IsFatal(e))
                    {
#pragma warning disable CA2219 // Do not raise exceptions in finally clauses - Fx.IsFatal filters out non-process ending exceptions
                        throw;
#pragma warning restore CA2219 // Do not raise exceptions in finally clauses
                    }
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperFatal(e.Message, e);
                }
            }

            //}
        }

        // UnPause is called on the original MessageRpc to continue work on the current thread, and the copy is ignored.
        // Since the copy is ignored, Decrement the BusyCount
        internal void UnPause()
        {
            IsPaused = false;
            DecrementBusyCount();
        }

        // internal bool UnlockInvokeContinueGate(out IAsyncResult result)
        // {
        //     return _invokeContinueGate.Unlock(out result);
        // }
        //
        // internal void PrepareInvokeContinueGate()
        // {
        //     _invokeContinueGate = new SignalGate<IAsyncResult>();
        // }

        private void IncrementBusyCount()
        {
            // TODO: Do we want a way to keep track of bust count? I believe this originally drove PerformanceCounters so we might want to re-work this functionality.
            // Only increment the counter on the service side.
            //if (Host != null)
            //{
            //Host.IncrementBusyCount();
            //if (AspNetEnvironment.Current.TraceIncrementBusyCountIsEnabled())
            //{
            //    AspNetEnvironment.Current.TraceIncrementBusyCount(SR.Format(SR.ServiceBusyCountTrace, this.Operation.Action));
            //}
            //}
        }

        private void DecrementBusyCount()
        {
            // See comment on IncrementBusyCount
            //if (Host != null)
            //{
            //    Host.DecrementBusyCount();
            //if (AspNetEnvironment.Current.TraceDecrementBusyCountIsEnabled())
            //{
            //    AspNetEnvironment.Current.TraceDecrementBusyCount(SR.Format(SR.ServiceBusyCountTrace, this.Operation.Action));
            //}
            //}
        }
    }

    internal class MessageRpcPooledObjectPolicy : IPooledObjectPolicy<MessageRpc>
    {
        public MessageRpc Create() => new MessageRpc();

        public bool Return(MessageRpc obj)
        {
            obj.Clear();
            return true;
        }
    }
}
