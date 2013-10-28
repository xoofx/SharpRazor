// Copyright (c) 2013 SharpRazor - Alexandre Mutel
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;

namespace SharpRazor
{
    /// <summary>
    /// AttributeValue used by the template.
    /// </summary>
    public class AttributeValue
    {
        // TODO Check what this class is exactly for?

        /// <summary>
        /// Initializes a new instance of the <see cref="AttributeValue"/> class.
        /// </summary>
        /// <param name="prefix">The prefix.</param>
        /// <param name="value">The value.</param>
        /// <param name="literal">if set to <c>true</c> [literal].</param>
        public AttributeValue(PositionTagged<string> prefix, PositionTagged<object> value, bool literal)
        {
            Prefix = prefix;
            Value = value;
            Literal = literal;
        }

        /// <summary>
        /// Gets the prefix.
        /// </summary>
        /// <value>The prefix.</value>
        public PositionTagged<string> Prefix { get; private set; }

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <value>The value.</value>
        public PositionTagged<object> Value { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this <see cref="AttributeValue"/> is literal.
        /// </summary>
        /// <value><c>true</c> if literal; otherwise, <c>false</c>.</value>
        public bool Literal { get; private set; }

        /// <summary>
        /// Creates a <see cref="AttributeValue"/> from a tuple {PrefixPosition, ValuePosition, Literal}.
        /// </summary>
        /// <param name="value">The tuple value.</param>
        /// <returns>A new instance of <see cref="AttributeValue"/>.</returns>
        public static AttributeValue FromTuple(Tuple<Tuple<string, int>, Tuple<object, int>, bool> value)
        {
            return new AttributeValue(value.Item1, value.Item2, value.Item3);
        }

        /// <summary>
        /// Creates a <see cref="AttributeValue"/> from a tuple {PrefixPosition, ValuePosition, Literal}.
        /// </summary>
        /// <param name="value">The tuple value.</param>
        /// <returns>A new instance of <see cref="AttributeValue"/>.</returns>
        public static AttributeValue FromTuple(Tuple<Tuple<string, int>, Tuple<string, int>, bool> value)
        {
            return new AttributeValue(value.Item1, new PositionTagged<object>(value.Item2.Item1, value.Item2.Item2), value.Item3);
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="Tuple{Tuple{System.StringSystem.Int32}Tuple{System.ObjectSystem.Int32}System.Boolean}"/> to <see cref="AttributeValue"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The result of the conversion.</returns>
        public static implicit operator AttributeValue(Tuple<Tuple<string, int>, Tuple<object, int>, bool> value)
        {
            return FromTuple(value);
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="Tuple{Tuple{System.StringSystem.Int32}Tuple{System.StringSystem.Int32}System.Boolean}"/> to <see cref="AttributeValue"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The result of the conversion.</returns>
        public static implicit operator AttributeValue(Tuple<Tuple<string, int>, Tuple<string, int>, bool> value)
        {
            return FromTuple(value);
        }
    }
}