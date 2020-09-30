using System;
using System.Collections.Generic;
using System.Text;


using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Encodings;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities;
using Org.BouncyCastle.Utilities.Encoders;

namespace HugeLib
{
    public static class BouncyCastle
    {
        public static void GenerateRsaPair(int keySize)
        {
            RsaKeyPairGenerator rsaGen = new RsaKeyPairGenerator();

            RsaKeyGenerationParameters keyPar = new RsaKeyGenerationParameters(Org.BouncyCastle.Math.BigInteger.ValueOf(3), new SecureRandom(), keySize, 80);
            rsaGen.Init(keyPar);
            AsymmetricCipherKeyPair keyPair = rsaGen.GenerateKeyPair();
            AsymmetricKeyParameter pb = keyPair.Public;
            AsymmetricKeyParameter pr = keyPair.Private;
        }
    }
}
