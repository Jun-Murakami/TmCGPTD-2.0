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

namespace TmCGPTD.Models
{
    public class HtmlProcess
    {
        // 表示用HTML初期化--------------------------------------------------------------
        public async Task<string> InitializeChatLogToHtml()
        {
            using var streamReader = new StreamReader(AvaloniaLocator.Current.GetService<IAssetLoader>().Open(new Uri("avares://TmCGPTD/Assets/ChatTemplete.html")));
            using var chatCssStreamReader = new StreamReader(AvaloniaLocator.Current.GetService<IAssetLoader>().Open(new Uri("avares://TmCGPTD/Assets/ChatStyles.css")));
            using var cssStreamReader = new StreamReader(AvaloniaLocator.Current.GetService<IAssetLoader>().Open(new Uri("avares://TmCGPTD/Assets/vs2015.min.css")));


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
            using var streamReader = new StreamReader(AvaloniaLocator.Current.GetService<IAssetLoader>().Open(new Uri("avares://TmCGPTD/Assets/ChatTemplete.html")));
            using var chatCssStreamReader = new StreamReader(AvaloniaLocator.Current.GetService<IAssetLoader>().Open(new Uri("avares://TmCGPTD/Assets/ChatStyles.css")));
            using var cssStreamReader = new StreamReader(AvaloniaLocator.Current.GetService<IAssetLoader>().Open(new Uri("avares://TmCGPTD/Assets/vs2015.min.css")));


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
                    foreach (var textNode in documentDiv.ChildNodes)
                    {
                        if (textNode.NodeType == HtmlNodeType.Text)
                        {
                            textNode.InnerHtml = textNode.InnerHtml.Trim('\r', '\n');
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


                HtmlNode titleNode = htmlDoc.DocumentNode.SelectSingleNode("//title");

                if (titleNode != null)
                {
                    string titleText = titleNode.InnerText;
                    if (titleText == "New chat" || titleText == "")
                    {
                        return "Please display chat screen.";
                    }
                    else
                    {
                        webChatTitle = titleText;
                    }
                }
                else
                {
                    return "Please display chat screen.";
                }

                // mainタグをサーチ
                var mainTag = htmlDoc.DocumentNode.SelectSingleNode("//main");
                if (mainTag == null)
                {
                    return "Please display chat screen.";
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

                        string htmlString = div.InnerHtml;

                        // 正規表現パターンに基づいて置換・削除
                        string pattern = "<title.*>.*</title>";
                        htmlString = Regex.Replace(htmlString, pattern, "");

                        pattern = "<text.*>.*</text>";
                        htmlString = Regex.Replace(htmlString, pattern, "");

                        pattern = "<div>Finished browsing</div>";
                        htmlString = Regex.Replace(htmlString, pattern, "");

                        pattern = "<span class=.*>[0-9]+ / [0-9]+</span>";
                        htmlString = Regex.Replace(htmlString, pattern, "");

                        // 置換処理
                        htmlString = htmlString.Replace("<pre class=\"\">", $"{br}{br}```")
                                               .Replace("<pre>", $"{br}{br}```")
                                               .Replace("</pre>", $"{br}```{br}{br}")
                                               .Replace("Copy code", $"{br}")
                                               .Replace("<ol>", $"{br}")
                                               .Replace("</ol>", $"{br}")
                                               .Replace("<ul>", $"{br}")
                                               .Replace("</ul>", $"{br}")
                                               .Replace("<li>", $"{br}- ")
                                               .Replace("</li>", $"{br}");

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
                var msg = await _dbProcess.InsertWebChatLogDatabaseAsync(webChatTitle, webConversationHistory, webLog);
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

        // APIに接続してレスポンス取得--------------------------------------------------------------
        public async Task<string> PostChatAsync(string chatTextPost)
        {
            try
            {
                List<Dictionary<string, object>> conversationHistory = VMLocator.ChatViewModel.ConversationHistory;

                bool isDeleteHistory = false;
                string chatTextRes = "";
                string currentTitle = VMLocator.ChatViewModel.ChatTitle;
                int MAX_CONTENT_LENGTH = VMLocator.MainWindowViewModel.MaxContentLength;
                TikToken tokenizer = TikToken.EncodingForModel("gpt-3.5-turbo");

                // 過去の会話履歴と現在の入力を結合する前に、過去の会話履歴に含まれるcontent文字列のトークン数を取得
                int historyContentTokenCount = conversationHistory.Sum(d => tokenizer.Encode(d["content"].ToString()).Count);

                // 要約前のトークン数を記録
                int preSummarizedHistoryTokenCount = historyContentTokenCount;

                // 履歴を逆順にする
                List<Dictionary<string, object>> reversedHistoryList = conversationHistory;
                reversedHistoryList.Reverse();

                // 入力文字列のトークン数を取得
                int inputTokenCount = tokenizer.Encode(chatTextPost).Count;

                // 入力文字列のトークン数が制限トークン数「MAX_CONTENT_LENGTH」を超えた場合
                if (inputTokenCount > MAX_CONTENT_LENGTH)
                {
                    throw new Exception($"The input text (inputTokenCount) exceeds the maximum token limit ({MAX_CONTENT_LENGTH}). Please remove {inputTokenCount - MAX_CONTENT_LENGTH} tokens.{Environment.NewLine}");
                }

                // 過去の履歴＋ユーザーの新規入力が制限トークン数「MAX_CONTENT_LENGTH」を超えた場合
                if (historyContentTokenCount + inputTokenCount > MAX_CONTENT_LENGTH)
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

                        if (i <= 4 && historyTokenCount < MAX_CONTENT_LENGTH / 5) //直近の会話が短ければそのまま生かす
                        {
                            messageStart += 1;
                        }

                        if (historyTokenCount > MAX_CONTENT_LENGTH)
                        {
                            messagesToSelect = i + 1; // 最後に処理した次のインデックスを記録
                            break;
                        }
                    }

                    foreach (var dict in reversedHistoryList)
                    {
                        string dictString = string.Join(", ", dict.Select(pair => $"{pair.Key}: {pair.Value}"));
                    }

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
                        conversationHistory.Clear();
                        isDeleteHistory = true;
                    }

                }

                // 現在のユーザーの入力を表すディクショナリ
                var userInput = new Dictionary<string, object>() { { "role", "user" }, { "content", chatTextPost } };

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
                        string line;

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

                        // 応答を受け取った後、conversationHistory に追加
                        conversationHistory.Add(new Dictionary<string, object>() { { "role", "assistant" }, { "content", chatTextRes } });

                        // usageを計算
                        chatTextRes += $"usage={{\"prompt_tokens\":{inputConversationTokenCount},\"completion_tokens\":{responseTokenCount},\"total_tokens\":{inputConversationTokenCount + responseTokenCount}}}" + Environment.NewLine;

                        // 要約が実行された場合、メソッドの戻り値の最後に要約前のトークン数と要約後のトークン数をメッセージとして付け加える
                        string postConversation = conversationHistory.Select(d => d["content"].ToString()).Aggregate((a, b) => a + b);
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
            public object finish_reason { get; set; }
        }

        public class Message
        {
            public string role { get; set; }
            public string content { get; set; }
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
            string summary;

            forTitleMes = VMLocator.ChatViewModel.ConversationHistory.Select(message => message["content"].ToString()).Reverse().Aggregate((a, b) => a + b); ;

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
                                    "- チャットで使われている言語でタイトルを考えてください。\n" +
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
                    summary = responseJson.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString().Trim();
                }
                else
                {
                    string errorBody = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Error: Response status code does not indicate success: {response.StatusCode} ({response.ReasonPhrase}). Response body: {errorBody}");
                }
            }

            return summary;
        }

        // 表示用チャットログHTML変換--------------------------------------------------------------
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
                    foreach (var textNode in documentDiv.ChildNodes)
                    {
                        if (textNode.NodeType == HtmlNodeType.Text)
                        {
                            textNode.InnerHtml = textNode.InnerHtml.Trim('\r', '\n');
                        }
                    }
                }
            }

            return doc.DocumentNode.OuterHtml;

        }
    }
}
