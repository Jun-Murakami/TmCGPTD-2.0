using CsvHelper;
using CsvHelper.Configuration;
using FluentAvalonia.UI.Controls;
using System;
using System.Text.Json;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.Entity.Core.Common.CommandTrees.ExpressionBuilder;
using System.Data.SQLite;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TmCGPTD.ViewModels;
using TmCGPTD.Views;
using Avalonia;
using System.Reflection;
using Avalonia.Controls;
using System.Diagnostics;
using System.Text.RegularExpressions;
using static TmCGPTD.Models.PostageSqlModels;
using static Postgrest.Constants;

namespace TmCGPTD.Models
{
    public class DatabaseProcess
    {
        private static DatabaseProcess? _instance;
        public static DatabaseProcess Instance
        {
            get
            {
                return _instance ??= new DatabaseProcess();
            }
        }

        readonly SyncProcess _syncProcess = new();
        private static string? Uid => SupabaseStates.Instance.Supabase?.Auth.CurrentSession?.User?.Id;
        public static SQLiteConnection? memoryConnection; // メモリ上のSQLコネクション

        public void SetLogDatabase()
        {
            SQLiteConnection.Changed += (sender, eventArgs) =>
            {
                Debug.WriteLine($"{eventArgs.EventType}: {eventArgs.Text}");
            };
        }

        // SQL db初期化--------------------------------------------------------------
        public static void CreateDatabase()
        {
            using var connection = new SQLiteConnection($"Data Source={AppSettings.Instance.DbPath};Version=3;");
            string sql = "CREATE TABLE phrase (id INTEGER PRIMARY KEY AUTOINCREMENT, name TEXT NOT NULL DEFAULT '', phrase TEXT NOT NULL DEFAULT '', date DATE);";

            using var command = new SQLiteCommand(sql, connection);
            // phraseテーブル作成
            connection.Open();
            command.ExecuteNonQuery();

            // phraseインデックス作成
            sql = "CREATE INDEX idx_text ON phrase (phrase);";
            command.CommandText = sql;
            command.ExecuteNonQuery();

            // chatlogテーブル作成
            sql = "CREATE TABLE chatlog (id INTEGER PRIMARY KEY AUTOINCREMENT, date DATE, title TEXT NOT NULL DEFAULT '', json TEXT NOT NULL DEFAULT '', text TEXT NOT NULL DEFAULT '', category TEXT NOT NULL DEFAULT '', lastprompt TEXT NOT NULL DEFAULT '', jsonprev TEXT NOT NULL DEFAULT '');";
            command.CommandText = sql;
            command.ExecuteNonQuery();

            // chatlogインデックス作成
            sql = "CREATE INDEX idx_chat_text ON chatlog (text);";
            command.CommandText = sql;
            command.ExecuteNonQuery();

            // editorlogテーブル作成
            sql = "CREATE TABLE editorlog (id INTEGER PRIMARY KEY AUTOINCREMENT, date DATE, text TEXT NOT NULL DEFAULT '');";
            command.CommandText = sql;
            command.ExecuteNonQuery();

            // editorlogインデックス作成
            sql = "CREATE INDEX idx_editor_text ON editorlog (text);";
            command.CommandText = sql;
            command.ExecuteNonQuery();

            // templateテーブル作成
            sql = "CREATE TABLE template (id INTEGER PRIMARY KEY AUTOINCREMENT, title TEXT NOT NULL DEFAULT '', text TEXT NOT NULL DEFAULT '', date DATE);";
            command.CommandText = sql;
            command.ExecuteNonQuery();

            // templateインデックス作成
            sql = "CREATE INDEX idx_template_text ON template (text);";
            command.CommandText = sql;
            command.ExecuteNonQuery();

            // managementテーブル作成
            sql = "CREATE TABLE management (id INTEGER PRIMARY KEY AUTOINCREMENT, user_id TEXT NOT NULL DEFAULT '', delete_table TEXT NOT NULL DEFAULT '', delete_id INTEGER, date DATE);";
            command.CommandText = sql;
            command.ExecuteNonQuery();
        }

        // データベースのチャットログをバージョンアップ--------------------------------------------------------------
        public async Task UpdateChatLogDatabaseAsync()
        {
            try
            {
                // SQLiteデータベースに接続
                using SQLiteConnection connection = new($"Data Source={AppSettings.Instance.DbPath};Version=3;");
                await connection.OpenAsync();

                bool categoryExists = false;
                bool lastPromptExists = false;
                bool jsonPrevExists = false;

                bool phraseDateExists = false;
                bool templateDateExists = false;

                object? result;

                using (var command = new SQLiteCommand(connection))
                {
                    // Check column
                    command.CommandText = "PRAGMA table_info(chatlog)";
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (reader.Read())
                        {
                            var columnName = reader["name"].ToString();
                            if (columnName == "category")
                            {
                                categoryExists = true;
                            }
                            else if (columnName == "lastprompt")
                            {
                                lastPromptExists = true;
                            }
                            else if (columnName == "jsonprev")
                            {
                                jsonPrevExists = true;
                            }
                        }
                    }

                    command.CommandText = "PRAGMA table_info(phrase)";
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (reader.Read())
                        {
                            var columnName = reader["name"].ToString();
                            if (columnName == "date")
                            {
                                phraseDateExists = true;
                            }
                        }
                    }

                    command.CommandText = "PRAGMA table_info(template)";
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (reader.Read())
                        {
                            var columnName = reader["name"].ToString();
                            if (columnName == "date")
                            {
                                templateDateExists = true;
                            }
                        }
                    }

                    // Check table
                    command.CommandText = $"SELECT name FROM sqlite_master WHERE type='table' AND name='management';";
                    // テーブルが存在しない場合、ExecuteScalar() は null を返す
                    result = await command.ExecuteScalarAsync();

                    // Backup database
                    if (!categoryExists || !lastPromptExists || !jsonPrevExists || !phraseDateExists || !templateDateExists || result == null)
                    {
                        string sourceFile = AppSettings.Instance.DbPath;
                        string backupFile = AppSettings.Instance.DbPath + ".backupV2.5";

                        // Ensure the target does not exist.
                        if (File.Exists(backupFile))
                        {
                            File.Delete(backupFile);
                        }

                        // Copy the file.
                        File.Copy(sourceFile, backupFile);
                    }

                    if (result == null)
                    {
                        // managementテーブル作成
                        command.CommandText = "CREATE TABLE management (id INTEGER PRIMARY KEY AUTOINCREMENT, user_id TEXT NOT NULL DEFAULT '', delete_table TEXT NOT NULL DEFAULT '', delete_id INTEGER, date DATE);";
                        await command.ExecuteNonQueryAsync();
                    }

                    // Add 'category' column
                    if (!categoryExists)
                    {
                        command.CommandText = "ALTER TABLE chatlog ADD COLUMN category TEXT NOT NULL DEFAULT ''";
                        await command.ExecuteNonQueryAsync();
                    }

                    // Add 'lastprompt' column
                    if (!lastPromptExists)
                    {
                        command.CommandText = "ALTER TABLE chatlog ADD COLUMN lastprompt TEXT NOT NULL DEFAULT ''";
                        await command.ExecuteNonQueryAsync();
                    }

                    // Add 'jsonprev' column
                    if (!jsonPrevExists)
                    {
                        command.CommandText = "ALTER TABLE chatlog ADD COLUMN jsonprev TEXT NOT NULL DEFAULT ''";
                        await command.ExecuteNonQueryAsync();
                    }

                    // Add 'date' column
                    if (!phraseDateExists)
                    {
                        command.CommandText = "ALTER TABLE phrase ADD COLUMN date DATE";
                        await command.ExecuteNonQueryAsync();
                    }

                    // Add 'date' column
                    if (!templateDateExists)
                    {
                        command.CommandText = "ALTER TABLE template ADD COLUMN date DATE";
                        await command.ExecuteNonQueryAsync();
                    }
                }

                if (!categoryExists || !lastPromptExists || !jsonPrevExists || !phraseDateExists || !templateDateExists || result == null)
                {
                    Application.Current!.TryFindResource("My.Strings.DatabaseUpdate", out object? resource1);
                    var dialog = new ContentDialog()
                    {
                        Title = $"{resource1}{Environment.NewLine}{Environment.NewLine}{AppSettings.Instance.DbPath}.backup",
                        PrimaryButtonText = "OK"
                    };
                    await VMLocator.MainViewModel.ContentDialogShowAsync(dialog);
                }
            }
            catch (Exception ex)
            {
                var dialog = new ContentDialog()
                {
                    Title = $"Error: {ex}",
                    PrimaryButtonText = "OK"
                };
                await VMLocator.MainViewModel.ContentDialogShowAsync(dialog);
                throw;
            }
            // インメモリをいったん閉じてまた開く
            await memoryConnection!.CloseAsync();
            await DbLoadToMemoryAsync();
        }

        // SQL dbファイルをメモリにロード--------------------------------------------------------------
        public async Task DbLoadToMemoryAsync()
        {
            var fileConnection = new SQLiteConnection($"Data Source={AppSettings.Instance.DbPath};Version=3;");
            fileConnection.Open();
            // メモリ上のDBファイルを作成
            memoryConnection = new SQLiteConnection("Data Source=:memory:");
            memoryConnection.Open();
            try
            {
                // SQL dbをメモリにコピー
                fileConnection.BackupDatabase(memoryConnection, "main", "main", -1, null, 0);
            }
            catch (Exception ex)
            {
                var dialog = new ContentDialog()
                {
                    Title = $"Error: {ex}",
                    PrimaryButtonText = "Ok"
                };
                await VMLocator.MainViewModel.ContentDialogShowAsync(dialog);
            }
            fileConnection.Close();
            if (AppSettings.Instance.SyncIsOn && SupabaseStates.Instance.Supabase != null && Uid != null)
            {
                await SupabaseStates.Instance.Supabase.Auth.RetrieveSessionAsync();
                await SupabaseProcess.Instance.SubscribeSyncAsync();
            }
        }

        // 定型句プリセットSave--------------------------------------------------------------
        public async Task SavePhrasesAsync(string name, string phrasesText)
        {
            DateTime now = DateTime.Now;
            now = now.AddTicks(-(now.Ticks % TimeSpan.TicksPerSecond));  // ミリ秒以下を切り捨てる
            using var connection = new SQLiteConnection($"Data Source={AppSettings.Instance.DbPath};Version=3;");
            await connection.OpenAsync();

            using var transaction = await connection.BeginTransactionAsync();
            try
            {
                if (AppSettings.Instance.SyncIsOn && SupabaseStates.Instance.Supabase != null && Uid != null)
                {
                    var result = await SupabaseStates.Instance.Supabase.From<Phrase>().Insert(new Phrase { UserId = Uid, Name = name, Content = phrasesText, Date = now });

                    string sql = $"INSERT INTO phrase (id, name, phrase, date) VALUES (@id, @name, @phrase, @date)";

                    using var command = new SQLiteCommand(sql, connection, (SQLiteTransaction)transaction);
                    command.Parameters.AddWithValue("@id", result.Models[0].Id);
                    command.Parameters.AddWithValue("@name", name);
                    command.Parameters.AddWithValue("@phrase", phrasesText);
                    command.Parameters.AddWithValue("@date", now.ToString("s"));
                    await command.ExecuteNonQueryAsync();
                }
                else
                {
                    string sql = $"INSERT INTO phrase (name, phrase, date) VALUES (@name, @phrase, @date)";

                    using var command = new SQLiteCommand(sql, connection, (SQLiteTransaction)transaction);
                    command.Parameters.AddWithValue("@name", name);
                    command.Parameters.AddWithValue("@phrase", phrasesText);
                    command.Parameters.AddWithValue("@date", now.ToString("s"));
                    await command.ExecuteNonQueryAsync();
                }
                await transaction.CommitAsync();
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
            await memoryConnection!.CloseAsync();
            await DbLoadToMemoryAsync();
        }

        // 定型句プリセット一覧Load--------------------------------------------------------------
        public async Task<List<string>> GetPhrasesAsync()
        {
            List<string> phrases = new List<string>();
            string sql = "SELECT name FROM phrase ORDER BY name COLLATE NOCASE";

            using (var command = new SQLiteCommand(sql, memoryConnection))
            {
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    phrases.Add(reader.GetString(0));
                }
            }
            return phrases;
        }

        // 定型句プリセット実体Load--------------------------------------------------------------
        public async Task<List<string>> GetPhrasePresetsAsync(string selectedPhraseItem)
        {
            List<string> phrases = new List<string>();
            string sql = "SELECT phrase FROM phrase WHERE name = @selectedPhraseItem";
            try
            {
                using var command = new SQLiteCommand(sql, memoryConnection);
                command.Parameters.AddWithValue("@selectedPhraseItem", selectedPhraseItem);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    phrases = reader.GetString(0).Split(Environment.NewLine).ToList();
                }
            }
            catch (Exception)
            {
                throw;
            }
            return phrases;
        }

        // 定型句プリセットRename--------------------------------------------------------------
        public async Task UpdatePhrasePresetNameAsync(string oldName, string newName)
        {
            using var connection = new SQLiteConnection($"Data Source={AppSettings.Instance.DbPath};Version=3;");
            await connection.OpenAsync();

            using var transaction = await connection.BeginTransactionAsync();
            try
            {
                if (AppSettings.Instance.SyncIsOn && SupabaseStates.Instance.Supabase != null && Uid != null)
                {
                    var update = await SupabaseStates.Instance.Supabase.From<Phrase>()
                                                .Where(x => x.Name == oldName)
                                                .Set(x => x.Name!, newName)
                                                .Update();
                }

                using var command = new SQLiteCommand(connection)
                {
                    CommandText = "UPDATE phrase SET name = @newName WHERE name = @oldName;"
                };

                command.Parameters.AddWithValue("@oldName", oldName);
                command.Parameters.AddWithValue("@newName", newName);

                int rowsAffected = await command.ExecuteNonQueryAsync();
                if (rowsAffected == 0)
                {
                    throw new Exception("No matching record found to update.");
                }
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw new Exception($"Updating the name from '{oldName}' to '{newName}': {ex.Message}", ex);
            }
            await memoryConnection!.CloseAsync();
            await DbLoadToMemoryAsync();
        }

        // 定型句プリセットUpdate--------------------------------------------------------------
        public async Task UpdatePhrasePresetAsync(string name, string phrasesText)
        {
            using var connection = new SQLiteConnection($"Data Source={AppSettings.Instance.DbPath};Version=3;");
            await connection.OpenAsync();

            using var transaction = await connection.BeginTransactionAsync();
            try
            {
                if (AppSettings.Instance.SyncIsOn && SupabaseStates.Instance.Supabase != null && Uid != null)
                {
                    var update = await SupabaseStates.Instance.Supabase.From<Phrase>()
                                                .Where(x => x.Name == name)
                                                .Set(x => x.Content!, phrasesText)
                                                .Update();
                }

                using var command = new SQLiteCommand(connection)
                {
                    CommandText = "UPDATE phrase SET phrase = @phrasesText WHERE name = @name;"
                };

                command.Parameters.AddWithValue("@name", name);
                command.Parameters.AddWithValue("@phrasesText", phrasesText);

                int rowsAffected = await command.ExecuteNonQueryAsync();
                if (rowsAffected == 0)
                {
                    throw new Exception("No matching record found to update.");
                }
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw new Exception($"Updating the phrase preset '{name}': {ex.Message}", ex);
            }
            await memoryConnection!.CloseAsync();
            await DbLoadToMemoryAsync();
        }

        // 定型句プリセットDelete--------------------------------------------------------------
        public async Task DeletePhrasePresetAsync(string selectedPhraseItem)
        {
            try
            {
                using var connection = new SQLiteConnection($"Data Source={AppSettings.Instance.DbPath};Version=3;");
                await connection.OpenAsync();

                using var transaction = await connection.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
                try
                {
                    DateTime date = DateTime.Now;
                    date = date.AddTicks(-(date.Ticks % TimeSpan.TicksPerSecond));
                    long targetId = 0;
                    if (AppSettings.Instance.SyncIsOn && SupabaseStates.Instance.Supabase != null && Uid != null)
                    {
                        var result = await SupabaseStates.Instance.Supabase.From<Phrase>()
                                       .Where(x => x.Name == selectedPhraseItem)
                                       .Get();
                        targetId = result.Models[0].Id;

                        await SupabaseStates.Instance.Supabase.From<Phrase>()
                                       .Where(x => x.Name == selectedPhraseItem)
                                       .Delete();
                        //削除履歴を追加
                        await SupabaseStates.Instance.Supabase.From<Management>().Insert(new Management { UserId = Uid!, DeleteTable = "phrase", DeleteId = targetId, Date = date });
                    }

                    string sql = "DELETE FROM phrase WHERE name = @selectedPhraseItem";
                    using var command = new SQLiteCommand(sql, connection, (SQLiteTransaction)transaction);
                    command.Parameters.AddWithValue("@selectedPhraseItem", selectedPhraseItem);
                    await command.ExecuteNonQueryAsync();

                    if (targetId > 0)
                    {
                        //削除履歴を追加
                        sql = "INSERT INTO management (user_id, delete_table, delete_id, date) VALUES (@Uid, @DeleteTable, @DeleteId, @Date)";
                        using var command2 = new SQLiteCommand(sql, connection, (SQLiteTransaction)transaction);
                        command2.Parameters.AddWithValue("@Uid", Uid);
                        command2.Parameters.AddWithValue("@DeleteTable", "phrase");
                        command2.Parameters.AddWithValue("@DeleteId", targetId);
                        command2.Parameters.AddWithValue("@Date", date.ToString("s"));
                        await command2.ExecuteNonQueryAsync();
                    }

                    await transaction.CommitAsync();
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    throw new Exception("Occurred while deleting the selected preset.", ex);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Occurred while connecting to the database.", ex);
            }
            await memoryConnection!.CloseAsync();
            await DbLoadToMemoryAsync();
        }

        // 定型句プリセットImport--------------------------------------------------------------
        public async Task<ObservableCollection<string>> ImportPhrasesFromTxtAsync(string selectedFilePath)
        {
            ObservableCollection<string> phrases = new ObservableCollection<string>();
            try
            {
                // Check if the file exists
                if (!File.Exists(selectedFilePath))
                {
                    throw new FileNotFoundException("The specified file does not exist.", selectedFilePath);
                }

                // Read the file asynchronously
                using StreamReader reader = new(selectedFilePath);
                for (int lineCount = 0; lineCount < 20; lineCount++)
                {
                    if (reader.EndOfStream)
                    {
                        phrases.Add(""); // Add an empty string if there are less than 20 lines
                    }
                    else
                    {
                        var line = await reader.ReadLineAsync();
                        if (line != null)
                        {
                            phrases.Add(line);
                        }
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
            return phrases;
        }

        // CSVインポート--------------------------------------------------------------
        public async Task<string> ImportCsvToTableAsync(string fileName, string tableName = "chatlog")
        {
            string msg;
            int processedCount = 0;
            int columnEnd;
            string columnNames;
            if (tableName == "editorlog")
            {
                columnEnd = 2;
                columnNames = "date, text";
            }
            else
            {
                columnEnd = 6;
                columnNames = "date, title, json, text, category, lastprompt, jsonprev";
            }

            try
            {
                // CSVファイルからデータを読み込む
                using var reader = new StreamReader(fileName, System.Text.Encoding.UTF8);
                var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HasHeaderRecord = true,
                    Delimiter = ","
                };
                using var csvReader = new CsvReader(reader, config);
                csvReader.Read(); // ヘッダー行をスキップ

                using var con = new SQLiteConnection($"Data Source={AppSettings.Instance.DbPath};Version=3;");
                await con.OpenAsync();
                using (var transaction = await con.BeginTransactionAsync())
                {
                    try
                    {
                        while (await csvReader.ReadAsync()) // データ行を読み込む
                        {
                            string insertQuery;
                            // データを取得
                            var rowData = new List<string>();
                            for (int i = 1, loopTo = columnEnd; i <= loopTo; i++)
                                rowData.Add(csvReader.GetField(i)!);

                            if (AppSettings.Instance.SyncIsOn && SupabaseStates.Instance.Supabase != null && Uid != null)
                            {
                                if (tableName == "editorlog")
                                {
                                    bool success = DateTime.TryParse(rowData[0], out DateTime date);
                                    if (success)
                                    {
                                        date = date.AddTicks(-(date.Ticks % TimeSpan.TicksPerSecond));  // ミリ秒以下を切り捨てる
                                        var resultEditor = await SupabaseStates.Instance.Supabase.From<EditorLog>()
                                             .Insert(new EditorLog { UserId = Uid, Date = date, Content = rowData[1] });

                                        long editorId = resultEditor.Models[0].Id;

                                        // INSERT文を作成
                                        string values = string.Join(", ", Enumerable.Range(0, rowData.Count).Select(i => $"@value{i}"));

                                        insertQuery = $"INSERT INTO {tableName} (id, {columnNames}) VALUES ({editorId}, {values});";
                                    }
                                    else
                                    {
                                        throw new Exception("The date format is invalid.");
                                    }
                                }
                                else
                                {
                                    bool success = DateTime.TryParse(rowData[0], out DateTime date);
                                    if (success)
                                    {
                                        date = date.AddTicks(-(date.Ticks % TimeSpan.TicksPerSecond));
                                        var resultChatRoom = await SupabaseStates.Instance.Supabase.From<ChatRoom>()
                                                                        .Insert(new ChatRoom { UserId = Uid, UpdatedOn = date, Title = rowData[1], Json = rowData[2], Category = rowData[4], LastPrompt = rowData[5], JsonPrev = rowData[6] });
                                        long chatRoomId = resultChatRoom.Models[0].Id;

                                        var models = new List<Message>();

                                        models.AddRange(_syncProcess.DivideMessage(rowData[3], chatRoomId, Uid));

                                        await SupabaseStates.Instance.Supabase.From<Message>().Upsert(models);

                                        // INSERT文を作成
                                        string values = string.Join(", ", Enumerable.Range(0, rowData.Count).Select(i => $"@value{i}"));

                                        insertQuery = $"INSERT INTO {tableName} (id, {columnNames}) VALUES ({chatRoomId}, {values});";
                                    }
                                    else
                                    {
                                        throw new Exception("The date format is invalid.");
                                    }
                                }
                            }
                            else
                            {
                                // INSERT文を作成
                                string values = string.Join(", ", Enumerable.Range(0, rowData.Count).Select(i => $"@value{i}"));

                                insertQuery = $"INSERT INTO {tableName} ({columnNames}) VALUES ({values});";
                            }

                            // データをデータベースに挿入
                            using (var command = new SQLiteCommand(insertQuery, con, (SQLiteTransaction)transaction))
                            {
                                for (int i = 0, loopTo1 = rowData.Count - 1; i <= loopTo1; i++)
                                    command.Parameters.AddWithValue($"@value{i}", rowData[i]);
                                await command.ExecuteNonQueryAsync();
                            }
                            processedCount += 1;
                        }
                        await transaction.CommitAsync();

                        msg = $"Successfully imported log. ({processedCount} Records)";
                    }
                    catch (Exception)
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                }
                await con.CloseAsync();
            }
            catch (Exception)
            {
                throw;
            }
            await memoryConnection!.CloseAsync();
            await DbLoadToMemoryAsync();
            return msg;
        }

        // CSVエクスポート--------------------------------------------------------------
        public async Task<string> ExportTableToCsvAsync(string fileName, string tableName = "chatlog")
        {
            try
            {
                int processedCount = 0;

                // SELECT クエリを実行し、テーブルのデータを取得
                var command = new SQLiteCommand($"SELECT * FROM {tableName};", memoryConnection);
                using (SQLiteDataReader reader = (SQLiteDataReader)await command.ExecuteReaderAsync())
                {
                    // CSV ファイルに書き込むための StreamWriter を作成
                    using var writer = new StreamWriter(fileName, false, System.Text.Encoding.UTF8);

                    // CsvWriter を作成し、設定を適用
                    var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                    {
                        HasHeaderRecord = true,
                        Delimiter = ","
                    };
                    using var csvWriter = new CsvWriter(writer, config);

                    var commandRowCount = new SQLiteCommand($"SELECT COUNT(*) FROM {tableName};", memoryConnection);
                    int rowCount = Convert.ToInt32(commandRowCount.ExecuteScalar());

                    // ヘッダー行を書き込む
                    for (int i = 0, loopTo = reader.FieldCount - 1; i <= loopTo; i++)
                        csvWriter.WriteField(reader.GetName(i));
                    csvWriter.NextRecord();

                    // データ行を書き込む

                    while (await reader.ReadAsync())
                    {
                        for (int i = 0, loopTo1 = reader.FieldCount - 1; i <= loopTo1; i++)
                        {
                            if (reader.GetFieldType(i) == typeof(DateTime))
                            {
                                var dateValue = reader.GetDateTime(i);
                                csvWriter.WriteField(dateValue.ToString("yyyy-MM-dd HH:mm:ss"));
                            }
                            else
                            {
                                csvWriter.WriteField(reader.GetValue(i));
                            }
                        }
                        csvWriter.NextRecord();
                        // Report progress
                        processedCount += 1;
                        int progressPercentage = (int)Math.Round(processedCount / (double)rowCount * 100d);
                    }
                }
                return $"Successfully exported log. ({processedCount} Records)";
            }
            catch (Exception)
            {
                throw;
            }
        }

        // データベースからチャットログを検索--------------------------------------------------------------
        public async Task<ObservableCollection<ChatList>> SearchChatDatabaseAsync(string searchKey = "")
        {
            string query;
            if (string.IsNullOrEmpty(searchKey))
            {
                query = "SELECT id, date, title, category FROM chatlog ORDER BY date DESC;";
            }
            else
            {
                query = "SELECT id, date, title, category FROM chatlog WHERE LOWER(text) LIKE LOWER(@searchKey) ORDER BY date DESC;";
            }

            var chatList = new ObservableCollection<ChatList>();
            using (var cmd = new SQLiteCommand(query, memoryConnection))
            {
                if (!string.IsNullOrEmpty(searchKey))
                {
                    searchKey = "%" + searchKey.ToLower() + "%";
                    cmd.Parameters.AddWithValue("@searchKey", searchKey);
                }
                using SQLiteDataReader reader = (SQLiteDataReader)await cmd.ExecuteReaderAsync();
                bool isFirstLine = true;
                while (await reader.ReadAsync())
                {
                    var chatItem = new ChatList();
                    if (isFirstLine)
                    {
                        chatItem.Id = reader.GetInt32(0);
                        isFirstLine = false;
                    }
                    else
                    {
                        chatItem.Id = reader.GetInt32(0);
                    }
                    chatItem.Date = reader.GetDateTime(1);
                    chatItem.Title = reader.GetString(2);
                    chatItem.Category = reader.GetString(3);
                    chatList.Add(chatItem);
                }
            }
            return chatList;
        }

        // データベースから表示用チャットログを取得--------------------------------------------------------------
        public async Task<List<string>> GetChatLogDatabaseAsync(long chatId)
        {
            string query = $"SELECT title, json, text, category, lastprompt, jsonprev FROM chatlog WHERE id = {chatId}";
            var result = new List<string>();
            using (var cmd = new SQLiteCommand(query, memoryConnection))
            {
                using SQLiteDataReader reader = (SQLiteDataReader)await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    result.Add(reader.GetString(0));
                    result.Add(reader.GetString(1));
                    result.Add(reader.GetString(2));
                    result.Add(reader.GetString(3));
                    result.Add(reader.GetString(4));
                    result.Add(reader.GetString(5));
                }
            }
            return result;
        }

        // チャットログ削除--------------------------------------------------------------
        public async Task DeleteChatLogDatabaseAsync(long chatId)
        {
            using (var connection = new SQLiteConnection($"Data Source={AppSettings.Instance.DbPath};Version=3;"))
            {
                connection.Open();
                using var transaction = await connection.BeginTransactionAsync();
                try
                {
                    DateTime date = DateTime.Now;
                    date = date.AddTicks(-(date.Ticks % TimeSpan.TicksPerSecond));
                    if (AppSettings.Instance.SyncIsOn && SupabaseStates.Instance.Supabase != null && Uid != null)
                    {
                        await SupabaseStates.Instance.Supabase.From<ChatRoom>()
                                       .Where(x => x.Id == chatId)
                                       .Delete();

                        //削除履歴を追加
                        await SupabaseStates.Instance.Supabase.From<Management>().Insert(new Management { UserId = Uid!, DeleteTable = "chatlog", DeleteId = chatId, Date = date });
                    }

                    using (var command = new SQLiteCommand("DELETE FROM chatlog WHERE id = @id", connection, (SQLiteTransaction)transaction))
                    {
                        command.Parameters.AddWithValue("@id", chatId);
                        await command.ExecuteNonQueryAsync();
                    }

                    //削除履歴を追加
                    using (var command = new SQLiteCommand("INSERT INTO management (user_id, delete_table, delete_id, date) VALUES (@user_id, @delete_table, @delete_id, @date)", connection, (SQLiteTransaction)transaction))
                    {
                        command.Parameters.AddWithValue("@user_id", Uid);
                        command.Parameters.AddWithValue("@delete_table", "chatlog");
                        command.Parameters.AddWithValue("@delete_id", chatId);
                        command.Parameters.AddWithValue("@date", date.ToString("s"));
                        await command.ExecuteNonQueryAsync();
                    }

                    await transaction.CommitAsync();
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            // インメモリをいったん閉じてまた開く
            await memoryConnection!.CloseAsync();
            await DbLoadToMemoryAsync();
            return;
        }

        // タイトルの更新--------------------------------------------------------------
        public async Task UpdateTitleDatabaseAsync(long chatId, string title)
        {
            try
            {
                if (AppSettings.Instance.SyncIsOn && SupabaseStates.Instance.Supabase != null && Uid != null)
                {
                    var update = await SupabaseStates.Instance.Supabase.From<ChatRoom>()
                                                .Where(x => x.Id == chatId)
                                                .Set(x => x.Title!, title)
                                                .Update();
                }

                using var connection = new SQLiteConnection($"Data Source={AppSettings.Instance.DbPath};Version=3;");
                await connection.OpenAsync();

                string query = "UPDATE chatlog SET title=@title WHERE id = @id";
                using var command = new SQLiteCommand(query, connection);
                command.Parameters.AddWithValue("@title", title);
                command.Parameters.AddWithValue("@id", chatId);

                await command.ExecuteNonQueryAsync();
            }
            catch (Exception)
            {
                throw;
            }
            // インメモリをいったん閉じてまた開く
            await memoryConnection!.CloseAsync();
            await DbLoadToMemoryAsync();
            VMLocator.DataGridViewModel.ChatList = await SearchChatDatabaseAsync();
        }

        // カテゴリの更新--------------------------------------------------------------
        public async Task UpdateCategoryDatabaseAsync(long chatId, string category)
        {
            try
            {
                if (AppSettings.Instance.SyncIsOn && SupabaseStates.Instance.Supabase != null && Uid != null)
                {
                    var update = await SupabaseStates.Instance.Supabase.From<ChatRoom>()
                                                .Where(x => x.Id == chatId)
                                                .Set(x => x.Category!, category)
                                                .Update();
                }

                using var connection = new SQLiteConnection($"Data Source={AppSettings.Instance.DbPath};Version=3;");
                await connection.OpenAsync();

                string query = "UPDATE chatlog SET category=@category WHERE id = @id";
                using var command = new SQLiteCommand(query, connection);
                command.Parameters.AddWithValue("@category", category);
                command.Parameters.AddWithValue("@id", chatId);

                await command.ExecuteNonQueryAsync();
            }
            catch (Exception)
            {
                throw;
            }
            // インメモリをいったん閉じてまた開く
            await memoryConnection!.CloseAsync();
            await DbLoadToMemoryAsync();
            VMLocator.DataGridViewModel.ChatList = await SearchChatDatabaseAsync();
        }

        // Webチャットログのインポート--------------------------------------------------------------
        public async Task<string> InsertWebChatLogDatabaseAsync(string webChatTitle, List<Dictionary<string, object>> webConversationHistory, string webLog, string chatService)
        {
            if (string.IsNullOrEmpty(webLog))
            {
                return "No chat log found.";
            }

            int? matchingId = null;
            string query = "";

            using (var command = new SQLiteCommand(memoryConnection))
            {
                command.CommandText = "SELECT id FROM chatlog WHERE title = @webChatTitle LIMIT 1";
                command.Parameters.AddWithValue("@webChatTitle", webChatTitle);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    matchingId = reader.GetInt32(0);
                }
            }

            DateTime date = DateTime.Now;
            date = date.AddTicks(-(date.Ticks % TimeSpan.TicksPerSecond));

            string jsonConversationHistory = JsonSerializer.Serialize(webConversationHistory);

            if (matchingId.HasValue)
            {
                Application.Current!.TryFindResource("My.Strings.OverWriteLog", out object? resource1);
                var dialog = new ContentDialog() { Title = $"{resource1}{Environment.NewLine}{Environment.NewLine}'{webChatTitle}'", PrimaryButtonText = "Overwrite", SecondaryButtonText = "Rename", CloseButtonText = "Cancel" };
                var dialogResult = await VMLocator.MainViewModel.ContentDialogShowAsync(dialog);
                if (dialogResult == ContentDialogResult.Primary)
                {
                    if (AppSettings.Instance.SyncIsOn && SupabaseStates.Instance.Supabase != null && Uid != null)
                    {
                        var resultUpdate = await SupabaseStates.Instance.Supabase.From<ChatRoom>()
                                                    .Where(x => x.Id == matchingId.Value)
                                                    .Set(x => x.UpdatedOn!, date)
                                                    .Set(x => x.Title!, webChatTitle)
                                                    .Set(x => x.Category!, chatService)
                                                    .Update();
                        long chatRoomId = resultUpdate.Models[0].Id;
                        var models = new List<Message>();

                        models.AddRange(_syncProcess.DivideMessage(webLog, chatRoomId, Uid));

                        await SupabaseStates.Instance.Supabase.From<Message>().Insert(models);
                    }
                    query = $"UPDATE chatlog SET date=@date, title=@title, json=@json, text=@text, category=category WHERE id={matchingId.Value}";
                }
                else if (dialogResult == ContentDialogResult.Secondary)
                {
                    dialog = new ContentDialog()
                    {
                        Title = "Please enter a new chat name.",
                        PrimaryButtonText = "OK",
                        CloseButtonText = "Cancel"
                    };

                    var viewModel = new PhrasePresetsNameInputViewModel(dialog);
                    dialog.Content = new PhrasePresetsNameInput()
                    {
                        DataContext = viewModel
                    };
                    dialogResult = await VMLocator.MainViewModel.ContentDialogShowAsync(dialog);
                    if (dialogResult != ContentDialogResult.Primary || string.IsNullOrWhiteSpace(viewModel.UserInput))
                    {
                        return "Cancel";
                    }
                    else
                    {
                        webChatTitle = viewModel.UserInput;
                        var msg = await InsertWebChatLogDatabaseAsync(webChatTitle, webConversationHistory, webLog, chatService);
                        if (msg == "Cancel")
                        {
                            return "Cancel";
                        }
                        return "OK";
                    }
                }
                else
                {
                    return "Cancel";
                }
            }
            else
            {
                if (AppSettings.Instance.SyncIsOn && SupabaseStates.Instance.Supabase != null && Uid != null)
                {
                    var resultInsert = await SupabaseStates.Instance.Supabase.From<ChatRoom>().Insert(new ChatRoom { UserId = Uid, UpdatedOn = date, Title = webChatTitle, Category = chatService, LastPrompt = "", Json = jsonConversationHistory, JsonPrev = "" });

                    long chatRoomId = resultInsert.Models[0].Id;
                    var models = new List<Message>();

                    models.AddRange(_syncProcess.DivideMessage(webLog, chatRoomId, Uid));

                    await SupabaseStates.Instance.Supabase.From<Message>().Insert(models);

                    query = $"INSERT INTO chatlog(id, date, title, json, text, category) VALUES ({chatRoomId}, @date, @title, @json, @text, @category)";
                }
                else
                {
                    query = "INSERT INTO chatlog(date, title, json, text, category) VALUES (@date, @title, @json, @text, @category)";
                }
            }

            using var connection = new SQLiteConnection($"Data Source={AppSettings.Instance.DbPath};Version=3;");
            await connection.OpenAsync();
            // トランザクションを開始する
            using var transaction = await connection.BeginTransactionAsync();
            try
            {
                // logテーブルにデータをインサートする
                using (var command = new SQLiteCommand(query, connection, (SQLiteTransaction)transaction))
                {
                    command.Parameters.AddWithValue("@date", date.ToString("s"));
                    command.Parameters.AddWithValue("@title", webChatTitle);
                    command.Parameters.AddWithValue("@json", jsonConversationHistory);
                    command.Parameters.AddWithValue("@text", webLog);
                    command.Parameters.AddWithValue("@category", chatService);
                    await command.ExecuteNonQueryAsync();
                }

                // トランザクションをコミットする
                await transaction.CommitAsync();
            }
            catch (Exception)
            {
                // エラーが発生した場合、トランザクションをロールバックする
                await transaction.RollbackAsync();
                throw;
            }
            // インメモリをいったん閉じてまた開く
            await memoryConnection!.CloseAsync();
            await DbLoadToMemoryAsync();
            VMLocator.DataGridViewModel.ChatList = await SearchChatDatabaseAsync();
            return "OK";
        }

        // データベースにTemplateをインサートする--------------------------------------------------------------
        public async Task InsertTemplateDatabasetAsync(string title)
        {
            DateTime date = DateTime.Now;
            date = date.AddTicks(-(date.Ticks % TimeSpan.TicksPerSecond));

            var _editorViewModel = VMLocator.EditorViewModel;

            List<string> inputText = new()
            {
                string.Join(Environment.NewLine, _editorViewModel.Editor1Text),
                string.Join(Environment.NewLine, _editorViewModel.Editor2Text),
                string.Join(Environment.NewLine, _editorViewModel.Editor3Text),
                string.Join(Environment.NewLine, _editorViewModel.Editor4Text),
                string.Join(Environment.NewLine, _editorViewModel.Editor5Text)
            };
            string finalText = string.Join(Environment.NewLine + "<---TMCGPT--->" + Environment.NewLine, inputText);

            using var connection = new SQLiteConnection($"Data Source={AppSettings.Instance.DbPath};Version=3;");
            await connection.OpenAsync();

            using var transaction = await connection.BeginTransactionAsync();
            try
            {
                if (AppSettings.Instance.SyncIsOn && SupabaseStates.Instance.Supabase != null && Uid != null)
                {
                    var result = await SupabaseStates.Instance.Supabase.From<Template>().Insert(new Template { UserId = Uid, Title = title, Content = finalText, Date = date });

                    long templateId = result.Models[0].Id;

                    using (var command = new SQLiteCommand("INSERT INTO template(id, title, text, date) VALUES (@id, @title, @text, @date)", connection, (SQLiteTransaction)transaction))
                    {
                        command.Parameters.AddWithValue("@id", templateId);
                        command.Parameters.AddWithValue("@title", title);
                        command.Parameters.AddWithValue("@text", finalText);
                        command.Parameters.AddWithValue("@date", date.ToString("s"));
                        await command.ExecuteNonQueryAsync();
                    }
                }
                else
                {
                    using (var command = new SQLiteCommand("INSERT INTO template(title, text, date) VALUES (@title, @text, @date)", connection, (SQLiteTransaction)transaction))
                    {
                        command.Parameters.AddWithValue("@title", title);
                        command.Parameters.AddWithValue("@text", finalText);
                        command.Parameters.AddWithValue("@date", date.ToString("s"));
                        await command.ExecuteNonQueryAsync();
                    }
                }
                await transaction.CommitAsync();
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
            await memoryConnection!.CloseAsync();
            await DbLoadToMemoryAsync();
        }

        // Template Update--------------------------------------------------------------
        public async Task UpdateTemplateAsync(string title)
        {
            using var connection = new SQLiteConnection($"Data Source={AppSettings.Instance.DbPath};Version=3;");
            await connection.OpenAsync();

            using var transaction = await connection.BeginTransactionAsync();
            try
            {
                var _editorViewModel = VMLocator.EditorViewModel;

                List<string> inputText = new()
                {
                    string.Join(Environment.NewLine, _editorViewModel.Editor1Text),
                    string.Join(Environment.NewLine, _editorViewModel.Editor2Text),
                    string.Join(Environment.NewLine, _editorViewModel.Editor3Text),
                    string.Join(Environment.NewLine, _editorViewModel.Editor4Text),
                    string.Join(Environment.NewLine, _editorViewModel.Editor5Text)
                };
                string finalText = string.Join(Environment.NewLine + "<---TMCGPT--->" + Environment.NewLine, inputText);

                if (AppSettings.Instance.SyncIsOn && SupabaseStates.Instance.Supabase != null && Uid != null)
                {
                    var update = await SupabaseStates.Instance.Supabase.From<Template>()
                                                .Where(x => x.Title == title)
                                                .Set(x => x.Content!, finalText)
                                                .Update();
                }

                using var command = new SQLiteCommand(connection)
                {
                    CommandText = "UPDATE template SET text = @templateText WHERE title = @title;"
                };

                command.Parameters.AddWithValue("@templateText", finalText);
                command.Parameters.AddWithValue("@title", title);

                int rowsAffected = await command.ExecuteNonQueryAsync();
                if (rowsAffected == 0)
                {
                    throw new Exception("No matching record found to update.");
                }
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw new Exception($"Updating template preset '{title}': {ex.Message}", ex);
            }
            await memoryConnection!.CloseAsync();
            await DbLoadToMemoryAsync();
        }

        // Template Rename--------------------------------------------------------------
        public async Task UpdateTemplateNameAsync(string oldName, string newName)
        {
            using var connection = new SQLiteConnection($"Data Source={AppSettings.Instance.DbPath};Version=3;");
            await connection.OpenAsync();

            using var transaction = await connection.BeginTransactionAsync();
            try
            {
                if (AppSettings.Instance.SyncIsOn && SupabaseStates.Instance.Supabase != null && Uid != null)
                {
                    var update = await SupabaseStates.Instance.Supabase.From<Template>()
                                                .Where(x => x.Title == oldName)
                                                .Set(x => x.Title!, newName)
                                                .Update();
                }

                using var command = new SQLiteCommand(connection)
                {
                    CommandText = "UPDATE template SET title = @newName WHERE title = @oldName;"
                };

                command.Parameters.AddWithValue("@oldName", oldName);
                command.Parameters.AddWithValue("@newName", newName);

                int rowsAffected = await command.ExecuteNonQueryAsync();
                if (rowsAffected == 0)
                {
                    throw new Exception("No matching record found to update.");
                }
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw new Exception($"Updating the name from '{oldName}' to '{newName}': {ex.Message}", ex);
            }
            await memoryConnection!.CloseAsync();
            await DbLoadToMemoryAsync();
        }

        // Template Delete--------------------------------------------------------------
        public async Task DeleteTemplateAsync(string selectedTemplateItem)
        {
            try
            {
                using var connection = new SQLiteConnection($"Data Source={AppSettings.Instance.DbPath};Version=3;");
                await connection.OpenAsync();

                using var transaction = await connection.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
                try
                {
                    DateTime date = DateTime.Now;
                    date = date.AddTicks(-(date.Ticks % TimeSpan.TicksPerSecond));
                    long targetId = 0;
                    if (AppSettings.Instance.SyncIsOn && SupabaseStates.Instance.Supabase != null && Uid != null)
                    {
                        var result = await SupabaseStates.Instance.Supabase.From<Template>()
                                                    .Where(x => x.Title == selectedTemplateItem)
                                                    .Get();
                        await SupabaseStates.Instance.Supabase.From<Template>()
                                       .Where(x => x.Title == selectedTemplateItem)
                                       .Delete();
                        //削除履歴を追加
                        targetId = result.Models[0].Id;
                        await SupabaseStates.Instance.Supabase.From<Management>().Insert(new Management { UserId = Uid!, DeleteTable = "template", DeleteId = targetId, Date = date });
                    }

                    string sql = "DELETE FROM template WHERE title = @selectedTemplateItem";
                    using var command = new SQLiteCommand(sql, connection, (SQLiteTransaction)transaction);
                    command.Parameters.AddWithValue("@selectedTemplateItem", selectedTemplateItem);
                    await command.ExecuteNonQueryAsync();

                    if (targetId > 0)
                    {
                        //削除履歴を追加
                        sql = "INSERT INTO management (user_id, delete_table, delete_id, date) VALUES (@Uid, @DeleteTable, @DeleteId, @Date)";
                        using var command2 = new SQLiteCommand(sql, connection, (SQLiteTransaction)transaction);
                        command2.Parameters.AddWithValue("@Uid", Uid);
                        command2.Parameters.AddWithValue("@DeleteTable", "template");
                        command2.Parameters.AddWithValue("@DeleteId", targetId);
                        command2.Parameters.AddWithValue("@Date", date.ToString("s"));
                        await command2.ExecuteNonQueryAsync();
                    }
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    throw new Exception("Occurred while deleting the selected template.", ex);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Occurred while connecting to the database.", ex);
            }
            await memoryConnection!.CloseAsync();
            await DbLoadToMemoryAsync();
        }

        // Template Import--------------------------------------------------------------
        public async Task<string> ImportTemplateFromTxtAsync(string selectedFilePath)
        {
            string text = "";

            try
            {
                // Check if the file exists
                if (!File.Exists(selectedFilePath))
                {
                    throw new FileNotFoundException("The specified file does not exist.", selectedFilePath);
                }

                // Read the file asynchronously
                using StreamReader reader = new(selectedFilePath);
                int lineCount = 0;
                while (lineCount > -1)
                {
                    if (reader.EndOfStream)
                    {
                        break;
                    }
                    else
                    {
                        text = text + await reader.ReadLineAsync();
                    }
                    lineCount++;
                }
                // textカラムの値を取得して、区切り文字で分割する
                string[] texts;
                texts = text.Split(new[] { "<---TMCGPT--->" }, StringSplitOptions.None);
                for (int i = 0, loopTo = Math.Min(texts.Length - 1, 4); i <= loopTo; i++) // 5要素目までを取得
                {
                    string propertyName = $"Editor{i + 1}Text";
                    PropertyInfo property = VMLocator.EditorViewModel.GetType().GetProperty(propertyName)!;
                    if (property != null)
                    {
                        property.SetValue(VMLocator.EditorViewModel, string.Empty);
                        if (!string.IsNullOrWhiteSpace(texts[i]))
                        {
                            property.SetValue(VMLocator.EditorViewModel, texts[i].Trim()); // 空白を削除して反映
                        }
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
            return text;
        }

        // データベースからTemplateログリストを取得--------------------------------------------------------------
        public async Task GetTemplateItemsAsync()
        {
            try
            {
                string query = $"SELECT id, title FROM template ORDER BY title ASC";

                using var cmd = new SQLiteCommand(query, memoryConnection);

                using SQLiteDataReader reader = (SQLiteDataReader)await cmd.ExecuteReaderAsync();

                var dropList = new ObservableCollection<PromptTemplate>();

                while (await reader.ReadAsync())
                {
                    long id = reader.GetInt64(0);
                    string text = reader.GetString(1).Replace("\r\n", " ").Replace("\r", " ").Replace("\n", " ").Replace("\t", " ");

                    text = text.Replace("<---TMCGPT--->", "-");
                    text = text.Length > 50 ? text.Substring(0, 50) + "..." : text;
                    var item = new PromptTemplate { Id = id, Title = text };
                    dropList.Add(item);
                }

                VMLocator.EditorViewModel.TemplateItems = dropList;
            }
            catch (Exception)
            {
                throw;
            }
        }

        // データベースからEditorログを取得して表示--------------------------------------------------------------
        public void ShowTemplateAsync(long id)
        {
            try
            {
                string query = $"SELECT text FROM template WHERE id = {id}";
                using var command = new SQLiteCommand(query, memoryConnection);
                using var reader = command.ExecuteReader();
                if (reader.Read())
                {
                    // textカラムの値を取得して、区切り文字で分割する
                    string[] texts;
                    string text = reader.GetString(0);
                    texts = text.Split(new[] { "<---TMCGPT--->" }, StringSplitOptions.None);
                    for (int i = 0, loopTo = Math.Min(texts.Length - 1, 4); i <= loopTo; i++) // 5要素目までを取得
                    {
                        string propertyName = $"Editor{i + 1}Text";
                        PropertyInfo property = VMLocator.EditorViewModel.GetType().GetProperty(propertyName)!;
                        if (property != null)
                        {
                            property.SetValue(VMLocator.EditorViewModel, string.Empty);
                            if (!string.IsNullOrWhiteSpace(texts[i]))
                            {
                                property.SetValue(VMLocator.EditorViewModel, texts[i].Trim()); // 空白を削除して反映
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                var dialog = new ContentDialog() { Title = "Error : " + ex.Message, PrimaryButtonText = "OK" };
                _ = VMLocator.MainViewModel.ContentDialogShowAsync(dialog);
            }
        }

        // データベースにEditorlogをインサートする--------------------------------------------------------------
        // 同期チェックは省略する
        public async Task InserEditorLogDatabasetAsync()
        {
            var _editorViewModel = VMLocator.EditorViewModel;

            List<string> inputText = new()
            {
                string.Join(Environment.NewLine, _editorViewModel.Editor1Text),
                string.Join(Environment.NewLine, _editorViewModel.Editor2Text),
                string.Join(Environment.NewLine, _editorViewModel.Editor3Text),
                string.Join(Environment.NewLine, _editorViewModel.Editor4Text),
                string.Join(Environment.NewLine, _editorViewModel.Editor5Text)
            };
            string finalText = string.Join(Environment.NewLine + "<---TMCGPT--->" + Environment.NewLine, inputText);

            DateTime date = DateTime.Now;
            date = date.AddTicks(-(date.Ticks % TimeSpan.TicksPerSecond));

            using var connection = new SQLiteConnection($"Data Source={AppSettings.Instance.DbPath};Version=3;");
            await connection.OpenAsync();

            using var transaction = await connection.BeginTransactionAsync();
            try
            {
                if (AppSettings.Instance.SyncIsOn && SupabaseStates.Instance.Supabase != null && Uid != null)
                {
                    var result = await SupabaseStates.Instance.Supabase.From<EditorLog>().Insert(new EditorLog { UserId = Uid, Date = date, Content = finalText });

                    long resultId = result.Models[0].Id;

                    using var command = new SQLiteCommand("INSERT INTO editorlog(id, date, text) VALUES (@id, @date, @text)", connection, (SQLiteTransaction)transaction);
                    command.Parameters.AddWithValue("@id", resultId);
                    command.Parameters.AddWithValue("@date", date.ToString("s"));
                    command.Parameters.AddWithValue("@text", finalText);
                    await command.ExecuteNonQueryAsync();

                    using var command2 = new SQLiteCommand("INSERT INTO editorlog(id, date, text) VALUES (@id, @date, @text)", memoryConnection);
                    command2.Parameters.AddWithValue("@id", resultId);
                    command2.Parameters.AddWithValue("@date", date.ToString("s"));
                    command2.Parameters.AddWithValue("@text", finalText);
                    await command2.ExecuteNonQueryAsync();
                }
                else
                {
                    using var command = new SQLiteCommand("INSERT INTO editorlog(date, text) VALUES (@date, @text)", connection, (SQLiteTransaction)transaction);
                    command.Parameters.AddWithValue("@date", date.ToString("s"));
                    command.Parameters.AddWithValue("@text", finalText);
                    await command.ExecuteNonQueryAsync();

                    using var command2 = new SQLiteCommand("INSERT INTO editorlog(date, text) VALUES (@date, @text)", memoryConnection);
                    command2.Parameters.AddWithValue("@date", date.ToString("s"));
                    command2.Parameters.AddWithValue("@text", finalText);
                    await command2.ExecuteNonQueryAsync();
                }

                await transaction.CommitAsync();
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
            // インメモリをいったん閉じてまた開く
            //await memoryConnection!.CloseAsync();
            //await DbLoadToMemoryAsync();
        }

        // データベースからEditorログリストを取得--------------------------------------------------------------
        public async Task GetEditorLogDatabaseAsync()
        {
            try
            {
                string query = $"SELECT id, text FROM editorlog ORDER BY date DESC LIMIT 200";

                using var cmd = new SQLiteCommand(query, memoryConnection);

                using SQLiteDataReader reader = (SQLiteDataReader)await cmd.ExecuteReaderAsync();

                var dropList = new ObservableCollection<EditorLogs>();

                while (await reader.ReadAsync())
                {
                    long id = reader.GetInt64(0);
                    string text = reader.GetString(1).Replace("\r\n", " ").Replace("\r", " ").Replace("\n", " ").Replace("\t", " ");

                    text = text.Replace("<---TMCGPT--->", "-");
                    text = text.Length > 50 ? text.Substring(0, 50) + "..." : text;
                    var item = new EditorLogs { Id = id, Title = text };
                    dropList.Add(item);
                }

                VMLocator.EditorViewModel.EditorLogLists = dropList;
            }
            catch (Exception)
            {
                throw;
            }
        }

        // データベースからEditorログを取得して表示--------------------------------------------------------------
        public void ShowEditorLogDatabaseAsync(long id)
        {
            try
            {
                string query = $"SELECT text FROM editorlog WHERE id = {id}";
                using var command = new SQLiteCommand(query, memoryConnection);
                using var reader = command.ExecuteReader();
                if (reader.Read())
                {
                    // textカラムの値を取得して、区切り文字で分割する
                    string text = reader.GetString(0);
                    string[] texts = text.Split(new[] { "<---TMCGPT--->" }, StringSplitOptions.None);
                    for (int i = 0, loopTo = Math.Min(texts.Length - 1, 4); i <= loopTo; i++) // 5要素目までを取得
                    {
                        string propertyName = $"Editor{i + 1}Text";
                        PropertyInfo property = VMLocator.EditorViewModel.GetType().GetProperty(propertyName)!;
                        if (property != null)
                        {
                            property.SetValue(VMLocator.EditorViewModel, string.Empty);
                            if (!string.IsNullOrWhiteSpace(texts[i]))
                            {
                                property.SetValue(VMLocator.EditorViewModel, texts[i].Trim()); // 空白を削除して反映
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                var dialog = new ContentDialog() { Title = "Error : " + ex.Message, PrimaryButtonText = "OK" };
                _ = VMLocator.MainViewModel.ContentDialogShowAsync(dialog);
            }
        }

        // データベースのEditorログをクリンナップ--------------------------------------------------------------
        public async Task CleanUpEditorLogDatabaseAsync()
        {
            if (AppSettings.Instance.SyncIsOn && SupabaseStates.Instance.Supabase != null && Uid != null)
            {
                var result = await SupabaseStates.Instance.Supabase
                        .From<EditorLog>()
                        .Select(x => new object[] { x.Id })
                        .Order(x => x.Date, Ordering.Ascending)
                        .Get();

                var DeleteList = new List<long>();

                for (int i = 0; i < result.Models.Count - 200; i++)
                {
                    DeleteList.Add(result.Models[i].Id);
                }

                foreach (var id in DeleteList)
                {
                    await SupabaseStates.Instance.Supabase
                        .From<EditorLog>()
                        .Where(x => x.Id == id)
                        .Delete();
                }
            }

            using SQLiteConnection connection = new SQLiteConnection($"Data Source={AppSettings.Instance.DbPath};Version=3;");
            await connection.OpenAsync();

            using (SQLiteCommand command = new SQLiteCommand("SELECT COUNT(*) FROM editorlog", connection))
            {
                var rowCount = (long)command.ExecuteScalar();

                if (rowCount > 200)
                {
                    // 日付が新しいもの200を残して削除
                    using SQLiteCommand deleteCommand = new SQLiteCommand(@"DELETE FROM editorlog WHERE rowid NOT IN ( SELECT rowid FROM editorlog ORDER BY date DESC LIMIT 200 )", connection);
                    await deleteCommand.ExecuteNonQueryAsync();
                }
            }
            await connection.CloseAsync();
        }

        // チャットログを更新--------------------------------------------------------------
        public async Task InsertDatabaseChatAsync(DateTime postDate, string postText, DateTime resDate, string resText)
        {
            postDate = postDate.AddTicks(-(postDate.Ticks % TimeSpan.TicksPerSecond));
            resDate = resDate.AddTicks(-(resDate.Ticks % TimeSpan.TicksPerSecond));

            var insertText = new List<string>();

            if (!string.IsNullOrWhiteSpace(resText))
            {
                insertText = new List<string>
                {
                    $"[{postDate}] by You" + Environment.NewLine,
                    postText + Environment.NewLine,
                    "(!--editable--)" + Environment.NewLine,
                    $"[{resDate}] by AI",
                    resText
                };
            }
            else
            {
                // AIの返答が空の場合(システムメッセージのみ)
                insertText = new List<string>
                {
                    $"[{postDate}] by You" + Environment.NewLine,
                    postText +
                    "---" + Environment.NewLine,
                    "(!--editable--)" + Environment.NewLine,
                };
            }

            var _editorViewModel = VMLocator.EditorViewModel;
            List<string> inputText = new()
            {
                string.Join(Environment.NewLine, _editorViewModel.Editor1Text),
                string.Join(Environment.NewLine, _editorViewModel.Editor2Text),
                string.Join(Environment.NewLine, _editorViewModel.Editor3Text),
                string.Join(Environment.NewLine, _editorViewModel.Editor4Text),
                string.Join(Environment.NewLine, _editorViewModel.Editor5Text)
            };
            string promptTextForSave = string.Join(Environment.NewLine + "<---TMCGPT--->" + Environment.NewLine, inputText);

            long lastRowId = VMLocator.ChatViewModel.LastId;
            string titleText = VMLocator.ChatViewModel.ChatTitle!;
            if (string.IsNullOrWhiteSpace(titleText))
            {
                titleText = "";
            }

            string categoryText = VMLocator.ChatViewModel.ChatCategory!;
            if (string.IsNullOrWhiteSpace(categoryText))
            {
                categoryText = "";
            }

            string jsonConversationHistory = JsonSerializer.Serialize(VMLocator.ChatViewModel.ConversationHistory);
            string jsonLastConversationHistory = JsonSerializer.Serialize(VMLocator.ChatViewModel.LastConversationHistory);
            if (string.IsNullOrWhiteSpace(jsonLastConversationHistory))
            {
                jsonLastConversationHistory = "";
            }

            using (var connection = new SQLiteConnection($"Data Source={AppSettings.Instance.DbPath};Version=3;"))
            {
                connection.Open();
                // トランザクションを開始する
                using var transaction = await connection.BeginTransactionAsync();
                try
                {
                    if (lastRowId != -1)
                    {
                        // 指定されたIDのデータを取得する
                        string currentText = "";
                        using (var command = new SQLiteCommand("SELECT text FROM chatlog WHERE id=@id", connection))
                        {
                            command.Parameters.AddWithValue("@id", lastRowId);
                            using SQLiteDataReader reader = (SQLiteDataReader)await command.ExecuteReaderAsync();
                            if (reader.Read())
                            {
                                currentText = reader.GetString(0);
                            }
                        }

                        currentText = Regex.Replace(currentText, @"\r\n|\r|\n", Environment.NewLine).Trim() + Environment.NewLine + Environment.NewLine; ;

                        string searchText = $"(!--editable--){Environment.NewLine}";
                        string byYouText = "] by You";

                        if (VMLocator.ChatViewModel.ReEditIsOn)
                        {
                            // 既存のテキストに(!--editable--)を見つけたら、直前の[*] by Youから最後までを削除する
                            if (currentText.Contains(searchText))
                            {
                                int editableIndex = currentText.IndexOf(searchText);
                                string textBeforeEditable = currentText.Substring(0, editableIndex);
                                int lastByYouIndex = textBeforeEditable.LastIndexOf(byYouText);
                                if (lastByYouIndex >= 0)
                                {
                                    int lastNewLineIndex = textBeforeEditable.LastIndexOf(Environment.NewLine, lastByYouIndex);
                                    if (lastNewLineIndex >= 0)
                                    {
                                        currentText = textBeforeEditable.Substring(0, lastNewLineIndex).Trim();
                                    }
                                    else
                                    {
                                        // lastByYouIndex以前に改行が存在しない場合は初回メッセージと判断
                                        currentText = "";
                                    }
                                }
                                else
                                {
                                    // [*] by Youが存在しない場合の処理 なにかがおかしい
                                    throw new Exception("Error : Incorrect log data. [*] by You ");
                                }
                            }
                        }
                        else
                        {
                            // 既存のテキストの(!--editable--)を削除する
                            if (currentText.Contains(searchText))
                            {
                                currentText = currentText.Replace(searchText, "");
                            }
                        }

                        // 既存のテキストに新しいメッセージを追加する
                        string newText = (currentText + Environment.NewLine + string.Join(Environment.NewLine, insertText)).Trim() + Environment.NewLine + Environment.NewLine;

                        if (AppSettings.Instance.SyncIsOn && SupabaseStates.Instance.Supabase != null && Uid != null)
                        {
                            var update = await SupabaseStates.Instance.Supabase.From<ChatRoom>()
                                                        .Where(x => x.Id == lastRowId)
                                                        .Set(x => x.UpdatedOn!, resDate)
                                                        .Set(x => x.Title!, titleText)
                                                        .Set(x => x.Category!, categoryText)
                                                        .Set(x => x.LastPrompt!, promptTextForSave)
                                                        .Set(x => x.Json!, jsonConversationHistory)
                                                        .Set(x => x.JsonPrev!, jsonLastConversationHistory)
                                                        .Update();

                            await SupabaseStates.Instance.Supabase.From<Message>() // 既存のデータを一旦削除する
                                               .Where(x => x.RoomId == lastRowId)
                                               .Delete();

                            var models = new List<Message>();

                            models.AddRange(_syncProcess.DivideMessage(newText.Trim(), lastRowId, Uid));

                            await SupabaseStates.Instance.Supabase.From<Message>().Insert(models); // 新しいデータを挿入する
                        }

                        // 指定されたIDに対してデータを更新する
                        using (var command = new SQLiteCommand("UPDATE chatlog SET date=@date, title=@title, json=@json, text=@text, category=@category, lastprompt=@lastprompt, jsonprev=@jsonprev WHERE id=@id", connection, (SQLiteTransaction)transaction))
                        {
                            command.Parameters.AddWithValue("@date", resDate.ToString("s"));
                            command.Parameters.AddWithValue("@title", titleText);
                            command.Parameters.AddWithValue("@json", jsonConversationHistory);
                            command.Parameters.AddWithValue("@text", newText);
                            command.Parameters.AddWithValue("@category", categoryText);
                            command.Parameters.AddWithValue("@lastprompt", promptTextForSave);
                            command.Parameters.AddWithValue("@jsonprev", jsonLastConversationHistory);
                            command.Parameters.AddWithValue("@id", lastRowId);
                            await command.ExecuteNonQueryAsync();
                        }
                    }
                    else
                    {
                        if (AppSettings.Instance.SyncIsOn && SupabaseStates.Instance.Supabase != null && Uid != null)
                        {
                            var result = await SupabaseStates.Instance.Supabase.From<ChatRoom>().Insert(new ChatRoom { UserId = Uid, UpdatedOn = resDate, Title = titleText, Category = categoryText, LastPrompt = promptTextForSave, Json = jsonConversationHistory, JsonPrev = jsonLastConversationHistory });

                            long chatRoomId = result.Models[0].Id;

                            var models = new List<Message>();

                            models.AddRange(_syncProcess.DivideMessage(string.Join(Environment.NewLine, insertText), chatRoomId, Uid));

                            await SupabaseStates.Instance.Supabase.From<Message>().Insert(models);

                            // logテーブルにデータをインサートする
                            using (var command = new SQLiteCommand("INSERT INTO chatlog(id, date, title, json, text, category, lastprompt, jsonprev) VALUES (@id, @date, @title, @json, @text, @category, @lastprompt, @jsonprev)", connection, (SQLiteTransaction)transaction))
                            {
                                command.Parameters.AddWithValue("@id", chatRoomId);
                                command.Parameters.AddWithValue("@date", resDate.ToString("s"));
                                command.Parameters.AddWithValue("@title", titleText);
                                command.Parameters.AddWithValue("@json", jsonConversationHistory);
                                command.Parameters.AddWithValue("@text", string.Join(Environment.NewLine, insertText));
                                command.Parameters.AddWithValue("@category", categoryText);
                                command.Parameters.AddWithValue("@lastprompt", promptTextForSave);
                                command.Parameters.AddWithValue("@jsonprev", jsonLastConversationHistory);
                                await command.ExecuteNonQueryAsync();
                            }
                        }
                        else
                        {
                            // logテーブルにデータをインサートする
                            using (var command = new SQLiteCommand("INSERT INTO chatlog(date, title, json, text, category, lastprompt, jsonprev) VALUES (@date, @title, @json, @text, @category, @lastprompt, @jsonprev)", connection, (SQLiteTransaction)transaction))
                            {
                                command.Parameters.AddWithValue("@date", resDate.ToString("s"));
                                command.Parameters.AddWithValue("@title", titleText);
                                command.Parameters.AddWithValue("@json", jsonConversationHistory);
                                command.Parameters.AddWithValue("@text", string.Join(Environment.NewLine, insertText));
                                command.Parameters.AddWithValue("@category", categoryText);
                                command.Parameters.AddWithValue("@lastprompt", promptTextForSave);
                                command.Parameters.AddWithValue("@jsonprev", jsonLastConversationHistory);
                                await command.ExecuteNonQueryAsync();
                            }
                        }

                        // 更新中チャットのIDを取得
                        string sqlLastRowId = "SELECT last_insert_rowid();";
                        using (var command = new SQLiteCommand(sqlLastRowId, connection, (SQLiteTransaction)transaction))
                        {
                            long insertedId = Convert.ToInt64(command.ExecuteScalar());
                            if (insertedId != VMLocator.ChatViewModel.LastId)
                            {
                                VMLocator.ChatViewModel.LastId = insertedId;
                            }
                        }
                    }
                    // トランザクションをコミットする
                    await transaction.CommitAsync();

                    // 成功したら各種変数を更新する
                    VMLocator.ChatViewModel.LastPrompt = promptTextForSave;
                    VMLocator.ChatViewModel.ReEditIsOn = false;
                }
                catch (Exception)
                {
                    // エラーが発生した場合、トランザクションをロールバックする
                    await transaction.RollbackAsync();
                    //var dialog = new ContentDialog() { Title = "Error : " + ex.Message, PrimaryButtonText = "OK" };
                    //await VMLocator.MainViewModel.ContentDialogShowAsync(dialog);
                    throw;
                }
            }
            // インメモリをいったん閉じてまた開く
            await memoryConnection!.CloseAsync();
            await DbLoadToMemoryAsync();
        }

        // データベースをチェック--------------------------------------------------------------
        public async Task<bool> CheckTableExists(string selectedFilePath)
        {
            // テーブル名のリスト
            string[] tableNames = { "phrase", "chatlog", "editorlog", "template" };

            try
            {
                // データベースに接続
                using (var connection = new SQLiteConnection($"Data Source={selectedFilePath};Version=3;"))
                {
                    connection.Open();

                    foreach (var tableName in tableNames)
                    {
                        // テーブルが存在するかどうかをチェック
                        string commandText = $"SELECT name FROM sqlite_master WHERE type='table' AND name='{tableName}';";
                        using (var command = new SQLiteCommand(commandText, connection))
                        {
                            // テーブルが存在しない場合、ExecuteScalar() は null を返す
                            var result = await command.ExecuteScalarAsync();
                            if (result == null)
                            {
                                // テーブルが存在しないため、false を返す
                                return false;
                            }
                        }
                    }

                    // インメモリをいったん閉じる
                    await memoryConnection!.CloseAsync();

                    // すべてのテーブルが存在するため、true を返す
                    return true;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
