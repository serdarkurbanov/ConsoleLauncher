using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ConsoleLauncher.UIHelpers
{
    public class UIExtensions
    {
        #region InputBindings for TreeViewItem
        // attached property to set InputBindings to elements like TreeViewItem via style
        // taken from http://stackoverflow.com/questions/1104601/xaml-how-to-have-global-inputbindings
        public static readonly DependencyProperty InputBindingsProperty = DependencyProperty.RegisterAttached(
            "InputBindings", 
            typeof(InputBindingCollection), 
            typeof(UIExtensions),
            new FrameworkPropertyMetadata(
                new InputBindingCollection(),
                (sender, e) =>
                {
                    var element = sender as UIElement;
                    if (element == null) return;
                    element.InputBindings.Clear();
                    element.InputBindings.AddRange((InputBindingCollection)e.NewValue);
                }
        ));

        public static InputBindingCollection GetInputBindings(UIElement element)
        {
            return (InputBindingCollection)element.GetValue(InputBindingsProperty);
        }

        public static void SetInputBindings(UIElement element, InputBindingCollection inputBindings)
        {
            element.SetValue(InputBindingsProperty, inputBindings);
        }

        #endregion

        #region scrolling listbox

        // scrolling listbox automatically to bottom
        // taken from http://stackoverflow.com/questions/2006729/how-can-i-have-a-listbox-auto-scroll-when-a-new-item-is-added
        static readonly Dictionary<ListBox, Capture> Associations =
       new Dictionary<ListBox, Capture>();

        public static bool GetScrollOnNewItem(DependencyObject obj)
        {
            return (bool)obj.GetValue(ScrollOnNewItemProperty);
        }

        public static void SetScrollOnNewItem(DependencyObject obj, bool value)
        {
            obj.SetValue(ScrollOnNewItemProperty, value);
        }

        public static readonly DependencyProperty ScrollOnNewItemProperty =
            DependencyProperty.RegisterAttached(
                "ScrollOnNewItem",
                typeof(bool),
                typeof(UIExtensions),
                new UIPropertyMetadata(false, OnScrollOnNewItemChanged));

        public static void OnScrollOnNewItemChanged(
            DependencyObject d,
            DependencyPropertyChangedEventArgs e)
        {
            var listBox = d as ListBox;
            if (listBox == null) return;
            bool oldValue = (bool)e.OldValue, newValue = (bool)e.NewValue;
            if (newValue == oldValue) return;
            if (newValue)
            {
                listBox.Loaded += ListBox_Loaded;
                listBox.Unloaded += ListBox_Unloaded;
                var itemsSourcePropertyDescriptor = TypeDescriptor.GetProperties(listBox)["ItemsSource"];
                itemsSourcePropertyDescriptor.AddValueChanged(listBox, ListBox_ItemsSourceChanged);
            }
            else
            {
                listBox.Loaded -= ListBox_Loaded;
                listBox.Unloaded -= ListBox_Unloaded;
                if (Associations.ContainsKey(listBox))
                    Associations[listBox].Dispose();
                var itemsSourcePropertyDescriptor = TypeDescriptor.GetProperties(listBox)["ItemsSource"];
                itemsSourcePropertyDescriptor.RemoveValueChanged(listBox, ListBox_ItemsSourceChanged);
            }
        }

        private static void ListBox_ItemsSourceChanged(object sender, EventArgs e)
        {
            var listBox = (ListBox)sender;
            if (Associations.ContainsKey(listBox))
                Associations[listBox].Dispose();
            Associations[listBox] = new Capture(listBox);
        }

        static void ListBox_Unloaded(object sender, RoutedEventArgs e)
        {
            var listBox = (ListBox)sender;
            if (Associations.ContainsKey(listBox))
                Associations[listBox].Dispose();
            listBox.Unloaded -= ListBox_Unloaded;
        }

        static void ListBox_Loaded(object sender, RoutedEventArgs e)
        {
            var listBox = (ListBox)sender;
            var incc = listBox.Items as INotifyCollectionChanged;
            if (incc == null) return;
            listBox.Loaded -= ListBox_Loaded;
            Associations[listBox] = new Capture(listBox);
        }

        class Capture : IDisposable
        {
            private readonly ListBox listBox;
            private readonly INotifyCollectionChanged incc;

            public Capture(ListBox listBox)
            {
                this.listBox = listBox;
                incc = listBox.ItemsSource as INotifyCollectionChanged;
                if (incc != null)
                {
                    incc.CollectionChanged += incc_CollectionChanged;
                }
            }

            void incc_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
            {
                if (e.Action == NotifyCollectionChangedAction.Add)
                {
                    listBox.ScrollIntoView(e.NewItems[0]);
                    listBox.SelectedItem = e.NewItems[0];
                }
            }

            public void Dispose()
            {
                if (incc != null)
                    incc.CollectionChanged -= incc_CollectionChanged;
            }
        }
        #endregion

        #region editable tooltip

        #endregion

        #region drag selection for ListBox
        // taken from http://stackoverflow.com/questions/2869566/wpf-listview-drag-select-multiple-items

        // need a static reference to the listbox otherwise it can't be accessed
        // (this only happened in the project I'm working on, if you're using a regular ListBox, with regular ListBoxItems you can get the ListBox from the ListBoxItems)
        public static ListBox ListBox { get; private set; }

        public static bool GetIsDragSelectionEnabled(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsDragSelectionEnabledProperty);
        }

        public static void SetIsDragSelectionEnabled(DependencyObject obj, bool value)
        {
            obj.SetValue(IsDragSelectionEnabledProperty, value);
        }

        public static readonly DependencyProperty IsDragSelectionEnabledProperty =
            DependencyProperty.RegisterAttached("IsDragSelectingEnabled", typeof(bool), typeof(UIExtensions), new UIPropertyMetadata(false, IsDragSelectingEnabledPropertyChanged));

        public static void IsDragSelectingEnabledPropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            ListBox listBox = o as ListBox;

            bool isDragSelectionEnabled = UIExtensions.GetIsDragSelectionEnabled(listBox);

            // if DragSelection is enabled
            if (isDragSelectionEnabled)
            {
                // set the listbox's selection mode to multiple ( didn't work with extended )
                listBox.SelectionMode = SelectionMode.Multiple;

                // set the static listbox property
                UIExtensions.ListBox = listBox;

                // and subscribe to the required events to handle the drag selection and the attached properties
                listBox.PreviewMouseLeftButtonDown += new MouseButtonEventHandler(UIExtensions.listBox_PreviewMouseLeftButtonDown);
                listBox.PreviewMouseRightButtonDown += new MouseButtonEventHandler(listBox_PreviewMouseRightButtonDown);
                listBox.MouseLeftButtonUp += new MouseButtonEventHandler(UIExtensions.listBox_MouseLeftButtonUp);
            }
            else // is selection is disabled
            {
                // set selection mode to the default
                listBox.SelectionMode = SelectionMode.Single;

                // dereference the listbox
                UIExtensions.ListBox = null;

                // unsuscribe from the events
                listBox.PreviewMouseLeftButtonDown -= new MouseButtonEventHandler(UIExtensions.listBox_PreviewMouseLeftButtonDown);
                listBox.MouseLeftButtonUp -= new MouseButtonEventHandler(UIExtensions.listBox_MouseLeftButtonUp);
                listBox.MouseLeftButtonUp -= new MouseButtonEventHandler(UIExtensions.listBox_MouseLeftButtonUp);
            }
        }

        static void listBox_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            // to prevent the listbox from selecting / deselecting wells on right click
            e.Handled = true;
        }

        private static void listBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // notify the helper class that the listbox has initiated the drag click
            UIExtensions.SetIsDragClickStarted(UIExtensions.ListBox, true);
        }

        private static void listBox_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            // notify the helper class that the list box has terminated the drag click
            UIExtensions.SetIsDragClickStarted(UIExtensions.ListBox, false);
        }

        public static bool GetIsDragSelecting(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsDragSelectingProperty);
        }

        public static void SetIsDragSelecting(DependencyObject obj, bool value)
        {
            obj.SetValue(IsDragSelectingProperty, value);
        }

        public static readonly DependencyProperty IsDragSelectingProperty =
            DependencyProperty.RegisterAttached("IsDragSelecting", typeof(bool), typeof(UIExtensions), new UIPropertyMetadata(false, IsDragSelectingPropertyChanged));

        public static void IsDragSelectingPropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            ListBoxItem item = o as ListBoxItem;

            bool clickInitiated = UIExtensions.GetIsDragClickStarted(UIExtensions.ListBox);

            // this is where the item.Parent was null, it was supposed to be the ListBox, I guess it's null because items are not
            // really ListBoxItems but are wells
            if (clickInitiated)
            {
                bool isDragSelecting = UIExtensions.GetIsDragSelecting(item);

                if (isDragSelecting)
                {
                    // using the ListBox static reference because could not get to it through the item.Parent property
                    UIExtensions.ListBox.SelectedItems.Add(item);
                }
            }
        }


        public static bool GetIsDragClickStarted(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsDragClickStartedProperty);
        }

        public static void SetIsDragClickStarted(DependencyObject obj, bool value)
        {
            obj.SetValue(IsDragClickStartedProperty, value);
        }

        public static readonly DependencyProperty IsDragClickStartedProperty =
            DependencyProperty.RegisterAttached("IsDragClickStarted", typeof(bool), typeof(UIExtensions), new UIPropertyMetadata(false, IsDragClickStartedPropertyChanged));

        public static void IsDragClickStartedPropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            bool isDragClickStarted = UIExtensions.GetIsDragClickStarted(UIExtensions.ListBox);

            // if click has been drag click has started, clear the current selected items and start drag selection operation again
            if (isDragClickStarted)
                UIExtensions.ListBox.SelectedItems.Clear();
        }

        #endregion drag selection for ListBox
    }
}
