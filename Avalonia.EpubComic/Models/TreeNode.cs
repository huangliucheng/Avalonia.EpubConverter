using System.Collections.Generic;

namespace Avalonia.EpubComic.Models;

// 文件路径的树状结构节点
public class TreeNode(string name, int level)
{
    public string Name { get; set; } = name;
    public List<TreeNode> Children { get; set; } = [];
    public int Level { get; set; } = level;

    //增加节点
    public void AddChild(TreeNode node)
    {
        Children.Add(node);
    }

    //获得子节点的个数
    public int GetChildCount()
    {
        return Children.Count;
    }

    // 克隆节点
    public TreeNode Clone()
    {
        TreeNode newNode = new(Name, Level);
        foreach (var child in Children) newNode.AddChild(child.Clone());
        return newNode;
    }
}