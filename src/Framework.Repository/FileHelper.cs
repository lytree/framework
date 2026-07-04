using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Framework.Repository;

public static class FileHelper
{
    #region 写文件

    /// <summary>
    /// 写文件
    /// </summary>
    /// <param name="path">文件路径</param>
    /// <param name="strings">文件内容</param>
    public static void WriteFile(string path, string strings)
    {
        if (!File.Exists(path))
        {
            File.Create(path).Close();
        }
        using var streamWriter = new StreamWriter(path, false);
        streamWriter.Write(strings);
    }

    /// <summary>
    /// 写文件
    /// </summary>
    /// <param name="path">文件路径</param>
    /// <param name="strings">文件内容</param>
    /// <param name="encode">编码格式</param>
    public static void WriteFile(string path, string strings, Encoding encode)
    {
        if (!File.Exists(path))
        {
            File.Create(path).Close();
        }
        using var streamWriter = new StreamWriter(path, false, encode);
        streamWriter.Write(strings);
    }

    #endregion 写文件

    #region 读文件

    /// <summary>
    /// 读文件
    /// </summary>
    /// <param name="path">文件路径</param>
    /// <returns></returns>
    public static string ReadFile(string path)
    {
        string s;
        if (!File.Exists(path))
            s = "不存在相应的目录";
        else
        {
            using var streamReader = new StreamReader(path);
            s = streamReader.ReadToEnd();
        }

        return s;
    }

    /// <summary>
    /// 读文件
    /// </summary>
    /// <param name="path">文件路径</param>
    /// <param name="encode">编码格式</param>
    /// <returns></returns>
    public static string ReadFile(string path, Encoding encode)
    {
        string s;
        if (!File.Exists(path))
            s = "不存在相应的目录";
        else
        {
            using var streamReader = new StreamReader(path, encode);
            s = streamReader.ReadToEnd();
        }

        return s;
    }

    #endregion 读文件
}
