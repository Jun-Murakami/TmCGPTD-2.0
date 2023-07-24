using Avalonia;
using Avalonia.Platform;
using HtmlAgilityPack;
using System;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.DependencyInjection;

namespace TmCGPTD.Models
{
    public class HtmlProcess
    {
        // 表示用HTML初期化--------------------------------------------------------------
        public async Task<string> InitializeChatLogToHtml()
        {
            using var streamReader = new StreamReader(AssetLoader.Open(new Uri("avares://TmCGPTD/Assets/ChatTempleteLogo.html")));
            using var chatCssStreamReader = new StreamReader(AssetLoader.Open(new Uri("avares://TmCGPTD/Assets/ChatStyles.css")));
            using var cssStreamReader = new StreamReader(AssetLoader.Open(new Uri("avares://TmCGPTD/Assets/vs2015.min.css")));

            string chatCssContent = await chatCssStreamReader.ReadToEndAsync();
            string cssContent = await cssStreamReader.ReadToEndAsync();
            string templateHtml = await streamReader.ReadToEndAsync();

            var doc = new HtmlDocument();
            doc.LoadHtml(templateHtml);

            var styleNode = doc.CreateElement("style");
            styleNode.InnerHtml = cssContent;
            doc.DocumentNode.SelectSingleNode("//head").AppendChild(styleNode);
            var styleNode2 = doc.CreateElement("style");
            styleNode2.InnerHtml = chatCssContent;
            doc.DocumentNode.SelectSingleNode("//head").AppendChild(styleNode2);

            return doc.DocumentNode.OuterHtml;
        }

        // ログインページHTML初期化--------------------------------------------------------------
        public async Task<string> InitializeLogInToHtml()
        {
            using var streamReader = new StreamReader(AssetLoader.Open(new Uri("avares://TmCGPTD/Assets/LogInTemplete.html")));
            using var chatCssStreamReader = new StreamReader(AssetLoader.Open(new Uri("avares://TmCGPTD/Assets/ChatStyles.css")));

            string chatCssContent = await chatCssStreamReader.ReadToEndAsync();
            string templateHtml = await streamReader.ReadToEndAsync();

            var doc = new HtmlDocument();
            doc.LoadHtml(templateHtml);

            var styleNode2 = doc.CreateElement("style");
            styleNode2.InnerHtml = chatCssContent;
            doc.DocumentNode.SelectSingleNode("//head").AppendChild(styleNode2);

            return doc.DocumentNode.OuterHtml;
        }

        // 表示用HTML変換--------------------------------------------------------------
        public async Task<string> ConvertChatLogToHtml(string plainTextChatLog)
        {
            plainTextChatLog = Regex.Replace(plainTextChatLog, @"\r\n|\r|\n", Environment.NewLine);

            using var streamReader = new StreamReader(AssetLoader.Open(new Uri("avares://TmCGPTD/Assets/ChatTemplete.html")));
            using var chatCssStreamReader = new StreamReader(AssetLoader.Open(new Uri("avares://TmCGPTD/Assets/ChatStyles.css")));
            using var cssStreamReader = new StreamReader(AssetLoader.Open(new Uri("avares://TmCGPTD/Assets/vs2015.min.css")));

            string chatCssContent = await chatCssStreamReader.ReadToEndAsync();
            string cssContent = await cssStreamReader.ReadToEndAsync();
            string templateHtml = await streamReader.ReadToEndAsync();

            var doc = new HtmlDocument();
            doc.LoadHtml(templateHtml);

            var styleNode = doc.CreateElement("style");
            styleNode.InnerHtml = cssContent;
            doc.DocumentNode.SelectSingleNode("//head").AppendChild(styleNode);
            var styleNode2 = doc.CreateElement("style");
            styleNode2.InnerHtml = chatCssContent;
            doc.DocumentNode.SelectSingleNode("//head").AppendChild(styleNode2);


            var chatLogRegex = new Regex(@"^\[(.+)\] by (You|AI)", RegexOptions.Multiline);
            var codeSnippetRegex = new Regex(@"^```(?:([\w-+#.]+)\s+)?([\s\S]*?)(^```)", RegexOptions.Multiline);
            var usageRegex = new Regex(@"(^usage=)|(^(\[tokens\]))", RegexOptions.Multiline);

            var scrollableWrapperNode = doc.DocumentNode.SelectSingleNode("//div[@id='scrollableWrapper']");
            var chatHtml = string.Empty;

            string WrapCodeSnippet(Match match)
            {
                var language = string.IsNullOrEmpty(match.Groups[1].Value) ? "" : $" class=\"{match.Groups[1].Value}\"";
                var codeContent = match.Groups[2].Value;

                codeContent = codeContent.Trim('\r', '\n');

                var codeHeader = "";
                var codeStyle = "";
                var preStyle = " style=\"margin:1.8em 0px 2.5em 0px\"";
                if (!string.IsNullOrEmpty(match.Groups[1].Value))
                {
                    codeHeader = "<div class=\"codeHeader\"><span class=\"lang\">" + match.Groups[1].Value + "</span><span class=\"codeCopy\"><button id=\"copyButton\">Copy code</button></span></div>";
                    codeStyle = " id=\"headerOn\"";
                    preStyle = " style=\"margin:0px 0px 2.5em 0px\"";
                }

                return $"</div>{codeHeader}<pre{preStyle}><code{language}{codeStyle}>{codeContent}</code></pre><div style=\"white-space: pre-wrap\" id=\"document\">";
            }

            MatchCollection matches = chatLogRegex.Matches(plainTextChatLog);

            for (int i = 0; i < matches.Count; i++)
            {
                var match = matches[i];
                var isUser = match.Groups[2].Value == "You";
                var className = isUser ? "user" : "assistant";
                var headerClassName = isUser ? "userHeader" : "assistantHeader";

                var endOfMatch = match.Index + match.Length;
                var nextMatchIndex = i < matches.Count - 1 ? matches[i + 1].Index : plainTextChatLog.Length;
                var content = plainTextChatLog.Substring(endOfMatch, nextMatchIndex - endOfMatch);

                content = WebUtility.HtmlEncode(content); // エスケープを適用
                content = codeSnippetRegex.Replace(content, WrapCodeSnippet);

                string pattern = @"\[\!\[(.*?)\]\((.*?)\)\]\((.*?)\)";
                content = Regex.Replace(content, pattern, @"<a href=""$3"" target=""_blank"" rel=""noopener noreferrer""><img src=""$2"" alt=""$1""></a>");

                Regex linkRegex = new Regex(@"\[([^\]]+?)\]\(([^\)]+?)\)");
                content = linkRegex.Replace(content, m => $"<a href=\"{m.Groups[2].Value}\" target=\"_blank\" rel=\"noopener noreferrer\">{m.Groups[1].Value}</a>");

                Regex imgRegex = new Regex(@"!\[([^\]]*?)\]\(([^\)]+?)\)");
                content = imgRegex.Replace(content, m => $"<img src=\"{m.Groups[2].Value}\" alt=\"{m.Groups[1].Value}\">");

                Regex strongRegex = new Regex(@"\*\*(.+?)\*\*");
                content = strongRegex.Replace(content, m => $"<strong>{m.Groups[1].Value}</strong>");

                pattern = @$"#(\s*)(?i)system({Environment.NewLine})*?---({Environment.NewLine})*";
                content = Regex.Replace(content, pattern, "<div class=\"codeHeader2\"><span class=\"lang\">System Message</span></div><pre style=\"margin:0px 0px 2.5em 0px\"><code id=\"headerOn\" class=\"plaintext\">System messages were turned off.</code></pre>", RegexOptions.Singleline);

                pattern = @$"#(\s*)(?i)system({Environment.NewLine})*(.+?)---({Environment.NewLine})*";
                content = Regex.Replace(content, pattern, "<div class=\"codeHeader2\"><span class=\"lang\">System Message</span></div><pre style=\"margin:0px 0px 2.5em 0px\"><code id=\"headerOn\" class=\"plaintext\">$3</code></pre>", RegexOptions.Singleline);

                pattern = @"`(.*?)`";
                string replacement = "<code class=\"inline\">`$1`</code>";
                content = Regex.Replace(content, pattern, replacement);

                pattern = @"\(\!--editable--\)";
                content = Regex.Replace(content, pattern, @"<div class=""editDiv""><button class=""editButton"">Edit</button></div>");


                var usageMatch = usageRegex.Match(content);
                if (usageMatch.Success)
                {
                    content = content.Substring(0, usageMatch.Index) + "<div class=\"usage\">" + content.Substring(usageMatch.Index).Trim('\r', '\n');
                    content += "</div>";
                }
                content = $"<div class=\"{className}\"><span class=\"{headerClassName}\">{match.Groups[0].Value}</span><div style=\"white-space: pre-wrap\" id=\"document\">{content}</div></div>";

                chatHtml += content;
            }

            scrollableWrapperNode.InnerHtml += chatHtml;

            var documentDivs = doc.DocumentNode.SelectNodes("//div[@id='document']");

            if (documentDivs != null)
            {
                foreach (var documentDiv in documentDivs)
                {
                    // ノードの先頭と末尾のテキストノードのみをトリム
                    if (documentDiv.HasChildNodes)
                    {
                        var firstNode = documentDiv.ChildNodes.First();
                        if (firstNode.NodeType == HtmlNodeType.Text)
                        {
                            firstNode.InnerHtml = firstNode.InnerHtml.TrimStart('\r', '\n');
                        }

                        var lastNode = documentDiv.ChildNodes.Last();
                        if (lastNode.NodeType == HtmlNodeType.Text)
                        {
                            lastNode.InnerHtml = lastNode.InnerHtml.TrimEnd('\r', '\n');
                        }
                    }
                }
            }

            return doc.DocumentNode.OuterHtml;
        }

        // Webチャットログインポート--------------------------------------------------------------
        public async Task<string> GetWebChatLogAsync(string htmlSource)
        {
            try
            {
                string webChatTitle;
                List<Dictionary<string, object>> webConversationHistory = new List<Dictionary<string, object>>();
                string webLog = "";

                // HtmlAgilityPackを使ってHTMLを解析
                HtmlAgilityPack.HtmlDocument htmlDoc = new HtmlAgilityPack.HtmlDocument();
                htmlDoc.LoadHtml(htmlSource);

                Avalonia.Application.Current!.TryFindResource("My.Strings.ChatScreenInfo", out object? resource1);
                string resourceString = resource1!.ToString()!;

                HtmlNode titleNode = htmlDoc.DocumentNode.SelectSingleNode("//title");

                if (titleNode != null)
                {
                    string titleText = titleNode.InnerText;
                    if (titleText == "New chat" || titleText == "" || titleText == "ChatGPT")
                    {
                        return resourceString;
                    }
                    else
                    {
                        webChatTitle = ReplaceEntities(titleText);
                    }
                }
                else
                {
                    return resourceString;
                }

                // mainタグをサーチ
                var mainTag = htmlDoc.DocumentNode.SelectSingleNode("//main");
                if (mainTag == null)
                {
                    return resourceString;
                }

                // divタグを取得
                var divTags = mainTag.SelectNodes("./*/*/*/*/div");
                int count = 0;

                // フィルタリングされたdivタグを保持するリスト
                List<HtmlNode> filteredDivs = new List<HtmlNode>();

                // divタグをフィルタリング
                foreach (var div in divTags)
                {
                    if (div.ChildNodes.Count == 0 || div.InnerText.Contains("Model:") || div.InnerText.Contains("Regenerate response"))
                    {
                        continue;
                    }
                    filteredDivs.Add(div);
                }

                foreach (var div in filteredDivs)
                {
                    var className = div.GetAttributeValue("class", "");
                    var regex = new Regex(@".*\[#\w{6}\].*");
                    var match = regex.Match(className);

                    string role;
                    string content;
                    string br = Environment.NewLine;

                    if (!match.Success)
                    {
                        role = "user";
                        // 子ノードのInnerTextを取得し、文字列として結合
                        string htmlString = div.InnerHtml;
                        string pattern = "<span class=.*>[0-9]+ / [0-9]+</span>";
                        htmlString = Regex.Replace(htmlString, pattern, "");

                        // 置換処理が完了した後、再度HTMLドキュメントに戻す
                        var modifiedHtmlDoc = new HtmlAgilityPack.HtmlDocument();
                        modifiedHtmlDoc.LoadHtml(htmlString);

                        // InnerText要素を結合して、宣言済みの変数contentに文字列として代入
                        StringBuilder contentBuilder = new StringBuilder();
                        foreach (var node in modifiedHtmlDoc.DocumentNode.ChildNodes)
                        {
                            if (!string.IsNullOrWhiteSpace(node.InnerText))
                            {
                                contentBuilder.Append(ReplaceEntities(node.InnerText));
                            }
                        }
                        content = contentBuilder.ToString();
                        content = content.Trim();

                        if (!string.IsNullOrWhiteSpace(content))
                        {
                            webConversationHistory.Add(new Dictionary<string, object>
                            {
                                { "role", role },
                                { "content", content }
                            });
                            webLog += $"[Web Chat] by You{br}{br}{content}{br}{br}{br}";
                        }
                    }
                    else
                    {
                        role = "assistant";

                        var nodes = div.DescendantsAndSelf().ToList();

                        foreach (var node in nodes)
                        {
                            if (node.Name == "code" && !IsInsidePreTag(node))
                            {
                                var textNode = htmlDoc.CreateTextNode("`" + node.InnerHtml + "`");
                                node.ParentNode.ReplaceChild(textNode, node);
                            }
                        }

                        bool IsInsidePreTag(HtmlNode node)
                        {
                            var currentNode = node;
                            while (currentNode.ParentNode != null)
                            {
                                if (currentNode.ParentNode.Name == "pre")
                                {
                                    return true;
                                }
                                currentNode = currentNode.ParentNode;
                            }
                            return false;
                        }

                        string htmlString = div.InnerHtml;

                        // 正規表現パターンに基づいて置換・削除
                        string pattern = "<title.*>.*</title>";
                        htmlString = Regex.Replace(htmlString, pattern, "");

                        pattern = "<text.*>.*</text>";
                        htmlString = Regex.Replace(htmlString, pattern, "");

                        pattern = "<div>Finished browsing</div>";
                        htmlString = Regex.Replace(htmlString, pattern, "");

                        pattern = "<sup>([^<]*?)</sup>";
                        htmlString = Regex.Replace(htmlString, pattern, "($1)");

                        pattern = "Used <b>[^<]*</b>";
                        htmlString = Regex.Replace(htmlString, pattern, $"");

                        pattern = "<a href=\"([^\"]*?)\"[^>]*?>([^<]*?)</a>";
                        string replacement = $"[$2]($1)";
                        htmlString = Regex.Replace(htmlString, pattern, replacement);

                        Regex _favconRegex = new Regex("<img[^>]*?src=\"[^>]*?\"[^>]*?alt=\"Favicon\"?[^>]*?>");
                        htmlString = _favconRegex.Replace(htmlString, "");

                        Regex _regex = new Regex("<p><img.+?src=\"(.*?)\".*?></p>");
                        htmlString = _regex.Replace(htmlString, $"![]($1)");

                        _regex = new Regex("<img.+?src=\"(.*?)\".*?>");
                        htmlString = _regex.Replace(htmlString, $"![]($1)");

                        pattern = @"![[]]\([^)]*.ico\)";
                        htmlString = Regex.Replace(htmlString, pattern, "");

                        pattern = @"![[]]\([^)]*.svg\)";
                        htmlString = Regex.Replace(htmlString, pattern, "");

                        pattern = "<span class=[^>]+?>[0-9]+? / [0-9]+?</span>";
                        htmlString = Regex.Replace(htmlString, pattern, "");

                        pattern = @"(?<=<pre>[^<]*?)(?<code><code>|<\/code>)(?=[^<]*?<\/pre>)";
                        htmlString = Regex.Replace(htmlString, pattern, "`", RegexOptions.Singleline);

                        //Debug.WriteLine(htmlString);

                        // 置換処理
                        htmlString = htmlString.Replace("<pre class=\"\">", $"{br}{br}```")
                                               .Replace("<pre>", $"{br}{br}```")
                                               .Replace("</pre>", $"{br}```{br}{br}")
                                               .Replace("Copy code", $"{br}")
                                               .Replace("<strong>", $"**")
                                               .Replace("</strong>", $"**")
                                               .Replace("<hr>", $"{br}")
                                               .Replace("<h2>", $"{br}")
                                               .Replace("</h2>", $"{br}")
                                               .Replace("<h3>", $"{br}")
                                               .Replace("</h3>", $"{br}")
                                               .Replace("<h4>", $"{br}")
                                               .Replace("</h4>", $"{br}")
                                               .Replace("<ol>", $"{br}")
                                               .Replace("</ol>", $"{br}")
                                               .Replace("<ul>", $"{br}")
                                               .Replace("</ul>", $"{br}")
                                               .Replace("<li><p>", $"- ")
                                               .Replace("</p></li>", $"{br}{br}")
                                               .Replace("<li>", $"- ")
                                               .Replace("</li>", $"{br}")
                                               .Replace($"::before", "`")
                                               .Replace($"::after", "`")
                                                .Replace("<tr>", $"- ")
                                                .Replace("</tr>", $"{br}")
                                                .Replace("<td>", $"")
                                                .Replace("</td>", $" ")
                                                .Replace("<th>", $"")
                                                .Replace("</th>", $" ")
                                               .Replace("<p>", $"")
                                               .Replace("</p>", $"{br}{br}")
                                               .Replace($"{br}{br}{br}", $"{br}{br}");

                        pattern = "(\r\n|\n|\r){3,}";
                        htmlString = Regex.Replace(htmlString, pattern, $"{br}{br}");



                        // 置換処理が完了した後、再度HTMLドキュメントに戻す
                        var modifiedHtmlDoc = new HtmlAgilityPack.HtmlDocument();
                        modifiedHtmlDoc.LoadHtml(htmlString);

                        // InnerText要素を結合して、宣言済みの変数contentに文字列として代入
                        StringBuilder contentBuilder = new StringBuilder();
                        foreach (var node in modifiedHtmlDoc.DocumentNode.ChildNodes)
                        {
                            if (!string.IsNullOrWhiteSpace(node.InnerText))
                            {
                                contentBuilder.Append(ReplaceEntities(node.InnerText));
                            }
                        }
                        content = contentBuilder.ToString();
                        content = content.Trim();

                        webConversationHistory.Add(new Dictionary<string, object>
                        {
                            { "role", role },
                            { "content", content }
                        });
                        webLog += $"[Web Chat] by AI{br}{br}{content}{br}{br}{br}";
                    }

                    count++;
                }

                DatabaseProcess _dbProcess = new DatabaseProcess();
                var msg = await _dbProcess.InsertWebChatLogDatabaseAsync(webChatTitle, webConversationHistory, webLog, "Web Chat");
                if (msg == "Cancel")
                {
                    return "Cancel";
                }
                return "OK";
                //return $"Successfully imported log:{Environment.NewLine}{Environment.NewLine}'{webChatTitle}' ({count} Messages)";
            }
            catch (Exception ex)
            {
                return $"Error : {ex.Message}";
            }
        }
        //htmlエンティティ変換
        private static readonly Dictionary<string, string> EntityToCharacter = new Dictionary<string, string>
        {
            {"&amp;", "&"},
            {"&quot;", "\""},
            {"&apos;", "'"},
            {"&lt;", "<"},
            {"&gt;", ">"}
        };
        public static string ReplaceEntities(string input)
        {
            foreach (var kvp in EntityToCharacter)
            {
                input = input.Replace(kvp.Key, kvp.Value);
            }
            return input;
        }

        // Webチャットログインポート(Bard)--------------------------------------------------------------
        public async Task<string> GetWebChatLogBardAsync(string htmlSource)
        {
            try
            {
                string webChatTitle;
                List<Dictionary<string, object>> webConversationHistory = new List<Dictionary<string, object>>();
                string webLog = "";

                // HtmlAgilityPackを使ってHTMLを解析
                HtmlAgilityPack.HtmlDocument htmlDoc = new HtmlAgilityPack.HtmlDocument();
                htmlDoc.LoadHtml(htmlSource);

                Avalonia.Application.Current!.TryFindResource("My.Strings.ChatScreenInfo", out object? resource1);
                string resourceString = resource1!.ToString()!;

                HtmlNode titleNode = htmlDoc.DocumentNode.SelectSingleNode("//title");

                if (titleNode != null)
                {
                    string titleText = titleNode.InnerText;
                    if (titleText != "Bard")
                    {
                        return resourceString;
                    }
                    else
                    {
                        webChatTitle = "Bard: " + DateTime.Now;
                    }
                }
                else
                {
                    return resourceString;
                }

                // mainタグをサーチ
                var mainTag = htmlDoc.DocumentNode.SelectSingleNode("//main");
                if (mainTag == null)
                {
                    return resourceString;
                }

                var conversationContainers = mainTag.Descendants("div")
                                                    .Where(div => div.GetAttributeValue("class", "").Contains("conversation-container"));

                string br = Environment.NewLine;
                int count = 0;

                foreach (var conversationContainer in conversationContainers)
                {
                    var userQueryContainer = conversationContainer.Descendants("div")
                                                                  .FirstOrDefault(div => div.GetAttributeValue("class", "").Contains("user-query-container"));
                    if (userQueryContainer != null)
                    {
                        var converter = new ReverseMarkdown.Converter();
                        string markdown = converter.Convert(userQueryContainer.InnerHtml.Trim());

                        string pattern = "<user-profile-picture.*>.*</user-profile-picture>";
                        markdown = Regex.Replace(markdown, pattern, "");

                        pattern = "<button.*?>.*?</button>";
                        markdown = Regex.Replace(markdown, pattern, "", RegexOptions.Singleline);

                        pattern = "^## ";
                        markdown = Regex.Replace(markdown, pattern, "", RegexOptions.Multiline);

                        // 置換処理が完了した後、再度HTMLドキュメントに戻す
                        var modifiedHtmlDoc = new HtmlAgilityPack.HtmlDocument();
                        modifiedHtmlDoc.LoadHtml(markdown);

                        // InnerText要素を結合して、宣言済みの変数contentに文字列として代入
                        StringBuilder contentBuilder = new StringBuilder();
                        foreach (var node in modifiedHtmlDoc.DocumentNode.ChildNodes)
                        {
                            if (!string.IsNullOrWhiteSpace(node.InnerText))
                            {
                                contentBuilder.Append(ReplaceEntities(node.InnerText));
                            }
                        }

                        string content = contentBuilder.ToString();
                        content = content.Trim();

                        if (!string.IsNullOrWhiteSpace(content))
                        {
                            webConversationHistory.Add(new Dictionary<string, object>
                            {
                                { "role", "user" },
                                { "content", content }
                            });
                            webLog += $"[Web Chat] by You{br}{br}{content}{br}{br}{br}";
                            count++;
                        }
                    }

                    var responseContainer = conversationContainer.Descendants("div")
                                                                  .FirstOrDefault(div => div.GetAttributeValue("class", "").Contains("presented-response-container"));
                    if (responseContainer != null)
                    {

                        // 'code'タグをMarkdownのコードブロックに変換
                        var nodes = responseContainer.DescendantsAndSelf().ToList();
                        foreach (var node in nodes)
                        {
                            if (node.Name == "code" && !IsInsidePreTag(node))
                            {
                                var textNode = htmlDoc.CreateTextNode("`" + node.InnerHtml + "`");
                                node.ParentNode.ReplaceChild(textNode, node);
                            }
                        }
                        // ヘルパー関数
                        bool IsInsidePreTag(HtmlNode node)
                        {
                            var currentNode = node;
                            while (currentNode.ParentNode != null)
                            {
                                if (currentNode.ParentNode.Name == "pre")
                                {
                                    return true;
                                }
                                currentNode = currentNode.ParentNode;
                            }
                            return false;
                        }

                        string markdown = responseContainer.InnerHtml.Trim();

                        //Debug.WriteLine(markdown);

                        string pattern = "<user-profile-picture.*>.*</user-profile-picture>";
                        markdown = Regex.Replace(markdown, pattern, "");

                        pattern = "<button[^>]*?>.*?</button>";
                        markdown = Regex.Replace(markdown, pattern, "", RegexOptions.Singleline);

                        pattern = "<h4[^>]*?>.*?</h4>";
                        markdown = Regex.Replace(markdown, pattern, "");

                        pattern = "<mat-icon[^>]*?>.*?</mat-icon>";
                        markdown = Regex.Replace(markdown, pattern, "");

                        //Debug.WriteLine(markdown);

                        pattern = "<div[^>]*?>コードは慎重に使用してください。.*?</div>";
                        markdown = Regex.Replace(markdown, pattern, "");

                        pattern = "<div[^>]*?(code-block-wrapper header|code-block-decoration header)[^>]*?>(.*?)</div>";
                        markdown = Regex.Replace(markdown, pattern, $"{br}```$2{br}");

                        pattern = "<pre[^>]*?>";
                        markdown = Regex.Replace(markdown, pattern, "");

                        pattern = "</pre>";
                        markdown = Regex.Replace(markdown, pattern, $"```");

                        //Debug.WriteLine(markdown);

                        var converter = new ReverseMarkdown.Converter();
                        markdown = converter.Convert(markdown);


                        pattern = "<a [^>]*?href=\"(http[^\"]*?)\"(.*)</a>";
                        string replacement = $"[<a $2</a>]($1){br}{br}";
                        markdown = Regex.Replace(markdown, pattern, replacement);

                        pattern = @"!\[\]\(http[^)]*?(.gif|.svg)\)";
                        markdown = Regex.Replace(markdown, pattern, "", RegexOptions.IgnoreCase);

                        markdown = markdown.Replace("<p>", $"")
                                            .Replace("</p>", $"{br}{br}")
                                            .Replace("<ol>", $"{br}")
                                            .Replace("</ol>", $"{br}")
                                            .Replace("<ul>", $"{br}")
                                            .Replace("</ul>", $"{br}")
                                            .Replace("<li><p>", "- ")
                                            .Replace("</p></li>", $"{br}{br}")
                                            .Replace("<li>", "- ")
                                            .Replace("</li>", $"{br}")
                                            .Replace("<tr>", "- ")
                                            .Replace("</tr>", $"{br}")
                                            .Replace("<td>", "")
                                            .Replace("</td>", " ")
                                            .Replace("<th>", "")
                                            .Replace("</th>", " ")
                                            .Replace("```コード スニペット", "```コードスニペット")
                                            .Replace($"{br}{br}{br}", $"{br}{br}");

                        pattern = "(\r\n|\n|\r){3,}";
                        markdown = Regex.Replace(markdown, pattern, $"{br}{br}");


                        //Debug.WriteLine(markdown);

                        // 置換処理が完了した後、再度HTMLドキュメントに戻す
                        var modifiedHtmlDoc = new HtmlAgilityPack.HtmlDocument();
                        modifiedHtmlDoc.LoadHtml(markdown);

                        // InnerText要素を結合して、宣言済みの変数contentに文字列として代入
                        StringBuilder contentBuilder = new StringBuilder();
                        foreach (var node in modifiedHtmlDoc.DocumentNode.ChildNodes)
                        {
                            if (!string.IsNullOrWhiteSpace(node.InnerText))
                            {
                                contentBuilder.Append(ReplaceEntities(node.InnerText));
                            }
                        }
                        string content = contentBuilder.ToString();
                        content = content.Trim();

                        if (!string.IsNullOrWhiteSpace(markdown))
                        {
                            webConversationHistory.Add(new Dictionary<string, object>
                            {
                                { "role", "assistant" },
                                { "content", content }
                            });
                            webLog += $"[Web Chat] by AI{br}{br}{content}{br}{br}{br}";
                            count++;
                        }
                    }
                }

                //Debug.WriteLine(webLog);

                DatabaseProcess _dbProcess = new DatabaseProcess();
                var msg = await _dbProcess.InsertWebChatLogDatabaseAsync(webChatTitle, webConversationHistory, webLog, "Bard");
                if (msg == "Cancel")
                {
                    return "Cancel";
                }
                return $"Successfully imported log:{Environment.NewLine}{Environment.NewLine}'{webChatTitle}' ({count} Messages)";
            }
            catch (Exception ex)
            {
                return $"Error : {ex.Message}{ex.StackTrace}";
            }
        }

        // ストリーム表示用チャットログHTML変換--------------------------------------------------------------
        public async Task<string> ConvertAddLogToHtml(string plainTextChatLog, DateTime resDate)
        {
            var doc = new HtmlDocument();

            await Task.Run(() =>
            {
                plainTextChatLog = Regex.Replace(plainTextChatLog, @"\r\n|\r|\n", Environment.NewLine);
                var codeSnippetRegex = new Regex(@"^```(?:([\w-+#.]+)\s+)?([\s\S]*?)(^```)", RegexOptions.Multiline);
                var usageRegex = new Regex(@"(^usage=)|(^(\[tokens\]))", RegexOptions.Multiline);

                string WrapCodeSnippet(Match match)
                {
                    var language = string.IsNullOrEmpty(match.Groups[1].Value) ? "" : $" class=\"{match.Groups[1].Value}\"";
                    var codeContent = match.Groups[2].Value;

                    codeContent = codeContent.Trim('\r', '\n');

                    var codeHeader = "";
                    var codeStyle = "";
                    var preStyle = " style=\"margin:1.8em 0px 2.5em 0px\"";
                    if (!string.IsNullOrEmpty(match.Groups[1].Value))
                    {
                        codeHeader = "<div class=\"codeHeader\"><span class=\"lang\">" + match.Groups[1].Value + "</span><span class=\"codeCopy\"><button id=\"copyButton\">Copy code</button></span></div>";
                        codeStyle = " id=\"headerOn\"";
                        preStyle = " style=\"margin:0px 0px 2.5em 0px\"";
                    }

                    return $"</div>{codeHeader}<pre{preStyle}><code{language}{codeStyle}>{codeContent}</code></pre><div style=\"white-space: pre-wrap\" id=\"document\">";
                }

                var content = WebUtility.HtmlEncode(plainTextChatLog); // エスケープを適用
                content = codeSnippetRegex.Replace(content, WrapCodeSnippet);

                string pattern = @"\[\!\[(.*?)\]\((.*?)\)\]\((.*?)\)";
                content = Regex.Replace(content, pattern, @"<a href=""$3"" target=""_blank"" rel=""noopener noreferrer""><img src=""$2"" alt=""$1""></a>");

                Regex linkRegex = new Regex(@"\[([^\]]+?)\]\(([^\)]+?)\)");
                content = linkRegex.Replace(content, m => $"<a href=\"{m.Groups[2].Value}\" target=\"_blank\" rel=\"noopener noreferrer\">{m.Groups[1].Value}</a>");

                Regex imgRegex = new Regex(@"!\[([^\]]*?)\]\(([^\)]+?)\)");
                content = imgRegex.Replace(content, m => $"<img src=\"{m.Groups[2].Value}\" alt=\"{m.Groups[1].Value}\">");

                Regex strongRegex = new Regex(@"\*\*(.+?)\*\*");
                content = strongRegex.Replace(content, m => $"<strong>{m.Groups[1].Value}</strong>");

                pattern = @"`(.*?)`";
                string replacement = "<code class=\"inline\">`$1`</code>";
                content = Regex.Replace(content, pattern, replacement);

                var usageMatch = usageRegex.Match(content);
                if (usageMatch.Success)
                {
                    content = content.Substring(0, usageMatch.Index) + "<div class=\"usage\">" + content.Substring(usageMatch.Index).Trim('\r', '\n');
                    content += "</div>";
                }

                content = $"<span class=\"assistantHeader\">[{resDate}] by AI</span><div style=\"white-space: pre-wrap\" id=\"document\">{content}</div>";


                doc.LoadHtml(content);

                var documentDivs = doc.DocumentNode.SelectNodes("//div[@id='document']");

                if (documentDivs != null)
                {
                    foreach (var documentDiv in documentDivs)
                    {
                        // ノードの先頭と末尾のテキストノードのみをトリム
                        if (documentDiv.HasChildNodes)
                        {
                            var firstNode = documentDiv.ChildNodes.First();
                            if (firstNode.NodeType == HtmlNodeType.Text)
                            {
                                firstNode.InnerHtml = firstNode.InnerHtml.TrimStart('\r', '\n');
                            }

                            var lastNode = documentDiv.ChildNodes.Last();
                            if (lastNode.NodeType == HtmlNodeType.Text)
                            {
                                lastNode.InnerHtml = lastNode.InnerHtml.TrimEnd('\r', '\n');
                            }
                        }
                    }
                }
            });
            return doc.DocumentNode.OuterHtml;
        }
    }
}
