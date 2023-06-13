using FluentAvalonia.UI.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TiktokenSharp;

namespace TmCGPTD.Models
{
    public class ChatProcess
    {
        // APIに接続してレスポンス取得--------------------------------------------------------------
        public async Task<string> PostChatAsync(string chatTextPost)
        {
            try
            {
                List<Dictionary<string, object>>? conversationHistory = VMLocator.ChatViewModel.ConversationHistory; // 会話履歴を取得

                string currentTitle = VMLocator.ChatViewModel.ChatTitle; // チャットのタイトルを取得

                // ----------------------------------------

                int? separatorIndex = null; // システムメッセージと本文の区切り位置
                string systemMessage = ""; // システムメッセージ

                // システムメッセージの判定
                if (chatTextPost.StartsWith("#system", StringComparison.OrdinalIgnoreCase) || chatTextPost.StartsWith("# system", StringComparison.OrdinalIgnoreCase))
                {
                    chatTextPost = Regex.Replace(chatTextPost, @"^#(\s*?)system", "", RegexOptions.IgnoreCase).Trim();

                    // 最初の"---"の位置を検索
                    separatorIndex = chatTextPost.IndexOf("---");

                    if (separatorIndex != -1)
                    {
                        systemMessage = chatTextPost.Substring(0, (int)separatorIndex).Trim();//システムメッセージを取得
                        chatTextPost = chatTextPost.Substring((int)(separatorIndex + 3)).Trim();//本文だけ残す
                    }
                    else
                    {   // 本文がない場合はシステムメッセージのみまたは初期化動作
                        systemMessage = chatTextPost.Trim();
                        chatTextPost = "";
                    }

                    // 本文が空またはシステムメッセージが空の場合
                    if (string.IsNullOrWhiteSpace(chatTextPost) || string.IsNullOrWhiteSpace(systemMessage))
                    {
                        VMLocator.ChatViewModel.LastConversationHistory = new List<Dictionary<string, object>>(conversationHistory);

                        var itemToRemove = GetSystemMessageItem(conversationHistory);
                        if (itemToRemove != null)
                        {
                            conversationHistory.Remove(itemToRemove);
                        }

                        var systemInput = new Dictionary<string, object>() { { "role", "system" }, { "content", systemMessage } };
                        if (!string.IsNullOrWhiteSpace(systemMessage))
                        {
                            // 会話履歴の先頭にシステムメッセージを追加
                            conversationHistory.Insert(0, systemInput);
                        }

                        // ビューモデルを更新
                        VMLocator.ChatViewModel.ConversationHistory = conversationHistory;

                        if (string.IsNullOrEmpty(VMLocator.ChatViewModel.ChatCategory))
                        {
                            VMLocator.ChatViewModel.ChatCategory = "API Chat";
                        }

                        return "";
                    }
                }

                // 既存のシステムメッセージをディープコピー
                Dictionary<string, object>? recenctSystemMessage = null;
                foreach (var item in conversationHistory)
                {
                    if (item.ContainsKey("role") && item["role"].ToString() == "system" && item.ContainsKey("content"))
                    {
                        string json = JsonSerializer.Serialize(item);
                        recenctSystemMessage = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(json);
                        break;
                    }
                }

                // ----------------------------------------

                // トークナイザーの初期化
                TikToken tokenizer = TikToken.EncodingForModel("gpt-3.5-turbo");

                // 入力文字列のトークン数を取得
                int inputTokenCount = tokenizer.Encode(chatTextPost).Count;

                // 過去の会話履歴と現在の入力を結合する前に、過去の会話履歴に含まれるcontent文字列のトークン数を取得
                int historyContentTokenCount = conversationHistory.Sum(d => tokenizer.Encode(d["content"].ToString()!).Count);

                // 要約前のトークン数を記録
                int preSummarizedHistoryTokenCount = historyContentTokenCount;

                // トークン関連のデフォルト値を取得し設定
                int maxTokens = VMLocator.MainWindowViewModel.ApiMaxTokens;
                if (!VMLocator.MainWindowViewModel.ApiMaxTokensIsEnable) { maxTokens = 0; }

                int maxContentLength = VMLocator.MainWindowViewModel.MaxContentLength;
                if (!VMLocator.MainWindowViewModel.MaxContentLengthIsEnable) { maxContentLength = 2048; }

                // 制限文字数の計算
                int limitLength = VMLocator.MainWindowViewModel.ApiMaxTokensIsEnable ? inputTokenCount + maxTokens + 400 : maxContentLength;

                // 履歴を逆順にして保存
                List<Dictionary<string, object>> reversedHistoryList = new List<Dictionary<string, object>>(conversationHistory);
                reversedHistoryList.Reverse();

                // 会話履歴を全削除したかどうかのフラグ
                bool isDeleteHistory = false;

                // ----------------------------------------

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

                // ----------------------------------------

                // 過去の履歴＋ユーザーの新規入力＋maxTokensがmaxContentLengthを超えた場合の要約処理
                if (historyContentTokenCount + inputTokenCount + maxTokens > maxContentLength && historyContentTokenCount > 0)
                {
                    int historyTokenCount = 0;
                    int messagesToSelect = 0; // 会話履歴のどのインデックスまで選択するか
                    int messageStart = 0; // 会話履歴のどのインデックスから選択するか
                    string? forCompMes = "";

                    // 会話履歴の最新のものからトークン数を数えて一時変数「historyTokenCount」に足していく
                    for (int i = 0; i < reversedHistoryList.Count; i += 1)
                    {
                        string? mes = reversedHistoryList[i]["content"].ToString();
                        int messageTokenCount = tokenizer.Encode(mes!).Count;
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

                    // 会話履歴から適切な数だけをセレクトする
                    int rangeLength = Math.Min(messagesToSelect - messageStart, reversedHistoryList.Count - messageStart);

                    if (rangeLength > 0)
                    {
                        forCompMes = reversedHistoryList.GetRange(messageStart, rangeLength).Select(message => message["content"].ToString()).Aggregate((a, b) => a + b);
                    }
                    else if (messagesToSelect == 0)
                    {
                        if (reversedHistoryList.Count > 0)
                        {
                            forCompMes = reversedHistoryList[0]["content"].ToString();
                        }
                        else
                        {
                            throw new Exception($"Error: Can't find conversation history to summarize.{Environment.NewLine}");
                        }
                    }

                    if (messagesToSelect > 0)
                    {
                        // 抽出したテキストを要約APIリクエストに送信
                        try
                        {
                            string summary = await GetSummaryAsync(forCompMes!);
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
                            conversationHistory.Reverse();
                            conversationHistory.RemoveRange(messageStart, conversationHistory.Count - messageStart);
                            conversationHistory.Reverse();
                            conversationHistory.Insert(0, new Dictionary<string, object>() { { "role", "assistant" }, { "content", summary } });
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

                    // 要約または削除完了後、システムメッセージが残っていれば削除
                    var itemToRemove = GetSystemMessageItem(conversationHistory);
                    if (itemToRemove != null)
                    {
                        conversationHistory.Remove(itemToRemove);
                    }

                    // 保存したシステムメッセージを再挿入
                    if (recenctSystemMessage != null)
                    {
                        conversationHistory.Insert(0, recenctSystemMessage);
                    }
                }

                // システムメッセージの処理
                if (separatorIndex != null)
                {
                    var systemInput = new Dictionary<string, object>() { { "role", "system" }, { "content", systemMessage } };

                    // 既存のシステムメッセージがあれば削除
                    var itemToRemove = GetSystemMessageItem(conversationHistory);
                    if (itemToRemove != null)
                    {
                        conversationHistory.Remove(itemToRemove);
                    }

                    // システムメッセージが空の時はリセットされる扱いにする
                    if (!string.IsNullOrWhiteSpace(systemMessage))
                    {
                        // 会話履歴の先頭にシステムメッセージを追加
                        conversationHistory.Insert(0, systemInput);
                    }
                }

                // 現在のユーザーの入力を表すディクショナリ
                var userInput = new Dictionary<string, object>() { { "role", "user" }, { "content", chatTextPost } };

                // 送信直前の会話履歴をpostedConversationHistoryにディープコピー
                List<Dictionary<string, object>>? postedConversationHistory = new List<Dictionary<string, object>>();
                string jsonCopy = JsonSerializer.Serialize(conversationHistory);
                postedConversationHistory = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(jsonCopy);

                // 会話履歴と現在の入力を結合
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


                // ----------------------------------------

                // レスポンス格納用変数
                string chatTextRes = "";

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
                        bool isDoneReceived = false;

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
                                    //await VMLocator.ChatViewModel.UpdateUIWithReceivedMessage("[DONE]", chatTextRes);
                                    isDoneReceived = true;
                                    break;
                                }

                                var chatResponse = JsonSerializer.Deserialize<ResponseChunkData>(json);

                                // choices[0].delta.contentに差分の文字列がある場合
                                if (chatResponse?.choices?.FirstOrDefault()?.delta?.content != null)
                                {
                                    // ログをUIに出力
                                    chatTextRes += chatResponse.choices[0].delta!.content;
                                    await VMLocator.ChatViewModel.UpdateUIWithReceivedMessage(chatResponse.choices[0].delta!.content, chatTextRes);
                                }
                            }
                        }

                        // [DONE]を受け取らなかったらエラー処理
                        if (!isDoneReceived)
                        {
                            await VMLocator.ChatViewModel.UpdateUIWithReceivedMessage("[ERROR]", chatTextRes);
                            chatTextRes += $"{Environment.NewLine}[ERROR] Connection has been terminated.";
                        }

                        // 入力トークン数を計算
                        var inputConversationTokenCount = tokenizer.Encode(conversationHistory.Select(d => d["content"].ToString()).Aggregate((a, b) => a + b)!).Count;

                        // レスポンスのトークン数を計算
                        var responseTokenCount = tokenizer.Encode(chatTextRes).Count;

                        // レス本文
                        chatTextRes = Environment.NewLine + chatTextRes + Environment.NewLine + Environment.NewLine;

                        // 応答に成功したconversationHistoryを保存
                        VMLocator.ChatViewModel.LastConversationHistory = postedConversationHistory!;

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
                        if (preSummarizedHistoryTokenCount > tokenizer.Encode(postConversation!).Count)
                        {
                            chatTextRes += $"-Conversation history has been summarized. before: {preSummarizedHistoryTokenCount}, after: {tokenizer.Encode(postConversation).Count}.{Environment.NewLine}";
                        }
                        else if (isDeleteHistory) // 会話履歴が全て削除された場合
                        {
                            chatTextRes += $"-Conversation history has been removed. before: {preSummarizedHistoryTokenCount}, after: {tokenizer.Encode(postConversation).Count}.{Environment.NewLine}";
                        }

                        await VMLocator.ChatViewModel.UpdateUIWithReceivedMessage("[DONE]", chatTextRes.Trim());

                        //会話が成立した時点でタイトルが空欄だったらタイトルを自動生成する
                        if (string.IsNullOrEmpty(currentTitle))
                        {
                            VMLocator.ChatViewModel.ChatTitle = await GetTitleAsync(currentTitle);
                        }

                        // 会話が成立した時点でカテゴリーが空欄だったら「API Chat」を自動設定する
                        if (string.IsNullOrEmpty(VMLocator.ChatViewModel.ChatCategory))
                        {
                            VMLocator.ChatViewModel.ChatCategory = "API Chat";
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

        private Dictionary<string, object>? GetSystemMessageItem(List<Dictionary<string, object>>? conversationHistory)
        {
            foreach (var item in conversationHistory!)
            {
                if (item.ContainsKey("role") && item["role"].ToString() == "system" && item.ContainsKey("content"))
                {
                    return item;
                }
            }
            return null;
        }

        public class ResponseChunkData
        {
            public string? id { get; set; }
            public string? @object { get; set; }
            public int created { get; set; }
            public string? model { get; set; }
            public List<ChunkChoice>? choices { get; set; }
        }

        public class ChunkChoice
        {
            public Message? delta { get; set; }
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
                    summary = responseJson.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString()!.Trim();
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

            forTitleMes = VMLocator.ChatViewModel.ConversationHistory.Select(message => message["content"].ToString()).Reverse().Aggregate((a, b) => a + b)!;

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
                    title = responseJson.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString()!.Trim();
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
    }
}
