using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Avalonia.EpubComic.Tools;

// 自然排序比较器
public class NaturalStringComparer : IComparer<string>
{
    private const string CoverKeyword = "cover"; // 定义关键词
    private static readonly Regex _numbersRegex = new(@"(\d+)", RegexOptions.Compiled);

    public int Compare(string x, string y)
    {
        // 获取文件名（不含路径）
        var xName = Path.GetFileName(x);
        var yName = Path.GetFileName(y);

        // 处理空值情况
        if (string.IsNullOrEmpty(xName)) return string.IsNullOrEmpty(yName) ? 0 : -1;
        if (string.IsNullOrEmpty(yName)) return 1;

        // 检查是否包含cover关键词（不区分大小写）
        var xHasCover = xName.IndexOf(CoverKeyword, StringComparison.OrdinalIgnoreCase) >= 0;
        var yHasCover = yName.IndexOf(CoverKeyword, StringComparison.OrdinalIgnoreCase) >= 0;

        // 特殊处理：包含cover的排在最前
        if (xHasCover && yHasCover)
            return string.Compare(xName, yName, StringComparison.OrdinalIgnoreCase);
        if (xHasCover) return -1; // x包含cover，排在最前
        if (yHasCover) return 1; // y包含cover，x不包含，y排在最前

        // 使用正则表达式分割字符串，将数字部分作为独立的标记
        string[] xParts = _numbersRegex.Split(xName);
        string[] yParts = _numbersRegex.Split(yName);

        // 比较每个部分
        for (var i = 0; i < Math.Min(xParts.Length, yParts.Length); i++)
            if (int.TryParse(xParts[i], out var xNum) && int.TryParse(yParts[i], out var yNum))
            {
                // 两部分都是数字，按数值比较
                var numComparison = xNum.CompareTo(yNum);
                if (numComparison != 0) return numComparison;
            }
            else
            {
                // 不是数字，按字符串比较
                var comparison = string.Compare(xParts[i], yParts[i], StringComparison.Ordinal);
                if (comparison != 0) return comparison;
            }

        // 所有部分都相同，则较短的字符串排在前面
        return xParts.Length.CompareTo(yParts.Length);
    }
}