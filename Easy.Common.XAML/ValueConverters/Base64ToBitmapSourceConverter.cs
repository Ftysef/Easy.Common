namespace Easy.Common.XAML
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Windows.Data;
    using System.Windows.Media.Imaging;

    /// <summary>
    /// An abstraction for converting a <c>Base64</c> encoded image to a <see cref="BitmapSource"/>.
    /// </summary>
    public sealed class Base64ToBitmapSourceConverter : IValueConverter
    {
        /// <summary>
        /// Converts the given <paramref name="value"/> to <see cref="BitmapSource"/>.
        /// </summary>
        /// <returns>The resulting <see cref="BitmapSource"/>.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string base64Img)
            {
                var bytes = System.Convert.FromBase64String(base64Img);
                using (var stream = new MemoryStream(bytes))
                {
                    return BitmapFrame.Create(stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                }
            }

            throw new InvalidDataException("The data is not base64 encoded image.");
        }

        /// <summary>
        /// This back conversion is not implemented.
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}