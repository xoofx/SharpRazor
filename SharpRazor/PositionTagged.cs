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
using System.Diagnostics;

namespace SharpRazor
{
    /// <summary>
    /// A tagged position.
    /// </summary>
    /// <typeparam name="T">Type of the tag</typeparam>
    [DebuggerDisplay("({Position})\"{Value}\"")]
    public class PositionTagged<T>
    {
        /// <summary>
        /// Prevents a default instance of the <see cref="PositionTagged{T}"/> class from being created.
        /// </summary>
        private PositionTagged()
        {
            Position = 0;
            Value = default(T);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PositionTagged{T}"/> class.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="offset">The offset.</param>
        public PositionTagged(T value, int offset)
        {
            Position = offset;
            Value = value;
        }

        /// <summary>
        /// Gets the position.
        /// </summary>
        /// <value>The position.</value>
        public int Position { get; private set; }

        /// <summary>
        /// Gets the value associated to this position.
        /// </summary>
        /// <value>The value.</value>
        public T Value { get; private set; }

        public override bool Equals(object obj)
        {
            var other = obj as PositionTagged<T>;
            return other != null &&
                   other.Position == Position &&
                   Equals(other.Value, Value);
        }

        public override int GetHashCode()
        {
            return HashCodeCombiner.Start()
                .Add(Position)
                .Add(Value)
                .CombinedHash;
        }

        public override string ToString()
        {
            return Value.ToString();
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="PositionTagged{"/> to <see cref="`0"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The result of the conversion.</returns>
        public static implicit operator T(PositionTagged<T> value)
        {
            return value.Value;
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="Tuple{"/> to <see cref="PositionTagged{`0}"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The result of the conversion.</returns>
        public static implicit operator PositionTagged<T>(Tuple<T, int> value)
        {
            return new PositionTagged<T>(value.Item1, value.Item2);
        }

        /// <summary>
        /// Implements the ==.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator ==(PositionTagged<T> left, PositionTagged<T> right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Implements the !=.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator !=(PositionTagged<T> left, PositionTagged<T> right)
        {
            return !Equals(left, right);
        }
    }
}