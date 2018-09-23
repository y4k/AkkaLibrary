using System.Collections;
using System.IO;

namespace AkkaLibrary.Common.Utilities
{
    /// <summary>
    /// Extensions to the binary reader for <see cref="Int24"/>
    /// and bit array utilities
    /// </summary>
    public static class BinaryExtensions
    {
        /// <summary>
        /// Reads three bytes from the binary reader and returns an <see cref="Int24"/>
        /// </summary>
        /// <param name="reader">Binary reader</param>
        /// <returns><see cref="Int24"/></returns>
        public static Int24 ReadInt24(this BinaryReader reader)
        {
            var bytes = reader.ReadBytes(3);
            return Int24.GetValue(bytes, 0);
        }

        /// <summary>
        /// Converts a bit array into a byte
        /// </summary>
        /// <param name="bits">Bit array</param>
        /// <returns>A single byte</returns>
        public static byte ConvertToByte(BitArray bits)
        {
            byte[] bytes = new byte[1];
            bits.CopyTo(bytes, 0);
            return bytes[0];
        }
    }
}