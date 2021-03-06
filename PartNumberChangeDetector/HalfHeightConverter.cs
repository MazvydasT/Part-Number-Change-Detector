using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace PartNumberChangeDetector
{
    public class HalfHeightConverter : MarkupExtension, IValueConverter
    {
        public override object ProvideValue(IServiceProvider serviceProvider) => this;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => (double)value / 2.0;
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
