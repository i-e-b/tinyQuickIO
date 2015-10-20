namespace Minimal
{
    using System;
    using System.IO;
    using System.Security.Principal;

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
            PathType = parsePathResult.PathType;

            if (PathLocation == LocalOrShare.Local)
            {
                var testRoot = IsRoot ? FullName : RootFullName;

                if (!Array.Exists(Environment.GetLogicalDrives(), drve => drve.Equals(testRoot, StringComparison.OrdinalIgnoreCase)))
                {
                    throw new Exception("UnsupportedDriveType "+testRoot);
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
        /// <see cref="PathType"/>
        /// </summary>
        public UncOrRegular PathType { get; set; }

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
        /// FindData
        /// </summary>
        internal Win32FindData FindData
        {
            get
            {
                if (IsRoot)
                {
                    throw new NotSupportedException("Root directory does not provide owner access");
                }
                return _findData ?? (_findData = File.GetFindDataFromPath(this));
            }
            set
            {
                _findData = value;
            }
        }
        private Win32FindData _findData;

        /// <summary>
        /// Attributes. Cached.
        /// </summary>
        /// <exception cref="NotSupportedException">if path is root</exception>
        public FileAttributes Attributes
        {
            get
            {
                if (IsRoot)
                {
                    throw new NotSupportedException("Root directory does not provide attributes");
                }
                return FindData.dwFileAttributes;
            }
        }

        /// <summary>
        /// Returns true if current path is root
        /// </summary>
        public bool IsRoot { get; private set; }

        /// <summary>
        /// Returns Root or null if current path is root
        /// </summary>
        public PathInfo Root
        {
            get { return (RootFullName == null ? null : new PathInfo(RootFullName)); }
        }

        /// <summary>
        /// Returns true if path exists.
        /// </summary>
        /// <returns></returns>
        public Boolean Exists
        {
            get
            {
                return File.Exists(this);
            }
        }

        ///// <summary>
        ///// Returns true if path exists. Checks <see cref="QuickIOFileSystemEntryType"/>
        ///// </summary>
        ///// <returns></returns>
        //public Boolean CheckExistance( QuickIOFileSystemEntryType? systemEntryType = null )
        //{
        //    return systemEntryType == null ? InternalQuickIO.Exists( this ) : InternalQuickIOCommon.Exists( FullNameUnc, ( QuickIOFileSystemEntryType ) systemEntryType );
        //}

        /// <summary>
        /// Returns true if path exists. Checks <see cref="FileOrDirectory"/> against the file system
        /// </summary>
        public FileOrDirectory SystemEntryType
        {
            get
            {
                return File.DetermineFileSystemEntry(this);
            }

        }

        /// <summary>
        /// Returns current <see cref="FileSecurity"/>
        /// </summary>
        /// <returns><see cref="FileSecurity"/></returns>
        public FileSecurity GetFileSystemSecurity()
        {
            return new FileSecurity(this);
        }

        /// <summary>
        /// Determines the owner
        /// </summary>
        /// <returns><see cref="NTAccount"/></returns>
        public NTAccount GetOwner()
        {
            if (IsRoot)
            {
                throw new NotSupportedException("Root directory does not provide owner access");
            }
            return GetOwnerIdentifier().Translate(typeof(NTAccount)) as NTAccount;
        }

        /// <summary>
        /// Determines the owner
        /// </summary>
        /// <returns><see cref="IdentityReference"/></returns>
        public IdentityReference GetOwnerIdentifier()
        {
            if (IsRoot)
            {
                throw new NotSupportedException("Root directory does not provide owner access");
            }
            return GetFileSystemSecurity().FileSystemSecurityInformation.GetOwner(typeof(SecurityIdentifier));
        }

        /// <summary>
        /// Determines the owner
        /// </summary>
        public void SetOwner(NTAccount newOwner)
        {
            if (IsRoot)
            {
                throw new NotSupportedException("Root directory does not provide owner access");
            }
            GetFileSystemSecurity().FileSystemSecurityInformation.SetOwner(newOwner.Translate(typeof(SecurityIdentifier)));
        }

        /// <summary>
        /// Determines the owner
        /// </summary>
        public void SetOwner(IdentityReference newOwersIdentityReference)
        {
            if (IsRoot)
            {
                throw new NotSupportedException("Root directory does not provide owner access");
            }
            GetFileSystemSecurity().FileSystemSecurityInformation.SetOwner(newOwersIdentityReference);
        }
    }
}