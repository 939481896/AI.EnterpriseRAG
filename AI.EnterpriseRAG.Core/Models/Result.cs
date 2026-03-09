using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AI.EnterpriseRAG.Core.Models
{
    /// <summary>
    /// 统一返回结果（企业级规范）
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Result<T>
    {
        public int Code { get; set; }
        public string Message { get; set; }=string.Empty;
        public T? Data { get; set; }
        public bool Success => Code == 200;

        public static Result<T> SuccessResult(T? data, string message="操作成功")
        {
            return new Result<T> { Code = 200, Data=data, Message = message };
        }

        public static Result<T> FailResult(string message, int code = 400)
        {
            return new Result<T>
            {
                Code = code,
                Message = message,
                Data = default
            };
        }

    }

    /// <summary>
    /// 无数据返回结果
    /// </summary>
    public class Result : Result<object>
    {
        public static Result Success(string message = "操作成功")
        {
            return new Result
            {
                Code = 200,
                Message = message
            };
        }

        public new static Result Fail(string message, int code = 400)
        {
            return new Result
            {
                Code = code,
                Message = message
            };
        }
    }
}
