namespace Minimal
{
    using System;
    using System.IO;
    using System.Runtime.InteropServices;
    using Microsoft.Win32.SafeHandles;

    static class Win32SafeNativeMethods
    {
        #region advapi32.dll
        [DllImport("advapi32.dll", CharSet = CharSet.Auto, ExactSpelling = true, SetLastError = true)]
        internal static extern uint GetSecurityDescriptorLength(IntPtr byteArray);



        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern uint GetNamedSecurityInfo(
            string unicodePath,
            Win32SecurityObjectType securityObjectType,
            Win32FileSystemEntrySecurityInformation securityInfo,
            out IntPtr sidOwner,
            out IntPtr sidGroup,
            out IntPtr dacl,
            out IntPtr sacl,
            out IntPtr securityDescriptor);


        #endregion

        #region kernel32.dll
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern SafeFileHandle CreateFile(
            string lpFileName,
            uint dwDesiredAccess,
            uint dwShareMode,
            IntPtr lpSecurityAttributes,
            uint dwCreationDisposition,
            uint dwFlagsAndAttributes,
            IntPtr hTemplateFile);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool CreateSymbolicLink(string lpSymlinkFileName, string lpTargetFileName, int dwFlags);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool DeviceIoControl(
            IntPtr hDevice,
            uint dwIoControlCode,
            IntPtr lpInBuffer,
            int nInBufferSize,
            IntPtr lpOutBuffer,
            int nOutBufferSize,
            out int lpBytesReturned,
            IntPtr lpOverlapped);

        /// <summary>
        /// Sets the last all times for files or directories
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true, EntryPoint = "SetFileTime", ExactSpelling = true)]
        internal static extern Int32 SetAllFileTimes(SafeFileHandle fileHandle, ref long lpCreationTime, ref long lpLastAccessTime, ref long lpLastWriteTime);

        /// <summary>
        /// Sets the last creation time for files or directories
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true, EntryPoint = "SetFileTime", ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetCreationFileTime(SafeFileHandle hFile, ref long lpCreationTime, IntPtr lpLastAccessTime, IntPtr lpLastWriteTime);

        /// <summary>
        /// Sets the last acess time for files or directories
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true, EntryPoint = "SetFileTime", ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetLastAccessFileTime(SafeFileHandle hFile, IntPtr lpCreationTime, ref long lpLastAccessTime, IntPtr lpLastWriteTime);

        /// <summary>
        /// Sets the last write time for files or directories
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true, EntryPoint = "SetFileTime", ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetLastWriteFileTime(SafeFileHandle hFile, IntPtr lpCreationTime, IntPtr lpLastAccessTime, ref long lpLastWriteTime);

        /// <summary>
        /// Create directory
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CreateDirectory(string fullName, IntPtr securityAttributes);

        /// <summary>
        /// Creates a file / directory or opens an handle for an existing file.
        /// <b>If you want to get an handle for an existing folder use <see cref="OpenReadWriteFileSystemEntryHandle"/> with ( 0x02000000 ) as attribute and FileMode ( 0x40000000 | 0x80000000 )</b>
        /// Otherwise it you'll get an invalid handle
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern SafeFileHandle CreateFile(
            string fullName,
            [MarshalAs(UnmanagedType.U4)] FileAccess dwDesiredAccess,
            [MarshalAs(UnmanagedType.U4)] FileShare dwShareMode,
            IntPtr lpSecurityAttributes,
            [MarshalAs(UnmanagedType.U4)] FileMode dwCreationDisposition,
            [MarshalAs(UnmanagedType.U4)] FileAttributes dwFlagsAndAttributes,
            IntPtr hTemplateFile);

        /// <summary>
        /// Use this to open an handle for an existing file or directory to change for example the timestamps
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "CreateFile")]
        internal static extern SafeFileHandle OpenReadWriteFileSystemEntryHandle(
            string fullName,
            uint dwAccess,
            [MarshalAs(UnmanagedType.U4)] FileShare dwShareMode,
            IntPtr lpSecurityAttributes,
            [MarshalAs(UnmanagedType.U4)]FileMode dwMode,
            uint dwAttribute,
            IntPtr hTemplateFile);

        /// <summary>
        /// Finds first file of given path
        /// </summary>
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern Win32FileHandle FindFirstFile(string fullName, [In, Out] Win32FindData win32FindData);

        /// <summary>
        /// Finds next file of current handle
        /// </summary>
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern bool FindNextFile(Win32FileHandle findFileHandle, [In, Out, MarshalAs(UnmanagedType.LPStruct)] Win32FindData win32FindData);

        /// <summary>
        /// Moves a directory
        /// </summary>
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool MoveFile(string fullNameSource, string fullNameTarget);

        /// <summary>
        /// Copy file
        /// </summary>
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CopyFile(
            [MarshalAs(UnmanagedType.LPWStr)] string fullNameSource,
            [MarshalAs(UnmanagedType.LPWStr)] string fullNameTarget,
            [MarshalAs(UnmanagedType.Bool)] bool failOnExists);

        /// <summary>
        /// Removes a file.
        /// </summary>
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DeleteFile(string fullName);

        /// <summary>
        /// Removes a file.
        /// </summary>
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool RemoveDirectory(string fullName);

        /// <summary>
        /// Set File Attributes
        /// </summary>
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern bool SetFileAttributes(string fullName, uint fileAttributes);

        /// <summary>
        /// Gets Attributes of given path
        /// </summary>
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern uint GetFileAttributes(string fullName);

        /// <summary>
        /// Close Hnalde
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = false, CharSet = CharSet.Unicode)]
        internal static extern bool FindClose(SafeHandle fileHandle);

        /// <summary>
        /// Free unmanaged memory
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = false, CharSet = CharSet.Unicode)]
        internal static extern IntPtr LocalFree(IntPtr handle);

        #endregion

        #region netapi32.dll
        /// <summary>
        /// Enumerate shares (NT)
        /// </summary>
        /// <remarks>http://msdn.microsoft.com/en-us/library/windows/desktop/bb525387(v=vs.85).aspx</remarks>
        [DllImport("netapi32", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern int NetShareEnum(
            string lpServerName,
            int dwLevel,
            out IntPtr lpBuffer,
            int dwPrefMaxLen,
            out int entriesRead,
            out int totalEntries,
            ref int hResume);

        [DllImport("netapi32", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern int NetApiBufferFree(IntPtr lpBuffer);
        #endregion
    }
}