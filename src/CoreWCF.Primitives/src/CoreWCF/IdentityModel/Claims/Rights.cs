﻿namespace CoreWCF.IdentityModel.Claims
{
    internal static class Rights
    {
        private const string rightNamespace = XsiConstants.Namespace + "/right";

        private const string identity = rightNamespace + "/identity";
        private const string possessProperty = rightNamespace + "/possessproperty";

        static public string Identity { get { return identity; } }
        static public string PossessProperty { get { return possessProperty; } }

    }
}