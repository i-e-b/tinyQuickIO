namespace Minimal
{
    using System;
    using System.IO;
    using System.Linq;

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
                throw new Exception("Path contains invalid characters"+ path);
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
                throw new Exception("Invalid path at "+path);
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
}