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
using System.Net.Sockets;

namespace Framework.Proxy
{
    /// <summary>
    /// Proxy client interface.  This is the interface that all proxy clients must implement.
    /// 定义代理客户端配置与同步/异步 TCP 隧道连接能力的统一契约。
    /// </summary>
    public interface IProxyClient
    {
        /// <summary>
        /// Event handler for CreateConnectionAsync method completed.
        /// 异步代理连接操作完成时触发的事件。
        /// </summary>
        event EventHandler<CreateConnectionAsyncCompletedEventArgs> CreateConnectionAsyncCompleted;

        /// <summary>
        /// Gets or sets proxy host name or IP address.
        /// 获取或设置代理服务器的主机名或 IP 地址。
        /// </summary>
        /// <returns>代理服务器的主机名或 IP 地址。</returns>
        string ProxyHost { get; set; }

        /// <summary>
        /// Gets or sets proxy port number.
        /// 获取或设置代理服务器端口。
        /// </summary>
        /// <returns>代理服务器端口。</returns>
        int ProxyPort { get; set; }

        /// <summary>
        /// Gets String representing the name of the proxy.
        /// 获取代理协议的名称。
        /// </summary>
        /// <returns>代理协议的名称，例如 <c>HTTP</c> 或 <c>SOCKS5</c>。</returns>
        string ProxyName { get; }

        /// <summary>
        /// Gets or sets the amount of time a TcpClient will wait for a send operation to complete successfully.
        /// 设置 TCP 发送操作允许等待的最长时间，单位为毫秒。
        /// </summary>
        /// <returns>TCP 发送超时时间，单位为毫秒。</returns>
        int SendTimeout { set; get; }

        /// <summary>
        /// Gets or sets the amount of time a TcpClient will wait to receive data once a read operation is initiated.
        /// 设置 TCP 接收操作允许等待的最长时间，单位为毫秒。
        /// </summary>
        /// <returns>TCP 接收超时时间，单位为毫秒。</returns>
        int ReceiveTimeout { get; set; }

        /// <summary>
        /// Gets or set the TcpClient object if one was specified in the constructor.
        /// 获取或设置可选的预连接 TCP 客户端。
        /// </summary>
        /// <returns>预连接客户端；未设置时为 <see langword="null"/>。</returns>
        TcpClient TcpClient { get; set; }

        /// <summary>
        /// Creates a remote TCP connection through a proxy server to the destination host on the destination port.
        /// 同步建立到目标地址的代理 TCP 连接。
        /// </summary>
        /// <param name="destinationHost">Destination host name or IP address.</param>
        /// <param name="destinationPort">Port number to connect to on the destination host.</param>
        /// <returns>
        /// Returns an open TcpClient object that can be used normally to communicate
        /// with the destination server
        /// </returns>
        /// <exception cref="ProxyException">当代理服务器配置无效、连接失败或代理响应表示拒绝建立隧道时抛出。</exception>
        /// <remarks>
        /// This method creates a connection to the proxy server and instructs the proxy server
        /// to make a pass through connection to the specified destination host on the specified
        /// port.  
        /// </remarks>
        TcpClient CreateConnection(string destinationHost, int destinationPort);

        /// <summary>
        /// Asynchronously creates a remote TCP connection through a proxy server to the destination host on the destination port.
        /// 在后台建立到目标地址的代理 TCP 连接，并通过完成事件返回结果或错误。
        /// </summary>
        /// <param name="destinationHost">Destination host name or IP address.</param>
        /// <param name="destinationPort">Port number to connect to on the destination host.</param>
        /// <returns>
        /// Returns an open TcpClient object that can be used normally to communicate
        /// with the destination server
        /// </returns>
        /// <exception cref="InvalidOperationException">当已有异步代理连接操作正在运行时抛出。</exception>
        /// <remarks>
        /// 该方法本身无返回值；实际连接结果通过 <see cref="CreateConnectionAsyncCompleted"/> 事件返回。
        /// This method creates a connection to the proxy server and instructs the proxy server
        /// to make a pass through connection to the specified destination host on the specified
        /// port.  
        /// </remarks>
        void CreateConnectionAsync(string destinationHost, int destinationPort);
    }
}