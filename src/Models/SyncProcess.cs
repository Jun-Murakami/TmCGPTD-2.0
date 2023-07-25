using System.Diagnostics;
using System;
using System.IO;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Threading.Tasks;
using static TmCGPTD.Models.PostageSqlModels;
using System.Text.RegularExpressions;
using FluentAvalonia.UI.Controls;
using Postgrest;
using static Postgrest.QueryOptions;
using static Postgrest.Constants;
using TmCGPTD.Views;
using Avalonia.Threading;
using Avalonia;
using Avalonia.Controls;

namespace TmCGPTD.Models
{
    public class SyncProcess
    {
        private bool syncIsRunning = false;

        // -------------------------------------------------------------------------------------------------------

        public async Task SyncDbAsync()
        {
            if (syncIsRunning) return;
            syncIsRunning = true;

            if (VMLocator.ChatViewModel.ChatIsRunning)
            {
                while (VMLocator.ChatViewModel.ChatIsRunning)
                {
                    await Task.Delay(1000);
                }
            }

            string accountCheck;

            try
            {
                if (SupabaseStates.Instance.Supabase == null)
                {
                    throw new Exception("Supabase initialization failed.");
                }

                if (SupabaseStates.Instance.Supabase.Auth.CurrentUser == null)
                {
                    throw new Exception("Failed to obtain Supabase user ID.");
                }

                accountCheck = await InitializeAndCheckManagementTableAsync();
                if (accountCheck == "switch")
                {
                    Application.Current!.TryFindResource("My.Strings.NewAccountDetected", out object? resource1);
                    ContentDialog? cdialog = null;
                    if (Dispatcher.UIThread.CheckAccess())
                    {
                        cdialog = new ContentDialog
                        {
                            Title = "Confirmation",
                            Content = resource1,
                            PrimaryButtonText = "Use Current Data",
                            SecondaryButtonText = "Use Cloud Data",
                            CloseButtonText = "Log Out"
                        };
                    }
                    else
                    {
                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            cdialog = new ContentDialog
                            {
                                Title = "Confirmation",
                                Content = resource1,
                                PrimaryButtonText = "Use Current Data",
                                SecondaryButtonText = "Use Cloud Data",
                                CloseButtonText = "Log Out"
                            };
                        });
                    }

                    var result = await VMLocator.MainViewModel.ContentDialogShowAsync(cdialog!);
                    if (result == ContentDialogResult.Primary)
                    {
                        //Do nothing
                    }
                    else if (result == ContentDialogResult.Secondary)
                    {
                        BackupDb();
                        await DeleteLocalDbAsync();
                        await InitializeAndCheckManagementTableAsync();
                    }
                    else
                    {
                        await VMLocator.CloudLoggedinViewModel.LogOutAsync();
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                ContentDialog? cdialog = null;
                if (Dispatcher.UIThread.CheckAccess())
                {
                    cdialog = new ContentDialog
                    {
                        Title = "Error",
                        Content = ex.Message + ex.StackTrace,
                        CloseButtonText = "OK"
                    };
                }
                else
                {
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        cdialog = new ContentDialog
                        {
                            Title = "Error",
                            Content = ex.Message + ex.StackTrace,
                            CloseButtonText = "OK"
                        };
                    });
                }

                await VMLocator.MainViewModel.ContentDialogShowAsync(cdialog!);
                await SupabaseProcess.Instance.SubscribeSyncAsync();
                syncIsRunning = false;
                return;
            }

            var supabase = SupabaseStates.Instance.Supabase;
            var uid = SupabaseStates.Instance.Supabase!.Auth.CurrentUser!.Id;

            int cloudIsNewer = 0;
            int localIsNewer = 0;
            int localOnly = 0;
            int cloudRecords = 0;
            int localRecords = 0;

            bool isDeleted = false;

            try
            {
                await SyncManagementTableAsync(); //削除フラグマネージメントテーブルの同期

                //クラウドの削除フラグマネージメントテーブルを取得
                var resultManagement = await supabase
                    .From<Management>()
                    .Select(x => new object[] { x.Id, x.DeleteTable!, x.DeleteId!, x.Date })
                    .Order(x => x.Id, Ordering.Descending)
                    .Get();

                var resultPhrase = await supabase
                                    .From<Phrase>()
                                    .Select(x => new object[] { x.Id, x.Date })
                                    .Order(x => x.Id, Ordering.Descending)
                                    .Get();

                cloudRecords = cloudRecords + resultPhrase.Models.Count;

                List<long> phraseDeletedId = new();

                string sql = "SELECT id, date FROM phrase ORDER BY id DESC";
                using var command = new SQLiteCommand(sql, DatabaseProcess.memoryConnection);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    //resultに同一のIDがあるか検索する
                    var target = resultPhrase.Models.Find(x => x.Id == (long)reader["id"]);
                    if (target != null)
                    {
                        if (reader["date"] != DBNull.Value)
                        {
                            if (target.Date > (DateTime)reader["date"]) //クラウドが新しい
                            {
                                cloudIsNewer++;
                            }
                            else if (target.Date < (DateTime)reader["date"]) //ローカルが新しい
                            {
                                localIsNewer++;
                            }
                        }
                        else //ローカルに日付がない
                        {
                            cloudIsNewer++;
                        }
                        localRecords++;
                    }
                    else //クラウドに該当IDがない
                    {
                        //削除フラグに含まれている場合はローカルのSQLデータベースを削除する
                        if (resultManagement.Models.Exists(x => x.DeleteTable == "phrase" && x.DeleteId == (long)reader["id"]))
                        {
                            phraseDeletedId.Add((long)reader["id"]);
                        }
                        else
                        {
                            localOnly++;
                            localRecords++;
                        }
                    }
                }


                var resultTemplate = await supabase
                                        .From<Template>()
                                        .Select(x => new object[] { x.Id, x.Date })
                                        .Order(x => x.Id, Ordering.Descending)
                                        .Get();

                cloudRecords = cloudRecords + resultTemplate.Models.Count;

                List<long> templeteDeletedId = new();

                sql = "SELECT id, date FROM template ORDER BY id DESC";
                using var command2 = new SQLiteCommand(sql, DatabaseProcess.memoryConnection);
                using var reader2 = await command2.ExecuteReaderAsync();

                while (await reader2.ReadAsync())
                {
                    var target = resultTemplate.Models.Find(x => x.Id == (long)reader2["id"]);
                    if (target != null)
                    {
                        if (reader2["date"] != DBNull.Value)
                        {
                            if (target.Date > (DateTime)reader2["date"])
                            {
                                cloudIsNewer++;
                            }
                            else if (target.Date < (DateTime)reader2["date"])
                            {
                                localIsNewer++;
                            }
                        }
                        else
                        {
                            cloudIsNewer++;
                        }
                        localRecords++;
                    }
                    else
                    {
                        //削除フラグに含まれている場合はローカルのSQLデータベースを削除する
                        if (resultManagement.Models.Exists(x => x.DeleteTable == "template" && x.DeleteId == (long)reader2["id"]))
                        {
                            templeteDeletedId.Add((long)reader2["id"]);
                        }
                        else
                        {
                            localOnly++;
                            localRecords++;
                        }
                    }
                }


                var resultEditorLog = await supabase
                                        .From<EditorLog>()
                                        .Select(x => new object[] { x.Id, x.Date })
                                        .Order(x => x.Id, Ordering.Descending)
                                        .Get();

                cloudRecords = cloudRecords + resultEditorLog.Models.Count;

                List<long> editorlogDeletedId = new();

                sql = "SELECT id, date FROM editorlog ORDER BY id DESC";
                using var command3 = new SQLiteCommand(sql, DatabaseProcess.memoryConnection);
                using var reader3 = await command3.ExecuteReaderAsync();

                while (await reader3.ReadAsync())
                {
                    var target = resultEditorLog.Models.FirstOrDefault(x => x.Id == (long)reader3["id"]);
                    if (target != null)
                    {
                        if (reader3["date"] != DBNull.Value)
                        {
                            if (target.Date > (DateTime)reader3["date"])
                            {
                                cloudIsNewer++;
                            }
                            else if (target.Date < (DateTime)reader3["date"])
                            {
                                localIsNewer++;
                            }
                        }
                        else
                        {
                            cloudIsNewer++;
                        }
                        localRecords++;
                    }
                    else
                    {
                        //削除フラグに含まれている場合はローカルのSQLデータベースを削除する
                        if (resultManagement.Models.Exists(x => x.DeleteTable == "editorlog" && x.DeleteId == (long)reader3["id"]))
                        {
                            editorlogDeletedId.Add((long)reader3["id"]);
                        }
                        else
                        {
                            localOnly++;
                            localRecords++;
                        }
                    }
                }


                var resultChatRoom = await supabase
                                        .From<ChatRoom>()
                                        .Select(x => new object[] { x.Id, x.UpdatedOn })
                                        .Order(x => x.Id, Ordering.Descending)
                                        .Get();

                cloudRecords = cloudRecords + resultChatRoom.Models.Count;

                List<long> chatlogDeletedId = new();

                sql = "SELECT id, date FROM chatlog ORDER BY id DESC";
                using var command4 = new SQLiteCommand(sql, DatabaseProcess.memoryConnection);
                using var reader4 = await command4.ExecuteReaderAsync();

                while (await reader4.ReadAsync())
                {
                    var target = resultChatRoom.Models.Find(x => x.Id == (long)reader4["id"]);
                    if (target != null)
                    {
                        if (reader4["date"] != DBNull.Value)
                        {
                            if (target.UpdatedOn > (DateTime)reader4["date"])
                            {
                                cloudIsNewer++;
                            }
                            else if (target.UpdatedOn < (DateTime)reader4["date"])
                            {
                                localIsNewer++;
                            }
                        }
                        else
                        {
                            cloudIsNewer++;
                        }
                        localRecords++;
                    }
                    else
                    {
                        //削除フラグに含まれている場合はローカルのSQLデータベースを削除する
                        if (resultManagement.Models.Exists(x => x.DeleteTable == "chatlog" && x.DeleteId == (long)reader4["id"]))
                        {
                            chatlogDeletedId.Add((long)reader4["id"]);
                        }
                        else
                        {
                            localOnly++;
                            localRecords++;
                        }
                    }
                }

                using SQLiteConnection connection = new($"Data Source={AppSettings.Instance.DbPath};Version=3;");
                await connection.OpenAsync();

                //保存した削除IDを削除する
                try
                {
                    if (phraseDeletedId.Count > 0)
                    {
                        sql = $"DELETE FROM phrase WHERE id IN ({string.Join(",", phraseDeletedId)})";
                        using var commandDel = new SQLiteCommand(sql, connection);
                        await commandDel.ExecuteNonQueryAsync();
                        isDeleted = true;
                    }

                    if (templeteDeletedId.Count > 0)
                    {
                        sql = $"DELETE FROM template WHERE id IN ({string.Join(",", templeteDeletedId)})";
                        using var commandDel2 = new SQLiteCommand(sql, connection);
                        await commandDel2.ExecuteNonQueryAsync();
                        isDeleted = true;
                    }

                    if (editorlogDeletedId.Count > 0)
                    {
                        sql = $"DELETE FROM editorlog WHERE id IN ({string.Join(",", editorlogDeletedId)})";
                        using var commandDel3 = new SQLiteCommand(sql, connection);
                        await commandDel3.ExecuteNonQueryAsync();
                        isDeleted = true;
                    }

                    if (chatlogDeletedId.Count > 0)
                    {
                        sql = $"DELETE FROM chatlog WHERE id IN ({string.Join(",", chatlogDeletedId)})";
                        using var commandDel4 = new SQLiteCommand(sql, connection);
                        await commandDel4.ExecuteNonQueryAsync();
                        isDeleted = true;
                    }
                    await connection.CloseAsync();
                }
                catch (Exception ex)
                {
                    ContentDialog? cdialog = null;
                    if (Dispatcher.UIThread.CheckAccess())
                    {
                        cdialog = new ContentDialog
                        {
                            Title = "Error",
                            Content = ex.Message + ex.StackTrace,
                            CloseButtonText = "OK"
                        };
                    }
                    else
                    {
                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            cdialog = new ContentDialog
                            {
                                Title = "Error",
                                Content = ex.Message + ex.StackTrace,
                                CloseButtonText = "OK"
                            };
                        });
                    }

                    await VMLocator.MainViewModel.ContentDialogShowAsync(cdialog!);
                    await SupabaseProcess.Instance.SubscribeSyncAsync();
                    syncIsRunning = false;
                    return;
                }

                if (isDeleted)
                {
                    // インメモリをいったん閉じてまた開く
                    await DatabaseProcess.memoryConnection!.CloseAsync();
                    await DatabaseProcess.Instance.DbLoadToMemoryAsync();
                    VMLocator.DataGridViewModel.ChatList = await DatabaseProcess.Instance.SearchChatDatabaseAsync();
                    await DatabaseProcess.Instance.GetEditorLogDatabaseAsync();
                    await DatabaseProcess.Instance.GetTemplateItemsAsync();
                    string selectedPhraseItem = VMLocator.MainViewModel.SelectedPhraseItem!;
                    await VMLocator.MainViewModel.LoadPhraseItemsAsync();
                    if (Dispatcher.UIThread.CheckAccess())
                    {
                        VMLocator.DataGridViewModel.SelectedItemIndex = -1;
                        VMLocator.MainViewModel.SelectedPhraseItem = selectedPhraseItem;
                        VMLocator.EditorViewModel.SelectedEditorLogIndex = -1;
                        VMLocator.EditorViewModel.SelectedTemplateItemIndex = -1;
                    }
                    else
                    {
                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            VMLocator.DataGridViewModel.SelectedItemIndex = -1;
                            VMLocator.MainViewModel.SelectedPhraseItem = selectedPhraseItem;
                            VMLocator.EditorViewModel.SelectedEditorLogIndex = -1;
                            VMLocator.EditorViewModel.SelectedTemplateItemIndex = -1;
                        });
                    }
                }

                //検証結果判定処理
                if (localOnly > 0 && cloudIsNewer == 0 && localIsNewer == 0 && cloudRecords == 0)
                {
                    //クラウドにデータが一つも無ければ、ローカルの全データをコピーして初回同期する
                    //コピー後にローカルを全削除してクラウドからフェッチし、IDを振りなおす
                    BackupDb();
                    await CopyAllLocalToCloudDbAsync();
                    VMLocator.MainViewModel.SyncLogText = "Synced to cloud from local: " + localOnly;
                }
                else if ((cloudIsNewer > 0 && localIsNewer == 0 && localOnly == 0) || (cloudRecords > localRecords && localIsNewer == 0 && localOnly == 0))
                {
                    //クラウドのデータが新しいか、データが多ければクラウドのデータをローカルにコピー
                    await UpsertToLocalDbAsync();
                    VMLocator.MainViewModel.SyncLogText = "Synced from cloud: " + (cloudRecords - localRecords) + cloudIsNewer;
                }
                else if (localIsNewer > 0 && cloudIsNewer == 0 && localOnly == 0)
                {
                    //ローカルのデータが新しければクラウドにコピー
                    await UpsertToCloudDbAsync();
                    VMLocator.MainViewModel.SyncLogText = "Synced to cloud from local: " + localIsNewer;
                }
                else if ((localOnly == 0 && cloudIsNewer == 0 && localIsNewer == 0 && localRecords == cloudRecords) || (localRecords == 0 && cloudRecords == 0))
                {
                    //同期済みまたは初期値
                    VMLocator.MainViewModel.SyncLogText = "Sync status is up-to-date.";
                }
                else
                {
                    ContentDialog? cdialog = null;
                    if (Dispatcher.UIThread.CheckAccess())
                    {
                        if (accountCheck == "new")
                        {
                            Application.Current!.TryFindResource("My.Strings.NewAccountConflicts", out object? resource1);
                            //初回同期
                            cdialog = new ContentDialog
                            {
                                Title = "Data conflicts.",
                                Content = $"{resource1}\nCloud records: {cloudRecords}, Local records: {localRecords}\nCloud is newer: {cloudIsNewer},  Local is newer: {localIsNewer},  Local only: {localOnly}",
                                PrimaryButtonText = "Merge",
                                SecondaryButtonText = "Cloud",
                                CloseButtonText = "Local"
                            };
                        }
                        else
                        {
                            Application.Current!.TryFindResource("My.Strings.DataConflicts", out object? resource2);
                            //競合が発生
                            cdialog = new ContentDialog
                            {
                                Title = "Data conflicts.",
                                Content = $"{resource2}\nCloud records: {cloudRecords}, Local records: {localRecords}\nCloud is newer: {cloudIsNewer},  Local is newer: {localIsNewer},  Local only: {localOnly}",
                                PrimaryButtonText = "Merge",
                                SecondaryButtonText = "Cloud",
                                CloseButtonText = "Local"
                            };
                        }
                    }
                    else
                    {
                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            if (accountCheck == "new")
                            {
                                Application.Current!.TryFindResource("My.Strings.NewAccountConflicts", out object? resource1);
                                //初回同期
                                cdialog = new ContentDialog
                                {
                                    Title = "Data conflicts.",
                                    Content = $"{resource1}\nCloud records: {cloudRecords}, Local records: {localRecords}\nCloud is newer: {cloudIsNewer},  Local is newer: {localIsNewer},  Local only: {localOnly}",
                                    PrimaryButtonText = "Merge",
                                    SecondaryButtonText = "Cloud",
                                    CloseButtonText = "Local"
                                };
                            }
                            else
                            {
                                Application.Current!.TryFindResource("My.Strings.DataConflicts", out object? resource2);
                                //競合が発生
                                cdialog = new ContentDialog
                                {
                                    Title = "Data conflicts.",
                                    Content = $"{resource2}\nCloud records: {cloudRecords}, Local records: {localRecords}\nCloud is newer: {cloudIsNewer},  Local is newer: {localIsNewer},  Local only: {localOnly}",
                                    PrimaryButtonText = "Merge",
                                    SecondaryButtonText = "Cloud",
                                    CloseButtonText = "Local"
                                };
                            }
                        });
                    }


                    var result = await VMLocator.MainViewModel.ContentDialogShowAsync(cdialog!);
                    if (result == ContentDialogResult.Primary) //マージ
                    {
                        BackupDb();
                        await UpsertToCloudDbAsync(true); //マージモードではRLS違反のIDが重複する可能性があるため、クラウド側のIDを再発行する
                        await UpsertToLocalDbAsync();
                        VMLocator.MainViewModel.SyncLogText = "Database merge completed.";
                    }
                    else if (result == ContentDialogResult.Secondary) //クラウドを優先
                    {
                        BackupDb();
                        await UpsertToLocalDbAsync(); //ローカルにしかデータが無い場合は削除される
                        VMLocator.MainViewModel.SyncLogText = "Database sync completed.";
                    }
                    else if (result == ContentDialogResult.None) //ローカルを優先
                    {
                        BackupDb();
                        await DeleteCloudDbAsync();
                        await CopyAllLocalToCloudDbAsync(); //IDの競合を避けるためにクラウドを一旦削除して全同期しなおす
                        VMLocator.MainViewModel.SyncLogText = "Database sync completed.";
                    }
                }

                await SupabaseProcess.Instance.SubscribeSyncAsync();
                syncIsRunning = false;
            }
            catch (Exception ex)
            {
                ContentDialog? cdialog = null;
                if (Dispatcher.UIThread.CheckAccess())
                {
                    cdialog = new ContentDialog
                    {
                        Title = "Error",
                        Content = ex.Message + ex.StackTrace,
                        CloseButtonText = "OK"
                    };
                }
                else
                {
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        cdialog = new ContentDialog
                        {
                            Title = "Error",
                            Content = ex.Message + ex.StackTrace,
                            CloseButtonText = "OK"
                        };
                    });
                }

                await VMLocator.MainViewModel.ContentDialogShowAsync(cdialog!);
                await SupabaseProcess.Instance.SubscribeSyncAsync();
                syncIsRunning = false;
                return;
            }
        }
        // -------------------------------------------------------------------------------------------------------
        public async Task UpsertToCloudDbAsync(bool isMerge = false)
        {
            var supabase = SupabaseStates.Instance.Supabase;
            var uid = SupabaseStates.Instance.Supabase!.Auth.CurrentUser!.Id;

            using SQLiteConnection connection = new($"Data Source={AppSettings.Instance.DbPath};Version=3;");
            await connection.OpenAsync();

            try
            {
                var resultPhrase = await supabase!
                      .From<Phrase>()
                      .Order(x => x.Id, Ordering.Descending)
                      .Get();

                var models = new List<Phrase>();
                List<long> mergedIdList = new();

                const string sql = "SELECT * FROM phrase ORDER BY name COLLATE NOCASE";
                using var command2 = new SQLiteCommand(sql, connection);
                using var reader = await command2.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var target = resultPhrase.Models.Find(x => x.Id == (long)reader["id"]);

                    if (target == null || reader["date"] is DBNull || target.Date != (DateTime)reader["date"]) // クラウドにデータが無いか、ローカルと日付が異なる場合
                    {
                        DateTime date;
                        if (reader["date"] != DBNull.Value)
                        {
                            date = (DateTime)reader["date"];
                        }
                        else //Nullの場合は現在時刻を入れる
                        {
                            date = DateTime.Now;
                        }
                        date = date.AddTicks(-(date.Ticks % TimeSpan.TicksPerSecond));

                        if (isMerge) //マージの場合はIDを振り直す
                        {
                            await supabase!.From<Phrase>().Upsert(new Phrase { UserId = uid, Name = (string)reader["name"], Content = (string)reader["phrase"], Date = date });
                            mergedIdList.Add((long)reader["id"]);
                        }
                        else
                        {
                            models.Add(new Phrase { Id = (long)reader["id"], UserId = uid, Name = (string)reader["name"], Content = (string)reader["phrase"], Date = date });
                        }
                    }
                }

                if (models.Count > 0 && !isMerge) await supabase!.From<Phrase>().Upsert(models);
                if (models.Count > 0 && isMerge) await supabase!.From<Phrase>().Insert(models);

                if (isMerge && mergedIdList.Count > 0)
                {
                    //保存したローカルIDのレコードをトランザクションで削除
                    using var transaction = await connection.BeginTransactionAsync();
                    try
                    {
                        foreach (var id in mergedIdList)
                        {
                            using var command = new SQLiteCommand($"DELETE FROM phrase WHERE id = {id}", connection, (SQLiteTransaction)transaction);
                            await command.ExecuteNonQueryAsync();
                        }
                        await transaction.CommitAsync();
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        throw new Exception($"Failed to delete merged records: {ex.Message}\n{ex.StackTrace}");
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to upload phrase presets: {ex.Message}\n{ex.StackTrace}");
            }


            try
            {
                var resultEditorLog = await supabase
                        .From<EditorLog>()
                        .Order(x => x.Id, Ordering.Descending)
                        .Get();

                var models = new List<EditorLog>();
                List<long> mergedIdList = new();

                const string sql = "SELECT * FROM editorlog ORDER BY date ASC LIMIT 200";
                using var command2 = new SQLiteCommand(sql, connection);
                using var reader = await command2.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var target = resultEditorLog.Models.Find(x => x.Id == (long)reader["id"]);

                    if (target == null || reader["date"] is DBNull || target.Date != (DateTime)reader["date"]) // クラウドにデータが無いか、ローカルと日付が異なる場合
                    {
                        DateTime date;
                        if (reader["date"] != DBNull.Value)
                        {
                            date = (DateTime)reader["date"];
                        }
                        else //Nullの場合は現在時刻を入れる
                        {
                            date = DateTime.Now;
                        }
                        date = date.AddTicks(-(date.Ticks % TimeSpan.TicksPerSecond));

                        if (isMerge) //マージの場合はIDを振り直す
                        {
                            models.Add(new EditorLog { UserId = uid, Content = (string)reader["text"], Date = date });
                            mergedIdList.Add((long)reader["id"]);
                        }
                        else
                        {
                            models.Add(new EditorLog { Id = (long)reader["id"], UserId = uid, Content = (string)reader["text"], Date = date });
                        }
                    }
                }

                if (models.Count > 0 && !isMerge) await supabase.From<EditorLog>().Upsert(models);
                if (models.Count > 0 && isMerge) await supabase.From<EditorLog>().Insert(models);

                if (isMerge && mergedIdList.Count > 0)
                {
                    //保存したローカルIDのレコードをトランザクションで削除
                    using var transaction = await connection.BeginTransactionAsync();
                    try
                    {
                        foreach (var id in mergedIdList)
                        {
                            using var command = new SQLiteCommand($"DELETE FROM editorlog WHERE id = {id}", connection, (SQLiteTransaction)transaction);
                            await command.ExecuteNonQueryAsync();
                        }
                        await transaction.CommitAsync();
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        throw new Exception($"Failed to delete merged records: {ex.Message}\n{ex.StackTrace}");
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to upload editor logs: {ex.Message}\n{ex.StackTrace}");
            }


            try
            {
                var resultTemplate = await supabase
                        .From<Template>()
                        .Order(x => x.Id, Ordering.Descending)
                        .Get();

                var models = new List<Template>();
                List<long> mergedIdList = new();

                const string sql = "SELECT * FROM template ORDER BY title ASC";
                using var command2 = new SQLiteCommand(sql, connection);
                using var reader = await command2.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var target = resultTemplate.Models.Find(x => x.Id == (long)reader["id"]);

                    if (target == null || reader["date"] is DBNull || target.Date != (DateTime)reader["date"]) // クラウドにデータが無いか、ローカルと日付が異なる場合
                    {
                        DateTime date;
                        if (reader["date"] != DBNull.Value)
                        {
                            date = (DateTime)reader["date"];
                        }
                        else //Nullの場合は現在時刻を入れる
                        {
                            date = DateTime.Now;
                        }
                        date = date.AddTicks(-(date.Ticks % TimeSpan.TicksPerSecond));

                        if (isMerge) //マージの場合はIDを振り直す
                        {
                            models.Add(new Template { UserId = uid, Title = (string)reader["title"], Content = (string)reader["text"], Date = date });
                            mergedIdList.Add((long)reader["id"]);
                        }
                        else
                        {
                            models.Add(new Template { Id = (long)reader["id"], UserId = uid, Title = (string)reader["title"], Content = (string)reader["text"], Date = date });
                        }
                    }
                }

                if (models.Count > 0 && !isMerge) await supabase.From<Template>().Upsert(models);
                if (models.Count > 0 && isMerge) await supabase.From<Template>().Insert(models);

                if (isMerge && mergedIdList.Count > 0)
                {
                    //保存したローカルIDのレコードをトランザクションで削除
                    using var transaction = await connection.BeginTransactionAsync();
                    try
                    {
                        foreach (var id in mergedIdList)
                        {
                            using var command = new SQLiteCommand($"DELETE FROM template WHERE id = {id}", connection, (SQLiteTransaction)transaction);
                            await command.ExecuteNonQueryAsync();
                        }
                        await transaction.CommitAsync();
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        throw new Exception($"Failed to delete merged records: {ex.Message}\n{ex.StackTrace}");
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to upload templates: {ex.Message}\n{ex.StackTrace}");
            }


            try
            {
                var resultChatRoom = await supabase
                        .From<ChatRoom>()
                        .Order(x => x.Id, Ordering.Descending)
                        .Get();

                var models2 = new List<Message>();
                List<long> mergedIdList = new();

                const string sql = "SELECT * FROM chatlog ORDER BY date ASC";
                using var command2 = new SQLiteCommand(sql, connection);
                using var reader = await command2.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var target = resultChatRoom.Models.Find(x => x.Id == (long)reader["id"]);

                    var models1 = new List<ChatRoom>();
                    if (target == null || reader["date"] is DBNull || target.UpdatedOn != (DateTime)reader["date"]) // クラウドにデータが無いか、ローカルと日付が異なる場合
                    {
                        DateTime date;
                        if (reader["date"] != DBNull.Value)
                        {
                            date = (DateTime)reader["date"];
                        }
                        else //Nullの場合は現在時刻を入れる
                        {
                            date = DateTime.Now;
                        }
                        date = date.AddTicks(-(date.Ticks % TimeSpan.TicksPerSecond));

                        if (target != null) // クラウドにデータがある場合は一旦削除（カスケードでメッセージを一旦消す）
                        {
                            await supabase.From<ChatRoom>()
                                           .Where(x => x.Id == target.Id)
                                           .Delete();
                        }

                        if (isMerge) //マージの場合はIDを振り直す
                        {
                            models1.Add(new ChatRoom { UserId = uid, Title = (string)reader["title"], Category = (string)reader["category"], LastPrompt = (string)reader["lastprompt"], Json = (string)reader["json"], JsonPrev = (string)reader["jsonprev"], UpdatedOn = date });
                            mergedIdList.Add((long)reader["id"]);
                        }
                        else
                        {
                            models1.Add(new ChatRoom { Id = (long)reader["id"], UserId = uid, Title = (string)reader["title"], Category = (string)reader["category"], LastPrompt = (string)reader["lastprompt"], Json = (string)reader["json"], JsonPrev = (string)reader["jsonprev"], UpdatedOn = date });
                        }

                        var returnValue = await supabase.From<ChatRoom>().Insert(models1, new QueryOptions { Returning = ReturnType.Representation });

                        var roomId = returnValue.Models[0].Id;

                        models2.AddRange(DivideMessage((string)reader["text"], roomId, uid!)); // メッセージを分割して追加
                    }
                }

                if (models2.Count > 0) await supabase.From<Message>().Insert(models2);

                if (isMerge && mergedIdList.Count > 0)
                {
                    //保存したローカルIDのレコードをトランザクションで削除
                    using var transaction = await connection.BeginTransactionAsync();
                    try
                    {
                        foreach (var id in mergedIdList)
                        {
                            using var command = new SQLiteCommand($"DELETE FROM chatlog WHERE id = {id}", connection, (SQLiteTransaction)transaction);
                            await command.ExecuteNonQueryAsync();
                        }
                        await transaction.CommitAsync();
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        throw new Exception($"Failed to delete merged records: {ex.Message}\n{ex.StackTrace}");
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to upload chat logs: {ex.Message}\n{ex.StackTrace}");
            }
        }

        // -------------------------------------------------------------------------------------------------------

        public async Task UpsertToLocalDbAsync()
        {
            var supabase = SupabaseStates.Instance.Supabase;

            using SQLiteConnection connection = new($"Data Source={AppSettings.Instance.DbPath};Version=3;");
            await connection.OpenAsync();

            try
            {
                var resultPhrase = await supabase!
                        .From<Phrase>()
                        .Order(x => x.Id, Ordering.Descending)
                        .Get();

                Dictionary<long, DateTime?> localData = new();

                using var command = new SQLiteCommand("SELECT id, date FROM phrase ORDER BY id DESC", connection);
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    long id = (long)reader["id"];
                    DateTime? date = reader["date"] == DBNull.Value ? null : (DateTime)reader["date"];
                    localData[id] = date;
                }

                foreach (var cloudData in resultPhrase.Models)
                {
                    // クラウドと日付が異なるか、ローカルの日付がNull、またはローカルにデータが存在しない場合
                    if (!localData.TryGetValue(cloudData.Id, out DateTime? localDate) || localDate == null || cloudData.Date != localDate)
                    {
                        const string updateSql = @"INSERT INTO phrase (id, name, phrase, date) VALUES (@Id, @Name, @Content, @Date) 
                                            ON CONFLICT(id) DO UPDATE SET name = excluded.name, phrase = excluded.phrase, date = excluded.date;";
                        var updateCommand = new SQLiteCommand(updateSql, connection);
                        updateCommand.Parameters.AddWithValue("@Id", cloudData.Id);
                        updateCommand.Parameters.AddWithValue("@Name", cloudData.Name);
                        updateCommand.Parameters.AddWithValue("@Content", cloudData.Content);
                        updateCommand.Parameters.AddWithValue("@Date", cloudData.Date.ToString("s"));

                        await updateCommand.ExecuteNonQueryAsync();
                    }
                }
                //ローカルデータを反復して、クラウドデータに存在しない場合は削除
                var cloudIds = resultPhrase.Models.ConvertAll(x => x.Id);
                foreach (var localId in localData.Keys)
                {
                    if (!cloudIds.Contains(localId))
                    {
                        const string deleteSql = "DELETE FROM phrase WHERE id = @Id;";
                        var deleteCommand = new SQLiteCommand(deleteSql, connection);
                        deleteCommand.Parameters.AddWithValue("@Id", localId);
                        await deleteCommand.ExecuteNonQueryAsync();
                    }
                }

            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to download phrase presets: {ex.Message}\n{ex.StackTrace}");
            }

            try
            {
                var resultTemplate = await supabase
                                .From<Template>()
                                .Order(x => x.Id, Ordering.Descending)
                                .Get();

                Dictionary<long, DateTime?> localData = new();

                const string sql = "SELECT id, date FROM template ORDER BY id DESC";
                using var command2 = new SQLiteCommand(sql, connection);
                using var reader = await command2.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    long id = (long)reader["id"];
                    DateTime? date = reader["date"] == DBNull.Value ? null : (DateTime)reader["date"];
                    localData[id] = date;
                }

                foreach (var cloudData in resultTemplate.Models)
                {
                    // クラウドと日付が異なるか、ローカルの日付がNull、またはローカルにデータが存在しない場合
                    if (!localData.TryGetValue(cloudData.Id, out DateTime? localDate) || localDate == null || cloudData.Date != localDate)
                    {
                        const string updateSql = @"INSERT INTO template (id, title, text, date) VALUES (@Id, @Name, @Content, @Date) 
                                            ON CONFLICT(id) DO UPDATE SET title = excluded.title, text = excluded.text, date = excluded.date;";
                        var command = new SQLiteCommand(updateSql, connection);
                        command.Parameters.AddWithValue("@Id", cloudData.Id);
                        command.Parameters.AddWithValue("@Name", cloudData.Title);
                        command.Parameters.AddWithValue("@Content", cloudData.Content);
                        command.Parameters.AddWithValue("@Date", cloudData.Date.ToString("s"));

                        await command.ExecuteNonQueryAsync();
                    }
                }
                //ローカルデータを反復して、クラウドデータに存在しない場合は削除
                var cloudIds = resultTemplate.Models.ConvertAll(x => x.Id);
                foreach (var localId in localData.Keys)
                {
                    if (!cloudIds.Contains(localId))
                    {
                        const string deleteSql = "DELETE FROM template WHERE id = @Id;";
                        var deleteCommand = new SQLiteCommand(deleteSql, connection);
                        deleteCommand.Parameters.AddWithValue("@Id", localId);
                        await deleteCommand.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to download template presets: {ex.Message}\n{ex.StackTrace}");
            }

            try
            {
                var resultEditorLog = await supabase
                                .From<EditorLog>()
                                .Order(x => x.Id, Ordering.Descending)
                                .Get();

                Dictionary<long, DateTime?> localData = new();

                const string sql = "SELECT id, date FROM editorlog ORDER BY id DESC";
                using var command3 = new SQLiteCommand(sql, connection);
                using var reader = await command3.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    long id = (long)reader["id"];
                    DateTime? date = reader["date"] == DBNull.Value ? null : (DateTime)reader["date"];
                    localData[id] = date;
                }

                foreach (var cloudData in resultEditorLog.Models)
                {
                    // クラウドと日付が異なるか、ローカルの日付がNull、またはローカルにデータが存在しない場合
                    if (!localData.TryGetValue(cloudData.Id, out DateTime? localDate) || localDate == null || cloudData.Date != localDate)
                    {
                        const string updateSql = @"INSERT INTO editorlog (id, date, text) VALUES (@Id, @Date, @Content) 
                                            ON CONFLICT(id) DO UPDATE SET date = excluded.date, text = excluded.text;";
                        var command = new SQLiteCommand(updateSql, connection);
                        command.Parameters.AddWithValue("@Id", cloudData.Id);
                        command.Parameters.AddWithValue("@Date", cloudData.Date.ToString("s"));
                        command.Parameters.AddWithValue("@Content", cloudData.Content);

                        await command.ExecuteNonQueryAsync();
                    }
                }
                //ローカルデータを反復して、クラウドデータに存在しない場合は削除
                var cloudIds = resultEditorLog.Models.ConvertAll(x => x.Id);
                foreach (var localId in localData.Keys)
                {
                    if (!cloudIds.Contains(localId))
                    {
                        const string deleteSql = "DELETE FROM editorlog WHERE id = @Id;";
                        var deleteCommand = new SQLiteCommand(deleteSql, connection);
                        deleteCommand.Parameters.AddWithValue("@Id", localId);
                        await deleteCommand.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to download editor logs: {ex.Message}\n{ex.StackTrace}");
            }

            try
            {
                var resultChatRoom = await supabase
                                .From<ChatRoom>()
                                .Order(x => x.Id, Ordering.Descending)
                                .Get();

                Dictionary<long, DateTime?> localData = new();

                const string sql = "SELECT * FROM chatlog ORDER BY id DESC";
                using var command4 = new SQLiteCommand(sql, connection);
                using var reader = await command4.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    long id = (long)reader["id"];
                    DateTime? date = reader["date"] == DBNull.Value ? null : (DateTime)reader["date"];
                    localData[id] = date;
                }

                foreach (var cloudData in resultChatRoom.Models)
                {
                    // クラウドと日付が異なるか、ローカルの日付がNull、またはローカルにデータが存在しない場合
                    if (!localData.TryGetValue(cloudData.Id, out DateTime? localDate) || localDate == null || cloudData.UpdatedOn != localDate)
                    {
                        var resultMessage = await supabase.From<Message>().Where(x => x.RoomId == cloudData.Id).Order(x => x.Id, Ordering.Ascending).Get();

                        string combinedMessage = CombineMessage(resultMessage.Models);

                        const string updateSql = @"INSERT INTO chatlog (id, date, title, json, text, category, lastprompt, jsonprev) VALUES (@Id, @UpdatedOn, @Title, @Json, @Message, @Category, @LastPrompt, @JsonPrev) 
                            ON CONFLICT(id) DO UPDATE SET date = excluded.date, title = excluded.title, json = excluded.json, text = excluded.text, category = excluded.category, lastprompt = excluded.lastprompt, jsonprev = excluded.jsonprev;";
                        var command = new SQLiteCommand(updateSql, connection);
                        command.Parameters.AddWithValue("@Id", cloudData.Id);
                        command.Parameters.AddWithValue("@UpdatedOn", cloudData.UpdatedOn.ToString("s"));
                        command.Parameters.AddWithValue("@Title", cloudData.Title);
                        command.Parameters.AddWithValue("@Json", cloudData.Json);
                        command.Parameters.AddWithValue("@Message", combinedMessage);
                        command.Parameters.AddWithValue("@Category", cloudData.Category);
                        command.Parameters.AddWithValue("@LastPrompt", cloudData.LastPrompt);
                        command.Parameters.AddWithValue("@JsonPrev", cloudData.JsonPrev);

                        await command.ExecuteNonQueryAsync();
                    }
                }
                //ローカルデータを反復して、クラウドデータに存在しない場合は削除
                var cloudIds = resultChatRoom.Models.ConvertAll(x => x.Id);
                foreach (var localId in localData.Keys)
                {
                    if (!cloudIds.Contains(localId))
                    {
                        const string deleteSql = "DELETE FROM chatlog WHERE id = @Id;";
                        var deleteCommand = new SQLiteCommand(deleteSql, connection);
                        deleteCommand.Parameters.AddWithValue("@Id", localId);
                        await deleteCommand.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to download chat logs: {ex.Message}\n{ex.StackTrace}");
            }

            // インメモリをいったん閉じてまた開く
            await DatabaseProcess.memoryConnection!.CloseAsync();
            await DatabaseProcess.Instance.DbLoadToMemoryAsync();
            VMLocator.DataGridViewModel.ChatList = await DatabaseProcess.Instance.SearchChatDatabaseAsync();
            await DatabaseProcess.Instance.GetEditorLogDatabaseAsync();
            await DatabaseProcess.Instance.GetTemplateItemsAsync();
            string selectedPhraseItem = VMLocator.MainViewModel.SelectedPhraseItem!;
            await VMLocator.MainViewModel.LoadPhraseItemsAsync();
            if (Dispatcher.UIThread.CheckAccess())
            {
                VMLocator.DataGridViewModel.SelectedItemIndex = -1;
                VMLocator.MainViewModel.SelectedPhraseItem = selectedPhraseItem;
                VMLocator.EditorViewModel.SelectedEditorLogIndex = -1;
                VMLocator.EditorViewModel.SelectedTemplateItemIndex = -1;
            }
            else
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    VMLocator.DataGridViewModel.SelectedItemIndex = -1;
                    VMLocator.MainViewModel.SelectedPhraseItem = selectedPhraseItem;
                    VMLocator.EditorViewModel.SelectedEditorLogIndex = -1;
                    VMLocator.EditorViewModel.SelectedTemplateItemIndex = -1;
                });
            }
        }

        // -------------------------------------------------------------------------------------------------------
        public async Task CopyAllLocalToCloudDbAsync()
        {
            using SQLiteConnection connection = new($"Data Source={AppSettings.Instance.DbPath};Version=3;");
            await connection.OpenAsync();

            var supabase = SupabaseStates.Instance.Supabase;
            var uid = SupabaseStates.Instance.Supabase!.Auth.CurrentUser!.Id;

            ContentDialog? cdialog = null;
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                cdialog = new ContentDialog() { Title = "Synchronizing..." };
                cdialog.Content = new ProgressView()
                {
                    DataContext = VMLocator.ProgressViewModel
                };
                VMLocator.ProgressViewModel.SetDialog(cdialog);
                _ = VMLocator.MainViewModel.ContentDialogShowAsync(cdialog);//awaitすると進まないのでawaitしないこと
            });

            try
            {
                var models = new List<Phrase>();

                string sql = "SELECT COUNT(*) FROM phrase";
                using var command = new SQLiteCommand(sql, connection);
                long? countTable = (long?)await command.ExecuteScalarAsync();
                VMLocator.ProgressViewModel.ProgressText = "Uploading phrase presets...";

                sql = "SELECT * FROM phrase ORDER BY name COLLATE NOCASE";
                using var command2 = new SQLiteCommand(sql, connection);

                using var reader = await command2.ExecuteReaderAsync();

                int i = 0;
                while (await reader.ReadAsync())
                {
                    i++;
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        VMLocator.ProgressViewModel.ProgressValue = ((double)i / countTable);
                    });
                    if (reader["date"] == DBNull.Value)
                    {
                        DateTime date = DateTime.Now;
                        date = date.AddTicks(-(date.Ticks % TimeSpan.TicksPerSecond));
                        models.Add(new Phrase { UserId = uid, Name = (string)reader["name"], Content = (string)reader["phrase"], Date = date });
                    }
                    else
                    {
                        DateTime date = (DateTime)reader["date"];
                        date = date.AddTicks(-(date.Ticks % TimeSpan.TicksPerSecond));
                        models.Add(new Phrase { UserId = uid, Name = (string)reader["name"], Content = (string)reader["phrase"], Date = date });
                    }
                    await Task.Delay(10);
                }

                await supabase!.From<Phrase>().Insert(models);
            }
            catch (Exception ex)
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    VMLocator.ProgressViewModel.Hide();
                });
                throw new Exception($"Failed to upload phrase presets: {ex.Message}\n{ex.StackTrace}");
            }


            try
            {
                var models = new List<EditorLog>();

                string sql = "SELECT COUNT(*) FROM editorlog LIMIT 200";
                using var command = new SQLiteCommand(sql, connection);
                long? countTable = (long?)await command.ExecuteScalarAsync();
                VMLocator.ProgressViewModel.ProgressText = $"Uploading editor-logs...";

                sql = "SELECT * FROM editorlog ORDER BY date ASC LIMIT 200";
                using var command2 = new SQLiteCommand(sql, connection);

                using var reader = await command2.ExecuteReaderAsync();

                int i = 0;
                while (await reader.ReadAsync())
                {
                    i++;
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        VMLocator.ProgressViewModel.ProgressValue = (double)i / countTable;
                    });
                    if (reader["date"] == DBNull.Value)
                    {
                        DateTime date = DateTime.Now;
                        date = date.AddTicks(-(date.Ticks % TimeSpan.TicksPerSecond));
                        models.Add(new EditorLog { UserId = uid, Content = (string)reader["text"], Date = date });
                    }
                    else
                    {
                        DateTime date = (DateTime)reader["date"];
                        date = date.AddTicks(-(date.Ticks % TimeSpan.TicksPerSecond));
                        models.Add(new EditorLog { UserId = uid, Content = (string)reader["text"], Date = date });
                    }
                    await Task.Delay(10);
                }

                await supabase.From<EditorLog>().Insert(models);
            }
            catch (Exception ex)
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    VMLocator.ProgressViewModel.Hide();
                });
                throw new Exception($"Failed to upload editor logs: {ex.Message}\n{ex.StackTrace}");
            }


            try
            {
                var models = new List<Template>();

                string sql = "SELECT COUNT(*) FROM template";
                using var command = new SQLiteCommand(sql, connection);
                long? countTable = (long?)await command.ExecuteScalarAsync();
                VMLocator.ProgressViewModel.ProgressText = "Uploading templates...";

                sql = "SELECT * FROM template ORDER BY title ASC";
                using var command2 = new SQLiteCommand(sql, connection);

                using var reader = await command2.ExecuteReaderAsync();

                int i = 0;
                while (await reader.ReadAsync())
                {
                    i++;
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        VMLocator.ProgressViewModel.ProgressValue = ((double)i / countTable);
                    });
                    if (reader["date"] == DBNull.Value)
                    {
                        DateTime date = DateTime.Now;
                        date = date.AddTicks(-(date.Ticks % TimeSpan.TicksPerSecond));
                        models.Add(new Template { UserId = uid, Title = (string)reader["title"], Content = (string)reader["text"], Date = date });
                    }
                    else
                    {
                        DateTime date = (DateTime)reader["date"];
                        date = date.AddTicks(-(date.Ticks % TimeSpan.TicksPerSecond));
                        models.Add(new Template { UserId = uid, Title = (string)reader["title"], Content = (string)reader["text"], Date = date });
                    }
                    await Task.Delay(10);
                }

                await supabase.From<Template>().Insert(models);
            }
            catch (Exception ex)
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    VMLocator.ProgressViewModel.Hide();
                });
                throw new Exception($"Failed to upload templates: {ex.Message}\n{ex.StackTrace}");
            }


            try
            {
                var models2 = new List<Message>();

                string sql = "SELECT COUNT(*) FROM chatlog";
                using var command = new SQLiteCommand(sql, connection);
                long? countTable = (long?)await command.ExecuteScalarAsync();
                VMLocator.ProgressViewModel.ProgressText = "Uploading chat-logs...";

                sql = "SELECT * FROM chatlog ORDER BY date ASC";
                using var command2 = new SQLiteCommand(sql, connection);

                using var reader = await command2.ExecuteReaderAsync();

                int j = 0;
                while (await reader.ReadAsync())
                {
                    j++;
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        VMLocator.ProgressViewModel.ProgressValue = ((double)j / countTable);
                    });
                    var models1 = new List<ChatRoom>();

                    if (reader["date"] == DBNull.Value)
                    {
                        DateTime date = DateTime.Now;
                        date = date.AddTicks(-(date.Ticks % TimeSpan.TicksPerSecond));
                        models1.Add(new ChatRoom { UserId = uid, Title = (string)reader["title"], Category = (string)reader["category"], LastPrompt = (string)reader["lastprompt"], Json = (string)reader["json"], JsonPrev = (string)reader["jsonprev"], UpdatedOn = date });
                    }
                    else
                    {
                        DateTime date = (DateTime)reader["date"];
                        date = date.AddTicks(-(date.Ticks % TimeSpan.TicksPerSecond));
                        models1.Add(new ChatRoom { UserId = uid, Title = (string)reader["title"], Category = (string)reader["category"], LastPrompt = (string)reader["lastprompt"], Json = (string)reader["json"], JsonPrev = (string)reader["jsonprev"], UpdatedOn = date });
                    }

                    var returnValue = await supabase.From<ChatRoom>().Insert(models1, new QueryOptions { Returning = ReturnType.Representation });

                    var roomId = returnValue.Models[0].Id;

                    models2.AddRange(DivideMessage((string)reader["text"], roomId, uid!));

                    await Task.Delay(10);
                }

                await supabase.From<Message>().Insert(models2);
            }
            catch (Exception ex)
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    VMLocator.ProgressViewModel.Hide();
                });
                throw new Exception($"Failed to upload chat logs: {ex.Message}\n{ex.StackTrace}");
            }


            VMLocator.ProgressViewModel.ProgressText = "Deleting local logs...";
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                VMLocator.ProgressViewModel.ProgressValue = 0;
            });
            try
            {
                await DeleteLocalDbAsync();
                await InitializeAndCheckManagementTableAsync();//全削除後にマネジメントテーブル作成
            }
            catch (Exception ex)
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    VMLocator.ProgressViewModel.Hide();
                });
                throw new Exception($"Failed to delete local logs: {ex.Message}\n{ex.StackTrace}");
            }

            using SQLiteConnection connection2 = new($"Data Source={AppSettings.Instance.DbPath};Version=3;");
            await connection2.OpenAsync();

            VMLocator.ProgressViewModel.ProgressText = "Fetching data from the cloud...";

            var resultPhrase = await supabase.From<Phrase>().Get();

            try
            {
                foreach (var phrase in resultPhrase.Models)
                {
                    var command = new SQLiteCommand("INSERT INTO phrase (id, name, phrase, date) VALUES (@Id, @Name, @Content, @Date)", connection2);
                    command.Parameters.AddWithValue("@Id", phrase.Id);
                    command.Parameters.AddWithValue("@Name", phrase.Name);
                    command.Parameters.AddWithValue("@Content", phrase.Content);
                    command.Parameters.AddWithValue("@Date", phrase.Date.ToString("s"));

                    await command.ExecuteNonQueryAsync();
                }
            }
            catch (Exception ex)
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    VMLocator.ProgressViewModel.Hide();
                });
                throw new Exception($"Failed to download phrases: {ex.Message}\n{ex.StackTrace}");
            }

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                VMLocator.ProgressViewModel.ProgressValue = 0.25;
            });

            var resultEditorLog = await supabase.From<EditorLog>().Get();

            try
            {
                foreach (var editorLog in resultEditorLog.Models)
                {
                    var command = new SQLiteCommand("INSERT INTO editorlog (id, date, text) VALUES (@Id, @Date, @Content )", connection2);
                    command.Parameters.AddWithValue("@Id", editorLog.Id);
                    command.Parameters.AddWithValue("@Date", editorLog.Date.ToString("s"));
                    command.Parameters.AddWithValue("@Content", editorLog.Content);

                    await command.ExecuteNonQueryAsync();
                }
            }
            catch (Exception ex)
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    VMLocator.ProgressViewModel.Hide();
                });
                throw new Exception($"Failed to download editor logs: {ex.Message}\n{ex.StackTrace}");
            }

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                VMLocator.ProgressViewModel.ProgressValue = 0.5;
            });

            var resultTemplate = await supabase.From<Template>().Get();
            try
            {
                foreach (var template in resultTemplate.Models)
                {
                    var command = new SQLiteCommand("INSERT INTO template (id, title, text, date) VALUES (@Id, @Name, @Content, @Date)", connection2);
                    command.Parameters.AddWithValue("@Id", template.Id);
                    command.Parameters.AddWithValue("@Name", template.Title);
                    command.Parameters.AddWithValue("@Content", template.Content);
                    command.Parameters.AddWithValue("@Date", template.Date.ToString("s"));

                    await command.ExecuteNonQueryAsync();
                }
            }
            catch (Exception ex)
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    VMLocator.ProgressViewModel.Hide();
                });
                throw new Exception($"Failed to download templates: {ex.Message}\n{ex.StackTrace}");
            }

            var resultChatLog = await supabase.From<ChatRoom>().Get();
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                VMLocator.ProgressViewModel.ProgressValue = 0.75;
            });


            try
            {
                foreach (var chatLog in resultChatLog.Models)
                {
                    var resultMessage = await supabase.From<Message>().Where(x => x.RoomId == chatLog.Id).Order(x => x.Id, Ordering.Ascending).Get();

                    string combinedMessage = CombineMessage(resultMessage.Models);

                    var command = new SQLiteCommand("INSERT INTO chatlog (id, date, title, json, text, category, lastprompt, jsonprev) VALUES (@Id, @UpdatedOn, @Title, @Json, @Message, @Category, @LastPrompt, @JsonPrev)", connection2);
                    command.Parameters.AddWithValue("@Id", chatLog.Id);
                    command.Parameters.AddWithValue("@UpdatedOn", chatLog.UpdatedOn.ToString("s"));
                    command.Parameters.AddWithValue("@Title", chatLog.Title);
                    command.Parameters.AddWithValue("@Json", chatLog.Json);
                    command.Parameters.AddWithValue("@Message", combinedMessage);
                    command.Parameters.AddWithValue("@Category", chatLog.Category);
                    command.Parameters.AddWithValue("@LastPrompt", chatLog.LastPrompt);
                    command.Parameters.AddWithValue("@JsonPrev", chatLog.JsonPrev);

                    await command.ExecuteNonQueryAsync();
                }
            }
            catch (Exception ex)
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    VMLocator.ProgressViewModel.Hide();
                });
                throw new Exception($"Failed to download chat logs: {ex.Message} {ex.StackTrace}");
            }

            try
            {
                await connection2.CloseAsync();

                VMLocator.ProgressViewModel.ProgressText = "Display is being updated...";
                if (Dispatcher.UIThread.CheckAccess())
                {
                    VMLocator.ProgressViewModel.ProgressValue = 1;
                }
                else
                {
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        VMLocator.ProgressViewModel.ProgressValue = 1;
                    });
                }

                // インメモリをいったん閉じてまた開く
                await DatabaseProcess.memoryConnection!.CloseAsync();
                await DatabaseProcess.Instance.DbLoadToMemoryAsync();
                VMLocator.DataGridViewModel.ChatList = await DatabaseProcess.Instance.SearchChatDatabaseAsync();
                await DatabaseProcess.Instance.GetEditorLogDatabaseAsync();
                await DatabaseProcess.Instance.GetTemplateItemsAsync();
                string selectedPhraseItem = VMLocator.MainViewModel.SelectedPhraseItem!;
                await VMLocator.MainViewModel.LoadPhraseItemsAsync();
                if (Dispatcher.UIThread.CheckAccess())
                {
                    VMLocator.DataGridViewModel.SelectedItemIndex = -1;
                    VMLocator.MainViewModel.SelectedPhraseItem = selectedPhraseItem;
                    VMLocator.EditorViewModel.SelectedEditorLogIndex = -1;
                    VMLocator.EditorViewModel.SelectedTemplateItemIndex = -1;
                }
                else
                {
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        VMLocator.DataGridViewModel.SelectedItemIndex = -1;
                        VMLocator.MainViewModel.SelectedPhraseItem = selectedPhraseItem;
                        VMLocator.EditorViewModel.SelectedEditorLogIndex = -1;
                        VMLocator.EditorViewModel.SelectedTemplateItemIndex = -1;
                    });
                }
            }
            catch (Exception ex)
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    VMLocator.ProgressViewModel.Hide();
                });
                throw new Exception($"Error during display update: {ex.Message}\n{ex.StackTrace}");
            }
        }

        // -------------------------------------------------------------------------------------------------------
        public string CombineMessage(List<Message> messages)
        {
            string combinedMessage = "";
            string br = Environment.NewLine;
            foreach (var message in messages)
            {
                string date = message.CreatedOn.ToString();
                if (message.CreatedOn.Year < 0002)
                {
                    date = "Web Chat";
                }
                string role = message.Role == "assistant" ? "AI" : "You";
                combinedMessage = $"{combinedMessage}[{date}] by {role}{br}{br}";
                combinedMessage = $"{combinedMessage}{message.Content}{br}{br}{message.Usage}";
                combinedMessage = combinedMessage.Trim() + br + br;
            }
            combinedMessage = combinedMessage.Trim();
            return combinedMessage;
        }

        // -------------------------------------------------------------------------------------------------------
        public List<Message> DivideMessage(string message, long roomId, string uid)
        {
            var models = new List<Message>();

            string normarizedContent = Regex.Replace(message, @"\r\n|\r|\n", Environment.NewLine);

            var chatLogRegex = new Regex(@"^\[(.+)\] by (You|AI)", RegexOptions.Multiline);
            var usageRegex = new Regex(@"(^usage=)|(^(\[tokens\]))", RegexOptions.Multiline);
            var systemOffRegex = new Regex(@$"#(\s*)(?i)system({Environment.NewLine})*?---({Environment.NewLine})*", RegexOptions.Singleline);
            var systemMessageRegex = new Regex(@$"#(\s*)(?i)system({Environment.NewLine})*(.+?)---({Environment.NewLine})*(\(!--editable--\))*", RegexOptions.Singleline);

            MatchCollection matches = chatLogRegex.Matches(normarizedContent);

            for (int i = 0; i < matches.Count; i++)
            {
                var match = matches[i];
                var isUser = match.Groups[2].Value == "You";
                var role = isUser ? "user" : "assistant";
                DateTime timestamp;
                if (DateTime.TryParse(match.Groups[1].Value, out DateTime result))
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
                    models.Add(new Message { UserId = uid, RoomId = roomId, CreatedOn = timestamp, Content = "System messages were turned off.", Role = "system", Usage = "" });
                    content = content.Replace(systemOffMatch.Value, "").Trim('\r', '\n');
                }

                var systemMessageMatch = systemMessageRegex.Match(content);
                if (systemMessageMatch.Success)
                {
                    models.Add(new Message { UserId = uid, RoomId = roomId, CreatedOn = timestamp, Content = systemMessageMatch.Value, Role = "system", Usage = "" });
                    content = content.Replace(systemMessageMatch.Value, "").Trim('\r', '\n');
                }

                var usageMatch = usageRegex.Match(content);
                if (usageMatch.Success)
                {
                    var usageStr = Regex.Replace(content.Substring(usageMatch.Index).Trim('\r', '\n'), "usage={\"prompt_tokens\":([0-9]+),\"completion_tokens\":([0-9]+),\"total_tokens\":([0-9]+)}", "[tokens] prompt:$1, completion:$2, total:$3");
                    content = content.Substring(0, usageMatch.Index).Trim('\r', '\n');
                    if (content.Length > 0)
                    {
                        models.Add(new Message { UserId = uid, RoomId = roomId, CreatedOn = timestamp, Content = content, Role = role, Usage = usageStr });
                    }
                }
                else
                {
                    content = content.Trim('\r', '\n');
                    if (content.Length > 0 && content != "(!--editable--)")
                    {
                        models.Add(new Message { UserId = uid, RoomId = roomId, CreatedOn = timestamp, Content = content, Role = role, Usage = "" });
                    }
                }
            }

            return models;
        }
        // -------------------------------------------------------------------------------------------------------
        private void BackupDb()
        {
            string sourceFile = AppSettings.Instance.DbPath;
            string backupFile = AppSettings.Instance.DbPath + ".backup-" + DateTime.Now.ToString("yyyyMMddHHmmss");

            // Ensure the target does not exist.
            if (File.Exists(backupFile))
            {
                File.Delete(backupFile);
            }

            // Copy the file.
            File.Copy(sourceFile, backupFile);
        }
        // -------------------------------------------------------------------------------------------------------
        private async Task DeleteLocalDbAsync()
        {
            using SQLiteConnection connection = new SQLiteConnection($"Data Source={AppSettings.Instance.DbPath};Version=3;");
            await connection.OpenAsync();
            using (var transaction = await connection.BeginTransactionAsync())
            {
                try
                {
                    foreach (var table in new List<string> { "phrase", "chatlog", "editorlog", "template", "management" })
                    {
                        var command = new SQLiteCommand($"DELETE FROM {table}", connection, (SQLiteTransaction)transaction);
                        await command.ExecuteNonQueryAsync();
                    }
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    throw new Exception($"Failed to delete local logs: {ex.Message}\n{ex.StackTrace}");
                }
            }
            await connection.CloseAsync();
        }
        // -------------------------------------------------------------------------------------------------------
        private async Task DeleteCloudDbAsync()
        {
            try
            {
                var supabase = SupabaseStates.Instance.Supabase;

                await supabase!
                        .From<ChatRoom>()
                        .Where(x => x.Id > 0)
                        .Delete();

                await supabase!
                        .From<EditorLog>()
                        .Where(x => x.Id > 0)
                        .Delete();

                await supabase!
                        .From<Phrase>()
                        .Where(x => x.Id > 0)
                        .Delete();

                await supabase!
                        .From<Template>()
                        .Where(x => x.Id > 0)
                        .Delete();
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to delete cloud logs: {ex.Message}\n{ex.StackTrace}");
            }
        }
        // -------------------------------------------------------------------------------------------------------
        public async Task<string> InitializeAndCheckManagementTableAsync()
        {
            try
            {
                var supabase = SupabaseStates.Instance.Supabase;
                var uid = SupabaseStates.Instance.Supabase!.Auth.CurrentUser!.Id;

                using SQLiteConnection connection = new($"Data Source={AppSettings.Instance.DbPath};Version=3;");
                await connection.OpenAsync();
                var command = new SQLiteCommand("SELECT * FROM management WHERE id = 0;", connection);
                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    if ((string)reader["user_id"] != SupabaseStates.Instance.Supabase?.Auth.CurrentUser!.Id)
                    {
                        await connection.CloseAsync();
                        return "switch";
                    }
                    else
                    {
                        await connection.CloseAsync();
                        return "ok";
                    }
                }
                else
                {
                    const string Sql = "INSERT INTO management (id, user_id, delete_table, delete_id, date) VALUES (@Id, @User_id, @Delete_table, @Delete_id, @Date) ";
                    command = new SQLiteCommand(Sql, connection);
                    command.Parameters.AddWithValue("@Id", "0");
                    command.Parameters.AddWithValue("@User_id", SupabaseStates.Instance.Supabase!.Auth.CurrentUser!.Id);
                    command.Parameters.AddWithValue("@Delete_table", "");
                    command.Parameters.AddWithValue("@Delete_id", "");
                    DateTime date = DateTime.Now;
                    date = date.AddTicks(-(date.Ticks % TimeSpan.TicksPerSecond));
                    command.Parameters.AddWithValue("@Date", date.ToString("s"));
                    await command.ExecuteNonQueryAsync();

                    await connection.CloseAsync();
                    return "new";
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Management table initialization failed: {ex.Message}\n{ex.StackTrace}");
            }
        }
        // -------------------------------------------------------------------------------------------------------
        public async Task SyncManagementTableAsync()
        {
            try
            {
                var supabase = SupabaseStates.Instance.Supabase;
                var uid = SupabaseStates.Instance.Supabase!.Auth.CurrentUser!.Id;

                // 外側の辞書を初期化。キーはdelete_tableの値、値は内側の辞書。
                var deleteDict = new Dictionary<string, Dictionary<long, DateTime>>
                            {
                                { "phrase", new Dictionary<long, DateTime>() },
                                { "chatlog", new Dictionary<long, DateTime>() },
                                { "editorlog", new Dictionary<long, DateTime>() },
                                { "template", new Dictionary<long, DateTime>() }
                            };

                using SQLiteConnection connection = new($"Data Source={AppSettings.Instance.DbPath};Version=3;");
                await connection.OpenAsync();
                var command0 = new SQLiteCommand("SELECT * FROM management WHERE id != 0;", connection);
                using var reader0 = command0.ExecuteReader();

                for (int i = 0; reader0.Read(); i++)
                {
                    string deleteTable = (string)reader0["delete_table"];
                    long deleteId = (long)reader0["delete_id"];
                    DateTime date = (DateTime)reader0["date"];

                    deleteDict[deleteTable][deleteId] = date;
                }

                var resultManagement = await supabase!
                                    .From<Management>()
                                    .Select(x => new object[] { x.Id, x.DeleteTable!, x.DeleteId!, x.Date })
                                    .Order(x => x.Id, Ordering.Descending)
                                    .Get();

                if (resultManagement.Models.Count > 0)
                {
                    foreach (var management in resultManagement.Models)
                    {
                        string deleteTable = management.DeleteTable!;
                        long deleteId = management.DeleteId!;
                        DateTime date = management.Date!;

                        //ローカルの削除リストにない場合は、SQLクエリでmanagementテーブルにid、uid、DeleteTable、DeleteId、Dateを追加する。
                        if (deleteDict.All(kv => kv.Value.Count != 0) && !deleteDict[deleteTable].ContainsKey(deleteId))
                        {
                            var command1 = new SQLiteCommand("INSERT INTO management (id, user_id, delete_table, delete_id, date) VALUES (@Id, @User_id, @Delete_table, @Delete_id, @Date) ", connection);
                            command1.Parameters.AddWithValue("@Id", management.Id);
                            command1.Parameters.AddWithValue("@User_id", uid);
                            command1.Parameters.AddWithValue("@Delete_table", deleteTable);
                            command1.Parameters.AddWithValue("@Delete_id", deleteId);
                            command1.Parameters.AddWithValue("@Date", date.ToString("s"));
                            await command1.ExecuteNonQueryAsync();
                        }
                    }
                }

                if (deleteDict.All(kv => kv.Value.Count != 0))
                {
                    var models = new List<Management>();
                    //deleteDictを反復して、クラウドの削除リストに無い場合は、Supabaseのmanagementテーブルにuid、deleteTable、deleteId、dateを追加する。
                    foreach (var deleteTable in deleteDict.Keys)
                    {
                        foreach (var deleteId in deleteDict[deleteTable].Keys)
                        {
                            DateTime date = deleteDict[deleteTable][deleteId];
                            //resultManagementに該当するdeleteTableとdeleteIdがない場合は、Supabaseのmanagementテーブルにuid、deleteTable、deleteId、dateを追加する。
                            if (resultManagement.Models.Count > 0 && !resultManagement.Models.Any(x => x.DeleteTable == deleteTable && x.DeleteId == deleteId))
                            {
                                models.Add(new Management { UserId = uid!, DeleteTable = deleteTable, DeleteId = deleteId, Date = date });
                            }
                        }
                    }

                    if (models.Count > 0)
                    {
                        await supabase.From<Management>().Insert(models);
                    }
                }
                await connection.CloseAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to sync management table: {ex.Message}\n{ex.StackTrace}");
            }
        }
        // -------------------------------------------------------------------------------------------------------
        public async Task DeleteManagementTableDbAsync()
        {
            using SQLiteConnection connection = new SQLiteConnection($"Data Source={AppSettings.Instance.DbPath};Version=3;");
            await connection.OpenAsync();
            using (var transaction = await connection.BeginTransactionAsync())
            {
                try
                {
                    var command = new SQLiteCommand("DELETE FROM management", connection, (SQLiteTransaction)transaction);
                    await command.ExecuteNonQueryAsync();

                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    throw new Exception($"Failed to delete local management table: {ex.Message}\n{ex.StackTrace}");
                }
            }
            await connection.CloseAsync();
        }
    }
}
