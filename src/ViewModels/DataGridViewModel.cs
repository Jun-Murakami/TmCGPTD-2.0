using System.Text.Json;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using TmCGPTD.Models;

namespace TmCGPTD.ViewModels
{
    public class DataGridViewModel : ViewModelBase
    {

        private int _selectedItemIndex;
        public int SelectedItemIndex
        {
            get => _selectedItemIndex;
            set => SetProperty(ref _selectedItemIndex, value);
        }

        private ObservableCollection<ChatList> _chatList;
        public ObservableCollection<ChatList> ChatList
        {
            get => _chatList;
            set => SetProperty(ref _chatList, value);
        }

        private ChatList _selectedItem;
        public ChatList SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (SetProperty(ref _selectedItem, value))
                {
                    OnPropertyChanged(nameof(SelectedItemId));

                    if (_selectedItem != default)
                    {
                        VMLocator.ChatViewModel.LastId = _selectedItem.Id;
                        ShowChatLogAsync(_selectedItem.Id);
                    }
                    else
                    {
                        //VMLocator.ChatViewModel.LastId = -1;
                    }
                }
            }
        }

        public long SelectedItemId => _selectedItem?.Id ?? default;

        DatabaseProcess _dbProcess = new DatabaseProcess();
        HtmlProcess _htmlProcess = new HtmlProcess();
        private async void ShowChatLogAsync(long _selectedItem)
        {
            var _chatViewModel = VMLocator.ChatViewModel;

            if (!_chatViewModel.ChatIsRunning)
            {
                var result = await _dbProcess.GetChatLogDatabaseAsync(_selectedItem);

                _chatViewModel.ChatTitle = result[0];
                _chatViewModel.ConversationHistory = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(result[1]);
                _chatViewModel.HtmlContent = await _htmlProcess.ConvertChatLogToHtml(result[2]);

                if (VMLocator.MainViewModel.SelectedLeftPane != "API Chat")
                {
                    VMLocator.MainViewModel.SelectedLeftPane = "API Chat";
                }
            }
        }


    }
}