using System;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class UnionAttribute : Attribute
{
    public Type[] Types { get; }

    public UnionAttribute(params Type[] types)
    {
        Types = types;
    }
}