using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Storage;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Google;
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.Plugins.Memory;

namespace GamerLinkApp.Services;

public class RagService : IRagService
{
    private const string MemoryCollectionName = "gamerlink-faq";

    private readonly SemaphoreSlim _initializationLock = new(1, 1);

    private Kernel? _kernel;
#pragma warning disable SKEXP0001 // Experimental API from Semantic Kernel.
    private ISemanticTextMemory? _memory;
#pragma warning restore SKEXP0001
    private bool _isInitialized;
    private string? _initializationError;

    public async Task InitializeAsync()
    {
        if (_isInitialized || !string.IsNullOrEmpty(_initializationError))
        {
            return;
        }

        await _initializationLock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_isInitialized || !string.IsNullOrEmpty(_initializationError))
            {
                return;
            }

            var geminiApiKey = await ResolveApiKeyAsync().ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(geminiApiKey))
            {
                _initializationError = "智能客服未启用：未检测到 GEMINI_API_KEY。";
                Console.WriteLine("RAG service initialization skipped: GEMINI_API_KEY is not configured.");
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

    public async Task<string> AskAsync(string question)
    {
        if (!_isInitialized && string.IsNullOrEmpty(_initializationError))
        {
            await InitializeAsync().ConfigureAwait(false);
        }

        if (!string.IsNullOrEmpty(_initializationError))
        {
            return $"{_initializationError}\n请联系管理员配置 GEMINI_API_KEY 后再试。";
        }

        if (!_isInitialized || _kernel is null || _memory is null)
        {
            return "智能客服正在初始化，请稍后再试。";
        }

        var searchResults = _memory.SearchAsync(
            collection: MemoryCollectionName,
            query: question,
            limit: 2,
            minRelevanceScore: 0.2);

        var relevantContext = new List<string>();
        await foreach (var searchResult in searchResults.ConfigureAwait(false))
        {
            relevantContext.Add(searchResult.Metadata.Text);
        }

        var contextString = string.Join("\n---\n", relevantContext);

        var augmentedPrompt = $"""
        你是游戏平台“GamerLink”的官方客服助手。请参考“已知信息”回答用户问题，回答需简洁友好并严格基于这些信息。
        如果“已知信息”不足以回答，请回复：“抱歉，关于这个问题我暂时无法提供帮助，您可以尝试换个问法。”
        ---
        已知信息:
        {contextString}
        ---

        用户的问题:
        {question}
        """;

        var chatCompletionService = _kernel.GetRequiredService<IChatCompletionService>();
        var chatHistory = new ChatHistory();
        chatHistory.AddUserMessage(augmentedPrompt);

        var chatResult = await chatCompletionService
            .GetChatMessageContentAsync(chatHistory)
            .ConfigureAwait(false);

        return chatResult.Content ?? "抱歉，我暂时无法回答这个问题。";
    }

    private async Task<bool> CreateKernelAsync(string geminiApiKey)
    {
        try
        {
            const string embeddingModelId = "text-embedding-004";
            const string chatModelId = "gemini-2.5-flash";

            var builder = Kernel.CreateBuilder();

            builder.AddGoogleAIEmbeddingGeneration(
                modelId: embeddingModelId,
                apiKey: geminiApiKey);

            builder.AddGoogleAIGeminiChatCompletion(
                modelId: chatModelId,
                apiKey: geminiApiKey);

#pragma warning disable SKEXP0050 // VolatileMemoryStore is experimental.
            var memoryStore = new VolatileMemoryStore();
#pragma warning restore SKEXP0050

            _kernel = builder.Build();

#pragma warning disable SKEXP0001
            _memory = new SemanticTextMemory(
                memoryStore,
                _kernel.GetRequiredService<ITextEmbeddingGenerationService>());
#pragma warning restore SKEXP0001

            return true;
        }
        catch (Exception ex)
        {
            _initializationError = $"智能客服初始化失败：{ex.Message}";
            Console.WriteLine($"RAG service kernel creation failed: {ex}");
            return false;
        }
    }

    private async Task<bool> IndexKnowledgeBaseAsync()
    {
        if (_memory is null)
        {
            _initializationError = "智能客服初始化失败：内存存储未就绪。";
            return false;
        }

        try
        {
            await using var stream = await OpenKnowledgeBaseStreamAsync().ConfigureAwait(false);
            if (stream is null)
            {
                _initializationError = "未找到知识库文件，智能客服无法启动。";
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
                    await _memory.SaveInformationAsync(
                        collection: MemoryCollectionName,
                        text: chunk,
                        id: $"faq-{index++}").ConfigureAwait(false);
                }
                catch (TaskCanceledException ex)
                {
                    _initializationError = "加载知识库失败：访问 Gemini 服务超时，请检查设备网络或稍后重试。";
                    Console.WriteLine($"RAG service memory save timeout: {ex}");
                    return false;
                }
                catch (HttpRequestException ex)
                {
                    _initializationError = $"加载知识库失败：无法连接到 Gemini 服务（{ex.Message}）。";
                    Console.WriteLine($"RAG service memory save http error: {ex}");
                    return false;
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            _initializationError = $"加载知识库失败：{ex.Message}";
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
                // ignore and try next source
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
                return fileKey;
            }
        }
        catch (FileNotFoundException)
        {
            // ignore
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
            // ignore
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
