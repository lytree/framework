using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Framework;


/// <summary>
/// 文件系统相关的辅助方法集，依赖 <see cref="System.IO.Path"/>、<see cref="File"/>、<see cref="Directory"/> 等标准 I/O 设施，
/// 提供临时文件创建与清理等常见运维操作，跟踪集合基于进程级 <see cref="ConcurrentDictionary{TKey, TValue}"/> 实现。
/// </summary>
public static partial class Helper
{

    // 使用 ConcurrentDictionary<string, byte> 而不是 ConcurrentBag<FileInfo>：
    // 1) 可以在 ClearTempFiles 中按路径精确移除已删除项，避免长跑进程下内存无限增长；
    // 2) 键去重，避免同路径被多次添加造成的重复 IO。
    private static readonly ConcurrentDictionary<string, byte> tempFiles = new(StringComparer.Ordinal);

    /// <summary>
    /// 在指定目录下创建一个临时文件（若同名文件已存在则循环重试），并把文件路径记录到进程级跟踪集合中。
    /// 创建后立即关闭底层流，文件保持 0 字节状态，可由调用方后续写入。
    /// </summary>
    /// <param name="baseFolder">目标目录。若不存在将自动创建。</param>
    /// <param name="desiredFileExtension">期望的文件扩展名（例如 <c>".csv"</c>）。为空或仅空白时不修改扩展名，使用 <see cref="Path.GetTempFileName"/> 自带的 <c>.tmp</c>。</param>
    /// <returns>已创建文件的 <see cref="FileInfo"/>。</returns>
    public static FileInfo CreateTempFile(string baseFolder, string? desiredFileExtension)
    {
        string tempFilePath;

        do
        {
            // 使用 Path.GetTempFileName 生成唯一的临时文件名，再拼接到 baseFolder
            tempFilePath = Path.Combine(baseFolder, Path.GetFileName(Path.GetTempFileName()));
            if (!string.IsNullOrWhiteSpace(desiredFileExtension))
            {
                tempFilePath = Path.ChangeExtension(tempFilePath, desiredFileExtension);
            }
        } while (File.Exists(tempFilePath));

        Directory.CreateDirectory(baseFolder);
        // 立即创建并释放流，使文件存在但保持空
        File.Create(tempFilePath).Dispose();
        var fileInfo = new FileInfo(tempFilePath);
        tempFiles.TryAdd(fileInfo.FullName, 0);
        return fileInfo;
    }

    /// <summary>
    /// 清理临时文件：
    /// 1) 删除由本进程 <see cref="CreateTempFile"/> 跟踪的所有文件；
    /// 2) 扫描 <paramref name="baseFolder"/> 下未被跟踪的文件，若其最后写入时间距今超过 2 分钟（UTC）则一并删除。
    /// 任何删除异常均被静默吞掉。
    /// </summary>
    /// <param name="baseFolder">要扫描清理的根目录。若不存在则跳过第二阶段的扫描。</param>
    /// <remarks>
    /// 跟踪集合使用 <see cref="ConcurrentDictionary{TKey, TValue}"/>，已删除的文件路径会从字典中同步移除，
    /// 避免长跑进程中字典无限增长。
    /// 未跟踪文件的"过期"判定改用 <see cref="FileSystemInfo.LastWriteTimeUtc"/> 而非
    /// <see cref="FileSystemInfo.CreationTimeUtc"/>，避免在文件被复制/移动后因创建时间过早而被误删。
    /// </remarks>
    public static void ClearTempFiles(string baseFolder)
    {
        // 阶段 1：删除被本进程跟踪的临时文件，每删一个就同步从字典移除。
        foreach (var tracked in tempFiles.Keys.ToArray())
        {
            try
            {
                if (File.Exists(tracked))
                    File.Delete(tracked);
            }
            catch
            {
                // Swallow.
            }
            finally
            {
                tempFiles.TryRemove(tracked, out _);
            }
        }

        if (!Directory.Exists(baseFolder))
            return;

        // 阶段 2：扫描目录下"未跟踪"的文件，按最后写入时间判定是否清理。
        foreach (string tempFilePath in Directory.GetFiles(baseFolder, string.Empty, SearchOption.AllDirectories))
        {
            // 已被本进程跟踪的文件由阶段 1 处理，此处跳过。
            if (tempFiles.ContainsKey(tempFilePath))
                continue;

            var tempFileInfo = new FileInfo(tempFilePath);
            // 改用 LastWriteTimeUtc，避免在文件被复制/移动时 CreationTimeUtc 早于实际写入时间而误删。
            if (DateTime.UtcNow - tempFileInfo.LastWriteTimeUtc > TimeSpan.FromMinutes(2))
            {
                try
                {
                    File.Delete(tempFilePath);
                }
                catch
                {
                    // Swallow.
                }
            }
        }
    }
}
