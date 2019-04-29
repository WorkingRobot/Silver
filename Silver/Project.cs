using PakReader;
using System;
using System.Collections.Generic;
using System.IO;

namespace Silver
{
    public class Project
    {
        public readonly ushort Version;
        public string Name;
        public readonly List<ProjectFile> Files;

        public Project(string path) : this(File.OpenRead(path)) { }

        public Project(Stream stream)
        {
            using (stream)
            using (var reader = new BinaryReader(stream))
            {
                Version = reader.ReadUInt16();
                Name = reader.ReadFString();
                Files = new List<ProjectFile>(reader.ReadUInt16());
                for (int i = 0; i < Files.Capacity; i++)
                {
                    Files.Add(new ProjectFile(reader));
                }
            }
        }

        // New project
        public Project()
        {
            Version = 1;
            Name = "Untitled Project";
            Files = new List<ProjectFile>();
        }

        public void Save(string path) => Save(File.OpenWrite(path));

        public void Save(Stream stream)
        {
            if (!stream.CanWrite)
            {
                throw new ArgumentException("Can't write to the stream");
            }
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(Version);
                writer.WriteFString(Name);
                writer.Write((ushort)Files.Count);
                foreach(var f in Files)
                {
                    f.Write(writer);
                }
            }
        }
    }

    public class ProjectFile
    {
        public readonly string Path;
        public readonly byte[] Hash; // unused
        public readonly byte[] Key;
        public readonly string MountPoint;
        public readonly ProjectFileIndex Index; // unused

        public ProjectFile(string path, byte[] key)
        {
            Path = path;
            Key = key;
        }

        public ProjectFile(BinaryReader reader)
        {
            Path = reader.ReadFString();
            MountPoint = reader.ReadFString();
            if (reader.ReadBoolean())
            {
                Hash = reader.ReadBytes(32);
            }
            if (reader.ReadBoolean())
            {
                Key = reader.ReadBytes(32);
            }
            if (reader.ReadBoolean())
            {
                Index = new ProjectFileIndex(reader);
            }
        }

        public void Write(BinaryWriter writer)
        {
            writer.WriteFString(Path);
            writer.WriteFString(MountPoint);
            if (Hash != null)
            {
                writer.Write(true);
                writer.Write(Hash);
            }
            else
            {
                writer.Write(false);
            }
            if (Key != null)
            {
                writer.Write(true);
                writer.Write(Key);
            }
            else
            {
                writer.Write(false);
            }
            if (Index != null)
            {
                writer.Write(true);
                Index.Write(writer);
            }
            else
            {
                writer.Write(false);
            }
        }
    }

    public class ProjectFileIndex
    {
        public readonly CompressionType Compression;
        public readonly IndexType Type;
        public readonly Entry[] Index;

        public ProjectFileIndex(BinaryReader reader)
        {
            Compression = (CompressionType)reader.ReadByte();
            switch (Compression)
            {
                case CompressionType.NONE:
                    break;
                default:
                    throw new NotImplementedException();
            }
            Type = (IndexType)reader.ReadByte();
            Index = new Entry[reader.ReadInt32()];
            for(int i = 0; i < Index.Length; i++)
            {
                Index[i] = new Entry(reader, Type);
            }
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write((byte)Compression);
            switch (Compression)
            {
                case CompressionType.NONE:
                    break;
                default:
                    throw new NotImplementedException();
            }
            writer.Write((byte)Type);
            writer.Write(Index.Length);
            foreach(var e in Index)
            {
                e.Write(writer, Type);
            }
        }

        public class Entry
        {
            public readonly string Name;

            public readonly EntryInfo Info;

            public Entry(BinaryReader reader, IndexType type)
            {
                switch (type)
                {
                    /*
                    case IndexType.FILE_NAME:
                        Name = reader.ReadFString();
                        break;
                    */
                    case IndexType.FILE_INFO:
                        Name = reader.ReadFString();
                        Info = new EntryInfo(reader);
                        break;
                    default:
                        throw new ArgumentException($"Index type ({type}) is invalid");
                }
            }

            public void Write(BinaryWriter writer, IndexType type)
            {
                switch (type)
                {
                    /*
                    case IndexType.FILE_NAME:
                        writer.WriteFString(Name);
                        break;
                    */
                    case IndexType.FILE_INFO:
                        writer.WriteFString(Name);
                        Info.Write(writer);
                        break;
                    default:
                        throw new ArgumentException($"Index type ({type}) is invalid");
                }
            }
        }

        public class EntryInfo : BasePakEntry
        {
            public EntryInfo(BinaryReader reader)
            {
                Pos = reader.ReadInt64();
                Size = reader.ReadInt64();
                StructSize = reader.ReadInt32();
                UncompressedSize = reader.ReadInt64();
                Encrypted = reader.ReadBoolean();
            }

            public void Write(BinaryWriter writer)
            {
                writer.Write(Pos);
                writer.Write(Size);
                writer.Write(StructSize);
                writer.Write(UncompressedSize);
                writer.Write(Encrypted);
            }
        }

        public enum CompressionType : byte
        {
            NONE,
            GZIP,
            BROTLI,
            BZIP2,
            DEFLATE,
            LZMA2,
            // LZMA
            // PPMD
        }

        public enum IndexType : byte
        {
            // FILE_NAME, // Not supported yet
            FILE_INFO
        }
    }
}
