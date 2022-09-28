using System.Runtime.CompilerServices;
using UnityEngine;

namespace System.IO
{
    public static class UnsafeExtenstion
    {
        #region Float Non Alloc

        public static void WriteNonAlloc(this BinaryWriter writer, float value)
        {
            GetBytes(value, Buffer);
            writer.Write(Buffer, 0, 4);
        }

        static readonly byte[] Buffer = new byte[4];

        public static unsafe void GetBytes(float value, byte[] buffer)
        {
            var bytes = (byte*)&value;
            if (BitConverter.IsLittleEndian)
            {
                buffer[0] = bytes[0];
                buffer[1] = bytes[1];
                buffer[2] = bytes[2];
                buffer[3] = bytes[3];
            }
            else
            {
                buffer[0] = bytes[3];
                buffer[1] = bytes[2];
                buffer[2] = bytes[1];
                buffer[3] = bytes[0];
            }
        }

        #endregion

        #region Half

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Half Int16BitsToHalf(short value)
        {
            return *(Half*)&value;
        }

        public static Half ReadHalf(this BinaryReader reader)
        {
            return Int16BitsToHalf(reader.ReadInt16());
        }

        static readonly byte[] BufferHalf = new byte[2];

        public static void WriteNonAlloc(this BinaryWriter writer, Half value)
        {
            GetBytes(value, BufferHalf);
            writer.Write(BufferHalf, 0, 2);
        }

        public static unsafe void GetBytes(Half value, byte[] buffer)
        {
            var bytes = (byte*)&value;
            if (BitConverter.IsLittleEndian)
            {
                buffer[0] = bytes[0];
                buffer[1] = bytes[1];
            }
            else
            {
                buffer[0] = bytes[1];
                buffer[1] = bytes[0];
            }
        }

        #endregion

        public static Vector3 ReadVector3(this BinaryReader reader)
        {
            return new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
        }

        public static void Write(this BinaryWriter writer, in ArraySegment<byte> segment)
        {
            writer.Write(segment.Count);
            writer.Write(segment.Array, 0, segment.Count);
        }

        public static void Write(this BinaryWriter writer, in Vector3 vector)
        {
            writer.WriteNonAlloc(vector.x);
            writer.WriteNonAlloc(vector.y);
            writer.WriteNonAlloc(vector.z);
        }
    }
}