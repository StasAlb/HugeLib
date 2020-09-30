using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace HugeLib
{
    public static class Utils
    {
        public static string Bin2AHex(byte bt)
        {
            return String.Format("{0:X2}", bt);
        }
        public static string Bin2AHex(byte[] bytes)
        {
            if (bytes == null)
                return "";
            string str = "";
            foreach (byte b in bytes)
                str = String.Format("{0}{1:X2}", str, b);
            return str;
        }
        public static string Bin2AHex(byte[] bytes, int cnt)
        {
            if (bytes == null)
                return "";
            string str = "";
            int i = 0;
            foreach (byte b in bytes)
            {
                str = String.Format("{0}{1:X2}", str, b);
                if (++i == cnt)
                    break;
            }
            return str;
        }
        public static string Bin2AHex(byte[] bytes, int start, int cnt)
        {
            if (bytes == null)
                return "";
            string str = "";
            for (int i = start; i < start + cnt && i < bytes.Length; i++)
                str = String.Format("{0}{1:X2}", str, bytes[i]);
            return str;
        }
        public static string Bin2String(byte[] bytes)
        {
            return Encoding.ASCII.GetString(bytes);
        }
        public static string Bin2String(byte[] bytes, int codepage)
        {
            return Encoding.GetEncoding(codepage).GetString(bytes);
        }
        public static string Bin2String(byte[] bytes, int start, int length)
        {
            if (start >= bytes.Length)
                return "";
            return Encoding.ASCII.GetString(bytes, start, length);
        }
        public static string Bin2String(byte[] bytes, int start, int length, int codepage)
        {
            if (start >= bytes.Length)
                return "";
            return Encoding.GetEncoding(codepage).GetString(bytes, start, length);
        }
        public static byte[] AHex2Bin(string str)
        {
            str = str.ToUpper();
            byte[] res = new byte[str.Length / 2];
            int i = 0, c1 = 0, c2 = 0;
            while (i < str.Length)
            {
                c1 = (Char.IsDigit(str, i)) ? str[i] - '0' : str[i] - 'A' + 10;
                if (i + 1 < str.Length)
                    c2 = (Char.IsDigit(str, i + 1)) ? str[i + 1] - '0' : str[i + 1] - 'A' + 10;
                else
                {
                    c2 = c1;
                    c1 = 0;
                }
                res[i/2] = (byte)(c1 * 16 + c2);
                i += 2;
            }
            return res;
        }
        public static string AHex2String(string str)
        {
            try
            {
                return Bin2String(AHex2Bin(str));
            }
            catch
            {
                return "";
            }
            
        }
        public static byte[] String2Bin(string str)
        {
            return Encoding.ASCII.GetBytes(str);
        }
        public static byte[] String2Bin(string str, int codepage)
        {
            return Encoding.GetEncoding(codepage).GetBytes(str);
        }
        public static string String2AHex(string str)
        {
            return Bin2AHex(Encoding.ASCII.GetBytes(str));
        }
        public static string String2AHex(string str, int codepage)
        {
            return Bin2AHex(Encoding.GetEncoding(codepage).GetBytes(str));
        }
        public static bool BinaryMask(byte val, string mask)
        {
            for (int i = 7; i >= 0; i--)
            {
                if (mask[7-i] != 'x' && mask[7-i]-48 != ((val >> i) & 1))
                    return false;
            }
            return true;
        }
        public static string FormatDateYDDD(string ymmm)
        {
            string res = "";
            try
            {
                int y = 0;
                int yy = DateTime.Now.Year;
                DateTime dt;
                if (ymmm != "0000")
                {
                    y = Convert.ToInt32(ymmm.Substring(0, 1));
                    y = (yy % 10 > y) ? 10 * (yy / 10) + y : 10 * (yy / 10) + y - 10;
                    dt = DateTime.Parse(String.Format("01.01.{0}", y));
                    dt = dt.AddDays(Convert.ToInt32(ymmm.Substring(1, 3)) - 1);
                    if (dt.AddYears(10) < DateTime.Now)
                        dt = dt.AddYears(10);
                    res = String.Format("{0:dd.MM.yyyy}", dt);
                }
                else
                    res = "";
            }
            catch
            {
                res = "";
            }
            return res;
        }
        public static int GetInt(byte b1, byte b2)
        {
            return Convert.ToInt32(b1 * 256 + b2);
        }
        public static int GetInt(string val)
        {
            return Int32.Parse(val, System.Globalization.NumberStyles.HexNumber);
        }
        public static string Int2AHex(int val)
        {
            return val.ToString("X8");
        }
        public static byte[] Int2Bytes(int nom)
        {
            byte[] bt = new byte[2];
            bt[0] = Convert.ToByte(nom / 256);
            bt[1] = Convert.ToByte(nom % 256);
            return bt;
        }
        public static byte[] Short2Bytes(ushort nom)
        {
            byte[] bt = new byte[2];
            bt[0] = Convert.ToByte(nom / 256);
            bt[1] = Convert.ToByte(nom % 256);
            return bt;
        }
        public static byte ArrXor(byte[] b1)
        {
            byte res = 0;
            foreach (byte b in b1)
                res = (byte)(res ^ b);
            return res;
        }
        public static string ReverseAHex(string str)
        {
            byte[] bt = AHex2Bin(str);
            Array.Reverse(bt);
            return Bin2AHex(bt);
        }
        /// <summary>
        /// Rotates the bits in an array of bytes to the left.
        /// </summary>
        /// <param name="bytes">The byte array to rotate.</param>
        public static string RotateLeft(byte[] bytes, int bitN)
        {
            for (; bitN > 0; bitN--)
            {
                bool carryFlag = ShiftLeft(bytes);

                if (carryFlag == true)
                    bytes[bytes.Length - 1] = (byte)(bytes[bytes.Length - 1] | 0x01);
            }
            return Utils.Bin2AHex(bytes);
        }

        /// <summary>
        /// Rotates the bits in an array of bytes to the right.
        /// </summary>
        /// <param name="bytes">The byte array to rotate.</param>
        public static string RotateRight(byte[] bytes, int bitN)
        {
            for (; bitN > 0; bitN--)
            {
                bool carryFlag = ShiftRight(bytes);
                if (carryFlag == true)
                    bytes[0] = (byte)(bytes[0] | 0x80);
            }
            return Utils.Bin2AHex(bytes);
        }
        /// <summary>
        /// Shifts the bits in an array of bytes to the left.
        /// </summary>
        /// <param name="bytes">The byte array to shift.</param>
        public static bool ShiftLeft(byte[] bytes)
        {
            bool leftMostCarryFlag = false;

            // Iterate through the elements of the array from left to right.
            for (int index = 0; index < bytes.Length; index++)
            {
                // If the leftmost bit of the current byte is 1 then we have a carry.
                bool carryFlag = (bytes[index] & 0x80) > 0;

                if (index > 0)
                {
                    if (carryFlag == true)
                    {
                        // Apply the carry to the rightmost bit of the current bytes neighbor to the left.
                        bytes[index - 1] = (byte)(bytes[index - 1] | 0x01);
                    }
                }
                else
                {
                    leftMostCarryFlag = carryFlag;
                }

                bytes[index] = (byte)(bytes[index] << 1);
            }

            return leftMostCarryFlag;
        }
        /// <summary>
        /// Shifts the bits in an array of bytes to the right.
        /// </summary>
        /// <param name="bytes">The byte array to shift.</param>
        public static bool ShiftRight(byte[] bytes)
        {
            bool rightMostCarryFlag = false;
            int rightEnd = bytes.Length - 1;

            // Iterate through the elements of the array right to left.
            for (int index = rightEnd; index >= 0; index--)
            {
                // If the rightmost bit of the current byte is 1 then we have a carry.
                bool carryFlag = (bytes[index] & 0x01) > 0;

                if (index < rightEnd)
                {
                    if (carryFlag == true)
                    {
                        // Apply the carry to the leftmost bit of the current bytes neighbor to the right.
                        bytes[index + 1] = (byte)(bytes[index + 1] | 0x80);
                    }
                }
                else
                {
                    rightMostCarryFlag = carryFlag;
                }

                bytes[index] = (byte)(bytes[index] >> 1);
            }

            return rightMostCarryFlag;
        }
    }
    public class tlv
    {
        public string tag;
        public string value;
        public int length;
        public tlv(string tg, int ln, string val)
        {
            tag = tg; length = ln; value = val;
        }
        public override string ToString()
        {
            return string.Format("{0,4} {1:X2}h {2}", tag, length, value);
        }
    }
    public class TagList
    {
        public bool GoodParse;
        public SortedList<string, tlv> sl = null;

        public TagList()
        {
            sl = new SortedList<string, tlv>();
        }
        public TagList(string str)
        {
            sl = new SortedList<string, tlv>();
            GoodParse = ParseString(str, null, null);
        }
        public TagList(string str, string[] two)
        {
            sl = new SortedList<string, tlv>();
            GoodParse = ParseString(str, two, null);
        }
        public TagList(string str, string[] two, string[] sost)
        {
            sl = new SortedList<string, tlv>();
            GoodParse = ParseString(str, two, sost);
        }
        public bool ParseString(string str, string[] two, string[] sost)
        {
            if (two == null)
                two = new string[] { "DF", "9F", "5F", "FF", "AA", "BF" };
            str = str.Trim();
            while (str.Length > 0)
            {
                string fff = str;
                string tg = str.Substring(0, 2);
                bool fl = false;
                foreach (string s in two)
                    if (s == tg.ToUpper())
                        fl = true;
                if (fl)
                    tg = str.Substring(0, 4);
                str = str.Substring(tg.Length);
                int ln = Utils.AHex2Bin(str.Substring(0, 2))[0];
                str = str.Substring(2);
                string val = "";
                if (ln * 2 > str.Length)    // вышли за границы данных
                    return false;
                val = str.Substring(0, ln * 2);
                string k = tg;
                fl = false;
                foreach (string s in sost)
                    if (s == k)
                        fl = true;
                if (fl)
                {
                    if (!ParseString(val, two, sost))
                        return false;
                }
                else
                {
                    while (k.Length < 4)
                        k = "0" + k;
                    if (!sl.ContainsKey(k))
                        sl.Add(k, new tlv(tg, ln, val));
                }
                if (str.Length <= (ln * 2))
                    str = "";
                else
                    str = str.Substring(ln * 2);
            }
            return true;
        }

        public tlv GetValue(string tag)
        {
            if (sl.ContainsKey(tag))
                return sl[tag];
            else
                return null;
        }
        public string GetValue(string tag, Encoding enc)
        {
            if (sl.ContainsKey(tag))
                return enc.GetString(Utils.AHex2Bin(sl[tag].value));
            else
                return "";
        }
    }
    public class IniFile
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        public static extern uint GetPrivateProfileString(string lpAppName, string lpKeyName, string lpDefault, StringBuilder lpReturnedString,
            uint nSize, string lpFileName);
        [DllImport("kernel32.dll")]
        public static extern uint GetPrivateProfileInt(string lpAppName, string lpKeyName, int nDefault, string lpFileName);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        public static extern uint GetPrivateProfileSection(string lpAppName, IntPtr lpReturnedString, uint nSize, string lpFileName);
        public static bool GetPrivateProfileSection(string appName, string fileName, out string[] section)
        {
            section = null;

            if (!System.IO.File.Exists(fileName))
                return false;

            uint MAX_BUFFER = 32767;

            IntPtr pReturnedString = Marshal.AllocCoTaskMem((int)MAX_BUFFER * sizeof(char));

            uint bytesReturned = GetPrivateProfileSection(appName, pReturnedString, MAX_BUFFER, fileName);

            if ((bytesReturned == MAX_BUFFER - 2) || (bytesReturned == 0))
            {
                Marshal.FreeCoTaskMem(pReturnedString);
                return false;
            }

            //bytesReturned -1 to remove trailing \0

            // NOTE: Calling Marshal.PtrToStringAuto(pReturnedString) will 
            //       result in only the first pair being returned
            string returnedString = Marshal.PtrToStringAuto(pReturnedString, (int)(bytesReturned - 1));

            section = returnedString.Split('\0');

            Marshal.FreeCoTaskMem(pReturnedString);
            return true;
        }
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool WritePrivateProfileString(string lpAppName, string lpKeyName, string lpString, string lpFileName);
    }
}
