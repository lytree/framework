using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Framework.AspNetCore.Mvc;

public class ApiException : Exception
{

    public string ApiMessage { get; set; }
    public string ApiCode { get; set; }
    public int StatusCode { get; set; } = (int)HttpStatusCode.OK;

    public ApiException()
    {
    }


    public ApiException(SerializationInfo serializationInfo, StreamingContext context)
		: base(serializationInfo, context)
    {
    }

    public ApiException(string message)
        : base(message)
    {
        ApiMessage = message;
    }

    public ApiException(string message, string code)
        : base(message)
    {
        ApiMessage = message;
        ApiCode = code;
    }

    public ApiException(string message, string code, int statusCode)
        : base(message)
    {
        ApiMessage = message;
        ApiCode = code;
        StatusCode = statusCode;
    }


    public ApiException(string message, Exception innerException)
        : base(message, innerException)
    {
        ApiMessage = message;
    }

    public ApiException(string message, string code, Exception innerException)
        : base(message, innerException)
    {
        ApiMessage = message;
        ApiCode = code;
    }

    public ApiException(string message, string code, int statusCode, Exception innerException)
        : base(message, innerException)
    {
        ApiMessage = message;
        ApiCode = code;
        StatusCode = statusCode;
    }
}
