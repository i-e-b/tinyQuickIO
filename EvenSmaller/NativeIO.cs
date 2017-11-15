using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace EvenSmaller
{
    /*
     * 
     * This gigantic mess is a cut down version of https://github.com/i-e-b/tinyQuickIO
     * Which enables handling of 32k length paths, where .Net is limited to ~250
     * This copy can also handle symlinks
     * 
     */
    enum Win32SecurityObjectType
    {
        SeUnknownObjectType = 0x0,
        SeFileObject = 0x1,
        SeService = 0x2,
        SePrinter = 0x3,
        SeRegistryKey = 0x4,
        SeLmshare = 0x5,
        SeKernelObject = 0x6,
        SeWindowObject = 0x7,
        SeDsObject = 0x8,
        SeDsObjectAll = 0x9,
        SeProviderDefinedObject = 0xa,
        SeWmiguidObject = 0xb,
        SeRegistryWow6432Key = 0xc
    }

    static class Win32SafeNativeMethods
    {
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
        /// Create directory
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CreateDirectory(string fullName, IntPtr securityAttributes);

        /// <summary>
        /// Creates a file / directory or opens an handle for an existing file.
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
        /// Copy file
        /// </summary>
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CopyFile(
            [MarshalAs(UnmanagedType.LPWStr)] string fullNameSource,
            [MarshalAs(UnmanagedType.LPWStr)] string fullNameTarget,
            [MarshalAs(UnmanagedType.Bool)] bool failOnExists);

        /// <summary>
        /// Gets Attributes of given path
        /// </summary>
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern FileAttributes GetFileAttributes(string fullName);
        
        /// <summary>
        /// Sets Attributes of given path
        /// </summary>
        [DllImport("kernel32.dll")]
        internal static extern bool SetFileAttributes(string lpFileName, [MarshalAs(UnmanagedType.U4)] FileAttributes dwFileAttributes);

        /// <summary>
        /// Close Hnalde
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = false, CharSet = CharSet.Unicode)]
        internal static extern bool FindClose(SafeHandle fileHandle);
    }
    
    [Flags] public enum FileAttributes : uint
    {
        Readonly = 0x00000001,
        Hidden = 0x00000002,
        System = 0x00000004,
        Directory = 0x00000010,
        Archive = 0x00000020,
        Device = 0x00000040,
        Normal = 0x00000080,
        Temporary = 0x00000100,
        SparseFile = 0x00000200,
        ReparsePoint = 0x00000400,
        Compressed = 0x00000800,
        Offline = 0x00001000,
        NotContentIndexed = 0x00002000,
        Encrypted = 0x00004000,
        Write_Through = 0x80000000,
        Overlapped = 0x40000000,
        NoBuffering = 0x20000000,
        RandomAccess = 0x10000000,
        SequentialScan = 0x08000000,
        DeleteOnClose = 0x04000000,
        BackupSemantics = 0x02000000,
        PosixSemantics = 0x01000000,
        OpenReparsePoint = 0x00200000,
        OpenNoRecall = 0x00100000,
        FirstPipeInstance = 0x00080000,
        InvalidFileAttributes =  0xffffffffu
    }


    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    [BestFitMapping(false)]
    public class Win32FindData
    {
        /// <summary>
        /// File Attributes
        /// </summary>
        public FileAttributes dwFileAttributes;

        /// <summary>
        /// Last Creation Time (Low DateTime)
        /// </summary>
        public UInt32 ftCreationTime_dwLowDateTime;

        /// <summary>
        /// Last Creation Time (High DateTime)
        /// </summary>
        public UInt32 ftCreationTime_dwHighDateTime;

        /// <summary>
        /// Last Access Time (Low DateTime)
        /// </summary>
        public UInt32 ftLastAccessTime_dwLowDateTime;

        /// <summary>
        /// Last Access Time (High DateTime)
        /// </summary>
        public UInt32 ftLastAccessTime_dwHighDateTime;

        /// <summary>
        /// Last Write Time (Low DateTime)
        /// </summary>
        public UInt32 ftLastWriteTime_dwLowDateTime;

        /// <summary>
        /// Last Write Time (High DateTime)
        /// </summary>
        public UInt32 ftLastWriteTime_dwHighDateTime;

        /// <summary>
        /// File Size High
        /// </summary>
        public UInt32 nFileSizeHigh;

        /// <summary>
        /// File Size Low
        /// </summary>
        public UInt32 nFileSizeLow;

        /// <summary>
        /// Reserved
        /// </summary>
        public Int32 dwReserved0;

        /// <summary>
        /// Reserved
        /// </summary>
        public int dwReserved1;

        /// <summary>
        /// File name
        /// </summary>
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string cFileName;

        /// <summary>
        /// Alternate File Name
        /// </summary>
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
        public string cAlternateFileName;

        /// <summary>
        /// Creates a new Instance
        /// </summary>
        public static Win32FindData New
        {
            get
            {
                return new Win32FindData();
            }
        }

        /// <summary>
        /// Returns the total size in bytes
        /// </summary>
        /// <returns></returns>
        public UInt64 CalculateBytes()
        {
            return ((UInt64)nFileSizeHigh << 32 | nFileSizeLow);
        }


        internal static DateTime ConvertDateTime(UInt32 high, UInt32 low)
        {
            return DateTime.FromFileTimeUtc((((Int64)high) << 0x20) | low);
        }

        /// <summary>
        /// Gets last write time based on UTC
        /// </summary>
        /// <returns></returns>
        public DateTime GetLastWriteTimeUtc()
        {
            return ConvertDateTime(ftLastWriteTime_dwHighDateTime, ftLastWriteTime_dwLowDateTime);
        }

        /// <summary>
        /// Gets last access time based on UTC
        /// </summary>
        /// <returns></returns>
        public DateTime GetLastAccessTimeUtc()
        {
            return ConvertDateTime(ftLastAccessTime_dwHighDateTime, ftLastAccessTime_dwLowDateTime);
        }

        /// <summary>
        /// Gets the creation time based on UTC
        /// </summary>
        /// <returns></returns>
        public DateTime GetCreationTimeUtc()
        {
            return ConvertDateTime(ftCreationTime_dwHighDateTime, ftCreationTime_dwLowDateTime);
        }


    }

    /// <remarks>
    /// Refer to http://msdn.microsoft.com/en-us/library/windows/hardware/ff552012%28v=vs.85%29.aspx
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    public struct SymbolicLinkReparseData
    {
        private const int maxUnicodePathLength = 33000;

        public uint ReparseTag;
        public ushort ReparseDataLength;
        public ushort Reserved;
        public ushort SubstituteNameOffset;
        public ushort SubstituteNameLength;
        public ushort PrintNameOffset;
        public ushort PrintNameLength;
        public uint Flags;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = maxUnicodePathLength)]
        public byte[] PathBuffer;
    }

    /// <summary>
    /// Provides a class for Win32 safe handle implementations
    /// </summary>
    internal sealed class Win32FileHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        /// <summary>
        /// Initializes a new instance of the Win32ApiFileHandle class, specifying whether the handle is to be reliably released.
        /// </summary>
        [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
        internal Win32FileHandle()
            : base(true)
        {
        }

        /// <summary>
        /// When overridden in a derived class, executes the code required to free the handle.
        /// </summary>
        protected override bool ReleaseHandle()
        {
            if (!(IsInvalid || IsClosed))
            {
                return Win32SafeNativeMethods.FindClose(this);
            }
            return (IsInvalid || IsClosed);
        }

        /// <summary>
        /// Releases the unmanaged resources used by the Win32ApiFileHandle class specifying whether to perform a normal dispose operation. 
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (!(IsInvalid || IsClosed))
            {
                Win32SafeNativeMethods.FindClose(this);
            }
            base.Dispose(disposing);
        }
    }
    /// <summary>
    /// Performs operations for files or directories and path information. 
    /// </summary>
    public static class PathTools
    {
        public const String RegularLocalPathPrefix = @"";
        public const String RegularSharePathPrefix = @"\\";
        public static readonly Int32 RegularSharePathPrefixLength = RegularSharePathPrefix.Length;
        public const String UncLocalPathPrefix = @"\\?\";
        public const String UncSharePathPrefix = @"\\?\UNC\";
        public static readonly Int32 UncSharePathPrefixLength = UncSharePathPrefix.Length;

        /// <summary>
        /// Converts unc path to regular path
        /// </summary>
        public static String ToRegularPath(String anyFullname)
        {
            // First: Check for UNC QuickIOShareInfo
            if (anyFullname.StartsWith(UncSharePathPrefix, StringComparison.Ordinal))
            {
                return ToShareRegularPath(anyFullname); // Convert
            }
            if (anyFullname.StartsWith(UncLocalPathPrefix, StringComparison.Ordinal))
            {
                return ToLocalRegularPath(anyFullname); // Convert
            }
            return anyFullname;
        }

        /// <summary>
        /// Converts an unc path to a local regular path
        /// </summary>
        /// <param name="uncLocalPath">Unc Path</param>
        /// <example>\\?\C:\temp\file.txt >> C:\temp\file.txt</example>
        /// <returns>Local Regular Path</returns>
        public static String ToLocalRegularPath(String uncLocalPath)
        {
            return uncLocalPath.Substring(UncLocalPathPrefix.Length);
        }

        /// <summary>
        /// Converts an unc path to a share regular path
        /// </summary>
        /// <param name="uncSharePath">Unc Path</param>
        /// <example>\\?\UNC\server\share >> \\server\share</example>
        /// <returns>QuickIOShareInfo Regular Path</returns>
        public static String ToShareRegularPath(String uncSharePath)
        {
            return RegularSharePathPrefix + uncSharePath.Substring(UncSharePathPrefix.Length);
        }

        /// <summary>
        /// Gets name of file or directory
        /// </summary>
        /// <param name="fullName">Path</param>
        /// <returns>Name of file or directory</returns>
        public static String GetName(String fullName)
        {
            var path = TrimTrailingSepartor(fullName);
            var sepPosition = path.LastIndexOf(Path.DirectorySeparatorChar);

            return sepPosition == -1 ? path : path.Substring(sepPosition + 1);
        }

        /// <summary>
        /// Removes Last <see cref="Path.DirectorySeparatorChar "/>
        /// </summary>
        private static String TrimTrailingSepartor(String path)
        {
            return path.TrimEnd(Path.DirectorySeparatorChar);
        }

        /// <summary>
        /// Invalid Chars are: " &lt; &gt; | and all chars lower than ASCII value 32
        /// </summary>
        /// <remarks>Ignores Unix File Systems</remarks>
        /// <param name="path">Path to check</param>
        public static void ThrowIfPathContainsInvalidChars(String path)
        {
            if (path.Any(currentChar => currentChar < 32 || currentChar == '\"' || currentChar == '<' || currentChar == '>' || currentChar == '|'))
            {
                throw new Exception("Path contains invalid characters" + path);
            }
        }

        /// <summary>
        /// Combines given path elements
        /// </summary>
        /// <param name="pathElements">Path elements to combine</param>
        /// <returns>Combined Path</returns>
        public static String Combine(params String[] pathElements)
        {
            if (pathElements == null || pathElements.Length == 0)
            {
                throw new ArgumentNullException("pathElements", "Cannot be null or empty");
            }

            // Verify not required; System.IO.Path.Combine calls internal path invalid char verifier

            // First Element
            var combinedPath = pathElements[0];

            // Other elements
            for (var i = 1; i < pathElements.Length; i++)
            {
                var el = pathElements[i];

                // Combine
                combinedPath = Path.Combine(combinedPath, el);
            }

            return combinedPath;
        }

        /// <summary>
        /// Returns true if path is local regular path such as 'C:\folder\folder\file.txt'
        /// </summary>
        /// <param name="path">Path</param>
        /// <returns>True if path is local regular path</returns>
        public static Boolean IsLocalRegularPath(String path)
        {
            return (path.Length >= 3 && Char.IsLetter(path[0]) && path[1] == ':' && path[2] == Path.DirectorySeparatorChar);
        }

        /// <summary>
        /// Returns true if path is local UNC path such as '\\?\C:\folder\folder\file.txt'
        /// </summary>
        /// <param name="path">Path</param>
        /// <returns>True if path is local UNC path</returns>
        public static Boolean IsLocalUncPath(String path)
        {
            return (path.Length >= 7 && path[0] == '\\' && path[1] == '\\' && (path[2] == '?' || path[2] == '.') && path[3] == '\\' && IsLocalRegularPath(path.Substring(4)));
        }

        /// <summary>
        /// Returns true if path is share regular path such as '\\server\share\folder\file.txt'
        /// </summary>
        /// <param name="path">Path</param>
        /// <returns>True if path is share regular path</returns>
        public static Boolean IsShareRegularPath(String path)
        {
            if (!path.StartsWith(RegularSharePathPrefix, StringComparison.Ordinal))
            {
                return false;
            }
            if (path.StartsWith(UncSharePathPrefix, StringComparison.Ordinal))
            {
                return false;
            }

            var pathElements = path.Substring(RegularSharePathPrefixLength).Split('\\');
            return (pathElements.Length >= 2);
        }

        /// <summary>
        /// Returns true if path is share UNC path such as '\\?\UNC\server\share\folder\file.txt'
        /// </summary>
        /// <param name="path">Path</param>
        /// <returns>True if path is share UNC path</returns>
        public static Boolean IsShareUncPath(String path)
        {
            if (!path.StartsWith(UncSharePathPrefix))
            {
                return false;
            }

            var pathElements = path.Substring(UncSharePathPrefixLength).Split('\\');
            return (pathElements.Length >= 2);
        }

        /// <summary>
        /// Try to parse path
        /// </summary>
        /// <param name="path">Path to parse</param>
        /// <param name="parsePathResult">result</param>
        /// <param name="supportRelativePath">true to support relative path</param>
        /// <returns>True on success. <paramref name="parsePathResult"/> is set.</returns>
        public static Boolean TryParsePath(String path, out PathResult parsePathResult, bool supportRelativePath = true)
        {
            if (TryParseLocalRegularPath(path, out parsePathResult))
            {
                return true;
            }
            if (TryParseLocalUncPath(path, out parsePathResult))
            {
                return true;
            }
            if (TryParseShareRegularPath(path, out parsePathResult))
            {
                return true;
            }
            if (TryParseShareUncPath(path, out parsePathResult))
            {
                return true;
            }

            if (supportRelativePath && TryParseLocalRegularPath(Path.GetFullPath(path), out parsePathResult))
            {
                return true;
            }

            return false;
        }

        public static PathResult ParsePath(string path, bool supportRelativePath = true)
        {
            PathResult result;
            if (!TryParsePath(path, out result))
            {
                throw new Exception("Invalid path at " + path);
            }

            return result;
        }


        /// <summary>
        /// Returns true if specified <paramref name="path"/> is local regular path and returns result due to <paramref name="parsePathResult"/>
        /// </summary>
        /// <param name="path">Local path to parse</param>
        /// <param name="parsePathResult"><see cref="PathResult"/></param>
        /// <returns>True if parse succeeded and <paramref name="parsePathResult"/> is filled</returns>
        public static Boolean TryParseLocalRegularPath(String path, out PathResult parsePathResult)
        {
            if (!IsLocalRegularPath(path))
            {
                parsePathResult = null;
                return false;
            }

            parsePathResult = new PathResult { PathLocation = LocalOrShare.Local, PathType = UncOrRegular.Regular };

            if (path.Length == 3)
            {
                parsePathResult.IsRoot = true;
                parsePathResult.ParentPath = null;
                parsePathResult.RootPath = null;
                parsePathResult.Name = null;
                parsePathResult.FullNameUnc = UncLocalPathPrefix + path;
                parsePathResult.FullName = path;
            }
            else
            {
                parsePathResult.IsRoot = false;
                parsePathResult.FullName = path.TrimEnd(Path.DirectorySeparatorChar);
                parsePathResult.FullNameUnc = UncLocalPathPrefix + parsePathResult.FullName;
                parsePathResult.ParentPath = parsePathResult.FullName.Substring(0, parsePathResult.FullName.LastIndexOf(Path.DirectorySeparatorChar));
                parsePathResult.RootPath = path.Substring(0, 3);

                parsePathResult.Name = parsePathResult.FullName.Substring(parsePathResult.FullName.LastIndexOf(Path.DirectorySeparatorChar) + 1);
            }

            return true;
        }

        /// <summary>
        /// Returns true if specified <paramref name="path"/> is local UNC path and returns result due to <paramref name="parsePathResult"/>
        /// </summary>
        /// <param name="path">Local UNC path to parse</param>
        /// <param name="parsePathResult"><see cref="PathResult"/></param>
        /// <returns>True if parse succeeded and <paramref name="parsePathResult"/> is filled</returns>
        public static Boolean TryParseLocalUncPath(String path, out PathResult parsePathResult)
        {
            if (!IsLocalUncPath(path))
            {
                parsePathResult = null;
                return false;
            }

            parsePathResult = new PathResult { PathLocation = LocalOrShare.Local, PathType = UncOrRegular.UNC };

            if (path.Length == 7)
            {
                parsePathResult.IsRoot = true;
                parsePathResult.ParentPath = null;
                parsePathResult.RootPath = null;

                parsePathResult.FullNameUnc = path;
                parsePathResult.FullName = path.Substring(4);
                parsePathResult.Name = null;
            }
            else
            {
                parsePathResult.IsRoot = false;
                parsePathResult.FullNameUnc = path.TrimEnd(Path.DirectorySeparatorChar);
                parsePathResult.FullName = parsePathResult.FullNameUnc.Substring(4);

                parsePathResult.ParentPath = parsePathResult.FullName.Substring(0, parsePathResult.FullName.LastIndexOf(Path.DirectorySeparatorChar));
                parsePathResult.RootPath = path.Substring(4, 3);

                parsePathResult.Name = parsePathResult.FullName.Substring(parsePathResult.FullName.LastIndexOf(Path.DirectorySeparatorChar) + 1);
            }

            return true;
        }

        /// <summary>
        /// Returns true if specified <paramref name="path"/> is share regular path and returns result due to <paramref name="parsePathResult"/>
        /// </summary>
        /// <param name="path">QuickIOShareInfo regular path to parse</param>
        /// <param name="parsePathResult"><see cref="PathResult"/></param>
        /// <returns>True if parse succeeded and <paramref name="parsePathResult"/> is filled</returns>
        public static Boolean TryParseShareRegularPath(String path, out PathResult parsePathResult)
        {
            if (!IsShareRegularPath(path))
            {
                parsePathResult = null;
                return false;
            }

            parsePathResult = new PathResult { PathLocation = LocalOrShare.Share, PathType = UncOrRegular.Regular };

            var cleanedPath = path.TrimEnd('\\');

            var pathElements = cleanedPath.Substring(RegularSharePathPrefixLength).Split('\\');

            var server = pathElements[0];
            var name = pathElements[1];

            var rootPath = RegularSharePathPrefix + server + @"\" + name;

            var completePath = rootPath;
            for (int i = 2; i < pathElements.Length; i++)
            {
                completePath += "\\" + pathElements[i];
            }

            // set
            parsePathResult.IsRoot = (cleanedPath == rootPath);

            if (parsePathResult.IsRoot)
            {
                parsePathResult.ParentPath = null;
                parsePathResult.RootPath = null;
                parsePathResult.Name = null;
                parsePathResult.FullNameUnc = UncSharePathPrefix + server + @"\" + name;
                parsePathResult.FullName = RegularSharePathPrefix + server + @"\" + name;
            }
            else
            {
                parsePathResult.FullName = cleanedPath;
                parsePathResult.FullNameUnc = UncSharePathPrefix + cleanedPath.Substring(2);
                parsePathResult.ParentPath = completePath.Substring(0, completePath.LastIndexOf(Path.DirectorySeparatorChar));
                parsePathResult.RootPath = rootPath;

                parsePathResult.Name = pathElements[pathElements.Length - 1];
            }

            return true;
        }

        /// <summary>
        /// Returns true if specified <paramref name="path"/> is share UNC path and returns result due to <paramref name="parsePathResult"/>
        /// </summary>
        /// <param name="path">QuickIOShareInfo UNC path to parse</param>
        /// <param name="parsePathResult"><see cref="PathResult"/></param>
        /// <returns>True if parse succeeded and <paramref name="parsePathResult"/> is filled</returns>
        public static Boolean TryParseShareUncPath(String path, out PathResult parsePathResult)
        {
            if (!IsShareUncPath(path))
            {
                parsePathResult = null;
                return false;
            }

            parsePathResult = new PathResult { PathLocation = LocalOrShare.Share, PathType = UncOrRegular.UNC };

            var cleanedPath = path.TrimEnd('\\');

            var pathElements = cleanedPath.Substring(UncSharePathPrefixLength).Split('\\');

            var server = pathElements[0];
            var name = pathElements[1];

            var completeRelativePath = server + @"\" + name;
            for (int i = 2; i < pathElements.Length; i++)
            {
                completeRelativePath += "\\" + pathElements[i];
            }

            // set
            parsePathResult.IsRoot = (cleanedPath == (UncSharePathPrefix + server + @"\" + name));

            if (parsePathResult.IsRoot)
            {
                parsePathResult.ParentPath = null;
                parsePathResult.RootPath = null;
                parsePathResult.Name = null;
                parsePathResult.FullNameUnc = UncSharePathPrefix + server + @"\" + name;
                parsePathResult.FullName = RegularSharePathPrefix + server + @"\" + name;
            }
            else
            {
                parsePathResult.FullName = RegularSharePathPrefix + completeRelativePath;
                parsePathResult.FullNameUnc = UncSharePathPrefix + completeRelativePath;
                parsePathResult.ParentPath = RegularSharePathPrefix + completeRelativePath.Substring(0, completeRelativePath.LastIndexOf(Path.DirectorySeparatorChar));
                parsePathResult.RootPath = RegularSharePathPrefix + server + @"\" + name;

                parsePathResult.Name = pathElements[pathElements.Length - 1];
            }

            return true;
        }
    }
    public class PathResult
    {
        /// <summary>
        /// Full root path
        /// </summary>
        /// <example><b>C:\folder\parent\file.txt</b> returns <b>C:\</b></example>
        /// <remarks>Returns null if source path is Root</remarks>
        public String RootPath { get; internal set; }

        /// <summary>
        /// Full parent path
        /// </summary>
        /// <example><b>C:\folder\parent\file.txt</b> returns <b>C:\folder\parent</b></example>
        /// <remarks>Returns null if source path is Root</remarks>
        public String ParentPath { get; internal set; }

        /// <summary>
        /// Name of file or directory
        /// </summary>
        /// <example><b>C:\folder\parent\file.txt</b> returns <b>file.txt</b></example>
        /// <example><b>C:\folder\parent</b> returns <b>parent</b></example>
        /// <remarks>Returns null if source path is Root</remarks>
        public String Name { get; internal set; }

        /// <summary>
        /// True if source path is root
        /// </summary>
        public Boolean IsRoot { get; internal set; }

        /// <summary>
        /// Full path without trailing directory separtor char
        /// </summary>
        public String FullName { get; internal set; }

        /// <summary>
        /// Full UNC path without trailing directory separtor char
        /// </summary>
        public string FullNameUnc { get; internal set; }

        /// <summary>
        /// <see cref="UncOrRegular"/>
        /// </summary>
        public UncOrRegular PathType { get; internal set; }

        /// <summary>
        /// <see cref="LocalOrShare"/>
        /// </summary>
        public LocalOrShare PathLocation { get; internal set; }

    }

    /// <summary>
    /// Provides properties and instance method for paths
    /// </summary>
    public sealed class PathInfo
    {
        /// <summary>
        /// Creates the path information container
        /// </summary>
        /// <param name="anyFullname">Full path to the file or directory (regular or unc)</param>
        public PathInfo(String anyFullname)
            : this(anyFullname, PathTools.GetName(anyFullname))
        {
            PathTools.ThrowIfPathContainsInvalidChars(anyFullname);
        }

        /// <summary>
        /// Creates the path information container
        /// </summary>
        /// <param name="anyFullname">Full path to the file or directory (regular or unc). Relative path will be recognized as local regular path.</param>
        /// <param name="name">Name of file or directory</param>
        public PathInfo(String anyFullname, String name)
        {
            PathResult parsePathResult;
            if (!PathTools.TryParsePath(anyFullname, out parsePathResult))
            {
                // Unknown path
                throw new Exception("Unable to parse path " + anyFullname);
            }

            TransferParseResult(parsePathResult);

            Name = name;
        }

        /// <summary>
        /// Transfers properties from result to current instance
        /// </summary>
        /// <param name="parsePathResult"></param>
        private void TransferParseResult(PathResult parsePathResult)
        {
            FullNameUnc = parsePathResult.FullNameUnc;
            FullName = parsePathResult.FullName;
            ParentFullName = parsePathResult.ParentPath;
            RootFullName = parsePathResult.RootPath;
            IsRoot = parsePathResult.IsRoot;
            PathLocation = parsePathResult.PathLocation;

            if (PathLocation == LocalOrShare.Local)
            {
                var testRoot = IsRoot ? FullName : RootFullName;

                if (!Array.Exists(Environment.GetLogicalDrives(), drve => drve.Equals(testRoot, StringComparison.OrdinalIgnoreCase)))
                {
                    throw new Exception("UnsupportedDriveType " + testRoot);
                }
            }

        }

        /// <summary>
        /// Path to file or directory (regular format)
        /// </summary>
        public String FullName { get; private set; }

        /// <summary>
        /// Path to file or directory (unc format)
        /// </summary>
        public String FullNameUnc { get; private set; }

        /// <summary>
        /// Name of file or directory
        /// </summary>
        public String Name { get; private set; }


        /// <summary>
        /// Fullname of Root. null if current path is root.
        /// </summary>
        public string RootFullName { get; set; }

        /// <summary>
        /// Fullname of Parent. null if current path is root.
        /// </summary>
        public string ParentFullName { get; set; }

        /// <summary>
        /// Parent Directory
        /// </summary>
        public PathInfo Parent
        {
            get { return (ParentFullName == null ? null : new PathInfo(ParentFullName)); }
        }

        /// <summary>
        /// <see cref="LocalOrShare"/> of current path
        /// </summary>
        public LocalOrShare PathLocation { get; private set; }

        /// <summary>
        /// Returns true if current path is root
        /// </summary>
        public bool IsRoot { get; private set; }

        /// <summary>
        /// Returns true if path exists.
        /// </summary>
        /// <returns></returns>
        public Boolean Exists
        {
            get
            {
                return NativeIO.Exists(this);
            }
        }

        /// <summary>
        /// Return a new PathInfo that has the prefix 'source' replaced by 'dest'
        /// </summary>
        public PathInfo Reroot(string source, string dest)
        {
            return new PathInfo(FullName.Replace(source, dest));
        }
    }

    public enum LocalOrShare
    {
        Local,
        Share
    }

    public enum UncOrRegular
    {
        Regular,
        UNC
    }

    public enum FileOrDirectory
    {
        File = 0,
        Directory = 1
    }

    public enum SuppressExceptions
    {
        None,
        SuppressAllExceptions
    }
    
    public class FileDetail
    {
        /// <summary>
        /// Creates the file information on the basis of the path and <see cref="Win32FindData"/>
        /// </summary>
        /// <param name="fullName">Full path to the file</param>
        /// <param name="win32FindData"><see cref="Win32FindData"/></param>
        internal FileDetail(String fullName, Win32FindData win32FindData)
            : this(new PathInfo(fullName), win32FindData)
        {
        }

        /// <summary>
        /// Creates the file information on the basis of the path and <see cref="Win32FindData"/>
        /// </summary>
        /// <param name="pathInfo">Full path to the file</param>
        /// <param name="win32FindData"><see cref="Win32FindData"/></param>
        internal FileDetail(PathInfo pathInfo, Win32FindData win32FindData)
        {
            PathInfo = pathInfo;
            CalculateSize(win32FindData);
        }

        /// <summary>
        /// Size of the file. Cached.
        /// </summary>
        public UInt64 Bytes { get; private set; }

        /// <summary>
        /// Size of the file (returns <see cref="Bytes"/>).
        /// </summary>
        public UInt64 Length { get { return Bytes; } }

        /// <summary>
        /// Calculates the size of the file from the handle
        /// </summary>
        /// <param name="win32FindData"></param>
        private void CalculateSize(Win32FindData win32FindData)
        {
            Bytes = win32FindData.CalculateBytes();
        }

        /// <summary>
        /// PathInfo Container
        /// </summary>
        public PathInfo PathInfo { get; protected internal set; }

        /// <summary>
        /// Name of file or directory
        /// </summary>
        public String Name { get { return PathInfo.Name; } }

        /// <summary>
        /// Full path of the directory or file.
        /// </summary>
        public String FullName { get { return PathInfo.FullName; } }

        public override string ToString() { return FullName; }
    }

    [FileIOPermission(SecurityAction.Demand, AllFiles = FileIOPermissionAccess.AllAccess)]
    public static class NativeIO
    {
        /// <summary>End of enumeration indicator in Win32</summary>
        public const Int32 ERROR_NO_MORE_FILES = 18;

        public static Boolean ContainsFileAttribute(FileAttributes source, FileAttributes attr) { return (source & attr) != 0; }

        public static void NativeExceptionMapping(String path, Int32 errorCode)
        {
            if (errorCode == 0)
            {
                return;
            }

            string affectedPath = PathTools.ToRegularPath(path);

            throw new Exception("Error on '" + affectedPath + "': See InnerException for details.", new Win32Exception(errorCode));
        }


        /// <summary>
        /// Opens a <see cref="FileStream"/> for access at the given path. Ensure stream is correctly disposed.
        /// </summary>
        public static FileStream OpenFileStream(PathInfo pathInfo, FileAccess fileAccess, FileMode fileOption = FileMode.Open, FileShare shareMode = FileShare.Read, Int32 buffer = 0)
        {
            var fileHandle = Win32SafeNativeMethods.CreateFile(pathInfo.FullNameUnc, fileAccess, shareMode, IntPtr.Zero, fileOption, 0, IntPtr.Zero);
            var win32Error = Marshal.GetLastWin32Error();
            if (fileHandle.IsInvalid)
            {
                NativeExceptionMapping(pathInfo.FullName, win32Error); // Throws an exception
            }

            return buffer > 0 ? new FileStream(fileHandle, fileAccess, buffer) : new FileStream(fileHandle, fileAccess);
        }

        /// <summary>
        /// Creates a new directory. If <paramref name="recursive" /> is false, the parent directory must exists.
        /// </summary>
        /// <param name="pathInfo">
        /// Complete path to create
        /// </param>
        /// <param name="recursive">If <paramref name="recursive" /> is false, the parent directory must exist.</param>
        public static void CreateDirectory(PathInfo pathInfo, bool recursive = false)
        {
            if (recursive)
            {
                var parent = pathInfo.Parent;
                if (parent.IsRoot)
                {
                    // Root
                    if (!parent.Exists)
                    {
                        throw new Exception("Root path does not exists. You cannot create a root this way. " + parent.FullName);
                    }
                }
                else if (!parent.Exists)
                {
                    CreateDirectory(parent, true);
                }
            }

            if (pathInfo.Exists)
            {
                return;
            }

            bool created = Win32SafeNativeMethods.CreateDirectory(pathInfo.FullNameUnc, IntPtr.Zero);
            int win32Error = Marshal.GetLastWin32Error();
            if (!created)
            {
                NativeExceptionMapping(pathInfo.FullName, win32Error);
            }
        }

        /// <summary>
        ///     Gets the <see cref="Win32FindData" /> from the passed path.
        /// </summary>
        /// <param name="pathInfo">Path</param>
        /// <param name="pathFindData"><seealso cref="Win32FindData" />. Will be null if path does not exist.</param>
        /// <returns>true if path is valid and <see cref="Win32FindData" /> is set</returns>
        /// <remarks>
        ///     <see>
        ///         <cref>QuickIOCommon.NativeExceptionMapping</cref>
        ///     </see>
        ///     if invalid handle found.
        /// </remarks>
        public static bool TryGetFindDataFromPath(PathInfo pathInfo, out Win32FindData pathFindData)
        {
            var win32FindData = new Win32FindData();
            int win32Error;


            using (var fileHandle = FindFirstSafeFileHandle(pathInfo.FullNameUnc, win32FindData, out win32Error))
            {
                // Take care of invalid handles
                if (fileHandle.IsInvalid)
                {
                    NativeExceptionMapping(pathInfo.FullName, win32Error);
                }

                // Ignore . and .. directories
                if (!IsSystemDirectoryEntry(win32FindData))
                {
                    pathFindData = win32FindData;
                    return true;
                }
            }

            pathFindData = null;
            return false;
        }

        static Boolean IsSystemDirectoryEntry(Win32FindData win32FindData)
        {
            if (win32FindData.cFileName.Length >= 3)
            {
                return false;
            }

            return (win32FindData.cFileName == "." || win32FindData.cFileName == "..");
        }

        /// <summary>
        ///     Returns the <see cref="SafeFileHandle" /> and fills <see cref="Win32FindData" /> from the passes path.
        /// </summary>
        /// <param name="path">Path to the file system entry</param>
        /// <param name="win32FindData"></param>
        /// <param name="win32Error">Last error code. 0 if no error occurs</param>
        /// <returns>
        ///     <see cref="SafeFileHandle" />
        /// </returns>
        static Win32FileHandle FindFirstSafeFileHandle(string path, Win32FindData win32FindData, out Int32 win32Error)
        {
            var result = Win32SafeNativeMethods.FindFirstFile(path, win32FindData);
            win32Error = Marshal.GetLastWin32Error();

            return result;
        }

        /// <summary>
        ///     Reurns true if passed path exists
        /// </summary>
        /// <param name="pathInfo">Path to check</param>
        public static Boolean Exists(PathInfo pathInfo)
        {
            var attributes = Win32SafeNativeMethods.GetFileAttributes(pathInfo.FullNameUnc);

            return !Equals(attributes, FileAttributes.InvalidFileAttributes);
        }

        /// <summary>
        /// Returns true if the file or directory is a reparse point (hopefully a symlink and not a hardlink)
        /// </summary>
        public static Boolean IsSymLink(PathInfo pathInfo)
        {
            return ((uint)Win32SafeNativeMethods.GetFileAttributes(pathInfo.FullNameUnc) & (uint)FileAttributes.ReparsePoint) == (uint)FileAttributes.ReparsePoint;
        }

        /// <summary>
        /// Sets the 'read-only' flag of a file to false
        /// </summary>
        /// <param name="pathInfo"></param>
        public static void EnsureNotReadonly(PathInfo pathInfo)
        {
            var existing = Win32SafeNativeMethods.GetFileAttributes(pathInfo.FullNameUnc);
            if (existing == FileAttributes.InvalidFileAttributes) return;
            var updated = existing & (~FileAttributes.Readonly);
            Win32SafeNativeMethods.SetFileAttributes(pathInfo.FullNameUnc, updated);
        }

        /// <summary>
        ///     Returns the <see cref="Win32FindData" /> from specified <paramref name="pathInfo" />
        /// </summary>
        /// <param name="pathInfo">Path to the file system entry</param>
        /// <returns>
        ///     <see cref="Win32FindData" />
        /// </returns>
        public static Win32FindData GetFindDataFromPath(PathInfo pathInfo)
        {
            var win32FindData = new Win32FindData();
            int win32Error;
            using (var fileHandle = FindFirstSafeFileHandle(pathInfo.FullNameUnc, win32FindData, out win32Error))
            {
                // Take care of invalid handles
                if (fileHandle.IsInvalid)
                {
                    NativeExceptionMapping(pathInfo.FullName, win32Error);
                }

                // Ignore . and .. directories
                if (!IsSystemDirectoryEntry(win32FindData))
                {
                    return win32FindData;
                }
            }

            throw new Exception("PathNotFound " + pathInfo.FullName);
        }

        /// <summary>
        ///     Returns the handle by given path and finddata
        /// </summary>
        /// <param name="uncPath">Specified path</param>
        /// <param name="win32FindData">FindData to fill</param>
        /// <param name="win32Error">Win32Error Code. 0 on success</param>
        /// <returns><see cref="Win32FileHandle" /> of specified path</returns>
        static Win32FileHandle FindFirstFileManaged(String uncPath, Win32FindData win32FindData, out Int32 win32Error)
        {
            var handle = Win32SafeNativeMethods.FindFirstFile(uncPath, win32FindData);
            win32Error = Marshal.GetLastWin32Error();
            return handle;
        }

        /// <summary>
        ///     Read contents of a directory
        /// </summary>
        /// <param name="uncDirectoryPath">Path of the directory</param>
        /// <param name="results">Select to return file, directories, or both</param>
        /// <param name="pattern">Search pattern. Uses Win32 native filtering.</param>
        /// <param name="searchOption">
        ///     <see cref="SearchOption" />
        /// </param>
        /// <param name="enumerateOptions">The enumeration options for exception handling</param>
        /// <returns>Collection of files</returns>
        internal static IEnumerable<FileDetail> EnumerateFiles(String uncDirectoryPath, ResultType results, String pattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly, SuppressExceptions enumerateOptions = SuppressExceptions.None)
        {
            // Match for start of search
            string currentPath = PathTools.Combine(uncDirectoryPath, pattern);

            // Find First file
            var win32FindData = new Win32FindData();
            int win32Error;
            using (var fileHandle = FindFirstFileManaged(currentPath, win32FindData, out win32Error))
            {
                // Take care of invalid handles
                if (fileHandle.IsInvalid && EnumerationHandleInvalidFileHandle(uncDirectoryPath, enumerateOptions, win32Error))
                {
                    yield return null;
                }

                // evaluate results
                do
                {
                    // Ignore . and .. directories
                    if (IsSystemDirectoryEntry(win32FindData))
                    {
                        continue;
                    }

                    // Create hit for current search result
                    var resultPath = PathTools.Combine(uncDirectoryPath, win32FindData.cFileName);

                    // Check for Directory
                    if (ContainsFileAttribute(win32FindData.dwFileAttributes, FileAttributes.Directory))
                    {
                        if (results == ResultType.DirectoriesOnly || results == ResultType.FilesAndDirectories)
                        {
                            yield return new FileDetail(resultPath, win32FindData);
                        }

                        // SubFolders?
                        if (searchOption == SearchOption.AllDirectories)
                        {
                            // check for sym link (always ignored in this copy)
                            if (SymbolicLink.IsSymLink(win32FindData)) { continue; }

                            foreach (var match in EnumerateFiles(resultPath, results, pattern, searchOption, enumerateOptions))
                            {
                                yield return match;
                            }
                        }
                    }
                    else
                    {
                        if (results == ResultType.FilesOnly || results == ResultType.FilesAndDirectories)
                        {
                            yield return new FileDetail(resultPath, win32FindData);
                        }
                    }

                    // Create new FindData object for next result
                    win32FindData = new Win32FindData();
                } // Search for next entry
                while (Win32SafeNativeMethods.FindNextFile(fileHandle, win32FindData));
            }
        }

        /// <summary>
        ///     Loads a file from specified path
        /// </summary>
        /// <param name="pathInfo">Full path</param>
        /// <returns>
        ///     <see cref="FileDetail" />
        /// </returns>
        public static FileDetail ReadFileDetails(PathInfo pathInfo)
        {
            Win32FindData findData;
            if (!TryGetFindDataFromPath(pathInfo, out findData))
            {
                throw new Exception("PathNotFound " + pathInfo.FullName);
            }
            if (DetermineFileSystemEntry(findData) != FileOrDirectory.File)
            {
                throw new Exception("UnmatchedFileSystemEntryType " + FileOrDirectory.File + ", " + FileOrDirectory.Directory + ", " + pathInfo.FullName);
            }
            return new FileDetail(pathInfo, findData);
        }

        internal static FileOrDirectory DetermineFileSystemEntry(Win32FindData findData)
        {
            return !ContainsFileAttribute(findData.dwFileAttributes, FileAttributes.Directory) ? FileOrDirectory.File : FileOrDirectory.Directory;
        }

        /// <summary>
        ///     Handles the options to the fired exception
        /// </summary>
        static bool EnumerationHandleInvalidFileHandle(string path, SuppressExceptions enumerateOptions, int win32Error)
        {
            try
            {
                NativeExceptionMapping(path, win32Error);
            }
            catch (Exception)
            {
                if (enumerateOptions == SuppressExceptions.SuppressAllExceptions)
                {
                    return true;
                }

                throw;
            }
            return false;
        }

        /// <summary>
        ///     Copies a file and overwrite existing files if desired.
        /// </summary>
        /// <param name="sourceFilePath">Full source path</param>
        /// <param name="targetFilePath">Full target path</param>
        /// <param name="overwrite">true to overwrite existing files</param>
        /// <returns>True if copy succeeded, false if not. Check last Win32 Error to get further information.</returns>
        public static bool CopyFile(PathInfo sourceFilePath, PathInfo targetFilePath, bool overwrite = false)
        {
            bool failOnExists = !overwrite;

            EnsureNotReadonly(targetFilePath);
            bool result = Win32SafeNativeMethods.CopyFile(sourceFilePath.FullNameUnc, targetFilePath.FullNameUnc, failOnExists);
            if (!result)
            {
                int win32Error = Marshal.GetLastWin32Error();
                NativeExceptionMapping(sourceFilePath.FullName, win32Error);
            }
            return result;
        }


        public static class SymbolicLink
        {
            private const uint GenericReadAccess = 0x80000000;
            private const uint FileFlagsForOpenReparsePointAndBackupSemantics = 0x02200000;
            private const int ioctlCommandGetReparsePoint = 0x000900A8;
            private const uint OpenExisting = 0x3;
            private const uint PathNotAReparsePointError = 0x80071126;
            private const uint ShareModeAll = 0x7; // Read, Write, Delete
            private const uint SymLinkTag = 0xA000000C;
            private const int TargetIsAFile = 0;
            private const int TargetIsADirectory = 1;

            public static void CreateDirectoryLink(string linkPath, string targetPath)
            {
                if (Win32SafeNativeMethods.CreateSymbolicLink(linkPath, targetPath, TargetIsADirectory) && Marshal.GetLastWin32Error() == 0) { return; }
                try
                {
                    Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
                }
                catch (COMException exception)
                {
                    throw new IOException(exception.Message, exception);
                }
            }

            public static void CreateFileLink(string linkPath, string targetPath)
            {
                if (!Win32SafeNativeMethods.CreateSymbolicLink(linkPath, targetPath, TargetIsAFile))
                {
                    Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
                }
            }

            private static SafeFileHandle getFileHandle(string path)
            {
                return Win32SafeNativeMethods.CreateFile(path, GenericReadAccess, ShareModeAll, IntPtr.Zero, OpenExisting,
                    FileFlagsForOpenReparsePointAndBackupSemantics, IntPtr.Zero);
            }

            public static string GetTarget(string path)
            {
                using (var fileHandle = getFileHandle(path))
                {
                    if (fileHandle.IsInvalid)
                    {
                        Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
                    }

                    int outBufferSize = Marshal.SizeOf(typeof(SymbolicLinkReparseData));
                    IntPtr outBuffer = IntPtr.Zero;
                    SymbolicLinkReparseData reparseDataBuffer;
                    try
                    {
                        outBuffer = Marshal.AllocHGlobal(outBufferSize);
                        int bytesReturned;
                        bool success = Win32SafeNativeMethods.DeviceIoControl(
                            fileHandle.DangerousGetHandle(), ioctlCommandGetReparsePoint, IntPtr.Zero, 0,
                            outBuffer, outBufferSize, out bytesReturned, IntPtr.Zero);

                        fileHandle.Close();

                        if (!success || bytesReturned <= 0)
                        {
                            if (((uint)Marshal.GetHRForLastWin32Error()) == PathNotAReparsePointError)
                            {
                                return null;
                            }
                            Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
                        }

                        reparseDataBuffer = (SymbolicLinkReparseData)Marshal.PtrToStructure(
                            outBuffer, typeof(SymbolicLinkReparseData));
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(outBuffer);
                    }

                    return reparseDataBuffer.ReparseTag != SymLinkTag
                        ? null
                        : Encoding.Unicode.GetString(reparseDataBuffer.PathBuffer, reparseDataBuffer.PrintNameOffset, reparseDataBuffer.PrintNameLength);
                }
            }

            public static bool IsSymLink(Win32FindData win32FindData)
            {
                return
                    ((uint)win32FindData.dwFileAttributes & (uint)FileAttributes.ReparsePoint) == (uint)FileAttributes.ReparsePoint
                    &&
                    ((uint)win32FindData.dwReserved0 & SymLinkTag) == SymLinkTag;
            }
        }
    }

    internal enum ResultType
    {
        FilesOnly, DirectoriesOnly, FilesAndDirectories
    }
}