using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Framework.SlideCaptcha.Storage
{
	/// <summary>
	/// 基于 <see cref="IDistributedCache"/> 的 <see cref="IStorage"/> 默认实现：
	/// 在写入时把对象 <see cref="JsonSerializer.Serialize"/> 为 UTF-8 字节，读取时反序列化；
	/// 同时使用 <see cref="CaptchaOptions.StorageKeyPrefix"/> 为所有 key 加上前缀，避免与其他业务缓存键冲突。
	/// </summary>
	public class DefaultStorage : IStorage
	{
		private readonly IDistributedCache _cache;
		private readonly IOptionsMonitor<CaptchaOptions> _options;

		/// <summary>
		/// 初始化 <see cref="DefaultStorage"/>。
		/// </summary>
		/// <param name="options">用于读取 <see cref="CaptchaOptions"/> 中 <c>StorageKeyPrefix</c> 的 options 监控器。</param>
		/// <param name="cache">底层分布式缓存（如 Redis、Memory）。</param>
		public DefaultStorage(IOptionsMonitor<CaptchaOptions> options, IDistributedCache cache)
		{
			_options = options;
			_cache = cache;
		}

		/// <summary>
		/// 为给定 <paramref name="key"/> 拼接 <see cref="CaptchaOptions.StorageKeyPrefix"/> 形成最终在分布式缓存中使用的键。
		/// 每次调用都重新读取当前配置，配置中前缀改动可即时生效。
		/// </summary>
		/// <param name="key">业务侧传入的原始键。</param>
		/// <returns>拼接前缀后的缓存键。</returns>
		private string WrapKey(string key)
		{
#pragma warning disable CS0618 // 旧拼写 StoreageKeyPrefix 仍可能被业务配置使用，保留 fallback
			string prefix = _options.CurrentValue.StorageKeyPrefix
#pragma warning restore CS0618
#pragma warning disable CS0618
							?? _options.CurrentValue.StoreageKeyPrefix
#pragma warning restore CS0618
							?? string.Empty;
			return prefix + key;
		}

		/// <summary>
		/// 从分布式缓存中读取并反序列化为指定类型。
		/// </summary>
		/// <typeparam name="T">目标类型，必须能被 <see cref="JsonSerializer"/> 反序列化。</typeparam>
		/// <param name="key">业务侧键（无需包含前缀），内部会拼接 <see cref="CaptchaOptions.StoreageKeyPrefix"/>。</param>
		/// <returns>反序列化后的对象；若缓存中不存在则返回 <c>default(T)</c>（引用类型为 <c>null</c>）。</returns>
		public T Get<T>(string key)
		{
			var bytes = _cache.Get(WrapKey(key));
			if (bytes == null) return default;
			var json = Encoding.UTF8.GetString(bytes, 0, bytes.Length);
			return JsonSerializer.Deserialize<T>(json);
		}

		/// <summary>
		/// 从分布式缓存中删除指定键。
		/// </summary>
		/// <param name="key">业务侧键（无需包含前缀）。</param>
		public void Remove(string key)
		{
			_cache.Remove(WrapKey(key));
		}

		/// <summary>
		/// 把 <paramref name="value"/> 序列化为 JSON 后写入分布式缓存，并设置绝对过期时间。
		/// </summary>
		/// <typeparam name="T">值的类型。</typeparam>
		/// <param name="key">业务侧键（无需包含前缀）。</param>
		/// <param name="value">待写入的对象。</param>
		/// <param name="absoluteExpiration">UTC 绝对过期时间，到点后缓存自动淘汰。</param>
		public void Set<T>(string key, T value, DateTimeOffset absoluteExpiration)
		{
			string json = JsonSerializer.Serialize(value);
			byte[] bytes = Encoding.UTF8.GetBytes(json);

			_cache.Set(WrapKey(key), bytes, new DistributedCacheEntryOptions
			{
				AbsoluteExpiration = absoluteExpiration
			});
		}
	}
}