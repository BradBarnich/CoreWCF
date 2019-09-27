﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CoreWCF.Channels;
using CoreWCF.Diagnostics;

namespace CoreWCF.Dispatcher
{
    using QName = CoreWCF.Dispatcher.EndpointAddressProcessor.QName;
    using HeaderBit = CoreWCF.Dispatcher.EndpointAddressProcessor.HeaderBit;

    internal class EndpointAddressMessageFilterTable<TFilterData> : IMessageFilterTable<TFilterData>
    {
        protected Dictionary<MessageFilter, TFilterData> filters;
        protected Dictionary<MessageFilter, Candidate> candidates;
        private WeakReference processorPool;

        private int size;
        private int nextBit;
        private Dictionary<string, HeaderBit[]> headerLookup;
        private Dictionary<Uri, CandidateSet> toHostLookup;
        private Dictionary<Uri, CandidateSet> toNoHostLookup;

        internal class ProcessorPool
        {
            private EndpointAddressProcessor processor;

            internal ProcessorPool()
            {
            }

            internal EndpointAddressProcessor Pop()
            {
                EndpointAddressProcessor p = processor;
                if (null != p)
                {
                    processor = (EndpointAddressProcessor)p.next;
                    p.next = null;
                    return p;
                }
                return null;
            }

            internal void Push(EndpointAddressProcessor p)
            {
                p.next = processor;
                processor = p;
            }
        }

        public EndpointAddressMessageFilterTable()
        {
            processorPool = new WeakReference(null);

            size = 0;
            nextBit = 0;

            filters = new Dictionary<MessageFilter, TFilterData>();
            candidates = new Dictionary<MessageFilter, Candidate>();
            headerLookup = new Dictionary<string, HeaderBit[]>();
            InitializeLookupTables();
        }

        protected virtual void InitializeLookupTables()
        {
            toHostLookup = new Dictionary<Uri, CandidateSet>(EndpointAddressMessageFilter.HostUriComparer.Value);
            toNoHostLookup = new Dictionary<Uri, CandidateSet>(EndpointAddressMessageFilter.NoHostUriComparer.Value);
        }

        public TFilterData this[MessageFilter filter]
        {
            get
            {
                return filters[filter];
            }
            set
            {
                if (filters.ContainsKey(filter))
                {
                    filters[filter] = value;
                    candidates[filter].data = value;
                }
                else
                {
                    Add(filter, value);
                }
            }
        }

        public int Count
        {
            get
            {
                return filters.Count;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        public ICollection<MessageFilter> Keys
        {
            get
            {
                return filters.Keys;
            }
        }

        public ICollection<TFilterData> Values
        {
            get
            {
                return filters.Values;
            }
        }

        public virtual void Add(MessageFilter filter, TFilterData data)
        {
            if (filter == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull(nameof(filter));
            }

            Add((EndpointAddressMessageFilter)filter, data);
        }

        public virtual void Add(EndpointAddressMessageFilter filter, TFilterData data)
        {
            if (filter == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull(nameof(filter));
            }

            filters.Add(filter, data);

            // Create the candidate
            byte[] mask = BuildMask(filter.HeaderLookup);
            Candidate can = new Candidate(filter, data, mask, filter.HeaderLookup);
            candidates.Add(filter, can);

            CandidateSet cset;
            Uri soapToAddress = filter.Address.Uri;
            if (filter.IncludeHostNameInComparison)
            {
                if (!toHostLookup.TryGetValue(soapToAddress, out cset))
                {
                    cset = new CandidateSet();
                    toHostLookup.Add(soapToAddress, cset);
                }
            }
            else
            {
                if (!toNoHostLookup.TryGetValue(soapToAddress, out cset))
                {
                    cset = new CandidateSet();
                    toNoHostLookup.Add(soapToAddress, cset);
                }
            }
            cset.candidates.Add(can);

            IncrementQNameCount(cset, filter.Address);
        }

        protected void IncrementQNameCount(CandidateSet cset, EndpointAddress address)
        {
            // Update the QName ref count
            QName qname;
            int cnt;
            for (int i = 0; i < address.Headers.Count; ++i)
            {
                AddressHeader parameter = address.Headers[i];
                qname.name = parameter.Name;
                qname.ns = parameter.Namespace;
                if (cset.qnames.TryGetValue(qname, out cnt))
                {
                    cset.qnames[qname] = cnt + 1;
                }
                else
                {
                    cset.qnames.Add(qname, 1);
                }
            }
        }

        public void Add(KeyValuePair<MessageFilter, TFilterData> item)
        {
            Add(item.Key, item.Value);
        }

        protected byte[] BuildMask(Dictionary<string, HeaderBit[]> headerLookup)
        {
            HeaderBit[] bits;
            byte[] mask = null;
            foreach (KeyValuePair<string, HeaderBit[]> item in headerLookup)
            {
                if (this.headerLookup.TryGetValue(item.Key, out bits))
                {
                    if (bits.Length < item.Value.Length)
                    {
                        int old = bits.Length;
                        Array.Resize(ref bits, item.Value.Length);
                        for (int i = old; i < item.Value.Length; ++i)
                        {
                            bits[i] = new HeaderBit(nextBit++);
                        }
                        this.headerLookup[item.Key] = bits;
                    }
                }
                else
                {
                    bits = new HeaderBit[item.Value.Length];
                    for (int i = 0; i < item.Value.Length; ++i)
                    {
                        bits[i] = new HeaderBit(nextBit++);
                    }
                    this.headerLookup.Add(item.Key, bits);
                }

                for (int i = 0; i < item.Value.Length; ++i)
                {
                    bits[i].AddToMask(ref mask);
                }
            }

            if (nextBit == 0)
            {
                size = 0;
            }
            else
            {
                size = (nextBit - 1) / 8 + 1;
            }

            return mask;
        }

        public void Clear()
        {
            size = 0;
            nextBit = 0;
            filters.Clear();
            candidates.Clear();
            headerLookup.Clear();
            ClearLookupTables();
        }

        protected virtual void ClearLookupTables()
        {
            if (toHostLookup != null)
            {
                toHostLookup.Clear();
            }
            if (toNoHostLookup != null)
            {
                toNoHostLookup.Clear();
            }
        }

        public bool Contains(KeyValuePair<MessageFilter, TFilterData> item)
        {
            return ((ICollection<KeyValuePair<MessageFilter, TFilterData>>)filters).Contains(item);
        }

        public bool ContainsKey(MessageFilter filter)
        {
            if (filter == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull(nameof(filter));
            }
            return filters.ContainsKey(filter);
        }

        public void CopyTo(KeyValuePair<MessageFilter, TFilterData>[] array, int arrayIndex)
        {
            ((ICollection<KeyValuePair<MessageFilter, TFilterData>>)filters).CopyTo(array, arrayIndex);
        }

        private EndpointAddressProcessor CreateProcessor(int length)
        {
            EndpointAddressProcessor p = null;
            lock (processorPool)
            {
                ProcessorPool pool = processorPool.Target as ProcessorPool;
                if (null != pool)
                {
                    p = pool.Pop();
                }
            }

            if (null != p)
            {
                p.Clear(length);
                return p;
            }

            return new EndpointAddressProcessor(length);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<KeyValuePair<MessageFilter, TFilterData>> GetEnumerator()
        {
            return filters.GetEnumerator();
        }

        internal virtual bool TryMatchCandidateSet(Uri to, bool includeHostNameInComparison, out CandidateSet cset)
        {
            if (includeHostNameInComparison)
            {
                return toHostLookup.TryGetValue(to, out cset);
            }
            else
            {
                return toNoHostLookup.TryGetValue(to, out cset);
            }
        }

        private Candidate InnerMatch(Message message)
        {
            Uri to = message.Headers.To;
            if (to == null)
            {
                return null;
            }

            CandidateSet cset = null;
            Candidate can = null;
            if (TryMatchCandidateSet(to, true/*includeHostNameInComparison*/, out cset))
            {
                can = GetSingleMatch(cset, message);
            }
            if (TryMatchCandidateSet(to, false/*includeHostNameInComparison*/, out cset))
            {
                Candidate c = GetSingleMatch(cset, message);
                if (c != null)
                {
                    if (can != null)
                    {
                        Collection<MessageFilter> matches = new Collection<MessageFilter>();
                        matches.Add(can.filter);
                        matches.Add(c.filter);
                        throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new MultipleFilterMatchesException(SR.FilterMultipleMatches, null, matches));
                    }
                    can = c;
                }
            }

            return can;
        }

        private Candidate GetSingleMatch(CandidateSet cset, Message message)
        {
            int candiCount = cset.candidates.Count;

            if (cset.qnames.Count == 0)
            {
                if (candiCount == 0)
                {
                    return null;
                }
                else if (candiCount == 1)
                {
                    return cset.candidates[0];
                }
                else
                {
                    Collection<MessageFilter> matches = new Collection<MessageFilter>();
                    for (int i = 0; i < candiCount; ++i)
                    {
                        matches.Add(cset.candidates[i].filter);
                    }
                    throw TraceUtility.ThrowHelperError(new MultipleFilterMatchesException(SR.FilterMultipleMatches, null, matches), message);
                }
            }

            EndpointAddressProcessor context = CreateProcessor(size);
            context.ProcessHeaders(message, cset.qnames, headerLookup);

            Candidate can = null;
            List<Candidate> candis = cset.candidates;
            for (int i = 0; i < candiCount; ++i)
            {
                if (context.TestMask(candis[i].mask))
                {
                    if (can != null)
                    {
                        Collection<MessageFilter> matches = new Collection<MessageFilter>();
                        matches.Add(can.filter);
                        matches.Add(candis[i].filter);
                        throw TraceUtility.ThrowHelperError(new MultipleFilterMatchesException(SR.FilterMultipleMatches, null, matches), message);
                    }
                    can = candis[i];
                }
            }

            ReleaseProcessor(context);

            return can;
        }

        private void InnerMatchData(Message message, ICollection<TFilterData> results)
        {
            Uri to = message.Headers.To;
            if (to != null)
            {
                CandidateSet cset;
                if (TryMatchCandidateSet(to, true /*includeHostNameInComparison*/, out cset))
                {
                    InnerMatchData(message, results, cset);
                }
                if (TryMatchCandidateSet(to, false /*includeHostNameInComparison*/, out cset))
                {
                    InnerMatchData(message, results, cset);
                }
            }
        }

        private void InnerMatchData(Message message, ICollection<TFilterData> results, CandidateSet cset)
        {
            EndpointAddressProcessor context = CreateProcessor(size);
            context.ProcessHeaders(message, cset.qnames, headerLookup);

            List<Candidate> candis = cset.candidates;
            for (int i = 0; i < candis.Count; ++i)
            {
                if (context.TestMask(candis[i].mask))
                {
                    results.Add(candis[i].data);
                }
            }

            ReleaseProcessor(context);
        }

        protected void InnerMatchFilters(Message message, ICollection<MessageFilter> results)
        {
            Uri to = message.Headers.To;
            if (to != null)
            {
                CandidateSet cset;
                if (TryMatchCandidateSet(to, true/*includeHostNameInComparison*/, out cset))
                {
                    InnerMatchFilters(message, results, cset);
                }
                if (TryMatchCandidateSet(to, false/*includeHostNameInComparison*/, out cset))
                {
                    InnerMatchFilters(message, results, cset);
                }
            }
        }

        private void InnerMatchFilters(Message message, ICollection<MessageFilter> results, CandidateSet cset)
        {
            EndpointAddressProcessor context = CreateProcessor(size);
            context.ProcessHeaders(message, cset.qnames, headerLookup);

            List<Candidate> candis = cset.candidates;
            for (int i = 0; i < candis.Count; ++i)
            {
                if (context.TestMask(candis[i].mask))
                {
                    results.Add(candis[i].filter);
                }
            }

            ReleaseProcessor(context);
        }

        public bool GetMatchingValue(Message message, out TFilterData data)
        {
            if (message == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull(nameof(message));
            }

            Candidate can = InnerMatch(message);
            if (can == null)
            {
                data = default(TFilterData);
                return false;
            }

            data = can.data;
            return true;
        }

        public bool GetMatchingValue(MessageBuffer messageBuffer, out TFilterData data)
        {
            if (messageBuffer == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull(nameof(messageBuffer));
            }

            Message msg = messageBuffer.CreateMessage();
            Candidate can = null;
            try
            {
                can = InnerMatch(msg);
            }
            finally
            {
                msg.Close();
            }

            if (can == null)
            {
                data = default(TFilterData);
                return false;
            }

            data = can.data;
            return true;
        }

        public bool GetMatchingValues(Message message, ICollection<TFilterData> results)
        {
            if (message == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull(nameof(message));
            }

            if (results == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull(nameof(results));
            }

            int count = results.Count;
            InnerMatchData(message, results);
            return count != results.Count;
        }

        public bool GetMatchingValues(MessageBuffer messageBuffer, ICollection<TFilterData> results)
        {
            if (messageBuffer == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull(nameof(messageBuffer));
            }

            if (results == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull(nameof(results));
            }

            Message msg = messageBuffer.CreateMessage();
            try
            {
                int count = results.Count;
                InnerMatchData(msg, results);
                return count != results.Count;
            }
            finally
            {
                msg.Close();
            }
        }

        public bool GetMatchingFilter(Message message, out MessageFilter filter)
        {
            if (message == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull(nameof(message));
            }

            Candidate can = InnerMatch(message);
            if (can != null)
            {
                filter = can.filter;
                return true;
            }

            filter = null;
            return false;
        }

        public bool GetMatchingFilter(MessageBuffer messageBuffer, out MessageFilter filter)
        {
            if (messageBuffer == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull(nameof(messageBuffer));
            }

            Message msg = messageBuffer.CreateMessage();
            Candidate can = null;
            try
            {
                can = InnerMatch(msg);
            }
            finally
            {
                msg.Close();
            }

            if (can != null)
            {
                filter = can.filter;
                return true;
            }

            filter = null;
            return false;
        }

        public bool GetMatchingFilters(Message message, ICollection<MessageFilter> results)
        {
            if (message == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull(nameof(message));
            }

            if (results == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull(nameof(results));
            }

            int count = results.Count;
            InnerMatchFilters(message, results);
            return count != results.Count;
        }

        public bool GetMatchingFilters(MessageBuffer messageBuffer, ICollection<MessageFilter> results)
        {
            if (messageBuffer == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull(nameof(messageBuffer));
            }

            if (results == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull(nameof(results));
            }

            Message msg = messageBuffer.CreateMessage();
            try
            {
                int count = results.Count;
                InnerMatchFilters(msg, results);
                return count != results.Count;
            }
            finally
            {
                msg.Close();
            }
        }

        protected void RebuildMasks()
        {
            nextBit = 0;
            size = 0;

            // Clear out all the bits.
            headerLookup.Clear();

            // Rebuild the masks
            foreach (Candidate can in candidates.Values)
            {
                can.mask = BuildMask(can.headerLookup);
            }
        }

        private void ReleaseProcessor(EndpointAddressProcessor processor)
        {
            lock (processorPool)
            {
                ProcessorPool pool = processorPool.Target as ProcessorPool;
                if (null == pool)
                {
                    pool = new ProcessorPool();
                    processorPool.Target = pool;
                }
                pool.Push(processor);
            }
        }

        public virtual bool Remove(MessageFilter filter)
        {
            if (filter == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull(nameof(filter));
            }

            EndpointAddressMessageFilter saFilter = filter as EndpointAddressMessageFilter;
            if (saFilter != null)
            {
                return Remove(saFilter);
            }

            return false;
        }

        public virtual bool Remove(EndpointAddressMessageFilter filter)
        {
            if (filter == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull(nameof(filter));
            }

            if (!filters.Remove(filter))
            {
                return false;
            }

            Candidate can = candidates[filter];
            Uri soapToAddress = filter.Address.Uri;

            CandidateSet cset = null;
            if (filter.IncludeHostNameInComparison)
            {
                cset = toHostLookup[soapToAddress];
            }
            else
            {
                cset = toNoHostLookup[soapToAddress];
            }

            candidates.Remove(filter);

            if (cset.candidates.Count == 1)
            {
                if (filter.IncludeHostNameInComparison)
                {
                    toHostLookup.Remove(soapToAddress);
                }
                else
                {
                    toNoHostLookup.Remove(soapToAddress);
                }
            }
            else
            {
                DecrementQNameCount(cset, filter.Address);

                // Remove Candidate
                cset.candidates.Remove(can);
            }

            RebuildMasks();
            return true;
        }

        protected void DecrementQNameCount(CandidateSet cset, EndpointAddress address)
        {
            // Adjust QName counts
            QName qname;
            for (int i = 0; i < address.Headers.Count; ++i)
            {
                AddressHeader parameter = address.Headers[i];
                qname.name = parameter.Name;
                qname.ns = parameter.Namespace;
                int cnt = cset.qnames[qname];
                if (cnt == 1)
                {
                    cset.qnames.Remove(qname);
                }
                else
                {
                    cset.qnames[qname] = cnt - 1;
                }
            }
        }

        public bool Remove(KeyValuePair<MessageFilter, TFilterData> item)
        {
            if (((ICollection<KeyValuePair<MessageFilter, TFilterData>>)filters).Contains(item))
            {
                return Remove(item.Key);
            }
            return false;
        }

        internal class Candidate
        {
            internal MessageFilter filter;
            internal TFilterData data;
            internal byte[] mask;
            internal Dictionary<string, HeaderBit[]> headerLookup;

            internal Candidate(MessageFilter filter, TFilterData data, byte[] mask, Dictionary<string, HeaderBit[]> headerLookup)
            {
                this.filter = filter;
                this.data = data;
                this.mask = mask;
                this.headerLookup = headerLookup;
            }
        }

        internal class CandidateSet
        {
            internal Dictionary<QName, int> qnames;
            internal List<Candidate> candidates;

            internal CandidateSet()
            {
                qnames = new Dictionary<QName, int>(EndpointAddressProcessor.QNameComparer);
                candidates = new List<Candidate>();
            }
        }

        public bool TryGetValue(MessageFilter filter, out TFilterData data)
        {
            return filters.TryGetValue(filter, out data);
        }
    }

}