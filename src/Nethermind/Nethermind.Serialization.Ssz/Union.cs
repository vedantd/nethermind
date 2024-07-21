using System;

public abstract class Union<TBase> where TBase : class
{
    public byte Selector { get; }
    public TBase Value { get; }

    protected Union(byte selector, TBase value)
    {
        Selector = selector;
        Value = value;
    }
}