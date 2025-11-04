using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Input;
using GamerLinkApp.Services;
using Microsoft.Maui.ApplicationModel;
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

        PopularQuestions.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasPopularQuestions));
        SendMessageCommand = new Command(async () => await SendMessageAsync(), CanSendMessage);
        SelectPopularQuestionCommand = new Command<string>(OnSelectPopularQuestion);

        _ = LoadPopularQuestionsAsync();
    }

    public ObservableCollection<SupportChatMessage> Messages { get; }

    public ObservableCollection<string> PopularQuestions { get; } = new();

    public bool HasPopularQuestions => PopularQuestions.Count > 0;

    public ICommand SendMessageCommand { get; }

    public ICommand SelectPopularQuestionCommand { get; }

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

    private async Task LoadPopularQuestionsAsync()
    {
        try
        {
            var questions = await _ragService.GetPopularQuestionsAsync().ConfigureAwait(false);
            if (questions is null || questions.Count == 0)
            {
                return;
            }

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                PopularQuestions.Clear();
                foreach (var question in questions)
                {
                    PopularQuestions.Add(question);
                }
            }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to load popular questions: {ex}");
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
            Debug.WriteLine($"RAG response success={response.IsSuccess}, detail={response.ErrorDetail}");

            Messages.Add(new SupportChatMessage(false, response.Message, response.ErrorDetail));
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to get RAG response: {ex}");
            var message = string.Format("\u62b1\u6b49\uff0c\u52a9\u624b\u6682\u65f6\u4e0d\u53ef\u7528\uff08{0}\uff09\uff0c\u8bf7\u7a0d\u540e\u518d\u8bd5\u3002", ex.Message);
            Messages.Add(new SupportChatMessage(false, message, ex.ToString()));
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async void OnSelectPopularQuestion(string? question)
    {
        if (string.IsNullOrWhiteSpace(question) || IsBusy)
        {
            return;
        }

        try
        {
            UserInput = question;

            await SendMessageAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to send popular question: {ex}");
        }
    }
}

public class SupportChatMessage : BaseViewModel
{
    public SupportChatMessage(bool isUser, string content, string? errorDetail = null)
    {
        IsUser = isUser;
        _content = content;
        _errorDetail = errorDetail;
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

    private string? _errorDetail;

    public string? ErrorDetail
    {
        get => _errorDetail;
        set
        {
            if (_errorDetail == value)
            {
                return;
            }

            _errorDetail = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasErrorDetail));
        }
    }

    public bool HasErrorDetail => !string.IsNullOrEmpty(_errorDetail);
}
