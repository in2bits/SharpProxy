using System;
using System.Runtime.InteropServices;

namespace IPHelper
{
    public static class Helper
    {
        // from header files
        public const uint LoadLibraryAsDatafile = 0x00000002;
        public const uint FormatMessageFromHmodule = 0x00000800;
        public const uint FormatMessageAllocateBuffer = 0x00000100;
        public const uint FormatMessageIgnoreInserts = 0x00000200;
        public const uint FormatMessageFromSystem = 0x00001000;

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr LoadLibraryEx([MarshalAs(UnmanagedType.LPTStr)] string lpFileName, IntPtr hFile,
                                                   uint dwFlags);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern int FormatMessageW(uint dwFormatFlags, IntPtr lpSource, int dwMessageId, int dwLanguageId,
                                                 out IntPtr msgBuffer, int nSize, IntPtr arguments);

        public static string GetMessage(int id, string dllFile)
        {
            IntPtr pMessageBuffer;
            string sMsg = "";
            uint dwFormatFlags = FormatMessageAllocateBuffer | FormatMessageFromSystem | FormatMessageIgnoreInserts;

            IntPtr hModule = LoadLibraryEx(dllFile, IntPtr.Zero, LoadLibraryAsDatafile);

            if (IntPtr.Zero != hModule)
            {
                dwFormatFlags |= FormatMessageFromHmodule;
                Console.WriteLine("\n > Found hmodule for: " + dllFile);
            }

            int dwBufferLength = FormatMessageW(dwFormatFlags, hModule, id, 0, out pMessageBuffer, 0, IntPtr.Zero);
            if (0 != dwBufferLength)
            {
                sMsg = Marshal.PtrToStringUni(pMessageBuffer);
                Marshal.FreeHGlobal(pMessageBuffer);
            }
            return sMsg;
        }
    }
}