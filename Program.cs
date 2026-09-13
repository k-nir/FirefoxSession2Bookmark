using System.Text;
using System.Text.Json;
using System.Web;

namespace FirefoxSession2Bookmark
{
    internal class Program
    {
        static Dictionary<string, string> ExtractGroups(FileStream ifs, ref byte[] buffer)
        {
            ifs.Read(buffer);
            Utf8JsonReader jr = new(buffer, isFinalBlock: false, state: default);

            Dictionary<string, string> groupIdName = [];

            int state = 0;
            string? idBuffer = null, nameBuffer = null;
            while (jr.ReadFileStream(ifs, ref buffer))
            {
                if (state == 0)
                {
                    if (jr.TokenType == JsonTokenType.PropertyName)
                    {
                        if (jr.ValueTextEquals(Encoding.UTF8.GetBytes("windows")))
                        {
                            jr.SkipArrayStart(ifs, ref buffer);
                            state = 1;
                        }
                    }
                }
                else if (state == 1) // inside "windows" array
                {
                    if (jr.TokenType == JsonTokenType.StartObject)
                    {
                        state = 2;
                    }
                    else if (jr.TokenType == JsonTokenType.EndArray)
                    {
                        state = 0;
                    }
                }
                else if (state == 2) // inside "windows" element
                {
                    if (jr.TokenType == JsonTokenType.PropertyName)
                    {
                        if (jr.ValueTextEquals(Encoding.UTF8.GetBytes("groups")))
                        {
                            jr.SkipArrayStart(ifs, ref buffer);
                            state = 3;
                        }
                        else
                        {
                            jr.SkipPropertyValue(ifs, ref buffer);
                        }
                    }
                    else if (jr.TokenType == JsonTokenType.EndObject)
                    {
                        state = 1;
                    }
                }
                else if (state == 3) // inside "windows/groups" array
                {
                    if (jr.TokenType == JsonTokenType.StartObject)
                    {
                        state = 4;
                    }
                    else if (jr.TokenType == JsonTokenType.EndArray)
                    {
                        state = 2;
                    }
                }
                else if (state == 4) // inside "windows/groups" element
                {
                    if (jr.TokenType == JsonTokenType.PropertyName)
                    {
                        if (jr.ValueTextEquals(Encoding.UTF8.GetBytes("id")))
                        {
                            jr.ReadFileStream(ifs, ref buffer);
                            idBuffer = jr.GetString();
                        }
                        else if (jr.ValueTextEquals(Encoding.UTF8.GetBytes("name")))
                        {
                            jr.ReadFileStream(ifs, ref buffer);
                            nameBuffer = jr.GetString();
                        }
                        else
                        {
                            jr.SkipPropertyValue(ifs, ref buffer);
                        }
                    }
                    else if (jr.TokenType == JsonTokenType.EndObject)
                    {
                        if (idBuffer == null || nameBuffer == null)
                        {
                            throw new FormatException("Group has no id or name.");
                        }

                        groupIdName.Add(idBuffer, nameBuffer);

                        idBuffer = null;
                        nameBuffer = null;
                        state = 3;
                    }
                }
            }

            return groupIdName;
        }

        internal class UrlAndTitle
        {
            public string? Url { get; set; } = null;
            public string? Title { get; set; } = null;
        }

        static void Tab2BookMark(FileStream ifs, ref byte[] buffer, StreamWriter ow, Dictionary<string, string> groups)
        {
            ifs.Read(buffer);
            Utf8JsonReader jr = new(buffer, isFinalBlock: false, state: default);

            int state = 0;
            string? gid = null;
            string? openedGroup = null;
            UrlAndTitle? utBuffer = null;
            int? utIndex = null;
            List<UrlAndTitle> utsBuffer = [];
            while (jr.ReadFileStream(ifs, ref buffer))
            {
                if (state == 0)
                {
                    if (jr.TokenType == JsonTokenType.PropertyName)
                    {
                        if (jr.ValueTextEquals(Encoding.UTF8.GetBytes("windows")))
                        {
                            jr.SkipArrayStart(ifs, ref buffer);
                            state = 1;
                        }
                    }
                }
                else if (state == 1) // inside "windows" array
                {
                    if (jr.TokenType == JsonTokenType.StartObject)
                    {
                        state = 2;
                    }
                    else if (jr.TokenType == JsonTokenType.EndArray)
                    {
                        state = 0;
                    }
                }
                else if (state == 2) // inside "windows" element
                {
                    if (jr.TokenType == JsonTokenType.PropertyName)
                    {
                        if (jr.ValueTextEquals(Encoding.UTF8.GetBytes("tabs")))
                        {
                            jr.SkipArrayStart(ifs, ref buffer);
                            state = 3;
                        }
                        else
                        {
                            jr.SkipPropertyValue(ifs, ref buffer);
                        }
                    }
                    else if (jr.TokenType == JsonTokenType.EndObject)
                    {
                        state = 1;
                    }
                }
                else if (state == 3) // inside "windows/tabs" array
                {
                    if (jr.TokenType == JsonTokenType.StartObject)
                    {
                        state = 4;
                    }
                    else if (jr.TokenType == JsonTokenType.EndArray)
                    {
                        state = 2;
                    }
                }
                else if (state == 4) // inside "windows/tabs" element
                {
                    if (jr.TokenType == JsonTokenType.PropertyName)
                    {
                        if (jr.ValueTextEquals(Encoding.UTF8.GetBytes("entries")))
                        {
                            jr.SkipArrayStart(ifs, ref buffer);
                            state = 5;
                        }
                        else if (jr.ValueTextEquals(Encoding.UTF8.GetBytes("index")))
                        {
                            jr.ReadFileStream(ifs, ref buffer);
                            utIndex = jr.GetInt32();
                        }
                        else if (jr.ValueTextEquals(Encoding.UTF8.GetBytes("groupId")))
                        {
                            jr.ReadFileStream(ifs, ref buffer);
                            gid = jr.GetString();
                        }
                        else
                        {
                            jr.SkipPropertyValue(ifs, ref buffer);
                        }
                    }
                    else if (jr.TokenType == JsonTokenType.EndObject)
                    {
                        if (utIndex != null)
                        {
                            UrlAndTitle ut = utsBuffer[utIndex.Value - 1];

                            if (gid != null && openedGroup == null)
                            {
                                ow.WriteLine("<DT><H3>{0}</H3>", HttpUtility.HtmlEncode(groups[gid]));
                                ow.WriteLine("<DL><p>");
                                openedGroup = gid;
                            }
                            else if (gid == null && openedGroup != null)
                            {
                                ow.WriteLine("</DL><p>");
                                openedGroup = null;
                            }
                            else if (gid != null && openedGroup != null)
                            {
                                if (gid != openedGroup)
                                {
                                    ow.WriteLine("</DL><p>");
                                    ow.WriteLine("<DT><H3>{0}</H3>", HttpUtility.HtmlEncode(groups[gid]));
                                    ow.WriteLine("<DL><p>");
                                    openedGroup = gid;
                                }
                            }

                            ow.WriteLine("<DT><A HREF=\"{0}\">", HttpUtility.HtmlAttributeEncode(ut.Url));
                            ow.WriteLine("{0}</A>", HttpUtility.HtmlEncode(ut.Title));
                        }
                        else
                        {
                            throw new FormatException("No index.");
                        }

                        utsBuffer.Clear();
                        utIndex = null;
                        gid = null;
                        state = 3;
                    }
                }
                else if (state == 5) // inside "windows/tabs/entries" array
                {
                    if (jr.TokenType == JsonTokenType.StartObject)
                    {
                        state = 6;
                    }
                    else if (jr.TokenType == JsonTokenType.EndArray)
                    {
                        state = 4;
                    }
                }
                else if (state == 6) // inside "windows/tabs/entries" element
                {
                    if (jr.TokenType == JsonTokenType.PropertyName)
                    {
                        string? s = jr.GetString();
                        if (jr.ValueTextEquals(Encoding.UTF8.GetBytes("url")))
                        {
                            jr.ReadFileStream(ifs, ref buffer);
                            utBuffer ??= new();
                            utBuffer.Url = jr.GetString() ?? throw new FormatException("No URL.");
                        }
                        else if (jr.ValueTextEquals(Encoding.UTF8.GetBytes("title")))
                        {
                            jr.ReadFileStream(ifs, ref buffer);
                            utBuffer ??= new();
                            utBuffer.Title = jr.GetString() ?? throw new FormatException("No Title.");
                        }
                        else
                        {
                            jr.SkipPropertyValue(ifs, ref buffer);
                        }
                    }
                    else if (jr.TokenType == JsonTokenType.EndObject)
                    {
                        if (utBuffer == null || utBuffer.Url == null || utBuffer.Title == null)
                        {
                            throw new FormatException("No URL or title.");
                        }

                        utsBuffer.Add(utBuffer!);
                        utBuffer = null;
                        state = 5;
                    }
                }
            }

            if (openedGroup != null)
            {
                ow.WriteLine("</DL><p>");
            }

            return;
        }

        static void Main(string[] args)
        {
            using FileStream ifs = File.OpenRead(args[0]);

            byte[] buffer = new byte[1024 * 4];
            Dictionary<string, string> groups = ExtractGroups(ifs, ref buffer);

            ifs.Seek(0, SeekOrigin.Begin);
            Array.Fill(buffer, (byte)' ');

            using FileStream ofs = File.OpenWrite(args[1]);
            using StreamWriter ow = new(ofs);
            ow.WriteLine("<!DOCTYPE NETSCAPE-Bookmark-file-1>");
            ow.WriteLine("<!-- This is an automatically generated file.");
            ow.WriteLine("     It will be read and overwritten.");
            ow.WriteLine("     DO NOT EDIT! -->");
            ow.WriteLine("<META HTTP-EQUIV=\"Content-Type\" CONTENT=\"text/html; charset=UTF-8\">");
            ow.WriteLine("<TITLE>Bookmarks</TITLE>");
            ow.WriteLine("<H1>Bookmarks Menu</H1>");
            ow.WriteLine();
            ow.WriteLine("<DL><p>");
            ow.WriteLine("<DT><H3>Restored from {0}</H3>", HttpUtility.HtmlEncode(Path.GetFileName(args[0])));
            ow.WriteLine("<DL><p>");

            Tab2BookMark(ifs, ref buffer, ow, groups);

            ow.WriteLine("</DL><p>");
            ow.WriteLine("</DL>");
            ow.Flush();
            return;
        }
    }
}
