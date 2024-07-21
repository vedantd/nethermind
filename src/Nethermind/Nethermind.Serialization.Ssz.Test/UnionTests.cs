using System;
using System.Collections.Generic;
using NUnit.Framework;
using Nethermind.Serialization.Ssz;

namespace Nethermind.Serialization.Ssz.Test
{
    public interface ITestMessage { }

    public class TestMessageA : ITestMessage
    {
        public uint Value { get; set; }
    }

    public class TestMessageB : ITestMessage
    {
        public byte[]? Value { get; set; } // Changed to nullable
    }

    [Union(typeof(TestMessageA), typeof(TestMessageB))]
    public class TestUnion : Union<ITestMessage>
    {
        public static readonly Dictionary<byte, Type> SelectorToType = new Dictionary<byte, Type>
        {
            { 0x00, typeof(TestMessageA) },
            { 0x01, typeof(TestMessageB) }
        };

        public TestUnion(byte selector, ITestMessage value) : base(selector, value) { }

        public static TestUnion CreateTestMessageA(TestMessageA value) => new TestUnion(0x00, value);
        public static TestUnion CreateTestMessageB(TestMessageB value) => new TestUnion(0x01, value);

        public static void Encode(Span<byte> span, TestUnion union)
        {
            Ssz.Encode(span.Slice(0, 1), union.Selector);

            switch (union.Value)
            {
                case TestMessageA messageA:
                    Ssz.Encode(span.Slice(1), messageA.Value);
                    break;
                case TestMessageB messageB:
                    Ssz.Encode(span.Slice(1), messageB.Value);
                    break;
                default:
                    throw new ArgumentException("Unknown union type");
            }
        }

        public static TestUnion Decode(ReadOnlySpan<byte> span)
        {
            byte selector = Ssz.DecodeByte(span.Slice(0, 1));

            switch (selector)
            {
                case 0x00:
                    uint valueA = Ssz.DecodeUInt(span.Slice(1, 4));
                    return new TestUnion(selector, new TestMessageA { Value = valueA });
                case 0x01:
                    byte[] valueB = Ssz.DecodeBytes(span.Slice(1)).ToArray();
                    return new TestUnion(selector, new TestMessageB { Value = valueB });
                default:
                    throw new ArgumentException("Unknown union selector");
            }
        }
    }

    [TestFixture]
    public class UnionTests
    {
        [Test]
        public void Can_create_union_with_correct_selector()
        {
            var messageA = new TestMessageA { Value = 42 };
            var unionA = TestUnion.CreateTestMessageA(messageA);
            Assert.That(unionA.Selector, Is.EqualTo((byte)0x00));
            Assert.That(unionA.Value, Is.EqualTo(messageA));

            var messageB = new TestMessageB { Value = new byte[] { 1, 2, 3, 4 } };
            var unionB = TestUnion.CreateTestMessageB(messageB);
            Assert.That(unionB.Selector, Is.EqualTo((byte)0x01));
            Assert.That(unionB.Value, Is.EqualTo(messageB));
        }

        [Test]
        public void Can_encode_and_decode_union()
        {
            var messageA = new TestMessageA { Value = 42 };
            var unionA = TestUnion.CreateTestMessageA(messageA);

            var buffer = new byte[5]; // 1 byte for selector, 4 bytes for uint
            var span = new Span<byte>(buffer);

            TestUnion.Encode(span, unionA);

            var decodedUnion = TestUnion.Decode(span);

            Assert.That(decodedUnion.Selector, Is.EqualTo((byte)0x00));
            Assert.That(decodedUnion.Value, Is.InstanceOf<TestMessageA>());
            Assert.That(((TestMessageA)decodedUnion.Value).Value, Is.EqualTo(42));

            var messageB = new TestMessageB { Value = new byte[] { 1, 2, 3, 4 } };
            var unionB = TestUnion.CreateTestMessageB(messageB);

            buffer = new byte[5]; // 1 byte for selector, 4 bytes for byte array
            span = new Span<byte>(buffer);

            TestUnion.Encode(span, unionB);

            decodedUnion = TestUnion.Decode(span);

            Assert.That(decodedUnion.Selector, Is.EqualTo((byte)0x01));
            Assert.That(decodedUnion.Value, Is.InstanceOf<TestMessageB>());
            Assert.That(((TestMessageB)decodedUnion.Value).Value, Is.EqualTo(new byte[] { 1, 2, 3, 4 }));
        }
    }
}