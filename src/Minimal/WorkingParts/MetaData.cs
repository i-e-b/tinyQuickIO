namespace Minimal
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;

    public abstract class MetadataBase
    {
        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="uncResultPath"></param>
        protected MetadataBase(string uncResultPath)
        {
            FullNameUnc = uncResultPath;
        }

        /// <summary>
        /// Transfers data from find data
        /// </summary>
        internal void SetFindData(Win32FindData win32FindData)
        {
            LastWriteTimeUtc = win32FindData.GetLastWriteTimeUtc();
            LastAccessTimeUtc = win32FindData.GetLastAccessTimeUtc();
            CreationTimeUtc = win32FindData.GetCreationTimeUtc();

            Name = win32FindData.cFileName;

            Attributes = win32FindData.dwFileAttributes;
        }

        /// <summary>
        /// Name of file or directory
        /// </summary>
        public String Name { get; private set; }
        /// <summary>
        /// Path to file or directory (unc format)
        /// </summary>
        public String FullNameUnc { get; private set; }

        #region FileTimes
        /// <summary>
        /// Gets the creation time (UTC)
        /// </summary>
        public DateTime CreationTimeUtc { get; private set; }
        /// <summary>
        /// Gets the creation time
        /// </summary>
        public DateTime CreationTime
        {
            get
            {
                return LastWriteTimeUtc.ToLocalTime();
            }
        }

        /// <summary>
        /// Gets the time (UTC) of last access. 
        /// </summary>
        public DateTime LastAccessTimeUtc { get; private set; }
        /// <summary>
        /// Gets the time that the  file was last accessed
        /// </summary>
        public DateTime LastAccessTime
        {
            get
            {
                return LastAccessTimeUtc.ToLocalTime();
            }
        }

        /// <summary>
        /// Gets the time (UTC) was last written to
        /// </summary>
        public DateTime LastWriteTimeUtc { get; private set; }
        /// <summary>
        /// Gets the time the file was last written to.
        /// </summary>
        public DateTime LastWriteTime
        {
            get
            {
                return LastWriteTimeUtc.ToLocalTime();
            }
        }
        #endregion

        /// <summary>
        /// File Attributes
        /// </summary>
        public FileAttributes Attributes { get; internal set; }

        /// <summary>
        /// Returns a new instance of <see cref="PathInfo"/> of the current path
        /// </summary>
        /// <returns><see cref="PathInfo"/></returns>
        public PathInfo ToPathInfo()
        {
            return new PathInfo(FullNameUnc);
        }
    }

    /// <summary>
    /// File metadata information
    /// </summary>
    public sealed class FileMetadata : MetadataBase
    {
        /// <summary>
        /// Creates instance of <see cref="FileMetadata"/>
        /// </summary>
        /// <param name="uncResultPath">UNC Path of current file</param>
        /// <param name="win32FindData">Win32FindData of current file</param>
        internal FileMetadata(string uncResultPath, Win32FindData win32FindData)
            : base(uncResultPath)
        {
            SetFindData(win32FindData);

            Bytes = win32FindData.CalculateBytes();
        }

        /// <summary>
        /// Size of the file. 
        /// </summary>
        public UInt64 Bytes { get; private set; }
    }

    /// <summary>
    /// Directory metadata information
    /// </summary>
    public sealed class DirectoryMetadata : MetadataBase
    {
        /// <summary>
        /// Creates instance of <see cref="DirectoryMetadata"/>
        /// </summary>
        /// <param name="win32FindData">Win32FindData of current directory</param>
        /// <param name="subDirs">Directories in current directory</param>
        /// <param name="subFiles">Files in current directory</param>
        /// <param name="uncFullname">UNC Path of current directory</param>
        internal DirectoryMetadata(string uncFullname, Win32FindData win32FindData, IList<DirectoryMetadata> subDirs, IList<FileMetadata> subFiles)
            : base(uncFullname)
        {
            Directories = new ReadOnlyCollection<DirectoryMetadata>(subDirs);
            Files = new ReadOnlyCollection<FileMetadata>(subFiles);

            SetFindData(win32FindData);
        }

        /// <summary>
        /// Directories in current directory
        /// </summary>
        public ReadOnlyCollection<DirectoryMetadata> Directories { get; internal set; }

        /// <summary>
        /// Files in current directory
        /// </summary>
        public ReadOnlyCollection<FileMetadata> Files { get; internal set; }

        UInt64? _bytes;
        /// <summary>
        /// Size of the file. 
        /// </summary>
        public UInt64 Bytes
        {
            get
            {
                if (_bytes != null)
                {
                    return (UInt64)_bytes;
                }
                _bytes = Directories.Aggregate<DirectoryMetadata, ulong>(0, (current, t) => current + +t.Bytes) + Files.Aggregate(_bytes, (current, t) => current + +t.Bytes);

                return _bytes ?? 0;
            }
        }
    }
}
