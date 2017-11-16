using Native;

namespace Minimal
{
    using System;
    using System.IO;
    using System.Security.Principal;

    public abstract class FileDetailBase
    {
        private DateTime _creationTimeUtc;
        private DateTime _lastAccessTimeUtc;
        private DateTime _lastWriteTimeUtc;

        public static FileAttributes ForceFileAttributesExistance(FileAttributes source, FileAttributes attr, bool existance)
        {
            var source1 = source | attr;
            var source2 = source & attr;
            return existance ? source1 : source2;
        }

        /// <summary>
        /// True if file is readonly. Cached.
        /// </summary>
        public Boolean IsReadOnly
        {
            get
            {
                return (Attributes & FileAttributes.ReadOnly) != 0;
            }
            set
            {
                ForceFileAttributesExistance(Attributes, FileAttributes.ReadOnly, value);
                NativeIO.SetAttributes(PathInfo, Attributes);
            }
        }

        /// <summary>
        /// PathInfo Container
        /// </summary>
        public PathInfo PathInfo { get; protected internal set; }

        /// <summary>
        /// Initializes a new instance of the QuickIOAbstractBase class, which acts as a wrapper for a file path.
        /// </summary>
        /// <param name="pathInfo"><see cref="Minimal.PathInfo"/></param>
        /// <param name="findData"><see cref="Win32FindData"/></param>
        internal FileDetailBase(PathInfo pathInfo, Win32FindData findData)
        {
            FindData = findData;
            PathInfo = pathInfo;
            if (findData != null)
            {
                Attributes = findData.dwFileAttributes;
            }
        }

        /// <summary>
        /// Name of file or directory
        /// </summary>
        public String Name { get { return PathInfo.Name; } }

        /// <summary>
        /// Full path of the directory or file.
        /// </summary>
        public String FullName { get { return PathInfo.FullName; } }
        /// <summary>
        /// Full path of the directory or file (unc format)
        /// </summary>
        public String FullNameUnc { get { return PathInfo.FullNameUnc; } }

        /// <summary>
        /// Fullname of Parent.
        /// </summary>
        public String ParentFullName { get { return PathInfo.ParentFullName; } }
        /// <summary>
        /// Parent. 
        /// </summary>
        public PathInfo Parent { get { return PathInfo.Parent; } }

        public FileSecurity GetFileSystemSecurity()
        {
            return PathInfo.GetFileSystemSecurity();
        }

        /// <summary>
        /// Fullname of Root. null if current path is root.
        /// </summary>
        public String RootFullName { get { return PathInfo.RootFullName; } }
        /// <summary>
        /// Returns Root or null if current path is root
        /// </summary>
        public PathInfo Root { get { return PathInfo.Root; } }


        /// <summary>
        /// Attributes (Cached Value)
        /// </summary>
        public FileAttributes Attributes { get; protected internal set; }

        #region UNC Times
        /// <summary>
        /// Gets the creation time (UTC)
        /// </summary>
        public DateTime CreationTimeUtc
        {
            get
            {
                if (PathInfo.IsRoot)
                {
                    throw new NotSupportedException("Root directory does not provide time access");
                }
                return _creationTimeUtc;
            }
            protected set
            {
                if (PathInfo.IsRoot)
                {
                    throw new NotSupportedException("Root directory does not provide time access");
                }
                _creationTimeUtc = value;
            }
        }

        /// <summary>
        /// Gets the time (UTC) of last access. 
        /// </summary>
        public DateTime LastAccessTimeUtc
        {
            get
            {
                if (PathInfo.IsRoot)
                {
                    throw new NotSupportedException("Root directory does not provide time access");
                }
                return _lastAccessTimeUtc;
            }
            protected set
            {
                if (PathInfo.IsRoot)
                {
                    throw new NotSupportedException("Root directory does not provide time access");
                }
                _lastAccessTimeUtc = value;
            }
        }

        /// <summary>
        /// Gets the time (UTC) was last written to
        /// </summary>
        public DateTime LastWriteTimeUtc
        {
            get
            {
                if (PathInfo.IsRoot)
                {
                    throw new NotSupportedException("Root directory does not provide time access");
                }
                return _lastWriteTimeUtc;
            }
            protected set
            {
                if (PathInfo.IsRoot)
                {
                    throw new NotSupportedException("Root directory does not provide time access");
                }
                _lastWriteTimeUtc = value;
            }
        }

        #endregion

        #region LocalTime
        /// <summary>
        /// Gets the creation time
        /// </summary>
        public DateTime CreationTime { get { return CreationTimeUtc.ToLocalTime(); } }

        /// <summary>
        /// Gets the time that the  file was last accessed
        /// </summary>
        public DateTime LastAccessTime { get { return LastAccessTimeUtc.ToLocalTime(); } }

        /// <summary>
        /// Gets the time the file was last written to.
        /// </summary>
        public DateTime LastWriteTime { get { return LastWriteTimeUtc.ToLocalTime(); } }
        #endregion

        public override string ToString()
        {
            return FullName;
        }

        /// <summary>
        /// Win32ApiFindData bag
        /// </summary>
        internal Win32FindData FindData { get; private set; }

        /// <summary>
        /// Determines the owner
        /// </summary>
        /// <returns><see cref="NTAccount"/></returns>
        public NTAccount GetOwner()
        {
            return PathInfo.GetOwner();
        }

        /// <summary>
        /// Determines the owner
        /// </summary>
        /// <returns><see cref="IdentityReference"/></returns>
        public IdentityReference GetOwnerIdentifier()
        {
            return PathInfo.GetOwnerIdentifier();
        }

        /// <summary>
        /// Determines the owner
        /// </summary>
        /// <returns><see cref="IdentityReference"/></returns>
        public void SetOwner(NTAccount newOwner)
        {
            PathInfo.SetOwner(newOwner);
        }

        /// <summary>
        /// Sets the owner
        /// </summary>
        public void SetOwner(IdentityReference newOwersIdentityReference)
        {
            PathInfo.SetOwner(newOwersIdentityReference);
        }
    }
    /// <summary>
    /// Provides properties and instance methods for directories
    /// </summary>
    public sealed class DirectoryDetail : FileDetailBase
    {


        /// <summary>
        /// Create new instance of <see cref="DirectoryDetail"/>
        /// </summary>
        public DirectoryDetail(String path) : this(new PathInfo(path)) { }

        /// <summary>
        /// Create new instance of <see cref="DirectoryDetail"/>
        /// </summary>
        public DirectoryDetail(PathInfo pathInfo) : this(pathInfo, pathInfo.IsRoot ? null : NativeIO.GetFindDataFromPath(pathInfo)) { }

        /// <summary>
        /// Creates the folder information on the basis of the path and the handles
        /// </summary>
        /// <param name="pathInfo"><see cref="PathInfo"/></param>
        /// <param name="win32FindData"><see cref="Win32FindData"/></param>
        internal DirectoryDetail(PathInfo pathInfo, Win32FindData win32FindData) :
            base(pathInfo, win32FindData)
        {
            if (win32FindData != null)
            {
                RetriveDateTimeInformation(win32FindData);
            }
        }

        /// <summary>
        /// Determines the time stamp of the given <see cref="Win32FindData"/>
        /// </summary>
        /// <param name="win32FindData"><see cref="Win32FindData"/></param>
        private void RetriveDateTimeInformation(Win32FindData win32FindData)
        {
            LastWriteTimeUtc = win32FindData.GetLastWriteTimeUtc();
            LastAccessTimeUtc = win32FindData.GetLastAccessTimeUtc();
            CreationTimeUtc = win32FindData.GetCreationTimeUtc();
        }
    }

    public sealed class FileDetail : FileDetailBase
    {
        /// <summary>
        /// Create new instance of <see cref="FileDetail"/>
        /// </summary>
        public FileDetail(String path) : this(new PathInfo(path)) { } 

        /// <summary>
        /// Create new instance of <see cref="FileDetail"/>
        /// </summary>
        public FileDetail(PathInfo pathInfo) : this(pathInfo, NativeIO.GetFindDataFromPath(pathInfo)) { }

        /// <summary>
        /// Creates the file information on the basis of the path and <see cref="Win32FindData"/>
        /// </summary>
        /// <param name="fullName">Full path to the file</param>
        /// <param name="win32FindData"><see cref="Win32FindData"/></param>
        internal FileDetail(String fullName, Win32FindData win32FindData)
            : this(new PathInfo(fullName), win32FindData)
        {
            RetriveDateTimeInformation(win32FindData);
            CalculateSize(win32FindData);
        }

        /// <summary>
        /// Creates the file information on the basis of the path and <see cref="Win32FindData"/>
        /// </summary>
        /// <param name="pathInfo">Full path to the file</param>
        /// <param name="win32FindData"><see cref="Win32FindData"/></param>
        internal FileDetail(PathInfo pathInfo, Win32FindData win32FindData)
            : base(pathInfo, win32FindData)
        {
            RetriveDateTimeInformation(win32FindData);
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
        /// Determines the time stamp of the given <see cref="Win32FindData"/>
        /// </summary>
        /// <param name="win32FindData"><see cref="Win32FindData"/></param>
        private void RetriveDateTimeInformation(Win32FindData win32FindData)
        {
            LastWriteTimeUtc = win32FindData.GetLastWriteTimeUtc();
            LastAccessTimeUtc = win32FindData.GetLastAccessTimeUtc();
            CreationTimeUtc = win32FindData.GetCreationTimeUtc();
        }

        /// <summary>
        /// Calculates the size of the file from the handle
        /// </summary>
        /// <param name="win32FindData"></param>
        private void CalculateSize(Win32FindData win32FindData)
        {
            Bytes = win32FindData.CalculateBytes();
        }
    }
}