﻿using System;
using System.Collections.Generic;
using System.Text;

namespace CoreWCF.Security
{
    public sealed class WindowsServiceCredential
    {
        private bool allowAnonymousLogons = SspiSecurityTokenProvider.DefaultAllowUnauthenticatedCallers;
        private bool includeWindowsGroups = SspiSecurityTokenProvider.DefaultExtractWindowsGroupClaims;
        private bool isReadOnly;

        internal WindowsServiceCredential()
        {
            // empty
        }

        internal WindowsServiceCredential(WindowsServiceCredential other)
        {
            allowAnonymousLogons = other.allowAnonymousLogons;
            includeWindowsGroups = other.includeWindowsGroups;
            isReadOnly = other.isReadOnly;
        }

        public bool AllowAnonymousLogons
        {
            get
            {
                return allowAnonymousLogons;
            }
            set
            {
                ThrowIfImmutable();
                allowAnonymousLogons = value;
            }
        }

        public bool IncludeWindowsGroups
        {
            get
            {
                return includeWindowsGroups;
            }
            set
            {
                ThrowIfImmutable();
                includeWindowsGroups = value;
            }
        }

        internal void MakeReadOnly()
        {
            isReadOnly = true;
        }

        private void ThrowIfImmutable()
        {
            if (isReadOnly)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(SR.ObjectIsReadOnly));
            }
        }
    }

}