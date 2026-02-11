using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using FrostNova.Core.Wpf.VisualHelpers;
using Microsoft.Xaml.Behaviors;

namespace FrostNova.Core.Wpf.Behaviors;

/// <summary>
/// synchronize scroll positions between two ScrollViewers. (can attach to ScrollViewer or ItemsControl.)(two way)
/// </summary>
/// <remarks>
/// ScrollViewerのスクロール位置を同期します ScrollViewer, ItemsControlにアタッチ可能 (双方向)
/// </remarks>
public class ScrollSyncBehavior : Behavior<FrameworkElement>
{
    private bool _isSyncing = false;
    private const double EPSILON = 0.5; // ジッタ抑制用のしきい値


    private ScrollViewer? _associatedScrollViewer;
    private ScrollViewer? _targetScrollViewer;

    #region DependencyProperty and Changed Callbacks

    /// <summary>
    /// enable vertical sync
    /// </summary>
    /// <remarks>
    /// 縦スクロールを有効にするかを設定します
    /// </remarks>
    public bool IsVerticalSync
    {
        get { return (bool)GetValue(IsVerticalSyncProperty); }
        set { SetValue(IsVerticalSyncProperty, value); }
    }
    public static DependencyProperty IsVerticalSyncProperty = DependencyProperty.Register(nameof(IsVerticalSync), typeof(bool), typeof(ScrollSyncBehavior), new PropertyMetadata(false));

    /// <summary> 
    /// enable horizontal sync
    /// </summary>
    /// <remarks>
    /// 横スクロールを有効にするかどうかを設定します
    /// </remarks>
    public bool IsHorizontalSync
    {
        get { return (bool)GetValue(IsHorizontalSyncProperty); }
        set { SetValue(IsHorizontalSyncProperty, value); }
    }
    public static DependencyProperty IsHorizontalSyncProperty = DependencyProperty.Register(nameof(IsHorizontalSync), typeof(bool), typeof(ScrollSyncBehavior), new PropertyMetadata(false));



    /// <summary>
    /// target scrollviewer
    /// </summary>
    public FrameworkElement SyncTarget
    {
        get { return (FrameworkElement)GetValue(SyncTargetProperty); }
        set { SetValue(SyncTargetProperty, value); }
    }
    public static DependencyProperty SyncTargetProperty = DependencyProperty.Register(nameof(SyncTarget), typeof(FrameworkElement), typeof(ScrollSyncBehavior), new PropertyMetadata(null, OnSyncTargetChanged));


    private static void OnSyncTargetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ScrollSyncBehavior b)
        {
            if (e.OldValue is FrameworkElement oldSv)
            {
               b. DetachFromResolvedTarget();
            }
            if (e.NewValue is FrameworkElement newSv)
            {
                b.TryResolveTargetScrollViewer(newSv);
            }
        }
    }

    #endregion

    #region initialization and events

    protected override void OnAttached()
    {
        base.OnAttached();
        TryResolveAssociatedScrollViewer();

        TryResolveTargetScrollViewer(SyncTarget);
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        DetachFromResolvedAssociated();
        DetachFromResolvedTarget();
    }


    private void TryResolveAssociatedScrollViewer()
    {
        DetachFromResolvedAssociated();

        GeneralHelper.ResolveChildWhenLoaded<ScrollViewer>(AssociatedObject, (sv) =>
        {
            _associatedScrollViewer = sv;
            _associatedScrollViewer.ScrollChanged += OnAssociatedObjectScrollChanged;

            // 接続時の初期同期
            Sync(_associatedScrollViewer, _targetScrollViewer, null);
        });
    }

    private void TryResolveTargetScrollViewer(FrameworkElement? rawTarget)
    {
        DetachFromResolvedTarget();

        GeneralHelper.ResolveChildWhenLoaded<ScrollViewer>(rawTarget, (sv) =>
        {
            _targetScrollViewer = sv;
            _targetScrollViewer.ScrollChanged += OnSyncTargetScrollChanged;

            // 接続時の初期同期
            Sync(_targetScrollViewer, _associatedScrollViewer, null);
        });
    }

    private void DetachFromResolvedTarget()
    {
        if (_targetScrollViewer != null)
        {
            _targetScrollViewer.ScrollChanged -= OnSyncTargetScrollChanged;
            _targetScrollViewer = null;
        }
    }

    private void DetachFromResolvedAssociated()
    {
        if (_associatedScrollViewer != null)
        {
            _associatedScrollViewer.ScrollChanged -= OnAssociatedObjectScrollChanged;
            _associatedScrollViewer = null;
        }
    }

    private void OnAssociatedObjectScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (_isSyncing || _associatedScrollViewer == null || _targetScrollViewer == null) return;
        Sync(_associatedScrollViewer, _targetScrollViewer, e);
    }

    private void OnSyncTargetScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (_isSyncing || _associatedScrollViewer == null || _targetScrollViewer == null) return;
        Sync(_targetScrollViewer, _associatedScrollViewer, e);
    }

    #endregion

    private void Sync(ScrollViewer source, ScrollViewer? target, ScrollChangedEventArgs? e)
    {

        if (_isSyncing || target == null) return;

        _isSyncing = true;
        try
        {
            // 垂直同期
            if (IsVerticalSync && (e == null ||e.VerticalChange != 0))
            {
                if (Math.Abs(target.VerticalOffset - source.VerticalOffset) > EPSILON)
                {
                    target.ScrollToVerticalOffset(source.VerticalOffset);
                }
            }

            // 水平同期
            if (IsHorizontalSync && (e == null || e.HorizontalChange != 0))
            {
                if (Math.Abs(target.HorizontalOffset - source.HorizontalOffset) > EPSILON)
                {
                    target.ScrollToHorizontalOffset(source.HorizontalOffset);
                }
            }
        }
        finally
        {
            _isSyncing = false;
        }
    }


}
