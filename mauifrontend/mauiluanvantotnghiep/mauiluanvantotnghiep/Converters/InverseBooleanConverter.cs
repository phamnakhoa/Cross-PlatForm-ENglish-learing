﻿// Converters/InverseBooleanConverter.cs
using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace mauiluanvantotnghiep.Converters;

public class InverseBooleanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is bool b ? !b : true;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
