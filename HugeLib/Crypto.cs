using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Security.Cryptography;
using HugeLib.Scripter;

namespace HugeLib.Crypto
{
    public static class MyCrypto
    {
        public static string DES_EncryptData(byte[] indata, byte[] desKey, CipherMode cm, PaddingMode pm)
        {
            DESCryptoServiceProvider des = new DESCryptoServiceProvider();
            byte[] outdata = new byte[des.FeedbackSize];
            byte[] resdata = new byte[((indata.Length % 8) == 0) ? indata.Length : ((indata.Length / 8) + 1) * 8];
            byte[] iv = new byte[des.BlockSize/8];
            int i = 0, o = 0;
            for (i = 0; i < des.BlockSize/8; i++)
                iv[i] = 0;
            des.Mode = cm;
            des.Padding = pm;
            ICryptoTransform ct = null;

            if (System.Security.Cryptography.DES.IsWeakKey(desKey))
            {
                MethodInfo mi = des.GetType().GetMethod("_NewEncryptor", BindingFlags.NonPublic | BindingFlags.Instance);
                object[] par = { desKey, des.Mode, iv, des.FeedbackSize, 0 };
                ct = (ICryptoTransform)mi.Invoke(des, par);
            }
            else
                ct = des.CreateEncryptor(desKey, iv);
            i = 0; o = 0;
            while ((i+des.BlockSize/8) < indata.Length)
            {
                ct.TransformBlock(indata, i, des.BlockSize/8, outdata, 0);
                Array.Copy(outdata, 0, resdata, o, des.BlockSize/8);
                i += des.BlockSize/8;
                o += des.BlockSize / 8;
            }
            byte[] temp = ct.TransformFinalBlock(indata, i, indata.Length - i);
            Array.Copy(temp, 0, resdata, o, temp.Length);
            des.Clear();
            return Utils.Bin2AHex(resdata);
        }
        public static string DES_EncryptData(string data, byte[] desKey, CipherMode cm, PaddingMode pm)
        {
            return DES_EncryptData(Utils.AHex2Bin(data), desKey, cm, pm);
        }
        public static string DES_DecryptData(byte[] indata, byte[] desKey, CipherMode cm, PaddingMode pm)
        {
            DESCryptoServiceProvider des = new DESCryptoServiceProvider();
            byte[] outdata = new byte[des.FeedbackSize];
            byte[] resdata = new byte[((indata.Length % 8) == 0) ? indata.Length : ((indata.Length / 8) + 1) * 8];
            byte[] iv = new byte[des.BlockSize / 8];
            int i = 0, o = 0;
            for (i = 0; i < des.BlockSize / 8; i++)
                iv[i] = 0;
            des.Mode = cm;
            des.Padding = pm;
            ICryptoTransform ct = null;

            if (System.Security.Cryptography.DES.IsWeakKey(desKey))
            {
                MethodInfo mi = des.GetType().GetMethod("_NewDecryptor", BindingFlags.NonPublic | BindingFlags.Instance);
                object[] par = { desKey, des.Mode, iv, des.FeedbackSize, 0 };
                ct = (ICryptoTransform)mi.Invoke(des, par);
            }
            else
                ct = des.CreateDecryptor(desKey, iv);
            i = 0; o = 0;
            while ((i + des.BlockSize / 8) < indata.Length)
            {
                ct.TransformBlock(indata, i, des.BlockSize / 8, outdata, 0);
                Array.Copy(outdata, 0, resdata, o, des.BlockSize / 8);
                i += des.BlockSize / 8;
                o += des.BlockSize / 8;
            }
            byte[] temp = ct.TransformFinalBlock(indata, i, indata.Length - i);
            Array.Copy(temp, 0, resdata, o, temp.Length);
            des.Clear();
            return Utils.Bin2AHex(resdata);
        }
        public static string DES_DecryptData(string data, byte[] desKey, CipherMode cm, PaddingMode pm)
        {
            return DES_DecryptData(Utils.AHex2Bin(data), desKey, cm, pm);
        }
        public static string TripleDES_EncryptData(string data, byte[] tdesKey, CipherMode cm, PaddingMode pm)
        {
            TripleDESCryptoServiceProvider tdes = new TripleDESCryptoServiceProvider();
            byte[] iv = new byte[tdes.BlockSize / 8];
            for (int i = 0; i < tdes.BlockSize / 8; i++)
                iv[i] = 0;
            return TripleDES_EncryptData(Utils.AHex2Bin(data), tdesKey, iv, cm, pm);
        }
        public static string TripleDES_EncryptData(byte[] indata, byte[] tdesKey, CipherMode cm, PaddingMode pm)
        {
            TripleDESCryptoServiceProvider tdes = new TripleDESCryptoServiceProvider();
            byte[] iv = new byte[tdes.BlockSize / 8];
            for (int i = 0; i < tdes.BlockSize / 8; i++)
                iv[i] = 0;
            return TripleDES_EncryptData(indata, tdesKey, iv, cm, pm);
        }
        public static string TripleDES_EncryptData(byte[] indata, byte[] tdesKey, byte[] iv, CipherMode cm, PaddingMode pm)
        {
            TripleDESCryptoServiceProvider tdes = new TripleDESCryptoServiceProvider();
            byte[] outdata = new byte[tdes.FeedbackSize];
            byte[] resdata = new byte[((indata.Length % 8) == 0) ? indata.Length : ((indata.Length / 8) + 1) * 8];
            tdes.Mode = cm;
            tdes.Padding = pm;
            ICryptoTransform ct = null;

            if (System.Security.Cryptography.TripleDES.IsWeakKey(tdesKey))
            {
                MethodInfo mi = tdes.GetType().GetMethod("_NewEncryptor", BindingFlags.NonPublic | BindingFlags.Instance);
                object[] par = { tdesKey, tdes.Mode, iv, tdes.FeedbackSize, 0 };
                ct = (ICryptoTransform)mi.Invoke(tdes, par);
            }
            else
                ct = tdes.CreateEncryptor(tdesKey, iv);
            int i = 0, o = 0;
            while ((i + tdes.BlockSize / 8) < indata.Length)
            {
                ct.TransformBlock(indata, i, tdes.BlockSize / 8, outdata, 0);
                Array.Copy(outdata, 0, resdata, o, tdes.BlockSize / 8);
                i += tdes.BlockSize / 8;
                o += tdes.BlockSize / 8;
            }
            byte[] temp = ct.TransformFinalBlock(indata, i, indata.Length - i);
            Array.Copy(temp, 0, resdata, o, temp.Length);
            tdes.Clear();
            return Utils.Bin2AHex(resdata);
        }
        public static string TripleDES_DecryptData(string data, byte[] tdesKey, CipherMode cm, PaddingMode pm)
        {
            TripleDESCryptoServiceProvider tdes = new TripleDESCryptoServiceProvider();
            byte[] iv = new byte[tdes.BlockSize / 8];
            for (int i = 0; i < tdes.BlockSize / 8; i++)
                iv[i] = 0;
            return TripleDES_DecryptData(Utils.AHex2Bin(data), tdesKey, iv, cm, pm);
        }
        public static string TripleDES_DecryptData(byte[] indata, byte[] tdesKey, CipherMode cm, PaddingMode pm)
        {
            TripleDESCryptoServiceProvider tdes = new TripleDESCryptoServiceProvider();
            byte[] iv = new byte[tdes.BlockSize / 8];
            for (int i = 0; i < tdes.BlockSize / 8; i++)
                iv[i] = 0;
            return TripleDES_DecryptData(indata, tdesKey, iv, cm, pm);
        }
        public static string TripleDES_DecryptData(byte[] indata, byte[] tdesKey, byte[] iv, CipherMode cm, PaddingMode pm)
        {
            TripleDESCryptoServiceProvider tdes = new TripleDESCryptoServiceProvider();
            byte[] outdata = new byte[tdes.FeedbackSize];
            byte[] resdata = new byte[((indata.Length % 8) == 0) ? indata.Length : ((indata.Length / 8) + 1) * 8];
            tdes.Mode = cm;
            tdes.Padding = pm;
            ICryptoTransform ct = null;

            if (System.Security.Cryptography.TripleDES.IsWeakKey(tdesKey))
            {
                MethodInfo mi = tdes.GetType().GetMethod("_NewEncryptor", BindingFlags.NonPublic | BindingFlags.Instance);
                object[] par = { tdesKey, tdes.Mode, iv, tdes.FeedbackSize, 1 };
                ct = (ICryptoTransform)mi.Invoke(tdes, par);
            }
            else
                ct = tdes.CreateDecryptor(tdesKey, iv);
            int i = 0, o = 0;
            while ((i + tdes.BlockSize / 8) < indata.Length)
            {
                ct.TransformBlock(indata, i, tdes.BlockSize / 8, outdata, 0);
                Array.Copy(outdata, 0, resdata, o, tdes.BlockSize / 8);
                i += tdes.BlockSize / 8;
                o += tdes.BlockSize / 8;
            }
            byte[] temp = ct.TransformFinalBlock(indata, i, indata.Length - i);
            Array.Copy(temp, 0, resdata, o, temp.Length);
            tdes.Clear();
            return Utils.Bin2AHex(resdata);
        }
        public static string TripleDES_DecryptData(byte[] indata, string pwd, CipherMode cm)
        {
            PasswordDeriveBytes pdb = new PasswordDeriveBytes(pwd, null);
            //pdb.GetBytes(16);
            return TripleDES_DecryptData(indata, pdb.GetBytes(16), cm, PaddingMode.None);
        }
        public static string AES_EncryptData(byte[] indata, byte[] aesKey, CipherMode cm, PaddingMode pm)
        {
            byte[] iv = new byte[aesKey.Length];
            Array.Clear(iv, 0, aesKey.Length);
            return AES_EncryptData(indata, aesKey, iv, cm, pm);
        }
        public static string AES_EncryptData(string data, byte[] aesKey, CipherMode cm, PaddingMode pm)
        {
            return AES_EncryptData(Utils.AHex2Bin(data), aesKey, cm, pm);
        }
        public static string AES_EncryptData(byte[] indata, byte[] aesKey, byte[] iv, CipherMode cm, PaddingMode pm)
        {
            AesCryptoServiceProvider aes = new AesCryptoServiceProvider();
            byte[] resdata = null;
            aes.Mode = cm;
            aes.Padding = pm;

            using (ICryptoTransform ct = aes.CreateEncryptor(aesKey, iv))
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, ct, CryptoStreamMode.Write))
                    {
                        cs.Write(indata, 0, indata.Length);
                        cs.FlushFinalBlock();
                        resdata = ms.ToArray();
                    }
                }
            }
            return Utils.Bin2AHex(resdata);
        }
        public static string AES_DecryptData(byte[] indata, byte[] aesKey, CipherMode cm, PaddingMode pm)
        {
            byte[] iv = new byte[aesKey.Length];
            Array.Clear(iv, 0, aesKey.Length);
            return AES_DecryptData(indata, aesKey, iv, cm, pm);
        }
        public static string AES_DecryptData(string data, byte[] aesKey, CipherMode cm, PaddingMode pm)
        {
            return AES_DecryptData(Utils.AHex2Bin(data), aesKey, cm, pm);
        }
        public static string AES_DecryptData(byte[] indata, byte[] aesKey, byte[] iv, CipherMode cm, PaddingMode pm)
        {
            AesCryptoServiceProvider aes = new AesCryptoServiceProvider();
            byte[] resdata = null;
            aes.Mode = cm;
            aes.Padding = pm;

            using (ICryptoTransform ct = aes.CreateDecryptor(aesKey, iv))
            {
                using (MemoryStream ms = new MemoryStream(indata))
                {
                    using (CryptoStream cs = new CryptoStream(ms, ct, CryptoStreamMode.Read))
                    {
                        resdata = new byte[indata.Length];
                        cs.Read(resdata, 0, resdata.Length);
                    }
                }
            }
            return Utils.Bin2AHex(resdata);
        }
        public static string Gost_Encrypt(byte[] indata, string pwd, byte[] iv, CipherMode cm)
        {
            GOST.GOSTsymmManaged gost = new GOST.GOSTsymmManaged();
            gost.LoadTestSBoxes(3);
            gost.Key = Utils.AHex2Bin(pwd);
            gost.IV = iv;
            gost.Mode = cm;
            gost.Padding = PaddingMode.None;

            GOST.GOSTsymmTransform ct_e = (GOST.GOSTsymmTransform)gost.CreateEncryptor();
            ct_e.UseExpandedSBoxes = false;

            MemoryStream ms2 = new MemoryStream();
            CryptoStream cs2 = new CryptoStream(ms2, ct_e, CryptoStreamMode.Write);

            cs2.Write(indata, 0, indata.Length);
            cs2.Close();
            return Utils.Bin2AHex(ms2.ToArray());
        }
        public static string Gost_Decrypt(byte[] indata, string pwd, byte[] iv, CipherMode cm)
        {
            GOST.GOSTsymmManaged gost = new GOST.GOSTsymmManaged();
            gost.LoadTestSBoxes(3);
            gost.Key = Utils.AHex2Bin(pwd);
            gost.IV = iv;
            gost.Mode = cm;
            gost.Padding = PaddingMode.None;

            GOST.GOSTsymmTransform ct_d = (GOST.GOSTsymmTransform)gost.CreateDecryptor();
            
            ct_d.UseExpandedSBoxes = true;

            MemoryStream ms2 = new MemoryStream();
            CryptoStream cs2 = new CryptoStream(ms2, ct_d, CryptoStreamMode.Write);

            cs2.Write(indata, 0, indata.Length);
            cs2.Close();
            return Utils.Bin2AHex(ms2.ToArray());
        }
        public static string CryptoProHash(byte[] indata)
        {
            //CryptoPro.Sharpei.Gost3411 gost = new Gost3411CryptoServiceProvider();
            //return Utils.Bin2AHex(gost.ComputeHash(indata));
            return "";
        }
        public static string Hash(byte[] indata)
        {
            SHA1Managed sha = new SHA1Managed();
            return Utils.Bin2AHex(sha.ComputeHash(indata));
        }
        public static string Hash(string ahex)
        {
            SHA1Managed sha = new SHA1Managed();
            return Utils.Bin2AHex(sha.ComputeHash(Utils.AHex2Bin(ahex)));
        }
        public static string RSA_DecryptData(byte[] indata, string keyname)
        {
            if (keyname.IndexOf(".prv.") >= 0)
            {
                //расшифровка на секретном ключе
                return "";
            }
            else
            {
                //расшифровка на публичном ключе
                RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
                rsa.FromXmlString(File.ReadAllText(keyname));
                RSAParameters rsaP = rsa.ExportParameters(false);
                BigInteger modul = new BigInteger(rsaP.Modulus);
                BigInteger exp = new BigInteger(rsaP.Exponent);
                BigInteger dat = new BigInteger(indata);
                rsa.Clear();
                return Utils.Bin2AHex(dat.modPow(exp, modul).getBytes());
            }
        }
        /// <summary>
        /// Mac - Full TripleDes
        /// </summary>
        /// <param name="data">данные</param>
        /// <param name="tdesKey">ключ</param>
        /// <returns></returns>
        public static string Mac1(string data, byte[] tdesKey)
        {
            int i = 0;
            byte[] iv = new byte[8];
            for (i = 0; i < 8; i++)
                iv[i] = 0;
            i = 0;
            string temp = "";
            data = String.Format("{0}80", data);
            while(data.Length%16 != 0)
                data = String.Format("{0}00", data);
            while (i<data.Length)
            {
                byte[] bytes = Utils.AHex2Bin(data.Substring(i, 16));
                for (int t=0;t<8;t++)
                    bytes[t] = (byte)(bytes[t]^iv[t]);
                temp = TripleDES_EncryptData(bytes, tdesKey, CipherMode.ECB, PaddingMode.None);
                Array.Copy(Utils.AHex2Bin(temp), iv, 8);
                i += 16;
            }
            return temp;
        }
        /// <summary>
        /// Mac - Full TripleDEs
        /// </summary>
        /// <param name="data">данный</param>
        /// <param name="tdesKey">ключ</param>
        /// <param name="iv">предыдущий mac</param>
        /// <returns></returns>
        public static string Mac1(string data, byte[] tdesKey, byte[] iv)
        {
            int i = 0;
            string temp = "";
            data = String.Format("{0}80", data);
            while (data.Length % 16 != 0)
                data = String.Format("{0}00", data);
            while (i < data.Length)
            {
                byte[] bytes = Utils.AHex2Bin(data.Substring(i, 16));
                for (int t = 0; t < 8; t++)
                    bytes[t] = (byte)(bytes[t] ^ iv[t]);
                temp = TripleDES_EncryptData(bytes, tdesKey, CipherMode.ECB, PaddingMode.None);
                Array.Copy(Utils.AHex2Bin(temp), iv, 8);
                i += 16;
            }
            return temp;
        }
        /// <summary>
        /// Mac - Full TripleDES без паддинга
        /// </summary>
        /// <param name="data">данные</param>
        /// <param name="tdesKey">ключ</param>
        /// <returns></returns>
        public static string Mac1n(string data, byte[] tdesKey)
        {
            int i = 0;
            byte[] iv = new byte[8];
            for (i = 0; i < 8; i++)
                iv[i] = 0;
            i = 0;
            string temp = "";
            while (i < data.Length)
            {
                byte[] bytes = Utils.AHex2Bin(data.Substring(i, 16));
                for (int t = 0; t < 8; t++)
                    bytes[t] = (byte)(bytes[t] ^ iv[t]);
                temp = TripleDES_EncryptData(bytes, tdesKey, CipherMode.ECB, PaddingMode.None);
                Array.Copy(Utils.AHex2Bin(temp), iv, 8);
                i += 16;
            }
            return temp;
        }
        /// <summary>
        /// Mac - Full TripleDES без паддинга
        /// </summary>
        /// <param name="data">данный</param>
        /// <param name="tdesKey">ключ</param>
        /// <param name="iv">предыдущий mac</param>
        /// <returns></returns>
        public static string Mac1n(string data, byte[] tdesKey, byte[] iv)
        {
            int i = 0;
            string temp = "";
            while (i < data.Length)
            {
                byte[] bytes = Utils.AHex2Bin(data.Substring(i, 16));
                for (int t = 0; t < 8; t++)
                    bytes[t] = (byte)(bytes[t] ^ iv[t]);
                temp = TripleDES_EncryptData(bytes, tdesKey, CipherMode.ECB, PaddingMode.None);
                Array.Copy(Utils.AHex2Bin(temp), iv, 8);
                i += 16;
            }
            return temp;
        }
        /// <summary>
        /// Mac - SingleDes + FinalTripleDes
        /// </summary>
        /// <param name="data">данные</param>
        /// <param name="tdesKey">ключ</param>
        /// <returns></returns>
        public static string Mac2(string data, byte[] tdesKey)
        {
            int i = 0;
            byte[] iv = new byte[8];
            byte[] desKey = new byte[8];
            Array.Copy(tdesKey, desKey, 8);
            for (i = 0; i < 8; i++)
                iv[i] = 0;
            i = 0;
            string temp = "";
            data = String.Format("{0}80", data);
            while (data.Length % 16 != 0)
                data = String.Format("{0}00", data);
            while (i < data.Length)
            {
                byte[] bytes = Utils.AHex2Bin(data.Substring(i, 16));
                for (int t = 0; t < 8; t++)
                    bytes[t] = (byte)(bytes[t] ^ iv[t]);
                if (i+16 < data.Length)
                    temp = DES_EncryptData(bytes, desKey, CipherMode.ECB, PaddingMode.None);
                else
                    temp = TripleDES_EncryptData(bytes, tdesKey, CipherMode.ECB, PaddingMode.None);
                Array.Copy(Utils.AHex2Bin(temp), iv, 8);
                i += 16;
            }
            return temp;
        }
        /// <summary>
        /// Mac - SingleDes + FinalTripleDes
        /// </summary>
        /// <param name="data">данные</param>
        /// <param name="tdesKey">ключ</param>
        /// <param name="iv">вектор</param>
        /// <returns></returns>
        public static string Mac2(string data, byte[] tdesKey, byte[] iv)
        {
            int i = 0;
            byte[] desKey = new byte[8];
            Array.Copy(tdesKey, desKey, 8);
            i = 0;
            string temp = "";
            data = String.Format("{0}80", data);
            while (data.Length % 16 != 0)
                data = String.Format("{0}00", data);
            while (i < data.Length)
            {
                byte[] bytes = Utils.AHex2Bin(data.Substring(i, 16));
                for (int t = 0; t < 8; t++)
                    bytes[t] = (byte)(bytes[t] ^ iv[t]);
                if (i + 16 < data.Length)
                    temp = DES_EncryptData(bytes, desKey, CipherMode.ECB, PaddingMode.None);
                else
                    temp = TripleDES_EncryptData(bytes, tdesKey, CipherMode.ECB, PaddingMode.None);
                Array.Copy(Utils.AHex2Bin(temp), iv, 8);
                i += 16;
            }
            return temp;
        }
        public static string Xor(byte[] b1, byte[] b2)
        {
            int len = (b1.Length > b2.Length) ? b1.Length : b2.Length;
            byte[] res = new byte[len];
            for (int i = 0; i < len; i++)
                res[i] = (byte)(((i < b1.Length) ? b1[i] : (byte)0) ^ ((i < b2.Length) ? b2[i] : (byte)0));
            return Utils.Bin2AHex(res);
        }
        public static string Xor(string data1, string data2)
        {
            return Xor(Utils.AHex2Bin(data1), Utils.AHex2Bin(data2));
        }
        public static string AdjustParity(string data)
        {
            byte[] bt = Utils.AHex2Bin(data);
            for (int i = 0; i < bt.Length; i++)
            {
                bool fl = false;
                for (int j = 0; j < 8; j++)
                {
                    if (Convert.ToBoolean(bt[i] & (0x01 << j)))
                        fl = !fl;
                }
                if (!fl)
                    bt[i] ^= 0x01;
            }
            
            return Utils.Bin2AHex(bt);
        }
        public static byte ReflectByte(byte b)
        {
            return (byte)((b * 0x0202020202 & 0x010884422010) % 1023);
        }
        public static string CRC8(byte[] data, CRC8_POLY poly)
        {
            byte[] table = new byte[256];
            for (int i = 0; i < 256; ++i)
            {
                int curr = (byte)i;
                for(int j=0;j<8;++j)
                {
                    if ((curr & 0x80) != 0)
                        curr = (curr << 1) ^ (int)poly;
                    else
                        curr <<= 1;
                }
                table[i] = (byte)curr;
            }
            byte[] res = new byte[] {ReflectByte(0xe3)}; //начальная инициализация
            foreach (byte b in data)
                res[0] = table[res[0] ^ b];
            return Utils.Bin2AHex(res);
        }
        /// <summary>
        /// CRC16 - CCITT
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static string CRC16C(string data, InitialCrcValue icv)
        {
            return CRC16C(Utils.AHex2Bin(data), icv);
        }
        /// <summary>
        /// CRC16 - CCITT
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static string CRC16C(byte[] data, InitialCrcValue icv)
        {
            ushort[] tab = new ushort[256];
            ushort t = 0, a = 0, crc = (ushort)icv;
            for (int i = 0; i < 256; i++)
            {
                t = 0;
                a = (ushort)(i << 8);
                for (int j = 0; j < 8; j++)
                {
                    if (((t ^ a) & 0x8000) != 0)
                        t = (ushort)((t << 1) ^ 0x1021);
                    else
                        t <<= 1;
                    a <<= 1;
                }
                tab[i] = t;
            }
            for (int i = 0; i < data.Length; i++)
            {
                crc = (ushort)((crc << 8) ^ tab[((crc >> 8) ^ (0xFF & data[i]))]);
            }
            return Utils.Bin2AHex(BitConverter.GetBytes(crc));
        }
        public static string LRC(byte[] data)
        {
            byte[] res = new byte[]{0x00};
            for (int i = 0; i < data.Length; i++)
                res[0] = (byte)(res[0] ^ data[i]);
            return Utils.Bin2AHex(res);
        }
        public static string LRC(byte[] data, int start, int length)
        {
            byte[] res = new byte[] { 0x00 };
            for (int i = start; i < data.Length && i-start < length; i++)
                res[0] = (byte)(res[0] ^ data[i]);
            return Utils.Bin2AHex(res);
        }



        public enum InitialCrcValue { Zeros = 0x0000, FFs = 0xffff }
        public enum CRC8_POLY
        {
            CRC8 = 0xd5,
            CRC8_CCITT = 0x07,
            CRC8_DALLAS_MAXIM = 0x31,
            CRC8_SAE_J1850 = 0x1D,
            CRC_8_WCDMA = 0x9b,
        };

        public const int PROV_RSA_FULL = 1;
        public const int AT_KEYEXCHANGE = 1;

    }
    public enum HSMType
    {
        None,
        SAMCard,
        HSexe,
        SafenetEFT,
        SafenetOG,
        Thales
    }
    public abstract class HSMBase
    {
        private HSMType htype;
        public HSMBase()
        {
            htype = HSMType.None;
        }
        public abstract void Init();
    }
    public class EFT : HSMBase
    {
        public override void Init()
        {
//            throw new Exception("The method or operation is not implemented.");
        }
    }
}
