using System;
using System.Collections.Generic;
using System.Text;

namespace Framework.SlideCaptcha;

public interface IStorage
{
	void Set<T>(string key, T value, DateTimeOffset absoluteExpiration);

	T Get<T>(string key);

	void Remove(string key);
}
