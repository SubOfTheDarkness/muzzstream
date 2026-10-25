using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;

namespace muzzstream.Controls {
    public class MarqueeTextBlock : Control {
        private double _offset;
        private DispatcherTimer? _timer;
        private FormattedText? _formattedText;
        private double _textWidth;
        
        private bool _isWaiting = true;
        private int _waitTicks;
        private const int WaitDurationTicks = 50;
        private const double Gap = 50.0; 

        public static readonly StyledProperty<string> TextProperty =
            AvaloniaProperty.Register<MarqueeTextBlock, string>(nameof(Text), string.Empty);

        public static readonly StyledProperty<IBrush> ForegroundProperty =
            AvaloniaProperty.Register<MarqueeTextBlock, IBrush>(nameof(Foreground), Brushes.White);

        public static readonly StyledProperty<double> FontSizeProperty =
            AvaloniaProperty.Register<MarqueeTextBlock, double>(nameof(FontSize), 14.0);

        public static readonly StyledProperty<FontWeight> FontWeightProperty =
            AvaloniaProperty.Register<MarqueeTextBlock, FontWeight>(nameof(FontWeight), FontWeight.Normal);

        public string Text {
            get => GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public IBrush Foreground {
            get => GetValue(ForegroundProperty);
            set => SetValue(ForegroundProperty, value);
        }

        public double FontSize {
            get => GetValue(FontSizeProperty);
            set => SetValue(FontSizeProperty, value);
        }

        public FontWeight FontWeight {
            get => GetValue(FontWeightProperty);
            set => SetValue(FontWeightProperty, value);
        }

        static MarqueeTextBlock() {
            AffectsRender<MarqueeTextBlock>(TextProperty);
            AffectsMeasure<MarqueeTextBlock>(TextProperty);
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e) {
            base.OnAttachedToVisualTree(e);
            _timer = new DispatcherTimer(TimeSpan.FromMilliseconds(20), DispatcherPriority.Render, OnTimerTick);
            _timer.Start();
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e) {
            base.OnDetachedFromVisualTree(e);
            _timer?.Stop();
            _timer = null;
        }

        private void OnTimerTick(object? sender, EventArgs e) {
            if (_textWidth <= Bounds.Width) {
                _offset = 0;
                _isWaiting = true;
                _waitTicks = 0;
                return;
            }

            if (_isWaiting) {
                _waitTicks++;
                if (_waitTicks >= WaitDurationTicks)
                {
                    _isWaiting = false;
                    _waitTicks = 0;
                }
                return;
            }

            _offset -= 1.0; 

            if (_offset <= -(_textWidth + Gap)) {
                _offset = 0;
                _isWaiting = true;
            }

            InvalidateVisual();
        }

        protected override Size MeasureOverride(Size availableSize) {
            CreateFormattedText();
            
            double width = double.IsInfinity(availableSize.Width) ? (_textWidth) : availableSize.Width;
            double height = _formattedText?.Height ?? 20;
            
            return new Size(width, height);
        }

        public override void Render(DrawingContext context) {
            CreateFormattedText();
            if (_formattedText == null) return;

            using (context.PushClip(new Rect(Bounds.Size))) {
                if (_textWidth <= Bounds.Width) {
                    context.DrawText(_formattedText, new Point(0, 0));
                }
                else {
                    context.DrawText(_formattedText, new Point(_offset, 0));
                    
                    context.DrawText(_formattedText, new Point(_offset + _textWidth + Gap, 0));
                }
            }
        }

        private void CreateFormattedText() {
            if (string.IsNullOrEmpty(Text)) {
                _formattedText = null;
                _textWidth = 0;
                return;
            }

            _formattedText = new FormattedText(
                Text,
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight),
                FontSize,
                Foreground);

            _textWidth = _formattedText.Width;
        }
    }
}
