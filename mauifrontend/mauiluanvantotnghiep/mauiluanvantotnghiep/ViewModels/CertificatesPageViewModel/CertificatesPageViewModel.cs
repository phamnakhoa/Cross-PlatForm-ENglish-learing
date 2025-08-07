using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using mauiluanvantotnghiep.Models;
using Microsoft.Maui.Storage;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace mauiluanvantotnghiep.ViewModels.CertificatesPageViewModel
{
    public partial class CertificatesPageViewModel : ObservableObject
    {
        private readonly HttpClient _httpClient;
        private List<CertificateResponse> _allCertificates = new();
        private const int PageSize = 3;

        [ObservableProperty]
        private ObservableCollection<CertificateResponse> certificates = new();

        [ObservableProperty]
        private bool isLoading = false;

        [ObservableProperty]
        private bool hasNoCertificates = false;

        [ObservableProperty]
        private bool hasCertificates = false;

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

        // Search properties
        [ObservableProperty]
        private string searchText = string.Empty;

        [ObservableProperty]
        private string verificationCode = string.Empty;

        [ObservableProperty]
        private bool isVerificationLoading = false;

        [ObservableProperty]
        private bool hasVerificationResult = false;

        [ObservableProperty]
        private VerifyCertificateResponse verificationResult;

        public CertificatesPageViewModel()
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };
            _httpClient = new HttpClient(handler);
        }

        partial void OnSearchTextChanged(string value)
        {
            CurrentPage = 1;
            ApplyFilterAndPagination();
        }

        [RelayCommand]
        public async Task LoadCertificatesAsync()
        {
            try
            {
                IsLoading = true;
                HasNoCertificates = false;
                HasCertificates = false;

                string token = await SecureStorage.GetAsync("auth_token");
                if (string.IsNullOrEmpty(token))
                {
                    await Shell.Current.DisplayAlert("Lỗi", "Bạn cần đăng nhập để xem chứng chỉ.", "OK");
                    return;
                }

                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                string apiUrl = $"{AppConfig.AppConfig.BaseUrl}/api/QuanLyCertificate/GetListCertificatesForUser";
                var response = await _httpClient.GetAsync(apiUrl);

                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var certificateList = JsonSerializer.Deserialize<List<CertificateResponse>>(json, options);

                    if (certificateList != null && certificateList.Any())
                    {
                        // Sort by creation date (newest first) and store all certificates
                        _allCertificates = certificateList.OrderByDescending(c => c.CreatedAt).ToList();
                        CurrentPage = 1;
                        ApplyFilterAndPagination();
                    }
                    else
                    {
                        _allCertificates.Clear();
                        Certificates.Clear();
                        HasNoCertificates = true;
                        HasCertificates = false;
                        UpdatePaginationInfo();
                    }
                }
                else
                {
                    await Shell.Current.DisplayAlert("Lỗi", "Không thể tải danh sách chứng chỉ.", "OK");
                    HasNoCertificates = true;
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Lỗi", $"Đã xảy ra lỗi: {ex.Message}", "OK");
                HasNoCertificates = true;
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
                // Filter certificates based on search text
                var filteredCertificates = _allCertificates.AsEnumerable();

                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    var searchLower = SearchText.ToLower();
                    filteredCertificates = filteredCertificates.Where(c =>
                        (!string.IsNullOrEmpty(c.CourseName) && c.CourseName.ToLower().Contains(searchLower)) ||
                        (!string.IsNullOrEmpty(c.VerificationCode) && c.VerificationCode.ToLower().Contains(searchLower)) ||
                        (!string.IsNullOrEmpty(c.CertificateTypeName) && c.CertificateTypeName.ToLower().Contains(searchLower))
                    );
                }

                var filteredList = filteredCertificates.ToList();

                // Calculate pagination
                TotalPages = (int)Math.Ceiling((double)filteredList.Count / PageSize);
                if (TotalPages == 0) TotalPages = 1;

                // Ensure current page is valid
                if (CurrentPage > TotalPages) CurrentPage = TotalPages;
                if (CurrentPage < 1) CurrentPage = 1;

                // Get certificates for current page
                var certificatesForPage = filteredList
                    .Skip((CurrentPage - 1) * PageSize)
                    .Take(PageSize)
                    .ToList();

                // Update UI
                Certificates.Clear();
                foreach (var cert in certificatesForPage)
                {
                    Certificates.Add(cert);
                }

                // Update states
                HasCertificates = certificatesForPage.Any();
                HasNoCertificates = !HasCertificates && !IsLoading;

                UpdatePaginationInfo();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Filter/Pagination error: {ex.Message}");
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
        private async Task ViewCertificateAsync(CertificateResponse certificate)
        {
            try
            {
                if (certificate == null)
                    return;

                // Navigate to certificate detail page
                await Shell.Current.GoToAsync($"certificatedetailpage?certificateId={certificate.CertificateId}");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Lỗi", $"Không thể mở chứng chỉ: {ex.Message}", "OK");
                System.Diagnostics.Debug.WriteLine($"Navigation error: {ex.Message}");
            }
        }

        [RelayCommand]
        public async Task VerifyCertificateAsync()
        {
            if (string.IsNullOrWhiteSpace(VerificationCode))
            {
                await Shell.Current.DisplayAlert("Lỗi", "Vui lòng nhập mã xác thực.", "OK");
                return;
            }

            try
            {
                IsVerificationLoading = true;
                HasVerificationResult = false;
                VerificationResult = null;

                var request = new VerifyCertificateRequest
                {
                    VerifyCode = VerificationCode.Trim()
                };

                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                string apiUrl = $"{AppConfig.AppConfig.BaseUrl}/api/Certificates/VerifyCertificate";
                var response = await _httpClient.PostAsync(apiUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    string responseJson = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    VerificationResult = JsonSerializer.Deserialize<VerifyCertificateResponse>(responseJson, options);
                    
                    if (VerificationResult != null)
                    {
                        HasVerificationResult = true;
                    }
                }
                else
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    await Shell.Current.DisplayAlert("Không tìm thấy", "Mã xác thực không hợp lệ hoặc chứng chỉ không tồn tại.", "OK");
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Lỗi", $"Đã xảy ra lỗi khi xác thực: {ex.Message}", "OK");
                System.Diagnostics.Debug.WriteLine($"Verification error: {ex.Message}");
            }
            finally
            {
                IsVerificationLoading = false;
            }
        }

        [RelayCommand]
        private async Task ClearVerificationAsync()
        {
            VerificationCode = string.Empty;
            HasVerificationResult = false;
            VerificationResult = null;
        }

        [RelayCommand]
        private async Task PasteFromClipboardAsync()
        {
            try
            {
                if (Clipboard.HasText)
                {
                    var clipboardText = await Clipboard.GetTextAsync();
                    if (!string.IsNullOrWhiteSpace(clipboardText))
                    {
                        VerificationCode = clipboardText.Trim();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Clipboard error: {ex.Message}");
            }
        }

        // Cleanup resources
        public void Dispose()
        {
            _httpClient?.Dispose();
        }

        ~CertificatesPageViewModel()
        {
            Dispose();
        }
    }
}
