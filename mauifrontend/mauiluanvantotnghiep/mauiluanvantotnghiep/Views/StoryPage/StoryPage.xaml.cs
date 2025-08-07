using mauiluanvantotnghiep.ViewModels;
using System.ComponentModel;
using Microsoft.Maui.Controls;
using CommunityToolkit.Maui.Views;

namespace mauiluanvantotnghiep.Views.StoryPage;

public partial class StoryPage : ContentPage
{
    private StoryViewModel _viewModel;
    private bool _isPlaying = false;
    private bool _isMediaPlayerReady = false;
    private TimeSpan _savedPosition = TimeSpan.Zero; // THÊM: Lưu vị trí khi pause

    public StoryPage()
    {
        InitializeComponent();
        _viewModel = (StoryViewModel)BindingContext;

        // Subscribe to property changes for better audio handling
        if (_viewModel != null)
        {
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        }
    }

    // Handle ViewModel property changes for better audio sync
    private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        try
        {
            switch (e.PropertyName)
            {
                case nameof(StoryViewModel.AudioUrl):
                    // Reset audio state when new audio URL is available
                    _isPlaying = false;
                    _isMediaPlayerReady = false;
                    _savedPosition = TimeSpan.Zero; // Reset saved position
                    ResetPlayButton();
                    break;

                case nameof(StoryViewModel.IsBusy):
                    // Reset audio controls when generation starts
                    if (_viewModel.IsBusy)
                    {
                        StopAudioPlayback();
                    }
                    break;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[OnViewModelPropertyChanged] Error: {ex.Message}");
        }
    }

    private async void OnPlayPauseClicked(object sender, EventArgs e)
    {
        try
        {
            if (sender is ImageButton btn && btn.CommandParameter is string url && !string.IsNullOrWhiteSpace(url))
            {
                if (!_isPlaying)
                {
                    // PLAY/RESUME: Change to pause icon
                    btn.Source = "https://img.icons8.com/ios-filled/50/ffffff/pause.png";

                    // Initialize media player if needed
                    if (!_isMediaPlayerReady || mediaPlayerUs.Source?.ToString() != url)
                    {
                        mediaPlayerUs.Source = url;
                        _isMediaPlayerReady = true;
                        await Task.Delay(100); // Allow media to load

                        // If we have a saved position, seek to it
                        if (_savedPosition > TimeSpan.Zero)
                        {
                            System.Diagnostics.Debug.WriteLine($"[OnPlayPauseClicked] Seeking to saved position: {_savedPosition}");
                            mediaPlayerUs.SeekTo(_savedPosition);
                            await Task.Delay(100); // Allow seek to complete
                        }
                    }
                    else if (_savedPosition > TimeSpan.Zero)
                    {
                        // Media is ready and we have saved position
                        System.Diagnostics.Debug.WriteLine($"[OnPlayPauseClicked] Seeking to saved position: {_savedPosition}");
                        mediaPlayerUs.SeekTo(_savedPosition);
                        await Task.Delay(100); // Allow seek to complete
                    }

                    // Start playback
                    mediaPlayerUs.Play();
                    _isPlaying = true;

                    System.Diagnostics.Debug.WriteLine($"[OnPlayPauseClicked] Started/Resumed playing from position: {_savedPosition}");
                }
                else
                {
                    // PAUSE: Save current position and change to play icon
                    _savedPosition = mediaPlayerUs.Position;
                    System.Diagnostics.Debug.WriteLine($"[OnPlayPauseClicked] Saving position: {_savedPosition}");

                    btn.Source = "https://img.icons8.com/ios-filled/50/ffffff/play.png";

                    mediaPlayerUs.Pause();
                    _isPlaying = false;

                    System.Diagnostics.Debug.WriteLine($"[OnPlayPauseClicked] Paused at position: {_savedPosition}");
                }
            }
            else
            {
                await DisplayAlert("Thông báo", "Chưa có audio để phát. Vui lòng tạo câu chuyện trước.", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Lỗi Audio", $"Không thể điều khiển audio: {ex.Message}", "OK");
            ResetPlayButton();
            System.Diagnostics.Debug.WriteLine($"[OnPlayPauseClicked] Error: {ex.Message}");
        }
    }

    private async void OnReplayClicked(object sender, EventArgs e)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(_viewModel?.ReadStoryURL))
            {
                // REPLAY: Reset saved position and restart from beginning
                _savedPosition = TimeSpan.Zero;

                // Stop current playback completely
                mediaPlayerUs.Stop();
                _isPlaying = false;

                // Small delay before restarting
                await Task.Delay(200);

                // Restart from beginning
                mediaPlayerUs.Source = _viewModel.ReadStoryURL;
                mediaPlayerUs.Play();
                _isPlaying = true;
                _isMediaPlayerReady = true;

                // Update play button to show pause icon
                if (FindByName("playPauseBtn") is ImageButton btn)
                {
                    btn.Source = "https://img.icons8.com/ios-filled/50/ffffff/pause.png";
                }

                System.Diagnostics.Debug.WriteLine("[OnReplayClicked] Restarted playback from beginning, reset saved position");
            }
            else
            {
                await DisplayAlert("Thông báo", "Chưa có audio để phát lại. Vui lòng tạo câu chuyện trước.", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Lỗi", $"Không thể phát lại audio: {ex.Message}", "OK");
            ResetPlayButton();
            System.Diagnostics.Debug.WriteLine($"[OnReplayClicked] Error: {ex.Message}");
        }
    }

    private void OnRewind10Clicked(object sender, EventArgs e)
    {
        try
        {
            if (_isMediaPlayerReady)
            {
                var currentPosition = _isPlaying ? mediaPlayerUs.Position : _savedPosition;
                var newPosition = currentPosition.Subtract(TimeSpan.FromSeconds(10));
                if (newPosition < TimeSpan.Zero)
                    newPosition = TimeSpan.Zero;

                if (_isPlaying)
                {
                    mediaPlayerUs.SeekTo(newPosition);
                }
                else
                {
                    // Update saved position if paused
                    _savedPosition = newPosition;
                }

                System.Diagnostics.Debug.WriteLine($"[OnRewind10Clicked] Seeked to: {newPosition}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[OnRewind10Clicked] Error: {ex.Message}");
        }
    }

    private void OnForward10Clicked(object sender, EventArgs e)
    {
        try
        {
            if (_isMediaPlayerReady)
            {
                var currentPosition = _isPlaying ? mediaPlayerUs.Position : _savedPosition;
                var newPosition = currentPosition.Add(TimeSpan.FromSeconds(10));
                var duration = mediaPlayerUs.Duration;

                if (duration > TimeSpan.Zero && newPosition > duration)
                    newPosition = duration;

                if (_isPlaying)
                {
                    mediaPlayerUs.SeekTo(newPosition);
                }
                else
                {
                    // Update saved position if paused
                    _savedPosition = newPosition;
                }

                System.Diagnostics.Debug.WriteLine($"[OnForward10Clicked] Seeked to: {newPosition}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[OnForward10Clicked] Error: {ex.Message}");
        }
    }

    private void OnMediaEnded(object sender, EventArgs e)
    {
        _isPlaying = false;
        _isMediaPlayerReady = false;
        _savedPosition = TimeSpan.Zero; // Reset saved position when media ends

        ResetPlayButton();
        System.Diagnostics.Debug.WriteLine("[OnMediaEnded] Media playback completed, reset saved position");
    }

    // Helper methods for better audio control
    private void StopAudioPlayback()
    {
        try
        {
            mediaPlayerUs.Stop();
            _isPlaying = false;
            _isMediaPlayerReady = false;
            _savedPosition = TimeSpan.Zero; // Reset saved position

            ResetPlayButton();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[StopAudioPlayback] Error: {ex.Message}");
            // Force reset state even if stop fails
            _isPlaying = false;
            _isMediaPlayerReady = false;
            _savedPosition = TimeSpan.Zero;

            ResetPlayButton();
        }
    }

    private void ResetPlayButton()
    {
        try
        {
            if (FindByName("playPauseBtn") is ImageButton btn)
            {
                btn.Source = "https://img.icons8.com/ios-filled/50/ffffff/play.png"; // White play icon
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ResetPlayButton] Error: {ex.Message}");
        }
    }

    private void UpdatePlayButton(string text)
    {
        try
        {
            if (FindByName("playPauseBtn") is Button playBtn)
            {
                playBtn.Text = text;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[UpdatePlayButton] Error: {ex.Message}");
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            if (_viewModel != null)
            {
                _viewModel.ErrorMessage = "";
            }

            // Reset audio state when page appears
            StopAudioPlayback();

            System.Diagnostics.Debug.WriteLine("[OnAppearing] StoryPage appeared");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[OnAppearing] Error: {ex.Message}");
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        try
        {
            // Save position before leaving page
            if (_isPlaying)
            {
                _savedPosition = mediaPlayerUs.Position;
            }

            // Stop audio when leaving page
            StopAudioPlayback();

            System.Diagnostics.Debug.WriteLine("[OnDisappearing] StoryPage disappeared");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[OnDisappearing] Error: {ex.Message}");
        }
    }

    // Animation cho Genre Card khi tap
    private async void OnGenreCardTapped(object sender, EventArgs e)
    {
        if (sender is Border border)
        {
            // Scale animation
            await border.ScaleTo(0.95, 100, Easing.CubicOut);
            await border.ScaleTo(1.05, 150, Easing.CubicOut);
            await border.ScaleTo(1.0, 100, Easing.CubicOut);
        }
    }

    // Animation cho Age Group Card khi tap
    private async void OnAgeGroupCardTapped(object sender, EventArgs e)
    {
        if (sender is Border border)
        {
            // Scale animation
            await border.ScaleTo(0.95, 100, Easing.CubicOut);
            await border.ScaleTo(1.05, 150, Easing.CubicOut);
            await border.ScaleTo(1.0, 100, Easing.CubicOut);
        }
    }

    // Cleanup when page is disposed
    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();

        if (Handler == null && _viewModel != null)
        {
            // Unsubscribe from events to prevent memory leaks
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }
    }
}