using System;
using System.IO;
using System.Text;
using System.Security.Cryptography;

class AESEncryptionTest
{
    public static void Test()
    {
        RNGCryptoServiceProvider rand = new RNGCryptoServiceProvider();

        byte[] iv = new byte[16];
        rand.GetBytes(iv);


        string originalText = "암호화되기전의 테스트입니다. 암호화되기전의 테스트입니다. 암호화되기전의 테스트입니다. 암호화되기전의 테스트입니다. 암호화되기전의 테스트입니다. 암호화되기전의 테스트입니다. 암호화되기전의 테스트입니다. ";

        // CryptoRandom으로 처리하는게 좋을듯...
        string key = "12345678";

        string en = Encrypt(originalText, key);
        string de = Decrypt(en, key);

        UnityEngine.Debug.Log("Original Text is " + originalText);
        UnityEngine.Debug.Log("Encryption text is " + en);
        UnityEngine.Debug.Log("Decrypted text is " + de);
    }

    public static string Encrypt(string plainText, string key)
    {
        RijndaelManaged rijndaelCipher = new RijndaelManaged();
        rijndaelCipher.Mode = CipherMode.CBC;
        rijndaelCipher.Padding = PaddingMode.PKCS7;
        rijndaelCipher.KeySize = 128;
        rijndaelCipher.BlockSize = 128;

        byte[] pwdBytes = Encoding.UTF8.GetBytes(key);
        byte[] keyBytes = new byte[16];
        int len = pwdBytes.Length;
        if (len > keyBytes.Length)
        {
            len = keyBytes.Length;
        }

        Array.Copy(pwdBytes, keyBytes, len);

        rijndaelCipher.Key = keyBytes;
        rijndaelCipher.IV = keyBytes; //이게 블럭 압호화시에 필요한건가??

        ICryptoTransform transform = rijndaelCipher.CreateEncryptor();
        byte[] plainTextBytes = Encoding.UTF8.GetBytes(plainText);
        return Convert.ToBase64String(transform.TransformFinalBlock(plainTextBytes, 0, plainTextBytes.Length));
    }

    public static string Decrypt(string encodedText, string key)
    {
        RijndaelManaged rijndaelCipher = new RijndaelManaged();
        rijndaelCipher.Mode = CipherMode.CBC;
        rijndaelCipher.Padding = PaddingMode.PKCS7;
        rijndaelCipher.KeySize = 128;
        rijndaelCipher.BlockSize = 128;

        byte[] encryptedData = Convert.FromBase64String(encodedText);
        byte[] pwdBytes = Encoding.UTF8.GetBytes(key);
        byte[] keyBytes = new byte[16];
        int len = pwdBytes.Length;
        if (len > keyBytes.Length)
        {
            len = keyBytes.Length;
        }

        Array.Copy(pwdBytes, keyBytes, len);

        rijndaelCipher.Key = keyBytes;
        // IV에 key를 사용하는게 맞는건가?
        // 별도로 지정해도 되는건가??
        rijndaelCipher.IV = keyBytes;
        byte[] plainText = rijndaelCipher.CreateDecryptor().TransformFinalBlock(encryptedData, 0, encryptedData.Length);
        return Encoding.UTF8.GetString(plainText);
    }
}
