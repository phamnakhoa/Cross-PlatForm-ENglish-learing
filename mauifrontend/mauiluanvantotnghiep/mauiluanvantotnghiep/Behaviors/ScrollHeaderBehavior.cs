using Microsoft.Maui.Controls;
using System;

namespace mauiluanvantotnghiep.Behaviors
{
    public class ScrollHeaderBehavior : Behavior<ScrollView>
    {
        public static readonly BindableProperty HeaderElementProperty =
            BindableProperty.Create(nameof(HeaderElement), typeof(VisualElement), typeof(ScrollHeaderBehavior));

        public static readonly BindableProperty ScrollFactorProperty =
            BindableProperty.Create(nameof(ScrollFactor), typeof(double), typeof(ScrollHeaderBehavior), 0.5);

        public VisualElement HeaderElement
        {
            get => (VisualElement)GetValue(HeaderElementProperty);
            set => SetValue(HeaderElementProperty, value);
        }

        public double ScrollFactor
        {
            get => (double)GetValue(ScrollFactorProperty);
            set => SetValue(ScrollFactorProperty, value);
        }

        private ScrollView _scrollView;
        private double _lastScrollY = 0;

        protected override void OnAttachedTo(ScrollView bindable)
        {
            base.OnAttachedTo(bindable);
            _scrollView = bindable;
            _scrollView.Scrolled += OnScrolled;
        }

        protected override void OnDetachingFrom(ScrollView bindable)
        {
            base.OnDetachingFrom(bindable);
            if (_scrollView != null)
            {
                _scrollView.Scrolled -= OnScrolled;
                _scrollView = null;
            }
        }

        private void OnScrolled(object sender, ScrolledEventArgs e)
        {
            if (HeaderElement == null) return;

            var currentScrollY = e.ScrollY;
            var scrollDelta = currentScrollY - _lastScrollY;

            // Calculate the translation based on scroll direction and factor
            var currentTranslationY = HeaderElement.TranslationY;
            var newTranslationY = currentTranslationY - (scrollDelta * ScrollFactor);

            // Apply bounds to prevent header from moving too far
            var maxTranslation = HeaderElement.Height * 0.8; // Maximum 80% of header height
            newTranslationY = Math.Max(-maxTranslation, Math.Min(maxTranslation, newTranslationY));

            // Apply the translation with animation
            HeaderElement.TranslateTo(HeaderElement.TranslationX, newTranslationY, 50, Easing.CubicOut);

            _lastScrollY = currentScrollY;
        }
    }
}
