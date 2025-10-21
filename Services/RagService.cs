using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Storage;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Google;
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.Plugins.Memory;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
namespace GamerLinkApp.Services;

public class RagService : IRagService
{
    private const string MemoryCollectionName = "gamerlink-faq";
    // Relax timeouts to better tolerate high-latency connections.
    private static readonly TimeSpan KnowledgeIndexTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan MemorySearchTimeout = TimeSpan.FromSeconds(12);
    private static readonly TimeSpan ChatCompletionTimeout = TimeSpan.FromSeconds(60);

    // Quick connectivity probe for generativelanguage.googleapis.com.
    // private static readonly Uri GeminiDiscoveryUri = new("https://generativelanguage.googleapis.com/$discovery/rest?version=v1");

    private static readonly Uri GeminiDiscoveryUri = new("https://www.google.com/");

    private readonly SemaphoreSlim _reconnectLock = new(1, 1);
    private readonly SemaphoreSlim _initializationLock = new(1, 1);

    private Kernel? _kernel;
#pragma warning disable SKEXP0001 // Experimental API from Semantic Kernel.
    private ISemanticTextMemory? _memory;
#pragma warning restore SKEXP0001
#pragma warning disable SKEXP0050 // VolatileMemoryStore is experimental.
    private readonly VolatileMemoryStore _memoryStore = new();
#pragma warning restore SKEXP0050

    private bool _isInitialized;
    private string? _initializationError;
    private bool _remoteReady;

    public async Task InitializeAsync()
    {
        if (_isInitialized)
        {
            return;
        }

        await _initializationLock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_isInitialized)
            {
                return;
            }

            var geminiApiKey = await ResolveApiKeyAsync().ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(geminiApiKey))
            {
                _initializationError = "\u667A\u80FD\u5BA2\u670D\u672A\u542F\u7528\uFF1A\u672A\u68C0\u6D4B\u5230 GEMINI_API_KEY\u3002";
                Console.WriteLine("RAG service initialization skipped: GEMINI_API_KEY is not configured.");
                return;
            }

            if (!await TestConnectivityAsync().ConfigureAwait(false))
            {
                return;
            }

            if (!await CreateKernelAsync(geminiApiKey).ConfigureAwait(false))
            {
                return;
            }

            if (!await IndexKnowledgeBaseAsync().ConfigureAwait(false))
            {
                return;
            }

            _isInitialized = true;
            Console.WriteLine("RAG service (Gemini) initialized and indexed.");
        }
        finally
        {
            _initializationLock.Release();
        }
    }

    private async Task EnsureRemoteReadyAsync()
    {
        if (_remoteReady && _kernel is not null && _memory is not null)
        {
            return;
        }

        await _reconnectLock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_remoteReady && _kernel is not null && _memory is not null)
            {
                return;
            }

            var key = await ResolveApiKeyAsync().ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(key))
            {
                _remoteReady = false;
                return;
            }

            if (!await TestConnectivityAsync().ConfigureAwait(false))
            {
                _remoteReady = false;
                return;
            }

            var ok = await CreateKernelAsync(key).ConfigureAwait(false);
            _remoteReady = ok && _kernel is not null && _memory is not null;
            if (_remoteReady)
            {
                _initializationError = null;
            }
        }
        finally
        {
            _reconnectLock.Release();
        }
    }

    private async Task<bool> TestConnectivityAsync()
    {
        try
        {
            return true;
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) }; 
            using var response = await http.GetAsync(GeminiDiscoveryUri, cts.Token).ConfigureAwait(false); 
            _initializationError = null;
            return true;
        }
        catch (TaskCanceledException)
        {
            _initializationError = "\u8fde\u63a5 Gemini \u670d\u52a1\u8d85\u65f6\uff0c\u8bf7\u68c0\u67e5\u7f51\u7edc\u6216\u4ee3\u7406\u8bbe\u7f6e\uff08generativelanguage.googleapis.com\uff09\u3002";
            return false;
        }
        catch (HttpRequestException ex)
        {
            _initializationError = $"\u65e0\u6cd5\u8fde\u63a5\u5230 Gemini \u670d\u52a1\u57df\u540d\uff08generativelanguage.googleapis.com\uff09\uff1a{ex.Message}";
            return false;
        }
        catch (Exception ex)
        {
            _initializationError = $"Gemini \u8fde\u901a\u6027\u68c0\u6d4b\u5931\u8d25\uff1a{ex.Message}";
            return false;
        }
    }

    public async Task<string> AskAsync(string question)
    {
        if (!_isInitialized)
        {
            await InitializeAsync().ConfigureAwait(false);
        }

        await EnsureRemoteReadyAsync().ConfigureAwait(false);

        if (_kernel is null || _memory is null)
        {
            return _initializationError ?? "\u667A\u80FD\u5BA2\u670D\u6B63\u5728\u521D\u59CB\u5316\uFF0C\u8BF7\u7A0D\u540E\u518D\u8BD5\u3002";
        }

        var relevantContext = new List<string>();

        using (var searchCts = new CancellationTokenSource(MemorySearchTimeout))
        {
            var searchResults = _memory.SearchAsync(
                collection: MemoryCollectionName,
                query: question,
                limit: 2,
                minRelevanceScore: 0.2);

            try
            {
                await foreach (var searchResult in searchResults.WithCancellation(searchCts.Token).ConfigureAwait(false))
                {
                    relevantContext.Add(searchResult.Metadata.Text);
                }
            }
            catch (OperationCanceledException)
            {
                return "\u68C0\u7D22\u5BA2\u670D\u77E5\u8BC6\u5E93\u8D85\u65F6\uFF0C\u8BF7\u68C0\u67E5\u7F51\u7EDC\u8FDE\u63A5\u540E\u91CD\u8BD5\u3002";
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"RAG service memory search failed: {ex}");
                return $"\u62B1\u6B49\uFF0C\u6682\u65F6\u65E0\u6CD5\u8FDE\u63A5\u667A\u80FD\u5BA2\u670D\u670D\u52A1\uFF08{ex.Message}\uFF09\u3002";
            }
        }

        if (relevantContext.Count == 0)
        {
            relevantContext.Add("\uFF08\u6682\u65E0\u5339\u914D\u7684\u77E5\u8BC6\u5E93\u5185\u5BB9\uFF09");
        }

        var promptLines = new[]
        {
            "\u4F60\u662F\u6E38\u620F\u5E73\u53F0\u201C GamerLink \u201D\u7684\u5B98\u65B9\u5BA2\u670D\u52A9\u624B\u3002\u8BF7\u53C2\u8003\u201C\u5DF2\u77E5\u4FE1\u606F\u201D\u56DE\u7B54\u7528\u6237\u95EE\u9898\uFF0C\u56DE\u7B54\u9700\u7B80\u6D01\u53CB\u597D\u5E76\u4E25\u683C\u57FA\u4E8E\u8FD9\u4E9B\u4FE1\u606F\u3002",
            "\u5982\u679C\u201C\u5DF2\u77E5\u4FE1\u606F\u201D\u4E0D\u8DB3\u4EE5\u56DE\u7B54\uFF0C\u8BF7\u56DE\u590D\uFF1A\u201C\u62B1\u6B49\uFF0C\u5173\u4E8E\u8FD9\u4E2A\u95EE\u9898\u6211\u6682\u65F6\u65E0\u6CD5\u63D0\u4F9B\u5E2E\u52A9\uFF0C\u60A8\u53EF\u4EE5\u5C1D\u8BD5\u6362\u4E2A\u95EE\u6CD5\u3002\u201D",
            "---",
            "\u5DF2\u77E5\u4FE1\u606F:",
            relevantContext.Count == 0 ? string.Empty : string.Join("\n---\n", relevantContext),
            "---",
            "\u7528\u6237\u7684\u95EE\u9898:",
            question
        };

        var augmentedPrompt = string.Join("\n", promptLines);

        var chatCompletionService = _kernel.GetRequiredService<IChatCompletionService>();
        var chatHistory = new ChatHistory();
        chatHistory.AddUserMessage(augmentedPrompt);

        try
        {
            using var chatCts = new CancellationTokenSource(ChatCompletionTimeout);
            var chatResult = await chatCompletionService
                .GetChatMessageContentAsync(chatHistory, cancellationToken: chatCts.Token)
                .ConfigureAwait(false);

            return chatResult.Content ?? "\u62B1\u6B49\uFF0C\u6211\u6682\u65F6\u65E0\u6CD5\u56DE\u7B54\u8FD9\u4E2A\u95EE\u9898\u3002";
        }
        catch (TaskCanceledException)
        {
            return "\u751F\u6210\u5BA2\u670D\u56DE\u590D\u8D85\u65F6\uFF0C\u8BF7\u7A0D\u540E\u91CD\u8BD5\u6216\u68C0\u67E5\u7F51\u7EDC\u72B6\u51B5\u3002";
        }
        catch (OperationCanceledException)
        {
            return "\u751F\u6210\u5BA2\u670D\u56DE\u590D\u8D85\u65F6\uFF0C\u8BF7\u7A0D\u540E\u91CD\u8BD5\u6216\u68C0\u67E5\u7F51\u7EDC\u72B6\u51B5\u3002";
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"RAG service chat request failed: {ex}");
            return $"\u62B1\u6B49\uFF0C\u6682\u65F6\u65E0\u6CD5\u8FDE\u63A5\u667A\u80FD\u5BA2\u670D\u670D\u52A1\uFF08{ex.Message}\uFF09\u3002";
        }
    }

    private async Task<bool> CreateKernelAsync(string geminiApiKey)
    {
        try
        {
            const string embeddingModelId = "text-embedding-004";
            const string chatModelId = "gemini-2.5-flash";

            var builder = Kernel.CreateBuilder();

            builder.AddGoogleAIEmbeddingGenerator(
                modelId: embeddingModelId,
                apiKey: geminiApiKey);

            builder.AddGoogleAIGeminiChatCompletion(
                modelId: chatModelId,
                apiKey: geminiApiKey);

            _kernel = builder.Build();

#pragma warning disable SKEXP0001
            var embeddingGenerator = _kernel.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();
            _memory = new SemanticTextMemory(_memoryStore, embeddingGenerator);
#pragma warning restore SKEXP0001

            return true;
        }
        catch (Exception ex)
        {
            _initializationError = $"\u667A\u80FD\u5BA2\u670D\u521D\u59CB\u5316\u5931\u8D25\uFF1A{ex.Message}";
            Console.WriteLine($"RAG service kernel creation failed: {ex}");
            return false;
        }
    }

    private async Task<bool> IndexKnowledgeBaseAsync()
    {
        if (_memory is null)
        {
            _initializationError = "\u667A\u80FD\u5BA2\u670D\u521D\u59CB\u5316\u5931\u8D25\uFF1A\u5185\u5B58\u5B58\u50A8\u672A\u5C31\u7EEA\u3002";
            return false;
        }

        try
        {
            await using var stream = await OpenKnowledgeBaseStreamAsync().ConfigureAwait(false);
            if (stream is null)
            {
                _initializationError = "\u672A\u627E\u5230\u77E5\u8BC6\u5E93\u6587\u4EF6\uFF0C\u667A\u80FD\u5BA2\u670D\u65E0\u6CD5\u542F\u52A8\u3002";
                Console.WriteLine("RAG service knowledge base file not found.");
                return false;
            }

            using var reader = new StreamReader(stream);
            var knowledgeBaseText = await reader.ReadToEndAsync().ConfigureAwait(false);

            var chunks = SplitMarkdownIntoChunks(knowledgeBaseText, maxChunkLength: 2000, overlapLength: 200);

            var index = 0;
            foreach (var chunk in chunks)
            {
                try
                {
                    using var embeddingCts = new CancellationTokenSource(KnowledgeIndexTimeout);
                    await _memory.SaveInformationAsync(
                        collection: MemoryCollectionName,
                        text: chunk,
                        id: $"faq-{index++}",
                        cancellationToken: embeddingCts.Token).ConfigureAwait(false);
                }
                catch (TaskCanceledException ex)
                {
                    _initializationError = "\u52A0\u8F7D\u77E5\u8BC6\u5E93\u5931\u8D25\uFF1A\u8BBF\u95EE Gemini \u670D\u52A1\u8D85\u65F6\uFF0C\u8BF7\u68C0\u67E5\u8BBE\u5907\u7F51\u7EDC\u6216\u7A0D\u540E\u91CD\u8BD5\u3002";
                    Console.WriteLine($"RAG service memory save timeout: {ex}");
                    return false;
                }
                catch (OperationCanceledException ex)
                {
                    _initializationError = "\u52A0\u8F7D\u77E5\u8BC6\u5E93\u5931\u8D25\uFF1A\u8BBF\u95EE Gemini \u670D\u52A1\u8D85\u65F6\uFF0C\u8BF7\u68C0\u67E5\u8BBE\u5907\u7F51\u7EDC\u6216\u7A0D\u540E\u91CD\u8BD5\u3002";
                    Console.WriteLine($"RAG service memory save canceled: {ex}");
                    return false;
                }
                catch (HttpRequestException ex)
                {
                    _initializationError = $"\u52A0\u8F7D\u77E5\u8BC6\u5E93\u5931\u8D25\uFF1A\u65E0\u6CD5\u8FDE\u63A5 Gemini \u670D\u52A1\uFF08{ex.Message}\uFF09\u3002";
                    Console.WriteLine($"RAG service memory save http error: {ex}");
                    return false;
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            _initializationError = $"\u52A0\u8F7D\u77E5\u8BC6\u5E93\u5931\u8D25\uFF1A{ex.Message}";
            Console.WriteLine($"RAG service knowledge base indexing failed: {ex}");
            return false;
        }
    }

    private static async Task<string?> ResolveApiKeyAsync()
    {
        string? key = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
        if (!string.IsNullOrWhiteSpace(key))
        {
            return key.Trim();
        }

        foreach (var target in new[]
                 {
                     EnvironmentVariableTarget.Process,
                     EnvironmentVariableTarget.User,
                     EnvironmentVariableTarget.Machine
                 })
        {
            try
            {
                key = Environment.GetEnvironmentVariable("GEMINI_API_KEY", target);
                if (!string.IsNullOrWhiteSpace(key))
                {
                    return key.Trim();
                }
            }
            catch
            {
                // ignored
            }
        }

        try
        {
            var baseDirectory = AppContext.BaseDirectory;
            var candidateFiles = new[]
            {
                Path.Combine(baseDirectory, "gemini_api_key.txt"),
                Path.Combine(baseDirectory, "Secrets", "gemini_api_key.txt")
            };

            foreach (var file in candidateFiles)
            {
                if (File.Exists(file))
                {
                    var fileKey = File.ReadAllText(file).Trim();
                    if (!string.IsNullOrWhiteSpace(fileKey))
                    {
                        return fileKey;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to read Gemini API key from base directory: {ex.Message}");
        }

        try
        {
            var appDataFile = Path.Combine(FileSystem.AppDataDirectory, "gemini_api_key.txt");
            if (File.Exists(appDataFile))
            {
                var fileKey = File.ReadAllText(appDataFile).Trim();
                if (!string.IsNullOrWhiteSpace(fileKey))
                {
                    return fileKey;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to read Gemini API key from app data: {ex.Message}");
        }

        try
        {
            using var stream = await FileSystem.OpenAppPackageFileAsync("gemini_api_key.txt").ConfigureAwait(false);
            using var reader = new StreamReader(stream);
            var fileKey = (await reader.ReadToEndAsync().ConfigureAwait(false)).Trim();
            if (!string.IsNullOrWhiteSpace(fileKey))
            {
                try
                {
                    var appDataFile = Path.Combine(FileSystem.AppDataDirectory, "gemini_api_key.txt");
                    var directory = Path.GetDirectoryName(appDataFile);
                    if (!string.IsNullOrEmpty(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    File.WriteAllText(appDataFile, fileKey);
                }
                catch
                {
                    // Best-effort persistence; failures are non-fatal.
                }

                return fileKey;
            }
        }
        catch (FileNotFoundException)
        {
            // ignored
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to read Gemini API key from app package: {ex.Message}");
        }

        return null;
    }

    private static async Task<Stream?> OpenKnowledgeBaseStreamAsync()
    {
        const string resourceName = "GamerLinkApp.Resources.Raw.knowledge_base.md";
        var assembly = Assembly.GetExecutingAssembly();

        try
        {
            var resourceStream = assembly.GetManifestResourceStream(resourceName);
            if (resourceStream is not null)
            {
                return resourceStream;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to read knowledge base from resources: {ex.Message}");
        }

        var candidatePaths = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "knowledge_base.md"),
            Path.Combine(AppContext.BaseDirectory, "Resources", "Raw", "knowledge_base.md"),
            Path.Combine(FileSystem.AppDataDirectory, "knowledge_base.md")
        };

        foreach (var path in candidatePaths)
        {
            try
            {
                if (File.Exists(path))
                {
                    return File.OpenRead(path);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to read knowledge base from {path}: {ex.Message}");
            }
        }

        try
        {
            return await FileSystem.OpenAppPackageFileAsync("knowledge_base.md").ConfigureAwait(false);
        }
        catch (FileNotFoundException)
        {
            // ignored
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to read knowledge base from app package: {ex.Message}");
        }

        return null;
    }

    private static IEnumerable<string> SplitMarkdownIntoChunks(string text, int maxChunkLength, int overlapLength)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            yield break;
        }

        var paragraphs = text.Split(new[] { "\r\n\r\n", "\n\n" }, StringSplitOptions.RemoveEmptyEntries);
        var builder = new StringBuilder();

        foreach (var paragraph in paragraphs)
        {
            var trimmed = paragraph.Trim();
            if (trimmed.Length == 0)
            {
                continue;
            }

            if (builder.Length + trimmed.Length > maxChunkLength && builder.Length > 0)
            {
                yield return builder.ToString();

                var overlapText = overlapLength > 0 && trimmed.Length > overlapLength
                    ? trimmed[^overlapLength..]
                    : trimmed;

                builder.Clear();
                builder.AppendLine(overlapText);
                builder.AppendLine();
                continue;
            }

            builder.AppendLine(trimmed);
            builder.AppendLine();
        }

        if (builder.Length > 0)
        {
            yield return builder.ToString();
        }
    }
}
