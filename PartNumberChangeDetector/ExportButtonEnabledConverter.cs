using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace PartNumberChangeDetector
{
    public class ExportButtonEnabledConverter : MarkupExtension, IMultiValueConverter
    {
        public override object ProvideValue(IServiceProvider serviceProvider) => this;

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) =>
            !(string.IsNullOrEmpty((string)values[0]) || string.IsNullOrEmpty((string)values[1]) || (int)values[2] == 0);

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
