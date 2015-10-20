namespace Minimal
{
    using System;

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
}