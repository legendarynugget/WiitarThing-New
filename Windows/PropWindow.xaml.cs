using System;
using System.Windows;
using System.Windows.Controls;

namespace WiitarThing
{
    public partial class PropWindow : Window
    {
        public bool doSave = false;
        public Property props;

        public PropWindow(Property org, string defaultName)
        {
            InitializeComponent();

            props = new Property(org);
            nameInput.Text = string.IsNullOrWhiteSpace(props.name) ? defaultName : props.name;

            if (props.autoNum >= 0 && props.autoNum < autoConnectNumber.Items.Count)
            {
                autoConnectNumber.SelectedIndex = props.autoNum;
            }
            else
            {
                autoConnectNumber.SelectedIndex = props.autoConnect ? 5 : 0;
            }

            rumbleCheckbox.IsChecked = props.useRumble;
        }

        private void nameInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (props != null)
            {
                props.name = nameInput.Text;
            }
        }

        private void AutoConnect_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (props != null)
            {
                props.autoConnect = autoConnectNumber.SelectedIndex > 0;
                props.autoNum = autoConnectNumber.SelectedIndex;
            }
        }

        private void rumbleCheckbox_Click(object sender, RoutedEventArgs e)
        {
            if (props != null)
            {
                props.useRumble = rumbleCheckbox.IsChecked == true;
            }
        }

        private void saveBtn_Click(object sender, RoutedEventArgs e)
        {
            doSave = true;
            DialogResult = true;
            Close();
        }

        private void cancelBtn_Click(object sender, RoutedEventArgs e)
        {
            doSave = false;
            DialogResult = false;
            Close();
        }
    }
}