using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace TmCGPTD
{
    public class AesSettings
    {
        public string? Text { get; set; }
        public string? Key { get; set; }
        public string? Iv { get; set; }
    }

    public static class AesEncryption
    {
        public static string Encrypt(AesSettings text)
        {
            byte[] keyByte = Convert.FromBase64String(text.Key!);
            byte[] ivByte = Convert.FromBase64String(text.Iv!);
            using Aes aes = Aes.Create();
            aes.KeySize = 256;
            aes.Key = keyByte;
            aes.IV = ivByte;

            var keyString = Convert.ToBase64String(aes.Key);
            var ivString = Convert.ToBase64String(aes.IV);

            ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

            using MemoryStream ms = new();
            using CryptoStream cs = new(ms, encryptor, CryptoStreamMode.Write);
            using (StreamWriter sw = new(cs))
            {
                sw.Write(text.Text);
            }

            return Convert.ToBase64String(ms.ToArray());
        }

        //Aes暗号化・複合化キーを下記の形式でappsettings.json設定ファイルに保存し,プロジェクトのルートに置いてAvaloniaResourceとしてビルドしてください。
        //aes.GenerateKey();、 aes.GenerateIV();メソッドを呼び出すことでC#上で生成することもできます。
        //{
        //  "Key": "Aes 256-bit Key",
        //  "Iv": "IV"
        //}

        public static string Decrypt(AesSettings text)
        {
            byte[] keyByte = Convert.FromBase64String(text.Key!);
            byte[] ivByte = Convert.FromBase64String(text.Iv!);
            using Aes aes = Aes.Create();
            aes.KeySize = 256;
            aes.Key = keyByte;
            aes.IV = ivByte;

            ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

            using MemoryStream ms = new(Convert.FromBase64String(text.Text!));
            using CryptoStream cs = new(ms, decryptor, CryptoStreamMode.Read);
            using StreamReader sr = new(cs);

            return sr.ReadToEnd();
        }
    }
}