using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;
using System.Collections.Concurrent;
using MapleBlog.Application.Interfaces;

namespace MapleBlog.Infrastructure.Services;

/// <summary>
/// 敏感词过滤服务实现
/// </summary>
public class SensitiveWordFilter : ISensitiveWordFilter
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SensitiveWordFilter> _logger;

    // 敏感词存储
    private readonly ConcurrentDictionary<string, SensitiveWordRiskLevel> _sensitiveWords = new();
    private readonly ConcurrentDictionary<SensitiveWordRiskLevel, HashSet<string>> _wordsByRisk = new();

    // AC自动机相关
    private readonly object _lockObject = new();
    private AhoCorasickNode? _rootNode;
    private volatile bool _isInitialized = false;

    // 配置选项
    private readonly string _maskChar;
    private readonly bool _enableFuzzyMatching;
    private readonly bool _caseSensitive;

    public SensitiveWordFilter(IConfiguration configuration, ILogger<SensitiveWordFilter> logger)
    {
        _configuration = configuration;
        _logger = logger;

        _maskChar = configuration.GetValue<string>("SensitiveWordFilter:MaskChar", "*");
        _enableFuzzyMatching = configuration.GetValue<bool>("SensitiveWordFilter:EnableFuzzyMatching", true);
        _caseSensitive = configuration.GetValue<bool>("SensitiveWordFilter:CaseSensitive", false);

        // 初始化敏感词库
        _ = Task.Run(InitializeSensitiveWordsAsync);
    }

    /// <summary>
    /// 检查内容是否包含敏感词
    /// </summary>
    public async Task<SensitiveWordResult> CheckContentAsync(string content, bool replaceWithMask = false)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return new SensitiveWordResult
            {
                ContainsSensitiveWords = false,
                FilteredContent = content
            };
        }

        await EnsureInitializedAsync();

        try
        {
            var detectedWords = FindSensitiveWords(content);

            if (!detectedWords.Any())
            {
                return new SensitiveWordResult
                {
                    ContainsSensitiveWords = false,
                    FilteredContent = content
                };
            }

            var highRiskWords = new List<string>();
            var mediumRiskWords = new List<string>();
            var lowRiskWords = new List<string>();

            foreach (var word in detectedWords)
            {
                if (_sensitiveWords.TryGetValue(word, out var riskLevel))
                {
                    switch (riskLevel)
                    {
                        case SensitiveWordRiskLevel.High:
                            highRiskWords.Add(word);
                            break;
                        case SensitiveWordRiskLevel.Medium:
                            mediumRiskWords.Add(word);
                            break;
                        case SensitiveWordRiskLevel.Low:
                            lowRiskWords.Add(word);
                            break;
                    }
                }
            }

            var filteredContent = replaceWithMask ? ReplaceSensitiveWords(content, detectedWords) : content;
            var requiresManualReview = highRiskWords.Any() || mediumRiskWords.Count >= 3;

            return new SensitiveWordResult
            {
                ContainsSensitiveWords = true,
                TotalDetectedWords = detectedWords.Count,
                DetectedWords = detectedWords,
                HighRiskWords = highRiskWords,
                MediumRiskWords = mediumRiskWords,
                LowRiskWords = lowRiskWords,
                FilteredContent = filteredContent,
                RequiresManualReview = requiresManualReview
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking content for sensitive words");

            // 出错时保守处理，假设包含敏感词
            return new SensitiveWordResult
            {
                ContainsSensitiveWords = true,
                TotalDetectedWords = 0,
                FilteredContent = content,
                RequiresManualReview = true
            };
        }
    }

    /// <summary>
    /// 批量检查内容
    /// </summary>
    public async Task<IEnumerable<SensitiveWordResult>> CheckBatchAsync(IEnumerable<string> contents, bool replaceWithMask = false)
    {
        var tasks = contents.Select(content => CheckContentAsync(content, replaceWithMask));
        return await Task.WhenAll(tasks);
    }

    /// <summary>
    /// 添加敏感词
    /// </summary>
    public async Task AddSensitiveWordsAsync(IEnumerable<string> words, SensitiveWordRiskLevel riskLevel)
    {
        await Task.Run(() =>
        {
            var normalizedWords = words.Select(NormalizeWord).Where(w => !string.IsNullOrWhiteSpace(w));

            foreach (var word in normalizedWords)
            {
                _sensitiveWords.AddOrUpdate(word, riskLevel, (key, oldValue) => riskLevel);

                _wordsByRisk.AddOrUpdate(riskLevel, new HashSet<string> { word }, (key, oldSet) =>
                {
                    lock (oldSet)
                    {
                        oldSet.Add(word);
                        return oldSet;
                    }
                });
            }

            RebuildAhoCorasickTree();
        });

        _logger.LogInformation("Added {Count} sensitive words with risk level {RiskLevel}", words.Count(), riskLevel);
    }

    /// <summary>
    /// 移除敏感词
    /// </summary>
    public async Task RemoveSensitiveWordsAsync(IEnumerable<string> words)
    {
        await Task.Run(() =>
        {
            var normalizedWords = words.Select(NormalizeWord).Where(w => !string.IsNullOrWhiteSpace(w));

            foreach (var word in normalizedWords)
            {
                if (_sensitiveWords.TryRemove(word, out var riskLevel))
                {
                    if (_wordsByRisk.TryGetValue(riskLevel, out var wordSet))
                    {
                        lock (wordSet)
                        {
                            wordSet.Remove(word);
                        }
                    }
                }
            }

            RebuildAhoCorasickTree();
        });

        _logger.LogInformation("Removed {Count} sensitive words", words.Count());
    }

    /// <summary>
    /// 重新加载敏感词库
    /// </summary>
    public async Task ReloadSensitiveWordsAsync()
    {
        _isInitialized = false;
        await InitializeSensitiveWordsAsync();
        _logger.LogInformation("Sensitive words reloaded");
    }

    /// <summary>
    /// 获取敏感词统计信息
    /// </summary>
    public async Task<(int HighRisk, int MediumRisk, int LowRisk, int Total)> GetSensitiveWordStatsAsync()
    {
        await Task.CompletedTask;

        var highRisk = _wordsByRisk.GetValueOrDefault(SensitiveWordRiskLevel.High)?.Count ?? 0;
        var mediumRisk = _wordsByRisk.GetValueOrDefault(SensitiveWordRiskLevel.Medium)?.Count ?? 0;
        var lowRisk = _wordsByRisk.GetValueOrDefault(SensitiveWordRiskLevel.Low)?.Count ?? 0;
        var total = _sensitiveWords.Count;

        return (highRisk, mediumRisk, lowRisk, total);
    }

    #region 私有方法

    /// <summary>
    /// 初始化敏感词库
    /// </summary>
    private async Task InitializeSensitiveWordsAsync()
    {
        try
        {
            // 加载内置敏感词
            await LoadBuiltinSensitiveWordsAsync();

            // 从配置文件加载
            await LoadConfigurationWordsAsync();

            // 从数据库加载（如果有）
            // await LoadDatabaseWordsAsync();

            RebuildAhoCorasickTree();
            _isInitialized = true;

            _logger.LogInformation("Sensitive words initialized. Total: {Count}", _sensitiveWords.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing sensitive words");
            _isInitialized = true; // 即使出错也标记为已初始化，避免重复尝试
        }
    }

    /// <summary>
    /// 加载内置敏感词
    /// </summary>
    private async Task LoadBuiltinSensitiveWordsAsync()
    {
        await Task.Run(() =>
        {
            // 高风险敏感词
            var highRiskWords = new[]
            {
                "法轮功", "六四", "天安门", "反党", "暴动", "颠覆", "分裂国家",
                "邪教", "恐怖主义", "暴恐", "极端主义"
            };

            // 中风险敏感词
            var mediumRiskWords = new[]
            {
                "毒品", "枪支", "爆炸", "自杀", "色情", "赌博", "诈骗", "洗钱",
                "人体器官", "黑客", "病毒", "木马", "钓鱼"
            };

            // 低风险敏感词
            var lowRiskWords = new[]
            {
                "垃圾", "傻逼", "草泥马", "操你妈", "去死", "智障", "脑残", "白痴",
                "广告", "推广", "代理", "加盟", "刷单", "兼职赚钱"
            };

            foreach (var word in highRiskWords)
            {
                _sensitiveWords.TryAdd(NormalizeWord(word), SensitiveWordRiskLevel.High);
            }

            foreach (var word in mediumRiskWords)
            {
                _sensitiveWords.TryAdd(NormalizeWord(word), SensitiveWordRiskLevel.Medium);
            }

            foreach (var word in lowRiskWords)
            {
                _sensitiveWords.TryAdd(NormalizeWord(word), SensitiveWordRiskLevel.Low);
            }

            // 按风险等级分组
            foreach (var kvp in _sensitiveWords)
            {
                _wordsByRisk.AddOrUpdate(kvp.Value, new HashSet<string> { kvp.Key }, (key, oldSet) =>
                {
                    oldSet.Add(kvp.Key);
                    return oldSet;
                });
            }
        });
    }

    /// <summary>
    /// 从配置加载敏感词
    /// </summary>
    private async Task LoadConfigurationWordsAsync()
    {
        await Task.Run(() =>
        {
            var configSection = _configuration.GetSection("SensitiveWordFilter:Words");
            foreach (var riskSection in configSection.GetChildren())
            {
                if (Enum.TryParse<SensitiveWordRiskLevel>(riskSection.Key, true, out var riskLevel))
                {
                    var words = riskSection.Get<string[]>() ?? Array.Empty<string>();
                    foreach (var word in words)
                    {
                        if (!string.IsNullOrWhiteSpace(word))
                        {
                            var normalizedWord = NormalizeWord(word);
                            _sensitiveWords.TryAdd(normalizedWord, riskLevel);

                            _wordsByRisk.AddOrUpdate(riskLevel, new HashSet<string> { normalizedWord }, (key, oldSet) =>
                            {
                                oldSet.Add(normalizedWord);
                                return oldSet;
                            });
                        }
                    }
                }
            }
        });
    }

    /// <summary>
    /// 确保已初始化
    /// </summary>
    private async Task EnsureInitializedAsync()
    {
        if (!_isInitialized)
        {
            await InitializeSensitiveWordsAsync();
        }
    }

    /// <summary>
    /// 标准化词语
    /// </summary>
    private string NormalizeWord(string word)
    {
        if (string.IsNullOrWhiteSpace(word))
            return string.Empty;

        var normalized = word.Trim();

        if (!_caseSensitive)
        {
            normalized = normalized.ToLowerInvariant();
        }

        // 移除特殊字符和空格（如果启用模糊匹配）
        if (_enableFuzzyMatching)
        {
            normalized = Regex.Replace(normalized, @"[^\w\u4e00-\u9fa5]", "");
        }

        return normalized;
    }

    /// <summary>
    /// 查找敏感词
    /// </summary>
    private List<string> FindSensitiveWords(string content)
    {
        var detectedWords = new List<string>();
        var normalizedContent = NormalizeWord(content);

        if (_rootNode == null)
            return detectedWords;

        // 使用AC自动机查找
        var currentNode = _rootNode;
        for (int i = 0; i < normalizedContent.Length; i++)
        {
            var character = normalizedContent[i];

            // 寻找匹配的子节点
            while (currentNode != _rootNode && !currentNode.Children.ContainsKey(character))
            {
                currentNode = currentNode.Failure ?? _rootNode;
            }

            if (currentNode.Children.TryGetValue(character, out var childNode))
            {
                currentNode = childNode;
            }

            // 检查是否匹配到敏感词
            var node = currentNode;
            while (node != null)
            {
                if (node.IsWordEnd && !string.IsNullOrEmpty(node.Word))
                {
                    detectedWords.Add(node.Word);
                }
                node = node.Failure;
            }
        }

        // 去重并返回
        return detectedWords.Distinct().ToList();
    }

    /// <summary>
    /// 替换敏感词
    /// </summary>
    private string ReplaceSensitiveWords(string content, List<string> sensitiveWords)
    {
        var result = content;

        foreach (var word in sensitiveWords.OrderByDescending(w => w.Length))
        {
            var mask = new string(_maskChar[0], word.Length);
            result = result.Replace(word, mask);

            // 如果启用了模糊匹配，还需要处理原始形式
            if (_enableFuzzyMatching)
            {
                var pattern = string.Join(@"[\s\-_]*", word.ToCharArray().Select(c => Regex.Escape(c.ToString())));
                result = Regex.Replace(result, pattern, mask, RegexOptions.IgnoreCase);
            }
        }

        return result;
    }


    /// <summary>
    /// 重建AC自动机
    /// </summary>
    private void RebuildAhoCorasickTree()
    {
        lock (_lockObject)
        {
            _rootNode = new AhoCorasickNode();

            // 构建Trie树
            foreach (var kvp in _sensitiveWords)
            {
                var word = kvp.Key;
                var currentNode = _rootNode;

                foreach (var character in word)
                {
                    if (!currentNode.Children.ContainsKey(character))
                    {
                        currentNode.Children[character] = new AhoCorasickNode();
                    }
                    currentNode = currentNode.Children[character];
                }

                currentNode.IsWordEnd = true;
                currentNode.Word = word;
            }

            // 构建失效指针
            BuildFailurePointers();
        }
    }

    /// <summary>
    /// 构建失效指针
    /// </summary>
    private void BuildFailurePointers()
    {
        if (_rootNode == null) return;

        var queue = new Queue<AhoCorasickNode>();

        // 第一层节点的失效指针指向根节点
        foreach (var child in _rootNode.Children.Values)
        {
            child.Failure = _rootNode;
            queue.Enqueue(child);
        }

        // BFS构建其他节点的失效指针
        while (queue.Count > 0)
        {
            var currentNode = queue.Dequeue();

            foreach (var kvp in currentNode.Children)
            {
                var character = kvp.Key;
                var childNode = kvp.Value;

                queue.Enqueue(childNode);

                var failureNode = currentNode.Failure;
                while (failureNode != null && !failureNode.Children.ContainsKey(character))
                {
                    failureNode = failureNode.Failure;
                }

                childNode.Failure = failureNode?.Children.GetValueOrDefault(character) ?? _rootNode;
            }
        }
    }

    #endregion

    #region AC自动机节点

    /// <summary>
    /// AC自动机节点
    /// </summary>
    private class AhoCorasickNode
    {
        public Dictionary<char, AhoCorasickNode> Children { get; } = new();
        public AhoCorasickNode? Failure { get; set; }
        public bool IsWordEnd { get; set; }
        public string Word { get; set; } = string.Empty;
    }

    #endregion
}