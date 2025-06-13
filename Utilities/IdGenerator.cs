using System.Security.Cryptography;

namespace EasyTrade_Crypto.Utilities;
public static class IdGenerator
{
    private const string Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-";

    public static string GenerateShortUniqueId(int length = 9)
    {
        char[] result = new char[length];
        using (var rng = RandomNumberGenerator.Create())
        {
            byte[] buffer = new byte[length];
            rng.GetBytes(buffer);
            for (int i = 0; i < length; i++)
            {
                // Use modulo to get an index into the Chars array.
                int index = buffer[i] % Chars.Length;
                result[i] = Chars[index];
            }
        }
        return new string(result);
    }
}
