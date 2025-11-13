using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using CalculatorGui.ViewModels;

namespace CalculatorGui;

public class ViewLocator : IDataTemplate
{
    public Control? Build(object? param) // builds view control from view model. replaces "ViewModel" with "View" in type name and creates instance
    {
        if (param is null)
            return null;

        var name = param.GetType().FullName!.Replace("ViewModel", "View", StringComparison.Ordinal); // get full type name and replace ViewModel with View
        var type = Type.GetType(name); // get type from name

        if (type != null)
        {
            return (Control)Activator.CreateInstance(type)!; // create instance of view type
        }

        return new TextBlock { Text = "Not Found: " + name }; // return error text if view not found
    }

    public bool Match(object? data) // checks if data is a ViewModelBase. returns true if match
    {
        return data is ViewModelBase;
    }
}