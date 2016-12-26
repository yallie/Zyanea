using System.Text;

namespace MessageWire
{
    public static class ByteArrayExtensions
    {
        public static byte[] ConvertToBytes(this string val)
        {
            return Encoding.UTF8.GetBytes(val);
        }

        public static string ConvertToString(this byte[] bytes)
        {
            return Encoding.UTF8.GetString(bytes);
        }

        public static bool IsEqualTo(this byte[] a1, byte[] a2)
        {
            if (a1.Length != a2.Length) return false;
            for (int i = 0; i < a1.Length; i++)
            {
                if (a1[i] != a2[i]) return false;
            }
            return true;
        }
    }
}