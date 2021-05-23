using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Threading;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WindowsInput.Native;
using WindowsInput;
using System.ComponentModel;
using CronExpressionDescriptor;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Collections.ObjectModel;
using HandyControl.Controls;
using System.Windows.Controls;
using Quartz.Impl;
using Quartz;
using System.Windows.Media.Imaging;

namespace WeChatHelper
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : HandyControl.Controls.Window
    {
        private ObservableCollection<SendTask> taskDataList = new ObservableCollection<SendTask>();
        private SendTask curSendTask = new SendTask("", "", new TaskCron(DateTime.Now));
        private SystemConfig sysConfig = new SystemConfig(Key.W, ModifierKeys.Shift | ModifierKeys.Alt, "");

        private StdSchedulerFactory schedulerFactory = new StdSchedulerFactory();
        private IScheduler scheduler;

        private System.Windows.Threading.DispatcherTimer wechatMonitorTimer = new System.Windows.Threading.DispatcherTimer();
        private System.Windows.Threading.DispatcherTimer timeDisplayTimer = new System.Windows.Threading.DispatcherTimer();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            DataBindingInit();
            ScheduleInit();
            TimerInit();
        }

        private void DataBindingInit()
        {
            this.dgTaskData.ItemsSource = taskDataList;
            this.tbxSend2Name.DataContext = curSendTask;
            this.tbxSendContent.DataContext = curSendTask;
            this.tbxWechatHoyKey.DataContext = sysConfig;
            this.tpTaskTime.DataContext = curSendTask.SendCron;
            this.lbxSendWeeks.ItemsSource = curSendTask.SendCron.SendWeeksList;
            this.tbxCustomCron.DataContext = curSendTask.SendCron;
            this.rbtnUseCustomCron.DataContext = curSendTask.SendCron;
        }

        private async void ScheduleInit()
        {
            scheduler = await schedulerFactory.GetScheduler();
            await scheduler.Start();
        }

        private void TimerInit()
        {
            wechatMonitorTimer.Tick += new EventHandler(WechatMonitorTimerExecute);
            wechatMonitorTimer.Interval = new TimeSpan(0, 0, 1);
            wechatMonitorTimer.Start();

            timeDisplayTimer.Tick += new EventHandler(TimeDisplayTimerExecute);
            timeDisplayTimer.Interval = new TimeSpan(0, 0, 1);
            timeDisplayTimer.Start();
        }

        private void TimeDisplayTimerExecute(object sender, EventArgs e)
        {
            this.tbCurrentTime.Text = DateTime.Now.ToString("F");
        }

        private void WechatMonitorTimerExecute(object sender, EventArgs e)
        {
            wechatMonitorTimer.Stop(); 
            if (NativeMethods.IsWeChatRun())
            {
                this.imgWeChat.Source = new BitmapImage(new Uri("yes.png", UriKind.Relative));
            }
            else
            {
                this.imgWeChat.Source = new BitmapImage(new Uri("no.png", UriKind.Relative));
            }
            wechatMonitorTimer.Start();
        }

        private void tbxWechatHoyKey_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Don't let the event pass further
            // because we don't want standard textbox shortcuts working
            e.Handled = true;

            // Get modifiers and key data
            var modifiers = Keyboard.Modifiers;
            var key = e.Key;

            // When Alt is pressed, SystemKey is used instead
            if (key == Key.System)
            {
                key = e.SystemKey;
            }

            // Pressing delete, backspace or escape without modifiers clears the current value
            if (modifiers == ModifierKeys.None &&
                (key == Key.Delete || key == Key.Back || key == Key.Escape))
            {
                return;
            }

            // If no actual key was pressed - return
            if (key == Key.LeftCtrl ||
                key == Key.RightCtrl ||
                key == Key.LeftAlt ||
                key == Key.RightAlt ||
                key == Key.LeftShift ||
                key == Key.RightShift ||
                key == Key.LWin ||
                key == Key.RWin ||
                key == Key.Clear ||
                key == Key.OemClear ||
                key == Key.Apps)
            {
                return;
            }

            // Update the value
            sysConfig.UpdateHotKey(key, modifiers);
        }

        private void btnHotKeyTest_Click(object sender, RoutedEventArgs e)
        {
            if (!NativeMethods.IsWeChatRun())
            {
                HandyControl.Controls.MessageBox.Error("未找到微信进程，请先启动微信", "错误");
                return;
            }
            sysConfig.SimulateKeyPress();
        }

        private async void btnAddTask_Click(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrEmpty(curSendTask.Send2Name) ||
                String.IsNullOrEmpty(curSendTask.SendContent))
            {
                HandyControl.Controls.MessageBox.Error("发送对象或内容为空", "错误");
                return;
            }
            SendTask sendTask = (SendTask)curSendTask.Clone();
            if (await sendTask.StartSchedule(scheduler, sysConfig))
            {
                taskDataList.Add(sendTask);
            }
        }

        private async void btnDeleteTask_Click(object sender, RoutedEventArgs e)
        {
            if (this.dgTaskData.SelectedItems.Count > 1)
            {
                MessageBoxResult result = HandyControl.Controls.MessageBox.Ask(String.Format("确认删除选中的 {0} 项任务", this.dgTaskData.SelectedItems.Count), "提示");
                if (result != MessageBoxResult.OK)
                {
                    return;
                }
            }
            Collection<SendTask> selectedSendTask = new Collection<SendTask>();
            foreach (var item in this.dgTaskData.SelectedItems)
            {
                int rowIndex = this.dgTaskData.Items.IndexOf(item);
                selectedSendTask.Add(taskDataList[rowIndex]);
            }
            foreach (var task in selectedSendTask)
            {
                await task.StopSchedule(scheduler);
                taskDataList.Remove(task);
            }
        }

        private void btnSendTest_Click(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrEmpty(curSendTask.Send2Name) ||
                String.IsNullOrEmpty(curSendTask.SendContent))
            {
                HandyControl.Controls.MessageBox.Error("发送对象或内容为空", "错误");
                return;
            }
            MessageBoxResult result = HandyControl.Controls.MessageBox.Ask(String.Format("确认发送消息给：{0}", curSendTask.Send2Name), "提示");
            if (result != MessageBoxResult.OK)
            {
                return;
            }
            curSendTask.DoSendOperation(sysConfig);
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (this.WindowState == WindowState.Minimized)
            {
                this.Visibility = Visibility.Hidden;
            }
        }

        private void NotifyIcon_Click(object sender, RoutedEventArgs e)
        {
            if (this.Visibility == Visibility.Visible)
            {
                this.Visibility = Visibility.Hidden;
            }
            else
            {
                this.Visibility = Visibility.Visible;
                this.WindowState = WindowState.Normal;
                this.Activate();
            }
        }
    }
}
