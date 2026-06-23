using Microsoft.Extensions.VectorData;
using System;
using System.Collections.Generic;

public static class DataService
{
    public static List<(string Question, string Answer)> GetData()
    {
        List<(string Question, string Answer)> knowledge =
        [
            new("neptune是什么？", "neptune 是一个开源的图形引擎库"),
            new("neptune有哪些功能？", "图形渲染、窗口管理、组件管理等"),
            new("neptune最近的开发方向是什么？", "ai agent"),
            new("neptune立项是在什么时候？", "25年四五月份"),
        ];

        return knowledge;
    }
}

public class VectorEntry
{
    [VectorStoreKey]
    public required Guid Id { get; set; }

    [VectorStoreData]
    public required string Question { get; set; }

    [VectorStoreData]
    public required string Answer { get; set; }

    [VectorStoreVector(384)]   // all-MiniLM-L6-v2 是 384 维
    public ReadOnlyMemory<float> Vector { get; set; }
}