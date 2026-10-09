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
    /// Compute standard CRC-16 hash value.  This class
    /// inherits from the standard .NET HashAlgorithm
    /// class.
    /// 基于查表算法实现标准 CRC-16 校验。
    /// </summary>
    internal class Crc16 : HashAlgorithm
    {
        // constants
        private const ushort Polynomial = 0xA001;
        private const int HashTableSize = 256;

        // hash lookup table
        private ushort[] _table = new ushort[HashTableSize];
        private ushort _crc = 0;

        /// <summary>
        /// Constructor
        /// 创建并初始化 CRC-16 校验实例。
        /// </summary>
        public Crc16()
        {
            Initialize();
        }

        /// <summary>
        /// Hash core override function.
        /// 将指定范围内的输入字节合并到当前 CRC-16 状态。
        /// </summary>
        /// <param name="buffer">Data buffer to hash.</param>
        /// <param name="offset">Offset value in the data buffer.</param>
        /// <param name="count">Number of bytes to hash.</param>
        protected override void HashCore(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            if (offset < 0 || count < 0 || offset + count > buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(count), "offset+count is out of buffer range.");

            // 反射多项式查表算法：低字节索引 = (CRC ^ byte) & 0xFF；
            // 状态用 CRC 自身右移 8 位后与表项异或更新。
            for (int i = 0; i < count; i++)
            {
                byte index = (byte)((_crc ^ buffer[offset + i]) & 0xFF);
                _crc = (ushort)((_crc >> 8) ^ _table[index]);
            }
        }

        /// <summary>
        /// Hash final bytes.  Nothing to do except return the hash result.
        /// 返回当前 CRC-16 状态的最终字节表示（按主机字节序）。
        /// </summary>
        /// <returns>Final hashed bytes.</returns>
        protected override byte[] HashFinal()
        {
            byte[] hashBuffer = BitConverter.GetBytes(_crc);
            // 同步设置基类 HashValue，保证 HashAlgorithm.Hash 与 TransformBlock 链路在所有 .NET 版本下都能读到结果。
            this.HashValue = hashBuffer;
            return hashBuffer;
        }

        /// <summary>
        /// Initialize the CRC16 hashing function by building a
        /// hash value table for fast lookup.
        /// 构建用于 CRC-16 快速查表的字节映射表。
        /// </summary>
        public override void Initialize()
        {
            _crc = 0;
            // 预计算每个可能字节的移位与多项式异或结果，避免在正式校验时逐位重复运算。
            // 标准反射式 CRC-16（多项式 0xA001）查表生成：仅右移 value，不右移 temp。
            for (ushort i = 0; i < _table.Length; ++i)
            {
                ushort value = i;
                for (byte j = 0; j < 8; ++j)
                {
                    if ((value & 0x0001) != 0)
                        value = (ushort)((value >> 1) ^ Polynomial);
                    else
                        value >>= 1;
                }
                _table[i] = value;
            }
        }
    }

}