using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia.EpubComic.Tools;

namespace Avalonia.EpubComic.Models;

// 用于处理文件结构的工具类
public static class ComicStructure
{
    // 建立文件路径树结构
    public static TreeNode BuildTree(string rootPath)
    {
        TreeNode rootNode = new(Path.GetFileName(rootPath), 1);
        BuildTreeRecursive(rootNode, rootPath, 1);
        return rootNode;
        
        // 使用递归方法建立文件路径树结构
        static void BuildTreeRecursive(TreeNode parentNode, string directoryPath, int level)
        {

            string[] directories = Directory.GetDirectories(directoryPath);
            directories = directories.OrderBy(f => f, new NaturalStringComparer()).ToArray();
            foreach (string directory in directories)
            {
                TreeNode childNode = new TreeNode(Path.GetFileName(directory), level + 1);
                parentNode.AddChild(childNode);
                BuildTreeRecursive(childNode, directory, level + 1);
            }
            
            string[] files = Directory.GetFiles(directoryPath);
            files = files.OrderBy(f => f, new NaturalStringComparer()).ToArray();
            files = files.Where(file => file.EndsWith(".jpg") || file.EndsWith(".png") || file.EndsWith(".jpeg")).ToArray();
            foreach (string file in files)
            {
                parentNode.AddChild(new TreeNode(Path.GetFileName(file), level + 1));
            }
        }
    }
    
    public static void GeneratePathList(List<string> pathsList, TreeNode node, string currentPath = "")
    {
        string combinePath = Path.Combine(currentPath, node.Name).Replace('\\', '/');
        if (node.Children.Count == 0)
        {
            pathsList.Add(combinePath);
        }
        else
        {
            foreach (var child in node.Children)
            {
                GeneratePathList(pathsList, child, combinePath);
            }
        }
    }
    
    //获得树形结构的深度
    private static int GetDepth(TreeNode node)
    {
        var children = node.Children;
        if (children.Count == 0) return 1;

        int maxChildDepth = 0;
        foreach (var child in children)
        {
            int childDepth = GetDepth(child);
            if (childDepth > maxChildDepth) maxChildDepth = childDepth;
        }

        return maxChildDepth + 1;
    }
    
    private static readonly string[] ComicValueList = ["Image", "Chapter", "Volume", "Comic"];
    public static void NodeValueReplace(TreeNode node)
    {
        static void ValueReplace(TreeNode node, int maxDepth)
        {
            var children = node.Children;
            if (children.Count == 0) return;

            for (int i = 0; i < children.Count; i++)
            {
                string levelName = ComicValueList[maxDepth - node.Level - 1];
                if (string.IsNullOrEmpty(children[i].Name)) continue;
                children[i].Name = $"{levelName}-{i + 1:D4}";
                ValueReplace(children[i], maxDepth);
            }
        }
        int maxDepth = GetDepth(node);
        TreeNode rootNode = new("head", 0);
        rootNode.AddChild(node);
        ValueReplace(rootNode, maxDepth);
    }

    public static void GetChapterNameList(TreeNode node, List<string> chapterNameList)
    {
        if (GetDepth(node) == 2)
        {
            chapterNameList.Add(node.Name);
            return;
        }
        foreach (var child in node.Children)
        {
            chapterNameList.Add(child.Name);
        }
    }

    public static void GetChapterLinkList(TreeNode node, List<string> chapterLinkList, string currentPath = "")
    {
        string combinePath = string.IsNullOrEmpty(currentPath) ? node.Name : Path.Combine(currentPath, node.Name);
        
        // 只处理那些包含图片的节点（叶子节点）的父节点
        if (node.Children.Count > 0 && node.Children[0].Children.Count == 0)
        {
            // 对于图片的父节点（章节），将第一个图片的路径添加到列表中
            if (node.Children.Count > 0)
            {
                string imagePath = Path.Combine(combinePath, node.Children[0].Name);
                imagePath = Path.ChangeExtension(imagePath, "xhtml").Replace('\\', '/');
                chapterLinkList.Add(imagePath);
            }
        }
        
        // 递归处理子节点
        foreach (var child in node.Children)
        {
            if (child.Children.Count > 0) // 只递归处理非叶子节点
            {
                GetChapterLinkList(child, chapterLinkList, combinePath);
            }
        }
    }
    
}