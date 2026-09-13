using System.Text.Json;

namespace FirefoxSession2Sql
{
    internal static class Utf8JsonReaderMyExtension
    {
        public static bool ReadFileStream(this ref Utf8JsonReader jr, FileStream fs, ref byte[] buffer)
        {
            if (jr.Read() == true)
            {
                return true;
            }

            if (jr.IsFinalBlock)
            {
                return false;
            }

            GetMoreBytesFromStream(fs, ref buffer, ref jr);

            return jr.Read();
        }

        private static void GetMoreBytesFromStream(FileStream fs, ref byte[] buffer, ref Utf8JsonReader jr)
        {
            int bytesRead, leftoverLen = 0;
            if (jr.BytesConsumed < buffer.Length)
            {
                ReadOnlySpan<byte> leftover = buffer.AsSpan((int)jr.BytesConsumed);
                leftoverLen = leftover.Length;

                if (leftoverLen == buffer.Length)
                {
                    Array.Resize(ref buffer, buffer.Length * 2);
                }

                leftover.CopyTo(buffer);
                bytesRead = fs.Read(buffer.AsSpan(leftoverLen));
            }
            else
            {
                bytesRead = fs.Read(buffer);
            }

            int totalContentLen = leftoverLen + bytesRead;
            Array.Fill(buffer, (byte)' ', totalContentLen, buffer.Length - totalContentLen);
            jr = new Utf8JsonReader(buffer, isFinalBlock: bytesRead == 0, jr.CurrentState);
        }

        public static void SkipArrayStart(this ref Utf8JsonReader jr, FileStream fs, ref byte[] buffer)
        {
            jr.ReadFileStream(fs, ref buffer);
            if (jr.TokenType != JsonTokenType.StartArray)
            {
                throw new FormatException("Invalid format.");
            }
        }

        public static void SkipPropertyValue(this ref Utf8JsonReader jr, FileStream fs, ref byte[] buffer)
        {
            jr.ReadFileStream(fs, ref buffer);

            JsonTokenType t = jr.TokenType;
            JsonTokenType? tc;

            switch (t)
            {
                case JsonTokenType.StartObject:
                    tc = JsonTokenType.EndObject;
                    break;
                case JsonTokenType.StartArray:
                    tc = JsonTokenType.EndArray;
                    break;
                default:
                    tc = null;
                    break;
            }

            if (tc != null)
            {
                int l = 1;
                while (l != 0)
                {
                    jr.ReadFileStream(fs, ref buffer);
                    if (jr.TokenType == t)
                    {
                        l++;
                    }
                    else if (jr.TokenType == tc)
                    {
                        l--;
                    }
                }
            }
        }
    }
}
