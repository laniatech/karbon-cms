﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Karbon.Core.IO
{
    public class LocalFileStore : FileStore
    {
        private string _rootPath;
        private string _rootPhysicalPath;
        private string _pathSeperator;

        public override void Initialize(NameValueCollection config)
        {
            base.Initialize(config);

            // Not keen on the fact this is not type safe
            // but it's a requirement of the provider pattern
            _rootPath = config["rootPath"];
            _pathSeperator = config["pathSeperator"];

            // Allow passing in the root physical path for testing
            _rootPhysicalPath = config.AllKeys.Any(x => x == "rootPhysicalPath")
                ? config["rootPhysicalPath"]
                : IOHelper.MapPath(_rootPath);
        }

        #region Directories

        public override IEnumerable<string> GetDirectories(string relativePath = "")
        {
            var path = GetPhysicalPath(relativePath)
                .EnsureTrailingDirectorySeparator();

            return Directory.Exists(path) 
                ? Directory.EnumerateDirectories(path).Select(GetRelativePath) 
                : Enumerable.Empty<string>();
        }

        public override void DeleteDirectory(string relativePath, bool recursive = false)
        {
            if (!DirectoryExists(relativePath))
                return;

            Directory.Delete(GetPhysicalPath(relativePath), recursive);
        }

        public override bool DirectoryExists(string relativePath)
        {
            return Directory.Exists(GetPhysicalPath(relativePath));
        }

        #endregion

        #region Files

        public override IEnumerable<string> GetFiles(string relativePath = "", string filter = "*.*")
        {
            var path = GetPhysicalPath(relativePath)
                .EnsureTrailingDirectorySeparator();

            return Directory.Exists(path)
                ? Directory.EnumerateFiles(path, filter).Select(GetRelativePath) 
                : Enumerable.Empty<string>();
        }

        public override void AddFile(string relativePath, Stream stream, bool overwrite = true)
        {
            if (FileExists(relativePath) && !overwrite)
                throw new InvalidOperationException(string.Format("A file at path '{0}' already exists", relativePath));

            EnsureDirectory(Path.GetDirectoryName(relativePath));

            if (stream.CanSeek)
                stream.Seek(0, 0);

            using (var destination = (Stream)File.Create(GetPhysicalPath(relativePath)))
                stream.CopyTo(destination);
        }

        public override Stream OpenFile(string relativePath)
        {
            return File.OpenRead(GetPhysicalPath(relativePath));
        }

        public override void DeleteFile(string relativePath)
        {
            if (!FileExists(relativePath))
                return;

            File.Delete(GetPhysicalPath(relativePath));
        }

        public override bool FileExists(string relativePath)
        {
            return File.Exists(GetPhysicalPath(relativePath));
        }

        #endregion

        #region Helper Methods

        public override DateTimeOffset GetLastModified(string relativePath)
        {
            return DirectoryExists(relativePath)
                ? Directory.GetLastWriteTimeUtc(GetPhysicalPath(relativePath))
                : File.GetLastWriteTimeUtc(GetPhysicalPath(relativePath));
        }

        public override DateTimeOffset GetCreated(string relativePath)
        {
            return DirectoryExists(relativePath)
                ? Directory.GetCreationTimeUtc(GetPhysicalPath(relativePath))
                : File.GetCreationTimeUtc(GetPhysicalPath(relativePath));
        }

        public override DateTimeOffset GetLastAccessed(string relativePath)
        {
            return DirectoryExists(relativePath)
                ? Directory.GetLastAccessTimeUtc(GetPhysicalPath(relativePath))
                : File.GetLastAccessTimeUtc(GetPhysicalPath(relativePath));
        }

        public override string GetAbsolutePath(string relativePath)
        {
            return _rootPath + _pathSeperator + relativePath;
        }

        protected virtual string GetRelativePath(string physicalPath)
        {
            return physicalPath
                .TrimStart(_rootPhysicalPath.EnsureTrailingDirectorySeparator())
                .Replace(Path.DirectorySeparatorChar.ToString(CultureInfo.InvariantCulture), _pathSeperator);
        }

        protected virtual string GetPhysicalPath(string relativePath)
        {
            return _rootPhysicalPath.EnsureTrailingDirectorySeparator() + relativePath
                .Replace(_pathSeperator, Path.DirectorySeparatorChar.ToString(CultureInfo.InvariantCulture));
        }

        protected virtual void EnsureDirectory(string relativePath)
        {
            Directory.CreateDirectory(GetPhysicalPath(relativePath));
        }

        #endregion
    }
}
