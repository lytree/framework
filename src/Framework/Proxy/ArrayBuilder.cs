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

namespace Framework.Proxy
{
    /// <summary>
    /// Builds a one-dimensional byte array of a fixed size and allows the consumer to easily
    /// append data to that byte array.
    /// 用于按固定容量顺序组装一维字节数组的内部构建器；容量在构造或重新调整后固定，写入超出容量时拒绝追加。
    /// </summary>
    internal class ArrayBuilder
    {
        private byte[] _buffer;
        private long _index;

        /// <summary>
        /// Constructor.
        /// 创建指定固定容量的字节数组构建器。
        /// </summary>
        /// <param name="size">The fixed size of the one-dimensional byte array to build.</param>
        public ArrayBuilder(long size)
        {
            _buffer = new byte[size];
        }

        /// <summary>
        /// Gets the length of the ArrayBuilder buffer in bytes.
        /// </summary>
        /// <returns>固定大小的底层字节数组长度。</returns>
        public int Length
        {
            get { return _buffer.Length; }
        }

        /// <summary>
        /// Appends bytes to the byte array.
        /// 将字节数组的全部内容追加到构建器当前位置。
        /// </summary>
        /// <param name="data">Bytes to append.</param>
        public void Append(params byte[] data)
        {
            Append(data, 0);
        }

        /// <summary>
        /// Appends bytes to the byte array at a specific starting index.
        /// 从源数组的指定索引开始追加字节，并推进内部写入位置。
        /// </summary>
        /// <param name="data">Bytes to append.</param>
        /// <param name="startIndex">Starting point to append bytes.</param>
        /// <exception cref="Exception">当追加后的数据长度超过固定缓冲区容量时抛出。</exception>
        public void Append(byte[] data, long startIndex)
        {
            if (_index + data.Length - startIndex > _buffer.Length)
            {
                throw new Exception(string.Format("Data is too large to append.  Current size is {0} bytes.", _buffer.Length.ToString()));
            }

            Array.Copy(data, startIndex, _buffer, _index, data.Length - startIndex);
            _index += data.Length - startIndex;
        }

        /// <summary>
        /// Returns the byte array.
        /// 复制并返回底层字节数组，避免调用方直接修改内部数据。
        /// </summary>
        /// <returns>Array of bytes.</returns>
        public byte[] GetBytes()
        {
            // we don't want to return a pointer to our internal array 
            // because the user might then modify the array and in return
            // modify the private _buffer variable.  This is an issue.
            // So we need to return a copy of the array instead.
            byte[] copy = new byte[_buffer.Length];
            Array.Copy(_buffer, copy, _buffer.Length);
            return copy;
        }

        /// <summary>
        /// Clears the bytes array.  Any appended data will be lost.  The original byte array size is preserved.
        /// 清空底层数组并将写入位置重置为起点，容量保持不变。
        /// </summary>
        public void Clear()
        {
            Array.Clear(_buffer, 0, _buffer.Length);
            _index = 0;
        }

        /// <summary>
        /// Creates a new byte array of the size specificed.  Any appended data will be lost.
        /// 按新容量重建底层数组并重置写入位置，已追加的数据会丢失。
        /// </summary>
        /// <param name="size">Size to rediminsion the array.</param>
        public void Redim(long size)
        {
            _buffer = new byte[size];
            _index = 0;
        }

    }
}
