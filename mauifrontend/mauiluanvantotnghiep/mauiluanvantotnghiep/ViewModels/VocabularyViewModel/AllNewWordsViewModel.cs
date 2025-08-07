using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using mauiluanvantotnghiep.Models;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.Json;

namespace mauiluanvantotnghiep.ViewModels
{
    public partial class AllNewWordsViewModel : ObservableObject
    {
        private List<Vocabulary> _allVocabularies = new();
        private const int PageSize = 10;

        public ObservableCollection<Vocabulary> AllNewVocabularies { get; } = new();

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private string generalError;

        // Search properties
        [ObservableProperty]
        private string searchText = string.Empty;

        // Pagination properties
        [ObservableProperty]
        private int currentPage = 1;

        [ObservableProperty]
        private int totalPages = 1;

        [ObservableProperty]
        private bool canGoToPreviousPage = false;

        [ObservableProperty]
        private bool canGoToNextPage = false;

        [ObservableProperty]
        private string pageInfo = "1 / 1";

        [ObservableProperty]
        private bool hasVocabularies = false;

        [ObservableProperty]
        private bool hasNoVocabularies = false;

        public AllNewWordsViewModel()
        {
            LoadAllNewWordsCommand.Execute(null);
        }

        partial void OnSearchTextChanged(string value)
        {
            CurrentPage = 1;
            ApplyFilterAndPagination();
        }

        [RelayCommand]
        private async Task LoadAllNewWordsAsync()
        {
            if (IsLoading) return;

            try
            {
                IsLoading = true;
                GeneralError = string.Empty;
                AllNewVocabularies.Clear();

                using var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (req, cert, chain, errors) => true
                };
                using var client = new HttpClient(handler);
                
                var url = $"{AppConfig.AppConfig.BaseUrl}/api/QuanLyVocabulary/GetNewVocabulariesThisMonth";
                Debug.WriteLine($"[LoadAllNewWords] URL: {url}");

                var resp = await client.GetAsync(url);
                if (!resp.IsSuccessStatusCode)
                {
                    GeneralError = $"Lỗi server: {resp.StatusCode}";
                    return;
                }

                var json = await resp.Content.ReadAsStringAsync();
                Debug.WriteLine($"[LoadAllNewWords] JSON: {json}");

                var items = JsonSerializer.Deserialize<Vocabulary[]>(
                    json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                ) ?? Array.Empty<Vocabulary>();

                _allVocabularies = items.OrderByDescending(v => v.CreatedAt).ToList();
                CurrentPage = 1;
                ApplyFilterAndPagination();

                Debug.WriteLine($"[LoadAllNewWords] Successfully loaded {_allVocabularies.Count} new vocabularies");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LoadAllNewWords] Exception: {ex}");
                GeneralError = $"Lỗi: {ex.Message}";
                HasNoVocabularies = true;
                HasVocabularies = false;
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ApplyFilterAndPagination()
        {
            try
            {
                // Filter vocabularies based on search text
                var filteredVocabularies = _allVocabularies.AsEnumerable();

                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    var searchLower = SearchText.ToLower();
                    filteredVocabularies = filteredVocabularies.Where(v =>
                        (!string.IsNullOrEmpty(v.Word) && v.Word.ToLower().Contains(searchLower)) ||
                        (!string.IsNullOrEmpty(v.Pronunciation) && v.Pronunciation.ToLower().Contains(searchLower))
                    );
                }

                var filteredList = filteredVocabularies.ToList();

                // Calculate pagination
                TotalPages = (int)Math.Ceiling((double)filteredList.Count / PageSize);
                if (TotalPages == 0) TotalPages = 1;

                // Ensure current page is valid
                if (CurrentPage > TotalPages) CurrentPage = TotalPages;
                if (CurrentPage < 1) CurrentPage = 1;

                // Get vocabularies for current page
                var vocabulariesForPage = filteredList
                    .Skip((CurrentPage - 1) * PageSize)
                    .Take(PageSize)
                    .ToList();

                // Update UI
                AllNewVocabularies.Clear();
                foreach (var vocab in vocabulariesForPage)
                {
                    AllNewVocabularies.Add(vocab);
                    Debug.WriteLine($"[ApplyFilterAndPagination] Added to page: {vocab.Word}, ID: {vocab.VocabularyId}");
                }

                // Update states
                HasVocabularies = vocabulariesForPage.Any();
                HasNoVocabularies = !HasVocabularies && !IsLoading;

                UpdatePaginationInfo();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ApplyFilterAndPagination] Exception: {ex}");
            }
        }

        private void UpdatePaginationInfo()
        {
            CanGoToPreviousPage = CurrentPage > 1;
            CanGoToNextPage = CurrentPage < TotalPages;
            PageInfo = $"{CurrentPage} / {TotalPages}";
        }

        [RelayCommand]
        private void GoToPreviousPage()
        {
            if (CanGoToPreviousPage)
            {
                CurrentPage--;
                ApplyFilterAndPagination();
            }
        }

        [RelayCommand]
        private void GoToNextPage()
        {
            if (CanGoToNextPage)
            {
                CurrentPage++;
                ApplyFilterAndPagination();
            }
        }

        [RelayCommand]
        private void GoToFirstPage()
        {
            CurrentPage = 1;
            ApplyFilterAndPagination();
        }

        [RelayCommand]
        private void GoToLastPage()
        {
            CurrentPage = TotalPages;
            ApplyFilterAndPagination();
        }

        [RelayCommand]
        private void ClearSearch()
        {
            SearchText = string.Empty;
        }

        [RelayCommand]
        private async Task VocabularyTappedAsync(Vocabulary vocabulary)
        {
            if (vocabulary != null)
            {
                Debug.WriteLine($"[VocabularyTapped] Selected: {vocabulary.Word}, ID: {vocabulary.VocabularyId}");
                if (vocabulary.VocabularyId > 0)
                {
                    await Shell.Current.GoToAsync($"vocabularydetailpage?vocabularyId={vocabulary.VocabularyId}");
                }
                else
                {
                    Debug.WriteLine("[VocabularyTapped] Invalid VocabularyId");
                }
            }
            else
            {
                Debug.WriteLine("[VocabularyTapped] Vocabulary is null");
            }
        }

        [RelayCommand]
        private async Task GoBackAsync()
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}
