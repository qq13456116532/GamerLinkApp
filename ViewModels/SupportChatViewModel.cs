using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Input;
using GamerLinkApp.Services;
using Microsoft.Maui.Controls;

namespace GamerLinkApp.ViewModels;

public class SupportChatViewModel : BaseViewModel
{
    private readonly IRagService _ragService;
    private string _userInput = string.Empty;
    private bool _isBusy;

    public SupportChatViewModel(IRagService ragService)
    {
        _ragService = ragService;

        Messages = new ObservableCollection<SupportChatMessage>
        {
            new(false, "\u6b22\u8fce\u4f7f\u7528 GamerLink \u52a9\u624b\uff0c\u8bf7\u63cf\u8ff0\u4f60\u7684\u95ee\u9898\u3002")
        };

        SendMessageCommand = new Command(async () => await SendMessageAsync(), CanSendMessage);
    }

    public ObservableCollection<SupportChatMessage> Messages { get; }

    public ICommand SendMessageCommand { get; }

    public string UserInput
    {
        get => _userInput;
        set
        {
            if (_userInput == value)
            {
                return;
            }

            _userInput = value;
            OnPropertyChanged();
            UpdateCanExecute();
        }
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (_isBusy == value)
            {
                return;
            }

            _isBusy = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanEdit));
            UpdateCanExecute();
        }
    }

    public bool CanEdit => !IsBusy;

    private bool CanSendMessage() => CanEdit && !string.IsNullOrWhiteSpace(UserInput);

    private void UpdateCanExecute()
    {
        if (SendMessageCommand is Command command)
        {
            command.ChangeCanExecute();
        }
    }

    private async Task SendMessageAsync()
    {
        var question = UserInput?.Trim();
        if (string.IsNullOrEmpty(question))
        {
            return;
        }

        try
        {
            IsBusy = true;

            Messages.Add(new SupportChatMessage(true, question));
            UserInput = string.Empty;

            var response = await _ragService.AskAsync(question);
            Messages.Add(new SupportChatMessage(false, response));
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to get RAG response: {ex}");
            var message = string.Format("\u62b1\u6b49\uff0c\u52a9\u624b\u6682\u65f6\u4e0d\u53ef\u7528\uff08{0}\uff09\uff0c\u8bf7\u7a0d\u540e\u518d\u8bd5\u3002", ex.Message);
            Messages.Add(new SupportChatMessage(false, message));
        }
        finally
        {
            IsBusy = false;
        }
    }
}

public class SupportChatMessage : BaseViewModel
{
    public SupportChatMessage(bool isUser, string content)
    {
        IsUser = isUser;
        _content = content;
    }

    public bool IsUser { get; }

    private string _content;

    public string Content
    {
        get => _content;
        set
        {
            if (_content == value)
            {
                return;
            }

            _content = value;
            OnPropertyChanged();
        }
    }
}
