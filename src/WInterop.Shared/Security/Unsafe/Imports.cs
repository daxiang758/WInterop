﻿// ------------------------
//    WInterop Framework
// ------------------------

// Copyright (c) Jeremy W. Kuhne. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using WInterop.Errors;
using WInterop.Memory;
using WInterop.ProcessAndThreads;

namespace WInterop.Security.Unsafe
{
    /// <summary>
    /// Direct usage of Imports isn't recommended. Use the wrappers that do the heavy lifting for you.
    /// </summary>
    public static partial class Imports
    {
        // Advapi (Advanced API) provides Win32 security and registry calls and as such hosts most
        // of the Authorization APIs.
        //
        // Advapi usually calls the NT Marta provider (Windows NT Multiple Access RouTing Authority).
        // https://msdn.microsoft.com/en-us/library/aa939264.aspx

        // https://msdn.microsoft.com/en-us/library/windows/desktop/aa446585.aspx
        [DllImport(Libraries.Advapi32, SetLastError = true, ExactSpelling = true)]
        public unsafe static extern Boolean32 CreateWellKnownSid(
            WellKnownSID WellKnownSidType,
            SID* DomainSid,
            SID* pSid,
            ref uint cbSid);

        // https://msdn.microsoft.com/en-us/library/windows/desktop/aa379154.aspx
        [DllImport(Libraries.Advapi32, ExactSpelling = true)]
        public unsafe static extern Boolean32 IsWellKnownSid(
            in SID pSid,
            WellKnownSID WellKnownSidType);

        // https://msdn.microsoft.com/en-us/library/windows/desktop/aa379151.aspx
        [DllImport(Libraries.Advapi32, ExactSpelling = true)]
        public unsafe static extern Boolean32 IsValidSid(
            in SID pSid);

        // https://msdn.microsoft.com/en-us/library/windows/desktop/aa376399.aspx
        [DllImport(Libraries.Advapi32, SetLastError = true, ExactSpelling = true)]
        public static extern Boolean32 ConvertSidToStringSidW(
            in SID Sid,
            out LocalHandle StringSid);

        // https://msdn.microsoft.com/en-us/library/windows/desktop/aa446658.aspx
        [DllImport(Libraries.Advapi32, SetLastError = true, ExactSpelling = true)]
        public unsafe static extern byte* GetSidSubAuthorityCount(
            in SID pSid);

        // https://msdn.microsoft.com/en-us/library/windows/desktop/aa446657.aspx
        [DllImport(Libraries.Advapi32, SetLastError = true, ExactSpelling = true)]
        public unsafe static extern uint* GetSidSubAuthority(
            in SID pSid,
            uint nSubAuthority);

        // https://msdn.microsoft.com/en-us/library/windows/desktop/aa376404.aspx
        [DllImport(Libraries.Advapi32, SetLastError = true, ExactSpelling = true)]
        public unsafe static extern Boolean32 CopySid(
            uint nDestinationSidLength,
            out SID pDestinationSid,
            SID* pSourceSid);

        // https://msdn.microsoft.com/en-us/library/windows/desktop/aa379159.aspx
        [DllImport(Libraries.Advapi32, SetLastError = true, ExactSpelling = true, CharSet = CharSet.Unicode)]
        public unsafe static extern bool LookupAccountNameW(
            string lpSystemName,
            string lpAccountName,
            SID* Sid,
            ref uint cbSid,
            char* ReferencedDomainName,
            ref uint cchReferencedDomainName,
            out SidNameUse peUse);

        // https://msdn.microsoft.com/en-us/library/windows/desktop/aa379166.aspx
        [DllImport(Libraries.Advapi32, SetLastError = true, ExactSpelling = true, CharSet = CharSet.Unicode)]

        public unsafe static extern bool LookupAccountSidW(
            string lpSystemName,
            in SID lpSid,
            SafeHandle lpName,
            ref uint cchName,
            SafeHandle lpReferencedDomainName,
            ref uint cchReferencedDomainName,
            out SidNameUse peUse);

        // https://docs.microsoft.com/en-us/windows/desktop/api/aclapi/nf-aclapi-getsecurityinfo
        [DllImport(Libraries.Advapi32, ExactSpelling = true)]
        public unsafe static extern WindowsError GetSecurityInfo(
            SafeHandle handle,
            ObjectType ObjectType,
            SecurityInformation SecurityInfo,
            SID** ppsidOwner = null,
            SID** ppsidGroup = null,
            ACL** ppDacl = null,
            ACL** ppSacl = null,
            SECURITY_DESCRIPTOR** ppSecurityDescriptor = null);

        // https://msdn.microsoft.com/en-us/library/windows/desktop/aa446645.aspx
        [DllImport(Libraries.Advapi32, ExactSpelling = true, CharSet = CharSet.Unicode)]
        public unsafe static extern WindowsError GetNamedSecurityInfoW(
            string pObjectName,
            ObjectType ObjectType,
            SecurityInformation SecurityInfo,
            SID** ppsidOwner = null,
            SID** ppsidGroup = null,
            ACL** ppDacl = null,
            ACL** ppSacl = null,
            SECURITY_DESCRIPTOR** ppSecurityDescriptor = null);

        // https://docs.microsoft.com/en-us/windows/desktop/api/aclapi/nf-aclapi-setsecurityinfo
        [DllImport(Libraries.Advapi32, ExactSpelling = true)]
        public unsafe static extern WindowsError SetSecurityInfo(
            SafeHandle handle,
            ObjectType ObjectType,
            SecurityInformation SecurityInfo,
            SID* psidOwner,
            SID* psidGroup,
            ACL* pDacl,
            ACL* pSacl);

        // https://msdn.microsoft.com/en-us/library/windows/desktop/aa446635.aspx
        [DllImport(Libraries.Advapi32, SetLastError = true, ExactSpelling = true)]
        public unsafe static extern Boolean32 GetAclInformation(
            SecurityDescriptor pAcl,
            void* pAclInformation,
            uint nAclInformationLength,
            AclInformationClass dwAclInformationClass);

        // https://msdn.microsoft.com/en-us/library/windows/desktop/aa374951.aspx
        [DllImport(Libraries.Advapi32, SetLastError = true, ExactSpelling = true)]
        public unsafe static extern Boolean32 AddAccessAllowedAceEx(
            SecurityDescriptor pAcl,
            uint dwAceRevision,
            // This is AceInheritence
            uint AceFlags,
            AccessMask AccessMask,
            SID* pSid);

        // https://msdn.microsoft.com/en-us/library/windows/desktop/aa379296.aspx
        [DllImport(Libraries.Advapi32, SetLastError = true, ExactSpelling = true)]
        public static extern bool OpenThreadToken(
            ThreadHandle ThreadHandle,
            AccessTokenRights DesiredAccess,
            Boolean32 OpenAsSelf,
            out AccessToken TokenHandle);

        // https://msdn.microsoft.com/en-us/library/windows/desktop/aa379295.aspx
        [DllImport(Libraries.Advapi32, SetLastError = true, ExactSpelling = true)]
        public static extern Boolean32 OpenProcessToken(
            IntPtr ProcessHandle,
            AccessTokenRights DesiredAccesss,
            out AccessToken TokenHandle);

        // https://msdn.microsoft.com/en-us/library/windows/desktop/aa446617.aspx
        [DllImport(Libraries.Advapi32, SetLastError = true, ExactSpelling = true)]
        public unsafe static extern Boolean32 DuplicateTokenEx(
            AccessToken hExistingToken,
            AccessTokenRights dwDesiredAccess,
            SECURITY_ATTRIBUTES* lpTokenAttributes,
            ImpersonationLevel ImpersonationLevel,
            TokenType TokenType,
            out AccessToken phNewToken);

        // https://msdn.microsoft.com/en-us/library/windows/desktop/aa379590.aspx
        [DllImport(Libraries.Advapi32, SetLastError = true, ExactSpelling = true)]
        public static extern Boolean32 SetThreadToken(
            ThreadHandle Thread,
            AccessToken Token);

        // https://msdn.microsoft.com/en-us/library/aa379180.aspx
        [DllImport(Libraries.Advapi32, CharSet = CharSet.Unicode, SetLastError = true, ExactSpelling = true)]
        public static extern Boolean32 LookupPrivilegeValueW(
            string lpSystemName,
            string lpName,
            out LUID lpLuid);

        // https://msdn.microsoft.com/en-us/library/windows/desktop/aa379176.aspx
        [DllImport(Libraries.Advapi32, SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true)]
        public static extern Boolean32 LookupPrivilegeNameW(
            IntPtr lpSystemName,
            ref LUID lpLuid,
            ref char lpName,
            ref uint cchName);

        // https://msdn.microsoft.com/en-us/library/windows/desktop/aa446671.aspx
        [DllImport(Libraries.Advapi32, SetLastError = true, ExactSpelling = true)]
        public unsafe static extern Boolean32 GetTokenInformation(
            AccessToken TokenHandle,
            TokenInformation TokenInformationClass,
            void* TokenInformation,
            uint TokenInformationLength,
            out uint ReturnLength);

        // https://msdn.microsoft.com/en-us/library/windows/desktop/aa375202.aspx
        [DllImport(Libraries.Advapi32, SetLastError = true, ExactSpelling = true)]
        public unsafe static extern Boolean32 AdjustTokenPrivileges(
            AccessToken TokenHandle,
            Boolean32 DisableAllPrivileges,
            TOKEN_PRIVILEGES* NewState,
            uint BufferLength,
            TOKEN_PRIVILEGES* PreviousState,
            out uint ReturnLength);

        // https://docs.microsoft.com/en-us/windows/desktop/api/Aclapi/nf-aclapi-getexplicitentriesfromaclw
        [DllImport(Libraries.Advapi32, ExactSpelling = true)]
        public unsafe static extern WindowsError GetExplicitEntriesFromAclW(
            ACL* pacl,
            out uint pcCountOfExplicitEntries,
            EXPLICIT_ACCESS** pListOfExplicitEntries);

        // https://docs.microsoft.com/en-us/windows/desktop/api/aclapi/nf-aclapi-setentriesinaclw
        [DllImport(Libraries.Advapi32, ExactSpelling = true)]
        public unsafe static extern WindowsError SetEntriesInAclW(
            uint cCountOfExplicitEntries,
            EXPLICIT_ACCESS* pListOfExplicitEntries,
            ACL* OldAcl,
            ACL** NewAcl);
    }
}
