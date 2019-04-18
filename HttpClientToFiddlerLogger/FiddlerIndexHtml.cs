using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace HttpClientToFiddlerLogger
{
    internal class FiddlerIndexHtml
    {
        public List<LogEntry> Entries { get; }

        public FiddlerIndexHtml()
        {
            Entries = new List<LogEntry>();
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append(
                "<html>" +
                "<head><style>body,thead,td,a,p{font-family:verdana,sans-serif;font-size: 10px;}</style></head>" +
                "<body><table cols=12>" +
                "<thead><tr><th>&nbsp;</th><th>#</th><th>Result</th><th>Protocol</th><th>Host</th><th>URL</th><th>Body</th><th>Caching</th><th>Content-Type</th><th>Process</th><th>Comments</th><th>Custom</th></tr>" +
                "</thead>" +
                "<tbody>");
            foreach (var entry in Entries)
            {
                sb.Append(entry.Html);
            }
            sb.Append("</tbody></table></body></html>");
            return sb.ToString();
        }
    }

    internal struct LogEntry
    {
        public int Index;
        public int StatusCode;
        public string Protocol;
        public string Host;
        public string Path;
        public long? Body;
        public string Caching;
        public string ContentType;
        public string Process;
        public string Comments;
        public string Custom;

        public string Html => $"<tr><td><a href='raw\\{Index.ToString("D8")}_c.txt'>C</a>&nbsp;<a href='raw\\{Index.ToString("D8")}_s.txt'>S</a>&nbsp;<a href='raw\\{Index.ToString("D8")}_m.xml'>M</a></td></td>" +
                              $"<td>{Index}</td>" +
                              $"<td>{StatusCode}</td>" +
                              $"<td>{Protocol}</td>" +
                              $"<td>{Host}</td>" +
                              $"<td>{Path}</td>" +
                              $"<td>{Body.GetValueOrDefault().ToString(new NumberFormatInfo(){ NumberGroupSeparator = "&#160;"})}</td>" +
                              $"<td>{Caching}</td>" +
                              $"<td>{ContentType}</td>" +
                              $"<td>{Process}</td>" +
                              $"<td>{Comments}</td>" +
                              $"<td>{Custom}</td>" +
                              $"</tr>";
    }
}