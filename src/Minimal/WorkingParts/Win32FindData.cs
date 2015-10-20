namespace Minimal
{
    using System;
    using System.IO;
    using System.Runtime.InteropServices;

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
}