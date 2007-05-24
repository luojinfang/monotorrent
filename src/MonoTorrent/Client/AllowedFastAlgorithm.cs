//
// AllowedFastAlgorithm.cs
//
// Authors:
//   Alan McGovern alan.mcgovern@gmail.com
//
// Copyright (C) 2006 Alan McGovern
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//


using System;
using System.Collections.Generic;
using MonoTorrent.Client.Encryption;
using System.Net;
using System.Security.Cryptography;

namespace MonoTorrent.Client
{
    /// <summary>
    /// Description of AllowedFastAlgorithm.
    /// </summary>
    public static class AllowedFastAlgorithm
    {
        public const int AllowedFastPieceCount = 10;
        private static SHA1Managed hasher = new SHA1Managed();

        /// <summary>
        /// Calculates the pieces a peer can request even if they're choked
        /// </summary>
        /// <param name="addressBytes">The IPv4 address of the peer in byte format</param>
        /// <param name="infohash">The info hash of the torrent</param>
        /// <param name="numberOfPieces">The number of pieces in the torrent</param>
        /// <returns></returns>
        internal static UInt32Collection Calculate(byte[] addressBytes, byte[] infohash, UInt32 numberOfPieces)
        {
            return Calculate(addressBytes, infohash, AllowedFastPieceCount, numberOfPieces);
        }

        internal static UInt32Collection Calculate(byte[] addressBytes, byte[] infohash, int count, UInt32 numberOfPieces)
        {
            byte[] hashBuffer = new byte[24];                // The hash buffer to be used in hashing
            UInt32Collection results = new UInt32Collection(count);  // The results array which will be returned

            // 1) Convert the bytes into an int32 and make them Network order
            int ip = IPAddress.HostToNetworkOrder(BitConverter.ToInt32(addressBytes, 0));

            // 2) binary AND this value with 0xFFFFFF00 to select the three most sigificant bytes
            int ipMostSignificant = (int)(0xFFFFFF00 & ip);

            // 3) Make ipMostSignificant into NetworkOrder
            UInt32 ip2 = (UInt32)IPAddress.HostToNetworkOrder(ipMostSignificant);

            // 4) Copy ip2 into the hashBuffer
            Buffer.BlockCopy(BitConverter.GetBytes(ip2), 0, hashBuffer, 0, 4);

            // 5) Copy the infohash into the hashbuffer
            Buffer.BlockCopy(infohash, 0, hashBuffer, 4, 20);

            // 6) Keep hashing and cycling until we have AllowedFastPieceCount number of results
            // Then return that result
            while (true)
            {
                lock (hasher)
                    hashBuffer = hasher.ComputeHash(hashBuffer);

                for (int i = 0; i < 20; i += 4)
                {
                    UInt32 result = (UInt32)IPAddress.HostToNetworkOrder(BitConverter.ToInt32(hashBuffer, i));
                    results.Add(result % numberOfPieces);
                    if (count == results.Count)
                        return results;
                }
            }
        }
    }
}