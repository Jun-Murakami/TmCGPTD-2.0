using System;
using System.IO;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static TmCGPTD.Models.PostageSqlProcess;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Reactive.Joins;
using static Postgrest.Constants;
using Postgrest;
using static Postgrest.QueryOptions;

namespace TmCGPTD.Models
{
    public class SQLiteProcess
    {
        private readonly PostageSqlProcess _postageSqlProcess;
        public static SQLiteConnection memoryConnection; // メモリ上のSQLコネクション
        public async Task CopyLocalToCloudDb()
        {

            string sourceFile = AppSettings.Instance.DbPath;
            string backupFile = AppSettings.Instance.DbPath + ".backupLocal";

            // Ensure the target does not exist.
            if (File.Exists(backupFile))
            {
                File.Delete(backupFile);
            }

            // Copy the file.
            File.Copy(sourceFile, backupFile);

            using SQLiteConnection connection = new SQLiteConnection($"Data Source={AppSettings.Instance.DbPath}");
            await connection.OpenAsync();

            try
            { 
                if (VMLocator.MainViewModel._supabase == null)
                {
                    throw new Exception($"Supabase initialization failed.");
                }

                if (VMLocator.MainViewModel._supabase.Auth.CurrentUser == null)
                {
                    throw new Exception($"Failed to obtain Supabase user ID.");
                }

                
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}");
            }

            var supabase = VMLocator.MainViewModel._supabase;
            var uid = VMLocator.MainViewModel._supabase.Auth.CurrentUser.Id;

            try
            {
                var models = new List<Phrase>();

                string sql = "SELECT * FROM phrase ORDER BY name COLLATE NOCASE";
                using var command = new SQLiteCommand(sql, connection);

                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    if (reader["date"] == DBNull.Value)
                    {
                        models.Add(new Phrase { UserId = uid, Name = (string)reader["name"], Content = (string)reader["phrase"], Date = DateTime.Now });
                    }
                    else
                    {
                        models.Add(new Phrase { UserId = uid, Name = (string)reader["name"], Content = (string)reader["phrase"], Date = (DateTime)reader["date"] });
                    }
                }

                //await supabase.From<Phrase>().Insert(models);

            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to synchronize phrase presets.: {ex.Message}");
            }


            try
            {
                var models = new List<EditorLog>();

                string sql = "SELECT * FROM editorlog ORDER BY date DESC LIMIT 200";
                using var command = new SQLiteCommand(sql, connection);

                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    if (reader["date"] == DBNull.Value)
                    {
                        models.Add(new EditorLog { UserId = uid, Content = (string)reader["text"], Date = DateTime.Now });
                    }
                    else
                    {
                        models.Add(new EditorLog { UserId = uid, Content = (string)reader["text"], Date = (DateTime)reader["date"] });
                    }
                }

                //await supabase.From<EditorLog>().Insert(models);

            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to synchronize editor logs.: {ex.Message}");
            }


            try
            {
                var models = new List<Template>();

                string sql = "SELECT * FROM template ORDER BY title ASC";
                using var command = new SQLiteCommand(sql, connection);

                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    if (reader["date"] == DBNull.Value)
                    {
                        models.Add(new Template { UserId = uid, Title = (string)reader["title"], Content = (string)reader["text"], Date = DateTime.Now });
                    }
                    else
                    {
                        models.Add(new Template { UserId = uid, Title = (string)reader["title"], Content = (string)reader["text"], Date = (DateTime)reader["date"] });
                    }
                }

                //await supabase.From<Template>().Insert(models);

            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to synchronize templates.: {ex.Message}");
            }


            try
            {
                var models2 = new List<Message>();

                string sql = "SELECT * FROM chatlog ORDER BY date DESC";
                using var command = new SQLiteCommand(sql, connection);

                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var models1 = new List<ChatRoom>();

                    if (reader["date"] == DBNull.Value)
                    {
                        models1.Add(new ChatRoom { UserId = uid, Title = (string)reader["title"], Category = (string)reader["category"], LastPrompt = (string)reader["lastprompt"], Json = (string)reader["json"], JsonPrev = (string)reader["jsonprev"], UpdatedOn = DateTime.Now });
                    }
                    else
                    {
                        models1.Add(new ChatRoom { UserId = uid, Title = (string)reader["title"], Category = (string)reader["category"], LastPrompt = (string)reader["lastprompt"], Json = (string)reader["json"], JsonPrev = (string)reader["jsonprev"], UpdatedOn = (DateTime)reader["date"] });
                    }

                    var returnValue = await supabase.From<ChatRoom>().Insert(models1, new QueryOptions { Returning = ReturnType.Representation});

                    var roomId = returnValue.Models[0].Id;

                    string normarizedContent = Regex.Replace((string)reader["text"], @"\r\n|\r|\n", Environment.NewLine);

                    var chatLogRegex = new Regex(@"^\[(.+)\] by (You|AI)", RegexOptions.Multiline);
                    var usageRegex = new Regex(@"^usage=.+$", RegexOptions.Multiline);
                    var systemOffRegex = new Regex(@$"#(\s*)(?i)system({Environment.NewLine})*?---({Environment.NewLine})*", RegexOptions.Singleline);
                    var systemMessageRegex = new Regex(@$"#(\s*)(?i)system({Environment.NewLine})*(.+?)---({Environment.NewLine})*", RegexOptions.Singleline);

                    MatchCollection matches = chatLogRegex.Matches(normarizedContent);

                    for (int i = 0; i < matches.Count; i++)
                    {
                        var match = matches[i];
                        var isUser = match.Groups[2].Value == "You";
                        var role = isUser ? "user" : "assistant";
                        DateTime timestamp;
                        if(DateTime.TryParse(match.Groups[1].Value, out DateTime result))
                        {
                            timestamp = result;
                        }
                        else
                        {
                            timestamp = DateTime.MinValue;
                        }

                        var endOfMatch = match.Index + match.Length;
                        var nextMatchIndex = i < matches.Count - 1 ? matches[i + 1].Index : normarizedContent.Length;
                        var content = normarizedContent.Substring(endOfMatch, nextMatchIndex - endOfMatch);

                        var systemOffMatch = systemOffRegex.Match(content);
                        if (systemOffMatch.Success)
                        {
                            models2.Add(new Message { UserId = uid, RoomId = roomId, CreatedOn = timestamp, Content = "System messages were turned off.", Role = "system" });
                            content = content.Substring(systemOffMatch.Index, systemOffMatch.Length).Trim('\r', '\n');
                        }

                        var systemMessageMatch = systemMessageRegex.Match(content);
                        if (systemMessageMatch.Success)
                        {
                            models2.Add(new Message { UserId = uid, RoomId = roomId, CreatedOn = timestamp, Content = systemMessageMatch.Value, Role = "system" });
                            content = content.Substring(systemMessageMatch.Index, systemMessageMatch.Length).Trim('\r', '\n');
                        }

                        var usageMatch = usageRegex.Match(content);
                        if (usageMatch.Success)
                        {
                            content = content.Substring(0, usageMatch.Index).Trim('\r', '\n');
                            var usageStr = Regex.Replace(usageMatch.Value, "usage={\"prompt_tokens\":([0-9]+),\"completion_tokens\":([0-9]+),\"total_tokens\":([0-9]+)}", "[tokens] prompt:$1, completion:$2, total:$3");
                            if (content.Length > 0)
                            {
                                models2.Add(new Message { UserId = uid, RoomId = roomId, CreatedOn = timestamp, Content = content, Role = role, Usage = usageStr });
                            }
                        }
                        else
                        {
                            content = content.Trim('\r', '\n');
                            if (content.Length > 0)
                            {
                                models2.Add(new Message { UserId = uid, RoomId = roomId, CreatedOn = timestamp, Content = content, Role = role });
                            }
                        }

                    }
                }

                
                await supabase.From<Message>().Insert(models2);


            }
            catch (Exception ex)
            {
                throw;
                //throw new Exception($"Failed to synchronize chat logs.: {ex.Message} {ex.StackTrace}");
            }
        }
    }
}
