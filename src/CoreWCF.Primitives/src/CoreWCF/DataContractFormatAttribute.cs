using System;

namespace CoreWCF
{
    public sealed class DataContractFormatAttribute : Attribute
    {
        OperationFormatStyle style;
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