using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Linq;
using System.Text.RegularExpressions;
using mauiluanvantotnghiep.Models;
using Microsoft.Maui.Dispatching;

namespace mauiluanvantotnghiep.ViewModels
{
    public partial class StoryViewModel : ObservableObject
    {
        // NEW ChatGPT-42 API constants - replacing OpenAI
        const string ChatGptApiUrl = "https://chatgpt-42.p.rapidapi.com/chat";
        const string RapidApiKey = "ad4a0be270mshd6064ddf3ce5ad8p15a3d7jsnb5563c70fcf4";

        // TTS API constants
        const string TtsApiUrl = "https://large-text-to-speech.p.rapidapi.com/tts";

        // Enhanced Genre collection with icons and colors
        public ObservableCollection<Genre> Genres { get; } = new()
        {
            new Genre { Name = "Fantasy", DisplayName = "Phiêu lưu", Icon = "🏰", Color = "#9C27B0", Description = "Thế giới phép thuật và phiêu lưu" },
            new Genre { Name = "Sci-Fi", DisplayName = "Khoa học viễn tưởng", Icon = "🚀", Color = "#2196F3", Description = "Công nghệ và tương lai" },
            new Genre { Name = "Mystery", DisplayName = "Bí ẩn", Icon = "🔍", Color = "#795548", Description = "Giải mã những điều bí ẩn" },
            new Genre { Name = "Romance", DisplayName = "Lãng mạn", Icon = "💖", Color = "#E91E63", Description = "Tình yêu và cảm xúc" },
            new Genre { Name = "Horror", DisplayName = "Kinh dị", Icon = "👻", Color = "#424242", Description = "Hồi hộp và sợ hãi" },
            new Genre { Name = "Adventure", DisplayName = "Mạo hiểm", Icon = "⛰️", Color = "#FF9800", Description = "Hành trình khám phá" },
            new Genre { Name = "Comedy", DisplayName = "Hài hước", Icon = "😄", Color = "#FFEB3B", Description = "Vui vẻ và giải trí" },
            new Genre { Name = "Drama", DisplayName = "Tâm lý", Icon = "🎭", Color = "#607D8B", Description = "Cảm xúc sâu sắc" },
            new Genre { Name = "Thriller", DisplayName = "Ly kỳ", Icon = "⚡", Color = "#F44336", Description = "Căng thẳng và hồi hộp" },
            new Genre { Name = "Historical Fiction", DisplayName = "Lịch sử", Icon = "🏛️", Color = "#8BC34A", Description = "Câu chuyện quá khứ" }
        };

        // Enhanced AgeGroup collection with icons and colors
        public ObservableCollection<AgeGroup> AgeGroups { get; } = new()
        {
            new AgeGroup { Name = "Children", DisplayName = "Trẻ em", Icon = "👶", Color = "#FF9800", Description = "Câu chuyện đơn giản và vui nhộn", AgeRange = "3-7 tuổi" },
            new AgeGroup { Name = "Pre-teen", DisplayName = "Tiểu học", Icon = "🧒", Color = "#4CAF50", Description = "Khám phá và học hỏi", AgeRange = "8-12 tuổi" },
            new AgeGroup { Name = "Teenager", DisplayName = "Thanh thiếu niên", Icon = "👦", Color = "#2196F3", Description = "Phiêu lưu và tình bạn", AgeRange = "13-17 tuổi" },
            new AgeGroup { Name = "Young Adult", DisplayName = "Người trẻ", Icon = "👨", Color = "#9C27B0", Description = "Tình yêu và sự nghiệp", AgeRange = "18-25 tuổi" },
            new AgeGroup { Name = "Adult", DisplayName = "Người lớn", Icon = "👔", Color = "#795548", Description = "Cuộc sống và trải nghiệm", AgeRange = "26+ tuổi" },
            new AgeGroup { Name = "Senior", DisplayName = "Người cao tuổi", Icon = "👴", Color = "#607D8B", Description = "Trí tuệ và kỷ niệm", AgeRange = "60+ tuổi" }
        };

        // Selected values
        [ObservableProperty] Genre selectedGenre;
        [ObservableProperty] AgeGroup selectedAgeGroup;
        [ObservableProperty] bool isGenrePickerVisible;
        [ObservableProperty] bool isAgePickerVisible;

        // Outputs
        [ObservableProperty] string story = "";
        [ObservableProperty] string storyVietnamese = ""; // Thêm bản dịch tiếng Việt
        [ObservableProperty] bool isBusy;
        [ObservableProperty] string errorMessage = "";
        [ObservableProperty] string audioUrl;
        [ObservableProperty] string readStoryURL;

        // New properties for enhanced UI
        [ObservableProperty] bool isStoryGenerated;
        [ObservableProperty] string generationStatus = "";
        [ObservableProperty] double generationProgress;

        // Bilingual display properties
        [ObservableProperty] bool isEnglishVisible = true;
        [ObservableProperty] bool isVietnameseVisible = false;
        [ObservableProperty] bool isBilingualMode = false;
        [ObservableProperty] string currentLanguageMode = "🇬🇧 Tiếng Anh";

        // Thêm property mới cho HTML content
        [ObservableProperty] HtmlWebViewSource htmlStoryContent;

        // NEW PROPERTY: Thêm bilingual story content
        [ObservableProperty] string bilingualStoryContent = "";

        // Helper để binding IsEnabled cho nút Generate
        public bool IsNotBusy => !IsBusy;

        public StoryViewModel()
        {
            Debug.WriteLine("[StoryViewModel] Initialized.");
            SelectedGenre = Genres[0];
            SelectedAgeGroup = AgeGroups[0];
        }

        // Command to show/hide genre picker
        [RelayCommand]
        void ToggleGenrePicker()
        {
            IsGenrePickerVisible = !IsGenrePickerVisible;
        }

        // Command to show/hide age picker
        [RelayCommand]
        void ToggleAgePicker()
        {
            IsAgePickerVisible = !IsAgePickerVisible;
        }

        // Command to select genre
        [RelayCommand]
        void SelectGenre(Genre genre)
        {
            // Unselect all genres
            foreach (var g in Genres)
                g.IsSelected = false;

            // Select the chosen genre
            genre.IsSelected = true;
            SelectedGenre = genre;
            IsGenrePickerVisible = false;

            Debug.WriteLine($"[SelectGenre] Selected: {genre.DisplayName}");
        }

        // Command to select age group
        [RelayCommand]
        void SelectAgeGroup(AgeGroup ageGroup)
        {
            // Unselect all age groups
            foreach (var ag in AgeGroups)
                ag.IsSelected = false;

            // Select the chosen age group
            ageGroup.IsSelected = true;
            SelectedAgeGroup = ageGroup;
            IsAgePickerVisible = false;

            Debug.WriteLine($"[SelectAgeGroup] Selected: {ageGroup.DisplayName}");
        }

        // NEW: Commands for language switching
        [RelayCommand]
        void ToggleLanguageMode()
        {
            if (!IsBilingualMode)
            {
                if (IsEnglishVisible)
                {
                    // Switch to Vietnamese only
                    IsEnglishVisible = false;
                    IsVietnameseVisible = true;
                    CurrentLanguageMode = "🇻🇳 Tiếng Việt";
                }
                else
                {
                    // Switch to Bilingual
                    IsEnglishVisible = true;
                    IsVietnameseVisible = true;
                    IsBilingualMode = true;
                    CurrentLanguageMode = "🌐 Song ngữ";
                }
            }
            else
            {
                // Switch back to English only
                IsEnglishVisible = true;
                IsVietnameseVisible = false;
                IsBilingualMode = false;
                CurrentLanguageMode = "🇬🇧 Tiếng Anh";
            }

            // Update HTML display
            CreateHtmlStoryContent();
        }

        // 1. Generate Story, Vietnamese Translation & Audio with progress updates
        [RelayCommand(CanExecute = nameof(CanGenerate))]
        async Task GenerateAllAsync()
        {
            Debug.WriteLine("[GenerateAllAsync] Starting with bilingual approach...");
            try
            {
                IsBusy = true;
                IsStoryGenerated = false;
                GenerationProgress = 0;
                Story = "";
                StoryVietnamese = "";
                BilingualStoryContent = "";
                ErrorMessage = "";
                AudioUrl = null;
                ReadStoryURL = null;

                // Step 1: Generate Bilingual Story using ChatGPT-42 API (ONE CALL)
                GenerationStatus = "🌐 Đang tạo câu chuyện song ngữ...";
                GenerationProgress = 0.33;

                var bilingualPrompt = BuildBilingualPrompt();
                Debug.WriteLine($"[GenerateAllAsync] Bilingual Prompt: {bilingualPrompt}");
                BilingualStoryContent = await CallChatGptApiAsync(bilingualPrompt);

                // Step 2: Parse bilingual content to extract English and Vietnamese
                GenerationStatus = "📝 Đang phân tích nội dung song ngữ...";
                GenerationProgress = 0.66;
                ParseBilingualContent(BilingualStoryContent);

                IsStoryGenerated = true;

                // Step 3: Generate Audio for English version
                GenerationStatus = "🎵 Đang tạo âm thanh cho phần tiếng Anh...";
                if (!string.IsNullOrWhiteSpace(Story))
                {
                    await ReadStoryAsync();
                }

                GenerationStatus = "✨ Hoàn thành!";
                GenerationProgress = 1.0;

                // Clear status after delay
                await Task.Delay(2000);
                GenerationStatus = "";
                GenerationProgress = 0;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Lỗi: {ex.Message}";
                Debug.WriteLine($"[GenerateAllAsync] Error: {ex.Message} - StackTrace: {ex.StackTrace}");
                Story = "";
                StoryVietnamese = "";
                BilingualStoryContent = "";
                AudioUrl = null;
                ReadStoryURL = null;
                IsStoryGenerated = false;
                await Application.Current.MainPage.DisplayAlert("Lỗi", ErrorMessage, "OK");
            }
            finally
            {
                IsBusy = false;
                GenerationStatus = "";
                GenerationProgress = 0;
                Debug.WriteLine("[GenerateAllAsync] Set IsBusy to false.");
                GenerateAllCommand.NotifyCanExecuteChanged();
                ReadStoryCommand.NotifyCanExecuteChanged();
            }
        }

        // ChatGPT-42 API call - COMPLETED METHOD
        async Task<string> CallChatGptApiAsync(string prompt)
        {
            Debug.WriteLine("[CallChatGptApiAsync] Starting...");
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(60);

            var req = new HttpRequestMessage(HttpMethod.Post, ChatGptApiUrl);
            req.Headers.Add("x-rapidapi-key", RapidApiKey);
            req.Headers.Add("x-rapidapi-host", "chatgpt-42.p.rapidapi.com");

            var payload = new
            {
                messages = new[]
                {
                    new
                    {
                        role = "user",
                        content = prompt
                    }
                },
                model = "gpt-4o-mini"
            };

            req.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            using var res = await client.SendAsync(req);
            var body = await res.Content.ReadAsStringAsync();
            Debug.WriteLine($"[CallChatGptApiAsync] Response Status: {res.StatusCode}");
            Debug.WriteLine($"[CallChatGptApiAsync] Response Body: {body}");

            // Handle specific error codes
            switch (res.StatusCode)
            {
                case System.Net.HttpStatusCode.Unauthorized:
                    throw new InvalidOperationException("API key không hợp lệ. Vui lòng kiểm tra lại.");

                case System.Net.HttpStatusCode.TooManyRequests:
                    throw new InvalidOperationException("Đã vượt quá giới hạn request. Vui lòng thử lại sau.");

                case System.Net.HttpStatusCode.BadRequest:
                    throw new InvalidOperationException("Request không hợp lệ. Vui lòng kiểm tra lại prompt.");

                case System.Net.HttpStatusCode.InternalServerError:
                    throw new InvalidOperationException("Lỗi server ChatGPT. Vui lòng thử lại sau.");

                case System.Net.HttpStatusCode.ServiceUnavailable:
                    throw new InvalidOperationException("Dịch vụ ChatGPT tạm thời không khả dụng. Vui lòng thử lại sau.");
            }

            res.EnsureSuccessStatusCode();

            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            if (!root.TryGetProperty("choices", out var choicesEl) || choicesEl.GetArrayLength() == 0)
            {
                throw new KeyNotFoundException("Không tìm thấy 'choices' trong phản hồi ChatGPT");
            }

            var firstChoice = choicesEl[0];
            if (!firstChoice.TryGetProperty("message", out var messageEl))
            {
                throw new KeyNotFoundException("Không tìm thấy 'message' trong choice");
            }

            if (!messageEl.TryGetProperty("content", out var contentEl))
            {
                throw new KeyNotFoundException("Không tìm thấy 'content' trong message");
            }

            var result = contentEl.GetString() ?? "";
            Debug.WriteLine($"[CallChatGptApiAsync] Result length: {result.Length}");
            return result;
        }

        // Build prompt for bilingual story generation - UPDATED TO EXCLUDE EMOJIS FROM ENGLISH
        string BuildBilingualPrompt()
        {
            var ageGroup = SelectedAgeGroup?.Name ?? "Children";

            var prompt = $"Create an engaging {SelectedGenre?.Name} story suitable for {ageGroup} in bilingual format (English + Vietnamese).\n\n" +
                        "IMPORTANT FORMAT REQUIREMENTS:\n" +
                        "- Write each English sentence followed immediately by its Vietnamese translation\n" +
                        "- Use this exact format for each sentence:\n" +
                        "  EN: [English sentence - NO EMOJIS]\n" +
                        "  VI: [Vietnamese translation - with emojis for engagement]\n" +
                        "- Include a bilingual title at the beginning\n" +
                        "- Length: 150-300 words total\n" +
                        "- Include dialogue if appropriate\n" +
                        "- Use descriptive language\n" +
                        "- Make it educational and entertaining\n" +
                        "- IMPORTANT: Only add emojis to Vietnamese lines (VI:), keep English lines (EN:) clean and emoji-free for text-to-speech\n\n" +
                        "EXAMPLE FORMAT:\n" +
                        "EN: The Magic Forest\n" +
                        "VI: Khu Rừng Phép Thuật 🌲✨\n\n" +
                        "EN: Once upon a time, there was a little girl named Lucy who loved adventures.\n" +
                        "VI: Ngày xửa ngày xưa, có một cô bé tên Lucy rất thích phiêu lưu. 🧚‍♀️\n\n" +
                        "EN: She discovered a magical forest behind her house.\n" +
                        "VI: Cô bé đã khám phá ra một khu rừng thần kỳ phía sau nhà mình. 🏠🌳\n\n" +
                        "Please follow this exact format. Keep English lines simple and clean for audio processing. Return only the story content, no additional comments.";

            Debug.WriteLine($"[BuildBilingualPrompt] Generated Bilingual Prompt: {prompt}");
            return prompt;
        }

        // Parse bilingual content to separate English and Vietnamese - FIXED ALIGNMENT
        private void ParseBilingualContent(string bilingualContent)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(bilingualContent))
                {
                    Debug.WriteLine("[ParseBilingualContent] Bilingual content is empty");
                    return;
                }

                var englishLines = new List<string>();
                var vietnameseLines = new List<string>();
                var lines = bilingualContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);

                string englishTitle = "";
                string vietnameseTitle = "";
                bool titleProcessed = false;

                for (int i = 0; i < lines.Length; i++)
                {
                    var trimmedLine = lines[i].Trim();

                    if (trimmedLine.StartsWith("EN:", StringComparison.OrdinalIgnoreCase))
                    {
                        var englishText = trimmedLine.Substring(3).Trim();

                        // First EN line is always the title
                        if (!titleProcessed && string.IsNullOrEmpty(englishTitle))
                        {
                            englishTitle = englishText;

                            // Look for the corresponding VI title on the next line
                            if (i + 1 < lines.Length)
                            {
                                var nextLine = lines[i + 1].Trim();
                                if (nextLine.StartsWith("VI:", StringComparison.OrdinalIgnoreCase))
                                {
                                    vietnameseTitle = nextLine.Substring(3).Trim();
                                    titleProcessed = true;
                                    continue; // Skip to next iteration to avoid processing VI title again
                                }
                            }
                        }
                        else if (titleProcessed)
                        {
                            englishLines.Add(englishText);
                        }
                    }
                    else if (trimmedLine.StartsWith("VI:", StringComparison.OrdinalIgnoreCase))
                    {
                        var vietnameseText = trimmedLine.Substring(3).Trim();

                        // Only add Vietnamese content if we've already processed the title
                        // (This prevents the title VI line from being processed again)
                        if (titleProcessed)
                        {
                            vietnameseLines.Add(vietnameseText);
                        }
                    }
                }

                // Combine title and content for English
                var englishContent = new List<string>();
                if (!string.IsNullOrEmpty(englishTitle))
                    englishContent.Add(englishTitle);
                englishContent.AddRange(englishLines);

                // Combine title and content for Vietnamese
                var vietnameseContent = new List<string>();
                if (!string.IsNullOrEmpty(vietnameseTitle))
                    vietnameseContent.Add(vietnameseTitle);
                vietnameseContent.AddRange(vietnameseLines);

                Story = string.Join("\n", englishContent);
                StoryVietnamese = string.Join("\n", vietnameseContent);

                Debug.WriteLine($"[ParseBilingualContent] English Title: '{englishTitle}'");
                Debug.WriteLine($"[ParseBilingualContent] Vietnamese Title: '{vietnameseTitle}'");
                Debug.WriteLine($"[ParseBilingualContent] Extracted English ({Story.Length} chars): {Story.Substring(0, Math.Min(100, Story.Length))}...");
                Debug.WriteLine($"[ParseBilingualContent] Extracted Vietnamese ({StoryVietnamese.Length} chars): {StoryVietnamese.Substring(0, Math.Min(100, StoryVietnamese.Length))}...");
                Debug.WriteLine($"[ParseBilingualContent] English lines: {englishLines.Count}, Vietnamese lines: {vietnameseLines.Count}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ParseBilingualContent] Error: {ex.Message}");
                // Fallback: use original content as English
                Story = bilingualContent;
                StoryVietnamese = "Không thể phân tích nội dung song ngữ. Vui lòng thử lại.";
            }
        }

        // Extract only English sentences for TTS (improved version)
        private string ExtractEnglishForTts(string story)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(story))
                    return "";

                // If the story is already in English-only format (parsed), use it directly
                // Otherwise, extract EN: lines from bilingual format
                if (!story.Contains("EN:") && !story.Contains("VI:"))
                {
                    // Already parsed English content
                    return CleanTextForTts(story);
                }

                // Extract English lines from bilingual format
                var englishSentences = new List<string>();
                var lines = story.Split('\n', StringSplitOptions.RemoveEmptyEntries);

                foreach (var line in lines)
                {
                    var trimmedLine = line.Trim();
                    if (trimmedLine.StartsWith("EN:", StringComparison.OrdinalIgnoreCase))
                    {
                        var englishText = trimmedLine.Substring(3).Trim();
                        if (!string.IsNullOrEmpty(englishText))
                        {
                            englishSentences.Add(englishText);
                        }
                    }
                }

                var extractedEnglish = string.Join(" ", englishSentences);
                Debug.WriteLine($"[ExtractEnglishForTts] Extracted {englishSentences.Count} English sentences, total length: {extractedEnglish.Length}");

                return CleanTextForTts(extractedEnglish);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ExtractEnglishForTts] Error: {ex.Message}");
                return CleanTextForTts(story); // Fallback to original cleaning
            }
        }

        // Create TTS job and return job ID (SINGLE VERSION - REMOVED DUPLICATE)
        private async Task<string> CreateTtsJobAsync()
        {
            Debug.WriteLine("[CreateTtsJobAsync] Starting with enhanced text cleaning...");

            // Extract and clean English text specifically for TTS
            var englishForTts = ExtractEnglishForTts(Story);

            // Additional validation
            if (!IsTextSafeForTts(englishForTts))
            {
                Debug.WriteLine("[CreateTtsJobAsync] Text failed safety check, applying additional cleaning...");
                englishForTts = Regex.Replace(englishForTts, @"[^\x00-\x7F]", ""); // Remove all non-ASCII
                englishForTts = Regex.Replace(englishForTts, @"[^\w\s\.,!?;:\-()]", ""); // Extra cleaning
                englishForTts = Regex.Replace(englishForTts, @"\s+", " ").Trim();

                if (englishForTts.Length < 10)
                {
                    englishForTts = "This is a sample story for text to speech conversion.";
                }
            }

            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(60);

            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(TtsApiUrl),
                Headers =
                {
                    { "x-rapidapi-key", RapidApiKey },
                    { "x-rapidapi-host", "large-text-to-speech.p.rapidapi.com" },
                },
                Content = new StringContent(JsonSerializer.Serialize(new { text = englishForTts }))
                {
                    Headers =
                    {
                        ContentType = new MediaTypeHeaderValue("application/json")
                    }
                }
            };

            Debug.WriteLine($"[CreateTtsJobAsync] Final TTS text length: {englishForTts.Length}");
            Debug.WriteLine($"[CreateTtsJobAsync] Final TTS text: {englishForTts}");

            using var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var body = await response.Content.ReadAsStringAsync();
            Debug.WriteLine($"[CreateTtsJobAsync] Response Body: {body}");

            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            if (root.TryGetProperty("id", out var idElement))
            {
                var jobId = idElement.GetString();
                if (string.IsNullOrEmpty(jobId))
                {
                    throw new InvalidOperationException("Job ID is null or empty");
                }
                return jobId;
            }
            else
            {
                throw new InvalidOperationException("Không tìm thấy job ID trong phản hồi");
            }
        }

        // UPDATED: More lenient validation since English should be clean
        private bool IsTextSafeForTts(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            // Check length limits
            if (text.Length < 10 || text.Length > 5000)
            {
                Debug.WriteLine($"[IsTextSafeForTts] Warning: Text length {text.Length} is outside safe range");
                return false;
            }

            // Check if text contains actual words (not just punctuation)
            var wordCount = Regex.Matches(text, @"\b\w+\b").Count;
            if (wordCount < 3)
            {
                Debug.WriteLine($"[IsTextSafeForTts] Warning: Text contains only {wordCount} words");
                return false;
            }
            return true;
        }

        // SIMPLIFIED: Text cleaning method with basic approach (no complex Unicode handling)
        private string CleanTextForTts(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "";

            Debug.WriteLine($"[CleanTextForTts] Original text length: {text.Length}");
            Debug.WriteLine($"[CleanTextForTts] Original text preview: {text.Substring(0, Math.Min(200, text.Length))}...");

            var cleaned = text;

            // Step 1: Basic cleaning only - since English should be emoji-free from API
            cleaned = cleaned
                .Replace("\n", " ")   // Line breaks to spaces
                .Replace("\r", " ")   // Carriage returns to spaces
                .Replace("\t", " ");  // Tabs to spaces

            // Step 2: Remove any remaining problematic characters using Regex
            cleaned = Regex.Replace(cleaned, @"[^\x00-\x7F]", ""); // Keep only ASCII
            cleaned = Regex.Replace(cleaned, @"[^\w\s\.,!?;:\-()']", ""); // Keep basic punctuation
            cleaned = Regex.Replace(cleaned, @"\s+", " ").Trim(); // Clean up spaces

            Debug.WriteLine($"[CleanTextForTts] Final cleaned length: {cleaned.Length}");
            Debug.WriteLine($"[CleanTextForTts] Final text preview: {(cleaned.Length > 200 ? cleaned.Substring(0, 200) : cleaned)}...");

            // Final validation
            if (string.IsNullOrWhiteSpace(cleaned))
            {
                Debug.WriteLine("[CleanTextForTts] ERROR: Cleaned text is empty!");
                return "This is a story about a magical adventure for children.";
            }

            if (cleaned.Length < 10)
            {
                Debug.WriteLine($"[CleanTextForTts] WARNING: Cleaned text is very short: '{cleaned}'");
                return "This is a story about a magical adventure for children.";
            }

            return cleaned;
        }

        // Poll TTS job until completion and return audio URL
        private async Task<string> PollTtsJobAsync(string jobId)
        {
            Debug.WriteLine($"[PollTtsJobAsync] Starting polling for job ID: {jobId}");

            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(30);

            var maxAttempts = 30; // Maximum 30 attempts (about 5 minutes with 10s delays)
            var delay = TimeSpan.FromSeconds(10);

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    var request = new HttpRequestMessage
                    {
                        Method = HttpMethod.Get,
                        RequestUri = new Uri($"{TtsApiUrl}?id={jobId}"),
                        Headers =
                        {
                            { "x-rapidapi-key", RapidApiKey },
                            { "x-rapidapi-host", "large-text-to-speech.p.rapidapi.com" },
                        },
                    };

                    Debug.WriteLine($"[PollTtsJobAsync] Attempt {attempt}: Checking job status...");
                    using var response = await client.SendAsync(request);
                    response.EnsureSuccessStatusCode();

                    var body = await response.Content.ReadAsStringAsync();
                    Debug.WriteLine($"[PollTtsJobAsync] Response Body: {body}");

                    using var doc = JsonDocument.Parse(body);
                    var root = doc.RootElement;

                    if (root.TryGetProperty("status", out var statusElement))
                    {
                        var status = statusElement.GetString();
                        Debug.WriteLine($"[PollTtsJobAsync] Job status: {status}");

                        if (status == "success")
                        {
                            if (root.TryGetProperty("url", out var urlElement))
                            {
                                var audioUrl = urlElement.GetString();
                                if (string.IsNullOrEmpty(audioUrl))
                                {
                                    throw new InvalidOperationException("Audio URL is null or empty");
                                }
                                Debug.WriteLine($"[PollTtsJobAsync] Success! Audio URL: {audioUrl}");
                                return audioUrl;
                            }
                            else
                            {
                                throw new InvalidOperationException("Không tìm thấy URL audio trong phản hồi success");
                            }
                        }
                        else if (status == "processing")
                        {
                            Debug.WriteLine($"[PollTtsJobAsync] Job still processing, waiting {delay.TotalSeconds}s before next attempt...");
                            await Task.Delay(delay);
                            continue;
                        }
                        else
                        {
                            throw new InvalidOperationException($"TTS job failed with status: {status}");
                        }
                    }
                    else
                    {
                        throw new InvalidOperationException("Không tìm thấy status trong phản hồi");
                    }
                }
                catch (Exception ex) when (attempt < maxAttempts)
                {
                    Debug.WriteLine($"[PollTtsJobAsync] Attempt {attempt} failed: {ex.Message}, retrying...");
                    await Task.Delay(delay);
                    continue;
                }
            }

            throw new InvalidOperationException($"TTS job timed out after {maxAttempts} attempts");
        }

        // Enhanced prompt building with better structure (English only)
        string BuildPrompt()
        {
            var ageGroup = SelectedAgeGroup?.Name ?? "Children";

            var prompt = $"Create an engaging {SelectedGenre?.Name} story suitable for {ageGroup}. " +
                        "Write the story in English.\n\n" +
                        "Requirements:\n" +
                        "- Length: 150-300 words\n" +
                        "- Include dialogue if appropriate\n" +
                        "- Use descriptive language\n" +
                        "- Make it educational and entertaining\n" +
                        "- Include emojis to make it more engaging\n" +
                        "- Structure: Title, story content\n" +
                        "- Ensure the story has a clear beginning, middle, and end\n\n" +
                        "Just return the story content with title, no additional questions or comments.";

            Debug.WriteLine($"[BuildPrompt] Generated Prompt: {prompt}");
            return prompt;
        }

        // Enhanced validation
        private bool CanGenerate()
        {
            bool isValid = !IsBusy &&
                          SelectedGenre != null &&
                          SelectedAgeGroup != null;
            Debug.WriteLine($"[CanGenerate] IsValid: {isValid}");
            return isValid;
        }

        bool CanRead() => !IsBusy && !string.IsNullOrWhiteSpace(Story);

        // Property change handlers
        partial void OnIsBusyChanged(bool oldValue, bool newValue)
        {
            Debug.WriteLine($"[OnIsBusyChanged] Changed from {oldValue} to {newValue}");
            OnPropertyChanged(nameof(IsNotBusy));
            GenerateAllCommand.NotifyCanExecuteChanged();
            ReadStoryCommand.NotifyCanExecuteChanged();
        }

        partial void OnStoryChanged(string oldValue, string newValue)
        {
            Debug.WriteLine($"[OnStoryChanged] Changed from '{oldValue}' to '{newValue}'");
            ReadStoryCommand.NotifyCanExecuteChanged();
            IsStoryGenerated = !string.IsNullOrWhiteSpace(newValue);

            // Tạo HTML content
            CreateHtmlStoryContent();
        }

        partial void OnStoryVietnameseChanged(string oldValue, string newValue)
        {
            // Cập nhật HTML khi có bản dịch mới
            CreateHtmlStoryContent();
        }

        // Create HTML content for WebView display with bilingual support   
        private void CreateHtmlStoryContent()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(Story) && string.IsNullOrWhiteSpace(StoryVietnamese))
                {
                    HtmlStoryContent = null;
                    return;
                }

                var htmlBuilder = new StringBuilder();
                htmlBuilder.Append("<!DOCTYPE html>\n");
                htmlBuilder.Append("<html>\n");
                htmlBuilder.Append("<head>\n");
                htmlBuilder.Append("    <meta name='viewport' content='width=device-width, initial-scale=1.0'>\n");
                htmlBuilder.Append("    <style>\n");
                htmlBuilder.Append("        body {\n");
                htmlBuilder.Append("            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;\n");
                htmlBuilder.Append("            line-height: 1.6;\n");
                htmlBuilder.Append("            margin: 15px;\n");
                htmlBuilder.Append("            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);\n");
                htmlBuilder.Append("            color: #333;\n");
                htmlBuilder.Append("            min-height: 100vh;\n");
                htmlBuilder.Append("        }\n");
                htmlBuilder.Append("        .story-container {\n");
                htmlBuilder.Append("            background: white;\n");
                htmlBuilder.Append("            border-radius: 15px;\n");
                htmlBuilder.Append("            padding: 20px;\n");
                htmlBuilder.Append("            box-shadow: 0 8px 32px rgba(0,0,0,0.1);\n");
                htmlBuilder.Append("            margin-bottom: 20px;\n");
                htmlBuilder.Append("        }\n");
                htmlBuilder.Append("        .bilingual-row {\n");
                htmlBuilder.Append("            margin-bottom: 15px;\n");
                htmlBuilder.Append("            border-radius: 8px;\n");
                htmlBuilder.Append("            overflow: hidden;\n");
                htmlBuilder.Append("            box-shadow: 0 2px 4px rgba(0,0,0,0.1);\n");
                htmlBuilder.Append("        }\n");
                htmlBuilder.Append("        .english-line {\n");
                htmlBuilder.Append("            background: #e3f2fd;\n");
                htmlBuilder.Append("            padding: 10px 15px;\n");
                htmlBuilder.Append("            border-left: 4px solid #2196F3;\n");
                htmlBuilder.Append("            font-size: 15px;\n");
                htmlBuilder.Append("            line-height: 1.5;\n");
                htmlBuilder.Append("        }\n");
                htmlBuilder.Append("        .vietnamese-line {\n");
                htmlBuilder.Append("            background: #fff3e0;\n");
                htmlBuilder.Append("            padding: 10px 15px;\n");
                htmlBuilder.Append("            border-left: 4px solid #FF9800;\n");
                htmlBuilder.Append("            font-size: 15px;\n");
                htmlBuilder.Append("            line-height: 1.5;\n");
                htmlBuilder.Append("            font-style: italic;\n");
                htmlBuilder.Append("        }\n");
                htmlBuilder.Append("        .story-title {\n");
                htmlBuilder.Append("            font-size: 22px;\n");
                htmlBuilder.Append("            font-weight: bold;\n");
                htmlBuilder.Append("            color: #4a5568;\n");
                htmlBuilder.Append("            text-align: center;\n");
                htmlBuilder.Append("            margin-bottom: 20px;\n");
                htmlBuilder.Append("            border-bottom: 2px solid #e2e8f0;\n");
                htmlBuilder.Append("            padding-bottom: 10px;\n");
                htmlBuilder.Append("        }\n");
                htmlBuilder.Append("        .language-label {\n");
                htmlBuilder.Append("            font-size: 12px;\n");
                htmlBuilder.Append("            font-weight: bold;\n");
                htmlBuilder.Append("            opacity: 0.7;\n");
                htmlBuilder.Append("            margin-bottom: 5px;\n");
                htmlBuilder.Append("        }\n");
                htmlBuilder.Append("        .genre-badge {\n");
                htmlBuilder.Append("            display: inline-block;\n");
                htmlBuilder.Append("            background: #667eea;\n");
                htmlBuilder.Append("            color: white;\n");
                htmlBuilder.Append("            padding: 5px 15px;\n");
                htmlBuilder.Append("            border-radius: 20px;\n");
                htmlBuilder.Append("            font-size: 12px;\n");
                htmlBuilder.Append("            margin-bottom: 15px;\n");
                htmlBuilder.Append("        }\n");
                htmlBuilder.Append("        @media (max-width: 600px) {\n");
                htmlBuilder.Append("            body { margin: 10px; }\n");
                htmlBuilder.Append("            .story-container { padding: 15px; }\n");
                htmlBuilder.Append("            .english-line, .vietnamese-line { font-size: 14px; }\n");
                htmlBuilder.Append("        }\n");
                htmlBuilder.Append("    </style>\n");
                htmlBuilder.Append("</head>\n");
                htmlBuilder.Append("<body>\n");
                htmlBuilder.Append("    <div class='story-container'>\n");

                // Add genre badge
                if (SelectedGenre != null)
                {
                    htmlBuilder.Append($"<div class='genre-badge'>{SelectedGenre.Icon} {SelectedGenre.DisplayName}</div>\n");
                }

                // Process the bilingual content to show paired sentences
                if (!string.IsNullOrWhiteSpace(BilingualStoryContent))
                {
                    ProcessBilingualContentForDisplay(htmlBuilder, BilingualStoryContent);
                }

                htmlBuilder.Append("    </div>\n");
                htmlBuilder.Append("</body>\n");
                htmlBuilder.Append("</html>");

                HtmlStoryContent = new HtmlWebViewSource
                {
                    Html = htmlBuilder.ToString()
                };

                Debug.WriteLine("[CreateHtmlStoryContent] Fixed bilingual HTML content created successfully");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CreateHtmlStoryContent] Error: {ex.Message}");
                HtmlStoryContent = null;
            }
        }

        // Process bilingual content for HTML display
        private void ProcessBilingualContentForDisplay(StringBuilder htmlBuilder, string bilingualContent)
        {
            try
            {
                var lines = bilingualContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                bool titleProcessed = false;

                for (int i = 0; i < lines.Length; i++)
                {
                    var trimmedLine = lines[i].Trim();

                    if (trimmedLine.StartsWith("EN:", StringComparison.OrdinalIgnoreCase))
                    {
                        var englishText = trimmedLine.Substring(3).Trim();
                        string vietnameseText = "";

                        // Look for the corresponding VI line
                        if (i + 1 < lines.Length)
                        {
                            var nextLine = lines[i + 1].Trim();
                            if (nextLine.StartsWith("VI:", StringComparison.OrdinalIgnoreCase))
                            {
                                vietnameseText = nextLine.Substring(3).Trim();
                                i++; // Skip the VI line in next iteration
                            }
                        }

                        // First pair is the title
                        if (!titleProcessed)
                        {
                            htmlBuilder.Append($"<div class='story-title'>{System.Net.WebUtility.HtmlEncode(englishText)}</div>\n");
                            titleProcessed = true;
                        }
                        else
                        {
                            // Regular content pairs
                            htmlBuilder.Append($"        <div class='bilingual-row'>\n");

                            // English line
                            htmlBuilder.Append($"            <div class='english-line'>\n");
                            htmlBuilder.Append("                <div class='language-label'>🇬🇧 English</div>\n");
                            htmlBuilder.Append($"                {System.Net.WebUtility.HtmlEncode(englishText)}\n");
                            htmlBuilder.Append("            </div>\n");

                            // Vietnamese line
                            if (!string.IsNullOrEmpty(vietnameseText))
                            {
                                htmlBuilder.Append($"            <div class='vietnamese-line'>\n");
                                htmlBuilder.Append("                <div class='language-label'>🇻🇳 Tiếng Việt</div>\n");
                                htmlBuilder.Append($"                {System.Net.WebUtility.HtmlEncode(vietnameseText)}\n");
                                htmlBuilder.Append("            </div>\n");
                            }

                            htmlBuilder.Append("        </div>\n");
                        }
                    }
                }

                Debug.WriteLine($"[ProcessBilingualContentForDisplay] Processed bilingual content successfully");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ProcessBilingualContentForDisplay] Error: {ex.Message}");
                // Fallback: display raw content
                htmlBuilder.Append($"<div class='bilingual-row'><div class='english-line'>{System.Net.WebUtility.HtmlEncode(bilingualContent)}</div></div>");
            }
        }

        // 2. Read Aloud using new TTS API (two-step process) - FIXED MISSING METHOD
        [RelayCommand(CanExecute = nameof(CanRead))]
        async Task ReadStoryAsync()
        {
            Debug.WriteLine("[ReadStoryAsync] Starting...");
            if (string.IsNullOrWhiteSpace(Story))
            {
                Debug.WriteLine("[ReadStoryAsync] Story is empty or null, skipping.");
                return;
            }

            // Nếu chưa có AudioUrl, gọi API để tạo
            if (string.IsNullOrWhiteSpace(AudioUrl))
            {
                try
                {
                    // Step 1: Create TTS job
                    string jobId = await CreateTtsJobAsync();
                    Debug.WriteLine($"[ReadStoryAsync] TTS Job created with ID: {jobId}");

                    // Step 2: Poll for result
                    string audioUrl = await PollTtsJobAsync(jobId);

                    AudioUrl = audioUrl;
                    ReadStoryURL = AudioUrl;
                    Debug.WriteLine($"[TTS] Audio URL assigned: {AudioUrl}, ReadStoryURL: {ReadStoryURL}");
                }
                catch (Exception ex)
                {
                    ErrorMessage = $"Lỗi đọc to: {ex.Message}";
                    Debug.WriteLine($"[ReadStoryAsync] Error: {ex.Message} - StackTrace: {ex.StackTrace}");
                    AudioUrl = null;
                    ReadStoryURL = null;
                    await Application.Current.MainPage.DisplayAlert("Lỗi", ErrorMessage, "OK");
                    return;
                }
            }
        }
    }
}