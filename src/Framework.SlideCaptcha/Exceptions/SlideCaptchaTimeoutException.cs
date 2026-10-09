using System;
using System.Collections.Generic;
using System.Text;

namespace Framework.SlideCaptcha.Exceptions
{
    /// <summary>
    /// 滑块验证码校验超时时抛出的异常，表示服务端存储的验证码数据已过期或缺失。
    /// </summary>
    public class SlideCaptchaTimeoutException : Exception
    {
        /// <summary>
        /// 初始化 <see cref="SlideCaptchaTimeoutException"/> 的新实例，使用默认错误信息。
        /// </summary>
        public SlideCaptchaTimeoutException() : base()
        {
        }

        /// <summary>
        /// 使用指定的错误消息初始化 <see cref="SlideCaptchaTimeoutException"/> 的新实例。
        /// </summary>
        /// <param name="message">描述异常原因的错误消息。</param>
        public SlideCaptchaTimeoutException(string message) : base(message)
        {
        }

        /// <summary>
        /// 使用指定的错误消息和内部异常初始化 <see cref="SlideCaptchaTimeoutException"/> 的新实例。
        /// </summary>
        /// <param name="message">描述异常原因的错误消息。</param>
        /// <param name="innerException">导致当前异常的内部异常，可用于保留底层堆栈信息。</param>
        public SlideCaptchaTimeoutException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}