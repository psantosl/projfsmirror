using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MirrorProvider
{
    public static class FileSystemVirtualizer
    {
        internal static string GetFullPathInMirror(Enlistment enlistment, string relativePath)
        {
            return Path.Combine(enlistment.MirrorRoot, relativePath);
        }

        internal static bool DirectoryExists(Enlistment enlistment, string relativePath)
        {
            string fullPathInMirror = GetFullPathInMirror(enlistment, relativePath);
            DirectoryInfo dirInfo = new DirectoryInfo(fullPathInMirror);

            return dirInfo.Exists;
        }

        internal static bool FileExists(Enlistment enlistment, string relativePath)
        {
            string fullPathInMirror = GetFullPathInMirror(enlistment, relativePath);
            FileInfo fileInfo = new FileInfo(fullPathInMirror);

            return fileInfo.Exists;
        }

        internal static ProjectedFileInfo GetFileInfo(
            Enlistment enlistment,
            string relativePath,
            StringComparison pathComparison)
        {
            string fullPathInMirror = GetFullPathInMirror(enlistment, relativePath);
            string fullParentPath = Path.GetDirectoryName(fullPathInMirror);
            string fileName = Path.GetFileName(relativePath);

            string actualCaseName;
            ProjectedFileInfo.FileType type;
            if (FileOrDirectoryExists(fullParentPath, fileName, pathComparison, out actualCaseName, out type))
            {
                return new ProjectedFileInfo(
                    actualCaseName, 
                    size: (type == ProjectedFileInfo.FileType.File) ? new FileInfo(fullPathInMirror).Length : 0, 
                    type: type);
            }

            return null;
        }

        internal static IEnumerable<ProjectedFileInfo> GetChildItems(Enlistment enlistment, string relativePath)
        {
            string fullPathInMirror = GetFullPathInMirror(enlistment, relativePath);
            DirectoryInfo dirInfo = new DirectoryInfo(fullPathInMirror);

            if (!dirInfo.Exists)
            {
                yield break;
            }

            foreach (FileSystemInfo fileSystemInfo in dirInfo.GetFileSystemInfos())
            {
                if ((fileSystemInfo.Attributes & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint)
                {
                    // While not 100% accurate on all platforms, for simplicity assume that if the the file has reparse data it's a symlink
                    yield return new ProjectedFileInfo(
                        fileSystemInfo.Name,
                        size: 0,
                        type: ProjectedFileInfo.FileType.SymLink);
                }
                else if ((fileSystemInfo.Attributes & FileAttributes.Directory) == FileAttributes.Directory)
                {
                    yield return new ProjectedFileInfo(
                        fileSystemInfo.Name,
                        size: 0,
                        type: ProjectedFileInfo.FileType.Directory);
                }
                else
                {
                    FileInfo fileInfo = fileSystemInfo as FileInfo;
                    yield return new ProjectedFileInfo(
                        fileInfo.Name,
                        fileInfo.Length,
                        ProjectedFileInfo.FileType.File);
                }

            }
        }

        internal static FileSystemResult HydrateFile(Enlistment enlistment, string relativePath, int bufferSize, Func<byte[], uint, bool> tryWriteBytes)
        {
            string fullPathInMirror = GetFullPathInMirror(enlistment, relativePath);
            if (!File.Exists(fullPathInMirror))
            {
                return FileSystemResult.EFileNotFound;
            }

            using (FileStream fs = new FileStream(fullPathInMirror, FileMode.Open, FileAccess.Read))
            {
                long remainingData = fs.Length;
                byte[] buffer = new byte[bufferSize];

                while (remainingData > 0)
                {
                    int bytesToCopy = (int)Math.Min(remainingData, buffer.Length);
                    if (fs.Read(buffer, 0, bytesToCopy) != bytesToCopy)
                    {
                        return FileSystemResult.EIOError;
                    }

                    if (!tryWriteBytes(buffer, (uint)bytesToCopy))
                    {
                        return FileSystemResult.EIOError;
                    }

                    remainingData -= bytesToCopy;
                }
            }

            return FileSystemResult.Success;
        }

        static bool FileOrDirectoryExists(
            string fullParentPath,
            string fileName,
            StringComparison pathComparison,
            out string actualCaseName,
            out ProjectedFileInfo.FileType type)
        {
            actualCaseName = null;
            type = ProjectedFileInfo.FileType.Invalid;

            DirectoryInfo dirInfo = new DirectoryInfo(fullParentPath);
            if (!dirInfo.Exists)
            {
                return false;
            }

            FileSystemInfo fileSystemInfo = 
                dirInfo
                .GetFileSystemInfos()
                .FirstOrDefault(fsInfo => fsInfo.Name.Equals(fileName, pathComparison));

            if (fileSystemInfo == null)
            {
                return false;
            }

            actualCaseName = fileSystemInfo.Name;

            if ((fileSystemInfo.Attributes & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint)
            {
                type = ProjectedFileInfo.FileType.SymLink;
            }
            else if ((fileSystemInfo.Attributes & FileAttributes.Directory) == FileAttributes.Directory)
            {
                type = ProjectedFileInfo.FileType.Directory;
            }
            else
            {
                type = ProjectedFileInfo.FileType.File;
            }

            return true;
        }
    }
}
