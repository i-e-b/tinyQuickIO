namespace Minimal
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Security.Permissions;
    using Microsoft.Win32.SafeHandles;

    [FileIOPermission(SecurityAction.Demand, AllFiles = FileIOPermissionAccess.AllAccess)]
    public static class File
    {
        public const Int32 ERROR_NO_MORE_FILES = 18;

        public static Boolean ContainsFileAttribute(FileAttributes source, FileAttributes attr)
        {
            return (source & attr) != 0;
        }

        public static void NativeExceptionMapping(String path, Int32 errorCode)
        {
            if (errorCode == 0)
            {
                return;
            }

            string affectedPath = PathTools.ToRegularPath(path);

            throw new Exception("Error on '" + affectedPath + "': See InnerException for details.", new Win32Exception(errorCode));
        }

        internal static FileOrDirectory DetermineFileSystemEntry(PathInfo pathInfo)
        {
            var findData = GetFindDataFromPath(pathInfo);

            return !ContainsFileAttribute(findData.dwFileAttributes, FileAttributes.Directory) ? FileOrDirectory.File : FileOrDirectory.Directory;
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
        ///     Removes a file.
        /// </summary>
        /// <param name="path">Path to the file to remove</param>
        /// <exception cref="FileNotFoundException">This error is fired if the specified file to remove does not exist.</exception>
        public static void DeleteFile(String path)
        {
            bool result = Win32SafeNativeMethods.DeleteFile(path);
            int win32Error = Marshal.GetLastWin32Error();
            if (!result)
            {
                NativeExceptionMapping(path, win32Error);
            }
        }

        /// <summary>
        ///     Removes a file.
        /// </summary>
        /// <param name="pathInfo">PathInfo of the file to remove</param>
        /// <exception cref="FileNotFoundException">This error is fired if the specified file to remove does not exist.</exception>
        public static void DeleteFile(PathInfo pathInfo)
        {
            RemoveAttribute(pathInfo, FileAttributes.ReadOnly);
            DeleteFile(pathInfo.FullNameUnc);
        }

        /// <summary>
        ///     Removes a file.
        /// </summary>
        /// <param name="fileInfo">FileInfo of the file to remove</param>
        public static void DeleteFile(FileDetail fileInfo)
        {
            RemoveAttribute(fileInfo.PathInfo, FileAttributes.ReadOnly);
            DeleteFile(fileInfo.PathInfo);
        }

        /// <summary>
        /// Deletes all files in the given directory. On request  all contents, too.
        /// </summary>
        /// <param name="directoryInfo">Info of directory to clear</param>
        /// <param name="recursive">If <paramref name="recursive"/> is true then all subfolders are also deleted.</param>
        /// <remarks>Function loads every file and attribute. Alls read-only flags will be removed before removing.</remarks>
        public static void DeleteDirectory(DirectoryDetail directoryInfo, bool recursive = false)
        {
            // Contents
            if (recursive)
            {
                // search all contents
                var subFiles = FindPaths(directoryInfo.FullNameUnc, pathFormatReturn: UncOrRegular.UNC);

                foreach (var item in subFiles)
                {
                    DeleteFile(item);
                }


                var subDirs = EnumerateDirectories(directoryInfo.PathInfo);

                foreach (var subDir in subDirs)
                {
                    DeleteDirectory(subDir, true);
                }
            }

            // Remove specified
            var removed = Win32SafeNativeMethods.RemoveDirectory(directoryInfo.FullNameUnc);
            var win32Error = Marshal.GetLastWin32Error();
            if (!removed)
            {
                NativeExceptionMapping(directoryInfo.FullName, win32Error);
            }
        }

        /// <summary>
        ///     Remove a file attribute
        /// </summary>
        /// <param name="pathInfo">Affected target</param>
        /// <param name="attribute">Attribute to remove</param>
        /// <returns>true if removed. false if not exists in attributes</returns>
        public static Boolean RemoveAttribute(PathInfo pathInfo, FileAttributes attribute)
        {
            if ((pathInfo.Attributes & attribute) != attribute)
            {
                return false;
            }
            var attributes = pathInfo.Attributes;
            attributes &= ~attribute;
            SetAttributes(pathInfo, attributes);
            return true;
        }

        /// <summary>
        ///     Adds a file attribute
        /// </summary>
        /// <param name="pathInfo">Affected target</param>
        /// <param name="attribute">Attribute to add</param>
        /// <returns>true if added. false if already exists in attributes</returns>
        public static Boolean AddAttribute(PathInfo pathInfo, FileAttributes attribute)
        {
            if ((pathInfo.Attributes & attribute) == attribute)
            {
                return false;
            }
            var attributes = pathInfo.Attributes;
            attributes |= attribute;
            SetAttributes(pathInfo, attributes);
            return true;
        }

        /// <summary>
        ///     Creates a new directory. If <paramref name="recursive" /> is false, the parent directory must exists.
        /// </summary>
        /// <param name="pathInfo">
        ///     <see cref="PathInfo" />
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
        ///     Deletes all files in the given directory.
        /// </summary>
        /// <param name="directoryPath">Path of directory to clear</param>
        /// <param name="recursive">If true all files in all all subfolders included</param>
        public static void DeleteFiles(String directoryPath, bool recursive = false)
        {
            var allFilePaths = EnumerateFilePaths(directoryPath, "*", recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
            foreach (string filePath in allFilePaths)
            {
                DeleteFile(filePath);
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

        public static Boolean IsSystemDirectoryEntry(Win32FindData win32FindData)
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
            return Exists(pathInfo.FullNameUnc);
        }

        /// <summary>
        ///     Reurns true if passed path exists
        /// </summary>
        /// <param name="fullnameUnc">Path to check</param>
        public static Boolean Exists(String fullnameUnc)
        {
            uint attributes = Win32SafeNativeMethods.GetFileAttributes(fullnameUnc);
            return !Equals(attributes, 0xffffffff);
        }

        ///// <summary>
        ///// Returns the <see cref="Win32FindData"/> from specified <paramref name="fullUncPath"/>
        ///// </summary>
        ///// <param name="fullUncPath">Path to the file system entry</param>
        ///// <returns><see cref="Win32FindData"/></returns>
        ///// <exception cref="PathNotFoundException">This error is fired if the specified path or a part of them does not exist.</exception>
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
        ///     Gets the <see cref="Win32FindData" /> from the passed <see cref="PathInfo" />
        /// </summary>
        /// <param name="pathInfo">Path to the file system entry</param>
        /// <param name="estimatedFileSystemEntryType">Estimated Type (File or Directory)</param>
        /// <returns>
        ///     <seealso cref="Win32FindData" />
        /// </returns>
        public static Win32FindData GetFindDataFromPath(PathInfo pathInfo, FileOrDirectory? estimatedFileSystemEntryType)
        {
            return GetFindDataFromPath(pathInfo.FullNameUnc, estimatedFileSystemEntryType);
        }

        /// <summary>
        ///     Gets the <see cref="Win32FindData" /> from the passed path.
        /// </summary>
        /// <param name="fullUncPath">Path to the file system entry</param>
        /// <param name="estimatedFileSystemEntryType">Estimated Type (File or Directory)</param>
        /// <returns>
        ///     <seealso cref="Win32FindData" />
        /// </returns>
        public static Win32FindData GetFindDataFromPath(String fullUncPath, FileOrDirectory? estimatedFileSystemEntryType)
        {
            var win32FindData = new Win32FindData();
            int win32Error;
            using (var fileHandle = FindFirstSafeFileHandle(fullUncPath, win32FindData, out win32Error))
            {
                // Take care of invalid handles
                if (fileHandle.IsInvalid)
                {
                    NativeExceptionMapping(fullUncPath, win32Error);
                }

                // Ignore . and .. directories
                if (IsSystemDirectoryEntry(win32FindData))
                {
                    throw new Exception("PathNotFound " + fullUncPath);
                }
                // Check for correct type
                switch (estimatedFileSystemEntryType)
                {
                        // Unimportant
                    case null:
                    {
                        return win32FindData;
                    }
                    case FileOrDirectory.Directory:
                    {
                        // Check for directory flag
                        if (ContainsFileAttribute(win32FindData.dwFileAttributes, FileAttributes.Directory))
                        {
                            return win32FindData;
                        }
                        throw new Exception("UnmatchedFileSystemEntryType " + FileOrDirectory.Directory + ", " + FileOrDirectory.File + ", " + fullUncPath);
                    }
                    case FileOrDirectory.File:
                    {
                        // Check for directory flag
                        if (!ContainsFileAttribute(win32FindData.dwFileAttributes, FileAttributes.Directory))
                        {
                            return win32FindData;
                        }
                        throw new Exception("UnmatchedFileSystemEntryType " + FileOrDirectory.File + ", " + FileOrDirectory.Directory + ", " + fullUncPath);
                    }
                }
                return win32FindData;
            }
        }

        /// <summary>
        ///     Returns the <see cref="SafeFileHandle" /> and fills <see cref="Win32FindData" /> from the passes path.
        /// </summary>
        /// <param name="info">Path to the file system entry</param>
        /// <returns>
        ///     <see cref="SafeFileHandle" />
        /// </returns>
        internal static SafeFileHandle CreateSafeFileHandle(PathInfo info)
        {
            return CreateSafeFileHandle(info.FullNameUnc);
        }

        /// <summary>
        ///     Returns the <see cref="SafeFileHandle" /> and fills <see cref="Win32FindData" /> from the passes path.
        /// </summary>
        /// <param name="path">Path to the file system entry</param>
        /// <returns>
        ///     <see cref="SafeFileHandle" />
        /// </returns>
        static SafeFileHandle CreateSafeFileHandle(string path)
        {
            return Win32SafeNativeMethods.CreateFile(path, FileAccess.ReadWrite, FileShare.None, IntPtr.Zero, FileMode.Open, FileAttributes.Normal, IntPtr.Zero);
        }

        /// <summary>
        ///     Returns the <see cref="SafeFileHandle" /> and fills <see cref="Win32FindData" /> from the passes path.
        /// </summary>
        /// <param name="path">Path to the file system entry</param>
        /// <returns>
        ///     <see cref="SafeFileHandle" />
        /// </returns>
        internal static SafeFileHandle OpenReadWriteFileSystemEntryHandle(string path)
        {
            return Win32SafeNativeMethods.OpenReadWriteFileSystemEntryHandle(path, (0x40000000 | 0x80000000), FileShare.Read | FileShare.Write | FileShare.Delete, IntPtr.Zero, FileMode.Open, (0x02000000), IntPtr.Zero);
        }

        /// <summary>
        ///     Determined metadata of directory
        /// </summary>
        /// <param name="pathInfo">Path of the directory</param>
        /// <param name="enumerateOptions">The enumeration options for exception handling</param>
        internal static DirectoryMetadata EnumerateDirectoryMetadata(PathInfo pathInfo, SuppressExceptions enumerateOptions = SuppressExceptions.None)
        {
            return EnumerateDirectoryMetadata(pathInfo.FullNameUnc, pathInfo.FindData, enumerateOptions);
        }

        /// <summary>
        ///     Determined metadata of directory
        /// </summary>
        /// <param name="uncDirectoryPath">Path of the directory</param>
        /// <param name="findData">
        ///     <see cref="Win32FindData" />
        /// </param>
        /// <param name="enumerateOptions">The enumeration options for exception handling</param>
        /// <returns><see cref="DirectoryMetadata" /> started with the given directory</returns>
        internal static DirectoryMetadata EnumerateDirectoryMetadata(String uncDirectoryPath, Win32FindData findData, SuppressExceptions enumerateOptions)
        {
            // Results
            var subFiles = new List<FileMetadata>();
            var subDirs = new List<DirectoryMetadata>();

            // Match for start of search
            string currentPath = PathTools.Combine(uncDirectoryPath, "*");

            // Find First file
            var win32FindData = new Win32FindData();
            int win32Error;
            using (var fileHandle = FindFirstSafeFileHandle(currentPath, win32FindData, out win32Error))
            {
                // Take care of invalid handles
                if (fileHandle.IsInvalid)
                {
                    if (win32Error != ERROR_NO_MORE_FILES)
                    {
                        NativeExceptionMapping(uncDirectoryPath, win32Error);
                    }

                    if (EnumerationHandleInvalidFileHandle(uncDirectoryPath, enumerateOptions, win32Error))
                    {
                        return null;
                    }
                }

                // Treffer auswerten
                do
                {
                    // Ignore . and .. directories
                    if (IsSystemDirectoryEntry(win32FindData))
                    {
                        continue;
                    }

                    // Create hit for current search result
                    var uncResultPath = PathTools.Combine(uncDirectoryPath, win32FindData.cFileName);

                    // if it's a file, add to the collection
                    if (!ContainsFileAttribute(win32FindData.dwFileAttributes, FileAttributes.Directory))
                    {
                        subFiles.Add(new FileMetadata(uncResultPath, win32FindData));
                    }
                    else
                    {
                        subDirs.Add(EnumerateDirectoryMetadata(uncResultPath, win32FindData, enumerateOptions));
                    }
                    // Create new FindData object for next result

                    win32FindData = new Win32FindData();
                } // Search for next entry
                while (Win32SafeNativeMethods.FindNextFile(fileHandle, win32FindData));
            }

            return new DirectoryMetadata(uncDirectoryPath, findData, subDirs, subFiles);
        }

        /// <summary>
        ///     Determined all subfolders of a directory
        /// </summary>
        /// <param name="pathInfo">Path of the directory</param>
        /// <param name="pattern">Search pattern. Uses Win32 native filtering.</param>
        /// <param name="searchOption">
        ///     <see cref="SearchOption" />
        /// </param>
        /// <param name="enumerateOptions">The enumeration options for exception handling</param>
        /// <returns><see cref="DirectoryDetail" /> collection of subfolders</returns>
        internal static IEnumerable<DirectoryDetail> EnumerateDirectories(PathInfo pathInfo, String pattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly, SuppressExceptions enumerateOptions = SuppressExceptions.None)
        {
            // Match for start of search
            string currentPath = PathTools.Combine(pathInfo.FullNameUnc, pattern);

            // Find First file
            var win32FindData = new Win32FindData();
            int win32Error;
            using (var fileHandle = FindFirstSafeFileHandle(currentPath, win32FindData, out win32Error))
            {
                // Take care of invalid handles
                if (fileHandle.IsInvalid)
                {
                    if (win32Error != ERROR_NO_MORE_FILES)
                    {
                        NativeExceptionMapping(pathInfo.FullName, win32Error);
                    }

                    if (EnumerationHandleInvalidFileHandle(pathInfo.FullName, enumerateOptions, win32Error))
                    {
                        yield return null;
                    }
                }

                do
                {
                    // Ignore . and .. directories
                    if (IsSystemDirectoryEntry(win32FindData))
                    {
                        continue;
                    }

                    // Create hit for current search result
                    string resultPath = PathTools.Combine(pathInfo.FullName, win32FindData.cFileName);

                    // Check for Directory
                    if (ContainsFileAttribute(win32FindData.dwFileAttributes, FileAttributes.Directory))
                    {
                        yield return new DirectoryDetail(resultPath, win32FindData);

                        // SubFolders?!
                        if (searchOption == SearchOption.AllDirectories)
                        {
                            foreach (var match in EnumerateDirectories(new PathInfo(resultPath, win32FindData.cFileName), pattern, searchOption, enumerateOptions))
                            {
                                yield return match;
                            }
                        }
                    }
                    // Create new FindData object for next result
                    win32FindData = new Win32FindData();
                } // Search for next entry
                while (Win32SafeNativeMethods.FindNextFile(fileHandle, win32FindData));
            }
        }

        /// <summary>
        ///     Determined all sub system entries of a directory
        /// </summary>
        /// <param name="pathInfo">Path of the directory</param>
        /// <param name="pattern">Search pattern. Uses Win32 native filtering.</param>
        /// <param name="searchOption">
        ///     <see cref="SearchOption" />
        /// </param>
        /// <param name="enumerateOptions">The enumeration options for exception handling</param>
        /// <returns>Collection of <see cref="DirectoryDetail" /></returns>
        internal static IEnumerable<KeyValuePair<PathInfo, FileOrDirectory>> EnumerateFileSystemEntries(PathInfo pathInfo, String pattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly, SuppressExceptions enumerateOptions = SuppressExceptions.None)
        {
            return EnumerateFileSystemEntries(pathInfo.FullNameUnc, pattern, searchOption, enumerateOptions);
        }

        /// <summary>
        ///     Determined all sub system entries of a directory
        /// </summary>
        /// <param name="uncDirectoryPath">Path of the directory</param>
        /// <param name="pattern">Search pattern. Uses Win32 native filtering.</param>
        /// <param name="searchOption">
        ///     <see cref="SearchOption" />
        /// </param>
        /// <param name="enumerateOptions">The enumeration options for exception handling</param>
        /// <returns>Collection of <see cref="DirectoryDetail" /></returns>
        static IEnumerable<KeyValuePair<PathInfo, FileOrDirectory>> EnumerateFileSystemEntries(String uncDirectoryPath, String pattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly, SuppressExceptions enumerateOptions = SuppressExceptions.None)
        {
            // Match for start of search
            string currentPath = PathTools.Combine(uncDirectoryPath, pattern);

            // Find First file
            var win32FindData = new Win32FindData();
            int win32Error;
            using (var fileHandle = FindFirstSafeFileHandle(currentPath, win32FindData, out win32Error))
            {
                // Take care of invalid handles
                if (fileHandle.IsInvalid)
                {
                    if (win32Error != ERROR_NO_MORE_FILES)
                    {
                        NativeExceptionMapping(uncDirectoryPath, win32Error);
                    }

                    if (EnumerationHandleInvalidFileHandle(uncDirectoryPath, enumerateOptions, win32Error))
                    {
                        yield return new KeyValuePair<PathInfo, FileOrDirectory>();
                    }
                }

                do
                {
                    // Ignore . and .. directories
                    if (IsSystemDirectoryEntry(win32FindData))
                    {
                        continue;
                    }

                    // Create hit for current search result
                    string resultPath = PathTools.Combine(uncDirectoryPath, win32FindData.cFileName);

                    // Check for Directory
                    if (ContainsFileAttribute(win32FindData.dwFileAttributes, FileAttributes.Directory))
                    {
                        yield return new KeyValuePair<PathInfo, FileOrDirectory>(new PathInfo(resultPath) { FindData = win32FindData }, FileOrDirectory.Directory);

                        // SubFolders?!
                        if (searchOption == SearchOption.AllDirectories)
                        {
                            foreach (var match in EnumerateFileSystemEntries(new PathInfo(resultPath, win32FindData.cFileName), pattern, searchOption, enumerateOptions))
                            {
                                yield return match;
                            }
                        }
                    }
                    else
                    {
                        yield return new KeyValuePair<PathInfo, FileOrDirectory>(new PathInfo(resultPath) { FindData = win32FindData }, FileOrDirectory.File);
                    }
                    // Create new FindData object for next result
                    win32FindData = new Win32FindData();
                } // Search for next entry
                while (Win32SafeNativeMethods.FindNextFile(fileHandle, win32FindData));
            }
        }

        /// <summary>
        ///     Determined all sub file system entries of a directory
        /// </summary>
        /// <param name="pathInfo">Path of the directory</param>
        /// <param name="pattern">Search pattern. Uses Win32 native filtering.</param>
        /// <param name="searchOption">
        ///     <see cref="SearchOption" />
        /// </param>
        /// <param name="pathFormatReturn">Specifies the type of path to return.</param>
        /// <param name="enumerateOptions">The enumeration options for exception handling</param>
        /// <returns>Collection of <see cref="DirectoryDetail" /></returns>
        internal static IEnumerable<KeyValuePair<String, FileOrDirectory>> EnumerateFileSystemEntryPaths(PathInfo pathInfo, String pattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly, SuppressExceptions enumerateOptions = SuppressExceptions.None, UncOrRegular pathFormatReturn = UncOrRegular.Regular)
        {
            return EnumerateFileSystemEntryPaths(pathInfo.FullNameUnc, pattern, searchOption, enumerateOptions, pathFormatReturn);
        }

        /// <summary>
        ///     Determined all sub file system entries of a directory
        /// </summary>
        /// <param name="uncDirectoryPath">Path of the directory</param>
        /// <param name="pattern">Search pattern. Uses Win32 native filtering.</param>
        /// <param name="searchOption">
        ///     <see cref="SearchOption" />
        /// </param>
        /// <param name="pathFormatReturn">Specifies the type of path to return.</param>
        /// <param name="enumerateOptions">The enumeration options for exception handling</param>
        /// <returns>Collection of <see cref="DirectoryDetail" /></returns>
        static IEnumerable<KeyValuePair<String, FileOrDirectory>> EnumerateFileSystemEntryPaths(String uncDirectoryPath, String pattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly, SuppressExceptions enumerateOptions = SuppressExceptions.None, UncOrRegular pathFormatReturn = UncOrRegular.Regular)
        {
            // Match for start of search
            string currentPath = PathTools.Combine(uncDirectoryPath, pattern);

            // Find First file
            var win32FindData = new Win32FindData();
            int win32Error;
            using (var fileHandle = FindFirstSafeFileHandle(currentPath, win32FindData, out win32Error))
            {
                // Take care of invalid handles
                if (fileHandle.IsInvalid)
                {
                    if (win32Error != ERROR_NO_MORE_FILES)
                    {
                        NativeExceptionMapping(uncDirectoryPath, win32Error);
                    }

                    if (EnumerationHandleInvalidFileHandle(uncDirectoryPath, enumerateOptions, win32Error))
                    {
                        yield return new KeyValuePair<string, FileOrDirectory>();
                    }
                }

                do
                {
                    // Ignore . and .. directories
                    if (IsSystemDirectoryEntry(win32FindData))
                    {
                        continue;
                    }

                    // Create hit for current search result
                    string resultPath = PathTools.Combine(uncDirectoryPath, win32FindData.cFileName);

                    // Check for Directory
                    if (ContainsFileAttribute(win32FindData.dwFileAttributes, FileAttributes.Directory))
                    {
                        yield return new KeyValuePair<String, FileOrDirectory>(FormatPathByType(pathFormatReturn, resultPath), FileOrDirectory.Directory);

                        // SubFolders?!
                        if (searchOption == SearchOption.AllDirectories)
                        {
                            foreach (var match in EnumerateFileSystemEntryPaths(resultPath, pattern, searchOption, enumerateOptions, pathFormatReturn))
                            {
                                yield return match;
                            }
                        }
                    }
                    else
                    {
                        yield return new KeyValuePair<String, FileOrDirectory>(FormatPathByType(pathFormatReturn, resultPath), FileOrDirectory.File);
                    }
                    // Create new FindData object for next result
                    win32FindData = new Win32FindData();
                } // Search for next entry
                while (Win32SafeNativeMethods.FindNextFile(fileHandle, win32FindData));
            }
        }

        /// <summary>
        ///     Determined all sub directory paths of a directory
        /// </summary>
        /// <param name="path">Path of the directory</param>
        /// <param name="pattern">Search pattern. Uses Win32 native filtering.</param>
        /// <param name="searchOption">
        ///     <see cref="SearchOption" />
        /// </param>
        /// <param name="pathFormatReturn">Specifies the type of path to return.</param>
        /// <param name="enumerateOptions">The enumeration options for exception handling</param>
        /// <returns>Collection of directory paths</returns>
        internal static IEnumerable<String> EnumerateDirectoryPaths(String path, String pattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly, SuppressExceptions enumerateOptions = SuppressExceptions.None, UncOrRegular pathFormatReturn = UncOrRegular.Regular)
        {
            return FindPaths(path, pattern, searchOption, FileOrDirectory.Directory, enumerateOptions, pathFormatReturn);
        }

        /// <summary>
        ///     Determined all files of a directory
        /// </summary>
        /// <param name="pathInfo">Path of the directory</param>
        /// <param name="pattern">Search pattern. Uses Win32 native filtering.</param>
        /// <param name="searchOption">
        ///     <see cref="SearchOption" />
        /// </param>
        /// <param name="enumerateOptions">Options <see cref="SuppressExceptions" /></param>
        /// <returns>Collection of files</returns>
        internal static IEnumerable<FileDetail> EnumerateFiles(PathInfo pathInfo, String pattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly, SuppressExceptions enumerateOptions = SuppressExceptions.None)
        {
            return EnumerateFiles(pathInfo.FullNameUnc, pattern, searchOption, enumerateOptions);
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
        ///     Determined all files of a directory
        /// </summary>
        /// <param name="uncDirectoryPath">Path of the directory</param>
        /// <param name="pattern">Search pattern. Uses Win32 native filtering.</param>
        /// <param name="searchOption">
        ///     <see cref="SearchOption" />
        /// </param>
        /// <param name="enumerateOptions">The enumeration options for exception handling</param>
        /// <returns>Collection of files</returns>
        internal static IEnumerable<FileDetail> EnumerateFiles(String uncDirectoryPath, String pattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly, SuppressExceptions enumerateOptions = SuppressExceptions.None)
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

                // Treffer auswerten
                do
                {
                    // Ignore . and .. directories
                    if (IsSystemDirectoryEntry(win32FindData))
                    {
                        continue;
                    }

                    // Create hit for current search result
                    string resultPath = PathTools.Combine(uncDirectoryPath, win32FindData.cFileName);

                    // Check for Directory
                    if (!ContainsFileAttribute(win32FindData.dwFileAttributes, FileAttributes.Directory))
                    {
                        yield return new FileDetail(resultPath, win32FindData);
                    }
                    else
                    {
                        // SubFolders?!
                        if (searchOption == SearchOption.AllDirectories)
                        {
                            foreach (var match in EnumerateFiles(resultPath, pattern, searchOption, enumerateOptions))
                            {
                                yield return match;
                            }
                        }
                    }
                    // Create new FindData object for next result
                    win32FindData = new Win32FindData();
                } // Search for next entry
                while (Win32SafeNativeMethods.FindNextFile(fileHandle, win32FindData));
            }
        }

        /// <summary>
        ///     Determined all files paths of a directory
        /// </summary>
        /// <param name="path">Path of the directory</param>
        /// <param name="pattern">Search pattern. Uses Win32 native filtering.</param>
        /// <param name="searchOption">
        ///     <see cref="SearchOption" />
        /// </param>
        /// <param name="pathFormatReturn">Specifies the type of path to return.</param>
        /// <param name="enumerateOptions">The enumeration options for exception handling</param>
        /// <returns>Collection of file paths</returns>
        internal static IEnumerable<String> EnumerateFilePaths(String path, String pattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly, SuppressExceptions enumerateOptions = SuppressExceptions.None, UncOrRegular pathFormatReturn = UncOrRegular.Regular)
        {
            return FindPaths(path, pattern, searchOption, FileOrDirectory.File, enumerateOptions, pathFormatReturn);
        }

        /// <summary>
        ///     Loads a file from specified path
        /// </summary>
        /// <param name="pathInfo">Full path</param>
        /// <returns>
        ///     <see cref="FileDetail" />
        /// </returns>
        public static FileDetail LoadFileFromPathInfo(PathInfo pathInfo)
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
        ///     Loads a directory from specified path
        /// </summary>
        /// <param name="pathInfo">Full path</param>
        /// <returns>
        ///     <see cref="DirectoryDetail" />
        /// </returns>
        public static DirectoryDetail LoadDirectoryFromPathInfo(PathInfo pathInfo)
        {
            Win32FindData findData;
            if (!TryGetFindDataFromPath(pathInfo, out findData))
            {
                throw new Exception("PathNotFound " + pathInfo.FullName);
            }
            if (DetermineFileSystemEntry(findData) != FileOrDirectory.Directory)
            {
                throw new Exception("UnmatchedFileSystemEntryType " + FileOrDirectory.File + ", " + FileOrDirectory.Directory + ", " + pathInfo.FullName);
            }
            return new DirectoryDetail(pathInfo, findData);
        }

        /// <summary>
        ///     Search Exection
        /// </summary>
        /// <param name="uncDirectoryPath">Start directory path</param>
        /// <param name="pattern">Search pattern. Uses Win32 native filtering.</param>
        /// <param name="searchOption">
        ///     <see cref="SearchOption" />
        /// </param>
        /// <param name="enumerateOptions">The enumeration options for exception handling</param>
        /// <param name="pathFormatReturn">Specifies the type of path to return.</param>
        /// <param name="filterType">
        ///     <see cref="FileOrDirectory" />
        /// </param>
        /// <returns>Collection of path</returns>
        static IEnumerable<String> FindPaths(String uncDirectoryPath, String pattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly, FileOrDirectory? filterType = null, SuppressExceptions enumerateOptions = SuppressExceptions.None, UncOrRegular pathFormatReturn = UncOrRegular.Regular)
        {
            // Result Container
            var results = new List<String>();

            // Match for start of search
            string currentPath = PathTools.Combine(uncDirectoryPath, pattern);

            // Find First file
            var win32FindData = new Win32FindData();
            int win32Error;
            using (var fileHandle = FindFirstSafeFileHandle(currentPath, win32FindData, out win32Error))
            {
                // Take care of invalid handles
                if (fileHandle.IsInvalid && EnumerationHandleInvalidFileHandle(uncDirectoryPath, enumerateOptions, win32Error))
                {
                    return new List<String>();
                }

                do
                {
                    // Ignore . and .. directories
                    if (IsSystemDirectoryEntry(win32FindData))
                    {
                        continue;
                    }

                    // Create hit for current search result
                    string resultPath = PathTools.Combine(uncDirectoryPath, win32FindData.cFileName);

                    // if it's a file, add to the collection
                    if (!ContainsFileAttribute(win32FindData.dwFileAttributes, FileAttributes.Directory))
                    {
                        if (filterType == null || ((FileOrDirectory)filterType == FileOrDirectory.File))
                        {
                            // It's a file
                            results.Add(FormatPathByType(pathFormatReturn, resultPath));
                        }
                    }
                    else
                    {
                        // It's a directory
                        // Check for search searchFocus directories
                        if (filterType != null && ((FileOrDirectory)filterType == FileOrDirectory.Directory))
                        {
                            results.Add(FormatPathByType(pathFormatReturn, resultPath));
                        }

                        // SubFolders?!
                        if (searchOption == SearchOption.AllDirectories)
                        {
                            var r = new List<String>(FindPaths(resultPath, pattern, searchOption, filterType, enumerateOptions));
                            if (r.Count > 0)
                            {
                                results.AddRange(r);
                            }
                        }
                    }

                    // Create new FindData object for next result
                    win32FindData = new Win32FindData();
                } // Search for next entry
                while (Win32SafeNativeMethods.FindNextFile(fileHandle, win32FindData));
            }
            // Return result;
            return results;
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
        ///     Formats a path
        /// </summary>
        /// <param name="pathFormatReturn">Target format type</param>
        /// <param name="uncPath">Path to format</param>
        /// <returns>Formatted path</returns>
        static string FormatPathByType(UncOrRegular pathFormatReturn, string uncPath)
        {
            return pathFormatReturn == UncOrRegular.Regular ? PathTools.ToRegularPath(uncPath) : uncPath;
        }

        /// <summary>
        ///     Gets the <see cref="FileAttributes" /> of the file on the entry.
        /// </summary>
        /// <param name="pathInfo">The path to the entry. </param>
        /// <returns>The <see cref="FileAttributes" /> of the file on the entry.</returns>
        internal static FileAttributes GetAttributes(PathInfo pathInfo)
        {
            return pathInfo.Attributes;
        }

        /// <summary>
        ///     Gets the <see cref="FileAttributes" /> of the file on the entry.
        /// </summary>
        /// <param name="uncPath">The path to the entry. </param>
        /// <returns>The <see cref="FileAttributes" /> of the file on the entry.</returns>
        internal static FileAttributes GetAttributes(String uncPath)
        {
            int win32Error;
            uint attrs = SafeGetAttributes(uncPath, out win32Error);
            NativeExceptionMapping(uncPath, win32Error);

            return (FileAttributes)attrs;
        }

        /// <summary>
        ///     Gets the <see cref="FileAttributes" /> of the file on the entry.
        /// </summary>
        /// <param name="uncPath">The path to the entry. </param>
        /// <param name="win32Error">Win32 Error Code</param>
        /// <returns>The <see cref="FileAttributes" /> of the file on the entry.</returns>
        internal static uint SafeGetAttributes(String uncPath, out Int32 win32Error)
        {
            uint attributes = Win32SafeNativeMethods.GetFileAttributes(uncPath);
            win32Error = (attributes) == 0xffffffff ? Marshal.GetLastWin32Error() : 0;
            return attributes;
        }

        /// <summary>
        ///     Sets the specified <see cref="FileAttributes" /> of the entry on the specified path.
        /// </summary>
        /// <param name="pathInfo">The path to the entry.</param>
        /// <param name="attributes">A bitwise combination of the enumeration values.</param>
        /// <exception cref="Win32Exception">Unmatched Exception</exception>
        public static void SetAttributes(PathInfo pathInfo, FileAttributes attributes)
        {
            if (Win32SafeNativeMethods.SetFileAttributes(pathInfo.FullNameUnc, (uint)attributes))
            {
                return;
            }
            int win32Error = Marshal.GetLastWin32Error();
            NativeExceptionMapping(pathInfo.FullName, win32Error);
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

            bool result = Win32SafeNativeMethods.CopyFile(sourceFilePath.FullNameUnc, targetFilePath.FullNameUnc, failOnExists);
            int win32Error = Marshal.GetLastWin32Error();
            NativeExceptionMapping(sourceFilePath.FullName, win32Error);
            return result;
        }

        /// <summary>
        ///     Moves a file
        /// </summary>
        /// <param name="sourceFileName">Full source path</param>
        /// <param name="destFileName">Full target path</param>
        public static void MoveFile(string sourceFileName, string destFileName)
        {
            if (Win32SafeNativeMethods.MoveFile(sourceFileName, destFileName))
            {
                return;
            }
            int win32Error = Marshal.GetLastWin32Error();
            NativeExceptionMapping(sourceFileName, win32Error);
        }

        /// <summary>
        ///     Sets the dates and times of given directory or file.
        /// </summary>
        /// <param name="pathInfo">Affected file or directory</param>
        /// <param name="creationTimeUtc">The time that is to be used (UTC)</param>
        /// <param name="lastAccessTimeUtc">The time that is to be used (UTC)</param>
        /// <param name="lastWriteTimeUtc">The time that is to be used (UTC)</param>
        public static void SetAllFileTimes(PathInfo pathInfo, DateTime creationTimeUtc, DateTime lastAccessTimeUtc, DateTime lastWriteTimeUtc)
        {
            long longCreateTime = creationTimeUtc.ToFileTime();
            long longAccessTime = lastAccessTimeUtc.ToFileTime();
            long longWriteTime = lastWriteTimeUtc.ToFileTime();

            using (SafeFileHandle fileHandle = OpenReadWriteFileSystemEntryHandle(pathInfo.FullNameUnc))
            {
                if (Win32SafeNativeMethods.SetAllFileTimes(fileHandle, ref longCreateTime, ref longAccessTime, ref longWriteTime) != 0)
                {
                    return;
                }
                int win32Error = Marshal.GetLastWin32Error();
                NativeExceptionMapping(pathInfo.FullName, win32Error);
            }
        }

        /// <summary>
        ///     Sets the time at which the file or directory was created (UTC)
        /// </summary>
        /// <param name="pathInfo">Affected file or directory</param>
        /// <param name="utcTime">The time that is to be used (UTC)</param>
        public static void SetCreationTimeUtc(PathInfo pathInfo, DateTime utcTime)
        {
            long longTime = utcTime.ToFileTime();
            using (SafeFileHandle fileHandle = OpenReadWriteFileSystemEntryHandle(pathInfo.FullNameUnc))
            {
                if (Win32SafeNativeMethods.SetCreationFileTime(fileHandle, ref longTime, IntPtr.Zero, IntPtr.Zero))
                {
                    return;
                }
                int win32Error = Marshal.GetLastWin32Error();
                NativeExceptionMapping(pathInfo.FullName, win32Error);
            }
        }

        /// <summary>
        ///     Sets the time at which the file or directory was last written to (UTC)
        /// </summary>
        /// <param name="pathInfo">Affected file or directory</param>
        /// <param name="utcTime">The time that is to be used (UTC)</param>
        public static void SetLastWriteTimeUtc(PathInfo pathInfo, DateTime utcTime)
        {
            long longTime = utcTime.ToFileTime();
            using (SafeFileHandle fileHandle = OpenReadWriteFileSystemEntryHandle(pathInfo.FullNameUnc))
            {
                if (Win32SafeNativeMethods.SetLastWriteFileTime(fileHandle, IntPtr.Zero, IntPtr.Zero, ref longTime))
                {
                    return;
                }
                int win32Error = Marshal.GetLastWin32Error();
                NativeExceptionMapping(pathInfo.FullName, win32Error);
            }
        }

        /// <summary>
        ///     Sets the time at which the file or directory was last accessed to (UTC)
        /// </summary>
        /// <param name="pathInfo">Affected file or directory</param>
        /// <param name="utcTime">The time that is to be used (UTC)</param>
        public static void SetLastAccessTimeUtc(PathInfo pathInfo, DateTime utcTime)
        {
            long longTime = utcTime.ToFileTime();
            using (SafeFileHandle fileHandle = OpenReadWriteFileSystemEntryHandle(pathInfo.FullNameUnc))
            {
                if (Win32SafeNativeMethods.SetLastAccessFileTime(fileHandle, IntPtr.Zero, ref longTime, IntPtr.Zero))
                {
                    return;
                }
                int win32Error = Marshal.GetLastWin32Error();
                NativeExceptionMapping(pathInfo.FullName, win32Error);
            }
        }

        /// <summary>
        ///     Gets Share Result
        /// </summary>
        /// <param name="machineName">Machine</param>
        /// <param name="level">API level</param>
        /// <param name="buffer">Buffer</param>
        /// <param name="entriesRead">Entries total read</param>
        /// <param name="totalEntries">Entries total</param>
        /// <param name="resumeHandle">Handle</param>
        /// <returns>Error Code</returns>
        /// <remarks>http://msdn.microsoft.com/en-us/library/windows/desktop/bb525387(v=vs.85).aspx</remarks>
        public static int GetShareEnumResult(string machineName, AdminOrNormal level, out IntPtr buffer, out int entriesRead, out int totalEntries, ref int resumeHandle)
        {
            return Win32SafeNativeMethods.NetShareEnum(machineName, (int)level, out buffer, -1, out entriesRead, out totalEntries, ref resumeHandle);
        }
    }
}
