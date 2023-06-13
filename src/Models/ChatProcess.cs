using FluentAvalonia.UI.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TiktokenSharp;

namespace TmCGPTD.Models
{
    public class ChatProcess
    {
        public class ChatParameters
        {
            public string? UserInput { get; set; } // ユーザー入力
            public string? UserInputBody { get; set; } //システムメッセージがある場合ここに本文だけが入る
            public string? AssistantResponse { get; set; } // アシスタントの返答
            public string? ChatTitle { get; set; } // チャットのタイトル
            public Dictionary<string, object>? OldSystemMessageDic { get; set; } // 古いシステムメッセージの辞書
            public string? NewSystemMessageStr { get; set; } // 新しいシステムメッセージの文字列
            public List<Dictionary<string, object>>? ConversationHistory { get; set; } // 会話履歴
            public List<Dictionary<string, object>>? PostedConversationHistory { get; set; } // 投稿確定の会話履歴
            public int PreSummarizedHistoryTokenCount { get; set; } // 要約前のトークン数
            public bool IsDeleteHistory { get; set; } // 会話履歴を全削除したかどうかのフラグ
            public Dictionary<string, object>? Options { get; set; } // チャットのパラメータ
        }

        // メインルーチン--------------------------------------------------------------
        public async Task<string> PostChatAsync(ChatParameters chatParameters)
        {
            try
            {
                chatParameters = await ProcessSendMessageAsync(chatParameters);
                chatParameters = await SetOptionParametersAsync(chatParameters);
                string chatTextRes = await SendAndRecieveChatAsync(chatParameters);

                return chatTextRes;
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}");
            }
        }

        // トークン数制限の事前処理--------------------------------------------------------------
        private async Task<ChatParameters> ProcessSendMessageAsync(ChatParameters chatParameters)
        {
            string? chatTextPost = chatParameters.UserInput; // ユーザー入力全文
            string currentTitle = chatParameters.ChatTitle!; // チャットタイトル

            List<Dictionary<string, object>>? conversationHistory = chatParameters.ConversationHistory; // 会話履歴

            TikToken tokenizer = TikToken.EncodingForModel("gpt-3.5-turbo"); // トークナイザーの初期化

            int inputTokenCount = tokenizer.Encode(chatTextPost!).Count; // 入力文字列のトークン数を取得

            // 過去の会話履歴と現在の入力を結合する前に、過去の会話履歴に含まれるcontent文字列のトークン数を取得
            int historyContentTokenCount = conversationHistory!.Sum(d => tokenizer.Encode(d["content"].ToString()!).Count);

            chatParameters.PreSummarizedHistoryTokenCount = historyContentTokenCount; // 要約前のトークン数を記録

            // トークン関連のデフォルト値を取得し設定
            int maxTokens = AppSettings.Instance.ApiMaxTokens;
            if (!AppSettings.Instance.ApiMaxTokensIsEnable) { maxTokens = 0; }

            int maxContentLength = AppSettings.Instance.MaxContentLength;
            if (!AppSettings.Instance.MaxContentLengthIsEnable) { maxContentLength = 2048; }

            // 制限文字数の計算
            int limitLength = AppSettings.Instance.ApiMaxTokensIsEnable ? inputTokenCount + maxTokens + 400 : maxContentLength;

            // 履歴を逆順にして保存
            List<Dictionary<string, object>> reversedHistoryList = new List<Dictionary<string, object>>(conversationHistory!);
            reversedHistoryList.Reverse();

            // ----------------------------------------
            // 入力文字列 + maxTokensが4096を超えた場合
            if ((inputTokenCount + maxTokens) > 4096)
            {
                if (AppSettings.Instance.ApiMaxTokensIsEnable)
                {
                    throw new Exception($"The values for input text ({inputTokenCount}) + max_tokens ({maxTokens}) exceeds 4097 tokens. Please reduce by at least {(inputTokenCount + maxTokens) - 4097} tokens.{Environment.NewLine}");
                }
                else
                {
                    throw new Exception($"The values for input text ({inputTokenCount}) exceeds 4097 tokens. Please reduce by at least {inputTokenCount - 4096} tokens.{Environment.NewLine}");
                }
            }

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

                    // 以下のコードは上記のコードと同じ処理
                    //List<Dictionary<string, object>> rangeList = reversedHistoryList.GetRange(messageStart, rangeLength);
                    //List<string> contentList = new List<string>();
                    //foreach (Dictionary<string, object> message in rangeList)
                    //{
                    //    contentList.Add(message["content"].ToString());
                    //}
                    //
                    //forCompMes = "";
                    //foreach (string content in contentList)
                    //{
                    //    forCompMes += content;
                    //}
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
                        conversationHistory!.Reverse();
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
                    if (conversationHistory!.Count() > 0)
                    {
                        conversationHistory!.Clear();
                        chatParameters.IsDeleteHistory = true;
                    }
                }

                // 要約または削除完了後、システムメッセージが残っていれば削除
                var itemToRemove = GetSystemMessageItem(conversationHistory);
                if (itemToRemove != null)
                {
                    conversationHistory!.Remove(itemToRemove);
                }

                // 保存したシステムメッセージがあれば再挿入
                if (chatParameters.OldSystemMessageDic != null)
                {
                    conversationHistory!.Insert(0, chatParameters.OldSystemMessageDic);
                }
            }

            chatParameters.ConversationHistory = conversationHistory;

            return chatParameters;
        }

        // システムメッセージ取得
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

        // オプションパラメータをセット--------------------------------------------------------------
        private async Task<ChatParameters> SetOptionParametersAsync(ChatParameters chatParameters)
        {
            string? chatTextPost = chatParameters.UserInput; // ユーザー入力全文
            List<Dictionary<string, object>>? conversationHistory = chatParameters.ConversationHistory; // 会話履歴

            // システムメッセージの処理
            if (!string.IsNullOrWhiteSpace(chatParameters.NewSystemMessageStr))
            {
                var systemInput = new Dictionary<string, object>() { { "role", "system" }, { "content", chatParameters.NewSystemMessageStr } };

                // 既存のシステムメッセージがあれば削除
                var itemToRemove = GetSystemMessageItem(conversationHistory);
                if (itemToRemove != null)
                {
                    conversationHistory!.Remove(itemToRemove);
                }

                // 会話履歴の先頭にシステムメッセージを追加
                conversationHistory!.Insert(0, systemInput);
            }

            if(!string.IsNullOrWhiteSpace(chatParameters.UserInputBody)) //システムメッセージがある場合、ユーザー入力は本文のみ
            {
                chatTextPost = chatParameters.UserInputBody;
            }

            // 現在のユーザーの入力を表すディクショナリ
            var userInput = new Dictionary<string, object>() { { "role", "user" }, { "content", chatTextPost! } };

            // 送信直前の会話履歴をpostedConversationHistoryにディープコピー
            string jsonCopy = JsonSerializer.Serialize(conversationHistory);
            chatParameters.PostedConversationHistory = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(jsonCopy);

            conversationHistory!.Add(userInput); // 会話履歴と現在の入力を結合

            // オプションパラメータを追加
            var settings = AppSettings.Instance;
            var options = new Dictionary<string, object>() { { "model", settings.ApiModel }, { "messages", conversationHistory } };
            if (settings.ApiMaxTokensIsEnable)
                options.Add("max_tokens", settings.ApiMaxTokens);
            if (settings.ApiTemperatureIsEnable)
                options.Add("temperature", settings.ApiTemperature);
            if (settings.ApiTopPIsEnable)
                options.Add("top_p", settings.ApiTopP);
            if (settings.ApiNIsEnable)
                options.Add("n", settings.ApiN);
            if (settings.ApiLogprobIsEnable)
                options.Add("logprobs", settings.ApiLogprobs);
            if (settings.ApiPresencePenaltyIsEnable)
                options.Add("presence_penalty", settings.ApiPresencePenalty);
            if (settings.ApiFrequencyPenaltyIsEnable)
                options.Add("frequency_penalty", settings.ApiFrequencyPenalty);
            if (settings.ApiBestOfIsEnable)
                options.Add("best_of", settings.ApiBestOf);

            if (settings.ApiStopIsEnable) // api_stop パラメータの処理
            {
                string[] stopSequence = settings.ApiStop.Split(',');
                options.Add("stop", stopSequence);
            }

            if (settings.ApiLogitBiasIsEnable) // api_logit_bias パラメータの処理
            {
                var logitBias = JsonSerializer.Deserialize<Dictionary<string, double>>(settings.ApiLogitBias);
                options.Add("logit_bias", logitBias!);
            }

            options["stream"] = true; // stream 有効化

            chatParameters.Options = options;
            chatParameters.ConversationHistory = conversationHistory;

            return chatParameters;
        }


        // APIに接続してレスポンス取得--------------------------------------------------------------
        public async Task<string> SendAndRecieveChatAsync(ChatParameters chatParameters)
        {
            string chatTextRes = ""; // レスポンス格納用変数
            List<Dictionary<string, object>>? conversationHistory = chatParameters.ConversationHistory; // 会話履歴

            // APIリクエストを送信
            using (var httpClientStr = new HttpClient())
            {
                var settings = AppSettings.Instance;

                httpClientStr.Timeout = TimeSpan.FromSeconds(300d);

                // HttpRequestMessageの作成
                var httpRequestMessage = new HttpRequestMessage
                {
                    Method = HttpMethod.Post,
                    RequestUri = new Uri(settings.ApiUrl),
                    Headers = {
                        { HttpRequestHeader.Authorization.ToString(), $"Bearer {settings.ApiKey}" },
                        { HttpRequestHeader.ContentType.ToString(), "application/json" }
                    },
                    Content = new StringContent(JsonSerializer.Serialize(chatParameters.Options), Encoding.UTF8, "application/json")
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
                    TikToken tokenizer = TikToken.EncodingForModel("gpt-3.5-turbo");
                    var inputConversationTokenCount = tokenizer.Encode(conversationHistory!.Select(d => d["content"].ToString()).Aggregate((a, b) => a + b)!).Count;

                    // レスポンスのトークン数を計算
                    var responseTokenCount = tokenizer.Encode(chatTextRes).Count;

                    // レス本文
                    chatTextRes = Environment.NewLine + chatTextRes + Environment.NewLine + Environment.NewLine;

                    // 応答に成功したconversationHistoryを保存
                    VMLocator.ChatViewModel.LastConversationHistory = chatParameters.PostedConversationHistory!;

                    // 応答を受け取った後、conversationHistory に追加
                    conversationHistory!.Add(new Dictionary<string, object>() { { "role", "assistant" }, { "content", chatTextRes } });

                    // ビューモデルを更新
                    VMLocator.ChatViewModel.ConversationHistory = conversationHistory;

                    // usageを計算
                    chatTextRes += $"usage={{\"prompt_tokens\":{inputConversationTokenCount},\"completion_tokens\":{responseTokenCount},\"total_tokens\":{inputConversationTokenCount + responseTokenCount}}}" + Environment.NewLine;

                    // 要約が実行された場合、メソッドの戻り値の最後に要約前のトークン数と要約後のトークン数をメッセージとして付け加える
                    string? postConversation = conversationHistory.Select(d => d["content"].ToString()).Aggregate((a, b) => a + b);
                    if (chatParameters.PreSummarizedHistoryTokenCount > tokenizer.Encode(postConversation!).Count)
                    {
                        chatTextRes += $"-Conversation history has been summarized. before: {chatParameters.PreSummarizedHistoryTokenCount}, after: {tokenizer.Encode(postConversation!).Count}.{Environment.NewLine}";
                    }
                    else if (chatParameters.IsDeleteHistory) // 会話履歴が全て削除された場合
                    {
                        chatTextRes += $"-Conversation history has been removed. before: {chatParameters.PreSummarizedHistoryTokenCount}, after: {tokenizer.Encode(postConversation!).Count}.{Environment.NewLine}";
                    }

                    await VMLocator.ChatViewModel.UpdateUIWithReceivedMessage("[DONE]", chatTextRes.Trim()); // Stream終了処理

                }
                else
                {
                    string errorBody = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Error: Response status code does not indicate success: {response.StatusCode} ({response.ReasonPhrase}). Response body: {errorBody}");
                }
            }
            return chatTextRes;
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
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", AppSettings.Instance.ApiKey);
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

                var response = await httpClient.PostAsync(AppSettings.Instance.ApiUrl, content);

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
        public async Task<string> GetTitleAsync(List<Dictionary<string, object>> conversationHistory)
        {
            string title;

            string forTitleMes = conversationHistory.Select(message => message["content"].ToString()).Reverse().Aggregate((a, b) => a + b)!;

            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", AppSettings.Instance.ApiKey);
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

                var response = await httpClient.PostAsync(AppSettings.Instance.ApiUrl, content);

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
