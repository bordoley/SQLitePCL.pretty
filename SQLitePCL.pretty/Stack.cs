﻿using System.Collections;
using System.Collections.Generic;

// A trivial cons list implementation
namespace SQLitePCL.pretty
{
    internal static class Stack
    {
        public static bool IsEmpty<T>(this Stack<T> stack)
        {
            return (stack.Head == null) && (stack.Tail == null);
        }
    }

    internal sealed class Stack<T>
    {
        private static readonly Stack<T> empty = new Stack<T>(default(T), null);

        public static Stack<T> Empty { get { return empty; } }

        private readonly Stack<T> tail;
        private readonly T head;

        internal Stack(T head, Stack<T> tail)
        {
            this.head = head;
            this.tail = tail;
        }

        public T Head
        {
            get
            {
                return head;
            }
        }

        public Stack<T> Tail 
        { 
            get
            {
                return tail;
            }
        }

        public Stack<T> Push(T element)
        {
            return new Stack<T>(element, this);
        }
    }
}