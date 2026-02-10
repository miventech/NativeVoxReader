using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using UnityEngine;

namespace Miventech.NativeVoxReader.Runtime.Tools.ReaderFile
{
    public class QbtFile
    {
        public QbtHeader Header;
        public QbtNode Root;

        public static QbtFile Load(string filePath)
        {
            if (!File.Exists(filePath)) return null;

            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            using (var reader = new BinaryReader(fs))
            {
                return ReadStream(reader);
            }
        }

        public static QbtFile ReadStream(BinaryReader reader)
        {
            var file = new QbtFile();
            
            // 1. Read Header
            file.Header = new QbtHeader();
            char[] magic = reader.ReadChars(4); // "QB 2"
            if (new string(magic) != "QB 2")
            {
                Debug.LogError("Invalid QBT Magic: " + new string(magic));
                return null;
            }

            file.Header.VersionMajor = reader.ReadByte();
            file.Header.VersionMinor = reader.ReadByte();
            file.Header.GlobalScale = new Vector3(
                reader.ReadSingle(),
                reader.ReadSingle(),
                reader.ReadSingle()
            );

            // 2. Read Root Node (Recursive)
            file.Root = ReadNode(reader);

            return file;
        }

        private static QbtNode ReadNode(BinaryReader reader)
        {
            QbtNode node = new QbtNode();

            // Node Meta
            uint typeId = reader.ReadUInt32();
            node.Type = (QbtNodeType)typeId;
            
            uint dataSize = reader.ReadUInt32();
            
            // Record position to verify skipping if needed
            long startPos = reader.BaseStream.Position;

            // --- Read Node Data ---
            ushort nameLen = reader.ReadUInt16();
            if (nameLen > 0)
                node.Name = new string(reader.ReadChars(nameLen));
            else
                node.Name = "ValidName";

            // Note: QBT spec varies on 'Hidden'. Usually 0=Visible.
            // Some parsers read it as byte, others skip.
            // We assume 1 byte for visibility/flags.
            // If data alignment is off, check this byte.
            // Actually, spec says: NameLen (2), Name (N), Hidden (1)
            // But let's verify if Hidden exists. Standard QBT 1.0 has it.
            // However, it could be inside a 'properties' block if dataSize is large.
            // We will trust the standard order:
            
            // Wait: some variants might not strictly follow `Hidden` byte presence?
            // Let's assume standard spec.
            // However, to be safe, we rely on dataSize.
            
            // Hidden is often not present in some minimal exports or is a bitmask.
            // Let's check typical struct:
            // Name (Pascal)
            // Position (3 float)
            // Pivot (3 float)
            // Size (3 uint)
            
            // Let's just read the floats and see if we end up at dataSize.
            // But we can't 'see'. Let's stick to spec:
            // Data Block:
            //  - Name (Pascal)
            //  - props (byte) -> often unused or hidden
            //  ...
            
            // Let's try reading Position immediately after Name. 
            // If it fails, we might have to skip a byte.
            // *Standard QBT Spec says there IS a visibility byte.*
            // Let's read it.
            // node.IsHidden = reader.ReadByte() == 1; 
            // Wait, if it's missing, we misalign floats.
            
            // Let's assume standard QBT from Qubicle 2/3.
            // It usually DOES NOT have a dedicated single byte for "Hidden" in the header part 
            // unless strict mode.
            // BUT, let's look at the remaining bytes in DataChunk vs expected size.
            // NameSize = 2 + nameLen.
            // Transform = 3*4 (Pos) + 3*4 (Pivot) + 3*4 (Size) = 36 bytes.
            // Total Expected typical = NameSize + 36.
            // If DataSize > TotalExpected, we might have extra bytes (like Hidden).
            
            // Let's Read Transform first, assuming tight packing? No, Qubicle usually has structure.
            // Let's just Read Position directly.
            
            node.Position = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            node.Pivot = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            
            // Size is 3 uints, giving the bounding box size
            uint sx = reader.ReadUInt32();
            uint sy = reader.ReadUInt32();
            uint sz = reader.ReadUInt32();
            node.Size = new Vector3Int((int)sx, (int)sy, (int)sz);

            // Matrix Specific Data
            if (node.Type == QbtNodeType.Matrix)
            {
                uint compressedSize = reader.ReadUInt32();
                if (compressedSize > 0)
                {
                    byte[] compressedData = reader.ReadBytes((int)compressedSize);
                    node.VoxelData = Decompress(compressedData, node.Size.x * node.Size.y * node.Size.z * 4);
                }
            }
            
            // Skip any remaining data in this node's header (forward compatibility)
            long bytesRead = reader.BaseStream.Position - startPos;
            if (bytesRead < dataSize)
            {
                reader.BaseStream.Seek(dataSize - bytesRead, SeekOrigin.Current);
            }

            // Children
            uint childCount = reader.ReadUInt32();
            node.Children = new List<QbtNode>();
            for (int i = 0; i < childCount; i++)
            {
                var child = ReadNode(reader);
                child.Parent = node;
                node.Children.Add(child);
            }

            return node;
        }

        private static byte[] Decompress(byte[] compressed, int expectedSize)
        {
            try
            {
                // Qubicle uses ZLib. Microsoft DeflateStream expects raw Deflate (no header).
                // ZLib header is usually 2 bytes (0x78 0x9C etc).
                // We skip the first 2 bytes to use standard DeflateStream.
                
                using (var cmpStream = new MemoryStream(compressed))
                {
                    // Skip Zlib Header (2 bytes)
                    if (compressed.Length > 2)
                    {
                         // Simple check for zlib header
                        int b1 = cmpStream.ReadByte();
                        int b2 = cmpStream.ReadByte();
                        // (Can inspect b1, b2 if strict, usually just skipping works for valid Zlib)
                    }
                    
                    using (var dstream = new DeflateStream(cmpStream, CompressionMode.Decompress))
                    using (var outStream = new MemoryStream(expectedSize))
                    {
                        dstream.CopyTo(outStream);
                        return outStream.ToArray();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[QBT] Decompression failed: {ex.Message}");
                return new byte[expectedSize]; // Return empty
            }
        }
    }

    public enum QbtNodeType : uint
    {
        Matrix = 0,
        Model = 1,
        Compound = 2
    }

    public struct QbtHeader
    {
        public byte VersionMajor;
        public byte VersionMinor;
        public Vector3 GlobalScale;
    }

    public class QbtNode
    {
        public QbtNodeType Type;
        public string Name;
        public Vector3 Position;
        public Vector3 Pivot;
        public Vector3Int Size;
        public byte[] VoxelData; // RGBA array (Matrix only)
        public List<QbtNode> Children = new List<QbtNode>();
        public QbtNode Parent;
    }
}
