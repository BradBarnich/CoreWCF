// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


namespace CoreWCF.Channels
{
    /// <summary>
    /// Supported compression formats
    /// </summary>
    public enum CompressionFormat
    {
        /// <summary>
        /// Default to compression off
        /// </summary>
        None,

        /// <summary>
        /// GZip compression
        /// </summary>
        GZip,

        /// <summary>
        /// Deflate compression
        /// </summary>
        Deflate,
    }
}
