﻿using System;
using System.Collections.Generic;
using System.Text;

namespace CoreWCF
{
    internal abstract class ServiceModelStrings
    {
        public abstract int Count { get; }
        public abstract string this[int index] { get; }
    }
}
