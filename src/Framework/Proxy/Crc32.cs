//
//  Author:
//       Benton Stark <benton.stark@gmail.com>
//
//  Copyright (c) 2016 Benton Stark
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
//
//  You should have received a copy of the GNU Lesser General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
//
//  Author:
//       Benton Stark <benton.stark@gmail.com>
//
//  Copyright (c) 2016 Benton Stark
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
//
//  You should have received a copy of the GNU Lesser General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Security.Cryptography;

namespace Framework.Proxy
{
    /// <summary>
    /// 提供 CRC-32 校验算法的 <see cref="HashAlgorithm"/> 实现，并支持使用自定义多项式与初始种子计算校验值。
    /// </summary>
    internal class Crc32 : HashAlgorithm
    {
        /// <summary>
        /// 标准 CRC-32 使用的反射多项式，对应 0xEDB88320。
        /// </summary>
        public const uint DefaultPolynomial = 0xedb88320;
        /// <summary>
        /// 标准 CRC-32 计算所使用的初始种子，对应全部位为 1。
        /// </summary>
        public const uint DefaultSeed = 0xffffffff;

        private uint hash;
        private uint seed;
        private uint[] table;
        private static uint[] defaultTable;

        /// <summary>
        /// 使用标准 CRC-32 多项式和初始种子创建校验算法实例。
        /// </summary>
        public Crc32()
        {
            table = InitializeTable(DefaultPolynomial);
            seed = DefaultSeed;
            Initialize();
        }

        /// <summary>
        /// 使用指定的反射多项式和初始种子创建 CRC-32 校验算法实例。
        /// </summary>
        /// <param name="polynomial">CRC 字节查表更新所使用的反射多项式。</param>
        /// <param name="seed">开始计算校验值时使用的初始种子。</param>
        public Crc32(uint polynomial, uint seed)
        {
            table = InitializeTable(polynomial);
            this.seed = seed;
            Initialize();
        }

        /// <summary>
        /// 将当前计算状态重置为构造实例时指定的初始种子，以便重新开始计算。
        /// </summary>
        public override void Initialize()
        {
            hash = seed;
        }

        /// <summary>
        /// 使用预计算查表更新输入范围内的 CRC 状态。
        /// </summary>
        /// <param name="buffer">包含待校验数据的字节数组。</param>
        /// <param name="start">待校验数据的起始索引。</param>
        /// <param name="length">待校验的数据长度。</param>
        protected override void HashCore(byte[] buffer, int start, int length)
        {
            hash = CalculateHash(table, hash, buffer, start, length);
        }

        /// <summary>
        /// 对当前 CRC 状态取反，并按网络字节序生成最终校验值。
        /// </summary>
        /// <returns>按大端序排列的四字节 CRC-32 校验值。</returns>
        protected override byte[] HashFinal()
        {
            byte[] hashBuffer = UInt32ToBigEndianBytes(~hash);
            this.HashValue = hashBuffer;
            return hashBuffer;
        }

        /// <summary>
        /// 获取最终 CRC-32 校验值的位数。
        /// </summary>
        /// <returns>固定返回 32，表示校验值包含 32 个二进制位。</returns>
        public override int HashSize
        {
            get { return 32; }
        }

        /// <summary>
        /// 使用标准 CRC-32 参数计算整个字节数组的校验值。
        /// </summary>
        /// <param name="buffer">待校验的字节数组。</param>
        /// <returns>标准 CRC-32 校验值。</returns>
        public static uint Compute(byte[] buffer)
        {
            return ~CalculateHash(InitializeTable(DefaultPolynomial), DefaultSeed, buffer, 0, buffer.Length);
        }

        /// <summary>
        /// 使用指定初始种子和标准多项式计算整个字节数组的 CRC-32 校验值。
        /// </summary>
        /// <param name="seed">计算开始时使用的初始种子。</param>
        /// <param name="buffer">待校验的字节数组。</param>
        /// <returns>CRC-32 校验值。</returns>
        public static uint Compute(uint seed, byte[] buffer)
        {
            return ~CalculateHash(InitializeTable(DefaultPolynomial), seed, buffer, 0, buffer.Length);
        }

        /// <summary>
        /// 使用指定多项式和初始种子计算整个字节数组的 CRC-32 校验值。
        /// </summary>
        /// <param name="polynomial">计算过程中使用的反射多项式。</param>
        /// <param name="seed">计算开始时使用的初始种子。</param>
        /// <param name="buffer">待校验的字节数组。</param>
        /// <returns>CRC-32 校验值。</returns>
        public static uint Compute(uint polynomial, uint seed, byte[] buffer)
        {
            return ~CalculateHash(InitializeTable(polynomial), seed, buffer, 0, buffer.Length);
        }

        private static uint[] InitializeTable(uint polynomial)
        {
            if (polynomial == DefaultPolynomial && defaultTable != null)
                return defaultTable;

            // 反射式 CRC 通过右移和多项式异或生成 256 项查表，每个表项对应一个输入字节。
            uint[] createTable = new uint[256];
            for (int i = 0; i < 256; i++)
            {
                uint entry = (uint)i;
                for (int j = 0; j < 8; j++)
                    if ((entry & 1) == 1)
                        entry = (entry >> 1) ^ polynomial;
                    else
                        entry = entry >> 1;
                createTable[i] = entry;
            }

            if (polynomial == DefaultPolynomial)
                defaultTable = createTable;

            return createTable;
        }

        private static uint CalculateHash(uint[] table, uint seed, byte[] buffer, int start, int size)
        {
            uint crc = seed;
            // 输入字节同时决定查表索引；右移后异或表项是 CRC-32 查表算法的核心更新步骤。
            for (int i = start; i < size; i++)
                unchecked
                {
                    crc = (crc >> 8) ^ table[buffer[i] ^ crc & 0xff];
                }
            return crc;
        }

        private byte[] UInt32ToBigEndianBytes(uint x)
        {
            // 协议字段按网络字节序传输，因此最高有效字节必须排在最前。
            return [
                (byte)((x >> 24) & 0xff),
                (byte)((x >> 16) & 0xff),
                (byte)((x >> 8) & 0xff),
                (byte)(x & 0xff)
            ];
        }
    }
}