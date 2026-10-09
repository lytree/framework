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
    /// The type of proxy.
    /// 表示工厂可创建的代理协议类型。
    /// </summary>
    public enum ProxyType
    {
        /// <summary>
        /// No Proxy specified.  Note this option will cause an exception to be thrown if used to create a proxy object by the factory.
        /// 不指定代理类型；传给工厂创建客户端时会抛出异常。
        /// </summary>
        None,
        /// <summary>
        /// HTTP Proxy
        /// HTTP CONNECT 代理类型。
        /// </summary>
        Http,
        /// <summary>
        /// SOCKS v4 Proxy
        /// SOCKS v4 代理类型。
        /// </summary>
        Socks4,
        /// <summary>
        /// SOCKS v4a Proxy
        /// 支持由代理端解析目标域名的 SOCKS v4a 代理类型。
        /// </summary>
        Socks4a,
        /// <summary>
        /// SOCKS v5 Proxy
        /// SOCKS v5 代理类型。
        /// </summary>
        Socks5
    }

    /// <summary>
    /// Factory class for creating new proxy client objects.
    /// 根据协议类型、地址和凭据创建对应的 <see cref="IProxyClient"/> 实现；仅负责实例化，不负责建立网络连接。
    /// </summary>
    /// <remarks>
    /// <code>
    /// // create an instance of the client proxy factory
    /// ProxyClientFactory factory = new ProxyClientFactory();
    ///        
	/// // use the proxy client factory to generically specify the type of proxy to create
    /// // the proxy factory method CreateProxyClient returns an IProxyClient object
    /// IProxyClient proxy = factory.CreateProxyClient(ProxyType.Http, "localhost", 6588);
    ///
	/// // create a connection through the proxy to www.starksoft.com over port 80
    /// System.Net.Sockets.TcpClient tcpClient = proxy.CreateConnection("www.starksoft.com", 80);
    /// </code>
    /// </remarks>
    public class ProxyClientFactory
    {
        /// <summary>
        /// Factory method for creating new proxy client objects.
        /// 根据解析结果中的协议、主机、端口和凭据创建代理客户端。
        /// </summary>
        /// <param name="proxy">The type of proxy client to create.</param>
        /// <returns>Proxy client object.</returns>
        /// <remarks>当前实现以小写精确匹配 <c>http</c>、<c>socket4</c>、<c>socket4a</c> 和 <c>socket5</c>。</remarks>
        /// <exception cref="ProxyException">当协议名称不属于当前实现支持的值时抛出。</exception>
        /// <exception cref="ArgumentOutOfRangeException">当解析结果中的端口不在 1 至 65535 范围内时，由具体代理客户端构造函数抛出。</exception>
        public IProxyClient CreateProxyClient(ParsedUrl proxy)
        {

            return proxy.Scheme switch
            {
                "http" => new HttpProxyClient(proxy.Host, proxy.Port, proxy.Username, proxy.Password),
                "socket4" => new Socks4ProxyClient(proxy.Host, proxy.Port, proxy.Username),
                "socket4a" => new Socks4aProxyClient(proxy.Host, proxy.Port, proxy.Username),
                "socket5" => new Socks5ProxyClient(proxy.Host, proxy.Port, proxy.Username, proxy.Password),
                _ => throw new ProxyException(string.Format("Unknown proxy type {0}.", proxy.Scheme)),
            };
        }

        /// <summary>
        /// Factory method for creating new proxy client objects using an existing TcpClient connection object.
        /// 使用调用方提供且已连接的 TCP 客户端创建代理客户端。
        /// </summary>
        /// <param name="type">The type of proxy client to create.</param>
        /// <param name="tcpClient">Open TcpClient object.</param>
        /// <returns>Proxy client object.</returns>
        /// <exception cref="ArgumentOutOfRangeException">当 <paramref name="type"/> 为 <see cref="ProxyType.None"/> 时抛出。</exception>
        /// <exception cref="ArgumentNullException">当 <paramref name="tcpClient"/> 为 <see langword="null"/> 时抛出。</exception>
        /// <exception cref="ProxyException">当 <paramref name="type"/> 不是受支持的代理类型时抛出。</exception>
        public IProxyClient CreateProxyClient(ProxyType type, TcpClient tcpClient)
        {
            if (type == ProxyType.None)
                throw new ArgumentOutOfRangeException(nameof(type));

            return type switch
            {
                ProxyType.Http => new HttpProxyClient(tcpClient),
                ProxyType.Socks4 => new Socks4ProxyClient(tcpClient),
                ProxyType.Socks4a => new Socks4aProxyClient(tcpClient),
                ProxyType.Socks5 => new Socks5ProxyClient(tcpClient),
                _ => throw new ProxyException(string.Format("Unknown proxy type {0}.", type.ToString())),
            };
        }

        /// <summary>
        /// Factory method for creating new proxy client objects.  
        /// 根据代理类型、主机和端口创建不携带预配置凭据的代理客户端。
        /// </summary>
        /// <param name="type">The type of proxy client to create.</param>
        /// <param name="proxyHost">The proxy host or IP address.</param>
        /// <param name="proxyPort">The proxy port number.</param>
        /// <returns>Proxy client object.</returns>
        /// <exception cref="ArgumentOutOfRangeException">当代理类型为 <see cref="ProxyType.None"/>，或具体客户端要求端口必须位于 1 至 65535 范围时抛出。</exception>
        /// <exception cref="ProxyException">当 <paramref name="type"/> 不是受支持的代理类型时抛出。</exception>
        public IProxyClient CreateProxyClient(ProxyType type, string proxyHost, int proxyPort)
        {
            if (type == ProxyType.None)
                throw new ArgumentOutOfRangeException(nameof(type));

            return type switch
            {
                ProxyType.Http => new HttpProxyClient(proxyHost, proxyPort),
                ProxyType.Socks4 => new Socks4ProxyClient(proxyHost, proxyPort),
                ProxyType.Socks4a => new Socks4aProxyClient(proxyHost, proxyPort),
                ProxyType.Socks5 => new Socks5ProxyClient(proxyHost, proxyPort),
                _ => throw new ProxyException(string.Format("Unknown proxy type {0}.", type.ToString())),
            };
        }

        /// <summary>
        /// Factory method for creating new proxy client objects.  
        /// 根据代理类型、地址及凭据创建代理客户端；密码仅用于 HTTP 和 SOCKS5 客户端。
        /// </summary>
        /// <param name="type">The type of proxy client to create.</param>
        /// <param name="proxyHost">The proxy host or IP address.</param>
        /// <param name="proxyPort">The proxy port number.</param>
        /// <param name="proxyUsername">The proxy username.  This parameter is only used by Http, Socks4 and Socks5 proxy objects.</param>
        /// <param name="proxyPassword">The proxy user password.  This parameter is only used Http, Socks5 proxy objects.</param>
        /// <returns>Proxy client object.</returns>
        /// <exception cref="ArgumentOutOfRangeException">当代理类型为 <see cref="ProxyType.None"/>，或具体客户端要求端口必须位于 1 至 65535 范围时抛出。</exception>
        /// <exception cref="ArgumentNullException">当 HTTP 或 SOCKS5 客户端所需的代理主机、用户名、密码或密码为 <see langword="null"/> 时抛出。</exception>
        /// <exception cref="ProxyException">当 <paramref name="type"/> 不是受支持的代理类型时抛出。</exception>
        public IProxyClient CreateProxyClient(ProxyType type, string proxyHost, int proxyPort, string proxyUsername, string proxyPassword)
        {
            if (type == ProxyType.None)
                throw new ArgumentOutOfRangeException(nameof(type));

            return type switch
            {
                ProxyType.Http => new HttpProxyClient(proxyHost, proxyPort, proxyUsername, proxyPassword),
                ProxyType.Socks4 => new Socks4ProxyClient(proxyHost, proxyPort, proxyUsername),
                ProxyType.Socks4a => new Socks4aProxyClient(proxyHost, proxyPort, proxyUsername),
                ProxyType.Socks5 => new Socks5ProxyClient(proxyHost, proxyPort, proxyUsername, proxyPassword),
                _ => throw new ProxyException(string.Format("Unknown proxy type {0}.", type.ToString())),
            };
        }

        /// <summary>
        /// Factory method for creating new proxy client objects.  
        /// 创建代理客户端并将调用方提供的已连接 TCP 客户端设为后续连接所使用的客户端。
        /// </summary>
        /// <param name="type">The type of proxy client to create.</param>
        /// <param name="tcpClient">Open TcpClient object.</param>
        /// <param name="proxyHost">The proxy host or IP address.</param>
        /// <param name="proxyPort">The proxy port number.</param>
        /// <param name="proxyUsername">The proxy username.  This parameter is only used by Http, Socks4 and Socks5 proxy objects.</param>
        /// <param name="proxyPassword">The proxy user password.  This parameter is only used Http, Socks5 proxy objects.</param>
        /// <returns>Proxy client object.</returns>
        /// <exception cref="ArgumentOutOfRangeException">当代理类型为 <see cref="ProxyType.None"/>，或具体客户端要求端口必须位于 1 至 65535 范围时抛出。</exception>
        /// <exception cref="ArgumentNullException">当 HTTP 或 SOCKS5 客户端所需的代理主机、用户名或密码为 <see langword="null"/> 时抛出。</exception>
        /// <exception cref="ProxyException">当 <paramref name="type"/> 不是受支持的代理类型时抛出。</exception>
        public IProxyClient CreateProxyClient(ProxyType type, TcpClient tcpClient, string proxyHost, int proxyPort, string proxyUsername, string proxyPassword)
        {
            IProxyClient c = CreateProxyClient(type, proxyHost, proxyPort, proxyUsername, proxyPassword);
            c.TcpClient = tcpClient;
            return c;
        }


    }



}
