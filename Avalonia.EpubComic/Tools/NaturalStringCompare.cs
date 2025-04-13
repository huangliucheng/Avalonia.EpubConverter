using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Avalonia.EpubComic.Models;

// 自然排序比较器
public class NaturalStringComparer : IComparer<string>
{
    private static readonly Regex _numbersRegex = new Regex(@"(\d+)", RegexOptions.Compiled);
    
    public int Compare(string x, string y)
    {
        // 获取文件名（不含路径）
        string xName = Path.GetFileName(x);
        string yName = Path.GetFileName(y);
        
        if (string.IsNullOrEmpty(xName) && string.IsNullOrEmpty(yName)) return 0;
        if (string.IsNullOrEmpty(xName)) return -1;
        if (string.IsNullOrEmpty(yName)) return 1;
        
        // 使用正则表达式分割字符串，将数字部分作为独立的标记
        string[] xParts = _numbersRegex.Split(xName);
        string[] yParts = _numbersRegex.Split(yName);
        
        // 比较每个部分
        for (int i = 0; i < Math.Min(xParts.Length, yParts.Length); i++)
        {
            int xNum = 0;
            int yNum = 0;
            bool xIsNumber = i < xParts.Length && int.TryParse(xParts[i], out xNum);
            bool yIsNumber = i < yParts.Length && int.TryParse(yParts[i], out yNum);
            
            if (xIsNumber && yIsNumber)
            {
                // 如果两部分都是数字，则按数值比较
                int numComparison = xNum.CompareTo(yNum);
                if (numComparison != 0) return numComparison;
            }
            else
            {
                // 如果不是数字，则按字符串比较
                int comparison = string.Compare(xParts[i], yParts[i], StringComparison.CurrentCulture);
                if (comparison != 0) return comparison;
            }
        }
        
        // 如果所有部分都相同，则较短的字符串排在前面
        return xParts.Length.CompareTo(yParts.Length);
    }
}