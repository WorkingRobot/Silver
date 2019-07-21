using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Silver
{
    class FSCollection<T> : IDisposable
    {
        SortedList<string, FSCollection<T>> Directories = new SortedList<string, FSCollection<T>>();
        SortedList<string, T> Files = new SortedList<string, T>();

        public void Add(string key, T value) => Add(Helpers.GetKeys(key), 0, value);

        public void Add(string[] keys, T value) => Add(keys, 0, value);

        public void Add(string[] keys, int offset, T value)
        {
            if (offset >= keys.Length)
            {
                throw new ArgumentOutOfRangeException("Offset must be less than the key length");
            }
            if (offset == keys.Length - 1)
            {
                Files[keys[offset]] = value;
            }
            else
            {
                if (!Directories.ContainsKey(keys[offset]))
                {
                    Directories[keys[offset]] = new FSCollection<T>();
                }
                Directories[keys[offset]].Add(keys, ++offset, value);
            }
        }

        public FSCollection<T> GetDirectory(string key) => GetDirectory(Helpers.GetKeys(key), 0);

        public FSCollection<T> GetDirectory(string[] keys) => GetDirectory(keys, 0);

        public FSCollection<T> GetDirectory(string[] keys, int offset)
        {
            if (offset >= keys.Length)
            {
                return this;
            }
            if (offset == keys.Length - 1)
            {
                return Directories[keys[offset]];
            }
            return Directories[keys[offset]].GetDirectory(keys, ++offset);
        }

        public T Get(string key) => Get(Helpers.GetKeys(key), 0);

        public T Get(string[] keys) => Get(keys, 0);

        public T Get(string[] keys, int offset)
        {
            if (offset >= keys.Length)
            {
                throw new ArgumentOutOfRangeException("Offset must be less than the key length");
            }
            if (offset == keys.Length - 1)
            {
                if (Files.TryGetValue(keys[offset], out var file))
                {
                    return file;
                }
                return default;
            }
            if (Directories.TryGetValue(keys[offset], out var dir))
            {
                return dir.Get(keys, ++offset);
            }
            return default;
        }

        private IEnumerable<KeyValuePair<(string Path, string Name), object>> SearchInternal(string inp, string delimeter, string curPath, StringComparison comparison)
        {
            foreach(var kv in Directories)
            {
                if ((curPath + delimeter + kv.Key).IndexOf(inp, comparison) >= 0)
                {
                    yield return new KeyValuePair<(string Path, string Name), object>((curPath, kv.Key), kv.Value);
                }
                else
                {
                    foreach (var i in kv.Value.SearchInternal(inp, delimeter, curPath + delimeter + kv.Key, comparison)) yield return i;
                }
            }
            foreach (var kv in Files)
            {
                if ((curPath + delimeter + kv.Key).IndexOf(inp, comparison) >= 0)
                {
                    yield return new KeyValuePair<(string Path, string Name), object>((curPath, kv.Key), kv.Value);
                }
            }
        }

        public IEnumerable<KeyValuePair<(string Path, string Name), object>> Search(string inp, string delimeter = "/", StringComparison comparison = StringComparison.InvariantCultureIgnoreCase) =>
            SearchInternal(inp, delimeter, "", comparison);


        public T this[string Path]
        {
            get => Get(Path);
            set => Add(Path, value);
        }

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            foreach (var kv in Directories)
            {
                yield return new KeyValuePair<string, object>(kv.Key, kv.Value);
            }
            foreach (var kv in Files)
            {
                yield return new KeyValuePair<string, object>(kv.Key, kv.Value);
            }
        }

        public IEnumerator<KeyValuePair<string, FSCollection<T>>> EnumDirectories() => Directories.GetEnumerator();
        
        public IEnumerator<KeyValuePair<string, T>> EnumFiles() => Files.GetEnumerator();

        public void Dispose()
        {
            Files.Clear();
            Directories.TrimExcess();
            Files = null;
            foreach(var dir in Directories.Values)
            {
                dir.Dispose();
            }
            Directories.Clear();
            Directories.TrimExcess();
            Directories = null;
        }
    }
}
