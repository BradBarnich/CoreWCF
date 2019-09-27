using System;
using System.Collections;
using System.Collections.Generic;
using CoreWCF.Runtime;
using CoreWCF.Security;

namespace CoreWCF.Channels
{
    public sealed class MessageProperties : IDictionary<string, object>, IDisposable
    {
        private Property[] properties;
        private int propertyCount;
        private MessageEncoder encoder;
        private Uri via;
        private object allowOutputBatching;
        private SecurityMessageProperty security;
        private bool disposed;
        private const int InitialPropertyCount = 2;
        private const int MaxRecycledArrayLength = 8;
        private const string ViaKey = "Via";
        private const string AllowOutputBatchingKey = "AllowOutputBatching";
        private const string SecurityKey = "Security";
        private const string EncoderKey = "Encoder";
        private const int NotFoundIndex = -1;
        private const int ViaIndex = -2;
        private const int AllowOutputBatchingIndex = -3;
        private const int SecurityIndex = -4;
        private const int EncoderIndex = -5;
        private static object trueBool = true;
        private static object falseBool = false;

        public MessageProperties()
        {
        }

        public MessageProperties(MessageProperties properties)
        {
            CopyProperties(properties);
        }

        internal MessageProperties(KeyValuePair<string, object>[] array)
        {
            if (array == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull(nameof(array));
            }

            CopyProperties(array);
        }

        private void ThrowDisposed()
        {
            throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ObjectDisposedException(string.Empty, SR.Format(SR.ObjectDisposed, GetType().ToString())));
        }

        public object this[string name]
        {
            get
            {
                if (disposed)
                {
                    ThrowDisposed();
                }

                object value;

                if (!TryGetValue(name, out value))
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentException(SR.Format(SR.MessagePropertyNotFound, name)));
                }

                return value;
            }
            set
            {
                if (disposed)
                {
                    ThrowDisposed();
                }

                UpdateProperty(name, value, false);
            }
        }

        internal bool CanRecycle
        {
            get
            {
                return properties == null || properties.Length <= MaxRecycledArrayLength;
            }
        }

        public int Count
        {
            get
            {
                if (disposed)
                {
                    ThrowDisposed();
                }

                return propertyCount;
            }
        }

        public MessageEncoder Encoder
        {
            get
            {
                if (disposed)
                {
                    ThrowDisposed();
                }

                return encoder;
            }
            set
            {
                if (disposed)
                {
                    ThrowDisposed();
                }

                AdjustPropertyCount((object)encoder == null, (object)value == null);
                encoder = value;
            }
        }

        public bool AllowOutputBatching
        {
            get
            {
                if (disposed)
                {
                    ThrowDisposed();
                }

                return (object)allowOutputBatching == trueBool;
            }
            set
            {
                if (disposed)
                {
                    ThrowDisposed();
                }

                AdjustPropertyCount((object)allowOutputBatching == null, false);

                if (value)
                {
                    allowOutputBatching = trueBool;
                }
                else
                {
                    allowOutputBatching = falseBool;
                }
            }
        }

        public bool IsFixedSize
        {
            get
            {
                if (disposed)
                {
                    ThrowDisposed();
                }

                return false;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                if (disposed)
                {
                    ThrowDisposed();
                }

                return false;
            }
        }

        public ICollection<string> Keys
        {
            get
            {
                if (disposed)
                {
                    ThrowDisposed();
                }

                List<string> keys = new List<string>();

                if ((object)via != null)
                {
                    keys.Add(ViaKey);
                }

                if ((object)allowOutputBatching != null)
                {
                    keys.Add(AllowOutputBatchingKey);
                }

                //if ((object)security != null)
                //{
                //    keys.Add(SecurityKey);
                //}

                if ((object)encoder != null)
                {
                    keys.Add(EncoderKey);
                }

                if (properties != null)
                {
                    for (int i = 0; i < properties.Length; i++)
                    {
                        string propertyName = properties[i].Name;

                        if (propertyName == null)
                        {
                            break;
                        }

                        keys.Add(propertyName);
                    }
                }

                return keys;
            }
        }

        public SecurityMessageProperty Security
        {
            get
            {
                if (disposed)
                {
                    ThrowDisposed();
                }

                return security;
            }
            set
            {
                if (disposed)
                {
                    ThrowDisposed();
                }

                AdjustPropertyCount((object)security == null, (object)value == null);
                security = value;
            }
        }

        public ICollection<object> Values
        {
            get
            {
                if (disposed)
                {
                    ThrowDisposed();
                }

                List<object> values = new List<object>();

                if ((object)via != null)
                {
                    values.Add(via);
                }

                if ((object)allowOutputBatching != null)
                {
                    values.Add(allowOutputBatching);
                }

                //if ((object)security != null)
                //{
                //    values.Add(security);
                //}

                if ((object)encoder != null)
                {
                    values.Add(encoder);
                }
                if (properties != null)
                {
                    for (int i = 0; i < properties.Length; i++)
                    {
                        if (properties[i].Name == null)
                        {
                            break;
                        }

                        values.Add(properties[i].Value);
                    }
                }

                return values;
            }
        }

        public Uri Via
        {
            get
            {
                if (disposed)
                {
                    ThrowDisposed();
                }

                return via;
            }
            set
            {
                if (disposed)
                {
                    ThrowDisposed();
                }

                AdjustPropertyCount((object)via == null, (object)value == null);
                via = value;
            }
        }

        public void Add(string name, object property)
        {
            if (disposed)
            {
                ThrowDisposed();
            }

            if (property == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull(nameof(property));
            }

            UpdateProperty(name, property, true);
        }

        private void AdjustPropertyCount(bool oldValueIsNull, bool newValueIsNull)
        {
            if (newValueIsNull)
            {
                if (!oldValueIsNull)
                {
                    propertyCount--;
                }
            }
            else
            {
                if (oldValueIsNull)
                {
                    propertyCount++;
                }
            }
        }

        public void Clear()
        {
            if (disposed)
            {
                ThrowDisposed();
            }

            if (properties != null)
            {
                for (int i = 0; i < properties.Length; i++)
                {
                    if (properties[i].Name == null)
                    {
                        break;
                    }

                    properties[i] = new Property();
                }
            }

            via = null;
            allowOutputBatching = null;
            //security = null;
            encoder = null;
            propertyCount = 0;
        }

        public void CopyProperties(MessageProperties properties)
        {
            // CopyProperties behavior should be equivalent to the behavior
            // of MergeProperties except that Merge supports property values that
            // implement the IMergeEnabledMessageProperty.  Any changes to CopyProperties
            // should be reflected in MergeProperties as well.
            if (properties == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull(nameof(properties));
            }

            if (disposed)
            {
                ThrowDisposed();
            }

            if (properties.properties != null)
            {
                for (int i = 0; i < properties.properties.Length; i++)
                {
                    if (properties.properties[i].Name == null)
                    {
                        break;
                    }

                    Property property = properties.properties[i];

                    // this[string] will call CreateCopyOfPropertyValue, so we don't need to repeat that here
                    this[property.Name] = property.Value;
                }
            }

            Via = properties.Via;
            AllowOutputBatching = properties.AllowOutputBatching;
            //this.Security = (properties.Security != null) ? (SecurityMessageProperty)properties.Security.CreateCopy() : null;
            Encoder = properties.Encoder;
        }

        internal void MergeProperties(MessageProperties properties)
        {
            // MergeProperties behavior should be equivalent to the behavior
            // of CopyProperties except that Merge supports property values that
            // implement the IMergeEnabledMessageProperty.  Any changes to CopyProperties
            // should be reflected in MergeProperties as well.
            if (properties == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull(nameof(properties));
            }

            if (disposed)
            {
                ThrowDisposed();
            }

            if (properties.properties != null)
            {
                for (int i = 0; i < properties.properties.Length; i++)
                {
                    if (properties.properties[i].Name == null)
                    {
                        break;
                    }

                    Property property = properties.properties[i];

                    // Used with Http transport
                    //IMergeEnabledMessageProperty currentValue;
                    //if (!this.TryGetValue<IMergeEnabledMessageProperty>(property.Name, out currentValue) ||
                    //    !currentValue.TryMergeWithProperty(property.Value))
                    //{
                        // Merge wasn't possible so copy
                        // this[string] will call CreateCopyOfPropertyValue, so we don't need to repeat that here
                        this[property.Name] = property.Value;
                    //}
                }
            }

            Via = properties.Via;
            AllowOutputBatching = properties.AllowOutputBatching;
            //this.Security = (properties.Security != null) ? (SecurityMessageProperty)properties.Security.CreateCopy() : null;
            Encoder = properties.Encoder;
        }

        internal void CopyProperties(KeyValuePair<string, object>[] array)
        {
            if (disposed)
            {
                ThrowDisposed();
            }

            for (int i = 0; i < array.Length; i++)
            {
                KeyValuePair<string, object> property = array[i];

                // this[string] will call CreateCopyOfPropertyValue, so we don't need to repeat that here
                this[property.Key] = property.Value;
            }
        }

        public bool ContainsKey(string name)
        {
            if (disposed)
            {
                ThrowDisposed();
            }

            if (name == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull(nameof(name));
            }

            int index = FindProperty(name);
            switch (index)
            {
                case ViaIndex:
                    return (object)via != null;
                case AllowOutputBatchingIndex:
                    return (object)allowOutputBatching != null;
                //case SecurityIndex:
                //    return (object)security != null;
                case EncoderIndex:
                    return (object)encoder != null;
                case NotFoundIndex:
                    return false;
                default:
                    return true;
            }
        }

        private object CreateCopyOfPropertyValue(object propertyValue)
        {
            IMessageProperty messageProperty = propertyValue as IMessageProperty;
            if (messageProperty == null)
            {
                return propertyValue;
            }

            object copy = messageProperty.CreateCopy();
            if (copy == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentException(SR.MessagePropertyReturnedNullCopy));
            }

            return copy;
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;

            if (properties != null)
            {
                for (int i = 0; i < properties.Length; i++)
                {
                    if (properties[i].Name == null)
                    {
                        break;
                    }

                    properties[i].Dispose();
                }
            }

            //if (this.security != null)
            //{
            //    this.security.Dispose();
            //}
        }

        private int FindProperty(string name)
        {
            if (name == ViaKey)
            {
                return ViaIndex;
            }
            else if (name == AllowOutputBatchingKey)
            {
                return AllowOutputBatchingIndex;
            }
            else if (name == EncoderKey)
            {
                return EncoderIndex;
            }
            else if (name == SecurityKey)
            {
                return SecurityIndex;
            }

            if (properties != null)
            {
                for (int i = 0; i < properties.Length; i++)
                {
                    string propertyName = properties[i].Name;

                    if (propertyName == null)
                    {
                        break;
                    }

                    if (propertyName == name)
                    {
                        return i;
                    }
                }
            }

            return NotFoundIndex;
        }

        internal void Recycle()
        {
            disposed = false;
            Clear();
        }

        public bool Remove(string name)
        {
            if (disposed)
            {
                ThrowDisposed();
            }

            int originalPropertyCount = propertyCount;
            UpdateProperty(name, null, false);
            return originalPropertyCount != propertyCount;
        }

        public bool TryGetValue(string name, out object value)
        {
            if (disposed)
            {
                ThrowDisposed();
            }

            if (name == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull(nameof(name));
            }

            int index = FindProperty(name);
            switch (index)
            {
                case ViaIndex:
                    value = via;
                    break;
                case AllowOutputBatchingIndex:
                    value = allowOutputBatching;
                    break;
                //case SecurityIndex:
                //    value = security;
                //    break;
                case EncoderIndex:
                    value = encoder;
                    break;
                case NotFoundIndex:
                    value = null;
                    break;
                default:
                    value = properties[index].Value;
                    break;
            }

            return value != null;
        }

        internal bool TryGetValue<TProperty>(string name, out TProperty property)
        {
            object o;
            if (TryGetValue(name, out o))
            {
                property = (TProperty)o;
                return true;
            }
            else
            {
                property = default(TProperty);
                return false;
            }
        }

        internal TProperty GetValue<TProperty>(string name) where TProperty : class
        {
            return GetValue<TProperty>(name, false);
        }

        internal TProperty GetValue<TProperty>(string name, bool ensureTypeMatch) where TProperty : class
        {
            object obj;
            if (!TryGetValue(name, out obj))
            {
                return null;
            }

            return ensureTypeMatch ? (TProperty)obj : obj as TProperty;
        }

        private void UpdateProperty(string name, object value, bool mustNotExist)
        {
            if (name == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull(nameof(name));
            }

            int index = FindProperty(name);
            if (index != NotFoundIndex)
            {
                if (mustNotExist)
                {
                    bool exists;
                    switch (index)
                    {
                        case ViaIndex:
                            exists = (object)via != null;
                            break;
                        case AllowOutputBatchingIndex:
                            exists = (object)allowOutputBatching != null;
                            break;
                        //case SecurityIndex:
                        //    exists = (object)security != null;
                        //    break;
                        case EncoderIndex:
                            exists = (object)encoder != null;
                            break;
                        default:
                            exists = true;
                            break;
                    }
                    if (exists)
                    {
                        throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentException(SR.Format(SR.DuplicateMessageProperty, name)));
                    }
                }

                if (index >= 0)
                {
                    if (value == null)
                    {
                        properties[index].Dispose();
                        int shiftIndex;
                        for (shiftIndex = index + 1; shiftIndex < properties.Length; shiftIndex++)
                        {
                            if (properties[shiftIndex].Name == null)
                            {
                                break;
                            }

                            properties[shiftIndex - 1] = properties[shiftIndex];
                        }
                        properties[shiftIndex - 1] = new Property();
                        propertyCount--;
                    }
                    else
                    {
                        properties[index].Value = CreateCopyOfPropertyValue(value);
                    }
                }
                else
                {
                    switch (index)
                    {
                        case ViaIndex:
                            Via = (Uri)value;
                            break;
                        case AllowOutputBatchingIndex:
                            AllowOutputBatching = (bool)value;
                            break;
                        //case SecurityIndex:
                        //    if (Security != null)
                        //        Security.Dispose();
                        //    Security = (SecurityMessageProperty)CreateCopyOfPropertyValue(value);
                        //    break;
                        case EncoderIndex:
                            Encoder = (MessageEncoder)value;
                            break;
                        default:
                            Fx.Assert("");
                            throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException());
                    }
                }
            }
            else if (value != null)
            {
                int newIndex;

                if (properties == null)
                {
                    properties = new Property[InitialPropertyCount];
                    newIndex = 0;
                }
                else
                {
                    for (newIndex = 0; newIndex < properties.Length; newIndex++)
                    {
                        if (properties[newIndex].Name == null)
                        {
                            break;
                        }
                    }

                    if (newIndex == properties.Length)
                    {
                        Property[] newProperties = new Property[properties.Length * 2];
                        Array.Copy(properties, newProperties, properties.Length);
                        properties = newProperties;
                    }
                }

                object newValue = CreateCopyOfPropertyValue(value);
                properties[newIndex] = new Property(name, newValue);
                propertyCount++;
            }
        }

        void ICollection<KeyValuePair<string, object>>.CopyTo(KeyValuePair<string, object>[] array, int index)
        {
            if (disposed)
            {
                ThrowDisposed();
            }

            if (array == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull(nameof(array));
            }

            if (array.Length < propertyCount)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentException(SR.MessagePropertiesArraySize0));
            }

            if (index < 0 || index > array.Length - propertyCount)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException(nameof(index), index,
                    SR.Format(SR.ValueMustBeInRange, 0, array.Length - propertyCount)));
            }

            if (via != null)
            {
                array[index++] = new KeyValuePair<string, object>(ViaKey, via);
            }

            if (allowOutputBatching != null)
            {
                array[index++] = new KeyValuePair<string, object>(AllowOutputBatchingKey, allowOutputBatching);
            }

            //if (this.security != null)
            //    array[index++] = new KeyValuePair<string, object>(SecurityKey, this.security.CreateCopy());

            if (encoder != null)
            {
                array[index++] = new KeyValuePair<string, object>(EncoderKey, encoder);
            }

            if (properties != null)
            {
                for (int i = 0; i < properties.Length; i++)
                {
                    string propertyName = properties[i].Name;

                    if (propertyName == null)
                    {
                        break;
                    }

                    array[index++] = new KeyValuePair<string, object>(propertyName, CreateCopyOfPropertyValue(properties[i].Value));
                }
            }
        }

        void ICollection<KeyValuePair<string, object>>.Add(KeyValuePair<string, object> pair)
        {
            if (disposed)
            {
                ThrowDisposed();
            }

            if (pair.Value == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("pair.Value");
            }

            UpdateProperty(pair.Key, pair.Value, true);
        }

        bool ICollection<KeyValuePair<string, object>>.Contains(KeyValuePair<string, object> pair)
        {
            if (disposed)
            {
                ThrowDisposed();
            }

            if (pair.Value == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("pair.Value");
            }

            if (pair.Key == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("pair.Key");
            }

            object value;
            if (!TryGetValue(pair.Key, out value))
            {
                return false;
            }
            return value.Equals(pair.Value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            if (disposed)
            {
                ThrowDisposed();
            }

            return ((IEnumerable<KeyValuePair<string, object>>)this).GetEnumerator();
        }

        IEnumerator<KeyValuePair<string, object>> IEnumerable<KeyValuePair<string, object>>.GetEnumerator()
        {
            if (disposed)
            {
                ThrowDisposed();
            }

            List<KeyValuePair<string, object>> pairs = new List<KeyValuePair<string, object>>(propertyCount);

            if (via != null)
            {
                pairs.Add(new KeyValuePair<string, object>(ViaKey, via));
            }

            if (allowOutputBatching != null)
            {
                pairs.Add(new KeyValuePair<string, object>(AllowOutputBatchingKey, allowOutputBatching));
            }

            //if (this.security != null)
            //    pairs.Add(new KeyValuePair<string, object>(SecurityKey, security));

            if (encoder != null)
            {
                pairs.Add(new KeyValuePair<string, object>(EncoderKey, encoder));
            }

            if (properties != null)
            {
                for (int i = 0; i < properties.Length; i++)
                {
                    string propertyName = properties[i].Name;

                    if (propertyName == null)
                    {
                        break;
                    }

                    pairs.Add(new KeyValuePair<string, object>(propertyName, properties[i].Value));
                }
            }

            return pairs.GetEnumerator();
        }

        bool ICollection<KeyValuePair<string, object>>.Remove(KeyValuePair<string, object> pair)
        {
            if (disposed)
            {
                ThrowDisposed();
            }

            if (pair.Value == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("pair.Value");
            }

            if (pair.Key == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("pair.Key");
            }

            object value;
            if (!TryGetValue(pair.Key, out value))
            {
                return false;
            }
            if (!value.Equals(pair.Value))
            {
                return false;
            }
            Remove(pair.Key);
            return true;
        }

        private struct Property : IDisposable
        {
            private string name;
            private object value;

            public Property(string name, object value)
            {
                this.name = name;
                this.value = value;
            }

            public string Name
            {
                get { return name; }
            }

            public object Value
            {
                get { return value; }
                set { this.value = value; }
            }

            public void Dispose()
            {
                IDisposable disposable = value as IDisposable;
                if (disposable != null)
                {
                    disposable.Dispose();
                }
            }
        }
    }
}