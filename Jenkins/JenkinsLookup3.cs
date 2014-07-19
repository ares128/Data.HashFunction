﻿using System;
using System.Collections.Generic;
using System.Data.HashFunction.Utilities;
using System.Data.HashFunction.Utilities.IntegerManipulation;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace System.Data.HashFunction
{
    /// <summary>
    /// Implementation of Bob Jenkins' Lookup3 hash function as specified at http://burtleburtle.net/bob/c/lookup3.c.
    /// </summary>
    public class JenkinsLookup3
        : HashFunctionBase
    {
        /// <inheritdoc/>
        public override IEnumerable<int> ValidHashSizes { get { return new[] { 32, 64}; } }

        /// <summary>
        /// First seed value for hash calculation.
        /// </summary>
        /// <remarks>
        /// Only seed value for 32-bit mode, first seed value for 64-bit mode.
        /// </remarks>
        public UInt32 InitVal1 { get; set; }

        /// <summary>
        /// Second seed value for hash calculation.
        /// </summary>
        /// <remarks>
        /// Not used for 32-bit mode, second seed value for 64-bit mode.
        /// </remarks>
        public UInt32 InitVal2 { get; set; }


        /// <inheritdoc/>
        protected override bool RequiresSeekableStream { get { return true; } }


        /// <summary>
        /// Constructs new <see cref="JenkinsLookup3"/> instance.
        /// </summary>
        public JenkinsLookup3()
            : base(64)
        {
            InitVal1 = 0;
            InitVal2 = 0;
        }


        /// <inheritdoc/>
        protected override byte[] ComputeHashInternal(Stream data)
        {
            UInt32 a = 0xdeadbeef + (UInt32) data.Length + InitVal1;
            UInt32 b = 0xdeadbeef + (UInt32) data.Length + InitVal1;
            UInt32 c = 0xdeadbeef + (UInt32) data.Length + InitVal1;

            if (HashSize == 64)
                c += InitVal2;


            var dataGroups = data.AsGroupedStreamData(12);
            int dataCount = 0;
            
            foreach (var dataGroup in dataGroups)
            {
                a += BitConverter.ToUInt32(dataGroup, 0);
                b += BitConverter.ToUInt32(dataGroup, 4);
                c += BitConverter.ToUInt32(dataGroup, 8);

                Mix(ref a, ref b, ref c);

                dataCount += dataGroup.Length;
            }


            var remainder = dataGroups.Remainder;

            switch (remainder.Length)
            {
                case 11: c += (UInt32) remainder[10] << 16;    goto case 10;
                case 10: c += (UInt32) remainder[ 9] << 8;     goto case  9;
                case  9: c += (UInt32) remainder[ 8];           goto case 8;   

                case 8:
                    b += BitConverter.ToUInt32(remainder, 4);
                    goto case 4;

                case 7: b += (UInt32) remainder[6] << 16;  goto case 6;
                case 6: b += (UInt32) remainder[5] <<  8;  goto case 5;
                case 5: b += (UInt32) remainder[4];        goto case 4;

                case 4:
                    a += BitConverter.ToUInt32(remainder, 0);
                    break;
                    
                case 3: a += (UInt32) remainder[2] << 16;  goto case 2;
                case 2: a += (UInt32) remainder[1] <<  8;  goto case 1;
                case 1: a += (UInt32) remainder[0];        break;
            }

            if (remainder.Length != 0 || dataCount == 12)
                Final(ref a, ref b, ref c);

            switch (HashSize)
            {
                case 32:
                    return BitConverter.GetBytes(c);
                case 64:
                    return BitConverter.GetBytes((((UInt64) b) << 32) | c);
                default:
                    throw new ArgumentOutOfRangeException("HashSize");
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Mix(ref UInt32 a, ref UInt32 b, ref UInt32 c)
        {
            a -= c; a ^= Rot(c,  4); c += b;
            b -= a; b ^= Rot(a,  6); a += c;
            c -= b; c ^= Rot(b,  8); b += a;

            a -= c; a ^= Rot(c, 16); c += b;
            b -= a; b ^= Rot(a, 19); a += c;
            c -= b; c ^= Rot(b,  4); b += a;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Final(ref UInt32 a, ref UInt32 b, ref UInt32 c)
        {
            c ^= b; c -= Rot(b, 14);
            a ^= c; a -= Rot(c, 11);
            b ^= a; b -= Rot(a, 25);

            c ^= b; c -= Rot(b, 16);
            a ^= c; a -= Rot(c,  4);
            b ^= a; b -= Rot(a, 14);

            c ^= b; c -= Rot(b, 24);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private UInt32 Rot(UInt32 value, int positions)
        {
            return (value << positions) | (value >> (32 - positions));
        }

    }
}