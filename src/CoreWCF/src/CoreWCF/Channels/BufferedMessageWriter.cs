﻿using System;
using System.IO;
using System.Xml;

namespace CoreWCF.Channels
{
    internal abstract class BufferedMessageWriter
    {
        private int[] sizeHistory;
        private int sizeHistoryIndex;
        private const int sizeHistoryCount = 4;
        private const int expectedSizeVariance = 256;
        private BufferManagerOutputStream stream;

        public BufferedMessageWriter()
        {
            stream = new BufferManagerOutputStream(SR.MaxSentMessageSizeExceeded);
            InitMessagePredictor();
        }

        protected abstract XmlDictionaryWriter TakeXmlWriter(Stream stream);
        protected abstract void ReturnXmlWriter(XmlDictionaryWriter writer);

        public ArraySegment<byte> WriteMessage(Message message, BufferManager bufferManager, int initialOffset, int maxSizeQuota)
        {
            int effectiveMaxSize;

            // make sure that maxSize has room for initialOffset without overflowing, since
            // the effective buffer size is message size + initialOffset
            if (maxSizeQuota <= int.MaxValue - initialOffset)
            {
                effectiveMaxSize = maxSizeQuota + initialOffset;
            }
            else
            {
                effectiveMaxSize = int.MaxValue;
            }

            int predictedMessageSize = PredictMessageSize();
            if (predictedMessageSize > effectiveMaxSize)
            {
                predictedMessageSize = effectiveMaxSize;
            }
            else if (predictedMessageSize < initialOffset)
            {
                predictedMessageSize = initialOffset;
            }

            try
            {
                stream.Init(predictedMessageSize, maxSizeQuota, effectiveMaxSize, bufferManager);
                stream.Skip(initialOffset);

                XmlDictionaryWriter writer = TakeXmlWriter(stream);
                OnWriteStartMessage(writer);
                message.WriteMessage(writer);
                OnWriteEndMessage(writer);
                writer.Flush();
                ReturnXmlWriter(writer);
                int size;
                byte[] buffer = stream.ToArray(out size);
                RecordActualMessageSize(size);
                return new ArraySegment<byte>(buffer, initialOffset, size - initialOffset);
            }
            finally
            {
                stream.Clear();
            }
        }

        protected virtual void OnWriteStartMessage(XmlDictionaryWriter writer)
        {
        }

        protected virtual void OnWriteEndMessage(XmlDictionaryWriter writer)
        {
        }

        private void InitMessagePredictor()
        {
            sizeHistory = new int[4];
            for (int i = 0; i < sizeHistoryCount; i++)
            {
                sizeHistory[i] = 256;
            }
        }

        private int PredictMessageSize()
        {
            int max = 0;
            for (int i = 0; i < sizeHistoryCount; i++)
            {
                if (sizeHistory[i] > max)
                {
                    max = sizeHistory[i];
                }
            }

            return max + expectedSizeVariance;
        }

        private void RecordActualMessageSize(int size)
        {
            sizeHistory[sizeHistoryIndex] = size;
            sizeHistoryIndex = (sizeHistoryIndex + 1) % sizeHistoryCount;
        }
    }

}