using System;
using System.Collections.Generic;
using System.Text;

namespace CoreWCF.Primitives.Tests
{
    internal class SimpleService : ISimpleService
    {
        public string Echo(string echo)
        {
            return echo;
        }
    }

    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    internal class SimpleSingletonService : ISimpleService
    {
        public string Echo(string echo)
        {
            return echo;
        }
    }
}
