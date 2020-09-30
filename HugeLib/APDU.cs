using System;
using System.Collections.Generic;
using System.Text;

namespace HugeLib.SCard
{
	public class APDUCommand
	{
		public const int APDU_MIN_LENGTH = 4;

		private byte m_bCla;
		private byte m_bIns;
		private byte m_bP1;
		private byte m_bP2;
        private byte m_bP3;
		private byte[] m_baData;
		private byte m_bLe;
        private APDUResponseType respType = APDUResponseType.Standart;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="bCla">Class byte</param>
		/// <param name="bIns">Instruction byte</param>
		/// <param name="bP1">Parameter P1 byte</param>
		/// <param name="bP2">Parameter P2 byte</param>
		/// <param name="baData">Data to send to the card if any, null if no data to send</param>
		/// <param name="bLe">Number of data expected, 0 if none</param>
		public APDUCommand(byte bCla, byte bIns, byte bP1, byte bP2, byte[] baData, byte bLe)
		{
			m_bCla = bCla;
			m_bIns = bIns;
			m_bP1 = bP1;
			m_bP2 = bP2;
            m_bP3 = 0x00;
			m_baData = baData;
			m_bLe = bLe;
		}
		public APDUCommand(char bCla, char bIns, char bP1, char bP2, byte[] baData, byte bLe)
		{
			m_bCla = (byte)bCla;
			m_bIns = (byte)bIns;
			m_bP1 = (byte)bP1;
			m_bP2 = (byte)bP2;
            m_bP3 = 0x00;
			m_baData = baData;
			m_bLe = bLe;
		}
        public APDUCommand(byte bCla, byte bIns, byte bP1, byte bP2, byte bP3, byte bLe)
        {
            m_bCla = bCla;
            m_bIns = bIns;
            m_bP1 = bP1;
            m_bP2 = bP2;
            m_bP3 = bP3;
            m_baData = null;
            m_bLe = bLe;
        }
        public APDUCommand(string apdu, byte bLe)
        {
            byte[] bytes = Utils.AHex2Bin(apdu);
            if (bytes.Length < 4)
                return;
            m_bCla = bytes[0];
            m_bIns = bytes[1];
            m_bP1 = bytes[2];
            m_bP2 = bytes[3];
            if (bytes.Length > 5)
                m_baData = Utils.AHex2Bin(apdu.Substring(10, apdu.Length - 10));
            m_bLe = bLe;
        }
        public APDUCommand(string apdu, string sData, byte bLe)
        {
            byte[] bytes = Utils.AHex2Bin(apdu);
            if (bytes.Length != 4)
                return;
            m_bCla = bytes[0];
            m_bIns = bytes[1];
            m_bP1 = bytes[2];
            m_bP2 = bytes[3];
            m_baData = Utils.AHex2Bin(sData);
            m_bP3 = (byte)m_baData.Length;
            m_bLe = bLe;
        }
        /// <summary>
        /// Update the current APDU with selected parameters
        /// </summary>
        /// <param name="apduParam">APDU parameters</param>
        public void Update(APDUParam apduParam)
		{
			if (apduParam.UseData)
				m_baData = apduParam.Data;

			if (apduParam.UseLe)
				m_bLe = apduParam.Le;

			if (apduParam.UseP1)
				m_bP1 = apduParam.P1;

			if (apduParam.UseP2)
				m_bP2 = apduParam.P2;

			if (apduParam.UseChannel)
				m_bCla += apduParam.Channel;
		}
		#region Accessors
		public byte Class
		{
			get
			{
				return m_bCla;
			}
		}
		public byte Ins
		{
			get
			{
				return m_bIns;
			}
		}
		public byte P1
		{
			get
			{
				return m_bP1;
			}
		}
		public byte P2
		{
			get
			{
				return m_bP2;
			}
		}
        public byte P3
        {
            get
            {
                return m_bP3;
            }
        }
		public byte[] Data
		{
			get
			{
				return m_baData;
			}
		}
		public byte Le
		{
			get
			{
				return m_bLe;
			}
		}
		#endregion
        public void SetLe(byte b)
        {
            m_bLe = b;
        }
        public void SetP1(byte b)
        {
            m_bP1 = b;
        }
        public void SetP2(byte b)
        {
            m_bP2 = b;
        }
        public void SetApduResponseType(APDUResponseType tp)
        {
            respType = tp;
        }
        public APDUResponseType GetApduResponseType()
        {
            return respType;
        }
		public override string ToString()
		{
			string strData = null;
			byte
				bLc = 0,
				bP3 = m_bLe;

			if (m_baData != null)
			{
				StringBuilder sData = new StringBuilder(m_baData.Length * 2);
				for (int nI = 0; nI < m_baData.Length; nI++)
					sData.AppendFormat("{0:X02}", m_baData[nI]);

				strData = "Data=" + sData.ToString();
				bLc = (byte)m_baData.Length;
				bP3 = bLc;
			}
			StringBuilder strApdu = new StringBuilder();

			strApdu.AppendFormat("Class={0:X02} Ins={1:X02} P1={2:X02} P2={3:X02} P3={4:X02} ",
				m_bCla, m_bIns, m_bP1, m_bP2, bP3);
			if (m_baData != null)
				strApdu.Append(strData);

			return strApdu.ToString();
		}
        public byte[] GetBytes()
        {
            uint RecvLength = (uint)(Le + APDUResponse.SW_LENGTH);
            byte[] ApduBuffer = null;
            byte[] ApduResponse = new byte[Le + APDUResponse.SW_LENGTH];
            SCard_IO_Request ioRequest = new SCard_IO_Request();
            int leIndex = -1;
            // Build the command APDU
            if (Data == null)
            {

                if (P3 > 0)
                {
                    ApduBuffer = new byte[APDUCommand.APDU_MIN_LENGTH + 2];
                    ApduBuffer[4] = P3;
                    ApduBuffer[5] = (byte)Le;
                    leIndex = 5;
                }
                else
                {
                    //ApduBuffer = new byte[APDUCommand.APDU_MIN_LENGTH + ((ApduCmd.Le != 0) ? 1 : 0)];
                    ApduBuffer = new byte[APDUCommand.APDU_MIN_LENGTH + 1];
                    //                    if (ApduCmd.Le != 0)
                    //                    {
                    ApduBuffer[4] = (byte)Le;
                    leIndex = 4;
                    //                    }
                }
            }
            else
            {
                ApduBuffer = new byte[APDUCommand.APDU_MIN_LENGTH + 1 + Data.Length];

                for (int nI = 0; nI < Data.Length; nI++)
                    ApduBuffer[APDUCommand.APDU_MIN_LENGTH + 1 + nI] = Data[nI];

                ApduBuffer[APDUCommand.APDU_MIN_LENGTH] = (byte)Data.Length;
            }
            //if (ApduCmd.Data == null)
            //{
            //    ApduBuffer = new byte[APDUCommand.APDU_MIN_LENGTH + 1];
            //    ApduBuffer[4] = 0x18;
            //leIndex = -1;
            //}
            ApduBuffer[0] = Class;
            ApduBuffer[1] = Ins;
            ApduBuffer[2] = P1;
            ApduBuffer[3] = P2;
            return ApduBuffer;
        }
	}
	public class APDUParam
	{
		byte
			m_bClass = 0,
			m_bChannel = 0,
			m_bP2 = 0,
			m_bP1 = 0;
		byte[] m_baData = null;
		short m_nLe = -1;
		bool
			m_fUseP1 = false,
			m_fUseP2 = false,
			m_fChannel = false,
			m_fData = false,
			m_fClass = false,
			m_fLe = false;

		#region Constructors
		public APDUParam()
		{
		}
		public APDUParam(APDUParam param)
		{
			if (param.m_baData != null)
				param.m_baData.CopyTo(m_baData, 0);
			m_bClass = param.m_bClass;
			m_bChannel = param.m_bChannel;
			m_bP1 = param.m_bP1;
			m_bP2 = param.m_bP2;
			m_nLe = param.m_nLe;

			m_fChannel = param.m_fChannel;
			m_fClass = param.m_fClass;
			m_fData = param.m_fData;
			m_fLe = param.m_fLe;
			m_fUseP1 = param.m_fUseP1;
			m_fUseP2 = param.m_fUseP2;
		}
		public APDUParam(byte bClass, byte bP1, byte bP2, byte[] baData, short nLe)
		{
			this.Class = bClass;
			this.P1 = bP1;
			this.P2 = bP2;
			this.Data = baData;
			this.Le = (byte)nLe;
		}
		#endregion
		public APDUParam Clone()
		{
			return new APDUParam(this);
		}
		public void Reset()
		{
			m_bClass = 0;
			m_bChannel = 0;
			m_bP2 = 0;
			m_bP1 = 0;

			m_baData = null;
			m_nLe = -1;

			m_fUseP1 = false;
			m_fUseP2 = false;
			m_fChannel = false;
			m_fData = false;
			m_fClass = false;
			m_fLe = false;
		}
		#region Flags properties
		public bool UseClass
		{
			get { return m_fClass; }
		}
		public bool UseChannel
		{
			get { return m_fChannel; }
		}
		public bool UseLe
		{
			get { return m_fLe; }
		}
		public bool UseData
		{
			get { return m_fData; }
		}
		public bool UseP1
		{
			get { return m_fUseP1; }
		}
		public bool UseP2
		{
			get { return m_fUseP2; }
		}
		#endregion
		#region Parameter properties
		public byte P1
		{
			get { return m_bP1; }
			set
			{
				m_bP1 = value;
				m_fUseP1 = true;
			}
		}
		public byte P2
		{
			get { return m_bP2; }
			set
			{
				m_bP2 = value;
				m_fUseP2 = true;
			}
		}
		public byte[] Data
		{
			get { return m_baData; }
			set
			{
				m_baData = value;
				m_fData = true;
			}
		}
		public byte Le
		{
			get { return (byte)m_nLe; }
			set
			{
				m_nLe = value;
				m_fLe = true;
			}
		}
		public byte Channel
		{
			get { return m_bChannel; }
			set
			{
				m_bChannel = value;
				m_fChannel = true;
			}
		}
		public byte Class
		{
			get { return m_bClass; }
			set
			{
				m_bClass = value;
				m_fClass = true;
			}
		}
		#endregion
	}
    public enum APDUResponseType
    {
        Standart,
        IdentiveMFPlus
    }
	public class APDUResponse
	{
		public const int SW_LENGTH = 2;
		private byte[] m_baData = null;
		private byte m_bSw1;
		private byte m_bSw2;
        private APDUResponseType respType = APDUResponseType.Standart;
        public APDUResponse(byte[] baData)
        {
            respType = APDUResponseType.Standart;
            ParseResponse(baData);
        }
        public APDUResponse(byte[] baData, APDUResponseType tp)
        {
            respType = tp;
            ParseResponse(baData);
        }
        private void ParseResponse(byte[] baData)
        { 
            // здесь сделано под работу Identive картами mifare plus - в этом случае ридер возвращает один байт, а не два.
            #warning хорошо бы подумать, нормален ли такой костыль
            if (respType == APDUResponseType.IdentiveMFPlus)
            {
                try
                {
                    if (baData.Length > 1)
                    {
                        m_baData = new byte[baData.Length - 1];
                        for (int nI = 0; nI < baData.Length - 1; nI++)
                            m_baData[nI] = baData[nI + 1];
                    }
                    //Console.WriteLine("!!! Attention! на самом деле пришел один байт (см mifare plus for identive)");
                    m_bSw2 = 0x00;
                    m_bSw1 = baData[0];
                }
                catch (Exception ex)
                {
                    int t = 0;
                }
            }
            if (respType == APDUResponseType.Standart)
            {
                if (baData.Length > SW_LENGTH)
                {
                    m_baData = new byte[baData.Length - SW_LENGTH];

                    for (int nI = 0; nI < baData.Length - SW_LENGTH; nI++)
                        m_baData[nI] = baData[nI];
                }

                m_bSw1 = baData[baData.Length - 2];
                m_bSw2 = baData[baData.Length - 1];
            }
		}
		public byte[] Data
		{
			get
			{
				return m_baData;
			}
		}
		public byte SW1
		{
			get
			{
				return m_bSw1;
			}
		}
		public byte SW2
		{
			get
			{
				return m_bSw2;
			}
		}
		public ushort Status
		{
			get
			{
				return (ushort)(((short)m_bSw1 << 8) + (short)m_bSw2);
			}
		}
		public override string ToString()
		{
			string sRet;
			// Display SW1 SW2
			sRet = string.Format("SW={0:X04}", Status);

			if (m_baData != null)
			{
				StringBuilder sData = new StringBuilder(m_baData.Length * 2);
				for (int nI = 0; nI < m_baData.Length; nI++)
					sData.AppendFormat("{0:X02}", m_baData[nI]);
				sRet += " Data=" + sData.ToString();
			}
			return sRet;
		}
	}
}
