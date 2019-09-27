using CoreWCF;
using System;
using System.Collections.Generic;
using System.Text;

[ServiceContract]
internal interface ISimpleService
{
    [OperationContract]
    string Echo(string echo);
}
