using System;
using System.Windows;
using System.Windows.Threading;

namespace ShopDraw.Views
{
    /// <summary>
    /// Interaction logic for ProgressBarView.xaml
    /// </summary>
    public partial class ProgressBarView : Window
    {
        private delegate void ProgressBarDelegate();
        private ProgressBarDelegate _changedEvent = null;
        public event EventHandler CancelRequested;


        public bool Flag = true;

        public ProgressBarView()
        {

            InitializeComponent();
            _changedEvent = new ProgressBarDelegate(UpdateProgress);
        }

        public bool Update(int max, string title, bool isNewProcess = false)
        {
            if (isNewProcess)
            {
                pb.Minimum = 0;
                pb.Value = 0;
            }

            pb.Maximum = max;

            // Increment first so the displayed value matches the current step
            pb.Value++;

            TbPercent.Text = $"{title + " " + Convert.ToInt32(pb.Value) + "/" + pb.Maximum} ( {Math.Round(pb.Value * 100 / pb.Maximum, 1) + "%"} ) ";

            pb.Dispatcher?.Invoke(new ProgressBarDelegate(() => { }), DispatcherPriority.Background);
            return Flag;
        }
        public bool UpdateNumber2(int current, int max, string title, bool isNewProcess = false)
        {
            if (isNewProcess)
            {
                pb.Minimum = 0;
                pb.Value = 0;
                pb.Maximum = max;
            }

            // Set current progress
            pb.Value = current;

            double percent = pb.Maximum == 0 ? 0 : Math.Round(pb.Value * 100.0 / pb.Maximum, 1);

            TbPercent.Text = $"{title} {pb.Value}/{pb.Maximum} ({percent}%)";

            pb.Dispatcher?.Invoke(_changedEvent, DispatcherPriority.Background);
            return Flag;
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            Flag = false;
            CancelRequested?.Invoke(this, EventArgs.Empty);
        }

        private void UpdateProgress()
        {
        }

        private void ProgressBarView_OnClosed(object sender, EventArgs e)
        {
            Flag = false;
            CancelRequested?.Invoke(this, EventArgs.Empty);
        }

        private void BtClose_OnClick(object sender, RoutedEventArgs e)
        {
            Flag = false;
            CancelRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
