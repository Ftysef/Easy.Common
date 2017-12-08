﻿namespace Easy.Common.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;

    /// <summary>
    /// Provides a set of useful methods for working with <see cref="FileInfo"/> and <see cref="DirectoryInfo"/>.
    /// </summary>
    public static class FileAndDirectoryInfoExtensions
    {
        private const char CarriageReturn = '\r';
        private const char NewLine = '\n';
        private const char Tab = '\t';

        private static readonly ThreadLocal<char[]> CharacterBuffer = new ThreadLocal<char[]>(() => new char[256]);

        /// <summary>
        /// Returns the size in bytes of the <paramref name="directoryInfo"/> represented by the <paramref name="directoryInfo"/> instance.
        /// </summary>
        /// <param name="directoryInfo">The <paramref name="directoryInfo"/> to get the size of.</param>
        /// <returns>The size of <paramref name="directoryInfo"/> in bytes.</returns>
        [DebuggerStepThrough]
        public static long GetSizeInByte(this DirectoryInfo directoryInfo)
        {
            var length = directoryInfo.GetFiles().Sum(file => file.Exists ? file.Length : 0);
            length += directoryInfo.GetDirectories().Sum(dir => dir.Exists ? dir.GetSizeInByte() : 0);
            return length;
        }

        /// <summary>
        /// Indicates if a given <paramref name="directoryInfo"/> is hidden.
        /// </summary>
        /// <param name="directoryInfo">The <paramref name="directoryInfo"/> to check.</param>
        /// <returns>Boolean indicating if the <paramref name="directoryInfo"/> is hidden.</returns>
        [DebuggerStepThrough]
        public static bool IsHidden(this DirectoryInfo directoryInfo) => (directoryInfo.Attributes & FileAttributes.Hidden) != 0;

        /// <summary>
        /// Indicates if a given <paramref name="fileInfo"/> is hidden.
        /// </summary>
        /// <param name="fileInfo">The <paramref name="fileInfo"/> to check.</param>
        /// <returns>
        /// Boolean indicating if the <paramref name="fileInfo"/> is hidden.
        /// </returns>
        [DebuggerStepThrough]
        public static bool IsHidden(this FileInfo fileInfo) => (fileInfo.Attributes & FileAttributes.Hidden) != 0;

        /// <summary>
        /// Renames the given <paramref name="fileInfo"/> to <paramref name="newName"/>.
        /// </summary>
        [DebuggerStepThrough]
        public static void Rename(this FileInfo fileInfo, string newName)
        {
            Ensure.NotNull(fileInfo, nameof(fileInfo));
            Ensure.NotNullOrEmptyOrWhiteSpace(newName);
            Ensure.That(newName.IsValidFileName(), "Invalid file name: " + newName);

            fileInfo.Refresh();
            if (fileInfo.DirectoryName != null && fileInfo.Directory != null
                && fileInfo.Directory.Exists && fileInfo.Exists)
            {
                fileInfo.MoveTo(Path.Combine(fileInfo.DirectoryName, newName));
            }
            else
            {
                throw new InvalidOperationException($"Unable to rename the file: {fileInfo.FullName} to: {newName}");
            }
        }

        /// <summary>
        /// Lazily reads every line in the <paramref name="fileInfo"/> without requiring a file lock.
        /// <remarks>
        /// This method is preferred over the <see cref="File.ReadLines(string)"/> which requires a file lock
        /// that may result <see cref="IOException"/> if the file is opened exclusively by another process such as <c>Excel</c>.
        /// </remarks>
        /// </summary>
        [DebuggerStepThrough]
        public static IEnumerable<string> ReadLines(this FileInfo fileInfo) 
            => fileInfo.ReadLines(Encoding.UTF8);

        /// <summary>
        /// Lazily reads every line in the <paramref name="fileInfo"/> without requiring a file lock.
        /// <remarks>
        /// This method is preferred over the <see cref="File.ReadLines(string)"/> which requires a file lock
        /// that may result <see cref="IOException"/> if the file is opened exclusively by another process such as <c>Excel</c>.
        /// </remarks>
        /// </summary>
        [DebuggerStepThrough]
        public static IEnumerable<string> ReadLines(this FileInfo fileInfo, Encoding encoding)
        {
            Ensure.NotNull(fileInfo, nameof(fileInfo));
            Ensure.NotNull(encoding, nameof(encoding));

            using (var stream = File.Open(fileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new StreamReader(stream, encoding))
            {
                while (!reader.EndOfStream)
                {
                    yield return reader.ReadLine();
                }
            }
        }

        /// <summary>
        /// Enumerates every directory inside the <paramref name="directory"/> without 
        /// throwing <see cref="UnauthorizedAccessException"/>.
        /// </summary>
        [DebuggerStepThrough]
        public static IEnumerable<DirectoryInfo> EnumerateDirectoriesSafe(this DirectoryInfo directory,
            string searchPattern = "*", SearchOption option = SearchOption.TopDirectoryOnly, bool throwOnPathTooLong = false)
        {
            try
            {
                var directories = Enumerable.Empty<DirectoryInfo>();
                if (option == SearchOption.AllDirectories)
                {
                    directories = directory.EnumerateDirectories()
                        .SelectMany(d => d.EnumerateDirectoriesSafe(searchPattern, option, throwOnPathTooLong));
                }

                return directories.Concat(directory.EnumerateDirectories(searchPattern));
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException || ex is PathTooLongException && !throwOnPathTooLong)
            {
                return Enumerable.Empty<DirectoryInfo>();
            }
        }

        /// <summary>
        /// Enumerates every file inside the <paramref name="directory"/> without 
        /// throwing <see cref="UnauthorizedAccessException"/>.
        /// </summary>
        [DebuggerStepThrough]
        public static IEnumerable<FileInfo> EnumerateFilesSafe(this DirectoryInfo directory,
            string searchPattern = "*", SearchOption option = SearchOption.TopDirectoryOnly, bool throwOnPathTooLong = false)
        {
            try
            {
                var files = Enumerable.Empty<FileInfo>();
                if (option == SearchOption.AllDirectories)
                {
                    files = directory.EnumerateDirectories()
                        .SelectMany(d => d.EnumerateFilesSafe(searchPattern, option, throwOnPathTooLong));
                }

                return files.Concat(directory.EnumerateFiles(searchPattern));
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException || ex is PathTooLongException && !throwOnPathTooLong)
            {
                return Enumerable.Empty<FileInfo>();
            }
        }

        /// <summary>
        /// Determines whether the given <paramref name="file"/> is binary or a text file.
        /// </summary>
        [DebuggerStepThrough]
        public static bool IsBinary(this FileSystemInfo file)
        {
            var buffer = CharacterBuffer.Value;

            using (var fileStream = File.Open(file.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new StreamReader(fileStream))
            {
                var read = reader.ReadBlock(buffer, 0, buffer.Length);
                return ContainsBinary(buffer, read);
            }

            bool ContainsBinary(char[] bytes, int count)
            {
                for (var i = 0; i < count; i++)
                {
                    var c = bytes[i];
                    if (char.IsControl(c) && c != CarriageReturn && c != NewLine && c != Tab)
                    {
                        return true;
                    }
                }
                return false;
            }
        }
    }
}