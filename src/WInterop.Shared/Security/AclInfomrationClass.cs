﻿// ------------------------
//    WInterop Framework
// ------------------------

// Copyright (c) Jeremy W. Kuhne. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace WInterop.Security
{
    /// <summary>
    /// Type of information being retrieved from an access control list. [ACL_INFORMATION_CLASS]
    /// </summary>
    /// <remarks>
    /// <see cref="https://msdn.microsoft.com/en-us/library/windows/desktop/aa374935.aspx"/>
    /// </remarks>
    public enum AclInformationClass : uint
    {
        /// <summary>
        /// [AclRevisionInformation] <see cref="AclRevisionInformation"/>
        /// </summary>
        RevisionInformation = 1,

        /// <summary>
        /// [AclSizeInformation] <see cref="AclRevisionInformation"/>
        /// </summary>
        SizeInformation = 2
    }
}
