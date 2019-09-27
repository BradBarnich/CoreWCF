using System;
using System.Collections.Generic;
using System.Text;

namespace CoreWCF.Primitives.Tests
{
    [ServiceContract]
    internal interface ISimpleService
    {
        [OperationContract]
        string Echo(string echo);
    }
}
