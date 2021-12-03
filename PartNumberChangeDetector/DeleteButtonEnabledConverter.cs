using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace PartNumberChangeDetector
{
    public class DeleteButtonEnabledConverter : MarkupExtension, IMultiValueConverter
    {
        public override object ProvideValue(IServiceProvider serviceProvider) => this;

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) => (bool)values[0] && values[1] != null;
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
