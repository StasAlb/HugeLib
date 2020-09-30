using System;
using System.IO;
using System.Collections.Generic;

namespace HugeLib
{
    public static class StreamReaderExtensions
    {
        public static IEnumerable<string> ReadUntil(this StreamReader reader, string delimiter)
        {
            List<char> buffer = new List<char>();
            CircularBuffer<char> delim_buffer = new CircularBuffer<char>(delimiter.Length);
            while (reader.Peek() >= 0)
            {
                char c = (char)reader.Read();
                //byte c = (byte)reader.Read();
                delim_buffer.Enqueue(c);
                if (delim_buffer.ToString() == delimiter || reader.EndOfStream)
                {
                    if (buffer.Count > 0)
                    {
                        if (delim_buffer.ToString() == delimiter)
                        //if (!reader.EndOfStream)
                        {
                            //yield return System.Text.Encoding.GetEncoding(encPage).GetString(buffer.ToArray(), 0, buffer.Count - delimiter.Length - 1);
                            yield return new String(buffer.ToArray()).Replace(delimiter.Substring(0, delimiter.Length - 1), string.Empty);
                        }
                        else
                        {
                            buffer.Add(c);
                            //yield return System.Text.Encoding.GetEncoding(encPage).GetString(buffer.ToArray());
                            yield return new String(buffer.ToArray());//,0, buffer.Count, System.Text.Encoding.GetEncoding(encPage));
                        }
                        buffer.Clear();
                    }
                    continue;
                }
                buffer.Add(c);
            }
        }
        public static IEnumerable<byte[]> ReadBytesUntil(this BinaryReader reader, byte[] delimiter)
        {
            List<byte> buffer = new List<byte>();
            CircularBuffer<byte> delim_buffer = new CircularBuffer<byte>(delimiter.Length);
            while (reader.PeekChar() >= 0)
            {
                byte c = (byte)reader.Read();
                delim_buffer.Enqueue(c);
                if (ArraysEqual<byte>(delim_buffer.ToArray(), delimiter) || reader.PeekChar() < 0)
                {
                    if (buffer.Count > 0)
                    {
                        if (ArraysEqual<byte>(delim_buffer.ToArray(), delimiter))
                        //if (!reader.EndOfStream)
                        {
                            //yield return System.Text.Encoding.GetEncoding(encPage).GetString(buffer.ToArray(), 0, buffer.Count - delimiter.Length - 1);
                            buffer.RemoveRange(buffer.Count - delimiter.Length + 1, delimiter.Length - 1);
                            yield return buffer.ToArray();// new String(buffer.ToArray()).Replace(delimiter.Substring(0, delimiter.Length - 1), string.Empty);
                        }
                        else
                        {
                            buffer.Add(c);
                            //yield return System.Text.Encoding.GetEncoding(encPage).GetString(buffer.ToArray());
                            yield return buffer.ToArray();//,0, buffer.Count, System.Text.Encoding.GetEncoding(encPage));
                        }
                        buffer.Clear();
                    }
                    continue;
                }
                buffer.Add(c);
            }
        }
        public static bool ArraysEqual<T>(T[] a1, T[] a2)
        {
            if (ReferenceEquals(a1, a2))
                return true;

            if (a1 == null || a2 == null)
                return false;

            if (a1.Length != a2.Length)
                return false;

            EqualityComparer<T> comparer = EqualityComparer<T>.Default;
            for (int i = 0; i < a1.Length; i++)
            {
                if (!comparer.Equals(a1[i], a2[i])) return false;
            }
            return true;
        }

        public class CircularBuffer<T> : Queue<T>
        {
            private int _capacity;

            public CircularBuffer(int capacity)
                : base(capacity)
            {
                _capacity = capacity;
            }

            new public void Enqueue(T item)
            {
                if (base.Count == _capacity)
                {
                    base.Dequeue();
                }
                base.Enqueue(item);
            }
            public override string ToString()
            {
                List<String> items = new List<string>();
                foreach (var x in this)
                {
                    items.Add(x.ToString());
                };
                return String.Join("", items);
            }
        }
    }
}