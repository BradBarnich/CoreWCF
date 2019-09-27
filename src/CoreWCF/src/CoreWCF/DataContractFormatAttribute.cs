using System;

namespace CoreWCF
{
    internal sealed class DataContractFormatAttribute : Attribute
    {
        private OperationFormatStyle style;
        public OperationFormatStyle Style
        {
            get { return style; }
            set
            {
                XmlSerializerFormatAttribute.ValidateOperationFormatStyle(style);
                style = value;
            }
        }

    }
}