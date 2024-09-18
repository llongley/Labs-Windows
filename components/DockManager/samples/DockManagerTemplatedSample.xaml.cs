// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace DockManagerExperiment.Samples;

[ToolkitSample(id: nameof(DockManagerTemplatedSample), "Templated control", description: "A sample for showing how to create and use a templated control.")]
public sealed partial class DockManagerTemplatedSample : Page
{
    public DockManagerTemplatedSample()
    {
        this.InitializeComponent();
    }

    public void CreateTab(TabView sender, object? args)
    {
        var new_tab = new TabViewItem();
        new_tab.IconSource = new SymbolIconSource() { Symbol = Symbol.Document };
        new_tab.Header = "demo.cs";

        Frame frame = new Frame();
        //frame.Navigate(typeof(blank_rect));

        new_tab.Content = frame;

        sender.TabItems.Add(new_tab);
        sender.SelectedItem = sender.TabItems[sender.TabItems.Count - 1];
    }

    private void TabBarLoaded(object Sender, RoutedEventArgs Args)
    {
        if (Sender is TabView tab_view)
        {
            if (tab_view.TabItems.Count > 0)
            {
                return;
            }
            CreateTab(tab_view, null);
        }
    }
}
