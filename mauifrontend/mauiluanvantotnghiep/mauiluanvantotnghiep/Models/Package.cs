using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mauiluanvantotnghiep.Models
{
   public partial class Package : ObservableObject
    {
        public int PackageId { get; set; }
        public string PackageName { get; set; }
        public int? DurationDay { get; set; }
        public decimal Price { get; set; }

        public string UrlImage { get; set; } = string.Empty;

        // Thêm property cho includedPackageIds
        public List<int> IncludedPackageIds { get; set; } = new List<int>();

        // Thuộc tính này dùng để xác định xem người dùng đã đăng ký gói này hay chưa.
        // Khi thay đổi, giao diện sẽ nhận thông báo và cập nhật lại
        [ObservableProperty]
        private bool isRegistered;

         // Thêm thông tin từ Registration
        [ObservableProperty]
        private DateTime registrationDate;

        [ObservableProperty]
        private DateTime? expirationDate;

        [ObservableProperty]
        private string expirationDateDisplay = "vĩnh viễn"; // Giá trị mặc định

        // Thêm property để hiển thị thông tin các gói được bao gồm
        [ObservableProperty]
        private ObservableCollection<Package> includedPackages = new ObservableCollection<Package>();

        [ObservableProperty]
        private string includedPackagesDisplay = string.Empty;

        // Cập nhật ExpirationDateDisplay khi ExpirationDate thay đổi
        partial void OnExpirationDateChanged(DateTime? oldValue, DateTime? newValue)
        {
            ExpirationDateDisplay = newValue.HasValue ? newValue.Value.ToString("dd/MM/yyyy") : "vĩnh viễn";
        }

        // Cập nhật IncludedPackagesDisplay khi IncludedPackages thay đổi
        partial void OnIncludedPackagesChanged(ObservableCollection<Package> oldValue, ObservableCollection<Package> newValue)
        {
            if (newValue != null && newValue.Any())
            {
                IncludedPackagesDisplay = string.Join(", ", newValue.Select(p => p.PackageName));
            }
            else
            {
                IncludedPackagesDisplay = "Không có gói phụ";
            }
        }
    }
}
