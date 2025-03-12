﻿using System.Windows;
using System.Diagnostics;
using System.Windows.Navigation;
using System.Reflection;

namespace MAMEUtility;

public partial class AboutWindow
{
    public AboutWindow()
    {
        InitializeComponent();
        DataContext = this; // Set the data context for data binding
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
    }

    private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = e.Uri.AbsoluteUri,
            UseShellExecute = true
        });
        e.Handled = true;
    }

    public static string ApplicationVersion
    {
        get
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            return "Version: " + (version?.ToString() ?? "Unknown");
        }
    }
}