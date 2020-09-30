using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Security.Cryptography;
using System.Runtime.InteropServices;
using System.Net.Sockets;
using System.Threading;
using System.IO.Ports;
using HugeLib.SCard;
using HugeLib.Crypto;

namespace HugeLib.Scripter
{
    public class Scripter
    {
        #region External functions
        [DllImport("jsrv.dll")]
        internal static extern int CreateCapData(string capName, [In, Out] byte[] sb, [In, Out] ref int cablength);
        [DllImport("kernel32.dll")]
        static extern int GetPrivateProfileInt(string sectionName, string keyName, int defaultValue, string fileName);
        [DllImport("kernel32.dll")]
        static extern int GetPrivateProfileString(string sectionName, string keyName, string defaultValue, StringBuilder res, int size, string fileName);
        [DllImport("kernel32.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        private static extern UInt32 GetPrivateProfileSection([In] [MarshalAs(UnmanagedType.LPStr)] string strSectionName, [In] IntPtr pReturnedString, [In] UInt32 nSize, [In] [MarshalAs(UnmanagedType.LPStr)] string strFileName);
        [DllImport("kernel32.dll")]
        static extern int GetPrivateProfileSection(string sectionName, StringBuilder sb, int size, string filename);
        #endregion
        public Scripter()
        {
            script = new ArrayList();
            r = new Random((int)(DateTime.Now.Ticks - DateTime.Today.Ticks));
            vars = new Hashtable();
            ip = "";
        }
        private string serviceTemp = "";
        private string readerName;
        public string ReaderName
        {
            get
            {
                return readerName;
            }
            set
            {
                readerName = value;
            }
        }
        private string ip;
        public string IP
        {
            set
            {
                ip = value;
            }
        }
        private int port;
        public int Port
        {
            set
            {
                port = value;
            }
        }
        private string rsport;
        public string RSPort
        {
            get
            {
                return rsport;
            }
            set
            {
                Console.WriteLine("!!!!! Port settings hard coded (see Compute HSM)");
                rsport = value;
            }
        }
        private string rssettings;
        public string RSSettings
        {
            get
            {
                return rssettings;
            }
            set
            {
                rssettings = value;
            }
        }
        private string errMessage;
        public string ErrMessage
        {
            get
            {
                return errMessage;
            }
        }
        PROTOCOL pr = PROTOCOL.T0orT1;
        public ArrayList script;
        private CardBase card;
        private dxp01sdk.SCard onewire = null;
        private dxp01sdk.BidiSplWrap bidi = null;
        private Hashtable vars;
        private bool skip = false;
        private bool consoleOutput = true;
        private int codepage = 1251;
        private bool closeReaderOnError = true;
        TcpClient mainTcpClient = null;
        NetworkStream networkStreamMain = null;
        Random r = null;
        APDUResponseType respType = APDUResponseType.Standart;
        public int AddVar(string name, byte[] val)
        {
            if (vars.ContainsKey(name))
                return -1;
            else
                vars.Add(name, val.Clone());
            return 1;
        }
        public int AddVar(string name, string val)
        {
            return AddVar(name, Utils.AHex2Bin(val));
        }
        public string GetVar(string name)
        {
            if (vars.ContainsKey(name))
                return Utils.Bin2AHex((byte[])vars[name]);
            else
                return "";
        }
        public int SetVar(string name, byte[] val)
        {
            if (vars.ContainsKey(name))
                vars[name] = val;
            else
                return -1;
            return 1;
        }
        public int SetVar(string name, string val)
        {
            return SetVar(name, Utils.AHex2Bin(val));
        }
        public void SetCloseErrorOnError(bool val)
        {
            closeReaderOnError = val;
        }

        public bool ParseData(string str)
        {
            TagList tl = new TagList(str);
            if (tl.GoodParse)
            {
                foreach (KeyValuePair<string, tlv> kvp in tl.sl)
                {
                    if (kvp.Key.StartsWith("00"))
                        AddVar(kvp.Key.Substring(2), Utils.AHex2Bin(kvp.Value.value));
                    else
                        AddVar(kvp.Key, Utils.AHex2Bin(kvp.Value.value));
                }
                return true;
            }
            return false;
        }
        public bool LoadScript(string scriptname)
        {
            vars.Clear();
            script.Clear();
            string str = "";
            string[] strs;
            if (!File.Exists(scriptname))
            {
                errMessage = String.Format("script {0} not found...", script);
                return false;
            }
            ShowConsole(String.Format("Open script {0}", scriptname));
            StreamReader sr = new StreamReader(scriptname);
            ScriptSection curSection = ScriptSection.None;
            AddVar("SCRes", new byte[] { });
            AddVar("HSRes", new byte[] { });
            AddVar("Kenc", new byte[] { });
            AddVar("Kmac", new byte[] { });
            AddVar("Kdec", new byte[] { });
            AddVar("Mac", new byte[] { });
            AddVar("SCounter", new byte[] { });
            AddVar("DPrep", new byte[] { });
            while (sr.Peek() >= 0)
            {
                str = sr.ReadLine().Trim();
                if (str.Trim().StartsWith(";"))
                    continue;
                if (curSection == ScriptSection.Codes && str.Trim().Length > 0)
                    script.Add(str);
                if (curSection == ScriptSection.Vars || curSection == ScriptSection.Keys)
                {
                    if (!str.Trim().StartsWith("["))
                    {
                        strs = str.Split('=', ';');
                        if (strs.Length > 0 && strs[0].Trim().Length > 0)
                        {
                            int i = 0;
                            if (strs[0].Trim().ToLower() == "storage")
                                i = AddVar("Storage", Utils.String2Bin(strs[1].Trim()));
                            else
                            {
                                if (strs.Length > 1)
                                    i = AddVar(strs[0].Trim(), Utils.AHex2Bin(strs[1].Trim().ToUpper()));
                                else
                                    i = AddVar(strs[0].Trim(), new byte[] { });
                            }
                            if (i == -1)
                            {
                                errMessage = String.Format("Дублируется переменная или ключ {0}", strs[0]);
                                return false;
                            }
                        }
                    }
                }
                if (str.ToLower().Trim().StartsWith("["))
                    curSection = ScriptSection.None;
                if (str.ToLower().Trim().StartsWith("[reader]"))
                    curSection = ScriptSection.Reader;
                if (str.ToLower().Trim().StartsWith("[keys]"))
                    curSection = ScriptSection.Keys;
                if (str.ToLower().Trim().StartsWith("[codes]"))
                    curSection = ScriptSection.Codes;
                if (str.ToLower().Trim().StartsWith("[vars]"))
                    curSection = ScriptSection.Vars;
                if (str.ToLower().Trim().StartsWith("name="))
                {
                    if (curSection == ScriptSection.Reader)
                        readerName = str.Trim().Substring(5);
                }
            }
            sr.Close();
            sr.Dispose();
            return true;
        }
        public void SetReader(string newReaderName)
        {
            readerName = newReaderName;
        }
        /// <summary>
        /// отработка скрипта
        /// </summary>
        /// <returns>
        ///  1 - ok
        /// -1 - неверное количество частей команды (делятся точками - сейчас должно быть 3 части)
        /// -2 - ошибка смарт-карты
        /// -3 - неизвестная команда
        /// -4 - APDU вернула не 9000 и не 61xx
        /// -5 - ошибка хранилища ключей
        /// -6 - разное
        /// -7 - Ошибка подготовки данных
        /// </returns>
        public int RunScript()
        {
            if (ip.Length > 0)
            {
                mainTcpClient = new TcpClient(ip, port);
                networkStreamMain = mainTcpClient.GetStream();
            }
            int t = 0, n = 0, i = 0;
            APDUResponse res = null;
            APDUCommand comm = null;
            string temp = "", cmac = "";
            byte[] data = null, apdu = null;
            byte le = 0x00, ln = 0x00;
            StringBuilder sb = new StringBuilder(100);
            //foreach (string str in script)
            for(int tt = 0; tt < script.Count; tt++)
            {
                string str = (string)script[tt];
                string[] parse = str.Split('.');
                if (parse[0] == "*/")
                    skip = false;
                if (skip)
                    continue;
                switch (parse[0])
                {
                    case("COMM"):
                        #region add commentary to log
                        ShowConsole(GetDataS(parse[1]));
                        break;
                        #endregion
                    case ("SCOP"):
                        #region open reader
                        try
                        {
                            card = new CardBase();
                            pr = PROTOCOL.T0orT1;
                            if (parse.Length > 1 && parse[1] == "T0")
                                pr = PROTOCOL.T0;
                            if (parse.Length > 1 && parse[1] == "T1")
                                pr = PROTOCOL.T1;
                            if (parse.Length > 1 && parse[1] == "T0T1")
                                pr = PROTOCOL.T0orT1;
                            card.Connect(readerName, SHARE.Shared, pr);
                        }
                        catch (Exception ex)
                        {
                            errMessage = ex.Message;
                            return -2;
                        }
                        ShowConsole(String.Format("Reader opened {0} (Protocol {1})", readerName, pr));
                        temp = Utils.Bin2AHex(card.GetAttribute(SCARD_ATTR_VALUE.ATR_STRING));
                        string ininame = Environment.CurrentDirectory + "\\data.ini";
                        GetPrivateProfileString("ATR", temp, "", sb, 100, ininame);
                        ShowConsole(String.Format("SC ATR={0} {1}", Utils.Bin2AHex(card.GetAttribute(SCARD_ATTR_VALUE.ATR_STRING)), sb.ToString()));
                        break;
                        #endregion
                    case ("OWOP"):
                        #region one wire reader open
                        try
                        {
                            bidi = new dxp01sdk.BidiSplWrap();
                            bidi.BindDevice(readerName);
                            string sssss = bidi.GetPrinterData(dxp01sdk.strings.SDK_VERSION);
                            onewire = new dxp01sdk.SCard(bidi);
                            uint protocol = (uint)dxp01sdk.scard_protocol.SCARD_PROTOCOL_Tx;
                            byte[] ATRBytes = new byte[0];
                            int[] states = new int[0];
                            long scardResult = onewire.SCardConnect(dxp01sdk.SCard.ChipConnection.contact, ref protocol);
                            var scardRes = onewire.SCardStatus(ref states, ref protocol, ref ATRBytes);
                            if (scardRes != 0)
                                Console.WriteLine("SCardStatus result: {0} {1}", scardRes, new System.ComponentModel.Win32Exception((int)scardRes).Message);
                            ShowConsole("SC ATR={0}", Utils.Bin2AHex(ATRBytes));
                        }
                        catch (Exception ex)
                        {
                            errMessage = ex.Message;
                            return -2;
                        }
                        break;
                        #endregion
                    case ("OWEX"):
                        #region one wire transmit command
                        if (parse.Length != 6)
                        {
                            errMessage = String.Format("Неверное количество частей команды. Строка {0}", n + 1);
                            return -1;
                        }
                        if (onewire == null)
                        {
                            errMessage = String.Format("Не создан класс карты. Строка {0}", n + 1);
                            return -2;
                        }
                        apdu = GetDataB(parse[1]);
                        data = null; ln = 0;
                        if (parse.Length > 2 && parse[2].Trim().Length > 0)
                            ln = GetDataB(parse[2])[0];
                        if (parse.Length > 3 && parse[3].Trim().Length > 0)
                            data = GetDataB(parse[3]);
                        if (apdu.Length != 4)
                        {
                            errMessage = String.Format("Неверные параметры apdu. Строка {0}", n + 1);
                            return -3;
                        }
                        le = 0xFF;
                        if (parse[4].Length > 0)
                            le = Utils.AHex2Bin(parse[4])[0];
                        if (data == null)
                            comm = new APDUCommand(apdu[0], apdu[1], apdu[2], apdu[3], ln, le); //для случаев когда в P3 - не длина
                        else
                            comm = new APDUCommand(apdu[0], apdu[1], apdu[2], apdu[3], data, le);
                        ShowConsole(comm);
                        byte[] resp = new byte[255];
                        long l = onewire.SCardTransmit(comm.GetBytes(), ref resp);
                        res = new APDUResponse(resp);
                        //ShowConsole(Utils.Bin2AHex(resp));
                        if (res.SW1 == 0x6C)
                        {
                            comm.SetLe(res.SW2);
                            onewire.SCardTransmit(comm.GetBytes(), ref resp);
                            res = new APDUResponse(resp);
                        }
                        if (res.SW1 == 0x61)
                        {
                            comm = new APDUCommand(0x00, 0xC0, 0x00, 0x00, null, res.SW2);
                            onewire.SCardTransmit(comm.GetBytes(), ref resp);
                            res = new APDUResponse(resp);
                        }
                        ShowConsole(res);
                        vars["SCRes"] = res.Data;
                        break;
                        #endregion
                    case ("OWCL"):
                        #region one wire close reader
                        onewire.SCardDisConnect((int)dxp01sdk.scard_disposition.SCARD_RESET_CARD);
                        bidi.UnbindDevice();
                        Console.WriteLine("One wire mode close");
                        break;
                    #endregion
                    case ("ARTP"):
                        #region apdu response type
                        if (parse.Length < 2)
                        {
                            errMessage = String.Format("Неверное количество частей команды. Строка {0}", n + 1);
                            return -1;
                        }
                        if (parse[1] == "0")
                        {
                            respType = APDUResponseType.Standart;
                            Console.WriteLine("Формат apdu ответа: стандартный");
                        }
                        if (parse[1] == "1")
                        {
                            respType = APDUResponseType.IdentiveMFPlus;
                            Console.WriteLine("Формат apdu ответа: identive mifare plus");
                        }
                        break;
                        #endregion
                    case ("SCEX"):
                        #region smart card reader exchange
                        if (parse.Length != 6)
                        {
                            errMessage = String.Format("Неверное количество частей команды. Строка {0}", n + 1);
                            return -1;
                        }
                        if (card == null)
                        {
                            errMessage = String.Format("Не создан класс карты. Строка {0}", n + 1);
                            return -2;
                        }
                        apdu = GetDataB(parse[1]);
                        data = null; ln = 0;
                        if (parse.Length > 2 && parse[2].Trim().Length > 0)
                            ln = GetDataB(parse[2])[0];
                        if (parse.Length > 3 && parse[3].Trim().Length > 0)
                            data = GetDataB(parse[3]);
                        if (apdu.Length != 4)
                        {
                            errMessage = String.Format("Неверные параметры apdu. Строка {0}", n + 1);
                            return -3;
                        }
                        le = 0xFF;
                        if (parse[4].Length > 0)
                            le = Utils.AHex2Bin(parse[4])[0];
                        if (data == null)
                            comm = new APDUCommand(apdu[0], apdu[1], apdu[2], apdu[3], ln, le); //для случаев когда в P3 - не длина
                        else
                            comm = new APDUCommand(apdu[0], apdu[1], apdu[2], apdu[3], data, le);
                        comm.SetApduResponseType(respType); //установка типа ответа apdu (по умолчанию standart, либо 1байт статуса в начале (identive mifare plus)
                        ShowConsole(comm);
                        res = card.TransmitA(comm);
                        ShowConsole(res);
                        //if (res.SW1 == 0x6C)
                        //{
                        //    comm.SetLe(res.SW2);
                        //    ShowConsole(comm);
                        //    res = card.Transmit(comm);
                        //    ShowConsole(res);
                        //}
                        //if (res.SW1 == 0x61)
                        //{
                        //    comm = new APDUCommand(0x00, 0xC0, 0x00, 0x00, null, res.SW2);
                        //    ShowConsole(comm);
                        //    res = card.Transmit(comm);
                        //    ShowConsole(res);
                        //}
                        if (parse[5] != "**" && res.Status != 0x9000)
                        {
                            errMessage = String.Format("Команда завершена с ошибкой. Строка {0}", n + 1);
                            if (closeReaderOnError)
                            {
                                card.Disconnect(DISCONNECT.Reset);
                                ShowConsole("Закрыт ридер");
                            }
                            return -4;
                        }
                        vars["SCRes"] = res.Data;
                        break;
                        #endregion
                    case ("SCES"):
                        #region smart card reader exchange + security
                        if (parse.Length != 8)
                        {
                            errMessage = String.Format("Неверное количество частей команды. Строка {0}", n + 1);
                            return -1;
                        }
                        if (card == null)
                        {
                            errMessage = String.Format("Не создан класс карты. Строка {0}", n + 1);
                            return -2;
                        }
                        apdu = Utils.AHex2Bin(parse[3]);
                        data = null;
                        if (parse[5].Trim().Length > 0)
                            data = GetDataB(parse[5]);
                        if (apdu.Length != 4)
                        {
                            errMessage = String.Format("Неверные параметры apdu. Строка {0}", n + 1);
                            return -3;
                        }
                        le = 0xFF;
                        if (parse[6].Length > 0)
                            le = Utils.AHex2Bin(parse[6])[0];
                        // secure channel protocol
                        if (parse[1] == "02")
                        {
                            // c-mac
                            cmac = "";
                            if (Utils.BinaryMask(Utils.AHex2Bin(parse[2])[0], "xxxxxxx1"))
                            {
                                t = (data == null) ? 0 : data.Length;
                                temp = String.Format("{0}{1:X2}{2}", parse[3], t+8, Utils.Bin2AHex(data));

                                //((byte[])vars["SCounter"])[1]++;
//                                string sdata = String.Format("0101{0}000000000000000000000000", GetDataS("*SCounter"));
//                                string SKmac = MyCrypto.TripleDES_EncryptData(sdata, GetDataB("*Kmac"), CipherMode.CBC, PaddingMode.None);

                                cmac = MyCrypto.Mac1(temp, GetDataB("*Kmac"), GetDataB("*Mac"));
                                
                                data = (data == null) ? Utils.AHex2Bin(cmac) : Utils.AHex2Bin(Utils.Bin2AHex(data)+cmac);
                                vars["Mac"] = Utils.AHex2Bin(cmac);
                            }
                            // c-encryption
                            if (Utils.BinaryMask(Utils.AHex2Bin(parse[2])[0], "xxxxxx1x"))
                            {
                                if (parse.Length > 5 && parse[5].Trim().Length > 0)
                                    temp = GetDataS(String.Format("#PAD80(?{0})", parse[5]));
                                else
                                    temp = "0080000000000000";
                                string sdata = String.Format("0182{0}000000000000000000000000", GetDataS("*SCounter"));
                                string SKdec = MyCrypto.TripleDES_EncryptData(sdata, GetDataB("*Kenc"), CipherMode.CBC, PaddingMode.None);
                                temp = MyCrypto.TripleDES_EncryptData(temp, GetDataB(SKdec), CipherMode.CBC, PaddingMode.None);
                                data = Utils.AHex2Bin(temp+cmac);
                            }
                        }
                        comm = new APDUCommand(apdu[0], apdu[1], apdu[2], apdu[3], data, le);
                        ShowConsole(comm);
                        res = card.Transmit(comm);
                        ShowConsole(res);
                        if (res.SW1 == 0x6C)
                        {
                            comm.SetLe(res.SW2);
                            ShowConsole(comm);
                            res = card.Transmit(comm);
                            ShowConsole(res);
                        }
                        if (res.SW1 == 0x61)
                        {
                            comm = new APDUCommand(0x00, 0xC0, 0x00, 0x00, null, res.SW2);
                            ShowConsole(comm);
                            res = card.Transmit(comm);
                            ShowConsole(res);
                        }
                        if (parse[7] != "**" && (res.SW1 != 0x90 || res.SW2 != 0x00))
                        {
                            errMessage = String.Format("Команда завершена с ошибкой. Строка {0}", n + 1);
                            card.Disconnect(DISCONNECT.Reset);
                            ShowConsole("Закрыт ридер");
                            return -4;
                        }
                        vars["SCRes"] = res.Data;
                        break;
                    #endregion
                    case ("SCCT"):
                        #region scardcontrol
                        ShowConsole(card.Control(Convert.ToUInt32(parse[1]), GetDataB(parse[2]), 0xFF));
                        break;
                        #endregion
                    case ("SCMC"):
                        #region smart card macro command
                        //if (card == null)
                        //{
                        //    errMessage = String.Format("No card class. Line {0}", n + 1);
                        //    return -2;
                        //}
                        t = MacroFunctions(str);
                        if (t != 1)
                        {
                            errMessage = String.Format("{0}. Line {1}", errMessage, n + 1);
                            return -3;
                        }
                        break;
                        #endregion
                    case ("SETV"):
                        #region set value
                        if ("SCRes, HSRes, Mac, Kenc, Kmac, Kdec".IndexOf(parse[1]) >= 0)
                        {
                            errMessage = "SCRes, HSRes, Mac, Kenc, Kmac, Kdec - зарезервированные имена";
                            return -6;
                        }
                        vars[parse[1]] = GetDataB(parse[2]);
                        bool show = true;
                        if (parse.Length > 3)
                            show = parse[3][0] != 'N';
                        if (show)
                            ShowConsole(String.Format("{0}={1}", parse[1], Utils.Bin2AHex((byte[])vars[parse[1]])));
                        break;
                        #endregion
                    case ("SCCL"):
                        #region close reader
                        if (card == null)
                        {
                            errMessage = String.Format("No card class. Line {0}", n + 1);
                            return -2;
                        }
                        if (parse.Length > 1)
                        {
                            if (parse[1] == "01")
                                card.Disconnect(DISCONNECT.Reset);
                            if (parse[1] == "02")
                                card.Disconnect(DISCONNECT.Unpower);
                        }
                        else
                            card.Disconnect(DISCONNECT.Reset);
                        ShowConsole("Reader closed");
                        break;
                        #endregion
                    case ("HSEX"):
                        #region perform crypto function
                        t = ComputeHSM(str);
                        if (t == 1)
                            ShowConsole(String.Format("HS >> {0}", Utils.Bin2AHex((byte[])vars["HSRes"])));
                        else
                        {
                            errMessage = String.Format("{0}. Строка {1}",errMessage, n + 1);
                            return -3;
                        }
                        break;
                        #endregion
                    case ("PRSE"):
                        #region parse response
                        t = ParseResponse(str);
                        if (t != 1)
                            ShowConsole(String.Format("Ошибка разбора ответа. Строка {1}. {0} Проигнорировано...", errMessage, n + 1));
                        break;
                        #endregion
                    case ("KEYS"):
                        #region key container functions
                        if (!vars.ContainsKey("Storage"))
                        {
                            errMessage = String.Format("Неопределенно имя хранилища ключей (Storage секции [Keys])");
                            return -5;
                        }
                        if (!Directory.Exists(Utils.Bin2String((byte[])vars["Storage"])))
                            Directory.CreateDirectory(Utils.Bin2String((byte[])vars["Storage"]));
                        t = KeyStorage(str);
                        if (t != 1)
                            return -5;
                        break;
                        #endregion
                    case ("DAPR"):
                        #region data preparation
                        t = DataPreparation(str);
                        if (t == 1)
                            ShowConsole(String.Format("DP >> {0}", Utils.Bin2AHex((byte[])vars["DPrep"])));
                        else
                        {
                            errMessage = String.Format("{0}. Строка {1}", errMessage, n + 1);
                            return -7;
                        }
                        break;
                        #endregion
                    case ("RSTR"):
                        #region read string - for making pause
                        Console.ReadKey();
                        break;
                        #endregion
                    case ("TEST"):
                        #region for test purposes
                        //res = card.Test();
                        //ShowConsole(res);
                        byte[] val = GetDataB(parse[1]);
                        SHA1Managed sha1 = new SHA1Managed();
                        SHA256Managed sha256 = new SHA256Managed();
                        SHA384Managed sha384 = new SHA384Managed();
                        SHA512Managed sha512 = new SHA512Managed();
                        MD5 md = MD5.Create();
                        Console.WriteLine(Utils.Bin2AHex(sha1.ComputeHash(val)));
                        Console.WriteLine(Utils.Bin2AHex(sha256.ComputeHash(val)));
                        Console.WriteLine(Utils.Bin2AHex(sha384.ComputeHash(val)));
                        Console.WriteLine(Utils.Bin2AHex(sha512.ComputeHash(val)));
                        Console.WriteLine(Utils.Bin2AHex(md.ComputeHash(val)));
                        break;
                        #endregion
                    case ("/*"):
                        #region start comment code
                        skip = true;
                        break;
                        #endregion                        
                    case ("*/"):
                        #region end comment code
                        skip = false;
                        break;
                        #endregion
                    case ("COFF"):
                        #region console output disable
                        consoleOutput = false; ;
                        break;
                        #endregion
                    case ("CON"):
                        #region console output enable
                        consoleOutput = true;
                        break;
                    #endregion
                    case ("CODEPAGE"):
                        #region установка кодовой страницы для текстовых функций
                        codepage = Convert.ToInt32(parse[1]);
                        ShowConsole("Code page: {0}", codepage);
                        break;
                    #endregion
                    case ("SLP"):
                        #region sleep
                        Thread.Sleep(Convert.ToInt32(parse[1]));
                        break;
                    #endregion
                    case ("RS232"):
                        #region com port
/*                        SerialPort sp = new SerialPort(rsport);
                        // это для verifone
                        sp.BaudRate = 1200;
                        sp.Parity = Parity.Even;
                        sp.DataBits = 7;
                        sp.StopBits = StopBits.One;

                        // это для принтера
                        //sp.BaudRate = 9600;
                        //sp.Parity = Parity.None;
                        //sp.DataBits = 8;
                        //sp.StopBits = StopBits.One;

                        sp.Handshake = Handshake.RequestToSend;
                        sp.ReadTimeout = 3000;
                        sp.Open();
                        b1 = GetDataB(strs[2]);
                        if (strs.Length > 3 && strs[3] == "ECD")
                        {
                            b2 = new byte[b1.Length + 1];
                            b1.CopyTo(b2, 0);
                            b2[b1.Length] = Utils.ArrXor(b1);
                            b1 = new byte[b2.Length];
                            b2.CopyTo(b1, 0);
                        }
                        ShowConsole("RS232 << " + Utils.Bin2AHex(b1));
                        sp.Write(b1, 0, b1.Length);
                        b1 = new byte[1024];
                        t = 1024;
                        try
                        {
                            //                        while (sp.BytesToRead > 0)
                            //                      {
                            Thread.Sleep(5000);
                            t = sp.Read(b1, 0, 1024);
                            //                    }
                        }
                        catch { }
                        res = Utils.Bin2AHex(b1, t);//Encoding.ASCII.GetBytes(str));
                                                    //ShowConsole(Utils.Bin2String(b1));
                        sp.Close();*/
                        break;
                        #endregion
                    default:
                        errMessage = String.Format("Unknown command. Row {0}", n + 1);
                        return -3;
                }
                n++;
            }
            if (mainTcpClient != null)
            {
                networkStreamMain.Close();
                mainTcpClient.Close();
            }
            return 1;
        }
        public void LinkReader(CardBase cb)
        {
            card = cb;
        }
        public void ResetScript()
        {
            vars.Clear();
            script.Clear();
            AddVar("SCRes", new byte[] { });
            AddVar("HSRes", new byte[] { });
            AddVar("Kenc", new byte[] { });
            AddVar("Kmac", new byte[] { });
            AddVar("Kdec", new byte[] { });
            AddVar("sKenc", new byte[] { });
            AddVar("sKmac", new byte[] { });
            AddVar("sKdec", new byte[] { });
            AddVar("Mac", new byte[] { });
            AddVar("SCounter", new byte[] { });
            AddVar("DPrep", new byte[] { });
        }
        public void AddToScript(string str)
        {
            script.Add(str);
        }
        private int MacroFunctions(string str)
        {
            string[] parse = str.Split('.');
            int t = 0, n = 0, i = 0;
            APDUResponse res = null;
            APDUCommand comm = null;
            string temp = "";
            byte[] data = null;
            byte le = 0x00;
            #region Load Applet
            if (parse[1] == "LAPP")
            {
                temp = GetDataS(parse[2]);
                int cnt = 200;
                if (parse.Length > 3 && parse[3].Trim().Length > 0)
                    cnt = Convert.ToInt32(GetDataS(parse[3]));
                le = (parse.Length > 4) ? GetDataB(parse[4])[0] : (byte)0x00;
                t = 0; i = 0;
                data = new byte[cnt];
                while (t + cnt < temp.Length / 2)
                {
                    Array.Copy(Utils.AHex2Bin(temp), t, data, 0, cnt);
                    comm = new APDUCommand(0x80, 0xE8, 0x00, (byte)i, data, le);
                    ShowConsole(comm);
                    res = card.Transmit(comm);
                    ShowConsole(res);
                    t += cnt;
                    i++;
                    if (res.SW1 != 0x90 && res.SW1 != 0x61)
                    {
                        errMessage = String.Format("Команда завершена с ошибкой. Строка {0}", n + 1);
                        card.Disconnect(DISCONNECT.Reset);
                        ShowConsole("Закрыт ридер");
                        return -4;
                    }
                }
                data = new byte[temp.Length / 2 - t];
                Array.Copy(Utils.AHex2Bin(temp), t, data, 0, temp.Length / 2 - t);
                comm = new APDUCommand(0x80, 0xE8, 0x80, (byte)i, data, le);
                ShowConsole(comm);
                res = card.Transmit(comm);
                ShowConsole(res);
                if (res.SW1 != 0x90 && res.SW1 != 0x61)
                {
                    errMessage = String.Format("Команда завершена с ошибкой. Строка {0}", n + 1);
                    card.Disconnect(DISCONNECT.Reset);
                    ShowConsole("Закрыт ридер");
                    return -4;
                }
                return 1;
            }
            #endregion
            #region GetStatus
            if (parse[1] == "GSTA")
            {
                le = (parse.Length > 3) ? GetDataB(parse[3])[0] : (byte)0x00;
                comm = new APDUCommand(0x80, 0xF2, GetDataB(parse[2])[0], 0x00, Utils.AHex2Bin("4F00"), le);
                ShowConsole(comm);
                res = card.Transmit(comm);
                ShowConsole(res);
                temp = Utils.Bin2AHex(res.Data);
                if (res.SW1 == 0x61)
                {
                    comm = new APDUCommand(0x00, 0xC0, 0x00, 0x00, null, res.SW2);
                    ShowConsole(comm);
                    res = card.Transmit(comm);
                    ShowConsole(res);
                    temp += Utils.Bin2AHex(res.Data);
                }
                while (res.SW1 == 0x63 && res.SW2 == 0x10)
                {
                    comm = new APDUCommand(0x80, 0xF2, GetDataB(parse[2])[0], 0x01, Utils.AHex2Bin("4F00"), le);
                    ShowConsole(comm);
                    res = card.Transmit(comm);
                    ShowConsole(res);
                    temp += Utils.Bin2AHex(res.Data);
                    if (res.SW1 == 0x61)
                    {
                        comm = new APDUCommand(0x00, 0xC0, 0x00, 0x00, null, res.SW2);
                        ShowConsole(comm);
                        res = card.Transmit(comm);
                        ShowConsole(res);
                        temp += Utils.Bin2AHex(res.Data);
                    }
                }
                if (res.SW1 != 0x90 && res.SW1 != 0x61)
                {
                    errMessage = String.Format("Команда завершена с ошибкой. Строка {0}", n + 1);
                    card.Disconnect(DISCONNECT.Reset);
                    ShowConsole("Закрыт ридер");
                    return -4;
                }
                vars["SCRes"] = Utils.AHex2Bin(temp);
                return 1;
            }
            #endregion
            #region Search for right KMC
            if (parse[1] == "SGPA") // search global platform authentication
            {
                StringBuilder sb = new StringBuilder(1024);
                string[] kmcs = GetAllKeysInIniFileSection("KMC", Environment.CurrentDirectory + "\\data.ini");
                string[] variants = new string[] { "01.0", "01.1", "01.2", "02.0", "02.1", "02.2"};
                bool find = false;
                foreach (string kmc in kmcs)
                {
                    foreach (string v in variants)
                    {
                        temp = GetDataS(parse[2]);
                        temp = String.Format("HSEX.EXAU.{0}.{1}.{2}.{3}.{4}.{5}", kmc.Split('=')[0],v,temp.Substring(0,20),temp.Substring(24,16), GetDataS(parse[3]), GetDataS(parse[4]));
                        t = ComputeHSM(temp,1);
                        if (serviceTemp == GetDataS(parse[2]).Substring(40,16))
                        {
                            Console.WriteLine("CCard = " + serviceTemp);
                            string sss = "";
                            if (v.Split('.')[1] == "2")
                                sss = " (Visa) ";
                            if (v.Split('.')[1] == "2")
                                sss = " (EMV_CPS) ";
                            Console.WriteLine("KMC = {0}, SCP {1}, Diver {2}{3}", kmc.Split('=')[0], v.Split('.')[0], v.Split('.')[1], sss);
                            find = true;
                            break;
                        }
                    }
                    if (find)
                        break;
                }
                return 1;
            }
            #endregion
            #region Mifare read
            if (parse[1] == "MFRD") //mifare batch read
            {
                //parse: 2 - тип, 3 - начальный сектор, 4 - конечный сектор, 5 - тип ключа, 6 - ключ
                int start = Convert.ToInt32(parse[3]), end = Convert.ToInt32(parse[4]);
                string read = "";
                #region duali
                if (parse[2] == "1") // duali
                {
                    for (i = start; i < end; i++)
                    {
                        comm = new APDUCommand(0xfd, 0x35, (parse[5] == "B") ? (byte)0x04 : (byte)0x00, 0x00, new byte[] { (byte)i }, 0xff);
                        ShowConsole(comm);
                        res = card.TransmitA(comm);
                        ShowConsole(res);
                        if (res.SW1 != 0x90 && res.SW1 != 0x00)
                        {
                            errMessage = String.Format("Command error");
                            card.Disconnect(DISCONNECT.Reset);
                            ShowConsole("Reader closed");
                            return -4;
                        }
                        if ((i+1)%4 != 0)
                            read += Utils.Bin2AHex(res.Data,1,16);
                    }
                }
                #endregion
                #region scm
                if (parse[2] == "2") //scm microsystems
                {
                    comm = new APDUCommand(0xff, 0x82, 0x00, (byte)((parse[5] == "B") ? 0x61 : 0x60), GetDataB(parse[6]), 0xff);
                    ShowConsole(comm);
                    res = card.TransmitA(comm);
                    ShowConsole(res);
                    for (i = start; i < end;i++)
                    {
                        comm = new APDUCommand(0xff, 0x88, 0x00, (byte)i, (byte)((parse[5] == "B") ? 0x61 : 0x60), 0xff);
                        ShowConsole(comm);
                        res = card.TransmitA(comm);
                        ShowConsole(res);
                        comm = new APDUCommand(0xff, 0xb0, 0x00, (byte)i, 0x00, 0xff);
                        ShowConsole(comm);
                        res = card.TransmitA(comm);
                        ShowConsole(res);
                        if ((i + 1) % 4 != 0)
                            read += Utils.Bin2AHex(res.Data, 0, 16);
                    }
                }
                #endregion
                vars["SCRes"] = Utils.AHex2Bin(read);
                ShowConsole("Result: " + read);
                return 1;
            }
            #endregion
            #region Mifare write
            if (parse[1] == "MFWR") //mifare batch write
            {
                int start = Convert.ToInt32(parse[3]), end = Convert.ToInt32(parse[4]);
                data = GetDataB(parse[7]);
                byte[] key = GetDataB(parse[6]);
                byte[] part = null;
                #region duali
                if (parse[2] == "1") // duali
                {
                    part = new byte[17];
                    comm = new APDUCommand(0xfd, 0x2f, (parse[5] == "B") ? (byte)0x04 : (byte)0x00, (byte)i, key, 0xff);
                    ShowConsole(comm);
                    res = card.TransmitA(comm);
                    ShowConsole(res);
                    for (i = start; i <= end && ((i- start) *16)<data.Length; i++)
                    {
                        Array.Clear(part, 0, 17);
                        part[0] = (byte)i;
                        Array.Copy(data, (i - start) * 16, part, 1, (((i- start) * 16) + 16 < data.Length) ? 16 : data.Length - ((i- start) * 16));
                        comm = new APDUCommand(0xfd, 0x37, (parse[5] == "B") ? (byte)0x04 : (byte)0x00, (byte)0x00, part, 0xff);
                        ShowConsole(comm);
                        res = card.TransmitA(comm);
                        ShowConsole(res);
                        if (res.SW1 != 0x90 && res.SW1 != 0x00)
                        {
                            errMessage = String.Format("Command error");
                            card.Disconnect(DISCONNECT.Reset);
                            ShowConsole("Reader closed");
                            return -4;
                        }
                    }
                }
                #endregion
                #region scm
                if (parse[2] == "2") //scm microsystems
                {
                    part = new byte[16];
                    comm = new APDUCommand(0xff, 0x82, 0x00, (byte)((parse[5] == "B") ? 0x61 : 0x60), GetDataB(parse[6]), 0xff);
                    ShowConsole(comm);
                    res = card.TransmitA(comm);
                    ShowConsole(res);
                    for (i = start; i <= end && ((i - start) * 16) < data.Length; i++)
                    {
                        comm = new APDUCommand(0xff, 0x88, 0x00, (byte)i, (byte)((parse[5] == "B") ? 0x61 : 0x60), 0xff);
                        ShowConsole(comm);
                        res = card.TransmitA(comm);
                        ShowConsole(res);
                        Array.Clear(part, 0, 16);
                        Array.Copy(data, (i - start) * 16, part, 0, (((i - start) * 16) + 16 < data.Length) ? 16 : data.Length - ((i - start) * 16));
                        comm = new APDUCommand(0xff, 0xd6, 0x00, (byte)i, part, 0xff);
                        ShowConsole(comm);
                        res = card.TransmitA(comm);
                        ShowConsole(res);
                        if (res.SW1 != 0x90 && res.SW1 != 0x00)
                        {
                            errMessage = String.Format("Command error");
                            card.Disconnect(DISCONNECT.Reset);
                            ShowConsole("Reader closed");
                            return -4;
                        }
                    }
                }
                #endregion
                if (parse[2] == "5") //identive запись данных на карту plus в режиме sl0
                {
                    part = new byte[19];
                    for (i = start; i <= end && ((i - start) * 16) < data.Length; i++)
                    {
                        Array.Clear(part, 0, 19);
                        Array.Copy(data, (i - start) * 16, part, 3, (((i - start) * 16) + 16 < data.Length) ? 16 : data.Length - ((i - start) * 16));
                        part[0] = 0xa8; part[1] = (byte)i; part[2] = 0x00;
                        comm = new APDUCommand(0xff, 0xfe, 0x00, 0x00, part, 0xff);
                        comm.SetApduResponseType(respType);
                        ShowConsole(comm);
                        res = card.TransmitA(comm);
                        ShowConsole(res);
                        if (res.SW1 != 0x90 && res.SW1 != 0x00)
                        {
                            errMessage = String.Format("Command error");
                            card.Disconnect(DISCONNECT.Reset);
                            ShowConsole("Reader closed");
                            return -4;
                        }
                    }
                }
                return 1;
            }
            #endregion
            #region Mifare read key sectors
            if (parse[1] == "MFRK") // mifare batch key sector read
            {
                if (parse[2] == "3") // acr
                {
                    string keys = GetDataS(parse[4]);
                    for (i = 0; i < 16; i++)
                    {
                        //загрузка ключа
                        comm = new APDUCommand(0xff, 0x82, 0x00, 0x20, Utils.AHex2Bin(keys.Substring(i*12, 12)), 0xff);
                        ShowConsole(comm);
                        res = card.TransmitA(comm);
                        ShowConsole(res);
                        if (res.SW1 != 0x90 && res.SW1 != 0x00)
                        {
                            errMessage = String.Format("Command error");
                            card.Disconnect(DISCONNECT.Reset);
                            ShowConsole("Reader closed");
                            return -4;
                        }
                        //аудентификация
                        comm = new APDUCommand(0xff, 0x86, 0x00, 0x00, Utils.AHex2Bin(String.Format("0100{0:X2}{1}20", i*4+3, (parse[3] == "B") ? "61" : "60")), 0xff);
                        ShowConsole(comm);
                        res = card.TransmitA(comm);
                        ShowConsole(res);
                        if (res.SW1 != 0x90 && res.SW1 != 0x00)
                        {
                            errMessage = String.Format("Command error");
                            card.Disconnect(DISCONNECT.Reset);
                            ShowConsole("Reader closed");
                            return -4;
                        }
                        comm = new APDUCommand(0xff, 0xb0, 0x00, (byte)(i*4+3), 0x00, 0x10);
                        ShowConsole(comm);
                        res = card.TransmitA(comm);
                        ShowConsole(res);
                        if (res.SW1 != 0x90 && res.SW1 != 0x00)
                        {
                            errMessage = String.Format("Command error");
                            card.Disconnect(DISCONNECT.Reset);
                            ShowConsole("Reader closed");
                            return -4;
                        }
                    }
                }
                return 1;
            }
            #endregion
            #region Mifare read key sectors
            if (parse[1] == "MFWK") // mifare batch key sector write
            {
                if (parse[2] == "3") // acr
                {
                    string keys = GetDataS(parse[4]);
                    for (i = 0; i < 16; i++)
                    {
                        //загрузка ключа
                        comm = new APDUCommand(0xff, 0x82, 0x00, 0x20, Utils.AHex2Bin(keys.Substring(i * 12, 12)), 0xff);
                        ShowConsole(comm);
                        res = card.TransmitA(comm);
                        ShowConsole(res);
                        if (res.SW1 != 0x90 && res.SW1 != 0x00)
                        {
                            errMessage = String.Format("Command error");
                            card.Disconnect(DISCONNECT.Reset);
                            ShowConsole("Reader closed");
                            return -4;
                        }
                        //аудентификация
                        comm = new APDUCommand(0xff, 0x86, 0x00, 0x00, Utils.AHex2Bin(String.Format("0100{0:X2}{1}20", i * 4 + 3, (parse[3] == "B") ? "61" : "60")), 0xff);
                        ShowConsole(comm);
                        res = card.TransmitA(comm);
                        ShowConsole(res);
                        if (res.SW1 != 0x90 && res.SW1 != 0x00)
                        {
                            errMessage = String.Format("Command error");
                            card.Disconnect(DISCONNECT.Reset);
                            ShowConsole("Reader closed");
                            return -4;
                        }
                        comm = new APDUCommand(0xff, 0xb0, 0x00, (byte)(i * 4 + 3), 0x00, 0x10);
                        ShowConsole(comm);
                        res = card.TransmitA(comm);
                        ShowConsole(res);
                        if (res.SW1 != 0x90 && res.SW1 != 0x00)
                        {
                            errMessage = String.Format("Command error");
                            card.Disconnect(DISCONNECT.Reset);
                            ShowConsole("Reader closed");
                            return -4;
                        }
                    }
                }
                return 1;
            }
            #endregion
            return -1;
        }
        private int ParseResponse(string str)
        {
            string[] strs = str.Split('.');
            string tmp = "";
            byte[] bt;
            int i = 0;
            Console.WriteLine("Parsing:");
            switch (strs[1])
            {
                case("SASD"):
                    #region select aid of security domain
                    tmp = GetDataS(strs[2]);
                    if (!tmp.StartsWith("6F"))
                    {
                        errMessage = "Не найден заголовок 6F.";
                        return -1;
                    }
                    TagList tl = new TagList(tmp);
                    if (tl.GoodParse)
                        foreach (KeyValuePair<string, tlv> kvp in tl.sl)
                        {
//                            tlv t = (tlv)kvp.Value;
                            ShowConsole("\t" + kvp.Value.ToString());
                            if (kvp.Value.tag == "6F")
                            {
                                TagList tl1 = new TagList(kvp.Value.value);
                                if (tl1.GoodParse)
                                {
                                    foreach (KeyValuePair<string, tlv> kvp1 in tl1.sl)
                                    {
                                        ShowConsole("\t\t" + kvp1.Value.ToString());
                                        if (kvp1.Value.tag == "A5")
                                        {
                                            TagList tl2 = new TagList(kvp1.Value.value);
                                            foreach (KeyValuePair<string, tlv> kvp2 in tl2.sl)
                                            {
                                                ShowConsole("\t\t\t" + kvp2.Value.ToString());
                                                if (kvp2.Value.tag == "73")
                                                {
                                                    TagList tl3 = new TagList(kvp2.Value.value);
                                                    foreach (KeyValuePair<string, tlv> kvp3 in tl3.sl)
                                                    {
                                                        if (kvp3.Value.tag == "60")
                                                            ShowConsole("\t\t\t\tопределяет поддержку GP (last 3/4 bytes)");
                                                        if (kvp3.Value.tag == "64")
                                                            ShowConsole("\t\t\t\tопределяет поддержку SCP (last 2 bytes)");
                                                        ShowConsole("\t\t\t\t" + kvp3.Value.ToString());
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    else
                    {
                        errMessage = "Ошибка разбора.";
                        return -1;
                    }
                    break;
                    #endregion
                case ("INUP"):
                    #region initialize update
                    tmp = GetDataS(strs[2]);
                    ShowConsole(String.Format("\t{0}\tkey diversification data", tmp.Substring(0,20)));
                    int scp = Utils.AHex2Bin(tmp.Substring(22,2))[0];
                    ShowConsole(String.Format("\t{0}\tkey version number", tmp.Substring(20,2)));
                    ShowConsole(String.Format("\t{0}\tsecure channel protocol", tmp.Substring(22,2)));
                    if (scp == 1)
                    {
                        ShowConsole(String.Format("\t{0}\tcard challenge", tmp.Substring(24, 16)));
                        ShowConsole(String.Format("\t{0}\tcard cryptogram", tmp.Substring(40, 16)));
                    }
                    if (scp == 2)
                    {
                        ShowConsole(String.Format("\t{0}\tsequence number", tmp.Substring(24,4)));
                        ShowConsole(String.Format("\t{0}\tcard challenge", tmp.Substring(28, 12)));
                        ShowConsole(String.Format("\t{0}\tcard cryptogram", tmp.Substring(40, 16)));
                    }
                    break;
                    #endregion
                case ("CPLC"):
                    #region get data cplc
                    tmp = GetDataS(strs[2]);
                    if (!tmp.StartsWith("9F7F2A"))
                    {
                        ShowConsole("Это не SCPL");
                        break;
                    }
                    ShowConsole(String.Format("\t{0}\t\tic fabricator", tmp.Substring(6,4)));
                    ShowConsole(String.Format("\t{0}\t\tic type", tmp.Substring(10, 4)));
                    ShowConsole(String.Format("\t{0}\t\tos identifier", tmp.Substring(14, 4)));
                    ShowConsole(String.Format("\t{0}\t\tos release date - {1}", tmp.Substring(18, 4), Utils.FormatDateYDDD(tmp.Substring(18, 4))));
                    ShowConsole(String.Format("\t{0}\t\tos release level", tmp.Substring(22, 4)));
                    ShowConsole(String.Format("\t{0}\t\tic fabrication date - {1}", tmp.Substring(26, 4), Utils.FormatDateYDDD(tmp.Substring(26, 4))));
                    ShowConsole(String.Format("\t{0}\tic serial number", tmp.Substring(30, 8)));
                    ShowConsole(String.Format("\t{0}\t\tic batch identifier", tmp.Substring(38, 4)));
                    ShowConsole(String.Format("\t{0}\t\tic module fabricator", tmp.Substring(42, 4)));
                    ShowConsole(String.Format("\t{0}\t\tic module packaging date - {1}", tmp.Substring(46, 4), Utils.FormatDateYDDD(tmp.Substring(46, 4))));
                    ShowConsole(String.Format("\t{0}\t\ticc manufaturer", tmp.Substring(50, 4)));
                    ShowConsole(String.Format("\t{0}\t\tic embedding date - {1}", tmp.Substring(54, 4), Utils.FormatDateYDDD(tmp.Substring(54, 4))));
                    ShowConsole(String.Format("\t{0}\t\tic pre-personaliser", tmp.Substring(58, 4)));
                    ShowConsole(String.Format("\t{0}\t\tic pre-personalisation date - {1}", tmp.Substring(62, 4), Utils.FormatDateYDDD(tmp.Substring(62, 4))));
                    ShowConsole(String.Format("\t{0}\tic pre-personalisation equipment identifier", tmp.Substring(66, 8)));
                    ShowConsole(String.Format("\t{0}\t\tic personaliser", tmp.Substring(74, 4)));
                    ShowConsole(String.Format("\t{0}\t\tic personalisation date - {1}", tmp.Substring(78, 4), Utils.FormatDateYDDD(tmp.Substring(78, 4))));
                    ShowConsole(String.Format("\t{0}\tic personalisation equipment identifier", tmp.Substring(82, 8)));
                    break;
                    #endregion
                case ("GSTA"):
                    #region get status with parameters
                    tmp = GetDataS(strs[2]);
                    int t = 0, ln = 0, tp = 0;
                    if (strs.Length > 3)
                    {
                        tp = (GetDataB(strs[3]))[0];
                        if (tp == 0x80)
                            ShowConsole("Issuer Security Domain only");
                        if (tp == 0x40)
                            ShowConsole("Application and Supplementary Security Domains only");
                        if (tp == 0x20)
                            ShowConsole("Executable Load Files only");
                        if (tp == 0x10)
                            ShowConsole("Executable Load Files and their Executable Modules only");
                    }
                    ShowConsole("AID".PadRight(40)+"State".PadRight(22)+"Priveleges".PadRight(15)+"PossibleName".PadRight(30));
                    while (t < tmp.Length)
                    {
                        ln = Convert.ToInt32(Utils.AHex2Bin(tmp.Substring(t, 2))[0]);
                        t += 2;
                        string status = "";
                        byte val = Utils.AHex2Bin(tmp.Substring(t + ln * 2, 2))[0];
                        if (tp == 0x80) // issuer security domain
                        {
                            if (val == 0x01)
                                status = " (OP_READY)";
                            if (val == 0x07)
                                status = " (INITIALIZED)";
                            if (val == 0x0F)
                                status = " (SECURED)";
                            if (val == 0x7F)
                                status = " (CARD_LOCKED)";
                            if (val == 0xFF)
                                status = " (TERMINATED)";
                        }
                        if (tp == 0x20) // executable load files
                        {
                            if (val == 0x01)
                                status = " (LOADED)";
                        }
                        if (tp == 0x40) // application and supplementary security domains
                        {
                            if (val == 0x03) 
                                status = " (INSTALLED)";
                            if (val == 0x07)
                                status = " (SELECTABLE)";
                            if (val == 0x0F)
                                status = " (PERSONALIZED)";
                            if (Utils.BinaryMask(val, "0xxxx111"))
                                status = " (APPLICATION STATE)";
                            if (Utils.BinaryMask(val, "1xxxxx11"))
                                status = " (LOCKED)";
                        }
                        string priv = "";
                        val = Utils.AHex2Bin(tmp.Substring(t + ln * 2 + 2, 2))[0];
                        if (Utils.BinaryMask(val, "1xxxxxxx"))
                            priv += "Security Domain, ";
                        if (Utils.BinaryMask(val, "11xxxxx0"))
                            priv += " DAP Verification, ";
                        if (Utils.BinaryMask(val, "1x1xxxxx"))
                            priv += "Delegated Management, ";
                        if (Utils.BinaryMask(val, "xxx1xxxx"))
                            priv += "Card Lock, ";
                        if (Utils.BinaryMask(val, "xxxx1xxx"))
                            priv += "Card Terminate, ";
                        if (Utils.BinaryMask(val, "xxxxx1xx"))
                            priv += "Card Reset, ";
                        if (Utils.BinaryMask(val, "xxxxxx1x"))
                            priv += "CVM Management, ";
                        if (Utils.BinaryMask(val, "11xxxxx1"))
                            priv += "Mandated DAP Verification, ";
                        if (priv.Length > 0)
                            priv = " (" + priv.Substring(0, priv.Length - 2) + ")";
                        StringBuilder sb = new StringBuilder(100);
                        string ininame = Environment.CurrentDirectory + "\\data.ini";
                        GetPrivateProfileString("AIDS", tmp.Substring(t, ln * 2), "", sb, 100, ininame);
                        ShowConsole(tmp.Substring(t, ln*2).PadRight(40,'.')+tmp.Substring(t+ln*2,2)+status.PadRight(20)+tmp.Substring(t+ln*2+2,2)+priv.PadRight(13)+sb.ToString());
                        t += ln*2 + 4;
                    }
                    break;
                    #endregion
                case ("ARRB"):
                    #region show array of bytes prepared for transfer to c++/java
                    bt = GetDataB(strs[2]);
                    foreach (byte b in bt)
                        tmp = String.Format("{0},(byte)0x{1:X2}", tmp, b);
                    ShowConsole(tmp);
                    break;
                    #endregion
                case ("NN01"):
                    #region NoName 01 - KeyStorage - read all
                    tmp = GetDataS(strs[2]);
                    int cnt = tmp.Length / (2 * 24);
                    for (i = 0; i < cnt; i++)
                        ShowConsole(String.Format("{0}\t{1}\t{2}", i, tmp.Substring(i*8*2, 8*2), tmp.Substring(cnt*8*2+i*16*2, 16*2)));
                    break;
                    #endregion
                case ("MFAB"):
                    #region parse access bits for mifare card
                    BitArray tb = new BitArray(GetDataB(strs[2]));
                    BitArray ba = new BitArray(24);
                    for (i = 0; i < 8; i++)
                    {
                        ba[i] = tb[7 - i];
                        ba[8+i] = tb[15 - i];
                        ba[16+i] = tb[23 - i];
                    }
                    if (ba[4] == ba[8] || ba[5] == ba[9] || ba[6] == ba[10] || ba[7] == ba[11])
                        ShowConsole("ошибка в С1");
                    if (ba[0] == ba[20] || ba[1] == ba[21] || ba[2] == ba[22] || ba[3] == ba[23])
                        ShowConsole("ошибка в С2");
                    if (ba[12] == ba[16] || ba[13] == ba[17] || ba[14] == ba[18] || ba[15] == ba[19])
                        ShowConsole("ошибка в С3");
                    ShowConsole(String.Format("  0 block - {0} {1} {2}", ba[11] ? "1" : "0", ba[23] ? "1" : "0", ba[19] ? "1" : "0"));
                    ShowConsole(String.Format("  1 block - {0} {1} {2}", ba[10] ? "1" : "0", ba[22] ? "1" : "0", ba[18] ? "1" : "0"));
                    ShowConsole(String.Format("  2 block - {0} {1} {2}", ba[9] ? "1" : "0", ba[21] ? "1" : "0", ba[17] ? "1" : "0"));
                    ShowConsole(String.Format("key block - {0} {1} {2}", ba[8] ? "1" : "0", ba[20] ? "1" : "0", ba[16] ? "1" : "0"));
                    break;
                #endregion
                case ("MFDT"):
                    bt = GetDataB(strs[2]);
                    i = 0;
                    while (i * 16 < bt.Length)
                    {
                        int len = (i * 16 + 16 < bt.Length) ? 16 : bt.Length - i * 16;
                        ShowConsole("{0:X2}: {1}", i, Utils.Bin2String(bt, i * 16, len, 1251));
                        i++;
                    }
                    break;
                case ("BITS"):
                    #region bits
                    BitArray bits = new BitArray(GetDataB(strs[2]));
                    str = "";
                    for (i = 0; i < bits.Count; i++)
                        str += (bits[(8*(i/8))+(8-i%8-1)])?"1":"0";
                    ShowConsole(str);
                    break;
                    #endregion
                case ("AH2B"):
                    #region ascii hex to bin
                    ShowConsole(Utils.Bin2String(GetDataB(strs[2])));
                    break;
                    #endregion
                case ("FILE"):
                    #region save to file
                    StreamWriter sw = new StreamWriter(strs[2], false);
                    bt = GetDataB(strs[3]);
                    sw.BaseStream.Write(bt, 0, bt.Length);
                    sw.Close();
                    break;
                    #endregion
                case ("KEYS"):
                    break;
                default:
                    break;
            }
            return 1;
        }
        private int ComputeHSM(string str)
        {
            return ComputeHSM(str, 0);
        }
        private int ComputeHSM(string str, int par)
        {
            string[] strs = str.Split('.');
            string res = "", temp = "";
            int ret = 1, i = 0;
            byte[] b1, b2;
            CipherMode cm;
            PaddingMode pm;
            switch (strs[1])
            {
                case("TCP"):
                    #region tcp/ip
                    //TcpClient tc = new TcpClient(ip, port);
                    b2 = new byte[0xFFFF];
                    //NetworkStream ns = mainTcpClient.GetStream();
                    b1 = GetDataB(strs[2]);
                    ShowConsole("TCP >> " + Utils.Bin2AHex(b1));
                    networkStreamMain.Write(b1, 0, b1.Length);
                    //b1 = new byte[1024];
                    b1 = new byte[2048];
                    int t = 0;
                    do
                    {
                        Thread.Sleep(1000);
                        i = networkStreamMain.Read(b1, 0, 2048);
                        Array.Copy(b1, 0, b2, t, i);
                        t += i;
                    }
                    while (networkStreamMain.DataAvailable);
                    res = Utils.Bin2AHex(b2,t);
                    //ns.Close();
                    //tc.Close();
                    break;
                    #endregion
                case("COM"):
                    #region com port
                    //!!! парсинг строки настроек пока не сделан
                    SerialPort sp = new SerialPort(rsport);
                    // это для verifone
                    //sp.BaudRate = 1200;
                    //sp.Parity = Parity.Even;
                    //sp.DataBits = 7;
                    //sp.StopBits = StopBits.One;
                    
                    
                    // это для принтера
                    sp.BaudRate = 9600;
                    sp.Parity = Parity.None;
                    sp.DataBits = 8;
                    sp.StopBits = StopBits.One;
                    
                    
                    sp.Handshake = Handshake.RequestToSend;
                    sp.ReadTimeout = 3000;
                    sp.Open();
                    b1 = GetDataB(strs[2]);
                    if (strs.Length > 3 && strs[3] == "ECD")
                    {
                        b2 = new byte[b1.Length + 1];
                        b1.CopyTo(b2, 0);
                        b2[b1.Length] = Utils.ArrXor(b1);
                        b1 = new byte[b2.Length];
                        b2.CopyTo(b1, 0);
                    }
                    ShowConsole("RS232 << " + Utils.Bin2AHex(b1));
                    sp.Write(b1, 0, b1.Length);
                    b1 = new byte[1024];
                    t = 1024;
                    try
                    {
//                        while (sp.BytesToRead > 0)
  //                      {
                            Thread.Sleep(5000);
                            t = sp.Read(b1, 0, 1024);
    //                    }
                    }
                    catch { }
                    res = Utils.Bin2AHex(b1, t);//Encoding.ASCII.GetBytes(str));
                    //ShowConsole(Utils.Bin2String(b1));
                    sp.Close();
                    break;
                    #endregion
                case ("XOR"):
                    #region xor
                    b1 = GetDataB(strs[2]);
                    b2 = GetDataB(strs[3]);
                    for (i = 0; i < b1.Length; i++)
                    {
                        if (i < b2.Length)
                            b1[i] = (byte)(b1[i] ^ b2[i]);
                    }
                    res = Utils.Bin2AHex(b1);
                    break;
                    #endregion
                case ("NOT"):
                    #region not
                    b1 = GetDataB(strs[2]);
                    for (i = 0; i < b1.Length; i++)
                        b1[i] = (byte)(~b1[i]);
                    res = Utils.Bin2AHex(b1);
                    break;
                    #endregion
                case ("DEEC"):
                    #region des encrypt on clear value
                    cm = CipherMode.CBC;
                    pm = PaddingMode.Zeros;
                    temp = GetDataS(strs[2]);
                    if (temp == "CBC")
                        cm = CipherMode.CBC;
                    if (temp == "ECB")
                        cm = CipherMode.ECB;
                    temp = GetDataS(strs[3]);
                    if (temp == "N")
                        pm = PaddingMode.None;
                    if (temp == "Z")
                        pm = PaddingMode.Zeros;
                    res = MyCrypto.DES_EncryptData(GetDataB(strs[5]), GetDataB(strs[4]), cm, pm);
                    break;
                    #endregion
                case ("DEDC"):
                    #region des decrypt on clear value
                    cm = CipherMode.CBC;
                    pm = PaddingMode.Zeros;
                    temp = GetDataS(strs[2]);
                    if (temp == "CBC")
                        cm = CipherMode.CBC;
                    if (temp == "ECB")
                        cm = CipherMode.ECB;
                    temp = GetDataS(strs[3]);
                    if (temp == "N")
                        pm = PaddingMode.None;
                    if (temp == "Z")
                        pm = PaddingMode.Zeros;
                    res = MyCrypto.DES_DecryptData(GetDataB(strs[5]), GetDataB(strs[4]), cm, pm);
                    break;
                    #endregion
                case ("TDEC"):
                    #region triple des encrypt on clear value
                    cm = CipherMode.CBC;
                    pm = PaddingMode.Zeros;
                    temp = GetDataS(strs[2]);
                    if (temp == "CBC")
                        cm = CipherMode.CBC;
                    if (temp == "ECB")
                        cm = CipherMode.ECB;
                    temp = GetDataS(strs[3]);
                    if (temp == "N")
                        pm = PaddingMode.None;
                    if (temp == "Z")
                        pm = PaddingMode.Zeros;
                    if (strs.Length == 7)
                    {
                        byte[] iv = GetDataB(strs[6]);
                        res = MyCrypto.TripleDES_EncryptData(GetDataB(strs[5]), GetDataB(strs[4]), iv, cm, pm);
                    }
                    else
                    {
                        res = MyCrypto.TripleDES_EncryptData(GetDataS(strs[5]), GetDataB(strs[4]), cm, pm);
                    }
                    break;
                    #endregion
                case ("TDDC"):
                    #region triple des decrypt on clear value
                    cm = CipherMode.CBC;
                    pm = PaddingMode.Zeros;
                    temp = GetDataS(strs[2]);
                    if (temp == "CBC")
                        cm = CipherMode.CBC;
                    if (temp == "ECB")
                        cm = CipherMode.ECB;
                    temp = GetDataS(strs[3]);
                    if (temp == "N")
                        pm = PaddingMode.None;
                    if (temp == "Z")
                        pm = PaddingMode.Zeros;
                    if (strs.Length == 7)
                    {
                        byte[] iv = GetDataB(strs[6]);
                        res = MyCrypto.TripleDES_DecryptData(GetDataB(strs[5]), GetDataB(strs[4]), iv, cm, pm);
                    }
                    else
                        res = MyCrypto.TripleDES_DecryptData(GetDataB(strs[5]), GetDataB(strs[4]), cm, pm);
                    break;
                    #endregion
                case ("AEEC"):
                    #region aes encrypt on clear value
                    cm = CipherMode.CBC;
                    pm = PaddingMode.Zeros;
                    temp = GetDataS(strs[2]);
                    if (temp == "CBC")
                        cm = CipherMode.CBC;
                    if (temp == "ECB")
                        cm = CipherMode.ECB;
                    temp = GetDataS(strs[3]);
                    if (temp == "N")
                        pm = PaddingMode.None;
                    if (temp == "Z")
                        pm = PaddingMode.Zeros;
                    if (strs.Length == 7)
                    {
                        byte[] iv = GetDataB(strs[6]);
                        res = MyCrypto.AES_EncryptData(GetDataB(strs[5]), GetDataB(strs[4]), iv, cm, pm);
                    }
                    else
                    {
                        res = MyCrypto.AES_EncryptData(GetDataS(strs[5]), GetDataB(strs[4]), cm, pm);
                    }
                    break;
                #endregion
                case ("AEDC"):
                    #region aes decrypt on clear value
                    cm = CipherMode.CBC;
                    pm = PaddingMode.Zeros;
                    temp = GetDataS(strs[2]);
                    if (temp == "CBC")
                        cm = CipherMode.CBC;
                    if (temp == "ECB")
                        cm = CipherMode.ECB;
                    temp = GetDataS(strs[3]);
                    if (temp == "N")
                        pm = PaddingMode.None;
                    if (temp == "Z")
                        pm = PaddingMode.Zeros;
                    if (strs.Length == 7)
                    {
                        byte[] iv = GetDataB(strs[6]);
                        res = MyCrypto.AES_DecryptData(GetDataB(strs[5]), GetDataB(strs[4]), iv, cm, pm);
                    }
                    else
                        res = MyCrypto.AES_DecryptData(GetDataB(strs[5]), GetDataB(strs[4]), cm, pm);
                    break;
                    #endregion
                case ("GSEC"):
                    #region gost decrypt on clear value
                    cm = CipherMode.CBC;
                    pm = PaddingMode.Zeros;
                    temp = GetDataS(strs[2]);
                    if (temp == "CBC")
                        cm = CipherMode.CBC;
                    if (temp == "ECB")
                        cm = CipherMode.ECB;
                    temp = GetDataS(strs[3]);
                    if (temp == "N")
                        pm = PaddingMode.None;
                    if (temp == "Z")
                        pm = PaddingMode.Zeros;
                    byte[] iv2 = GetDataB(strs[6]);
                    res = MyCrypto.Gost_Encrypt(GetDataB(strs[5]), GetDataS(strs[4]), iv2, cm);
                    break;
                    #endregion
                case ("GSDC"):
                    #region gost decrypt on clear value
                    cm = CipherMode.CBC;
                    pm = PaddingMode.Zeros;
                    temp = GetDataS(strs[2]);
                    if (temp == "CBC")
                        cm = CipherMode.CBC;
                    if (temp == "ECB")
                        cm = CipherMode.ECB;
                    temp = GetDataS(strs[3]);
                    if (temp == "N")
                        pm = PaddingMode.None;
                    if (temp == "Z")
                        pm = PaddingMode.Zeros;
                    byte[] iv1 = GetDataB(strs[6]);
                    res = MyCrypto.Gost_Decrypt(GetDataB(strs[5]), GetDataS(strs[4]), iv1, cm);
                    break;
                    #endregion
                case ("CPHA"):
                    #region CryptoPro hash function
                    res = MyCrypto.CryptoProHash(GetDataB(strs[2]));
                    break;
                    #endregion
                case ("HASH"):
                    #region hash
                    res = MyCrypto.Hash(GetDataB(strs[2]));
                    break;
                    #endregion
                case ("TDDP"):
                    #region triple des decrypt on password
                    cm = CipherMode.CBC;
                    temp = GetDataS(strs[2]);
                    if (temp == "CBC")
                        cm = CipherMode.CBC;
                    if (temp == "ECB")
                        cm = CipherMode.ECB;
                    res = MyCrypto.TripleDES_DecryptData(GetDataB(strs[4]), GetDataS(strs[3]), cm);
                    break;
                    #endregion
                case ("RSDC"):
                    #region rsa decrypt
                    if (!vars.ContainsKey("Storage"))
                    {
                        errMessage = String.Format("Неопределенно имя хранилища ключей (Storage секции [Keys])");
                        return -5;
                    }
                    temp = String.Format("{0}\\{1}.{2}.xml", Utils.Bin2String((byte[])vars["Storage"]), GetDataS(strs[2]), (GetDataS(strs[3]).ToUpper() != "PB") ? "prv" : "pbl");
                    if (!File.Exists(temp))
                    {
                        errMessage = String.Format("Не найден файл ключа {0}", temp);
                        return -5;
                    }
                    res = MyCrypto.RSA_DecryptData(GetDataB(strs[4]), temp);
                    break;
                    #endregion
                case ("RSEC"):
                    #region rsa encrypt
                    break;
                    #endregion
                case ("EXAU"):
                    #region external authentication (SCP01, 02) - global platform
                    // варианты для 3 и gp - попытки сделать gemxpresso, но пока неудачные
                        
                    // пример из документации по futurecard
                    // кмс в скрипте вбить 4755525557414C54455244534F555A41
                    // 000070280104 - card data, 2820208D - sn, FF - version of KMC, 02 - algoritm for Secire Channel Protocol
                    // 0009 - sequence counter, 43BE60D338C0 - card challenge, D3E1256A6EAFAECF - card cryptogram
                    //vars["SCRes"] = Utils.AHex2Bin("0000FFFFFFFFFFFFFF41FF0117983306C048AF50E4E5AF4703C728A3");//
                    //для Futurecard
                    //vars["Rand"] = 0102030405060708
                    //vars["SCRes"] = Utils.AHex2Bin("0000702801042820208DFF02000943BE60D338C0D3E1256A6EAFAECF");
                    //для GemXpresso 211PK
                    //vars["Rand"] = Utils.AHex2Bin("0000000000000000");
                    //vars["SCRes"] = Utils.AHex2Bin("434D10580000124500FC0D01F1B191E0D898B02408B9C58E5BA896CB");

//                    strs[5] = "000000000001A918191D02020004"; //key derivication data
//                    strs[6] = "000461308FD83D76"; //rnd карты (или 2b seq num + rnd карты)
//                    strs[7] = "F64A1248FB736897"; //rnd host

                    string keydata = "";
                    string SKenc, SKmac, SKdec;
                    string sec_type = "00";
                    if (strs.Length >= 9 && strs[8].Length>0)
                        sec_type = GetDataS(strs[8]);
                    // диверсификационные ключи
                    // без диверсификации
                    if (strs[4].Trim().Length == 0 || Convert.ToInt32(strs[4]) == 0)
                    {
                        vars["Kenc"] = GetDataB(strs[2]);
                        vars["Kmac"] = GetDataB(strs[2]);
                        vars["Kdec"] = GetDataB(strs[2]);
                    }
                    else
                    {
                        if (Convert.ToInt32(strs[4]) == 1)
                        {
                            keydata = GetDataS(strs[5]);
                            keydata = String.Format("{0}{1}", keydata.Substring(0,4), keydata.Substring(8,8));
                            temp = String.Format("{0}F001{0}0F01", keydata);
                            vars["Kenc"] = Utils.AHex2Bin(MyCrypto.TripleDES_EncryptData(temp, GetDataB(strs[2]), CipherMode.ECB, PaddingMode.None));
                            temp = String.Format("{0}F002{0}0F02", keydata);
                            vars["Kmac"] = Utils.AHex2Bin(MyCrypto.TripleDES_EncryptData(temp, GetDataB(strs[2]), CipherMode.ECB, PaddingMode.None));
                            temp = String.Format("{0}F003{0}0F03", keydata);
                            vars["Kdec"] = Utils.AHex2Bin(MyCrypto.TripleDES_EncryptData(temp, GetDataB(strs[2]), CipherMode.ECB, PaddingMode.None));
                        }
                        if (Convert.ToInt32(strs[4]) == 2)
                        {
                            keydata = GetDataS(strs[5]);
                            keydata = keydata.Substring(8, 12);
                            temp = String.Format("{0}F001{0}0F01", keydata);
                            vars["Kenc"] = Utils.AHex2Bin(MyCrypto.TripleDES_EncryptData(temp, GetDataB(strs[2]), CipherMode.ECB, PaddingMode.None));
                            temp = String.Format("{0}F002{0}0F02", keydata);
                            vars["Kmac"] = Utils.AHex2Bin(MyCrypto.TripleDES_EncryptData(temp, GetDataB(strs[2]), CipherMode.ECB, PaddingMode.None));
                            temp = String.Format("{0}F003{0}0F03", keydata);
                            vars["Kdec"] = Utils.AHex2Bin(MyCrypto.TripleDES_EncryptData(temp, GetDataB(strs[2]), CipherMode.ECB, PaddingMode.None));
                        }
                        if (Convert.ToInt32(strs[4]) == 3)
                        {
                            keydata = GetDataS(strs[5]);
                            temp = String.Format("FFFF{0}010000000000", keydata.Substring(0,16));
                            vars["Kenc"] = Utils.AHex2Bin(MyCrypto.TripleDES_EncryptData(temp, Utils.AHex2Bin(strs[2]), CipherMode.ECB, PaddingMode.None));
                            temp = String.Format("{0}F002{0}0F02", keydata);
                            vars["Kmac"] = Utils.AHex2Bin(MyCrypto.TripleDES_EncryptData(temp, GetDataB(strs[2]), CipherMode.ECB, PaddingMode.None));
                            temp = String.Format("{0}F003{0}0F03", keydata);
                            vars["Kdec"] = Utils.AHex2Bin(MyCrypto.TripleDES_EncryptData(temp, GetDataB(strs[2]), CipherMode.ECB, PaddingMode.None));
                        }
                    }
                    if (strs[3] == "gp")
                    {
                        // сессионные ключи
                        keydata = String.Format("{0}{1}{2}{3}", GetDataS(strs[7]).Substring(8), GetDataS(strs[8]).Substring(0, 8), GetDataS(strs[7]).Substring(0, 8), GetDataS(strs[8]).Substring(8));
                        SKenc = MyCrypto.TripleDES_EncryptData(keydata, GetDataB("*Kenc"), CipherMode.ECB, PaddingMode.None);
                        SKmac = MyCrypto.TripleDES_EncryptData(keydata, GetDataB("*Kmac"), CipherMode.ECB, PaddingMode.None);
                        byte[] bts = Utils.AHex2Bin(SKenc);
                        for (i = 0; i < bts.Length; i++)
                            bts[i] = (bts[i] % 2 == 0) ? (byte)202 : (byte)45;
                        SKenc = Utils.Bin2AHex(bts);
                        // криптограмма и mac
                        temp = String.Format("{0}{1}", GetDataS(strs[8]), GetDataS(strs[7]));
                        res = MyCrypto.Mac1(temp, Utils.AHex2Bin(SKenc)); // криптограмма карты, padding внутри мака
                        if (par == 0)
                            Console.WriteLine("CCard = " + res);
                        serviceTemp = res;
                        temp = String.Format("{0}{1}", GetDataS(strs[7]), GetDataS(strs[8]));
                        res = MyCrypto.Mac1(temp, Utils.AHex2Bin(SKenc)); // криптограмма хоста, padding внутри мака
                        temp = String.Format("8482{0}0010{1}", sec_type, res);
                        temp = MyCrypto.Mac1(temp, Utils.AHex2Bin(SKmac)); // mac
                        vars["Mac"] = Utils.AHex2Bin(temp);
                        res = String.Format("{0}{1}", res, temp);
                    }
                    if (strs[3] == "01") // Secure Channel Protocol 01
                    {
                        // сессионные ключи
                        keydata = String.Format("{0}{1}{2}{3}", GetDataS(strs[6]).Substring(8), GetDataS(strs[7]).Substring(0, 8), GetDataS(strs[6]).Substring(0, 8), GetDataS(strs[7]).Substring(8));
                        SKenc = MyCrypto.TripleDES_EncryptData(keydata, GetDataB("*Kenc"), CipherMode.ECB, PaddingMode.None);
                        SKmac = MyCrypto.TripleDES_EncryptData(keydata, GetDataB("*Kmac"), CipherMode.ECB, PaddingMode.None);
                        SKdec = MyCrypto.TripleDES_EncryptData(keydata, GetDataB("*Kdec"), CipherMode.ECB, PaddingMode.None);

                        vars["sKenc"] = Utils.AHex2Bin(SKenc);
                        vars["sKmac"] = Utils.AHex2Bin(SKmac);
                        //vars["Kdec"] = Utils.AHex2Bin(SKdec); //- сессионным не делается
                        // криптограмма и mac
                        temp = String.Format("{0}{1}", GetDataS(strs[7]), GetDataS(strs[6]));
                        res = MyCrypto.Mac1(temp, Utils.AHex2Bin(SKenc)); // криптограмма карты, padding внутри мака
                        if (par == 0)
                            Console.WriteLine("CCard = " + res);
                        serviceTemp = res; //сохраняем, чтобы сравнивать в случае перебора в макрокоманде
                        temp = String.Format("{0}{1}", GetDataS(strs[6]), GetDataS(strs[7]));
                        res = MyCrypto.Mac1(temp, Utils.AHex2Bin(SKenc)); // криптограмма хоста, padding внутри мака
                        temp = String.Format("8482{0}0010{1}", sec_type, res);
                        temp = MyCrypto.Mac1(temp, Utils.AHex2Bin(SKmac)); // mac
                        vars["Mac"] = Utils.AHex2Bin(temp);
                        res = String.Format("{0}{1}", res, temp);
                    }
                    if (strs[3] == "02") // Secure Channel Protocol 02
                    {
                        // сессионные ключи
                        temp = String.Format("0182{0}000000000000000000000000", GetDataS(strs[6]).Substring(0,4));
                        SKenc = MyCrypto.TripleDES_EncryptData(temp, GetDataB("*Kenc"), CipherMode.CBC, PaddingMode.None);
                        temp = String.Format("0101{0}000000000000000000000000", GetDataS(strs[6]).Substring(0,4));
                        SKmac = MyCrypto.TripleDES_EncryptData(temp, GetDataB("*Kmac"), CipherMode.CBC, PaddingMode.None);
                        temp = String.Format("0181{0}000000000000000000000000", GetDataS(strs[6]).Substring(0,4));
                        SKdec = MyCrypto.TripleDES_EncryptData(temp, GetDataB("*Kdec"), CipherMode.CBC, PaddingMode.None);

                        vars["sKenc"] = Utils.AHex2Bin(SKenc);
                        vars["sKmac"] = Utils.AHex2Bin(SKmac);
                        vars["sKdec"] = Utils.AHex2Bin(SKdec);
                        vars["SCounter"] = Utils.AHex2Bin(GetDataS(strs[6]).Substring(0, 4));
                        // криптограмма и mac
                        temp = String.Format("{0}{1}", GetDataS(strs[7]), GetDataS(strs[6]));
                        temp = MyCrypto.Mac1(temp, Utils.AHex2Bin(SKenc)); //криптограмма карты
                        if (par == 0)
                            Console.WriteLine("CCard = " + temp);
                        serviceTemp = temp;
                        temp = String.Format("{0}{1}", GetDataS(strs[6]), GetDataS(strs[7]));
                        res = MyCrypto.Mac1(temp, Utils.AHex2Bin(SKenc)); // криптограмма хоста, padding внутри мака
                        temp = String.Format("8482{0}0010{1}", sec_type, res);
//                        ShowConsole("temp = " + temp);
//                        ShowConsole("SKmac = " + SKmac);
                        temp = MyCrypto.Mac2(temp, Utils.AHex2Bin(SKmac)); // mac, padding внутри
                        vars["Mac"] = Utils.AHex2Bin(temp);
                        res = String.Format("{0}{1}", res, temp);
                    }
                    break;
                    #endregion
                case ("D8RM"):
                    #region begin r-mac session (c-mac + r-mac) - optelio d8
                    string SKcmac, SKrmac;
                    string ivc, ivr;
                    string half1;
                    int sqc = Utils.GetInt(GetDataB(strs[5])[0], GetDataB(strs[5])[1]);
                    // без диверсификации
                    if (strs[3].Length == 0 || Convert.ToInt32(strs[3]) == 0)
                    {
                        vars["Kmac"] = GetDataB(strs[2]);
                        vars["Kdec"] = GetDataB(strs[2]);
                    }
                    else
                    {
                        if (Convert.ToInt32(strs[3]) == 1)
                        {
                            temp = String.Format("{0}F002", GetDataS(strs[4]));
                            half1 = MyCrypto.TripleDES_EncryptData(temp, GetDataB(strs[2]), CipherMode.CBC, PaddingMode.None);
                            temp = MyCrypto.Xor(temp, "000000000000FF00");
                            temp = MyCrypto.Xor(half1, temp);
                            temp = MyCrypto.TripleDES_EncryptData(temp, GetDataB(strs[2]), CipherMode.CBC, PaddingMode.None);
                            vars["Kmac"] = Utils.AHex2Bin(half1+temp);
                            temp = String.Format("{0}F003", GetDataS(strs[4]));
                            half1 = MyCrypto.TripleDES_EncryptData(temp, GetDataB(strs[2]), CipherMode.CBC, PaddingMode.None);
                            temp = MyCrypto.Xor(temp, "000000000000FF00");
                            temp = MyCrypto.Xor(half1, temp);
                            temp = MyCrypto.TripleDES_EncryptData(temp, GetDataB(strs[2]), CipherMode.CBC, PaddingMode.None);
                            vars["Kdec"] = Utils.AHex2Bin(half1+temp);
                        }
                    }
                    temp = String.Format("0101{0:X4}000000000000000000000000", sqc);
                    SKcmac = MyCrypto.TripleDES_EncryptData(temp, (byte[])vars["Kmac"], CipherMode.CBC, PaddingMode.None);
                    sqc++;
                    temp = String.Format("0102{0:X4}000000000000000000000000", sqc);
                    SKrmac = MyCrypto.TripleDES_EncryptData(temp, (byte[])vars["Kmac"], CipherMode.CBC, PaddingMode.None);
                    ivc = MyCrypto.Mac1("A00000003001", Utils.AHex2Bin(SKcmac));
                    ivr = MyCrypto.Mac1("A00000003001", Utils.AHex2Bin(SKrmac));
                    res = MyCrypto.Mac2(String.Format("807A0001{0:X2}{1}", GetDataB(strs[6]).Length, GetDataS(strs[6])), Utils.AHex2Bin(SKcmac), Utils.AHex2Bin(ivc));
                    temp = MyCrypto.Mac2(String.Format("807A0001{0:X2}{1}009000", GetDataB(strs[6]).Length, GetDataS(strs[6])), Utils.AHex2Bin(SKrmac), Utils.AHex2Bin(ivr));
                    res += temp;
                    break;
                    #endregion
                case ("MAC1"):
                    #region Mac1 - Full Triple Des
                    if (strs.Length == 4)
                        res = MyCrypto.Mac1(GetDataS(strs[3]), GetDataB(strs[2]));
                    if (strs.Length == 5)
                        res = MyCrypto.Mac1(GetDataS(strs[3]), GetDataB(strs[2]), GetDataB(strs[4]));
                    break;
                #endregion
                case ("MAC1n"):
                    #region Mac1n - Full Triple Des без паддинга
                    if (strs.Length == 4)
                        res = MyCrypto.Mac1n(GetDataS(strs[3]), GetDataB(strs[2]));
                    if (strs.Length == 5)
                        res = MyCrypto.Mac1n(GetDataS(strs[3]), GetDataB(strs[2]), GetDataB(strs[4]));
                    break;
                #endregion
                case ("MAC2"):
                    #region Mac2 - Single Des + Final Triple Des
                    if (strs.Length == 4)
                        res = MyCrypto.Mac2(GetDataS(strs[3]), GetDataB(strs[2]));
                    if (strs.Length == 5)
                        res = MyCrypto.Mac2(GetDataS(strs[3]), GetDataB(strs[2]), GetDataB(strs[4]));
                    break;
                    #endregion
                case ("APAR"):
                    #region adjust parity
                    res = MyCrypto.AdjustParity(GetDataS(strs[2]));
                    break;
                    #endregion
                case ("CRC16C"):
                    #region compute CRC16 
                    res = MyCrypto.CRC16C(GetDataS(strs[2]), MyCrypto.InitialCrcValue.Zeros);
                    break;
                #endregion
                case ("CRC8"):
                    #region compute CRC16
                    // надо еще сделать поддержку выбора через скрипт типа полинома, начальной инициализации и надо ли переворачивать байты
                    res = MyCrypto.CRC8(GetDataB(strs[2]), MyCrypto.CRC8_POLY.CRC8_SAE_J1850);
                    break;
                    #endregion
                case ("LRC"):
                    #region compute LRC
                    res = MyCrypto.LRC(GetDataB(strs[2]));
                    break;
                #endregion
                case ("SHA"):
                    #region sha
                   break;
                    #endregion
                default:
                    errMessage = "Неизвестная команда HSEX";
                    ret = -1;
                    break;
            }
            vars["HSRes"] = Utils.AHex2Bin(res);
            return ret;
        }
        private int DataPreparation(string str)
        {
            string[] strs = str.Split('.');
            string res = "", temp = "";
            int ret = 1;
            switch (strs[1])
            {
                case("VINP"):
                    #region visa inp file
                    if (!vars.ContainsKey("Storage"))
                    {
                        errMessage = String.Format("Неопределенно имя хранилища ключей (Storage секции [Keys])");
                        return -5;
                    }
                    temp = String.Format("{0}\\{1}.pbl.xml", Utils.Bin2String((byte[])vars["Storage"]), GetDataS(strs[2]));
                    if (!File.Exists(temp))
                    {
                        errMessage = String.Format("Не найден файл ключа {0}", temp);
                        return -5;
                    }
                    RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
                    rsa.FromXmlString(File.ReadAllText(temp));
                    RSAParameters rsaP = rsa.ExportParameters(false);
                    rsa.Clear();
                    break;
                    #endregion
                case("PVV"):
                    #region pvv
                    temp = String.Format("{0}{1}{2}", GetDataS(strs[3]).Substring(4, 11), Convert.ToInt32(GetDataS(strs[4])), GetDataS(GetDataS(strs[5])).Substring(0,4));
                    temp = MyCrypto.TripleDES_EncryptData(temp, GetDataB(strs[2]), CipherMode.ECB, PaddingMode.None);
                    for (int i = 0; i < temp.Length; i++)
                    {
                        if (Char.IsDigit(temp[i]))
                            res += temp[i];
                        if (res.Length == 4)
                            break;
                    }
                    if (res.Length < 4)
                    {
                        for (int i = 0; i < temp.Length; i++)
                        {
                            if (Char.IsLetter(temp[i]))
                            {
                                res += String.Format("{0}", temp[i] - 41);
                            }
                        }
                    }
                    break;
                    #endregion
                default:
                    errMessage = "Неизвестная команда DAPR";
                    ret = -1;
                    break;
            }
            vars["DPrep"] = Utils.AHex2Bin(res);
            return ret;
        }
        private int KeyStorage(string str)
        {
            string[] strs = str.Split('.');
            string fname = Utils.Bin2String((byte[])vars["Storage"]);
            RSACryptoServiceProvider rsa = null;
            RSAParameters rsap;
            CspParameters csp = null;
            int ret = 1;

            switch (strs[1])
            {
                case ("GRSA"):
                    #region generate rsa key-pair and save it
//                    if (File.Exists(String.Format("{0}\\{1}.prv.xml", fname, strs[3])) || File.Exists(String.Format("{0}\\{1}.pbl.xml", fname, strs[3])))
  //                  {
    //                    errMessage = String.Format("Ключевые файлы {0} уже существуют в хранилище {1}", strs[3], fname);
      //                  ret = -1;
        //                break;
          //          }
                    
                    
                    csp = new CspParameters();
                    csp.KeyContainerName = fname;
                    csp.ProviderType = MyCrypto.PROV_RSA_FULL;
                    csp.KeyNumber = MyCrypto.AT_KEYEXCHANGE;
                    rsa = new RSACryptoServiceProvider(Convert.ToInt32(strs[2]), csp);
                    rsap = new RSAParameters();
                    rsap.Exponent = new byte[] { 1,1 };

  //                  rsa = new RSACryptoServiceProvider(csp);
    //                rsa.PersistKeyInCsp = false;
                    rsa.ImportParameters(rsap);
                    //File.WriteAllText(String.Format("{0}\\{1}.prv.xml", fname, strs[3]), rsa.ToXmlString(true));
                    //File.WriteAllText(String.Format("{0}\\{1}.pbl.xml", fname, strs[3]), rsa.ToXmlString(false));
                    ShowConsole(String.Format("KEYS << Generate key {0}", strs[3]));
                    rsap = rsa.ExportParameters(true);
                    ShowConsole(String.Format("KEYS <<   P: {0}", Utils.Bin2AHex(rsap.P)));
                    ShowConsole(String.Format("KEYS <<   Q: {0}", Utils.Bin2AHex(rsap.Q)));
                    ShowConsole(String.Format("KEYS <<  dP: {0}", Utils.Bin2AHex(rsap.DP)));
                    ShowConsole(String.Format("KEYS <<  dQ: {0}", Utils.Bin2AHex(rsap.DQ)));
                    ShowConsole(String.Format("KEYS << Q-1: {0}", Utils.Bin2AHex(rsap.InverseQ)));
                    ShowConsole(String.Format("KEYS << PbE: {0}", Utils.Bin2AHex(rsap.Exponent)));
                    ShowConsole(String.Format("KEYS << Mod: {0}", Utils.Bin2AHex(rsap.Modulus)));
                    ShowConsole(String.Format("KEYS << PrE: {0}", Utils.Bin2AHex(rsap.D)));

                    rsa.Clear();
                    break;
                    #endregion
                case ("SRSA"):
                    #region save public or private part of rsa key-pair
                    if (strs[2] == "PB")
                    {
                        if (File.Exists(String.Format("{0}\\{1}.pbl.xml", fname, strs[3])))
                        {
                            errMessage = String.Format("Ключевой файл {0} публичного ключа уже существует в хранилище {1}", strs[3], fname);
                            ret = -1;
                            break;
                        }
                        rsap = new RSAParameters();
                        rsap.Modulus = GetDataB(strs[4]);
                        rsap.Exponent = GetDataB(strs[5]);
                        rsa = new RSACryptoServiceProvider();
                        rsa.PersistKeyInCsp = false;
                        rsa.ImportParameters(rsap);
                        fname = String.Format("{0}\\{1}.pbl.xml", Utils.Bin2String((byte[])vars["Storage"]), strs[3]);
                        File.WriteAllText(fname, rsa.ToXmlString(false));
                        rsa.Clear();
                        ShowConsole(String.Format("KEYS << Public key {0} saved", strs[3]));
                    }
                    break;
                    #endregion
            }
            return ret;
        } 
        private string GetDataS(string str)
        {
            str = str.Trim();
            string res = "", temp = "";
            if (str.Length == 0)
                return res;
            int n1, n2, i;
            string[] strs;
            switch (str[0])
            {
                case('*'):
                    #region get value of variable
                    if (str.IndexOf(',') > 0)
                        str = str.Substring(0, str.IndexOf(','));
                    if (vars.ContainsKey(str.Replace("*", "")))
                        res = Utils.Bin2AHex((byte[])vars[str.Replace("*", "")]);
                    else
                    {
                        ShowConsole("");
                        ShowConsole("!!!!!!!!!!Error: no such variable " + str);
                        ShowConsole("");
                        return "";
                    }
                    break;
                    #endregion
                case ('#'):
                    #region special functions
                    //сперва вырезаем что внутри скобок
                    n1 = str.IndexOf('(');
                    n2 = str.LastIndexOf(')');
                    temp = str.Substring(n1 + 1, n2 - n1 - 1);
                    #region #CUT - вырезание по индексу
                    if (str.StartsWith("#CUT"))
                    {
                        strs = temp.Split(',');
                        if (strs.Length != 3)
                            return "";
                        if (strs[2].Length > 0)
                            res = GetDataS(strs[0]).Substring(Convert.ToInt32(strs[1])*2, Convert.ToInt32(strs[2])*2);
                        else
                            res = GetDataS(strs[0]).Substring(Convert.ToInt32(strs[1])*2);
                    }
                    #endregion
                    #region #SCUT - вырезание по строке
                    if (str.StartsWith("#SCUT"))
                    {
                        strs = temp.Split(',');
                        if (strs.Length != 3)
                            return "";
                        res = Utils.Bin2String(GetDataB(strs[0]));
                        n1 = res.IndexOf(strs[1]);
                        if (n1 == -1)
                            n1 = 0;
                        n2 = res.IndexOf(strs[2],n1);
                        if (n2 > n1)
                            res = res.Substring(n1 + strs[1].Length, n2 - n1 - strs[1].Length);
                        else
                            res = res.Substring(n1 + strs[1].Length);
                        res = Utils.Bin2AHex(Utils.String2Bin(res));
                    }
                    #endregion
                    #region #SPL - split
                    if (str.StartsWith("#SPL"))
                    {
                        strs = temp.Split(',');
                        if (strs.Length != 3)
                            return "";
                        res = Utils.Bin2String(GetDataB(strs[0]));
                        string[] mmm = res.Split(new string[] { Utils.Bin2String(GetDataB(strs[1])) }, StringSplitOptions.None);
                        int index = Convert.ToInt32(strs[2]);
                        if (mmm.Length > index)
                            res = mmm[index];
                        else
                            res = "";
                    }
                    #endregion
                    #region #TRIM - trim
                    if (str.StartsWith("#TRIM"))
                    {
                        temp = GetDataS(temp);
                        temp = Utils.Bin2String(Utils.AHex2Bin(temp));
                        temp = temp.Trim();
                        res = Utils.Bin2AHex(Utils.String2Bin(temp));
                    }
                    #endregion
                    #region #SUBS - подстрока
                    if (str.StartsWith("#SUBS"))
                    {
                        strs = temp.Split(',');
                        if (strs.Length != 3)
                            return "";
                        if (strs[2].Length > 0)
                            res = GetDataS(strs[0]).Substring(Convert.ToInt32(strs[1]), Convert.ToInt32(strs[2]));
                        else
                            res = GetDataS(strs[0]).Substring(Convert.ToInt32(strs[1]));
                    }
                    #endregion
                    #region #RND - случайное строка заданной длины
                    if (str.StartsWith("#RND"))
                    {
                        n2 = Convert.ToInt32(temp);
                        byte[] bytes = new byte[n2];
                        r.NextBytes(bytes);
                        res = Utils.Bin2AHex(bytes);
                    }
                    #endregion
                    #region #LCAP - формирование кода аплета
                    if (str.StartsWith("#LCAP"))
                    {
                        strs = temp.Split(',');
                        if (strs.Length != 2)
                            return "";
                        int cnt = Convert.ToInt32(strs[1]);
                        byte[] bt = new byte[cnt];
                        CreateCapData(strs[0].Replace('#','.'), bt, ref cnt);
                        ShowConsole("LoadCapData: " + cnt.ToString() + " bytes");
                        res = Utils.Bin2AHex(bt).Substring(0, cnt * 2);
                    }
                    #endregion
                    #region #LAPP - формирование кода аплета, новый вариант
                    if (str.StartsWith("#LAPP"))
                    {
                        strs = temp.Split(',');
                        if (strs.Length != 2)
                            return "";
                        int cnt = Convert.ToInt32(strs[1]);
                        byte[] bt = new byte[cnt];
                        res = JSrv2.JApplet.CreateCapData(strs[0].Replace('#', '.'));
                        ShowConsole($"LoadCapData: {res.Length/2} bytes");
                    }
                    #endregion
                    #region #PAD80 - добавление паддинга
                    if (str.StartsWith("#PAD80"))
                    {
                        temp = GetDataS(temp);
                        temp += "80";
                        if (temp.Length % 2 != 0)
                            return "";
                        while (temp.Length % 16 != 0)
                            temp += "00";
                        res = temp;
                    }
                    #endregion
                    #region #PADI - padding
                    if (str.StartsWith("#PADI"))
                    {
                        strs = temp.Split(',');
                        if (strs.Length != 3)
                            return "";
                        res = GetDataS(strs[0]);
                        i = Convert.ToInt32(strs[1]);
                        while (res.Length < i*2)
                            res += strs[2];
                    }
                    #endregion
                    #region #ASCII - строка -> AHEX
                    if (str.StartsWith("#ASCII"))
                    {
                        temp = GetDataS(temp);
                        res = Utils.Bin2AHex(Utils.String2Bin(temp));
                    }
                    #endregion
                    #region #866 - строка кодировки 866 -> AHEX
                    if (str.StartsWith("#866"))
                    {
                        temp = GetDataS(temp);
                        res = Utils.Bin2AHex(Utils.String2Bin(temp,866));
                    }
                    #endregion
                    #region #TEXT - Str -> AHEX в произвольной кодировке
                    if (str.StartsWith("#TEXT"))
                    {
                        strs = temp.Split(',');
                        if (strs.Length != 2)
                            return "";
                        res = Utils.Bin2AHex(Utils.String2Bin(GetDataS(strs[0]), Convert.ToInt32(strs[1])));
                    }
                    #endregion
                    #region #BASE64D - декодировать base64
                    if (str.StartsWith("#BASE64D"))
                    {
                        temp = GetDataS(temp);
                        res =Utils.Bin2AHex(Convert.FromBase64String(temp));
                        break;
                    }
                    #endregion
                    #region #BASE64E - кодировать base64
                    if (str.StartsWith("#BASE64E"))
                    {
                        temp = GetDataS(temp);
                        res = Convert.ToBase64String(Utils.AHex2Bin(temp));
                        break;
                    }
                    #endregion
                    #region #ATR
                    if (str.StartsWith("#ATR"))
                    {
                        res = Utils.Bin2AHex(card.GetAttribute(SCARD_ATTR_VALUE.ATR_STRING));
                    }
                    #endregion
                    #region #LUHN - подсчет алгоритма Луна
                    if (str.StartsWith("#LUHN"))
                    {
                        temp = GetDataS(temp);
                        int sum = 0;
                        for (i = 0; i < temp.Length; i++)
                        {
                            if (Char.IsDigit(temp[temp.Length - i - 1]))
                            {
                                int p = Convert.ToInt32(temp.Substring(temp.Length - i- 1, 1));
                                if (i % 2 == 0)
                                {
                                    p *= 2;
                                    p = ( p > 9) ? p-9 : p;
                                }
                                sum += p;
                            }
                            else
                                return "";
                        }
                        sum = (10 - (sum % 10)) % 10;
                        return temp+sum.ToString();
                    }
                    #endregion
                    #region #STR - AHEX -> строка
                    if (str.StartsWith("#STR"))
                    {
                        temp = GetDataS(temp);
                        strs = temp.Split(',');
                        if (strs.Length > 1)
                            res = Utils.Bin2String(Utils.AHex2Bin(strs[0]), Convert.ToInt32(strs[1]));
                        else
                            res = Utils.Bin2String(Utils.AHex2Bin(temp));
                    }
                    #endregion
                    #region #GPWD - формирование пароля по строке
                    if (str.StartsWith("#GPWD"))
                    {
                        temp = GetDataS(temp);
                        PasswordDeriveBytes pdb = new PasswordDeriveBytes(temp, null);
                        byte[] bt = pdb.GetBytes(16);
                        res = Utils.Bin2AHex(bt);
                        Console.WriteLine("Key = {0}", res);
                    }
                    #endregion
                    #region #FILE - считывание бинарного файла
                    if (str.StartsWith("#FILE"))
                    {
                        temp = GetDataS(temp);
                        StreamReader sr = new StreamReader(temp.Replace('#','.'));
                        BinaryReader br = new BinaryReader(sr.BaseStream);
                        br.BaseStream.Seek(0, SeekOrigin.Begin);
                        byte[] bts = br.ReadBytes((int)br.BaseStream.Length);
                        br.Close();
                        sr.Close();
                        res = Utils.Bin2AHex(bts);
                    }
                    #endregion
                    #region #LEN - подсчет длины
                    if (str.StartsWith("#LEN"))
                    {
                        temp = GetDataS(temp);
                        res = String.Format("{0:X}", temp.Length / 2);
                    }
                    #endregion
                    #region #BITS - преобразование битов в байты
                    if (str.StartsWith("#BITS"))
                    {
                        int r = 0;
                        for (i = 0; i < temp.Length; i++)
                            if (temp[i] == '1')
                                r |= 1 << temp.Length - i - 1;
                        res = Utils.Bin2AHex(BitConverter.GetBytes(r)).Substring(0,2);
                    }
                    #endregion
                    #region rotate left
                    if (str.StartsWith("#RTL"))
                    {
                        strs = temp.Split(',');
                        if (strs.Length > 1)
                            res = Utils.RotateLeft(GetDataB(strs[0]), Convert.ToInt32(strs[1]));
                        else
                            res = Utils.RotateLeft(GetDataB(strs[0]), 1);
                    }
                    #endregion
                    #region rotate right
                    if (str.StartsWith("#RTR"))
                    {
                        strs = temp.Split(',');
                        if (strs.Length > 1)
                            res = Utils.RotateRight(GetDataB(strs[0]), Convert.ToInt32(strs[1]));
                        else
                            res = Utils.RotateRight(GetDataB(strs[0]), 1);
                    }
                    #endregion
                    #region if
                    if (str.StartsWith("#IF"))
                    {
                        strs = temp.Split(',');
                        string left = GetDataS(strs[0]);
                        string right = GetDataS(strs[1]);
                        if (left == right)
                            res = GetDataS(strs[2]);
                        else
                            res = GetDataS(strs[3]);
                    }
                    #endregion
                    break;
                    #endregion
                case ('['):
                    #region concatinate
                    n1 = str.IndexOf('[');
                    n2 = str.LastIndexOf(']');
                    temp = str.Substring(n1 + 1, n2 - n1 - 1);
                    i = 0;
                    while (i < temp.Length)
                    {
                        n1 = temp.IndexOf(',', i);
                        n2 = temp.IndexOf('(', i);
                        if (n2 < n1 && n2 >= 0)
                        {
                            n2 = temp.IndexOf(')', i);
                            n1 = temp.IndexOf(',', n2);
                            res = String.Format("{0}{1}", res, GetDataS(temp.Substring(i, n2 + 1 - i)));
                            if (n1 < 0)
                                break;
                            i = n1 + 1;
                        }
                        else
                        {
                            if (n1 >= 0)
                                res = String.Format("{0}{1}", res, GetDataS(temp.Substring(i, n1 + 1 - i-1)));
                            else
                            {
                                res = String.Format("{0}{1}", res, GetDataS(temp.Substring(i)));
                                break;
                            }
                            i = n1 + 1;
                        }
                    }
                    break;
                    #endregion
                case ('?'):
                    #region get length
                    res = (str[1] == '?') ? GetDataS(str.Substring(2)) : GetDataS(str.Substring(1));
                    res = (str[1] == '?') ? String.Format("{0:X4}{1}", res.Length/2, res) : String.Format("{0:X2}{1}", res.Length/2, res);
                    break;
                    #endregion
                case((char)39): //апостроф
                    #region перевод строки в ASCII-HEX, аналог #ASCII
                    n1 = str.IndexOf((char)39);
                    n2 = str.IndexOf((char)39, n1 + 1);
                    temp = GetDataS(str.Substring(n1 + 1, n2 - n1 - 1));
                    res = Utils.Bin2AHex(Utils.String2Bin(temp, codepage));
                    #endregion
                    break;
                default:
                    res = str;
                    break;
            }
            return res;
        }
        private byte[] GetDataB(string str)
        {
            return Utils.AHex2Bin(GetDataS(str));
        }
        private void ShowConsole(string str, params object[] par)
        {
            if (!consoleOutput)
                return;
            if (par != null && par.Length > 0)
                Console.WriteLine(String.Format(str, par));
            else
                Console.WriteLine(str);
        }
        private void ShowConsole(APDUCommand comm)
        {
            if (!consoleOutput)
                return;
            Console.Write("SC << {0:X2}{1:X2} {2:X2}{3:X2}", comm.Class, comm.Ins, comm.P1, comm.P2);
            if (comm.Data != null)
            {
                Console.Write(" {0:X2}", comm.Data.Length);
                if (comm.Data.Length > 0)
                    Console.Write(" {0}", Utils.Bin2AHex(comm.Data));
            }
            else
            {
                if (comm.P3 != 0)
                    Console.Write(" {0:X2}", comm.P3);
            }
            Console.WriteLine(" {0:X2}", comm.Le);
        }
        private void ShowConsole(APDUResponse res)
        {
            if (!consoleOutput || res == null)
                return;
            if (res.Data != null && res.Data.Length > 0)
                Console.WriteLine("SC >> {0} {1:X2}{2:X2}", Utils.Bin2AHex(res.Data), res.SW1, res.SW2);
            else
                Console.WriteLine("SC >> {0:X2}{1:X2}", res.SW1, res.SW2);
        }
        private string[] GetAllKeysInIniFileSection(string strSectionName, string strIniFileName)
        {
            IntPtr pBuffer = Marshal.AllocHGlobal(32767);
            string[] strArray = new string[0];
            UInt32 uiNumCharCopied = 0;

            uiNumCharCopied = GetPrivateProfileSection(strSectionName, pBuffer, 32767, strIniFileName);

            int iStartAddress = pBuffer.ToInt32();
            int iEndAddress = iStartAddress + (int)uiNumCharCopied;

            while (iStartAddress < iEndAddress)
            {
                int iArrayCurrentSize = strArray.Length;
                Array.Resize<string>(ref strArray, iArrayCurrentSize + 1);
                string strCurrent = Marshal.PtrToStringAnsi(new IntPtr(iStartAddress));
                strArray[iArrayCurrentSize] = strCurrent;
                iStartAddress += (strCurrent.Length + 1);
            }

            Marshal.FreeHGlobal(pBuffer);
            pBuffer = IntPtr.Zero;

            return strArray;
        }
    }
    public enum ScriptSection
    {
        None,
        Reader,
        Keys,
        Codes,
        Vars,
        HSM
    }
}