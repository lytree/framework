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

using System.Security.Cryptography;

namespace Framework.Proxy
{
    /// <summary>
    /// Longitudinal Redundancy Check hash algorithm implementation.
    /// 实现对输入字节逐位异或汇总的纵向冗余校验算法。
    /// </summary>
    internal class Lrc : HashAlgorithm
    {
        private byte _lrc;

        /// <summary>
        /// Initializes an implementation of HashAlgorithm.
        /// 将 LRC 计算状态重置为零。
        /// </summary>
        public override void Initialize()
        {
            _lrc = 0;
        }

        /// <summary>
        /// Computes LRC hash on supplied data.
        /// 将指定范围内的输入字节异或合并到当前 LRC 状态。
        /// </summary>
        /// <param name="buffer">Buffer data to computer LRC value on.</param>
        /// <param name="offset">Offset value to begin hash function processing.</param>
        /// <param name="count">Count of bytes to perform hash function on.</param>
        protected override void HashCore(byte[] buffer, int offset, int count)
        {
            // Save the text in the buffer. 
            for (int i = offset; i < count; i++)
            {
                _lrc ^= buffer[i];
            }
        }

        /// <summary>
        /// Finialize LRC hash value.
        /// 返回单字节的最终 LRC 校验结果。
        /// </summary>
        /// <returns>Finalize byte array.</returns>
        protected override byte[] HashFinal()
        {
            byte[] finalHash = [_lrc];
            return finalHash;
        }
    }
}
