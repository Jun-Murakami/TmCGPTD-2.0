using System.Text.Json;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using TmCGPTD.Models;
using Avalonia.Collections;

namespace TmCGPTD.ViewModels
{
    public class DataGridViewModel : ViewModelBase
    {
        private long _selectedItemIndex;
        public long SelectedItemIndex
        {
            get => _selectedItemIndex;
            set => SetProperty(ref _selectedItemIndex, value);
        }

        private bool _dataGridIsFocused;
        public bool DataGridIsFocused
        {
            get => _dataGridIsFocused;
            set => SetProperty(ref _dataGridIsFocused, value);
        }

        private DataGridCollectionView? _dataGridCollection;
        public DataGridCollectionView? DataGridCollection
        {
            get => _dataGridCollection;
            set => SetProperty(ref _dataGridCollection, value);
        }

        private ObservableCollection<ChatList>? _chatList;
        public ObservableCollection<ChatList>? ChatList
        {
            get => _chatList;
            set
            {
                if (SetProperty(ref _chatList, value))
                {
                    DataGridCollection = new DataGridCollectionView(ChatList);
                    DataGridCollection.GroupDescriptions.Add(new DataGridPathGroupDescription("Category"));
                }
            }
        }

        private ChatList? _selectedItem;
        public ChatList? SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (SetProperty(ref _selectedItem, value))
                {
                    OnPropertyChanged(nameof(SelectedItemId));

                    if (SelectedItemId != -1 && _selectedItem != null && !VMLocator.ChatViewModel.ChatIsRunning && DataGridIsFocused)
                    {
                        VMLocator.ChatViewModel.LastId = _selectedItem.Id;
                        ShowChatLogAsync(_selectedItem.Id);
                    }
                    else if (!string.IsNullOrWhiteSpace(VMLocator.MainViewModel.SearchLogKeyword))
                    {
                        SelectedItemIndex = -1;
                    }
                }
            }
        }

        public long SelectedItemId => _selectedItem?.Id ?? default;

        DatabaseProcess _dbProcess = new DatabaseProcess();
        HtmlProcess _htmlProcess = new HtmlProcess();
        private async void ShowChatLogAsync(long _selectedItem)
        {
            if (!VMLocator.ChatViewModel.ChatIsRunning)
            {
                VMLocator.ChatViewModel.ChatViewIsVisible = true;
                var result = await _dbProcess.GetChatLogDatabaseAsync(_selectedItem);

                VMLocator.ChatViewModel.ReEditIsOn = false;
                VMLocator.ChatViewModel.ChatTitle = result[0];
                if (!string.IsNullOrWhiteSpace(result[1]))
                {
                    VMLocator.ChatViewModel.ConversationHistory = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(result[1])!;
                }
                VMLocator.ChatViewModel.HtmlContent = await _htmlProcess.ConvertChatLogToHtml(result[2]);
                VMLocator.ChatViewModel.ChatCategory = result[3];
                VMLocator.ChatViewModel.LastPrompt = result[4];
                if (!string.IsNullOrWhiteSpace(result[5]))
                {
                    VMLocator.ChatViewModel.LastConversationHistory = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(result[5])!;
                }

                if (VMLocator.MainViewModel.SelectedLeftPane != "API Chat" || VMLocator.MainViewModel.LoginStatus != 4)
                {
                    VMLocator.MainViewModel.SelectedLeftPane = "API Chat";
                    VMLocator.MainViewModel.LoginStatus = 4;
                }
            }
        }


    }
}