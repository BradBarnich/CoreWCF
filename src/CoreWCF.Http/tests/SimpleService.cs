using System;
using System.Collections.Generic;
using System.Text;

internal class SimpleService : ISimpleService
{
    public string Echo(string echo)
    {
        return echo;
    }
}
