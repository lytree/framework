using System;
using System.Runtime.CompilerServices;

namespace Framework.AspNetCore.AspNetCore.Mvc;


public interface IApiResponse<T> : IApiResponse
{
	T Data { get; set; }
}

public interface IApiResponse
{
	int StatusCode { get; set; }
	bool Success { get; set; }
	string Code { get; set; }
	string Message { get; set; }
}
