using System;
using System.Collections.Generic;
using System.Text;

namespace Framework.SlideCaptcha.Exceptions
{
    /// <summary>
    /// 滑块验证码模块的基础异常类型，封装所有业务级别的失败原因。
    /// </summary>
    public class SlideCaptchaException : Exception
    {
        /// <summary>
        /// 初始化 <see cref="SlideCaptchaException"/> 的新实例，使用默认错误信息。
        /// </summary>
        public SlideCaptchaException() : base()
        {
        }

        /// <summary>
        /// 使用指定的错误消息初始化 <see cref="SlideCaptchaException"/> 的新实例。
        /// </summary>
        /// <param name="message">描述异常原因的错误消息。</param>
        public SlideCaptchaException(string message) : base(message)
        {
        }

        /// <summary>
        /// 使用指定的错误消息和内部异常初始化 <see cref="SlideCaptchaException"/> 的新实例。
        /// </summary>
        /// <param name="message">描述异常原因的错误消息。</param>
        /// <param name="innerException">导致当前异常的内部异常，可用于保留底层堆栈信息。</param>
        public SlideCaptchaException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}