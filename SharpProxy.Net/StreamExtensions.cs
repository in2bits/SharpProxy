using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SharpProxy
{
    public static class StreamExtensions
    {
        public static string ReadLine(this Stream stream)
        {
            string line = "";
            int allowedBadChars = 1;
            while (true)
            {
                int b = stream.ReadByte();
                if (b < 0)
                {
                    if (allowedBadChars == 0)
                        break;
                    allowedBadChars++;
                    continue;
                }
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

        async public static Task CopyToAsync(this Stream source, Stream target, long length)
        {
            var totalToRead = length;
            var buffer = new byte[0x10000];
            Debug.WriteLine("Copying " + totalToRead + " ...");
            while (totalToRead > 0)
            {
                var read = await source.ReadAsync(buffer, 0, buffer.Length);
                await target.WriteAsync(buffer, 0, read);
                totalToRead -= read;
                Debug.WriteLine("TotalToRead: " + totalToRead);
            }
            Debug.WriteLine("Copying DONE (" + length + ")");
        }

        async public static Task CopyHttpMessageToAsync(this Stream source, Socket sourceSocket, Stream target, long contentLength = -1)
        {
            if (sourceSocket.Available == 0)
            {
                Debug.WriteLine("DataAvailable: 0");
                return;
            }

            var isChunked = contentLength == -1;
            var remaining = contentLength;
            int read = 0;
            var bufferLength = 0x1000;
            byte[] buffer = new byte[bufferLength];
            string chunkHeader = null;
            do
            {
                if (isChunked)
                {
                    bufferLength = GetChunkLength(source, out chunkHeader);
                    if (bufferLength == 0)
                    {
                        source.ReadLine(); //eat last \r\n
                        target.WriteLine();
                        bufferLength = -1;
                    }

                    if (bufferLength == -1)
                        return;

                    Debug.WriteLine("Expecting Chunk: " + bufferLength);
                    target.WriteLine(chunkHeader);
                    buffer = new byte[bufferLength];
                    remaining = bufferLength;
                }

                do
                {
                    read = await source.ReadAsync(buffer, 0, (int) Math.Min(buffer.Length, remaining));
                    Debug.WriteLine("Read: " + read);
                    remaining -= read;

                    await target.WriteAsync(buffer, 0, read);
                } while (read > 0 && remaining > 0);

                if (isChunked)
                {
                    var line = source.ReadLine(); //eat \r\n
                    if (line != "")
                        throw new InvalidDataException("Unexpected line content");
                    remaining = 1; //continue to next chunk
                }
                else
                {
                    remaining -= read;
                }
            } while (remaining > 0);
        }

        private static int GetChunkLength(Stream source, out string chunkHeader)
        {
            chunkHeader = null;
            try
            {
                chunkHeader = source.ReadLine();
                var parts = chunkHeader.Split(new char[] {':'}, 2, StringSplitOptions.None);
                var sizeString = parts[0].Trim();
                var chunkSize = int.Parse(sizeString, NumberStyles.AllowHexSpecifier);
                return chunkSize;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("CopyAllToAsync error: " + ex.Message);
                return -1;
            }
        }
    }

    //public class CustomNetworkStream : NetworkStream
    //{
    //    public CustomNetworkStream(Socket socket) : base(socket)
    //    {
    //    }

    //    override 
    //}
}