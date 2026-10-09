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
/// 提供临时文件创建与清理等常见运维操作，跟踪集合基于进程级 <see cref="ConcurrentBag{T}"/> 实现。
/// </summary>
public static partial class Helper
{

    private static readonly ConcurrentBag<FileInfo> tempFiles = [];

    /// <summary>
    /// 在指定目录下创建一个临时文件（若同名文件已存在则循环重试），并把 <see cref="FileInfo"/> 记录到进程级跟踪集合中。
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
        tempFiles.Add(fileInfo);
        return fileInfo;
    }

    /// <summary>
    /// 清理临时文件：
    /// 1) 删除由本进程 <see cref="CreateTempFile"/> 跟踪的所有文件；
    /// 2) 扫描 <paramref name="baseFolder"/> 下未被跟踪的文件，若其创建时间距今超过 2 分钟（UTC）则一并删除。
    /// 任何删除异常均被静默吞掉。
    /// </summary>
    /// <param name="baseFolder">要扫描清理的根目录。若不存在则跳过第二阶段的扫描。</param>
    public static void ClearTempFiles(string baseFolder)
    {
        FileInfo[] files = [.. tempFiles];
        foreach (FileInfo tempFile in files)
        {
            try
            {
                if (File.Exists(tempFile.FullName))
                {
                    File.Delete(tempFile.FullName);
                }
            }
            catch
            {
                // Swallow.
            }
        }

        if (Directory.Exists(baseFolder))
        {
            foreach (string tempFilePath in Directory.GetFiles(baseFolder, string.Empty, SearchOption.AllDirectories))
            {
                // We found a file that isn't tracked by the current instance of the app.
                // If the file is older than 2 minutes, let's destroy it.

                var tempFileInfo = new FileInfo(tempFilePath);
                if (DateTime.UtcNow - tempFileInfo.CreationTimeUtc > TimeSpan.FromMinutes(2))
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
}
