using System;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;

namespace TraducaoTIME.UIWPF.Behaviors
{
    /// <summary>
    /// Attached Behavior que faz auto-scroll para o final quando novos itens
    /// são adicionados a um ItemsControl dentro de um ScrollViewer
    /// </summary>
    public static class AutoScrollBehavior
    {
        public static bool GetAutoScroll(DependencyObject obj)
        {
            return (bool)obj.GetValue(AutoScrollProperty);
        }

        public static void SetAutoScroll(DependencyObject obj, bool value)
        {
            obj.SetValue(AutoScrollProperty, value);
        }

        public static readonly DependencyProperty AutoScrollProperty =
            DependencyProperty.RegisterAttached(
                "AutoScroll",
                typeof(bool),
                typeof(AutoScrollBehavior),
                new PropertyMetadata(false, AutoScrollPropertyChanged));

        private static void AutoScrollPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var scrollViewer = d as ScrollViewer;
            if (scrollViewer == null)
                return;

            if ((bool)e.NewValue)
            {
                // Quando inicializado, encontrar o ItemsControl dentro do ScrollViewer
                scrollViewer.Loaded += (sender, args) =>
                {
                    AttachToItemsSource(scrollViewer);
                };
            }
        }

        private static void AttachToItemsSource(ScrollViewer scrollViewer)
        {
            // Encontrar o ItemsControl dentro do ScrollViewer
            foreach (var child in GetVisualChildren(scrollViewer))
            {
                if (child is ItemsControl itemsControl)
                {
                    // Se ItemsSource implementa INotifyCollectionChanged, se inscrever nos eventos
                    if (itemsControl.ItemsSource is INotifyCollectionChanged notifyCollection)
                    {
                        notifyCollection.CollectionChanged += (sender, e) =>
                        {
                            if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems != null && e.NewItems.Count > 0)
                            {
                                // Agendar o scroll para o final na próxima passagem do dispatcher
                                scrollViewer.Dispatcher.BeginInvoke(
                                    new Action(() =>
                                    {
                                        scrollViewer.ScrollToEnd();
                                    }),
                                    System.Windows.Threading.DispatcherPriority.Background);
                            }
                        };
                    }
                    break;
                }
            }
        }

        private static System.Collections.Generic.IEnumerable<DependencyObject> GetVisualChildren(DependencyObject parent)
        {
            int childCount = System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childCount; i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                yield return child;
                foreach (var descendant in GetVisualChildren(child))
                {
                    yield return descendant;
                }
            }
        }
    }
}
