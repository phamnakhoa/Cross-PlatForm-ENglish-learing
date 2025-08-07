using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using mauiluanvantotnghiep.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;


namespace mauiluanvantotnghiep.ViewModels.VocabularyViewModel
{
    public partial class CategoryVocabularyViewModel : ObservableObject
    {
        private List<VocabularyCategory> _allCategories = new();
        private const int PageSize = 6; // 3 rows × 2 columns = 6 items per page

        // Danh sách category hiển thị cho trang hiện tại
        public ObservableCollection<VocabularyCategory> Categories { get; } = new();

        [ObservableProperty]
        private bool isLoadingCategories;

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
        private bool hasCategories = false;

        [ObservableProperty]
        private bool hasNoCategories = false;

        public CategoryVocabularyViewModel()
        {
            // Tải danh sách category khi khởi tạo
            LoadCategoriesCommand.Execute(null);
        }

        partial void OnSearchTextChanged(string value)
        {
            CurrentPage = 1;
            ApplyFilterAndPagination();
        }

        [RelayCommand]
        private async Task LoadCategoriesAsync()
        {
            if (IsLoadingCategories) return;
            try
            {
                IsLoadingCategories = true;
                GeneralError = string.Empty;
                
                using var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (req, cert, chain, errors) => true
                };
                using var client = new HttpClient(handler);
                var url = $"{AppConfig.AppConfig.BaseUrl}/api/QuanLyVocabulary/GetListVocabularyCategory";
                var resp = await client.GetAsync(url);
                if (!resp.IsSuccessStatusCode)
                {
                    GeneralError = $"Lỗi server: {resp.StatusCode}";
                    return;
                }

                var json = await resp.Content.ReadAsStringAsync();
                Debug.WriteLine($"[LoadCategories] JSON: {json}");
                var items = JsonSerializer.Deserialize<VocabularyCategory[]>(
                    json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                ) ?? Array.Empty<VocabularyCategory>();

                var colorHexes = new[]
                   {
                    "#7F55B1", "#9B7EBD", "#F49BAB","#F7CFD8","#A6D6D6","#FFE1E0","#F4F8D3","#8E7DBE"
                };

                var rnd = new Random();
                _allCategories.Clear();

                foreach (var c in items)
                {
                    // Random lấy mã màu từ danh sách
                    var hex = colorHexes[rnd.Next(colorHexes.Length)];

                    // Convert từ HEX sang Color
                    c.BackgroundColor = Color.FromArgb(hex);

                    _allCategories.Add(c);
                }

                CurrentPage = 1;
                ApplyFilterAndPagination();

                Debug.WriteLine($"[LoadCategories] Successfully loaded {_allCategories.Count} categories");

            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LoadCategories] Exception: {ex}");
                GeneralError = $"Lỗi: {ex.Message}";
                HasNoCategories = true;
                HasCategories = false;
            }
            finally
            {
                IsLoadingCategories = false;
            }
        }

        private void ApplyFilterAndPagination()
        {
            try
            {
                // Filter categories based on search text
                var filteredCategories = _allCategories.AsEnumerable();

                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    var searchLower = SearchText.ToLower();
                    filteredCategories = filteredCategories.Where(c =>
                        (!string.IsNullOrEmpty(c.VocabularyCategoryName) && c.VocabularyCategoryName.ToLower().Contains(searchLower))
                    );
                }

                var filteredList = filteredCategories.ToList();

                // Calculate pagination
                TotalPages = (int)Math.Ceiling((double)filteredList.Count / PageSize);
                if (TotalPages == 0) TotalPages = 1;

                // Ensure current page is valid
                if (CurrentPage > TotalPages) CurrentPage = TotalPages;
                if (CurrentPage < 1) CurrentPage = 1;

                // Get categories for current page
                var categoriesForPage = filteredList
                    .Skip((CurrentPage - 1) * PageSize)
                    .Take(PageSize)
                    .ToList();

                // Update UI
                Categories.Clear();
                foreach (var category in categoriesForPage)
                {
                    Categories.Add(category);
                    Debug.WriteLine($"[ApplyFilterAndPagination] Added to page: {category.VocabularyCategoryName}, ID: {category.VocabularyCategoryId}");
                }

                // Update states
                HasCategories = categoriesForPage.Any();
                HasNoCategories = !HasCategories && !IsLoadingCategories;

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
        private async Task NavigateToFlashcardAsync(int categoryId)
        {
            try
            {
                await Shell.Current.GoToAsync($"flashcardpage?categoryId={categoryId}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[NavigateToFlashcard] Exception: {ex}");
                GeneralError = $"Lỗi khi chuyển đến Flashcard: {ex.Message}";
            }
        }
    }
}
