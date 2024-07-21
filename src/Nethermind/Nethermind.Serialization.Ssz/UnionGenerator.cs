// SPDX-FileCopyrightText: 2022 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System;
using System.CodeDom.Compiler;
using System.IO;
using System.Reflection;
using System.Text;
using System.Collections.Generic;

namespace Nethermind.Serialization.Ssz
{
    public static class UnionGenerator
    {
        public static string GenerateUnionCode<TUnion, TBase>() where TUnion : Union<TBase> where TBase : class
        {
            var unionAttribute = typeof(TUnion).GetCustomAttribute<UnionAttribute>();
            if (unionAttribute == null)
            {
                throw new ArgumentException($"Type {typeof(TUnion)} is not marked with UnionAttribute");
            }

            var sb = new StringBuilder();
            using (var writer = new StringWriter(sb))
            using (var indentedWriter = new IndentedTextWriter(writer, "    "))
            {
                indentedWriter.WriteLine($"public class {typeof(TUnion).Name} : Union<{typeof(TBase).Name}>");
                indentedWriter.WriteLine("{");
                indentedWriter.Indent++;

                // Generate SelectorToType dictionary
                indentedWriter.WriteLine("public static readonly Dictionary<byte, Type> SelectorToType = new Dictionary<byte, Type>");
                indentedWriter.WriteLine("{");
                indentedWriter.Indent++;
                for (byte i = 0; i < unionAttribute.Types.Length; i++)
                {
                    indentedWriter.WriteLine($"{{ 0x{i:X2}, typeof({unionAttribute.Types[i].Name}) }},");
                }
                indentedWriter.Indent--;
                indentedWriter.WriteLine("};");
                indentedWriter.WriteLine();

                // Generate constructor
                indentedWriter.WriteLine($"public {typeof(TUnion).Name}(byte selector, {typeof(TBase).Name} value) : base(selector, value) {{ }}");
                indentedWriter.WriteLine();

                // Generate factory methods
                for (byte i = 0; i < unionAttribute.Types.Length; i++)
                {
                    var type = unionAttribute.Types[i];
                    indentedWriter.WriteLine($"public static {typeof(TUnion).Name} Create{type.Name}({type.Name} value) => new {typeof(TUnion).Name}(0x{i:X2}, value);");
                }
                indentedWriter.WriteLine();

                // Generate Decode method
                indentedWriter.WriteLine($"public static {typeof(TUnion).Name} Decode(ReadOnlySpan<byte> span, ref int offset)");
                indentedWriter.WriteLine("{");
                indentedWriter.Indent++;
                indentedWriter.WriteLine($"return ({typeof(TUnion).Name})Ssz.DecodeUnion(span, ref offset, selector => SelectorToType[selector]);");
                indentedWriter.Indent--;
                indentedWriter.WriteLine("}");

                indentedWriter.Indent--;
                indentedWriter.WriteLine("}");
            }

            return sb.ToString();
        }
    }
}