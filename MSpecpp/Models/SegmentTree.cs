using System;
using Avalonia.Rendering.Composition.Animations;

namespace MSpecpp;

public record struct SegmentTreeNode(float Min, float Max);

public class SegmentTree
{
    public SegmentTreeNode[] Nodes;
    public float[] RawArray;

    public SegmentTree(float[] array)
    {
        RawArray = array;
        int nodeCount = (int)Math.Pow(2, Math.Ceiling(Math.Log2(array.Length)) + 1);
        Nodes = new SegmentTreeNode[nodeCount];
        BuildTree(0, array.Length, 1);
    }

    void BuildTree(int start, int end, int nodeIndex)
    {
        if (end - start < 1)
        {
            return;
        }

        if (end - start == 1)
        {
            Nodes[nodeIndex] = new SegmentTreeNode(RawArray[start], RawArray[start]);
            return;
        }

        int mid = (end + start) / 2;
        int left = nodeIndex << 1;
        int right = left | 1;

        BuildTree(start, mid, left);
        BuildTree(mid, end, right);
        Nodes[nodeIndex] = new SegmentTreeNode(
            Math.Min(Nodes[left].Min, Nodes[right].Min),
            Math.Max(Nodes[left].Max, Nodes[right].Max)
        );
    }

    private (float, float) QueryMinMaxImpl(int start, int end, int from, int to, int nodeIndex)
    {
        
        int mid = (end + start) / 2;

        if (from <= start && to >= end)
        {
            return (Nodes[nodeIndex].Min, Nodes[nodeIndex].Max);
        }

        float min = float.MaxValue, max = float.MinValue;
        int left = nodeIndex << 1;
        int right = left | 1;
        if (from < mid)
        {
            (min, max) = QueryMinMaxImpl(start, mid, from, to, left);
        }

        if (to > mid)
        {
            var (newMin, newMax) = QueryMinMaxImpl(mid, end, from, to, right);
            min = Math.Min(min, newMin);
            max = Math.Max(max, newMax);
        }

        return (min, max);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="startIndex">Inclusive</param>
    /// <param name="endIndex">Exclusive</param>
    /// <returns></returns>
    public (float, float) QueryMinMax(int startIndex, int endIndex)
    {
        return QueryMinMaxImpl(0, RawArray.Length, startIndex, endIndex, 1);
    }
}