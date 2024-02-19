using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Framework.AspNetCore.Mvc;

[AttributeUsage(AttributeTargets.Field)]
public class ApiResultAttribute : Attribute
{
    public string Code { get; set; }

    public string Message { get; set; }

    /// <summary>
    /// Gets the description stored in this attribute.
    /// </summary>


    public ApiResultAttribute(string Code, string Message)
    {
        this.Code = Code;
        this.Message = Message;
    }
}


public enum ApiResult
{
    [ApiResult("0000","请求成功")]
    Success
}
