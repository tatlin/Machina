﻿using System;

//██╗   ██╗████████╗██╗██╗     
//██║   ██║╚══██╔══╝██║██║     
//██║   ██║   ██║   ██║██║     
//██║   ██║   ██║   ██║██║     
//╚██████╔╝   ██║   ██║███████╗
// ╚═════╝    ╚═╝   ╚═╝╚══════╝

/// <summary>
/// A bunch of static utility functions (probably many of them could be moved to certain related classes...)
/// </summary>
namespace Machina
{
    /// <summary>
    /// Utility static methods
    /// </summary>
    public static class Util
    {
        /// <summary>
        /// Remaps a value from source to target numerical domains.
        /// </summary>
        /// <param name="val"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <param name="newMin"></param>
        /// <param name="newMax"></param>
        /// <returns></returns>
        public static double Remap(double val, double min, double max, double newMin, double newMax)
        {
            return newMin + (val - min) * (newMax - newMin) / (max - min);
        }

        /// <summary>
        /// Converts an array of signed int32 to a byte array. Useful for buffering. 
        /// </summary>
        /// <param name="intArray"></param>
        /// <param name="littleEndian">Set endianness. Windows systems are little endian, while most network communication is bigendian.</param>
        /// <returns></returns>
        public static byte[] Int32ArrayToByteArray(int[] intArray, bool littleEndian = false)
        {
            byte[] buffer = new byte[4 * intArray.Length];

            // Windows systems are little endian, but the UR takes bigendian... :(
            if (BitConverter.IsLittleEndian == littleEndian)
            {
                Buffer.BlockCopy(intArray, 0, buffer, 0, buffer.Length);
            }

            // If the system stores data differently than requested, must manually reverse each byte!
            else
            {
                byte[] bint;
                for (var i = 0; i < intArray.Length; i++)
                {
                    bint = BitConverter.GetBytes(intArray[i]);
                    Array.Reverse(bint);
                    for (var j = 0; j < 4; j++)
                    {
                        buffer[4 * i + j] = bint[j];
                    }
                }
            }

            return buffer;
        }

        /// <summary>
        /// Converts an array of bytes to an array of signed int32. 
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="byteCount">If 0, the whole byte array will be used.</param>
        /// <param name="bytesAreLittleEndian">Is the byte array little endian? This will be used to define how to translate the buffer to this system's endianness.</param>
        /// <returns></returns>
        public static int[] ByteArrayToInt32Array(byte[] bytes, int byteCount = 0, bool bytesAreLittleEndian = false)
        {
            if (byteCount == 0)
            {
                byteCount = bytes.Length;
            }

            //// Sanity -> for the sake of performance, let's trust the user knows what s/he is doing... 
            //if (byteCount % 4 != 0) throw new Exception("byteCount must be multiple of 4");
            //if (byteCount > bytes.Length) throw new Exception("byteCount is larger than array size");
            int[] ints = new int[byteCount / 4];

            // Windows systems are little endian... 
            if (BitConverter.IsLittleEndian == bytesAreLittleEndian)
            {
                Buffer.BlockCopy(bytes, 0, ints, 0, byteCount);
            }

            // ...but network communication is usually bigendian.
            // If the system stores data differently than the array to process, must manually reverse each 4 bytes!
            else
            {
                byte[] clone = new byte[ints.Length * 4];  // don't reverse the order of the passed array
                byte first, second;
                for (int i = 0; i < ints.Length; i++)
                {
                    first = bytes[4 * i];
                    second = bytes[4 * i + 1];

                    clone[4 * i] = bytes[4 * i + 3];
                    clone[4 * i + 1] = bytes[4 * i + 2];
                    clone[4 * i + 2] = second;
                    clone[4 * i + 3] = first;
                }

                Buffer.BlockCopy(clone, 0, ints, 0, byteCount);
            }

            return ints;
        }

    }


    
}
