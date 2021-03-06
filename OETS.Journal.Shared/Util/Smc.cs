using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Diagnostics;

namespace OETS.Shared.Util
{
	public class Smc
	{
		private string mKey = string.Empty;
		private string mSalt = string.Empty;
		private ServiceProviderEnum mAlgorithm;
		private SymmetricAlgorithm mCryptoService;

		public enum ServiceProviderEnum : int
		{
			Rijndael,
			RC2,
			DES,
			TripleDES
		}

		#region properties
		public string Key
		{
			get { return mKey; }
			set { mKey = value; }
		}

		public string Salt
		{
			get { return mSalt; }
			set { mSalt = value; }
		}
		#endregion properties

		public Smc()
		{
			// Default symmetric algorithm
			mCryptoService = new RijndaelManaged();
			mCryptoService.Mode = CipherMode.CBC;
			mAlgorithm = ServiceProviderEnum.Rijndael;
		}

		public Smc(ServiceProviderEnum serviceProvider)
		{
			// Select symmetric algorithm
			switch (serviceProvider)
			{
				case ServiceProviderEnum.Rijndael:
					mCryptoService = new RijndaelManaged();
					mAlgorithm = ServiceProviderEnum.Rijndael;
					break;
				case ServiceProviderEnum.RC2:
					mCryptoService = new RC2CryptoServiceProvider();
					mAlgorithm = ServiceProviderEnum.RC2;
					break;
				case ServiceProviderEnum.DES:
					mCryptoService = new DESCryptoServiceProvider();
					mAlgorithm = ServiceProviderEnum.DES;
					break;
				case ServiceProviderEnum.TripleDES:
					mCryptoService = new TripleDESCryptoServiceProvider();
					mAlgorithm = ServiceProviderEnum.TripleDES;
					break;
			}
			mCryptoService.Mode = CipherMode.CBC;
		}

		private void SetLegalIV()
		{
			// Set symmetric algorithm
			switch (mAlgorithm)
			{
				case ServiceProviderEnum.Rijndael:
					mCryptoService.IV = new byte[] {0xf, 0x6f, 0x13, 0x2e, 0x35, 0xc2, 0xcd, 0xf9, 0x5, 0x46, 0x9c, 0xea, 0xa8, 0x4b, 0x73, 0xcc};
					break;
				default:
					mCryptoService.IV = new byte[] {0xf, 0x6f, 0x13, 0x2e, 0x35, 0xc2, 0xcd, 0xf9};
					break;
			}
		}

		public virtual byte[] GetLegalKey()
		{
			// Adjust key if necessary, and return a valid key
			if (mCryptoService.LegalKeySizes.Length > 0)
			{
				// Key sizes in bits
				int keySize = mKey.Length*8;
				int minSize = mCryptoService.LegalKeySizes[0].MinSize;
				int maxSize = mCryptoService.LegalKeySizes[0].MaxSize;
				int skipSize = mCryptoService.LegalKeySizes[0].SkipSize;

				if (keySize > maxSize)
				{
					// Extract maximum size allowed
					mKey = mKey.Substring(0, maxSize/8);
				}
				else if (keySize < maxSize)
				{
					// Set valid size
					int validSize = (keySize <= minSize) ? minSize :
						(keySize - keySize%skipSize) + skipSize;
					if (keySize < validSize)
					{
						// Pad the key with asterisk to make up the size
						mKey = mKey.PadRight(validSize/8, '*');
					}
				}
			}
			PasswordDeriveBytes key = new PasswordDeriveBytes(mKey, ASCIIEncoding.ASCII.GetBytes(mSalt));
			return key.GetBytes(mKey.Length);
		}

		public virtual string Encrypt(string plainText)
		{
			byte[] plainByte = ASCIIEncoding.ASCII.GetBytes(plainText);
			byte[] keyByte = GetLegalKey();

			// Set private key
			mCryptoService.Key = keyByte;
			SetLegalIV();

			// Encryptor object
			ICryptoTransform cryptoTransform = mCryptoService.CreateEncryptor();

			// Memory stream object
			MemoryStream ms = new MemoryStream();

			// Crpto stream object
			CryptoStream cs = new CryptoStream(ms, cryptoTransform, CryptoStreamMode.Write);

			// Write encrypted byte to memory stream
			cs.Write(plainByte, 0, plainByte.Length);
			cs.FlushFinalBlock();

			// Get the encrypted byte length
			byte[] cryptoByte = ms.ToArray();

			// Convert into base 64 to enable result to be used in Xml
			return Convert.ToBase64String(cryptoByte, 0, cryptoByte.GetLength(0));
		}

		public virtual string Decrypt(string cryptoText)
		{
			try
			{
				// Convert from base 64 string to bytes
				byte[] cryptoByte = Convert.FromBase64String(cryptoText);
				byte[] keyByte = GetLegalKey();

				// Set private key
				mCryptoService.Key = keyByte;
				SetLegalIV();

				// Decryptor object
				ICryptoTransform cryptoTransform = mCryptoService.CreateDecryptor();
				// Memory stream object
				MemoryStream ms = new MemoryStream(cryptoByte, 0, cryptoByte.Length);

				// Crpto stream object
				CryptoStream cs = new CryptoStream(ms, cryptoTransform, CryptoStreamMode.Read);

				// Get the result from the Crypto stream
				StreamReader sr = new StreamReader(cs);
				return sr.ReadToEnd();
			}
			catch (Exception exc)
			{
				Trace.Write(exc.StackTrace);
				return null;
			}
		}
	}	// Smc
}