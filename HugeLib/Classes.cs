using System;
using System.Reflection;
using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Text;

namespace HugeLib
{
    [System.Serializable]
    public class MyException : Exception
    {
        public MyException() { }
        public MyException(string message) : base(message) { }
        public MyException(string message, Exception inner) : base(message, inner) { }
        public MyException(SerializationInfo info, StreamingContext context) : base(info, context) { }

    }
    public class LogClass
    {
        private static object lockLog = new object();
        public static bool ToConsole = false;
        public static int Level = 0;

        public static void WriteToLog(int level, string str, params object[] pars)
        {
            if (level < Level)
                return;
            lock (lockLog)
            {
                DateTime dt = DateTime.Now;
                string strLog = String.Format("{0:HH.mm.ss}:{1:000}\t", dt, dt.Millisecond);
                strLog += String.Format(str, pars);
                if (ToConsole)
                    Console.WriteLine(strLog);
                try
                {
                    System.IO.StreamWriter sw = new System.IO.StreamWriter(string.Format(@"{0}\Logs\{1}_{2:yyMMdd}.log", System.AppDomain.CurrentDomain.BaseDirectory, Assembly.GetEntryAssembly().GetName().Name, dt), true, System.Text.Encoding.GetEncoding(1251));
                    //sw.Write("{0:HH.mm.ss}:{1:000}\t", dt, dt.Millisecond);
                    //sw.WriteLine(str, pars);
                    sw.WriteLine(strLog);
                    sw.Close();
                }
                catch (Exception ex)
                {
                    System.Windows.Forms.MessageBox.Show(ex.Message);
                }
            }
        }

        public static void WriteToLog(string str, params object[] pars)
        {
            WriteToLog(0, str, pars);
        }
        public static void CustomLogFile(string logname, string str)
        {
            lock (lockLog)
            {
                DateTime dt = DateTime.Now;
                try
                {
                    System.IO.StreamWriter sw = new System.IO.StreamWriter(logname, true, System.Text.Encoding.GetEncoding(1251));
                    sw.Write("{0:HH:mm:ss}.{1:000}\t", dt, dt.Millisecond);
                    sw.WriteLine(str);
                    sw.Close();
                }
                catch (Exception ex)
                {
                    System.Windows.Forms.MessageBox.Show(ex.Message);
                }
            }
        }
        public static void WriteToLog(HugeLib.SCard.APDUCommand comm)
        {
            string str = String.Format("SC << {0:X2}{1:X2} {2:X2}{3:X2}", comm.Class, comm.Ins, comm.P1, comm.P2);
            if (comm.Data != null)
            {
                str = String.Format("{0} {1:X2}", str, comm.Data.Length);
                if (comm.Data.Length > 0)
                    str = String.Format("{0} {1}", str, HugeLib.Utils.Bin2AHex(comm.Data));
            }
            else
            {
                if (comm.P3 != 0)
                    str = String.Format("{0} {1:X2}", str, comm.P3);
            }
            str = String.Format("{0} {1:X2}", str, comm.Le);
            WriteToLog(str);
        }
        public static void WriteToLog(HugeLib.SCard.APDUResponse res)
        {
            if (res.Data != null && res.Data.Length > 0)
                WriteToLog("SC >> {0} {1:X2}{2:X2}", HugeLib.Utils.Bin2AHex(res.Data), res.SW1, res.SW2);
            else
                WriteToLog("SC >> {0:X2}{1:X2}", res.SW1, res.SW2);
        }
    }
}
