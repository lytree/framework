using Framework.Helper;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Framework.SlideCaptcha;

public class DefaultStorage : IStorage
{
	private readonly IDistributedCache _cache;
	private readonly IOptionsMonitor<CaptchaOptions> _options;

	public DefaultStorage(IOptionsMonitor<CaptchaOptions> options, IDistributedCache cache)
	{
		_options = options;
		_cache = cache;
	}

	private string WrapKey(string key)
	{
		return $"{_options.CurrentValue.StoreageKeyPrefix}{key}";
	}

	public T Get<T>(string key)
	{
		var bytes = _cache.Get(WrapKey(key));
		if (bytes == null) return default;
		var json = Encoding.UTF8.GetString(bytes, 0, bytes.Length);
		return JsonHelper.Deserialize<T>(json);
	}

	public void Remove(string key)
	{
		_cache.Remove(WrapKey(key));
	}

	public void Set<T>(string key, T value, DateTimeOffset absoluteExpiration)
	{
		string json = JsonHelper.Serialize(value);
		byte[] bytes = Encoding.UTF8.GetBytes(json);

		_cache.Set(WrapKey(key), bytes, new DistributedCacheEntryOptions
		{
			AbsoluteExpiration = absoluteExpiration
		});
	}
}
