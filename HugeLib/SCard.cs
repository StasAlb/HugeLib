using System;
using System.Collections;
using System.Text;
using System.Runtime.InteropServices;
using System.Threading;

namespace HugeLib.SCard
{
	public class SCARD_ATTR_VALUE
	{
		private const uint
			SCARD_CLASS_COMMUNICATIONS = 2,
			SCARD_CLASS_PROTOCOL = 3,
			SCARD_CLASS_MECHANICAL = 6,
			SCARD_CLASS_VENDOR_DEFINED = 7,
			SCARD_CLASS_IFD_PROTOCOL = 8,
			SCARD_CLASS_ICC_STATE = 9,
			SCARD_CLASS_SYSTEM = 0x7fff;

		private static UInt32 SCardAttrValue(UInt32 attrClass, UInt32 val)
		{
			return (attrClass << 16) | val;
		}

		public static UInt32 CHANNEL_ID
		{ 
			get 
			{ 
				return SCardAttrValue(SCARD_CLASS_COMMUNICATIONS, 0x0110); 
			} 
		}
		public static UInt32 CHARACTERISTICS 
		{ 
			get 
			{ 
				return SCardAttrValue(SCARD_CLASS_MECHANICAL, 0x0150); 
			} 
		}
		public static UInt32 CURRENT_PROTOCOL_TYPE 
		{ 
			get 
			{ 
				return SCardAttrValue(SCARD_CLASS_IFD_PROTOCOL, 0x0201); 
			} 
		}
		public static UInt32 DEVICE_UNIT 
		{ 
			get 
			{ 
				return SCardAttrValue(SCARD_CLASS_SYSTEM, 0x0001); 
			} 
		}
		public static UInt32 DEVICE_FRIENDLY_NAME 
		{ 
			get 
			{ 
				return SCardAttrValue(SCARD_CLASS_SYSTEM, 0x0003); 
			} 
		}
		public UInt32 DEVICE_SYSTEM_NAME 
		{ 
			get 
			{ 
				return SCardAttrValue(SCARD_CLASS_SYSTEM, 0x0004); 
			} 
		}
		public static UInt32 ICC_PRESENCE 
		{ 
			get 
			{ 
				return SCardAttrValue(SCARD_CLASS_ICC_STATE, 0x0300); 
			} 
		}
		public static UInt32 ICC_INTERFACE_STATUS 
		{ 
			get 
			{ 
				return SCardAttrValue(SCARD_CLASS_ICC_STATE, 0x0301); 
			} 
		}
		public static UInt32 ATR_STRING 
		{ 
			get 
			{ 
				return SCardAttrValue(SCARD_CLASS_ICC_STATE, 0x0303); 
			} 
		}
		public static UInt32 ICC_TYPE_PER_ATR 
		{ 
			get 
			{ 
				return SCardAttrValue(SCARD_CLASS_ICC_STATE, 0x0304); 
			} 
		}
		public static UInt32 PROTOCOL_TYPES 
		{ 
			get 
			{ 
				return SCardAttrValue(SCARD_CLASS_PROTOCOL, 0x0120); 
			} 
		}
		public static UInt32 VENDOR_NAME 
		{ 
			get 
			{ 
				return SCardAttrValue(SCARD_CLASS_VENDOR_DEFINED, 0x0100); 
			} 
		}
		public static UInt32 VENDOR_IFD_TYPE 
		{ 
			get 
			{ 
				return SCardAttrValue(SCARD_CLASS_VENDOR_DEFINED, 0x0101); 
			} 
		}
		public static UInt32 VENDOR_IFD_VERSION 
		{
			get 
			{ 
				return SCardAttrValue(SCARD_CLASS_VENDOR_DEFINED, 0x0102); 
			} 
		}
		public static UInt32 VENDOR_IFD_SERIAL_NO 
		{ 
			get 
			{ 
				return SCardAttrValue(SCARD_CLASS_VENDOR_DEFINED, 0x0103); 
			} 
		}
	}
    public delegate void CardInsertedEventHandler();
	public delegate void CardRemovedEventHandler();

    public delegate void CardInsertedEventHandlerName(string readerName);
    public delegate void CardRemovedEventHandlerName(string readerName);

    enum CARD_STATE
	{
		UNAWARE = 0x00000000,
		IGNORE = 0x00000001,
		CHANGED = 0x00000002,
		UNKNOWN = 0x00000004,
		UNAVAILABLE = 0x00000008,
		EMPTY = 0x00000010,
		PRESENT = 0x00000020,
		ATRMATCH = 0x00000040,
		EXCLUSIVE = 0x00000080,
		INUSE = 0x00000100,
		MUTE = 0x00000200,
		UNPOWERED = 0x00000400
	}
	[StructLayout(LayoutKind.Sequential)]
	public struct SCard_IO_Request
	{
		public UInt32 m_dwProtocol;
		public UInt32 m_cbPciLength;
	}
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
	public struct SCard_ReaderState
	{
		public string m_szReader;
		public IntPtr m_pvUserData;
		public UInt32 m_dwCurrentState;
		public UInt32 m_dwEventState;
		public UInt32 m_cbAtr;
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
		public byte[] m_rgbAtr;
	}
	public enum SHARE
	{
		/// <summary>
		/// This application is not willing to share this card with other applications.
		/// </summary>
		Exclusive = 1,

		/// <summary>
		/// This application is willing to share this card with other applications.
		/// </summary>
		Shared,

		/// <summary>
		/// This application demands direct control of the reader, so it is not available to other applications.
		/// </summary>
		Direct
	}
	public enum PROTOCOL
	{
		/// <summary>
		/// There is no active protocol.
		/// </summary>
		Undefined = 0x00000000,

		/// <summary>
		/// T=0 is the active protocol.
		/// </summary>
		T0 = 0x00000001,

		/// <summary>
		/// T=1 is the active protocol.
		/// </summary>
		T1 = 0x00000002,

		/// <summary>
		/// Raw is the active protocol.
		/// </summary>
		Raw = 0x00010000,
		Default = unchecked((int)0x80000000),  // Use implicit PTS.

		/// <summary>
		/// T=1 or T=0 can be the active protocol
		/// </summary>
		T0orT1 = T0 | T1
	}
	public enum DISCONNECT
	{
		/// <summary>
		/// Don't do anything special on close
		/// </summary>
		Leave,

		/// <summary>
		/// Reset the card on close
		/// </summary>
		Reset,

		/// <summary>
		/// Power down the card on close
		/// </summary>
		Unpower,

		/// <summary>
		/// Eject(!) the card on close
		/// </summary>
		Eject
	}
	public enum SCOPE
	{
		User,
		Terminal,
		System
	}

	public class CardBase
	{
		private UInt32 m_hContext = 0;
		private UInt32 m_hCard = 0;
		private UInt32 m_nProtocol = (uint)PROTOCOL.T0;
		private int m_nLastError = 0;


		protected const uint INFINITE = 0xFFFFFFFF;
		protected const uint WAIT_TIME = 250;

		protected bool m_bRunCardDetection = true;
		protected Thread m_thread = null;

		public event CardInsertedEventHandler OnCardInserted = null;
		public event CardRemovedEventHandler OnCardRemoved = null;

		public CardBase()
		{
		}
		~CardBase()
		{
			StopCardEvents();
		}

		public string[] ListReaders()
		{
			EstablishContext(SCOPE.User);

			string[] sListReaders = null;
			UInt32 pchReaders = 0;
			IntPtr szListReaders = IntPtr.Zero;

			m_nLastError = SCardListReaders(m_hContext, null, szListReaders, out pchReaders);
			if (m_nLastError == 0)
			{
				szListReaders = Marshal.AllocHGlobal((int)pchReaders);
				m_nLastError = SCardListReaders(m_hContext, null, szListReaders, out pchReaders);
				if (m_nLastError == 0)
				{
					char[] caReadersData = new char[pchReaders];
					int nbReaders = 0;
					for (int nI = 0; nI < pchReaders; nI++)
					{
						caReadersData[nI] = (char)Marshal.ReadByte(szListReaders, nI);

						if (caReadersData[nI] == 0)
							nbReaders++;
					}
					--nbReaders;
					if (nbReaders != 0)
					{
						sListReaders = new string[nbReaders];
						char[] caReader = new char[pchReaders];
						int nIdx = 0;
						int nIdy = 0;
						int nIdz = 0;

						while (nIdx < pchReaders - 1)
						{
							caReader[nIdy] = caReadersData[nIdx];
							if (caReader[nIdy] == 0)
							{
								sListReaders[nIdz] = new string(caReader, 0, nIdy);
								++nIdz;
								nIdy = 0;
								caReader = new char[pchReaders];
							}
							else
								++nIdy;

							++nIdx;
						}
					}

				}
				Marshal.FreeHGlobal(szListReaders);
			}
			ReleaseContext();
			return sListReaders;
		}
		public void Connect(string Reader, SHARE ShareMode, PROTOCOL PreferredProtocols)
		{
			EstablishContext(SCOPE.User);

			IntPtr hCard = Marshal.AllocHGlobal(Marshal.SizeOf(m_hCard));
			IntPtr pProtocol = Marshal.AllocHGlobal(Marshal.SizeOf(m_nProtocol));
            
            
			m_nLastError = SCardConnect(m_hContext,
				Reader,
				(uint)ShareMode,
				(uint)PreferredProtocols,
				hCard,
				pProtocol);

            if (m_nLastError != 0)
			{
				Marshal.FreeHGlobal(hCard);
				Marshal.FreeHGlobal(pProtocol);
                unchecked
                {
                    switch (m_nLastError)
                    {
                        case ((int)0x80100009):
                            throw new Exception("Reader not found...");
                        case ((int)0x80100069):
                            throw new Exception("No smart card in reader");
                        default:
                            throw new Exception(String.Format("SCardConnect error: 0x{0:X}", m_nLastError));
                    }
                }
			}

			m_hCard = (uint)Marshal.ReadInt32(hCard);
			m_nProtocol = (uint)Marshal.ReadInt32(pProtocol);

			Marshal.FreeHGlobal(hCard);
			Marshal.FreeHGlobal(pProtocol);
		}
        public PROTOCOL GetProtocol()
        {
            return (m_nProtocol == 1) ? PROTOCOL.T1 : PROTOCOL.T0;
        }
		public void Disconnect(DISCONNECT Disposition)
		{
			if (m_hCard != 0)
			{
				m_nLastError = SCardDisconnect(m_hCard, (uint)Disposition);
				m_hCard = 0;

				if (m_nLastError != 0)
				{
					string msg = "SCardDisconnect error: " + m_nLastError;
					throw new Exception(msg);
				}

				ReleaseContext();
			}

		}
        public string Control(uint controlByte, byte[] data, uint len)
        {
            byte[] res = new byte[255];
            uint resLen = 255, res1Len = 0;
            m_nLastError = SCardControl(m_hCard, controlByte , data, (uint)data.Length, res, resLen, out res1Len);
            return Utils.Bin2AHex(res);
        }
		public APDUResponse Transmit(APDUCommand ApduCmd)
		{
			uint RecvLength = (uint)(ApduCmd.Le + APDUResponse.SW_LENGTH);
			byte[] ApduBuffer = null;
			byte[] ApduResponse = new byte[ApduCmd.Le + APDUResponse.SW_LENGTH];
			SCard_IO_Request ioRequest = new SCard_IO_Request();
			ioRequest.m_dwProtocol = m_nProtocol;
			ioRequest.m_cbPciLength = 8;
            int leIndex = -1;
			// Build the command APDU
			if (ApduCmd.Data == null)
			{

                if (ApduCmd.P3 > 0)
                {
                    ApduBuffer = new byte[APDUCommand.APDU_MIN_LENGTH + 2];
                    ApduBuffer[4] = ApduCmd.P3;
                    ApduBuffer[5] = (byte)ApduCmd.Le;
                    leIndex = 5;
                }
                else
                {
                    //ApduBuffer = new byte[APDUCommand.APDU_MIN_LENGTH + ((ApduCmd.Le != 0) ? 1 : 0)];
                    ApduBuffer = new byte[APDUCommand.APDU_MIN_LENGTH + 1];
                    //                    if (ApduCmd.Le != 0)
                    //                    {
                    ApduBuffer[4] = (byte)ApduCmd.Le;
                        leIndex = 4;
//                    }
                }
			}
			else
			{
				ApduBuffer = new byte[APDUCommand.APDU_MIN_LENGTH + 1 + ApduCmd.Data.Length];

				for (int nI = 0; nI < ApduCmd.Data.Length; nI++)
					ApduBuffer[APDUCommand.APDU_MIN_LENGTH + 1 + nI] = ApduCmd.Data[nI];

				ApduBuffer[APDUCommand.APDU_MIN_LENGTH] = (byte)ApduCmd.Data.Length;

                //ApduBuffer[APDUCommand.APDU_MIN_LENGTH + 2 + ApduCmd.Data.Length - 1] = (byte)ApduCmd.Le;

            }
            //if (ApduCmd.Data == null)
            //{
            //    ApduBuffer = new byte[APDUCommand.APDU_MIN_LENGTH + 1];
            //    ApduBuffer[4] = 0x18;
                //leIndex = -1;
            //}
			ApduBuffer[0] = ApduCmd.Class;
			ApduBuffer[1] = ApduCmd.Ins;
			ApduBuffer[2] = ApduCmd.P1;
			ApduBuffer[3] = ApduCmd.P2;

			m_nLastError = SCardTransmit(m_hCard, ref ioRequest, ApduBuffer, (uint)ApduBuffer.Length, IntPtr.Zero, ApduResponse, out RecvLength);
			if (m_nLastError != 0)
			{
				string msg = "SCardTransmit error: " + m_nLastError + String.Format(" ({0:X})", m_nLastError);
				throw new Exception(msg);
			}

			byte[] ApduData = new byte[RecvLength];

			for (int nI = 0; nI < RecvLength; nI++)
				ApduData[nI] = ApduResponse[nI];

			return new APDUResponse(ApduData, ApduCmd.GetApduResponseType());
		}
        /// <summary>
        /// transmit apdu с поддержкой ответов 6c/61
        /// </summary>
        /// <param name="ApduCmd"></param>
        /// <returns></returns>
        public APDUResponse TransmitA(APDUCommand ApduCmd)
        {
            APDUResponse res = Transmit(ApduCmd);
            if (res.SW1 == 0x6C)
            {
                ApduCmd.SetLe(res.SW2);
                res = Transmit(ApduCmd);
            }
            if (res.SW1 == 0x61)
            {
                ApduCmd = new APDUCommand(0x00, 0xC0, 0x00, 0x00, null, res.SW2);
                res = Transmit(ApduCmd);
            }
            return res;
        }
		public void BeginTransaction()
		{ }
		public void EndTransaction(DISCONNECT Disposition)
		{ }
		public byte[] GetAttribute(UInt32 AttribId)
		{
			byte[] attr = null;
			UInt32 attrLen = 0;

			m_nLastError = SCardGetAttrib(m_hCard, AttribId, attr, out attrLen);
			if (m_nLastError == 0)
			{
				if (attrLen != 0)
				{
					attr = new byte[attrLen];
					m_nLastError = SCardGetAttrib(m_hCard, AttribId, attr, out attrLen);
					if (m_nLastError != 0)
					{
						string msg = "SCardGetAttr error: " + m_nLastError;
						throw new Exception(msg);
					}
				}
			}
			else
			{
				string msg = "SCardGetAttr error: " + m_nLastError;
				throw new Exception(msg);
			}
			return attr;
		}

		#region WINSCARD
        [DllImport("winscard.dll", SetLastError = true)]
        internal static extern int SCardGetStatusChange(UInt32 hContext,
            UInt32 dwTimeout,
            [In, Out] SCard_ReaderState[] rgReaderStates,
            UInt32 cReaders);
		[DllImport("winscard.dll", SetLastError = true)]
		internal static extern int SCardListReaders(UInt32 hContext,
			[MarshalAs(UnmanagedType.LPTStr)] string mszGroups,
			IntPtr mszReaders,
			out UInt32 pcchReaders);
		[DllImport("winscard.dll", SetLastError = true)]
		internal static extern int SCardEstablishContext(UInt32 dwScope,
			IntPtr pvReserved1,
			IntPtr pvReserved2,
			IntPtr phContext);
		[DllImport("winscard.dll", SetLastError = true)]
		internal static extern int SCardReleaseContext(UInt32 hContext);
		[DllImport("winscard.dll", SetLastError = true, CharSet = CharSet.Auto)]
		internal static extern int SCardConnect(UInt32 hContext,
			[MarshalAs(UnmanagedType.LPTStr)] string szReader,
			UInt32 dwShareMode,
			UInt32 dwPreferredProtocols,
			IntPtr phCard,
			IntPtr pdwActiveProtocol);
		[DllImport("winscard.dll", SetLastError = true)]
		internal static extern int SCardDisconnect(UInt32 hCard,
			UInt32 dwDisposition);
		[DllImport("winscard.dll", SetLastError = true)]
		internal static extern int SCardGetAttrib(UInt32 hCard,
			UInt32 dwAttribId,
			[Out] byte[] pbAttr,
			out UInt32 pcbAttrLen);
		[DllImport("winscard.dll", SetLastError = true)]
		internal static extern int SCardTransmit(UInt32 hCard,
			[In] ref SCard_IO_Request pioSendPci,
			byte[] pbSendBuffer,
			UInt32 cbSendLength,
			IntPtr pioRecvPci,
			[Out] byte[] pbRecvBuffer,
			out UInt32 pcbRecvLength
			);
        [DllImport("winscard.dll", SetLastError = true)]
        internal static extern int SCardControl(UInt32 hCard, 
            [In] UInt32 dwControlCode,
            [In] byte[] lpInBuffer, 
            [In] UInt32 nInBufferSize, 
            [In, Out] byte[] lpOutBuffer, 
            UInt32 nOutBufferSize, 
            out uint lpBytesReturned);
		#endregion


		/// <summary>
		/// This method should start a thread that checks for card insertion or removal
		/// </summary>
		/// <param name="Reader"></param>
		public void StartCardEvents(string Reader)
		{
			if (m_thread == null)
			{
				m_bRunCardDetection = true;

				m_thread = new Thread(new ParameterizedThreadStart(RunCardDetection));
				m_thread.Start(Reader);
			}
		}

		/// <summary>
		/// Stops the card events thread
		/// </summary>
		public void StopCardEvents()
		{
			if (m_thread != null)
			{
				int
					nTimeOut = 10,
					nCount = 0;
				bool m_bStop = false;
				m_bRunCardDetection = false;

				do
				{
					if (nCount > nTimeOut)
					{
						m_thread.Abort();
						break;
					}

					if (m_thread.ThreadState == ThreadState.Aborted)
						m_bStop = true;

					if (m_thread.ThreadState == ThreadState.Stopped)
						m_bStop = true;

					Thread.Sleep(200);
					++nCount;           // Manage time out
				}
				while (!m_bStop);

				m_thread = null;
			}
		}

		/// <summary>
		/// This function must implement a card detection mechanism.
		/// 
		/// When card insertion is detected, it must call the method CardInserted()
		/// When card removal is detected, it must call the method CardRemoved()
		/// 
		/// </summary>
		/// <param name="Reader">Name of the reader to scan for card event</param>
		protected void RunCardDetection(object Reader)
		{
            bool bFirstLoop = true;
            UInt32 hContext = 0;    // Local context
            IntPtr phContext;

            phContext = Marshal.AllocHGlobal(Marshal.SizeOf(hContext));

            if (SCardEstablishContext((uint) SCOPE.User, IntPtr.Zero, IntPtr.Zero, phContext) == 0)
            {
                hContext = (uint)Marshal.ReadInt32(phContext);
                Marshal.FreeHGlobal(phContext);

                UInt32 nbReaders = 1;
                SCard_ReaderState[] readerState = new SCard_ReaderState[nbReaders];

                readerState[0].m_dwCurrentState = (UInt32) CARD_STATE.UNAWARE;
                readerState[0].m_szReader = (string)Reader;

                UInt32 eventState;
                UInt32 currentState = readerState[0].m_dwCurrentState;

                // Card detection loop
                do
                {
                    if (SCardGetStatusChange(hContext, WAIT_TIME
                        , readerState, nbReaders) == 0)
                    {
                        eventState = readerState[0].m_dwEventState;
                        currentState = readerState[0].m_dwCurrentState;

                        // Check state
                        if (((eventState & (uint) CARD_STATE.CHANGED) == (uint) CARD_STATE.CHANGED) && !bFirstLoop)    
                        {
                            // State has changed
                            if ((eventState & (uint) CARD_STATE.EMPTY) == (uint) CARD_STATE.EMPTY)
                            {
                                // There is no card, card has been removed -> Fire CardRemoved event
                                CardRemoved();
                            }

                            if (((eventState & (uint)CARD_STATE.PRESENT) == (uint)CARD_STATE.PRESENT) && 
                                ((eventState & (uint) CARD_STATE.PRESENT) != (currentState & (uint) CARD_STATE.PRESENT)))
                            {
                                // There is a card in the reader -> Fire CardInserted event
                                CardInserted();
                            }

                            if ((eventState & (uint) CARD_STATE.ATRMATCH) == (uint) CARD_STATE.ATRMATCH)
                            {
                                // There is a card in the reader and it matches the ATR we were expecting-> Fire CardInserted event
                                CardInserted();
                            }
                        }

                        // The current state is now the event state
                        readerState[0].m_dwCurrentState = eventState;

                        bFirstLoop = false;
                    }

                    Thread.Sleep(100);

                    if (m_bRunCardDetection == false)
                        break;
                }
                while (true);    // Exit on request
            }
            else
            {
                Marshal.FreeHGlobal(phContext);
				return;
                throw new Exception("PC/SC error");
            }

            SCardReleaseContext(hContext);
        }

		#region Event methods
		protected void CardInserted()
		{
			if (OnCardInserted != null)
				OnCardInserted();
        }

		protected void CardRemoved()
		{
			if (OnCardRemoved != null)
				OnCardRemoved();
		}
		#endregion
		public void EstablishContext(SCOPE Scope)
		{
			IntPtr hContext = Marshal.AllocHGlobal(Marshal.SizeOf(m_hContext));

			m_nLastError = SCardEstablishContext((uint)Scope, IntPtr.Zero, IntPtr.Zero, hContext);
			if (m_nLastError != 0)
			{
				string msg = "SCardEstablishContext error: " + m_nLastError;

				Marshal.FreeHGlobal(hContext);
				throw new Exception(msg);
			}

			m_hContext = (uint)Marshal.ReadInt32(hContext);

			Marshal.FreeHGlobal(hContext);
		}
		public void ReleaseContext()
		{
			if (m_hContext != 0)
			{
				m_nLastError = SCardReleaseContext(m_hContext);

				if (m_nLastError != 0)
				{
					string msg = "SCardReleaseContext error: " + m_nLastError;
					throw new Exception(msg);
				}

				m_hContext = 0;
			}
		}
		public string GetLastError()
		{
			switch (m_nLastError)
			{
				case (-2146434967):
					return "no smartcard";
				case (-2146435063):
                    return "The specified reader name is not recognized";
				case (-2146435068):
					return "invalid parameter";
				case (-2146435064):
					return "data buffer for return is too small";
				case (-2146435071):
					return "an internal consistency check failed";
                case (-2146435025):
                    return "A communications error with the smart card has been detected";
				default:
					return ":unknown error " + m_nLastError.ToString();
			}
		}
        public APDUResponse Test()
        {
            uint RecvLength = 3;
            byte[] ApduBuffer = new byte[5];
            byte[] ApduResponse = new byte[3];
            SCard_IO_Request ioRequest = new SCard_IO_Request();
            ioRequest.m_dwProtocol = m_nProtocol;
            ioRequest.m_cbPciLength = 8;
            //if (ApduCmd.Data == null)
            //{
            //    ApduBuffer = new byte[APDUCommand.APDU_MIN_LENGTH + 1];
            //    ApduBuffer[4] = 0x18;
            //leIndex = -1;
            //}
            ApduBuffer[0] = 0xFE;
            ApduBuffer[1] = 0x11;
            ApduBuffer[2] = 0xFE;
            ApduBuffer[3] = 0xFE;
            ApduBuffer[4] = 0x00;

            m_nLastError = SCardTransmit(m_hCard, ref ioRequest, ApduBuffer, (uint)ApduBuffer.Length, IntPtr.Zero, ApduResponse, out RecvLength);
            if (m_nLastError != 0)
            {
                string msg = "SCardTransmit error: " + m_nLastError;
                throw new Exception(msg);
            }

            byte[] ApduData = new byte[RecvLength];

            for (int nI = 0; nI < RecvLength; nI++)
                ApduData[nI] = ApduResponse[nI];

            return new APDUResponse(ApduData);
        }
	}

    public class SmartClass
    {
        private static UInt32 m_hContext = 0;
        private static int m_nLastError = 0;

        protected const uint INFINITE = 0xFFFFFFFF;
        protected const uint WAIT_TIME = 250;

        protected bool m_bRunCardDetection = true;

        ArrayList threads = new ArrayList();

        public event CardInsertedEventHandlerName OnCardInserted = null;
        public event CardRemovedEventHandlerName OnCardRemoved = null;

        #region WINSCARD
        [DllImport("winscard.dll", SetLastError = true)]
    internal static extern int SCardGetStatusChange(UInt32 hContext,
            UInt32 dwTimeout,
            [In, Out] SCard_ReaderState[] rgReaderStates,
            UInt32 cReaders);
    [DllImport("winscard.dll", SetLastError = true)]
    internal static extern int SCardListReaders(UInt32 hContext,
        [MarshalAs(UnmanagedType.LPTStr)] string mszGroups,
        IntPtr mszReaders,
        out UInt32 pcchReaders);
    [DllImport("winscard.dll", SetLastError = true)]
    internal static extern int SCardEstablishContext(UInt32 dwScope,
        IntPtr pvReserved1,
        IntPtr pvReserved2,
        IntPtr phContext);
    [DllImport("winscard.dll", SetLastError = true)]
    internal static extern int SCardReleaseContext(UInt32 hContext);
    [DllImport("winscard.dll", SetLastError = true, CharSet = CharSet.Auto)]
    internal static extern int SCardConnect(UInt32 hContext,
        [MarshalAs(UnmanagedType.LPTStr)] string szReader,
        UInt32 dwShareMode,
        UInt32 dwPreferredProtocols,
        IntPtr phCard,
        IntPtr pdwActiveProtocol);
    [DllImport("winscard.dll", SetLastError = true)]
    internal static extern int SCardDisconnect(UInt32 hCard,
        UInt32 dwDisposition);
    [DllImport("winscard.dll", SetLastError = true)]
    internal static extern int SCardGetAttrib(UInt32 hCard,
        UInt32 dwAttribId,
        [Out] byte[] pbAttr,
        out UInt32 pcbAttrLen);
    [DllImport("winscard.dll", SetLastError = true)]
    internal static extern int SCardTransmit(UInt32 hCard,
        [In] ref SCard_IO_Request pioSendPci,
        byte[] pbSendBuffer,
        UInt32 cbSendLength,
        IntPtr pioRecvPci,
        [Out] byte[] pbRecvBuffer,
        out UInt32 pcbRecvLength
        );
    [DllImport("winscard.dll", SetLastError = true)]
    internal static extern int SCardControl(UInt32 hCard,
        [In] UInt32 dwControlCode,
        [In] byte[] lpInBuffer,
        [In] UInt32 nInBufferSize,
        [In, Out] byte[] lpOutBuffer,
        UInt32 nOutBufferSize,
        out uint lpBytesReturned);
		#endregion

        public static int ReaderIndex(string readerName)
        {
            string[] readers = ListReaders();
            for (int tt = 0; tt < readers.Length; tt++)
                if (readers[tt] == readerName)
                    return tt;
            return -1;
        }
        public static string[] ListReaders()
        {
            EstablishContext(SCOPE.User);

            string[] sListReaders = null;
            UInt32 pchReaders = 0;
            IntPtr szListReaders = IntPtr.Zero;

            m_nLastError = SCardListReaders(m_hContext, null, szListReaders, out pchReaders);
            if (m_nLastError == 0)
            {
                szListReaders = Marshal.AllocHGlobal((int)pchReaders);
                m_nLastError = SCardListReaders(m_hContext, null, szListReaders, out pchReaders);
                if (m_nLastError == 0)
                {
                    char[] caReadersData = new char[pchReaders];
                    int nbReaders = 0;
                    for (int nI = 0; nI < pchReaders; nI++)
                    {
                        caReadersData[nI] = (char)Marshal.ReadByte(szListReaders, nI);

                        if (caReadersData[nI] == 0)
                            nbReaders++;
                    }
                    --nbReaders;
                    if (nbReaders != 0)
                    {
                        sListReaders = new string[nbReaders];
                        char[] caReader = new char[pchReaders];
                        int nIdx = 0;
                        int nIdy = 0;
                        int nIdz = 0;

                        while (nIdx < pchReaders - 1)
                        {
                            caReader[nIdy] = caReadersData[nIdx];
                            if (caReader[nIdy] == 0)
                            {
                                sListReaders[nIdz] = new string(caReader, 0, nIdy);
                                ++nIdz;
                                nIdy = 0;
                                caReader = new char[pchReaders];
                            }
                            else
                                ++nIdy;

                            ++nIdx;
                        }
                    }

                }
                Marshal.FreeHGlobal(szListReaders);
            }
            ReleaseContext();
            return sListReaders;
        }

        /// <summary>
        /// This method should start a thread that checks for card insertion or removal
        /// </summary>
        /// <param name="Reader"></param>
        public void StartCardEvents(string[] readers)
        {
            threads.Clear();
            m_bRunCardDetection = true;
            foreach (string reader in readers)
            {
                Thread th = new Thread(new ParameterizedThreadStart(RunCardDetection));
                th.Start(reader);
                threads.Add(th);
            }
        }

        /// <summary>
        /// Stops the card events thread
        /// </summary>
        public void StopCardEvents()
        {
            foreach (Thread th in threads)
            {
                if (th != null)
                {
                    int
                        nTimeOut = 10,
                        nCount = 0;
                    bool m_bStop = false;
                    m_bRunCardDetection = false;

                    do
                    {
                        if (nCount > nTimeOut)
                        {
                            th.Abort();
                            break;
                        }

                        if (th.ThreadState == ThreadState.Aborted)
                            m_bStop = true;

                        if (th.ThreadState == ThreadState.Stopped)
                            m_bStop = true;

                        Thread.Sleep(200);
                        ++nCount;           // Manage time out
                    }
                    while (!m_bStop);
                }
            }
            threads.Clear();
        }

        /// <summary>
        /// This function must implement a card detection mechanism.
        /// 
        /// When card insertion is detected, it must call the method CardInserted()
        /// When card removal is detected, it must call the method CardRemoved()
        /// 
        /// </summary>
        /// <param name="Reader">Name of the reader to scan for card event</param>
        protected void RunCardDetection(object Reader)
        {
            bool bFirstLoop = true;
            UInt32 hContext = 0;    // Local context
            IntPtr phContext;

            phContext = Marshal.AllocHGlobal(Marshal.SizeOf(hContext));

            if (SCardEstablishContext((uint)SCOPE.User, IntPtr.Zero, IntPtr.Zero, phContext) == 0)
            {
                hContext = (uint)Marshal.ReadInt32(phContext);
                Marshal.FreeHGlobal(phContext);

                UInt32 nbReaders = 1;
                SCard_ReaderState[] readerState = new SCard_ReaderState[nbReaders];

                readerState[0].m_dwCurrentState = (UInt32)CARD_STATE.UNAWARE;
                readerState[0].m_szReader = (string)Reader;

                UInt32 eventState;
                UInt32 currentState = readerState[0].m_dwCurrentState;

                // Card detection loop
                do
                {
                    if (SCardGetStatusChange(hContext, WAIT_TIME
                        , readerState, nbReaders) == 0)
                    {
                        eventState = readerState[0].m_dwEventState;
                        currentState = readerState[0].m_dwCurrentState;

                        // Check state
                        if (((eventState & (uint)CARD_STATE.CHANGED) == (uint)CARD_STATE.CHANGED) && !bFirstLoop)
                        {
                            // State has changed
                            if ((eventState & (uint)CARD_STATE.EMPTY) == (uint)CARD_STATE.EMPTY)
                            {
                                // There is no card, card has been removed -> Fire CardRemoved event
                                CardRemoved((string)Reader);
                            }

                            if (((eventState & (uint)CARD_STATE.PRESENT) == (uint)CARD_STATE.PRESENT) &&
                                ((eventState & (uint)CARD_STATE.PRESENT) != (currentState & (uint)CARD_STATE.PRESENT)))
                            {
                                // There is a card in the reader -> Fire CardInserted event
                                CardInserted((string)Reader);
                            }

                            if ((eventState & (uint)CARD_STATE.ATRMATCH) == (uint)CARD_STATE.ATRMATCH)
                            {
                                // There is a card in the reader and it matches the ATR we were expecting-> Fire CardInserted event
                                CardInserted((string)Reader);
                            }
                        }

                        // The current state is now the event state
                        readerState[0].m_dwCurrentState = eventState;

                        bFirstLoop = false;
                    }

                    Thread.Sleep(100);

                    if (m_bRunCardDetection == false)
                        break;
                }
                while (true);    // Exit on request
            }
            else
            {
                Marshal.FreeHGlobal(phContext);
                throw new Exception("PC/SC error");
            }

            SCardReleaseContext(hContext);
        }
        
        protected void CardInserted(string readerName)
        {
            if (OnCardInserted != null)
                OnCardInserted(readerName);
        }

        protected void CardRemoved(string readerName)
        {
            if (OnCardRemoved != null)
                OnCardRemoved(readerName);
        }

        public static void EstablishContext(SCOPE Scope)
        {
            IntPtr hContext = Marshal.AllocHGlobal(Marshal.SizeOf(m_hContext));

            m_nLastError = SCardEstablishContext((uint)Scope, IntPtr.Zero, IntPtr.Zero, hContext);
            if (m_nLastError != 0)
            {
                string msg = "SCardEstablishContext error: " + m_nLastError;

                Marshal.FreeHGlobal(hContext);
                throw new Exception(msg);
            }

            m_hContext = (uint)Marshal.ReadInt32(hContext);

            Marshal.FreeHGlobal(hContext);
        }
        public static void ReleaseContext()
        {
            if (m_hContext != 0)
            {
                m_nLastError = SCardReleaseContext(m_hContext);

                if (m_nLastError != 0)
                {
                    string msg = "SCardReleaseContext error: " + m_nLastError;
                    throw new Exception(msg);
                }

                m_hContext = 0;
            }
        }
    }
}