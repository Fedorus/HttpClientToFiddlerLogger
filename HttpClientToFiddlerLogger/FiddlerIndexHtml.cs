using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace HttpClientToFiddlerLogger
{
    internal class FiddlerIndexHtml
    {
        public int LastIndex { get; private set; }
        public List<LogEntry> Entries { get; }

        public FiddlerIndexHtml()
        {
            Entries = new List<LogEntry>();
        }
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append(
                "<html><head><style>body,thead,td,a,p{font-family:verdana,sans-serif;font-size: 10px;}</style></head><body><table cols=12><thead><tr><th>&nbsp;</th><th>#</th><th>Result</th><th>Protocol</th><th>Host</th><th>URL</th><th>Body</th><th>Caching</th><th>Content-Type</th><th>Process</th><th>Comments</th><th>Custom</th></tr></thead><tbody>");
            foreach (var entry in Entries)
            {
                sb.Append(entry.Html);
            }
            sb.Append("</tbody></table></body></html>");
            return sb.ToString();
        }

        public static FiddlerIndexHtml Parse(string content)
        {
            int bodyStart = content.IndexOf("<tbody>", StringComparison.Ordinal) + 7;
            int bodyEnd = content.LastIndexOf("</tbody>", StringComparison.Ordinal);
            var body = content.Substring(bodyStart, bodyEnd - bodyStart);
            
            var result = new FiddlerIndexHtml();

            var entries = body.Split(new[] {"</tr>"}, StringSplitOptions.RemoveEmptyEntries);

            var logEntry = new LogEntry();
            foreach (var entry in entries)
            {
                var fields = entry.Replace("<td>", "").Split(new[] {"</td>"}, StringSplitOptions.None);
                logEntry.Index = int.Parse(fields[1]);
                logEntry.StatusCode = int.Parse(fields[2]);
                logEntry.Protocol = fields[3];
                logEntry.Host = fields[4];
                logEntry.Path = fields[5];
                logEntry.Body = string.IsNullOrWhiteSpace(fields[6])
                    ? null
                    : (long?) long.Parse(fields[6], new NumberFormatInfo() {NumberGroupSeparator = "&#160;"});
                logEntry.Caching = fields[7];
                logEntry.ContentType = fields[8];
                logEntry.Process = fields[9];
                logEntry.Comments = fields[10];
                logEntry.Custom = fields[11];
                result.Entries.Add(logEntry);
            }
            result.LastIndex = logEntry.Index;
            return result;
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

        public string Html => $"<tr><td><a href='raw\\{Index.ToString("D8")}_c.txt'>C</a>&nbsp;<a href='raw\\{Index.ToString("D8")}_s.txt'>S</a>&nbsp;<a href='raw\\{Index.ToString("D8")}_m.xml'>M</a></td>" +
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
                              "</tr>";
    }
}