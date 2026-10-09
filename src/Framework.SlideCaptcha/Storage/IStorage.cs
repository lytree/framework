using System;
using System.Collections.Generic;
using System.Text;

namespace Framework.SlideCaptcha.Storage
{
	/// <summary>
	/// 验证码数据存储契约：在生成阶段写入 <see cref="CaptchaValidateData"/>，在验证阶段读取并消费。
	/// 实现可基于内存、Redis 等任何能给出「键-值 + 过期时间」语义的存储。
	/// </summary>
	public interface IStorage
	{
		/// <summary>
		/// 把任意对象写入存储，并设置绝对过期时间。
		/// </summary>
		/// <typeparam name="T">值的类型。</typeparam>
		/// <param name="key">存储键（<paramref name="captchaId"/>）。</param>
		/// <param name="value">待写入的对象。</param>
		/// <param name="absoluteExpiration">UTC 绝对过期时间，到点后实现应自动淘汰该键。</param>
		void Set<T>(string key, T value, DateTimeOffset absoluteExpiration);

		/// <summary>
		/// 读取并反序列化指定键对应的值。
		/// </summary>
		/// <typeparam name="T">目标类型。</typeparam>
		/// <param name="key">存储键（<paramref name="captchaId"/>）。</param>
		/// <returns>键对应的值；若不存在或已过期则返回 <c>default(T)</c>（引用类型为 <c>null</c>）。</returns>
		T Get<T>(string key);

		/// <summary>
		/// 从存储中删除指定键。无论键是否存在，实现都应幂等地完成。
		/// </summary>
		/// <param name="key">存储键（<paramref name="captchaId"/>）。</param>
		void Remove(string key);
	}
}