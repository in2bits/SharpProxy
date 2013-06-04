using System;
using System.IO;
using System.Text;

namespace SharpProxy
{
    public static class StreamExtensions
    {
        public static string ReadLine(this Stream stream)
        {
            string line = "";
            while (true)
            {
                int b = stream.ReadByte();
                if (b == '\r')
                    continue;
                if (b == '\n')
                    break;
                line += Convert.ToChar(b);
            }
            return line;
        }

        public static void WriteLine(this Stream stream, string line = null)
        {
            line = line ?? "";
            var lineBytes = Encoding.UTF8.GetBytes(line + "\r\n");
            stream.Write(lineBytes, 0, lineBytes.Length);
        }
    }
}