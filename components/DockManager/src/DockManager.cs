// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Controls;
using Windows.Graphics;

namespace CommunityToolkit.WinUI.Controls;

[TemplatePart(Name = nameof(TheRoot), Type = typeof(Grid))]
[TemplatePart(Name = nameof(TheTabView), Type = typeof(TabView))]
[TemplatePart(Name = nameof(TheCanvas), Type = typeof(Canvas))]
[TemplatePart(Name = nameof(the_grid), Type = typeof(Grid))]
[TemplatePart(Name = nameof(TopButton), Type = typeof(Button))]
[TemplatePart(Name = nameof(LeftButton), Type = typeof(Button))]
[TemplatePart(Name = nameof(RightButton), Type = typeof(Button))]
[TemplatePart(Name = nameof(BottomButton), Type = typeof(Button))]
public sealed partial class DockManager : Control
{
    private AppWindow? TheWindow { get; set; }
    private Grid? TheRoot { get; set; }
    private TabView? TheTabView { get; set; }
    private Canvas? TheCanvas { get; set; }
    private Grid? the_grid { get; set; }
    private Button? TopButton { get; set; }
    private Button? LeftButton { get; set; }
    private Button? RightButton { get; set; }
    private Button? BottomButton { get; set; }
    private bool RightSplit = false;
    private bool LeftSplit = false;
    private bool TopSplit = false;
    private bool BottomSplit = false;
    public Guid DockID = Guid.NewGuid();
    public WindowId id { get; set; }
    public InputNonClientPointerSource? pointer;
    public DockPosition? CurrentDockPosition;
    public Dictionary<AppWindow, XamlRoot> windows = new Dictionary<AppWindow, XamlRoot>();
    double sXCoor = 0;
    double sYCoor = 0;
    private double ScaleFactor = 1;
    public ((AppWindow, XamlRoot)?, DockPosition)? DockInfo;
    public static readonly DependencyProperty DisableLeftDockProperty =
        DependencyProperty.Register(nameof(DisableLeftDock), typeof(bool), typeof(DockManager), new PropertyMetadata(false));
    public bool DisableLeftDock
    {
        get { return (bool)GetValue(DisableLeftDockProperty); }
        set { SetValue(DisableLeftDockProperty, value); }
    }
    public static readonly DependencyProperty DisableRightDockProperty =
        DependencyProperty.Register(nameof(DisableRightDock), typeof(bool), typeof(DockManager), new PropertyMetadata(false));
    public bool DisableRightDock
    {
        get { return (bool)GetValue(DisableRightDockProperty); }
        set { SetValue(DisableRightDockProperty, value); }
    }
    public static readonly DependencyProperty DisableTopDockProperty =
        DependencyProperty.Register(nameof(DisableTopDock), typeof(bool), typeof(DockManager), new PropertyMetadata(false));
    public bool DisableTopDock
    {
        get { return (bool)GetValue(DisableTopDockProperty); }
        set { SetValue(DisableTopDockProperty, value); }
    }
    public static readonly DependencyProperty DisableBottomDockProperty =
        DependencyProperty.Register(nameof(DisableBottomDock), typeof(bool), typeof(DockManager), new PropertyMetadata(false));
    public bool DisableBottomDock
    {
        get { return (bool)GetValue(DisableBottomDockProperty); }
        set { SetValue(DisableBottomDockProperty, value); }
    }
    public static readonly DependencyProperty DisableSpaceReclaimProperty =
        DependencyProperty.Register(nameof(DisableSpaceReclaim), typeof(bool), typeof(DockManager), new PropertyMetadata(false));
    public static readonly DependencyProperty TabItemsProperty =
        DependencyProperty.Register(nameof(TabItems), typeof(ObservableCollection<TabViewItem>), typeof(DockManager), new PropertyMetadata(null, OnTabItemsChanged));
    public ObservableCollection<TabViewItem> TabItems
    {
        get { return (ObservableCollection<TabViewItem>)GetValue(TabItemsProperty); }
        set { SetValue(TabItemsProperty, value); }
    }
    private static void OnTabItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        DockManager? Dock = d as DockManager;
        Dock?.OnTabItemsChanged((ObservableCollection<TabViewItem>)e.OldValue, (ObservableCollection<TabViewItem>)e.NewValue);
    }
    private void OnTabItemsChanged(ObservableCollection<TabViewItem> OldValue, ObservableCollection<TabViewItem> NewValue)
    {
        if (OldValue != null)
        {
            OldValue.CollectionChanged -= TabItems_CollectionChanged;
        }
        if (NewValue != null)
        {
            NewValue.CollectionChanged += TabItems_CollectionChanged;
        }
        UpdateTabItems();
    }

    private void TabItems_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        UpdateTabItems();
    }
    private void UpdateTabItems()
    {
        if (TheTabView != null && TabItems != null)
        {
            TheTabView.TabItems.Clear();
            foreach (TabViewItem item in TabItems)
            {
                TheTabView.TabItems.Add(item);
            }
        }
    }
    public bool DisableSpaceReclaim
    {
        get { return (bool)GetValue(DisableSpaceReclaimProperty); }
        set { SetValue(DisableSpaceReclaimProperty, value); }
    }
    public event TypedEventHandler<TabView, object>? AddTabButtonClicked;
    public event RoutedEventHandler? TabBarLoaded;
    public event TypedEventHandler<DockManager, DockManagerArgs>? DockDropped;
    public event TypedEventHandler<FrameworkElement, object>? SpaceReclaimed;
    public DockManager()
    {
        this.DefaultStyleKey = typeof(DockManager);
        SetValue(TabItemsProperty, new ObservableCollection<TabViewItem>());
    }
    private void UpdateScaleFactor()
    {
        //ScaleFactor = Windows.Graphics.Display.DisplayInformation.GetForCurrentView().RawPixelsPerViewPixel;
    }
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        TheRoot = GetTemplateChild(nameof(TheRoot)) as Grid;

        TheTabView = GetTemplateChild(nameof(TheTabView)) as TabView;

        if (TheTabView != null)
        {
            TheTabView.AddTabButtonClick += AddTabButtonClicked;
            TheTabView.TabCloseRequested += CloseTab;
            TheTabView.TabTearOutRequested += OnTabTearOutRequested;
            TheTabView.TabTearOutWindowRequested += OnTabTearOutWindowRequested;
            TheTabView.ExternalTornOutTabsDropping += OnExternalTornOutTabsDropping;
            TheTabView.ExternalTornOutTabsDropped += OnExternalTornOutTabsDropped;
            TheTabView.Loaded += TabBarLoaded;
            TheTabView.CanTearOutTabs = true;
            TheTabView.VerticalAlignment = VerticalAlignment.Stretch;

            UpdateTabItems();
        }

        TheCanvas = GetTemplateChild(nameof(TheCanvas)) as Canvas;

        the_grid = GetTemplateChild(nameof(the_grid)) as Grid;

        TopButton = GetTemplateChild(nameof(TopButton)) as Button;
        LeftButton = GetTemplateChild(nameof(LeftButton)) as Button;
        RightButton = GetTemplateChild(nameof(RightButton)) as Button;
        BottomButton = GetTemplateChild(nameof(BottomButton)) as Button;

        TheWindow = AppWindow.GetFromWindowId(this.XamlRoot.ContentIslandEnvironment.AppWindowId);
        TheWindow.Changed += WindowMoved;

        UpdateScaleFactor();

        pointer = InputNonClientPointerSource.GetForWindowId(TheWindow.Id);
        pointer.PointerEntered += WindowDropped;

        AddWindow((TheWindow, this.XamlRoot));
    }
    private Grid? GetParentGrid(FrameworkElement? Dock)
    {
        DependencyObject? current = Dock;

        while (current != null)
        {
            if (current is Grid grid)
            {
                return grid;
            }
            current = VisualTreeHelper.GetParent(current);
        }
        return null;
    }
    private Grid? GetParentGridGrid(FrameworkElement? Element)
    {
        DependencyObject? current = VisualTreeHelper.GetParent(Element);

        while (current != null)
        {
            if (current is Grid grid)
            {
                return grid;
            }
            current = VisualTreeHelper.GetParent(current);
        }
        return null;
    }
    private DockManager? GetParentDock(TabView TV)
    {
        DependencyObject current = TV;

        while (current != null)
        {
            if (current is DockManager Dock)
            {
                return Dock;
            }
            current = VisualTreeHelper.GetParent(current);
        }
        return null;
    }
    private bool IsMainGrid(Grid? grid)
    {
        DependencyObject? current = grid;
        current = VisualTreeHelper.GetParent(current);

        while (current != null)
        {
            if (current is Grid)
            {
                return false;
            }
            current = VisualTreeHelper.GetParent(current);
        }
        return true;
    }
    private void CloseTab(TabView Sender, TabViewTabCloseRequestedEventArgs Args)
    {
        Sender.TabItems.Remove(Args.Tab);
        if (Sender.TabItems.Count == 0)
        {
            if (!DisableSpaceReclaim)
            {
                RemoveAndReclaim(null, Grid.GetRow(Sender), Grid.GetRowSpan(Sender), Grid.GetColumn(Sender), Grid.GetColumnSpan(Sender));
            }
        }
    }
    DockManager? NewDockManager = null;
    private void OnTabTearOutWindowRequested(TabView Sender, TabViewTabTearOutWindowRequestedEventArgs Args)
    {
        Window NewWindow = new Window();
        Grid grid = new Grid();

        NewDockManager = new DockManager();
        DockManager? Parent = GetParentDock(Sender);
        NewDockManager.AddTabButtonClicked = Parent?.AddTabButtonClicked;


        NewDockManager.pointer = InputNonClientPointerSource.GetForWindowId(NewWindow.AppWindow.Id);
        NewDockManager.id = NewWindow.AppWindow.Id;
        NewDockManager.windows = windows;

        grid.Children.Add(NewDockManager);

        NewWindow.Content = grid;

        Args.NewWindowId = NewWindow.AppWindow.Id;
        NewWindow.Activate();
    }

    private void OnTabTearOutRequested(TabView Sender, TabViewTabTearOutRequestedEventArgs Args)
    {
        foreach (TabViewItem item in Args.Items)
        {
            Sender.TabItems.Remove(item);
            NewDockManager?.TheTabView?.TabItems.Add(item);
        }
    }

    private void OnExternalTornOutTabsDropping(TabView Sender, TabViewExternalTornOutTabsDroppingEventArgs Args)
    {
        Args.AllowDrop = true;
    }

    private void OnExternalTornOutTabsDropped(TabView Sender, TabViewExternalTornOutTabsDroppedEventArgs Args)
    {
        // TODO: fill me in
    }

    private void AddWindow((AppWindow, XamlRoot) combo)
    {
        foreach ((AppWindow w, XamlRoot root) in windows)
        {
            if (root.Content is Grid grid)
            {
                foreach (FrameworkElement Element in grid.Children)
                {
                    if (Element is DockManager Dock)
                    {
                        if (Dock.windows == windows)
                        {
                            continue;
                        }
                        Dock.windows.TryAdd(combo.Item1, combo.Item2);
                    }
                }
            }
        }
        windows.Add(combo.Item1, combo.Item2);
    }
    private void RemoveWindow(AppWindow? window, WindowId window_id)
    {
        if (window == null)
        {
            return;
        }

        foreach ((AppWindow w, XamlRoot root) in windows)
        {
            if (w == TheWindow)
            {
                continue;
            }
            if (root.Content is Grid grid)
            {
                foreach (FrameworkElement Element in grid.Children)
                {
                    if (Element is DockManager Dock)
                    {
                        Dock.windows.Remove(window);
                    }
                }
            }
        }
        windows.Remove(window);
    }
    private void WindowDropped(InputNonClientPointerSource Sender, NonClientPointerEventArgs Args)
    {
        if (DockInfo == null)
        {
            return;
        }
        else
        {
            var (combo, Position) = DockInfo.Value;
            if (combo == null)
            {
                foreach ((AppWindow w, XamlRoot root_x) in windows)
                {
                    if (w == null)
                    {
                        continue;
                    }
                    if (root_x.Content is Grid grid_x)
                    {
                        ClearDocks(grid_x);
                    }
                }
                return;
            }

            DockManager TransferDock = this;
            if (this.XamlRoot.Content is Grid t_grid)
            {
                foreach (FrameworkElement Element in t_grid.Children)
                {
                    if (Element is DockManager Dock)
                    {
                        TransferDock = Dock;
                        t_grid.Children.Remove(Dock);
                    }
                }
            }

            DockManager? TheDock = null;
            if (combo.Value.Item2.Content is Grid grid)
            {
                FindDockResult Result = FindDock(null, grid, ref sXCoor, ref sYCoor);
                if (Result.Dock == null)
                {
                    return;
                }
                TheDock = Result.Dock;
            }
            else
            {
                return;
            }

            Grid? TheGrid = TheDock.TheRoot;

            GridSplitter gridSplitter = new GridSplitter();
            gridSplitter.ResizeBehavior = GridSplitter.GridResizeBehavior.BasedOnAlignment;
            gridSplitter.ResizeDirection = GridSplitter.GridResizeDirection.Auto;

            switch (Position)
            {
                case DockPosition.Top:
                    RemoveWindow(TheWindow, id);
                    TheWindow?.Destroy();

                    Grid.SetColumn(TransferDock, Grid.GetColumn(TheDock.TheTabView));
                    Grid.SetColumnSpan(TransferDock, Grid.GetColumnSpan(TheDock.TheTabView));
                    Grid.SetRow(TransferDock, Grid.GetRow(TheDock.TheTabView) - 2);

                    gridSplitter.Height = 16;
                    Grid.SetColumnSpan(gridSplitter, Grid.GetColumnSpan(TheDock.TheTabView));
                    Grid.SetRow(gridSplitter, Grid.GetRow(TheDock.TheTabView) - 1);
                    Grid.SetColumn(gridSplitter, Grid.GetColumn(TheDock.TheTabView));

                    TheGrid?.Children.Add(gridSplitter);
                    TheGrid?.Children.Add(TransferDock);

                    Grid.SetRowSpan(TheDock.TheCanvas, 1);
                    Grid.SetRow(TheDock.TheCanvas, Grid.GetRow(TheDock.TheTabView));

                    TheDock.TopSplit = false;

                    TheDock.DockDropped?.Invoke(this, new DockManagerArgs(TransferDock, DockPosition.Right));
                    break;
                case DockPosition.Left:
                    RemoveWindow(TheWindow, id);
                    TheWindow?.Destroy();

                    Grid.SetRowSpan(TransferDock, Grid.GetRowSpan(TheDock.TheTabView));
                    Grid.SetRow(TransferDock, Grid.GetRow(TheDock.TheTabView));
                    Grid.SetColumn(TransferDock, Grid.GetColumn(TheDock.TheTabView) - 2);

                    gridSplitter.Width = 16;
                    Grid.SetRowSpan(gridSplitter, Grid.GetRowSpan(TheDock.TheTabView));
                    Grid.SetColumn(gridSplitter, Grid.GetColumn(TheDock.TheTabView) - 1);
                    Grid.SetRow(gridSplitter, Grid.GetRow(TheDock.TheTabView));

                    TheGrid?.Children.Add(TransferDock);
                    TheGrid?.Children.Add(gridSplitter);

                    Grid.SetColumnSpan(TheDock.TheCanvas, 1);
                    Grid.SetColumn(TheDock.TheCanvas, Grid.GetColumn(TheDock.TheTabView));

                    TheDock.LeftSplit = false;

                    TheDock.DockDropped?.Invoke(this, new DockManagerArgs(TransferDock, DockPosition.Right));
                    break;
                case DockPosition.Right:
                    RemoveWindow(TheWindow, id);
                    TheWindow?.Destroy();

                    Grid.SetRowSpan(TransferDock, Grid.GetRowSpan(TheDock.TheTabView));
                    Grid.SetRow(TransferDock, Grid.GetRow(TheDock.TheTabView));
                    Grid.SetColumn(TransferDock, Grid.GetColumn(TheDock.TheTabView) + Grid.GetColumnSpan(TheDock.TheTabView) + 1);

                    gridSplitter.Width = 16;
                    Grid.SetRowSpan(gridSplitter, Grid.GetRowSpan(TheDock.TheTabView));
                    Grid.SetColumn(gridSplitter, Grid.GetColumn(TheDock.TheTabView) + Grid.GetColumnSpan(TheDock.TheTabView));
                    Grid.SetRow(gridSplitter, Grid.GetRow(TheDock.TheTabView));

                    TheGrid?.Children.Add(gridSplitter);
                    TheGrid?.Children.Add(TransferDock);

                    Grid.SetColumnSpan(TheDock.TheCanvas, 1);

                    TheDock.RightSplit = false;

                    TheDock.DockDropped?.Invoke(this, new DockManagerArgs(TransferDock, DockPosition.Right));
                    break;
                case DockPosition.Bottom:
                    RemoveWindow(TheWindow, id);
                    TheWindow?.Destroy();

                    Grid.SetColumnSpan(TransferDock, Grid.GetColumnSpan(TheDock.TheTabView));
                    Grid.SetRow(TransferDock, Grid.GetRow(TheDock.TheTabView) + Grid.GetRowSpan(TheDock.TheTabView) + 1);
                    Grid.SetColumn(TransferDock, Grid.GetColumn(TheDock.TheTabView));

                    gridSplitter.Height = 16;
                    Grid.SetColumnSpan(gridSplitter, Grid.GetColumnSpan(TheDock.TheTabView));
                    Grid.SetRow(gridSplitter, Grid.GetRow(TheDock.TheTabView) + Grid.GetRowSpan(TheDock.TheTabView));
                    Grid.SetColumn(gridSplitter, Grid.GetColumn(TheDock.TheTabView));

                    TheGrid?.Children.Add(gridSplitter);
                    TheGrid?.Children.Add(TransferDock);

                    Grid.SetRowSpan(TheDock.TheCanvas, 1);
                    Grid.SetRow(TheDock.TheCanvas, Grid.GetRow(TheDock.TheTabView));

                    TheDock.BottomSplit = false;

                    TheDock.DockDropped?.Invoke(this, new DockManagerArgs(TransferDock, DockPosition.Right));
                    break;
                default:
                    break;
            }
            DockInfo = null;
            TheDock.BlackoutFlyoutMenu();
            TheDock.DockInfo = null;
            TheDock.CurrentDockPosition = null;

            if (TheDock.TheCanvas != null)
            {
                TheDock.TheCanvas.Visibility = Visibility.Collapsed;
            }

            TransferDock.windows = TheDock.windows;
            TransferDock.DockInfo = null;
            return;
        }
    }
    public void DropLeft(FrameworkElement Element)
    {
        PreviewLeft();
        Grid.SetColumn(Element, Grid.GetColumn(TheTabView) - 2);
        Grid.SetRowSpan(Element, Grid.GetRowSpan(TheTabView));
        Grid.SetRow(Element, Grid.GetRow(TheTabView));

        GridSplitter gridSplitter = new GridSplitter();
        gridSplitter.ResizeBehavior = GridSplitter.GridResizeBehavior.BasedOnAlignment;
        gridSplitter.ResizeDirection = GridSplitter.GridResizeDirection.Auto;
        gridSplitter.Width = 16;

        Grid.SetRowSpan(gridSplitter, Grid.GetRowSpan(TheTabView));
        Grid.SetColumn(gridSplitter, Grid.GetColumn(TheTabView) - 1);
        Grid.SetRow(gridSplitter, Grid.GetRow(TheTabView));

        TheRoot?.Children.Add(gridSplitter);
        TheRoot?.Children.Add(Element);

        if (TheCanvas != null)
        {
            TheCanvas.Visibility = Visibility.Collapsed;
        }

        CurrentDockPosition = null;
        LeftSplit = false;
        DockInfo = null;
    }
    public void DropRight(FrameworkElement Element)
    {
        PreviewRight();
        Grid.SetRowSpan(Element, Grid.GetRowSpan(TheTabView));
        Grid.SetRow(Element, Grid.GetRow(TheTabView));
        Grid.SetColumn(Element, Grid.GetColumn(TheTabView) + 2);

        GridSplitter gridSplitter = new GridSplitter();
        gridSplitter.ResizeBehavior = GridSplitter.GridResizeBehavior.BasedOnAlignment;
        gridSplitter.ResizeDirection = GridSplitter.GridResizeDirection.Auto;
        gridSplitter.Width = 16;

        Grid.SetRowSpan(gridSplitter, Grid.GetRowSpan(TheTabView));
        Grid.SetColumn(gridSplitter, Grid.GetColumn(TheTabView) + 1);
        Grid.SetRow(gridSplitter, Grid.GetRow(TheTabView));

        TheRoot?.Children.Add(gridSplitter);
        TheRoot?.Children.Add(Element);

        if (TheCanvas != null)
        {
            TheCanvas.Visibility = Visibility.Collapsed;
        }

        CurrentDockPosition = null;
        RightSplit = false;
        DockInfo = null;
    }
    public void DropTop(FrameworkElement Element)
    {
        PreviewTop();
        Grid.SetColumnSpan(Element, Grid.GetColumnSpan(TheTabView));
        Grid.SetColumn(Element, Grid.GetColumn(TheTabView));
        Grid.SetRow(Element, Grid.GetRow(TheTabView) - 2);

        GridSplitter gridSplitter = new GridSplitter();
        gridSplitter.ResizeBehavior = GridSplitter.GridResizeBehavior.BasedOnAlignment;
        gridSplitter.ResizeDirection = GridSplitter.GridResizeDirection.Auto;

        gridSplitter.Height = 16;
        Grid.SetColumnSpan(gridSplitter, Grid.GetColumnSpan(TheTabView));
        Grid.SetRow(gridSplitter, Grid.GetRow(TheTabView) - 1);
        Grid.SetColumn(gridSplitter, Grid.GetColumn(TheTabView));

        TheRoot?.Children.Add(Element);

        if (TheCanvas != null)
        {
            TheCanvas.Visibility = Visibility.Collapsed;
        }

        CurrentDockPosition = null;
        TopSplit = false;
        DockInfo = null;
    }
    public void DropBottom(FrameworkElement Element)
    {
        PreviewBottom();
        Grid.SetColumnSpan(Element, Grid.GetColumnSpan(TheTabView));
        Grid.SetColumn(Element, Grid.GetColumn(TheTabView));
        Grid.SetRow(Element, Grid.GetRow(TheTabView) + 2);

        GridSplitter gridSplitter = new GridSplitter();
        gridSplitter.ResizeBehavior = GridSplitter.GridResizeBehavior.BasedOnAlignment;
        gridSplitter.ResizeDirection = GridSplitter.GridResizeDirection.Auto;
        gridSplitter.Height = 16;

        Grid.SetColumnSpan(gridSplitter, Grid.GetColumnSpan(TheTabView));
        Grid.SetRow(gridSplitter, Grid.GetRow(TheTabView) + 1);
        Grid.SetColumn(gridSplitter, Grid.GetColumn(TheTabView));

        TheRoot?.Children.Add(gridSplitter);
        TheRoot?.Children.Add(Element);

        if (TheCanvas != null)
        {
            TheCanvas.Visibility = Visibility.Collapsed;
        }

        CurrentDockPosition = null;
        BottomSplit = false;
        DockInfo = null;
    }
    private void PreviewLeft()
    {
        if (TheRoot == null)
        {
            return;
        }

        if (!LeftSplit)
        {
            ColumnDefinition TColDef = TheRoot.ColumnDefinitions.ElementAt(Grid.GetColumn(TheTabView));
            GridLength Width = new GridLength(TColDef.ActualWidth / 2, GridUnitType.Star);
            TColDef.Width = Width;

            ColumnDefinition ColDef = new ColumnDefinition();
            ColDef.Width = Width;

            int Col = Grid.GetColumn(TheTabView);
            int ColSpan = Grid.GetColumnSpan(TheTabView);

            TheRoot.ColumnDefinitions.Insert(Col + ColSpan, ColDef);
            TheRoot.ColumnDefinitions.Insert(Col + ColSpan, new ColumnDefinition() { Width = new GridLength(16) });

            Grid.SetColumn(TheTabView, Grid.GetColumn(TheTabView) + ColSpan + 1);

            foreach (FrameworkElement Element in TheRoot.Children)
            {
                if (Element is Canvas || Element is TabView)
                {
                    continue;
                }
                if (Grid.GetColumn(Element) > Grid.GetColumn(TheTabView) - ColSpan - 1 &&
                    Grid.GetRow(Element) == Grid.GetRow(TheTabView))
                {
                    Grid.SetColumn(Element, Grid.GetColumn(Element) + 2);
                }
                if (Grid.GetColumn(Element) <= Grid.GetColumn(TheTabView) &&
                    Grid.GetColumn(Element) + Grid.GetColumnSpan(Element) > Grid.GetColumn(TheTabView) - 2 &&
                    Grid.GetRow(Element) != Grid.GetRow(TheTabView))
                {
                    Grid.SetColumnSpan(Element, Grid.GetColumnSpan(Element) + 2);
                }
            }

            LeftSplit = true;
            CurrentDockPosition = DockPosition.Left;

            Grid.SetColumnSpan(TheCanvas, 3);
        }
    }
    private void UndoPreviewLeft()
    {
        if (TheRoot == null)
        {
            return;
        }

        CurrentDockPosition = null;

        Grid.SetColumn(TheTabView, Grid.GetColumn(TheTabView) - Grid.GetColumnSpan(TheTabView) - 1);

        foreach (FrameworkElement Element in TheRoot.Children)
        {
            if (Element is Canvas || Element is TabView)
            {
                continue;
            }
            if (Grid.GetColumn(Element) > Grid.GetColumn(TheTabView) && Grid.GetRow(Element) == Grid.GetRow(TheTabView))
            {
                Grid.SetColumn(Element, Grid.GetColumn(Element) - 2);
            }
            if (Grid.GetColumn(Element) <= Grid.GetColumn(TheTabView) &&
                Grid.GetColumn(Element) + Grid.GetColumnSpan(Element) > Grid.GetColumn(TheTabView) &&
                Grid.GetRow(Element) != Grid.GetRow(TheTabView))
            {
                Grid.SetColumnSpan(Element, Grid.GetColumnSpan(Element) - 2);
            }
        }

        TheRoot.ColumnDefinitions.RemoveAt(Grid.GetColumn(TheTabView) + Grid.GetColumnSpan(TheTabView) + 1);
        TheRoot.ColumnDefinitions.RemoveAt(Grid.GetColumn(TheTabView) + Grid.GetColumnSpan(TheTabView));

        ColumnDefinition ColDef = TheRoot.ColumnDefinitions.ElementAt(Grid.GetColumn(TheTabView));
        GridLength Length = new GridLength(ColDef.ActualWidth * 2, GridUnitType.Star);
        ColDef.Width = Length;

        Grid.SetColumnSpan(TheCanvas, 1);
        LeftSplit = false;
    }
    private void PreviewRight()
    {
        if (TheRoot == null)
        {
            return;
        }

        if (!RightSplit)
        {
            ColumnDefinition TColDef = TheRoot.ColumnDefinitions.ElementAt(Grid.GetColumn(TheTabView));
            GridLength Width = new GridLength((TColDef.ActualWidth / 2) - 8, GridUnitType.Star);
            TColDef.Width = Width;

            ColumnDefinition ColDef = new ColumnDefinition();
            ColDef.Width = Width;

            int ColSpan = Grid.GetColumnSpan(TheTabView);
            int Col = Grid.GetColumn(TheTabView);

            TheRoot.ColumnDefinitions.Insert(Col + ColSpan, new ColumnDefinition() { Width = new GridLength(16) });
            TheRoot.ColumnDefinitions.Insert(Col + ColSpan + 1, ColDef);

            foreach (FrameworkElement Element in TheRoot.Children)
            {
                if (Element is Canvas || Element is TabView)
                {
                    continue;
                }
                if (Grid.GetColumn(Element) > Col && Grid.GetRow(Element) == Grid.GetRow(TheTabView))
                {
                    Grid.SetColumn(Element, Grid.GetColumn(Element) + 2);
                }
                if (Grid.GetColumn(Element) <= Col &&
                    Grid.GetColumn(Element) + Grid.GetColumnSpan(Element) > Col &&
                    Grid.GetRow(Element) != Grid.GetRow(TheTabView))
                {
                    Grid.SetColumnSpan(Element, Grid.GetColumnSpan(Element) + 2);
                }
            }

            RightSplit = true;
            CurrentDockPosition = DockPosition.Right;

            Grid.SetColumnSpan(TheCanvas, 3);
        }
    }
    private void UndoPreviewRight()
    {
        if (TheRoot == null)
        {
            return;
        }

        CurrentDockPosition = null;

        foreach (FrameworkElement Element in TheRoot.Children)
        {
            if (Element is Canvas || Element is TabView)
            {
                continue;
            }
            if (Grid.GetColumn(Element) > Grid.GetColumn(TheTabView) && Grid.GetRow(Element) == Grid.GetRow(TheTabView))
            {
                Grid.SetColumn(Element, Grid.GetColumn(Element) - 2);
            }
            if (Grid.GetColumn(Element) <= Grid.GetColumn(TheTabView) &&
                Grid.GetColumn(Element) + Grid.GetColumnSpan(Element) > Grid.GetColumn(TheTabView) &&
                Grid.GetRow(Element) != Grid.GetRow(TheTabView))
            {
                Grid.SetColumnSpan(Element, Grid.GetColumnSpan(Element) - 2);
            }
        }

        TheRoot.ColumnDefinitions.RemoveAt(Grid.GetColumn(TheTabView) + Grid.GetColumnSpan(TheTabView) + 1);
        TheRoot.ColumnDefinitions.RemoveAt(Grid.GetColumn(TheTabView) + Grid.GetColumnSpan(TheTabView));

        ColumnDefinition ColDef = TheRoot.ColumnDefinitions.ElementAt(Grid.GetColumn(TheTabView));
        GridLength Length = new GridLength((ColDef.ActualWidth + 8) * 2, GridUnitType.Star);
        ColDef.Width = Length;

        Grid.SetColumnSpan(TheCanvas, 1);

        RightSplit = false;
    }
    private void PreviewBottom()
    {
        if (TheRoot == null)
        {
            return;
        }

        if (!BottomSplit)
        {
            RowDefinition TRowDef = TheRoot.RowDefinitions.ElementAt(Grid.GetRow(TheTabView));
            GridLength Height = new GridLength(TRowDef.ActualHeight / 2, GridUnitType.Star);
            TRowDef.Height = Height;

            RowDefinition RowDef = new RowDefinition();
            RowDef.Height = Height;

            int RowSpan = Grid.GetRowSpan(TheTabView);
            int Row = Grid.GetRow(TheTabView);

            TheRoot.RowDefinitions.Insert(Row + RowSpan, new RowDefinition() { Height = new GridLength(16) });
            TheRoot.RowDefinitions.Insert(Row + RowSpan + 1, RowDef);

            foreach (FrameworkElement Element in TheRoot.Children)
            {
                if (Element is Canvas || Element is TabView)
                {
                    continue;
                }
                if (Grid.GetRow(Element) > Grid.GetRow(TheTabView) &&
                    Grid.GetColumn(Element) == Grid.GetColumn(TheTabView))
                {
                    Grid.SetRow(Element, Grid.GetRow(Element) + 2);
                }
                if (Grid.GetRow(Element) <= Grid.GetRow(TheTabView) &&
                    Grid.GetRow(Element) + Grid.GetRowSpan(Element) > Grid.GetRow(TheTabView) &&
                    Grid.GetColumn(Element) != Grid.GetColumn(TheTabView))
                {
                    Grid.SetRowSpan(Element, Grid.GetRowSpan(Element) + 2);
                }
            }

            BottomSplit = true;
            CurrentDockPosition = DockPosition.Bottom;

            Grid.SetRowSpan(TheCanvas, 3);
        }
    }
    private void UndoPreviewBottom()
    {
        if (TheRoot == null)
        {
            return;
        }

        CurrentDockPosition = null;

        int RowSpan = Grid.GetRowSpan(TheTabView);
        int Row = Grid.GetRow(TheTabView);

        foreach (FrameworkElement Element in TheRoot.Children)
        {
            if (Element is Canvas || Element is TabView)
            {
                continue;
            }
            if (Grid.GetRow(Element) > Row && Grid.GetColumn(Element) == Grid.GetColumn(TheTabView))
            {
                Grid.SetRow(Element, Grid.GetRow(Element) - 2);
            }
            if (Grid.GetRow(Element) <= Row &&
                Grid.GetRow(Element) + Grid.GetRowSpan(Element) > Row &&
                Grid.GetColumn(Element) != Grid.GetColumn(TheTabView))
            {
                Grid.SetRowSpan(Element, Grid.GetRowSpan(Element) - 2);
            }
        }

        RowDefinition RowDef = TheRoot.RowDefinitions.ElementAt(Grid.GetRow(TheTabView));
        GridLength Height = new GridLength(RowDef.ActualHeight * 2, GridUnitType.Star);
        RowDef.Height = Height;

        TheRoot.RowDefinitions.RemoveAt(Grid.GetRow(TheTabView) + Grid.GetRowSpan(TheTabView) + 1);
        TheRoot.RowDefinitions.RemoveAt(Grid.GetRow(TheTabView) + Grid.GetRowSpan(TheTabView));

        Grid.SetRowSpan(TheCanvas, 1);
        BottomSplit = false;
        BlackoutFlyoutMenu();
    }
    private void PreviewTop()
    {
        if (TheRoot == null)
        {
            return;
        }

        if (!TopSplit)
        {
            RowDefinition TRowDef = TheRoot.RowDefinitions.ElementAt(Grid.GetRow(TheTabView));
            GridLength Height = new GridLength(TRowDef.ActualHeight / 2, GridUnitType.Star);
            TRowDef.Height = Height;

            RowDefinition RowDef = new RowDefinition();
            RowDef.Height = Height;

            int RowSpan = Grid.GetRowSpan(TheTabView);

            TheRoot.RowDefinitions.Insert(Grid.GetRow(TheTabView) + Grid.GetRowSpan(TheTabView), RowDef);
            TheRoot.RowDefinitions.Insert(Grid.GetRow(TheTabView) + Grid.GetRowSpan(TheTabView), new RowDefinition() { Height = new GridLength(16) });

            Grid.SetRow(TheTabView, Grid.GetRow(TheTabView) + RowSpan + 1);

            foreach (FrameworkElement Element in TheRoot.Children)
            {
                if (Element is Canvas || Element is TabView)
                {
                    continue;
                }
                if (Grid.GetRow(Element) > Grid.GetRow(TheTabView) + 2 &&
                    Grid.GetColumn(Element) == Grid.GetColumn(TheTabView))
                {
                    Grid.SetRow(Element, Grid.GetRow(Element) + 2);
                }
                if (Grid.GetRow(Element) <= Grid.GetRow(TheTabView) &&
                    Grid.GetRow(Element) + Grid.GetRowSpan(Element) > Grid.GetRow(TheTabView) - 2 &&
                    Grid.GetColumn(Element) != Grid.GetColumn(TheTabView))
                {
                    Grid.SetRowSpan(Element, Grid.GetRowSpan(Element) + 2);
                }
            }

            TopSplit = true;
            CurrentDockPosition = DockPosition.Top;

            Grid.SetRowSpan(TheCanvas, 3);
        }
    }
    private void UndoPreviewTop()
    {
        if (TheRoot == null)
        {
            return;
        }

        CurrentDockPosition = null;

        Grid.SetRow(TheTabView, Grid.GetRow(TheTabView) - Grid.GetRowSpan(TheTabView) - 1);

        foreach (FrameworkElement Element in TheRoot.Children)
        {
            if (Element is Canvas || Element is TabView)
            {
                continue;
            }
            if (Grid.GetRow(Element) > Grid.GetRow(TheTabView) && Grid.GetColumn(Element) == Grid.GetColumn(TheTabView))
            {
                Grid.SetRow(Element, Grid.GetRow(Element) - 2);
            }
            if (Grid.GetRow(Element) <= Grid.GetRow(TheTabView) &&
                Grid.GetRow(Element) + Grid.GetRowSpan(Element) > Grid.GetRow(TheTabView) &&
                Grid.GetColumn(Element) != Grid.GetColumn(TheTabView))
            {
                Grid.SetRowSpan(Element, Grid.GetRowSpan(Element) - 2);
            }
        }

        TheRoot.RowDefinitions.RemoveAt(Grid.GetRow(TheTabView) + Grid.GetRowSpan(TheTabView));
        TheRoot.RowDefinitions.RemoveAt(Grid.GetRow(TheTabView) + Grid.GetRowSpan(TheTabView));

        RowDefinition RowDef = TheRoot.RowDefinitions.ElementAt(Grid.GetRow(TheTabView));
        GridLength Height = new GridLength(RowDef.ActualHeight * 2, GridUnitType.Star);
        RowDef.Height = Height;

        Grid.SetRowSpan(TheCanvas, 1);

        TopSplit = false;
    }
    private Point LastPosition = new Point(0, 0);
    private const double MIN_MOVEMENT_THRESHOLD = 5; // Minimum movement in pixels to trigger an update
    private void WindowMoved(AppWindow Sender, AppWindowChangedEventArgs Args)
    {
        if (!Args.DidPositionChange)
        {
            return;
        }

        int XCoordinate = (int)(Sender.Position.X / ScaleFactor);
        int YCoordinate = (int)(Sender.Position.Y / ScaleFactor);

        // Check if the window has moved enough to warrant an update
        if (Math.Abs(XCoordinate - LastPosition.X) < MIN_MOVEMENT_THRESHOLD &&
            Math.Abs(YCoordinate - LastPosition.Y) < MIN_MOVEMENT_THRESHOLD)
        {
            return; // Skip processing if movement is below threshold
        }

        LastPosition = new Point(XCoordinate, YCoordinate);

        foreach ((AppWindow window, XamlRoot root) in windows.Reverse())
        {
            if (root == this.XamlRoot)
            {
                continue;
            }

            PointInt32 WindowPosition = window.Position;
            ClearDocks(root.Content);
            if (XCoordinate > (WindowPosition.X / ScaleFactor) && XCoordinate < (WindowPosition.X / ScaleFactor) + window.Size.Width &&
                YCoordinate > (WindowPosition.Y / ScaleFactor) && YCoordinate < (WindowPosition.Y / ScaleFactor) + window.Size.Height)
            {
                double CalibratedXCoor = XCoordinate - (window.Position.X / ScaleFactor);
                double CalibratedYCoor = YCoordinate - (window.Position.Y / ScaleFactor);
                sXCoor = CalibratedXCoor;
                sYCoor = CalibratedYCoor;
                FindDockResult Result = FindDock(null, root.Content, ref CalibratedXCoor, ref CalibratedYCoor);
                if (Result.Dock != null)
                {
                    Result.Dock.DockLogic(Result.XCoor, Result.YCoor, (window, root), this);
                    return;
                }
            }
        }
    }
    private void ClearDocks(in DependencyObject o)
    {
        Queue<DependencyObject> elements = new Queue<DependencyObject>();
        elements.Enqueue(o);

        while (elements.Count > 0)
        {
            DependencyObject current = elements.Dequeue();
            if (current is DockManager Dock)
            {
                Dock.RemoveAllFlyouts(false);
            }
            else
            {
                int count = VisualTreeHelper.GetChildrenCount(current);
                for (int i = 0; i < count; i++)
                {
                    elements.Enqueue(VisualTreeHelper.GetChild(current, i));
                }
            }
        }
    }
    public void RemoveAndReclaim(Grid? TheRoot, int Row, int RowSpan, int Col, int ColSpan)
    {
        bool ElementExpanded = false;
        bool ElementRemoved = false;
        bool GridSplitterRemoved = false;
        if (TheRoot == null)
        {
            TheRoot = this.TheRoot;
        }
        if (TheRoot == null)
        {
            return;
        }

        if (IsMainGrid(TheRoot))
        {
            if (ColSpan >= TheRoot.ColumnDefinitions.Count)
            {
                if (RowSpan >= TheRoot.RowDefinitions.Count)
                {
                    //RemoveWindow(TheWindow, id);
                    //TheWindow.Destroy();
                    return;
                }
                else
                {
                    foreach (FrameworkElement x in TheRoot.Children)
                    {
                        if (!ElementRemoved)
                        {
                            if (Grid.GetRow(x) == Row && x is DockManager)
                            {
                                TheRoot.Children.Remove(x);
                                ElementRemoved = true;
                                continue;
                            }
                        }
                        if (Grid.GetRow(x) > Row + RowSpan)
                        {
                            Grid.SetRow(x, Grid.GetRow(x) - RowSpan);
                        }
                    }
                    int times = RowSpan;
                    for (int i = 0; i < times; i++)
                    {
                        TheRoot.RowDefinitions.RemoveAt(TheRoot.RowDefinitions.Count - (i + 1));
                    }
                    return;
                }
            }
            else if (RowSpan >= TheRoot.RowDefinitions.Count)
            {
                foreach (FrameworkElement x in TheRoot.Children)
                {
                    if (!ElementRemoved)
                    {
                        if (Grid.GetColumn(x) == Col && x is DockManager)
                        {
                            TheRoot.Children.Remove(x);
                            ElementRemoved = true;
                            continue;
                        }
                    }
                    if (Grid.GetColumn(x) > Col + ColSpan)
                    {
                        Grid.SetColumn(x, Grid.GetColumn(x) - ColSpan);
                    }
                }
                int times = ColSpan;
                for (int i = 0; i < times; i++)
                {
                    TheRoot.ColumnDefinitions.RemoveAt(TheRoot.ColumnDefinitions.Count - (i + 1));
                }
                return;
            }
            else
            {
                foreach (FrameworkElement x in TheRoot.Children)
                {
                    if (!ElementRemoved)
                    {
                        if (Grid.GetColumn(x) == Col)
                        {
                            if (Grid.GetRow(x) == Row)
                            {
                                TheRoot.Children.Remove(x);
                                ElementRemoved = true;
                                continue;
                            }
                        }
                    }
                    if (!ElementExpanded && Grid.GetColumn(x) == (Col + ColSpan) && Grid.GetRow(x) == Row)
                    {
                        if (Grid.GetRowSpan(x) == RowSpan)
                        {
                            Grid.SetColumn(x, Col);
                            Grid.SetColumnSpan(x, Grid.GetColumnSpan(x) + ColSpan);
                            ElementExpanded = true;
                        }
                    }
                    else if (!ElementExpanded && Grid.GetRow(x) == (Row + RowSpan))
                    {
                        if (Grid.GetColumn(x) == Col)
                        {
                            if (Grid.GetColumnSpan(x) == ColSpan)
                            {
                                Grid.SetRow(x, Row);
                                Grid.SetRowSpan(x, Grid.GetRowSpan(x) + RowSpan);
                                ElementExpanded = true;
                            }
                        }
                    }
                    else if (!ElementExpanded && Grid.GetColumn(x) == (Col - ColSpan) && Grid.GetRow(x) == Row)
                    {
                        if (Grid.GetRowSpan(x) == RowSpan)
                        {
                            Grid.SetColumnSpan(x, Grid.GetColumnSpan(x) + ColSpan);
                            ElementExpanded = true;
                        }
                    }
                    else if (!ElementExpanded && Grid.GetRow(x) < Row)
                    {
                        for (int i = 0; i < ColSpan; i++)
                        {
                            foreach (FrameworkElement y in TheRoot.Children)
                            {
                                if (Grid.GetColumn(y) == (Col + i) && Grid.GetRow(y) < Row)
                                {
                                    Grid.SetRowSpan(y, Grid.GetRowSpan(y) + RowSpan);
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }
        else
        {
            if (ColSpan >= TheRoot.ColumnDefinitions.Count)
            {
                if (RowSpan >= TheRoot.RowDefinitions.Count)
                {
                    Grid? thing = GetParentGrid(this);
                    if (thing != null)
                    {
                        RemoveAndReclaim(thing, Grid.GetRow(this), Grid.GetRowSpan(this), Grid.GetColumn(this), Grid.GetColumnSpan(this));
                    }
                    return;
                }
                else
                {
                    int times = TheRoot.Children.Count;
                    for (int i = 0; i < times; i++)
                    {
                        FrameworkElement x = (FrameworkElement)TheRoot.Children.ElementAt(i);
                        if (!ElementRemoved)
                        {
                            if (Grid.GetRow(x) == Row && x is DockManager)
                            {
                                TheRoot.Children.Remove(x);
                                ElementRemoved = true;
                                i--;
                                times--;
                                continue;
                            }
                        }
                        if (!GridSplitterRemoved)
                        {
                            if (Row == 0)
                            {
                                if (Grid.GetRow(x) == Row + 1 && x is GridSplitter)
                                {
                                    TheRoot.Children.Remove(x);
                                    GridSplitterRemoved = true;
                                    i--;
                                    times--;
                                    continue;
                                }
                            }
                            else
                            {
                                if (Grid.GetRow(x) == Row - 1 && x is GridSplitter)
                                {
                                    TheRoot.Children.Remove(x);
                                    GridSplitterRemoved = true;
                                    i--;
                                    times--;
                                    continue;
                                }
                            }
                        }
                        if (Grid.GetRow(x) > Row)
                        {
                            Grid.SetRow(x, Grid.GetRow(x) - 2);
                        }
                    }
                    times = TheRoot.RowDefinitions.Count;
                    TheRoot.RowDefinitions.RemoveAt(Row);
                    if (GridSplitterRemoved)
                    {
                        if (Row == 0)
                        {
                            TheRoot.RowDefinitions.RemoveAt(Row);
                        }
                        else
                        {
                            TheRoot.RowDefinitions.RemoveAt(Row - 1);
                        }
                    }
                    return;
                }
            }
            else if (RowSpan >= TheRoot.RowDefinitions.Count)
            {
                int times = TheRoot.Children.Count;
                for (int i = 0; i < times; i++)
                {
                    FrameworkElement x = (FrameworkElement)TheRoot.Children.ElementAt(i);
                    if (!ElementRemoved)
                    {
                        if (Grid.GetColumn(x) == Col && x is DockManager)
                        {
                            TheRoot.Children.Remove(x);
                            ElementRemoved = true;
                            i--;
                            times--;
                            continue;
                        }
                    }
                    if (!GridSplitterRemoved)
                    {
                        if (Col == 0)
                        {
                            if (Grid.GetColumn(x) == Col + 1 && x is GridSplitter)
                            {
                                TheRoot.Children.Remove(x);
                                GridSplitterRemoved = true;
                                i--;
                                times--;
                                continue;
                            }
                        }
                        else
                        {
                            if (Grid.GetColumn(x) == Col - 1 && x is GridSplitter)
                            {
                                TheRoot.Children.Remove(x);
                                GridSplitterRemoved = true;
                                i--;
                                times--;
                                continue;
                            }
                        }
                    }
                    if (Grid.GetColumn(x) > Col)
                    {
                        Grid.SetColumn(x, Grid.GetColumn(x) - 2);
                    }
                }
                times = TheRoot.ColumnDefinitions.Count;
                TheRoot.ColumnDefinitions.RemoveAt(Col);
                if (GridSplitterRemoved)
                {
                    if (Col == 0)
                    {
                        TheRoot.ColumnDefinitions.RemoveAt(Col);
                    }
                    else
                    {
                        TheRoot.ColumnDefinitions.RemoveAt(Col - 1);
                    }
                }
                return;
            }
            else
            {
                foreach (FrameworkElement x in TheRoot.Children)
                {
                    if (!ElementRemoved)
                    {
                        if (Grid.GetColumn(x) == Col)
                        {
                            if (Grid.GetRow(x) == Row)
                            {
                                TheRoot.Children.Remove(x);
                                ElementRemoved = true;
                                continue;
                            }
                        }
                    }
                    if (!ElementExpanded && Grid.GetColumn(x) == (Col + 1) && Grid.GetRow(x) == Row)
                    {
                        if (Grid.GetRowSpan(x) == RowSpan)
                        {
                            Grid.SetColumn(x, Col);
                            Grid.SetColumnSpan(x, Grid.GetColumnSpan(x) + 1);
                            ElementExpanded = true;
                        }
                    }
                    else if (!ElementExpanded && Grid.GetRow(x) == (Row + 1))
                    {
                        if (Grid.GetColumn(x) == Col)
                        {
                            if (Grid.GetColumnSpan(x) == ColSpan)
                            {
                                Grid.SetRow(x, Row);
                                Grid.SetRowSpan(x, Grid.GetRowSpan(x) + 1);
                                ElementExpanded = true;
                            }
                        }
                    }
                    else if (!ElementExpanded && Grid.GetColumn(x) == (Col - 1) && Grid.GetRow(x) == Row)
                    {
                        if (Grid.GetRowSpan(x) == RowSpan)
                        {
                            Grid.SetColumnSpan(x, Grid.GetColumnSpan(x) + 1);
                            ElementExpanded = true;
                        }
                    }
                    else if (!ElementExpanded && Grid.GetRow(x) == (Row - 1))
                    {
                        for (int i = 0; i < ColSpan; i++)
                        {
                            foreach (FrameworkElement y in TheRoot.Children)
                            {
                                if (Grid.GetColumn(y) == (Col + i) && Grid.GetRow(y) == (Row - 1))
                                {
                                    Grid.SetRowSpan(y, Grid.GetRowSpan(y) + 1);
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }
    }
    private void ShowFlyout()
    {
        BlackoutFlyoutMenu();
    }
    private void RemoveAllFlyouts(bool Done)
    {
        if (TheCanvas == null || TheRoot == null)
        {
            return;
        }

        TheCanvas.Visibility = Visibility.Collapsed;
        BlackoutFlyoutMenu();
        Grid? ParentGrid = GetParentGridGrid(TheRoot);
        if (!Done)
        {
            if (ParentGrid != null)
            {
                foreach (FrameworkElement Element in ParentGrid.Children)
                {
                    if (Element is DockManager Dock)
                    {
                        Dock.RemoveAllFlyouts(true);
                    }
                }
            }
        }
        foreach (FrameworkElement Element in TheRoot.Children)
        {
            if (Element is DockManager Dock)
            {
                Dock.RemoveAllFlyouts(true);
            }
        }
    }
    private void DockLogic(in double XCoor, in double YCoor, (AppWindow, XamlRoot) window, in DockManager dragged)
    {
        if (TheTabView == null)
        {
            return;
        }

        /*
        this triggers when there is no Dock within the hovered area
        so it gets the parent which had the correct Coordinates and 
        does preview logic
         */
        ShowFlyout();
        if (YCoor > (TheTabView.ActualHeight / 2) && YCoor < ((TheTabView.ActualHeight / 2) + 50))
        {
            if (!BottomSplit && !TopSplit)
            {
                if (RightSplit)
                {
                    if (XCoor > 16 && XCoor < (66))
                    {
                        if (DisableRightDock)
                        {
                            return;
                        }
                        dragged.DockInfo = (window, DockPosition.Right);
                        ButtonHovered(RightButton);
                        PreviewRight();
                        return;
                    }
                    UndoPreviewRight();
                    return;
                }
                else if (LeftSplit)
                {
                    if (XCoor > ((TheTabView.ActualWidth) - 66) && XCoor < ((TheTabView.ActualWidth) - 16))
                    {
                        if (DisableLeftDock)
                        {
                            return;
                        }
                        dragged.DockInfo = (window, DockPosition.Left);
                        ButtonHovered(LeftButton);
                        PreviewLeft();
                        return;
                    }
                    UndoPreviewLeft();
                    return;
                }
                else
                {
                    if (XCoor > ((TheTabView.ActualWidth / 2) + 25) && XCoor < ((TheTabView.ActualWidth / 2) + 75))
                    {
                        if (DisableRightDock)
                        {
                            return;
                        }
                        dragged.DockInfo = (window, DockPosition.Right);
                        ButtonHovered(RightButton);
                        PreviewRight();
                        return;
                    }
                    else if (XCoor > ((TheTabView.ActualWidth / 2) - 75) && XCoor < ((TheTabView.ActualWidth / 2) - 25))
                    {
                        if (DisableLeftDock)
                        {
                            return;
                        }
                        DockInfo = (window, DockPosition.Left);
                        ButtonHovered(LeftButton);
                        PreviewLeft();
                        return;
                    }
                }
            }
        }
        if (XCoor > ((TheTabView.ActualWidth / 2) - 25) && XCoor < ((TheTabView.ActualWidth / 2) + 25))
        {
            if (!RightSplit || !LeftSplit)
            {
                if (BottomSplit)
                {
                    if (YCoor > 25 && YCoor < 100)
                    {
                        if (DisableBottomDock)
                        {
                            return;
                        }
                        dragged.DockInfo = (window, DockPosition.Bottom);
                        ButtonHovered(BottomButton);
                        PreviewBottom();
                        return;
                    }
                    UndoPreviewBottom();
                    return;
                }
                else if (TopSplit)
                {
                    if (YCoor > ((TheTabView.ActualHeight) - 50) && YCoor < TheTabView.ActualHeight + 1000)
                    {
                        if (DisableTopDock)
                        {
                            return;
                        }
                        dragged.DockInfo = (window, DockPosition.Top);
                        ButtonHovered(TopButton);
                        PreviewTop();
                        return;
                    }
                    UndoPreviewTop();
                    return;
                }
                else
                {
                    if (YCoor > ((TheTabView.ActualHeight / 2) + 50) && YCoor < ((TheTabView.ActualHeight / 2) + 100))
                    {
                        if (DisableBottomDock)
                        {
                            return;
                        }
                        dragged.DockInfo = (window, DockPosition.Bottom);
                        ButtonHovered(BottomButton);
                        PreviewBottom();
                        return;
                    }
                    else if (YCoor > ((TheTabView.ActualHeight / 2) - 50) && YCoor < ((TheTabView.ActualHeight / 2)))
                    {
                        if (DisableTopDock)
                        {
                            return;
                        }
                        dragged.DockInfo = (window, DockPosition.Top);
                        ButtonHovered(TopButton);
                        PreviewTop();
                        return;
                    }
                }
            }
        }
        switch (CurrentDockPosition)
        {
            case DockPosition.Top:
                UndoPreviewTop();
                break;
            case DockPosition.Bottom:
                UndoPreviewBottom();
                break;
            case DockPosition.Left:
                UndoPreviewLeft();
                break;
            case DockPosition.Right:
                UndoPreviewRight();
                break;
        }
        BlackoutFlyoutMenu();
        CurrentDockPosition = null;
        TopSplit = false;
        RightSplit = false;
        BottomSplit = false;
        LeftSplit = false;
        dragged.DockInfo = (null, DockPosition.None);
    }
    private void CoorToRowCol(in Grid? TheGrid, ref double XCoor, ref double YCoor, out int Row, out int Col)
    {
        Col = 0;
        Row = 0;

        if (TheGrid == null)
        {
            return;
        }

        foreach (ColumnDefinition ColumnDefinition in TheGrid.ColumnDefinitions)
        {
            if (XCoor < ColumnDefinition.ActualWidth)
            {
                break;
            }
            XCoor -= ColumnDefinition.ActualWidth;
            Col += 1;
        }
        foreach (RowDefinition RowDefinition in TheGrid.RowDefinitions)
        {
            if (YCoor < RowDefinition.ActualHeight)
            {
                break;
            }
            YCoor -= RowDefinition.ActualHeight;
            Row += 1;
        }
    }
    private FindDockResult FindDock(DockManager? Sender, DependencyObject? obj, ref double XCoor, ref double YCoor)
    {
        Grid? TheGrid = obj as Grid;
        if (TheGrid == null)
        {
            return new FindDockResult(null, ref XCoor, ref YCoor);
        }

        double originalXCoor = XCoor;
        double originalYCoor = YCoor;

        CoorToRowCol(TheGrid, ref XCoor, ref YCoor, out int Row, out int Col);

        DockManager? Saved = null;
        bool Done = false;

        foreach (FrameworkElement Element in TheGrid.Children)
        {
            if (Element is DockManager Dock)
            {
                if (Grid.GetColumn(Element) <= Col && (Grid.GetColumn(Element) + Grid.GetColumnSpan(Element)) > Col &&
                    Grid.GetRow(Element) <= Row && (Grid.GetRow(Element) + Grid.GetRowSpan(Element)) > Row)
                {
                    Saved = Dock;
                    continue;
                }
                Dock.RemoveAllFlyouts(Done);
                Done = true;
            }
        }
        if (Saved != null)
        {
            if (Sender != null)
            {
                Sender.RemoveAllFlyouts(true);
            }

            double DockXCoor = originalXCoor - GetColumnOffset(TheGrid, Grid.GetColumn(Saved));
            double DockYCoor = originalYCoor - GetRowOffset(TheGrid, Grid.GetRow(Saved));

            if (DockXCoor >= 0 && DockXCoor <= Saved.ActualWidth &&
                DockYCoor >= 0 && DockYCoor <= Saved.ActualHeight)
            {
                return Saved.FindDock(Saved, Saved?.TheRoot, ref DockXCoor, ref DockYCoor);
            }
        }
        return new FindDockResult(Sender, ref XCoor, ref YCoor);
    }
    private double GetColumnOffset(in Grid grid, int column)
    {
        double offset = 0;
        for (int i = 0; i < column; i++)
        {
            offset += grid.ColumnDefinitions[i].ActualWidth;
        }
        return offset;
    }
    private double GetRowOffset(in Grid grid, int row)
    {
        double offset = 0;
        for (int i = 0; i < row; i++)
        {
            offset += grid.RowDefinitions[i].ActualHeight;
        }
        return offset;
    }

    private void BlackoutFlyoutMenu()
    {
        if (TopButton != null)
        {
            TopButton.Background = new SolidColorBrush(Colors.Black);
        }

        if (LeftButton != null)
        {
            LeftButton.Background = new SolidColorBrush(Colors.Black);
        }

        if (RightButton != null)
        {
            RightButton.Background = new SolidColorBrush(Colors.Black);
        }

        if (BottomButton != null)
        {
            BottomButton.Background = new SolidColorBrush(Colors.Black);
        }
    }

    public void ButtonHovered(Button? button)
    {
        if (button != null)
        {
            button.Background = new SolidColorBrush(Colors.Blue);
        }
    }
}
public enum DockPosition
{
    Left,
    Top,
    Right,
    Bottom,
    None
}
public class FindDockResult
{
    public DockManager? Dock { get; set; }
    public double XCoor { get; set; }
    public double YCoor { get; set; }
    public FindDockResult(in DockManager? dock, ref double xCoor, ref double yCoor)
    {
        Dock = dock;
        XCoor = xCoor;
        YCoor = yCoor;
    }
}
public class DockManagerArgs
{
    public DockPosition Position { get; set; }
    public FrameworkElement DroppedElement { get; set; }
    public DockManagerArgs(FrameworkElement element, DockPosition position)
    {
        Position = position;
        DroppedElement = element;
    }
}
