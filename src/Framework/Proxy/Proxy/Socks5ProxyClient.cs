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
using System.ComponentModel;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;


namespace Framework.Proxy
{
    /// <summary>
    /// Socks5 connection proxy class.  This class implements the Socks5 standard proxy protocol.
    /// SOCKS5 TCP 代理客户端，负责认证协商、目标地址编码、CONNECT 握手和响应状态校验。
    /// </summary>
    /// <remarks>
    /// This implementation supports TCP proxy connections with a Socks v5 server.
    /// </remarks>
    public class Socks5ProxyClient : IProxyClient
    {
        private string _proxyHost;
        private int _proxyPort;
        private string _proxyUserName;
        private string _proxyPassword;
        private SocksAuthentication _proxyAuthMethod;
        private TcpClient _tcpClient;
        private TcpClient _tcpClientCached;

        private const string PROXY_NAME = "SOCKS5";
        private const int SOCKS5_DEFAULT_PORT = 1080;

        private const byte SOCKS5_VERSION_NUMBER = 5;
        private const byte SOCKS5_RESERVED = 0x00;
        private const byte SOCKS5_AUTH_NUMBER_OF_AUTH_METHODS_SUPPORTED = 2;
        private const byte SOCKS5_AUTH_METHOD_NO_AUTHENTICATION_REQUIRED = 0x00;
        private const byte SOCKS5_AUTH_METHOD_GSSAPI = 0x01;
        private const byte SOCKS5_AUTH_METHOD_USERNAME_PASSWORD = 0x02;
        private const byte SOCKS5_AUTH_METHOD_IANA_ASSIGNED_RANGE_BEGIN = 0x03;
        private const byte SOCKS5_AUTH_METHOD_IANA_ASSIGNED_RANGE_END = 0x7f;
        private const byte SOCKS5_AUTH_METHOD_RESERVED_RANGE_BEGIN = 0x80;
        private const byte SOCKS5_AUTH_METHOD_RESERVED_RANGE_END = 0xfe;
        private const byte SOCKS5_AUTH_METHOD_REPLY_NO_ACCEPTABLE_METHODS = 0xff;
        private const byte SOCKS5_CMD_CONNECT = 0x01;
        private const byte SOCKS5_CMD_BIND = 0x02;
        private const byte SOCKS5_CMD_UDP_ASSOCIATE = 0x03;
        private const byte SOCKS5_CMD_REPLY_SUCCEEDED = 0x00;
        private const byte SOCKS5_CMD_REPLY_GENERAL_SOCKS_SERVER_FAILURE = 0x01;
        private const byte SOCKS5_CMD_REPLY_CONNECTION_NOT_ALLOWED_BY_RULESET = 0x02;
        private const byte SOCKS5_CMD_REPLY_NETWORK_UNREACHABLE = 0x03;
        private const byte SOCKS5_CMD_REPLY_HOST_UNREACHABLE = 0x04;
        private const byte SOCKS5_CMD_REPLY_CONNECTION_REFUSED = 0x05;
        private const byte SOCKS5_CMD_REPLY_TTL_EXPIRED = 0x06;
        private const byte SOCKS5_CMD_REPLY_COMMAND_NOT_SUPPORTED = 0x07;
        private const byte SOCKS5_CMD_REPLY_ADDRESS_TYPE_NOT_SUPPORTED = 0x08;
        private const byte SOCKS5_ADDRTYPE_IPV4 = 0x01;
        private const byte SOCKS5_ADDRTYPE_DOMAIN_NAME = 0x03;
        private const byte SOCKS5_ADDRTYPE_IPV6 = 0x04;

        /// <summary>
        /// Authentication itemType.
        /// </summary>
        private enum SocksAuthentication
        {
            /// <summary>
            /// No authentication used.
            /// </summary>
            None,
            /// <summary>
            /// Username and password authentication.
            /// </summary>
            UsernamePassword
        }

        /// <summary>
        /// Create a Socks5 proxy client object. 
        /// 创建使用默认配置的 SOCKS5 代理客户端。
        /// </summary>
        public Socks5ProxyClient() { }

        /// <summary>
        /// Creates a Socks5 proxy client object using the supplied TcpClient object connection.
        /// 使用调用方提供的 TCP 连接创建 SOCKS5 代理客户端。
        /// </summary>
        /// <param name="tcpClient">A TcpClient connection object.</param>
        /// <exception cref="ArgumentNullException">当 <paramref name="tcpClient"/> 为 <see langword="null"/> 时抛出。</exception>
        public Socks5ProxyClient(TcpClient tcpClient)
        {
            ArgumentNullException.ThrowIfNull(tcpClient);

            _tcpClientCached = tcpClient;
        }

        /// <summary>
        /// Create a Socks5 proxy client object.  The default proxy port 1080 is used.
        /// 创建指定代理主机并使用默认端口 1080 的 SOCKS5 代理客户端。
        /// </summary>
        /// <param name="proxyHost">Host name or IP address of the proxy server.</param>
        /// <exception cref="ArgumentNullException">当 <paramref name="proxyHost"/> 为 <see langword="null"/> 或空字符串时抛出。</exception>
        public Socks5ProxyClient(string proxyHost)
        {
            if (string.IsNullOrEmpty(proxyHost))
                throw new ArgumentNullException(nameof(proxyHost));

            _proxyHost = proxyHost;
            _proxyPort = SOCKS5_DEFAULT_PORT;
        }

        /// <summary>
        /// Create a Socks5 proxy client object.
        /// 创建指定代理主机和端口的 SOCKS5 代理客户端。
        /// </summary>
        /// <param name="proxyHost">Host name or IP address of the proxy server.</param>
        /// <param name="proxyPort">Port used to connect to proxy server.</param>
        /// <exception cref="ArgumentNullException">当 <paramref name="proxyHost"/> 为 <see langword="null"/> 或空字符串时抛出。</exception>
        /// <exception cref="ArgumentOutOfRangeException">当 <paramref name="proxyPort"/> 不在 1 至 65535 范围内时抛出。</exception>
        public Socks5ProxyClient(string proxyHost, int proxyPort)
        {
            if (string.IsNullOrEmpty(proxyHost))
                throw new ArgumentNullException(nameof(proxyHost));

            if (proxyPort <= 0 || proxyPort > 65535)
                throw new ArgumentOutOfRangeException(nameof(proxyPort), "port must be greater than zero and less than 65535");

            _proxyHost = proxyHost;
            _proxyPort = proxyPort;
        }

        /// <summary>
        /// Create a Socks5 proxy client object.  The default proxy port 1080 is used.
        /// 创建带有用户名密码并使用默认端口 1080 的 SOCKS5 代理客户端。
        /// </summary>
        /// <param name="proxyHost">Host name or IP address of the proxy server.</param>
        /// <param name="proxyUserName">Proxy authentication user name.</param>
        /// <param name="proxyPassword">Proxy authentication password.</param>
        /// <exception cref="ArgumentNullException">当代理主机、用户名或密码为 <see langword="null"/>，或代理主机为空字符串时抛出。</exception>
        public Socks5ProxyClient(string proxyHost, string proxyUserName, string proxyPassword)
        {
            if (string.IsNullOrEmpty(proxyHost))
                throw new ArgumentNullException(nameof(proxyHost));

            if (proxyUserName == null)
                throw new ArgumentNullException(nameof(proxyUserName));

            if (proxyPassword == null)
                throw new ArgumentNullException(nameof(proxyPassword));

            _proxyHost = proxyHost;
            _proxyPort = SOCKS5_DEFAULT_PORT;
            _proxyUserName = proxyUserName;
            _proxyPassword = proxyPassword;
        }

        /// <summary>
        /// Create a Socks5 proxy client object.  
        /// 创建指定代理地址和用户名密码的 SOCKS5 代理客户端。
        /// </summary>
        /// <param name="proxyHost">Host name or IP address of the proxy server.</param>
        /// <param name="proxyPort">Port used to connect to proxy server.</param>
        /// <param name="proxyUserName">Proxy authentication user name.</param>
        /// <param name="proxyPassword">Proxy authentication password.</param>
        /// <exception cref="ArgumentNullException">当代理主机、用户名或密码为 <see langword="null"/>，或代理主机为空字符串时抛出。</exception>
        /// <exception cref="ArgumentOutOfRangeException">当 <paramref name="proxyPort"/> 不在 1 至 65535 范围内时抛出。</exception>
        public Socks5ProxyClient(string proxyHost, int proxyPort, string proxyUserName, string proxyPassword)
        {
            if (string.IsNullOrEmpty(proxyHost))
                throw new ArgumentNullException(nameof(proxyHost));

            if (proxyPort <= 0 || proxyPort > 65535)
                throw new ArgumentOutOfRangeException(nameof(proxyPort), "port must be greater than zero and less than 65535");

            if (proxyUserName == null)
                throw new ArgumentNullException(nameof(proxyUserName));

            if (proxyPassword == null)
                throw new ArgumentNullException(nameof(proxyPassword));

            _proxyHost = proxyHost;
            _proxyPort = proxyPort;
            _proxyUserName = proxyUserName;
            _proxyPassword = proxyPassword;
        }

        /// <summary>
        /// Gets or sets host name or IP address of the proxy server.
        /// 获取或设置代理服务器的主机名或 IP 地址。
        /// </summary>
        /// <returns>代理服务器的主机名或 IP 地址。</returns>
        public string ProxyHost
        {
            get { return _proxyHost; }
            set { _proxyHost = value; }
        }

        /// <summary>
        /// Gets or sets port used to connect to proxy server.
        /// 获取或设置用于连接代理服务器的端口。
        /// </summary>
        /// <returns>代理服务器端口。</returns>
        public int ProxyPort
        {
            get { return _proxyPort; }
            set { _proxyPort = value; }
        }

        /// <summary>
        /// Gets string representing the name of the proxy. 
        /// 获取当前代理协议的名称。
        /// </summary>
        /// <remarks>This property will always return the value 'SOCKS5'</remarks>
        /// <returns>固定返回 <c>SOCKS5</c>。</returns>
        public string ProxyName
        {
            get { return PROXY_NAME; }
        }

        /// <summary>
        /// Gets or sets the amount of time, in milliseconds, that a TCP send operation may wait before timing out.
        /// 设置代理连接后续使用的 TCP 发送超时时间，单位为毫秒。
        /// </summary>
        public int SendTimeout
        {
            get => _sendTimeout;
            set => _sendTimeout = value;
        }

        /// <summary>
        /// Gets or sets the amount of time, in milliseconds, that a TCP receive operation may wait before timing out.
        /// 设置代理连接后续使用的 TCP 接收超时时间，单位为毫秒。
        /// </summary>
        public int ReceiveTimeout
        {
            get => _receiveTimeout;
            set => _receiveTimeout = value;
        }

        /// <summary>
        /// Gets or sets proxy authentication user name.
        /// 获取或设置代理认证用户名。
        /// </summary>
        /// <returns>代理认证用户名。</returns>
        public string ProxyUserName
        {
            get { return _proxyUserName; }
            set { _proxyUserName = value; }
        }

        /// <summary>
        /// Gets or sets proxy authentication password.
        /// 获取或设置代理认证密码。
        /// </summary>
        /// <returns>代理认证密码。</returns>
        public string ProxyPassword
        {
            get { return _proxyPassword; }
            set { _proxyPassword = value; }
        }

        /// <summary>
        /// Gets or sets the TcpClient object. 
        /// This property can be set prior to executing CreateConnection to use an existing TcpClient connection.
        /// 获取或设置可选的预连接 TCP 客户端。
        /// </summary>
        /// <returns>预连接客户端；未设置时为 <see langword="null"/>。</returns>
        public TcpClient TcpClient
        {
            get { return _tcpClientCached; }
            set { _tcpClientCached = value; }
        }

        /// <summary>
        /// Creates a remote TCP connection through a proxy server to the destination host on the destination port.
        /// 同步完成 SOCKS5 认证协商并建立到目标地址的 CONNECT 隧道连接。
        /// </summary>
        /// <param name="destinationHost">Destination host name or IP address of the destination server.</param>
        /// <param name="destinationPort">Port number to connect to on the destination host.</param>
        /// <returns>
        /// Returns an open TcpClient object that can be used normally to communicate
        /// with the destination server
        /// </returns>
        /// <exception cref="ArgumentNullException">当 <paramref name="destinationHost"/> 为 <see langword="null"/> 或空字符串时抛出。</exception>
        /// <exception cref="ArgumentOutOfRangeException">当目标端口不在 1 至 65535 范围内时抛出。</exception>
        /// <exception cref="ProxyException">当代理配置无效、连接或认证失败、目标地址不受支持或 SOCKS5 服务器拒绝连接时抛出。</exception>
        /// <remarks>
        /// This method creates a connection to the proxy server and instructs the proxy server
        /// to make a pass through connection to the specified destination host on the specified
        /// port.  
        /// </remarks>
        public TcpClient CreateConnection(string destinationHost, int destinationPort)
        {
            if (string.IsNullOrEmpty(destinationHost))
                throw new ArgumentNullException(nameof(destinationHost));

            if (destinationPort <= 0 || destinationPort > 65535)
                throw new ArgumentOutOfRangeException(nameof(destinationPort), "port must be greater than zero and less than 65535");

            try
            {
                // if we have no cached tcpip connection then create one
                if (_tcpClientCached == null)
                {
                    if (string.IsNullOrEmpty(_proxyHost))
                        throw new ProxyException("ProxyHost property must contain a value.");

                    if (_proxyPort <= 0 || _proxyPort > 65535)
                        throw new ProxyException("ProxyPort value must be greater than zero and less than 65535");

                    //  create new tcp client object to the proxy server
                    _tcpClient = new TcpClient
                    {
                        SendTimeout = _sendTimeout,
                        ReceiveTimeout = _receiveTimeout
                    };

                    // attempt to open the connection
                    _tcpClient.Connect(_proxyHost, _proxyPort);
                }
                else
                {
                    _tcpClient = _tcpClientCached;
                }

                //  determine which authentication method the client would like to use
                DetermineClientAuthMethod();

                // negotiate which authentication methods are supported / accepted by the server
                NegotiateServerAuthMethod();

                // send a connect command to the proxy server for destination host and port
                SendCommand(SOCKS5_CMD_CONNECT, destinationHost, destinationPort);

                // remove the private reference to the tcp client so the proxy object does not keep it
                // return the open proxied tcp client object to the caller for normal use
                TcpClient rtn = _tcpClient;
                _tcpClient = null;
                return rtn;
            }
            catch (Exception ex)
            {
                throw new ProxyException(string.Format(CultureInfo.InvariantCulture, "Connection to proxy host {0} on port {1} failed.", Utils.GetHost(_tcpClient), Utils.GetPort(_tcpClient)), ex);
            }
        }


        private void DetermineClientAuthMethod()
        {
            //  set the authentication itemType used based on values inputed by the user
            if (_proxyUserName != null && _proxyPassword != null)
                _proxyAuthMethod = SocksAuthentication.UsernamePassword;
            else
                _proxyAuthMethod = SocksAuthentication.None;
        }

        private void NegotiateServerAuthMethod()
        {
            //  get a reference to the network stream
            NetworkStream stream = _tcpClient.GetStream();

            // SERVER AUTHENTICATION REQUEST
            // The client connects to the server, and sends a version
            // identifier/method selection message:
            //
            //      +----+----------+----------+
            //      |VER | NMETHODS | METHODS  |
            //      +----+----------+----------+
            //      | 1  |    1     | 1 to 255 |
            //      +----+----------+----------+

            byte[] authRequest = new byte[4];
            authRequest[0] = SOCKS5_VERSION_NUMBER;
            authRequest[1] = SOCKS5_AUTH_NUMBER_OF_AUTH_METHODS_SUPPORTED;
            authRequest[2] = SOCKS5_AUTH_METHOD_NO_AUTHENTICATION_REQUIRED;
            authRequest[3] = SOCKS5_AUTH_METHOD_USERNAME_PASSWORD;

            //  send the request to the server specifying authentication types supported by the client.
            stream.Write(authRequest, 0, authRequest.Length);

            //  SERVER AUTHENTICATION RESPONSE
            //  The server selects from one of the methods given in METHODS, and
            //  sends a METHOD selection message:
            //
            //     +----+--------+
            //     |VER | METHOD |
            //     +----+--------+
            //     | 1  |   1    |
            //     +----+--------+
            //
            //  If the selected METHOD is X'FF', none of the methods listed by the
            //  client are acceptable, and the client MUST close the connection.
            //
            //  The values currently defined for METHOD are:
            //   * X'00' NO AUTHENTICATION REQUIRED
            //   * X'01' GSSAPI
            //   * X'02' USERNAME/PASSWORD
            //   * X'03' to X'7F' IANA ASSIGNED
            //   * X'80' to X'FE' RESERVED FOR PRIVATE METHODS
            //   * X'FF' NO ACCEPTABLE METHODS

            //  receive the server response 
            byte[] response = new byte[2];
            stream.ReadExactly(response, 0, response.Length);

            //  the first byte contains the socks version number (e.g. 5)
            //  the second byte contains the auth method acceptable to the proxy server
            byte acceptedAuthMethod = response[1];

            // if the server does not accept any of our supported authenication methods then throw an error
            if (acceptedAuthMethod == SOCKS5_AUTH_METHOD_REPLY_NO_ACCEPTABLE_METHODS)
            {
                _tcpClient.Close();
                throw new ProxyException("The proxy destination does not accept the supported proxy client authentication methods.");
            }

            // 服务器选择了我们没有实现的认证方法（GSSAPI 或保留/IANA 私有范围），
            // 原实现会落到 username/password 子协商并向代理写错位的子协议，握手一定失败。
            // 显式拒绝以避免发送错误子协商的副作用。
            if (acceptedAuthMethod == SOCKS5_AUTH_METHOD_GSSAPI
                || (acceptedAuthMethod >= SOCKS5_AUTH_METHOD_IANA_ASSIGNED_RANGE_BEGIN && acceptedAuthMethod <= SOCKS5_AUTH_METHOD_IANA_ASSIGNED_RANGE_END)
                || (acceptedAuthMethod >= SOCKS5_AUTH_METHOD_RESERVED_RANGE_BEGIN && acceptedAuthMethod <= SOCKS5_AUTH_METHOD_RESERVED_RANGE_END))
            {
                _tcpClient.Close();
                throw new ProxyException(string.Format(CultureInfo.InvariantCulture,
                    "The SOCKS5 proxy selected authentication method 0x{0:X2} which is not supported by this client (only NO_AUTH and USERNAME/PASSWORD are implemented).",
                    acceptedAuthMethod));
            }

            // if the server accepts a username and password authentication and none is provided by the user then throw an error
            if (acceptedAuthMethod == SOCKS5_AUTH_METHOD_USERNAME_PASSWORD && _proxyAuthMethod == SocksAuthentication.None)
            {
                _tcpClient.Close();
                throw new ProxyException("The proxy destination requires a username and password for authentication.  If you received this error attempting to connect to the Tor network provide an string empty value for ProxyUserName and ProxyPassword.");
            }

            if (acceptedAuthMethod == SOCKS5_AUTH_METHOD_USERNAME_PASSWORD)
            {

                // USERNAME / PASSWORD SERVER REQUEST
                // Once the SOCKS V5 server has started, and the client has selected the
                // Username/Password Authentication protocol, the Username/Password
                // subnegotiation begins.  This begins with the client producing a
                // Username/Password request:
                //
                //       +----+------+----------+------+----------+
                //       |VER | ULEN |  UNAME   | PLEN |  PASSWD  |
                //       +----+------+----------+------+----------+
                //       | 1  |  1   | 1 to 255 |  1   | 1 to 255 |
                //       +----+------+----------+------+----------+

                // create a data structure (binary array) containing credentials
                // to send to the proxy server which consists of clear username and password data
                byte[] credentials = new byte[_proxyUserName.Length + _proxyPassword.Length + 3];

                // for SOCKS5 username/password authentication the VER field must be set to 0x01
                //  http://en.wikipedia.org/wiki/SOCKS
                //      field 1: version number, 1 byte (must be 0x01)"
                credentials[0] = 0x01;
                credentials[1] = (byte)_proxyUserName.Length;
                Array.Copy(Encoding.ASCII.GetBytes(_proxyUserName), 0, credentials, 2, _proxyUserName.Length);
                credentials[_proxyUserName.Length + 2] = (byte)_proxyPassword.Length;
                Array.Copy(Encoding.ASCII.GetBytes(_proxyPassword), 0, credentials, _proxyUserName.Length + 3, _proxyPassword.Length);

                // USERNAME / PASSWORD SERVER RESPONSE
                // The server verifies the supplied UNAME and PASSWD, and sends the
                // following response:
                //
                //   +----+--------+
                //   |VER | STATUS |
                //   +----+--------+
                //   | 1  |   1    |
                //   +----+--------+
                //
                // A STATUS field of X'00' indicates success. If the server returns a
                // `failure' (STATUS value other than X'00') status, it MUST close the
                // connection.

                // transmit credentials to the proxy server
                stream.Write(credentials, 0, credentials.Length);

                // read the response from the proxy server
                byte[] crResponse = new byte[2];

                // byte[] crResponse = new byte[2];
                stream.ReadExactly(crResponse, 0, 2);

                // check to see if the proxy server accepted the credentials
                if (crResponse[1] != 0)
                {
                    _tcpClient.Close();
                    throw new ProxyException("Proxy authentification failure!  The proxy server has reported that the userid and/or password is not valid.");
                }

            }
        }

        private byte GetDestAddressType(string host)
        {
            bool result = IPAddress.TryParse(host, out IPAddress ipAddr);

            if (!result)
                return SOCKS5_ADDRTYPE_DOMAIN_NAME;

            return ipAddr.AddressFamily switch
            {
                AddressFamily.InterNetwork => SOCKS5_ADDRTYPE_IPV4,
                AddressFamily.InterNetworkV6 => SOCKS5_ADDRTYPE_IPV6,
                _ => throw new ProxyException(string.Format(CultureInfo.InvariantCulture, "The host addess {0} of type '{1}' is not a supported address type.  The supported types are InterNetwork and InterNetworkV6.", host, Enum.GetName(typeof(AddressFamily), ipAddr.AddressFamily))),
            };
        }

        private byte[] GetDestAddressBytes(byte addressType, string host)
        {
            switch (addressType)
            {
                case SOCKS5_ADDRTYPE_IPV4:
                case SOCKS5_ADDRTYPE_IPV6:
                    return IPAddress.Parse(host).GetAddressBytes();
                case SOCKS5_ADDRTYPE_DOMAIN_NAME:
                    //  create a byte array to hold the host name bytes plus one byte to store the length
                    byte[] bytes = new byte[host.Length + 1];
                    //  if the address field contains a fully-qualified domain name.  The first
                    //  octet of the address field contains the number of octets of name that
                    //  follow, there is no terminating NUL octet.
                    bytes[0] = Convert.ToByte(host.Length);
                    Encoding.ASCII.GetBytes(host).CopyTo(bytes, 1);
                    return bytes;
                default:
                    return null;
            }
        }

        private byte[] GetDestPortBytes(int value)
        {
            byte[] array = [Convert.ToByte(value / 256), Convert.ToByte(value % 256)];
            return array;
        }

        private void SendCommand(byte command, string destinationHost, int destinationPort)
        {
            NetworkStream stream = _tcpClient.GetStream();

            byte addressType = GetDestAddressType(destinationHost);
            byte[] destAddr = GetDestAddressBytes(addressType, destinationHost);
            byte[] destPort = GetDestPortBytes(destinationPort);

            //  The connection request is made up of 6 bytes plus the
            //  length of the variable address byte array
            //
            //  +----+-----+-------+------+----------+----------+
            //  |VER | CMD |  RSV  | ATYP | DST.ADDR | DST.PORT |
            //  +----+-----+-------+------+----------+----------+
            //  | 1  |  1  | X'00' |  1   | Variable |    2     |
            //  +----+-----+-------+------+----------+----------+
            //
            // * VER protocol version: X'05'
            // * CMD
            //   * CONNECT X'01'
            //   * BIND X'02'
            //   * UDP ASSOCIATE X'03'
            // * RSV RESERVED
            // * ATYP address itemType of following address
            //   * IP V4 address: X'01'
            //   * DOMAINNAME: X'03'
            //   * IP V6 address: X'04'
            // * DST.ADDR desired destination address
            // * DST.PORT desired destination port in network octet order            

            byte[] request = new byte[4 + destAddr.Length + 2];
            request[0] = SOCKS5_VERSION_NUMBER;
            request[1] = command;
            request[2] = SOCKS5_RESERVED;
            request[3] = addressType;
            destAddr.CopyTo(request, 4);
            destPort.CopyTo(request, 4 + destAddr.Length);

            // send connect request.
            stream.Write(request, 0, request.Length);
            //  PROXY SERVER RESPONSE
            //  +----+-----+-------+------+----------+----------+
            //  |VER | REP |  RSV  | ATYP | BND.ADDR | BND.PORT |
            //  +----+-----+-------+------+----------+----------+
            //  | 1  |  1  | X'00' |  1   | Variable |    2     |
            //  +----+-----+-------+------+----------+----------+
            //
            // * VER protocol version: X'05'
            // * REP Reply field:
            //   * X'00' succeeded
            //   * X'01' general SOCKS server failure
            //   * X'02' connection not allowed by ruleset
            //   * X'03' Network unreachable
            //   * X'04' Host unreachable
            //   * X'05' Connection refused
            //   * X'06' TTL expired
            //   * X'07' Command not supported
            //   * X'08' Address itemType not supported
            //   * X'09' to X'FF' unassigned
            // RSV RESERVED
            // ATYP address itemType of following address

            byte[] response = new byte[255];

            // 按 SOCKS5 协议至少包含 10 字节（VER + REP + RSV + ATYP + 4 字节 IPv4 + 2 字节端口），
            // 域名或 IPv6 响应会更长。先读固定 4 字节头部，再依据 ATYP 读取剩余部分，
            // 避免使用 Read 时只读到部分字节就返回，导致后续解析读到未初始化的零字节。
            int read = stream.ReadAtLeast(response, 4, throwOnEndOfStream: true);
            if (read < 4)
                throw new ProxyException("SOCKS5 proxy response is too short.");

            byte replyCode = response[1];
            if (replyCode != SOCKS5_CMD_REPLY_SUCCEEDED)
            {
                HandleProxyCommandError(response, destinationHost, destinationPort);
                return;
            }

            // 读取 BND.ADDR 与 BND.PORT，按地址族长度补齐。
            byte addrType = response[3];
            int addrLen = addrType switch
            {
                SOCKS5_ADDRTYPE_IPV4 => 4,
                SOCKS5_ADDRTYPE_IPV6 => 16,
                SOCKS5_ADDRTYPE_DOMAIN_NAME => response[4],
                _ => throw new ProxyException(string.Format(CultureInfo.InvariantCulture,
                    "SOCKS5 proxy returned an unsupported address type {0}.", addrType))
            };
            int totalLen = 4 + addrLen + 2;
            if (read < totalLen)
                stream.ReadExactly(response, read, totalLen - read);
        }
        private void HandleProxyCommandError(byte[] response, string destinationHost, int destinationPort)
        {
            byte replyCode = response[1];
            byte addrType = response[3];
            string addr;
            short port;
            switch (addrType)
            {
                case SOCKS5_ADDRTYPE_DOMAIN_NAME:
                    int addrLen = Convert.ToInt32(response[4]);
                    byte[] addrBytes = new byte[addrLen];
                    for (int i = 0; i < addrLen; i++)
                        addrBytes[i] = response[i + 5];
                    addr = Encoding.ASCII.GetString(addrBytes);
                    byte[] portBytesDomain = [response[6 + addrLen], response[5 + addrLen]];
                    port = BitConverter.ToInt16(portBytesDomain, 0);
                    break;

                case SOCKS5_ADDRTYPE_IPV4:
                    byte[] ipv4Bytes = new byte[4];
                    for (int i = 0; i < 4; i++)
                        ipv4Bytes[i] = response[i + 4];
                    IPAddress ipv4 = new(ipv4Bytes);
                    addr = ipv4.ToString();
                    byte[] portBytesIpv4 = [response[9], response[8]];
                    port = BitConverter.ToInt16(portBytesIpv4, 0);
                    break;

                case SOCKS5_ADDRTYPE_IPV6:
                    byte[] ipv6Bytes = new byte[16];
                    for (int i = 0; i < 16; i++)
                        ipv6Bytes[i] = response[i + 4];
                    IPAddress ipv6 = new(ipv6Bytes);
                    addr = ipv6.ToString();
                    byte[] portBytesIpv6 = [response[21], response[20]];
                    port = BitConverter.ToInt16(portBytesIpv6, 0);
                    break;
            }

            string proxyErrorText = replyCode switch
            {
                SOCKS5_CMD_REPLY_GENERAL_SOCKS_SERVER_FAILURE => "a general socks destination failure occurred",
                SOCKS5_CMD_REPLY_CONNECTION_NOT_ALLOWED_BY_RULESET => "the connection is not allowed by proxy destination rule set",
                SOCKS5_CMD_REPLY_NETWORK_UNREACHABLE => "the network was unreachable",
                SOCKS5_CMD_REPLY_HOST_UNREACHABLE => "the host was unreachable",
                SOCKS5_CMD_REPLY_CONNECTION_REFUSED => "the connection was refused by the remote network",
                SOCKS5_CMD_REPLY_TTL_EXPIRED => "the time to live (TTL) has expired",
                SOCKS5_CMD_REPLY_COMMAND_NOT_SUPPORTED => "the command issued by the proxy client is not supported by the proxy destination",
                SOCKS5_CMD_REPLY_ADDRESS_TYPE_NOT_SUPPORTED => "the address type specified is not supported",
                _ => string.Format(CultureInfo.InvariantCulture, "an unknown SOCKS reply with the code value '{0}' was received", replyCode.ToString(CultureInfo.InvariantCulture)),
            };
            string responseText = response != null ? ArrayUtils.HexEncode(response) : "";
            string exceptionMsg = string.Format(CultureInfo.InvariantCulture, "proxy error: {0} for destination host {1} port number {2}.  Server response (hex): {3}.", proxyErrorText, destinationHost, destinationPort, responseText);

            throw new ProxyException(exceptionMsg);

        }


        #region "Async Methods"

        private BackgroundWorker _asyncWorker;
        private Exception _asyncException;
        bool _asyncCancelled;
        private int _sendTimeout = 5000;
        private int _receiveTimeout = 5000;

        /// <summary>
        /// Gets a value indicating whether an asynchronous operation is running.
        /// 获取当前是否有异步连接操作正在执行。
        /// </summary>
        /// <remarks>Returns true if an asynchronous operation is running; otherwise, false.
        /// </remarks>
        public bool IsBusy
        {
            get { return _asyncWorker != null && _asyncWorker.IsBusy; }
        }

        /// <summary>
        /// Gets a value indicating whether an asynchronous operation is cancelled.
        /// 获取当前异步连接操作是否已请求取消。
        /// </summary>
        /// <remarks>Returns true if an asynchronous operation is cancelled; otherwise, false.
        /// </remarks>
        public bool IsAsyncCancelled
        {
            get { return _asyncCancelled; }
        }

        /// <summary>
        /// Cancels any asychronous operation that is currently active.
        /// 请求取消当前正在执行的异步连接操作。
        /// </summary>
        /// <remarks>
        /// BackgroundWorker 本身无法打断同步阻塞的网络调用。本实现除了设置取消标志外，
        /// 还会主动关闭正在协商的 <see cref="TcpClient"/>，让阻塞在 <c>ReadExactly</c> 上的
        /// 工作线程抛出 <see cref="IOException"/>，并被 <see cref="CreateConnectionAsync_DoWork"/>
        /// 捕获后以取消状态上报到完成事件。
        /// </remarks>
        public void CancelAsync()
        {
            if (_asyncWorker != null && _asyncWorker.IsBusy)
            {
                _asyncCancelled = true;
                _asyncWorker.CancelAsync();
                // 主动关闭底层 socket，使阻塞中的网络读取/连接立即抛异常。
                try { _tcpClient?.Close(); }
                catch { /* ignore */ }
            }
        }

        private void CreateAsyncWorker()
        {
            _asyncWorker?.Dispose();
            _asyncException = null;
            _asyncWorker = null;
            _asyncCancelled = false;
            _asyncWorker = new BackgroundWorker();
        }

        /// <summary>
        /// Event handler for CreateConnectionAsync method completed.
        /// 异步连接操作完成时触发的事件。
        /// </summary>
        public event EventHandler<CreateConnectionAsyncCompletedEventArgs> CreateConnectionAsyncCompleted;


        /// <summary>
        /// Asynchronously creates a remote TCP connection through a proxy server to the destination host on the destination port.
        /// 异步完成 SOCKS5 认证协商并建立到目标地址的 CONNECT 隧道连接。
        /// </summary>
        /// <param name="destinationHost">Destination host name or IP address.</param>
        /// <param name="destinationPort">Port number to connect to on the destination host.</param>
        /// <returns>
        /// Returns TcpClient object that can be used normally to communicate
        /// with the destination server.
        /// </returns>
        /// <exception cref="InvalidOperationException">当已有异步代理连接操作正在运行时抛出。</exception>
        /// <remarks>
        /// 该方法本身无返回值；实际连接结果通过 <see cref="CreateConnectionAsyncCompleted"/> 事件返回。
        /// This method instructs the proxy server
        /// to make a pass through connection to the specified destination host on the specified
        /// port.
        /// </remarks>
        public void CreateConnectionAsync(string destinationHost, int destinationPort)
        {
            if (_asyncWorker != null && _asyncWorker.IsBusy)
                throw new InvalidOperationException("The Socks4 object is already busy executing another asynchronous operation.  You can only execute one asychronous method at a time.");

            CreateAsyncWorker();
            _asyncWorker.WorkerSupportsCancellation = true;
            _asyncWorker.DoWork += new DoWorkEventHandler(CreateConnectionAsync_DoWork);
            _asyncWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(CreateConnectionAsync_RunWorkerCompleted);
            object[] args = new object[2];
            args[0] = destinationHost;
            args[1] = destinationPort;
            _asyncWorker.RunWorkerAsync(args);
        }

        private void CreateConnectionAsync_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                object[] args = (object[])e.Argument;
                e.Result = CreateConnection((string)args[0], (int)args[1]);
            }
            catch (Exception ex)
            {
                // 捕获工作线程异常，包括被取消时关闭 socket 引发的 IOException/ObjectDisposedException。
                _asyncException = ex;
                if (_asyncCancelled)
                    e.Cancel = true;
            }
        }

        private void CreateConnectionAsync_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            // 取消路径下 Result 为 null，事件参数正确反映 cancelled/error 状态。
            TcpClient result = null;
            if (!e.Cancelled && e.Error == null)
                result = e.Result as TcpClient;
            CreateConnectionAsyncCompleted?.Invoke(this, new CreateConnectionAsyncCompletedEventArgs(_asyncException, _asyncCancelled, result));
        }



        #endregion
    }
}
