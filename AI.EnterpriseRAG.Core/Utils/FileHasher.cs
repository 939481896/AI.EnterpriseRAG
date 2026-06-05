using System.Security.Cryptography;
using System.Text;

namespace AI.EnterpriseRAG.Core.Utils;

/// <summary>
/// 文件哈希工具类
/// 用途：计算文件指纹，用于重复检测
/// </summary>
public static class FileHasher
{
    /// <summary>
    /// 计算文件的MD5哈希（异步）
    /// </summary>
    /// <param name="stream">文件流</param>
    /// <param name="resetPosition">是否重置流位置（默认true）</param>
    /// <returns>32位小写MD5字符串</returns>
    public static async Task<string> ComputeMD5Async(Stream stream, bool resetPosition = true)
    {
        if (stream == null)
            throw new ArgumentNullException(nameof(stream));
        
        var originalPosition = stream.Position;
        
        try
        {
            stream.Position = 0;
            
            using var md5 = MD5.Create();
            var hashBytes = await md5.ComputeHashAsync(stream);
            
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
        }
        finally
        {
            if (resetPosition && stream.CanSeek)
            {
                stream.Position = originalPosition;
            }
        }
    }
    
    /// <summary>
    /// 计算文件的SHA256哈希（更安全但慢）
    /// </summary>
    public static async Task<string> ComputeSHA256Async(Stream stream, bool resetPosition = true)
    {
        if (stream == null)
            throw new ArgumentNullException(nameof(stream));
        
        var originalPosition = stream.Position;
        
        try
        {
            stream.Position = 0;
            
            using var sha256 = SHA256.Create();
            var hashBytes = await sha256.ComputeHashAsync(stream);
            
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
        }
        finally
        {
            if (resetPosition && stream.CanSeek)
            {
                stream.Position = originalPosition;
            }
        }
    }
    
    /// <summary>
    /// 计算字符串的MD5哈希
    /// </summary>
    public static string ComputeMD5(string input)
    {
        if (string.IsNullOrEmpty(input))
            throw new ArgumentNullException(nameof(input));
        
        using var md5 = MD5.Create();
        var inputBytes = Encoding.UTF8.GetBytes(input);
        var hashBytes = md5.ComputeHash(inputBytes);
        
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
    }
    
    /// <summary>
    /// 快速计算文件哈希（只读取前1MB）
    /// 适用场景：大文件预检（性能优先）
    /// </summary>
    public static async Task<string> ComputeQuickHashAsync(Stream stream, bool resetPosition = true)
    {
        if (stream == null)
            throw new ArgumentNullException(nameof(stream));
        
        var originalPosition = stream.Position;
        
        try
        {
            stream.Position = 0;
            
            // 只读取前1MB
            var bufferSize = Math.Min(1024 * 1024, (int)stream.Length);
            var buffer = new byte[bufferSize];
            
            await stream.ReadAsync(buffer, 0, bufferSize);
            
            using var md5 = MD5.Create();
            var hashBytes = md5.ComputeHash(buffer);
            
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
        }
        finally
        {
            if (resetPosition && stream.CanSeek)
            {
                stream.Position = originalPosition;
            }
        }
    }
    
    /// <summary>
    /// 验证文件哈希
    /// </summary>
    public static async Task<bool> VerifyHashAsync(Stream stream, string expectedHash, HashAlgorithmType algorithm = HashAlgorithmType.MD5)
    {
        var actualHash = algorithm switch
        {
            HashAlgorithmType.MD5 => await ComputeMD5Async(stream),
            HashAlgorithmType.SHA256 => await ComputeSHA256Async(stream),
            _ => throw new ArgumentException($"不支持的哈希算法：{algorithm}")
        };
        
        return string.Equals(actualHash, expectedHash, StringComparison.OrdinalIgnoreCase);
    }
}

/// <summary>
/// 哈希算法类型
/// </summary>
public enum HashAlgorithmType
{
    MD5,
    SHA256
}
