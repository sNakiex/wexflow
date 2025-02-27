﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.IO.Enumeration;

namespace Wexflow.Core.PollingFileSystemWatcher
{
    internal class FileSystemChangeEnumerator : FileSystemEnumerator<string>
    {
        private FileChangeList _changes = new();
        private string _currentDirectory;
        private readonly PollingFileSystemWatcher _watcher;

        internal FileSystemChangeEnumerator(PollingFileSystemWatcher watcher, string directory, EnumerationOptions options = null)
            : base(directory, options)
        {
            _watcher = watcher;
        }

        public FileChangeList Changes => _changes;

        protected override void OnDirectoryFinished(ReadOnlySpan<char> directory)
        {
            _currentDirectory = null;
        }

        protected override string TransformEntry(ref FileSystemEntry entry)
        {
            _watcher.UpdateState(_currentDirectory, ref _changes, ref entry);

            return null;
        }

        protected override bool ShouldIncludeEntry(ref FileSystemEntry entry)
        {
            // Don't want to convert this to string every time
            _currentDirectory ??= entry.Directory.ToString();

            return _watcher.ShouldIncludeEntry(ref entry);
        }

        protected override bool ShouldRecurseIntoEntry(ref FileSystemEntry entry)
        {
            return _watcher.ShouldRecurseIntoEntry(ref entry);
        }
    }
}
