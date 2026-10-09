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
using System.Runtime.Serialization;

namespace Framework.Proxy
{

    /// <summary>
    /// This exception is thrown when a general, unexpected proxy error.   
    /// 用于表示代理连接、握手或协议响应处理过程中出现的代理相关错误。
    /// </summary>
    [Serializable()]
    public class ProxyException : Exception
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public ProxyException()
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="message">Exception message text.</param>
        /// <remarks>创建不带内部异常信息的代理异常。</remarks>
        public ProxyException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="message">Exception message text.</param>
        /// <param name="innerException">The inner exception object.</param>
        /// <remarks>内部异常用于保留底层网络或协议操作的原始失败原因。</remarks>
        public ProxyException(string message, Exception innerException)
            :
           base(message, innerException)
        {
        }

    }

}