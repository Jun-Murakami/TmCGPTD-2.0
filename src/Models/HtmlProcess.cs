using Avalonia.Platform;
using HtmlAgilityPack;
using System;
using System.Text.Json;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Net.Http;
using TiktokenSharp;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using FluentAvalonia.UI.Controls;
using CommunityToolkit.Mvvm.DependencyInjection;
using System.Reflection.Metadata;
using ReverseMarkdown.Converters;
using System.Reactive.Joins;

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
            var usageRegex = new Regex(@"^usage=", RegexOptions.Multiline);

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

                Application.Current!.TryFindResource("My.Strings.ChatScreenInfo", out object resource1);
                string resourceString = resource1.ToString();

                HtmlNode titleNode = htmlDoc.DocumentNode.SelectSingleNode("//title");

                if (titleNode != null)
                {
                    string titleText = titleNode.InnerText;
                    if (titleText == "New chat" || titleText == "")
                    {
                        return resourceString;
                    }
                    else
                    {
                        webChatTitle = titleText;
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

                        Debug.WriteLine(htmlString);

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
                var msg = await _dbProcess.InsertWebChatLogDatabaseAsync(webChatTitle, webConversationHistory, webLog, "from Web Chat");
                if (msg == "Cancel")
                {
                    return "Cancel";
                }
                return $"Successfully imported log:{Environment.NewLine}{Environment.NewLine}'{webChatTitle}' ({count} Messages)";
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

                Application.Current!.TryFindResource("My.Strings.ChatScreenInfo", out object resource1);
                string resourceString = resource1.ToString();

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

                        Debug.WriteLine(markdown);

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

                        pattern = @"^!\[\]\(http[^)]*?(.gif|.svg)\)";
                        markdown = Regex.Replace(markdown, pattern, "", RegexOptions.Multiline);

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
                var msg = await _dbProcess.InsertWebChatLogDatabaseAsync(webChatTitle, webConversationHistory, webLog, "from Bard");
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

        // APIに接続してレスポンス取得--------------------------------------------------------------
        public async Task<string> PostChatAsync(string chatTextPost)
        {
            try
            {
                List<Dictionary<string, object>>? conversationHistory = VMLocator.ChatViewModel.ConversationHistory;
                List<Dictionary<string, object>>? postedConversationHistory = new List<Dictionary<string, object>>();

                bool isDeleteHistory = false;
                string chatTextRes = "";
                string currentTitle = VMLocator.ChatViewModel.ChatTitle;

                int maxTokens = VMLocator.MainWindowViewModel.ApiMaxTokens;
                if (!VMLocator.MainWindowViewModel.ApiMaxTokensIsEnable)
                {
                    maxTokens = 0;
                }

                int maxContentLength = VMLocator.MainWindowViewModel.MaxContentLength;
                if (!VMLocator.MainWindowViewModel.MaxContentLengthIsEnable)
                {
                    maxContentLength = 3072;
                }

                TikToken tokenizer = TikToken.EncodingForModel("gpt-3.5-turbo");

                // 過去の会話履歴と現在の入力を結合する前に、過去の会話履歴に含まれるcontent文字列のトークン数を取得
                int historyContentTokenCount = conversationHistory.Sum(d => tokenizer.Encode(d["content"].ToString()).Count);

                // 要約前のトークン数を記録
                int preSummarizedHistoryTokenCount = historyContentTokenCount;

                // 履歴を逆順にする
                List<Dictionary<string, object>> reversedHistoryList = new List<Dictionary<string, object>>(conversationHistory);
                reversedHistoryList.Reverse();

                // 入力文字列のトークン数を取得
                int inputTokenCount = tokenizer.Encode(chatTextPost).Count;

                // 入力文字列 + maxTokensが4096を超えた場合
                if ((inputTokenCount + maxTokens) > 4096)
                {
                    if (VMLocator.MainWindowViewModel.ApiMaxTokensIsEnable)
                    {
                        throw new Exception($"The values for input text ({inputTokenCount}) + max_tokens ({maxTokens}) exceeds 4097 tokens. Please reduce by at least {(inputTokenCount + maxTokens) - 4097} tokens.{Environment.NewLine}");
                    }
                    else
                    {
                        throw new Exception($"The values for input text ({inputTokenCount}) exceeds 4097 tokens. Please reduce by at least {inputTokenCount - 4096} tokens.{Environment.NewLine}");
                    }
                }

                // 制限文字数の計算
                int limitLength = VMLocator.MainWindowViewModel.ApiMaxTokensIsEnable ? inputTokenCount + maxTokens + 400 : maxContentLength;

                // 過去の履歴＋ユーザーの新規入力＋maxTokensがmaxContentLengthを超えた場合の要約処理
                if (historyContentTokenCount + inputTokenCount + maxTokens > maxContentLength)
                {
                    int historyTokenCount = 0;
                    int messagesToSelect = 0;
                    int messageStart = 0;
                    string forCompMes = "";

                    // 会話履歴の最新のものからトークン数を数えて一時変数「historyTokenCount」に足していく
                    for (int i = 0; i < reversedHistoryList.Count; i += 1)
                    {
                        string mes = reversedHistoryList[i]["content"].ToString();
                        int messageTokenCount = tokenizer.Encode(mes).Count;
                        historyTokenCount += messageTokenCount;

                        if (i <= 4 && historyTokenCount < limitLength / 5) //直近の会話が短ければそのまま生かす
                        {
                            messageStart += 1;
                        }

                        if (historyTokenCount > limitLength) // トークン数が制限文字数を超えたらブレイク
                        {
                            messagesToSelect = i + 1; // 最後に処理した次のインデックスを記録
                            break;
                        }
                    }

                    //string debugStr = string.Join(Environment.NewLine, reversedHistoryList.Select(dict => string.Join(", ", dict.Select(pair => $"{pair.Key}: {pair.Value}")))); // デバッグ用

                    // 会話履歴から適切な数だけをセレクトする
                    int rangeLength = Math.Min(messagesToSelect - messageStart, reversedHistoryList.Count - messageStart);

                    if (rangeLength > 0)
                    {
                        forCompMes = reversedHistoryList.GetRange(messageStart, rangeLength).Select(message => message["content"].ToString()).Aggregate((a, b) => a + b);
                    }
                    else if (messagesToSelect == 0)
                    {
                        forCompMes = reversedHistoryList[0]["content"].ToString();
                    }

                    if (messagesToSelect > 0)
                    {
                        // 抽出したテキストを要約APIリクエストに送信
                        try
                        {
                            string summary = await GetSummaryAsync(forCompMes);
                            summary = currentTitle + ": " + summary;

                            string summaryLog = "";
                            if (messageStart > 0)
                            {
                                summaryLog += $"{messageStart} latest message(s) + {Environment.NewLine}{Environment.NewLine}{summary}";
                            }
                            else
                            {
                                summaryLog = summary;
                            }

                            // 返ってきた要約文で、conversationHistoryを書き換える
                            conversationHistory.RemoveRange(messageStart, conversationHistory.Count - messageStart);
                            conversationHistory.Add(new Dictionary<string, object>() { { "role", "assistant" }, { "content", summary } });
                        }
                        catch (Exception ex)
                        {
                            throw new Exception($"{ex.Message + Environment.NewLine}");
                        }
                    }
                    else
                    {
                        // 直近の履歴削除の必要がある場合、会話履歴が空でなければ、会話履歴を削除
                        if (conversationHistory.Count() > 0)
                        {
                            conversationHistory.Clear();
                            isDeleteHistory = true;
                        }
                    }
                }

                // 現在のユーザーの入力を表すディクショナリ
                var userInput = new Dictionary<string, object>() { { "role", "user" }, { "content", chatTextPost } };

                // 圧縮済みの会話履歴をpostedConversationHistoryにディープコピー
                string jsonCopy = System.Text.Json.JsonSerializer.Serialize(conversationHistory);
                postedConversationHistory = System.Text.Json.JsonSerializer.Deserialize<List<Dictionary<string, object>>>(jsonCopy);

                // 過去の会話履歴と現在の入力を結合
                conversationHistory.Add(userInput);

                // リクエストパラメータを作成
                var options = new Dictionary<string, object>() { { "model", VMLocator.MainWindowViewModel.ApiModel }, { "messages", conversationHistory } };

                // オプションパラメータを追加
                if (VMLocator.MainWindowViewModel.ApiMaxTokensIsEnable)
                    options.Add("max_tokens", VMLocator.MainWindowViewModel.ApiMaxTokens);
                if (VMLocator.MainWindowViewModel.ApiTemperatureIsEnable)
                    options.Add("temperature", VMLocator.MainWindowViewModel.ApiTemperature);
                if (VMLocator.MainWindowViewModel.ApiTopPIsEnable)
                    options.Add("top_p", VMLocator.MainWindowViewModel.ApiTopP);
                if (VMLocator.MainWindowViewModel.ApiNIsEnable)
                    options.Add("n", VMLocator.MainWindowViewModel.ApiN);
                if (VMLocator.MainWindowViewModel.ApiLogprobIsEnable)
                    options.Add("logprobs", VMLocator.MainWindowViewModel.ApiLogprobs);
                if (VMLocator.MainWindowViewModel.ApiPresencePenaltyIsEnable)
                    options.Add("presence_penalty", VMLocator.MainWindowViewModel.ApiPresencePenalty);
                if (VMLocator.MainWindowViewModel.ApiFrequencyPenaltyIsEnable)
                    options.Add("frequency_penalty", VMLocator.MainWindowViewModel.ApiFrequencyPenalty);
                if (VMLocator.MainWindowViewModel.ApiBestOfIsEnable)
                    options.Add("best_of", VMLocator.MainWindowViewModel.ApiBestOf);

                // api_stop パラメータの処理
                if (VMLocator.MainWindowViewModel.ApiStopIsEnable)
                {
                    string[] stopSequence = VMLocator.MainWindowViewModel.ApiStop.Split(',');
                    options.Add("stop", stopSequence);
                }

                // api_logit_bias パラメータの処理
                if (VMLocator.MainWindowViewModel.ApiLogitBiasIsEnable)
                {
                    var logitBias = JsonSerializer.Deserialize<Dictionary<string, double>>(VMLocator.MainWindowViewModel.ApiLogitBias);
                    options.Add("logit_bias", logitBias);
                }

                // stream 有効化
                options["stream"] = true;

                // APIリクエストを送信
                using (var httpClientStr = new HttpClient())
                {
                    httpClientStr.Timeout = TimeSpan.FromSeconds(300d);

                    // HttpRequestMessageの作成
                    var httpRequestMessage = new HttpRequestMessage
                    {
                        Method = HttpMethod.Post,
                        RequestUri = new Uri(VMLocator.MainWindowViewModel.ApiUrl),
                        Headers = {
                        { HttpRequestHeader.Authorization.ToString(), $"Bearer {VMLocator.MainWindowViewModel.ApiKey}" },
                        { HttpRequestHeader.ContentType.ToString(), "application/json" }
                    },
                        Content = new StringContent(JsonSerializer.Serialize(options), Encoding.UTF8, "application/json")
                    };

                    // SendAsyncでレスポンスを取得
                    var response = await httpClientStr.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseHeadersRead);

                    // レスポンスが成功した場合
                    if (response.IsSuccessStatusCode)
                    {
                        // レスポンスのStreamを取得
                        using var stream = await response.Content.ReadAsStreamAsync();
                        using var reader = new StreamReader(stream);
                        string? line;

                        // レスポンスを行ごとに読み込む
                        while ((line = await reader.ReadLineAsync()) != null)
                        {
                            // "data: "で始まる行をパース
                            if (line.StartsWith("data: "))
                            {
                                var json = line.Substring(6);

                                // "data: [DONE]"を受け取ったらループを終了
                                if (json == "[DONE]")
                                {
                                    await VMLocator.ChatViewModel.UpdateUIWithReceivedMessage("[DONE]");
                                    break;
                                }

                                var chatResponse = JsonSerializer.Deserialize<ResponseChunkData>(json);

                                // choices[0].delta.contentに差分の文字列がある場合
                                if (chatResponse?.choices?.FirstOrDefault()?.delta?.content != null)
                                {
                                    // ログをUIに出力
                                    chatTextRes += chatResponse.choices[0].delta.content;
                                    await VMLocator.ChatViewModel.UpdateUIWithReceivedMessage(chatTextRes);
                                }
                            }
                        }

                        // 入力トークン数を計算
                        var inputConversationTokenCount = tokenizer.Encode(conversationHistory.Select(d => d["content"].ToString()).Aggregate((a, b) => a + b)).Count;

                        // レスポンスのトークン数を計算
                        var responseTokenCount = tokenizer.Encode(chatTextRes).Count;

                        // レス本文
                        chatTextRes = Environment.NewLine + chatTextRes + Environment.NewLine + Environment.NewLine;

                        // 応答に成功したconversationHistoryを保存
                        VMLocator.ChatViewModel.LastConversationHistory = postedConversationHistory;

                        // 応答を受け取った後、conversationHistory に追加
                        conversationHistory.Add(new Dictionary<string, object>() { { "role", "assistant" }, { "content", chatTextRes } });

                        // ビューモデルを更新
                        VMLocator.ChatViewModel.ConversationHistory = conversationHistory;

                        // デバッグ
                        //var cdialog = new ContentDialog() { Title = string.Join("\n", conversationHistory.Select(d => string.Join(", ", d.Select(pair => $"{pair.Key}: {pair.Value}")))), PrimaryButtonText = "OK" };
                        //await VMLocator.MainViewModel.ContentDialogShowAsync(cdialog);

                        // usageを計算
                        chatTextRes += $"usage={{\"prompt_tokens\":{inputConversationTokenCount},\"completion_tokens\":{responseTokenCount},\"total_tokens\":{inputConversationTokenCount + responseTokenCount}}}" + Environment.NewLine;

                        // 要約が実行された場合、メソッドの戻り値の最後に要約前のトークン数と要約後のトークン数をメッセージとして付け加える
                        string? postConversation = conversationHistory.Select(d => d["content"].ToString()).Aggregate((a, b) => a + b);
                        if (preSummarizedHistoryTokenCount > tokenizer.Encode(postConversation).Count)
                        {
                            chatTextRes += $"-Conversation history has been summarized. before: {preSummarizedHistoryTokenCount}, after: {tokenizer.Encode(postConversation).Count}.{Environment.NewLine}";
                        }
                        else if (isDeleteHistory) // 会話履歴が全て削除された場合
                        {
                            chatTextRes += $"-Conversation history has been removed. before: {preSummarizedHistoryTokenCount}, after: {tokenizer.Encode(postConversation).Count}.{Environment.NewLine}";
                        }

                        //会話が成立した時点でタイトルが空欄だったらタイトルを自動生成する
                        if (string.IsNullOrEmpty(currentTitle))
                        {
                            VMLocator.ChatViewModel.ChatTitle = await GetTitleAsync(currentTitle);
                        }
                    }
                    else
                    {
                        string errorBody = await response.Content.ReadAsStringAsync();
                        throw new Exception($"Error: Response status code does not indicate success: {response.StatusCode} ({response.ReasonPhrase}). Response body: {errorBody}");
                    }
                }
                return chatTextRes;
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}");
            }
        }

        public class ResponseChunkData
        {
            public string id { get; set; }
            public string @object { get; set; }
            public int created { get; set; }
            public string model { get; set; }
            public List<ChunkChoice> choices { get; set; }
        }

        public class ChunkChoice
        {
            public Message delta { get; set; }
            public int index { get; set; }
            public object? finish_reason { get; set; }
        }

        public class Message
        {
            public string? role { get; set; }
            public string? content { get; set; }
        }

        //文章要約圧縮メソッド--------------------------------------------------------------
        public async Task<string> GetSummaryAsync(string forCompMes)
        {
            string summary;

            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", VMLocator.MainWindowViewModel.ApiKey);
                httpClient.Timeout = TimeSpan.FromSeconds(200d);

                var options = new Dictionary<string, object>
                {
                    { "model", "gpt-3.5-turbo" },
                    { "messages", new List<Dictionary<string, object>>
                        {
                            new Dictionary<string, object> { { "role", "system" }, { "content", "You are a professional editor. Please summarize the following chat log in about 300 tokens using the language in which the text is written. For a text that includes multiple conversations, the conversation set that appears at the beginning is the most important." } },
                            new Dictionary<string, object> { { "role", "user" }, { "content", forCompMes } }
                        }
                    }
                };

                string jsonContent = JsonSerializer.Serialize(options);

                var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync(VMLocator.MainWindowViewModel.ApiUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    string responseBody = await response.Content.ReadAsStringAsync();
                    var responseJson = JsonSerializer.Deserialize<JsonElement>(responseBody);
                    summary = responseJson.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString().Trim();
                }
                else
                {
                    string errorBody = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Summarize Error: Response status code does not indicate success: {response.StatusCode} ({response.ReasonPhrase}). Response body: {errorBody}");
                }
            }

            return summary;
        }

        //タイトル命名メソッド--------------------------------------------------------------
        public async Task<string> GetTitleAsync(string forTitleMes)
        {
            string title;

            forTitleMes = VMLocator.ChatViewModel.ConversationHistory.Select(message => message["content"].ToString()).Reverse().Aggregate((a, b) => a + b);

            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", VMLocator.MainWindowViewModel.ApiKey);
                httpClient.Timeout = TimeSpan.FromSeconds(200d);

                var options = new Dictionary<string, object>
                {
                    { "model", "gpt-3.5-turbo" },
                    { "messages", new List<Dictionary<string, object>>
                        {
                            new Dictionary<string, object> { { "role", "system" }, { "content",
                                    "あなたはプロの編集者です。これから送るチャットログにチャットタイトルをつけてそれだけを回答してください。\n" +
                                    "- チャットの会話で使われている言語でタイトルを考えてください。\n" +
                                    "- ログは冒頭に行くほど重要な情報です。\n" +
                                    "# 制約条件\n" +
                                    "- 「」や\"\'などの記号を使わないこと。\n" +
                                    "- 句読点を使わないこと。\n" +
                                    "- 短くシンプルに、UNICODEの全角文字に換算して最大でも16文字を絶対に超えないように。これは重要な条件です。\n" +
                                    "# 例\n" +
                                    "宣言型モデルとフロントエンド\n" +
                                    "日英翻訳" } },
                            new Dictionary<string, object> { { "role", "user" }, { "content", forTitleMes } }
                        }
                    }
                };

                string jsonContent = JsonSerializer.Serialize(options);

                var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync(VMLocator.MainWindowViewModel.ApiUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    string responseBody = await response.Content.ReadAsStringAsync();
                    var responseJson = JsonSerializer.Deserialize<JsonElement>(responseBody);
                    char[] charsToTrim = { ' ', '\"', '\'', '[', ']', '「', '」' };
                    title = responseJson.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString().Trim();
                }
                else
                {
                    string errorBody = await response.Content.ReadAsStringAsync();
                    var cdialog = new ContentDialog() { Title = $"Title Naming Error: Response status code does not indicate success: {response.StatusCode} ({response.ReasonPhrase}). Response body: {errorBody}", PrimaryButtonText = "OK" };
                    await VMLocator.MainViewModel.ContentDialogShowAsync(cdialog);
                    return $"Error in title naming";
                }
            }

            return title;
        }

        // ストリーム表示用チャットログHTML変換--------------------------------------------------------------
        public async Task<string> ConvertAddLogToHtml(string plainTextChatLog, DateTime resDate)
        {
            plainTextChatLog = Regex.Replace(plainTextChatLog, @"\r\n|\r|\n", Environment.NewLine);
            var codeSnippetRegex = new Regex(@"^```(?:([\w-+#.]+)\s+)?([\s\S]*?)(^```)", RegexOptions.Multiline);
            var usageRegex = new Regex(@"^usage=", RegexOptions.Multiline);


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

            var doc = new HtmlDocument();
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

            return doc.DocumentNode.OuterHtml;

        }
    }
}
